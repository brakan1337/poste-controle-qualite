using System.IO.Ports;

namespace ControleQualite.Instruments;

/// <summary>Un instrument qui envoie ses mesures au poste.</summary>
public interface IInstrument : IDisposable
{
    string Description { get; }

    /// <summary>Levé à chaque mesure reçue, en millimètres.</summary>
    event EventHandler<double>? MesureRecue;

    /// <summary>Levé quand une trame arrive mais ne se lit pas (câble, réglage de l'instrument).</summary>
    event EventHandler<string>? TrameIllisible;

    void Demarrer();
}

/// <summary>Un instrument branché sur un port série (RS-232 ou adaptateur USB).</summary>
public sealed class InstrumentSerie : IInstrument
{
    private readonly SerialPort _port;

    public InstrumentSerie(string port, int vitesse = 9600)
    {
        _port = new SerialPort(port, vitesse, Parity.None, 8, StopBits.One)
        {
            NewLine = "\r",
            ReadTimeout = SerialPort.InfiniteTimeout,
        };
        _port.DataReceived += Lire;
        Description = $"{port}, {vitesse} bauds";
    }

    public string Description { get; }
    public event EventHandler<double>? MesureRecue;
    public event EventHandler<string>? TrameIllisible;

    public void Demarrer() => _port.Open();

    private void Lire(object? sender, SerialDataReceivedEventArgs e)
    {
        // Une trame peut arriver en plusieurs morceaux : on lit ligne par ligne ce qui est complet.
        while (_port.BytesToRead > 0)
        {
            var ligne = _port.ReadLine().Trim('\n', '\r', ' ');
            if (ligne.Length == 0)
            {
                continue;
            }
            if (Trame.EssayerLire(ligne, out var valeur))
            {
                MesureRecue?.Invoke(this, valeur);
            }
            else
            {
                TrameIllisible?.Invoke(this, ligne);
            }
        }
    }

    public void Dispose()
    {
        _port.DataReceived -= Lire;
        _port.Dispose();
    }
}

/// <summary>
/// Un instrument simulé, pour la démonstration et les essais sans matériel.
/// Il passe par le même décodage de trame qu'un vrai instrument.
/// </summary>
public sealed class InstrumentSimule(Random hasard) : IInstrument
{
    public string Description => "Instrument simulé";
    public event EventHandler<double>? MesureRecue;
    public event EventHandler<string>? TrameIllisible;

    public void Demarrer()
    {
    }

    /// <summary>Simule l'appui sur « envoi » : une mesure autour du nominal, avec la dispersion donnée.</summary>
    public void Envoyer(double nominal, double ecartType, double decalage = 0)
    {
        // Loi normale par la méthode de Box-Muller.
        var u1 = 1 - hasard.NextDouble();
        var u2 = hasard.NextDouble();
        var normale = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
        var valeur = nominal + decalage + (normale * ecartType);
        Recevoir(valeur.ToString("+0.0000;-0.0000", System.Globalization.CultureInfo.InvariantCulture) + " mm");
    }

    public void Recevoir(string trame)
    {
        if (Trame.EssayerLire(trame, out var valeur))
        {
            MesureRecue?.Invoke(this, valeur);
        }
        else
        {
            TrameIllisible?.Invoke(this, trame);
        }
    }

    public void Dispose()
    {
    }
}
