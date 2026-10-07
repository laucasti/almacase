# Publicar Alma Case en internet (gratis)

Se usan tres servicios con plan gratuito:

| Servicio | Para qué | Costo |
|---|---|---|
| **GitHub** | Guardar el código | Gratis (repositorio privado) |
| **Aiven** | Base de datos MySQL en la nube | Plan *Free* |
| **Render** | Ejecutar la página web | Plan *Free* |

> Los planes gratuitos pueden cambiar: revisa las condiciones al crear cada cuenta.
> En el plan gratis de Render la página se "duerme" si nadie la usa por unos 15 minutos;
> la siguiente visita tarda alrededor de un minuto en cargar y después funciona normal.

---

## Paso 1 — Subir el código a GitHub

1. Crea una cuenta en <https://github.com> (si no tienes).
2. Clic en **New repository** → nombre `almacase` → marca **Private** → **Create repository**.
   No marques "Add README" ni ".gitignore" (el proyecto ya los tiene).
3. En una terminal, dentro de la carpeta `AlmaCase`, ejecuta (cambia `TU_USUARIO`):

```bash
git remote add origin https://github.com/TU_USUARIO/almacase.git
git push -u origin main
```

La primera vez se abrirá una ventana para iniciar sesión en GitHub.

✅ Lo que **sí** se sube: el código, las vistas, el logo.
🚫 Lo que **no** se sube (lo bloquea `.gitignore`): `bin/`, `obj/` y `appsettings.Local.json` (tus contraseñas).

---

## Paso 2 — Crear la base de datos MySQL en Aiven

1. Crea una cuenta en <https://aiven.io> → **Create service** → **MySQL** → plan **Free**.
2. Cuando el servicio esté *Running*, en **Overview → Connection information** copia el **Service URI**
   (empieza por `mysql://avnadmin:...`). Esa es tu cadena de conexión, tal cual.

   También sirve el formato clásico, en una sola línea:

```
server=HOST;port=PUERTO;database=almacase_db;user=avnadmin;password=CONTRASEÑA;SslMode=Required
```

No necesitas crear tablas: el sistema crea la base `almacase_db` y todas las tablas al arrancar.

---

## Paso 3 — Publicar la página en Render

1. Crea una cuenta en <https://render.com> (puedes entrar con tu cuenta de GitHub).
2. **New → Web Service** → conecta tu repositorio `almacase`.
3. Configura:
   - **Language / Runtime:** `Docker` (Render detecta el `Dockerfile` solo)
   - **Instance type:** `Free`
4. En **Environment Variables** agrega estas tres:

| Key | Value |
|---|---|
| `ConnectionStrings__AlmaCaseDb` | la cadena del paso 2 |
| `Admin__Usuario` | el usuario con el que vas a entrar, ej. `alma` |
| `Admin__Password` | una contraseña segura (mínimo 8 caracteres) |

   *(Son **dos** guiones bajos `__` en cada nombre.)*

5. Clic en **Create Web Service**. Tarda unos minutos la primera vez.
6. Render te da una dirección como `https://almacase.onrender.com` → entra con el usuario y contraseña del punto 4.

El usuario administrador se crea **solo la primera vez**. Después puedes cambiar la contraseña
desde el menú (ícono 🔑). Cambiar `Admin__Password` en Render más adelante **no** cambia la contraseña.

---

## Actualizar la página después de hacer cambios

Cada vez que subes cambios a GitHub, Render vuelve a publicar automáticamente:

```bash
git add .
git commit -m "Describe tu cambio"
git push
```

---

## Usarlo en tu computador (opcional)

1. Copia `appsettings.Local.example.json` como `appsettings.Local.json` (ya existe uno en tu PC).
2. Pon ahí tu cadena de MySQL local y el usuario/contraseña de administrador.
3. Ejecuta `dotnet run`.

Si en `appsettings.Local.json` pones la cadena de **Aiven**, tu PC y la página de internet
usan la misma base de datos.

---

## Si algo falla

- En Render, abre la pestaña **Logs** del servicio: ahí aparece el error.
- `Unable to connect to any of the specified MySQL hosts` → revisa host, puerto y que la cadena termine en `SslMode=Required`.
- "Usuario o contraseña incorrectos" en el primer ingreso → revisa que las variables se llamen exactamente `Admin__Usuario` y `Admin__Password`.
