using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ControleQualite.Donnees;
using ControleQualite.Instruments;
using ControleQualite.Presentation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ControleQualite.Poste;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "Une Application WPF libère ses ressources dans OnExit, à la fermeture.")]
public partial class App : Application
{
    private BaseQualite? _db;
    private SqliteConnection? _memoire;
    private IInstrument? _instrument;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var capture = e.Args.SkipWhile(a => a != "--capture").Skip(1).FirstOrDefault();
            if (capture is not null)
            {
                await CapturerAsync(capture);
                Shutdown();
                return;
            }
            await DemarrerAsync(Configuration.Charger());
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException or UnauthorizedAccessException or DbUpdateException or SqliteException)
        {
            MessageBox.Show(ex.Message, "Le poste ne peut pas démarrer", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private async Task DemarrerAsync(Configuration config)
    {
        if (config.Base.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var fichier = new SqliteConnectionStringBuilder(config.Connexion).DataSource;
            Directory.CreateDirectory(Path.GetDirectoryName(fichier)!);
        }
        _db = new BaseQualite(FabriqueBase.Options(config.Base, config.Connexion));
        FabriqueBase.Preparer(_db);
        if (config.Demonstration)
        {
            Demonstration.Remplir(_db, DateTime.UtcNow);
        }

        _instrument = config.Instrument.Equals("Simule", StringComparison.OrdinalIgnoreCase)
            ? new InstrumentSimule(new Random())
            : new InstrumentSerie(config.Instrument);

        var poste = new PosteViewModel(new Atelier(_db, TimeProvider.System), _instrument, Environment.UserName, config.Poste, SynchronizationContext.Current);
        var fenetre = new MainWindow { DataContext = poste };
        MainWindow = fenetre;
        fenetre.Show();
        await poste.InitialiserAsync();
    }

    /// <summary>
    /// Mode capture : la vraie fenêtre, sur la base de démonstration en mémoire, après un petit scénario
    /// (une pièce bonne, puis une pièce non conforme). L'image sert au README et prouve en CI que l'application démarre.
    /// </summary>
    private async Task CapturerAsync(string fichierPng)
    {
        _memoire = new SqliteConnection("Data Source=:memory:");
        _memoire.Open();
        _db = new BaseQualite(new DbContextOptionsBuilder<BaseQualite>().UseSqlite(_memoire).Options);
        FabriqueBase.Preparer(_db);
        Demonstration.Remplir(_db, DateTime.UtcNow);

        var simule = new InstrumentSimule(new Random(11));
        _instrument = simule;
        var poste = new PosteViewModel(new Atelier(_db, TimeProvider.System), simule, "Opérateur de démonstration", "Contrôle final · ligne 2", ecran: null);
        await poste.InitialiserAsync();

        poste.NumeroSaisi = "AX120-0126";
        await poste.CommencerCommand.ExecuteAsync(null);
        simule.Recevoir("+11.9962 mm");
        simule.Recevoir("+120.008 mm");
        simule.Recevoir("+17.996 mm");
        poste.NumeroSaisi = "AX120-0127";
        await poste.CommencerCommand.ExecuteAsync(null);
        simule.Recevoir("+12.0034 mm");

        var fenetre = new MainWindow
        {
            DataContext = poste,
            Width = 1440,
            Height = 900,
            Left = -20000,
            ShowActivated = false,
            ShowInTaskbar = false,
        };
        fenetre.Show();
        var contenu = (FrameworkElement)fenetre.Content;
        contenu.UpdateLayout();
        await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

        var image = new RenderTargetBitmap((int)contenu.ActualWidth, (int)contenu.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        image.Render(contenu);
        var encodeur = new PngBitmapEncoder();
        encodeur.Frames.Add(BitmapFrame.Create(image));
        using (var flux = File.Create(fichierPng))
        {
            encodeur.Save(flux);
        }
        fenetre.Close();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _instrument?.Dispose();
        _db?.Dispose();
        _memoire?.Dispose();
        base.OnExit(e);
    }
}
