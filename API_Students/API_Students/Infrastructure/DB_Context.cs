using API_Students.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API_Students.Infrastructure
{
    public class DB_Context : IdentityDbContext
    {
        public DbSet<RefreshToken> RefreshToken { get; set; }

        public DB_Context()
        {
                
        }

        public DB_Context(DbContextOptions<DB_Context> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=DESKTOP-2O26GRI\SQLEXPRESS; initial catalog=Students;Trusted_Connection=true;Encrypt=false;TrustServerCertificate=true;");
        }
    }
}
