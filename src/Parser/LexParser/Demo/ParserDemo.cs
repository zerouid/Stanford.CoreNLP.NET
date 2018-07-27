using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Parser.Lexparser.Demo
{
	internal class ParserDemo
	{
		/// <summary>The main method demonstrates the easiest way to load a parser.</summary>
		/// <remarks>
		/// The main method demonstrates the easiest way to load a parser.
		/// Simply call loadModel and specify the path of a serialized grammar
		/// model, which can be a file, a resource on the classpath, or even a URL.
		/// For example, this demonstrates loading a grammar from the models jar
		/// file, which you therefore need to include on the classpath for ParserDemo
		/// to work.
		/// Usage:
		/// <c>java ParserDemo [[model] textFile]</c>
		/// e.g.: java ParserDemo edu/stanford/nlp/models/lexparser/chineseFactored.ser.gz data/chinese-onesent-utf8.txt
		/// </remarks>
		public static void Main(string[] args)
		{
			string parserModel = "edu/stanford/nlp/models/lexparser/englishPCFG.ser.gz";
			if (args.Length > 0)
			{
				parserModel = args[0];
			}
			LexicalizedParser lp = ((LexicalizedParser)LexicalizedParser.LoadModel(parserModel));
			if (args.Length == 0)
			{
				DemoAPI(lp);
			}
			else
			{
				string textFile = (args.Length > 1) ? args[1] : args[0];
				DemoDP(lp, textFile);
			}
		}

		/// <summary>
		/// demoDP demonstrates turning a file into tokens and then parse
		/// trees.
		/// </summary>
		/// <remarks>
		/// demoDP demonstrates turning a file into tokens and then parse
		/// trees.  Note that the trees are printed by calling pennPrint on
		/// the Tree object.  It is also possible to pass a PrintWriter to
		/// pennPrint if you want to capture the output.
		/// This code will work with any supported language.
		/// </remarks>
		public static void DemoDP(LexicalizedParser lp, string filename)
		{
			// This option shows loading, sentence-segmenting and tokenizing
			// a file using DocumentPreprocessor.
			ITreebankLanguagePack tlp = lp.TreebankLanguagePack();
			// a PennTreebankLanguagePack for English
			IGrammaticalStructureFactory gsf = null;
			if (tlp.SupportsGrammaticalStructures())
			{
				gsf = tlp.GrammaticalStructureFactory();
			}
			// You could also create a tokenizer here (as below) and pass it
			// to DocumentPreprocessor
			foreach (IList<IHasWord> sentence in new DocumentPreprocessor(filename))
			{
				Tree parse = lp.Apply(sentence);
				parse.PennPrint();
				System.Console.Out.WriteLine();
				if (gsf != null)
				{
					GrammaticalStructure gs = gsf.NewGrammaticalStructure(parse);
					ICollection tdl = gs.TypedDependenciesCCprocessed();
					System.Console.Out.WriteLine(tdl);
					System.Console.Out.WriteLine();
				}
			}
		}

		/// <summary>
		/// demoAPI demonstrates other ways of calling the parser with
		/// already tokenized text, or in some cases, raw text that needs to
		/// be tokenized as a single sentence.
		/// </summary>
		/// <remarks>
		/// demoAPI demonstrates other ways of calling the parser with
		/// already tokenized text, or in some cases, raw text that needs to
		/// be tokenized as a single sentence.  Output is handled with a
		/// TreePrint object.  Note that the options used when creating the
		/// TreePrint can determine what results to print out.  Once again,
		/// one can capture the output by passing a PrintWriter to
		/// TreePrint.printTree. This code is for English.
		/// </remarks>
		public static void DemoAPI(LexicalizedParser lp)
		{
			// This option shows parsing a list of correctly tokenized words
			string[] sent = new string[] { "This", "is", "an", "easy", "sentence", "." };
			IList<CoreLabel> rawWords = SentenceUtils.ToCoreLabelList(sent);
			Tree parse = lp.Apply(rawWords);
			parse.PennPrint();
			System.Console.Out.WriteLine();
			// This option shows loading and using an explicit tokenizer
			string sent2 = "This is another sentence.";
			ITokenizerFactory<CoreLabel> tokenizerFactory = PTBTokenizer.Factory(new CoreLabelTokenFactory(), string.Empty);
			ITokenizer<CoreLabel> tok = tokenizerFactory.GetTokenizer(new StringReader(sent2));
			IList<CoreLabel> rawWords2 = tok.Tokenize();
			parse = lp.Apply(rawWords2);
			ITreebankLanguagePack tlp = lp.TreebankLanguagePack();
			// PennTreebankLanguagePack for English
			IGrammaticalStructureFactory gsf = tlp.GrammaticalStructureFactory();
			GrammaticalStructure gs = gsf.NewGrammaticalStructure(parse);
			IList<TypedDependency> tdl = gs.TypedDependenciesCCprocessed();
			System.Console.Out.WriteLine(tdl);
			System.Console.Out.WriteLine();
			// You can also use a TreePrint object to print trees and dependencies
			TreePrint tp = new TreePrint("penn,typedDependenciesCollapsed");
			tp.PrintTree(parse);
		}

		private ParserDemo()
		{
		}
		// static methods only
	}
}
