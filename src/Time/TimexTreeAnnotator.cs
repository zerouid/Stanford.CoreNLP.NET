using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Time
{
	public class TimexTreeAnnotator : IAnnotator
	{
		public enum MatchType
		{
			ExactMatch,
			SmallestEnclosing
		}

		private TimexTreeAnnotator.MatchType matchType;

		public TimexTreeAnnotator(TimexTreeAnnotator.MatchType matchType)
		{
			this.matchType = matchType;
		}

		public virtual void Annotate(Annotation document)
		{
			foreach (ICoreMap sentence in document.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
				Tree tree = sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
				tree.IndexSpans(0);
				// add a tree to each timex annotation
				foreach (ICoreMap timexAnn in sentence.Get(typeof(TimeAnnotations.TimexAnnotations)))
				{
					Tree subtree;
					int timexBegin = BeginOffset(timexAnn);
					int timexEnd = EndOffset(timexAnn);
					IEnumerable<Tree> possibleMatches;
					switch (this.matchType)
					{
						case TimexTreeAnnotator.MatchType.ExactMatch:
						{
							// only use trees that match exactly
							possibleMatches = Iterables.Filter(tree, null);
							IEnumerator<Tree> treeIter = possibleMatches.GetEnumerator();
							subtree = treeIter.MoveNext() ? treeIter.Current : null;
							break;
						}

						case TimexTreeAnnotator.MatchType.SmallestEnclosing:
						{
							// select the smallest enclosing tree
							possibleMatches = Iterables.Filter(tree, null);
							IList<Tree> sortedMatches = CollectionUtils.ToList(possibleMatches);
							sortedMatches.Sort(null);
							subtree = sortedMatches[0];
							break;
						}

						default:
						{
							// more cases could go here if they're added
							throw new Exception("unexpected match type");
						}
					}
					// add the subtree to the time annotation
					if (subtree != null)
					{
						timexAnn.Set(typeof(TreeCoreAnnotations.TreeAnnotation), subtree);
					}
				}
			}
		}

		private static int BeginOffset(Tree tree, IList<CoreLabel> tokens)
		{
			ICoreMap label = (ICoreMap)tree.Label();
			int beginToken = label.Get(typeof(CoreAnnotations.BeginIndexAnnotation));
			return BeginOffset(tokens[beginToken]);
		}

		private static int EndOffset(Tree tree, IList<CoreLabel> tokens)
		{
			ICoreMap label = (ICoreMap)tree.Label();
			int endToken = label.Get(typeof(CoreAnnotations.EndIndexAnnotation));
			if (endToken > tokens.Count)
			{
				string msg = "no token %d in tree:\n%s\ntokens:\n%s";
				throw new Exception(string.Format(msg, endToken - 1, tree, tokens));
			}
			return EndOffset(tokens[endToken - 1]);
		}

		private static int BeginOffset(ICoreMap map)
		{
			return map.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
		}

		private static int EndOffset(ICoreMap map)
		{
			return map.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation
				), typeof(CoreAnnotations.SentencesAnnotation))));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			// TODO: not sure what goes here
			return Java.Util.Collections.EmptySet();
		}
	}
}
