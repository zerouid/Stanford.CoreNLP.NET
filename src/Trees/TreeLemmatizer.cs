using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;


namespace Edu.Stanford.Nlp.Trees
{
	public class TreeLemmatizer : ITreeTransformer
	{
		public virtual Tree TransformTree(Tree t)
		{
			Morphology morphology = new Morphology();
			IList<TaggedWord> tagged = null;
			int index = 0;
			foreach (Tree leaf in t.GetLeaves())
			{
				ILabel label = leaf.Label();
				if (label == null)
				{
					continue;
				}
				string tag;
				if (!(label is IHasTag) || ((IHasTag)label).Tag() == null)
				{
					if (tagged == null)
					{
						tagged = t.TaggedYield();
					}
					tag = tagged[index].Tag();
				}
				else
				{
					tag = ((IHasTag)label).Tag();
				}
				if (!(label is IHasLemma))
				{
					throw new ArgumentException("Got a tree with labels which do not support lemma");
				}
				((IHasLemma)label).SetLemma(morphology.Lemma(label.Value(), tag, true));
				++index;
			}
			return t;
		}
	}
}
