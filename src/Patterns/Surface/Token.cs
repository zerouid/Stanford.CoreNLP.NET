using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Patterns;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns.Surface
{
	/// <summary>Currently can handle only ORs.</summary>
	/// <author>sonalg</author>
	/// <version>10/16/14</version>
	[System.Serializable]
	public class Token
	{
		internal static IDictionary<Type, string> class2KeyMapping = new ConcurrentHashMap<Type, string>();

		internal IDictionary<Type, string> classORrestrictions;

		internal string envBindBooleanRestriction;

		private readonly Pattern alphaNumeric = Pattern.Compile("^[\\p{Alnum}\\s]+$");

		internal int numMinOcc = 1;

		internal int numMaxOcc = 1;

		internal PatternFactory.PatternType type;

		public Token(PatternFactory.PatternType type)
		{
			//Can be semgrex.Env but does not matter
			//static public Env env = TokenSequencePattern.getNewEnv();
			//All the restrictions of a token: for example, word:xyz
			//TODO: may be change this to map to true values?
			this.type = type;
		}

		public Token(Type c, string s, PatternFactory.PatternType type)
			: this(type)
		{
			AddORRestriction(c, s);
		}

		public virtual IDictionary<string, string> ClassORRestrictionsAsString()
		{
			if (classORrestrictions == null || classORrestrictions.IsEmpty())
			{
				return null;
			}
			IDictionary<string, string> str = new Dictionary<string, string>();
			foreach (KeyValuePair<Type, string> en in classORrestrictions)
			{
				str[class2KeyMapping[en.Key]] = en.Value;
			}
			return str;
		}

		public override string ToString()
		{
			if (type.Equals(PatternFactory.PatternType.Surface))
			{
				return ToStringSurface();
			}
			else
			{
				if (type.Equals(PatternFactory.PatternType.Dep))
				{
					return ToStringDep();
				}
				else
				{
					throw new NotSupportedException();
				}
			}
		}

		private string ToStringDep()
		{
			string str = string.Empty;
			if (classORrestrictions != null && !this.classORrestrictions.IsEmpty())
			{
				foreach (KeyValuePair<Type, string> en in this.classORrestrictions)
				{
					string orgVal = en.Value.ToString();
					string val;
					if (!alphaNumeric.Matcher(orgVal).Matches())
					{
						val = "/" + Pattern.Quote(orgVal.ReplaceAll("/", "\\\\/")) + "/";
					}
					else
					{
						val = orgVal;
					}
					if (str.IsEmpty())
					{
						str = "{" + class2KeyMapping[en.Key] + ":" + val + "}";
					}
					else
					{
						str += " | " + "{" + class2KeyMapping[en.Key] + ":" + val + "}";
					}
				}
			}
			return str.Trim();
		}

		private string ToStringSurface()
		{
			string str = string.Empty;
			if (classORrestrictions != null && !this.classORrestrictions.IsEmpty())
			{
				foreach (KeyValuePair<Type, string> en in this.classORrestrictions)
				{
					string orgVal = en.Value.ToString();
					string val;
					if (!alphaNumeric.Matcher(orgVal).Matches())
					{
						val = "/" + Pattern.Quote(orgVal.ReplaceAll("/", "\\\\/")) + "/";
					}
					else
					{
						val = "\"" + orgVal + "\"";
					}
					if (str.IsEmpty())
					{
						str = "{" + class2KeyMapping[en.Key] + ":" + val + "}";
					}
					else
					{
						str += " | " + "{" + class2KeyMapping[en.Key] + ":" + val + "}";
					}
				}
				str = "[" + str + "]";
			}
			else
			{
				if (envBindBooleanRestriction != null && !envBindBooleanRestriction.IsEmpty())
				{
					str = envBindBooleanRestriction;
				}
			}
			if (numMinOcc != 1 || numMaxOcc != 1)
			{
				str += "{" + numMinOcc + "," + numMaxOcc + "}";
			}
			return str.Trim();
		}

		public virtual string GetSimple()
		{
			string str = string.Empty;
			if (classORrestrictions != null && !this.classORrestrictions.IsEmpty())
			{
				foreach (KeyValuePair<Type, string> en in this.classORrestrictions)
				{
					if (str.IsEmpty())
					{
						str = en.Value.ToString();
					}
					else
					{
						str += "|" + en.Value.ToString();
					}
				}
			}
			else
			{
				if (envBindBooleanRestriction != null && !envBindBooleanRestriction.IsEmpty())
				{
					if (envBindBooleanRestriction.StartsWith("$FILLER"))
					{
						str = "FW";
					}
					else
					{
						if (envBindBooleanRestriction.StartsWith("$STOP"))
						{
							str = "SW";
						}
					}
				}
			}
			return str.Trim();
		}

		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		public override bool Equals(object o)
		{
			if (!(o is Edu.Stanford.Nlp.Patterns.Surface.Token))
			{
				return false;
			}
			return o.ToString().Equals(this.ToString());
		}

		public virtual void AddORRestriction(Type classR, string value)
		{
			GetKeyForClass(classR);
			if (this.envBindBooleanRestriction != null && !this.envBindBooleanRestriction.IsEmpty())
			{
				throw new Exception("cannot add restriction to something that is binding to an env variable");
			}
			if (classORrestrictions == null)
			{
				classORrestrictions = new SortedDictionary<Type, string>(new Token.ClassComparator(this));
			}
			System.Diagnostics.Debug.Assert(value != null);
			classORrestrictions[classR] = value;
		}

		public virtual void SetEnvBindRestriction(string envBind)
		{
			if (this.classORrestrictions != null && !this.classORrestrictions.IsEmpty())
			{
				throw new Exception("cannot add env bind restriction to something that has restricted");
			}
			this.envBindBooleanRestriction = envBind;
		}

		public virtual void SetNumOcc(int min, int max)
		{
			numMinOcc = min;
			numMaxOcc = max;
		}

		public virtual bool IsEmpty()
		{
			return (this.envBindBooleanRestriction == null || this.envBindBooleanRestriction.IsEmpty()) && (this.classORrestrictions == null || this.classORrestrictions.IsEmpty());
		}

		public static string GetKeyForClass(Type classR)
		{
			string key = class2KeyMapping[classR];
			if (key == null)
			{
				foreach (KeyValuePair<string, object> vars in ConstantsAndVariables.globalEnv.GetVariables())
				{
					if (vars.Value.Equals(classR))
					{
						key = vars.Key.ToLower();
						class2KeyMapping[classR] = key;
						break;
					}
				}
			}
			if (key == null)
			{
				key = classR.GetSimpleName().ToLower();
				class2KeyMapping[classR] = key;
				ConstantsAndVariables.globalEnv.Bind(key, classR);
			}
			return key;
		}

		[System.Serializable]
		public class ClassComparator : IComparator<Type>
		{
			public virtual int Compare(Type o1, Type o2)
			{
				return string.CompareOrdinal(o1.ToString(), o2.ToString());
			}

			internal ClassComparator(Token _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly Token _enclosing;
		}

		public static string ToStringClass2KeyMapping()
		{
			StringBuilder str = new StringBuilder();
			foreach (KeyValuePair<Type, string> en in class2KeyMapping)
			{
				if (str.Length > 0)
				{
					str.Append("\n");
				}
				str.Append(en.Key.FullName + "###" + en.Value);
			}
			return str.ToString();
		}

		/// <exception cref="System.TypeLoadException"/>
		public static void SetClass2KeyMapping(File file)
		{
			foreach (string line in IOUtils.ReadLines(file))
			{
				string[] toks = line.Split("###");
				class2KeyMapping[Sharpen.Runtime.GetType(toks[0])] = toks[1];
			}
		}
	}
}
