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
                 Hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, vm.Password) != PasswordVerificationResult.Failed;

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

        if (Hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, vm.Actual) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(vm.Actual), "La contraseña actual no es correcta.");
            return View(vm);
        }

        usuario.PasswordHash = Hasher.HashPassword(usuario, vm.Nueva);
        await db.SaveChangesAsync();
        TempData["Ok"] = "Contraseña actualizada.";
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Crea el usuario administrador la primera vez, con los datos de la configuración
    /// (Admin:Usuario y Admin:Password). Si ya existen usuarios no hace nada.
    /// </summary>
    public static void CrearAdminInicial(AppDbContext db, IConfiguration config, ILogger logger)
    {
        if (db.Usuarios.Any()) return;

        var nombreUsuario = config["Admin:Usuario"];
        var password = config["Admin:Password"];
        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No hay usuarios. Configure Admin:Usuario y Admin:Password para crear el administrador.");
            return;
        }

        var admin = new Usuario { NombreUsuario = nombreUsuario.Trim(), Nombre = "Administrador" };
        admin.PasswordHash = Hasher.HashPassword(admin, password);
        db.Usuarios.Add(admin);
        db.SaveChanges();
        logger.LogInformation("Usuario administrador '{Usuario}' creado.", admin.NombreUsuario);
    }
}
