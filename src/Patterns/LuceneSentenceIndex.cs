using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns.Surface;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Analysis.Core;
using Org.Apache.Lucene.Document;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;


namespace Edu.Stanford.Nlp.Patterns
{
	/// <summary>To create a lucene inverted index from tokens to sentence ids.</summary>
	/// <remarks>
	/// To create a lucene inverted index from tokens to sentence ids.
	/// (Right now it is not storing all core tokens although some functions might suggest that.)
	/// </remarks>
	/// <author>Sonal Gupta on 10/14/14.</author>
	public class LuceneSentenceIndex<E> : SentenceIndex<E>
		where E : Pattern
	{
		internal bool saveTokens = false;

		internal IndexWriter indexWriter;

		internal File indexDir = null;

		internal Directory dir;

		internal Analyzer analyzer = new KeywordAnalyzer();

		internal IndexWriterConfig iwc;

		internal DirectoryReader reader = null;

		internal IndexSearcher searcher;

		internal ProtobufAnnotationSerializer p = new ProtobufAnnotationSerializer();

		public LuceneSentenceIndex(Properties props, ICollection<string> stopWords, string indexDirStr, Func<CoreLabel, IDictionary<string, string>> transformer)
			: base(stopWords, transformer)
		{
			iwc = new IndexWriterConfig(Version.Lucene42, analyzer);
			//  Analyzer analyzer = new Analyzer() {
			//    @Override
			//    protected TokenStreamComponents createComponents(String fieldName, Reader reader) {
			//      Tokenizer source = new KeywordTokenizer(reader);
			//      TokenStream result = new StopWordsFilter(source);
			//      return new TokenStreamComponents(source, result);
			//    }
			//  };
			//  public final class StopWordsFilter extends FilteringTokenFilter {
			//    /**
			//     * Build a filter that removes words that are too long or too
			//     * short from the text.
			//     */
			//    public StopWordsFilter(TokenStream in) {
			//      super(true, in);
			//    }
			//
			//    @Override
			//    public boolean accept() throws IOException {
			//      return !stopWords.contains(input.toString().toLowerCase());
			//    }
			//  }
			//StandardAnalyzer analyzer = new StandardAnalyzer(Version.LUCENE_42);
			//The fields in index are: tokens, sentence id, List<CoreLabel> annotation of the sentence (optional; not used when sentences are in memory)
			ArgumentParser.FillOptions(this, props);
			indexDir = new File(indexDirStr);
		}

		/// <exception cref="System.IO.IOException"/>
		internal virtual void SetIndexReaderSearcher()
		{
			FSDirectory index = FSDirectory.Open(indexDir);
			if (reader == null)
			{
				reader = DirectoryReader.Open(index);
				searcher = new IndexSearcher(reader);
			}
			else
			{
				DirectoryReader newreader = DirectoryReader.OpenIfChanged(reader);
				if (newreader != null)
				{
					reader.Close();
					reader = newreader;
					searcher = new IndexSearcher(reader);
				}
			}
		}

		//  SentenceIndex.SentenceIteratorWithWords queryIndex(SurfacePattern pat){
		//
		//
		//    String[] n = pat.getSimplerTokensNext();
		//    String[] pr = pat.getSimplerTokensPrev();
		//    boolean rest = false;
		//    if(n!=null){
		//      for(String e: n){
		//        if(!specialWords.contains(e)){
		//          rest = true;
		//          break;
		//        }
		//      }
		//    }
		//    if(rest == false && pr!=null){
		//      for(String e: pr){
		//        if(!specialWords.contains(e) && !stopWords.contains(e)){
		//          rest = true;
		//          break;
		//        }
		//      }
		//    }
		//
		//  }
		/// <summary>give all sentences that have these words</summary>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"/>
		internal virtual ICollection<string> QueryIndexGetSentences(CollectionValuedMap<string, string> words)
		{
			SetIndexReaderSearcher();
			BooleanQuery query = new BooleanQuery();
			string pkey = Token.GetKeyForClass(typeof(PatternsAnnotations.ProcessedTextAnnotation));
			foreach (KeyValuePair<string, ICollection<string>> en in words)
			{
				bool processedKey = en.Key.Equals(pkey);
				foreach (string en2 in en.Value)
				{
					if (!processedKey || !stopWords.Contains(en2.ToLower()))
					{
						query.Add(new BooleanClause(new TermQuery(new Term(en.Key, en2)), BooleanClause.Occur.Must));
					}
				}
			}
			//query.add(new BooleanClause(new TermQuery(new Term("textannotation","sonal")), BooleanClause.Occur.MUST));
			//    String queryStr = "";
			//    for(Map.Entry<String, Collection<String>> en: words.entrySet()){
			//      for(String en2: en.getValue()){
			//        queryStr+= " " + en.getKey() + ":"+en2;
			//      }
			//    }
			//    QueryParser queryParser = new QueryParser(Version.LUCENE_42, "sentence", analyzer);
			//
			//    queryParser.setDefaultOperator(QueryParser.Operator.AND);
			//
			//    Query query = queryParser.parse(queryStr);
			//Map<String, List<CoreLabel>> sents = null;
			TopDocs tp = searcher.Search(query, int.MaxValue);
			ICollection<string> sentids = new HashSet<string>();
			if (tp.totalHits > 0)
			{
				foreach (ScoreDoc s in tp.scoreDocs)
				{
					int docId = s.doc;
					Org.Apache.Lucene.Document.Document d = searcher.Doc(docId);
					//        byte[] sent = d.getBinaryValue("tokens").bytes;
					//        if(saveTokens) {
					//          sents = new HashMap<String, List<CoreLabel>>();
					//          List<CoreLabel> tokens = readProtoBufAnnotation(sent);
					//          sents.put(d.get("sentid"), tokens);
					//        } else{
					sentids.Add(d.Get("sentid"));
				}
			}
			else
			{
				//}
				throw new Exception("how come no documents for " + words + ". Query formed is " + query);
			}
			//System.out.println("number of sentences for tokens " + words + " are " + sentids);
			//    if(!saveTokens){
			//      sents = getSentences(sentids);
			//    }
			return sentids;
		}

		public override void Add(IDictionary<string, DataInstance> sentences, bool addProcessedText)
		{
			try
			{
				this.SetIndexWriter();
				foreach (KeyValuePair<string, DataInstance> en in sentences)
				{
					//String sentence = StringUtils.joinWords(en.getValue(), " ");
					Add(en.Value.GetTokens(), en.Key, addProcessedText);
				}
				indexWriter.Commit();
				CloseIndexWriter();
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
		}

		public override IDictionary<E, ICollection<string>> QueryIndex(ICollection<E> patterns)
		{
			try
			{
				IDictionary<E, ICollection<string>> sents = new Dictionary<E, ICollection<string>>();
				foreach (E p in patterns)
				{
					ICollection<string> sentids = QueryIndexGetSentences(p.GetRelevantWords());
					sents[p] = sentids;
				}
				return sents;
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void ListAllDocuments()
		{
			SetIndexReaderSearcher();
			for (int i = 0; i < reader.NumDocs(); i++)
			{
				Org.Apache.Lucene.Document.Document d = searcher.Doc(i);
				//      byte[] sent = d.getBinaryValue("tokens").bytes;
				//      List<CoreLabel> tokens = readProtoBufAnnotation(sent);
				System.Console.Out.WriteLine(d.Get("sentid"));
			}
		}

		/// <exception cref="System.IO.IOException"/>
		private IList<CoreLabel> ReadProtoBufAnnotation(byte[] sent)
		{
			ProtobufAnnotationSerializer p = new ProtobufAnnotationSerializer();
			IList<CoreLabel> toks = new List<CoreLabel>();
			ByteArrayInputStream @is = new ByteArrayInputStream(sent);
			CoreNLPProtos.Token d;
			do
			{
				d = CoreNLPProtos.Token.ParseDelimitedFrom(@is);
				if (d != null)
				{
					toks.Add(p.FromProto(d));
				}
			}
			while (d != null);
			return toks;
		}

		/// <exception cref="System.IO.IOException"/>
		internal virtual byte[] GetProtoBufAnnotation(IList<CoreLabel> tokens)
		{
			ByteArrayOutputStream os = new ByteArrayOutputStream();
			foreach (CoreLabel token in tokens)
			{
				CoreNLPProtos.Token ptoken = p.ToProto(token);
				ptoken.WriteDelimitedTo(os);
			}
			os.Flush();
			return os.ToByteArray();
		}

		protected internal override void Add(IList<CoreLabel> tokens, string sentid, bool addProcessedText)
		{
			try
			{
				SetIndexWriter();
				Org.Apache.Lucene.Document.Document doc = new Org.Apache.Lucene.Document.Document();
				foreach (CoreLabel l in tokens)
				{
					foreach (KeyValuePair<string, string> en in transformCoreLabeltoString.Apply(l))
					{
						doc.Add(new StringField(en.Key, en.Value, Field.Store.Yes));
					}
					//, ANALYZED));
					if (addProcessedText)
					{
						string ptxt = l.Get(typeof(PatternsAnnotations.ProcessedTextAnnotation));
						if (!stopWords.Contains(ptxt.ToLower()))
						{
							doc.Add(new StringField(Token.GetKeyForClass(typeof(PatternsAnnotations.ProcessedTextAnnotation)), ptxt, Field.Store.Yes));
						}
					}
				}
				//, ANALYZED));
				doc.Add(new StringField("sentid", sentid, Field.Store.Yes));
				if (tokens != null && saveTokens)
				{
					doc.Add(new Field("tokens", GetProtoBufAnnotation(tokens), LuceneFieldType.NotIndexed));
				}
				indexWriter.AddDocument(doc);
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
		}

		public override void FinishUpdating()
		{
			if (indexWriter != null)
			{
				try
				{
					indexWriter.Commit();
				}
				catch (IOException e)
				{
					throw new Exception(e);
				}
			}
			CloseIndexWriter();
		}

		public override void Update(IList<CoreLabel> tokens, string sentid)
		{
			try
			{
				SetIndexWriter();
				indexWriter.DeleteDocuments(new TermQuery(new Term("sentid", sentid)));
				Add(tokens, sentid, true);
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
		}

		internal virtual void SetIndexWriter()
		{
			try
			{
				if (indexWriter == null)
				{
					dir = FSDirectory.Open(indexDir);
					Redwood.Log(Redwood.Dbg, "Updating lucene index at " + indexDir);
					indexWriter = new IndexWriter(dir, iwc);
				}
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
		}

		internal virtual void CloseIndexWriter()
		{
			try
			{
				if (indexWriter != null)
				{
					indexWriter.Close();
				}
				indexWriter = null;
				if (dir != null)
				{
					dir.Close();
				}
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
		}

		public override void SaveIndex(string dir)
		{
			if (!indexDir.ToString().Equals(dir))
			{
				try
				{
					IOUtils.Cp(indexDir, new File(dir), true);
				}
				catch (IOException e)
				{
					throw new Exception(e);
				}
			}
		}

		public static Edu.Stanford.Nlp.Patterns.LuceneSentenceIndex CreateIndex(IDictionary<string, IList<CoreLabel>> sentences, Properties props, ICollection<string> stopWords, string indexDiskDir, Func<CoreLabel, IDictionary<string, string>> 
			transformer)
		{
			try
			{
				Edu.Stanford.Nlp.Patterns.LuceneSentenceIndex sentindex = new Edu.Stanford.Nlp.Patterns.LuceneSentenceIndex(props, stopWords, indexDiskDir, transformer);
				System.Console.Out.WriteLine("Creating lucene index at " + indexDiskDir);
				IOUtils.DeleteDirRecursively(sentindex.indexDir);
				if (sentences != null)
				{
					sentindex.SetIndexWriter();
					sentindex.Add(sentences, true);
					sentindex.CloseIndexWriter();
					sentindex.SetIndexReaderSearcher();
					System.Console.Out.WriteLine("Number of documents added are " + sentindex.reader.NumDocs());
					sentindex.numAllSentences += sentindex.reader.NumDocs();
				}
				return sentindex;
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
		}

		public static Edu.Stanford.Nlp.Patterns.LuceneSentenceIndex LoadIndex(Properties props, ICollection<string> stopwords, string dir, Func<CoreLabel, IDictionary<string, string>> transformSentenceToString)
		{
			try
			{
				Edu.Stanford.Nlp.Patterns.LuceneSentenceIndex sentindex = new Edu.Stanford.Nlp.Patterns.LuceneSentenceIndex(props, stopwords, dir, transformSentenceToString);
				sentindex.SetIndexReaderSearcher();
				System.Console.Out.WriteLine("Number of documents read from the index " + dir + " are " + sentindex.reader.NumDocs());
				sentindex.numAllSentences += sentindex.reader.NumDocs();
				return sentindex;
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
		}
	}
}
