using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ShiftReduceParserTest
	{
		internal string commaTreeString = "(ROOT (FRAG (NP (DT A) (@NP (ADJP (JJ short) (@ADJP (, ,) (JJ simple))) (NN test)))))";

		internal string[] treeStrings = new string[] { "(ROOT (S (INTJ (RB No)) (@S (, ,) (@S (NP (PRP it)) (@S (VP (@VP (VBD was) (RB n't)) (NP (NNP Black) (NNP Monday))) (. .))))) (.$$. .$.))", "(ROOT (S (CC But) (@S (SBAR (IN while) (S (NP (DT the) (@NP (NNP New) (@NP (NNP York) (@NP (NNP Stock) (NNP Exchange))))) (VP (@VP (VBD did) (RB n't)) (VP (@VP (@VP (VB fall) (ADVP (RB apart))) (NP (NNP Friday))) (SBAR (IN as) (S (NP (DT the) (@NP (NNP Dow) (@NP (NNP Jones) (@NP (NNP Industrial) (NNP Average))))) (VP (VBD plunged) (NP (NP (CD 190.58) (NNS points)) (PRN (: --) (@PRN (NP (@NP (NP (JJS most)) (PP (IN of) (NP (PRP it)))) (PP (IN in) (NP (DT the) (@NP (JJ final) (NN hour))))) (: --))))))))))) (@S (NP (PRP it)) (@S (ADVP (RB barely)) (@S (VP (VBD managed) (S (VP (TO to) (VP (VB stay) (NP (NP (DT this) (NN side)) (PP (IN of) (NP (NN chaos)))))))) (. .)))))) (.$$. .$.))"
			, "(ROOT (S (NP (NP (DT Some) (@NP (`` ``) (@NP (NN circuit) (@NP (NNS breakers) ('' ''))))) (VP (VBN installed) (PP (IN after) (NP (DT the) (@NP (NNP October) (@NP (CD 1987) (NN crash))))))) (@S (VP (@VP (@VP (VBD failed) (NP (PRP$ their) (@NP (JJ first) (NN test)))) (PRN (, ,) (@PRN (S (NP (NNS traders)) (VP (VBP say))) (, ,)))) (S (ADJP (JJ unable) (S (VP (TO to) (VP (VB cool) (NP (NP (DT the) (@NP (NN selling) (NN panic))) (PP (IN in) (NP (DT both) (@NP (@NP (NNS stocks) (CC and)) (NNS futures))))))))))) (. .))) (.$$. .$.))"
			, "(ROOT (S (NP (SBAR foo))))", commaTreeString };

		/// <summary>
		/// Test that the entire transition process is working: get the
		/// transitions from a few trees, start an empty state from those
		/// trees, and verify that running the transitions on those states
		/// gets back the correct tree.
		/// </summary>
		/// <remarks>
		/// Test that the entire transition process is working: get the
		/// transitions from a few trees, start an empty state from those
		/// trees, and verify that running the transitions on those states
		/// gets back the correct tree.  Runs the test with unary transitions
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestUnaryTransitions()
		{
			foreach (string treeText in treeStrings)
			{
				Tree tree = ConvertTree(treeText);
				IList<ITransition> transitions = CreateTransitionSequence.CreateTransitionSequence(tree, false, Collections.Singleton("ROOT"), Collections.Singleton("ROOT"));
				State state = ShiftReduceParser.InitialStateFromGoldTagTree(tree);
				foreach (ITransition transition in transitions)
				{
					state = transition.Apply(state);
				}
				NUnit.Framework.Assert.AreEqual(tree, state.stack.Peek());
			}
		}

		/// <summary>Same thing, but with compound unary transitions</summary>
		[NUnit.Framework.Test]
		public virtual void TestCompoundUnaryTransitions()
		{
			foreach (string treeText in treeStrings)
			{
				Tree tree = ConvertTree(treeText);
				IList<ITransition> transitions = CreateTransitionSequence.CreateTransitionSequence(tree, true, Java.Util.Collections.Singleton("ROOT"), Java.Util.Collections.Singleton("ROOT"));
				State state = ShiftReduceParser.InitialStateFromGoldTagTree(tree);
				foreach (ITransition transition in transitions)
				{
					state = transition.Apply(state);
				}
				NUnit.Framework.Assert.AreEqual(tree, state.stack.Peek());
			}
		}

		internal virtual Tree ConvertTree(string treeText)
		{
			Options op = new Options();
			IHeadFinder binaryHeadFinder = new BinaryHeadFinder(op.tlpParams.HeadFinder());
			Tree tree = Tree.ValueOf(treeText);
			Edu.Stanford.Nlp.Trees.Trees.ConvertToCoreLabels(tree);
			tree.PercolateHeadAnnotations(binaryHeadFinder);
			return tree;
		}

		[NUnit.Framework.Test]
		public virtual void TestSeparators()
		{
			Tree tree = ConvertTree(commaTreeString);
			IList<ITransition> transitions = CreateTransitionSequence.CreateTransitionSequence(tree, true, Java.Util.Collections.Singleton("ROOT"), Java.Util.Collections.Singleton("ROOT"));
			IList<string> expectedTransitions = Arrays.AsList(new string[] { "Shift", "Shift", "Shift", "Shift", "RightBinary(@ADJP)", "RightBinary(ADJP)", "Shift", "RightBinary(@NP)", "RightBinary(NP)", "CompoundUnary*([ROOT, FRAG])", "Finalize", "Idle"
				 });
			NUnit.Framework.Assert.AreEqual(expectedTransitions, CollectionUtils.TransformAsList(transitions, null));
			string expectedSeparators = "[{2=,}]";
			State state = ShiftReduceParser.InitialStateFromGoldTagTree(tree);
			NUnit.Framework.Assert.AreEqual(1, state.separators.Count);
			NUnit.Framework.Assert.AreEqual(2, state.separators.FirstKey());
			NUnit.Framework.Assert.AreEqual(",", state.separators[2]);
		}

		[NUnit.Framework.Test]
		public virtual void TestInitialStateFromTagged()
		{
			string[] words = new string[] { "This", "is", "a", "short", "test", "." };
			string[] tags = new string[] { "DT", "VBZ", "DT", "JJ", "NN", "." };
			NUnit.Framework.Assert.AreEqual(words.Length, tags.Length);
			IList<TaggedWord> sentence = SentenceUtils.ToTaggedList(Arrays.AsList(words), Arrays.AsList(tags));
			State state = ShiftReduceParser.InitialStateFromTaggedSentence(sentence);
			for (int i = 0; i < words.Length; ++i)
			{
				NUnit.Framework.Assert.AreEqual(tags[i], state.sentence[i].Value());
				NUnit.Framework.Assert.AreEqual(1, state.sentence[i].Children().Length);
				NUnit.Framework.Assert.AreEqual(words[i], state.sentence[i].Children()[0].Value());
			}
		}

		public virtual void Binarize()
		{
		}
		// TreeBinarizer binarizer = new TreeBinarizer(new PennTreebankLanguagePack().headFinder(), new PennTreebankLanguagePack(),
		//                                             false, false, 0, false, false, 0.0, false, true, true);
		// Tree tree = Tree.valueOf(commas);
		// Trees.convertToCoreLabels(tree);
		// Tree binarized = binarizer.transformTree(tree);
		// System.err.println(binarized);
	}
}
