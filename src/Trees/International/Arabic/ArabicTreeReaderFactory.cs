using Edu.Stanford.Nlp.Trees;
using Java.IO;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Arabic
{
	/// <summary>Reads ArabicTreebank trees.</summary>
	/// <remarks>
	/// Reads ArabicTreebank trees.  See
	/// <see cref="ArabicTreeNormalizer"/>
	/// for the
	/// meaning of the constructor parameters.
	/// </remarks>
	/// <author>Roger Levy</author>
	/// <author>Christopher Manning</author>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class ArabicTreeReaderFactory : ITreeReaderFactory
	{
		private const long serialVersionUID = 1973767605277873017L;

		private readonly bool retainNPTmp;

		private readonly bool retainNPSbj;

		private readonly bool retainPRD;

		private readonly bool retainPPClr;

		private readonly bool changeNoLabels;

		private readonly bool filterX;

		private readonly bool noNormalization;

		public ArabicTreeReaderFactory()
			: this(false, false, false, false, false, false, false)
		{
		}

		public ArabicTreeReaderFactory(bool retainNPTmp, bool retainPRD, bool changeNoLabels, bool filterX, bool retainNPSbj, bool noNormalization, bool retainPPClr)
		{
			this.retainNPTmp = retainNPTmp;
			this.retainPRD = retainPRD;
			this.changeNoLabels = changeNoLabels;
			this.filterX = filterX;
			this.retainNPSbj = retainNPSbj;
			this.noNormalization = noNormalization;
			this.retainPPClr = retainPPClr;
		}

		public virtual ITreeReader NewTreeReader(Reader @in)
		{
			ITreeReader tr = null;
			if (noNormalization)
			{
				tr = new PennTreeReader(@in, new LabeledScoredTreeFactory(), new TreeNormalizer(), new ArabicTreebankTokenizer(@in));
			}
			else
			{
				tr = new PennTreeReader(@in, new LabeledScoredTreeFactory(), new ArabicTreeNormalizer(retainNPTmp, retainPRD, changeNoLabels, retainNPSbj, retainPPClr), new ArabicTreebankTokenizer(@in));
			}
			if (filterX)
			{
				tr = new FilteringTreeReader(tr, new ArabicTreeReaderFactory.XFilter());
			}
			return tr;
		}

		[System.Serializable]
		internal class XFilter : IPredicate<Tree>
		{
			private const long serialVersionUID = -4522060160716318895L;

			public XFilter()
			{
			}

			public virtual bool Test(Tree t)
			{
				return !(t.NumChildren() == 1 && "X".Equals(t.FirstChild().Value()));
			}
		}

		[System.Serializable]
		public class ArabicRawTreeReaderFactory : ArabicTreeReaderFactory
		{
			private const long serialVersionUID = -5693371540982097793L;

			public ArabicRawTreeReaderFactory()
				: base(false, false, true, false, false, false, false)
			{
			}

			public ArabicRawTreeReaderFactory(bool noNormalization)
				: base(false, false, true, false, false, noNormalization, false)
			{
			}
		}
	}
}
