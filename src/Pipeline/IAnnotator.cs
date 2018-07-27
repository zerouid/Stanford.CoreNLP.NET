using System;
using System.Collections.Generic;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// This is an interface for adding annotations to a partially annotated
	/// Annotation.
	/// </summary>
	/// <remarks>
	/// This is an interface for adding annotations to a partially annotated
	/// Annotation.  In some ways, it is just a glorified function, except
	/// that it explicitly operates in-place on Annotation objects.  Annotators
	/// should be given to an AnnotationPipeline in order to make
	/// annotation pipelines (the whole motivation of this package), and
	/// therefore implementers of this interface should be designed to play
	/// well with other Annotators and in their javadocs they should
	/// explicitly state what annotations they are assuming already exist
	/// in the annotation (like parse, POS tag, etc), what keys they are
	/// expecting them under (see, for instance, the ones in CoreAnnotations),
	/// and what annotations they will add (or modify) and the keys
	/// for them as well.  If you would like to look at the code for a
	/// relatively simple Annotator, I recommend NERAnnotator.  For a lot
	/// of code you could just add the implements directly, but I recommend
	/// wrapping instead because I believe that it will help to keep the
	/// pipeline code more manageable.
	/// An Annotator should also provide a description of what it produces and
	/// a description of what it requires to have been produced by using Sets
	/// of requirements.
	/// The StanfordCoreNLP version of the AnnotationPipeline can
	/// enforce requirements, throwing an exception if an annotator does
	/// not have all of its prerequisites met.  An Annotator which does not
	/// participate in this system can simply return Collections.emptySet()
	/// for both requires() and requirementsSatisfied().
	/// <h2>Properties</h2>
	/// We extensively use Properties objects to configure each Annotator.
	/// In particular, CoreNLP has most of its properties in an informal
	/// namespace with properties names like "parse.maxlen" to specify that
	/// a property only applies to a parser annotator. There can also be
	/// global properties; they should not have any periods in their names.
	/// Each Annotator knows its own name; we assume these don't collide badly,
	/// though possibly two parsers could share the "parse.*" namespace.
	/// An Annotator should have a constructor that simply takes a Properties
	/// object. At this point, the Annotator should expect to be getting
	/// properties in namespaces. The classes that annotators call (like
	/// a concrete parser, tagger, or whatever) mainly expect properties
	/// not in namespaces. In general the annotator should subset the
	/// passed in properties to keep only global properties and ones in
	/// its own namespace, and then strip the namespace prefix from the
	/// latter properties.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	public interface IAnnotator
	{
		/// <summary>Given an Annotation, perform a task on this Annotation.</summary>
		void Annotate(Annotation annotation);

		/// <summary>
		/// A block of code called when this annotator unmounts from the
		/// <see cref="AnnotatorPool"/>
		/// .
		/// By default, nothing is done.
		/// </summary>
		void Unmount();

		/// <summary>
		/// Returns a set of requirements for which tasks this annotator can
		/// provide.
		/// </summary>
		/// <remarks>
		/// Returns a set of requirements for which tasks this annotator can
		/// provide.  For example, the POS annotator will return "pos".
		/// </remarks>
		ICollection<Type> RequirementsSatisfied();

		/// <summary>
		/// Returns the set of tasks which this annotator requires in order
		/// to perform.
		/// </summary>
		/// <remarks>
		/// Returns the set of tasks which this annotator requires in order
		/// to perform.  For example, the POS annotator will return
		/// "tokenize", "ssplit".
		/// </remarks>
		ICollection<Type> Requires();

		private sealed class _Dictionary_121 : Dictionary<string, ICollection<string>>
		{
			public _Dictionary_121()
			{
				{
					// TODO(jebolton) Merge with entitymention
					this[IAnnotator.StanfordTokenize] = new LinkedHashSet<string>(Arrays.AsList());
					this[IAnnotator.StanfordCleanXml] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize));
					this[IAnnotator.StanfordSsplit] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize));
					this[IAnnotator.StanfordPos] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit));
					this[IAnnotator.StanfordLemma] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos));
					this[IAnnotator.StanfordNer] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos, IAnnotator.StanfordLemma));
					this[IAnnotator.StanfordTokensregex] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize));
					this[IAnnotator.StanfordRegexner] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit));
					this[IAnnotator.StanfordEntityMentions] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos, IAnnotator.StanfordLemma, IAnnotator.StanfordNer));
					this[IAnnotator.StanfordGender] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos, IAnnotator.StanfordLemma, IAnnotator.StanfordNer));
					this[IAnnotator.StanfordTruecase] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit));
					this[IAnnotator.StanfordParse] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos));
					this[IAnnotator.StanfordDeterministicCoref] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos, IAnnotator.StanfordLemma, IAnnotator.StanfordNer, IAnnotator.StanfordParse)
						);
					this[IAnnotator.StanfordCoref] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos, IAnnotator.StanfordLemma, IAnnotator.StanfordNer, IAnnotator.StanfordDependencies));
					this[IAnnotator.StanfordCorefMention] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos, IAnnotator.StanfordLemma, IAnnotator.StanfordNer, IAnnotator.StanfordDependencies
						));
					this[IAnnotator.StanfordRelation] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos, IAnnotator.StanfordLemma, IAnnotator.StanfordNer, IAnnotator.StanfordParse, IAnnotator
						.StanfordDependencies));
					this[IAnnotator.StanfordSentiment] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos, IAnnotator.StanfordParse));
					this[IAnnotator.StanfordColumnDataClassifier] = new LinkedHashSet<string>(Arrays.AsList());
					this[IAnnotator.StanfordDependencies] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos));
					this[IAnnotator.StanfordNatlog] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos, IAnnotator.StanfordLemma, IAnnotator.StanfordDependencies));
					this[IAnnotator.StanfordOpenie] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos, IAnnotator.StanfordLemma, IAnnotator.StanfordDependencies, IAnnotator.StanfordNatlog));
					this[IAnnotator.StanfordQuote] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos, IAnnotator.StanfordLemma, IAnnotator.StanfordNer));
					this[IAnnotator.StanfordQuoteAttribution] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos, IAnnotator.StanfordLemma, IAnnotator.StanfordNer, IAnnotator.StanfordCorefMention
						, IAnnotator.StanfordDependencies, IAnnotator.StanfordQuote));
					this[IAnnotator.StanfordUdFeatures] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos, IAnnotator.StanfordDependencies));
					this[IAnnotator.StanfordLink] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos, IAnnotator.StanfordDependencies, IAnnotator.StanfordLemma, IAnnotator.StanfordNer, IAnnotator
						.StanfordEntityMentions));
					this[IAnnotator.StanfordKbp] = new LinkedHashSet<string>(Arrays.AsList(IAnnotator.StanfordTokenize, IAnnotator.StanfordSsplit, IAnnotator.StanfordPos, IAnnotator.StanfordDependencies, IAnnotator.StanfordLemma, IAnnotator.StanfordNer, IAnnotator
						.StanfordCoref));
				}
			}
		}
	}

	public static class AnnotatorConstants
	{
		/// <summary>These are annotators which StanfordCoreNLP knows how to create.</summary>
		/// <remarks>
		/// These are annotators which StanfordCoreNLP knows how to create.
		/// Add new annotators and/or annotators from other groups here!
		/// </remarks>
		public const string StanfordTokenize = "tokenize";

		public const string StanfordCleanXml = "cleanxml";

		public const string StanfordSsplit = "ssplit";

		public const string StanfordPos = "pos";

		public const string StanfordLemma = "lemma";

		public const string StanfordNer = "ner";

		public const string StanfordRegexner = "regexner";

		public const string StanfordTokensregex = "tokensregex";

		public const string StanfordEntityMentions = "entitymentions";

		public const string StanfordGender = "gender";

		public const string StanfordTruecase = "truecase";

		public const string StanfordParse = "parse";

		public const string StanfordDeterministicCoref = "dcoref";

		public const string StanfordCoref = "coref";

		public const string StanfordCorefMention = "coref.mention";

		public const string StanfordRelation = "relation";

		public const string StanfordSentiment = "sentiment";

		public const string StanfordColumnDataClassifier = "cdc";

		public const string StanfordDependencies = "depparse";

		public const string StanfordNatlog = "natlog";

		public const string StanfordOpenie = "openie";

		public const string StanfordQuote = "quote";

		public const string StanfordQuoteAttribution = "quote.attribution";

		public const string StanfordUdFeatures = "udfeats";

		public const string StanfordLink = "entitylink";

		public const string StanfordKbp = "kbp";

		/// <summary>A mapping from an annotator to a its default transitive dependencies.</summary>
		/// <remarks>
		/// A mapping from an annotator to a its default transitive dependencies.
		/// Note that this is not guaranteed to be accurate, as properties set in the annotator
		/// can change the annotator's dependencies; but, it's a reasonable guess if you're using
		/// things out-of-the-box.
		/// </remarks>
		public const IDictionary<string, ICollection<string>> DefaultRequirements = new _Dictionary_121();
	}
}
