
using NUnit.Framework;


namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// Test some of the utility functions in
	/// <see cref="StanfordCoreNLP"/>
	/// .
	/// </summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	public class StanfordCoreNLPTest
	{
		[Test]
		public virtual void TestPrereqAnnotatorsBasic()
		{
			NUnit.Framework.Assert.AreEqual("tokenize,ssplit,pos,parse", StanfordCoreNLP.EnsurePrerequisiteAnnotators(new string[] { "parse" }, new Properties()));
			NUnit.Framework.Assert.AreEqual("tokenize,ssplit,pos,depparse", StanfordCoreNLP.EnsurePrerequisiteAnnotators(new string[] { "depparse" }, new Properties()));
			NUnit.Framework.Assert.AreEqual("tokenize,ssplit,pos,depparse", StanfordCoreNLP.EnsurePrerequisiteAnnotators(new string[] { "depparse", "tokenize" }, new Properties()));
			NUnit.Framework.Assert.AreEqual("tokenize,ssplit,pos,lemma,depparse,natlog", StanfordCoreNLP.EnsurePrerequisiteAnnotators(new string[] { "natlog", "tokenize" }, new Properties()));
		}

		[Test]
		public virtual void TestPrereqAnnotatorsOrderPreserving()
		{
			NUnit.Framework.Assert.AreEqual("tokenize,ssplit,pos,lemma,depparse,natlog", StanfordCoreNLP.EnsurePrerequisiteAnnotators(new string[] { "lemma", "depparse", "natlog" }, new Properties()));
			NUnit.Framework.Assert.AreEqual("tokenize,ssplit,pos,depparse,lemma,natlog", StanfordCoreNLP.EnsurePrerequisiteAnnotators(new string[] { "depparse", "lemma", "natlog" }, new Properties()));
			NUnit.Framework.Assert.AreEqual("tokenize,ssplit,pos,lemma,ner,regexner", StanfordCoreNLP.EnsurePrerequisiteAnnotators(new string[] { "ner", "regexner" }, new Properties()));
			NUnit.Framework.Assert.AreEqual("tokenize,ssplit,pos,lemma,ner,depparse", StanfordCoreNLP.EnsurePrerequisiteAnnotators(new string[] { "ner", "depparse" }, new Properties()));
			NUnit.Framework.Assert.AreEqual("tokenize,ssplit,pos,lemma,depparse,ner", StanfordCoreNLP.EnsurePrerequisiteAnnotators(new string[] { "depparse", "ner" }, new Properties()));
		}

		[Test]
		public virtual void TestPrereqAnnotatorsRegexNERAfterNER()
		{
			NUnit.Framework.Assert.AreEqual("tokenize,ssplit,pos,lemma,ner,regexner", StanfordCoreNLP.EnsurePrerequisiteAnnotators(new string[] { "regexner", "ner" }, new Properties()));
		}

		[Test]
		public virtual void TestPrereqAnnotatorsCorefBeforeOpenIE()
		{
			NUnit.Framework.Assert.AreEqual("tokenize,ssplit,pos,lemma,depparse,natlog,ner,coref,openie", StanfordCoreNLP.EnsurePrerequisiteAnnotators(new string[] { "openie", "coref" }, new Properties()));
			NUnit.Framework.Assert.AreEqual("tokenize,ssplit,pos,lemma,ner,depparse,natlog,coref,openie", StanfordCoreNLP.EnsurePrerequisiteAnnotators(new string[] { "coref", "openie" }, new Properties()));
		}

		[Test]
		public virtual void TestPrereqAnnotatorsCoref()
		{
			Properties props = new Properties();
			NUnit.Framework.Assert.AreEqual("tokenize,ssplit,pos,lemma,ner,depparse,coref", StanfordCoreNLP.EnsurePrerequisiteAnnotators(new string[] { "coref" }, props));
			NUnit.Framework.Assert.AreEqual("dep", props.GetProperty("coref.md.type", string.Empty));
		}

		[Test]
		public virtual void TestPrereqAnnotatorsCorefWithParse()
		{
			Properties props = new Properties();
			NUnit.Framework.Assert.AreEqual("tokenize,ssplit,pos,lemma,ner,parse,coref", StanfordCoreNLP.EnsurePrerequisiteAnnotators(new string[] { "parse", "coref" }, props));
			NUnit.Framework.Assert.AreEqual("__empty__", props.GetProperty("coref.md.type", "__empty__"));
		}
	}
}
