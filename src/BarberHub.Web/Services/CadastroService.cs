using BarberHub.Web.Data;
using System.Globalization;
using System.Text;
using BarberHub.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BarberHub.Web.Services;

public record NovaBarbearia(string Nome, string Email, string Senha, Plano Plano);

public class CadastroService(AppDbContext db, TimeProvider relogio)
{
    public const int DiasGratis = 14;

    // Hash com salt (PBKDF2) do próprio ASP.NET Core: a senha nunca é gravada em texto puro.
    private static readonly PasswordHasher<Barbearia> Hasher = new();

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
        barbearia.SenhaHash = Hasher.HashPassword(barbearia, dados.Senha!);
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

    public async Task<Barbearia?> Autenticar(string email, string senha)
    {
        var normalizado = (email ?? "").Trim().ToLowerInvariant();
        var barbearia = await db.Barbearias.SingleOrDefaultAsync(b => b.Email == normalizado);
        if (barbearia is null)
            return null;

        var verificacao = Hasher.VerifyHashedPassword(barbearia, barbearia.SenhaHash, senha ?? "");
        return verificacao == PasswordVerificationResult.Failed ? null : barbearia;
    }
}
