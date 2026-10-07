using System.ComponentModel.DataAnnotations;

namespace AlmaCase.Models;

public class Cliente
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Documento")]
    public string? Documento { get; set; }

    [StringLength(30)]
    [Display(Name = "Teléfono / WhatsApp")]
    public string? Telefono { get; set; }

    [StringLength(150)]
    [EmailAddress(ErrorMessage = "Correo no válido")]
    [Display(Name = "Correo")]
    public string? Email { get; set; }

    [StringLength(80)]
    [Display(Name = "Instagram")]
    public string? Instagram { get; set; }

    [StringLength(200)]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [StringLength(80)]
    public string? Ciudad { get; set; }

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public List<Venta> Ventas { get; set; } = new();
}
