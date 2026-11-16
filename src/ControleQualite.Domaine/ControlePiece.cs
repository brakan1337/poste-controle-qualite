namespace ControleQualite.Domaine;

public enum EtatPiece
{
    EnCours,
    Conforme,
    NonConforme,
}

/// <summary>
/// Le contrôle d'une pièce : les cotes se mesurent dans l'ordre du plan, une à une.
/// Dès qu'une cote sort de sa tolérance, la pièce est non conforme, même si d'autres restent à mesurer :
/// l'opérateur l'isole sans perdre de temps sur le reste.
/// </summary>
public sealed class ControlePiece
{
    private readonly List<double> _valeurs = [];

    public ControlePiece(string numero, IReadOnlyList<Cote> cotes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numero);
        if (cotes.Count == 0)
        {
            throw new ArgumentException("Une référence sans cote ne se contrôle pas.", nameof(cotes));
        }
        Numero = numero.Trim();
        Cotes = cotes;
    }

    public string Numero { get; }
    public IReadOnlyList<Cote> Cotes { get; }
    public IReadOnlyList<double> Valeurs => _valeurs;

    /// <summary>La cote qui attend sa mesure, ou null quand tout est mesuré.</summary>
    public Cote? CoteSuivante => _valeurs.Count < Cotes.Count ? Cotes[_valeurs.Count] : null;

    public IEnumerable<(Cote Cote, double Valeur, Verdict Verdict)> Resultats =>
        _valeurs.Select((v, i) => (Cotes[i], v, Cotes[i].Evaluer(v)));

    public EtatPiece Etat
    {
        get
        {
            if (Resultats.Any(r => r.Verdict != Verdict.Conforme))
            {
                return EtatPiece.NonConforme;
            }
            return _valeurs.Count == Cotes.Count ? EtatPiece.Conforme : EtatPiece.EnCours;
        }
    }

    /// <summary>Enregistre la mesure de la cote suivante et renvoie son verdict.</summary>
    public Verdict Mesurer(double valeur)
    {
        if (Etat != EtatPiece.EnCours)
        {
            throw new InvalidOperationException($"La pièce {Numero} est déjà {(Etat == EtatPiece.Conforme ? "conforme" : "non conforme")} : plus de mesure à prendre.");
        }
        if (double.IsNaN(valeur) || double.IsInfinity(valeur))
        {
            throw new ArgumentOutOfRangeException(nameof(valeur), "Mesure illisible.");
        }
        var cote = CoteSuivante!;
        _valeurs.Add(valeur);
        return cote.Evaluer(valeur);
    }
}
