


namespace Edu.Stanford.Nlp.Trees
{
	public class UniversalEnglishGrammaticalStructureFactory : IGrammaticalStructureFactory
	{
		private readonly IPredicate<string> puncFilter;

		private readonly IHeadFinder hf;

		public UniversalEnglishGrammaticalStructureFactory()
			: this(null, null)
		{
		}

		public UniversalEnglishGrammaticalStructureFactory(IPredicate<string> puncFilter)
			: this(puncFilter, null)
		{
		}

		public UniversalEnglishGrammaticalStructureFactory(IPredicate<string> puncFilter, IHeadFinder hf)
		{
			this.puncFilter = puncFilter;
			this.hf = hf;
		}

		public virtual UniversalEnglishGrammaticalStructure NewGrammaticalStructure(Tree t)
		{
			if (puncFilter == null && hf == null)
			{
				return new UniversalEnglishGrammaticalStructure(t);
			}
			else
			{
				if (hf == null)
				{
					return new UniversalEnglishGrammaticalStructure(t, puncFilter);
				}
				else
				{
					return new UniversalEnglishGrammaticalStructure(t, puncFilter, hf);
				}
			}
		}
	}
}
