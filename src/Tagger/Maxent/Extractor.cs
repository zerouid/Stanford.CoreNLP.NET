using System;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>
	/// This class serves as the base class for classes which extract relevant
	/// information from a history to give it to the features.
	/// </summary>
	/// <remarks>
	/// This class serves as the base class for classes which extract relevant
	/// information from a history to give it to the features. Every feature has
	/// an associated extractor or maybe more.  GlobalHolder keeps all the
	/// extractors; two histories are considered equal if all extractors return
	/// equal values for them.  The main functionality of the Extractors is
	/// provided by the method extract which takes a History as an argument.
	/// The Extractor looks at the history and takes out something important for
	/// the features - e.g. specific words and tags at specific positions or
	/// some function of the History. The histories are effectively vectors
	/// of values, with each dimension being the output of some extractor.
	/// <p>
	/// New extractors are created in either ExtractorFrames or
	/// ExtractorFramesRare; those are the places you want to consider
	/// adding your new extractor.  For a new Extractor, typically the things
	/// that you have to define are:
	/// <ul>
	/// <li>leftContext() and/or rightContext() if the extractor uses the tag
	/// sequence to the left or right (so that dynamic programming will be done
	/// correctly.
	/// <li>isLocal() Return true iff the function is only of the current word
	/// (for efficiency)
	/// <li>isDynamic() Return true if a function of any tags (for efficiency)
	/// <li>extract(History, PairsHolder) The actual function that returns the
	/// value for the feature.
	/// </ul>
	/// <p>
	/// Note that some extractors can be reused across multiple taggers,
	/// but many cannot.  Any extractor that uses information from the
	/// tagger such as its dictionary, for example, cannot.  For the
	/// moment, some of the extractors in ExtractorFrames and
	/// ExtractorFramesRare are static; those are all reusable at the
	/// moment, but if you change them in any way to make them not
	/// reusable, make sure to change the way they are constructed as well.
	/// </remarks>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	[System.Serializable]
	public class Extractor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.Extractor));

		private const long serialVersionUID = -4694133872973560083L;

		internal const string zeroSt = "0";

		internal readonly int position;

		private readonly bool isTag;

		public Extractor()
			: this(int.MaxValue, false)
		{
		}

		/// <summary>
		/// This constructor creates an extractor which extracts either the tag or
		/// the word from position position in the history.
		/// </summary>
		/// <param name="position">
		/// The position of the thing to be extracted. This is
		/// relative to the current word. For example, position 0
		/// will be the current word, -1 will be
		/// the word before +1 will be the word after, etc.
		/// </param>
		/// <param name="isTag">
		/// If true this means that the POS tag is extracted from
		/// position, otherwise the word is extracted.
		/// </param>
		protected internal Extractor(int position, bool isTag)
		{
			this.position = position;
			this.isTag = isTag;
		}

		/// <summary>
		/// Subclasses should override this method and keep only the data
		/// they want about the tagger.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this method and keep only the data
		/// they want about the tagger.  Note that such data should also be
		/// declared "transient" if it is already available in the tagger.
		/// This is because, when we save the tagger to disk, we do so by
		/// writing out objects, and there is no need to write the same
		/// object more than once.  setGlobalHolder will be called both after
		/// construction when building a new tag and when loading existing
		/// taggers from disk, so the same data will available then as well.
		/// </remarks>
		protected internal virtual void SetGlobalHolder(MaxentTagger tagger)
		{
		}

		/// <summary>
		/// This evaluates any precondition for a feature being applicable based
		/// on a certain tag.
		/// </summary>
		/// <remarks>
		/// This evaluates any precondition for a feature being applicable based
		/// on a certain tag. It returns true if the feature is applicable.
		/// By default an Extractor is applicable everywhere, but some
		/// subclasses limit application.
		/// </remarks>
		/// <param name="tag">The possible tag that the feature will be generated for</param>
		/// <returns>Whether the feature extractor is applicable (true) or not (false)</returns>
		public virtual bool Precondition(string tag)
		{
			return true;
		}

		/// <returns>the number of positions to the left the extractor looks at (only tags, because words are fixed.)</returns>
		public virtual int LeftContext()
		{
			if (isTag)
			{
				if (position < 0)
				{
					return -position;
				}
			}
			return 0;
		}

		/// <returns>the number of positions to the right the extractor looks at (only tags, because words are fixed.)</returns>
		public virtual int RightContext()
		{
			if (isTag)
			{
				if (position > 0)
				{
					return position;
				}
			}
			return 0;
		}

		// CDM May 2007: This feature is currently never used. Maybe we should
		// change things so it is, and each feature template has a threshold, but
		// need to then work out what a TaggerFeature is and whether we should still
		// be using one of those to index with.
		// At present real threshold check happens in TaggerExperiments with
		// the populated(int, int) method.
		//  public boolean isPopulated(TaggerFeature f) {
		//    return (f.indexedValues.length > GlobalHolder.minFeatureThresh);
		//  }
		/// <summary>
		/// Subclasses should only override the two argument version
		/// of this method.
		/// </summary>
		/// <param name="h">The history to extract from</param>
		/// <returns>The feature value</returns>
		internal string Extract(History h)
		{
			return Extract(h, h.pairs);
		}

		/// <returns>
		/// Returns true if extractor is a function of POS tags; if it returns false,
		/// features are pre-computed.
		/// </returns>
		public virtual bool IsDynamic()
		{
			return isTag;
		}

		/// <returns>
		/// Returns true if extractor is not a function of POS tags, and only
		/// depends on current word.
		/// </returns>
		public virtual bool IsLocal()
		{
			return !isTag && position == 0;
		}

		internal virtual string Extract(History h, PairsHolder pH)
		{
			return isTag ? pH.GetTag(h, position) : pH.GetWord(h, position);
		}

		internal virtual string ExtractLV(History h, PairsHolder pH)
		{
			// should extract last verbal word and also the current word
			int start = h.start;
			string lastverb = "NA";
			int current = h.current;
			int index = current - 1;
			while (index >= start)
			{
				string tag = pH.GetTag(index);
				if (tag.StartsWith("VB"))
				{
					lastverb = pH.GetWord(index);
					break;
				}
				if (tag.StartsWith(","))
				{
					break;
				}
				index--;
			}
			return lastverb;
		}

		internal virtual string ExtractLV(History h, PairsHolder pH, int bound)
		{
			// should extract last verbal word and also the current word
			int start = h.start;
			string lastverb = "NA";
			int current = h.current;
			int index = current - 1;
			while ((index >= start) && (index >= current - bound))
			{
				string tag = pH.GetTag(index);
				if (tag.StartsWith("VB"))
				{
					lastverb = pH.GetWord(index);
					break;
				}
				if (tag.StartsWith(","))
				{
					break;
				}
				index--;
			}
			return lastverb;
		}

		// By default the bound is ignored, but a few subclasses make use of it.
		internal virtual string Extract(History h, PairsHolder pH, int bound)
		{
			return Extract(h, pH);
		}

		public override string ToString()
		{
			string cl = GetType().FullName;
			int ind = cl.LastIndexOf('.');
			// MAX_VALUE is the default value and means we aren't using these two arguments
			string args = (position == int.MaxValue) ? string.Empty : (position + "," + (isTag ? "tag" : "word"));
			return Sharpen.Runtime.Substring(cl, ind + 1) + '(' + args + ')';
		}

		/// <summary>This is used for argument parsing in arch variable.</summary>
		/// <remarks>
		/// This is used for argument parsing in arch variable.
		/// It can extract a comma separated argument.
		/// Assumes the input format is "name(arg,arg,arg)".
		/// </remarks>
		/// <param name="str">arch variable component input</param>
		/// <param name="num">Number of argument</param>
		/// <returns>The parenthesized String, or null if none.</returns>
		internal static string GetParenthesizedArg(string str, int num)
		{
			string[] args = str.Split("\\s*[,()]\\s*");
			if (args.Length <= num)
			{
				return null;
			}
			// log.info("getParenthesizedArg split " + str + " into " + args.length + " pieces; returning number " + num);
			// for (int i = 0; i < args.length; i++) {
			//   log.info("  " + args[i]);
			// }
			return args[num];
		}

		/// <summary>This is used for argument parsing in arch variable.</summary>
		/// <remarks>
		/// This is used for argument parsing in arch variable.
		/// It can extract a comma separated argument.
		/// Assumes the input format is "name(arg,arg,arg)", with possible
		/// spaces around the parentheses and comma(s).
		/// </remarks>
		/// <param name="str">arch variable component input</param>
		/// <param name="num">Number of argument</param>
		/// <returns>The int value of the arg or 0 if missing or empty</returns>
		internal static int GetParenthesizedNum(string str, int num)
		{
			string[] args = str.Split("\\s*[,()]\\s*");
			int ans = 0;
			try
			{
				ans = System.Convert.ToInt32(args[num]);
			}
			catch (Exception)
			{
			}
			// just leave ans as 0
			return ans;
		}
	}
}
