using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Tuebadz
{
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class TueBaDZTreeReaderFactory : ITreeReaderFactory
	{
		private const long serialVersionUID = 1614799885744961795L;

		private ITreebankLanguagePack tlp;

		private int nodeCleanup;

		public TueBaDZTreeReaderFactory(ITreebankLanguagePack tlp)
			: this(tlp, 0)
		{
		}

		public TueBaDZTreeReaderFactory(ITreebankLanguagePack tlp, int nodeCleanup)
		{
			this.tlp = tlp;
			this.nodeCleanup = nodeCleanup;
		}

		public virtual ITreeReader NewTreeReader(Reader @in)
		{
			TreeNormalizer tn1 = new GrammaticalFunctionTreeNormalizer(tlp, nodeCleanup);
			TueBaDZPennTreeNormalizer tn2 = new TueBaDZPennTreeNormalizer(tlp, nodeCleanup);
			TreeNormalizer norm = new OrderedCombinationTreeNormalizer(Arrays.AsList(tn1, tn2));
			return new PennTreeReader(@in, new LabeledScoredTreeFactory(new StringLabelFactory()), norm);
		}
	}
}
