using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	public class BinaryGrammarExtractor : AbstractTreeExtractor<Pair<UnaryGrammar, BinaryGrammar>>
	{
		protected internal IIndex<string> stateIndex;

		private ClassicCounter<UnaryRule> unaryRuleCounter = new ClassicCounter<UnaryRule>();

		private ClassicCounter<BinaryRule> binaryRuleCounter = new ClassicCounter<BinaryRule>();

		protected internal ClassicCounter<string> symbolCounter = new ClassicCounter<string>();

		private ICollection<BinaryRule> binaryRules = Generics.NewHashSet();

		private ICollection<UnaryRule> unaryRules = Generics.NewHashSet();

		public BinaryGrammarExtractor(Options op, IIndex<string> index)
			: base(op)
		{
			//  protected void tallyTree(Tree t, double weight) {
			//    super.tallyTree(t, weight);
			//    System.out.println("Tree:");
			//    t.pennPrint();
			//  }
			this.stateIndex = index;
		}

		protected internal override void TallyInternalNode(Tree lt, double weight)
		{
			if (lt.Children().Length == 1)
			{
				UnaryRule ur = new UnaryRule(stateIndex.AddToIndex(lt.Label().Value()), stateIndex.AddToIndex(lt.Children()[0].Label().Value()));
				symbolCounter.IncrementCount(stateIndex.Get(ur.parent), weight);
				unaryRuleCounter.IncrementCount(ur, weight);
				unaryRules.Add(ur);
			}
			else
			{
				BinaryRule br = new BinaryRule(stateIndex.AddToIndex(lt.Label().Value()), stateIndex.AddToIndex(lt.Children()[0].Label().Value()), stateIndex.AddToIndex(lt.Children()[1].Label().Value()));
				symbolCounter.IncrementCount(stateIndex.Get(br.parent), weight);
				binaryRuleCounter.IncrementCount(br, weight);
				binaryRules.Add(br);
			}
		}

		public override Pair<UnaryGrammar, BinaryGrammar> FormResult()
		{
			stateIndex.AddToIndex(LexiconConstants.BoundaryTag);
			BinaryGrammar bg = new BinaryGrammar(stateIndex);
			UnaryGrammar ug = new UnaryGrammar(stateIndex);
			// add unaries
			foreach (UnaryRule ur in unaryRules)
			{
				ur.score = (float)Math.Log(unaryRuleCounter.GetCount(ur) / symbolCounter.GetCount(stateIndex.Get(ur.parent)));
				if (op.trainOptions.CompactGrammar() >= 4)
				{
					ur.score = (float)unaryRuleCounter.GetCount(ur);
				}
				ug.AddRule(ur);
			}
			// add binaries
			foreach (BinaryRule br in binaryRules)
			{
				br.score = (float)Math.Log((binaryRuleCounter.GetCount(br) - op.trainOptions.ruleDiscount) / symbolCounter.GetCount(stateIndex.Get(br.parent)));
				if (op.trainOptions.CompactGrammar() >= 4)
				{
					br.score = (float)binaryRuleCounter.GetCount(br);
				}
				bg.AddRule(br);
			}
			return new Pair<UnaryGrammar, BinaryGrammar>(ug, bg);
		}
	}
}
