using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Arabic;
using Edu.Stanford.Nlp.Trees.Treebank;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.International.Arabic.Pipeline
{
	/// <summary>Decimates a set of ATB parse trees.</summary>
	/// <remarks>
	/// Decimates a set of ATB parse trees. For every 10 parse trees, eight are added to the training set, and one
	/// is added to each of the dev and test sets.
	/// </remarks>
	/// <author>Spence Green</author>
	public class DecimatedArabicDataset : ATBArabicDataset
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(DecimatedArabicDataset));

		private bool taggedOutput = false;

		private string wordTagDelim = "_";

		public override void Build()
		{
			//Set specific options for this dataset
			if (options.Contains(ConfigParser.paramSplit))
			{
				System.Console.Error.Printf("%s: Ignoring split parameter for this dataset type\n", this.GetType().FullName);
			}
			else
			{
				if (options.Contains(ConfigParser.paramTagDelim))
				{
					wordTagDelim = options.GetProperty(ConfigParser.paramTagDelim);
					taggedOutput = true;
				}
			}
			foreach (File path in pathsToData)
			{
				int prevSize = treebank.Count;
				treebank.LoadPath(path, treeFileExtension, false);
				toStringBuffer.Append(string.Format(" Loaded %d trees from %s\n", treebank.Count - prevSize, path.GetPath()));
				prevSize = treebank.Count;
			}
			DecimatedArabicDataset.ArabicTreeDecimatedNormalizer tv = new DecimatedArabicDataset.ArabicTreeDecimatedNormalizer(this, outFileName, makeFlatFile, taggedOutput);
			treebank.Apply(tv);
			Sharpen.Collections.AddAll(outputFileList, tv.GetFilenames());
			tv.CloseOutputFiles();
		}

		public class ArabicTreeDecimatedNormalizer : ATBArabicDataset.ArabicRawTreeNormalizer
		{
			private int treesVisited = 0;

			private readonly string trainExtension = ".train";

			private readonly string testExtension = ".test";

			private readonly string devExtension = ".dev";

			private readonly string flatExtension = ".flat";

			private bool makeFlatFile = false;

			private bool taggedOutput = false;

			private IDictionary<string, string> outFilenames;

			private IDictionary<string, PrintWriter> outFiles;

			public ArabicTreeDecimatedNormalizer(DecimatedArabicDataset _enclosing, string filePrefix, bool makeFlat, bool makeTagged)
				: base(_enclosing)
			{
				this._enclosing = _enclosing;
				this.makeFlatFile = makeFlat;
				this.taggedOutput = makeTagged;
				//Setup the decimation output files
				this.outFilenames = Generics.NewHashMap();
				this.outFilenames[this.trainExtension] = filePrefix + this.trainExtension;
				this.outFilenames[this.testExtension] = filePrefix + this.testExtension;
				this.outFilenames[this.devExtension] = filePrefix + this.devExtension;
				if (this.makeFlatFile)
				{
					this.outFilenames[this.trainExtension + this.flatExtension] = filePrefix + this.trainExtension + this.flatExtension;
					this.outFilenames[this.testExtension + this.flatExtension] = filePrefix + this.testExtension + this.flatExtension;
					this.outFilenames[this.devExtension + this.flatExtension] = filePrefix + this.devExtension + this.flatExtension;
				}
				this.SetupOutputFiles();
			}

			private void SetupOutputFiles()
			{
				PrintWriter outfile = null;
				string curOutFileName = string.Empty;
				try
				{
					this.outFiles = Generics.NewHashMap();
					foreach (string keyForFile in this.outFilenames.Keys)
					{
						curOutFileName = this.outFilenames[keyForFile];
						if (!this.makeFlatFile && curOutFileName.Contains(this.flatExtension))
						{
							continue;
						}
						outfile = new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(curOutFileName), "UTF-8")));
						this.outFiles[keyForFile] = outfile;
					}
				}
				catch (UnsupportedEncodingException e)
				{
					System.Console.Error.Printf("%s: Filesystem does not support UTF-8 output\n", this.GetType().FullName);
					Sharpen.Runtime.PrintStackTrace(e);
				}
				catch (FileNotFoundException)
				{
					System.Console.Error.Printf("%s: Could not open %s for writing\n", this.GetType().FullName, curOutFileName);
				}
			}

			public virtual void CloseOutputFiles()
			{
				foreach (string keyForFile in this.outFiles.Keys)
				{
					this.outFiles[keyForFile].Close();
				}
			}

			public override void VisitTree(Tree t)
			{
				if (t == null || t.Value().Equals("X"))
				{
					return;
				}
				t = t.Prune(this.nullFilter, new LabeledScoredTreeFactory());
				//Do *not* strip traces here. The ArabicTreeReader will do that if needed
				foreach (Tree node in t)
				{
					if (node.IsPreTerminal())
					{
						this.ProcessPreterminal(node);
					}
				}
				this.treesVisited++;
				string flatString = (this.makeFlatFile) ? ATBTreeUtils.FlattenTree(t) : null;
				//Do the decimation
				if (this.treesVisited % 9 == 0)
				{
					this.Write(t, this.outFiles[this.devExtension]);
					if (this.makeFlatFile)
					{
						this.outFiles[this.devExtension + this.flatExtension].Println(flatString);
					}
				}
				else
				{
					if (this.treesVisited % 10 == 0)
					{
						this.Write(t, this.outFiles[this.testExtension]);
						if (this.makeFlatFile)
						{
							this.outFiles[this.testExtension + this.flatExtension].Println(flatString);
						}
					}
					else
					{
						this.Write(t, this.outFiles[this.trainExtension]);
						if (this.makeFlatFile)
						{
							this.outFiles[this.trainExtension + this.flatExtension].Println(flatString);
						}
					}
				}
			}

			private void Write(Tree t, PrintWriter pw)
			{
				if (this.taggedOutput)
				{
					pw.Println(ATBTreeUtils.TaggedStringFromTree(t, this._enclosing.removeEscapeTokens, this._enclosing.wordTagDelim));
				}
				else
				{
					t.PennPrint(pw);
				}
			}

			public virtual IList<string> GetFilenames()
			{
				IList<string> filenames = new List<string>();
				foreach (string keyForFile in this.outFilenames.Keys)
				{
					filenames.Add(this.outFilenames[keyForFile]);
				}
				return filenames;
			}

			private readonly DecimatedArabicDataset _enclosing;
		}
	}
}
