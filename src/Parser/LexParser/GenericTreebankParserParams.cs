using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	[System.Serializable]
	public class GenericTreebankParserParams : AbstractTreebankParserParams
	{
		private const long serialVersionUID = -617650500538652513L;

		protected internal GenericTreebankParserParams(ITreebankLanguagePack tlp)
			: base(tlp)
		{
		}

		// TODO Auto-generated constructor stub
		public override ITreeTransformer Collinizer()
		{
			// TODO Auto-generated method stub
			return null;
		}

		public override ITreeTransformer CollinizerEvalb()
		{
			// TODO Auto-generated method stub
			return null;
		}

		public override void Display()
		{
		}

		// TODO Auto-generated method stub
		public override IHeadFinder HeadFinder()
		{
			// TODO Auto-generated method stub
			return null;
		}

		public override IHeadFinder TypedDependencyHeadFinder()
		{
			// TODO Auto-generated method stub
			return null;
		}

		public override Edu.Stanford.Nlp.Trees.MemoryTreebank MemoryTreebank()
		{
			// TODO Auto-generated method stub
			return null;
		}

		public override int SetOptionFlag(string[] args, int i)
		{
			// TODO Auto-generated method stub
			return 0;
		}

		public override string[] SisterSplitters()
		{
			// TODO Auto-generated method stub
			return null;
		}

		public override Tree TransformTree(Tree t, Tree root)
		{
			// TODO Auto-generated method stub
			return null;
		}

		public override IList<IHasWord> DefaultTestSentence()
		{
			// TODO Auto-generated method stub
			return null;
		}

		public override Edu.Stanford.Nlp.Trees.DiskTreebank DiskTreebank()
		{
			// TODO Auto-generated method stub
			return null;
		}

		public override ITreeReaderFactory TreeReaderFactory()
		{
			// TODO Auto-generated method stub
			return null;
		}
	}
}
