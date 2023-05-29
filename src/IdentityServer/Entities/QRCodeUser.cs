using Microsoft.AspNetCore.Identity;
using System;

namespace IdentityServer.Entities
{
    public class QRCodeUser: IdentityUser
    {
        public string PublicKey { get; set; }
        public string LastChallenge { get; set; }
        public string SessionId { get; set; }
        public DateTime SessionEexpirationDate { get; set; }
        public bool IsAuthenticated { get; set; }
    }
}
