using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Patterns
{
	/// <summary>Created by sonalg on 10/15/14.</summary>
	public abstract class SentenceIndex<E>
		where E : Pattern
	{
		internal ICollection<string> stopWords;

		internal int numAllSentences = 0;

		internal Func<CoreLabel, IDictionary<string, string>> transformCoreLabeltoString;

		public SentenceIndex(ICollection<string> stopWords, Func<CoreLabel, IDictionary<string, string>> transformCoreLabeltoString)
		{
			this.stopWords = stopWords;
			this.transformCoreLabeltoString = transformCoreLabeltoString;
		}

		public virtual int Size()
		{
			return this.numAllSentences;
		}

		/// <summary>addProcessedText is true when inserting sentences for the first time</summary>
		/// <param name="sents"/>
		/// <param name="addProcessedText"/>
		public abstract void Add(IDictionary<string, DataInstance> sents, bool addProcessedText);

		//  protected CollectionValuedMap<String, String> getRelevantWords(Set<Integer> pats, Index<E> EIndex){
		//    CollectionValuedMap<String, String> relwords = new CollectionValuedMap<String, String>();
		//    for(Integer p : pats)
		//    relwords.addAll(getRelevantWords(EIndex.get(p)));
		//    return relwords;
		//  }
		//  protected CollectionValuedMap<String, String> getRelevantWords(E pat){
		//    return pat.getRelevantWords();
		//  }
		/*
		returns className->list_of_relevant_words in relWords
		*/
		//  protected Set<String> getRelevantWords(E pat){
		//
		//      Set<String> relwordsThisPat = new HashSet<String>();
		//      String[] next = pat.getSimplerTokensNext();
		//      if (next != null)
		//        for (String s : next) {
		//          s = s.trim();
		//          if (matchLowerCaseContext)
		//            s = s.toLowerCase();
		//          if (!s.isEmpty() & !stopWords.contains(s) && !specialWords.contains(s))
		//            relwordsThisPat.add(s);
		//        }
		//      String[] prev = pat.getSimplerTokensPrev();
		//      if (prev != null)
		//        for (String s : prev) {
		//          s = s.trim();
		//          if (matchLowerCaseContext)
		//            s = s.toLowerCase();
		//          if (!s.isEmpty() & !stopWords.contains(s) && !specialWords.contains(s))
		//            relwordsThisPat.add(s);
		//        }
		//
		//    return relwordsThisPat;
		//  }
		//TODO: what if someone calls with SentenceIndex.class?
		public static Edu.Stanford.Nlp.Patterns.SentenceIndex CreateIndex(Type indexClass, IDictionary<string, IList<CoreLabel>> sents, Properties props, ICollection<string> stopWords, string indexDirectory, Func<CoreLabel, IDictionary<string, 
			string>> transformCoreLabeltoString)
		{
			try
			{
				ArgumentParser.FillOptions(typeof(Edu.Stanford.Nlp.Patterns.SentenceIndex), props);
				MethodInfo m = indexClass.GetMethod("createIndex", typeof(IDictionary), typeof(Properties), typeof(ISet), typeof(string), typeof(Func));
				Edu.Stanford.Nlp.Patterns.SentenceIndex index = (Edu.Stanford.Nlp.Patterns.SentenceIndex)m.Invoke(null, new object[] { sents, props, stopWords, indexDirectory, transformCoreLabeltoString });
				return index;
			}
			catch (ReflectiveOperationException e)
			{
				throw new Exception(e);
			}
		}

		public abstract IDictionary<E, ICollection<string>> QueryIndex(ICollection<E> Es);

		//,  EIndex EIndex);
		public virtual void SetUp(Properties props)
		{
			ArgumentParser.FillOptions(this, props);
		}

		protected internal abstract void Add(IList<CoreLabel> value, string sentId, bool addProcessedText);

		public abstract void FinishUpdating();

		public abstract void Update(IList<CoreLabel> value, string key);

		public abstract void SaveIndex(string dir);

		public static Edu.Stanford.Nlp.Patterns.SentenceIndex LoadIndex(Type indexClass, Properties props, ICollection<string> stopWords, string indexDirectory, Func<CoreLabel, IDictionary<string, string>> transformCoreLabeltoString)
		{
			try
			{
				ArgumentParser.FillOptions(typeof(Edu.Stanford.Nlp.Patterns.SentenceIndex), props);
				MethodInfo m = indexClass.GetMethod("loadIndex", typeof(Properties), typeof(ISet), typeof(string), typeof(Func));
				Edu.Stanford.Nlp.Patterns.SentenceIndex index = (Edu.Stanford.Nlp.Patterns.SentenceIndex)m.Invoke(null, new object[] { props, stopWords, indexDirectory, transformCoreLabeltoString });
				return index;
			}
			catch (ReflectiveOperationException e)
			{
				throw new Exception(e);
			}
		}
	}
}
