using AlmaCase.Data;
using AlmaCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmaCase.Controllers;

public class ProductosController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, string? categoria, bool inactivos = false)
    {
        var query = db.Productos.AsNoTracking().Where(p => p.Activo != inactivos);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Nombre.Contains(q) || (p.Descripcion != null && p.Descripcion.Contains(q)));
        if (!string.IsNullOrWhiteSpace(categoria))
            query = query.Where(p => p.Categoria == categoria);

        ViewBag.Q = q;
        ViewBag.Categoria = categoria;
        ViewBag.Inactivos = inactivos;
        ViewBag.Categorias = await CategoriasAsync();
        return View(await query.OrderBy(p => p.Nombre).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Categorias = await CategoriasAsync();
        return View("Form", new Producto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Producto producto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categorias = await CategoriasAsync();
            return View("Form", producto);
        }
        producto.FechaRegistro = DateTime.Now;
        producto.Activo = true;
        db.Productos.Add(producto);
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Producto \"{producto.Nombre}\" creado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var producto = await db.Productos.FindAsync(id);
        if (producto == null) return NotFound();
        ViewBag.Categorias = await CategoriasAsync();
        return View("Form", producto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Producto producto)
    {
        if (id != producto.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            ViewBag.Categorias = await CategoriasAsync();
            return View("Form", producto);
        }
        var actual = await db.Productos.FindAsync(id);
        if (actual == null) return NotFound();

        actual.Nombre = producto.Nombre;
        actual.Categoria = producto.Categoria;
        actual.Descripcion = producto.Descripcion;
        actual.Cantidad = producto.Cantidad;
        actual.Precio = producto.Precio;
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Producto \"{actual.Nombre}\" actualizado.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Suma unidades al inventario (cuando llega mercancía nueva).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarStock(int id, int cantidad)
    {
        var producto = await db.Productos.FindAsync(id);
        if (producto == null) return NotFound();
        if (cantidad <= 0)
        {
            TempData["Error"] = "La cantidad a agregar debe ser mayor que cero.";
            return RedirectToAction(nameof(Index));
        }
        producto.Cantidad += cantidad;
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Se agregaron {cantidad} unidades a \"{producto.Nombre}\". Stock actual: {producto.Cantidad}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var producto = await db.Productos.FindAsync(id);
        if (producto == null) return NotFound();

        bool tieneVentas = await db.DetallesVenta.AnyAsync(d => d.ProductoId == id);
        if (tieneVentas)
        {
            // No se borra para no perder el historial de ventas: se desactiva.
            producto.Activo = false;
            TempData["Ok"] = $"\"{producto.Nombre}\" tiene ventas registradas, así que se desactivó en lugar de eliminarse.";
        }
        else
        {
            db.Productos.Remove(producto);
            TempData["Ok"] = $"Producto \"{producto.Nombre}\" eliminado.";
        }
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id)
    {
        var producto = await db.Productos.FindAsync(id);
        if (producto == null) return NotFound();
        producto.Activo = true;
        await db.SaveChangesAsync();
        TempData["Ok"] = $"\"{producto.Nombre}\" está activo de nuevo.";
        return RedirectToAction(nameof(Index));
    }

    private Task<List<string>> CategoriasAsync() =>
        db.Productos.Where(p => p.Categoria != null && p.Categoria != "")
            .Select(p => p.Categoria!).Distinct().OrderBy(c => c).ToListAsync();
}
