using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Quoteattribution
{
	/// <summary>Created by mjfang on 12/18/16.</summary>
	/// <remarks>
	/// Created by mjfang on 12/18/16. Annotates each sentence with what chapter it is in (1-indexed).
	/// Currently uses "CHAPTER" as a delimiter; may have to be extended in the future.
	/// </remarks>
	public class ChapterAnnotator : IAnnotator
	{
		public string ChapterBreak = "CHAPTER";

		public class ChapterAnnotation : ICoreAnnotation<int>
		{
			//key to a list of sentences that begin chapters
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		public virtual void Annotate(Annotation doc)
		{
			IDictionary<int, int> sentenceToChapter = new Dictionary<int, int>();
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			int chapterNum = 0;
			int sentenceIndex = 0;
			foreach (ICoreMap sentence in sentences)
			{
				if (sentence.Get(typeof(CoreAnnotations.TextAnnotation)).Contains(ChapterBreak))
				{
					chapterNum++;
				}
				sentence.Set(typeof(ChapterAnnotator.ChapterAnnotation), chapterNum);
				sentenceToChapter[sentenceIndex] = chapterNum;
				sentenceIndex++;
			}
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return null;
		}

		public virtual ICollection<Type> Requires()
		{
			return new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.SentencesAnnotation)));
		}
	}
}
