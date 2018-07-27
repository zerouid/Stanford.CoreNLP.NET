using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Semgraph.Semgrex;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Pred
{
	[System.Serializable]
	public class SsurgOrPred : List<ISsurgPred>, ISsurgPred
	{
		private const long serialVersionUID = 4581463857927967518L;

		/// <exception cref="System.Exception"/>
		public virtual bool Test(SemgrexMatcher matcher)
		{
			foreach (ISsurgPred term in this)
			{
				if (term.Test(matcher))
				{
					return true;
				}
			}
			return false;
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
