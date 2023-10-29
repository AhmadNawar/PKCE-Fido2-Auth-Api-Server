using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System;
using IdentityServer.Entities;
using IdentityServer.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using IdentityServer.Models;
using System.Text;
using Microsoft.AspNetCore.Identity;
using IdentityServer4;

namespace IdentityServer.QRCodeAuthentication
{
    [Route("api/[controller]")]
    [ApiController]
    public class QRCodeAuthenticationController : ControllerBase
    {

        public const string ChallangeSessionKeyName = "_Challange";
        const int SessionTimeOutHours = 1;

        public ApplicationDbContext _context { get; }
        private readonly SignInManager<QRCodeUser> _signInManager;

        public QRCodeAuthenticationController(ApplicationDbContext context, SignInManager<QRCodeUser> signInManager)
        {
            _context = context;
            _signInManager = signInManager;
        }

        [HttpGet("generate-qr-challange/{username}")]
        public async Task<IActionResult> GenerateQRChallange(string username)
        {
            byte[] randomBytes = new byte[128];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }

            string base64String = Convert.ToBase64String(randomBytes);
            string randomSessionID = Guid.NewGuid().ToString().Substring(0, 10);

            Random random = new Random();
            int length = random.Next(43, 128);
            var challange = base64String.Substring(0, length);

            var existingUser = await _context.QRCodeUsers.FirstOrDefaultAsync(x => x.UserName == username);

            if (existingUser == null)
            {
                var newUser = new QRCodeUser
                {
                    UserName = username,
                    LastChallenge = challange,
                    SessionId = randomSessionID,
                    SessionEexpirationDate = DateTime.UtcNow.AddHours(SessionTimeOutHours),
                    IsAuthenticated = false
                };

                await _context.QRCodeUsers.AddAsync(newUser);
            } else
            {
                existingUser.LastChallenge = challange;
                existingUser.SessionId = randomSessionID;
                existingUser.SessionEexpirationDate = DateTime.UtcNow.AddHours(SessionTimeOutHours);
                existingUser.IsAuthenticated = false;
                _context.QRCodeUsers.Update(existingUser);
            }

            await _context.SaveChangesAsync();

            return new OkObjectResult(new { code = challange, sessionId = randomSessionID });
        }

        [Route("submit-public-key")]
        [HttpPost]
        public async Task<IActionResult> SubmitPublicKey(SubmitPublicKeyModel model)
        {
            var existingUser = await _context.QRCodeUsers.FirstOrDefaultAsync(x => x.UserName == model.Username);

            if (existingUser == null || existingUser.SessionId != model.SessionId)
            {
                return NotFound();
            }

            if (existingUser.SessionEexpirationDate < DateTime.UtcNow)
            {
                return BadRequest(new
                {
                    error = "Session Expired"
                });
            }

            var isValidSignature = VerifySignature(existingUser.LastChallenge, model.SignedChallenge, model.PublicKey);

            if (!isValidSignature)
            {
                return BadRequest("Signature Not Valid");
            }

            existingUser.PublicKey = model.PublicKey;
            existingUser.IsAuthenticated = true;
            _context.QRCodeUsers.Update(existingUser);
            await _context.SaveChangesAsync();

            return Ok();

        }

        [Route("get-session-status")]
        [HttpGet]
        public async Task<IActionResult> GeChallange(string username, string sessionId)
        {
            var user = await _context.QRCodeUsers.FirstOrDefaultAsync(x => x.UserName == username);

            if (user  == null || user.SessionId != sessionId)
            {
                return NotFound();
            }
            
            if (user.SessionEexpirationDate < DateTime.UtcNow)
            {
                return BadRequest(new {
                    error = "Session Expired"
                });
            }

            // issue authentication cookie with subject ID and username
            var isuser = new IdentityServerUser(user.Id)
            {
                DisplayName = user.UserName
            };

            var responseMessage = "Session pending approvel";
            var authenticated = false;

            if (user.IsAuthenticated)
            {
                await HttpContext.SignInAsync(isuser);
                await _signInManager.SignInAsync(user, isPersistent: false);
                responseMessage = "User authenticated";
                authenticated = true;
            }
            return Ok(new { authenticated = authenticated,  message = responseMessage});
        }

        [Route("verify-challenge")]
        [HttpPost]
        public async Task<IActionResult> VerifyChallenge(VerifyChallengeModel model)
        {
            var existingUser = await _context.QRCodeUsers.FirstOrDefaultAsync(x => x.UserName == model.Username);

            if (existingUser == null || existingUser.SessionId != model.SessionId)
            {
                return NotFound();
            }

            if (existingUser.SessionEexpirationDate < DateTime.UtcNow)
            {
                return BadRequest(new
                {
                    error = "Session Expired"
                });
            }

            if (string.IsNullOrEmpty(existingUser.PublicKey))
            {
                return BadRequest("User hasn't submitted a public key yet.");
            }


            var isValidSignature = VerifySignature(existingUser.LastChallenge, model.SignedChallenge, existingUser.PublicKey);

            if (!isValidSignature)
            {
                return BadRequest("Signature Not Valid");
            }

            existingUser.IsAuthenticated = true;
            _context.QRCodeUsers.Update(existingUser);
            await _context.SaveChangesAsync();

            return Ok();

        }

        public static bool VerifySignature(string challenge, string signature, string publicKeyPem)
        {
            // Decode the PEM-encoded public key
            var publicKeyBytes = Convert.FromBase64String(publicKeyPem
                .Replace("-----BEGIN RSA PUBLIC KEY-----", "")
                .Replace("-----END RSA PUBLIC KEY-----", "")
                .Replace("\n", ""));

            // Create an RSA object and initialize it with the public key
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(publicKeyBytes, out _);

            // Decode the signature from base64
            var signatureBytes = Convert.FromBase64String(signature);

            // Compute the SHA512 hash of the original message
            var sha512 = new SHA512Managed();
            var hashBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(challenge));

            // Verify the signature using RSA
            var verified = rsa.VerifyHash(hashBytes, signatureBytes, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);

            return verified;
        }
    }
}
