using AlmaCase.Data;
using AlmaCase.Helpers;
using AlmaCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmaCase.Controllers;

/// <summary>Gastos del negocio (CapCut Pro, gasolina, publicidad, empaques...).</summary>
public class GastosController(AppDbContext db) : Controller
{
    /// <param name="mes">Formato yyyy-MM. Si no se envía, se muestra el mes actual. "todos" muestra todo.</param>
    public async Task<IActionResult> Index(string? mes, string? categoria, string? q)
    {
        mes ??= DateTime.Today.ToString("yyyy-MM");
        var query = db.Gastos.AsNoTracking();

        if (mes != "todos" && DateTime.TryParse(mes + "-01", out var inicio))
        {
            var fin = inicio.AddMonths(1);
            query = query.Where(g => g.Fecha >= inicio && g.Fecha < fin);
            ViewBag.MesNombre = $"{Formato.NombreMes(inicio.Month)} {inicio.Year}";
        }
        if (!string.IsNullOrWhiteSpace(categoria))
            query = query.Where(g => g.Categoria == categoria);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(g => g.Concepto.Contains(q) || (g.Nota != null && g.Nota.Contains(q)));

        ViewBag.Mes = mes;
        ViewBag.Categoria = categoria;
        ViewBag.Q = q;
        ViewBag.Categorias = await CategoriasAsync();
        return View(await query.OrderByDescending(g => g.Fecha).ThenByDescending(g => g.Id).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        await CargarListasAsync();
        return View("Form", new Gasto { Fecha = DateTime.Today });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Gasto gasto)
    {
        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View("Form", gasto);
        }
        gasto.Concepto = gasto.Concepto.Trim();
        gasto.Categoria = gasto.Categoria.Trim();
        db.Gastos.Add(gasto);
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Gasto \"{gasto.Concepto}\" por {gasto.Monto.Cop()} registrado.";
        return RedirectToAction(nameof(Index), new { mes = gasto.Fecha.ToString("yyyy-MM") });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var gasto = await db.Gastos.FindAsync(id);
        if (gasto == null) return NotFound();
        await CargarListasAsync();
        return View("Form", gasto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Gasto gasto)
    {
        if (id != gasto.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View("Form", gasto);
        }
        var actual = await db.Gastos.FindAsync(id);
        if (actual == null) return NotFound();

        actual.Fecha = gasto.Fecha;
        actual.Concepto = gasto.Concepto.Trim();
        actual.Categoria = gasto.Categoria.Trim();
        actual.Monto = gasto.Monto;
        actual.MetodoPago = gasto.MetodoPago;
        actual.Nota = gasto.Nota;
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Gasto \"{actual.Concepto}\" actualizado.";
        return RedirectToAction(nameof(Index), new { mes = actual.Fecha.ToString("yyyy-MM") });
    }

    /// <summary>Copia un gasto con la fecha de hoy (útil para suscripciones que se pagan cada mes).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Repetir(int id)
    {
        var original = await db.Gastos.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
        if (original == null) return NotFound();

        var copia = new Gasto
        {
            Fecha = DateTime.Today,
            Concepto = original.Concepto,
            Categoria = original.Categoria,
            Monto = original.Monto,
            MetodoPago = original.MetodoPago,
            Nota = original.Nota
        };
        db.Gastos.Add(copia);
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Se registró \"{copia.Concepto}\" por {copia.Monto.Cop()} con fecha de hoy.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var gasto = await db.Gastos.FindAsync(id);
        if (gasto == null) return NotFound();
        db.Gastos.Remove(gasto);
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Gasto \"{gasto.Concepto}\" eliminado.";
        return RedirectToAction(nameof(Index), new { mes = gasto.Fecha.ToString("yyyy-MM") });
    }

    /// <summary>Categorías sugeridas más las que ya se han usado.</summary>
    private async Task<List<string>> CategoriasAsync()
    {
        var usadas = await db.Gastos.Select(g => g.Categoria).Distinct().ToListAsync();
        return Gasto.CategoriasSugeridas.Union(usadas.OrderBy(c => c)).ToList();
    }

    private async Task CargarListasAsync()
    {
        ViewBag.Categorias = await CategoriasAsync();
        // Conceptos usados antes, para autocompletar (CapCut Pro, Gasolina...)
        ViewBag.Conceptos = await db.Gastos.Select(g => g.Concepto).Distinct().OrderBy(c => c).Take(100).ToListAsync();
    }
}
