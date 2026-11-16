using System.Globalization;
using System.Text.RegularExpressions;

namespace ControleQualite.Instruments;

/// <summary>
/// Lit la ligne ASCII qu'envoie un instrument de mesure quand l'opérateur appuie sur « envoi » :
/// un signe, une valeur, éventuellement une unité. Par exemple « +012.345 mm », « -0,0042in », « 11982 µm ».
///
/// Beaucoup de pieds à coulisse, micromètres et colonnes de mesure parlent ce format, avec des variantes.
/// Un protocole propre à une marque s'ajoute dans une classe à part, sans toucher au reste du poste.
/// </summary>
public static partial class Trame
{
    [GeneratedRegex(@"^\s*(?:\d{1,2}[A-Z])?\s*(?<valeur>[+-]?\s*\d+(?:[.,]\d+)?)\s*(?<unite>mm|µm|um|in|"")?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Motif();

    /// <summary>Convertit une trame en millimètres. Renvoie false si la trame est illisible.</summary>
    public static bool EssayerLire(string? trame, out double millimetres)
    {
        millimetres = 0;
        if (string.IsNullOrWhiteSpace(trame))
        {
            return false;
        }
        var correspondance = Motif().Match(trame);
        if (!correspondance.Success)
        {
            return false;
        }
        var texte = correspondance.Groups["valeur"].Value.Replace(" ", "", StringComparison.Ordinal).Replace(',', '.');
        if (!double.TryParse(texte, NumberStyles.Float, CultureInfo.InvariantCulture, out var valeur))
        {
            return false;
        }
        millimetres = correspondance.Groups["unite"].Value.ToLowerInvariant() switch
        {
            "in" or "\"" => valeur * 25.4,
            "µm" or "um" => valeur / 1000,
            _ => valeur,
        };
        return true;
    }
}
