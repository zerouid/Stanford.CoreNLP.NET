using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Regexp;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>This class adds NER information to an annotation using the RegexNERSequenceClassifier.</summary>
	/// <remarks>
	/// This class adds NER information to an annotation using the RegexNERSequenceClassifier.
	/// It assumes that the Annotation has already been split into sentences, then tokenized
	/// into Lists of CoreLabels. Adds NER information to each CoreLabel as a NamedEntityTagAnnotation.
	/// </remarks>
	/// <author>jtibs</author>
	public class RegexNERAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.RegexNERAnnotator));

		private readonly RegexNERSequenceClassifier classifier;

		private readonly bool verbose;

		public static PropertiesUtils.Property[] SupportedProperties = new PropertiesUtils.Property[] { new PropertiesUtils.Property("mapping", DefaultPaths.DefaultRegexnerRules, "Mapping file to use."), new PropertiesUtils.Property("ignorecase", "true"
			, "Whether to ignore case or not when matching patterns."), new PropertiesUtils.Property("validpospattern", string.Empty, "Regular expression pattern for matching POS tags."), new PropertiesUtils.Property("verbose", "false", string.Empty) };

		public RegexNERAnnotator(string name, Properties properties)
		{
			string mapping = properties.GetProperty(name + ".mapping", DefaultPaths.DefaultRegexnerRules);
			bool ignoreCase = bool.Parse(properties.GetProperty(name + ".ignorecase", "true"));
			string validPosPattern = properties.GetProperty(name + ".validpospattern", RegexNERSequenceClassifier.DefaultValidPos);
			bool overwriteMyLabels = true;
			bool verbose = bool.Parse(properties.GetProperty(name + ".verbose", "false"));
			classifier = new RegexNERSequenceClassifier(mapping, ignoreCase, overwriteMyLabels, validPosPattern);
			this.verbose = verbose;
		}

		public RegexNERAnnotator(string mapping)
			: this(mapping, false)
		{
		}

		public RegexNERAnnotator(string mapping, bool ignoreCase)
			: this(mapping, ignoreCase, RegexNERSequenceClassifier.DefaultValidPos)
		{
		}

		public RegexNERAnnotator(string mapping, bool ignoreCase, string validPosPattern)
			: this(mapping, ignoreCase, true, validPosPattern, false)
		{
		}

		public RegexNERAnnotator(string mapping, bool ignoreCase, bool overwriteMyLabels, string validPosPattern, bool verbose)
		{
			classifier = new RegexNERSequenceClassifier(mapping, ignoreCase, overwriteMyLabels, validPosPattern);
			this.verbose = verbose;
		}

		public virtual void Annotate(Annotation annotation)
		{
			if (verbose)
			{
				log.Info("Adding RegexNER annotations ... ");
			}
			if (!annotation.ContainsKey(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				throw new Exception("Unable to find sentences in " + annotation);
			}
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			foreach (ICoreMap sentence in sentences)
			{
				IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
				classifier.Classify(tokens);
				foreach (CoreLabel token in tokens)
				{
					if (token.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)) == null)
					{
						token.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), classifier.flags.backgroundSymbol);
					}
				}
				for (int start = 0; start < tokens.Count; start++)
				{
					CoreLabel token_1 = tokens[start];
					string answerType = token_1.Get(typeof(CoreAnnotations.AnswerAnnotation));
					if (answerType == null)
					{
						continue;
					}
					string NERType = token_1.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
					int answerEnd = FindEndOfAnswerAnnotation(tokens, start);
					int NERStart = FindStartOfNERAnnotation(tokens, start);
					int NEREnd = FindEndOfNERAnnotation(tokens, start);
					// check that the spans are the same, specially handling the case of
					// tokens with background named entity tags ("other")
					if ((NERStart == start || NERType.Equals(classifier.flags.backgroundSymbol)) && (answerEnd == NEREnd || (NERType.Equals(classifier.flags.backgroundSymbol) && NEREnd >= answerEnd)))
					{
						// annotate each token in the span
						for (int i = start; i < answerEnd; i++)
						{
							tokens[i].Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), answerType);
						}
					}
					start = answerEnd - 1;
				}
			}
			if (verbose)
			{
				log.Info("done.");
			}
		}

		private static int FindEndOfAnswerAnnotation(IList<CoreLabel> tokens, int start)
		{
			string type = tokens[start].Get(typeof(CoreAnnotations.AnswerAnnotation));
			while (start < tokens.Count && type.Equals(tokens[start].Get(typeof(CoreAnnotations.AnswerAnnotation))))
			{
				start++;
			}
			return start;
		}

		private static int FindStartOfNERAnnotation(IList<CoreLabel> tokens, int start)
		{
			string type = tokens[start].Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
			while (start >= 0 && type.Equals(tokens[start].Get(typeof(CoreAnnotations.NamedEntityTagAnnotation))))
			{
				start--;
			}
			return start + 1;
		}

		private static int FindEndOfNERAnnotation(IList<CoreLabel> tokens, int start)
		{
			string type = tokens[start].Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
			while (start < tokens.Count && type.Equals(tokens[start].Get(typeof(CoreAnnotations.NamedEntityTagAnnotation))))
			{
				start++;
			}
			return start;
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation
				), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.PartOfSpeechAnnotation))));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			// TODO: we might want to allow for different RegexNER annotators
			// to satisfy different requirements
			return Java.Util.Collections.EmptySet();
		}
	}
}
