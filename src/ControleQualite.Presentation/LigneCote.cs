using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using ControleQualite.Domaine;

namespace ControleQualite.Presentation;

/// <summary>Une ligne du tableau des cotes de la pièce en cours.</summary>
public sealed class LigneCote(Cote cote, int coteId) : ObservableObject
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    private double? _valeur;
    private Verdict? _verdict;
    private bool _attendue;

    public Cote Cote { get; } = cote;
    public int CoteId { get; } = coteId;
    public string Nom => Cote.Nom;
    public string Tolerance => string.Create(Fr, $"{Cote.LimiteInferieure:0.000} à {Cote.LimiteSuperieure:0.000}");

    public double? Valeur
    {
        get => _valeur;
        set
        {
            if (SetProperty(ref _valeur, value))
            {
                OnPropertyChanged(nameof(ValeurTexte));
            }
        }
    }

    public string ValeurTexte => Valeur is { } v ? v.ToString("0.0000", Fr) : "—";

    public Verdict? Verdict
    {
        get => _verdict;
        set
        {
            if (SetProperty(ref _verdict, value))
            {
                OnPropertyChanged(nameof(VerdictTexte));
            }
        }
    }

    public string VerdictTexte => Verdict switch
    {
        Domaine.Verdict.Conforme => "Conforme",
        Domaine.Verdict.TropPetit => "Trop petit",
        Domaine.Verdict.TropGrand => "Trop grand",
        _ => Attendue ? "À mesurer" : "",
    };

    /// <summary>La cote que l'instrument doit mesurer maintenant.</summary>
    public bool Attendue
    {
        get => _attendue;
        set
        {
            if (SetProperty(ref _attendue, value))
            {
                OnPropertyChanged(nameof(VerdictTexte));
            }
        }
    }

    public void Effacer()
    {
        Valeur = null;
        Verdict = null;
        Attendue = false;
    }
}
