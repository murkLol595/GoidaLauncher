using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace GoidaLauncher.Pages;

/// <summary>
/// Показывает 3D-модель игрока (геометрия — из присланной GLB-модели, см. SteveMeshData)
/// с наложенным поверх скином. Вращается сама по себе, либо мышью — потяни за модель.
/// </summary>
public partial class SkinViewer3D : UserControl
{
    private readonly ModelVisual3D _modelVisual = new();
    private readonly AxisAngleRotation3D _rotation = new(new Vector3D(0, 1, 0), 0);
    private bool _dragging;
    private Point _lastMousePos;

    public SkinViewer3D()
    {
        InitializeComponent();
        SetupScene();

        MouseLeftButtonDown += OnMouseDown;
        MouseLeftButtonUp += OnMouseUp;
        MouseMove += OnMouseMove;
        MouseLeave += (_, _) => EndDrag();

        IsVisibleChanged += (_, e) =>
        {
            if ((bool)e.NewValue)
                Resume();
            else
                Pause();
        };

        StartAutoRotate(0);
    }

    private void SetupScene()
    {
        var camera = new PerspectiveCamera
        {
            Position = new Point3D(0, 0, 58),
            LookDirection = new Vector3D(0, -0.01, -1),
            UpDirection = new Vector3D(0, 1, 0),
            FieldOfView = 34,
        };
        Viewport.Camera = camera;

        var lights = new Model3DGroup();
        lights.Children.Add(new AmbientLight(Color.FromRgb(150, 150, 150)));
        lights.Children.Add(new DirectionalLight(Color.FromRgb(170, 170, 170), new Vector3D(-0.4, -0.6, -1)));
        lights.Children.Add(new DirectionalLight(Color.FromRgb(90, 90, 90), new Vector3D(0.5, 0.3, 0.8)));
        Viewport.Children.Add(new ModelVisual3D { Content = lights });

        _modelVisual.Transform = new RotateTransform3D(_rotation);
        Viewport.Children.Add(_modelVisual);
    }

    /// <summary>Накладывает PNG-скин на модель. null/отсутствующий файл — показываем подсказку вместо модели.</summary>
    public void SetSkin(string? pngPath)
    {
        if (string.IsNullOrWhiteSpace(pngPath) || !File.Exists(pngPath))
        {
            _modelVisual.Content = null;
            EmptyHint.Visibility = Visibility.Visible;
            return;
        }

        EmptyHint.Visibility = Visibility.Collapsed;

        BitmapSource bitmap;
        MeshGeometry3D baseMesh;
        MeshGeometry3D outerMesh;
        try
        {
            (bitmap, baseMesh, outerMesh) = SteveMeshData.LoadSkinAndBuildModel(pngPath);
        }
        catch
        {
            _modelVisual.Content = null;
            EmptyHint.Text = "Не удалось открыть скин";
            EmptyHint.Visibility = Visibility.Visible;
            return;
        }

        if (baseMesh.CanFreeze) baseMesh.Freeze();
        if (outerMesh.CanFreeze) outerMesh.Freeze();

        var brush = new ImageBrush(bitmap)
        {
            ViewportUnits = BrushMappingMode.Absolute,
            Viewport = new Rect(0, 0, 1, 1),
        };
        RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.NearestNeighbor);
        if (brush.CanFreeze) brush.Freeze();

        var material = new DiffuseMaterial(brush);
        if (material.CanFreeze) material.Freeze();

        var group = new Model3DGroup();
        group.Children.Add(new GeometryModel3D(baseMesh, material) { BackMaterial = material });
        group.Children.Add(new GeometryModel3D(outerMesh, material) { BackMaterial = material });
        if (group.CanFreeze) group.Freeze();

        _modelVisual.Content = group;
    }

    // ---------------------------------------------------------------
    // Вращение: авто-поворот, либо мышью (тянешь — крутится, отпустил — авто-поворот продолжается)
    // ---------------------------------------------------------------

    /// <summary>
    /// Приостанавливает 3D-вращение для экономии ресурсов GPU/CPU.
    /// </summary>
    public void Pause()
    {
        _rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, null);
    }

    /// <summary>
    /// Возобновляет вращение при возврате на вкладку или закрытии игры.
    /// </summary>
    public void Resume()
    {
        if (IsVisible && !_dragging)
        {
            StartAutoRotate(_rotation.Angle);
        }
    }

    private void StartAutoRotate(double fromAngle)
    {
        var anim = new DoubleAnimation(fromAngle, fromAngle + 360, TimeSpan.FromSeconds(14))
        {
            RepeatBehavior = RepeatBehavior.Forever,
        };
        Timeline.SetDesiredFrameRate(anim, 45); // 45 FPS для 3D превью абсолютно плавно и экономит GPU
        _rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, anim);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true;
        _lastMousePos = e.GetPosition(this);
        _rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, null); // фиксирует текущий угол
        CaptureMouse();
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e) => EndDrag();

    private void EndDrag()
    {
        if (!_dragging) return;
        _dragging = false;
        ReleaseMouseCapture();
        StartAutoRotate(_rotation.Angle);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var pos = e.GetPosition(this);
        _rotation.Angle += (pos.X - _lastMousePos.X) * 0.6;
        _lastMousePos = pos;
    }
}
