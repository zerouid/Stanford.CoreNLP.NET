using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic.Pipeline
{
	/// <summary>
	/// Converts
	/// <c>VP &lt; PP-CLR</c>
	/// construction to
	/// <c>MWV &lt; MWP</c>
	/// .
	/// </summary>
	/// <author>Spence Green</author>
	public class MWETreeVisitor : ITreeVisitor
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Arabic.Pipeline.MWETreeVisitor));

		private const bool Debug = false;

		private readonly IList<Pair<TregexPattern, TsurgeonPattern>> ops;

		public MWETreeVisitor()
		{
			ops = LoadOps();
		}

		private static IList<Pair<TregexPattern, TsurgeonPattern>> LoadOps()
		{
			IList<Pair<TregexPattern, TsurgeonPattern>> ops = new List<Pair<TregexPattern, TsurgeonPattern>>();
			try
			{
				BufferedReader br = new BufferedReader(new StringReader(editStr));
				IList<TsurgeonPattern> tsp = new List<TsurgeonPattern>();
				for (string line; (line = br.ReadLine()) != null; )
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
				log.Warn(ioe);
			}
			return ops;
		}

		private static bool Continuing(string str)
		{
			return str != null && !str.Matches("\\s*");
		}

		public virtual void VisitTree(Tree t)
		{
			Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPatternsOnTree(ops, t);
		}

		/// <summary>The Tsurgeon patterns</summary>
		private const string editStr = ("@VP=vp < /PP-CLR/=pp\n" + "relabel vp MWV\n" + "relabel pp MWP\n" + "\n");
		//Mark MWEs
	}
}
