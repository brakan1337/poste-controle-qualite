using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using ControleQualite.Presentation;

namespace ControleQualite.Poste;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is INotifyPropertyChanged poste)
            {
                poste.PropertyChanged += RemettreLeCurseurSurLeScan;
            }
        };
        Loaded += (_, _) => Scan.Focus();
    }

    /// <summary>Dès que le poste attend une nouvelle pièce, le curseur revient dans le champ du scan : la douchette écrit au bon endroit.</summary>
    private void RemettreLeCurseurSurLeScan(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PosteViewModel.PeutScanner) && sender is PosteViewModel { PeutScanner: true })
        {
            Dispatcher.BeginInvoke(() => Scan.Focus(), DispatcherPriority.Input);
        }
    }
}
