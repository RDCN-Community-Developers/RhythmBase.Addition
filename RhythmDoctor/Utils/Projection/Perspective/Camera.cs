using RhythmBase.Global.Components.Vector;
using static RhythmBase.RhythmDoctor.Extensions.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhythmBase.RhythmDoctor.Utils.Projection.Perspective;

public readonly struct ProjectionMatrix4x4
{
    public readonly float M11, M12, M13, M14;
    public readonly float M21, M22, M23, M24;
    public readonly float M31, M32, M33, M34;
    public readonly float M41, M42, M43, M44;

    public ProjectionMatrix4x4(
        float m11, float m12, float m13, float m14,
        float m21, float m22, float m23, float m24,
        float m31, float m32, float m33, float m34,
        float m41, float m42, float m43, float m44)
    {
        M11 = m11; M12 = m12; M13 = m13; M14 = m14;
        M21 = m21; M22 = m22; M23 = m23; M24 = m24;
        M31 = m31; M32 = m32; M33 = m33; M34 = m34;
        M41 = m41; M42 = m42; M43 = m43; M44 = m44;
    }

    public static ProjectionMatrix4x4 LookAt(PointN3 eye, PointN3 target, PointN3 up)
    {
        var zAxis = (target - eye).Normalized();
        var xAxis = PointN3.Cross(up, zAxis).Normalized();
        var yAxis = PointN3.Cross(zAxis, xAxis);

        return new ProjectionMatrix4x4(
            xAxis.X, yAxis.X, zAxis.X, 0,
            xAxis.Y, yAxis.Y, zAxis.Y, 0,
            xAxis.Z, yAxis.Z, zAxis.Z, 0,
            -PointN3.Dot(xAxis, eye), -PointN3.Dot(yAxis, eye), -PointN3.Dot(zAxis, eye), 1
        );
    }

    public PointN3 Transform(PointN3 p)
    {
        return new PointN3(
            M11 * p.X + M12 * p.Y + M13 * p.Z + M14,
            M21 * p.X + M22 * p.Y + M23 * p.Z + M24,
            M31 * p.X + M32 * p.Y + M33 * p.Z + M34
        );
    }

    public (PointN3 point, float w) TransformProjective(PointN3 p)
    {
        float x = M11 * p.X + M12 * p.Y + M13 * p.Z + M14;
        float y = M21 * p.X + M22 * p.Y + M23 * p.Z + M24;
        float z = M31 * p.X + M32 * p.Y + M33 * p.Z + M34;
        float w = M41 * p.X + M42 * p.Y + M43 * p.Z + M44;
        return (new PointN3(x, y, z), w);
    }
}
public enum PerspectiveType
{
    OnePoint,   // 一点透视：正对一面
    TwoPoint,   // 两点透视：斜视，垂直线保持垂直
    ThreePoint, // 三点透视：斜视+仰视/俯视
    Custom      // 自定义（使用自定义投影矩阵）
}

// ==================== 投影结果结构体 ====================

/// <summary>点云投影结果，包含完整的投影信息和可见性状态</summary>
public readonly struct PointProjectionResult
{
    /// <summary>原始点索引</summary>
    public readonly int OriginalIndex;

    /// <summary>屏幕坐标（像素单位，左上0,0 右下 PixelateWidth×PixelateHeight）</summary>
    public readonly PointN ScreenPoint;

    /// <summary>到摄像机的距离（用于Z排序和雾效）</summary>
    public readonly float Distance;

    /// <summary>相机空间Z值（负值为前方，用于精确深度测试）</summary>
    public readonly float CameraSpaceZ;

    /// <summary>可见性状态标记</summary>
    public readonly VisibilityFlags Visibility;

    public PointProjectionResult(
        int originalIndex,
        PointN screenPoint,
        float distance,
        float cameraSpaceZ,
        VisibilityFlags visibility)
    {
        OriginalIndex = originalIndex;
        ScreenPoint = screenPoint;
        Distance = distance;
        CameraSpaceZ = cameraSpaceZ;
        Visibility = visibility;
    }

    /// <summary>是否在屏幕范围内（像素坐标）</summary>
    public bool IsOnScreen =>
        ScreenPoint.X >= 0f && ScreenPoint.X <= PixelateWidth &&
        ScreenPoint.Y >= 0f && ScreenPoint.Y <= PixelateHeight;

    /// <summary>是否完全可见（无裁剪）</summary>
    public bool IsFullyVisible => Visibility == VisibilityFlags.Visible;

    /// <summary>是否应被渲染（可见或部分可见）</summary>
    public bool ShouldRender =>
        (Visibility & VisibilityFlags.Culled) == 0 &&
        (Visibility & VisibilityFlags.BehindCamera) == 0;

    public override string ToString() =>
        $"[{OriginalIndex}] Screen:{ScreenPoint}, Dist:{Distance:F2}, Z:{CameraSpaceZ:F2}, {Visibility}";
}

/// <summary>可见性状态标记（位标志，可组合）</summary>
[Flags]
public enum VisibilityFlags
{
    None = 0,

    /// <summary>完全可见，无裁剪</summary>
    Visible = 1,

    /// <summary>在相机后方（Z > NearPlane）</summary>
    BehindCamera = 2,

    /// <summary>超出远裁剪面</summary>
    BeyondFarPlane = 4,

    /// <summary>超出近裁剪面</summary>
    BeforeNearPlane = 8,

    /// <summary>超出屏幕左边界</summary>
    OffScreenLeft = 16,

    /// <summary>超出屏幕右边界</summary>
    OffScreenRight = 32,

    /// <summary>超出屏幕下边界</summary>
    OffScreenBottom = 64,

    /// <summary>超出屏幕上边界</summary>
    OffScreenTop = 128,

    /// <summary>被距离阈值剔除</summary>
    DistanceCulled = 256,

    /// <summary>被自定义规则剔除</summary>
    CustomCulled = 512,

    /// <summary>任何裁剪状态（用于快速检查）</summary>
    Culled = BehindCamera | BeyondFarPlane | BeforeNearPlane |
             OffScreenLeft | OffScreenRight | OffScreenBottom | OffScreenTop |
             DistanceCulled | CustomCulled
}
public class Camera
{
    // 位置
    public PointN3 Position { get; set; }

    // 旋转（欧拉角，度）：Yaw(X轴旋转), Pitch(Y轴旋转), Roll(Z轴旋转)
    // 或者理解为：Yaw=水平旋转, Pitch=俯仰, Roll=翻滚
    public float Yaw { get; set; }   // 绕Y轴旋转（水平转向）
    public float Pitch { get; set; } // 绕X轴旋转（俯仰）
    public float Roll { get; set; }  // 绕Z轴旋转（翻滚）

    // 透视参数
    public float Fov { get; set; } = 60f;           // 垂直视野角度
    public float NearPlane { get; set; } = 0.1f;    // 近裁剪面
    public float FarPlane { get; set; } = 1000f;    // 远裁剪面

    // 画布比例（默认16:9）
    public float AspectRatio { get; set; } = 16f / 9f;

    // 可选：强制使用特定透视类型
    public PerspectiveType? ForcePerspective { get; set; }

    private ProjectionMatrix4x4? _cachedViewMatrix;
    private (PointN3 pos, float y, float p, float r) _cachedParams;

    /// <summary>获取视图矩阵（世界空间→相机空间，右手系，相机看向 -Z）</summary>
    public ProjectionMatrix4x4 GetViewMatrix()
    {
        var currentParams = (Position, Yaw, Pitch, Roll);
        if (_cachedViewMatrix.HasValue && _cachedParams == currentParams)
            return _cachedViewMatrix.Value;

        var (sinY, cosY) = (MathF.Sin(Yaw * MathF.PI / 180), MathF.Cos(Yaw * MathF.PI / 180));
        var (sinP, cosP) = (MathF.Sin(Pitch * MathF.PI / 180), MathF.Cos(Pitch * MathF.PI / 180));
        var (sinR, cosR) = (MathF.Sin(Roll * MathF.PI / 180), MathF.Cos(Roll * MathF.PI / 180));

        PointN3 forward = new(sinY * cosP, sinP, -cosY * cosP);
        PointN3 right = new(cosY * cosR + sinY * sinP * sinR, -cosP * sinR, sinY * cosR - cosY * sinP * sinR);
        PointN3 up = PointN3.Cross(forward, right);

        _cachedViewMatrix = ProjectionMatrix4x4.LookAt(Position, Position + forward, up);
        _cachedParams = currentParams;
        return _cachedViewMatrix.Value;
    }

    /// <summary>获取投影矩阵（相机空间→裁剪空间，右手系）</summary>
    public ProjectionMatrix4x4 GetProjectionMatrix()
    {
        float fovRad = Fov * MathF.PI / 180;
        float f = 1f / MathF.Tan(fovRad / 2);

        return new ProjectionMatrix4x4(
            f / AspectRatio, 0, 0, 0,
            0, f, 0, 0,
            0, 0, (FarPlane + NearPlane) / (NearPlane - FarPlane),
                 (2 * FarPlane * NearPlane) / (NearPlane - FarPlane),
            0, 0, -1, 0
        );
    }
}

// ==================== 批量投影器（封装版本） ====================
public static class PerspectiveProjection
{
    // 当前使用的投影方法（可替换）
    public static ProjectionMethod Current { get; set; } = StandardPerspective;

    private static readonly PointN PixelCenter = new(PixelateWidth / 2f, PixelateHeight / 2f);

    private static PointN NormalizedToPixel(float normalizedX, float normalizedY) => new(
        normalizedX * PixelateWidth,
        (1f - normalizedY) * PixelateHeight
    );

    private static (float nx, float ny) PixelToNormalized(PointN pixel) => (
        pixel.X / PixelateWidth,
        1f - pixel.Y / PixelateHeight
    );

    private static float FocalLength(float fovDeg) =>
        1f / MathF.Tan(fovDeg * MathF.PI / 360f);

    /// <summary>
    /// 标准透视投影（右手系，相机看向 -Z，支持一点/两点/三点，由相机旋转自动决定）
    /// </summary>
    public static ProjectionResult StandardPerspective(PointN3 point, Camera camera)
    {
        var viewMatrix = camera.GetViewMatrix();
        var cameraPoint = viewMatrix.Transform(point);

        float distance = cameraPoint.Length;

        if (cameraPoint.Z >= -camera.NearPlane)
            return new(PixelCenter, distance, false);

        float f = FocalLength(camera.Fov);
        float invZ = -1f / cameraPoint.Z;

        float screenX = cameraPoint.X * invZ * f;
        float screenY = cameraPoint.Y * invZ * f;

        float aspectCorrectedX = screenX / camera.AspectRatio;

        float normalizedX = aspectCorrectedX * 0.5f + 0.5f;
        float normalizedY = screenY * 0.5f + 0.5f;

        return new(NormalizedToPixel(normalizedX, normalizedY), distance, true);
    }

    /// <summary>
    /// 强制一点透视（无视相机旋转，始终正对XY平面，右手系）
    /// </summary>
    public static ProjectionResult OnePoint(PointN3 point, Camera camera)
    {
        float dx = point.X - camera.Position.X;
        float dy = point.Y - camera.Position.Y;
        float dz = point.Z - camera.Position.Z;

        float distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

        if (dz >= -camera.NearPlane)
            return new(PixelCenter, distance, false);

        float f = FocalLength(camera.Fov);
        float scale = f / -dz;

        float normalizedX = dx * scale / camera.AspectRatio + 0.5f;
        float normalizedY = dy * scale + 0.5f;

        return new(NormalizedToPixel(normalizedX, normalizedY), distance, true);
    }

    /// <summary>
    /// 强制两点透视（保持垂直线垂直，水平线汇聚于两个灭点，右手系）
    /// </summary>
    public static ProjectionResult TwoPoint(PointN3 point, Camera camera)
    {
        var (sinY, cosY) = (MathF.Sin(camera.Yaw * MathF.PI / 180),
                           MathF.Cos(camera.Yaw * MathF.PI / 180));

        float dx = point.X - camera.Position.X;
        float dy = point.Y - camera.Position.Y;
        float dz = point.Z - camera.Position.Z;

        float rotX = dx * cosY - dz * sinY;
        float rotZ = dx * sinY + dz * cosY;

        float distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

        if (rotZ >= -camera.NearPlane)
            return new(PixelCenter, distance, false);

        float f = FocalLength(camera.Fov);
        float scale = f / -rotZ;

        float normalizedX = rotX * scale / camera.AspectRatio + 0.5f;
        float normalizedY = dy * scale + 0.5f;

        return new(NormalizedToPixel(normalizedX, normalizedY), distance, true);
    }

    /// <summary>
    /// 强制三点透视（完整旋转，所有方向都有透视收敛）
    /// </summary>
    public static ProjectionResult ThreePoint(PointN3 point, Camera camera)
    {
        // 使用完整的视图矩阵（标准方法已经支持）
        // 但这里可以添加特定的三点透视艺术效果，比如灭点位置调整
        return StandardPerspective(point, camera);
    }

    /// <summary>
    /// 鱼眼效果投影（等距鱼眼模型 r' = f * θ）
    /// </summary>
    public static ProjectionResult FishEye(PointN3 point, Camera camera)
    {
        var standard = StandardPerspective(point, camera);
        if (!standard.InFront) return standard;

        var (nx, ny) = PixelToNormalized(standard.ScreenPoint);
        float x = (nx - 0.5f) * 2;
        float y = (ny - 0.5f) * 2;

        float r = MathF.Sqrt(x * x + y * y);
        if (r < 1e-6f) return standard;
        float theta = MathF.Atan2(y, x);

        float fishEyeR = MathF.Atan(r) / (MathF.PI / 2f);
        fishEyeR = MathF.Min(fishEyeR, 1.5f);

        float newX = fishEyeR * MathF.Cos(theta);
        float newY = fishEyeR * MathF.Sin(theta);

        return new(
            NormalizedToPixel(newX * 0.5f + 0.5f, newY * 0.5f + 0.5f),
            standard.Distance,
            true
        );
    }

    /// <summary>
    /// 等立体角鱼眼投影（r' = 2 * sin(θ/2)）
    /// </summary>
    public static ProjectionResult FishEyeEquisolid(PointN3 point, Camera camera)
    {
        var standard = StandardPerspective(point, camera);
        if (!standard.InFront) return standard;

        var (nx, ny) = PixelToNormalized(standard.ScreenPoint);
        float x = (nx - 0.5f) * 2;
        float y = (ny - 0.5f) * 2;

        float r = MathF.Sqrt(x * x + y * y);
        if (r < 1e-6f) return standard;
        float theta = MathF.Atan2(y, x);

        float fishEyeR = 2f * MathF.Sin(MathF.Atan(r) / 2f);
        fishEyeR = MathF.Min(fishEyeR, 1.5f);

        float newX = fishEyeR * MathF.Cos(theta);
        float newY = fishEyeR * MathF.Sin(theta);

        return new(
            NormalizedToPixel(newX * 0.5f + 0.5f, newY * 0.5f + 0.5f),
            standard.Distance,
            true
        );
    }

    /// <summary>
    /// 等轴透视（无透视效果，平行投影）
    /// </summary>
    public static ProjectionResult Isometric(PointN3 point, Camera camera)
    {
        float angleX = 35.264f * MathF.PI / 180;
        float angleY = 45f * MathF.PI / 180;

        float dx = point.X - camera.Position.X;
        float dy = point.Y - camera.Position.Y;
        float dz = point.Z - camera.Position.Z;

        float isoX = (dx - dz) * MathF.Cos(angleY);
        float isoY = dy + (dx + dz) * MathF.Sin(angleX);

        float scale = 0.01f * FocalLength(camera.Fov) / FocalLength(60f);

        float normalizedX = isoX * scale / camera.AspectRatio + 0.5f;
        float normalizedY = isoY * scale + 0.5f;

        float distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

        return new(NormalizedToPixel(normalizedX, normalizedY), distance, true);
    }

    /// <summary>
    /// 小星球效果（立体投影 r' = 2/(1+r)）
    /// </summary>
    public static ProjectionResult LittlePlanet(PointN3 point, Camera camera)
    {
        var standard = StandardPerspective(point, camera);
        if (!standard.InFront) return standard;

        var (nx, ny) = PixelToNormalized(standard.ScreenPoint);
        float x = (nx - 0.5f) * 2;
        float y = (ny - 0.5f) * 2;

        float r = MathF.Sqrt(x * x + y * y);
        if (r < 1e-6f) return standard;
        float theta = MathF.Atan2(y, x);

        float newR = 2f / (1f + r);

        return new(
            NormalizedToPixel(newR * MathF.Cos(theta) * 0.5f + 0.5f,
                              newR * MathF.Sin(theta) * 0.5f + 0.5f),
            standard.Distance,
            true
        );
    }
}
public class PointCloudProjector
{
    private readonly Camera _camera;
    private ProjectionMethod _projection;

    // 配置参数
    public float ScreenMargin { get; set; } = 0.1f;  // 屏幕外边界容差比例（相对于屏幕尺寸）
    public bool SortByDepth { get; set; } = true;     // 是否按深度排序
    public bool DescendingOrder { get; set; } = true; // true=远到近（画家算法），false=近到远

    public PointCloudProjector(Camera camera, ProjectionMethod? method = null)
    {
        _camera = camera;
        _projection = method ?? PerspectiveProjection.StandardPerspective;
    }

    /// <summary>切换投影方法</summary>
    public void SetProjectionMethod(ProjectionMethod method) => _projection = method;

    /// <summary>
    /// 批量投影点云，返回所有点的投影结果（包含可见性标记，不剔除）
    /// </summary>
    /// <param name="points">3D点数组</param>
    /// <param name="maxDistance">可选的最大距离阈值</param>
    /// <returns>投影结果数组，与输入点一一对应（索引相同）</returns>
    public PointProjectionResult[] ProjectAll(
        PointN3[] points,
        float? maxDistance = null)
    {
        var results = new PointProjectionResult[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            results[i] = ProjectSingle(points[i], i, maxDistance);
        }

        return results;
    }

    /// <summary>
    /// 投影单个点并计算可见性
    /// </summary>
    private PointProjectionResult ProjectSingle(
        PointN3 point,
        int index,
        float? maxDistance)
    {
        // 执行投影
        var proj = _projection(point, _camera);

        // 初始化可见性
        var visibility = VisibilityFlags.Visible;

        // 检查相机后方
        if (!proj.InFront)
        {
            visibility = VisibilityFlags.BehindCamera;
        }
        else
        {
            // 检查裁剪面
            if (proj.Distance > _camera.FarPlane)
                visibility |= VisibilityFlags.BeyondFarPlane;

            // 注意：CameraSpaceZ 需要从投影方法获取，这里简化处理
            // 实际应该在 ProjectionResult 中包含 CameraSpaceZ
            float cameraZ = -proj.Distance; // 简化估算

            if (cameraZ > -_camera.NearPlane)
                visibility |= VisibilityFlags.BeforeNearPlane;

            // 检查距离阈值
            if (maxDistance.HasValue && proj.Distance > maxDistance.Value)
                visibility |= VisibilityFlags.DistanceCulled;

            // 检查屏幕边界（像素坐标，考虑边距比例）
            float marginX = ScreenMargin * PixelateWidth;
            float marginY = ScreenMargin * PixelateHeight;
            float minX = -marginX, maxX = PixelateWidth + marginX;
            float minY = -marginY, maxY = PixelateHeight + marginY;

            if (proj.ScreenPoint.X < minX) visibility |= VisibilityFlags.OffScreenLeft;
            if (proj.ScreenPoint.X > maxX) visibility |= VisibilityFlags.OffScreenRight;
            if (proj.ScreenPoint.Y < minY) visibility |= VisibilityFlags.OffScreenBottom;
            if (proj.ScreenPoint.Y > maxY) visibility |= VisibilityFlags.OffScreenTop;

            // 如果有任何裁剪标记，移除 Visible
            if (visibility != VisibilityFlags.Visible)
                visibility &= ~VisibilityFlags.Visible;
        }

        return new PointProjectionResult(
            originalIndex: index,
            screenPoint: proj.ScreenPoint,
            distance: proj.Distance,
            cameraSpaceZ: proj.InFront ? -proj.Distance : proj.Distance,
            visibility: visibility
        );
    }

    /// <summary>
    /// 获取过滤后的可见点（用于实际渲染）
    /// </summary>
    public PointProjectionResult[] GetVisibleOnly(PointProjectionResult[] allResults)
    {
        int count = 0;
        foreach (var r in allResults)
            if (r.ShouldRender) count++;

        var filtered = new PointProjectionResult[count];
        int idx = 0;
        foreach (var r in allResults)
            if (r.ShouldRender)
                filtered[idx++] = r;

        return filtered;
    }

    /// <summary>
    /// 按深度排序结果（修改数组顺序）</summary>
    public void SortByDistance(PointProjectionResult[] results)
    {
        if (!SortByDepth) return;

        var comparer = DescendingOrder
            ? Comparer<PointProjectionResult>.Create((a, b) => b.Distance.CompareTo(a.Distance))
            : Comparer<PointProjectionResult>.Create((a, b) => a.Distance.CompareTo(b.Distance));

        Array.Sort(results, comparer);
    }

    /// <summary>
    /// 完整处理流程：投影+可选排序（返回全部结果）</summary>
    public PointProjectionResult[] Process(
        PointN3[] points,
        float? maxDistance = null,
        bool sort = true)
    {
        var results = ProjectAll(points, maxDistance);
        if (sort) SortByDistance(results);
        return results;
    }
}

// ==================== 扩展：带CameraSpaceZ的投影结果 ====================

/// <summary>增强版投影结果，包含相机空间坐标</summary>
public readonly struct ProjectionResultEx
{
    /// <summary>屏幕坐标（像素单位）</summary>
    public readonly PointN ScreenPoint;
    public readonly float Distance;
    public readonly float CameraSpaceZ;  // 相机空间Z（负值为前方）
    public readonly bool InFront;

    // 相机空间XY（用于精确裁剪）
    public readonly float CameraSpaceX;
    public readonly float CameraSpaceY;

    public ProjectionResultEx(
        PointN screen,
        float dist,
        float camZ,
        bool inFront,
        float camX = 0,
        float camY = 0)
    {
        ScreenPoint = screen;
        Distance = dist;
        CameraSpaceZ = camZ;
        InFront = inFront;
        CameraSpaceX = camX;
        CameraSpaceY = camY;
    }
}

/// <summary>增强版投影委托</summary>
public delegate ProjectionResultEx ProjectionMethodEx(PointN3 point, Camera camera);

// ==================== 兼容层：将新委托适配到旧委托 ====================

public static class ProjectionAdapter
{
    public static ProjectionMethod ToLegacy(ProjectionMethodEx modern) =>
        (point, cam) =>
        {
            var ex = modern(point, cam);
            return new ProjectionResult(ex.ScreenPoint, ex.Distance, ex.InFront);
        };
}

/// <summary>投影方法委托：将3D点映射到2D屏幕坐标并计算距离</summary>
public delegate ProjectionResult ProjectionMethod(PointN3 point, Camera camera);