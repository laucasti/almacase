using MySqlConnector;

namespace AlmaCase.Helpers;

/// <summary>
/// Acepta la cadena de conexión en cualquiera de estos formatos:
///   server=HOST;port=3306;database=almacase_db;user=USUARIO;password=CLAVE;SslMode=Required
///   mysql://USUARIO:CLAVE@HOST:PUERTO/BASE?ssl-mode=REQUIRED   (formato "Service URI" de Aiven)
///   DATABASE_URL=mysql://...                                    (si se pegó con el nombre adelante)
/// </summary>
public static class CadenaConexion
{
    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new InvalidOperationException(
                "Falta la cadena de conexión. Configure ConnectionStrings__AlmaCaseDb (en Render) " +
                "o ConnectionStrings:AlmaCaseDb en appsettings.Local.json.");

        var texto = valor.Trim().Trim('"', '\'');

        // Si se pegó "DATABASE_URL=..." o "NOMBRE=mysql://...", quitar el nombre.
        var idx = texto.IndexOf("mysql://", StringComparison.OrdinalIgnoreCase);
        if (idx > 0) texto = texto[idx..];

        if (!texto.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase))
            return texto; // ya está en formato server=...;port=...

        var uri = new Uri(texto);
        var credenciales = uri.UserInfo.Split(':', 2);
        var b = new MySqlConnectionStringBuilder
        {
            Server = uri.Host,
            Port = (uint)(uri.Port > 0 ? uri.Port : 3306),
            UserID = Uri.UnescapeDataString(credenciales[0]),
            Password = credenciales.Length > 1 ? Uri.UnescapeDataString(credenciales[1]) : "",
            Database = uri.AbsolutePath.Trim('/') is { Length: > 0 } db ? Uri.UnescapeDataString(db) : "almacase_db",
            SslMode = MySqlSslMode.Required // los MySQL en la nube exigen conexión cifrada
        };

        foreach (var parte in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = parte.Split('=', 2);
            if (kv.Length == 2 && kv[0].Replace("-", "").Replace("_", "").Equals("sslmode", StringComparison.OrdinalIgnoreCase)
                && Enum.TryParse<MySqlSslMode>(kv[1].Replace("-", "").Replace("_", ""), true, out var modo))
                b.SslMode = modo;
        }
        return b.ConnectionString;
    }
}
