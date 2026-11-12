using Fundoo.User.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Fundoo.User.Infrastructure.Persistence
{
    public class FundooContext : DbContext
    {
        public FundooContext(DbContextOptions<FundooContext> options) : base(options)
        {
        }

        public DbSet<UserEntity> Users { get; set; }
    }
}

