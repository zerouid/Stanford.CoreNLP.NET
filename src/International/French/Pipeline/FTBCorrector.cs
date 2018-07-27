using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.French;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.International.French.Pipeline
{
	/// <summary>Makes FTB trees consistent with FrenchTreebankLanguagePack.</summary>
	/// <remarks>
	/// Makes FTB trees consistent with FrenchTreebankLanguagePack. Specifically, it removes
	/// sentence-initial punctuation, and constraints sentence-final punctuation to be one of
	/// [.!?].
	/// <p>
	/// Also discards two trees of the form (SENT .), which appear in the Candito training
	/// set.
	/// </remarks>
	/// <author>Spence Green</author>
	public class FTBCorrector : ITreeTransformer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.French.Pipeline.FTBCorrector));

		private const bool Debug = false;

		private readonly IList<Pair<TregexPattern, TsurgeonPattern>> ops;

		public FTBCorrector()
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
		private const string editStr = ("@PUNC=punc <: __ >, @SENT\n" + "delete punc\n" + "\n") + ("@PUNC=punc <: __ >>- @SENT $, @PUNC\n" + "delete punc\n" + "\n") + ("@PUNC=punc <: __ >>- @SENT $, @PUNC\n" + "delete punc\n" + "\n") + ("@PUNC <: /^[^!\\.\\?]$/=term >>- @SENT !$, @PUNC\n"
			 + "relabel term /./\n" + "\n") + ("@PUNC=punc <: (/^[!\\.\\?]$/ . __)\n" + "delete punc\n" + "\n") + ("@PUNC=punc <: /^[\\.!\\?]$/ >>- (@SENT <- __=sfpos) !> @SENT\n" + "move punc $- sfpos\n" + "\n") + ("!@PUNC <: /^[^\\.!\\?]$/ >>- (@SENT <- __=loc)\n"
			 + "insert (PUNC .) $- loc\n" + "\n") + ("@PUNC <: /^[\\.!\\?]+$/=punc . (@PUNC <: /[\\.!\\?]/)\n" + "prune punc\n" + "\n") + ("@NP=bad > @MWADV\n" + "excise bad bad\n" + "\n") + ("X=bad < demi\n" + "relabel bad A\n" + "\n") + ("PC=pc < D'|depuis|après\n"
			 + "relabel pc P\n" + "\n");

		//Delete sentence-initial punctuation
		//Delete sentence final punctuation that is preceded by punctuation (first time)
		//Delete sentence final punctuation that is preceded by punctuation (second time)
		//Convert remaining sentence-final punctuation to either . if it is not [.!?]
		//Delete medial, sentence-final punctuation
		//Now move the sentence-final mark under SENT
		//For those trees that lack a sentence-final punc, add one.
		//Finally, delete these punctuation marks, which I can't seem to kill otherwise...
		//A bad MWADV tree in the training set
		// Not sure why this got a label of X.  Similar trees suggest it
		// should be A instead
		// This also seems to be mislabeled
		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				log.Info("Usage: java " + typeof(Edu.Stanford.Nlp.International.French.Pipeline.FTBCorrector).FullName + " filename\n");
				System.Environment.Exit(-1);
			}
			ITreeTransformer tt = new Edu.Stanford.Nlp.International.French.Pipeline.FTBCorrector();
			File f = new File(args[0]);
			try
			{
				//These bad trees in the Candito training set should be thrown out:
				//  (ROOT (SENT (" ") (. .)))
				//  (ROOT (SENT (. .)))
				TregexPattern pBadTree = TregexPattern.Compile("@SENT <: @PUNC");
				TregexPattern pBadTree2 = TregexPattern.Compile("@SENT <1 @PUNC <2 @PUNC !<3 __");
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(f), "UTF-8"));
				ITreeReaderFactory trf = new FrenchTreeReaderFactory();
				ITreeReader tr = trf.NewTreeReader(br);
				int nTrees = 0;
				for (Tree t; (t = tr.ReadTree()) != null; nTrees++)
				{
					TregexMatcher m = pBadTree.Matcher(t);
					TregexMatcher m2 = pBadTree2.Matcher(t);
					if (m.Find() || m2.Find())
					{
						log.Info("Discarding tree: " + t.ToString());
					}
					else
					{
						Tree fixedT = tt.TransformTree(t);
						System.Console.Out.WriteLine(fixedT.ToString());
					}
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
			catch (TregexParseException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
