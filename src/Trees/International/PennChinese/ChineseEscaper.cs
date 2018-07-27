using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>An Escaper for Chinese normalization to match Treebank.</summary>
	/// <remarks>
	/// An Escaper for Chinese normalization to match Treebank.
	/// Currently normalizes "ASCII" characters into the full-width
	/// range used inside the Penn Chinese Treebank.
	/// <p/>
	/// <i>Notes:</i> Smart quotes appear in CTB, and are left unchanged.
	/// I think you get various hyphen types from U+2000 range too - certainly,
	/// Roger lists them in LanguagePack.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class ChineseEscaper : IFunction<IList<IHasWord>, IList<IHasWord>>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(ChineseEscaper));

		/// <summary>IBM entity normalization patterns</summary>
		private static readonly Pattern p2 = Pattern.Compile("\\$[a-z]+_\\((.*?)\\|\\|.*?\\)");

		/// <summary><i>Note:</i> At present this clobbers the input list items.</summary>
		/// <remarks>
		/// <i>Note:</i> At present this clobbers the input list items.
		/// This should be fixed.
		/// </remarks>
		public virtual IList<IHasWord> Apply(IList<IHasWord> arg)
		{
			IList<IHasWord> ans = new List<IHasWord>(arg);
			foreach (IHasWord wd in ans)
			{
				string w = wd.Word();
				Matcher m2 = p2.Matcher(w);
				// log.info("Escaper: w is " + w);
				if (m2.Find())
				{
					// log.info("  Found pattern.");
					w = m2.ReplaceAll("$1");
				}
				// log.info("  Changed it to: " + w);
				string newW = UTF8EquivalenceFunction.ReplaceAscii(w);
				wd.SetWord(newW);
			}
			return ans;
		}
	}
}
