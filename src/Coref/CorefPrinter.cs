using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref
{
	/// <summary>Class for printing out coreference output.</summary>
	/// <author>Heeyoung Lee</author>
	/// <author>Kevin Clark</author>
	public class CorefPrinter
	{
		public static string PrintConllOutput(Document document, bool gold)
		{
			return PrintConllOutput(document, gold, false);
		}

		public static string PrintConllOutput(Document document, bool gold, bool filterSingletons)
		{
			IList<IList<Mention>> orderedMentions = gold ? document.goldMentions : document.predictedMentions;
			if (filterSingletons)
			{
				orderedMentions = orderedMentions.Stream().Map(null).Collect(Collectors.ToList());
			}
			return CorefPrinter.PrintConllOutput(document, orderedMentions, gold);
		}

		public static string PrintConllOutput(Document document, IList<IList<Mention>> orderedMentions, bool gold)
		{
			Annotation anno = document.annotation;
			IList<IList<string[]>> conllDocSentences = document.conllDoc.sentenceWordLists;
			string docID = anno.Get(typeof(CoreAnnotations.DocIDAnnotation));
			StringBuilder sb = new StringBuilder();
			sb.Append("#begin document ").Append(docID).Append("\n");
			IList<ICoreMap> sentences = anno.Get(typeof(CoreAnnotations.SentencesAnnotation));
			for (int sentNum = 0; sentNum < sentences.Count; sentNum++)
			{
				IList<CoreLabel> sentence = sentences[sentNum].Get(typeof(CoreAnnotations.TokensAnnotation));
				IList<string[]> conllSentence = conllDocSentences[sentNum];
				IDictionary<int, ICollection<Mention>> mentionBeginOnly = Generics.NewHashMap();
				IDictionary<int, ICollection<Mention>> mentionEndOnly = Generics.NewHashMap();
				IDictionary<int, ICollection<Mention>> mentionBeginEnd = Generics.NewHashMap();
				for (int i = 0; i < sentence.Count; i++)
				{
					mentionBeginOnly[i] = new LinkedHashSet<Mention>();
					mentionEndOnly[i] = new LinkedHashSet<Mention>();
					mentionBeginEnd[i] = new LinkedHashSet<Mention>();
				}
				foreach (Mention m in orderedMentions[sentNum])
				{
					if (m.startIndex == m.endIndex - 1)
					{
						mentionBeginEnd[m.startIndex].Add(m);
					}
					else
					{
						mentionBeginOnly[m.startIndex].Add(m);
						mentionEndOnly[m.endIndex - 1].Add(m);
					}
				}
				for (int i_1 = 0; i_1 < sentence.Count; i_1++)
				{
					StringBuilder sb2 = new StringBuilder();
					foreach (Mention m_1 in mentionBeginOnly[i_1])
					{
						if (sb2.Length > 0)
						{
							sb2.Append("|");
						}
						int corefClusterId = (gold) ? m_1.goldCorefClusterID : m_1.corefClusterID;
						sb2.Append("(").Append(corefClusterId);
					}
					foreach (Mention m_2 in mentionBeginEnd[i_1])
					{
						if (sb2.Length > 0)
						{
							sb2.Append("|");
						}
						int corefClusterId = (gold) ? m_2.goldCorefClusterID : m_2.corefClusterID;
						sb2.Append("(").Append(corefClusterId).Append(")");
					}
					foreach (Mention m_3 in mentionEndOnly[i_1])
					{
						if (sb2.Length > 0)
						{
							sb2.Append("|");
						}
						int corefClusterId = (gold) ? m_3.goldCorefClusterID : m_3.corefClusterID;
						sb2.Append(corefClusterId).Append(")");
					}
					if (sb2.Length == 0)
					{
						sb2.Append("-");
					}
					string[] columns = conllSentence[i_1];
					for (int j = 0; j < columns.Length - 1; j++)
					{
						string column = columns[j];
						sb.Append(column).Append("\t");
					}
					sb.Append(sb2).Append("\n");
				}
				sb.Append("\n");
			}
			sb.Append("#end document").Append("\n");
			return sb.ToString();
		}
	}
}
