using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading
{
	/// <summary>Class for comparing the output of information extraction to a gold standard, and printing the results.</summary>
	/// <remarks>
	/// Class for comparing the output of information extraction to a gold standard, and printing the results.
	/// Subclasses may customize the formatting and content of the printout.
	/// </remarks>
	/// <author>mrsmith</author>
	public abstract class ResultsPrinter
	{
		/// <summary>
		/// Given a set of sentences with annotations from an information extractor class, and the same sentences
		/// with gold-standard annotations, print results on how the information extraction performed.
		/// </summary>
		public virtual string PrintResults(ICoreMap goldStandard, ICoreMap extractorOutput)
		{
			StringWriter sw = new StringWriter();
			PrintWriter pw = new PrintWriter(sw, true);
			IList<ICoreMap> mutableGold = new List<ICoreMap>();
			Sharpen.Collections.AddAll(mutableGold, goldStandard.Get(typeof(CoreAnnotations.SentencesAnnotation)));
			IList<ICoreMap> mutableOutput = new List<ICoreMap>();
			Sharpen.Collections.AddAll(mutableOutput, extractorOutput.Get(typeof(CoreAnnotations.SentencesAnnotation)));
			PrintResults(pw, mutableGold, mutableOutput);
			return sw.GetBuffer().ToString();
		}

		public virtual string PrintResults(IList<string> goldStandard, IList<string> extractorOutput)
		{
			StringWriter sw = new StringWriter();
			PrintWriter pw = new PrintWriter(sw, true);
			PrintResultsUsingLabels(pw, goldStandard, extractorOutput);
			return sw.GetBuffer().ToString();
		}

		public abstract void PrintResults(PrintWriter pw, IList<ICoreMap> goldStandard, IList<ICoreMap> extractorOutput);

		public abstract void PrintResultsUsingLabels(PrintWriter pw, IList<string> goldStandard, IList<string> extractorOutput);

		/// <summary>If the same set of sentences is contained in two lists, order the lists so that their sentences are in the same order (and return true).</summary>
		/// <remarks>
		/// If the same set of sentences is contained in two lists, order the lists so that their sentences are in the same order (and return true).
		/// Return false if the lists don't contain the same set of sentences.
		/// </remarks>
		public static void Align(IList<ICoreMap> list1, IList<ICoreMap> list2)
		{
			bool alignable = true;
			if (list1.Count != list2.Count)
			{
				alignable = false;
			}
			list1.Sort(new _T214739841(this));
			list2.Sort(new _T214739841(this));
			for (int i = 0; i < list1.Count; i++)
			{
				if (!list1[i].Get(typeof(CoreAnnotations.TextAnnotation)).Equals(list2[i].Get(typeof(CoreAnnotations.TextAnnotation))))
				{
					alignable = false;
				}
			}
			if (!alignable)
			{
				throw new Exception("ResultsPrinter.align: gold standard sentences don't match extractor output sentences!");
			}
		}

		internal class _T214739841 : IComparator<ICoreMap>
		{
			public virtual int Compare(ICoreMap sent1, ICoreMap sent2)
			{
				string d1 = sent1.Get(typeof(CoreAnnotations.DocIDAnnotation));
				string d2 = sent2.Get(typeof(CoreAnnotations.DocIDAnnotation));
				if (d1 != null && d2 != null && !d1.Equals(d2))
				{
					return string.CompareOrdinal(d1, d2);
				}
				string t1 = sent1.Get(typeof(CoreAnnotations.TextAnnotation));
				string t2 = sent2.Get(typeof(CoreAnnotations.TextAnnotation));
				return string.CompareOrdinal(t1, t2);
			}

			internal _T214739841(ResultsPrinter _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly ResultsPrinter _enclosing;
		}
	}
}
