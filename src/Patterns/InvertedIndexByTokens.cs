using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns.Surface;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns
{
	/// <summary>Creates an inverted index of (classkey:value) =&gt; {sentid1,sentid2,..</summary>
	/// <remarks>Creates an inverted index of (classkey:value) =&gt; {sentid1,sentid2,.. }.</remarks>
	/// <author>Sonal Gupta (sonalg@stanford.edu)</author>
	[System.Serializable]
	public class InvertedIndexByTokens<E> : SentenceIndex<E>
		where E : Pattern
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Patterns.InvertedIndexByTokens));

		private const long serialVersionUID = 1L;

		internal IDictionary<string, ICollection<string>> index;

		public InvertedIndexByTokens(Properties props, ICollection<string> stopWords, IFunction<CoreLabel, IDictionary<string, string>> transformSentenceToString)
			: base(stopWords, transformSentenceToString)
		{
			ArgumentParser.FillOptions(this, props);
			index = new Dictionary<string, ICollection<string>>();
		}

		public InvertedIndexByTokens(Properties props, ICollection<string> stopWords, IFunction<CoreLabel, IDictionary<string, string>> transformSentenceToString, IDictionary<string, ICollection<string>> index)
			: base(stopWords, transformSentenceToString)
		{
			ArgumentParser.FillOptions(this, props);
			this.index = index;
		}

		public override void Add(IDictionary<string, DataInstance> sents, bool addProcessedText)
		{
			foreach (KeyValuePair<string, DataInstance> sEn in sents)
			{
				Add(sEn.Value.GetTokens(), sEn.Key, addProcessedText);
			}
		}

		protected internal override void Add(IList<CoreLabel> sent, string sentId, bool addProcessedText)
		{
			numAllSentences++;
			foreach (CoreLabel l in sent)
			{
				//String w = l.word();
				//        w = w.replaceAll("/", "\\\\/");
				//        add(w, sEn.getKey());
				IDictionary<string, string> addThis = this.transformCoreLabeltoString.Apply(l);
				foreach (KeyValuePair<string, string> en in addThis)
				{
					string val = CombineKeyValue(en.Key, en.Value);
					Add(val, sentId);
				}
				if (addProcessedText)
				{
					string val = Token.GetKeyForClass(typeof(PatternsAnnotations.ProcessedTextAnnotation)) + ":" + l.Get(typeof(PatternsAnnotations.ProcessedTextAnnotation));
					if (!stopWords.Contains(val.ToLower()))
					{
						Add(val, sentId);
					}
				}
			}
		}

		public override void FinishUpdating()
		{
		}

		//nothing to do right now!
		public override void Update(IList<CoreLabel> tokens, string sentid)
		{
			Add(tokens, sentid, false);
		}

		internal virtual void Add(string w, string sentid)
		{
			ICollection<string> sentids = index[w];
			if (sentids == null)
			{
				sentids = new HashSet<string>();
			}
			sentids.Add(sentid);
			index[w] = sentids;
		}

		internal virtual string CombineKeyValue(string key, string value)
		{
			return key + ":" + value;
		}

		public virtual ICollection<string> GetFileSentIds(CollectionValuedMap<string, string> relevantWords)
		{
			ICollection<string> sentids = null;
			foreach (KeyValuePair<string, ICollection<string>> en in relevantWords)
			{
				foreach (string en2 in en.Value)
				{
					if (!stopWords.Contains(en2.ToLower()))
					{
						string w = CombineKeyValue(en.Key, en2);
						ICollection<string> st = index[w];
						if (st == null)
						{
							//log.info("\n\nWARNING: INDEX HAS NO SENTENCES FOR " + w);
							return Java.Util.Collections.EmptySet();
						}
						//throw new RuntimeException("How come the index does not have sentences for " + w);
						if (sentids == null)
						{
							sentids = st;
						}
						else
						{
							sentids = CollectionUtils.Intersection(sentids, st);
						}
					}
				}
			}
			return sentids;
		}

		//returns for each pattern, list of sentence ids
		public virtual IDictionary<E, ICollection<string>> GetFileSentIdsFromPats(ICollection<E> pats)
		{
			IDictionary<E, ICollection<string>> sents = new Dictionary<E, ICollection<string>>();
			foreach (E pat in pats)
			{
				ICollection<string> ids = GetFileSentIds(pat.GetRelevantWords());
				Redwood.Log(ConstantsAndVariables.extremedebug, "For pattern with index " + pat + " extracted the following sentences from the index " + ids);
				sents[pat] = ids;
			}
			return sents;
		}

		//The last variable is not really used!
		public static Edu.Stanford.Nlp.Patterns.InvertedIndexByTokens CreateIndex(IDictionary<string, IList<CoreLabel>> sentences, Properties props, ICollection<string> stopWords, string dir, IFunction<CoreLabel, IDictionary<string, string>> transformCoreLabeltoString
			)
		{
			Edu.Stanford.Nlp.Patterns.InvertedIndexByTokens inv = new Edu.Stanford.Nlp.Patterns.InvertedIndexByTokens(props, stopWords, transformCoreLabeltoString);
			if (sentences != null && sentences.Count > 0)
			{
				inv.Add(sentences, true);
			}
			System.Console.Out.WriteLine("Created index with size " + inv.Size() + ". Don't worry if it's zero and you are using batch process sents.");
			return inv;
		}

		public override IDictionary<E, ICollection<string>> QueryIndex(ICollection<E> patterns)
		{
			IDictionary<E, ICollection<string>> sentSentids = GetFileSentIdsFromPats(patterns);
			return sentSentids;
		}

		public override void SaveIndex(string dir)
		{
			try
			{
				IOUtils.EnsureDir(new File(dir));
				IOUtils.WriteObjectToFile(index, dir + "/map.ser");
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
		}

		//called by SentenceIndex.loadIndex
		public static Edu.Stanford.Nlp.Patterns.InvertedIndexByTokens LoadIndex(Properties props, ICollection<string> stopwords, string dir, IFunction<CoreLabel, IDictionary<string, string>> transformSentenceToString)
		{
			try
			{
				IDictionary<string, ICollection<string>> index = IOUtils.ReadObjectFromFile(dir + "/map.ser");
				System.Console.Out.WriteLine("Loading inverted index from " + dir);
				return new Edu.Stanford.Nlp.Patterns.InvertedIndexByTokens(props, stopwords, transformSentenceToString, index);
			}
			catch (Exception e)
			{
				throw new Exception("Cannot load the inverted index. " + e);
			}
		}
	}
}
