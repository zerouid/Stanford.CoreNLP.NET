using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util.Concurrent;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns.Surface
{
	[NUnit.Framework.TestFixture]
	public class SurfacePatternTest
	{
		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
		}

		[Test]
		public virtual void TestSubsumesArray()
		{
			string[] arr1 = new string[] { ",", "line", ",", "on" };
			string[] arr2 = new string[] { ",", "line", "," };
			NUnit.Framework.Assert.IsTrue(SurfacePattern.SubsumesArray(arr1, arr2));
			NUnit.Framework.Assert.IsFalse(SurfacePattern.SubsumesArray(arr2, null));
		}

		internal virtual Token[] CreateContext(IDictionary<Type, string> res)
		{
			Token[] toks = new Token[res.Count];
			int i = 0;
			foreach (KeyValuePair<Type, string> en in res)
			{
				Token t = new Token(PatternFactory.PatternType.Surface);
				t.AddORRestriction(en.Key, en.Value);
				toks[i] = t;
				i++;
			}
			return toks;
		}

		[Test]
		public virtual void TestSimplerTokens()
		{
			IDictionary<Type, string> prev = new _Dictionary_44();
			IDictionary<Type, string> next = new _Dictionary_49();
			PatternToken token = new PatternToken("V", false, true, 2, null, false, false, null);
			SurfacePattern p = new SurfacePattern(CreateContext(prev), token, CreateContext(next), SurfacePatternFactory.Genre.Prevnext);
			IDictionary<Type, string> prev2 = new _Dictionary_58();
			IDictionary<Type, string> next2 = new _Dictionary_63();
			PatternToken token2 = new PatternToken("V", false, true, 2, null, false, false, null);
			SurfacePattern p2 = new SurfacePattern(CreateContext(prev2), token2, CreateContext(next2), SurfacePatternFactory.Genre.Prevnext);
			System.Diagnostics.Debug.Assert(p.CompareTo(p2) == 0);
			ICounter<SurfacePattern> pats = new ClassicCounter<SurfacePattern>();
			pats.SetCount(p, 1);
			pats.SetCount(p2, 1);
			System.Diagnostics.Debug.Assert(pats.Size() == 1);
			System.Console.Out.WriteLine("pats size is " + pats.Size());
			ConcurrentHashIndex<SurfacePattern> index = new ConcurrentHashIndex<SurfacePattern>();
			index.Add(p);
			index.Add(p2);
			System.Diagnostics.Debug.Assert(index.Count == 1);
		}

		private sealed class _Dictionary_44 : Dictionary<Type, string>
		{
			public _Dictionary_44()
			{
				{
					this[typeof(CoreAnnotations.LemmaAnnotation)] = "name";
					this[typeof(CoreAnnotations.LemmaAnnotation)] = "is";
				}
			}
		}

		private sealed class _Dictionary_49 : Dictionary<Type, string>
		{
			public _Dictionary_49()
			{
				{
					this[typeof(CoreAnnotations.LemmaAnnotation)] = "Duck";
				}
			}
		}

		private sealed class _Dictionary_58 : Dictionary<Type, string>
		{
			public _Dictionary_58()
			{
				{
					this[typeof(CoreAnnotations.LemmaAnnotation)] = "name";
					this[typeof(CoreAnnotations.LemmaAnnotation)] = "is";
				}
			}
		}

		private sealed class _Dictionary_63 : Dictionary<Type, string>
		{
			public _Dictionary_63()
			{
				{
					this[typeof(CoreAnnotations.LemmaAnnotation)] = "Duck";
				}
			}
		}
		//String[] sim = p.getSimplerTokensPrev();
		//System.out.println(Arrays.toString(sim));
	}
}
