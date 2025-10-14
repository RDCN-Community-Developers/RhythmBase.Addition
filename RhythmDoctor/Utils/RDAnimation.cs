using Newtonsoft.Json;
using RhythmBase.Global.Components.Easing;
using RhythmBase.RhythmDoctor.Components;

namespace RhythmBase.RhythmDoctor.Utils
{
	public struct RDAnimation()
	{
		private float _rTimeOff;
		private float _rDurOff;
		public enum ThresholdType
		{
			In,
			Out,
			InOut,
		}
		public EaseType Type { get; set; } = EaseType.Linear;
		public float Duration { get; set; } = 0;
		public float StartThreshold { get; set; } = 0;
		public ThresholdType StartThresholdType { get; set; } = ThresholdType.InOut;
		public float EndThreshold { get; set; } = 0;
		public ThresholdType EndThresholdType { get; set; } = ThresholdType.InOut;
		public readonly RDBeat RandomizedTime(RDBeat time) => time + _rTimeOff;
		public readonly float RandomizedDuration() => Duration + _rDurOff - _rTimeOff;
		[JsonIgnore]
		public Random Random { get; set; } = Random.Shared;
		public RDAnimation Randomized()
		{
			_rTimeOff = ((StartThreshold == 0) ? 0 :
			(StartThresholdType) switch
			{
				ThresholdType.InOut => ((Random.NextSingle() * 2) - 1) * StartThreshold,
				ThresholdType.In => Random.NextSingle() * -StartThreshold,
				ThresholdType.Out => Random.NextSingle() * StartThreshold,
				_ => 0,
			});
			_rDurOff = ((EndThreshold == 0) ? 0 :
			(EndThresholdType) switch
			{
				ThresholdType.InOut => ((Random.NextSingle() * 2) - 1) * EndThreshold,
				ThresholdType.In => Random.NextSingle() * -EndThreshold,
				ThresholdType.Out => Random.NextSingle() * EndThreshold,
				_ => 0,
			});
			return this;
		}
		public RDAnimation WithRandom(Random random)
		{
			return new RDAnimation()
			{
				Type = Type,
				Duration = Duration,
				StartThreshold = StartThreshold,
				StartThresholdType = StartThresholdType,
				EndThreshold = EndThreshold,
				EndThresholdType = EndThresholdType,
				Random = random
			};
		}
	}
}
