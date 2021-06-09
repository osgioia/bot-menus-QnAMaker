using Microsoft.EntityFrameworkCore;
using QnABot.Models;

namespace QnABot
{
    public partial class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
        }

        public DbSet<UserReportedQuestion> UserReportedQuestion { get; set; }
        public DbSet<UserQnAReceived> UserQnAReceived { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
