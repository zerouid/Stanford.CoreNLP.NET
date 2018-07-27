using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Arabic;
using Edu.Stanford.Nlp.Trees.Treebank;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.International.Arabic.Pipeline
{
	/// <summary>
	/// Converts ATB gold parse trees to a format appropriate for training a POS tagger (especially
	/// the Stanford POS tagger!).
	/// </summary>
	/// <author>Spence Green</author>
	public class TaggedArabicDataset : ATBArabicDataset
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(TaggedArabicDataset));

		private string wordTagDelim = "_";

		public override void Build()
		{
			//Set specific options for this dataset
			if (options.Contains(ConfigParser.paramTagDelim))
			{
				wordTagDelim = options.GetProperty(ConfigParser.paramTagDelim);
			}
			foreach (File path in pathsToData)
			{
				int prevSize = treebank.Count;
				if (splitFilter == null)
				{
					treebank.LoadPath(path, treeFileExtension, false);
				}
				else
				{
					treebank.LoadPath(path, splitFilter);
				}
				toStringBuffer.Append(string.Format(" Loaded %d trees from %s\n", treebank.Count - prevSize, path.GetPath()));
				prevSize = treebank.Count;
			}
			PrintWriter outfile = null;
			PrintWriter flatFile = null;
			try
			{
				outfile = new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(outFileName), "UTF-8")));
				flatFile = (makeFlatFile) ? new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(flatFileName), "UTF-8"))) : null;
				TaggedArabicDataset.ArabicTreeTaggedNormalizer tv = new TaggedArabicDataset.ArabicTreeTaggedNormalizer(this, outfile, flatFile);
				treebank.Apply(tv);
				outputFileList.Add(outFileName);
				if (makeFlatFile)
				{
					outputFileList.Add(flatFileName);
				}
			}
			catch (UnsupportedEncodingException e)
			{
				System.Console.Error.Printf("%s: Filesystem does not support UTF-8 output%n", this.GetType().FullName);
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException)
			{
				System.Console.Error.Printf("%s: Could not open %s for writing%n", this.GetType().FullName, outFileName);
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

		protected internal class ArabicTreeTaggedNormalizer : ATBArabicDataset.ArabicRawTreeNormalizer
		{
			public ArabicTreeTaggedNormalizer(TaggedArabicDataset _enclosing, PrintWriter outFile, PrintWriter flatFile)
				: base(_enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override void VisitTree(Tree t)
			{
				if (t == null || t.Value().Equals("X"))
				{
					return;
				}
				t = t.Prune(this.nullFilter, new LabeledScoredTreeFactory());
				foreach (Tree node in t)
				{
					if (node.IsPreTerminal())
					{
						this.ProcessPreterminal(node);
					}
				}
				this.outfile.Println(ATBTreeUtils.TaggedStringFromTree(t, this._enclosing.removeEscapeTokens, this._enclosing.wordTagDelim));
				if (this.flatFile != null)
				{
					this.flatFile.Println(ATBTreeUtils.FlattenTree(t));
				}
			}

			private readonly TaggedArabicDataset _enclosing;
		}
	}
}
