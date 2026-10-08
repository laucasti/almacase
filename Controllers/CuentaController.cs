using System.Security.Claims;
using AlmaCase.Data;
using AlmaCase.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmaCase.Controllers;

public class CuentaController(AppDbContext db) : Controller
{
    private static readonly PasswordHasher<Usuario> Hasher = new();

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginVM { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == vm.Usuario.Trim());
        var ok = usuario != null &&
                 Hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, vm.Password.Trim()) != PasswordVerificationResult.Failed;

        if (!ok)
        {
            await Task.Delay(800); // frena los intentos de adivinar contraseñas
            ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
            return View(vm);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario!.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre ?? usuario.NombreUsuario)
        };
        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return Redirect(vm.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult CambiarPassword() => View(new CambiarPasswordVM());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await db.Usuarios.FindAsync(id);
        if (usuario == null) return RedirectToAction(nameof(Login));

        if (Hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, vm.Actual.Trim()) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(vm.Actual), "La contraseña actual no es correcta.");
            return View(vm);
        }

        usuario.PasswordHash = Hasher.HashPassword(usuario, vm.Nueva.Trim());
        await db.SaveChangesAsync();
        TempData["Ok"] = "Contraseña actualizada.";
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Crea el usuario administrador la primera vez, con los datos de la configuración
    /// (Admin:Usuario y Admin:Password). Si ya existen usuarios no hace nada.
    /// </summary>
    /// <remarks>
    /// Si Admin:Restablecer = true, en cada arranque fija la contraseña de Admin:Usuario
    /// (y lo crea si no existe). Sirve para recuperar el acceso; luego se debe quitar.
    /// </remarks>
    public static void CrearAdminInicial(AppDbContext db, IConfiguration config, ILogger logger)
    {
        var nombreUsuario = config["Admin:Usuario"]?.Trim();
        var password = config["Admin:Password"]?.Trim(); // evita espacios pegados por error
        var restablecer = string.Equals(config["Admin:Restablecer"]?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        var hayUsuarios = db.Usuarios.Any();

        if (hayUsuarios && !restablecer)
        {
            logger.LogInformation("Usuarios registrados: {Usuarios}",
                string.Join(", ", db.Usuarios.Select(u => u.NombreUsuario)));
            return;
        }

        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No hay usuarios. Configure Admin__Usuario y Admin__Password (con dos guiones bajos) para crear el administrador.");
            return;
        }

        var admin = db.Usuarios.FirstOrDefault(u => u.NombreUsuario == nombreUsuario);
        if (admin == null)
        {
            admin = new Usuario { NombreUsuario = nombreUsuario, Nombre = "Administrador" };
            db.Usuarios.Add(admin);
        }
        admin.PasswordHash = Hasher.HashPassword(admin, password);
        db.SaveChanges();

        logger.LogWarning(restablecer
            ? "Contraseña del usuario '{Usuario}' restablecida. Quite la variable Admin__Restablecer."
            : "Usuario administrador '{Usuario}' creado.", admin.NombreUsuario);
    }
}
