using System.Collections.Generic;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Time;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Regexp
{
	/// <summary>A simple rule-based classifier that detects NUMBERs in a sequence of Chinese tokens.</summary>
	/// <remarks>
	/// A simple rule-based classifier that detects NUMBERs in a sequence of Chinese tokens. This classifier mimics the
	/// behavior of
	/// <see cref="NumberSequenceClassifier"/>
	/// (without using SUTime) and works on Chinese sequence.
	/// TODO: An interface needs to be used to reuse code for NumberSequenceClassifier
	/// TODO: Ideally a Chinese version of SUTime needs to be used to provide more flexibility and accuracy.
	/// </remarks>
	/// <author>Yuhao Zhang</author>
	/// <author>Peng Qi</author>
	public class ChineseNumberSequenceClassifier : AbstractSequenceClassifier<CoreLabel>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Regexp.ChineseNumberSequenceClassifier));

		private const bool Debug = false;

		private readonly bool useSUTime;

		public const bool UseSutimeDefault = false;

		public const string UseSutimeProperty = "ner.useSUTime";

		public const string UseSutimePropertyBase = "useSUTime";

		public const string SutimeProperty = "sutime";

		private readonly ITimeExpressionExtractor timexExtractor;

		public ChineseNumberSequenceClassifier()
			: this(new Properties(), UseSutimeDefault, new Properties())
		{
		}

		public ChineseNumberSequenceClassifier(bool useSUTime)
			: this(new Properties(), useSUTime, new Properties())
		{
		}

		public ChineseNumberSequenceClassifier(Properties props, bool useSUTime, Properties sutimeProps)
			: base(props)
		{
			//import edu.stanford.nlp.pipeline.StanfordCoreNLP;
			this.useSUTime = useSUTime;
			if (this.useSUTime)
			{
				// TODO: Need a Chinese version of SUTime
				log.Warn("SUTime currently does not support Chinese. Ignore property ner.useSUTime.");
			}
			this.timexExtractor = null;
		}

		public const string NumberTag = "NUMBER";

		public const string DateTag = "DATE";

		public const string TimeTag = "TIME";

		public const string MoneyTag = "MONEY";

		public const string OrdinalTag = "ORDINAL";

		public const string PercentTag = "PERCENT";

		public static readonly Pattern CurrencyWordPattern = Pattern.Compile("元|刀|(?:美|欧|澳|加|日|韩)元|英?镑|法郎|卢比|卢布|马克|先令|克朗|泰?铢|(?:越南)?盾|美分|便士|块钱|毛钱|角钱");

		public static readonly Pattern PercentWordPattern1 = Pattern.Compile("(?:百分之|千分之).+");

		public static readonly Pattern PercentWordPattern2 = Pattern.Compile(".+%");

		public static readonly Pattern DatePattern1 = Pattern.Compile(".+(?:年代?|月份?|日|号|世纪)");

		public static readonly Pattern DatePattern2 = Pattern.Compile("(?:星期|周|礼拜).+");

		public static readonly Pattern DatePattern3 = Pattern.Compile("[0-9一二三四五六七八九零〇十]{2,4}");

		public static readonly Pattern DatePattern4 = Pattern.Compile("(?:[0-9]{2,4}[/\\-\\.][0-9]+[/\\-\\.][0-9]+|[0-9]+[/\\-\\.][0-9]+[/\\-\\.][0-9]{2,4}|[0-9]+[/\\-\\.]?[0-9]+)");

		public static readonly Pattern DatePattern5 = Pattern.Compile("[昨今明][天晨晚夜早]");

		public static readonly Pattern TimePattern1 = Pattern.Compile(".+(?::|点|时)(?:过|欠|差)?(?:.+(?::|分)?|整?|钟?|.+刻)?(?:.+秒?)");

		private static readonly Pattern ChineseAndArabicNumeralsPattern = Pattern.Compile("[一二三四五六七八九零十〇\\d]+");

		private const string DateAgeLocalizer = "后";

		public static readonly string[] CurrencyWordsValues = new string[] { "越南盾", "美元", "欧元", "澳元", "加元", "日元", "韩元", "英镑", "法郎", "卢比", "卢布", "马克", "先令", "克朗", "泰铢", "盾", "铢", "刀", "镑", "元" };

		public static readonly string[] DateWordsValues = new string[] { "明天", "后天", "昨天", "前天", "明年", "后年", "去年", "前年", "昨日", "明日", "来年", "上月", "本月", "目前", "今后", "未来", "日前", "最近", "当时", "后来", "那时", "这时", "今", "今天", "当今", "如今", "之后", "当代", "以前", "现在"
			, "将来", "此时", "此前", "元旦" };

		public static readonly HashSet<string> DateWords = new HashSet<string>(Arrays.AsList(DateWordsValues));

		public static readonly string[] TimeWordsValues = new string[] { "早晨", "清晨", "凌晨", "上午", "中午", "下午", "傍晚", "晚上", "夜间", "晨间", "晚间", "午前", "午后", "早", "晚" };

		public static readonly HashSet<string> TimeWords = new HashSet<string>(Arrays.AsList(TimeWordsValues));

		// All the tags we need
		// Patterns we need
		// In theory 块 钱 should be separated by segmenter, but just in case segmenter fails
		// TODO(yuhao): Need to add support for 块 钱, 毛 钱, 角 钱, 角, 五 块 二
		// This only works when POS = NT
		// This is used to capture a special case of date in Chinese: 70 后 or 七零 后
		// order it by number of characters DESC for handy one-by-one matching of string suffix
		/// <summary>Use a set of heuristic rules to assign NER tags to tokens.</summary>
		/// <param name="document">
		/// A
		/// <see cref="System.Collections.IList{E}"/>
		/// of something that extends
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// .
		/// </param>
		/// <returns/>
		public override IList<CoreLabel> Classify(IList<CoreLabel> document)
		{
			// The actual implementation of the classifier
			PaddedList<CoreLabel> pl = new PaddedList<CoreLabel>(document, pad);
			for (int i = 0; i < sz; i++)
			{
				CoreLabel me = pl[i];
				CoreLabel prev = pl[i - 1];
				CoreLabel next = pl[i + 1];
				// by default set to be "O"
				me.Set(typeof(CoreAnnotations.AnswerAnnotation), flags.backgroundSymbol);
				// If current word is OD, label it as ORDINAL
				if (me.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("OD"))
				{
					me.Set(typeof(CoreAnnotations.AnswerAnnotation), OrdinalTag);
				}
				else
				{
					if (CurrencyWordPattern.Matcher(me.Word()).Matches() && prev.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("CD"))
					{
						// If current word is currency word and prev word is a CD
						me.Set(typeof(CoreAnnotations.AnswerAnnotation), MoneyTag);
					}
					else
					{
						if (me.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("CD"))
						{
							// TODO(yuhao): Need to support Chinese captial numbers like 叁拾 (This won't be POS-tagged as CD).
							// If current word is a CD
							if (PercentWordPattern1.Matcher(me.Word()).Matches() || PercentWordPattern2.Matcher(me.Word()).Matches())
							{
								// If current word is a percent
								me.Set(typeof(CoreAnnotations.AnswerAnnotation), PercentTag);
							}
							else
							{
								if (RightScanFindsMoneyWord(pl, i))
								{
									// If one the right finds a currency word
									me.Set(typeof(CoreAnnotations.AnswerAnnotation), MoneyTag);
								}
								else
								{
									if (me.Word().Length == 2 && ChineseAndArabicNumeralsPattern.Matcher(me.Word()).Matches() && DateAgeLocalizer.Equals(next.Word()))
									{
										// This is to extract a special case of DATE: 70 后 or 七零 后
										me.Set(typeof(CoreAnnotations.AnswerAnnotation), DateTag);
									}
									else
									{
										// Otherwise we should safely label it as NUMBER
										me.Set(typeof(CoreAnnotations.AnswerAnnotation), NumberTag);
									}
								}
							}
						}
						else
						{
							if (me.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("NT"))
							{
								// If current word is a NT (temporal noun)
								if (DatePattern1.Matcher(me.Word()).Matches() || DatePattern2.Matcher(me.Word()).Matches() || DatePattern3.Matcher(me.Word()).Matches() || DatePattern4.Matcher(me.Word()).Matches() || DatePattern5.Matcher(me.Word()).Matches() || DateWords.Contains
									(me.Word()))
								{
									me.Set(typeof(CoreAnnotations.AnswerAnnotation), DateTag);
								}
								else
								{
									if (TimePattern1.Matcher(me.Word()).Matches() || TimeWords.Contains(me.Word()))
									{
										me.Set(typeof(CoreAnnotations.AnswerAnnotation), TimeTag);
									}
									else
									{
										// TIME may have more variants (really?) so always add as TIME by default
										me.Set(typeof(CoreAnnotations.AnswerAnnotation), TimeTag);
									}
								}
							}
							else
							{
								if (DateAgeLocalizer.Equals(me.Word()) && prev.Word() != null && prev.Word().Length == 2 && ChineseAndArabicNumeralsPattern.Matcher(prev.Word()).Matches())
								{
									// Label 后 as DATE if the sequence is 70 后 or 七零 后
									me.Set(typeof(CoreAnnotations.AnswerAnnotation), DateTag);
								}
							}
						}
					}
				}
			}
			return document;
		}

		/// <summary>Look along CD words and see if next thing is a money word.</summary>
		/// <param name="pl">The list of CoreLabel</param>
		/// <param name="i">The position to scan right from</param>
		/// <returns>Whether a money word is found</returns>
		private static bool RightScanFindsMoneyWord(IList<CoreLabel> pl, int i)
		{
			int j = i;
			int sz = pl.Count;
			while (j < sz && pl[j].GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("CD"))
			{
				j++;
			}
			if (j >= sz)
			{
				return false;
			}
			string tag = pl[j].GetString<CoreAnnotations.PartOfSpeechAnnotation>();
			string word = pl[j].Word();
			return (tag.Equals("M") || tag.Equals("NN") || tag.Equals("NNS")) && CurrencyWordPattern.Matcher(word).Matches();
		}

		public override IList<CoreLabel> ClassifyWithGlobalInformation(IList<CoreLabel> tokenSequence, ICoreMap document, ICoreMap sentence)
		{
			if (useSUTime)
			{
				log.Warn("Warning: ChineseNumberSequenceClassifier does not have SUTime implementation.");
			}
			return Classify(tokenSequence);
		}

		public override void Train(ICollection<IList<CoreLabel>> docs, IDocumentReaderAndWriter<CoreLabel> readerAndWriter)
		{
		}

		// Train is not needed for this rule-based classifier
		public override void SerializeClassifier(string serializePath)
		{
		}

		public override void SerializeClassifier(ObjectOutputStream oos)
		{
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public override void LoadClassifier(ObjectInputStream @in, Properties props)
		{
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
		}
		/* Properties props = StringUtils.argsToProperties("-props", "/Users/yuhao/Research/tmp/ChineseNumberClassifierProps.properties");
		//    Properties props = StringUtils.argsToProperties("-props", "/Users/yuhao/Research/tmp/EnglishNumberClassifierProps.properties");
		props.setProperty("outputFormat", "text");
		props.setProperty("ssplit.boundaryTokenRegex", "\\n"); // one sentence per line
		StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
		String docFileName = "/Users/yuhao/Research/tmp/chinese_number_examples.txt";
		//    String docFileName = "/Users/yuhao/Research/tmp/english_number_examples.txt";
		List<String> docLines = IOUtils.linesFromFile(docFileName);
		PrintStream out = new PrintStream(docFileName + ".out");
		for (String docLine : docLines) {
		Annotation sentenceAnnotation = new Annotation(docLine);
		pipeline.annotate(sentenceAnnotation);
		pipeline.prettyPrint(sentenceAnnotation, out);
		pipeline.prettyPrint(sentenceAnnotation, System.out);
		}
		
		out.close();*/
	}
}
