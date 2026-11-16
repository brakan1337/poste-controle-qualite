using ControleQualite.Domaine;

namespace ControleQualite.Donnees;

/// <summary>
/// Données fictives : un axe de pompe usiné, et un ordre de fabrication déjà bien entamé.
/// Le diamètre dérive lentement vers le haut sur les dernières pièces (usure d'outil) :
/// toutes restent conformes, mais la carte de contrôle le voit venir.
/// </summary>
public static class Demonstration
{
    public const string NumeroOf = "OF-2026-1187";

    public static void Remplir(BaseQualite db, DateTime maintenant, int graine = 7)
    {
        if (db.References.Any())
        {
            return;
        }
        var hasard = new Random(graine);

        var axe = new Reference
        {
            Code = "AX-120",
            Designation = "Axe de pompe AX-120",
            Cotes =
            [
                Entite(1, Cote.ParEcarts("Diamètre portée Ø12 h7", 12.000, -0.018, 0)),
                Entite(2, Cote.ParEcarts("Longueur totale 120 ±0,1", 120.000, -0.1, 0.1)),
                Entite(3, Cote.ParEcarts("Diamètre épaulement Ø18 ±0,05", 18.000, -0.05, 0.05)),
            ],
        };
        var of = new OrdreFabrication { Numero = NumeroOf, Reference = axe, Quantite = 500, OuvertLe = maintenant.AddDays(-2) };
        db.OrdresFabrication.Add(of);
        db.SaveChanges();

        var cotes = axe.Cotes.OrderBy(c => c.Ordre).ToList();
        const int pieces = 125;
        for (var i = 0; i < pieces; i++)
        {
            // Usure d'outil sur le dernier quart : +0,2 µm par pièce sur la portée, sans sortir de la tolérance.
            var derive = i > 95 ? (i - 95) * 0.0002 : 0;
            var valeurs = new[]
            {
                Normale(hasard, 11.989 + derive, 0.0015),
                Normale(hasard, 120.002, 0.018),
                Normale(hasard, 17.998, 0.009),
            };
            db.Pieces.Add(new PieceControlee
            {
                OrdreFabrication = of,
                Numero = $"AX120-{i + 1:0000}",
                Operateur = "Équipe de nuit",
                ControleeLe = maintenant.AddMinutes(-4 * (pieces - i)),
                Etat = valeurs.Select((v, k) => cotes[k].VersDomaine().Evaluer(v)).All(v => v == Verdict.Conforme)
                    ? EtatPiece.Conforme : EtatPiece.NonConforme,
                MotifQuarantaine = null,
                Mesures = [.. valeurs.Select((v, k) => new MesureEntite { CoteId = cotes[k].Id, Valeur = Math.Round(v, 4) })],
            });
        }
        db.SaveChanges();

        // Les éventuelles non-conformités de l'historique ont déjà été traitées par l'équipe de nuit.
        foreach (var p in db.Pieces.Where(p => p.Etat == EtatPiece.NonConforme))
        {
            p.MotifQuarantaine = "Isolée par l'équipe de nuit, en attente d'analyse.";
            p.QuarantaineLe = p.ControleeLe;
        }
        db.SaveChanges();
    }

    private static CoteEntite Entite(int ordre, Cote cote) => new()
    {
        Ordre = ordre,
        Nom = cote.Nom,
        Nominal = cote.Nominal,
        LimiteInferieure = cote.LimiteInferieure,
        LimiteSuperieure = cote.LimiteSuperieure,
    };

    private static double Normale(Random hasard, double moyenne, double ecartType)
    {
        var u1 = 1 - hasard.NextDouble();
        var u2 = hasard.NextDouble();
        return moyenne + (Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2) * ecartType);
    }
}
