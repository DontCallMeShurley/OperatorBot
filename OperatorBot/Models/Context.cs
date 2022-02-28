using Microsoft.EntityFrameworkCore;


namespace OperatorBot.Models
{
    public class Context : DbContext
    {
        public Context()
        {
        }
        public DbSet<Driver> Driver { get; set; }
        public DbSet<Licenser> Licenser { get; set; }
        public DbSet<Settings> Settings { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Driver>()
                .HasOne(p => p.licenser)
                .WithMany(b => b.drivers)
                .HasForeignKey(p => p.licenser_id);

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=OperatorBot;Trusted_Connection=True;");
        }

    }
}
