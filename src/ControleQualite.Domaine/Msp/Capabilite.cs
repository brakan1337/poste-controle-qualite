namespace ControleQualite.Domaine.Msp;

public enum NiveauCapabilite
{
    /// <summary>Cpk ≥ 1,33 : le procédé tient la tolérance avec de la marge.</summary>
    Capable,

    /// <summary>1 ≤ Cpk &lt; 1,33 : il tient la tolérance, sans marge.</summary>
    Limite,

    /// <summary>Cpk &lt; 1 : une partie des pièces sortira de la tolérance.</summary>
    NonCapable,
}

/// <summary>
/// Capabilité d'un procédé face à une tolérance.
///
/// Cp et Cpk utilisent la dispersion à court terme (à l'intérieur des sous-groupes) :
/// ce que la machine sait faire. Pp et Ppk utilisent la dispersion globale, réglages
/// et dérives compris : ce que le client reçoit vraiment. Un grand écart entre les deux
/// signale un procédé qui dérive.
/// </summary>
public sealed record Capabilite(double Cp, double Cpk, double Pp, double Ppk, double Moyenne, double EcartTypeIntra, double EcartTypeGlobal, int NombreMesures)
{
    public const double SeuilCapable = 1.33;

    public NiveauCapabilite Niveau => Cpk switch
    {
        >= SeuilCapable => NiveauCapabilite.Capable,
        >= 1 => NiveauCapabilite.Limite,
        _ => NiveauCapabilite.NonCapable,
    };

    /// <summary>En dessous de 50 mesures, les indices bougent beaucoup d'un jour à l'autre : à lire avec prudence.</summary>
    public bool Fiable => NombreMesures >= 50;

    public static Capabilite Calculer(CarteXbarR carte, Cote cote)
    {
        var mesures = carte.MesuresRetenues;
        var moyenne = mesures.Average();
        var ecartTypeGlobal = Math.Sqrt(mesures.Sum(x => (x - moyenne) * (x - moyenne)) / (mesures.Count - 1));
        var sigma = carte.EcartTypeIntra;

        double Indice(double s) => cote.IntervalleDeTolerance / (6 * s);
        double IndiceCentre(double s) => Math.Min(cote.LimiteSuperieure - moyenne, moyenne - cote.LimiteInferieure) / (3 * s);

        return new Capabilite(
            Indice(sigma), IndiceCentre(sigma),
            Indice(ecartTypeGlobal), IndiceCentre(ecartTypeGlobal),
            moyenne, sigma, ecartTypeGlobal, mesures.Count);
    }
}
