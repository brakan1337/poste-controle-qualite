using System.IO;
using System.Text.Json;

namespace ControleQualite.Poste;

/// <summary>
/// La configuration du poste, lue dans appsettings.json à côté de l'exécutable.
/// Chez le client : Base = SqlServer, la chaîne de connexion de son serveur, et le port COM de l'instrument.
/// </summary>
public sealed record Configuration(string Poste, string Base, string Connexion, string Instrument, bool Demonstration)
{
    private static readonly JsonSerializerOptions Lecture = new() { PropertyNameCaseInsensitive = true };

    public static Configuration Charger()
    {
        var chemin = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var lue = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(chemin), Lecture)
            ?? throw new InvalidOperationException("appsettings.json est vide.");
        return lue with { Connexion = Environment.ExpandEnvironmentVariables(lue.Connexion) };
    }
}
