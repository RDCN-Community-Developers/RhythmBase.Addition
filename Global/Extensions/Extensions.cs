using RhythmBase.Global.Components.Vector;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RhythmBase.Global.Extensions;

public static class Extensions
{
	public static char UppercasePrefixChar { get; set; } = '^';
	private static StringBuilder sb = new();
	public static string WithUppercasePrefix(this string word)
	{
		sb.Clear();
		for (int i = 0; i < word.Length; i++)
		{
			if (char.IsUpper(word[i]) || word[i] == UppercasePrefixChar)
				sb.Append(UppercasePrefixChar);
			sb.Append(word[i]);
		}
		return sb.ToString();
	}
	public static string WithUppercasePrefix(this char c) => char.IsUpper(c) ? $"{UppercasePrefixChar}{c}" : c.ToString();
	public static string WithoutUppercasePrefix(this string word)
	{
		sb.Clear();
		for (int i = 0; i < word.Length; i++)
		{
			if (word[i] == UppercasePrefixChar && i + 1 < word.Length && char.IsUpper(word[i + 1]))
				continue;
			sb.Append(word[i]);
		}
		return sb.ToString();
	}
	public static SKPointI ToSKPointI(this PointNI p) => new(p.X, p.Y);
	public static SKPoint ToSKPointI(this PointN p) => new(p.X, p.Y);
	public static SKSizeI ToSKSizeI(this SizeNI p) => new(p.Width, p.Height);
	public static SKSize ToSKSizeI(this SizeN p) => new(p.Width, p.Height);
	public static Vector2 ToVector2(this PointNI p) => new(p.X, p.Y);
	public static Vector2 ToVector2(this PointN p) => new(p.X, p.Y);
	public static Vector2 ToVector2(this SizeN p) => new(p.Width, p.Height);
	public static Vector2 ToVector2(this SizeNI p) => new(p.Width, p.Height);
	public static PointNI ToPointNI(this SKPointI p) => new(p.X, p.Y);
	public static PointN ToPointNI(this SKPoint p) => new(p.X, p.Y);
	public static SizeNI ToSizeN(this SKSizeI p) => new(p.Width, p.Height);
	public static SizeN ToSizeN(this SKSize p) => new(p.Width, p.Height);
	public static PointN ToPointN(this Vector2 p) => new(p.X, p.Y);
	public static SizeN ToSizeN(this Vector2 p) => new(p.X, p.Y);
	public static SKRectI ToSKRectNI(this RectNI p) => new(p.Left, p.Top, p.Right, p.Bottom);
	public static SKRect ToSKRectN(this RectN p) => new(p.Left, p.Top, p.Right, p.Bottom);
	public static RectNI ToRectNI(this SKRectI p) => new(p.Left, p.Top, p.Right, p.Bottom);
	public static RectN ToRectN(this SKRect p) => new(p.Left, p.Top, p.Right, p.Bottom);
}
