using ControleQualite.Domaine;

namespace ControleQualite.Donnees;

/// <summary>Une référence de pièce et son plan : la liste ordonnée des cotes à contrôler.</summary>
public class Reference
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Designation { get; set; }
    public List<CoteEntite> Cotes { get; set; } = [];

    public IReadOnlyList<Cote> CotesDuPlan() => [.. Cotes.OrderBy(c => c.Ordre).Select(c => c.VersDomaine())];
}

public class CoteEntite
{
    public int Id { get; set; }
    public int ReferenceId { get; set; }
    public int Ordre { get; set; }
    public required string Nom { get; set; }
    public double Nominal { get; set; }
    public double LimiteInferieure { get; set; }
    public double LimiteSuperieure { get; set; }

    public Cote VersDomaine() => new(Nom, Nominal, LimiteInferieure, LimiteSuperieure);
}

public class OrdreFabrication
{
    public int Id { get; set; }
    public required string Numero { get; set; }
    public int ReferenceId { get; set; }
    public Reference Reference { get; set; } = null!;
    public int Quantite { get; set; }
    public DateTime OuvertLe { get; set; }
    public bool Clos { get; set; }
}

public class PieceControlee
{
    public int Id { get; set; }
    public int OrdreFabricationId { get; set; }
    public OrdreFabrication OrdreFabrication { get; set; } = null!;
    public required string Numero { get; set; }
    public DateTime ControleeLe { get; set; }
    public required string Operateur { get; set; }
    public EtatPiece Etat { get; set; }
    public string? MotifQuarantaine { get; set; }
    public DateTime? QuarantaineLe { get; set; }
    public List<MesureEntite> Mesures { get; set; } = [];

    /// <summary>Une pièce non conforme sans motif bloque le poste jusqu'à son isolement.</summary>
    public bool AttendQuarantaine => Etat == EtatPiece.NonConforme && MotifQuarantaine is null;
}

public class MesureEntite
{
    public long Id { get; set; }
    public int PieceControleeId { get; set; }
    public int CoteId { get; set; }
    public CoteEntite Cote { get; set; } = null!;
    public double Valeur { get; set; }
}
