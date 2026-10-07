using System.Globalization;
using AlmaCase.Controllers;
using AlmaCase.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Datos privados (contraseña de MySQL, usuario admin) van en appsettings.Local.json,
// que NO se sube a Git. En el servidor se configuran como variables de entorno.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Render (y otros hosting) indican el puerto en la variable PORT.
var puerto = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(puerto))
    builder.WebHost.UseUrls($"http://0.0.0.0:{puerto}");

// Todas las páginas exigen haber iniciado sesión, excepto las marcadas con [AllowAnonymous].
builder.Services.AddControllersWithViews(o =>
    o.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Cuenta/Login";
        o.LogoutPath = "/Cuenta/Logout";
        o.AccessDeniedPath = "/Cuenta/Login";
        o.Cookie.Name = "AlmaCase.Sesion";
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.SlidingExpiration = true;
    });

var cadena = builder.Configuration.GetConnectionString("AlmaCaseDb");
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseMySql(cadena, new MySqlServerVersion(new Version(8, 0, 36)),
        my => my.EnableRetryOnFailure(3)));

// Las llaves que cifran la sesión se guardan en MySQL; así no se cierra la sesión
// cada vez que el servidor gratuito se "duerme" y se reinicia.
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>()
    .SetApplicationName("AlmaCase");

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// Crea la base de datos y las tablas automáticamente la primera vez que se ejecuta.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Tablas agregadas después de la primera versión (por si la base ya existía).
    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS `Usuarios` (
            `Id` int NOT NULL AUTO_INCREMENT,
            `NombreUsuario` varchar(50) NOT NULL,
            `Nombre` varchar(100) NULL,
            `PasswordHash` varchar(300) NOT NULL,
            PRIMARY KEY (`Id`),
            UNIQUE KEY `IX_Usuarios_NombreUsuario` (`NombreUsuario`)
        ) CHARACTER SET utf8mb4;
        """);
    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS `DataProtectionKeys` (
            `Id` int NOT NULL AUTO_INCREMENT,
            `FriendlyName` longtext NULL,
            `Xml` longtext NULL,
            PRIMARY KEY (`Id`)
        ) CHARACTER SET utf8mb4;
        """);

    CuentaController.CrearAdminInicial(db, app.Configuration, app.Logger);
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Los números de los formularios se leen con punto decimal (input type="number"),
// sin importar la configuración regional de Windows. En pantalla los valores se
// muestran en formato colombiano con el helper Formato.Cop().
var invariante = CultureInfo.InvariantCulture;
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(invariante),
    SupportedCultures = [invariante],
    SupportedUICultures = [invariante]
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
