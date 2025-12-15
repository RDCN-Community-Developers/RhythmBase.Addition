using RhythmBase.Global.Components.Vector;
using RhythmBase.Global.Events;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;
using System.Numerics;

namespace RhythmBase.RhythmDoctor.Utils
{
	/// <summary>
	/// Generates and manages particles for the level.
	/// </summary>
	public class ParticleGenerator
	{
		/// <summary>
		/// Represents a single particle with its decoration and position.
		/// </summary>
		internal class Particle
		{
			/// <summary>
			/// The decoration associated with the particle.
			/// </summary>
			public required Decoration Deco;
			/// <summary>
			/// The position of the particle in 3D space.
			/// </summary>
			public RDPointN3 Position;
		}
		private class ParticleAction
		{
			public RDBeat Beat;
			public RDAnimation Animation;
			public required Func<RDPointN3, BaseDecorationAction> Action;
			public bool Flushed = false;
		}
		private readonly RDLevel level;
		internal readonly List<Particle> particles = [];
		public readonly Random Random = new();
		private readonly List<ParticleAction> actions = [];

		/// <summary>
		/// Initializes a new instance of the <see cref="ParticleGenerator"/> class.
		/// </summary>
		/// <param name="level">The level to which the particles belong.</param>
		/// <param name="room">The room in which the particles are generated.</param>
		/// <param name="startDepth">The starting depth for the particles.</param>
		/// <param name="count">The number of particles to generate.</param>
		/// <param name="random">The random number generator to use.</param>
		public ParticleGenerator(RDLevel level, RDSingleRoom room, int startDepth, int count, Random random)
		{
			this.level = level;
			this.Random = random;
			particles = [.. Enumerable.Range(startDepth, count).Select(i => {
			var p = new Particle()
			{
				Deco = new Decoration()
				{
					Depth = i,
					Room = room,
					Visible = false,
				},
				Position = new RDPointN3(
					random.NextSingle(),
					random.NextSingle(),
					(i - startDepth) / (float)count)
			};
				level.Decorations.Add(p.Deco);
			return  p;
			})];
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParticleGenerator"/> class with a shared random generator.
		/// </summary>
		/// <param name="level">The level to which the particles belong.</param>
		/// <param name="room">The room in which the particles are generated.</param>
		/// <param name="startDepth">The starting depth for the particles.</param>
		/// <param name="count">The number of particles to generate.</param>
		public ParticleGenerator(RDLevel level, RDSingleRoom room, int startDepth, int count)
			: this(level, room, startDepth, count, Random.Shared) { }

		/// <summary>
		/// Randomizes the positions of all particles.
		/// </summary>
		public void Randomize()
		{
			for (int i = 0; i < particles.Count; i++)
			{
				particles[i].Position = new RDPointN3(
					Random.NextSingle(),
					Random.NextSingle(),
					i / (float)particles.Count);
			}
		}

		/// <summary>
		/// Adds an action to be applied to the particles.
		/// </summary>
		/// <param name="animation">The animation associated with the action.</param>
		/// <param name="action">A function that generates a decoration action based on a particle's position.</param>
		public void AddAction(RDBeat beat, RDAnimation animation, Func<RDPointN3, BaseDecorationAction> action)
		{
			actions.Add(new ParticleAction()
			{
				Beat = beat,
				Animation = animation.WithRandom(Random),
				Action = action
			});
		}

		/// <summary>
		/// Flushes all actions, applying them to the particles.
		/// </summary>
		public void Flush()
		{
			foreach (var action in actions)
			{
				if (action.Flushed) continue;
				action.Flushed = true;
				foreach (var particle in particles)
				{
					var deco = particle.Deco;
					BaseDecorationAction decoact = action.Action(particle.Position);
					deco.Add(decoact);
					decoact.Beat = action.Animation.RandomizedTime(action.Beat);
					if (decoact is IDurationEvent e)
						e.Duration = action.Animation.RandomizedDuration();
					if (decoact is IEaseEvent e2)
						e2.Ease = action.Animation.Type;
					level.Decorations.Add(deco);
				}
			}
		}
		public Events.Particle GetParticle(Func<RDPointN3, BaseDecorationAction> action) => new(this, action);
	}
}
