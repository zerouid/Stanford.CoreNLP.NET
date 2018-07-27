using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	public class ChineseGrammaticalStructureFactory : IGrammaticalStructureFactory
	{
		private readonly IPredicate<string> puncFilter;

		private readonly IHeadFinder hf;

		public ChineseGrammaticalStructureFactory()
			: this(null, null)
		{
		}

		public ChineseGrammaticalStructureFactory(IPredicate<string> puncFilter)
			: this(puncFilter, null)
		{
		}

		public ChineseGrammaticalStructureFactory(IPredicate<string> puncFilter, IHeadFinder hf)
		{
			this.puncFilter = puncFilter;
			this.hf = hf;
		}

		public virtual ChineseGrammaticalStructure NewGrammaticalStructure(Tree t)
		{
			if (puncFilter == null && hf == null)
			{
				return new ChineseGrammaticalStructure(t);
			}
			else
			{
				if (hf == null)
				{
					return new ChineseGrammaticalStructure(t, puncFilter);
				}
				else
				{
					return new ChineseGrammaticalStructure(t, puncFilter, hf);
				}
			}
		}
	}
}
