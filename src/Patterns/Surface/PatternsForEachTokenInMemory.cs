using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Java.Util.Concurrent;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns.Surface
{
	/// <summary>Created by sonalg on 10/22/14.</summary>
	public class PatternsForEachTokenInMemory<E> : PatternsForEachToken<E>
		where E : Pattern
	{
		public static ConcurrentHashMap<string, IDictionary<int, ICollection<Pattern>>> patternsForEachToken = null;

		public PatternsForEachTokenInMemory(Properties props, IDictionary<string, IDictionary<int, ICollection<E>>> pats)
		{
			ArgumentParser.FillOptions(this, props);
			//TODO: make this atomic
			if (patternsForEachToken == null)
			{
				patternsForEachToken = new ConcurrentHashMap<string, IDictionary<int, ICollection<Pattern>>>();
			}
			if (pats != null)
			{
				AddPatterns(pats);
			}
		}

		public PatternsForEachTokenInMemory(Properties props)
			: this(props, null)
		{
		}

		public override void AddPatterns(string sentId, IDictionary<int, ICollection<E>> patterns)
		{
			if (!patternsForEachToken.Contains(sentId))
			{
				patternsForEachToken[sentId] = new ConcurrentHashMap<int, ICollection<Pattern>>();
			}
			patternsForEachToken[sentId].PutAll(patterns);
		}

		public override void AddPatterns(IDictionary<string, IDictionary<int, ICollection<E>>> pats)
		{
			foreach (KeyValuePair<string, IDictionary<int, ICollection<E>>> en in pats)
			{
				AddPatterns(en.Key, en.Value);
			}
		}

		public override IDictionary<int, ICollection<E>> GetPatternsForAllTokens(string sentId)
		{
			return (IDictionary<int, ICollection<E>>)(patternsForEachToken.Contains(sentId) ? patternsForEachToken[sentId] : Java.Util.Collections.EmptyMap());
		}

		public override void SetupSearch()
		{
		}

		//nothing to do
		//  @Override
		//  public ConcurrentHashIndex<SurfacePattern> readPatternIndex(String dir) throws IOException, ClassNotFoundException {
		//    return IOUtils.readObjectFromFile(dir+"/patternshashindex.ser");
		//  }
		//
		//  @Override
		//  public void savePatternIndex(ConcurrentHashIndex<SurfacePattern> index, String dir) throws IOException {
		//    if(dir != null){
		//    writePatternsIfInMemory(dir+"/allpatterns.ser");
		//    IOUtils.writeObjectToFile(index, dir+"/patternshashindex.ser");
		//    }
		//  }
		public override IDictionary<string, IDictionary<int, ICollection<E>>> GetPatternsForAllTokens(ICollection<string> sampledSentIds)
		{
			IDictionary<string, IDictionary<int, ICollection<E>>> pats = new Dictionary<string, IDictionary<int, ICollection<E>>>();
			foreach (string s in sampledSentIds)
			{
				pats[s] = GetPatternsForAllTokens(s);
			}
			return pats;
		}

		public override void Close()
		{
		}

		//nothing to do
		public override void Load(string allPatternsDir)
		{
			try
			{
				AddPatterns(IOUtils.ReadObjectFromFile(allPatternsDir + "/allpatterns.ser"));
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		public override bool Save(string dir)
		{
			try
			{
				IOUtils.EnsureDir(new File(dir));
				string f = dir + "/allpatterns.ser";
				IOUtils.WriteObjectToFile(this.patternsForEachToken, f);
				Redwood.Log(Redwood.Dbg, "Saving the patterns to " + f);
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
			return true;
		}

		public override void CreateIndexIfUsingDBAndNotExists()
		{
			//nothing to do
			return;
		}

		public virtual bool ContainsSentId(string sentId)
		{
			return this.patternsForEachToken.Contains(sentId);
		}

		internal override int Size()
		{
			return this.patternsForEachToken.Count;
		}
	}
}
