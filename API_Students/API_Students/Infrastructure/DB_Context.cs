using API_Students.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API_Students.Infrastructure
{
    public class DB_Context : IdentityDbContext
    {
        public DbSet<RefreshToken> RefreshToken { get; set; } = null!;

        public DB_Context()
        {
                
        }

        public DB_Context(DbContextOptions<DB_Context> options) : base(options)
        {
        }
    }
}
