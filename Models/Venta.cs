using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmaCase.Models;

/// <summary>Una compra que un cliente le hace a Alma Case.</summary>
public class Venta
{
    public int Id { get; set; }

    [Display(Name = "Cliente")]
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(14,2)")]
    public decimal Total { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }

    public List<DetalleVenta> Detalles { get; set; } = new();
    public List<Pago> Pagos { get; set; } = new();

    [NotMapped]
    public decimal TotalPagado => Pagos.Sum(p => p.Monto);

    [NotMapped]
    public decimal Saldo => Total - TotalPagado;

    [NotMapped]
    public string Estado => Saldo <= 0 ? "Pagada" : TotalPagado > 0 ? "Abonada" : "Pendiente";
}

public class DetalleVenta
{
    public int Id { get; set; }

    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int Cantidad { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal PrecioUnitario { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal Subtotal { get; set; }
}

/// <summary>Pago o abono que hace un cliente sobre una venta.</summary>
public class Pago
{
    public int Id { get; set; }

    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(14,2)")]
    public decimal Monto { get; set; }

    [StringLength(30)]
    [Display(Name = "Método de pago")]
    public string MetodoPago { get; set; } = "Efectivo";

    [StringLength(200)]
    public string? Nota { get; set; }

    public static readonly string[] Metodos = { "Efectivo", "Nequi", "Daviplata", "Transferencia", "Tarjeta", "Otro" };
}
