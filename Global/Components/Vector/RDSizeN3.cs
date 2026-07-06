using System.Diagnostics.CodeAnalysis;

namespace RhythmBase.Global.Components.Vector
{
    public struct SizeN3 : IVector<SizeN3, SizeN3, float>
	{
		public float Width { get; set; }
		public float Height { get; set; }
		public float Depth { get; set; }
		public readonly bool Equals(SizeN3 other) => Width == other.Width && Height == other.Height && Depth == other.Depth;
		public static SizeN3 operator +(SizeN3 left, SizeN3 right) => new()
        {
			Width = left.Width + right.Width,
			Height = left.Height + right.Height,
			Depth = left.Depth + right.Depth
		};
		public static SizeN3 operator -(SizeN3 left, SizeN3 right) => new()
        {
			Width = left.Width - right.Width,
			Height = left.Height - right.Height,
			Depth = left.Depth - right.Depth
		};
		public static SizeN3 operator *(SizeN3 left, float right) => new()
        {
			Width = left.Width * right,
			Height = left.Height * right,
			Depth = left.Depth * right
		};
		public static SizeN3 operator /(SizeN3 left, float right) => new()
        {
			Width = left.Width / right,
			Height = left.Height / right,
			Depth = left.Depth / right
		};
		public static bool operator ==(SizeN3 left, SizeN3 right) => left.Equals(right);
		public static bool operator !=(SizeN3 left, SizeN3 right) => !left.Equals(right);
		public override readonly int GetHashCode() => HashCode.Combine(Width, Height, Depth);
		public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is SizeN3 other && Equals(other);
	}
}
