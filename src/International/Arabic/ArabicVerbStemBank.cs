using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.International.Arabic.Pipeline;
using Edu.Stanford.Nlp.Trees.Treebank;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.International.Arabic
{
	/// <summary>A singleton class backed by a map between words and stems.</summary>
	/// <remarks>
	/// A singleton class backed by a map between words and stems. The present input format is
	/// the same as that used by the Arabic subject detector.
	/// </remarks>
	/// <author>Spence Green</author>
	public class ArabicVerbStemBank
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Arabic.ArabicVerbStemBank));

		private static Edu.Stanford.Nlp.International.Arabic.ArabicVerbStemBank thisInstance = null;

		private readonly IDictionary<string, string> verbStems;

		private readonly Buckwalter b2a;

		private readonly IMapper lexMapper;

		private ArabicVerbStemBank()
		{
			verbStems = Generics.NewHashMap();
			b2a = new Buckwalter();
			lexMapper = new DefaultLexicalMapper();
		}

		public static Edu.Stanford.Nlp.International.Arabic.ArabicVerbStemBank GetInstance()
		{
			lock (typeof(ArabicVerbStemBank))
			{
				if (thisInstance == null)
				{
					thisInstance = new Edu.Stanford.Nlp.International.Arabic.ArabicVerbStemBank();
				}
				return thisInstance;
			}
		}

		public virtual string GetStem(string word)
		{
			if (verbStems.Contains(word))
			{
				return verbStems[word];
			}
			return word;
		}

		public virtual void Load(string filename)
		{
			try
			{
				BufferedReader br = IOUtils.ReaderFromString(filename);
				while (br.Ready())
				{
					string[] toks = br.ReadLine().Split("\\t");
					IList<string> toksList = Arrays.AsList(toks);
					System.Diagnostics.Debug.Assert(toksList.Count == 8);
					string word = toksList[0].ReplaceAll("\\|", string.Empty);
					string stem = toksList[7].ReplaceAll("[_|-].*\\d$", string.Empty);
					if (stem.Equals("NA") || stem.Equals("O"))
					{
						continue;
					}
					stem = lexMapper.Map(null, stem);
					string uniStem = b2a.BuckwalterToUnicode(stem);
					if (!verbStems.Contains(word))
					{
						verbStems[word] = uniStem;
					}
				}
				System.Console.Error.Printf("%s: Loaded %d stems\n", this.GetType().FullName, verbStems.Keys.Count);
			}
			catch (UnsupportedEncodingException e)
			{
				// TODO Auto-generated catch block
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException e)
			{
				// TODO Auto-generated catch block
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException e)
			{
				//TODO Need to add proper debugging
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		//WSGDEBUG - For debugging
		public virtual void DebugPrint(PrintWriter pw)
		{
			foreach (string word in verbStems.Keys)
			{
				pw.Printf("%s : %s\n", word, GetStem(word));
			}
		}

		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.International.Arabic.ArabicVerbStemBank vsb = Edu.Stanford.Nlp.International.Arabic.ArabicVerbStemBank.GetInstance();
			vsb.Load("e.test");
			PrintWriter pw = new PrintWriter(System.Console.Out, true);
			vsb.DebugPrint(pw);
		}
	}
}
