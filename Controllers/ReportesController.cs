using AlmaCase.Data;
using AlmaCase.Models;
using AlmaCase.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmaCase.Controllers;

public class ReportesController(AppDbContext db) : Controller
{
    /// <summary>Totales mes a mes de un año.</summary>
    public async Task<IActionResult> Index(int? anio)
    {
        var año = anio ?? DateTime.Today.Year;
        var desde = new DateTime(año, 1, 1);
        var hasta = desde.AddYears(1);

        var anios = await db.Ventas.Select(v => v.Fecha.Year).Distinct().ToListAsync();
        anios.Add(DateTime.Today.Year);
        anios.Add(año);

        var delAnio = db.DetallesVenta.AsNoTracking()
            .Where(d => d.Venta!.Fecha >= desde && d.Venta.Fecha < hasta);

        var top = await delAnio
            .GroupBy(d => d.Producto!.Nombre)
            .Select(g => new ProductoVendidoVM
            {
                Producto = g.Key,
                Unidades = g.Sum(d => d.Cantidad),
                Total = g.Sum(d => d.Subtotal),
                Costo = g.Sum(d => d.CostoUnitario * d.Cantidad)
            })
            .OrderByDescending(x => x.Unidades)
            .Take(10)
            .ToListAsync();

        var vm = new ReporteAnualVM
        {
            Anio = año,
            AniosDisponibles = anios.Distinct().OrderByDescending(a => a).ToList(),
            Meses = await ReporteService.ResumenPorMesAsync(db, desde, hasta),
            TopProductos = top,
            HayCostosIncompletos = await delAnio.AnyAsync(d => d.CostoUnitario <= 0),
            GastosPorCategoria = await db.Gastos.AsNoTracking()
                .Where(g => g.Fecha >= desde && g.Fecha < hasta)
                .GroupBy(g => g.Categoria)
                .Select(g => new CategoriaTotalVM { Categoria = g.Key, Total = g.Sum(x => x.Monto), Cantidad = g.Count() })
                .OrderByDescending(x => x.Total)
                .ToListAsync()
        };
        return View(vm);
    }
}
