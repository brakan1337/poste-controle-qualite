namespace ControleQualite.Domaine.Msp;

/// <summary>Un point de la carte : la moyenne et l'étendue d'un sous-groupe de pièces consécutives.</summary>
public sealed record PointCarte(int Rang, double Moyenne, double Etendue);

/// <summary>
/// Carte de contrôle moyenne / étendue (X̄-R), outil de base de la maîtrise statistique des procédés (MSP).
///
/// Les mesures sont regroupées par sous-groupes de taille fixe (souvent 5 pièces consécutives).
/// Les limites de contrôle se calculent à partir des données, pas de la tolérance du plan :
/// elles disent ce que le procédé fait d'habitude, pas ce que le client accepte.
/// </summary>
public sealed class CarteXbarR
{
    // Constantes classiques des cartes X̄-R, selon la taille n du sous-groupe (n = 2 à 10).
    private static readonly double[] A2 = [1.880, 1.023, 0.729, 0.577, 0.483, 0.419, 0.373, 0.337, 0.308];
    private static readonly double[] D3 = [0, 0, 0, 0, 0, 0.076, 0.136, 0.184, 0.223];
    private static readonly double[] D4 = [3.267, 2.574, 2.282, 2.114, 2.004, 1.924, 1.864, 1.816, 1.777];
    private static readonly double[] D2 = [1.128, 1.693, 2.059, 2.326, 2.534, 2.704, 2.847, 2.970, 3.078];

    public CarteXbarR(IReadOnlyList<double> mesures, int tailleSousGroupe = 5)
    {
        if (tailleSousGroupe is < 2 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(tailleSousGroupe), "Un sous-groupe compte de 2 à 10 pièces.");
        }
        var nbSousGroupes = mesures.Count / tailleSousGroupe;
        if (nbSousGroupes < 2)
        {
            throw new ArgumentException($"Il faut au moins {2 * tailleSousGroupe} mesures pour tracer la carte.", nameof(mesures));
        }

        TailleSousGroupe = tailleSousGroupe;
        // Un sous-groupe incomplet en fin de série attend ses dernières pièces : on ne le trace pas encore.
        Points = Enumerable.Range(0, nbSousGroupes)
            .Select(i => mesures.Skip(i * tailleSousGroupe).Take(tailleSousGroupe).ToArray())
            .Select((g, i) => new PointCarte(i + 1, g.Average(), g.Max() - g.Min()))
            .ToArray();

        var k = tailleSousGroupe - 2;
        MoyenneGenerale = Points.Average(p => p.Moyenne);
        EtendueMoyenne = Points.Average(p => p.Etendue);
        LimiteSuperieureMoyenne = MoyenneGenerale + (A2[k] * EtendueMoyenne);
        LimiteInferieureMoyenne = MoyenneGenerale - (A2[k] * EtendueMoyenne);
        LimiteSuperieureEtendue = D4[k] * EtendueMoyenne;
        LimiteInferieureEtendue = D3[k] * EtendueMoyenne;
        EcartTypeIntra = EtendueMoyenne / D2[k];
        MesuresRetenues = mesures.Take(nbSousGroupes * tailleSousGroupe).ToArray();
    }

    public int TailleSousGroupe { get; }
    public IReadOnlyList<PointCarte> Points { get; }
    public IReadOnlyList<double> MesuresRetenues { get; }

    /// <summary>X̿ : la moyenne des moyennes.</summary>
    public double MoyenneGenerale { get; }

    /// <summary>R̄ : l'étendue moyenne.</summary>
    public double EtendueMoyenne { get; }

    public double LimiteSuperieureMoyenne { get; }
    public double LimiteInferieureMoyenne { get; }
    public double LimiteSuperieureEtendue { get; }
    public double LimiteInferieureEtendue { get; }

    /// <summary>
    /// σ estimé à l'intérieur des sous-groupes (R̄ / d2) : la dispersion à court terme,
    /// celle qui sert au Cp et au Cpk.
    /// </summary>
    public double EcartTypeIntra { get; }
}
