using System.Collections.Generic;



namespace Edu.Stanford.Nlp.IE.Machinereading.Structure
{
	/// <author>Andrey Gusev</author>
	/// <author>Mason Smith</author>
	/// <author>Mihai</author>
	[System.Serializable]
	public class ExtractionDataSet
	{
		private const long serialVersionUID = 201150461234284548L;

		private readonly IList<ExtractionSentence> sentences;

		public ExtractionDataSet()
		{
			sentences = new List<ExtractionSentence>();
		}

		/// <summary>Copy c'tor that performs deep copy of the sentences in the original dataset</summary>
		public ExtractionDataSet(Edu.Stanford.Nlp.IE.Machinereading.Structure.ExtractionDataSet original)
		{
			sentences = new List<ExtractionSentence>();
			foreach (ExtractionSentence sent in original.GetSentences())
			{
				// deep copy of the sentence: we create new entity/relation/event lists here
				// however, we do not deep copy the ExtractionObjects themselves!
				ExtractionSentence sentCopy = new ExtractionSentence(sent);
				sentences.Add(sentCopy);
			}
		}

		public virtual ExtractionSentence GetSentence(int i)
		{
			return sentences[i];
		}

		public virtual int SentenceCount()
		{
			return sentences.Count;
		}

		public virtual void AddSentence(ExtractionSentence sentence)
		{
			this.sentences.Add(sentence);
		}

		public virtual void AddSentences(IList<ExtractionSentence> sentences)
		{
			foreach (ExtractionSentence sent in sentences)
			{
				AddSentence(sent);
			}
		}

		public virtual IList<ExtractionSentence> GetSentences()
		{
			return Java.Util.Collections.UnmodifiableList(this.sentences);
		}

		public virtual void Shuffle()
		{
			// we use a constant seed for replicability of experiments
			Java.Util.Collections.Shuffle(sentences, new Random(0));
		}
		/*
		public List<List<CoreLabel>> toCoreLabels(Set<String> annotationsToSkip, boolean useSubTypes) {
		List<List<CoreLabel>> retVal = new ArrayList<List<CoreLabel>>();
		
		for (ExtractionSentence sentence : sentences) {
		List<CoreLabel> labeledSentence = sentence.toCoreLabels(true, annotationsToSkip, useSubTypes);
		
		if (labeledSentence != null) {
		// here we accumulate all sentences (we split into training and test set
		// if and when doing cross validation)
		retVal.add(labeledSentence);
		}
		}
		
		return retVal;
		}
		*/
	}
}
