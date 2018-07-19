using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Trees;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Negra
{
	/// <summary>
	/// A TreeReaderFactory for the Negra and Tiger treebanks in their
	/// Penn Treebank compatible export format.
	/// </summary>
	/// <author>Roger Levy</author>
	[System.Serializable]
	public class NegraPennTreeReaderFactory : ITreeReaderFactory
	{
		private const long serialVersionUID = 5731352106152470304L;

		private readonly int nodeCleanup;

		private readonly ITreebankLanguagePack tlp;

		private readonly bool treeNormalizerInsertNPinPP;

		public NegraPennTreeReaderFactory()
			: this(2, false, true, new NegraPennLanguagePack())
		{
		}

		public NegraPennTreeReaderFactory(ITreebankLanguagePack tlp)
			: this(0, false, false, tlp)
		{
		}

		public NegraPennTreeReaderFactory(int nodeCleanup, bool treeNormalizerInsertNPinPP, bool treeNormalizerLeaveGF, ITreebankLanguagePack tlp)
		{
			// = 0;
			// = false;
			this.nodeCleanup = nodeCleanup;
			this.treeNormalizerInsertNPinPP = treeNormalizerInsertNPinPP;
			this.tlp = tlp;
		}

		public virtual ITreeReader NewTreeReader(Reader @in)
		{
			NegraPennTreeNormalizer tn = new NegraPennTreeNormalizer(tlp, nodeCleanup);
			if (treeNormalizerInsertNPinPP)
			{
				tn.SetInsertNPinPP(true);
			}
			return new PennTreeReader(@in, new LabeledScoredTreeFactory(), tn, new NegraPennTokenizer(@in));
		}

		/// <param name="args">File to run on</param>
		public static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				System.Console.Out.Printf("Usage: java %s tree_file%n", typeof(Edu.Stanford.Nlp.Trees.International.Negra.NegraPennTreeReaderFactory).FullName);
				return;
			}
			ITreebankLanguagePack tlp = new NegraPennLanguagePack();
			ITreeReaderFactory trf = new Edu.Stanford.Nlp.Trees.International.Negra.NegraPennTreeReaderFactory(2, false, false, tlp);
			try
			{
				ITreeReader tr = trf.NewTreeReader(IOUtils.ReaderFromString(args[0], tlp.GetEncoding()));
				for (Tree t; (t = tr.ReadTree()) != null; )
				{
					t.PennPrint();
				}
				tr.Close();
			}
			catch (UnsupportedEncodingException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
