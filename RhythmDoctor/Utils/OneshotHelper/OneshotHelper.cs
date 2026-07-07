using RhythmBase.RhythmDoctor.Components;
namespace RhythmBase.RhythmDoctor.Utils.OneshotHelper
{
	public class NurseSayConfig
	{
		public SayReadyGetSetGoVoiceSource VoiceSource { get; set; } = SayReadyGetSetGoVoiceSource.Nurse;
	}
	public record struct OneshotPulseHit()
	{
		public float TickTime { get; set; } // 击打时间点
		public float Offset { get; set; } // 冰冻拍和灼热拍
		public float[] Pulses { get; set; } = []; // 脉冲时间点，相对于击打时间点的偏移
		public int Subdivision { get; set; } = 0; // 脉冲细分
		public bool Skip { get; set; } = false;
		public static OneshotPulseHit operator <<(OneshotPulseHit hit, float delta)
		{
			hit.TickTime -= delta;
			return hit;
		}
		public static OneshotPulseHit operator >>(OneshotPulseHit hit, float delta)
		{
			hit.TickTime += delta;
			return hit;
		}
		public OneshotPulseHit Copy()
		{
			return new OneshotPulseHit()
			{
				TickTime = TickTime,
				Offset = Offset,
				Pulses = [.. Pulses],
				Skip = Skip
			};
		}
	}
	public class OneshotHitPattern
	{
		private readonly OneshotPulseHit[] _template;
		private readonly SortedList<int, OneshotPulseHit[]> _cache = [];
		public float Length { get; private init; }
		/// <summary>
		/// Gets or sets the <see cref="OneshotPulseHit"/> at the specified loop and index.
		/// </summary>
		/// <remarks>Accessing this property may involve initializing the cache for the specified loop by creating a
		/// copy of the internal collection. This ensures that modifications to the cache do not affect the original
		/// data.</remarks>
		/// <param name="loop">The loop identifier. Must be non-negative.</param>
		/// <param name="index">The index within the loop. Must be non-negative.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="loop"/> or <paramref name="index"/> is negative.</exception>
		public OneshotPulseHit this[int loop, int index]
		{
			get
			{
				if (loop < 0)
					throw new ArgumentOutOfRangeException(nameof(loop), "Loop must be non-negative.");
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index), "Index must be non-negative.");
				if (_cache.TryGetValue(loop, out var cached))
					return cached[index];
				cached = [.. _template.Select(i => i.Copy())];
				_cache.Add(loop, cached);
				return cached[index];
			}
			set
			{
				if (loop < 0)
					throw new ArgumentOutOfRangeException(nameof(loop), "Loop must be non-negative.");
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index), "Index must be non-negative.");
				if (_cache.TryGetValue(loop, out var cached))
				{
					cached[index] = value;
				}
				else
				{
					cached = [.. _template.Select(i => i.Copy())];
					cached[index] = value;
					_cache.Add(loop, cached);
				}
			}
		}
		/// <summary>
		/// Creates a new instance of the <see cref="OneshotHitPattern"/> class with the specified pattern and length.
		/// </summary>
		/// <param name="pattern">
		/// A string representing the pattern to be used for the hit sequence. Cannot be null or empty.
		/// <para>The pattern uses the following symbols:</para>
		/// <para><c>-</c> : pulse</para>
		/// <para><c>.</c> : hit</para>
		/// <para><c>space</c> : ignore</para>
		/// Example:
		/// <code>
		/// "- .- -. "
		/// </code>
		/// </param>
		/// <param name="barLength">The duration of the pattern, in seconds. Must be a positive value.</param>
		/// <returns>A new <see cref="OneshotHitPattern"/> instance configured with the specified pattern and length.</returns>
		public OneshotHitPattern(string pattern, float barLength)
		{
			List<int> pulses = [];
			List<OneshotPulseHit> hits = [];
			for (int i = 0; i < pattern.Length; i++)
			{
				char c = pattern[i];
				switch (c)
				{
					case '-':
						pulses.Add(i);
						break;
					case '.':
						hits.Add(new()
						{
							TickTime = i * (barLength / pattern.Length),
							Pulses = [.. pulses.Select(p => (p - i) * (barLength / pattern.Length))]
						});
						break;
					case ' ':
						break;
					default:
						throw new ArgumentException("Pattern can only contain '-', '.', and ' ' characters.");
				}
			}
			_template = [.. hits];
			Length = barLength;
		}
	}
	public static class OneshotHelper
	{
		public static void AddOneshotHitPattern(
			this Level level,
			Row e,
			TickTime start,
			OneshotHitPattern pattern,
			bool addNurseSay = true)
		{
			if(e.RowType != RowType.Oneshot)
				throw new InvalidOperationException("Can only add oneshot hit patterns to oneshot rows.");
			start = new(level.Calculator, start);
			
		}
	}
}
