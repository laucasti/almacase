# Alma Case — Sistema de información

Sistema para manejar el inventario, los clientes, las ventas y la cartera de **Alma Case** (@AlmaCase.col).

**Tecnología:** ASP.NET Core MVC (.NET 9) · Entity Framework Core · MySQL · Bootstrap 5 · Chart.js

## Módulos

| Módulo | Qué hace |
|---|---|
| **Inicio** | Vendido y recaudado del mes, total por cobrar, inventario, productos con poco stock, gráfico de los últimos 6 meses y últimas ventas. |
| **Productos** | Crear, editar, buscar y filtrar por categoría. Nombre, categoría, descripción, **cantidad** y **precio**. Botón para *agregar stock* cuando llega mercancía. Si un producto ya tiene ventas no se borra, se desactiva. |
| **Clientes** | Nombre, documento, WhatsApp, Instagram, correo, dirección y ciudad. La ficha del cliente muestra su historial de compras, lo que ha pagado y lo que debe. |
| **Ventas** | Registrar las compras que te hacen: cliente, varios productos, precio (se puede ajustar para descuentos) y abono inicial. Descuenta el inventario automáticamente. Desde el detalle se registran **abonos/pagos** (efectivo, Nequi, Daviplata…) y se puede imprimir el recibo con el logo. |
| **Por cobrar** | Quién te falta por pagar, cuánto debe cada cliente, hace cuántos días, y botón de **WhatsApp** para recordarle. |
| **Reporte mensual** | Totales **mes a mes** del año: número de ventas, unidades, total vendido, total recaudado y pendiente, con gráfico, total del año y productos más vendidos. |

**Estados de una venta:** `Pendiente` (no ha pagado nada) · `Abonada` (pagó una parte) · `Pagada`.

## Cómo ejecutarlo

### 1. Requisitos
- [.NET SDK 9](https://dotnet.microsoft.com/download) o superior
- MySQL 8 (MySQL Server, XAMPP/WAMP o Docker)

### 2. Configurar la conexión y el usuario
Las contraseñas van en `appsettings.Local.json`, que **no se sube a Git**. Si no existe, cópialo de
`appsettings.Local.example.json` y llénalo:

```json
{
  "ConnectionStrings": {
    "AlmaCaseDb": "server=localhost;port=3306;database=almacase_db;user=root;password=TU_PASSWORD"
  },
  "Admin": { "Usuario": "alma", "Password": "TU_CONTRASEÑA_PARA_ENTRAR" }
}
```

No necesitas crear la base de datos: **la primera vez que se ejecuta, el sistema crea `almacase_db`, todas sus tablas
y el usuario administrador** con el que inicias sesión.

### 3. Ejecutar
```bash
dotnet run
```
Abre la dirección que aparece en la consola (por ejemplo `http://localhost:5xxx`).
También puedes abrir `AlmaCase.csproj` en Visual Studio y presionar **F5**.

## Publicar en internet

Mira la guía paso a paso en [DESPLIEGUE.md](DESPLIEGUE.md): GitHub + Aiven (MySQL) + Render, todo con plan gratis.

## Estructura

```
AlmaCase/
├── Controllers/     Home, Productos, Clientes, Ventas, Reportes
├── Data/            AppDbContext (Entity Framework + MySQL)
├── Models/          Producto, Cliente, Venta, DetalleVenta, Pago, ViewModels
├── Services/        ReporteService (cálculos mes a mes)
├── Helpers/         Formato (pesos colombianos, meses en español)
├── Views/           Vistas Razor con Bootstrap
└── wwwroot/
    ├── css/site.css Colores del logo (azul #3D8BEA, azul oscuro #1C3F7A, celeste #A9D3FB)
    └── img/logo.png Logo de Alma Case
```

## Tablas en MySQL

- `Productos` — inventario
- `Clientes`
- `Ventas` — encabezado de cada compra (cliente, fecha, total)
- `DetallesVenta` — productos de cada venta (cantidad, precio, subtotal)
- `Pagos` — pagos y abonos de cada venta

> **Nota:** si más adelante agregas campos nuevos a los modelos, como la base se crea con `EnsureCreated()`, tendrás que usar migraciones de EF Core (`dotnet ef migrations add ...`) o agregar la columna manualmente en MySQL.
