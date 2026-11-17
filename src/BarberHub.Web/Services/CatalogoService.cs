using BarberHub.Web.Data;
using BarberHub.Web.Domain;

namespace BarberHub.Web.Services;

public class CatalogoService(AppDbContext db)
{
    public Task<Resultado<Servico>> CadastrarServico(int barbeariaId, string nome, decimal preco, int duracaoMinutos) =>
        throw new NotImplementedException();

    public Task<List<Servico>> ListarServicosAtivos(int barbeariaId) => throw new NotImplementedException();
}
