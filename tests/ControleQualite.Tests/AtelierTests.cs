using ControleQualite.Domaine;
using ControleQualite.Domaine.Msp;
using ControleQualite.Donnees;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ControleQualite.Tests;

/// <summary>
/// La base est SQLite en mémoire par défaut. Avec la variable TESTS_SQLSERVER (une chaîne de connexion),
/// les mêmes tests tournent sur SQL Server avec les vraies migrations : c'est ce que fait la CI.
/// </summary>
public sealed class AtelierTests : IDisposable
{
    private static readonly DateTime Maintenant = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);
    private readonly SqliteConnection? _connexion;
    private readonly BaseQualite _db;
    private readonly Atelier _atelier;

    public AtelierTests()
    {
        var sqlServer = Environment.GetEnvironmentVariable("TESTS_SQLSERVER");
        if (sqlServer is not null)
        {
            _db = new BaseQualite(FabriqueBase.Options("SqlServer", sqlServer));
            _db.Database.EnsureDeleted();
        }
        else
        {
            _connexion = new SqliteConnection("Data Source=:memory:");
            _connexion.Open();
            _db = new BaseQualite(new DbContextOptionsBuilder<BaseQualite>().UseSqlite(_connexion).Options);
        }
        FabriqueBase.Preparer(_db);
        Demonstration.Remplir(_db, Maintenant);
        _atelier = new Atelier(_db, new HorlogeFixe(Maintenant));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connexion?.Dispose();
    }

    private async Task<(OrdreFabrication Of, IReadOnlyList<Cote> Plan)> OfDeDemo()
    {
        var of = Assert.Single(await _atelier.OrdresOuverts());
        return (of, of.Reference.CotesDuPlan());
    }

    private static ControlePiece Controler(string numero, IReadOnlyList<Cote> plan, params double[] valeurs)
    {
        var piece = new ControlePiece(numero, plan);
        foreach (var v in valeurs)
        {
            piece.Mesurer(v);
        }
        return piece;
    }

    [Fact]
    public async Task Les_donnees_de_demo_sont_chargees()
    {
        var (of, plan) = await OfDeDemo();
        Assert.Equal(Demonstration.NumeroOf, of.Numero);
        Assert.Equal(3, plan.Count);
        var bilan = await _atelier.Bilan(of.Id);
        Assert.Equal(125, bilan.Controlees);
        Assert.Equal(500, bilan.Quantite);
    }

    [Fact]
    public async Task Enregistre_une_piece_conforme_avec_ses_mesures()
    {
        var (of, plan) = await OfDeDemo();
        var piece = await _atelier.Enregistrer(of.Id, Controler("AX120-9001", plan, 11.991, 120.01, 18.0), "Opérateur test");

        Assert.Equal(EtatPiece.Conforme, piece.Etat);
        Assert.Equal(3, await _db.Mesures.CountAsync(m => m.PieceControleeId == piece.Id));
        Assert.Equal(Maintenant, piece.ControleeLe);
    }

    [Fact]
    public async Task Une_piece_ne_se_controle_qu_une_fois_par_OF()
    {
        var (of, plan) = await OfDeDemo();
        var erreur = await Assert.ThrowsAsync<PieceDejaControleeException>(() =>
            _atelier.Enregistrer(of.Id, Controler("AX120-0001", plan, 11.991, 120.01, 18.0), "x"));
        Assert.Contains("AX120-0001", erreur.Message);
    }

    [Fact]
    public async Task Une_piece_en_cours_ne_s_enregistre_pas()
    {
        var (of, plan) = await OfDeDemo();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _atelier.Enregistrer(of.Id, Controler("AX120-9002", plan, 11.991), "x"));
    }

    [Fact]
    public async Task Une_piece_non_conforme_bloque_le_poste_jusqu_a_sa_quarantaine()
    {
        var (of, plan) = await OfDeDemo();
        Assert.Null(await _atelier.QuarantaineEnAttente(of.Id));

        var rebut = await _atelier.Enregistrer(of.Id, Controler("AX120-9003", plan, 12.006), "Opérateur test");
        Assert.Equal(EtatPiece.NonConforme, rebut.Etat);
        Assert.Equal(rebut.Id, (await _atelier.QuarantaineEnAttente(of.Id))?.Id);

        await Assert.ThrowsAsync<ArgumentException>(() => _atelier.MettreEnQuarantaine(rebut.Id, "ok"));
        await _atelier.MettreEnQuarantaine(rebut.Id, "Portée trop grande, bac rouge n° 2");

        Assert.Null(await _atelier.QuarantaineEnAttente(of.Id));
        var bilan = await _atelier.Bilan(of.Id);
        Assert.True(bilan.NonConformes >= 1);
    }

    [Fact]
    public async Task L_historique_d_une_cote_va_du_plus_ancien_au_plus_recent()
    {
        var (of, plan) = await OfDeDemo();
        await _atelier.Enregistrer(of.Id, Controler("AX120-9004", plan, 11.9999, 120.0, 18.0), "x");

        var coteId = of.Reference.Cotes.Single(c => c.Ordre == 1).Id;
        var historique = await _atelier.Historique(coteId, 30);
        Assert.Equal(30, historique.Count);
        Assert.Equal(11.9999, historique[^1]);
    }

    [Fact]
    public async Task La_carte_de_la_demo_voit_venir_l_usure_d_outil()
    {
        var (of, plan) = await OfDeDemo();
        var coteId = of.Reference.Cotes.Single(c => c.Ordre == 1).Id;
        var carte = new CarteXbarR(await _atelier.Historique(coteId));

        Assert.Equal(25, carte.Points.Count);
        Assert.NotEmpty(Regles.Analyser(carte));
        Assert.All(carte.MesuresRetenues, v => Assert.Equal(Verdict.Conforme, plan[0].Evaluer(v)));
    }

    private sealed class HorlogeFixe(DateTime instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(instant);
    }
}
