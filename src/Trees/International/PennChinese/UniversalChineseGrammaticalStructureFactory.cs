using Edu.Stanford.Nlp.Trees;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	public class UniversalChineseGrammaticalStructureFactory : IGrammaticalStructureFactory
	{
		private readonly IPredicate<string> puncFilter;

		private readonly IHeadFinder hf;

		public UniversalChineseGrammaticalStructureFactory()
			: this(null, null)
		{
		}

		public UniversalChineseGrammaticalStructureFactory(IPredicate<string> puncFilter)
			: this(puncFilter, null)
		{
		}

		public UniversalChineseGrammaticalStructureFactory(IPredicate<string> puncFilter, IHeadFinder hf)
		{
			this.puncFilter = puncFilter;
			this.hf = hf;
		}

		public virtual UniversalChineseGrammaticalStructure NewGrammaticalStructure(Tree t)
		{
			if (puncFilter == null && hf == null)
			{
				return new UniversalChineseGrammaticalStructure(t);
			}
			else
			{
				if (hf == null)
				{
					return new UniversalChineseGrammaticalStructure(t, puncFilter);
				}
				else
				{
					return new UniversalChineseGrammaticalStructure(t, puncFilter, hf);
				}
			}
		}
	}
}
