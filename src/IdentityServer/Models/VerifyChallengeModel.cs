namespace IdentityServer.Models
{
    public class VerifyChallengeModel
    {
        public string Username { get; set; }
        public string SignedChallenge { get; set; }
        public string SessionId { get; set; }
    }
}
