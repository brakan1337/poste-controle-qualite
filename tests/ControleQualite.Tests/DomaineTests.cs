using ControleQualite.Domaine;

namespace ControleQualite.Tests;

public class CoteTests
{
    private static readonly Cote Portee = Cote.ParEcarts("Ø12 h7", 12.000, -0.018, 0);

    [Theory]
    [InlineData(12.000, Verdict.Conforme)]
    [InlineData(11.982, Verdict.Conforme)] // pile sur la limite basse
    [InlineData(11.9905, Verdict.Conforme)]
    [InlineData(11.9819, Verdict.TropPetit)]
    [InlineData(12.0001, Verdict.TropGrand)]
    public void Evalue_une_mesure_face_a_la_tolerance(double valeur, Verdict attendu) =>
        Assert.Equal(attendu, Portee.Evaluer(valeur));

    [Fact]
    public void La_virgule_flottante_ne_refuse_pas_une_piece_sur_la_limite()
    {
        // 12 - 0.018 vaut 11.982000000000001 en virgule flottante : la pièce à 11,982 doit passer.
        Assert.Equal(Verdict.Conforme, Portee.Evaluer(11.982));
        Assert.Equal(0.018, Portee.IntervalleDeTolerance, 6);
    }

    [Fact]
    public void Refuse_un_plan_incoherent()
    {
        Assert.Throws<ArgumentException>(() => new Cote("x", 10, 10.1, 10.0));
        Assert.Throws<ArgumentException>(() => new Cote("x", 11, 9.9, 10.1));
    }
}

public class ControlePieceTests
{
    private static readonly Cote[] Plan =
    [
        Cote.ParEcarts("Diamètre", 12, -0.018, 0),
        Cote.ParEcarts("Longueur", 120, -0.1, 0.1),
    ];

    [Fact]
    public void Les_cotes_se_mesurent_dans_l_ordre_du_plan()
    {
        var piece = new ControlePiece(" AX-0001 ", Plan);
        Assert.Equal("AX-0001", piece.Numero);
        Assert.Equal("Diamètre", piece.CoteSuivante!.Nom);

        piece.Mesurer(11.99);
        Assert.Equal("Longueur", piece.CoteSuivante!.Nom);
        Assert.Equal(EtatPiece.EnCours, piece.Etat);

        piece.Mesurer(120.05);
        Assert.Null(piece.CoteSuivante);
        Assert.Equal(EtatPiece.Conforme, piece.Etat);
    }

    [Fact]
    public void Une_cote_hors_tolerance_arrete_le_controle()
    {
        var piece = new ControlePiece("AX-0002", Plan);
        Assert.Equal(Verdict.TropGrand, piece.Mesurer(12.004));
        Assert.Equal(EtatPiece.NonConforme, piece.Etat);
        var erreur = Assert.Throws<InvalidOperationException>(() => piece.Mesurer(120));
        Assert.Contains("non conforme", erreur.Message);
    }

    [Fact]
    public void Une_mesure_illisible_est_refusee() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ControlePiece("x", Plan).Mesurer(double.NaN));
}
