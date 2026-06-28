using WmsPortal.Core.Interfaces;
using Microsoft.AspNetCore.HttpsPolicy;
using WmsPortal.Data.Providers;
using WmsPortal.Data.Repositories;
using WmsPortal.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(8);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

// Conexión maestra (HANA - Depor) desde appsettings
string masterDbType  = builder.Configuration["Master:DbType"]  ?? "HANA";
string masterConnStr = builder.Configuration["Master:ConnStr"]  ?? "";
string masterSchema  = builder.Configuration["Master:Schema"]   ?? "CLPRD_WMS";

// Infraestructura
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<TransaccionRepository>();
builder.Services.AddScoped<WmsStageRepository>();
builder.Services.AddScoped(sp => new UserRepository(
    sp.GetRequiredService<IDbConnectionFactory>(),
    masterDbType, masterConnStr, masterSchema));

// Servicios de negocio
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITransaccionService, TransaccionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IWmsStageService, WmsStageService>();

// For IIS with multiple addresses: set explicit HTTPS port to avoid Ambiguous IServerAddressesFeature
builder.Services.Configure<HttpsRedirectionOptions>(options =>
{
    options.HttpsPort = 443;
});

var app = builder.Build();

app.UsePathBase("/WmsPortal");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Index}/{id?}");

app.Run();
