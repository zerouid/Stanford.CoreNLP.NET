using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	[System.Serializable]
	public class OracleSieve : Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Coref.Hybrid.Sieve.OracleSieve));

		private const long serialVersionUID = 3510248899162246138L;

		public OracleSieve(Properties props, string sievename)
			: base(props, sievename)
		{
			this.classifierType = Sieve.ClassifierType.Oracle;
		}

		/// <exception cref="System.Exception"/>
		public override void FindCoreferentAntecedent(Mention m, int mIdx, Document document, Dictionaries dict, Properties props, StringBuilder sbLog)
		{
			for (int distance = 0; distance <= m.sentNum; distance++)
			{
				IList<Mention> candidates = document.predictedMentions[m.sentNum - distance];
				foreach (Mention candidate in candidates)
				{
					if (!MatchedMentionType(candidate, aTypeStr) || !MatchedMentionType(m, mTypeStr))
					{
						continue;
					}
					//        if(!options.mType.contains(m.mentionType) || !options.aType.contains(candidate.mentionType)) continue;
					if (candidate == m)
					{
						continue;
					}
					if (distance == 0 && m.AppearEarlierThan(candidate))
					{
						continue;
					}
					// ignore cataphora
					if (Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve.IsReallyCoref(document, m.mentionID, candidate.mentionID))
					{
						if (m.mentionType == Dictionaries.MentionType.List)
						{
							log.Info("LIST MATCHING MENTION : " + m.SpanToString() + "\tANT: " + candidate.SpanToString());
						}
						Edu.Stanford.Nlp.Coref.Hybrid.Sieve.Sieve.Merge(document, m.mentionID, candidate.mentionID);
						return;
					}
				}
			}
		}
	}
}
