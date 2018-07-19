using System;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Naturalli;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>A class abstracting the implementation of various annotators.</summary>
	/// <remarks>
	/// A class abstracting the implementation of various annotators.
	/// Importantly, subclasses of this class can overwrite the implementation
	/// of these annotators by returning a different annotator, and
	/// <see cref="StanfordCoreNLP"/>
	/// will automatically load
	/// the new annotator instead.
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class AnnotatorImplementations
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(AnnotatorImplementations));

		/// <summary>Tokenize, emulating the Penn Treebank</summary>
		public virtual IAnnotator Tokenizer(Properties properties)
		{
			return new TokenizerAnnotator(properties);
		}

		/// <summary>Clean XML input</summary>
		public virtual CleanXmlAnnotator CleanXML(Properties properties)
		{
			return new CleanXmlAnnotator(properties);
		}

		/// <summary>Sentence split, in addition to a bunch of other things in this annotator (be careful to check the implementation!)</summary>
		public virtual IAnnotator WordToSentences(Properties properties)
		{
			return new WordsToSentencesAnnotator(properties);
		}

		/// <summary>Part of speech tag</summary>
		public virtual IAnnotator PosTagger(Properties properties)
		{
			string annotatorName = "pos";
			return new POSTaggerAnnotator(annotatorName, properties);
		}

		/// <summary>Annotate lemmas</summary>
		public virtual IAnnotator Morpha(Properties properties, bool verbose)
		{
			return new MorphaAnnotator(verbose);
		}

		/// <summary>Annotate for named entities -- note that this combines multiple NER tag sets, and some auxiliary things (like temporal tagging)</summary>
		public virtual IAnnotator Ner(Properties properties)
		{
			try
			{
				return new NERCombinerAnnotator(properties);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>Run TokensRegex -- annotate patterns found in tokens</summary>
		public virtual IAnnotator Tokensregex(Properties properties, string name)
		{
			return new TokensRegexAnnotator(name, properties);
		}

		/// <summary>Run RegexNER -- rule-based NER based on a deterministic mapping file</summary>
		public virtual IAnnotator TokensRegexNER(Properties properties, string name)
		{
			return new TokensRegexNERAnnotator(name, properties);
		}

		/// <summary>Annotate mentions</summary>
		public virtual IAnnotator EntityMentions(Properties properties, string name)
		{
			return new EntityMentionsAnnotator(name, properties);
		}

		/// <summary>Annotate for gender of tokens</summary>
		public virtual IAnnotator Gender(Properties properties, string name)
		{
			return new GenderAnnotator(name, properties);
		}

		/// <summary>Annotate parse trees</summary>
		/// <param name="properties">Properties that control the behavior of the parser. It use "parse.x" properties.</param>
		/// <returns>A ParserAnnotator</returns>
		public virtual IAnnotator Parse(Properties properties)
		{
			string parserType = properties.GetProperty("parse.type", "stanford");
			string maxLenStr = properties.GetProperty("parse.maxlen");
			if (Sharpen.Runtime.EqualsIgnoreCase(parserType, "stanford"))
			{
				return new ParserAnnotator("parse", properties);
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(parserType, "charniak"))
				{
					string model = properties.GetProperty("parse.model");
					string parserExecutable = properties.GetProperty("parse.executable");
					if (model == null || parserExecutable == null)
					{
						throw new Exception("Both parse.model and parse.executable properties must be specified if parse.type=charniak");
					}
					int maxLen = 399;
					if (maxLenStr != null)
					{
						maxLen = System.Convert.ToInt32(maxLenStr);
					}
					return new CharniakParserAnnotator(model, parserExecutable, false, maxLen);
				}
				else
				{
					throw new Exception("Unknown parser type: " + parserType + " (currently supported: stanford and charniak)");
				}
			}
		}

		public virtual IAnnotator Custom(Properties properties, string property)
		{
			string customName = property;
			string customClassName = properties.GetProperty(StanfordCoreNLP.CustomAnnotatorPrefix + property);
			if (property.StartsWith(StanfordCoreNLP.CustomAnnotatorPrefix))
			{
				customName = Sharpen.Runtime.Substring(property, StanfordCoreNLP.CustomAnnotatorPrefix.Length);
				customClassName = properties.GetProperty(property);
			}
			try
			{
				// name + properties
				return new MetaClass(customClassName).CreateInstance(customName, properties);
			}
			catch (MetaClass.ConstructorNotFoundException)
			{
				try
				{
					// name
					return new MetaClass(customClassName).CreateInstance(customName);
				}
				catch (MetaClass.ConstructorNotFoundException)
				{
					// properties
					try
					{
						return new MetaClass(customClassName).CreateInstance(properties);
					}
					catch (MetaClass.ConstructorNotFoundException)
					{
						// empty arguments
						return new MetaClass(customClassName).CreateInstance();
					}
				}
			}
		}

		/// <summary>Infer the original casing of tokens</summary>
		public virtual IAnnotator TrueCase(Properties properties)
		{
			return new TrueCaseAnnotator(properties);
		}

		/// <summary>Annotate for mention (statistical or hybrid)</summary>
		public virtual IAnnotator CorefMention(Properties properties)
		{
			// TO DO: split up coref and mention properties
			Properties corefProperties = PropertiesUtils.ExtractPrefixedProperties(properties, AnnotatorConstants.StanfordCoref + ".", true);
			Properties mentionProperties = PropertiesUtils.ExtractPrefixedProperties(properties, AnnotatorConstants.StanfordCorefMention + ".", true);
			Properties allPropsForCoref = new Properties();
			allPropsForCoref.PutAll(corefProperties);
			allPropsForCoref.PutAll(mentionProperties);
			return new CorefMentionAnnotator(allPropsForCoref);
		}

		/// <summary>Annotate for coreference (statistical or hybrid)</summary>
		public virtual IAnnotator Coref(Properties properties)
		{
			Properties corefProperties = PropertiesUtils.ExtractPrefixedProperties(properties, AnnotatorConstants.StanfordCoref + ".", true);
			Properties mentionProperties = PropertiesUtils.ExtractPrefixedProperties(properties, AnnotatorConstants.StanfordCorefMention + ".", true);
			Properties allPropsForCoref = new Properties();
			allPropsForCoref.PutAll(corefProperties);
			allPropsForCoref.PutAll(mentionProperties);
			return new CorefAnnotator(allPropsForCoref);
		}

		/// <summary>Annotate for coreference (deterministic)</summary>
		public virtual IAnnotator Dcoref(Properties properties)
		{
			return new DeterministicCorefAnnotator(properties);
		}

		/// <summary>Annotate for relations expressed in sentences</summary>
		public virtual IAnnotator Relations(Properties properties)
		{
			return new RelationExtractorAnnotator(properties);
		}

		/// <summary>Annotate for sentiment in sentences</summary>
		public virtual IAnnotator Sentiment(Properties properties, string name)
		{
			return new SentimentAnnotator(name, properties);
		}

		/// <summary>Annotate with the column data classifier.</summary>
		public virtual IAnnotator ColumnData(Properties properties)
		{
			if (properties.Contains("classify.loadClassifier"))
			{
				properties.SetProperty("loadClassifier", properties.GetProperty("classify.loadClassifier"));
			}
			if (!properties.Contains("loadClassifier"))
			{
				throw new Exception("Must load a classifier when creating a column data classifier annotator");
			}
			return new ColumnDataClassifierAnnotator(properties);
		}

		/// <summary>Annotate dependency relations in sentences</summary>
		public virtual IAnnotator Dependencies(Properties properties)
		{
			Properties relevantProperties = PropertiesUtils.ExtractPrefixedProperties(properties, AnnotatorConstants.StanfordDependencies + '.');
			return new DependencyParseAnnotator(relevantProperties);
		}

		/// <summary>Annotate operators (e.g., quantifiers) and polarity of tokens in a sentence</summary>
		public virtual IAnnotator Natlog(Properties properties)
		{
			Properties relevantProperties = PropertiesUtils.ExtractPrefixedProperties(properties, AnnotatorConstants.StanfordNatlog + '.');
			return new NaturalLogicAnnotator(relevantProperties);
		}

		/// <summary>
		/// Annotate
		/// <see cref="Edu.Stanford.Nlp.IE.Util.RelationTriple"/>
		/// s from text.
		/// </summary>
		public virtual IAnnotator Openie(Properties properties)
		{
			Properties relevantProperties = PropertiesUtils.ExtractPrefixedProperties(properties, AnnotatorConstants.StanfordOpenie + '.');
			return new OpenIE(relevantProperties);
		}

		/// <summary>Annotate quotes and extract them like sentences</summary>
		public virtual IAnnotator Quote(Properties properties)
		{
			Properties relevantProperties = PropertiesUtils.ExtractPrefixedProperties(properties, AnnotatorConstants.StanfordQuote + '.');
			return new QuoteAnnotator(relevantProperties);
		}

		/// <summary>Attribute quotes to speakers</summary>
		public virtual IAnnotator Quoteattribution(Properties properties)
		{
			Properties relevantProperties = PropertiesUtils.ExtractPrefixedProperties(properties, AnnotatorConstants.StanfordQuoteAttribution + '.');
			return new QuoteAttributionAnnotator(relevantProperties);
		}

		/// <summary>Add universal dependencies features</summary>
		public virtual IAnnotator Udfeats(Properties properties)
		{
			return new UDFeatureAnnotator();
		}

		/// <summary>Annotate for KBP relations</summary>
		public virtual IAnnotator Kbp(Properties properties)
		{
			return new KBPAnnotator(AnnotatorConstants.StanfordKbp, properties);
		}

		public virtual IAnnotator Link(Properties properties)
		{
			return new WikidictAnnotator(AnnotatorConstants.StanfordLink, properties);
		}
	}
}
