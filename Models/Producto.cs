using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmaCase.Models;

public class Producto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(80)]
    [Display(Name = "Categoría")]
    public string? Categoria { get; set; }

    [StringLength(500)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria")]
    [Range(0, int.MaxValue, ErrorMessage = "La cantidad no puede ser negativa")]
    [Display(Name = "Cantidad en stock")]
    public int Cantidad { get; set; }

    [Required(ErrorMessage = "El precio es obligatorio")]
    [Range(0, 999999999, ErrorMessage = "El precio no es válido")]
    [Column(TypeName = "decimal(14,2)")]
    [Display(Name = "Precio de venta")]
    public decimal Precio { get; set; }

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public bool Activo { get; set; } = true;
}
