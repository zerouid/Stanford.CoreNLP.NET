using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>This class will add the lemmas of all the words to the Annotation.</summary>
	/// <remarks>
	/// This class will add the lemmas of all the words to the Annotation.
	/// It assumes that the Annotation already contains the tokenized words as
	/// a
	/// <c>List&lt;CoreLabel&gt;</c>
	/// for a list of sentences under the
	/// <c>SentencesAnnotation.class</c>
	/// key.
	/// The Annotator adds lemma information to each CoreLabel,
	/// in the LemmaAnnotation.class.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	public class MorphaAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.MorphaAnnotator));

		private bool Verbose = false;

		private static readonly string[] prep = new string[] { "abroad", "across", "after", "ahead", "along", "aside", "away", "around", "back", "down", "forward", "in", "off", "on", "over", "out", "round", "together", "through", "up" };

		private static readonly IList<string> particles = Arrays.AsList(prep);

		public MorphaAnnotator()
			: this(true)
		{
		}

		public MorphaAnnotator(bool verbose)
		{
			Verbose = verbose;
		}

		public virtual void Annotate(Annotation annotation)
		{
			if (Verbose)
			{
				log.Info("Finding lemmas ...");
			}
			Morphology morphology = new Morphology();
			if (annotation.ContainsKey(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
					//log.info("Lemmatizing sentence: " + tokens);
					foreach (CoreLabel token in tokens)
					{
						string text = token.Get(typeof(CoreAnnotations.TextAnnotation));
						string posTag = token.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
						AddLemma(morphology, typeof(CoreAnnotations.LemmaAnnotation), token, text, posTag);
					}
				}
			}
			else
			{
				throw new Exception("Unable to find words/tokens in: " + annotation);
			}
		}

		private static void AddLemma(Morphology morpha, Type ann, ICoreMap map, string word, string tag)
		{
			if (!tag.IsEmpty())
			{
				string phrasalVerb = PhrasalVerb(morpha, word, tag);
				if (phrasalVerb == null)
				{
					map.Set(ann, morpha.Lemma(word, tag));
				}
				else
				{
					map.Set(ann, phrasalVerb);
				}
			}
			else
			{
				map.Set(ann, morpha.Stem(word));
			}
		}

		/// <summary>
		/// If a token is a phrasal verb with an underscore between a verb and a
		/// particle, return the phrasal verb lemmatized.
		/// </summary>
		/// <remarks>
		/// If a token is a phrasal verb with an underscore between a verb and a
		/// particle, return the phrasal verb lemmatized. If not, return null
		/// </remarks>
		private static string PhrasalVerb(Morphology morpha, string word, string tag)
		{
			// must be a verb and contain an underscore
			System.Diagnostics.Debug.Assert((word != null));
			System.Diagnostics.Debug.Assert((tag != null));
			if (!tag.StartsWith("VB") || !word.Contains("_"))
			{
				return null;
			}
			// check whether the last part is a particle
			string[] verb = word.Split("_");
			if (verb.Length != 2)
			{
				return null;
			}
			string particle = verb[1];
			if (particles.Contains(particle))
			{
				string @base = verb[0];
				string lemma = morpha.Lemma(@base, tag);
				return lemma + '_' + particle;
			}
			return null;
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.PartOfSpeechAnnotation
				))));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.Singleton(typeof(CoreAnnotations.LemmaAnnotation));
		}
	}
}
