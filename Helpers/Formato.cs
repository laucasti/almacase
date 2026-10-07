using System.Globalization;

namespace AlmaCase.Helpers;

public static class Formato
{
    public static readonly CultureInfo Co = new("es-CO");

    /// <summary>Formatea como pesos colombianos: $ 1.250.000</summary>
    public static string Cop(this decimal valor) => valor.ToString("C0", Co);

    public static string NombreMes(int mes) =>
        Co.TextInfo.ToTitleCase(Co.DateTimeFormat.GetMonthName(mes));

    public static string Fecha(this DateTime f) => f.ToString("dd/MM/yyyy", Co);

    public static string ClaseEstado(string estado) => estado switch
    {
        "Pagada" => "badge-pagada",
        "Abonada" => "badge-abonada",
        _ => "badge-pendiente"
    };
}
