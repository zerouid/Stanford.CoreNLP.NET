using System.Collections.Generic;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Performs action on a sequence</summary>
	/// <author>Angel Chang</author>
	public interface ISequenceMatchAction<T>
	{
		ISequenceMatchResult<T> Apply(ISequenceMatchResult<T> matchResult, params int[] groups);

		public sealed class BoundAction<T>
		{
			internal ISequenceMatchAction<T> action;

			internal int[] groups;

			public ISequenceMatchResult<T> Apply(ISequenceMatchResult<T> seqMatchResult)
			{
				return action.Apply(seqMatchResult, groups);
			}
		}

		public sealed class StartMatchAction<T> : ISequenceMatchAction<T>
		{
			internal SequencePattern<T> pattern;

			public StartMatchAction(SequencePattern<T> pattern)
			{
				this.pattern = pattern;
			}

			public ISequenceMatchResult<T> Apply(ISequenceMatchResult<T> seqMatchResult, params int[] groups)
			{
				SequenceMatcher<T> matcher = pattern.GetMatcher(seqMatchResult.Elements());
				if (matcher.Find())
				{
					return matcher;
				}
				else
				{
					return null;
				}
			}
		}

		public sealed class NextMatchAction<T> : ISequenceMatchAction<T>
		{
			public ISequenceMatchResult<T> Apply(ISequenceMatchResult<T> seqMatchResult, params int[] groups)
			{
				if (seqMatchResult is SequenceMatcher)
				{
					SequenceMatcher<T> matcher = (SequenceMatcher<T>)seqMatchResult;
					if (matcher.Find())
					{
						return matcher;
					}
					else
					{
						return null;
					}
				}
				else
				{
					return null;
				}
			}
		}

		public sealed class BranchAction<T> : ISequenceMatchAction<T>
		{
			internal IPredicate<ISequenceMatchResult<T>> filter;

			internal ISequenceMatchAction<T> acceptBranch;

			internal ISequenceMatchAction<T> rejectBranch;

			public BranchAction(IPredicate<ISequenceMatchResult<T>> filter, ISequenceMatchAction<T> acceptBranch, ISequenceMatchAction<T> rejectBranch)
			{
				this.filter = filter;
				this.acceptBranch = acceptBranch;
				this.rejectBranch = rejectBranch;
			}

			public ISequenceMatchResult<T> Apply(ISequenceMatchResult<T> seqMatchResult, params int[] groups)
			{
				if (filter.Test(seqMatchResult))
				{
					return (acceptBranch != null) ? acceptBranch.Apply(seqMatchResult) : null;
				}
				else
				{
					return (rejectBranch != null) ? rejectBranch.Apply(seqMatchResult) : null;
				}
			}
		}

		public sealed class SeriesAction<T> : ISequenceMatchAction<T>
		{
			internal IList<ISequenceMatchAction<T>> actions;

			public SeriesAction(params ISequenceMatchAction<T>[] actions)
			{
				this.actions = Arrays.AsList(actions);
			}

			public SeriesAction(IList<ISequenceMatchAction<T>> actions)
			{
				this.actions = actions;
			}

			public ISequenceMatchResult<T> Apply(ISequenceMatchResult<T> seqMatchResult, params int[] groups)
			{
				ISequenceMatchResult<T> res = seqMatchResult;
				foreach (ISequenceMatchAction<T> a in actions)
				{
					res = a.Apply(res, groups);
				}
				return res;
			}
		}
	}

	public static class SequenceMatchActionConstants
	{
	}
}
