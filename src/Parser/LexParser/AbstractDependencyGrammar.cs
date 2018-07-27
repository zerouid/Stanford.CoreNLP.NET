using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>An abstract base class for dependency grammars.</summary>
	/// <remarks>
	/// An abstract base class for dependency grammars.  The only thing you have
	/// to implement in a subclass is scoreTB (score a "tag binned" dependency
	/// in the tagProjection space).  A subclass also has to either call
	/// super() in its constructor, or otherwise initialize the tagBin array.
	/// The call to initTagBins() (in the constructor) must be made after all
	/// keys have been entered into tagIndex.
	/// </remarks>
	/// <author>Galen Andrew</author>
	[System.Serializable]
	public abstract class AbstractDependencyGrammar : IDependencyGrammar
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.AbstractDependencyGrammar));

		protected internal ITagProjection tagProjection;

		protected internal readonly IIndex<string> tagIndex;

		protected internal readonly IIndex<string> wordIndex;

		protected internal int numTagBins;

		protected internal int[] tagBin;

		protected internal ITreebankLanguagePack tlp;

		protected internal bool directional;

		protected internal bool useDistance;

		protected internal bool useCoarseDistance;

		protected internal ILexicon lex;

		protected internal readonly IntTaggedWord stopTW;

		protected internal readonly IntTaggedWord wildTW;

		[System.NonSerialized]
		protected internal IDictionary<IntDependency, IntDependency> expandDependencyMap = Generics.NewHashMap();

		private const bool Debug = false;

		protected internal int[] coarseDistanceBins = new int[] { 0, 2, 5 };

		protected internal int[] regDistanceBins = new int[] { 0, 1, 5, 10 };

		protected internal readonly Options op;

		[System.NonSerialized]
		protected internal Interner<IntTaggedWord> itwInterner = new Interner<IntTaggedWord>();

		public AbstractDependencyGrammar(ITreebankLanguagePack tlp, ITagProjection tagProjection, bool directional, bool useDistance, bool useCoarseDistance, Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			this.tlp = tlp;
			this.tagProjection = tagProjection;
			this.directional = directional;
			this.useDistance = useDistance;
			this.useCoarseDistance = useCoarseDistance;
			this.op = op;
			this.wordIndex = wordIndex;
			this.tagIndex = tagIndex;
			stopTW = new IntTaggedWord(IntTaggedWord.StopWordInt, IntTaggedWord.StopTagInt);
			wildTW = new IntTaggedWord(IntTaggedWord.AnyWordInt, IntTaggedWord.AnyTagInt);
			InitTagBins();
		}

		public virtual void SetLexicon(ILexicon lexicon)
		{
			lex = lexicon;
		}

		/// <summary>Default is no-op.</summary>
		public virtual void Tune(ICollection<Tree> trees)
		{
		}

		public virtual int NumTagBins()
		{
			return numTagBins;
		}

		public virtual int TagBin(int tag)
		{
			if (tag < 0)
			{
				return tag;
			}
			else
			{
				return tagBin[tag];
			}
		}

		public virtual bool RootTW(IntTaggedWord rTW)
		{
			// System.out.println("rootTW: checking if " + rTW.toString("verbose") +
			// " == " + Lexicon.BOUNDARY_TAG + "[" +
			// tagIndex.indexOf(Lexicon.BOUNDARY_TAG) + "]" + ": " +
			// (rTW.tag == tagIndex.indexOf(Lexicon.BOUNDARY_TAG)));
			return rTW.tag == tagIndex.IndexOf(LexiconConstants.BoundaryTag);
		}

		protected internal virtual short ValenceBin(int distance)
		{
			if (!useDistance)
			{
				return 0;
			}
			if (distance < 0)
			{
				return -1;
			}
			if (distance == 0)
			{
				return 0;
			}
			return 1;
		}

		public virtual int NumDistBins()
		{
			return useCoarseDistance ? 4 : 5;
		}

		public virtual short DistanceBin(int distance)
		{
			if (!useDistance)
			{
				return 0;
			}
			else
			{
				if (useCoarseDistance)
				{
					return CoarseDistanceBin(distance);
				}
				else
				{
					return RegDistanceBin(distance);
				}
			}
		}

		public virtual short RegDistanceBin(int distance)
		{
			for (short i = 0; i < regDistanceBins.Length; ++i)
			{
				if (distance <= regDistanceBins[i])
				{
					return i;
				}
			}
			return (short)regDistanceBins.Length;
		}

		public virtual short CoarseDistanceBin(int distance)
		{
			for (short i = 0; i < coarseDistanceBins.Length; ++i)
			{
				if (distance <= coarseDistanceBins[i])
				{
					return i;
				}
			}
			return (short)coarseDistanceBins.Length;
		}

		internal virtual void SetCoarseDistanceBins(int[] bins)
		{
			System.Diagnostics.Debug.Assert((bins.Length == 3));
			coarseDistanceBins = bins;
		}

		internal virtual void SetRegDistanceBins(int[] bins)
		{
			System.Diagnostics.Debug.Assert((bins.Length == 4));
			regDistanceBins = bins;
		}

		protected internal virtual void InitTagBins()
		{
			IIndex<string> tagBinIndex = new HashIndex<string>();
			tagBin = new int[tagIndex.Size()];
			for (int t = 0; t < tagBin.Length; t++)
			{
				string tagStr = tagIndex.Get(t);
				string binStr;
				if (tagProjection == null)
				{
					binStr = tagStr;
				}
				else
				{
					binStr = tagProjection.Project(tagStr);
				}
				tagBin[t] = tagBinIndex.AddToIndex(binStr);
			}
			numTagBins = tagBinIndex.Size();
		}

		public virtual double Score(IntDependency dependency)
		{
			return ScoreTB(dependency.head.word, TagBin(dependency.head.tag), dependency.arg.word, TagBin(dependency.arg.tag), dependency.leftHeaded, dependency.distance);
		}

		// currently unused
		public virtual double Score(int headWord, int headTag, int argWord, int argTag, bool leftHeaded, int dist)
		{
			IntDependency tempDependency = new IntDependency(headWord, headTag, argWord, argTag, leftHeaded, dist);
			return Score(tempDependency);
		}

		// this method tag bins
		public virtual double ScoreTB(int headWord, int headTag, int argWord, int argTag, bool leftHeaded, int dist)
		{
			IntDependency tempDependency = new IntDependency(headWord, headTag, argWord, argTag, leftHeaded, dist);
			return ScoreTB(tempDependency);
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private void ReadObject(ObjectInputStream ois)
		{
			ois.DefaultReadObject();
			// reinitialize the transient objects
			itwInterner = new Interner<IntTaggedWord>();
		}

		/// <summary>Default is to throw exception.</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual void ReadData(BufferedReader @in)
		{
			throw new NotSupportedException();
		}

		/// <summary>Default is to throw exception.</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual void WriteData(PrintWriter @out)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// This is a custom interner that simultaneously creates and interns
		/// an IntDependency.
		/// </summary>
		/// <returns>An interned IntDependency</returns>
		protected internal virtual IntDependency Intern(IntTaggedWord headTW, IntTaggedWord argTW, bool leftHeaded, short dist)
		{
			IDictionary<IntDependency, IntDependency> map = expandDependencyMap;
			IntDependency internTempDependency = new IntDependency(itwInterner.Intern(headTW), itwInterner.Intern(argTW), leftHeaded, dist);
			IntDependency returnDependency = internTempDependency;
			if (map != null)
			{
				returnDependency = map[internTempDependency];
				if (returnDependency == null)
				{
					map[internTempDependency] = internTempDependency;
					returnDependency = internTempDependency;
				}
			}
			return returnDependency;
		}

		private const long serialVersionUID = 3L;

		public abstract double ScoreTB(IntDependency arg1);
	}
}
