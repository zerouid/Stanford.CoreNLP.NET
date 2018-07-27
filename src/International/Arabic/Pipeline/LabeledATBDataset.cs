using System;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.International.Arabic.Pipeline
{
	public class LabeledATBDataset : ATBArabicDataset
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(LabeledATBDataset));

		public override void Build()
		{
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
			}
			PrintWriter outfile = null;
			PrintWriter flatFile = null;
			try
			{
				outfile = new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(outFileName), "UTF-8")));
				flatFile = (makeFlatFile) ? new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(flatFileName), "UTF-8"))) : null;
				ATBArabicDataset.ArabicRawTreeNormalizer tv = new LabeledATBDataset.LabelingTreeNormalizer(this, outfile, flatFile);
				treebank.Apply(tv);
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

		protected internal class LabelingTreeNormalizer : ATBArabicDataset.ArabicRawTreeNormalizer
		{
			private readonly Pattern leftClitic;

			private readonly Pattern rightClitic;

			public LabelingTreeNormalizer(LabeledATBDataset _enclosing, PrintWriter outFile, PrintWriter flatFile)
				: base(_enclosing)
			{
				this._enclosing = _enclosing;
				this.leftClitic = Pattern.Compile("^-");
				this.rightClitic = Pattern.Compile("-$");
			}

			protected internal override void ProcessPreterminal(Tree node)
			{
				string rawTag = node.Value();
				if (rawTag.Equals("-NONE-"))
				{
					return;
				}
				string rawWord = node.FirstChild().Value().Trim();
				Matcher left = this.leftClitic.Matcher(rawWord);
				bool hasLeft = left.Find();
				Matcher right = this.rightClitic.Matcher(rawWord);
				bool hasRight = right.Find();
				if (rawTag.Equals("PUNC") || !(hasRight || hasLeft))
				{
					node.FirstChild().SetValue("XSEG");
				}
				else
				{
					if (hasRight && hasLeft)
					{
						node.FirstChild().SetValue("SEGC");
					}
					else
					{
						if (hasRight)
						{
							node.FirstChild().SetValue("SEGL");
						}
						else
						{
							if (hasLeft)
							{
								node.FirstChild().SetValue("SEGR");
							}
							else
							{
								throw new Exception("Messy token: " + rawWord);
							}
						}
					}
				}
			}

			private readonly LabeledATBDataset _enclosing;
		}
	}
}
