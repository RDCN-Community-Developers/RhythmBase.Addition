using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;

namespace RhythmBase.RhythmDoctor.Extensions
{
	public static class CommentExtensions
	{
		public enum RangeTagType
		{
			None,
			Start,
			End,
		}
		public static IEnumerable<IBaseEvent> InCommentRange<TEvent>(this OrderedEventCollection<TEvent> e, Func<string, bool> isCommentAvaliable) where TEvent : IBaseEvent
		{
			Tab? tab = null;
			Comment? start = null;
			Comment? end = null;
			List<IBaseEvent> bufferLower = [];
			List<IBaseEvent> bufferUpper = [];
			foreach (IBaseEvent ev in e)
				if (ev is Comment comment
					&& isCommentAvaliable(comment.Text))
					if (tab is null)
					{
						start = comment;
						tab = comment.Tab;
					}
					else
					{
						end = comment;
						break;
					}
				else if (tab is not null && ev.Tab == tab)
					if (ev.Y < start!.Y)
						bufferLower.Add(ev);
					else
						bufferUpper.Add(ev);
			if (start is null)
				return [];
			if (end!.Y > start!.Y)
				return bufferUpper.Where(i => i.Y < end!.Y);
			else
				return bufferLower.Where(i => i.Y >= end!.Y);
		}
	}
}