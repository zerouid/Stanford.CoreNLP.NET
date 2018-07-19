using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Java.Util.Concurrent.Atomic;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Analysis.Core;
using Org.Apache.Lucene.Document;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns.Surface
{
	/// <summary>Created by sonalg on 10/22/14.</summary>
	public class PatternsForEachTokenLucene<E> : PatternsForEachToken<E>
		where E : Pattern
	{
		internal static IndexWriter indexWriter;

		internal static File indexDir = null;

		internal static Directory dir;

		internal static Analyzer analyzer = new KeywordAnalyzer();

		internal static IndexWriterConfig iwc = new IndexWriterConfig(Version.Lucene42, analyzer);

		internal static DirectoryReader reader = null;

		internal static IndexSearcher searcher;

		internal static AtomicBoolean openIndexWriter = new AtomicBoolean(false);

		internal string allPatternsDir;

		internal bool createPatLuceneIndex;

		public PatternsForEachTokenLucene(Properties props, IDictionary<string, IDictionary<int, ICollection<E>>> pats)
		{
			//ProtobufAnnotationSerializer p = new ProtobufAnnotationSerializer();
			ArgumentParser.FillOptions(this, props);
			if (allPatternsDir == null)
			{
				File f;
				try
				{
					f = File.CreateTempFile("allpatterns", "index");
					System.Console.Out.WriteLine("No directory provided for creating patternsForEachToken lucene index. Making it at " + f.GetAbsolutePath());
				}
				catch (IOException e)
				{
					throw new Exception(e);
				}
				f.DeleteOnExit();
				allPatternsDir = f.GetAbsolutePath();
			}
			if (createPatLuceneIndex)
			{
				Redwood.Log("Deleting any exising index at " + allPatternsDir);
				IOUtils.DeleteDirRecursively(new File(allPatternsDir));
			}
			indexDir = new File(allPatternsDir);
			if (pats != null)
			{
				AddPatterns(pats);
			}
		}

		//setIndexReaderSearcher();
		public virtual void CheckClean()
		{
			try
			{
				dir = FSDirectory.Open(indexDir);
				CheckIndex checkIndex = new CheckIndex(dir);
				CheckIndex.Status status = checkIndex.CheckIndex();
				System.Diagnostics.Debug.Assert((status.clean), "index is not clean");
				dir.Close();
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
		}

		public PatternsForEachTokenLucene(Properties props)
			: this(props, null)
		{
		}

		public override void SetupSearch()
		{
			SetIndexReaderSearcher();
		}

		internal static void SetIndexReaderSearcher()
		{
			lock (typeof(PatternsForEachTokenLucene))
			{
				try
				{
					FSDirectory index = NIOFSDirectory.Open(indexDir);
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
				catch (IOException e)
				{
					throw new Exception(e);
				}
			}
		}

		public override void AddPatterns(IDictionary<string, IDictionary<int, ICollection<E>>> pats)
		{
			try
			{
				SetIndexWriter();
				foreach (KeyValuePair<string, IDictionary<int, ICollection<E>>> en in pats)
				{
					//String sentence = StringUtils.joinWords(en.getValue(), " ");
					AddPatterns(en.Key, en.Value, false);
				}
				indexWriter.Commit();
			}
			catch (IOException e)
			{
				//closeIndexWriter();
				throw new Exception(e);
			}
		}

		//
		//  @Override
		//  public void finishUpdating() {
		//    if(indexWriter != null){
		//      try {
		//        indexWriter.commit();
		//      } catch (IOException e) {
		//        throw new RuntimeException(e);
		//      }
		//    }
		//    closeIndexWriter();
		//  }
		//
		//  @Override
		//  public void update(List<CoreLabel> tokens, String sentid) {
		//    try {
		//      setIndexWriter();
		//      indexWriter.deleteDocuments(new TermQuery(new Term("sentid",sentid)));
		//      add(tokens, sentid);
		//    } catch (IOException e) {
		//      throw new RuntimeException(e);
		//    }
		//
		//  }
		internal static void SetIndexWriter()
		{
			lock (typeof(PatternsForEachTokenLucene))
			{
				try
				{
					if (!openIndexWriter.Get())
					{
						dir = FSDirectory.Open(indexDir);
						Redwood.Log(Redwood.Dbg, "Updating lucene index at " + indexDir);
						indexWriter = new IndexWriter(dir, iwc);
						openIndexWriter.Set(true);
					}
				}
				catch (IOException e)
				{
					throw new Exception(e);
				}
			}
		}

		internal static void CloseIndexWriter()
		{
			lock (typeof(PatternsForEachTokenLucene))
			{
				try
				{
					if (openIndexWriter.Get())
					{
						indexWriter.Close();
						openIndexWriter.Set(false);
						indexWriter = null;
						Redwood.Log(Redwood.Dbg, "closing index writer");
					}
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
		}

		public override void Close()
		{
			CloseIndexWriter();
		}

		public override void Load(string allPatternsDir)
		{
			System.Diagnostics.Debug.Assert(new File(allPatternsDir).Exists());
		}

		public override void AddPatterns(string id, IDictionary<int, ICollection<E>> p)
		{
			AddPatterns(id, p, true);
		}

		private void AddPatterns(string id, IDictionary<int, ICollection<E>> p, bool commit)
		{
			try
			{
				SetIndexWriter();
				Org.Apache.Lucene.Document.Document doc = new Org.Apache.Lucene.Document.Document();
				doc.Add(new StringField("sentid", id, Field.Store.Yes));
				doc.Add(new Field("patterns", GetBytes(p), LuceneFieldType.NotIndexed));
				indexWriter.AddDocument(doc);
				if (commit)
				{
					indexWriter.Commit();
				}
			}
			catch (IOException e)
			{
				//closeIndexWriter();
				throw new Exception(e);
			}
		}

		private byte[] GetBytes(IDictionary<int, ICollection<E>> p)
		{
			try
			{
				ByteArrayOutputStream baos = new ByteArrayOutputStream();
				ObjectOutputStream oos = new ObjectOutputStream(baos);
				oos.WriteObject(p);
				return baos.ToByteArray();
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
		}

		//  private byte[] getProtoBufAnnotation(Map<Integer, Set<Integer>> p) {
		//    //TODO: finish this
		//    return new byte[0];
		//  }
		public override void CreateIndexIfUsingDBAndNotExists()
		{
			//nothing to do
			return;
		}

		public override IDictionary<int, ICollection<E>> GetPatternsForAllTokens(string sentId)
		{
			try
			{
				TermQuery query = new TermQuery(new Term("sentid", sentId));
				TopDocs tp = searcher.Search(query, 1);
				if (tp.totalHits > 0)
				{
					foreach (ScoreDoc s in tp.scoreDocs)
					{
						int docId = s.doc;
						Org.Apache.Lucene.Document.Document d = searcher.Doc(docId);
						byte[] st = d.GetBinaryValue("patterns").bytes;
						ByteArrayInputStream baip = new ByteArrayInputStream(st);
						ObjectInputStream ois = new ObjectInputStream(baip);
						return (IDictionary<int, ICollection<E>>)ois.ReadObject();
					}
				}
				else
				{
					throw new Exception("Why no patterns for sentid " + sentId + ". Number of documents in index are " + Size());
				}
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
			catch (TypeLoadException e)
			{
				throw new Exception(e);
			}
			return null;
		}

		public override bool Save(string dir)
		{
			//nothing to do
			return false;
		}

		//  private Map<Integer, Set<Integer>> readProtoBuf(byte[] patterns) {
		//      //TODO
		//  }
		//  @Override
		//  public ConcurrentHashIndex<SurfacePattern> readPatternIndex(String dir) {
		//    try {
		//      return IOUtils.readObjectFromFile(dir+"/patternshashindex.ser");
		//    } catch (IOException e) {
		//      throw new RuntimeException(e);
		//    } catch (ClassNotFoundException e) {
		//      throw new RuntimeException(e);
		//    }
		//  }
		//  @Override
		//  public void savePatternIndex(ConcurrentHashIndex<SurfacePattern> index, String dir) {
		//    try {
		//      if(dir != null)
		//        IOUtils.writeObjectToFile(index, dir+"/patternshashindex.ser");
		//    } catch (IOException e) {
		//      throw new RuntimeException(e);
		//    }
		//  }
		public override IDictionary<string, IDictionary<int, ICollection<E>>> GetPatternsForAllTokens(ICollection<string> sentIds)
		{
			Close();
			SetIndexReaderSearcher();
			IDictionary<string, IDictionary<int, ICollection<E>>> pats = new Dictionary<string, IDictionary<int, ICollection<E>>>();
			foreach (string s in sentIds)
			{
				pats[s] = GetPatternsForAllTokens(s);
			}
			SetIndexWriter();
			return pats;
		}

		internal override int Size()
		{
			SetIndexReaderSearcher();
			return searcher.GetIndexReader().NumDocs();
		}
	}
}
