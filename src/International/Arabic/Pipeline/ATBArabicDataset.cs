using Edu.Stanford.Nlp.International.Arabic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Arabic;
using Edu.Stanford.Nlp.Trees.Treebank;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic.Pipeline
{
	/// <summary>Converts raw ATB trees into a format appropriate for treebank parsing.</summary>
	/// <author>Spence Green</author>
	public class ATBArabicDataset : AbstractDataset
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Arabic.Pipeline.ATBArabicDataset));

		public ATBArabicDataset()
			: base()
		{
			//Read the raw file as UTF-8 irrespective of output encoding
			treebank = new DiskTreebank(new ArabicTreeReaderFactory.ArabicRawTreeReaderFactory(true), "UTF-8");
		}

		public override void Build()
		{
			foreach (File path in pathsToData)
			{
				if (splitFilter == null)
				{
					treebank.LoadPath(path, treeFileExtension, false);
				}
				else
				{
					treebank.LoadPath(path, splitFilter);
				}
			}
			PrintWriter outfile = null;
			PrintWriter flatFile = null;
			try
			{
				outfile = new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(outFileName), "UTF-8")));
				flatFile = (makeFlatFile) ? new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(flatFileName), "UTF-8"))) : null;
				treebank.Apply(new ATBArabicDataset.ArabicRawTreeNormalizer(this, outfile, flatFile));
				outputFileList.Add(outFileName);
				if (makeFlatFile)
				{
					outputFileList.Add(flatFileName);
					toStringBuffer.Append(" Made flat files\n");
				}
			}
			catch (UnsupportedEncodingException e)
			{
				System.Console.Error.Printf("%s: Filesystem does not support UTF-8 output\n", this.GetType().FullName);
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException)
			{
				System.Console.Error.Printf("%s: Could not open %s for writing\n", this.GetType().FullName, outFileName);
			}
			finally
			{
				if (outfile != null)
				{
					outfile.Close();
				}
				if (flatFile != null)
				{
					flatFile.Close();
				}
			}
		}

		public override bool SetOptions(Properties opts)
		{
			bool ret = base.SetOptions(opts);
			if (lexMapper == null)
			{
				lexMapper = new DefaultLexicalMapper();
				lexMapper.Setup(null, lexMapOptions.Split(","));
			}
			if (pathsToMappings.Count != 0)
			{
				if (posMapper == null)
				{
					posMapper = new LDCPosMapper(addDeterminer);
				}
				string[] mapOpts = posMapOptions.Split(",");
				foreach (File path in pathsToMappings)
				{
					posMapper.Setup(path, mapOpts);
				}
			}
			return ret;
		}

		/// <summary>
		/// A
		/// <see cref="Edu.Stanford.Nlp.Trees.ITreeVisitor"/>
		/// for raw ATB trees. This class performs
		/// minimal pre-processing (for example, it does not prune traces). It also provides
		/// a facility via <code>enableIBMArabicEscaping</code> for sub-classes to process
		/// IBM Arabic parse trees.
		/// </summary>
		protected internal class ArabicRawTreeNormalizer : ITreeVisitor
		{
			protected internal readonly Buckwalter encodingMap;

			protected internal readonly PrintWriter outfile;

			protected internal readonly PrintWriter flatFile;

			protected internal readonly IPredicate<Tree> nullFilter;

			protected internal readonly IPredicate<Tree> aOverAFilter;

			protected internal readonly ITreeFactory tf;

			protected internal readonly ITreebankLanguagePack tlp;

			public ArabicRawTreeNormalizer(ATBArabicDataset _enclosing, PrintWriter outFile, PrintWriter flatFile)
			{
				this._enclosing = _enclosing;
				this.encodingMap = (this._enclosing.encoding == Dataset.Encoding.Utf8) ? new Buckwalter() : new Buckwalter(true);
				this.outfile = outFile;
				this.flatFile = flatFile;
				this.nullFilter = new ArabicTreeNormalizer.ArabicEmptyFilter();
				this.aOverAFilter = new BobChrisTreeNormalizer.AOverAFilter();
				this.tf = new LabeledScoredTreeFactory();
				this.tlp = new ArabicTreebankLanguagePack();
			}

			protected internal virtual void ProcessPreterminal(Tree node)
			{
				string rawTag = node.Value();
				string posTag = (this._enclosing.posMapper == null) ? rawTag : this._enclosing.posMapper.Map(rawTag, node.FirstChild().Value());
				string rawWord = node.FirstChild().Value();
				//Hack for LDC2008E22 idiosyncrasy in which (NOUN.VN F) is a pre-terminal/word
				//This is a bare fathatan that bears no semantic content. Replacing it with the
				//conjunction ف / f .
				if (rawWord.Equals("F"))
				{
					posTag = posTag.Equals("NOUN.VN") ? "CONJ" : "CC";
					rawWord = "f";
				}
				// Hack for annotation error in ATB
				if (rawWord.StartsWith("MERGE_with_previous_token:"))
				{
					rawWord = rawWord.Replace("MERGE_with_previous_token:", string.Empty);
				}
				// Hack for annotation error in ATB
				if (rawWord.Contains("e"))
				{
					rawWord = rawWord.Replace("e", string.Empty);
				}
				string finalWord = this._enclosing.lexMapper.Map(rawTag, rawWord);
				if (this._enclosing.lexMapper.CanChangeEncoding(rawTag, finalWord))
				{
					finalWord = this.encodingMap.Apply(finalWord);
				}
				node.SetValue(posTag);
				if (this._enclosing.morphDelim == null)
				{
					node.FirstChild().SetValue(finalWord);
					if (node.FirstChild().Label() is CoreLabel)
					{
						((CoreLabel)node.FirstChild().Label()).SetWord(finalWord);
					}
				}
				else
				{
					node.FirstChild().SetValue(finalWord + this._enclosing.morphDelim + rawTag);
				}
			}

			//Modifies the tree in-place...should be run after
			//mapping to reduced tag set
			public virtual Tree ArabicAoverAFilter(Tree t)
			{
				if (t == null || t.IsLeaf() || t.IsPreTerminal())
				{
					return t;
				}
				//Specific nodes to filter out
				if (t.NumChildren() == 1)
				{
					Tree fc = t.FirstChild();
					//A over A nodes i.e. from BobChrisTreeNormalizer
					if (t.Label() != null && fc.Label() != null && t.Value().Equals(fc.Value()))
					{
						t.SetChildren(fc.Children());
					}
				}
				foreach (Tree kid in t.GetChildrenAsList())
				{
					this.ArabicAoverAFilter(kid);
				}
				return t;
			}

			public virtual void VisitTree(Tree t)
			{
				// Filter out XBar trees
				if (t == null || t.Value().Equals("X"))
				{
					return;
				}
				if (t.Yield().Count > this._enclosing.maxLen)
				{
					return;
				}
				// Strip out traces and pronoun deletion markers,
				t = t.Prune(this.nullFilter, this.tf);
				t = this.ArabicAoverAFilter(t);
				// Visit nodes with a custom visitor
				if (this._enclosing.customTreeVisitor != null)
				{
					this._enclosing.customTreeVisitor.VisitTree(t);
				}
				// Process each node in the tree
				foreach (Tree node in t)
				{
					if (node.IsPreTerminal())
					{
						this.ProcessPreterminal(node);
					}
					if (this._enclosing.removeDashTags && !node.IsLeaf())
					{
						node.SetValue(this.tlp.BasicCategory(node.Value()));
					}
				}
				// Add a ROOT node if necessary
				if (this._enclosing.addRoot && t.Value() != null && !t.Value().Equals("ROOT"))
				{
					t = this.tf.NewTreeNode("ROOT", Collections.SingletonList(t));
				}
				// Output the trees to file
				this.outfile.Println(t.ToString());
				if (this.flatFile != null)
				{
					string flatString = (this._enclosing.removeEscapeTokens) ? ATBTreeUtils.UnEscape(ATBTreeUtils.FlattenTree(t)) : ATBTreeUtils.FlattenTree(t);
					this.flatFile.Println(flatString);
				}
			}

			private readonly ATBArabicDataset _enclosing;
		}
	}
}
