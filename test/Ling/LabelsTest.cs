using System;


namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// Tests the behavior of things implementing the Label interface and the
	/// traditional behavior of things now in the ValueLabel hierarchy.
	/// </summary>
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class LabelsTest
	{
		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
		}

		private static void InternalValidation(string type, ILabel lab, string val)
		{
			NUnit.Framework.Assert.AreEqual(type + " does not have value it was constructed with", lab.Value(), val);
			string newVal = "feijoa";
			lab.SetValue(newVal);
			NUnit.Framework.Assert.AreEqual(type + " does not have value set with setValue", newVal, lab.Value());
			// restore value
			lab.SetValue(val);
			string @out = lab.ToString();
			ILabel lab3 = lab.LabelFactory().NewLabel(val);
			NUnit.Framework.Assert.AreEqual(type + " made by label factory has diferent value", lab.Value(), lab3.Value());
			lab3 = lab.LabelFactory().NewLabel(lab);
			NUnit.Framework.Assert.AreEqual(type + " made from label factory is not equal", lab, lab3);
			try
			{
				ILabel lab2 = lab.LabelFactory().NewLabelFromString(@out);
				NUnit.Framework.Assert.AreEqual(type + " factory fromString and toString are not inverses", lab, lab2);
				lab3.SetFromString(@out);
				NUnit.Framework.Assert.AreEqual(type + " setFromString and toString are not inverses", lab, lab3);
			}
			catch (NotSupportedException)
			{
			}
		}

		// It's okay to not support the fromString operation
		private static void ValidateHasTag(string type, IHasTag lab, string tag)
		{
			NUnit.Framework.Assert.AreEqual(type + " does not have tag it was constructed with", lab.Tag(), tag);
			string newVal = "feijoa";
			lab.SetTag(newVal);
			NUnit.Framework.Assert.AreEqual(type + " does not have tag set with setTag", newVal, lab.Tag());
			// restore value
			lab.SetTag(tag);
		}

		[NUnit.Framework.Test]
		public virtual void TestStringLabel()
		{
			string val = "octopus";
			ILabel sl = new StringLabel(val);
			InternalValidation("StringLabel ", sl, val);
		}

		[NUnit.Framework.Test]
		public virtual void TestWord()
		{
			string val = "octopus";
			ILabel sl = new Word(val);
			InternalValidation("Word ", sl, val);
		}

		[NUnit.Framework.Test]
		public virtual void TestTaggedWord()
		{
			string val = "fish";
			TaggedWord sl = new TaggedWord(val);
			InternalValidation("TaggedWord", sl, val);
			string tag = "NN";
			sl = new TaggedWord(val, tag);
			InternalValidation("TaggedWord", sl, val);
			ValidateHasTag("TaggedWord", sl, tag);
			TaggedWord tw2 = new TaggedWord(sl);
			InternalValidation("TaggedWord", tw2, val);
			ValidateHasTag("TaggedWord", tw2, tag);
		}

		[NUnit.Framework.Test]
		public virtual void TestWordTag()
		{
			string val = "fowl";
			WordTag sl = new WordTag(val);
			InternalValidation("WordTag", sl, val);
			string tag = "NN";
			sl = new WordTag(val, tag);
			InternalValidation("WordTag", sl, val);
			ValidateHasTag("WordTag", sl, tag);
			WordTag wt2 = new WordTag(sl);
			InternalValidation("WordTag", wt2, val);
			ValidateHasTag("WordTag", wt2, tag);
		}
	}
}
