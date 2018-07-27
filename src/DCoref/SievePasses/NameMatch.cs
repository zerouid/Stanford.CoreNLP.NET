using System.Collections.Generic;
using Edu.Stanford.Nlp.Dcoref;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Dcoref.Sievepasses
{
	/// <summary>Use name matcher - match full names only</summary>
	/// <author>Angel Chang</author>
	public class NameMatch : DeterministicCorefSieve
	{
		protected internal IMentionMatcher mentionMatcher = null;

		protected internal int minTokens = 0;

		protected internal bool ignoreGender = true;

		private ICollection<string> supportedNerTypes = Generics.NewHashSet();

		public NameMatch()
			: base()
		{
			// Minimum number of tokens in name before attempting match
			flags.USE_iwithini = true;
			flags.UseNameMatch = true;
			// Stick with mainly person and organizations
			supportedNerTypes.Add("ORG");
			supportedNerTypes.Add("ORGANIZATION");
			supportedNerTypes.Add("PER");
			supportedNerTypes.Add("PERSON");
			supportedNerTypes.Add("MISC");
		}

		public override void Init(Properties props)
		{
			// TODO: Can get custom mention matcher
			mentionMatcher = ReflectionLoading.LoadByReflection("edu.stanford.nlp.kbp.entitylinking.classify.namematcher.RuleBasedNameMatcher", "dcoref.mentionMatcher", props);
		}

		private static bool IsNamedMention(Mention m, Dictionaries dict, ICollection<Mention> roleSet)
		{
			return m.mentionType == Dictionaries.MentionType.Proper;
		}

		public override bool CheckEntityMatch(Document document, CorefCluster mentionCluster, CorefCluster potentialAntecedent, Dictionaries dict, ICollection<Mention> roleSet)
		{
			bool matched = false;
			Mention mainMention = mentionCluster.GetRepresentativeMention();
			Mention antMention = potentialAntecedent.GetRepresentativeMention();
			// Check if the representative mentions are compatible
			if (IsNamedMention(mainMention, dict, roleSet) && IsNamedMention(antMention, dict, roleSet))
			{
				if (mainMention.originalSpan.Count > minTokens || antMention.originalSpan.Count > minTokens)
				{
					if (Rules.EntityAttributesAgree(mentionCluster, potentialAntecedent, ignoreGender))
					{
						if (supportedNerTypes.Contains(mainMention.nerString) || supportedNerTypes.Contains(antMention.nerString))
						{
							matched = mentionMatcher.IsCompatible(mainMention, antMention);
							if (matched != null)
							{
								//Redwood.log("Match '" + mainMention + "' with '" + antMention + "' => " + matched);
								if (!matched)
								{
									document.AddIncompatible(mainMention, antMention);
								}
							}
							else
							{
								matched = false;
							}
						}
					}
				}
			}
			return matched;
		}
	}
}
