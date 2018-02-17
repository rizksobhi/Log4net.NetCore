using Log4net.NetCore.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Log4net.NetCore.Data
{
    public class Log4netDBContext : DbContext
    {
        public virtual DbSet<Log> Logs { get; set; }

        public Log4netDBContext(DbContextOptions<Log4netDBContext> options)
            : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Log>().ToTable(nameof(Log));

            base.OnModelCreating(modelBuilder);
        }
    }
}
