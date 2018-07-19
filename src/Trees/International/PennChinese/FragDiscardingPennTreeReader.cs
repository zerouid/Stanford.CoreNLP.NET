using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <author>Galen Andrew</author>
	public class FragDiscardingPennTreeReader : PennTreeReader
	{
		public FragDiscardingPennTreeReader(Reader @in, ITreeFactory tf, TreeNormalizer tn, ITokenizer<string> tk)
			: base(@in, tf, tn, tk)
		{
		}

		//  private static PrintWriter pw;
		//
		//  static {
		//    try {
		//      if (false) {
		//        pw = new PrintWriter(new OutputStreamWriter(new FileOutputStream("discardedFRAGs.chi"), "GB18030"), true);
		//      }
		//    } catch (Exception e) {
		//      throw new RuntimeException("");
		//    }
		//  }
		/// <exception cref="System.IO.IOException"/>
		public override Tree ReadTree()
		{
			Tree tr = base.ReadTree();
			while (tr != null && tr.FirstChild().Value().Equals("FRAG"))
			{
				//      if (pw != null) {
				//        pw.println("Discarding Tree:");
				//        tr.pennPrint(pw);
				//      }
				tr = base.ReadTree();
			}
			return tr;
		}
	}
}
