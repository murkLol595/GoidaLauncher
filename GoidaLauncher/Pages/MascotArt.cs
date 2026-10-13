using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace GoidaLauncher.Pages;

/// <summary>
/// Простая декоративная блочная свинка и овечка для пустого места в левой панели.
/// Нарисованы вручную геометрическими фигурами — свой стиль, не текстуры Minecraft.
/// Кликабельны (см. HomePage.MascotArea_Click) — пасхалка №2.
/// </summary>
public static class MascotArt
{
    public static void Draw(Canvas canvas)
    {
        canvas.Children.Clear();
        DrawSheep(canvas, offsetX: 0);
        DrawPig(canvas, offsetX: 110);
    }

    private static void DrawSheep(Canvas canvas, double offsetX)
    {
        var wool = new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2));
        var face = new SolidColorBrush(Color.FromRgb(0x5A, 0x4A, 0x3E));
        var dark = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));

        AddRect(canvas, offsetX + 5, 70, 110, 65, 22, wool);
        AddRect(canvas, offsetX + 15, 35, 55, 48, 16, wool);
        AddRect(canvas, offsetX + 24, 52, 36, 28, 10, face);
        AddEllipse(canvas, offsetX + 30, 62, 6, 6, dark);
        AddEllipse(canvas, offsetX + 50, 62, 6, 6, dark);

        foreach (var lx in new[] { 15.0, 40.0, 70.0, 95.0 })
            AddRect(canvas, offsetX + lx, 130, 12, 40, 3, dark);
    }

    private static void DrawPig(Canvas canvas, double offsetX)
    {
        var pink = new SolidColorBrush(Color.FromRgb(0xF4, 0xA9, 0xB4));
        var darkPink = new SolidColorBrush(Color.FromRgb(0xE0, 0x84, 0x94));
        var dark = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));

        AddRect(canvas, offsetX + 0, 95, 120, 58, 22, pink);
        AddRect(canvas, offsetX + 65, 55, 65, 55, 18, pink);
        AddRect(canvas, offsetX + 82, 82, 30, 22, 10, darkPink);
        AddEllipse(canvas, offsetX + 88, 91, 5, 7, dark);
        AddEllipse(canvas, offsetX + 100, 91, 5, 7, dark);
        AddEllipse(canvas, offsetX + 78, 68, 6, 6, dark);
        AddEllipse(canvas, offsetX + 108, 68, 6, 6, dark);
        AddRect(canvas, offsetX + 70, 45, 16, 16, 4, pink);
        AddRect(canvas, offsetX + 112, 45, 16, 16, 4, pink);

        foreach (var lx in new[] { 10.0, 35.0, 75.0, 100.0 })
            AddRect(canvas, offsetX + lx, 148, 14, 32, 3, darkPink);
    }

    private static void AddRect(Canvas canvas, double x, double y, double w, double h, double radius, Brush fill)
    {
        var rect = new Rectangle { Width = w, Height = h, RadiusX = radius, RadiusY = radius, Fill = fill };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        canvas.Children.Add(rect);
    }

    private static void AddEllipse(Canvas canvas, double x, double y, double w, double h, Brush fill)
    {
        var ellipse = new Ellipse { Width = w, Height = h, Fill = fill };
        Canvas.SetLeft(ellipse, x);
        Canvas.SetTop(ellipse, y);
        canvas.Children.Add(ellipse);
    }

    /// <summary>Пасхалка №2: короткий "подпрыг" при клике.</summary>
    public static void PlayBounce(Canvas canvas)
    {
        if (canvas.RenderTransform is not TranslateTransform transform)
        {
            transform = new TranslateTransform();
            canvas.RenderTransform = transform;
        }

        var anim = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(600) };
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0)));
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(-22, KeyTime.FromPercent(0.35))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        });
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1))
        {
            EasingFunction = new BounceEase { Bounces = 1, Bounciness = 3 },
        });

        transform.BeginAnimation(TranslateTransform.YProperty, anim);
    }
}
