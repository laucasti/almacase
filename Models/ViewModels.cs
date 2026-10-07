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
}

public class DashboardVM
{
    public decimal VendidoMes { get; set; }
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
    public List<(string Producto, int Unidades, decimal Total)> TopProductos { get; set; } = new();
}
