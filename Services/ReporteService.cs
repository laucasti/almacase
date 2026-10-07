using AlmaCase.Data;
using AlmaCase.Models;
using Microsoft.EntityFrameworkCore;

namespace AlmaCase.Services;

public static class ReporteService
{
    /// <summary>
    /// Calcula el resumen de cada mes entre <paramref name="desde"/> (inclusive)
    /// y <paramref name="hasta"/> (exclusivo). Ambos deben ser el día 1 de un mes.
    /// </summary>
    public static async Task<List<ResumenMes>> ResumenPorMesAsync(AppDbContext db, DateTime desde, DateTime hasta)
    {
        var ventas = await db.Ventas.AsNoTracking()
            .Include(v => v.Detalles)
            .Include(v => v.Pagos)
            .AsSplitQuery()
            .Where(v => v.Fecha >= desde && v.Fecha < hasta)
            .ToListAsync();

        var pagos = await db.Pagos.AsNoTracking()
            .Where(p => p.Fecha >= desde && p.Fecha < hasta)
            .Select(p => new { p.Fecha, p.Monto })
            .ToListAsync();

        var resultado = new List<ResumenMes>();
        for (var m = desde; m < hasta; m = m.AddMonths(1))
        {
            var delMes = ventas.Where(v => v.Fecha.Year == m.Year && v.Fecha.Month == m.Month).ToList();
            resultado.Add(new ResumenMes
            {
                Anio = m.Year,
                Mes = m.Month,
                NumeroVentas = delMes.Count,
                UnidadesVendidas = delMes.Sum(v => v.Detalles.Sum(d => d.Cantidad)),
                TotalVendido = delMes.Sum(v => v.Total),
                PendientePorCobrar = delMes.Sum(v => Math.Max(v.Saldo, 0)),
                TotalRecaudado = pagos.Where(p => p.Fecha.Year == m.Year && p.Fecha.Month == m.Month).Sum(p => p.Monto)
            });
        }
        return resultado;
    }
}
