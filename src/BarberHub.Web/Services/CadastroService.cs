using BarberHub.Web.Data;
using System.Globalization;
using System.Text;
using BarberHub.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace BarberHub.Web.Services;

public record NovaBarbearia(string Nome, string Email, string Senha, Plano Plano);

public class CadastroService(AppDbContext db, TimeProvider relogio)
{
    public const int DiasGratis = 14;

    public async Task<Resultado<Barbearia>> CadastrarBarbearia(NovaBarbearia dados)
    {
        var email = (dados.Email ?? "").Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(dados.Nome))
            return Resultado<Barbearia>.Falha("Informe o nome da barbearia.");
        if (!System.Net.Mail.MailAddress.TryCreate(email, out _))
            return Resultado<Barbearia>.Falha("Informe um e-mail válido.");
        if ((dados.Senha ?? "").Length < 6)
            return Resultado<Barbearia>.Falha("A senha deve ter pelo menos 6 caracteres.");
        if (await db.Barbearias.AnyAsync(b => b.Email == email))
            return Resultado<Barbearia>.Falha("Este e-mail já está cadastrado.");

        var hoje = DateOnly.FromDateTime(relogio.AgoraLocal());
        var barbearia = new Barbearia
        {
            Nome = dados.Nome.Trim(),
            Slug = await GerarSlugUnico(dados.Nome),
            Email = email,
            Plano = dados.Plano,
            EmPeriodoGratis = true,
            AssinaturaValidaAte = hoje.AddDays(DiasGratis)
        };
        db.Barbearias.Add(barbearia);
        await db.SaveChangesAsync();
        return Resultado<Barbearia>.Ok(barbearia);
    }

    /// <summary>"Barbearia do Zé" -> "barbearia-do-ze"; se já existir, "barbearia-do-ze-2", "-3"...</summary>
    private async Task<string> GerarSlugUnico(string nome)
    {
        var semAcento = new StringBuilder();
        foreach (var c in nome.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                semAcento.Append(char.ToLowerInvariant(c));

        var palavras = new string(semAcento.ToString().Select(c => char.IsAsciiLetterOrDigit(c) ? c : ' ').ToArray())
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var baseSlug = string.Join('-', palavras);

        var slug = baseSlug;
        for (var n = 2; await db.Barbearias.AnyAsync(b => b.Slug == slug); n++)
            slug = $"{baseSlug}-{n}";
        return slug;
    }

    public Task<Barbearia?> Autenticar(string email, string senha) => throw new NotImplementedException();
}
