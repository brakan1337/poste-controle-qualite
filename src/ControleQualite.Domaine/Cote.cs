namespace ControleQualite.Domaine;

/// <summary>Résultat d'une mesure face à sa tolérance.</summary>
public enum Verdict
{
    Conforme,
    TropPetit,
    TropGrand,
}

/// <summary>
/// Une cote du plan : une dimension à contrôler, avec sa valeur nominale et ses limites.
/// Toutes les valeurs sont en millimètres.
/// </summary>
public sealed record Cote
{
    public Cote(string nom, double nominal, double limiteInferieure, double limiteSuperieure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nom);
        if (limiteInferieure >= limiteSuperieure)
        {
            throw new ArgumentException($"La limite inférieure ({limiteInferieure}) doit être sous la limite supérieure ({limiteSuperieure}).");
        }
        if (nominal < limiteInferieure || nominal > limiteSuperieure)
        {
            throw new ArgumentException($"Le nominal {nominal} sort de sa propre tolérance.");
        }
        Nom = nom;
        Nominal = nominal;
        LimiteInferieure = limiteInferieure;
        LimiteSuperieure = limiteSuperieure;
    }

    /// <summary>Construit une cote à partir d'écarts, comme sur un plan : Ø12 avec -0,018 / 0.</summary>
    public static Cote ParEcarts(string nom, double nominal, double ecartInferieur, double ecartSuperieur) =>
        new(nom, nominal, nominal + ecartInferieur, nominal + ecartSuperieur);

    public string Nom { get; }
    public double Nominal { get; }
    public double LimiteInferieure { get; }
    public double LimiteSuperieure { get; }

    public double IntervalleDeTolerance => LimiteSuperieure - LimiteInferieure;

    /// <summary>
    /// Une valeur sur la limite est conforme. On arrondit au micromètre avant de comparer :
    /// 11,982 lu par l'instrument ne doit pas être refusé à cause d'un 11,98199999 en virgule flottante.
    /// </summary>
    public Verdict Evaluer(double valeur)
    {
        var arrondie = Math.Round(valeur, 4, MidpointRounding.AwayFromZero);
        if (arrondie < Math.Round(LimiteInferieure, 4))
        {
            return Verdict.TropPetit;
        }
        return arrondie > Math.Round(LimiteSuperieure, 4) ? Verdict.TropGrand : Verdict.Conforme;
    }
}
