using Newtonsoft.Json;
using RhythmBase.Global.Events;
using RhythmBase.RhythmDoctor.Utils;
using System.Numerics;

namespace RhythmBase.RhythmDoctor.Events
{
	public class Particle : MacroEvent<RDAnimation>
	{
		private readonly Func<Vector3, BaseDecorationAction> action;
		private readonly ParticleGenerator generator;
		public RDAnimation Animation
		{
			get => Data;
			set => Data = value;
		}
		internal Particle(ParticleGenerator generator, Func<Vector3, BaseDecorationAction> action)
		{
			this.generator = generator;
			this.action = action;
			Data = new();
		}
		public override IEnumerable<BaseEvent> GenerateEvents()
		{
			foreach (var particle in generator.particles)
			{
				var deco = particle.Deco;
				BaseDecorationAction decoact = action(particle.Position);
				var d = Data.Randomized(); 
				decoact.Beat = d.RandomizedTime(Beat);
				if (decoact is IDurationEvent e)
					e.Duration = d.RandomizedDuration();
				if (decoact is IEaseEvent e2)
					e2.Ease = d.Type;
				yield return SetParent(decoact, deco);
			}
		}
	}
}