using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>Maintains a set of feature extractors for a maxent POS tagger and applies them.</summary>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	[System.Serializable]
	public class Extractors
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.Extractors));

		private readonly Extractor[] v;

		private const bool Debug = false;

		[System.NonSerialized]
		internal IList<Pair<int, Extractor>> local;

		[System.NonSerialized]
		internal IList<Pair<int, Extractor>> localContext;

		[System.NonSerialized]
		internal IList<Pair<int, Extractor>> dynamic;

		/// <summary>Set the extractors from an array.</summary>
		/// <param name="extrs">The array of extractors.  It is copied in this init.</param>
		public Extractors(Extractor[] extrs)
		{
			// extractors only looking at current word
			// extractors only looking at words, except those in "local"
			// extractors depending on class labels
			v = new Extractor[extrs.Length];
			System.Array.Copy(extrs, 0, v, 0, extrs.Length);
			InitTypes();
		}

		/// <summary>Determine type of each feature extractor.</summary>
		internal virtual void InitTypes()
		{
			local = new List<Pair<int, Extractor>>();
			localContext = new List<Pair<int, Extractor>>();
			dynamic = new List<Pair<int, Extractor>>();
			for (int i = 0; i < v.Length; ++i)
			{
				Extractor e = v[i];
				if (e.IsLocal() && e.IsDynamic())
				{
					throw new Exception("Extractors can't both be local and dynamic!");
				}
				if (e.IsLocal())
				{
					local.Add(Pair.MakePair(i, e));
				}
				else
				{
					//localContext.put(i,e);
					if (e.IsDynamic())
					{
						dynamic.Add(Pair.MakePair(i, e));
					}
					else
					{
						localContext.Add(Pair.MakePair(i, e));
					}
				}
			}
		}

		/// <summary>Extract using the i'th extractor.</summary>
		/// <param name="i">The extractor to use</param>
		/// <param name="h">The history to extract from</param>
		/// <returns>String The feature value</returns>
		internal virtual string Extract(int i, History h)
		{
			return v[i].Extract(h);
		}

		internal virtual bool Equals(History h, History h1)
		{
			foreach (Extractor extractor in v)
			{
				if (!(extractor.Extract(h).Equals(extractor.Extract(h1))))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Find maximum left context of extractors.</summary>
		/// <remarks>Find maximum left context of extractors. Used in TagInference to decide windows for dynamic programming.</remarks>
		/// <returns>The maximum of the left contexts used by all extractors.</returns>
		internal virtual int LeftContext()
		{
			int max = 0;
			foreach (Extractor extractor in v)
			{
				int lf = extractor.LeftContext();
				if (lf > max)
				{
					max = lf;
				}
			}
			return max;
		}

		/// <summary>Find maximum right context of extractors.</summary>
		/// <remarks>Find maximum right context of extractors. Used in TagInference to decide windows for dynamic programming.</remarks>
		/// <returns>The maximum of the right contexts used by all extractors.</returns>
		internal virtual int RightContext()
		{
			int max = 0;
			foreach (Extractor extractor in v)
			{
				int lf = extractor.RightContext();
				if (lf > max)
				{
					max = lf;
				}
			}
			return max;
		}

		public virtual int Size()
		{
			return v.Length;
		}

		protected internal virtual void SetGlobalHolder(MaxentTagger tagger)
		{
			foreach (Extractor extractor in v)
			{
				extractor.SetGlobalHolder(tagger);
			}
		}

		/*
		public void save(String filename) {
		try {
		DataOutputStream rf = IOUtils.getDataOutputStream(filename);
		rf.writeInt(v.length);
		for (Extractor extr : v) {
		rf.writeBytes(extr.toString());
		}
		rf.close();
		} catch (IOException e) {
		e.printStackTrace();
		}
		}
		
		public void read(String filename) {
		try {
		InDataStreamFile rf = new InDataStreamFile(filename);
		int len = rf.readInt();
		v = new Extractor[len];
		//GlobalHolder.init();
		} catch (IOException e) {
		e.printStackTrace();
		}
		}
		*/
		internal virtual Extractor Get(int index)
		{
			return v[index];
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder("Extractors[");
			for (int i = 0; i < v.Length; i++)
			{
				sb.Append(v[i]);
				if (i < v.Length - 1)
				{
					sb.Append(", ");
				}
			}
			sb.Append(']');
			return sb.ToString();
		}

		/// <summary>
		/// Prints out the pair of
		/// <c>Extractors</c>
		/// objects found in the
		/// file that is the first and only argument.
		/// </summary>
		/// <param name="args">
		/// Filename of extractors file (standardly written with
		/// <c>.ex</c>
		/// extension)
		/// </param>
		public static void Main(string[] args)
		{
			try
			{
				ObjectInputStream @in = new ObjectInputStream(new FileInputStream(args[0]));
				Edu.Stanford.Nlp.Tagger.Maxent.Extractors extrs = (Edu.Stanford.Nlp.Tagger.Maxent.Extractors)@in.ReadObject();
				Edu.Stanford.Nlp.Tagger.Maxent.Extractors extrsRare = (Edu.Stanford.Nlp.Tagger.Maxent.Extractors)@in.ReadObject();
				@in.Close();
				System.Console.Out.WriteLine("All words:  " + extrs);
				System.Console.Out.WriteLine("Rare words: " + extrsRare);
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		private const long serialVersionUID = -4777107742414749890L;
	}
}
