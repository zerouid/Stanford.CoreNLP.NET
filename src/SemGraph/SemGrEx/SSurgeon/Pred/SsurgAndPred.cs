using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Pred
{
	[System.Serializable]
	public class SsurgAndPred : List<ISsurgPred>, ISsurgPred
	{
		private const long serialVersionUID = 760573332472162149L;

		/// <exception cref="System.Exception"/>
		public virtual bool Test(SemgrexMatcher matcher)
		{
			foreach (ISsurgPred term in this)
			{
				if (term.Test(matcher) == false)
				{
					return false;
				}
			}
			return true;
		}

		public override string ToString()
		{
			StringWriter buf = new StringWriter();
			buf.Write("(ssurg-and");
			foreach (ISsurgPred term in this)
			{
				buf.Write(" ");
				buf.Write(term.ToString());
			}
			buf.Write(")");
			return buf.ToString();
		}
	}
}
