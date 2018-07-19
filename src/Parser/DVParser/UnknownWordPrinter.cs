using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Metrics;
using Edu.Stanford.Nlp.Trees;
using Java.IO;
using Java.Util;
using Org.Ejml.Simple;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Dvparser
{
	/// <summary>Prints out words which are unknown to the DVParser.</summary>
	/// <remarks>
	/// Prints out words which are unknown to the DVParser.
	/// <br />
	/// This does not have to be specific to the DVParser.  We could easily
	/// add an interface which lets it call something to ask if the word is
	/// known or not, and if not, keeps track of those words.
	/// </remarks>
	/// <author>John Bauer</author>
	public class UnknownWordPrinter : IEval
	{
		internal readonly DVModel model;

		internal readonly SimpleMatrix unk;

		internal readonly TreeSet<string> unkWords = new TreeSet<string>();

		public UnknownWordPrinter(DVModel model)
		{
			this.model = model;
			this.unk = model.GetUnknownWordVector();
		}

		public virtual void Evaluate(Tree guess, Tree gold)
		{
			Evaluate(guess, gold, new PrintWriter(System.Console.Out, true));
		}

		public virtual void Evaluate(Tree guess, Tree gold, PrintWriter pw)
		{
			Evaluate(guess, gold, pw, 1.0);
		}

		public virtual void Evaluate(Tree guess, Tree gold, PrintWriter pw, double weight)
		{
			IList<ILabel> words = guess.Yield();
			int pos = 0;
			foreach (ILabel word in words)
			{
				++pos;
				SimpleMatrix wv = model.GetWordVector(word.Value());
				// would be faster but more implementation-specific if we
				// removed wv.equals
				if (wv == unk || wv.Equals(unk))
				{
					pw.Printf("  Unknown word in position %d: %s%n", pos, word.Value());
					unkWords.Add(word.Value());
				}
			}
		}

		public virtual void Display(bool verbose)
		{
			Display(verbose, new PrintWriter(System.Console.Out, true));
		}

		public virtual void Display(bool verbose, PrintWriter pw)
		{
			if (unkWords.IsEmpty())
			{
				pw.Printf("UnknownWordPrinter: all words known by DVModel%n");
			}
			else
			{
				pw.Printf("UnknownWordPrinter: the following words are unknown%n");
				foreach (string word in unkWords)
				{
					pw.Printf("  %s%n", word);
				}
			}
		}
	}
}
