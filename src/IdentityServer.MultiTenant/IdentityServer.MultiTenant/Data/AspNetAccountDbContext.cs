using IdentityServer.MultiTenant.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityServer.MultiTenant.Data
{
    public class AspNetAccountDbContext:IdentityDbContext<ApplicationUser>
    {
        public AspNetAccountDbContext(DbContextOptions<AspNetAccountDbContext> options) : base(options) {

        }

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);
        }
    }
}
