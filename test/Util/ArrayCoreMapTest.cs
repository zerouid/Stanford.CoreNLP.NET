using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Test various operations of the ArrayCoreMap: equals, toString, etc.</summary>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ArrayCoreMapTest
	{
		[NUnit.Framework.Test]
		public virtual void TestCreate()
		{
			ArrayCoreMap foo = new ArrayCoreMap();
			NUnit.Framework.Assert.AreEqual(0, foo.Size());
		}

		[NUnit.Framework.Test]
		public virtual void TestGetAndSet()
		{
			ArrayCoreMap foo = new ArrayCoreMap();
			NUnit.Framework.Assert.AreEqual(0, foo.Size());
			foo.Set(typeof(CoreAnnotations.TextAnnotation), "foo");
			NUnit.Framework.Assert.AreEqual("foo", foo.Get(typeof(CoreAnnotations.TextAnnotation)));
			NUnit.Framework.Assert.AreEqual(null, foo.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)));
			NUnit.Framework.Assert.AreEqual(null, foo.Get(typeof(CoreAnnotations.ParagraphsAnnotation)));
			NUnit.Framework.Assert.AreEqual(1, foo.Size());
			foo.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "F");
			NUnit.Framework.Assert.AreEqual("foo", foo.Get(typeof(CoreAnnotations.TextAnnotation)));
			NUnit.Framework.Assert.AreEqual("F", foo.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)));
			NUnit.Framework.Assert.AreEqual(null, foo.Get(typeof(CoreAnnotations.ParagraphsAnnotation)));
			NUnit.Framework.Assert.AreEqual(2, foo.Size());
			IList<ICoreMap> paragraphs = new List<ICoreMap>();
			ArrayCoreMap f1 = new ArrayCoreMap();
			f1.Set(typeof(CoreAnnotations.TextAnnotation), "f");
			paragraphs.Add(f1);
			ArrayCoreMap f2 = new ArrayCoreMap();
			f2.Set(typeof(CoreAnnotations.TextAnnotation), "o");
			paragraphs.Add(f2);
			foo.Set(typeof(CoreAnnotations.ParagraphsAnnotation), paragraphs);
			NUnit.Framework.Assert.AreEqual("foo", foo.Get(typeof(CoreAnnotations.TextAnnotation)));
			NUnit.Framework.Assert.AreEqual("F", foo.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)));
			// will test equality of the coremaps in another test
			NUnit.Framework.Assert.AreEqual(3, foo.Size());
		}

		[NUnit.Framework.Test]
		public virtual void TestSimpleEquals()
		{
			ArrayCoreMap foo = new ArrayCoreMap();
			IList<ICoreMap> paragraphs = new List<ICoreMap>();
			ArrayCoreMap f1 = new ArrayCoreMap();
			f1.Set(typeof(CoreAnnotations.TextAnnotation), "f");
			paragraphs.Add(f1);
			ArrayCoreMap f2 = new ArrayCoreMap();
			f2.Set(typeof(CoreAnnotations.TextAnnotation), "o");
			paragraphs.Add(f2);
			foo.Set(typeof(CoreAnnotations.ParagraphsAnnotation), paragraphs);
			ArrayCoreMap bar = new ArrayCoreMap();
			bar.Set(typeof(CoreAnnotations.ParagraphsAnnotation), paragraphs);
			NUnit.Framework.Assert.AreEqual(foo, bar);
			NUnit.Framework.Assert.AreEqual(bar, foo);
			NUnit.Framework.Assert.IsFalse(foo.Equals(f1));
			NUnit.Framework.Assert.IsFalse(foo.Equals(f2));
			NUnit.Framework.Assert.AreEqual(f1, f1);
			NUnit.Framework.Assert.IsFalse(f1.Equals(f2));
		}

		/// <summary>Test that neither hashCode() nor toString() hang</summary>
		[NUnit.Framework.Test]
		public virtual void TestKeySet()
		{
			ArrayCoreMap foo = new ArrayCoreMap();
			foo.Set(typeof(CoreAnnotations.TextAnnotation), "foo");
			foo.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "NN");
			foo.Set(typeof(CoreAnnotations.DocIDAnnotation), null);
			NUnit.Framework.Assert.IsTrue(foo.KeySet().Contains(typeof(CoreAnnotations.TextAnnotation)));
			NUnit.Framework.Assert.IsTrue(foo.KeySet().Contains(typeof(CoreAnnotations.PartOfSpeechAnnotation)));
			NUnit.Framework.Assert.IsTrue(foo.KeySet().Contains(typeof(CoreAnnotations.DocIDAnnotation)));
			NUnit.Framework.Assert.IsFalse(foo.KeySet().Contains(typeof(CoreAnnotations.TokensAnnotation)));
		}

		/// <summary>Test that neither hashCode() nor toString() hang</summary>
		[NUnit.Framework.Test]
		public virtual void TestNoHanging()
		{
			ArrayCoreMap foo = new ArrayCoreMap();
			IList<ICoreMap> paragraphs = new List<ICoreMap>();
			ArrayCoreMap f1 = new ArrayCoreMap();
			f1.Set(typeof(CoreAnnotations.TextAnnotation), "f");
			paragraphs.Add(f1);
			ArrayCoreMap f2 = new ArrayCoreMap();
			f2.Set(typeof(CoreAnnotations.TextAnnotation), "o");
			paragraphs.Add(f2);
			foo.Set(typeof(CoreAnnotations.ParagraphsAnnotation), paragraphs);
			foo.ToString();
			foo.GetHashCode();
		}

		[NUnit.Framework.Test]
		public virtual void TestRemove()
		{
			ArrayCoreMap foo = new ArrayCoreMap();
			foo.Set(typeof(CoreAnnotations.TextAnnotation), "foo");
			foo.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "F");
			NUnit.Framework.Assert.AreEqual("foo", foo.Get(typeof(CoreAnnotations.TextAnnotation)));
			NUnit.Framework.Assert.AreEqual("F", foo.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)));
			NUnit.Framework.Assert.AreEqual(2, foo.Size());
			foo.Remove(typeof(CoreAnnotations.TextAnnotation));
			NUnit.Framework.Assert.AreEqual(1, foo.Size());
			NUnit.Framework.Assert.AreEqual(null, foo.Get(typeof(CoreAnnotations.TextAnnotation)));
			NUnit.Framework.Assert.AreEqual("F", foo.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)));
			foo.Set(typeof(CoreAnnotations.TextAnnotation), "bar");
			NUnit.Framework.Assert.AreEqual("bar", foo.Get(typeof(CoreAnnotations.TextAnnotation)));
			NUnit.Framework.Assert.AreEqual("F", foo.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)));
			NUnit.Framework.Assert.AreEqual(2, foo.Size());
			foo.Remove(typeof(CoreAnnotations.TextAnnotation));
			NUnit.Framework.Assert.AreEqual(1, foo.Size());
			NUnit.Framework.Assert.AreEqual(null, foo.Get(typeof(CoreAnnotations.TextAnnotation)));
			NUnit.Framework.Assert.AreEqual("F", foo.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)));
			foo.Remove(typeof(CoreAnnotations.PartOfSpeechAnnotation));
			NUnit.Framework.Assert.AreEqual(0, foo.Size());
			NUnit.Framework.Assert.AreEqual(null, foo.Get(typeof(CoreAnnotations.TextAnnotation)));
			NUnit.Framework.Assert.AreEqual(null, foo.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)));
			// Removing an element that doesn't exist
			// shouldn't blow up on us in any way
			foo.Remove(typeof(CoreAnnotations.PartOfSpeechAnnotation));
			NUnit.Framework.Assert.AreEqual(0, foo.Size());
			NUnit.Framework.Assert.AreEqual(null, foo.Get(typeof(CoreAnnotations.TextAnnotation)));
			NUnit.Framework.Assert.AreEqual(null, foo.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)));
			// after removing all sorts of stuff, the original ArrayCoreMap
			// should now be equal to a new empty one
			ArrayCoreMap bar = new ArrayCoreMap();
			NUnit.Framework.Assert.AreEqual(foo, bar);
			foo.Set(typeof(CoreAnnotations.TextAnnotation), "foo");
			foo.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "F");
			bar.Set(typeof(CoreAnnotations.TextAnnotation), "foo");
			NUnit.Framework.Assert.IsFalse(foo.Equals(bar));
			foo.Remove(typeof(CoreAnnotations.PartOfSpeechAnnotation));
			NUnit.Framework.Assert.AreEqual(foo, bar);
			NUnit.Framework.Assert.AreEqual(1, foo.Size());
			foo.Remove(typeof(CoreAnnotations.PartOfSpeechAnnotation));
			NUnit.Framework.Assert.AreEqual(1, foo.Size());
			NUnit.Framework.Assert.AreEqual("foo", foo.Get(typeof(CoreAnnotations.TextAnnotation)));
			NUnit.Framework.Assert.AreEqual(null, foo.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)));
		}

		[NUnit.Framework.Test]
		public virtual void TestToShortString()
		{
			ArrayCoreMap foo = new ArrayCoreMap();
			foo.Set(typeof(CoreAnnotations.TextAnnotation), "word");
			foo.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "NN");
			NUnit.Framework.Assert.AreEqual("word/NN", foo.ToShortString("Text", "PartOfSpeech"));
			NUnit.Framework.Assert.AreEqual("NN", foo.ToShortString("PartOfSpeech"));
			NUnit.Framework.Assert.AreEqual(string.Empty, foo.ToShortString("Lemma"));
			NUnit.Framework.Assert.AreEqual("word|NN", foo.ToShortString('|', "Text", "PartOfSpeech", "Lemma"));
			foo.Set(typeof(CoreAnnotations.AntecedentAnnotation), "the price of tea");
			NUnit.Framework.Assert.AreEqual("{word/NN/the price of tea}", foo.ToShortString("Text", "PartOfSpeech", "Antecedent"));
		}

		/// <summary>
		/// Tests equals in the case of different annotations added in
		/// different orders
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestEqualsReversedInsertOrder()
		{
			ArrayCoreMap foo = new ArrayCoreMap();
			IList<ICoreMap> paragraphs = new List<ICoreMap>();
			ArrayCoreMap f1 = new ArrayCoreMap();
			f1.Set(typeof(CoreAnnotations.TextAnnotation), "f");
			paragraphs.Add(f1);
			ArrayCoreMap f2 = new ArrayCoreMap();
			f2.Set(typeof(CoreAnnotations.TextAnnotation), "o");
			paragraphs.Add(f2);
			foo.Set(typeof(CoreAnnotations.ParagraphsAnnotation), paragraphs);
			foo.Set(typeof(CoreAnnotations.TextAnnotation), "A");
			foo.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "B");
			ArrayCoreMap bar = new ArrayCoreMap();
			IList<ICoreMap> paragraphs2 = new List<ICoreMap>(paragraphs);
			bar.Set(typeof(CoreAnnotations.TextAnnotation), "A");
			bar.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "B");
			bar.Set(typeof(CoreAnnotations.ParagraphsAnnotation), paragraphs2);
			NUnit.Framework.Assert.AreEqual(foo, bar);
			NUnit.Framework.Assert.AreEqual(bar, foo);
			NUnit.Framework.Assert.IsFalse(foo.Equals(f1));
			NUnit.Framework.Assert.IsFalse(foo.Equals(f2));
			NUnit.Framework.Assert.AreEqual(3, foo.Size());
		}

		/// <summary>
		/// ArrayCoreMap should be able to handle loops in its annotations
		/// without blowing up
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestObjectLoops()
		{
			ArrayCoreMap foo = new ArrayCoreMap();
			foo.Set(typeof(CoreAnnotations.TextAnnotation), "foo");
			foo.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "B");
			IList<ICoreMap> fooParagraph = new List<ICoreMap>();
			fooParagraph.Add(foo);
			ArrayCoreMap f1 = new ArrayCoreMap();
			f1.Set(typeof(CoreAnnotations.ParagraphsAnnotation), fooParagraph);
			IList<ICoreMap> p1 = new List<ICoreMap>();
			p1.Add(f1);
			foo.Set(typeof(CoreAnnotations.ParagraphsAnnotation), p1);
			foo.ToString();
			foo.GetHashCode();
		}

		[NUnit.Framework.Test]
		public virtual void TestObjectLoopEquals()
		{
			ArrayCoreMap foo = new ArrayCoreMap();
			foo.Set(typeof(CoreAnnotations.TextAnnotation), "foo");
			foo.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "B");
			IList<ICoreMap> fooParagraph = new List<ICoreMap>();
			fooParagraph.Add(foo);
			ArrayCoreMap f1 = new ArrayCoreMap();
			f1.Set(typeof(CoreAnnotations.ParagraphsAnnotation), fooParagraph);
			IList<ICoreMap> p1 = new List<ICoreMap>();
			p1.Add(f1);
			foo.Set(typeof(CoreAnnotations.ParagraphsAnnotation), p1);
			foo.ToString();
			int fh = foo.GetHashCode();
			ArrayCoreMap bar = new ArrayCoreMap();
			bar.Set(typeof(CoreAnnotations.TextAnnotation), "foo");
			bar.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "B");
			IList<ICoreMap> barParagraph = new List<ICoreMap>();
			barParagraph.Add(bar);
			ArrayCoreMap f2 = new ArrayCoreMap();
			f2.Set(typeof(CoreAnnotations.ParagraphsAnnotation), barParagraph);
			IList<ICoreMap> p2 = new List<ICoreMap>();
			p2.Add(f2);
			bar.Set(typeof(CoreAnnotations.ParagraphsAnnotation), p2);
			bar.ToString();
			int bh = bar.GetHashCode();
			NUnit.Framework.Assert.AreEqual(foo, bar);
			NUnit.Framework.Assert.AreEqual(bar, foo);
			NUnit.Framework.Assert.AreEqual(fh, bh);
			ArrayCoreMap baz = new ArrayCoreMap();
			baz.Set(typeof(CoreAnnotations.TextAnnotation), "foo");
			baz.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "B");
			IList<ICoreMap> foobarParagraph = new List<ICoreMap>();
			foobarParagraph.Add(foo);
			foobarParagraph.Add(bar);
			ArrayCoreMap f3 = new ArrayCoreMap();
			f3.Set(typeof(CoreAnnotations.ParagraphsAnnotation), foobarParagraph);
			IList<ICoreMap> p3 = new List<ICoreMap>();
			p3.Add(f3);
			baz.Set(typeof(CoreAnnotations.ParagraphsAnnotation), p3);
			NUnit.Framework.Assert.IsFalse(foo.Equals(baz));
			NUnit.Framework.Assert.IsFalse(baz.Equals(foo));
			ArrayCoreMap biff = new ArrayCoreMap();
			biff.Set(typeof(CoreAnnotations.TextAnnotation), "foo");
			biff.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "B");
			IList<ICoreMap> barfooParagraph = new List<ICoreMap>();
			barfooParagraph.Add(foo);
			barfooParagraph.Add(bar);
			ArrayCoreMap f4 = new ArrayCoreMap();
			f4.Set(typeof(CoreAnnotations.ParagraphsAnnotation), barfooParagraph);
			IList<ICoreMap> p4 = new List<ICoreMap>();
			p4.Add(f4);
			biff.Set(typeof(CoreAnnotations.ParagraphsAnnotation), p4);
			NUnit.Framework.Assert.AreEqual(baz, biff);
			barfooParagraph.Clear();
			NUnit.Framework.Assert.IsFalse(baz.Equals(biff));
			barfooParagraph.Add(foo);
			NUnit.Framework.Assert.IsFalse(baz.Equals(biff));
			barfooParagraph.Add(baz);
			NUnit.Framework.Assert.IsFalse(baz.Equals(biff));
			barfooParagraph.Clear();
			NUnit.Framework.Assert.IsFalse(baz.Equals(biff));
			barfooParagraph.Add(foo);
			barfooParagraph.Add(bar);
			NUnit.Framework.Assert.AreEqual(baz, biff);
		}

		[NUnit.Framework.Test]
		public virtual void TestCoreLabelSetWordBehavior()
		{
			CoreLabel foo = new CoreLabel();
			foo.Set(typeof(CoreAnnotations.TextAnnotation), "foo");
			foo.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "B");
			foo.Set(typeof(CoreAnnotations.LemmaAnnotation), "fool");
			// Lemma gets removed with word
			ArrayCoreMap copy = new ArrayCoreMap(foo);
			NUnit.Framework.Assert.AreEqual(copy, foo);
			foo.SetWord("foo");
			NUnit.Framework.Assert.AreEqual(copy, foo);
			// same word set
			foo.SetWord("bar");
			NUnit.Framework.Assert.IsFalse(copy.Equals(foo));
			// lemma removed
			foo.SetWord("foo");
			NUnit.Framework.Assert.IsFalse(copy.Equals(foo));
			// still removed
			foo.Set(typeof(CoreAnnotations.LemmaAnnotation), "fool");
			NUnit.Framework.Assert.AreEqual(copy, foo);
			// back to normal
			// Hash code is consistent
			int hashCode = foo.GetHashCode();
			NUnit.Framework.Assert.AreEqual(copy.GetHashCode(), hashCode);
			foo.SetWord("bar");
			NUnit.Framework.Assert.IsFalse(hashCode == foo.GetHashCode());
			foo.SetWord("foo");
			NUnit.Framework.Assert.IsFalse(hashCode == foo.GetHashCode());
			// Hash code doesn't care between a value of null and the key not existing
			NUnit.Framework.Assert.IsTrue(foo.Lemma() == null);
			int lemmalessHashCode = foo.GetHashCode();
			foo.Remove(typeof(CoreAnnotations.LemmaAnnotation));
			NUnit.Framework.Assert.AreEqual(lemmalessHashCode, foo.GetHashCode());
			foo.SetLemma(null);
			NUnit.Framework.Assert.AreEqual(lemmalessHashCode, foo.GetHashCode());
			foo.SetLemma("fool");
			NUnit.Framework.Assert.AreEqual(hashCode, foo.GetHashCode());
			// Check equals
			foo.SetWord("bar");
			foo.SetWord("foo");
			ArrayCoreMap nulledCopy = new ArrayCoreMap(foo);
			NUnit.Framework.Assert.AreEqual(nulledCopy, foo);
			foo.Remove(typeof(CoreAnnotations.LemmaAnnotation));
			NUnit.Framework.Assert.AreEqual(nulledCopy, foo);
		}

		[NUnit.Framework.Test]
		public virtual void TestCopyConstructor()
		{
			ArrayCoreMap biff = new ArrayCoreMap();
			biff.Set(typeof(CoreAnnotations.TextAnnotation), "foo");
			biff.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "B");
			biff.Set(typeof(CoreAnnotations.LemmaAnnotation), "fozzle");
			ArrayCoreMap boff = new ArrayCoreMap(biff);
			NUnit.Framework.Assert.AreEqual(3, boff.Size());
			NUnit.Framework.Assert.AreEqual(biff, boff);
			NUnit.Framework.Assert.AreEqual("fozzle", boff.Get(typeof(CoreAnnotations.LemmaAnnotation)));
		}
	}
}
