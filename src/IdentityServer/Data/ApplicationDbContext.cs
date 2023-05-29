//using Fido2Identity;
using IdentityServer.Entities;
using IdentityServer.Fido2;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityServer.Data
{
    public class ApplicationDbContext : IdentityDbContext<QRCodeUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<FidoStoredCredential> FidoStoredCredential { get; set; }
        public DbSet<QRCodeUser> QRCodeUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<FidoStoredCredential>().HasKey(m => m.Id);
            builder.Entity<QRCodeUser>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.HasIndex(m => m.UserName).IsUnique();
            });

            base.OnModelCreating(builder);
        }
    }
}
