using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Coref.Hybrid;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Neural;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Data
{
	/// <summary>Stores various data used for coreference.</summary>
	/// <remarks>
	/// Stores various data used for coreference.
	/// TODO: get rid of dependence on HybridCorefProperties
	/// </remarks>
	/// <author>Heeyoung Lee</author>
	public class Dictionaries
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Coref.Data.Dictionaries));

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

		public ICollection<string> reportVerb;

		public ICollection<string> reportNoun;

		public ICollection<string> nonWords;

		public ICollection<string> copulas;

		public ICollection<string> quantifiers;

		public ICollection<string> parts;

		public ICollection<string> temporals;

		public ICollection<string> femalePronouns;

		public ICollection<string> malePronouns;

		public ICollection<string> neutralPronouns;

		public ICollection<string> possessivePronouns;

		public ICollection<string> otherPronouns;

		public ICollection<string> thirdPersonPronouns;

		public ICollection<string> secondPersonPronouns;

		public ICollection<string> firstPersonPronouns;

		public ICollection<string> moneyPercentNumberPronouns;

		public ICollection<string> dateTimePronouns;

		public ICollection<string> organizationPronouns;

		public ICollection<string> locationPronouns;

		public ICollection<string> inanimatePronouns;

		public ICollection<string> animatePronouns;

		public ICollection<string> indefinitePronouns;

		public ICollection<string> relativePronouns;

		public ICollection<string> interrogativePronouns;

		public ICollection<string> GPEPronouns;

		public ICollection<string> pluralPronouns;

		public ICollection<string> singularPronouns;

		public ICollection<string> facilityVehicleWeaponPronouns;

		public ICollection<string> miscPronouns;

		public ICollection<string> reflexivePronouns;

		public ICollection<string> transparentNouns;

		public ICollection<string> stopWords;

		public ICollection<string> notOrganizationPRP;

		public ICollection<string> quantifiers2;

		public ICollection<string> determiners;

		public ICollection<string> negations;

		public ICollection<string> neg_relations;

		public ICollection<string> modals;

		public ICollection<string> titleWords;

		public ICollection<string> removeWords;

		public ICollection<string> removeChars;

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

		private void ReadWordLists(Locale lang)
		{
			switch (lang.GetLanguage())
			{
				case "en":
				default:
				{
					reportVerb = WordLists.reportVerbEn;
					reportNoun = WordLists.reportNounEn;
					nonWords = WordLists.nonWordsEn;
					copulas = WordLists.copulasEn;
					quantifiers = WordLists.quantifiersEn;
					parts = WordLists.partsEn;
					temporals = WordLists.temporalsEn;
					femalePronouns = WordLists.femalePronounsEn;
					malePronouns = WordLists.malePronounsEn;
					neutralPronouns = WordLists.neutralPronounsEn;
					possessivePronouns = WordLists.possessivePronounsEn;
					otherPronouns = WordLists.otherPronounsEn;
					thirdPersonPronouns = WordLists.thirdPersonPronounsEn;
					secondPersonPronouns = WordLists.secondPersonPronounsEn;
					firstPersonPronouns = WordLists.firstPersonPronounsEn;
					moneyPercentNumberPronouns = WordLists.moneyPercentNumberPronounsEn;
					dateTimePronouns = WordLists.dateTimePronounsEn;
					organizationPronouns = WordLists.organizationPronounsEn;
					locationPronouns = WordLists.locationPronounsEn;
					inanimatePronouns = WordLists.inanimatePronounsEn;
					animatePronouns = WordLists.animatePronounsEn;
					indefinitePronouns = WordLists.indefinitePronounsEn;
					relativePronouns = WordLists.relativePronounsEn;
					GPEPronouns = WordLists.GPEPronounsEn;
					pluralPronouns = WordLists.pluralPronounsEn;
					singularPronouns = WordLists.singularPronounsEn;
					facilityVehicleWeaponPronouns = WordLists.facilityVehicleWeaponPronounsEn;
					miscPronouns = WordLists.miscPronounsEn;
					reflexivePronouns = WordLists.reflexivePronounsEn;
					transparentNouns = WordLists.transparentNounsEn;
					stopWords = WordLists.stopWordsEn;
					notOrganizationPRP = WordLists.notOrganizationPRPEn;
					quantifiers2 = WordLists.quantifiers2En;
					determiners = WordLists.determinersEn;
					negations = WordLists.negationsEn;
					neg_relations = WordLists.neg_relationsEn;
					modals = WordLists.modalsEn;
					break;
				}

				case "zh":
				{
					reportVerb = WordLists.reportVerbZh;
					reportNoun = WordLists.reportNounZh;
					nonWords = WordLists.nonWordsZh;
					copulas = WordLists.copulasZh;
					quantifiers = WordLists.quantifiersZh;
					parts = WordLists.partsZh;
					temporals = WordLists.temporalsZh;
					femalePronouns = WordLists.femalePronounsZh;
					malePronouns = WordLists.malePronounsZh;
					neutralPronouns = WordLists.neutralPronounsZh;
					possessivePronouns = WordLists.possessivePronounsZh;
					otherPronouns = WordLists.otherPronounsZh;
					thirdPersonPronouns = WordLists.thirdPersonPronounsZh;
					secondPersonPronouns = WordLists.secondPersonPronounsZh;
					firstPersonPronouns = WordLists.firstPersonPronounsZh;
					moneyPercentNumberPronouns = WordLists.moneyPercentNumberPronounsZh;
					dateTimePronouns = WordLists.dateTimePronounsZh;
					organizationPronouns = WordLists.organizationPronounsZh;
					locationPronouns = WordLists.locationPronounsZh;
					inanimatePronouns = WordLists.inanimatePronounsZh;
					animatePronouns = WordLists.animatePronounsZh;
					indefinitePronouns = WordLists.indefinitePronounsZh;
					relativePronouns = WordLists.relativePronounsZh;
					interrogativePronouns = WordLists.interrogativePronounsZh;
					GPEPronouns = WordLists.GPEPronounsZh;
					pluralPronouns = WordLists.pluralPronounsZh;
					singularPronouns = WordLists.singularPronounsZh;
					facilityVehicleWeaponPronouns = WordLists.facilityVehicleWeaponPronounsZh;
					miscPronouns = WordLists.miscPronounsZh;
					reflexivePronouns = WordLists.reflexivePronounsZh;
					transparentNouns = WordLists.transparentNounsZh;
					stopWords = WordLists.stopWordsZh;
					notOrganizationPRP = WordLists.notOrganizationPRPZh;
					quantifiers2 = WordLists.quantifiers2Zh;
					determiners = WordLists.determinersZh;
					negations = WordLists.negationsZh;
					neg_relations = WordLists.neg_relationsZh;
					modals = WordLists.modalsZh;
					titleWords = WordLists.titleWordsZh;
					removeWords = WordLists.removeWordsZh;
					removeChars = WordLists.removeCharsZh;
					break;
				}
			}
		}

		public int dimVector;

		public VectorMap vectors = new VectorMap();

		public IDictionary<string, string> strToEntity = Generics.NewHashMap();

		public ICounter<string> dictScore = new ClassicCounter<string>();

		private void SetPronouns()
		{
			Sharpen.Collections.AddAll(personPronouns, animatePronouns);
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
		private void LoadStateAbbreviation(string statesFile)
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
		/// demonyms is a has from each country (etc.) to a set of demonymic Strings;
		/// adjectiveNation is a set of demonymic Strings;
		/// demonymSet has all country (etc.) names and all demonymic Strings.
		/// </remarks>
		private void LoadDemonymLists(string demonymFile)
		{
			try
			{
				using (BufferedReader reader = IOUtils.ReaderFromString(demonymFile))
				{
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
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
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

		/*
		private void loadGenderNumber(String file, String neutralWordsFile) {
		try {
		getWordsFromFile(neutralWordsFile, neutralWords, false);
		Map<List<String>, Gender> temp = IOUtils.readObjectFromURLOrClasspathOrFileSystem(file);
		genderNumber.putAll(temp);
		} catch (IOException e) {
		throw new RuntimeIOException(e);
		} catch (ClassNotFoundException e) {
		throw new RuntimeIOException(e);
		}
		}
		*/
		/// <summary>Load Bergsma and Lin (2006) gender and number list.</summary>
		private void LoadGenderNumber(string file, string neutralWordsFile)
		{
			try
			{
				using (BufferedReader reader = IOUtils.ReaderFromString(file))
				{
					GetWordsFromFile(neutralWordsFile, neutralWords, false);
					string[] split = new string[2];
					string[] countStr = new string[3];
					for (string line; (line = reader.ReadLine()) != null; )
					{
						StringUtils.SplitOnChar(split, line, '\t');
						StringUtils.SplitOnChar(countStr, split[1], ' ');
						int male = System.Convert.ToInt32(countStr[0]);
						int female = System.Convert.ToInt32(countStr[1]);
						int neutral = System.Convert.ToInt32(countStr[2]);
						Dictionaries.Gender gender = Dictionaries.Gender.Unknown;
						if (male * 0.5 > female + neutral && male > 2)
						{
							gender = Dictionaries.Gender.Male;
						}
						else
						{
							if (female * 0.5 > male + neutral && female > 2)
							{
								gender = Dictionaries.Gender.Female;
							}
							else
							{
								if (neutral * 0.5 > male + female && neutral > 2)
								{
									gender = Dictionaries.Gender.Neutral;
								}
							}
						}
						if (gender == Dictionaries.Gender.Unknown)
						{
							continue;
						}
						string[] words = split[0].Split(" ");
						IList<string> tokens = Arrays.AsList(words);
						genderNumber[tokens] = gender;
					}
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		private void LoadChineseGenderNumberAnimacy(string file)
		{
			string[] split = new string[8];
			foreach (string line in IOUtils.ReadLines(file))
			{
				if (line.StartsWith("#WORD"))
				{
					continue;
				}
				// ignore first row
				StringUtils.SplitOnChar(split, line, '\t');
				string word = split[0];
				int animate = System.Convert.ToInt32(split[1]);
				int inanimate = System.Convert.ToInt32(split[2]);
				int male = System.Convert.ToInt32(split[3]);
				int female = System.Convert.ToInt32(split[4]);
				int neutral = System.Convert.ToInt32(split[5]);
				int singular = System.Convert.ToInt32(split[6]);
				int plural = System.Convert.ToInt32(split[7]);
				if (male * 0.5 > female + neutral && male > 2)
				{
					maleWords.Add(word);
				}
				else
				{
					if (female * 0.5 > male + neutral && female > 2)
					{
						femaleWords.Add(word);
					}
					else
					{
						if (neutral * 0.5 > male + female && neutral > 2)
						{
							neutralWords.Add(word);
						}
					}
				}
				if (animate * 0.5 > inanimate && animate > 2)
				{
					animateWords.Add(word);
				}
				else
				{
					if (inanimate * 0.5 > animate && inanimate > 2)
					{
						inanimateWords.Add(word);
					}
				}
				if (singular * 0.5 > plural && singular > 2)
				{
					singularWords.Add(word);
				}
				else
				{
					if (plural * 0.5 > singular && plural > 2)
					{
						pluralWords.Add(word);
					}
				}
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
						dict[i].SetCount(new Pair<string, string>(split[0], split[1]), double.ParseDouble(split[2]));
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
					dict.SetCount(new Pair<string, string>(split[0], split[1]), double.ParseDouble(split[3]));
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
						cntr.SetCount(split[i], double.ParseDouble(split[i + 1]));
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

		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.IO.IOException"/>
		public virtual void LoadSemantics(Properties props)
		{
			log.Info("LOADING SEMANTICS");
			//    wordnet = new WordNet();
			// load word vector
			if (HybridCorefProperties.LoadWordEmbedding(props))
			{
				log.Info("LOAD: WordVectors");
				string wordvectorFile = HybridCorefProperties.GetPathSerializedWordVectors(props);
				string word2vecFile = HybridCorefProperties.GetPathWord2Vec(props);
				try
				{
					// Try to read the serialized vectors
					vectors = VectorMap.Deserialize(wordvectorFile);
				}
				catch (IOException e)
				{
					// If that fails, try to read the vectors from the word2vec file
					if (new File(word2vecFile).Exists())
					{
						vectors = VectorMap.ReadWord2Vec(word2vecFile);
						if (wordvectorFile != null && !wordvectorFile.StartsWith("edu"))
						{
							vectors.Serialize(wordvectorFile);
						}
					}
					else
					{
						// If that fails, give up and crash
						throw new RuntimeIOException(e);
					}
				}
				dimVector = vectors.GetEnumerator().Current.Value.Length;
			}
		}

		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.IO.IOException"/>
		public Dictionaries(Properties props)
			: this(props.GetProperty(HybridCorefProperties.LangProp, HybridCorefProperties.LanguageDefault.ToLanguageTag()), props.GetProperty(HybridCorefProperties.DemonymProp, DefaultPaths.DefaultDcorefDemonym), props.GetProperty(HybridCorefProperties
				.AnimateProp, DefaultPaths.DefaultDcorefAnimate), props.GetProperty(HybridCorefProperties.InanimateProp, DefaultPaths.DefaultDcorefInanimate), props.GetProperty(HybridCorefProperties.MaleProp), props.GetProperty(HybridCorefProperties.NeutralProp
				), props.GetProperty(HybridCorefProperties.FemaleProp), props.GetProperty(HybridCorefProperties.PluralProp), props.GetProperty(HybridCorefProperties.SingularProp), props.GetProperty(HybridCorefProperties.StatesProp, DefaultPaths.DefaultDcorefStates
				), props.GetProperty(HybridCorefProperties.GenderNumberProp, HybridCorefProperties.GetGenderNumber(props)), props.GetProperty(HybridCorefProperties.CountriesProp, DefaultPaths.DefaultDcorefCountries), props.GetProperty(HybridCorefProperties
				.StatesProvincesProp, DefaultPaths.DefaultDcorefStatesAndProvinces), HybridCorefProperties.GetSieves(props).Contains("CorefDictionaryMatch"), PropertiesUtils.GetStringArray(props, HybridCorefProperties.DictListProp, new string[] { DefaultPaths
				.DefaultDcorefDict1, DefaultPaths.DefaultDcorefDict2, DefaultPaths.DefaultDcorefDict3, DefaultPaths.DefaultDcorefDict4 }), props.GetProperty(HybridCorefProperties.DictPmiProp, DefaultPaths.DefaultDcorefDict1), props.GetProperty(HybridCorefProperties
				.SignaturesProp, DefaultPaths.DefaultDcorefNeSignatures))
		{
			//    if(Boolean.parseBoolean(props.getProperty("useValDictionary"))) {
			//      log.info("LOAD: ValDictionary");
			//      for(String line : IOUtils.readLines(valDict)) {
			//        String[] split = line.toLowerCase().split("\t");
			//        strToEntity.put(split[0], split[2]);
			//        dictScore.setCount(split[0], Double.parseDouble(split[1]));
			//      }
			//    }
			/*if(CorefProperties.useSemantics(props)) {
			loadSemantics(props);
			} else {
			log.info("SEMANTICS NOT LOADED");
			}*/
			if (props.Contains("coref.zh.dict"))
			{
				LoadChineseGenderNumberAnimacy(props.GetProperty("coref.zh.dict"));
			}
		}

		public static string Signature(Properties props)
		{
			StringBuilder os = new StringBuilder();
			os.Append(HybridCorefProperties.DemonymProp + ":" + props.GetProperty(HybridCorefProperties.DemonymProp, DefaultPaths.DefaultDcorefDemonym));
			os.Append(HybridCorefProperties.AnimateProp + ":" + props.GetProperty(HybridCorefProperties.AnimateProp, DefaultPaths.DefaultDcorefAnimate));
			os.Append(HybridCorefProperties.InanimateProp + ":" + props.GetProperty(HybridCorefProperties.InanimateProp, DefaultPaths.DefaultDcorefInanimate));
			if (props.Contains(HybridCorefProperties.MaleProp))
			{
				os.Append(HybridCorefProperties.MaleProp + ":" + props.GetProperty(HybridCorefProperties.MaleProp));
			}
			if (props.Contains(HybridCorefProperties.NeutralProp))
			{
				os.Append(HybridCorefProperties.NeutralProp + ":" + props.GetProperty(HybridCorefProperties.NeutralProp));
			}
			if (props.Contains(HybridCorefProperties.FemaleProp))
			{
				os.Append(HybridCorefProperties.FemaleProp + ":" + props.GetProperty(HybridCorefProperties.FemaleProp));
			}
			if (props.Contains(HybridCorefProperties.PluralProp))
			{
				os.Append(HybridCorefProperties.PluralProp + ":" + props.GetProperty(HybridCorefProperties.PluralProp));
			}
			if (props.Contains(HybridCorefProperties.SingularProp))
			{
				os.Append(HybridCorefProperties.SingularProp + ":" + props.GetProperty(HybridCorefProperties.SingularProp));
			}
			os.Append(HybridCorefProperties.StatesProp + ":" + props.GetProperty(HybridCorefProperties.StatesProp, DefaultPaths.DefaultDcorefStates));
			os.Append(HybridCorefProperties.GenderNumberProp + ":" + props.GetProperty(HybridCorefProperties.GenderNumberProp, DefaultPaths.DefaultDcorefGenderNumber));
			os.Append(HybridCorefProperties.CountriesProp + ":" + props.GetProperty(HybridCorefProperties.CountriesProp, DefaultPaths.DefaultDcorefCountries));
			os.Append(HybridCorefProperties.StatesProvincesProp + ":" + props.GetProperty(HybridCorefProperties.StatesProvincesProp, DefaultPaths.DefaultDcorefStatesAndProvinces));
			return os.ToString();
		}

		public Dictionaries(string language, string demonymWords, string animateWords, string inanimateWords, string maleWords, string neutralWords, string femaleWords, string pluralWords, string singularWords, string statesWords, string genderNumber
			, string countries, string states, bool loadCorefDict, string[] corefDictFiles, string corefDictPMIFile, string signaturesFile)
		{
			Locale lang = Locale.ForLanguageTag(language);
			ReadWordLists(lang);
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

		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.IO.IOException"/>
		public Dictionaries()
			: this(new Properties())
		{
		}
	}
}
