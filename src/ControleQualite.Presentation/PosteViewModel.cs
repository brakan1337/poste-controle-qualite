using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleQualite.Domaine;
using ControleQualite.Donnees;
using ControleQualite.Instruments;

namespace ControleQualite.Presentation;

public enum EtatPoste
{
    /// <summary>Le poste attend le numéro de la pièce suivante.</summary>
    Pret,

    /// <summary>Une pièce est en cours : les mesures arrivent.</summary>
    Mesure,

    /// <summary>Une pièce non conforme attend d'être isolée : rien d'autre n'est possible.</summary>
    Quarantaine,
}

/// <summary>
/// Le poste de contrôle final. L'opérateur scanne le numéro de la pièce, l'instrument envoie
/// une mesure par cote, le poste juge. Une pièce non conforme bloque le poste tant qu'elle
/// n'est pas isolée avec un motif : impossible de l'oublier dans le flux des pièces bonnes.
/// </summary>
public sealed class PosteViewModel : ObservableObject, IDisposable
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    private readonly Atelier _atelier;
    private readonly IInstrument _instrument;
    private readonly SynchronizationContext? _ecran;

    private OrdreFabrication? _of;
    private ControlePiece? _piece;
    private PieceControlee? _enQuarantaine;
    private string _dernierNumero = "";
    private EtatPoste _etat;
    private string _numeroSaisi = "";
    private string _saisieManuelle = "";
    private string _motif = "";
    private string _message = "";
    private bool _messageAlerte;
    private string _bilan = "";
    private VueCarte? _carte;
    private LigneCote? _coteSuivie;

    /// <param name="ecran">Le contexte du fil de l'écran (celui de WPF), ou null pour tout traiter sur place, comme dans les tests.</param>
    public PosteViewModel(Atelier atelier, IInstrument instrument, string operateur, string nomPoste, SynchronizationContext? ecran)
    {
        _atelier = atelier;
        _instrument = instrument;
        _ecran = ecran;
        Operateur = operateur;
        NomPoste = nomPoste;

        CommencerCommand = new AsyncRelayCommand(CommencerAsync, () => Etat == EtatPoste.Pret);
        SaisirCommand = new AsyncRelayCommand(SaisirAsync, () => Etat == EtatPoste.Mesure);
        IsolerCommand = new AsyncRelayCommand(IsolerAsync, () => Etat == EtatPoste.Quarantaine);
        SimulerCommand = new RelayCommand(SimulerEnvoi, () => Etat == EtatPoste.Mesure && _instrument is InstrumentSimule);

        _instrument.MesureRecue += SurMesure;
        _instrument.TrameIllisible += SurTrameIllisible;
    }

    public string Operateur { get; }
    public string NomPoste { get; }
    public string Instrument => _instrument.Description;
    public ObservableCollection<OrdreFabrication> Ordres { get; } = [];
    public ObservableCollection<LigneCote> Cotes { get; } = [];

    public IAsyncRelayCommand CommencerCommand { get; }
    public IAsyncRelayCommand SaisirCommand { get; }
    public IAsyncRelayCommand IsolerCommand { get; }

    /// <summary>Sans matériel branché : simule l'appui sur « envoi » de l'instrument.</summary>
    public IRelayCommand SimulerCommand { get; }

    public bool InstrumentSimule => _instrument is InstrumentSimule;

    public OrdreFabrication? Of
    {
        get => _of;
        private set
        {
            if (SetProperty(ref _of, value))
            {
                OnPropertyChanged(nameof(TitreOf));
            }
        }
    }

    public string TitreOf => Of is null ? "Aucun ordre de fabrication ouvert" : $"{Of.Numero} · {Of.Reference.Designation}";

    public EtatPoste Etat
    {
        get => _etat;
        private set
        {
            if (SetProperty(ref _etat, value))
            {
                OnPropertyChanged(nameof(EnQuarantaine));
                OnPropertyChanged(nameof(PeutScanner));
                CommencerCommand.NotifyCanExecuteChanged();
                SaisirCommand.NotifyCanExecuteChanged();
                IsolerCommand.NotifyCanExecuteChanged();
                SimulerCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool EnQuarantaine => Etat == EtatPoste.Quarantaine;
    public bool PeutScanner => Etat == EtatPoste.Pret;
    public string NumeroEnQuarantaine => _enQuarantaine?.Numero ?? "";

    public string NumeroSaisi { get => _numeroSaisi; set => SetProperty(ref _numeroSaisi, value); }
    public string SaisieManuelle { get => _saisieManuelle; set => SetProperty(ref _saisieManuelle, value); }
    public string Motif { get => _motif; set => SetProperty(ref _motif, value); }
    public string Message { get => _message; private set => SetProperty(ref _message, value); }
    public bool MessageAlerte { get => _messageAlerte; private set => SetProperty(ref _messageAlerte, value); }
    public string Bilan { get => _bilan; private set => SetProperty(ref _bilan, value); }
    public VueCarte? Carte { get => _carte; private set => SetProperty(ref _carte, value); }

    /// <summary>La cote dont la carte de contrôle est affichée.</summary>
    public LigneCote? CoteSuivie
    {
        get => _coteSuivie;
        set
        {
            if (SetProperty(ref _coteSuivie, value) && value is not null)
            {
                _ = ActualiserCarteAsync();
            }
        }
    }

    /// <summary>La pièce dont les cotes sont affichées : celle en cours, ou la dernière jugée jusqu'au scan suivant.</summary>
    public string NumeroPieceEnCours => _piece?.Numero ?? _dernierNumero;

    public async Task InitialiserAsync()
    {
        foreach (var of in await _atelier.OrdresOuverts())
        {
            Ordres.Add(of);
        }
        Of = Ordres.FirstOrDefault();
        if (Of is null)
        {
            Informer("Aucun ordre de fabrication ouvert : rien à contrôler.", alerte: true);
            return;
        }
        foreach (var c in Of.Reference.Cotes.OrderBy(c => c.Ordre))
        {
            Cotes.Add(new LigneCote(c.VersDomaine(), c.Id));
        }
        _instrument.Demarrer();

        // Une non-conformité laissée en plan (poste redémarré, coupure) bloque toujours le poste.
        _enQuarantaine = await _atelier.QuarantaineEnAttente(Of.Id);
        if (_enQuarantaine is not null)
        {
            Etat = EtatPoste.Quarantaine;
            OnPropertyChanged(nameof(NumeroEnQuarantaine));
            Informer($"La pièce {_enQuarantaine.Numero} est non conforme et n'a pas été isolée.", alerte: true);
        }
        else
        {
            Informer("Scannez le numéro de la pièce.", alerte: false);
        }
        CoteSuivie = Cotes.FirstOrDefault();
        await ActualiserBilanAsync();
    }

    private async Task CommencerAsync()
    {
        if (Of is null || string.IsNullOrWhiteSpace(NumeroSaisi))
        {
            Informer("Scannez ou tapez le numéro de la pièce.", alerte: true);
            return;
        }
        if (await _atelier.DejaControlee(Of.Id, NumeroSaisi))
        {
            Informer($"La pièce {NumeroSaisi.Trim()} a déjà été contrôlée sur cet OF.", alerte: true);
            NumeroSaisi = "";
            return;
        }
        _piece = new ControlePiece(NumeroSaisi, [.. Cotes.Select(c => c.Cote)]);
        _dernierNumero = _piece.Numero;
        NumeroSaisi = "";
        foreach (var c in Cotes)
        {
            c.Effacer();
        }
        Cotes[0].Attendue = true;
        OnPropertyChanged(nameof(NumeroPieceEnCours));
        Etat = EtatPoste.Mesure;
        Informer($"Pièce {_piece.Numero} : mesurez « {Cotes[0].Nom} ».", alerte: false);
    }

    private async Task SaisirAsync()
    {
        var texte = SaisieManuelle.Trim().Replace(',', '.');
        if (!double.TryParse(texte, NumberStyles.Float, CultureInfo.InvariantCulture, out var valeur))
        {
            Informer($"« {SaisieManuelle} » n'est pas une mesure.", alerte: true);
            return;
        }
        SaisieManuelle = "";
        await EnregistrerMesureAsync(valeur);
    }

    /// <summary>Les mesures de l'instrument arrivent sur le fil du port série : on les ramène sur celui de l'écran.</summary>
    private void SurMesure(object? sender, double valeur)
    {
        if (_ecran is null)
        {
            EnregistrerMesureAsync(valeur).GetAwaiter().GetResult();
        }
        else
        {
            _ecran.Post(async _ => await EnregistrerMesureAsync(valeur), null);
        }
    }

    private void SurTrameIllisible(object? sender, string trame)
    {
        void Afficher(object? _) => Informer($"Trame illisible reçue de l'instrument : « {trame} ». Vérifiez son réglage.", alerte: true);
        if (_ecran is null)
        {
            Afficher(null);
        }
        else
        {
            _ecran.Post(Afficher, null);
        }
    }

    private async Task EnregistrerMesureAsync(double valeur)
    {
        if (Etat != EtatPoste.Mesure || _piece is null || Of is null)
        {
            Informer("Mesure reçue hors contrôle : scannez d'abord une pièce.", alerte: true);
            return;
        }

        var rang = _piece.Valeurs.Count;
        var verdict = _piece.Mesurer(valeur);
        var ligne = Cotes[rang];
        ligne.Valeur = valeur;
        ligne.Verdict = verdict;
        ligne.Attendue = false;

        if (_piece.Etat == EtatPiece.EnCours)
        {
            Cotes[rang + 1].Attendue = true;
            Informer($"Pièce {_piece.Numero} : mesurez « {Cotes[rang + 1].Nom} ».", alerte: false);
            return;
        }

        var numero = _piece.Numero;
        PieceControlee enregistree;
        try
        {
            enregistree = await _atelier.Enregistrer(Of.Id, _piece, Operateur);
        }
        catch (PieceDejaControleeException e)
        {
            // Un autre poste a pu contrôler la même pièce entre-temps.
            _piece = null;
            Etat = EtatPoste.Pret;
            Informer(e.Message, alerte: true);
            return;
        }
        _piece = null;
        if (enregistree.Etat == EtatPiece.Conforme)
        {
            Etat = EtatPoste.Pret;
            Informer($"Pièce {numero} conforme. Scannez la suivante.", alerte: false);
        }
        else
        {
            _enQuarantaine = enregistree;
            OnPropertyChanged(nameof(NumeroEnQuarantaine));
            Etat = EtatPoste.Quarantaine;
            Informer($"Pièce {numero} NON CONFORME : « {ligne.Nom} » {ligne.VerdictTexte.ToLower(Fr)}. Isolez-la.", alerte: true);
        }
        await ActualiserBilanAsync();
        await ActualiserCarteAsync();
    }

    private void SimulerEnvoi()
    {
        if (_instrument is InstrumentSimule simule && Cotes.FirstOrDefault(c => c.Attendue) is { } ligne)
        {
            // Une mesure centrée dans la tolérance, avec une dispersion d'un dixième de l'intervalle.
            var cote = ligne.Cote;
            simule.Envoyer((cote.LimiteInferieure + cote.LimiteSuperieure) / 2, cote.IntervalleDeTolerance / 10);
        }
    }

    private async Task IsolerAsync()
    {
        if (_enQuarantaine is null)
        {
            return;
        }
        try
        {
            await _atelier.MettreEnQuarantaine(_enQuarantaine.Id, Motif);
        }
        catch (ArgumentException e)
        {
            Informer(e.Message, alerte: true);
            return;
        }
        Informer($"Pièce {_enQuarantaine.Numero} isolée. Scannez la pièce suivante.", alerte: false);
        _enQuarantaine = null;
        _dernierNumero = "";
        OnPropertyChanged(nameof(NumeroPieceEnCours));
        Motif = "";
        OnPropertyChanged(nameof(NumeroEnQuarantaine));
        foreach (var c in Cotes)
        {
            c.Effacer();
        }
        Etat = EtatPoste.Pret;
    }

    private async Task ActualiserBilanAsync()
    {
        if (Of is null)
        {
            return;
        }
        var b = await _atelier.Bilan(Of.Id);
        Bilan = string.Create(Fr, $"{b.Controlees} / {b.Quantite} pièces contrôlées · {b.NonConformes} non conforme(s) ({b.TauxNonConformite:P1})");
    }

    private async Task ActualiserCarteAsync()
    {
        if (CoteSuivie is null)
        {
            return;
        }
        var historique = await _atelier.Historique(CoteSuivie.CoteId);
        Carte = VueCarte.Construire(CoteSuivie.Cote, historique);
    }

    private void Informer(string message, bool alerte)
    {
        Message = message;
        MessageAlerte = alerte;
    }

    public void Dispose()
    {
        _instrument.MesureRecue -= SurMesure;
        _instrument.TrameIllisible -= SurTrameIllisible;
    }
}
