using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Paragraphs
{
	/// <author>Grace Muzny</author>
	public class ParagraphAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Paragraphs.ParagraphAnnotator));

		private readonly bool Verbose;

		private readonly bool Debug = true;

		public string ParagraphBreak = "two";

		public ParagraphAnnotator(Properties props, bool verbose)
		{
			// Whether or not to allow quotes of the same type embedded inside of each other
			// ["one" | "two"]
			ParagraphBreak = props.GetProperty("paragraphBreak", "two");
			Verbose = verbose;
		}

		public virtual void Annotate(Annotation annotation)
		{
			if (Verbose)
			{
				System.Console.Error.Write("Adding paragraph index annotation (" + ParagraphBreak + ") ...");
			}
			Pattern paragraphSplit = null;
			if (ParagraphBreak.Equals("two"))
			{
				paragraphSplit = Pattern.Compile("\\n\\n+");
			}
			else
			{
				if (ParagraphBreak.Equals("one"))
				{
					paragraphSplit = Pattern.Compile("\\n+");
				}
			}
			string fullText = annotation.Get(typeof(CoreAnnotations.TextAnnotation));
			Matcher m = paragraphSplit.Matcher(fullText);
			IList<int> paragraphBreaks = Generics.NewArrayList();
			while (m.Find())
			{
				// get the staring index
				paragraphBreaks.Add(m.Start());
			}
			// each sentence gets a paragraph id annotation
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			int currParagraph = -1;
			int nextParagraphStartIndex = -1;
			foreach (ICoreMap sent in sentences)
			{
				int sentBegin = sent.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				if (sentBegin >= nextParagraphStartIndex)
				{
					if (currParagraph + 1 < paragraphBreaks.Count)
					{
						nextParagraphStartIndex = paragraphBreaks[currParagraph + 1];
					}
					else
					{
						nextParagraphStartIndex = fullText.Length;
					}
					currParagraph++;
				}
				sent.Set(typeof(CoreAnnotations.ParagraphIndexAnnotation), currParagraph);
			}
			if (Verbose)
			{
				System.Console.Error.WriteLine("done");
			}
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.Singleton(typeof(CoreAnnotations.ParagraphIndexAnnotation));
		}

		public virtual ICollection<Type> Requires()
		{
			return new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation
				), typeof(CoreAnnotations.BeforeAnnotation), typeof(CoreAnnotations.AfterAnnotation), typeof(CoreAnnotations.TokenBeginAnnotation), typeof(CoreAnnotations.TokenEndAnnotation), typeof(CoreAnnotations.IndexAnnotation), typeof(CoreAnnotations.OriginalTextAnnotation
				)));
		}
	}
}
