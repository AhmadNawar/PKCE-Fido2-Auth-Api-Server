//using Fido2Identity;
using IdentityServer.Fido2;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityServer.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<FidoStoredCredential> FidoStoredCredential { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<FidoStoredCredential>().HasKey(m => m.Id);

            base.OnModelCreating(builder);
        }
    }
}
