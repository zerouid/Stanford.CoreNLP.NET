using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;






namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Pattern for matching a complex data structure</summary>
	/// <author>Angel Chang</author>
	[System.Serializable]
	public class ComplexNodePattern<M, K> : NodePattern<M>
	{
		private readonly IList<Pair<K, NodePattern>> annotationPatterns;

		private readonly IBiFunction<M, K, object> getter;

		public ComplexNodePattern(IBiFunction<M, K, object> getter, IList<Pair<K, NodePattern>> annotationPatterns)
		{
			// TODO: Change/Augment from list of class to pattern to list of conditions for matching
			//       (so we can have more flexible matches)
			this.annotationPatterns = annotationPatterns;
			this.getter = getter;
		}

		public ComplexNodePattern(IBiFunction<M, K, object> getter, params Pair<K, NodePattern>[] annotationPatterns)
		{
			this.annotationPatterns = Arrays.AsList(annotationPatterns);
			this.getter = getter;
		}

		public ComplexNodePattern(IBiFunction<M, K, object> getter, K key, NodePattern pattern)
			: this(getter, Pair.MakePair(key, pattern))
		{
		}

		public virtual IList<Pair<K, NodePattern>> GetAnnotationPatterns()
		{
			return Java.Util.Collections.UnmodifiableList(annotationPatterns);
		}

		private static readonly Pattern LiteralPattern = Pattern.Compile("[^\\[\\]?.\\\\^$()*+{}|]*");

		// TODO: make this a pattern of non special characters: [,],?,.,\,^,$,(,),*,+,{,},| ... what else?
		//private static final Pattern LITERAL_PATTERN = Pattern.compile("[A-Za-z0-9_\\-']*");
		public static NodePattern<string> NewStringRegexPattern(string regex, int flags)
		{
			bool isLiteral = ((flags & Pattern.Literal) != 0) || LiteralPattern.Matcher(regex).Matches();
			if (isLiteral)
			{
				bool caseInsensitive = (flags & (Pattern.CaseInsensitive | Pattern.UnicodeCase)) != 0;
				int stringMatchFlags = (caseInsensitive) ? (CaseInsensitive | UnicodeCase) : 0;
				return new ComplexNodePattern.StringAnnotationPattern(regex, stringMatchFlags);
			}
			else
			{
				return new ComplexNodePattern.StringAnnotationRegexPattern(regex, flags);
			}
		}

		public static Edu.Stanford.Nlp.Ling.Tokensregex.ComplexNodePattern ValueOf<M, K>(Env env, IDictionary<string, string> attributes, IBiFunction<M, K, object> getter, IFunction<Pair<Env, string>, K> getKey)
		{
			Edu.Stanford.Nlp.Ling.Tokensregex.ComplexNodePattern<M, K> p = new Edu.Stanford.Nlp.Ling.Tokensregex.ComplexNodePattern<M, K>(getter, new List<Pair<K, NodePattern>>(attributes.Count));
			p.Populate(env, attributes, getKey);
			return p;
		}

		protected internal virtual void Populate(Env env, IDictionary<string, string> attributes, IFunction<Pair<Env, string>, K> getKey)
		{
			Edu.Stanford.Nlp.Ling.Tokensregex.ComplexNodePattern<M, K> p = this;
			foreach (string attr in attributes.Keys)
			{
				string value = attributes[attr];
				K c = getKey.Apply(Pair.MakePair(env, attr));
				if (c != null)
				{
					if (value.StartsWith("\"") && value.EndsWith("\""))
					{
						value = Sharpen.Runtime.Substring(value, 1, value.Length - 1);
						value = value.ReplaceAll("\\\\\"", "\"");
						// Unescape quotes...
						p.Add(c, new ComplexNodePattern.StringAnnotationPattern(value, env.defaultStringMatchFlags));
					}
					else
					{
						if (value.StartsWith("/") && value.EndsWith("/"))
						{
							value = Sharpen.Runtime.Substring(value, 1, value.Length - 1);
							value = value.ReplaceAll("\\\\/", "/");
							// Unescape forward slash
							string regex = (env != null) ? env.ExpandStringRegex(value) : value;
							int flags = (env != null) ? env.defaultStringPatternFlags : 0;
							p.Add(c, NewStringRegexPattern(regex, flags));
						}
						else
						{
							if (value.StartsWith("::"))
							{
								switch (value)
								{
									case "::IS_NIL":
									case "::NOT_EXISTS":
									{
										p.Add(c, new ComplexNodePattern.NilAnnotationPattern());
										break;
									}

									case "::EXISTS":
									case "::NOT_NIL":
									{
										p.Add(c, new ComplexNodePattern.NotNilAnnotationPattern());
										break;
									}

									case "::IS_NUM":
									{
										p.Add(c, new ComplexNodePattern.NumericAnnotationPattern(0, ComplexNodePattern.NumericAnnotationPattern.CmpType.IsNum));
										break;
									}

									default:
									{
										bool ok = false;
										if (env != null)
										{
											object custom = env.Get(value);
											if (custom != null)
											{
												p.Add(c, (NodePattern)custom);
												ok = true;
											}
										}
										if (!ok)
										{
											throw new ArgumentException("Invalid value " + value + " for key: " + attr);
										}
										break;
									}
								}
							}
							else
							{
								if (value.StartsWith("<="))
								{
									double v = double.ParseDouble(Sharpen.Runtime.Substring(value, 2));
									p.Add(c, new ComplexNodePattern.NumericAnnotationPattern(v, ComplexNodePattern.NumericAnnotationPattern.CmpType.Le));
								}
								else
								{
									if (value.StartsWith(">="))
									{
										double v = double.ParseDouble(Sharpen.Runtime.Substring(value, 2));
										p.Add(c, new ComplexNodePattern.NumericAnnotationPattern(v, ComplexNodePattern.NumericAnnotationPattern.CmpType.Ge));
									}
									else
									{
										if (value.StartsWith("=="))
										{
											double v = double.ParseDouble(Sharpen.Runtime.Substring(value, 2));
											p.Add(c, new ComplexNodePattern.NumericAnnotationPattern(v, ComplexNodePattern.NumericAnnotationPattern.CmpType.Eq));
										}
										else
										{
											if (value.StartsWith("!="))
											{
												double v = double.ParseDouble(Sharpen.Runtime.Substring(value, 2));
												p.Add(c, new ComplexNodePattern.NumericAnnotationPattern(v, ComplexNodePattern.NumericAnnotationPattern.CmpType.Ne));
											}
											else
											{
												if (value.StartsWith(">"))
												{
													double v = double.ParseDouble(Sharpen.Runtime.Substring(value, 1));
													p.Add(c, new ComplexNodePattern.NumericAnnotationPattern(v, ComplexNodePattern.NumericAnnotationPattern.CmpType.Gt));
												}
												else
												{
													if (value.StartsWith("<"))
													{
														double v = double.ParseDouble(Sharpen.Runtime.Substring(value, 1));
														p.Add(c, new ComplexNodePattern.NumericAnnotationPattern(v, ComplexNodePattern.NumericAnnotationPattern.CmpType.Lt));
													}
													else
													{
														if (value.Matches("[A-Za-z0-9_+-.]+"))
														{
															p.Add(c, new ComplexNodePattern.StringAnnotationPattern(value, env.defaultStringMatchFlags));
														}
														else
														{
															throw new ArgumentException("Invalid value " + value + " for key: " + attr);
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
				else
				{
					throw new ArgumentException("Unknown annotation key: " + attr);
				}
			}
		}

		public virtual void Add(K c, NodePattern pattern)
		{
			annotationPatterns.Add(Pair.MakePair(c, pattern));
		}

		public override bool Match(M token)
		{
			bool matched = true;
			foreach (Pair<K, NodePattern> entry in annotationPatterns)
			{
				NodePattern annoPattern = entry.second;
				object anno = getter.Apply(token, entry.first);
				if (!annoPattern.Match(anno))
				{
					matched = false;
					break;
				}
			}
			return matched;
		}

		public override object MatchWithResult(M token)
		{
			IDictionary<K, object> matchResults = new Dictionary<K, object>();
			//Generics.newHashMap();
			if (Match(token, matchResults))
			{
				return matchResults;
			}
			else
			{
				return null;
			}
		}

		// Does matching, returning match results
		protected internal virtual bool Match(M token, IDictionary<K, object> matchResults)
		{
			bool matched = true;
			foreach (Pair<K, NodePattern> entry in annotationPatterns)
			{
				NodePattern annoPattern = entry.second;
				object anno = getter.Apply(token, entry.first);
				object matchResult = annoPattern.MatchWithResult(anno);
				if (matchResult != null)
				{
					matchResults[entry.first] = matchResult;
				}
				else
				{
					matched = false;
					break;
				}
			}
			return matched;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (Pair<K, NodePattern> entry in annotationPatterns)
			{
				if (sb.Length > 0)
				{
					sb.Append(", ");
				}
				sb.Append(entry.first).Append(entry.second);
			}
			return sb.ToString();
		}

		[System.Serializable]
		public class NilAnnotationPattern : NodePattern<object>
		{
			public override bool Match(object obj)
			{
				return obj == null;
			}

			public override string ToString()
			{
				return "::IS_NIL";
			}
		}

		[System.Serializable]
		public class NotNilAnnotationPattern : NodePattern<object>
		{
			public override bool Match(object obj)
			{
				return obj != null;
			}

			public override string ToString()
			{
				return "::NOT_NIL";
			}
		}

		[System.Serializable]
		public class SequenceRegexPattern<T> : NodePattern<IList<T>>
		{
			internal SequencePattern<T> pattern;

			public SequenceRegexPattern(SequencePattern<T> pattern)
			{
				this.pattern = pattern;
			}

			public virtual SequencePattern<T> GetPattern()
			{
				return pattern;
			}

			public virtual SequenceMatcher<T> Matcher(IList<T> list)
			{
				return pattern.GetMatcher(list);
			}

			public override bool Match(IList<T> list)
			{
				return pattern.GetMatcher(list).Matches();
			}

			public override object MatchWithResult(IList<T> list)
			{
				SequenceMatcher<T> m = pattern.GetMatcher(list);
				if (m.Matches())
				{
					return m.ToBasicSequenceMatchResult();
				}
				else
				{
					return null;
				}
			}

			public override string ToString()
			{
				return ":" + pattern.ToString();
			}
		}

		[System.Serializable]
		public class StringAnnotationRegexPattern : NodePattern<string>
		{
			internal Pattern pattern;

			public StringAnnotationRegexPattern(Pattern pattern)
			{
				this.pattern = pattern;
			}

			public StringAnnotationRegexPattern(string regex, int flags)
			{
				this.pattern = Pattern.Compile(regex, flags);
			}

			public virtual Pattern GetPattern()
			{
				return pattern;
			}

			public virtual Java.Util.Regex.Matcher Matcher(string str)
			{
				return pattern.Matcher(str);
			}

			public override bool Match(string str)
			{
				if (str == null)
				{
					return false;
				}
				else
				{
					return pattern.Matcher(str).Matches();
				}
			}

			public override object MatchWithResult(string str)
			{
				if (str == null)
				{
					return null;
				}
				Java.Util.Regex.Matcher m = pattern.Matcher(str);
				if (m.Matches())
				{
					return m.ToMatchResult();
				}
				else
				{
					return null;
				}
			}

			public override string ToString()
			{
				return ":/" + pattern.Pattern() + "/";
			}
		}

		[System.Serializable]
		public abstract class AbstractStringAnnotationPattern : NodePattern<string>
		{
			internal int flags;

			public virtual bool IgnoreCase()
			{
				return (flags & (CaseInsensitive | UnicodeCase)) != 0;
			}

			public virtual bool Normalize()
			{
				return (flags & Normalize) != 0;
			}

			public virtual string GetNormalized(string str)
			{
				if (Normalize())
				{
					str = StringUtils.Normalize(str);
				}
				if (IgnoreCase())
				{
					str = str.ToLower();
				}
				return str;
			}
		}

		[System.Serializable]
		public class StringAnnotationPattern : ComplexNodePattern.AbstractStringAnnotationPattern
		{
			internal string target;

			public StringAnnotationPattern(string str, int flags)
			{
				this.target = str;
				this.flags = flags;
			}

			public StringAnnotationPattern(string str)
			{
				this.target = str;
			}

			public virtual string GetString()
			{
				return target;
			}

			public override bool Match(string str)
			{
				if (Normalize())
				{
					str = GetNormalized(str);
				}
				if (IgnoreCase())
				{
					return Sharpen.Runtime.EqualsIgnoreCase(target, str);
				}
				else
				{
					return target.Equals(str);
				}
			}

			public override string ToString()
			{
				return ":" + target;
			}
		}

		[System.Serializable]
		public class StringInSetAnnotationPattern : ComplexNodePattern.AbstractStringAnnotationPattern
		{
			internal ICollection<string> targets;

			public StringInSetAnnotationPattern(ICollection<string> targets, int flags)
			{
				this.flags = flags;
				// if ignoreCase/normalize is true - convert targets to lowercase/normalized
				this.targets = new HashSet<string>(targets.Count);
				foreach (string target in targets)
				{
					this.targets.Add(GetNormalized(target));
				}
			}

			public StringInSetAnnotationPattern(ICollection<string> targets)
				: this(targets, 0)
			{
			}

			public virtual ICollection<string> GetTargets()
			{
				return targets;
			}

			public override bool Match(string str)
			{
				return targets.Contains(GetNormalized(str));
			}

			public override string ToString()
			{
				return ":" + targets;
			}
		}

		[System.Serializable]
		public class NumericAnnotationPattern : NodePattern<object>
		{
			[System.Serializable]
			internal sealed class CmpType
			{
				public static readonly ComplexNodePattern.NumericAnnotationPattern.CmpType IsNum = new ComplexNodePattern.NumericAnnotationPattern.CmpType();

				public static readonly ComplexNodePattern.NumericAnnotationPattern.CmpType Eq = new ComplexNodePattern.NumericAnnotationPattern.CmpType();

				public static readonly ComplexNodePattern.NumericAnnotationPattern.CmpType Ne = new ComplexNodePattern.NumericAnnotationPattern.CmpType();

				public static readonly ComplexNodePattern.NumericAnnotationPattern.CmpType Gt = new ComplexNodePattern.NumericAnnotationPattern.CmpType();

				public static readonly ComplexNodePattern.NumericAnnotationPattern.CmpType Ge = new ComplexNodePattern.NumericAnnotationPattern.CmpType();

				public static readonly ComplexNodePattern.NumericAnnotationPattern.CmpType Lt = new ComplexNodePattern.NumericAnnotationPattern.CmpType();

				public static readonly ComplexNodePattern.NumericAnnotationPattern.CmpType Le = new ComplexNodePattern.NumericAnnotationPattern.CmpType();

				// TODO: equal with doubles is not so good
				// TODO: equal with doubles is not so good
				internal bool Accept(double v1, double v2)
				{
					return false;
				}
			}

			internal ComplexNodePattern.NumericAnnotationPattern.CmpType cmpType;

			internal double value;

			public NumericAnnotationPattern(double value, ComplexNodePattern.NumericAnnotationPattern.CmpType cmpType)
			{
				this.value = value;
				this.cmpType = cmpType;
			}

			public override bool Match(object node)
			{
				if (node is string)
				{
					return Match((string)node);
				}
				else
				{
					if (node is Number)
					{
						return Match((Number)node);
					}
					else
					{
						return false;
					}
				}
			}

			public virtual bool Match(Number number)
			{
				if (number != null)
				{
					return cmpType.Accept(number, value);
				}
				else
				{
					return false;
				}
			}

			public virtual bool Match(string str)
			{
				if (str != null)
				{
					try
					{
						double v = double.ParseDouble(str);
						return cmpType.Accept(v, value);
					}
					catch (NumberFormatException)
					{
					}
				}
				return false;
			}

			public override string ToString()
			{
				return " " + cmpType + " " + value;
			}
		}

		public class AttributesEqualMatchChecker<K> : SequencePattern.INodesMatchChecker<IDictionary<K, object>>
		{
			internal ICollection<K> keys;

			public AttributesEqualMatchChecker(params K[] keys)
			{
				this.keys = CollectionUtils.AsSet(keys);
			}

			public virtual bool Matches(IDictionary<K, object> o1, IDictionary<K, object> o2)
			{
				foreach (K key in keys)
				{
					object v1 = o1[key];
					object v2 = o2[key];
					if (v1 != null)
					{
						if (!v1.Equals(v2))
						{
							return false;
						}
					}
					else
					{
						if (v2 != null)
						{
							return false;
						}
					}
				}
				return true;
			}
		}

		[System.Serializable]
		public class IntegerAnnotationPattern : NodePattern<int>
		{
			internal int value;

			public IntegerAnnotationPattern(int v)
			{
				//For exact matching integers. Presumably faster than NumericAnnotationPattern
				//TODO : add this in the valueOf function of MapNodePattern
				this.value = v;
			}

			public override bool Match(int node)
			{
				return value == node;
			}

			public virtual int GetValue()
			{
				return value;
			}
		}
	}
}
