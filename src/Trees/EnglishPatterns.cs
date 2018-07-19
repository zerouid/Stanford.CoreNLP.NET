using Edu.Stanford.Nlp.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// This class contains some English String or Tregex regular expression
	/// patterns.
	/// </summary>
	/// <remarks>
	/// This class contains some English String or Tregex regular expression
	/// patterns. They originated in other classes like
	/// EnglishGrammaticalRelations, but were collected here so that they
	/// could be used without having to load large classes (which we might want
	/// to have parallel versions of).
	/// Some are just stored here as String objects, since they are often used as
	/// sub-patterns inside larger patterns.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class EnglishPatterns
	{
		public static readonly string[] copularVerbs = new string[] { "be", "being", "been", "am", "are", "r", "is", "ai", "was", "were", "'m", "m", "'re", "'s", "s", "`s", "art", "ar", "wase" };

		public static readonly string[] beGetVerbs = new string[] { "be", "being", "been", "am", "are", "r", "is", "ai", "was", "were", "'m", "m", "'re", "'s", "s", "`s", "art", "ar", "wase", "get", "getting", "gets", "got", "gotten" };

		public const string timeWordRegex = "/^(?i:Mondays?|Tuesdays?|Wednesdays?|Thursdays?|Fridays?|Saturdays?|Sundays?|years?|months?|weeks?|days?|mornings?|evenings?|nights?|January|Jan\\.|February|Feb\\.|March|Mar\\.|April|Apr\\.|May|June|July|August|Aug\\.|September|Sept\\.|October|Oct\\.|November|Nov\\.|December|Dec\\.|today|yesterday|tomorrow|spring|summer|fall|autumn|winter)$/";

		public const string timeWordLotRegex = "/^(?i:Mondays?|Tuesdays?|Wednesdays?|Thursdays?|Fridays?|Saturdays?|Sundays?|years?|months?|weeks?|days?|mornings?|evenings?|nights?|January|Jan\\.|February|Feb\\.|March|Mar\\.|April|Apr\\.|May|June|July|August|Aug\\.|September|Sept\\.|October|Oct\\.|November|Nov\\.|December|Dec\\.|today|yesterday|tomorrow|spring|summer|fall|autumn|winter|lot)$/";

		public static readonly string copularWordRegex = "/^(?i:" + StringUtils.Join(copularVerbs, "|") + ")$/";

		public const string clausalComplementRegex = "/^(?i:seem|seems|seemed|seeming|resemble|resembles|resembled|resembling|become|becomes|became|becoming|remain|remains|remained|remaining)$/";

		public static readonly string passiveAuxWordRegex = "/^(?i:" + StringUtils.Join(beGetVerbs, "|") + ")$/";

		public const string beAuxiliaryRegex = "/^(?i:am|is|are|r|be|being|'s|'re|'m|was|were|been|s|ai|m|art|ar|wase)$/";

		public const string haveRegex = "/^(?i:have|had|has|having|'ve|ve|v|'d|d|hvae|hav|as)$/";

		public const string selfRegex = "/^(?i:myself|yourself|himself|herself|itself|ourselves|yourselves|themselves)$/";

		public const string xcompVerbRegex = "/^(?i:advise|advises|advised|advising|allow|allows|allowed|allowing|ask|asks|asked|asking|beg|begs|begged|begging|convice|convinces|convinced|convincing|demand|demands|demanded|demanding|desire|desires|desired|desiring|expect|expects|expected|expecting|encourage|encourages|encouraged|encouraging|force|forces|forced|forcing|implore|implores|implored|imploring|lobby|lobbies|lobbied|lobbying|order|orders|ordered|ordering|persuade|persuades|persuaded|persuading|pressure|pressures|pressured|pressuring|prompt|prompts|prompted|prompting|require|requires|required|requiring|tell|tells|told|telling|urge|urges|urged|urging)$/";

		public const string xcompNoObjVerbRegex = "/^(?i:advis|afford|allow|am$|appear|are$|ask|attempt|avoid|be$|bec[oa]m|beg[ia]n|believ|call|caus[ei]|ceas[ei]|choos[ei]|chose|claim|consider|continu|convinc|decid|decline|end|enjoy|expect|feel|felt|find|forb[ia]d|forc[ei]|forg[eo]t|found|going|gon|g[eo]t|happen|hat[ei]|ha[vds]|help|hesitat|hop[ei]|intend|instruct|invit|['i]s$|keep|kept|learn|leav[ei]|left|let|lik[ei]|look|lov[ei]|made|mak[ei]|manag|nam[ei]|need|offer|order|plan|pretend|proceed|promis|prov[ei]|rate|recommend|refus|regret|remember|requir|sa[iy]|seem|sound|start|stop|suggest|suppos|tell|tend|threaten|told|tr[yi]|turn|used|wan|was$|willing|wish)/";

		public const string ccompVerbRegex = "/^(?i:ask|asks|asked|asking|know|knows|knew|knowing|specify|specifies|specified|specifying|tell|tells|told|telling|understand|understands|understood|understanding|wonder|wonders|wondered|wondering)$/";

		public const string ccompObjVerbRegex = "/^(?i:tell|tells|told|telling)$/";

		public const string RelativizingWordRegex = "(?i:that|what|which|who|whom|whose)";

		public static readonly Pattern RelativizingWordPattern = Pattern.Compile(RelativizingWordRegex);

		public const string NpVSInfVerbsRegex = "(?i:acquiesce|submit|bow|defer|accede|succumb|yield|capitulate|despise|disdain|dislike|regret|like|love|enjoy|fear|hate|pledge|proceed|begin|start|commence|recommence|resume|undertake|ally|collaborate|collude|conspire|discriminate|legislate|partner|protest|rebel|retaliate|scheme|sin|befriend|continue|broadcast|cable|e-mail|fax|modem|netmail|phone|radio|relay|satellite|semaphore|sign|signal|telecast|telegraph|telephone|telex|wire|wireless|ache|crave|fall|hanker|hope|hunger|itch|long|lust|pine|pray|thirst|wish|yearn|dangle|hanker|lust|thirst|yearn|babble|bark|bawl|bellow|bleat|blubber|boom|bray|burble|bluster|cackle|call|carol|chant|chatter|chirp|chortle|chuckle|cluck|coo|croak|croon|crow|cry|drawl|drone|gabble|gasp|gibber|groan|growl|grumble|grunt|hiss|holler|hoot|howl|jabber|keen|lilt|lisp|mewl|moan|mumble|murmur|mutter|nasal|natter|pant|prattle|purr|quaver|rage|rant|rasp|roar|rumble|scream|screech|shout|shriek|sibilate|simper|sigh|sing|smatter|smile|snap|snarl|snivel|snuffle|splutter|squall|squawk|squeak|squeal|stammer|stemmer|stutter|thunder|tisk|trill|trumpet|twang|twitter|vociferate|wail|warble|wheeze|whimper|whine|whisper|whistle|witter|whoop|yammer|yap|yell|yelp|yodel|blare|gurgle|hum|neglect|fail|forego|forgo|flub|overleap|manage|omit|seem|appear|prove|manage|fail|flub|try|attempt|intend|enjoy|expect|wish|hope|intend|mean|plan|propose|think|aim|dream|imagine|yen)";

		private EnglishPatterns()
		{
		}
		// r is for texting r = are
		// private static final String stopKeepRegex = "/^(?i:stop|stops|stopped|stopping|keep|keeps|kept|keeping)$/";
		// A list of verbs with an xcomp as an argument
		// which don't require a NP before the xcomp.
		// A list of verbs where the answer to a question involving that
		// verb would be a ccomp.  For example, "I know when the train is
		// arriving."  What does the person know?
		// A subset of ccompVerbRegex where you could expect an object and
		// still have a ccomp.  For example, "They told me when ..." can
		// still have a ccomp.  "They know my order when ..." would not
		// expect a ccomp between "know" and the head of "when ..."
		// TODO: is there some better pattern to look for? We do not have tag information at this point
		// Lemmata of verbs with the argument structure NP V S-INF.
		// Extracted from VerbNet 3.2.
		// static constants
	}
}
