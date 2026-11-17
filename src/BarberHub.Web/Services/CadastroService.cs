using BarberHub.Web.Data;
using BarberHub.Web.Domain;

namespace BarberHub.Web.Services;

public record NovaBarbearia(string Nome, string Email, string Senha, Plano Plano);

public class CadastroService(AppDbContext db, TimeProvider relogio)
{
    public Task<Resultado<Barbearia>> CadastrarBarbearia(NovaBarbearia dados) => throw new NotImplementedException();
}
