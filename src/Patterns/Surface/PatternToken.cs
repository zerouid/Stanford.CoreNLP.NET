using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Patterns.Surface
{
	/// <summary>Class to represent a target phrase.</summary>
	/// <remarks>
	/// Class to represent a target phrase. Note that you can give additional negative constraints
	/// in getTokenStr(List) but those are not used by toString, hashCode and equals functions
	/// Author: Sonal Gupta (sonalg@stanford.edu)
	/// </remarks>
	[System.Serializable]
	public class PatternToken
	{
		private const long serialVersionUID = 1L;

		internal string tag;

		internal bool useTag;

		internal int numWordsCompound;

		internal bool useNER = false;

		internal string nerTag = null;

		internal bool useTargetParserParentRestriction = false;

		internal string grandparentParseTag;

		public PatternToken(string tag, bool useTag, bool getCompoundPhrases, int numWordsCompound, string nerTag, bool useNER, bool useTargetParserParentRestriction, string grandparentParseTag)
		{
			if (useNER && nerTag == null)
			{
				throw new Exception("NER tag is null and using NER restriction is true. Check your data.");
			}
			this.tag = tag;
			this.useTag = useTag;
			this.numWordsCompound = numWordsCompound;
			if (!getCompoundPhrases)
			{
				this.numWordsCompound = 1;
			}
			this.nerTag = nerTag;
			this.useNER = useNER;
			this.useTargetParserParentRestriction = useTargetParserParentRestriction;
			if (useTargetParserParentRestriction)
			{
				if (grandparentParseTag == null)
				{
					Redwood.Log(ConstantsAndVariables.extremedebug, "Grand parent parse tag null ");
					this.grandparentParseTag = "null";
				}
				else
				{
					this.grandparentParseTag = grandparentParseTag;
				}
			}
		}

		// static public PatternToken parse(String str) {
		// String[] t = str.split("#");
		// String tag = t[0];
		// boolean usetag = Boolean.parseBoolean(t[1]);
		// int num = Integer.parseInt(t[2]);
		// boolean useNER = false;
		// String ner = "";
		// if(t.length > 3){
		// useNER = true;
		// ner = t[4];
		// }
		//
		// return new PatternToken(tag, usetag, true, num, ner, useNER);
		// }
		public virtual string ToStringToWrite()
		{
			string s = "X";
			if (useTag)
			{
				s += ":" + tag;
			}
			if (useNER)
			{
				s += ":" + nerTag;
			}
			if (useTargetParserParentRestriction)
			{
				s += ":" + grandparentParseTag;
			}
			// if(notAllowedClasses !=null && notAllowedClasses.size() > 0){
			// s+= ":!(";
			// s+= StringUtils.join(notAllowedClasses,"|")+")";
			// }
			if (numWordsCompound > 1)
			{
				s += "{" + numWordsCompound + "}";
			}
			return s;
		}

		internal virtual string GetTokenStr(IList<string> notAllowedClasses)
		{
			string str = " (?$term ";
			IList<string> restrictions = new List<string>();
			if (useTag)
			{
				restrictions.Add("{tag:/" + tag + ".*/}");
			}
			if (useNER)
			{
				restrictions.Add("{ner:" + nerTag + "}");
			}
			if (useTargetParserParentRestriction)
			{
				restrictions.Add("{grandparentparsetag:\"" + grandparentParseTag + "\"}");
			}
			if (notAllowedClasses != null && notAllowedClasses.Count > 0)
			{
				foreach (string na in notAllowedClasses)
				{
					restrictions.Add("!{" + na + ":" + na + "}");
				}
			}
			str += "[" + StringUtils.Join(restrictions, " & ") + "]{1," + numWordsCompound + "}";
			str += ")";
			str = StringUtils.ToAscii(str);
			return str;
		}

		public override bool Equals(object b)
		{
			if (!(b is Edu.Stanford.Nlp.Patterns.Surface.PatternToken))
			{
				return false;
			}
			Edu.Stanford.Nlp.Patterns.Surface.PatternToken t = (Edu.Stanford.Nlp.Patterns.Surface.PatternToken)b;
			if (this.useNER != t.useNER || this.useTag != t.useTag || this.useTargetParserParentRestriction != t.useTargetParserParentRestriction || this.numWordsCompound != t.numWordsCompound)
			{
				return false;
			}
			if (useTag && !this.tag.Equals(t.tag))
			{
				return false;
			}
			if (useNER && !this.nerTag.Equals(t.nerTag))
			{
				return false;
			}
			if (useTargetParserParentRestriction && !this.grandparentParseTag.Equals(t.grandparentParseTag))
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			return GetTokenStr(null).GetHashCode();
		}

		public virtual Edu.Stanford.Nlp.Patterns.Surface.PatternToken Copy()
		{
			Edu.Stanford.Nlp.Patterns.Surface.PatternToken t = new Edu.Stanford.Nlp.Patterns.Surface.PatternToken(tag, useTag, numWordsCompound > 1, numWordsCompound, nerTag, useNER, useTargetParserParentRestriction, grandparentParseTag);
			return t;
		}
	}
}
