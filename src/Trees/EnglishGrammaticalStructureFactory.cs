using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	public class EnglishGrammaticalStructureFactory : IGrammaticalStructureFactory
	{
		private readonly IPredicate<string> puncFilter;

		private readonly IHeadFinder hf;

		public EnglishGrammaticalStructureFactory()
			: this(null, null)
		{
		}

		public EnglishGrammaticalStructureFactory(IPredicate<string> puncFilter)
			: this(puncFilter, null)
		{
		}

		public EnglishGrammaticalStructureFactory(IPredicate<string> puncFilter, IHeadFinder hf)
		{
			this.puncFilter = puncFilter;
			this.hf = hf;
		}

		public virtual EnglishGrammaticalStructure NewGrammaticalStructure(Tree t)
		{
			if (puncFilter == null && hf == null)
			{
				return new EnglishGrammaticalStructure(t);
			}
			else
			{
				if (hf == null)
				{
					return new EnglishGrammaticalStructure(t, puncFilter);
				}
				else
				{
					return new EnglishGrammaticalStructure(t, puncFilter, hf);
				}
			}
		}
	}
}
