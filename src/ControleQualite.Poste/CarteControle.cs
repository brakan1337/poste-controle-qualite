using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ControleQualite.Presentation;

namespace ControleQualite.Poste;

/// <summary>
/// Trace la carte des moyennes (X̄) : un point par sous-groupe, la moyenne générale et les limites de contrôle.
/// Les limites de tolérance du plan n'y figurent pas : elles s'appliquent aux pièces, pas aux moyennes,
/// et les mélanger sur ce graphique fait croire à tort qu'un procédé est sûr.
/// </summary>
public sealed class CarteControle : FrameworkElement
{
    public static readonly DependencyProperty VueProperty = DependencyProperty.Register(
        nameof(Vue), typeof(VueCarte), typeof(CarteControle),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");
    private static readonly Typeface Police = new("Segoe UI");
    private static readonly Pen Axe = Gele(new Pen(new SolidColorBrush(Color.FromRgb(0xDA, 0xDD, 0xE2)), 1));
    private static readonly Pen Moyenne = Gele(new Pen(new SolidColorBrush(Color.FromRgb(0x5C, 0x66, 0x70)), 1.2));
    private static readonly Pen Limite = Gele(new Pen(new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18)), 1.2) { DashStyle = DashStyles.Dash });
    private static readonly Pen Trace = Gele(new Pen(new SolidColorBrush(Color.FromRgb(0x1F, 0x5F, 0xBF)), 2));
    private static readonly Brush Point = Gele(new SolidColorBrush(Color.FromRgb(0x1F, 0x5F, 0xBF)));
    private static readonly Brush PointSignal = Gele(new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18)));
    private static readonly Brush Libelle = Gele(new SolidColorBrush(Color.FromRgb(0x5C, 0x66, 0x70)));

    public VueCarte? Vue
    {
        get => (VueCarte?)GetValue(VueProperty);
        set => SetValue(VueProperty, value);
    }

    private static T Gele<T>(T objet) where T : Freezable
    {
        objet.Freeze();
        return objet;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var dc = drawingContext;
        var carte = Vue?.Carte;
        if (carte is null || ActualWidth < 100 || ActualHeight < 60)
        {
            return;
        }

        const double gauche = 78, droite = 12, haut = 10, bas = 22;
        var largeur = ActualWidth - gauche - droite;
        var hauteur = ActualHeight - haut - bas;

        var valeurs = carte.Points.Select(p => p.Moyenne).Append(carte.LimiteSuperieureMoyenne).Append(carte.LimiteInferieureMoyenne).ToArray();
        var min = valeurs.Min();
        var max = valeurs.Max();
        var marge = (max - min) * 0.12;
        min -= marge;
        max += marge;

        double Y(double v) => haut + ((max - v) / (max - min) * hauteur);
        double X(int i) => gauche + (carte.Points.Count == 1 ? largeur / 2 : i * largeur / (carte.Points.Count - 1));

        dc.DrawLine(Axe, new Point(gauche, haut), new Point(gauche, haut + hauteur));
        dc.DrawLine(Axe, new Point(gauche, haut + hauteur), new Point(gauche + largeur, haut + hauteur));

        void Horizontale(Pen crayon, double valeur, string nom)
        {
            var y = Y(valeur);
            dc.DrawLine(crayon, new Point(gauche, y), new Point(gauche + largeur, y));
            // Au-dessus de la ligne : la limite basse ne déborde jamais du cadre.
            Ecrire(dc, $"{nom} {valeur.ToString("0.0000", Fr)}", new Point(4, y - 16), 11);
        }
        Horizontale(Limite, carte.LimiteSuperieureMoyenne, "LCS");
        Horizontale(Moyenne, carte.MoyenneGenerale, "X̿");
        Horizontale(Limite, carte.LimiteInferieureMoyenne, "LCI");

        var signaux = Vue!.Signaux.Select(s => s.Rang).ToHashSet();
        var trace = new StreamGeometry();
        using (var ctx = trace.Open())
        {
            ctx.BeginFigure(new Point(X(0), Y(carte.Points[0].Moyenne)), false, false);
            for (var i = 1; i < carte.Points.Count; i++)
            {
                ctx.LineTo(new Point(X(i), Y(carte.Points[i].Moyenne)), true, true);
            }
        }
        trace.Freeze();
        dc.DrawGeometry(null, Trace, trace);

        for (var i = 0; i < carte.Points.Count; i++)
        {
            var p = carte.Points[i];
            var enSignal = signaux.Contains(p.Rang);
            dc.DrawEllipse(enSignal ? PointSignal : Point, null, new Point(X(i), Y(p.Moyenne)), enSignal ? 5 : 3.5, enSignal ? 5 : 3.5);
        }

        Ecrire(dc, "1", new Point(X(0) - 3, haut + hauteur + 4), 11);
        Ecrire(dc, $"{carte.Points.Count} sous-groupes de {carte.TailleSousGroupe}", new Point(gauche + largeur - 140, haut + hauteur + 4), 11);
    }

    private void Ecrire(DrawingContext dc, string texte, Point position, double taille)
    {
        var mise = new FormattedText(texte, Fr, FlowDirection.LeftToRight, Police, taille, Libelle, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(mise, position);
    }
}
