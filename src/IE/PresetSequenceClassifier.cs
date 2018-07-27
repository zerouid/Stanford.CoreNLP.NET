using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.IE
{
	/// <summary>Created by jebolton on 7/14/17.</summary>
	public class PresetSequenceClassifier<In> : AbstractSequenceClassifier<In>
		where In : ICoreMap
	{
		public PresetSequenceClassifier(Properties props)
			: base(props)
		{
			if (classIndex == null)
			{
				classIndex = new HashIndex<string>();
			}
			// classIndex.add("O");
			classIndex.Add(flags.backgroundSymbol);
		}

		/// <summary><inheritDoc/></summary>
		public override void SerializeClassifier(string serializePath)
		{
		}

		/// <summary><inheritDoc/></summary>
		public override void SerializeClassifier(ObjectOutputStream oos)
		{
		}

		/// <summary><inheritDoc/></summary>
		public override void LoadClassifier(ObjectInputStream ois, Properties props)
		{
		}

		public override IList<In> Classify(IList<In> document)
		{
			foreach (IN token in document)
			{
				string presetAnswer = token.Get(typeof(CoreAnnotations.PresetAnswerAnnotation));
				token.Set(typeof(CoreAnnotations.AnswerAnnotation), presetAnswer);
			}
			return document;
		}

		public override IList<In> ClassifyWithGlobalInformation(IList<In> tokenSeq, ICoreMap doc, ICoreMap sent)
		{
			return Classify(tokenSeq);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override void Train(ICollection<IList<In>> objectBankWrapper, IDocumentReaderAndWriter<In> readerAndWriter)
		{
		}
	}
}
