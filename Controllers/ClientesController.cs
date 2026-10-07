using AlmaCase.Data;
using AlmaCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmaCase.Controllers;

public class ClientesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        var query = db.Clientes.AsNoTracking()
            .Include(c => c.Ventas).ThenInclude(v => v.Pagos)
            .AsSplitQuery();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(c => c.Nombre.Contains(q)
                || (c.Telefono != null && c.Telefono.Contains(q))
                || (c.Documento != null && c.Documento.Contains(q))
                || (c.Instagram != null && c.Instagram.Contains(q)));

        ViewBag.Q = q;
        return View(await query.OrderBy(c => c.Nombre).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var cliente = await db.Clientes.AsNoTracking()
            .Include(c => c.Ventas).ThenInclude(v => v.Pagos)
            .Include(c => c.Ventas).ThenInclude(v => v.Detalles).ThenInclude(d => d.Producto)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id);
        if (cliente == null) return NotFound();
        cliente.Ventas = cliente.Ventas.OrderByDescending(v => v.Fecha).ToList();
        return View(cliente);
    }

    public IActionResult Create(string? volverA)
    {
        ViewBag.VolverA = volverA;
        return View("Form", new Cliente());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Cliente cliente, string? volverA)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.VolverA = volverA;
            return View("Form", cliente);
        }
        cliente.FechaRegistro = DateTime.Now;
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Cliente \"{cliente.Nombre}\" registrado.";

        // Si venía desde "Nueva venta", regresa allá con el cliente seleccionado.
        if (volverA == "venta")
            return RedirectToAction("Create", "Ventas", new { clienteId = cliente.Id });
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var cliente = await db.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();
        return View("Form", cliente);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Cliente cliente)
    {
        if (id != cliente.Id) return BadRequest();
        if (!ModelState.IsValid) return View("Form", cliente);

        var actual = await db.Clientes.FindAsync(id);
        if (actual == null) return NotFound();
        actual.Nombre = cliente.Nombre;
        actual.Documento = cliente.Documento;
        actual.Telefono = cliente.Telefono;
        actual.Email = cliente.Email;
        actual.Instagram = cliente.Instagram;
        actual.Direccion = cliente.Direccion;
        actual.Ciudad = cliente.Ciudad;
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Cliente \"{actual.Nombre}\" actualizado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var cliente = await db.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();

        if (await db.Ventas.AnyAsync(v => v.ClienteId == id))
        {
            TempData["Error"] = $"No se puede eliminar a \"{cliente.Nombre}\" porque tiene compras registradas.";
            return RedirectToAction(nameof(Details), new { id });
        }
        db.Clientes.Remove(cliente);
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Cliente \"{cliente.Nombre}\" eliminado.";
        return RedirectToAction(nameof(Index));
    }
}
