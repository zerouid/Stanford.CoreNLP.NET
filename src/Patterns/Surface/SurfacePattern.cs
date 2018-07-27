using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Patterns.Surface
{
	/// <summary>
	/// To represent a surface pattern in more detail than TokenSequencePattern (this
	/// class object is eventually compiled as TokenSequencePattern via the toString
	/// method).
	/// </summary>
	/// <remarks>
	/// To represent a surface pattern in more detail than TokenSequencePattern (this
	/// class object is eventually compiled as TokenSequencePattern via the toString
	/// method). See
	/// <see cref="PatternToken"/>
	/// for more info on how matching of target
	/// phrases is done.
	/// Author: Sonal Gupta (sonalg@stanford.edu)
	/// </remarks>
	[System.Serializable]
	public class SurfacePattern : Pattern, IComparable<Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern>
	{
		public override CollectionValuedMap<string, string> GetRelevantWords()
		{
			CollectionValuedMap<string, string> relwordsThisPat = new CollectionValuedMap<string, string>();
			Token[] next = GetNextContext();
			GetRelevantWordsBase(next, relwordsThisPat);
			Token[] prev = GetPrevContext();
			GetRelevantWordsBase(prev, relwordsThisPat);
			return relwordsThisPat;
		}

		public override int EqualContext(Pattern p)
		{
			return EqualContext((Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern)p);
		}

		private const long serialVersionUID = 1L;

		public Token[] prevContext;

		public Token[] nextContext;

		public PatternToken token;

		protected internal int hashcode;

		protected internal SurfacePatternFactory.Genre genre;

		// String prevContextStr = "", nextContextStr = "";
		// protected String[] originalPrev;
		// protected String[] originalNext;
		// protected String originalPrevStr = "";
		// protected String originalNextStr = "";
		// protected String toString;
		public virtual SurfacePatternFactory.Genre GetGenre()
		{
			return genre;
		}

		public virtual void SetGenre(SurfacePatternFactory.Genre genre)
		{
			this.genre = genre;
		}

		public SurfacePattern(Token[] prevContext, PatternToken token, Token[] nextContext, SurfacePatternFactory.Genre genre)
			: base(PatternFactory.PatternType.Surface)
		{
			this.SetPrevContext(prevContext);
			this.SetNextContext(nextContext);
			this.SetToken(token);
			this.genre = genre;
			hashcode = ToString().GetHashCode();
		}

		public virtual Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern CopyNewToken()
		{
			return new Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern(this.prevContext, token.Copy(), this.nextContext, genre);
		}

		public static Token GetContextToken(CoreLabel tokenj)
		{
			Token token = new Token(PatternFactory.PatternType.Surface);
			token.AddORRestriction(typeof(PatternsAnnotations.ProcessedTextAnnotation), tokenj.Get(typeof(PatternsAnnotations.ProcessedTextAnnotation)));
			return token;
		}

		//  public static String getContextStr(CoreLabel tokenj, boolean useLemmaContextTokens, boolean lowerCaseContext) {
		//    String str = "";
		//
		//    if (useLemmaContextTokens) {
		//      String tok = tokenj.lemma();
		//      if (lowerCaseContext)
		//        tok = tok.toLowerCase();
		//      str = "[{lemma:/" + Pattern.quote(tok.replaceAll("/", "\\\\/"))+ "/}] ";
		//      //str = "[{lemma:/\\Q" + tok.replaceAll("/", "\\\\/") + "\\E/}] ";
		//    } else {
		//      String tok = tokenj.word();
		//      if (lowerCaseContext)
		//        tok = tok.toLowerCase();
		//      str = "[{word:/" + Pattern.quote(tok.replaceAll("/", "\\\\/")) + "/}] ";
		//      //str = "[{word:/\\Q" + tok.replaceAll("/", "\\\\/") + "\\E/}] ";
		//
		//    }
		//    return str;
		//  }
		public static string GetContextStr(string w)
		{
			string str = "[/" + Pattern.Quote(w.ReplaceAll("/", "\\\\/")) + "/] ";
			//String str = "[/\\Q" + w.replaceAll("/", "\\\\/") + "\\E/] ";
			return str;
		}

		public override string ToString(IList<string> notAllowedClasses)
		{
			string prevContextStr = string.Empty;
			string nextContextStr = string.Empty;
			if (prevContext != null)
			{
				prevContextStr = StringUtils.Join(prevContext, " ");
			}
			if (nextContext != null)
			{
				nextContextStr = StringUtils.Join(nextContext, " ");
			}
			return (prevContextStr + " " + GetToken().GetTokenStr(notAllowedClasses) + " " + nextContextStr).Trim();
		}

		public virtual string ToString(string morePreviousPattern, string moreNextPattern, IList<string> notAllowedClasses)
		{
			string prevContextStr = string.Empty;
			string nextContextStr = string.Empty;
			if (prevContext != null)
			{
				prevContextStr = StringUtils.Join(prevContext, " ");
			}
			if (nextContext != null)
			{
				nextContextStr = StringUtils.Join(nextContext, " ");
			}
			return (prevContextStr + " " + morePreviousPattern + " " + GetToken().GetTokenStr(notAllowedClasses) + " " + moreNextPattern + " " + nextContextStr).Trim();
		}

		public virtual string GetPrevContextStr()
		{
			string prevContextStr = string.Empty;
			if (prevContext != null)
			{
				prevContextStr = StringUtils.Join(prevContext, " ");
			}
			return prevContextStr;
		}

		public virtual string GetNextContextStr()
		{
			string nextContextStr = string.Empty;
			if (nextContext != null)
			{
				nextContextStr = StringUtils.Join(nextContext, " ");
			}
			return nextContextStr;
		}

		// returns 0 is exactly equal, Integer.MAX_VALUE if the contexts are not same.
		// If contexts are same : it returns (objects restrictions on the token minus
		// p's restrictions on the token). So if returns negative then p has more
		// restrictions.
		public virtual int EqualContext(Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern p)
		{
			if (p.Equals(this))
			{
				return 0;
			}
			if (Arrays.Equals(this.prevContext, p.GetPrevContext()) && Arrays.Equals(this.nextContext, p.GetNextContext()))
			{
				int this_restriction = 0;
				int p_restriction = 0;
				if (this.GetToken().useTag)
				{
					this_restriction++;
				}
				if (p.GetToken().useTag)
				{
					p_restriction++;
				}
				if (this.GetToken().useNER)
				{
					this_restriction++;
				}
				if (p.GetToken().useNER)
				{
					p_restriction++;
				}
				if (this.GetToken().useTargetParserParentRestriction)
				{
					this_restriction++;
				}
				if (p.GetToken().useTargetParserParentRestriction)
				{
					p_restriction++;
				}
				this_restriction -= this.GetToken().numWordsCompound;
				p_restriction -= this.GetToken().numWordsCompound;
				return this_restriction - p_restriction;
			}
			return int.MaxValue;
		}

		public override bool Equals(object b)
		{
			if (!(b is Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern))
			{
				return false;
			}
			Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern p = (Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern)b;
			// if (toString().equals(p.toString()))
			if (!token.Equals(p.token))
			{
				return false;
			}
			if ((this.prevContext == null && p.prevContext != null) || (this.prevContext != null && p.prevContext == null))
			{
				return false;
			}
			if ((this.nextContext == null && p.nextContext != null) || (this.nextContext != null && p.nextContext == null))
			{
				return false;
			}
			if (this.prevContext != null && !Arrays.Equals(this.prevContext, p.prevContext))
			{
				return false;
			}
			if (this.nextContext != null && !Arrays.Equals(this.nextContext, p.nextContext))
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			return hashcode;
		}

		public override string ToString()
		{
			return ToString(null);
		}

		public virtual string ToStringToWrite()
		{
			return GetPrevContextStr() + "##" + GetToken().ToStringToWrite() + "##" + GetNextContextStr();
		}

		public virtual string[] GetSimplerTokensPrev()
		{
			return GetSimplerTokens(prevContext);
		}

		public virtual string[] GetSimplerTokensNext()
		{
			return GetSimplerTokens(nextContext);
		}

		//  static Pattern p1 = Pattern.compile(Pattern.quote("[") + "\\s*" + Pattern.quote("{") + "\\s*(lemma|word)\\s*:\\s*/" + Pattern.quote("\\Q") + "(.*)"
		//      + Pattern.quote("\\E") + "/\\s*" + Pattern.quote("}") + "\\s*" + Pattern.quote("]"));
		//
		//  static Pattern p2 = Pattern.compile(Pattern.quote("[") + "\\s*" + Pattern.quote("{") + "\\s*(.*)\\s*:\\s*(.*)\\s*" + Pattern.quote("}") + "\\s*"
		//      + Pattern.quote("]"));
		public virtual string[] GetSimplerTokens(Token[] p)
		{
			if (p == null)
			{
				return null;
			}
			string[] sim = new string[p.Length];
			for (int i = 0; i < p.Length; i++)
			{
				System.Diagnostics.Debug.Assert(p[i] != null, "How is the any one " + Arrays.ToString(p) + " null!");
				sim[i] = p[i].GetSimple();
			}
			return sim;
		}

		/*
		public String[] getSimplerTokens(String[] p) {
		if (p == null)
		return null;
		
		String[] sim = new String[p.length];
		for (int i = 0; i < p.length; i++) {
		
		assert p[i] != null : "How is the any one " + Arrays.toString(p) + " null!";
		
		if (p1 == null)
		throw new RuntimeException("how is p1 null");
		
		Matcher m = p1.matcher(p[i]);
		
		if (m.matches()) {
		sim[i] = m.group(2);
		} else {
		Matcher m2 = p2.matcher(p[i]);
		if (m2.matches()) {
		sim[i] = m2.group(2);
		} else if (p[i].startsWith("$FILLER"))
		sim[i] = "FW";
		else if (p[i].startsWith("$STOP"))
		sim[i] = "SW";
		else
		throw new RuntimeException("Cannot understand " + p[i]);
		}
		}
		return sim;
		
		}
		*/
		public override string ToStringSimple()
		{
			string[] simprev = GetSimplerTokensPrev();
			string[] simnext = GetSimplerTokensNext();
			string prevstr = simprev == null ? string.Empty : StringUtils.Join(simprev, " ");
			string nextstr = simnext == null ? string.Empty : StringUtils.Join(simnext, " ");
			string sim = prevstr.Trim() + " <b>" + GetToken().ToStringToWrite() + "</b> " + nextstr.Trim();
			return sim;
		}

		public virtual Token[] GetPrevContext()
		{
			return prevContext;
		}

		public virtual void SetPrevContext(Token[] prevContext)
		{
			this.prevContext = prevContext;
		}

		public virtual Token[] GetNextContext()
		{
			return nextContext;
		}

		public virtual void SetNextContext(Token[] nextContext)
		{
			this.nextContext = nextContext;
		}

		public virtual PatternToken GetToken()
		{
			return token;
		}

		public virtual void SetToken(PatternToken token)
		{
			this.token = token;
		}

		// private String getOriginalPrevStr() {
		// String originalPrevStr = "";
		// if (originalPrev != null)
		// originalPrevStr = StringUtils.join(originalPrev, " ");
		//
		// return originalPrevStr;
		// }
		// public void setOriginalPrevStr(String originalPrevStr) {
		// this.originalPrevStr = originalPrevStr;
		// }
		// public String getOriginalNextStr() {
		// String originalNextStr = "";
		// if (originalNext != null)
		// originalNextStr = StringUtils.join(originalNext, " ");
		// return originalNextStr;
		// }
		// public void setOriginalNextStr(String originalNextStr) {
		// this.originalNextStr = originalNextStr;
		// }
		// public String[] getOriginalPrev() {
		// return originalPrev;
		// }
		//
		// public void setOriginalPrev(String[] originalPrev) {
		// this.originalPrev = originalPrev;
		// }
		//
		// public String[] getOriginalNext() {
		// return originalNext;
		// }
		//
		// public void setOriginalNext(String[] originalNext) {
		// this.originalNext = originalNext;
		// }
		public static bool SameGenre(Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern p1, Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern p2)
		{
			return p1.GetGenre().Equals(p2.GetGenre());
		}

		/// <summary>True if array1 contains array2.</summary>
		/// <remarks>
		/// True if array1 contains array2. Also true if both array1 and array2 are
		/// null
		/// </remarks>
		/// <param name="array1"/>
		/// <param name="array2"/>
		/// <returns/>
		public static bool SubsumesArray(object[] array1, object[] array2)
		{
			if ((array1 == null && array2 == null))
			{
				return true;
			}
			// only one of them is null
			if (array1 == null || array2 == null)
			{
				return false;
			}
			if (array2.Length > array1.Length)
			{
				return false;
			}
			for (int i = 0; i < array1.Length; i++)
			{
				if (array1[i].Equals(array2[0]))
				{
					bool found = true;
					for (int j = 0; j < array2.Length; j++)
					{
						if (array1.Length <= i + j || !array2[j].Equals(array1[i + j]))
						{
							found = false;
							break;
						}
					}
					if (found)
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>True p1 subsumes p2 (p1 has longer context than p2)</summary>
		/// <param name="p1"/>
		/// <param name="p2"/>
		/// <returns/>
		public static bool Subsumes(Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern p1, Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern p2)
		{
			if (SubsumesArray(p1.GetNextContext(), p2.GetNextContext()) && SubsumesArray(p1.GetPrevContext(), p2.GetPrevContext()))
			{
				return true;
			}
			return false;
		}

		// true if one pattern subsumes another
		public static bool SubsumesEitherWay(Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern p1, Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern p2)
		{
			if (Subsumes(p1, p2) || Subsumes(p2, p1))
			{
				return true;
			}
			return false;
		}

		public static bool SameRestrictions(Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern p1, Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern p2)
		{
			PatternToken token1 = p1.token;
			PatternToken token2 = p2.token;
			if (token1.Equals(token2))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public virtual int CompareTo(Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern o)
		{
			int numthis = this.GetPreviousContextLen() + this.GetNextContextLen();
			int numthat = o.GetPreviousContextLen() + o.GetNextContextLen();
			if (numthis > numthat)
			{
				return -1;
			}
			else
			{
				if (numthis < numthat)
				{
					return 1;
				}
				else
				{
					return string.CompareOrdinal(this.ToString(), o.ToString());
				}
			}
		}

		public virtual int GetPreviousContextLen()
		{
			if (this.prevContext == null)
			{
				return 0;
			}
			else
			{
				return this.prevContext.Length;
			}
		}

		public virtual int GetNextContextLen()
		{
			if (this.nextContext == null)
			{
				return 0;
			}
			else
			{
				return this.nextContext.Length;
			}
		}

		public static bool SameLength(Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern p1, Edu.Stanford.Nlp.Patterns.Surface.SurfacePattern p2)
		{
			if (p1.GetPreviousContextLen() == p2.GetPreviousContextLen() && p1.GetNextContextLen() == p2.GetNextContextLen())
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public virtual void SetNumWordsCompound(int numWordsCompound)
		{
			token.numWordsCompound = numWordsCompound;
		}
		// public static SurfacePattern parse(String s) {
		// String[] t = s.split("##", -1);
		// String prev = t[0];
		// PatternToken tok = PatternToken.parse(t[1]);
		// String next = t[2];
		// return new SurfacePattern(prev, tok, next);
		// }
	}
}
