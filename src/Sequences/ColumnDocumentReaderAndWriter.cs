using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>DocumentReader for column format.</summary>
	/// <author>Jenny Finkel</author>
	[System.Serializable]
	public class ColumnDocumentReaderAndWriter : IDocumentReaderAndWriter<CoreLabel>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(ColumnDocumentReaderAndWriter));

		private const long serialVersionUID = 3806263423697973704L;

		private Type[] map;

		private IIteratorFromReaderFactory<IList<CoreLabel>> factory;

		//  private SeqClassifierFlags flags; // = null;
		//map can be something like "word=0,tag=1,answer=2"
		// = null;
		//  public void init(SeqClassifierFlags flags) {
		//    this.flags = flags;
		//    this.map = StringUtils.mapStringToArray(flags.map);
		//    factory = DelimitRegExIterator.getFactory("\n(\\s*\n)+", new ColumnDocParser());
		//  }
		public virtual void Init(SeqClassifierFlags flags)
		{
			Init(flags.map);
		}

		public virtual void Init(string map)
		{
			// this.flags = null;
			this.map = CoreLabel.ParseStringKeys(StringUtils.MapStringToArray(map));
			factory = DelimitRegExIterator.GetFactory("\n(?:\\s*\n)+", new ColumnDocumentReaderAndWriter.ColumnDocParser(this));
		}

		public virtual IEnumerator<IList<CoreLabel>> GetIterator(Reader r)
		{
			return factory.GetIterator(r);
		}

		[System.Serializable]
		private class ColumnDocParser : IFunction<string, IList<CoreLabel>>
		{
			private const long serialVersionUID = -6266332661459630572L;

			private readonly Pattern whitePattern = Pattern.Compile("\\s+");

			private int lineCount;

			// private int num; // = 0;
			// should this really only do a tab?
			// = 0;
			public virtual IList<CoreLabel> Apply(string doc)
			{
				// if (num > 0 && num % 1000 == 0) { log.info("["+num+"]"); } // cdm: Not so useful to do in new logging world
				// num++;
				IList<CoreLabel> words = new List<CoreLabel>();
				string[] lines = doc.Split("\n");
				foreach (string line in lines)
				{
					++this.lineCount;
					if (line.Trim().IsEmpty())
					{
						continue;
					}
					// Optimistic splitting on tabs first. If that doesn't work, use any whitespace (slower, because of regexps).
					string[] info = line.Split("\t");
					if (info.Length == 1)
					{
						info = this.whitePattern.Split(line);
					}
					CoreLabel wi;
					try
					{
						wi = new CoreLabel(this._enclosing.map, info);
						// Since the map normally only specified answer, we copy it to GoldAnswer unless they've put something else there!
						if (!wi.ContainsKey(typeof(CoreAnnotations.GoldAnswerAnnotation)) && wi.ContainsKey(typeof(CoreAnnotations.AnswerAnnotation)))
						{
							wi.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), wi.Get(typeof(CoreAnnotations.AnswerAnnotation)));
						}
					}
					catch (Exception e)
					{
						ColumnDocumentReaderAndWriter.log.Info("Error on line " + this.lineCount + ": " + line);
						throw;
					}
					words.Add(wi);
				}
				return words;
			}

			internal ColumnDocParser(ColumnDocumentReaderAndWriter _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly ColumnDocumentReaderAndWriter _enclosing;
		}

		// end class ColumnDocParser
		public virtual void PrintAnswers(IList<CoreLabel> doc, PrintWriter @out)
		{
			foreach (CoreLabel wi in doc)
			{
				string answer = wi.Get(typeof(CoreAnnotations.AnswerAnnotation));
				string goldAnswer = wi.Get(typeof(CoreAnnotations.GoldAnswerAnnotation));
				@out.Println(wi.Word() + '\t' + goldAnswer + '\t' + answer);
			}
			@out.Println();
		}
	}
}
