using System.Globalization;
using BarberHub.Web.Data;
using BarberHub.Web.Pagamentos;
using BarberHub.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Interface: Razor Pages renderizadas no servidor; tudo em /Painel exige login.
builder.Services.AddRazorPages(o => o.Conventions.AuthorizeFolder("/Painel"));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Entrar";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// Armazenamento: PostgreSQL via Entity Framework Core.
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Lógica da aplicação: serviços de domínio (cobertos pelos testes) + injeção de dependência.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<CadastroService>();
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddScoped<AgendamentoService>();
builder.Services.AddScoped<AssinaturaService>();
// Para usar um gateway real, troque apenas esta linha (ex.: MercadoPagoGateway).
builder.Services.AddSingleton<IGatewayPagamento, GatewayPagamentoSimulado>();

var app = builder.Build();

var ptBr = new CultureInfo("pt-BR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(ptBr),
    SupportedCultures = [ptBr],
    SupportedUICultures = [ptBr]
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

await DadosIniciais.PrepararBanco(app.Services);

app.Run();
