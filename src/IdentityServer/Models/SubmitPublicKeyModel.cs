namespace IdentityServer.Models
{
    public class SubmitPublicKeyModel : VerifyChallengeModel
    {
        public string PublicKey { get; set; }
    }
}
