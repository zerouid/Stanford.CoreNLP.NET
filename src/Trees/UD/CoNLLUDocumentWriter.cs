using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.UD
{
	/// <author>Sebastian Schuster</author>
	public class CoNLLUDocumentWriter
	{
		private const string LrbPattern = "(?i)-LRB-";

		private const string RrbPattern = "(?i)-RRB-";

		public virtual string PrintSemanticGraph(SemanticGraph sg)
		{
			return PrintSemanticGraph(sg, true);
		}

		public virtual string PrintSemanticGraph(SemanticGraph sg, bool unescapeParenthesis)
		{
			bool isTree = SemanticGraphUtils.IsTree(sg);
			StringBuilder sb = new StringBuilder();
			/* Print comments. */
			foreach (string comment in sg.GetComments())
			{
				sb.Append(comment).Append("\n");
			}
			foreach (IndexedWord token in sg.VertexListSorted())
			{
				/* Check for multiword tokens. */
				if (token.ContainsKey(typeof(CoreAnnotations.CoNLLUTokenSpanAnnotation)))
				{
					IntPair tokenSpan = token.Get(typeof(CoreAnnotations.CoNLLUTokenSpanAnnotation));
					if (tokenSpan.GetSource() == token.Index())
					{
						string range = string.Format("%d-%d", tokenSpan.GetSource(), tokenSpan.GetTarget());
						sb.Append(string.Format("%s\t%s\t_\t_\t_\t_\t_\t_\t_\t_%n", range, token.OriginalText()));
					}
				}
				/* Try to find main governor and additional dependencies. */
				string govIdx = null;
				GrammaticalRelation reln = null;
				Dictionary<string, string> enhancedDependencies = new Dictionary<string, string>();
				foreach (IndexedWord parent in sg.GetParents(token))
				{
					SemanticGraphEdge edge = sg.GetEdge(parent, token);
					if (govIdx == null && !edge.IsExtra())
					{
						govIdx = parent.ToCopyIndex();
						reln = edge.GetRelation();
					}
					enhancedDependencies[parent.ToCopyIndex()] = edge.GetRelation().ToString();
				}
				string additionalDepsString = isTree ? "_" : CoNLLUUtils.ToExtraDepsString(enhancedDependencies);
				string word = token.Word();
				string featuresString = CoNLLUUtils.ToFeatureString(token.Get(typeof(CoreAnnotations.CoNLLUFeats)));
				string pos = token.GetString<CoreAnnotations.PartOfSpeechAnnotation>("_");
				string upos = token.GetString<CoreAnnotations.CoarseTagAnnotation>("_");
				string misc = token.GetString<CoreAnnotations.CoNLLUMisc>("_");
				string lemma = token.GetString<CoreAnnotations.LemmaAnnotation>("_");
				string relnName = reln == null ? "_" : reln.ToString();
				/* Root. */
				if (govIdx == null && sg.GetRoots().Contains(token))
				{
					govIdx = "0";
					relnName = GrammaticalRelation.Root.ToString();
					additionalDepsString = isTree ? "_" : "0:" + relnName;
				}
				else
				{
					if (govIdx == null)
					{
						govIdx = "_";
						relnName = "_";
					}
				}
				if (unescapeParenthesis)
				{
					word = word.ReplaceAll(LrbPattern, "(");
					word = word.ReplaceAll(RrbPattern, ")");
					lemma = lemma.ReplaceAll(LrbPattern, "(");
					lemma = lemma.ReplaceAll(RrbPattern, ")");
				}
				sb.Append(string.Format("%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s%n", token.ToCopyIndex(), word, lemma, upos, pos, featuresString, govIdx, relnName, additionalDepsString, misc));
			}
			sb.Append("\n");
			return sb.ToString();
		}

		/// <summary>
		/// Outputs a partial CONLL-U file with token information (form, lemma, POS)
		/// but without any dependency information.
		/// </summary>
		/// <param name="sentence"/>
		/// <returns/>
		public virtual string PrintPOSAnnotations(ICoreMap sentence)
		{
			StringBuilder sb = new StringBuilder();
			foreach (CoreLabel token in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				string upos = token.GetString<CoreAnnotations.CoarseTagAnnotation>("_");
				string lemma = token.GetString<CoreAnnotations.LemmaAnnotation>("_");
				string pos = token.GetString<CoreAnnotations.PartOfSpeechAnnotation>("_");
				string featuresString = CoNLLUUtils.ToFeatureString(token.Get(typeof(CoreAnnotations.CoNLLUFeats)));
				string misc = token.GetString<CoreAnnotations.CoNLLUMisc>("_");
				sb.Append(string.Format("%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s%n", token.Index(), token.Word(), lemma, upos, pos, featuresString, "_", "_", "_", misc));
			}
			sb.Append("\n");
			return sb.ToString();
		}
	}
}
