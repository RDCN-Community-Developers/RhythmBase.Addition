//using Brc;
//using RhythmBase.Global.Components;
//using RhythmBase.RhythmDoctor.Assets;
//using RhythmBase.RhythmDoctor.Components;
//using RhythmBase.RhythmDoctor.Events;
//using RhythmBase.RhythmDoctor.Extensions;
//using SkiaSharp;

//namespace RhythmBase.RhythmDoctor.Utils
//{
//	public class LyricBuilder(Level level)
//	{
//		private Level _level = level;
//		public FloatingText[] AddLyrics(Brc.Brc lyric, FloatingText template)
//		{
//			List<FloatingText> floatingTexts = [];
//			TickTime linestart = _level.DefaultTickTime;

//			foreach (var sentence in lyric)
//			{
//				TickTime wordstart = linestart;
//				FloatingText? head = null;
//				string headString = "";
//				foreach (var line in sentence)
//				{
//					foreach (var word in line)
//					{
//						if (!string.IsNullOrEmpty(word.Word))
//						{
//							if (head == null)
//							{
//								head = template.Clone<FloatingText>() ?? new FloatingText();
//								head.TickTime = wordstart;
//								head.FadeOutRate = word.Duration;
//								_level.Add(head);
//								headString = word.Word;
//							}
//							else
//							{
//								var child = head.CreateAdvanceText(wordstart);
//								_level.Add(child);
//								child.FadeOutDuration = word.Duration;
//								headString += "/" + word.Word;
//							}
//						}
//						wordstart += word.Duration;
//					}
//				}
//				if (head != null)
//				{
//					head.Text = headString;
//					floatingTexts.Add(head);
//				}
//				linestart += linestart.CPB;
//			}
//			return [.. floatingTexts];
//		}
//		public void AddLyrics(Brc.Brc lyric, SKFont font, string filename,
//			SpriteSheetCanvasSettings settings,
//			Action<Func<char, DecorationPool>, Func<string, float, PointN[]>, TickTime, BrcLine> lineAction
//			)
//		{
//			HashSet<char> chars = [];

//			foreach (var sentence in lyric)
//				foreach (var line in sentence)
//					foreach (var word in line)
//						if (!string.IsNullOrEmpty(word.Word))
//							foreach (char c in word.Word)
//								chars.Add(c);

//			SpriteTextBuilder builder = new(font, filename, new string([.. chars]), settings);

//			Dictionary<string, DecorationPool> pools = [];
//			for (int i = 0; i < builder.Sprites.Length; i++)
//			{
//				RDSprite sprite = builder.Sprites[i];
//				pools[sprite.Name + "_" + i] = new DecorationPool(_level, sprite.Name + "_" + i);
//			}

//			TickTime sentencestart = _level.DefaultTickTime;
//			foreach (var sentence in lyric)
//			{
//				foreach (var line in sentence)
//				{
//					float off = 0;
//					foreach (var word in line)
//					{
//						if (string.IsNullOrEmpty(word.Word))
//							off += word.Duration;
//						else
//							break;
//					}
//					lineAction(c => pools[builder.SpriteNameOf(c)], builder.GetPositions, sentencestart + off, line);
//				}
//				sentencestart += _level.OfEvent<SetCrotchetsPerBar>().InRange(new RDRange(null, sentencestart)).LastOrDefault()?.CrotchetsPerBar ?? 8;
//			}
//		}
//	}
//}