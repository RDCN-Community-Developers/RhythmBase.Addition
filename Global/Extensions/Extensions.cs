using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RhythmBase.Global.Extensions
{
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
				{
					continue;
				}
				sb.Append(word[i]);
			}
			return sb.ToString();
		}
	}
}
