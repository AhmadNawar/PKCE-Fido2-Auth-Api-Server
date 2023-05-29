using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fido2NetLib.Objects;
using Fido2NetLib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using IdentityServer4.Services;
using IdentityServer4.Events;
using IdentityServerHost.Quickstart.UI;
using Microsoft.AspNetCore.Authentication;
using IdentityServer4;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using static Fido2NetLib.AuthenticatorAssertionRawResponse;
using IdentityServer.Entities;

namespace IdentityServer.Fido2
{
    [Route("api/[controller]")]
    public class PwFido2SignInController : Controller
    {
        private readonly Fido2NetLib.Fido2 _lib;
        public static IMetadataService _mds;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly Fido2Storage _fido2Storage;
        private readonly UserManager<QRCodeUser> _userManager;
        private readonly SignInManager<QRCodeUser> _signInManager;
        private readonly IOptions<Fido2Configuration> _optionsFido2Configuration;
        private readonly IEventService _events;

        public PwFido2SignInController(
            IEventService events,
            IIdentityServerInteractionService interaction,
            Fido2Storage fido2Storage,
            UserManager<QRCodeUser> userManager,
            SignInManager<QRCodeUser> signInManager,
            IOptions<Fido2Configuration> optionsFido2Configuration)
        {
            _userManager = userManager;
            _optionsFido2Configuration = optionsFido2Configuration;
            _signInManager = signInManager;
            _userManager = userManager;
            _fido2Storage = fido2Storage;
            _interaction = interaction;
            _events = events;

            _lib = new Fido2NetLib.Fido2(new Fido2Configuration()
            {
                ServerDomain = _optionsFido2Configuration.Value.ServerDomain,
                ServerName = _optionsFido2Configuration.Value.ServerName,
                Origin = _optionsFido2Configuration.Value.Origin,
                TimestampDriftTolerance = _optionsFido2Configuration.Value.TimestampDriftTolerance
            });
        }

        private string FormatException(Exception e)
        {
            return string.Format("{0}{1}", e.Message, e.InnerException != null ? " (" + e.InnerException.Message + ")" : "");
        }

        [HttpPost]
        [Route("/pwassertionOptions")]
        public async Task<ActionResult> AssertionOptionsPost([FromForm] string username, [FromForm] string userVerification)
        {
            try
            {

                var existingCredentials = new List<PublicKeyCredentialDescriptor>();

                if (!string.IsNullOrEmpty(username))
                {
                    var identityUser = await _userManager.FindByNameAsync(username);
                    var user = new Fido2User
                    {
                        DisplayName = identityUser.UserName,
                        Name = identityUser.UserName,
                        Id = Encoding.UTF8.GetBytes(identityUser.UserName) // byte representation of userID is required
                    };

                    if (user == null) throw new ArgumentException("Username was not registered");

                    // 2. Get registered credentials from database
                    var items = await _fido2Storage.GetCredentialsByUsername(identityUser.UserName);
                    existingCredentials = items.Select(c => c.Descriptor).ToList();
                }

                var exts = new AuthenticationExtensionsClientInputs() { 
                    SimpleTransactionAuthorization = "FIDO", 
                    GenericTransactionAuthorization = new TxAuthGenericArg { 
                        ContentType = "text/plain", 
                        Content = new byte[] { 0x46, 0x49, 0x44, 0x4F } 
                    }, 
                    UserVerificationIndex = true,
                    Location = true, 
                    UserVerificationMethod = true 
                };

                // 3. Create options
                var uv = string.IsNullOrEmpty(userVerification) ? UserVerificationRequirement.Discouraged : userVerification.ToEnum<UserVerificationRequirement>();
                var options = _lib.GetAssertionOptions(
                    existingCredentials,
                    uv,
                    exts
                );

                // 4. Temporarily store options, session/in-memory cache/redis/db
                HttpContext.Session.SetString("fido2.assertionOptions", options.ToJson());

                // 5. Return options to client
                return Json(options);
            }

            catch (Exception e)
            {
                return Json(new AssertionOptions { Status = "error", ErrorMessage = FormatException(e) });
            }
        }

        [HttpPost]
        [Route("/pwmakeAssertion")]
        public async Task<JsonResult> MakeAssertion([FromBody] AuthenticatorAssertionRawResponseWithReturnUrl clientResponse)
        {
            try
            {
                // check if we are in the context of an authorization request
                var context = await _interaction.GetAuthorizationContextAsync(clientResponse.ReturnUrl);
                // 1. Get the assertion options we sent the client
                var jsonOptions = HttpContext.Session.GetString("fido2.assertionOptions");
                var options = AssertionOptions.FromJson(jsonOptions);

                // 2. Get registered credential from database
                var creds = await _fido2Storage.GetCredentialById(clientResponse.Id);

                if (creds == null)
                {
                    throw new Exception("Unknown credentials");
                }

                // 3. Get credential counter from database
                var storedCounter = creds.SignatureCounter;

                // 4. Create callback to check if userhandle owns the credentialId
                IsUserHandleOwnerOfCredentialIdAsync callback = async (args) =>
                {
                    var storedCreds = await _fido2Storage.GetCredentialsByUserHandleAsync(args.UserHandle);
                    return storedCreds.Exists(c => c.Descriptor.Id.SequenceEqual(args.CredentialId));
                };

                // 5. Make the assertion
                var res = await _lib.MakeAssertionAsync(clientResponse, options, creds.PublicKey, storedCounter, callback);

                // 6. Store the updated counter
                await _fido2Storage.UpdateCounter(res.CredentialId, res.Counter);

                var identityUser = await _userManager.FindByNameAsync(creds.Username);
                if (identityUser == null)
                {
                    throw new InvalidOperationException($"Unable to load user.");
                }
                await _events.RaiseAsync(new UserLoginSuccessEvent(creds.Username, identityUser.Id, identityUser.UserName, clientId: context?.Client.ClientId));

                // only set explicit expiration here if user chooses "remember me". 
                // otherwise we rely upon expiration configured in cookie middleware.
                AuthenticationProperties props = null;

                // issue authentication cookie with subject ID and username
                var isuser = new IdentityServerUser(identityUser.Id)
                {
                    DisplayName = identityUser.UserName
                };

                await HttpContext.SignInAsync(isuser);
                await _signInManager.SignInAsync(identityUser, isPersistent: false);

                // 7. return OK to client
                return Json(res);
            }
            catch (Exception e)
            {
                return Json(new AssertionVerificationResult { Status = "error", ErrorMessage = FormatException(e) });
            }
        }

        public class AuthenticatorAssertionRawResponseWithReturnUrl : AuthenticatorAssertionRawResponse
        {

            [JsonProperty("returnUrl")]
            public string ReturnUrl { get; set; }
        }

        private AuthenticatorAssertionRawResponse MapAuthenticatorAssertionWithUrlToNormal(AuthenticatorAssertionRawResponseWithReturnUrl authRequest)
        {
            var mappedResponse = new AuthenticatorAssertionRawResponse()
            {
                Id = authRequest.Id,
                RawId = authRequest.RawId,
                Extensions = authRequest.Extensions,
                Response = new AssertionResponse()
                {
                    AuthenticatorData = authRequest.Response.AuthenticatorData,
                    Signature = authRequest.Response.Signature,
                    ClientDataJson = authRequest.Response.ClientDataJson,
                    UserHandle = authRequest.Response.UserHandle,
                },
                Type = authRequest.Type,
            };

            return mappedResponse;

        }
    }
}
