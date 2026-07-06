using System.Diagnostics.CodeAnalysis;

namespace RhythmBase.Global.Components.Vector
{
	public struct PointN3(float x, float y, float z) : IVector<PointN3, SizeN3, float>
	{
		public float X { get; set; } = x;
		public float Y { get; set; } = y;
		public float Z { get; set; } = z;
		public readonly float LengthSquared => X * X + Y * Y + Z * Z;
        public readonly float Length => float.Sqrt(LengthSquared); 
		public PointN3 Normalized()
        {
            var len = Length;
            return len < 0.0001f ? new PointN3(0, 0, 1) : new(X / len, Y / len, Z / len);
        }
        public readonly bool Equals(PointN3 other) => X == other.X && Y == other.Y && Z == other.Z;
		public static PointN3 operator +(PointN3 left, SizeN3 right) => new()
        {
			X = left.X + right.Width,
			Y = left.Y + right.Height,
			Z = left.Z + right.Depth
		};
		public static PointN3 operator +(PointN3 left, PointN3 right) => new()
        {
			X = left.X + right.X,
			Y = left.Y + right.Y,
			Z = left.Z + right.Z
		};
		public static PointN3 operator -(PointN3 left, SizeN3 right) => new()
        {
			X = left.X - right.Width,
			Y = left.Y - right.Height,
			Z = left.Z - right.Depth
		};
		public static PointN3 operator -(PointN3 left, PointN3 right) => new()
        {
			X = left.X - right.X,
			Y = left.Y - right.Y,
			Z = left.Z - right.Z
		};
		public static PointN3 operator *(PointN3 left, float right) => new()
        {
			X = left.X * right,
			Y = left.Y * right,
			Z = left.Z * right
		};
		public static PointN3 operator /(PointN3 left, float right) => new()
        {
			X = left.X / right,
			Y = left.Y / right,
			Z = left.Z / right
		};
		public static bool operator ==(PointN3 left, PointN3 right) => left.Equals(right);
		public static bool operator !=(PointN3 left, PointN3 right) => !left.Equals(right);
		public static PointN3 Cross(PointN3 a, PointN3 b) => new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );
        public static float Dot(PointN3 a, PointN3 b) =>
            a.X * b.X + a.Y * b.Y + a.Z * b.Z;
		public static implicit operator PointN3((float x, float y, float z) tuple) => new(tuple.x, tuple.y, tuple.z);
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);
		public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is PointN3 other && Equals(other);
	}
}
