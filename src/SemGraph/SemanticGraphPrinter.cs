using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Semgraph
{
	/// <summary>
	/// This class contains only a main method, which prints out various
	/// views of SemanticGraphs.
	/// </summary>
	/// <remarks>
	/// This class contains only a main method, which prints out various
	/// views of SemanticGraphs.  This method is separate from
	/// SemanticGraph so that packages don't need to include the
	/// LexicalizedParser in order to include SemanticGraph.
	/// </remarks>
	public class SemanticGraphPrinter
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Semgraph.SemanticGraphPrinter));

		private SemanticGraphPrinter()
		{
		}

		// main method only
		public static void Main(string[] args)
		{
			Treebank tb = new MemoryTreebank();
			Properties props = StringUtils.ArgsToProperties(args);
			string treeFileName = props.GetProperty("treeFile");
			string sentFileName = props.GetProperty("sentFile");
			string testGraph = props.GetProperty("testGraph");
			if (testGraph == null)
			{
				testGraph = "false";
			}
			string load = props.GetProperty("load");
			string save = props.GetProperty("save");
			if (load != null)
			{
				log.Info("Load not implemented!");
				return;
			}
			if (sentFileName == null && treeFileName == null)
			{
				log.Info("Usage: java SemanticGraph [-sentFile file|-treeFile file] [-testGraph]");
				Tree t = Tree.ValueOf("(ROOT (S (NP (NP (DT An) (NN attempt)) (PP (IN on) (NP (NP (NNP Andres) (NNP Pastrana) (POS 's)) (NN life)))) (VP (VBD was) (VP (VBN carried) (PP (IN out) (S (VP (VBG using) (NP (DT a) (JJ powerful) (NN bomb))))))) (. .)))"
					);
				tb.Add(t);
			}
			else
			{
				if (treeFileName != null)
				{
					tb.LoadPath(treeFileName);
				}
				else
				{
					string[] options = new string[] { "-retainNPTmpSubcategories" };
					LexicalizedParser lp = ((LexicalizedParser)LexicalizedParser.LoadModel("/u/nlp/data/lexparser/englishPCFG.ser.gz", options));
					BufferedReader reader = null;
					try
					{
						reader = IOUtils.ReaderFromString(sentFileName);
					}
					catch (IOException e)
					{
						throw new RuntimeIOException("Cannot find or open " + sentFileName, e);
					}
					try
					{
						System.Console.Out.WriteLine("Processing sentence file " + sentFileName);
						for (string line; (line = reader.ReadLine()) != null; )
						{
							System.Console.Out.WriteLine("Processing sentence: " + line);
							PTBTokenizer<Word> ptb = PTBTokenizer.NewPTBTokenizer(new StringReader(line));
							IList<Word> words = ptb.Tokenize();
							Tree parseTree = lp.ParseTree(words);
							tb.Add(parseTree);
						}
						reader.Close();
					}
					catch (Exception e)
					{
						throw new Exception("Exception reading key file " + sentFileName, e);
					}
				}
			}
			foreach (Tree t_1 in tb)
			{
				SemanticGraph sg = SemanticGraphFactory.GenerateUncollapsedDependencies(t_1);
				System.Console.Out.WriteLine(sg.ToString());
				System.Console.Out.WriteLine(sg.ToCompactString());
				if (testGraph.Equals("true"))
				{
					SemanticGraph g1 = SemanticGraphFactory.GenerateCollapsedDependencies(t_1);
					System.Console.Out.WriteLine("TEST SEMANTIC GRAPH - graph ----------------------------");
					System.Console.Out.WriteLine(g1.ToString());
					System.Console.Out.WriteLine("readable ----------------------------");
					System.Console.Out.WriteLine(g1.ToString(SemanticGraph.OutputFormat.Readable));
					System.Console.Out.WriteLine("List of dependencies ----------------------------");
					System.Console.Out.WriteLine(g1.ToList());
					System.Console.Out.WriteLine("xml ----------------------------");
					System.Console.Out.WriteLine(g1.ToString(SemanticGraph.OutputFormat.Xml));
					System.Console.Out.WriteLine("dot ----------------------------");
					System.Console.Out.WriteLine(g1.ToDotFormat());
					System.Console.Out.WriteLine("dot (simple) ----------------------------");
					System.Console.Out.WriteLine(g1.ToDotFormat("Simple", CoreLabel.OutputFormat.Value));
				}
			}
			// System.out.println(" graph ----------------------------");
			// System.out.println(t.allTypedDependenciesCCProcessed(false));
			if (save != null)
			{
				log.Info("Save not implemented!");
			}
		}
		// end main
	}
}
