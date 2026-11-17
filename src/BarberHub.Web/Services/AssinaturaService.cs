using BarberHub.Web.Data;
using BarberHub.Web.Domain;
using BarberHub.Web.Pagamentos;

namespace BarberHub.Web.Services;

public class AssinaturaService(AppDbContext db, IGatewayPagamento gateway, TimeProvider relogio)
{
    public Task<Resultado<Barbearia>> Checkout(int barbeariaId, Plano plano, DadosCartao cartao) => throw new NotImplementedException();
}
