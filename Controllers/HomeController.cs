using System.Diagnostics;
using AlmaCase.Data;
using AlmaCase.Models;
using AlmaCase.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmaCase.Controllers;

public class HomeController(AppDbContext db) : Controller
{
    public const int StockMinimo = 3;

    public async Task<IActionResult> Index()
    {
        var hoy = DateTime.Today;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

        var meses = await ReporteService.ResumenPorMesAsync(db, inicioMes.AddMonths(-5), inicioMes.AddMonths(1));
        var actual = meses.Last();

        var ventasConSaldo = await db.Ventas.AsNoTracking()
            .Include(v => v.Pagos)
            .ToListAsync();
        var conDeuda = ventasConSaldo.Where(v => v.Saldo > 0).ToList();

        var productos = await db.Productos.AsNoTracking().Where(p => p.Activo).ToListAsync();

        var vm = new DashboardVM
        {
            VendidoMes = actual.TotalVendido,
            GananciaMes = actual.Ganancia,
            MargenMes = actual.Margen,
            CostoInventario = productos.Sum(p => p.Cantidad * p.PrecioCompra),
            RecaudadoMes = actual.TotalRecaudado,
            VentasMes = actual.NumeroVentas,
            TotalPorCobrar = conDeuda.Sum(v => v.Saldo),
            ClientesConDeuda = conDeuda.Select(v => v.ClienteId).Distinct().Count(),
            TotalClientes = await db.Clientes.CountAsync(),
            TotalProductos = productos.Count,
            UnidadesInventario = productos.Sum(p => p.Cantidad),
            ValorInventario = productos.Sum(p => p.Cantidad * p.Precio),
            BajoStock = productos.Where(p => p.Cantidad <= StockMinimo).OrderBy(p => p.Cantidad).Take(8).ToList(),
            UltimasVentas = await db.Ventas.AsNoTracking()
                .Include(v => v.Cliente).Include(v => v.Pagos)
                .OrderByDescending(v => v.Fecha).Take(6).ToListAsync(),
            UltimosMeses = meses
        };

        return View(vm);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
