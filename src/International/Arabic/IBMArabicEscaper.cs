using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International.Arabic.Pipeline;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees.International.Arabic;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.International.Arabic
{
	/// <summary>
	/// This escaper is intended for use on flat input to be parsed by
	/// <c>LexicalizedParser</c>
	/// .
	/// It performs these functions functions:
	/// <ul>
	/// <li>Deletes the clitic markers inserted by the IBM segmenter ('#' and '+')
	/// <li>Deletes IBM classing for numbers
	/// <li>Replaces tokens that must be escaped with the appropriate LDC escape sequences
	/// <li>Applies the same orthographic normalization performed by
	/// <see cref="Edu.Stanford.Nlp.Trees.International.Arabic.ArabicTreeNormalizer"/>
	/// <li>intern()'s strings
	/// </ul>
	/// This class supports both Buckwalter and UTF-8 encoding.
	/// IMPORTANT: This class must implement
	/// <c>Function&lt;List&lt;HasWord&gt;, List&lt;HasWord&gt;&gt;</c>
	/// in order to run with the parser.
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <author>Spence Green</author>
	public class IBMArabicEscaper : Func<IList<IHasWord>, IList<IHasWord>>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Arabic.IBMArabicEscaper));

		private static readonly Pattern pEnt = Pattern.Compile("\\$[a-z]+_\\((.*?)\\)");

		private bool warnedEntityEscaping = false;

		private bool warnedProcliticEnclitic = false;

		private readonly DefaultLexicalMapper lexMapper;

		private readonly bool annotationsAndClassingOnly;

		public IBMArabicEscaper()
			: this(false)
		{
		}

		public IBMArabicEscaper(bool annoteAndClassOnly)
		{
			annotationsAndClassingOnly = annoteAndClassOnly;
			lexMapper = new DefaultLexicalMapper();
		}

		/// <summary>Disable warnings generated when tokens are escaped.</summary>
		public virtual void DisableWarnings()
		{
			warnedEntityEscaping = true;
			warnedProcliticEnclitic = true;
		}

		/// <summary>Escapes a word.</summary>
		/// <remarks>Escapes a word. This method will *not* map a word to the null string.</remarks>
		/// <returns>The escaped string</returns>
		private string EscapeString(string word)
		{
			string firstStage = StripAnnotationsAndClassing(word);
			string secondStage = ATBTreeUtils.Escape(firstStage);
			if (secondStage.IsEmpty())
			{
				return firstStage;
			}
			else
			{
				if (!firstStage.Equals(secondStage))
				{
					return secondStage;
				}
			}
			string thirdStage = lexMapper.Map(null, secondStage);
			if (thirdStage.IsEmpty())
			{
				return secondStage;
			}
			return thirdStage;
		}

		//    Matcher mAM = pAM.matcher(w);
		//    if (mAM.find()) {
		//      if ( ! warnedNormalization) {
		//        log.info("IBMArabicEscaper Note: equivalence classing certain characters, such as Alef with madda/hamza, e.g., in: " + w);
		//        warnedNormalization = true;
		//      }
		//      // 'alif maqSuura mapped to yaa
		//      w = mAM.replaceAll("\u064A");
		//    }
		//    Matcher mYH = pYaaHamza.matcher(w);
		//    if (mYH.find()) {
		//      if ( ! warnedNormalization) {
		//        log.info("IBMArabicEscaper Note: equivalence classing certain characters, such as Alef with madda/hamza, e.g., in: " + w);
		//        warnedNormalization = true;
		//      }
		//      // replace yaa followed by hamza with hamza on kursi (yaa)
		//      w = mYH.replaceAll("\u0626");
		//    }
		//    w = StringUtils.tr(w, "\u060C\u061B\u061F\u066A\u066B\u066C\u066D\u06D4\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u0966\u0967\u0968\u0969\u096A\u096B\u096C\u096D\u096E\u096F\u2013\u2014\u0091\u0092\u2018\u2019\u0093\u0094\u201C\u201D",
		//    ",;%.,*.01234567890123456789--''''\"\"\"\"");
		/// <summary>Removes IBM clitic annotations and classing from a word.</summary>
		/// <remarks>
		/// Removes IBM clitic annotations and classing from a word.
		/// Note: We do not want to nullify a word, so we only perform these operations
		/// on words of length 1 or more.
		/// </remarks>
		/// <param name="word">The unescaped word</param>
		/// <returns>The escaped word</returns>
		private string StripAnnotationsAndClassing(string word)
		{
			string w = word;
			int wLen = w.Length;
			if (wLen > 1)
			{
				// only for two or more letter words
				Matcher m2 = pEnt.Matcher(w);
				if (m2.Matches())
				{
					if (!warnedEntityEscaping)
					{
						System.Console.Error.Printf("%s: Removing IBM MT-style classing: %s --> %s\n", this.GetType().FullName, m2.Group(0), m2.Group(1));
						warnedEntityEscaping = true;
					}
					w = m2.ReplaceAll("$1");
				}
				else
				{
					if (w[0] == '+')
					{
						if (!warnedProcliticEnclitic)
						{
							warnedProcliticEnclitic = true;
							System.Console.Error.Printf("%s: Removing IBM MT-style proclitic/enclitic indicators\n", this.GetType().FullName);
						}
						w = Sharpen.Runtime.Substring(w, 1);
					}
					else
					{
						if (w[wLen - 1] == '#')
						{
							if (!warnedProcliticEnclitic)
							{
								warnedProcliticEnclitic = true;
								System.Console.Error.Printf("%s: Removing IBM MT-style proclitic/enclitic indicators\n", this.GetType().FullName);
							}
							w = Sharpen.Runtime.Substring(w, 0, wLen - 1);
						}
					}
				}
			}
			// Don't map a word to null
			if (w.IsEmpty())
			{
				return word;
			}
			return w;
		}

		/// <summary>
		/// Converts an input list of
		/// <see cref="Edu.Stanford.Nlp.Ling.IHasWord"/>
		/// in IBM Arabic to
		/// LDC ATBv3 representation. The method safely copies the input object
		/// prior to escaping.
		/// </summary>
		/// <param name="sentence">
		/// A collection of type
		/// <see cref="Edu.Stanford.Nlp.Ling.Word"/>
		/// </param>
		/// <returns>A copy of the input with each word escaped.</returns>
		/// <exception cref="System.Exception">If a word is mapped to null</exception>
		public virtual IList<IHasWord> Apply(IList<IHasWord> sentence)
		{
			IList<IHasWord> newSentence = new List<IHasWord>(sentence);
			foreach (IHasWord wd in newSentence)
			{
				wd.SetWord(Apply(wd.Word()));
			}
			return newSentence;
		}

		/// <summary>Applies escaping to a single word.</summary>
		/// <remarks>Applies escaping to a single word. Interns the escaped string.</remarks>
		/// <param name="w">The word</param>
		/// <returns>The escaped word</returns>
		/// <exception cref="System.Exception">
		/// If a word is nullified (which is really bad for the parser and
		/// for MT)
		/// </exception>
		public virtual string Apply(string w)
		{
			string escapedWord = (annotationsAndClassingOnly) ? StripAnnotationsAndClassing(w) : EscapeString(w);
			if (escapedWord.IsEmpty())
			{
				throw new Exception(string.Format("Word (%s) mapped to null", w));
			}
			return string.Intern(escapedWord);
		}

		/// <summary>
		/// This main method preprocesses one-sentence-per-line input, making the
		/// same changes as the Function.
		/// </summary>
		/// <remarks>
		/// This main method preprocesses one-sentence-per-line input, making the
		/// same changes as the Function.  By default it writes the output to files
		/// with the same name as the files passed in on the command line but with
		/// <c>.sent</c>
		/// appended to their names.  If you give the flag
		/// <c>-f</c>
		/// then output is instead sent to stdout.  Input and output
		/// is always in UTF-8.
		/// </remarks>
		/// <param name="args">A list of filenames.  The files must be UTF-8 encoded.</param>
		/// <exception cref="System.IO.IOException">If there are any issues</exception>
		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.International.Arabic.IBMArabicEscaper escaper = new Edu.Stanford.Nlp.International.Arabic.IBMArabicEscaper();
			bool printToStdout = false;
			foreach (string arg in args)
			{
				if ("-f".Equals(arg))
				{
					printToStdout = true;
					continue;
				}
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(arg), "UTF-8"));
				PrintWriter pw;
				if (printToStdout)
				{
					pw = new PrintWriter(new BufferedWriter(new OutputStreamWriter(System.Console.Out, "UTF-8")));
				}
				else
				{
					string outFile = arg + ".sent";
					pw = new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(outFile), "UTF-8")));
				}
				for (string line; (line = br.ReadLine()) != null; )
				{
					string[] words = line.Split("\\s+");
					for (int i = 0; i < words.Length; i++)
					{
						string w = escaper.EscapeString(words[i]);
						pw.Print(w);
						if (i != words.Length - 1)
						{
							pw.Print(" ");
						}
					}
					pw.Println();
				}
				br.Close();
				pw.Close();
			}
		}
	}
}
