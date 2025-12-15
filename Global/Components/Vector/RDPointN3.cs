using System.Diagnostics.CodeAnalysis;

namespace RhythmBase.Global.Components.Vector
{
	public struct RDPointN3(float x, float y, float z) : IRDVector<RDPointN3, RDSizeN3, float>
	{
		public float X { get; set; } = x;
		public float Y { get; set; } = y;
		public float Z { get; set; } = z;
		public readonly bool Equals(RDPointN3 other) => X == other.X && Y == other.Y && Z == other.Z;
		public static RDPointN3 operator +(RDPointN3 left, RDSizeN3 right) => new()
        {
			X = left.X + right.Width,
			Y = left.Y + right.Height,
			Z = left.Z + right.Depth
		};
		public static RDPointN3 operator -(RDPointN3 left, RDSizeN3 right) => new()
        {
			X = left.X - right.Width,
			Y = left.Y - right.Height,
			Z = left.Z - right.Depth
		};
		public static RDPointN3 operator *(RDPointN3 left, float right) => new()
        {
			X = left.X * right,
			Y = left.Y * right,
			Z = left.Z * right
		};
		public static RDPointN3 operator /(RDPointN3 left, float right) => new()
        {
			X = left.X / right,
			Y = left.Y / right,
			Z = left.Z / right
		};
		public static bool operator ==(RDPointN3 left, RDPointN3 right) => left.Equals(right);
		public static bool operator !=(RDPointN3 left, RDPointN3 right) => !left.Equals(right);
		public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);
		public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is RDPointN3 other && Equals(other);
	}
}
