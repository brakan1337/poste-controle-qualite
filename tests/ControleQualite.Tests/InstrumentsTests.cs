using ControleQualite.Instruments;

namespace ControleQualite.Tests;

public class InstrumentsTests
{
    [Theory]
    [InlineData("+012.345 mm", 12.345)]
    [InlineData("12.345", 12.345)]
    [InlineData("  -0,0042in ", -0.10668)]
    [InlineData("11982 µm", 11.982)]
    [InlineData("01A+00012.345", 12.345)]
    [InlineData("+ 120.0040", 120.004)]
    public void Lit_les_trames_courantes(string trame, double attendu)
    {
        Assert.True(Trame.EssayerLire(trame, out var valeur));
        Assert.Equal(attendu, valeur, 6);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ERR 05")]
    [InlineData("12.3.4")]
    [InlineData("12 kg")]
    public void Refuse_une_trame_illisible(string trame) =>
        Assert.False(Trame.EssayerLire(trame, out _));

    [Fact]
    public void L_instrument_simule_passe_par_le_meme_decodage()
    {
        using var instrument = new InstrumentSimule(new Random(1));
        var recues = new List<double>();
        var illisibles = new List<string>();
        instrument.MesureRecue += (_, v) => recues.Add(v);
        instrument.TrameIllisible += (_, t) => illisibles.Add(t);

        instrument.Envoyer(12, 0.002);
        instrument.Recevoir("ERR");

        var valeur = Assert.Single(recues);
        Assert.InRange(valeur, 11.99, 12.01);
        Assert.Equal(Math.Round(valeur, 4), valeur); // l'instrument envoie 4 décimales
        Assert.Equal(["ERR"], illisibles);
    }
}
