using RhythmBase.Global.Components.Vector;
using static RhythmBase.RhythmDoctor.Extensions.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhythmBase.RhythmDoctor.Utils.Projection.Perspective;

public readonly struct Matrix4x4
{
    public readonly float M11, M12, M13, M14;
    public readonly float M21, M22, M23, M24;
    public readonly float M31, M32, M33, M34;
    public readonly float M41, M42, M43, M44;

    public Matrix4x4(
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

    public static Matrix4x4 LookAt(PointN3 eye, PointN3 target, PointN3 up)
    {
        var zAxis = (target - eye).Normalized();  // 前
        var xAxis = PointN3.Cross(up, zAxis).Normalized(); // 右
        var yAxis = PointN3.Cross(zAxis, xAxis); // 上

        return new Matrix4x4(
            xAxis.X, yAxis.X, zAxis.X, 0,
            xAxis.Y, yAxis.Y, zAxis.Y, 0,
            xAxis.Z, yAxis.Z, zAxis.Z, 0,
            -PointN3.Dot(xAxis, eye), -PointN3.Dot(yAxis, eye), -PointN3.Dot(zAxis, eye), 1
        );
    }

    public PointN3 Transform(PointN3 p)
    {
        // 假设w=1，不处理透视除法（在投影阶段处理）
        return new PointN3(
            M11 * p.X + M12 * p.Y + M13 * p.Z + M14,
            M21 * p.X + M22 * p.Y + M23 * p.Z + M24,
            M31 * p.X + M32 * p.Y + M33 * p.Z + M34
        );
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

    // 缓存的视图矩阵（每次参数变化时重新计算）
    private Matrix4x4? _cachedViewMatrix;
    private (PointN3 pos, float y, float p, float r) _cachedParams;

    /// <summary>获取视图矩阵（世界空间→相机空间）</summary>
    public Matrix4x4 GetViewMatrix()
    {
        var currentParams = (Position, Yaw, Pitch, Roll);
        if (_cachedViewMatrix.HasValue && _cachedParams == currentParams)
            return _cachedViewMatrix.Value;

        // 构建旋转矩阵（先计算相机朝向的基向量）
        var (sinY, cosY) = (MathF.Sin(Yaw * MathF.PI / 180), MathF.Cos(Yaw * MathF.PI / 180));
        var (sinP, cosP) = (MathF.Sin(Pitch * MathF.PI / 180), MathF.Cos(Pitch * MathF.PI / 180));
        var (sinR, cosR) = (MathF.Sin(Roll * MathF.PI / 180), MathF.Cos(Roll * MathF.PI / 180));

        // 相机坐标系的基向量（相机看向 -Z 方向）
        // 前向量（相机看向的方向）
        PointN3 forward = new(
            sinY * cosP,
            sinP,
            -cosY * cosP
        );

        // 右向量
        PointN3 right = new(
            cosY * cosR + sinY * sinP * sinR,
            -cosP * sinR,
            sinY * cosR - cosY * sinP * sinR
        );

        // 上向量
        PointN3 up = PointN3.Cross(forward, right);

        _cachedViewMatrix = Matrix4x4.LookAt(Position, Position + forward, up);
        _cachedParams = currentParams;
        return _cachedViewMatrix.Value;
    }

    /// <summary>获取投影矩阵（相机空间→裁剪空间）</summary>
    public Matrix4x4 GetProjectionMatrix()
    {
        float fovRad = Fov * MathF.PI / 180;
        float f = 1f / MathF.Tan(fovRad / 2);

        return new Matrix4x4(
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

    /// <summary>
    /// 标准透视投影（支持一点/两点/三点，由相机旋转自动决定）
    /// </summary>
    public static ProjectionResult StandardPerspective(PointN3 point, Camera camera)
    {
        // 1. 世界坐标 → 相机坐标（视图变换）
        var viewMatrix = camera.GetViewMatrix();
        var cameraPoint = viewMatrix.Transform(point);

        // 2. 相机坐标 → 裁剪坐标（投影变换）
        // 简化透视除法：假设使用标准透视投影矩阵

        float distance = cameraPoint.Length; // 到相机的实际距离

        // 如果点在相机后方（Z>0是前方，这里相机看向-Z，所以相机空间Z<0是前方）
        // 注意：不同坐标系约定可能不同，这里假设相机空间Z<0是前方
        bool inFront = cameraPoint.Z < -camera.NearPlane;

        if (!inFront)
            return new(PixelCenter, distance, false); // 返回中心点但标记为后方

        // 透视除法：x' = x / -z, y' = y / -z （标准透视投影）
        // 使用 -z 因为相机看向 -Z 方向，远处Z更负
        float perspectiveScale = camera.Fov / 60f; // 归一化FOV影响
        float invZ = -1f / cameraPoint.Z;

        float screenX = cameraPoint.X * invZ * perspectiveScale;
        float screenY = cameraPoint.Y * invZ * perspectiveScale;

        // 3. 裁剪空间 → 屏幕坐标（像素单位，左上0,0）
        // 考虑画布16:9比例，需要将X进行修正
        float aspectCorrectedX = screenX / camera.AspectRatio;

        // 映射到 0-1 范围再转像素
        float normalizedX = aspectCorrectedX * 0.5f + 0.5f;
        float normalizedY = screenY * 0.5f + 0.5f;

        return new(NormalizedToPixel(normalizedX, normalizedY), distance, true);
    }

    /// <summary>
    /// 强制一点透视（无视相机旋转，始终正对XY平面）
    /// </summary>
    public static ProjectionResult OnePoint(PointN3 point, Camera camera)
    {
        // 相对相机位置
        float dx = point.X - camera.Position.X;
        float dy = point.Y - camera.Position.Y;
        float dz = point.Z - camera.Position.Z;

        float distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

        // 一点透视：Z轴深度决定缩放，XY直接映射
        if (dz >= -camera.NearPlane) // 在相机后方或太近
            return new(PixelCenter, distance, false);

        float scale = -camera.Fov / (60f * dz); // FOV归一化后的缩放

        // 直接映射XY，考虑画布比例
        float normalizedX = dx * scale / camera.AspectRatio + 0.5f;
        float normalizedY = dy * scale + 0.5f;

        return new(NormalizedToPixel(normalizedX, normalizedY), distance, true);
    }

    /// <summary>
    /// 强制两点透视（保持垂直线垂直，水平线汇聚于两个灭点）
    /// </summary>
    public static ProjectionResult TwoPoint(PointN3 point, Camera camera)
    {
        // 两点透视：绕Y轴旋转（Yaw）有效，Pitch和Roll强制为0
        var (sinY, cosY) = (MathF.Sin(camera.Yaw * MathF.PI / 180),
                           MathF.Cos(camera.Yaw * MathF.PI / 180));

        // 相对位置
        float dx = point.X - camera.Position.X;
        float dy = point.Y - camera.Position.Y;
        float dz = point.Z - camera.Position.Z;

        // 应用Yaw旋转（绕Y轴）
        float rotX = dx * cosY - dz * sinY;
        float rotZ = dx * sinY + dz * cosY;

        float distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

        if (rotZ >= -camera.NearPlane)
            return new(PixelCenter, distance, false);

        float scale = -camera.Fov / (60f * rotZ);

        // Y保持垂直（不旋转），X根据深度透视
        float normalizedX = rotX * scale / camera.AspectRatio + 0.5f;
        float normalizedY = dy * scale + 0.5f; // Y直接映射，保持垂直

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
    /// 鱼眼效果投影（非线性投影，视野更广但有畸变）
    /// </summary>
    public static ProjectionResult FishEye(PointN3 point, Camera camera)
    {
        // 先进行标准透视投影
        var standard = StandardPerspective(point, camera);
        if (!standard.InFront) return standard;

        // 将像素坐标转换到 -1 到 1 范围
        var (nx, ny) = PixelToNormalized(standard.ScreenPoint);
        float x = (nx - 0.5f) * 2;
        float y = (ny - 0.5f) * 2;

        // 计算极坐标
        float r = MathF.Sqrt(x * x + y * y);
        float theta = MathF.Atan2(y, x);

        // 鱼眼畸变：r' = 2 * sin(r * π/2) / π （等距鱼眼）
        // 或使用更强烈的 r' = r^0.5 （你原代码的效果）
        float fishEyeR = MathF.Sqrt(r); // 你的原始效果

        // 限制在合理范围内
        fishEyeR = MathF.Min(fishEyeR, 1.5f);

        // 转换回笛卡尔坐标
        float newX = fishEyeR * MathF.Cos(theta);
        float newY = fishEyeR * MathF.Sin(theta);

        // 映射回像素坐标
        return new(
            NormalizedToPixel(newX * 0.5f + 0.5f, newY * 0.5f + 0.5f),
            standard.Distance,
            standard.InFront
        );
    }

    /// <summary>
    /// 等轴透视（无透视效果，平行投影，用于对比参考）
    /// </summary>
    public static ProjectionResult Isometric(PointN3 point, Camera camera)
    {
        // 等轴透视：无视Z深度，所有物体同等大小
        // 使用特定角度：约30度俯角，45度水平角

        float angleX = 35.264f * MathF.PI / 180; // 垂直倾斜
        float angleY = 45f * MathF.PI / 180;     // 水平旋转

        float dx = point.X - camera.Position.X;
        float dy = point.Y - camera.Position.Y;
        float dz = point.Z - camera.Position.Z;

        // 等轴投影公式
        float isoX = (dx - dz) * MathF.Cos(angleY);
        float isoY = dy + (dx + dz) * MathF.Sin(angleX);

        // 归一化到屏幕（需要缩放因子）
        float scale = 0.01f * camera.Fov / 60f;

        float normalizedX = isoX * scale / camera.AspectRatio + 0.5f;
        float normalizedY = isoY * scale + 0.5f;

        // 距离用于排序（虽然无透视，但仍需Z排序）
        float distance = dy; // 使用Y作为深度线索

        return new(NormalizedToPixel(normalizedX, normalizedY), distance, true);
    }

    /// <summary>
    /// 全景/小星球效果（极坐标投影）
    /// </summary>
    public static ProjectionResult LittlePlanet(PointN3 point, Camera camera)
    {
        var standard = StandardPerspective(point, camera);
        if (!standard.InFront) return standard;

        var (nx, ny) = PixelToNormalized(standard.ScreenPoint);
        float x = (nx - 0.5f) * 2;
        float y = (ny - 0.5f) * 2;

        float r = MathF.Sqrt(x * x + y * y);
        float theta = MathF.Atan2(y, x);

        // 小星球效果：将平面映射到球面
        float newR = 2f / (1 + r); // 反比映射

        return new(
            NormalizedToPixel(newR * MathF.Cos(theta) * 0.5f + 0.5f,
                              newR * MathF.Sin(theta) * 0.5f + 0.5f),
            standard.Distance,
            standard.InFront
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