using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace GoidaLauncher.Pages;

public readonly struct BoxFaceUvs
{
    public readonly Rect Top;
    public readonly Rect Bottom;
    public readonly Rect Right;
    public readonly Rect Front;
    public readonly Rect Left;
    public readonly Rect Back;

    public BoxFaceUvs(Rect top, Rect bottom, Rect right, Rect front, Rect left, Rect back)
    {
        Top = top;
        Bottom = bottom;
        Right = right;
        Front = front;
        Left = left;
        Back = back;
    }

    public static BoxFaceUvs Create(int u, int v, int w, int h, int d)
    {
        return new BoxFaceUvs(
            top:    new Rect(u + d, v, w, d),
            bottom: new Rect(u + d + w, v, w, d),
            right:  new Rect(u, v + d, d, h),
            front:  new Rect(u + d, v + d, w, h),
            left:   new Rect(u + d + w, v + d, d, h),
            back:   new Rect(u + d + w + d, v + d, w, h)
        );
    }
}

public static class SteveMeshData
{
    public static (BitmapSource texture, MeshGeometry3D baseMesh, MeshGeometry3D outerMesh) LoadSkinAndBuildModel(string filePath)
    {
        var (texture, raw64, isSlim) = LoadAndProcessSkin(filePath);
        var (baseMesh, outerMesh) = BuildMeshes(raw64, isSlim, posed: true);
        return (texture, baseMesh, outerMesh);
    }

    public static (MeshGeometry3D baseMesh, MeshGeometry3D outerMesh) BuildMeshes(
        byte[] skin64, bool isSlim, bool posed = true)
    {
        var baseMesh = new MeshGeometry3D();
        var outerMesh = new MeshGeometry3D();

        int armW = isSlim ? 3 : 4;
        double armWd = isSlim ? 3.0 : 4.0;

        // Поза персонажа (небольшой естественный разворот конечностей, как в Minecraft)
        double rightArmAngle = posed ? -12.0 : 0.0;
        double leftArmAngle = posed ? 12.0 : 0.0;
        double rightLegAngle = posed ? 12.0 : 0.0;
        double leftLegAngle = posed ? -12.0 : 0.0;
        double headAngleY = posed ? -5.0 : 0.0;

        // 1. Голова (пивот в шее, y=8)
        var headUvs = BoxFaceUvs.Create(0, 0, 8, 8, 8);
        var hatUvs  = BoxFaceUvs.Create(32, 0, 8, 8, 8);
        AddBox(baseMesh, -4, 8, -4, 4, 16, 4, headUvs, skin64, isOuter: false, angleX: 0, angleY: headAngleY, pivotY: 8);
        AddBox(outerMesh, -4.5, 7.5, -4.5, 4.5, 16.5, 4.5, hatUvs, skin64, isOuter: true, angleX: 0, angleY: headAngleY, pivotY: 8);

        // 2. Тело (центр)
        var torsoUvs  = BoxFaceUvs.Create(16, 16, 8, 12, 4);
        var jacketUvs = BoxFaceUvs.Create(16, 32, 8, 12, 4);
        AddBox(baseMesh, -4, -4, -2, 4, 8, 2, torsoUvs, skin64, isOuter: false);
        AddBox(outerMesh, -4.25, -4.25, -2.25, 4.25, 8.25, 2.25, jacketUvs, skin64, isOuter: true);

        // 3. Правая рука (пивот в плече: y=6)
        double rArmX0 = -4 - armWd;
        double rArmX1 = -4;
        var rArmUvs    = BoxFaceUvs.Create(40, 16, armW, 12, 4);
        var rSleeveUvs = BoxFaceUvs.Create(40, 32, armW, 12, 4);
        AddBox(baseMesh, rArmX0, -4, -2, rArmX1, 8, 2, rArmUvs, skin64, isOuter: false, angleX: rightArmAngle, pivotY: 6);
        AddBox(outerMesh, rArmX0 - 0.25, -4.25, -2.25, rArmX1 + 0.25, 8.25, 2.25, rSleeveUvs, skin64, isOuter: true, angleX: rightArmAngle, pivotY: 6);

        // 4. Левая рука (пивот в плече: y=6)
        double lArmX0 = 4;
        double lArmX1 = 4 + armWd;
        var lArmUvs    = BoxFaceUvs.Create(32, 48, armW, 12, 4);
        var lSleeveUvs = BoxFaceUvs.Create(48, 48, armW, 12, 4);
        AddBox(baseMesh, lArmX0, -4, -2, lArmX1, 8, 2, lArmUvs, skin64, isOuter: false, angleX: leftArmAngle, pivotY: 6);
        AddBox(outerMesh, lArmX0 - 0.25, -4.25, -2.25, lArmX1 + 0.25, 8.25, 2.25, lSleeveUvs, skin64, isOuter: true, angleX: leftArmAngle, pivotY: 6);

        // 5. Правая нога (пивот в бедре: y=-4)
        var rLegUvs   = BoxFaceUvs.Create(0, 16, 4, 12, 4);
        var rPantsUvs = BoxFaceUvs.Create(0, 32, 4, 12, 4);
        AddBox(baseMesh, -4, -16, -2, 0, -4, 2, rLegUvs, skin64, isOuter: false, angleX: rightLegAngle, pivotY: -4);
        AddBox(outerMesh, -4.25, -16.25, -2.25, 0.25, -3.75, 2.25, rPantsUvs, skin64, isOuter: true, angleX: rightLegAngle, pivotY: -4);

        // 6. Левая нога (пивот в бедре: y=-4)
        var lLegUvs   = BoxFaceUvs.Create(16, 48, 4, 12, 4);
        var lPantsUvs = BoxFaceUvs.Create(0, 48, 4, 12, 4);
        AddBox(baseMesh, 0, -16, -2, 4, -4, 2, lLegUvs, skin64, isOuter: false, angleX: leftLegAngle, pivotY: -4);
        AddBox(outerMesh, -0.25, -16.25, -2.25, 4.25, -3.75, 2.25, lPantsUvs, skin64, isOuter: true, angleX: leftLegAngle, pivotY: -4);

        baseMesh.Freeze();
        outerMesh.Freeze();
        return (baseMesh, outerMesh);
    }

    private static void AddBox(
        MeshGeometry3D mesh,
        double x0, double y0, double z0,
        double x1, double y1, double z1,
        BoxFaceUvs uvs,
        byte[] skin64,
        bool isOuter,
        double angleX = 0, double angleY = 0,
        double pivotY = 0, double pivotZ = 0)
    {
        Point3D XForm(double px, double py, double pz)
        {
            double rx = px, ry = py, rz = pz;
            if (Math.Abs(angleX) > 0.001)
            {
                double rad = angleX * Math.PI / 180.0;
                double dy = ry - pivotY;
                double dz = rz - pivotZ;
                ry = pivotY + (dy * Math.Cos(rad) - dz * Math.Sin(rad));
                rz = pivotZ + (dy * Math.Sin(rad) + dz * Math.Cos(rad));
            }
            if (Math.Abs(angleY) > 0.001)
            {
                double rad = angleY * Math.PI / 180.0;
                double nx = rx * Math.Cos(rad) + rz * Math.Sin(rad);
                double nz = -rx * Math.Sin(rad) + rz * Math.Cos(rad);
                rx = nx;
                rz = nz;
            }
            return new Point3D(rx, ry, rz);
        }

        Vector3D XFormNorm(Vector3D n)
        {
            double rx = n.X, ry = n.Y, rz = n.Z;
            if (Math.Abs(angleX) > 0.001)
            {
                double rad = angleX * Math.PI / 180.0;
                double ny = ry * Math.Cos(rad) - rz * Math.Sin(rad);
                double nz = ry * Math.Sin(rad) + rz * Math.Cos(rad);
                ry = ny; rz = nz;
            }
            if (Math.Abs(angleY) > 0.001)
            {
                double rad = angleY * Math.PI / 180.0;
                double nx = rx * Math.Cos(rad) + rz * Math.Sin(rad);
                double nz = -rx * Math.Sin(rad) + rz * Math.Cos(rad);
                rx = nx; rz = nz;
            }
            return new Vector3D(rx, ry, rz);
        }

        void AddFace(Point3D p0, Point3D p1, Point3D p2, Point3D p3, Vector3D normal, Rect uv)
        {
            if (!isOuter)
            {
                // Для базового слоя: вся поверхность непрозрачна
                int baseIdx = mesh.Positions.Count;
                mesh.Positions.Add(p0);
                mesh.Positions.Add(p1);
                mesh.Positions.Add(p2);
                mesh.Positions.Add(p3);

                mesh.Normals.Add(normal);
                mesh.Normals.Add(normal);
                mesh.Normals.Add(normal);
                mesh.Normals.Add(normal);

                mesh.TextureCoordinates.Add(new Point(uv.Left / 64.0, uv.Bottom / 64.0));
                mesh.TextureCoordinates.Add(new Point(uv.Right / 64.0, uv.Bottom / 64.0));
                mesh.TextureCoordinates.Add(new Point(uv.Right / 64.0, uv.Top / 64.0));
                mesh.TextureCoordinates.Add(new Point(uv.Left / 64.0, uv.Top / 64.0));

                mesh.TriangleIndices.Add(baseIdx + 0);
                mesh.TriangleIndices.Add(baseIdx + 1);
                mesh.TriangleIndices.Add(baseIdx + 2);
                mesh.TriangleIndices.Add(baseIdx + 0);
                mesh.TriangleIndices.Add(baseIdx + 2);
                mesh.TriangleIndices.Add(baseIdx + 3);
            }
            else
            {
                // Для внешнего слоя: генерируем квады ТОЛЬКО для непрозрачных пикселей (как в Minecraft).
                // Это полностью устраняет баг Z-буфера WPF 3D (когда прозрачные пиксели перекрывали базовую модель
                // и вырезали в ней дыры), а также объединяет соседние пиксели в эффективные полосы.
                int w = (int)uv.Width;
                int h = (int)uv.Height;
                int uvLeft = (int)uv.Left;
                int uvTop = (int)uv.Top;

                for (int py = 0; py < h; py++)
                {
                    int texY = uvTop + py;
                    if (texY < 0 || texY >= 64) continue;

                    int x = 0;
                    while (x < w)
                    {
                        int texX = uvLeft + x;
                        if (texX < 0 || texX >= 64) { x++; continue; }

                        int alpha = skin64[(texY * 64 + texX) * 4 + 3];
                        if (alpha <= 10)
                        {
                            x++;
                            continue;
                        }

                        // Нашли непрозрачный пиксель, объединяем непрерывную горизонтальную цепочку
                        int xStart = x;
                        while (x < w)
                        {
                            int curTexX = uvLeft + x;
                            if (curTexX < 0 || curTexX >= 64) break;
                            if (skin64[(texY * 64 + curTexX) * 4 + 3] <= 10) break;
                            x++;
                        }
                        int xEnd = x - 1;

                        double u0 = (double)xStart / w;
                        double u1 = (double)(xEnd + 1) / w;
                        double v0 = (double)(h - (py + 1)) / h; // низ квада в 3D
                        double v1 = (double)(h - py) / h;       // верх квада в 3D

                        Point3D q0 = LerpQuad(p0, p1, p2, p3, u0, v0);
                        Point3D q1 = LerpQuad(p0, p1, p2, p3, u1, v0);
                        Point3D q2 = LerpQuad(p0, p1, p2, p3, u1, v1);
                        Point3D q3 = LerpQuad(p0, p1, p2, p3, u0, v1);

                        double tU0 = (uvLeft + xStart) / 64.0;
                        double tU1 = (uvLeft + xEnd + 1) / 64.0;
                        double tV0 = (uvTop + py + 1) / 64.0; // bottom V
                        double tV1 = (uvTop + py) / 64.0;     // top V

                        int baseIdx = mesh.Positions.Count;
                        mesh.Positions.Add(q0);
                        mesh.Positions.Add(q1);
                        mesh.Positions.Add(q2);
                        mesh.Positions.Add(q3);

                        mesh.Normals.Add(normal);
                        mesh.Normals.Add(normal);
                        mesh.Normals.Add(normal);
                        mesh.Normals.Add(normal);

                        mesh.TextureCoordinates.Add(new Point(tU0, tV0));
                        mesh.TextureCoordinates.Add(new Point(tU1, tV0));
                        mesh.TextureCoordinates.Add(new Point(tU1, tV1));
                        mesh.TextureCoordinates.Add(new Point(tU0, tV1));

                        mesh.TriangleIndices.Add(baseIdx + 0);
                        mesh.TriangleIndices.Add(baseIdx + 1);
                        mesh.TriangleIndices.Add(baseIdx + 2);
                        mesh.TriangleIndices.Add(baseIdx + 0);
                        mesh.TriangleIndices.Add(baseIdx + 2);
                        mesh.TriangleIndices.Add(baseIdx + 3);
                    }
                }
            }
        }

        // Front (+Z)
        AddFace(
            XForm(x0, y0, z1), XForm(x1, y0, z1), XForm(x1, y1, z1), XForm(x0, y1, z1),
            XFormNorm(new Vector3D(0, 0, 1)), uvs.Front);

        // Back (-Z)
        AddFace(
            XForm(x1, y0, z0), XForm(x0, y0, z0), XForm(x0, y1, z0), XForm(x1, y1, z0),
            XFormNorm(new Vector3D(0, 0, -1)), uvs.Back);

        // Top (+Y)
        AddFace(
            XForm(x0, y1, z1), XForm(x1, y1, z1), XForm(x1, y1, z0), XForm(x0, y1, z0),
            XFormNorm(new Vector3D(0, 1, 0)), uvs.Top);

        // Bottom (-Y)
        AddFace(
            XForm(x0, y0, z0), XForm(x1, y0, z0), XForm(x1, y0, z1), XForm(x0, y0, z1),
            XFormNorm(new Vector3D(0, -1, 0)), uvs.Bottom);

        // Right (-X face)
        AddFace(
            XForm(x0, y0, z0), XForm(x0, y0, z1), XForm(x0, y1, z1), XForm(x0, y1, z0),
            XFormNorm(new Vector3D(-1, 0, 0)), uvs.Right);

        // Left (+X face)
        AddFace(
            XForm(x1, y0, z1), XForm(x1, y0, z0), XForm(x1, y1, z0), XForm(x1, y1, z1),
            XFormNorm(new Vector3D(1, 0, 0)), uvs.Left);
    }

    private static Point3D LerpQuad(Point3D p0, Point3D p1, Point3D p2, Point3D p3, double u, double v)
    {
        double bx = p0.X + u * (p1.X - p0.X);
        double by = p0.Y + u * (p1.Y - p0.Y);
        double bz = p0.Z + u * (p1.Z - p0.Z);

        double tx = p3.X + u * (p2.X - p3.X);
        double ty = p3.Y + u * (p2.Y - p3.Y);
        double tz = p3.Z + u * (p2.Z - p3.Z);

        return new Point3D(
            bx + v * (tx - bx),
            by + v * (ty - by),
            bz + v * (tz - bz)
        );
    }

    public static (BitmapSource texture, byte[] raw64, bool isSlim) LoadAndProcessSkin(string filePath)
    {
        var rawBmp = new BitmapImage();
        rawBmp.BeginInit();
        rawBmp.CacheOption = BitmapCacheOption.OnLoad;
        rawBmp.UriSource = new Uri(filePath, UriKind.Absolute);
        rawBmp.EndInit();
        rawBmp.Freeze();

        int width = rawBmp.PixelWidth;
        int height = rawBmp.PixelHeight;

        var converted = new FormatConvertedBitmap(rawBmp, PixelFormats.Bgra32, null, 0);
        converted.Freeze();

        int stride = width * 4;
        byte[] srcPixels = new byte[height * stride];
        converted.CopyPixels(srcPixels, stride, 0);

        bool is64x32 = (width == 64 && height == 32) || (width == height * 2);

        byte[] pixels64 = new byte[64 * 64 * 4];

        if (is64x32)
        {
            for (int y = 0; y < 32; y++)
            {
                Buffer.BlockCopy(srcPixels, y * 64 * 4, pixels64, y * 64 * 4, 64 * 4);
            }

            CopyMirrored(pixels64, 0, 16, 4, 12, 4, 16, 48);
            CopyMirrored(pixels64, 40, 16, 4, 12, 4, 32, 48);
        }
        else if (width == 64 && height == 64)
        {
            Buffer.BlockCopy(srcPixels, 0, pixels64, 0, 64 * 64 * 4);
        }
        else
        {
            for (int y = 0; y < 64; y++)
            {
                int sy = (int)(y * (double)height / 64.0);
                for (int x = 0; x < 64; x++)
                {
                    int sx = (int)(x * (double)width / 64.0);
                    int sIdx = (sy * width + sx) * 4;
                    int dIdx = (y * 64 + x) * 4;
                    pixels64[dIdx + 0] = srcPixels[sIdx + 0];
                    pixels64[dIdx + 1] = srcPixels[sIdx + 1];
                    pixels64[dIdx + 2] = srcPixels[sIdx + 2];
                    pixels64[dIdx + 3] = srcPixels[sIdx + 3];
                }
            }
        }

        // Сначала определяем модель (Alex или Steve) по исходной прозрачности неиспользуемых зон
        bool isSlim = IsAlexSkin(pixels64);

        // В Minecraft базовый слой всегда непрозрачен (только используемые полигоны)
        ForceOpaqueBaseLayer(pixels64, isSlim);

        // Очищаем неиспользуемые области текстуры (водяные знаки, цифры авторов и т.д.)
        ClearUnusedAreas(pixels64, isSlim);

        // КЛЮЧЕВОЙ МОМЕНТ: Апскейл до 1024×1024 методом Nearest-Neighbor (без размытия).
        // Это полностью убирает мыло в WPF 3D (Direct3D интерполирует 16×16 блоки без потери чёткости).
        const int scale = 16;
        int dstSize = 64 * scale;
        byte[] upscaled = new byte[dstSize * dstSize * 4];

        for (int y = 0; y < dstSize; y++)
        {
            int sy = y / scale;
            int sRow = sy * 64 * 4;
            int dRow = y * dstSize * 4;

            for (int x = 0; x < dstSize; x++)
            {
                int sx = x / scale;
                int sIdx = sRow + sx * 4;
                int dIdx = dRow + x * 4;

                upscaled[dIdx + 0] = pixels64[sIdx + 0];
                upscaled[dIdx + 1] = pixels64[sIdx + 1];
                upscaled[dIdx + 2] = pixels64[sIdx + 2];
                upscaled[dIdx + 3] = pixels64[sIdx + 3];
            }
        }

        var writeable = new WriteableBitmap(dstSize, dstSize, 96, 96, PixelFormats.Bgra32, null);
        writeable.WritePixels(new Int32Rect(0, 0, dstSize, dstSize), upscaled, dstSize * 4, 0);
        writeable.Freeze();

        return (writeable, pixels64, isSlim);
    }

    private static void ForceOpaqueBaseLayer(byte[] p, bool isSlim)
    {
        void MakeOpaque(int x0, int y0, int w, int h)
        {
            for (int y = y0; y < y0 + h; y++)
            {
                for (int x = x0; x < x0 + w; x++)
                {
                    p[(y * 64 + x) * 4 + 3] = 255;
                }
            }
        }

        MakeOpaque(0, 0, 32, 16);   // Head
        MakeOpaque(16, 16, 24, 16); // Torso
        MakeOpaque(0, 16, 16, 16);  // Right Leg
        MakeOpaque(16, 48, 16, 16); // Left Leg

        if (isSlim)
        {
            // Alex (3px arm):
            MakeOpaque(44, 16, 6, 4);   // Right Arm Top/Bottom
            MakeOpaque(40, 20, 14, 12); // Right Arm Sides
            MakeOpaque(36, 48, 6, 4);   // Left Arm Top/Bottom
            MakeOpaque(32, 52, 14, 12); // Left Arm Sides
        }
        else
        {
            // Steve (4px arm):
            MakeOpaque(40, 16, 16, 16); // Right Arm
            MakeOpaque(32, 48, 16, 16); // Left Arm
        }
    }

    private static void ClearUnusedAreas(byte[] p, bool isSlim)
    {
        void Clear(int x0, int y0, int w, int h)
        {
            for (int y = y0; y < y0 + h; y++)
            {
                for (int x = x0; x < x0 + w; x++)
                {
                    p[(y * 64 + x) * 4 + 3] = 0;
                }
            }
        }

        // Неиспользуемые углы шляпы (в скинах часто содержат тестовые цифры или мусор)
        Clear(0, 0, 8, 8);
        Clear(24, 0, 8, 8);
        Clear(32, 0, 8, 8);
        Clear(56, 0, 8, 8);

        // Правая часть полосы рукавов (в скинах там часто авторские надписи/водяные знаки вроде "Alf.ART")
        Clear(56, 16, 8, 32);

        if (isSlim)
        {
            // Зоны, не используемые моделью Alex
            Clear(50, 16, 6, 4);   // Right Arm верхний правый остаток
            Clear(54, 20, 2, 12);  // Right Arm боковой остаток
            Clear(50, 32, 6, 4);   // Right Sleeve верхний остаток
            Clear(54, 36, 2, 12);  // Right Sleeve боковой остаток
            Clear(42, 48, 6, 4);   // Left Arm верхний остаток
            Clear(46, 52, 2, 12);  // Left Arm боковой остаток
            Clear(58, 48, 6, 4);   // Left Sleeve верхний остаток
            Clear(62, 52, 2, 12);  // Left Sleeve боковой остаток
        }
    }

    private static bool IsAlexSkin(byte[] p)
    {
        bool HasTransparency(int x0, int y0, int w, int h)
        {
            for (int y = y0; y < y0 + h; y++)
            {
                for (int x = x0; x < x0 + w; x++)
                {
                    if (p[(y * 64 + x) * 4 + 3] <= 10) return true;
                }
            }
            return false;
        }

        bool IsAreaColor(int x0, int y0, int w, int h, byte r, byte g, byte b)
        {
            for (int y = y0; y < y0 + h; y++)
            {
                for (int x = x0; x < x0 + w; x++)
                {
                    int idx = (y * 64 + x) * 4;
                    if (p[idx + 3] <= 10 || p[idx + 0] != b || p[idx + 1] != g || p[idx + 2] != r)
                        return false;
                }
            }
            return true;
        }

        // В официальном формате Minecraft Alex (3px arm) не использует 4 области:
        // Правая рука: (50, 16, 2, 4) и (54, 20, 2, 12)
        // Левая рука:  (42, 48, 2, 4) и (46, 52, 2, 12)
        bool isTransparent =
            HasTransparency(50, 16, 2, 4) ||
            HasTransparency(54, 20, 2, 12) ||
            HasTransparency(42, 48, 2, 4) ||
            HasTransparency(46, 52, 2, 12);

        if (isTransparent) return true;

        bool isBlack =
            IsAreaColor(50, 16, 2, 4, 0, 0, 0) &&
            IsAreaColor(54, 20, 2, 12, 0, 0, 0) &&
            IsAreaColor(42, 48, 2, 4, 0, 0, 0) &&
            IsAreaColor(46, 52, 2, 12, 0, 0, 0);

        if (isBlack) return true;

        bool isWhite =
            IsAreaColor(50, 16, 2, 4, 255, 255, 255) &&
            IsAreaColor(54, 20, 2, 12, 255, 255, 255) &&
            IsAreaColor(42, 48, 2, 4, 255, 255, 255) &&
            IsAreaColor(46, 52, 2, 12, 255, 255, 255);

        return isWhite;
    }

    private static void CopyMirrored(byte[] p, int su, int sv, int w, int h, int d, int du, int dv)
    {
        void CopyR(int sx, int sy, int dx, int dy, int cw, int ch, bool fx)
        {
            for (int y = 0; y < ch; y++)
            {
                for (int x = 0; x < cw; x++)
                {
                    int srcX = fx ? (sx + cw - 1 - x) : (sx + x);
                    int sIdx = ((sy + y) * 64 + srcX) * 4;
                    int dIdx = ((dy + y) * 64 + (dx + x)) * 4;
                    p[dIdx + 0] = p[sIdx + 0];
                    p[dIdx + 1] = p[sIdx + 1];
                    p[dIdx + 2] = p[sIdx + 2];
                    p[dIdx + 3] = p[sIdx + 3];
                }
            }
        }

        CopyR(su + d, sv, du + d, dv, w, d, true);
        CopyR(su + d + w, sv, du + d + w, dv, w, d, true);
        CopyR(su, sv + d, du + d + w, dv + d, d, h, true);
        CopyR(su + d, sv + d, du + d, dv + d, w, h, true);
        CopyR(su + d + w, sv + d, du, dv + d, d, h, true);
        CopyR(su + d + w + d, sv + d, du + d + w + d, dv + d, w, h, true);
    }

    /// <summary>
    /// Создает пиксель-арт аватар головы (32x32) из скина (накладывая слой шапки/волос).
    /// </summary>
    public static BitmapSource CreateHeadAvatar(string? skinPngPath)
    {
        byte[]? raw64 = null;
        if (!string.IsNullOrWhiteSpace(skinPngPath) && File.Exists(skinPngPath))
        {
            try
            {
                var (_, raw, _) = LoadAndProcessSkin(skinPngPath);
                raw64 = raw;
            }
            catch { }
        }

        const int scale = 4;
        const int size = 8 * scale; // 32x32
        var pixels = new byte[size * size * 4];

        for (int hy = 0; hy < 8; hy++)
        {
            for (int hx = 0; hx < 8; hx++)
            {
                byte r, g, b, a;
                if (raw64 != null)
                {
                    int headIdx = ((8 + hy) * 64 + (8 + hx)) * 4;
                    r = raw64[headIdx];
                    g = raw64[headIdx + 1];
                    b = raw64[headIdx + 2];
                    a = raw64[headIdx + 3];

                    int hatIdx = ((8 + hy) * 64 + (40 + hx)) * 4;
                    byte hatA = raw64[hatIdx + 3];
                    if (hatA > 20)
                    {
                        float alpha = hatA / 255.0f;
                        r = (byte)(raw64[hatIdx] * alpha + r * (1 - alpha));
                        g = (byte)(raw64[hatIdx + 1] * alpha + g * (1 - alpha));
                        b = (byte)(raw64[hatIdx + 2] * alpha + b * (1 - alpha));
                        a = 255;
                    }
                }
                else
                {
                    bool isHair = hy < 3 || (hy == 3 && (hx < 1 || hx > 6));
                    r = isHair ? (byte)74 : (byte)187;
                    g = isHair ? (byte)44 : (byte)133;
                    b = isHair ? (byte)23 : (byte)109;
                    a = 255;
                }

                for (int sy = 0; sy < scale; sy++)
                {
                    for (int sx = 0; sx < scale; sx++)
                    {
                        int targetIdx = ((hy * scale + sy) * size + (hx * scale + sx)) * 4;
                        pixels[targetIdx] = b;     // Blue (BGRA)
                        pixels[targetIdx + 1] = g; // Green
                        pixels[targetIdx + 2] = r; // Red
                        pixels[targetIdx + 3] = a; // Alpha
                    }
                }
            }
        }

        var bmp = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
        bmp.WritePixels(new Int32Rect(0, 0, size, size), pixels, size * 4, 0);
        bmp.Freeze();
        return bmp;
    }
}

