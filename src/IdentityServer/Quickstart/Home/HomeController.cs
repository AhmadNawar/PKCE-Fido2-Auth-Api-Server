// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System;

namespace IdentityServerHost.Quickstart.UI
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger _logger;
        public const string ChallangeSessionKeyName = "_Challange";

        public HomeController(IIdentityServerInteractionService interaction, IWebHostEnvironment environment, ILogger<HomeController> logger)
        {
            _interaction = interaction;
            _environment = environment;
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (_environment.IsDevelopment())
            {
                // only show in development
                return View();
            }

            _logger.LogInformation("Homepage is disabled in production. Returning 404.");
            return NotFound();
        }

        /// <summary>
        /// Shows the error page
        /// </summary>
        public async Task<IActionResult> Error(string errorId)
        {
            var vm = new ErrorViewModel();

            // retrieve error details from identityserver
            var message = await _interaction.GetErrorContextAsync(errorId);
            if (message != null)
            {
                vm.Error = message;

                if (!_environment.IsDevelopment())
                {
                    // only show in development
                    message.ErrorDescription = null;
                }
            }

            return View("Error", vm);
        }

        [Route("/callback")]
        public IActionResult AutthenticationCallback()
        {
            return View("Callback");
        }

        [Route("/generate-qr-challange")]
        [HttpGet]
        public IActionResult GenerateQRChallange()
        {
            byte[] randomBytes = new byte[128];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }

            string base64String = Convert.ToBase64String(randomBytes);

            Random random = new Random();
            int length = random.Next(43, 128);
            var challange = base64String.Substring(0, length);

            HttpContext.Session.SetString(ChallangeSessionKeyName, challange);

            return new OkObjectResult(new {code = challange });
        }
        [Route("/get-challange")]
        [HttpGet]
        public IActionResult GeChallange()
        {
            var challange = HttpContext.Session.GetString(ChallangeSessionKeyName);

            return Ok(challange);
        }
    }
}