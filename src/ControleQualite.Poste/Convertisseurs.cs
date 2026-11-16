using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ControleQualite.Domaine;

namespace ControleQualite.Poste;

/// <summary>Vert pour une cote conforme, rouge sinon, bleu pour la cote attendue.</summary>
public sealed class CouleurVerdict : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var verdict = values[0] as Verdict?;
        var attendue = values[1] is true;
        var fond = parameter as string == "fond";
        var cle = verdict switch
        {
            Verdict.Conforme => fond ? "BonFond" : "Bon",
            Verdict.TropPetit or Verdict.TropGrand => fond ? "MauvaisFond" : "Mauvais",
            _ when attendue => fond ? "Fond" : "Accent",
            _ => fond ? "Carte" : "TexteDoux",
        };
        return Application.Current.Resources[cle];
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Le bandeau de message passe au rouge quand il signale un problème.</summary>
public sealed class CouleurMessage : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Application.Current.Resources[value is true ? (parameter as string == "fond" ? "MauvaisFond" : "Mauvais") : (parameter as string == "fond" ? "BonFond" : "Bon")];

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class CouleurNiveau : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Application.Current.Resources[(value as string) switch
        {
            "Procédé capable" => "Bon",
            "Procédé limite" => "Accent",
            _ => "Mauvais",
        }];

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class CouleurSignal : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is string texte && texte.StartsWith("Aucun", StringComparison.Ordinal)
            ? Application.Current.Resources["TexteDoux"]
            : (Brush)Application.Current.Resources["Mauvais"];

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
