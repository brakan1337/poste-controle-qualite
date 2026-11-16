using ControleQualite.Domaine;
using ControleQualite.Donnees;
using ControleQualite.Instruments;
using ControleQualite.Presentation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ControleQualite.Tests;

/// <summary>Le poste tel que l'opérateur le vit, sans fenêtre : scanner, recevoir les mesures, isoler.</summary>
public sealed class PosteTests : IDisposable
{
    private readonly SqliteConnection _connexion = new("Data Source=:memory:");
    private readonly BaseQualite _db;
    private readonly InstrumentSimule _instrument = new(new Random(3));
    private readonly PosteViewModel _poste;

    public PosteTests()
    {
        _connexion.Open();
        _db = new BaseQualite(new DbContextOptionsBuilder<BaseQualite>().UseSqlite(_connexion).Options);
        FabriqueBase.Preparer(_db);
        Demonstration.Remplir(_db, DateTime.UtcNow);
        _poste = new PosteViewModel(new Atelier(_db, TimeProvider.System), _instrument, "Opérateur test", "Contrôle final", ecran: null);
    }

    public void Dispose()
    {
        _poste.Dispose();
        _db.Dispose();
        _connexion.Dispose();
    }

    private async Task Scanner(string numero)
    {
        _poste.NumeroSaisi = numero;
        await _poste.CommencerCommand.ExecuteAsync(null);
    }

    [Fact]
    public async Task Au_demarrage_le_poste_attend_une_piece()
    {
        await _poste.InitialiserAsync();
        Assert.Equal(EtatPoste.Pret, _poste.Etat);
        Assert.Equal(3, _poste.Cotes.Count);
        Assert.Contains("125 / 500", _poste.Bilan);
        Assert.NotNull(_poste.Carte?.Carte);
        Assert.False(_poste.SaisirCommand.CanExecute(null));
    }

    [Fact]
    public async Task Une_piece_bonne_de_bout_en_bout()
    {
        await _poste.InitialiserAsync();
        await Scanner("AX120-0500");
        Assert.Equal(EtatPoste.Mesure, _poste.Etat);
        Assert.True(_poste.Cotes[0].Attendue);

        _instrument.Recevoir("+11.9921 mm");
        Assert.Equal(Verdict.Conforme, _poste.Cotes[0].Verdict);
        Assert.True(_poste.Cotes[1].Attendue);
        _instrument.Recevoir("+120.012 mm");
        _instrument.Recevoir("+17.995 mm");

        Assert.Equal(EtatPoste.Pret, _poste.Etat);
        Assert.Contains("conforme", _poste.Message);
        Assert.Contains("126 / 500", _poste.Bilan);
    }

    [Fact]
    public async Task Une_piece_non_conforme_bloque_le_poste_jusqu_a_son_isolement()
    {
        await _poste.InitialiserAsync();
        await Scanner("AX120-0501");
        _instrument.Recevoir("+12.0040 mm");

        Assert.Equal(EtatPoste.Quarantaine, _poste.Etat);
        Assert.True(_poste.MessageAlerte);
        Assert.Contains("trop grand", _poste.Message);
        Assert.False(_poste.CommencerCommand.CanExecute(null)); // impossible de passer à la suivante

        _poste.Motif = "ok";
        await _poste.IsolerCommand.ExecuteAsync(null);
        Assert.Equal(EtatPoste.Quarantaine, _poste.Etat); // motif trop court : toujours bloqué

        _poste.Motif = "Portée trop grande, bac rouge";
        await _poste.IsolerCommand.ExecuteAsync(null);
        Assert.Equal(EtatPoste.Pret, _poste.Etat);
    }

    [Fact]
    public async Task Le_blocage_survit_a_un_redemarrage_du_poste()
    {
        await _poste.InitialiserAsync();
        await Scanner("AX120-0502");
        _instrument.Recevoir("11.970");

        using var redemarre = new PosteViewModel(new Atelier(_db, TimeProvider.System), new InstrumentSimule(new Random(4)), "Autre opérateur", "Contrôle final", ecran: null);
        await redemarre.InitialiserAsync();
        Assert.Equal(EtatPoste.Quarantaine, redemarre.Etat);
        Assert.Equal("AX120-0502", redemarre.NumeroEnQuarantaine);
    }

    [Fact]
    public async Task Une_piece_deja_controlee_est_refusee_des_le_scan()
    {
        await _poste.InitialiserAsync();
        await Scanner("AX120-0001");
        Assert.Equal(EtatPoste.Pret, _poste.Etat);
        Assert.Contains("déjà été contrôlée", _poste.Message);
    }

    [Fact]
    public async Task Saisie_manuelle_avec_une_virgule_et_trame_illisible()
    {
        await _poste.InitialiserAsync();
        await Scanner("AX120-0503");

        _instrument.Recevoir("ERR 05");
        Assert.Contains("Trame illisible", _poste.Message);
        Assert.Null(_poste.Cotes[0].Valeur);

        _poste.SaisieManuelle = "11,9915";
        await _poste.SaisirCommand.ExecuteAsync(null);
        Assert.Equal(11.9915, _poste.Cotes[0].Valeur);
        Assert.Equal("11,9915", _poste.Cotes[0].ValeurTexte);
    }

    [Fact]
    public async Task La_carte_affiche_la_derive_de_la_demo()
    {
        await _poste.InitialiserAsync();
        var carte = _poste.Carte!;
        Assert.NotEmpty(carte.Signaux);
        Assert.StartsWith("Sous-groupe", carte.TexteSignal);
        Assert.Contains("Cpk", carte.TexteCapabilite);
    }
}
