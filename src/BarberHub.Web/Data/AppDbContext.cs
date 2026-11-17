using BarberHub.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace BarberHub.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Barbearia> Barbearias => Set<Barbearia>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Barbeiro> Barbeiros => Set<Barbeiro>();
    public DbSet<HorarioFuncionamento> Expedientes => Set<HorarioFuncionamento>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Barbearia>().HasIndex(b => b.Email).IsUnique();
        modelBuilder.Entity<Barbearia>().HasIndex(b => b.Slug).IsUnique();
        modelBuilder.Entity<HorarioFuncionamento>().HasIndex(h => new { h.BarbeariaId, h.DiaSemana }).IsUnique();
        modelBuilder.Entity<Servico>().Property(s => s.Preco).HasPrecision(10, 2);
        modelBuilder.Entity<Agendamento>().Property(a => a.Preco).HasPrecision(10, 2);
        modelBuilder.Entity<Pagamento>().Property(p => p.Valor).HasPrecision(10, 2);
        modelBuilder.Entity<Cliente>().HasIndex(c => new { c.BarbeariaId, c.Telefone }).IsUnique();

        // Datas são gravadas no horário local da barbearia (sem fuso).
        foreach (var propriedade in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
        {
            propriedade.SetColumnType("timestamp without time zone");
        }
    }
}
