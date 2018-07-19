using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser.Demo
{
	internal class ParserDemo2
	{
		/// <summary>This example shows a few more ways of providing input to a parser.</summary>
		/// <remarks>
		/// This example shows a few more ways of providing input to a parser.
		/// Usage: ParserDemo2 [grammar [textFile]]
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			string grammar = args.Length > 0 ? args[0] : "edu/stanford/nlp/models/lexparser/englishPCFG.ser.gz";
			string[] options = new string[] { "-maxLength", "80", "-retainTmpSubcategories" };
			LexicalizedParser lp = ((LexicalizedParser)LexicalizedParser.LoadModel(grammar, options));
			ITreebankLanguagePack tlp = lp.GetOp().Langpack();
			IGrammaticalStructureFactory gsf = tlp.GrammaticalStructureFactory();
			IEnumerable<IList<IHasWord>> sentences;
			if (args.Length > 1)
			{
				DocumentPreprocessor dp = new DocumentPreprocessor(args[1]);
				IList<IList<IHasWord>> tmp = new List<IList<IHasWord>>();
				foreach (IList<IHasWord> sentence in dp)
				{
					tmp.Add(sentence);
				}
				sentences = tmp;
			}
			else
			{
				// Showing tokenization and parsing in code a couple of different ways.
				string[] sent = new string[] { "This", "is", "an", "easy", "sentence", "." };
				IList<IHasWord> sentence = new List<IHasWord>();
				foreach (string word in sent)
				{
					sentence.Add(new Word(word));
				}
				string sent2 = ("This is a slightly longer and more complex " + "sentence requiring tokenization.");
				// Use the default tokenizer for this TreebankLanguagePack
				ITokenizer<IHasWord> toke = tlp.GetTokenizerFactory().GetTokenizer(new StringReader(sent2));
				IList<IHasWord> sentence2 = toke.Tokenize();
				string[] sent3 = new string[] { "It", "can", "can", "it", "." };
				string[] tag3 = new string[] { "PRP", "MD", "VB", "PRP", "." };
				// Parser gets second "can" wrong without help
				IList<TaggedWord> sentence3 = new List<TaggedWord>();
				for (int i = 0; i < sent3.Length; i++)
				{
					sentence3.Add(new TaggedWord(sent3[i], tag3[i]));
				}
				Tree parse = lp.Parse(sentence3);
				parse.PennPrint();
				IList<IList<IHasWord>> tmp = new List<IList<IHasWord>>();
				tmp.Add(sentence);
				tmp.Add(sentence2);
				tmp.Add(sentence3);
				sentences = tmp;
			}
			foreach (IList<IHasWord> sentence_1 in sentences)
			{
				Tree parse = lp.Parse(sentence_1);
				parse.PennPrint();
				System.Console.Out.WriteLine();
				GrammaticalStructure gs = gsf.NewGrammaticalStructure(parse);
				IList<TypedDependency> tdl = gs.TypedDependenciesCCprocessed();
				System.Console.Out.WriteLine(tdl);
				System.Console.Out.WriteLine();
				System.Console.Out.WriteLine("The words of the sentence:");
				foreach (ILabel lab in parse.Yield())
				{
					if (lab is CoreLabel)
					{
						System.Console.Out.WriteLine(((CoreLabel)lab).ToString(CoreLabel.OutputFormat.ValueMap));
					}
					else
					{
						System.Console.Out.WriteLine(lab);
					}
				}
				System.Console.Out.WriteLine();
				System.Console.Out.WriteLine(parse.TaggedYield());
				System.Console.Out.WriteLine();
			}
			// This method turns the String into a single sentence using the
			// default tokenizer for the TreebankLanguagePack.
			string sent3_1 = "This is one last test!";
			lp.Parse(sent3_1).PennPrint();
		}

		private ParserDemo2()
		{
		}
		// static methods only
	}
}
