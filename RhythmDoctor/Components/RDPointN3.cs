using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhythmBase.RhythmDoctor.Components
{
	public struct RDSizeN3 : IRDVortex<RDSizeN3, RDSizeN3, float>
	{
		public float Width { get; set; }
		public float Height { get; set; }
		public float Depth { get; set; }
		public readonly bool Equals(RDSizeN3 other) => Width == other.Width && Height == other.Height && Depth == other.Depth;

		public static RDSizeN3 operator +(RDSizeN3 left, RDSizeN3 right) => new RDSizeN3
		{
			Width = left.Width + right.Width,
			Height = left.Height + right.Height,
			Depth = left.Depth + right.Depth
		};

		public static RDSizeN3 operator -(RDSizeN3 left, RDSizeN3 right) => new RDSizeN3
		{
			Width = left.Width - right.Width,
			Height = left.Height - right.Height,
			Depth = left.Depth - right.Depth
		};

		public static RDSizeN3 operator *(RDSizeN3 left, float right) => new RDSizeN3
		{
			Width = left.Width * right,
			Height = left.Height * right,
			Depth = left.Depth * right
		};

		public static RDSizeN3 operator /(RDSizeN3 left, float right) => new RDSizeN3
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
	public struct RDPointN3(float x, float y, float z) : IRDVortex<RDPointN3, RDSizeN3, float>
	{
		public float X { get; set; } = x;
		public float Y { get; set; } = y;
		public float Z { get; set; } = z;

		public readonly bool Equals(RDPointN3 other) => X == other.X && Y == other.Y && Z == other.Z;

		public static RDPointN3 operator +(RDPointN3 left, RDSizeN3 right) => new RDPointN3
		{
			X = left.X + right.Width,
			Y = left.Y + right.Height,
			Z = left.Z + right.Depth
		};

		public static RDPointN3 operator -(RDPointN3 left, RDSizeN3 right) => new RDPointN3
		{
			X = left.X - right.Width,
			Y = left.Y - right.Height,
			Z = left.Z - right.Depth
		};

		public static RDPointN3 operator *(RDPointN3 left, float right) => new RDPointN3
		{
			X = left.X * right,
			Y = left.Y * right,
			Z = left.Z * right
		};

		public static RDPointN3 operator /(RDPointN3 left, float right) => new RDPointN3
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
