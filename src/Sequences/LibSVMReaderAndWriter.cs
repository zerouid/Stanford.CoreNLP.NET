using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>DocumentReader for column format</summary>
	/// <author>Jenny Finkel</author>
	[System.Serializable]
	public class LibSVMReaderAndWriter : IDocumentReaderAndWriter<CoreLabel>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(LibSVMReaderAndWriter));

		private const long serialVersionUID = -7997837004847909059L;

		private SeqClassifierFlags flags = null;

		private IIteratorFromReaderFactory<IList<CoreLabel>> factory;

		//TODO: repair this so it works with the feature label/coreLabel change
		public virtual void Init(SeqClassifierFlags flags)
		{
			this.flags = flags;
			factory = DelimitRegExIterator.GetFactory("\n(\\s*\n)+", new LibSVMReaderAndWriter.ColumnDocParser(this));
		}

		public virtual IEnumerator<IList<CoreLabel>> GetIterator(Reader r)
		{
			return factory.GetIterator(r);
		}

		internal int num = 0;

		private class ColumnDocParser : Func<string, IList<CoreLabel>>
		{
			public virtual IList<CoreLabel> Apply(string doc)
			{
				if (this._enclosing.num % 1000 == 0)
				{
					LibSVMReaderAndWriter.log.Info("[" + this._enclosing.num + "]");
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
					CoreLabel wi = new CoreLabel();
					string[] info = line.Split("\\s+");
					wi.Set(typeof(CoreAnnotations.AnswerAnnotation), info[0]);
					wi.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), info[0]);
					for (int j = 1; j < info.Length; j++)
					{
						string[] bits = info[j].Split(":");
					}
					//wi.set(bits[0], bits[1]);
					//        log.info(wi);
					words.Add(wi);
				}
				return words;
			}

			internal ColumnDocParser(LibSVMReaderAndWriter _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly LibSVMReaderAndWriter _enclosing;
		}

		public virtual void PrintAnswers(IList<CoreLabel> doc, PrintWriter @out)
		{
			foreach (CoreLabel wi in doc)
			{
				string answer = wi.Get(typeof(CoreAnnotations.AnswerAnnotation));
				string goldAnswer = wi.Get(typeof(CoreAnnotations.GoldAnswerAnnotation));
				@out.Println(goldAnswer + "\t" + answer);
			}
			@out.Println();
		}
	}
}
