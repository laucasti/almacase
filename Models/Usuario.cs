using System.ComponentModel.DataAnnotations;

namespace AlmaCase.Models;

public class Usuario
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Nombre { get; set; }

    /// <summary>Contraseña cifrada (nunca se guarda en texto plano).</summary>
    [Required, StringLength(300)]
    public string PasswordHash { get; set; } = string.Empty;
}

public class LoginVM
{
    [Required(ErrorMessage = "Ingrese el usuario")]
    [Display(Name = "Usuario")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese la contraseña")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class CambiarPasswordVM
{
    [Required(ErrorMessage = "Ingrese su contraseña actual")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string Actual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese la nueva contraseña")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string Nueva { get; set; } = string.Empty;

    [Compare(nameof(Nueva), ErrorMessage = "Las contraseñas no coinciden")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nueva contraseña")]
    public string Confirmar { get; set; } = string.Empty;
}
