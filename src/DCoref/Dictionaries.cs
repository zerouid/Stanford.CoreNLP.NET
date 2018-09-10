using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Dcoref
{
	/// <summary>
	/// Provides accessors for various grammatical, semantic, and world knowledge
	/// lexicons and word lists primarily used by the Sieve coreference system,
	/// but sometimes also drawn on from other code.
	/// </summary>
	/// <remarks>
	/// Provides accessors for various grammatical, semantic, and world knowledge
	/// lexicons and word lists primarily used by the Sieve coreference system,
	/// but sometimes also drawn on from other code.
	/// The source of the dictionaries on Stanford NLP machines is
	/// /u/nlp/data/coref/gazetteers/dcoref/ . In models jars, they live in
	/// edu/stanford/nlp/models/dcoref .
	/// </remarks>
	public class Dictionaries
	{
		[System.Serializable]
		public sealed class MentionType
		{
			public static readonly Dictionaries.MentionType Pronominal = new Dictionaries.MentionType(1);

			public static readonly Dictionaries.MentionType Nominal = new Dictionaries.MentionType(3);

			public static readonly Dictionaries.MentionType Proper = new Dictionaries.MentionType(4);

			public static readonly Dictionaries.MentionType List = new Dictionaries.MentionType(2);

			/// <summary>
			/// A higher representativeness means that this type of mention is more preferred for choosing
			/// the representative mention.
			/// </summary>
			/// <remarks>
			/// A higher representativeness means that this type of mention is more preferred for choosing
			/// the representative mention. See
			/// <see cref="Mention.MoreRepresentativeThan(Mention)"/>
			/// .
			/// </remarks>
			public readonly int representativeness;

			internal MentionType(int representativeness)
			{
				this.representativeness = representativeness;
			}
		}

		public enum Gender
		{
			Male,
			Female,
			Neutral,
			Unknown
		}

		public enum Number
		{
			Singular,
			Plural,
			Unknown
		}

		public enum Animacy
		{
			Animate,
			Inanimate,
			Unknown
		}

		public enum Person
		{
			I,
			You,
			He,
			She,
			We,
			They,
			It,
			Unknown
		}

		public readonly ICollection<string> reportVerb = Generics.NewHashSet(Arrays.AsList("accuse", "acknowledge", "add", "admit", "advise", "agree", "alert", "allege", "announce", "answer", "apologize", "argue", "ask", "assert", "assure", "beg", "blame"
			, "boast", "caution", "charge", "cite", "claim", "clarify", "command", "comment", "compare", "complain", "concede", "conclude", "confirm", "confront", "congratulate", "contend", "contradict", "convey", "counter", "criticize", "debate", "decide"
			, "declare", "defend", "demand", "demonstrate", "deny", "describe", "determine", "disagree", "disclose", "discount", "discover", "discuss", "dismiss", "dispute", "disregard", "doubt", "emphasize", "encourage", "endorse", "equate", "estimate"
			, "expect", "explain", "express", "extoll", "fear", "feel", "find", "forbid", "forecast", "foretell", "forget", "gather", "guarantee", "guess", "hear", "hint", "hope", "illustrate", "imagine", "imply", "indicate", "inform", "insert", "insist"
			, "instruct", "interpret", "interview", "invite", "issue", "justify", "learn", "maintain", "mean", "mention", "negotiate", "note", "observe", "offer", "oppose", "order", "persuade", "pledge", "point", "point out", "praise", "pray", "predict"
			, "prefer", "present", "promise", "prompt", "propose", "protest", "prove", "provoke", "question", "quote", "raise", "rally", "read", "reaffirm", "realise", "realize", "rebut", "recall", "reckon", "recommend", "refer", "reflect", "refuse", "refute"
			, "reiterate", "reject", "relate", "remark", "remember", "remind", "repeat", "reply", "report", "request", "respond", "restate", "reveal", "rule", "say", "see", "show", "signal", "sing", "slam", "speculate", "spoke", "spread", "state", "stipulate"
			, "stress", "suggest", "support", "suppose", "surmise", "suspect", "swear", "teach", "tell", "testify", "think", "threaten", "told", "uncover", "underline", "underscore", "urge", "voice", "vow", "warn", "welcome", "wish", "wonder", "worry", 
			"write"));

		public readonly ICollection<string> reportNoun = Generics.NewHashSet(Arrays.AsList("acclamation", "account", "accusation", "acknowledgment", "address", "addressing", "admission", "advertisement", "advice", "advisory", "affidavit", "affirmation"
			, "alert", "allegation", "analysis", "anecdote", "annotation", "announcement", "answer", "antiphon", "apology", "applause", "appreciation", "argument", "arraignment", "article", "articulation", "aside", "assertion", "asseveration", "assurance"
			, "attestation", "attitude", "averment", "avouchment", "avowal", "axiom", "backcap", "band-aid", "basic", "belief", "bestowal", "bill", "blame", "blow-by-blow", "bomb", "book", "bow", "break", "breakdown", "brief", "briefing", "broadcast", 
			"broadcasting", "bulletin", "buzz", "cable", "calendar", "call", "canard", "canon", "card", "cause", "censure", "certification", "characterization", "charge", "chat", "chatter", "chitchat", "chronicle", "chronology", "citation", "claim", "clarification"
			, "close", "cognizance", "comeback", "comment", "commentary", "communication", "communique", "composition", "concept", "concession", "conference", "confession", "confirmation", "conjecture", "connotation", "construal", "construction", "consultation"
			, "contention", "contract", "convention", "conversation", "converse", "conviction", "counterclaim", "credenda", "creed", "critique", "cry", "declaration", "defense", "definition", "delineation", "delivery", "demonstration", "denial", "denotation"
			, "depiction", "deposition", "description", "detail", "details", "detention", "dialogue", "diction", "dictum", "digest", "directive", "disclosure", "discourse", "discovery", "discussion", "dispatch", "display", "disquisition", "dissemination"
			, "dissertation", "divulgence", "dogma", "editorial", "ejaculation", "emphasis", "enlightenment", "enunciation", "essay", "evidence", "examination", "example", "excerpt", "exclamation", "excuse", "execution", "exegesis", "explanation", "explication"
			, "exposing", "exposition", "expounding", "expression", "eye-opener", "feedback", "fiction", "findings", "fingerprint", "flash", "formulation", "fundamental", "gift", "gloss", "goods", "gospel", "gossip", "gratitude", "greeting", "guarantee"
			, "hail", "hailing", "handout", "hash", "headlines", "hearing", "hearsay", "ideas", "idiom", "illustration", "impeachment", "implantation", "implication", "imputation", "incrimination", "indication", "indoctrination", "inference", "info", "information"
			, "innuendo", "insinuation", "insistence", "instruction", "intelligence", "interpretation", "interview", "intimation", "intonation", "issue", "item", "itemization", "justification", "key", "knowledge", "leak", "letter", "locution", "manifesto"
			, "meaning", "meeting", "mention", "message", "missive", "mitigation", "monograph", "motive", "murmur", "narration", "narrative", "news", "nod", "note", "notice", "notification", "oath", "observation", "okay", "opinion", "oral", "outline", 
			"paper", "parley", "particularization", "phrase", "phraseology", "phrasing", "picture", "piece", "pipeline", "pitch", "plea", "plot", "portraiture", "portrayal", "position", "potboiler", "prating", "precept", "prediction", "presentation", "presentment"
			, "principle", "proclamation", "profession", "program", "promulgation", "pronouncement", "pronunciation", "propaganda", "prophecy", "proposal", "proposition", "prosecution", "protestation", "publication", "publicity", "publishing", "quotation"
			, "ratification", "reaction", "reason", "rebuttal", "receipt", "recital", "recitation", "recognition", "record", "recount", "recountal", "refutation", "regulation", "rehearsal", "rejoinder", "relation", "release", "remark", "rendition", "repartee"
			, "reply", "report", "reporting", "representation", "resolution", "response", "result", "retort", "return", "revelation", "review", "rule", "rumble", "rumor", "rundown", "saying", "scandal", "scoop", "scuttlebutt", "sense", "showing", "sign"
			, "signature", "significance", "sketch", "skinny", "solution", "speaking", "specification", "speech", "statement", "story", "study", "style", "suggestion", "summarization", "summary", "summons", "tale", "talk", "talking", "tattle", "telecast"
			, "telegram", "telling", "tenet", "term", "testimonial", "testimony", "text", "theme", "thesis", "tract", "tractate", "tradition", "translation", "treatise", "utterance", "vent", "ventilation", "verbalization", "version", "vignette", "vindication"
			, "warning", "warrant", "whispering", "wire", "word", "work", "writ", "write-up", "writeup", "writing", "acceptance", "complaint", "concern", "disappointment", "disclose", "estimate", "laugh", "pleasure", "regret", "resentment", "view"));

		public readonly ICollection<string> nonWords = Generics.NewHashSet(Arrays.AsList("mm", "hmm", "ahem", "um"));

		public readonly ICollection<string> copulas = Generics.NewHashSet(Arrays.AsList("is", "are", "were", "was", "be", "been", "become", "became", "becomes", "seem", "seemed", "seems", "remain", "remains", "remained"));

		public readonly ICollection<string> quantifiers = Generics.NewHashSet(Arrays.AsList("not", "every", "any", "none", "everything", "anything", "nothing", "all", "enough"));

		public readonly ICollection<string> parts = Generics.NewHashSet(Arrays.AsList("half", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "hundred", "thousand", "million", "billion", "tens", "dozens", "hundreds", "thousands"
			, "millions", "billions", "group", "groups", "bunch", "number", "numbers", "pinch", "amount", "amount", "total", "all", "mile", "miles", "pounds"));

		public readonly ICollection<string> temporals = Generics.NewHashSet(Arrays.AsList("second", "minute", "hour", "day", "week", "month", "year", "decade", "century", "millennium", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday"
			, "sunday", "now", "yesterday", "tomorrow", "age", "time", "era", "epoch", "morning", "evening", "day", "night", "noon", "afternoon", "semester", "trimester", "quarter", "term", "winter", "spring", "summer", "fall", "autumn", "season", "january"
			, "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december"));

		public readonly ICollection<string> femalePronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "her", "hers", "herself", "she" }));

		public readonly ICollection<string> malePronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "he", "him", "himself", "his" }));

		public readonly ICollection<string> neutralPronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "it", "its", "itself", "where", "here", "there", "which" }));

		public readonly ICollection<string> possessivePronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "my", "your", "his", "her", "its", "our", "their", "whose" }));

		public readonly ICollection<string> otherPronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "who", "whom", "whose", "where", "when", "which" }));

		public readonly ICollection<string> thirdPersonPronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "he", "him", "himself", "his", "she", "her", "herself", "hers", "her", "it", "itself", "its", "one", "oneself", "one's", "they", "them"
			, "themself", "themselves", "theirs", "their", "they", "them", "'em", "themselves" }));

		public readonly ICollection<string> secondPersonPronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "you", "yourself", "yours", "your", "yourselves" }));

		public readonly ICollection<string> firstPersonPronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "i", "me", "myself", "mine", "my", "we", "us", "ourself", "ourselves", "ours", "our" }));

		public readonly ICollection<string> moneyPercentNumberPronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "it", "its" }));

		public readonly ICollection<string> dateTimePronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "when" }));

		public readonly ICollection<string> organizationPronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "it", "its", "they", "their", "them", "which" }));

		public readonly ICollection<string> locationPronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "it", "its", "where", "here", "there" }));

		public readonly ICollection<string> inanimatePronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "it", "itself", "its", "where", "when" }));

		public readonly ICollection<string> animatePronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "i", "me", "myself", "mine", "my", "we", "us", "ourself", "ourselves", "ours", "our", "you", "yourself", "yours", "your", "yourselves", "he"
			, "him", "himself", "his", "she", "her", "herself", "hers", "her", "one", "oneself", "one's", "they", "them", "themself", "themselves", "theirs", "their", "they", "them", "'em", "themselves", "who", "whom", "whose" }));

		public readonly ICollection<string> indefinitePronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "another", "anybody", "anyone", "anything", "each", "either", "enough", "everybody", "everyone", "everything", "less", "little", "much"
			, "neither", "no one", "nobody", "nothing", "one", "other", "plenty", "somebody", "someone", "something", "both", "few", "fewer", "many", "others", "several", "all", "any", "more", "most", "none", "some", "such" }));

		public readonly ICollection<string> relativePronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "that", "who", "which", "whom", "where", "whose" }));

		public readonly ICollection<string> GPEPronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "it", "itself", "its", "they", "where" }));

		public readonly ICollection<string> pluralPronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "we", "us", "ourself", "ourselves", "ours", "our", "yourself", "yourselves", "they", "them", "themself", "themselves", "theirs", "their" })
			);

		public readonly ICollection<string> singularPronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "i", "me", "myself", "mine", "my", "yourself", "he", "him", "himself", "his", "she", "her", "herself", "hers", "her", "it", "itself", "its"
			, "one", "oneself", "one's" }));

		public readonly ICollection<string> facilityVehicleWeaponPronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "it", "itself", "its", "they", "where" }));

		public readonly ICollection<string> miscPronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "it", "itself", "its", "they", "where" }));

		public readonly ICollection<string> reflexivePronouns = Generics.NewHashSet(Arrays.AsList(new string[] { "myself", "yourself", "yourselves", "himself", "herself", "itself", "ourselves", "themselves", "oneself" }));

		public readonly ICollection<string> transparentNouns = Generics.NewHashSet(Arrays.AsList(new string[] { "bunch", "group", "breed", "class", "ilk", "kind", "half", "segment", "top", "bottom", "glass", "bottle", "box", "cup", "gem", "idiot", "unit"
			, "part", "stage", "name", "division", "label", "group", "figure", "series", "member", "members", "first", "version", "site", "side", "role", "largest", "title", "fourth", "third", "second", "number", "place", "trio", "two", "one", "longest"
			, "highest", "shortest", "head", "resident", "collection", "result", "last" }));

		public readonly ICollection<string> stopWords = Generics.NewHashSet(Arrays.AsList(new string[] { "a", "an", "the", "of", "at", "on", "upon", "in", "to", "from", "out", "as", "so", "such", "or", "and", "those", "this", "these", "that", "for", 
			",", "is", "was", "am", "are", "'s", "been", "were" }));

		public readonly ICollection<string> notOrganizationPRP = Generics.NewHashSet(Arrays.AsList(new string[] { "i", "me", "myself", "mine", "my", "yourself", "he", "him", "himself", "his", "she", "her", "herself", "hers", "here" }));

		public readonly ICollection<string> quantifiers2 = Generics.NewHashSet(Arrays.AsList("all", "both", "neither", "either"));

		public readonly ICollection<string> determiners = Generics.NewHashSet(Arrays.AsList("the", "this", "that", "these", "those", "his", "her", "my", "your", "their", "our"));

		public readonly ICollection<string> negations = Generics.NewHashSet(Arrays.AsList("n't", "not", "nor", "neither", "never", "no", "non", "any", "none", "nobody", "nothing", "nowhere", "nearly", "almost", "if", "false", "fallacy", "unsuccessfully"
			, "unlikely", "impossible", "improbable", "uncertain", "unsure", "impossibility", "improbability", "cancellation", "breakup", "lack", "long-stalled", "end", "rejection", "failure", "avoid", "bar", "block", "break", "cancel", "cease", "cut", 
			"decline", "deny", "deprive", "destroy", "excuse", "fail", "forbid", "forestall", "forget", "halt", "lose", "nullify", "prevent", "refrain", "reject", "rebut", "remain", "refuse", "stop", "suspend", "ward"));

		public readonly ICollection<string> neg_relations = Generics.NewHashSet(Arrays.AsList("nmod:without", "acl:without", "advcl:without", "nmod:except", "acl:except", "advcl:except", "nmod:excluding", "acl:excluding", "advcl:excluding", "nmod:if"
			, "acl:if", "advcl:if", "nmod:whether", "acl:whether", "advcl:whether", "nmod:away_from", "acl:away_from", "advcl:away_fom", "nmod:instead_of", "acl:instead_of", "advcl:instead_of"));

		public readonly ICollection<string> modals = Generics.NewHashSet(Arrays.AsList("can", "could", "may", "might", "must", "should", "would", "seem", "able", "apparently", "necessarily", "presumably", "probably", "possibly", "reportedly", "supposedly"
			, "inconceivable", "chance", "impossibility", "improbability", "encouragement", "improbable", "impossible", "likely", "necessary", "probable", "possible", "uncertain", "unlikely", "unsure", "likelihood", "probability", "possibility", "eventual"
			, "hypothetical", "presumed", "supposed", "reported", "apparent"));

		public readonly ICollection<string> personPronouns = Generics.NewHashSet();

		public readonly ICollection<string> allPronouns = Generics.NewHashSet();

		public readonly IDictionary<string, string> statesAbbreviation = Generics.NewHashMap();

		private readonly IDictionary<string, ICollection<string>> demonyms = Generics.NewHashMap();

		public readonly ICollection<string> demonymSet = Generics.NewHashSet();

		private readonly ICollection<string> adjectiveNation = Generics.NewHashSet();

		public readonly ICollection<string> countries = Generics.NewHashSet();

		public readonly ICollection<string> statesAndProvinces = Generics.NewHashSet();

		public readonly ICollection<string> neutralWords = Generics.NewHashSet();

		public readonly ICollection<string> femaleWords = Generics.NewHashSet();

		public readonly ICollection<string> maleWords = Generics.NewHashSet();

		public readonly ICollection<string> pluralWords = Generics.NewHashSet();

		public readonly ICollection<string> singularWords = Generics.NewHashSet();

		public readonly ICollection<string> inanimateWords = Generics.NewHashSet();

		public readonly ICollection<string> animateWords = Generics.NewHashSet();

		public readonly IDictionary<IList<string>, Dictionaries.Gender> genderNumber = Generics.NewHashMap();

		public readonly List<ICounter<Pair<string, string>>> corefDict = new List<ICounter<Pair<string, string>>>(4);

		public readonly ICounter<Pair<string, string>> corefDictPMI = new ClassicCounter<Pair<string, string>>();

		public readonly IDictionary<string, ICounter<string>> NE_signatures = Generics.NewHashMap();

		private void SetPronouns()
		{
			foreach (string s in animatePronouns)
			{
				personPronouns.Add(s);
			}
			Sharpen.Collections.AddAll(allPronouns, firstPersonPronouns);
			Sharpen.Collections.AddAll(allPronouns, secondPersonPronouns);
			Sharpen.Collections.AddAll(allPronouns, thirdPersonPronouns);
			Sharpen.Collections.AddAll(allPronouns, otherPronouns);
			Sharpen.Collections.AddAll(stopWords, allPronouns);
		}

		/// <summary>
		/// The format of each line of this file is
		/// fullStateName ( TAB  abbrev )
		/// The file is cased and checked cased.
		/// </summary>
		/// <remarks>
		/// The format of each line of this file is
		/// fullStateName ( TAB  abbrev )
		/// The file is cased and checked cased.
		/// The result is: statesAbbreviation is a hash from each abbrev to the fullStateName.
		/// </remarks>
		public virtual void LoadStateAbbreviation(string statesFile)
		{
			BufferedReader reader = null;
			try
			{
				reader = IOUtils.ReaderFromString(statesFile);
				for (string line; (line = reader.ReadLine()) != null; )
				{
					string[] tokens = line.Split("\t");
					foreach (string token in tokens)
					{
						statesAbbreviation[token] = tokens[0];
					}
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(reader);
			}
		}

		/// <summary>If the input string is an abbreviation of a U.S.</summary>
		/// <remarks>
		/// If the input string is an abbreviation of a U.S. state name
		/// or the canonical name, the canonical name is returned.
		/// Otherwise, null is returned.
		/// </remarks>
		/// <param name="name">Is treated as a cased string. ME != me</param>
		public virtual string LookupCanonicalAmericanStateName(string name)
		{
			return statesAbbreviation[name];
		}

		/// <summary>
		/// The format of the demonyms file is
		/// countryCityOrState ( TAB demonym )
		/// Lines starting with # are ignored
		/// The file is cased but stored in in-memory data structures uncased.
		/// </summary>
		/// <remarks>
		/// The format of the demonyms file is
		/// countryCityOrState ( TAB demonym )
		/// Lines starting with # are ignored
		/// The file is cased but stored in in-memory data structures uncased.
		/// The results are:
		/// demonyms is a hash from each country (etc.) to a set of demonymic Strings;
		/// adjectiveNation is a set of demonymic Strings;
		/// demonymSet has all country (etc.) names and all demonymic Strings.
		/// </remarks>
		private void LoadDemonymLists(string demonymFile)
		{
			BufferedReader reader = null;
			try
			{
				reader = IOUtils.ReaderFromString(demonymFile);
				for (string line; (line = reader.ReadLine()) != null; )
				{
					line = line.ToLower(Locale.English);
					string[] tokens = line.Split("\t");
					if (tokens[0].StartsWith("#"))
					{
						continue;
					}
					ICollection<string> set = Generics.NewHashSet();
					foreach (string s in tokens)
					{
						set.Add(s);
						demonymSet.Add(s);
					}
					demonyms[tokens[0]] = set;
				}
				Sharpen.Collections.AddAll(adjectiveNation, demonymSet);
				adjectiveNation.RemoveAll(demonyms.Keys);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(reader);
			}
		}

		/// <summary>Returns a set of demonyms for a country (or city or region).</summary>
		/// <param name="name">Some string perhaps a country name like "Australia"</param>
		/// <returns>
		/// A Set of demonym Strings, perhaps { "Australian", "Aussie", "Aussies" }.
		/// If none are known (including if the argument isn't a country/region name,
		/// then the empty set will be returned.
		/// </returns>
		public virtual ICollection<string> GetDemonyms(string name)
		{
			ICollection<string> result = demonyms[name];
			if (result == null)
			{
				result = Java.Util.Collections.EmptySet();
			}
			return result;
		}

		/// <summary>
		/// Returns whether this mention (possibly multi-word) is the
		/// adjectival form of a demonym, like "African" or "Iraqi".
		/// </summary>
		/// <remarks>
		/// Returns whether this mention (possibly multi-word) is the
		/// adjectival form of a demonym, like "African" or "Iraqi".
		/// True if it is an adjectival form, even if also a name for a
		/// person of that country (such as "Iraqi").
		/// </remarks>
		public virtual bool IsAdjectivalDemonym(string token)
		{
			return adjectiveNation.Contains(token.ToLower(Locale.English));
		}

		/// <exception cref="System.IO.IOException"/>
		private static void GetWordsFromFile(string filename, ICollection<string> resultSet, bool lowercase)
		{
			if (filename == null)
			{
				return;
			}
			using (BufferedReader reader = IOUtils.ReaderFromString(filename))
			{
				while (reader.Ready())
				{
					if (lowercase)
					{
						resultSet.Add(reader.ReadLine().ToLower());
					}
					else
					{
						resultSet.Add(reader.ReadLine());
					}
				}
			}
		}

		private void LoadAnimacyLists(string animateWordsFile, string inanimateWordsFile)
		{
			try
			{
				GetWordsFromFile(animateWordsFile, animateWords, false);
				GetWordsFromFile(inanimateWordsFile, inanimateWords, false);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		private void LoadGenderLists(string maleWordsFile, string neutralWordsFile, string femaleWordsFile)
		{
			try
			{
				GetWordsFromFile(maleWordsFile, maleWords, false);
				GetWordsFromFile(neutralWordsFile, neutralWords, false);
				GetWordsFromFile(femaleWordsFile, femaleWords, false);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		private void LoadNumberLists(string pluralWordsFile, string singularWordsFile)
		{
			try
			{
				GetWordsFromFile(pluralWordsFile, pluralWords, false);
				GetWordsFromFile(singularWordsFile, singularWords, false);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		private void LoadStatesLists(string file)
		{
			try
			{
				GetWordsFromFile(file, statesAndProvinces, true);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		private void LoadCountriesLists(string file)
		{
			try
			{
				using (BufferedReader reader = IOUtils.ReaderFromString(file))
				{
					for (string line; (line = reader.ReadLine()) != null; )
					{
						countries.Add(line.Split("\t")[1].ToLower());
					}
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>Load Bergsma and Lin (2006) gender and number list.</summary>
		/// <remarks>
		/// Load Bergsma and Lin (2006) gender and number list.
		/// <br />
		/// The list is converted from raw text and numbers to a serialized
		/// map, which saves quite a bit of time loading.
		/// See edu.stanford.nlp.dcoref.util.ConvertGenderFile
		/// </remarks>
		private void LoadGenderNumber(string file, string neutralWordsFile)
		{
			try
			{
				GetWordsFromFile(neutralWordsFile, neutralWords, false);
			}
			catch (IOException)
			{
				throw new RuntimeIOException("Couldn't load " + neutralWordsFile);
			}
			try
			{
				IDictionary<IList<string>, Dictionaries.Gender> temp = IOUtils.ReadObjectFromURLOrClasspathOrFileSystem(file);
				genderNumber.PutAll(temp);
			}
			catch (Exception)
			{
				throw new RuntimeIOException("Couldn't load " + file);
			}
		}

		private static void LoadCorefDict(string[] file, List<ICounter<Pair<string, string>>> dict)
		{
			for (int i = 0; i < 4; i++)
			{
				dict.Add(new ClassicCounter<Pair<string, string>>());
				BufferedReader reader = null;
				try
				{
					reader = IOUtils.ReaderFromString(file[i]);
					// Skip the first line (header)
					reader.ReadLine();
					while (reader.Ready())
					{
						string[] split = reader.ReadLine().Split("\t");
						dict[i].SetCount(new Pair<string, string>(split[0], split[1]), double.Parse(split[2]));
					}
				}
				catch (IOException e)
				{
					throw new Exception(e);
				}
				finally
				{
					IOUtils.CloseIgnoringExceptions(reader);
				}
			}
		}

		private static void LoadCorefDictPMI(string file, ICounter<Pair<string, string>> dict)
		{
			BufferedReader reader = null;
			try
			{
				reader = IOUtils.ReaderFromString(file);
				// Skip the first line (header)
				reader.ReadLine();
				while (reader.Ready())
				{
					string[] split = reader.ReadLine().Split("\t");
					dict.SetCount(new Pair<string, string>(split[0], split[1]), double.Parse(split[3]));
				}
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(reader);
			}
		}

		private static void LoadSignatures(string file, IDictionary<string, ICounter<string>> sigs)
		{
			BufferedReader reader = null;
			try
			{
				reader = IOUtils.ReaderFromString(file);
				while (reader.Ready())
				{
					string[] split = reader.ReadLine().Split("\t");
					ICounter<string> cntr = new ClassicCounter<string>();
					sigs[split[0]] = cntr;
					for (int i = 1; i < split.Length; i = i + 2)
					{
						cntr.SetCount(split[i], double.Parse(split[i + 1]));
					}
				}
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(reader);
			}
		}

		public Dictionaries(Properties props)
			: this(props.GetProperty(Constants.DemonymProp, DefaultPaths.DefaultDcorefDemonym), props.GetProperty(Constants.AnimateProp, DefaultPaths.DefaultDcorefAnimate), props.GetProperty(Constants.InanimateProp, DefaultPaths.DefaultDcorefInanimate), 
				props.GetProperty(Constants.MaleProp), props.GetProperty(Constants.NeutralProp), props.GetProperty(Constants.FemaleProp), props.GetProperty(Constants.PluralProp), props.GetProperty(Constants.SingularProp), props.GetProperty(Constants.StatesProp
				, DefaultPaths.DefaultDcorefStates), props.GetProperty(Constants.GenderNumberProp, DefaultPaths.DefaultDcorefGenderNumber), props.GetProperty(Constants.CountriesProp, DefaultPaths.DefaultDcorefCountries), props.GetProperty(Constants.StatesProvincesProp
				, DefaultPaths.DefaultDcorefStatesAndProvinces), props.GetProperty(Constants.SievesProp, Constants.Sievepasses).Contains("CorefDictionaryMatch"), PropertiesUtils.GetStringArray(props, Constants.DictListProp, new string[] { DefaultPaths.DefaultDcorefDict1
				, DefaultPaths.DefaultDcorefDict2, DefaultPaths.DefaultDcorefDict3, DefaultPaths.DefaultDcorefDict4 }), props.GetProperty(Constants.DictPmiProp, DefaultPaths.DefaultDcorefDict1), props.GetProperty(Constants.SignaturesProp, DefaultPaths.DefaultDcorefNeSignatures
				))
		{
		}

		public static string Signature(Properties props)
		{
			StringBuilder os = new StringBuilder();
			os.Append(Constants.DemonymProp + ":" + props.GetProperty(Constants.DemonymProp, DefaultPaths.DefaultDcorefDemonym));
			os.Append(Constants.AnimateProp + ":" + props.GetProperty(Constants.AnimateProp, DefaultPaths.DefaultDcorefAnimate));
			os.Append(Constants.InanimateProp + ":" + props.GetProperty(Constants.InanimateProp, DefaultPaths.DefaultDcorefInanimate));
			if (props.Contains(Constants.MaleProp))
			{
				os.Append(Constants.MaleProp + ":" + props.GetProperty(Constants.MaleProp));
			}
			if (props.Contains(Constants.NeutralProp))
			{
				os.Append(Constants.NeutralProp + ":" + props.GetProperty(Constants.NeutralProp));
			}
			if (props.Contains(Constants.FemaleProp))
			{
				os.Append(Constants.FemaleProp + ":" + props.GetProperty(Constants.FemaleProp));
			}
			if (props.Contains(Constants.PluralProp))
			{
				os.Append(Constants.PluralProp + ":" + props.GetProperty(Constants.PluralProp));
			}
			if (props.Contains(Constants.SingularProp))
			{
				os.Append(Constants.SingularProp + ":" + props.GetProperty(Constants.SingularProp));
			}
			os.Append(Constants.StatesProp + ":" + props.GetProperty(Constants.StatesProp, DefaultPaths.DefaultDcorefStates));
			os.Append(Constants.GenderNumberProp + ":" + props.GetProperty(Constants.GenderNumberProp, DefaultPaths.DefaultDcorefGenderNumber));
			os.Append(Constants.CountriesProp + ":" + props.GetProperty(Constants.CountriesProp, DefaultPaths.DefaultDcorefCountries));
			os.Append(Constants.StatesProvincesProp + ":" + props.GetProperty(Constants.StatesProvincesProp, DefaultPaths.DefaultDcorefStatesAndProvinces));
			os.Append(Constants.ReplicateconllProp + ":" + props.GetProperty(Constants.ReplicateconllProp, "false"));
			return os.ToString();
		}

		public Dictionaries(string demonymWords, string animateWords, string inanimateWords, string maleWords, string neutralWords, string femaleWords, string pluralWords, string singularWords, string statesWords, string genderNumber, string countries
			, string states, bool loadCorefDict, string[] corefDictFiles, string corefDictPMIFile, string signaturesFile)
		{
			LoadDemonymLists(demonymWords);
			LoadStateAbbreviation(statesWords);
			LoadAnimacyLists(animateWords, inanimateWords);
			LoadGenderLists(maleWords, neutralWords, femaleWords);
			LoadNumberLists(pluralWords, singularWords);
			LoadGenderNumber(genderNumber, neutralWords);
			LoadCountriesLists(countries);
			LoadStatesLists(states);
			SetPronouns();
			if (loadCorefDict)
			{
				LoadCorefDict(corefDictFiles, corefDict);
				LoadCorefDictPMI(corefDictPMIFile, corefDictPMI);
				LoadSignatures(signaturesFile, NE_signatures);
			}
		}

		public Dictionaries()
			: this(new Properties())
		{
		}
	}
}
