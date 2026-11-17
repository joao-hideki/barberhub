using System.Security.Claims;
using BarberHub.Web.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BarberHub.Web.Infra;

/// <summary>Login por cookie: o Id da barbearia vai no cookie e isola os dados de cada cliente do SaaS.</summary>
public static class Sessao
{
    public static int BarbeariaId(this ClaimsPrincipal usuario) =>
        int.Parse(usuario.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static Task Entrar(HttpContext http, Barbearia barbearia)
    {
        var identidade = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, barbearia.Id.ToString()),
            new Claim(ClaimTypes.Name, barbearia.Nome)
        ], CookieAuthenticationDefaults.AuthenticationScheme);
        return http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidade));
    }

    public static Task Sair(HttpContext http) => http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}
