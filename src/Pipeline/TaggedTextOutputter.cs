using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Created by michaelf on 7/15/15.</summary>
	/// <remarks>Created by michaelf on 7/15/15. Outputs document back into text format, with verbs and nouns tagged as such (_V or _N) and also lemmatized.</remarks>
	public class TaggedTextOutputter : AnnotationOutputter
	{
		public TaggedTextOutputter()
		{
		}

		/// <exception cref="System.IO.IOException"/>
		public override void Print(Annotation doc, OutputStream target, AnnotationOutputter.Options options)
		{
			PrintWriter os = new PrintWriter(IOUtils.EncodedOutputStreamWriter(target, options.encoding));
			Print(doc, os, options);
		}

		/// <exception cref="System.IO.IOException"/>
		private static void Print(Annotation annotation, PrintWriter pw, AnnotationOutputter.Options options)
		{
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			if (sentences != null)
			{
				for (int i = 0; i < sentences.Count; i++)
				{
					ICoreMap sentence = sentences[i];
					StringBuilder sentenceToWrite = new StringBuilder();
					foreach (CoreLabel token in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
					{
						sentenceToWrite.Append(" ");
						sentenceToWrite.Append(token.Lemma().ToLower());
						if (token.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).StartsWith("V"))
						{
							//verb
							sentenceToWrite.Append("_V");
						}
						else
						{
							if (token.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).StartsWith("N"))
							{
								//noun
								sentenceToWrite.Append("_N");
							}
						}
					}
					pw.Print(sentenceToWrite.ToString());
				}
			}
		}

		//omit first space
		//from TextOutputter
		/// <summary>Static helper</summary>
		public static void PrettyPrint(Annotation annotation, OutputStream stream, StanfordCoreNLP pipeline)
		{
			PrettyPrint(annotation, new PrintWriter(stream), pipeline);
		}

		/// <summary>Static helper</summary>
		public static void PrettyPrint(Annotation annotation, PrintWriter pw, StanfordCoreNLP pipeline)
		{
			try
			{
				Edu.Stanford.Nlp.Pipeline.TaggedTextOutputter.Print(annotation, pw, GetOptions(pipeline));
			}
			catch (IOException e)
			{
				// already flushed
				// don't close, might not want to close underlying stream
				throw new RuntimeIOException(e);
			}
		}
	}
}
