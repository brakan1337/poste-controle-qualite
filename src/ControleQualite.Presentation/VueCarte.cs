using System.Globalization;
using ControleQualite.Domaine;
using ControleQualite.Domaine.Msp;

namespace ControleQualite.Presentation;

/// <summary>Ce que l'écran affiche de la carte de contrôle d'une cote : le tracé, les signaux, la capabilité.</summary>
public sealed class VueCarte
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    private VueCarte(Cote cote, CarteXbarR? carte, IReadOnlyList<Signal> signaux, Capabilite? capabilite, int nombreMesures)
    {
        Cote = cote;
        Carte = carte;
        Signaux = signaux;
        Capabilite = capabilite;
        NombreMesures = nombreMesures;
    }

    public Cote Cote { get; }
    public CarteXbarR? Carte { get; }
    public IReadOnlyList<Signal> Signaux { get; }
    public Capabilite? Capabilite { get; }
    public int NombreMesures { get; }

    public static VueCarte Construire(Cote cote, IReadOnlyList<double> historique, int tailleSousGroupe = 5)
    {
        if (historique.Count < 2 * tailleSousGroupe)
        {
            return new VueCarte(cote, null, [], null, historique.Count);
        }
        var carte = new CarteXbarR(historique, tailleSousGroupe);
        return new VueCarte(cote, carte, Regles.Analyser(carte), Capabilite.Calculer(carte, cote), historique.Count);
    }

    public string Titre => $"Carte X̄-R : {Cote.Nom}";

    public string TexteCapabilite => Capabilite is null
        ? $"Pas encore assez de mesures ({NombreMesures})."
        : string.Create(Fr, $"Cp {Capabilite.Cp:0.00} · Cpk {Capabilite.Cpk:0.00} · Ppk {Capabilite.Ppk:0.00} sur {Capabilite.NombreMesures} mesures");

    public string NiveauTexte => Capabilite?.Niveau switch
    {
        NiveauCapabilite.Capable => "Procédé capable",
        NiveauCapabilite.Limite => "Procédé limite",
        NiveauCapabilite.NonCapable => "Procédé non capable",
        _ => "",
    };

    public string TexteSignal => Signaux.Count == 0
        ? "Aucun signal : le procédé est stable."
        : Signaux[^1].Message + " Vérifiez le réglage avant que les pièces sortent de la tolérance.";
}
