using Microsoft.EntityFrameworkCore;
using Randomizer.Models;

namespace Randomizer.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            {
                var connection = Database.GetDbConnection();
                Console.WriteLine($"Connected to database: {connection.Database} on server: {connection.DataSource}");
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // Pretvori imena tabel v male črke
                entity.SetTableName(entity.GetTableName().ToLower());
            }
        }

        public DbSet<Podrocje> Podrocje { get; set; }
        public DbSet<Podpodrocje> Podpodrocje { get; set; }
        public DbSet<Prijave> Prijave { get; set; }
        public DbSet<Recenzent> Recenzenti { get; set; }
        public DbSet<IzloceniCOI> IzloceniCOI { get; set; }
        public DbSet<IzloceniOsebni> IzloceniOsebni { get; set; }
        public DbSet<Grozdi> Grozdi { get; set; }
        public DbSet<PrijavaGrozdi> PrijavaGrozdi { get; set; }
        public DbSet<GrozdiRecenzenti> GrozdiRecenzenti { get; set; }
        public DbSet<RecenzentiPodrocja> RecenzentiPodrocja { get; set; }
        public DbSet<RecenzentiZavrnitve> RecenzentiZavrnitve { get; set; }
        public DbSet<Randomizer.Models.RecenzentiPodpodrocjaFull> RecenzentiPodpodrocjaFull { get; set; } = default!;
        public DbSet<Randomizer.Models.GrozdiRecenzentiZavrnitve> GrozdiRecenzentiZavrnitve { get; set; } = default!;
    }
}
