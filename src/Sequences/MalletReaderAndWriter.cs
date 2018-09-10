using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>DocumentReaderAndWriter for SimpleTagger of Mallet.</summary>
	/// <remarks>
	/// DocumentReaderAndWriter for SimpleTagger of Mallet.
	/// Each line represents one instance, and contains any number of features followed by
	/// the class label. Empty lines are treated as sequence delimiters.
	/// See http://mallet.cs.umass.edu/index.php/SimpleTagger_example for more information.
	/// </remarks>
	/// <author>Michel Galley</author>
	[System.Serializable]
	public class MalletReaderAndWriter : IDocumentReaderAndWriter<CoreLabel>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(MalletReaderAndWriter));

		private const long serialVersionUID = 3806263423691913704L;

		private SeqClassifierFlags flags = null;

		private string[] map = null;

		private IIteratorFromReaderFactory factory;

		public virtual void Init(SeqClassifierFlags flags)
		{
			this.flags = flags;
			this.map = StringUtils.MapStringToArray(flags.map);
			factory = DelimitRegExIterator.GetFactory("\n(\\s*\n)+", new MalletReaderAndWriter.MalletDocParser(this));
		}

		public virtual IEnumerator<IList<CoreLabel>> GetIterator(Reader r)
		{
			return factory.GetIterator(r);
		}

		internal int num = 0;

		[System.Serializable]
		private class MalletDocParser : Func<string, IList<CoreLabel>>
		{
			private const long serialVersionUID = -6211332661459630572L;

			public virtual IList<CoreLabel> Apply(string doc)
			{
				if (this._enclosing.num % 1000 == 0)
				{
					MalletReaderAndWriter.log.Info("[" + this._enclosing.num + "]");
				}
				this._enclosing.num++;
				IList<CoreLabel> words = new List<CoreLabel>();
				string[] lines = doc.Split("\n");
				foreach (string line in lines)
				{
					if (line.Trim().Length < 1)
					{
						continue;
					}
					int idx = line.LastIndexOf(" ");
					if (idx < 0)
					{
						throw new Exception("Bad line: " + line);
					}
					CoreLabel wi = new CoreLabel();
					wi.SetWord(Sharpen.Runtime.Substring(line, 0, idx));
					wi.Set(typeof(CoreAnnotations.AnswerAnnotation), Sharpen.Runtime.Substring(line, idx + 1));
					wi.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), Sharpen.Runtime.Substring(line, idx + 1));
					words.Add(wi);
				}
				return words;
			}

			internal MalletDocParser(MalletReaderAndWriter _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly MalletReaderAndWriter _enclosing;
		}

		public virtual void PrintAnswers(IList<CoreLabel> doc, PrintWriter @out)
		{
			foreach (CoreLabel wi in doc)
			{
				string answer = wi.Get(typeof(CoreAnnotations.AnswerAnnotation));
				string goldAnswer = wi.Get(typeof(CoreAnnotations.GoldAnswerAnnotation));
				@out.Println(wi.Word() + "\t" + goldAnswer + "\t" + answer);
			}
			@out.Println();
		}
	}
}
