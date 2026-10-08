using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmaCase.Models;

/// <summary>Gasto del negocio: suscripciones, transporte, publicidad, empaques, etc.</summary>
public class Gasto
{
    public int Id { get; set; }

    [DataType(DataType.Date)]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Escriba en qué se gastó")]
    [StringLength(150)]
    [Display(Name = "Concepto")]
    public string Concepto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elija una categoría")]
    [StringLength(60)]
    [Display(Name = "Categoría")]
    public string Categoria { get; set; } = string.Empty;

    [Range(1, 999999999, ErrorMessage = "Ingrese un monto válido")]
    [Column(TypeName = "decimal(14,2)")]
    public decimal Monto { get; set; }

    [StringLength(30)]
    [Display(Name = "Método de pago")]
    public string MetodoPago { get; set; } = "Efectivo";

    [StringLength(300)]
    public string? Nota { get; set; }

    public static readonly string[] CategoriasSugeridas =
    {
        "Suscripciones y apps", "Transporte y gasolina", "Publicidad", "Empaques y envíos",
        "Servicios (internet, celular)", "Herramientas y equipos", "Comisiones y bancos", "Otros"
    };

    public static string Icono(string categoria) => categoria switch
    {
        "Suscripciones y apps" => "bi-phone-vibrate",
        "Transporte y gasolina" => "bi-fuel-pump",
        "Publicidad" => "bi-megaphone",
        "Empaques y envíos" => "bi-box2-heart",
        "Servicios (internet, celular)" => "bi-wifi",
        "Herramientas y equipos" => "bi-tools",
        "Comisiones y bancos" => "bi-bank",
        _ => "bi-receipt"
    };
}

public class CategoriaTotalVM
{
    public string Categoria { get; set; } = "";
    public decimal Total { get; set; }
    public int Cantidad { get; set; }
}
