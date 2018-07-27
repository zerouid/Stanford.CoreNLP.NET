using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// This class implements the
	/// <c>TreeReader</c>
	/// interface to read Penn Treebank-style
	/// files. The reader is implemented as a push-down automaton (PDA) that parses the Lisp-style
	/// format in which the trees are stored. This reader is compatible with both PTB
	/// and PATB trees.
	/// <br />
	/// One small detail to note is that the <code>PennTreeReader</code>
	/// silently replaces \* with * and \/ with /.  Two possible designs
	/// for this were to make the <code>PennTreeReader</code> always do
	/// this or to make the <code>TreeNormalizers</code> do this.  We
	/// decided to put it in the <code>PennTreeReader</code> class itself
	/// to avoid the problem of people making new
	/// <code>TreeNormalizers</code> and forgetting to include the
	/// unescaping.
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <author>Roger Levy</author>
	/// <author>Spence Green</author>
	public class PennTreeReader : ITreeReader
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.PennTreeReader));

		private readonly Reader reader;

		private readonly ITokenizer<string> tokenizer;

		private readonly TreeNormalizer treeNormalizer;

		private readonly ITreeFactory treeFactory;

		private const bool Debug = false;

		private Tree currentTree;

		private List<Tree> stack;

		private const string leftParen = "(";

		private const string rightParen = ")";

		/// <summary>Read parse trees from a <code>Reader</code>.</summary>
		/// <remarks>
		/// Read parse trees from a <code>Reader</code>.
		/// For the defaulted arguments, you get a
		/// <code>SimpleTreeFactory</code>, no <code>TreeNormalizer</code>, and
		/// a <code>PennTreebankTokenizer</code>.
		/// </remarks>
		/// <param name="in">The <code>Reader</code></param>
		public PennTreeReader(Reader @in)
			: this(@in, new LabeledScoredTreeFactory())
		{
		}

		/// <summary>Read parse trees from a <code>Reader</code>.</summary>
		/// <param name="in">the Reader</param>
		/// <param name="tf">TreeFactory -- factory to create some kind of Tree</param>
		public PennTreeReader(Reader @in, ITreeFactory tf)
			: this(@in, tf, null, new PennTreebankTokenizer(@in))
		{
		}

		/// <summary>Read parse trees from a Reader.</summary>
		/// <param name="in">Reader</param>
		/// <param name="tf">TreeFactory -- factory to create some kind of Tree</param>
		/// <param name="tn">the method of normalizing trees</param>
		public PennTreeReader(Reader @in, ITreeFactory tf, TreeNormalizer tn)
			: this(@in, tf, tn, new PennTreebankTokenizer(@in))
		{
		}

		/// <summary>Read parse trees from a Reader.</summary>
		/// <param name="in">Reader</param>
		/// <param name="tf">TreeFactory -- factory to create some kind of Tree</param>
		/// <param name="tn">the method of normalizing trees</param>
		/// <param name="st">Tokenizer that divides up Reader</param>
		public PennTreeReader(Reader @in, ITreeFactory tf, TreeNormalizer tn, ITokenizer<string> st)
		{
			// misuse a list as a stack, since we want to avoid the synchronized and old Stack, but don't need the power and JDK 1.6 dependency of a Deque
			reader = @in;
			treeFactory = tf;
			treeNormalizer = tn;
			tokenizer = st;
			// check for whacked out headers still present in Brown corpus in Treebank 3
			string first = (st.MoveNext() ? st.Peek() : null);
			if (first != null && first.StartsWith("*x*x*x"))
			{
				int foundCount = 0;
				while (foundCount < 4 && st.MoveNext())
				{
					first = st.Current;
					if (first != null && first.StartsWith("*x*x*x"))
					{
						foundCount++;
					}
				}
			}
		}

		/// <summary>
		/// Reads a single tree in standard Penn Treebank format from the
		/// input stream.
		/// </summary>
		/// <remarks>
		/// Reads a single tree in standard Penn Treebank format from the
		/// input stream. The method supports additional parentheses around the
		/// tree (an unnamed ROOT node) so long as they are balanced. If the token stream
		/// ends before the current tree is complete, then the method will throw an
		/// <code>IOException</code>.
		/// <p>
		/// Note that the method will skip malformed trees and attempt to
		/// read additional trees from the input stream. It is possible, however,
		/// that a malformed tree will corrupt the token stream. In this case,
		/// an <code>IOException</code> will eventually be thrown.
		/// </remarks>
		/// <returns>A single tree, or <code>null</code> at end of token stream.</returns>
		/// <exception cref="System.IO.IOException"/>
		public virtual Tree ReadTree()
		{
			Tree t = null;
			while (tokenizer.MoveNext() && t == null)
			{
				//Setup PDA
				this.currentTree = null;
				this.stack = new List<Tree>();
				try
				{
					t = GetTreeFromInputStream();
				}
				catch (NoSuchElementException)
				{
					throw new IOException("End of token stream encountered before parsing could complete.");
				}
				if (t != null)
				{
					// cdm 20100618: Don't do this!  This was never the historical behavior!!!
					// Escape empty trees e.g. (())
					// while(t != null && (t.value() == null || t.value().equals("")) && t.numChildren() <= 1)
					//   t = t.firstChild();
					if (treeNormalizer != null && treeFactory != null)
					{
						t = treeNormalizer.NormalizeWholeTree(t, treeFactory);
					}
					if (t != null)
					{
						t.IndexLeaves(true);
					}
				}
			}
			return t;
		}

		private static readonly Pattern StarPattern = Pattern.Compile("\\\\\\*");

		private static readonly Pattern SlashPattern = Pattern.Compile("\\\\/");

		/// <exception cref="Java.Util.NoSuchElementException"/>
		private Tree GetTreeFromInputStream()
		{
			int wordIndex = 1;
			// FSA
			while (tokenizer.MoveNext())
			{
				string token = tokenizer.Current;
				switch (token)
				{
					case leftParen:
					{
						// cdm 20100225: This next line used to have "" instead of null, but the traditional and current tree normalizers depend on the label being null not "" when there is no label on a tree (like the outermost English PTB level)
						string label = (tokenizer.Peek().Equals(leftParen)) ? null : tokenizer.Current;
						if (rightParen.Equals(label))
						{
							//Skip past empty trees
							continue;
						}
						else
						{
							if (treeNormalizer != null)
							{
								label = treeNormalizer.NormalizeNonterminal(label);
							}
						}
						if (label != null)
						{
							label = StarPattern.Matcher(label).ReplaceAll("*");
							label = SlashPattern.Matcher(label).ReplaceAll("/");
						}
						Tree newTree = treeFactory.NewTreeNode(label, null);
						// dtrs are added below
						if (currentTree == null)
						{
							stack.Add(newTree);
						}
						else
						{
							currentTree.AddChild(newTree);
							stack.Add(currentTree);
						}
						currentTree = newTree;
						break;
					}

					case rightParen:
					{
						if (stack.IsEmpty())
						{
							// Warn that file has too many right parentheses
							log.Info("PennTreeReader: warning: file has extra non-matching right parenthesis [ignored]");
							goto label_break;
						}
						//Accept
						currentTree = stack.Remove(stack.Count - 1);
						// i.e., stack.pop()
						if (stack.IsEmpty())
						{
							return currentTree;
						}
						break;
					}

					default:
					{
						if (currentTree == null)
						{
							// A careful Reader should warn here, but it's kind of useful to
							// suppress this because then the TreeReader doesn't print a ton of
							// messages if there is a README file in a directory of Trees.
							// log.info("PennTreeReader: warning: file has extra token not in a s-expression tree: " + token + " [ignored]");
							goto label_break;
						}
						string terminal = (treeNormalizer == null) ? token : treeNormalizer.NormalizeTerminal(token);
						terminal = StarPattern.Matcher(terminal).ReplaceAll("*");
						terminal = SlashPattern.Matcher(terminal).ReplaceAll("/");
						Tree leaf = treeFactory.NewLeaf(terminal);
						if (leaf.Label() is IHasIndex)
						{
							IHasIndex hi = (IHasIndex)leaf.Label();
							hi.SetIndex(wordIndex);
						}
						if (leaf.Label() is IHasWord)
						{
							IHasWord hw = (IHasWord)leaf.Label();
							hw.SetWord(leaf.Label().Value());
						}
						if (leaf.Label() is IHasTag)
						{
							IHasTag ht = (IHasTag)leaf.Label();
							ht.SetTag(currentTree.Label().Value());
						}
						wordIndex++;
						currentTree.AddChild(leaf);
						// cdm: Note: this implementation just isn't as efficient as the old recursive descent parser (see 2008 code), where all the daughters are gathered before the tree is made....
						break;
					}
				}
label_continue: ;
			}
label_break: ;
			//Reject
			if (currentTree != null)
			{
				log.Info("PennTreeReader: warning: incomplete tree (extra left parentheses in input): " + currentTree);
			}
			return null;
		}

		/// <summary>
		/// Closes the underlying <code>Reader</code> used to create this
		/// class.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual void Close()
		{
			reader.Close();
		}

		/// <summary>Loads treebank data from first argument and prints it.</summary>
		/// <param name="args">Array of command-line arguments: specifies a filename</param>
		public static void Main(string[] args)
		{
			try
			{
				ITreeFactory tf = new LabeledScoredTreeFactory();
				Reader r = new BufferedReader(new InputStreamReader(new FileInputStream(args[0]), "UTF-8"));
				ITreeReader tr = new Edu.Stanford.Nlp.Trees.PennTreeReader(r, tf);
				Tree t = tr.ReadTree();
				while (t != null)
				{
					System.Console.Out.WriteLine(t);
					System.Console.Out.WriteLine();
					t = tr.ReadTree();
				}
				r.Close();
			}
			catch (IOException ioe)
			{
				Sharpen.Runtime.PrintStackTrace(ioe);
			}
		}
	}
}
