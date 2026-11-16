using ControleQualite.Domaine;
using Microsoft.EntityFrameworkCore;

namespace ControleQualite.Donnees;

public sealed class PieceDejaControleeException(string numero, string of)
    : InvalidOperationException($"La pièce {numero} a déjà été contrôlée sur l'OF {of}.");

public sealed record BilanOf(int Controlees, int Conformes, int NonConformes, int Quantite)
{
    public double TauxNonConformite => Controlees == 0 ? 0 : (double)NonConformes / Controlees;
}

/// <summary>Ce que le poste fait avec la base : enregistrer les pièces, isoler les non conformes, relire l'historique.</summary>
public sealed class Atelier(BaseQualite db, TimeProvider horloge)
{
    public async Task<IReadOnlyList<OrdreFabrication>> OrdresOuverts() =>
        await db.OrdresFabrication.Where(o => !o.Clos)
            .Include(o => o.Reference).ThenInclude(r => r.Cotes)
            .OrderBy(o => o.Numero).ToListAsync();

    public async Task<PieceControlee> Enregistrer(int ofId, ControlePiece piece, string operateur)
    {
        if (piece.Etat == EtatPiece.EnCours)
        {
            throw new InvalidOperationException($"La pièce {piece.Numero} n'est pas entièrement contrôlée.");
        }
        var of = await db.OrdresFabrication.Include(o => o.Reference).ThenInclude(r => r.Cotes).SingleAsync(o => o.Id == ofId);
        if (await db.Pieces.AnyAsync(p => p.OrdreFabricationId == ofId && p.Numero == piece.Numero))
        {
            throw new PieceDejaControleeException(piece.Numero, of.Numero);
        }
        var cotes = of.Reference.Cotes.OrderBy(c => c.Ordre).ToList();
        var enregistree = new PieceControlee
        {
            OrdreFabricationId = ofId,
            Numero = piece.Numero,
            Operateur = operateur,
            ControleeLe = horloge.GetUtcNow().UtcDateTime,
            Etat = piece.Etat,
            Mesures = [.. piece.Valeurs.Select((v, i) => new MesureEntite { CoteId = cotes[i].Id, Valeur = v })],
        };
        db.Pieces.Add(enregistree);
        await db.SaveChangesAsync();
        return enregistree;
    }

    public Task<bool> DejaControlee(int ofId, string numero)
    {
        var n = numero.Trim();
        return db.Pieces.AnyAsync(p => p.OrdreFabricationId == ofId && p.Numero == n);
    }

    public async Task MettreEnQuarantaine(int pieceId, string motif)
    {
        if (string.IsNullOrWhiteSpace(motif) || motif.Trim().Length < 5)
        {
            throw new ArgumentException("Décrivez le défaut en quelques mots (5 caractères au moins).", nameof(motif));
        }
        var piece = await db.Pieces.SingleAsync(p => p.Id == pieceId);
        if (piece.Etat != EtatPiece.NonConforme)
        {
            throw new InvalidOperationException("Seule une pièce non conforme part en quarantaine.");
        }
        piece.MotifQuarantaine = motif.Trim();
        piece.QuarantaineLe = horloge.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// La pièce non conforme qui bloque encore le poste. Le blocage est en base :
    /// redémarrer le poste ne permet pas de passer outre.
    /// </summary>
    public Task<PieceControlee?> QuarantaineEnAttente(int ofId) =>
        db.Pieces.Where(p => p.OrdreFabricationId == ofId && p.Etat == EtatPiece.NonConforme && p.MotifQuarantaine == null)
            .OrderBy(p => p.ControleeLe).FirstOrDefaultAsync();

    /// <summary>Les dernières mesures d'une cote, de la plus ancienne à la plus récente : la matière de la carte de contrôle.</summary>
    public async Task<IReadOnlyList<double>> Historique(int coteId, int nombre = 125)
    {
        var dernieres = await db.Mesures.Where(m => m.CoteId == coteId)
            .OrderByDescending(m => m.Id).Take(nombre).Select(m => m.Valeur).ToListAsync();
        dernieres.Reverse();
        return dernieres;
    }

    public async Task<BilanOf> Bilan(int ofId)
    {
        var etats = await db.Pieces.Where(p => p.OrdreFabricationId == ofId).GroupBy(p => p.Etat)
            .Select(g => new { Etat = g.Key, Nombre = g.Count() }).ToListAsync();
        var quantite = await db.OrdresFabrication.Where(o => o.Id == ofId).Select(o => o.Quantite).SingleAsync();
        var conformes = etats.Where(e => e.Etat == EtatPiece.Conforme).Sum(e => e.Nombre);
        var nonConformes = etats.Where(e => e.Etat == EtatPiece.NonConforme).Sum(e => e.Nombre);
        return new BilanOf(conformes + nonConformes, conformes, nonConformes, quantite);
    }
}
