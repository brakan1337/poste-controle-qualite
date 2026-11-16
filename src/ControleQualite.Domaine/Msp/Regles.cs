namespace ControleQualite.Domaine.Msp;

public enum Regle
{
    /// <summary>Un point hors des limites de contrôle.</summary>
    HorsLimites,

    /// <summary>Sept points de suite du même côté de la moyenne : le réglage a bougé.</summary>
    SerieMemeCote,

    /// <summary>Six points de suite qui montent (ou descendent) : usure d'outil, échauffement.</summary>
    Tendance,
}

/// <summary>Un signal sur la carte : le procédé change, il faut regarder avant que les pièces sortent de la tolérance.</summary>
public sealed record Signal(int Rang, Regle Regle, string Message);

/// <summary>
/// Les règles de lecture d'une carte de contrôle. Elles détectent une dérive alors que
/// toutes les pièces sont encore conformes : c'est tout l'intérêt de la carte.
/// </summary>
public static class Regles
{
    public const int LongueurSerie = 7;
    public const int LongueurTendance = 6;

    public static IReadOnlyList<Signal> Analyser(CarteXbarR carte)
    {
        var signaux = new List<Signal>();
        var points = carte.Points;

        foreach (var p in points)
        {
            if (p.Moyenne > carte.LimiteSuperieureMoyenne || p.Moyenne < carte.LimiteInferieureMoyenne)
            {
                signaux.Add(new Signal(p.Rang, Regle.HorsLimites, $"Sous-groupe {p.Rang} : moyenne hors des limites de contrôle."));
            }
            else if (p.Etendue > carte.LimiteSuperieureEtendue)
            {
                signaux.Add(new Signal(p.Rang, Regle.HorsLimites, $"Sous-groupe {p.Rang} : dispersion anormale (étendue hors limite)."));
            }
        }

        // Une série n'est signalée qu'une fois, sur le point qui atteint la longueur.
        var serie = 0;
        var cotePrecedent = 0;
        foreach (var p in points)
        {
            var cote = Math.Sign(p.Moyenne - carte.MoyenneGenerale);
            serie = cote != 0 && cote == cotePrecedent ? serie + 1 : 1;
            cotePrecedent = cote;
            if (cote != 0 && serie == LongueurSerie)
            {
                var sens = cote > 0 ? "au-dessus" : "en dessous";
                signaux.Add(new Signal(p.Rang, Regle.SerieMemeCote, $"Sous-groupe {p.Rang} : {LongueurSerie} points de suite {sens} de la moyenne."));
            }
        }

        var tendance = 1;
        var sensPrecedent = 0;
        for (var i = 1; i < points.Count; i++)
        {
            var sens = Math.Sign(points[i].Moyenne - points[i - 1].Moyenne);
            tendance = sens != 0 && sens == sensPrecedent ? tendance + 1 : 2;
            sensPrecedent = sens;
            if (sens != 0 && tendance == LongueurTendance)
            {
                var mot = sens > 0 ? "montent" : "descendent";
                signaux.Add(new Signal(points[i].Rang, Regle.Tendance, $"Sous-groupe {points[i].Rang} : {LongueurTendance} points de suite qui {mot}."));
            }
        }

        return signaux.OrderBy(s => s.Rang).ToArray();
    }
}
