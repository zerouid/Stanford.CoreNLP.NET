using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Sentiment;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	public class CoreSentence
	{
		private CoreDocument document;

		private ICoreMap sentenceCoreMap;

		private IList<CoreEntityMention> entityMentions;

		public CoreSentence(CoreDocument myDocument, ICoreMap coreMapSentence)
		{
			this.document = myDocument;
			this.sentenceCoreMap = coreMapSentence;
		}

		/// <summary>create list of CoreEntityMention's based on the CoreMap's entity mentions</summary>
		public virtual void WrapEntityMentions()
		{
			if (this.sentenceCoreMap.Get(typeof(CoreAnnotations.MentionsAnnotation)) != null)
			{
				entityMentions = this.sentenceCoreMap.Get(typeof(CoreAnnotations.MentionsAnnotation)).Stream().Map(null).Collect(Collectors.ToList());
			}
		}

		/// <summary>get the document this sentence is in</summary>
		public virtual CoreDocument Document()
		{
			return document;
		}

		/// <summary>get the underlying CoreMap if need be</summary>
		public virtual ICoreMap CoreMap()
		{
			return sentenceCoreMap;
		}

		/// <summary>full text of the sentence</summary>
		public virtual string Text()
		{
			return sentenceCoreMap.Get(typeof(CoreAnnotations.TextAnnotation));
		}

		/// <summary>char offsets of mention</summary>
		public virtual Pair<int, int> CharOffsets()
		{
			int beginCharOffset = this.sentenceCoreMap.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			int endCharOffset = this.sentenceCoreMap.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
			return new Pair<int, int>(beginCharOffset, endCharOffset);
		}

		/// <summary>list of tokens</summary>
		public virtual IList<CoreLabel> Tokens()
		{
			return sentenceCoreMap.Get(typeof(CoreAnnotations.TokensAnnotation));
		}

		/// <summary>list of pos tags</summary>
		public virtual IList<string> PosTags()
		{
			return Tokens().Stream().Map(null).Collect(Collectors.ToList());
		}

		/// <summary>list of ner tags</summary>
		public virtual IList<string> NerTags()
		{
			return Tokens().Stream().Map(null).Collect(Collectors.ToList());
		}

		/// <summary>constituency parse</summary>
		public virtual Tree ConstituencyParse()
		{
			return sentenceCoreMap.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
		}

		/// <summary>dependency parse</summary>
		public virtual SemanticGraph DependencyParse()
		{
			return sentenceCoreMap.Get(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation));
		}

		/// <summary>sentiment</summary>
		public virtual string Sentiment()
		{
			return sentenceCoreMap.Get(typeof(SentimentCoreAnnotations.SentimentClass));
		}

		/// <summary>sentiment tree</summary>
		public virtual Tree SentimentTree()
		{
			return sentenceCoreMap.Get(typeof(SentimentCoreAnnotations.SentimentAnnotatedTree));
		}

		/// <summary>list of entity mentions</summary>
		public virtual IList<CoreEntityMention> EntityMentions()
		{
			return this.entityMentions;
		}

		/// <summary>list of KBP relations found</summary>
		public virtual IList<RelationTriple> Relations()
		{
			return sentenceCoreMap.Get(typeof(CoreAnnotations.KBPTriplesAnnotation));
		}

		public override string ToString()
		{
			return CoreMap().ToString();
		}
	}
}
