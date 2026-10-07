using AlmaCase.Data;
using AlmaCase.Helpers;
using AlmaCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmaCase.Controllers;

/// <summary>Compras que los clientes le hacen a Alma Case, con sus pagos y abonos.</summary>
public class VentasController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? estado, string? mes, string? q)
    {
        var query = db.Ventas.AsNoTracking()
            .Include(v => v.Cliente)
            .Include(v => v.Pagos)
            .Include(v => v.Detalles)
            .AsSplitQuery();

        if (!string.IsNullOrWhiteSpace(mes) && DateTime.TryParse(mes + "-01", out var inicio))
        {
            var fin = inicio.AddMonths(1);
            query = query.Where(v => v.Fecha >= inicio && v.Fecha < fin);
        }
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(v => v.Cliente!.Nombre.Contains(q));

        var ventas = await query.OrderByDescending(v => v.Fecha).ToListAsync();

        // El estado se calcula a partir de los pagos, por eso se filtra en memoria.
        if (!string.IsNullOrWhiteSpace(estado))
            ventas = ventas.Where(v => v.Estado == estado).ToList();

        ViewBag.Estado = estado;
        ViewBag.Mes = mes;
        ViewBag.Q = q;
        return View(ventas);
    }

    public async Task<IActionResult> Create(int? clienteId)
    {
        await CargarListasAsync();
        return View(new VentaCrearVM { ClienteId = clienteId, Fecha = DateTime.Now });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VentaCrearVM vm)
    {
        var items = vm.Items.Where(i => i.ProductoId > 0 && i.Cantidad > 0).ToList();
        if (items.Count == 0)
            ModelState.AddModelError("", "Agregue al menos un producto a la venta.");

        if (vm.ClienteId.HasValue && !await db.Clientes.AnyAsync(c => c.Id == vm.ClienteId))
            ModelState.AddModelError(nameof(vm.ClienteId), "El cliente no existe.");

        var ids = items.Select(i => i.ProductoId).Distinct().ToList();
        var productos = await db.Productos.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        foreach (var grupo in items.GroupBy(i => i.ProductoId))
        {
            if (!productos.TryGetValue(grupo.Key, out var prod))
            {
                ModelState.AddModelError("", "Uno de los productos seleccionados no existe.");
                continue;
            }
            var pedidas = grupo.Sum(i => i.Cantidad);
            if (pedidas > prod.Cantidad)
                ModelState.AddModelError("", $"No hay suficiente stock de \"{prod.Nombre}\": pidió {pedidas}, hay {prod.Cantidad}.");
        }
        if (items.Any(i => i.PrecioUnitario < 0))
            ModelState.AddModelError("", "Los precios no pueden ser negativos.");

        var venta = new Venta
        {
            ClienteId = vm.ClienteId ?? 0,
            Fecha = vm.Fecha,
            Observaciones = vm.Observaciones
        };
        foreach (var item in items.Where(i => productos.ContainsKey(i.ProductoId)))
        {
            var precio = item.PrecioUnitario > 0 ? item.PrecioUnitario : productos[item.ProductoId].Precio;
            venta.Detalles.Add(new DetalleVenta
            {
                ProductoId = item.ProductoId,
                Cantidad = item.Cantidad,
                PrecioUnitario = precio,
                Subtotal = precio * item.Cantidad
            });
        }
        venta.Total = venta.Detalles.Sum(d => d.Subtotal);

        if (vm.AbonoInicial > venta.Total)
            ModelState.AddModelError(nameof(vm.AbonoInicial), "El abono no puede ser mayor que el total de la venta.");

        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(vm);
        }

        foreach (var d in venta.Detalles)
            productos[d.ProductoId].Cantidad -= d.Cantidad;

        if (vm.AbonoInicial > 0)
            venta.Pagos.Add(new Pago { Fecha = vm.Fecha, Monto = vm.AbonoInicial, MetodoPago = vm.MetodoPago, Nota = "Pago inicial" });

        db.Ventas.Add(venta);
        await db.SaveChangesAsync(); // Un solo SaveChanges = una sola transacción.

        TempData["Ok"] = $"Venta #{venta.Id} registrada por {venta.Total.Cop()}.";
        return RedirectToAction(nameof(Details), new { id = venta.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var venta = await db.Ventas.AsNoTracking()
            .Include(v => v.Cliente)
            .Include(v => v.Detalles).ThenInclude(d => d.Producto)
            .Include(v => v.Pagos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == id);
        if (venta == null) return NotFound();
        venta.Pagos = venta.Pagos.OrderBy(p => p.Fecha).ToList();
        return View(venta);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarPago(PagoVM vm)
    {
        var venta = await db.Ventas.Include(v => v.Pagos).FirstOrDefaultAsync(v => v.Id == vm.VentaId);
        if (venta == null) return NotFound();

        if (vm.Monto <= 0)
            TempData["Error"] = "Ingrese un monto mayor que cero.";
        else if (vm.Monto > venta.Saldo)
            TempData["Error"] = $"El pago ({vm.Monto.Cop()}) es mayor que el saldo pendiente ({venta.Saldo.Cop()}).";
        else
        {
            db.Pagos.Add(new Pago
            {
                VentaId = venta.Id,
                Monto = vm.Monto,
                MetodoPago = Pago.Metodos.Contains(vm.MetodoPago) ? vm.MetodoPago : "Otro",
                Fecha = vm.Fecha == default ? DateTime.Now : vm.Fecha,
                Nota = vm.Nota
            });
            await db.SaveChangesAsync();
            var saldo = venta.Total - venta.Pagos.Sum(p => p.Monto);
            TempData["Ok"] = saldo <= 0
                ? $"Pago registrado. ¡La venta #{venta.Id} quedó pagada!"
                : $"Abono registrado. Saldo pendiente: {saldo.Cop()}.";
        }
        return RedirectToAction(nameof(Details), new { id = venta.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarPago(int id)
    {
        var pago = await db.Pagos.FindAsync(id);
        if (pago == null) return NotFound();
        db.Pagos.Remove(pago);
        await db.SaveChangesAsync();
        TempData["Ok"] = "Pago eliminado.";
        return RedirectToAction(nameof(Details), new { id = pago.VentaId });
    }

    /// <summary>Elimina la venta y devuelve las unidades al inventario.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var venta = await db.Ventas
            .Include(v => v.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(v => v.Id == id);
        if (venta == null) return NotFound();

        foreach (var d in venta.Detalles)
            if (d.Producto != null) d.Producto.Cantidad += d.Cantidad;

        db.Ventas.Remove(venta);
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Venta #{id} eliminada y productos devueltos al inventario.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Clientes que todavía deben dinero.</summary>
    public async Task<IActionResult> Pendientes()
    {
        var ventas = await db.Ventas.AsNoTracking()
            .Include(v => v.Cliente)
            .Include(v => v.Pagos)
            .Include(v => v.Detalles).ThenInclude(d => d.Producto)
            .AsSplitQuery()
            .ToListAsync();

        var deudores = ventas.Where(v => v.Saldo > 0)
            .GroupBy(v => v.ClienteId)
            .Select(g => new DeudorVM
            {
                Cliente = g.First().Cliente!,
                Ventas = g.OrderBy(v => v.Fecha).ToList()
            })
            .OrderByDescending(d => d.TotalDeuda)
            .ToList();

        return View(deudores);
    }

    private async Task CargarListasAsync()
    {
        ViewBag.Clientes = await db.Clientes.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync();
        ViewBag.Productos = await db.Productos.AsNoTracking().Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
    }
}
