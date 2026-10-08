using System.ComponentModel.DataAnnotations;

namespace AlmaCase.Models;

public class VentaCrearVM
{
    [Required(ErrorMessage = "Seleccione un cliente")]
    [Display(Name = "Cliente")]
    public int? ClienteId { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    public string? Observaciones { get; set; }

    public List<ItemVM> Items { get; set; } = new();

    [Display(Name = "Abono inicial")]
    [Range(0, 999999999, ErrorMessage = "Monto no válido")]
    public decimal AbonoInicial { get; set; }

    [Display(Name = "Método de pago")]
    public string MetodoPago { get; set; } = "Efectivo";
}

public class ItemVM
{
    public int ProductoId { get; set; }
    // El costo no se recibe del formulario: se toma del producto al guardar.
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class PagoVM
{
    public int VentaId { get; set; }

    [Range(1, 999999999, ErrorMessage = "Ingrese un monto válido")]
    public decimal Monto { get; set; }

    public string MetodoPago { get; set; } = "Efectivo";
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string? Nota { get; set; }
}

public class ResumenMes
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public int NumeroVentas { get; set; }
    public int UnidadesVendidas { get; set; }
    public decimal TotalVendido { get; set; }
    /// <summary>Dinero que entró en el mes (pagos registrados con fecha de este mes).</summary>
    public decimal TotalRecaudado { get; set; }
    /// <summary>Saldo que aún deben las ventas hechas en este mes.</summary>
    public decimal PendientePorCobrar { get; set; }
    /// <summary>Lo que costaron (precio de compra) los productos vendidos en el mes.</summary>
    public decimal CostoVendido { get; set; }
    public decimal Ganancia => TotalVendido - CostoVendido;
    public decimal? Margen => TotalVendido > 0 ? Ganancia * 100 / TotalVendido : null;
}

public class DashboardVM
{
    public decimal VendidoMes { get; set; }
    public decimal GananciaMes { get; set; }
    public decimal? MargenMes { get; set; }
    public decimal CostoInventario { get; set; }
    public decimal RecaudadoMes { get; set; }
    public decimal TotalPorCobrar { get; set; }
    public int VentasMes { get; set; }
    public int TotalClientes { get; set; }
    public int ClientesConDeuda { get; set; }
    public int TotalProductos { get; set; }
    public int UnidadesInventario { get; set; }
    public decimal ValorInventario { get; set; }
    public List<Producto> BajoStock { get; set; } = new();
    public List<Venta> UltimasVentas { get; set; } = new();
    public List<ResumenMes> UltimosMeses { get; set; } = new();
}

public class DeudorVM
{
    public Cliente Cliente { get; set; } = null!;
    public List<Venta> Ventas { get; set; } = new();
    public decimal TotalDeuda => Ventas.Sum(v => v.Saldo);
}

public class ReporteAnualVM
{
    public int Anio { get; set; }
    public List<int> AniosDisponibles { get; set; } = new();
    public List<ResumenMes> Meses { get; set; } = new();
    public List<ProductoVendidoVM> TopProductos { get; set; } = new();
    /// <summary>Hay ventas del año con productos sin precio de compra (la ganancia sale más alta de lo real).</summary>
    public bool HayCostosIncompletos { get; set; }
}

public class ProductoVendidoVM
{
    public string Producto { get; set; } = "";
    public int Unidades { get; set; }
    public decimal Total { get; set; }
    public decimal Costo { get; set; }
    public decimal Ganancia => Total - Costo;
}
