using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Docreader;
using Edu.Stanford.Nlp.Coref.MD;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Data
{
	/// <summary>
	/// Class for creating
	/// <see cref="Document"/>
	/// s from raw
	/// <see cref="Edu.Stanford.Nlp.Pipeline.Annotation"/>
	/// s or from CoNLL input data.
	/// </summary>
	/// <author>Heeyoung Lee</author>
	/// <author>Kevin Clark</author>
	public class DocumentMaker
	{
		private readonly Properties props;

		private readonly IDocReader reader;

		private readonly IHeadFinder headFinder;

		private readonly Dictionaries dict;

		private readonly CorefMentionFinder md;

		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.IO.IOException"/>
		public DocumentMaker(Properties props, Dictionaries dictionaries)
		{
			this.props = props;
			this.dict = dictionaries;
			reader = GetDocumentReader(props);
			headFinder = CorefProperties.GetHeadFinder(props);
			md = CorefProperties.UseGoldMentions(props) ? new RuleBasedCorefMentionFinder(headFinder, props) : null;
		}

		private static IDocReader GetDocumentReader(Properties props)
		{
			string corpusPath = CorefProperties.GetInputPath(props);
			if (corpusPath == null)
			{
				return null;
			}
			CoNLLDocumentReader.Options options = new CoNLLDocumentReader.Options();
			if (!PropertiesUtils.GetBool(props, "coref.printConLLLoadingMessage", true))
			{
				options.printConLLLoadingMessage = false;
			}
			options.annotateTokenCoref = false;
			string conllFileFilter = props.GetProperty("coref.conllFileFilter", ".*_auto_conll$");
			options.SetFilter(conllFileFilter);
			options.lang = CorefProperties.GetLanguage(props);
			return new CoNLLDocumentReader(corpusPath, options);
		}

		/// <exception cref="System.Exception"/>
		public virtual Document MakeDocument(Annotation anno)
		{
			return MakeDocument(new InputDoc(anno, null, null));
		}

		/// <exception cref="System.Exception"/>
		public virtual Document MakeDocument(InputDoc input)
		{
			IList<IList<Mention>> mentions = new List<IList<Mention>>();
			if (CorefProperties.UseGoldMentions(props))
			{
				IList<ICoreMap> sentences = input.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
				for (int i = 0; i < sentences.Count; i++)
				{
					ICoreMap sentence = sentences[i];
					IList<CoreLabel> sentenceWords = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
					IList<Mention> sentenceMentions = new List<Mention>();
					mentions.Add(sentenceMentions);
					foreach (Mention g in input.goldMentions[i])
					{
						sentenceMentions.Add(new Mention(-1, g.startIndex, g.endIndex, sentenceWords, null, null, new List<CoreLabel>(sentenceWords.SubList(g.startIndex, g.endIndex))));
					}
					md.FindHead(sentence, sentenceMentions);
				}
			}
			else
			{
				foreach (ICoreMap sentence in input.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					mentions.Add(sentence.Get(typeof(CorefCoreAnnotations.CorefMentionsAnnotation)));
				}
			}
			Document doc = new Document(input, mentions);
			if (input.goldMentions != null)
			{
				FindGoldMentionHeads(doc);
			}
			DocumentPreprocessor.Preprocess(doc, dict, null, headFinder);
			return doc;
		}

		private static void FindGoldMentionHeads(Document doc)
		{
			IList<ICoreMap> sentences = doc.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			for (int i = 0; i < sentences.Count; i++)
			{
				DependencyCorefMentionFinder.FindHeadInDependency(sentences[i], doc.goldMentions[i]);
			}
		}

		private StanfordCoreNLP coreNLP;

		private StanfordCoreNLP GetStanfordCoreNLP(Properties props)
		{
			if (coreNLP != null)
			{
				return coreNLP;
			}
			Properties pipelineProps = new Properties(props);
			if (CorefProperties.Conll(props))
			{
				pipelineProps.SetProperty("annotators", (CorefProperties.GetLanguage(props) == Locale.Chinese ? "lemma, ner" : "lemma") + (CorefProperties.UseGoldMentions(props) ? string.Empty : ", coref.mention"));
				pipelineProps.SetProperty("ner.applyFineGrained", "false");
			}
			else
			{
				pipelineProps.SetProperty("annotators", "pos, lemma, ner, " + (CorefProperties.UseConstituencyParse(props) ? "parse" : "depparse") + (CorefProperties.UseGoldMentions(props) ? string.Empty : ", coref.mention"));
				pipelineProps.SetProperty("ner.applyFineGrained", "false");
			}
			return (coreNLP = new StanfordCoreNLP(pipelineProps, false));
		}

		/// <exception cref="System.Exception"/>
		public virtual Document NextDoc()
		{
			InputDoc input = reader.NextDoc();
			if (input == null)
			{
				return null;
			}
			if (!CorefProperties.UseConstituencyParse(props))
			{
				foreach (ICoreMap sentence in input.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					sentence.Remove(typeof(TreeCoreAnnotations.TreeAnnotation));
				}
			}
			GetStanfordCoreNLP(props).Annotate(input.annotation);
			if (CorefProperties.Conll(props))
			{
				input.annotation.Set(typeof(CoreAnnotations.UseMarkedDiscourseAnnotation), true);
			}
			return MakeDocument(input);
		}

		public virtual void ResetDocs()
		{
			reader.Reset();
		}
	}
}
