


namespace Edu.Stanford.Nlp.Dcoref
{
	public class SieveOptions
	{
		public bool DoPronoun;

		public bool UseIncompatibles;

		public bool USE_iwithini;

		public bool UseApposition;

		public bool UsePredicatenominatives;

		public bool UseAcronym;

		public bool UseRelativepronoun;

		public bool UseRoleapposition;

		public bool UseExactstringmatch;

		public bool UseNameMatch;

		public bool UseInclusionHeadmatch;

		public bool UseRelaxedHeadmatch;

		public bool UseIncompatibleModifier;

		public bool UseDemonym;

		public bool UseWordsInclusion;

		public bool UseRoleSkip;

		public bool UseRelaxedExactstringmatch;

		public bool UseAttributesAgree;

		public bool UseWnHypernym;

		public bool UseWnSynonym;

		public bool UseDifferentLocation;

		public bool UseNumberInMention;

		public bool UseProperheadAtLast;

		public bool UseAlias;

		public bool UseSlotMatch;

		public bool UseDiscoursematch;

		public bool UseDistance;

		public bool UseNumberAnimacyNeAgree;

		public bool UseCorefDict;

		public override string ToString()
		{
			StringBuilder os = new StringBuilder();
			os.Append("{");
			if (DoPronoun)
			{
				os.Append("DO_PRONOUN");
			}
			if (UseIncompatibles)
			{
				os.Append(", USE_INCOMPATIBLES");
			}
			if (USE_iwithini)
			{
				os.Append(", USE_iwithini");
			}
			if (UseApposition)
			{
				os.Append(", USE_APPOSITION");
			}
			if (UsePredicatenominatives)
			{
				os.Append(", USE_PREDICATENOMINATIVES");
			}
			if (UseAcronym)
			{
				os.Append(", USE_ACRONYM");
			}
			if (UseRelativepronoun)
			{
				os.Append(", USE_RELATIVEPRONOUN");
			}
			if (UseRoleapposition)
			{
				os.Append(", USE_ROLEAPPOSITION");
			}
			if (UseExactstringmatch)
			{
				os.Append(", USE_EXACTSTRINGMATCH");
			}
			if (UseNameMatch)
			{
				os.Append(", USE_NAME_MATCH");
			}
			if (UseInclusionHeadmatch)
			{
				os.Append(", USE_INCLUSION_HEADMATCH");
			}
			if (UseRelaxedHeadmatch)
			{
				os.Append(", USE_RELAXED_HEADMATCH");
			}
			if (UseIncompatibleModifier)
			{
				os.Append(", USE_INCOMPATIBLE_MODIFIER");
			}
			if (UseDemonym)
			{
				os.Append(", USE_DEMONYM");
			}
			if (UseWordsInclusion)
			{
				os.Append(", USE_WORDS_INCLUSION");
			}
			if (UseRoleSkip)
			{
				os.Append(", USE_ROLE_SKIP");
			}
			if (UseRelaxedExactstringmatch)
			{
				os.Append(", USE_RELAXED_EXACTSTRINGMATCH");
			}
			if (UseAttributesAgree)
			{
				os.Append(", USE_ATTRIBUTES_AGREE");
			}
			if (UseWnHypernym)
			{
				os.Append(", USE_WN_HYPERNYM");
			}
			if (UseWnSynonym)
			{
				os.Append(", USE_WN_SYNONYM");
			}
			if (UseDifferentLocation)
			{
				os.Append(", USE_DIFFERENT_LOCATION");
			}
			if (UseNumberInMention)
			{
				os.Append(", USE_NUMBER_IN_MENTION");
			}
			if (UseProperheadAtLast)
			{
				os.Append(", USE_PROPERHEAD_AT_LAST");
			}
			if (UseAlias)
			{
				os.Append(", USE_ALIAS");
			}
			if (UseSlotMatch)
			{
				os.Append(", USE_SLOT_MATCH");
			}
			if (UseDiscoursematch)
			{
				os.Append(", USE_DISCOURSEMATCH");
			}
			if (UseDistance)
			{
				os.Append(", USE_DISTANCE");
			}
			if (UseNumberAnimacyNeAgree)
			{
				os.Append(", USE_NUMBER_ANIMACY_NE_AGREE");
			}
			if (UseCorefDict)
			{
				os.Append(", USE_COREF_DICT");
			}
			os.Append("}");
			return os.ToString();
		}

		public SieveOptions()
		{
			DoPronoun = false;
			UseIncompatibles = true;
			USE_iwithini = false;
			UseApposition = false;
			UsePredicatenominatives = false;
			UseAcronym = false;
			UseRelativepronoun = false;
			UseRoleapposition = false;
			UseExactstringmatch = false;
			UseInclusionHeadmatch = false;
			UseRelaxedHeadmatch = false;
			UseIncompatibleModifier = false;
			UseDemonym = false;
			UseWordsInclusion = false;
			UseRoleSkip = false;
			UseRelaxedExactstringmatch = false;
			UseAttributesAgree = false;
			UseWnHypernym = false;
			UseWnSynonym = false;
			UseDifferentLocation = false;
			UseNumberInMention = false;
			UseProperheadAtLast = false;
			UseAlias = false;
			UseSlotMatch = false;
			UseDiscoursematch = false;
			UseDistance = false;
			UseNumberAnimacyNeAgree = false;
			UseCorefDict = false;
		}
	}
}
