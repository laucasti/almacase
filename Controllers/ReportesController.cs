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

        var top = await db.DetallesVenta.AsNoTracking()
            .Where(d => d.Venta!.Fecha >= desde && d.Venta.Fecha < hasta)
            .GroupBy(d => d.Producto!.Nombre)
            .Select(g => new { Producto = g.Key, Unidades = g.Sum(d => d.Cantidad), Total = g.Sum(d => d.Subtotal) })
            .OrderByDescending(x => x.Unidades)
            .Take(10)
            .ToListAsync();

        var vm = new ReporteAnualVM
        {
            Anio = año,
            AniosDisponibles = anios.Distinct().OrderByDescending(a => a).ToList(),
            Meses = await ReporteService.ResumenPorMesAsync(db, desde, hasta),
            TopProductos = top.Select(t => (t.Producto, t.Unidades, t.Total)).ToList()
        };
        return View(vm);
    }
}
