using System.Diagnostics.CodeAnalysis;

namespace RhythmBase.Global.Components.Vector
{
    public struct RDSizeN3 : IRDVector<RDSizeN3, RDSizeN3, float>
	{
		public float Width { get; set; }
		public float Height { get; set; }
		public float Depth { get; set; }
		public readonly bool Equals(RDSizeN3 other) => Width == other.Width && Height == other.Height && Depth == other.Depth;
		public static RDSizeN3 operator +(RDSizeN3 left, RDSizeN3 right) => new()
        {
			Width = left.Width + right.Width,
			Height = left.Height + right.Height,
			Depth = left.Depth + right.Depth
		};
		public static RDSizeN3 operator -(RDSizeN3 left, RDSizeN3 right) => new()
        {
			Width = left.Width - right.Width,
			Height = left.Height - right.Height,
			Depth = left.Depth - right.Depth
		};
		public static RDSizeN3 operator *(RDSizeN3 left, float right) => new()
        {
			Width = left.Width * right,
			Height = left.Height * right,
			Depth = left.Depth * right
		};
		public static RDSizeN3 operator /(RDSizeN3 left, float right) => new()
        {
			Width = left.Width / right,
			Height = left.Height / right,
			Depth = left.Depth / right
		};
		public static bool operator ==(RDSizeN3 left, RDSizeN3 right) => left.Equals(right);
		public static bool operator !=(RDSizeN3 left, RDSizeN3 right) => !left.Equals(right);
		public override readonly int GetHashCode() => HashCode.Combine(Width, Height, Depth);
		public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is RDSizeN3 other && Equals(other);
	}
}
