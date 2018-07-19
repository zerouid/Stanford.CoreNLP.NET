using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Arabic;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic.Pipeline
{
	/// <summary>Makes ATB trees consistent with ArabicTreebankLanguagePack.</summary>
	/// <remarks>
	/// Makes ATB trees consistent with ArabicTreebankLanguagePack. Specifically, it removes
	/// sentence-initial punctuation, and constraints sentence-final punctuation to be one of
	/// [.!?].
	/// <p>
	/// Also cleans up some of the headlines, and other weirdly tokenized sentences.
	/// </remarks>
	/// <author>Spence Green</author>
	public class ATBCorrector : ITreeTransformer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Arabic.Pipeline.ATBCorrector));

		private const bool Debug = false;

		private readonly IList<Pair<TregexPattern, TsurgeonPattern>> ops;

		public ATBCorrector()
		{
			ops = LoadOps();
		}

		private IList<Pair<TregexPattern, TsurgeonPattern>> LoadOps()
		{
			IList<Pair<TregexPattern, TsurgeonPattern>> ops = new List<Pair<TregexPattern, TsurgeonPattern>>();
			string line = null;
			try
			{
				BufferedReader br = new BufferedReader(new StringReader(editStr));
				IList<TsurgeonPattern> tsp = new List<TsurgeonPattern>();
				while ((line = br.ReadLine()) != null)
				{
					TregexPattern matchPattern = TregexPattern.Compile(line);
					tsp.Clear();
					while (Continuing(line = br.ReadLine()))
					{
						TsurgeonPattern p = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation(line);
						tsp.Add(p);
					}
					if (!tsp.IsEmpty())
					{
						TsurgeonPattern tp = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.CollectOperations(tsp);
						ops.Add(new Pair<TregexPattern, TsurgeonPattern>(matchPattern, tp));
					}
				}
			}
			catch (IOException ioe)
			{
				// while not at end of file
				Sharpen.Runtime.PrintStackTrace(ioe);
			}
			return ops;
		}

		private static bool Continuing(string str)
		{
			return str != null && !str.Matches("\\s*");
		}

		public virtual Tree TransformTree(Tree t)
		{
			return Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPatternsOnTree(ops, t);
		}

		/// <summary>The Tsurgeon patterns</summary>
		private const string editStr = ("@PUNC=punc <: __ >>, (/^S/ > @ROOT) \n" + "prune punc\n" + "\n") + ("@PUNC=punc <: __ >>, (/^S/ > @ROOT) \n" + "prune punc\n" + "\n") + ("@PUNC=punc >>- (/^S/ > @ROOT) <: __ $, @PUNC \n" + "prune punc\n" + "\n"
			) + ("@PUNC=punc >>- (/^S/ > @ROOT) <: __ $, @PUNC \n" + "prune punc\n" + "\n") + ("@PUNC=pos >>- (/^S/ > @ROOT) <: /[^\\.\\?!]/=term !$, @PUNC \n" + "relabel pos PUNC\n" + "relabel term /./\n" + "\n") + ("@PUNC=punc <: /^[\\.!\\?]+$/ >>- (/^S/ > @ROOT <- __=sfpos) !> (/^S/ > @ROOT)\n"
			 + "move punc $- sfpos\n" + "\n");

		//Delete sentence-initial punctuation
		//Delete sentence-initial punctuation (again)
		//Delete sentence final punctuation that is preceded by punctuation (first time)
		//Delete sentence final punctuation that is preceded by punctuation (second time)
		//Convert remaining sentence-final punctuation to . if it is not [.!?]
		//Delete medial, sentence-final punctuation
		//    ("@PUNC=punc <: /[!\\.\\?]+/ $. __\n"
		//        + "prune punc\n"
		//        + "\n") +
		//Now move the sentence-final mark under the top-level node
		//For those trees that lack a sentence-final punc, add one.
		//    ("/^[^\\.!\\?]$/ >>- (__ > @ROOT <- __=loc) <: __\n"
		//        + "insert (PUNC .) $- loc\n"
		//        + "\n");
		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				log.Info("Usage: java " + typeof(Edu.Stanford.Nlp.International.Arabic.Pipeline.ATBCorrector).FullName + " filename\n");
				System.Environment.Exit(-1);
			}
			ITreeTransformer tt = new Edu.Stanford.Nlp.International.Arabic.Pipeline.ATBCorrector();
			File f = new File(args[0]);
			try
			{
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(f), "UTF-8"));
				ITreeReaderFactory trf = new ArabicTreeReaderFactory.ArabicRawTreeReaderFactory();
				ITreeReader tr = trf.NewTreeReader(br);
				int nTrees = 0;
				for (Tree t; (t = tr.ReadTree()) != null; nTrees++)
				{
					Tree fixedT = tt.TransformTree(t);
					System.Console.Out.WriteLine(fixedT.ToString());
				}
				tr.Close();
				System.Console.Error.Printf("Wrote %d trees%n", nTrees);
			}
			catch (UnsupportedEncodingException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
