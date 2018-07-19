using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.IE.Regexp;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// This class provides a facility for normalizing content of numerical named
	/// entities (number, money, date, time) in the pipeline package world.
	/// </summary>
	/// <remarks>
	/// This class provides a facility for normalizing content of numerical named
	/// entities (number, money, date, time) in the pipeline package world. It uses a
	/// lot of code with
	/// <see cref="Edu.Stanford.Nlp.IE.QuantifiableEntityNormalizer"/>
	/// .
	/// New stuff should generally be added there so as to reduce code duplication.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	/// <author>Christopher Manning (extended for RTE)</author>
	/// <author>Chris Cox (original version)</author>
	public class QuantifiableEntityNormalizingAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.QuantifiableEntityNormalizingAnnotator));

		private Timing timer = new Timing();

		private readonly bool Verbose;

		private const string DefaultBackgroundSymbol = "O";

		private readonly bool collapse;

		public const string BackgroundSymbolProperty = "background";

		public const string CollapseProperty = "collapse";

		public QuantifiableEntityNormalizingAnnotator()
			: this(DefaultBackgroundSymbol, true)
		{
		}

		public QuantifiableEntityNormalizingAnnotator(bool verbose)
			: this(DefaultBackgroundSymbol, verbose)
		{
		}

		public QuantifiableEntityNormalizingAnnotator(string name, Properties props)
		{
			// TODO: collpase = true won't work properly (see annotateTokens)
			string property = name + "." + BackgroundSymbolProperty;
			string backgroundSymbol = props.GetProperty(property, DefaultBackgroundSymbol);
			// this next line is yuck as QuantifiableEntityNormalizer is still static
			QuantifiableEntityNormalizer.BackgroundSymbol = backgroundSymbol;
			property = name + "." + CollapseProperty;
			collapse = PropertiesUtils.GetBool(props, property, false);
			if (this.collapse)
			{
				log.Info("WARNING: QuantifiableEntityNormalizingAnnotator does not work well with collapse=true");
			}
			Verbose = false;
		}

		/// <summary>
		/// Do quantity entity normalization and collapse together multitoken quantity
		/// entities into a single token.
		/// </summary>
		/// <param name="backgroundSymbol">NER background symbol</param>
		/// <param name="verbose">Whether to write messages</param>
		public QuantifiableEntityNormalizingAnnotator(string backgroundSymbol, bool verbose)
			: this(backgroundSymbol, verbose, false)
		{
		}

		/// <summary>
		/// Do quantity entity normalization and collapse together multitoken quantity
		/// entities into a single token.
		/// </summary>
		/// <param name="verbose">Whether to write messages</param>
		/// <param name="collapse">Whether to collapse multitoken quantity entities.</param>
		public QuantifiableEntityNormalizingAnnotator(bool verbose, bool collapse)
			: this(DefaultBackgroundSymbol, verbose, collapse)
		{
		}

		public QuantifiableEntityNormalizingAnnotator(string backgroundSymbol, bool verbose, bool collapse)
		{
			// this next line is yuck as QuantifiableEntityNormalizer is still static
			QuantifiableEntityNormalizer.BackgroundSymbol = backgroundSymbol;
			Verbose = verbose;
			this.collapse = collapse;
			if (this.collapse)
			{
				log.Info("WARNING: QuantifiableEntityNormalizingAnnotator does not work well with collapse=true");
			}
		}

		public virtual void Annotate(Annotation annotation)
		{
			if (Verbose)
			{
				timer.Start();
				log.Info("Normalizing quantifiable entities...");
			}
			if (annotation.ContainsKey(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
				foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
					AnnotateTokens(tokens);
				}
				if (Verbose)
				{
					timer.Stop("done.");
					log.Info("output: " + sentences + '\n');
				}
			}
			else
			{
				if (annotation.ContainsKey(typeof(CoreAnnotations.TokensAnnotation)))
				{
					IList<CoreLabel> tokens = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
					AnnotateTokens(tokens);
				}
				else
				{
					throw new Exception("unable to find sentences in: " + annotation);
				}
			}
		}

		private void AnnotateTokens<Token>(IList<TOKEN> tokens)
			where Token : CoreLabel
		{
			// Make a copy of the tokens before annotating because QuantifiableEntityNormalizer may change the POS too
			IList<CoreLabel> words = new List<CoreLabel>();
			foreach (CoreLabel token in tokens)
			{
				CoreLabel word = new CoreLabel();
				word.SetWord(token.Word());
				word.SetNER(token.Ner());
				word.SetTag(token.Tag());
				// copy fields potentially set by SUTime
				NumberSequenceClassifier.TransferAnnotations(token, word);
				words.Add(word);
			}
			DoOneSentence(words);
			// TODO: If collapsed is set, tokens for entities are collapsed into one node then
			// (words.size() != tokens.size() and the logic below just don't work!!!
			for (int i = 0; i < words.Count; i++)
			{
				string ner = words[i].Ner();
				tokens[i].SetNER(ner);
				tokens[i].Set(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation), words[i].Get(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation)));
			}
		}

		private void DoOneSentence<Token>(IList<TOKEN> words)
			where Token : CoreLabel
		{
			QuantifiableEntityNormalizer.AddNormalizedQuantitiesToEntities(words, collapse);
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation
				), typeof(CoreAnnotations.SentencesAnnotation))));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			// technically it adds some NER, but someone who wants full NER
			// labels will be very disappointed, so we do not claim to produce NER
			return Java.Util.Collections.Singleton(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation));
		}
	}
}
