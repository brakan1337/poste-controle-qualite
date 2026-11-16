using ControleQualite.Domaine;
using ControleQualite.Domaine.Msp;

namespace ControleQualite.Tests;

public class MspTests
{
    /// <summary>Sous-groupes de 5 autour de chaque moyenne donnée : m-4, m-2, m, m+2, m+4 (étendue 8).</summary>
    private static double[] SousGroupes(params double[] moyennes) =>
        [.. moyennes.SelectMany(m => new[] { m - 4, m - 2, m, m + 2, m + 4 })];

    [Fact]
    public void Limites_de_la_carte_avec_les_constantes_classiques()
    {
        // Moyenne 12, étendue 4 dans chaque sous-groupe : les limites se vérifient à la main.
        var mesures = Enumerable.Repeat(new double[] { 10, 11, 12, 13, 14 }, 6).SelectMany(x => x).ToArray();
        var carte = new CarteXbarR(mesures);

        Assert.Equal(6, carte.Points.Count);
        Assert.Equal(12, carte.MoyenneGenerale, 9);
        Assert.Equal(4, carte.EtendueMoyenne, 9);
        Assert.Equal(12 + (0.577 * 4), carte.LimiteSuperieureMoyenne, 9);
        Assert.Equal(12 - (0.577 * 4), carte.LimiteInferieureMoyenne, 9);
        Assert.Equal(2.114 * 4, carte.LimiteSuperieureEtendue, 9);
        Assert.Equal(4 / 2.326, carte.EcartTypeIntra, 9);
    }

    [Fact]
    public void Un_sous_groupe_incomplet_n_est_pas_trace()
    {
        var carte = new CarteXbarR([.. SousGroupes(10, 10, 10), 99, 99]);
        Assert.Equal(3, carte.Points.Count);
        Assert.Equal(15, carte.MesuresRetenues.Count);
    }

    [Fact]
    public void Trop_peu_de_mesures_pour_une_carte() =>
        Assert.Throws<ArgumentException>(() => new CarteXbarR([1, 2, 3, 4, 5, 6]));

    [Fact]
    public void Capabilite_d_un_procede_centre_puis_decentre()
    {
        var mesures = Enumerable.Repeat(new double[] { 10, 11, 12, 13, 14 }, 6).SelectMany(x => x).ToArray();
        var carte = new CarteXbarR(mesures);

        var centre = Capabilite.Calculer(carte, new Cote("c", 12, 0, 24));
        Assert.Equal(2.326, centre.Cp, 3); // 24 / (6 × 4 / 2,326)
        Assert.Equal(centre.Cp, centre.Cpk, 9);
        Assert.Equal(NiveauCapabilite.Capable, centre.Niveau);

        var decentre = Capabilite.Calculer(carte, new Cote("c", 12, 6, 24));
        Assert.Equal(6 / (3 * (4 / 2.326)), decentre.Cpk, 9); // le côté le plus proche compte
        Assert.Equal(NiveauCapabilite.Limite, decentre.Niveau);
        Assert.False(decentre.Fiable); // 30 mesures seulement
    }

    [Fact]
    public void Pp_plus_faible_que_Cp_quand_le_procede_derive()
    {
        // Chaque sous-groupe est serré, mais les moyennes glissent : la dispersion globale dépasse la dispersion interne.
        var carte = new CarteXbarR(SousGroupes(10, 12, 14, 16, 18, 20));
        var c = Capabilite.Calculer(carte, new Cote("c", 15, 0, 30));
        Assert.True(c.Pp < c.Cp, $"Pp {c.Pp} devrait être sous Cp {c.Cp}");
    }

    [Fact]
    public void Sept_points_du_meme_cote_de_la_moyenne()
    {
        var signaux = Regles.Analyser(new CarteXbarR(SousGroupes(10, 10, 10, 14, 14, 14, 14, 14, 14, 14)));
        var signal = Assert.Single(signaux);
        Assert.Equal(Regle.SerieMemeCote, signal.Regle);
        Assert.Equal(10, signal.Rang);
        Assert.Contains("au-dessus", signal.Message);
    }

    [Fact]
    public void Six_points_qui_montent()
    {
        var signaux = Regles.Analyser(new CarteXbarR(SousGroupes(12, 12, 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12, 12)));
        var signal = Assert.Single(signaux);
        Assert.Equal(Regle.Tendance, signal.Regle);
        Assert.Equal(7, signal.Rang);
    }

    [Fact]
    public void Un_point_hors_des_limites()
    {
        var signaux = Regles.Analyser(new CarteXbarR(SousGroupes(12, 12, 12, 12, 30, 12, 12, 12)));
        Assert.Contains(signaux, s => s.Regle == Regle.HorsLimites && s.Rang == 5);
    }

    [Fact]
    public void Un_procede_stable_ne_donne_aucun_signal() =>
        Assert.Empty(Regles.Analyser(new CarteXbarR(SousGroupes(12, 12.5, 11.8, 12.2, 11.9, 12.4, 11.7, 12.1))));
}
