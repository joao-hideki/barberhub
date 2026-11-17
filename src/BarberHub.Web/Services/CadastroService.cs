using BarberHub.Web.Data;
using BarberHub.Web.Domain;

namespace BarberHub.Web.Services;

public record NovaBarbearia(string Nome, string Email, string Senha, Plano Plano);

public class CadastroService(AppDbContext db, TimeProvider relogio)
{
    public const int DiasGratis = 14;

    public async Task<Resultado<Barbearia>> CadastrarBarbearia(NovaBarbearia dados)
    {
        var hoje = DateOnly.FromDateTime(relogio.AgoraLocal());
        var barbearia = new Barbearia
        {
            Nome = dados.Nome,
            Email = dados.Email,
            Plano = dados.Plano,
            EmPeriodoGratis = true,
            AssinaturaValidaAte = hoje.AddDays(DiasGratis)
        };
        db.Barbearias.Add(barbearia);
        await db.SaveChangesAsync();
        return Resultado<Barbearia>.Ok(barbearia);
    }
}
