using System;



namespace Edu.Stanford.Nlp.Coref.Data
{
	/// <summary>Semantic knowledge: currently WordNet is available</summary>
	public class Semantics
	{
		public object wordnet;

		public Semantics()
		{
		}

		/// <exception cref="System.Exception"/>
		public Semantics(Dictionaries dict)
		{
			Constructor<object> wordnetConstructor = (Sharpen.Runtime.GetType("edu.stanford.nlp.hcoref.WordNet")).GetConstructor();
			wordnet = wordnetConstructor.NewInstance();
		}
	}
}
