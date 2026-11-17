using BarberHub.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace BarberHub.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Barbearia> Barbearias => Set<Barbearia>();
    public DbSet<Servico> Servicos => Set<Servico>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Servico>().Property(s => s.Preco).HasPrecision(10, 2);

        // Datas são gravadas no horário local da barbearia (sem fuso).
        foreach (var propriedade in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
        {
            propriedade.SetColumnType("timestamp without time zone");
        }
    }
}
