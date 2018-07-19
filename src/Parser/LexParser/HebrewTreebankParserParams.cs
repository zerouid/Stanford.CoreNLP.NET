using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Hebrew;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Initial version of a parser pack for the HTB.</summary>
	/// <remarks>
	/// Initial version of a parser pack for the HTB. Not yet integrated
	/// into the Stanford parser.
	/// <p>
	/// This package assumes the romanized orthographic form of Hebrew as
	/// used in the treebank.
	/// </remarks>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class HebrewTreebankParserParams : AbstractTreebankParserParams
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.HebrewTreebankParserParams));

		private const long serialVersionUID = -3466519995341208619L;

		private readonly StringBuilder optionsString;

		private static readonly string[] EmptyStringArray = new string[0];

		public HebrewTreebankParserParams()
			: this(new HebrewTreebankLanguagePack())
		{
		}

		protected internal HebrewTreebankParserParams(ITreebankLanguagePack tlp)
			: base(tlp)
		{
			optionsString = new StringBuilder();
			optionsString.Append("HebrewTreebankParserParams\n");
		}

		public override ITreeTransformer Collinizer()
		{
			return new TreeCollinizer(tlp, true, false);
		}

		/// <summary>Stand-in collinizer does nothing to the tree.</summary>
		public override ITreeTransformer CollinizerEvalb()
		{
			return Collinizer();
		}

		public override Edu.Stanford.Nlp.Trees.MemoryTreebank MemoryTreebank()
		{
			return new Edu.Stanford.Nlp.Trees.MemoryTreebank(TreeReaderFactory(), inputEncoding);
		}

		public override Edu.Stanford.Nlp.Trees.DiskTreebank DiskTreebank()
		{
			return new Edu.Stanford.Nlp.Trees.DiskTreebank(TreeReaderFactory(), inputEncoding);
		}

		public override void Display()
		{
			log.Info(optionsString.ToString());
		}

		//TODO Add Reut's rules (from her thesis).
		public override IHeadFinder HeadFinder()
		{
			return new LeftHeadFinder();
		}

		public override IHeadFinder TypedDependencyHeadFinder()
		{
			return HeadFinder();
		}

		public override string[] SisterSplitters()
		{
			return EmptyStringArray;
		}

		public override Tree TransformTree(Tree t, Tree root)
		{
			return t;
		}

		public override IList<IHasWord> DefaultTestSentence()
		{
			string[] sent = new string[] { "H", "MWX", "MTPLC", "LA", "RQ", "M", "H", "TWPEH", "H", "MBIFH", "ALA", "GM", "M", "DRKI", "H", "HERMH", "yyDOT" };
			return SentenceUtils.ToWordList(sent);
		}

		public override ITreeReaderFactory TreeReaderFactory()
		{
			return new HebrewTreeReaderFactory();
		}
	}
}
