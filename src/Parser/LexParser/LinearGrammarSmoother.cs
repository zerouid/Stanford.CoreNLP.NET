using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Implements linear rule smoothing a la Petrov et al.</summary>
	/// <remarks>Implements linear rule smoothing a la Petrov et al. (2006).</remarks>
	/// <author>Spence Green</author>
	public class LinearGrammarSmoother : IFunction<Pair<UnaryGrammar, BinaryGrammar>, Pair<UnaryGrammar, BinaryGrammar>>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.LinearGrammarSmoother));

		private const bool Debug = false;

		private double Alpha = 0.01;

		private readonly string[] annotationIntroducingChars = new string[] { "-", "=", "|", "#", "^", "~", "_" };

		private readonly ICollection<string> annoteChars;

		private readonly TrainOptions trainOptions;

		private readonly IIndex<string> stateIndex;

		private readonly IIndex<string> tagIndex;

		public LinearGrammarSmoother(TrainOptions trainOptions, IIndex<string> stateIndex, IIndex<string> tagIndex)
		{
			annoteChars = Generics.NewHashSet(Arrays.AsList(annotationIntroducingChars));
			//  private static final String SYNTH_NODE_MARK = "@";
			//  
			//  private static final Pattern pContext = Pattern.compile("(\\|.+)$");
			// Do not include @ in this list! @ marks synthetic nodes!
			// Stole these from PennTreebankLanguagePack
			this.trainOptions = trainOptions;
			this.stateIndex = stateIndex;
			this.tagIndex = tagIndex;
		}

		/// <summary>Destructively modifies the input and returns it as a convenience.</summary>
		public virtual Pair<UnaryGrammar, BinaryGrammar> Apply(Pair<UnaryGrammar, BinaryGrammar> bgug)
		{
			Alpha = trainOptions.ruleSmoothingAlpha;
			ICounter<string> symWeights = new ClassicCounter<string>();
			ICounter<string> symCounts = new ClassicCounter<string>();
			//Tally unary rules
			foreach (UnaryRule rule in bgug.First())
			{
				if (!tagIndex.Contains(rule.parent))
				{
					UpdateCounters(rule, symWeights, symCounts);
				}
			}
			//Tally binary rules
			foreach (BinaryRule rule_1 in bgug.Second())
			{
				UpdateCounters(rule_1, symWeights, symCounts);
			}
			//Compute smoothed rule scores, unary
			foreach (UnaryRule rule_2 in bgug.First())
			{
				if (!tagIndex.Contains(rule_2.parent))
				{
					rule_2.score = SmoothRuleWeight(rule_2, symWeights, symCounts);
				}
			}
			//Compute smoothed rule scores, binary
			foreach (BinaryRule rule_3 in bgug.Second())
			{
				rule_3.score = SmoothRuleWeight(rule_3, symWeights, symCounts);
			}
			return bgug;
		}

		private void UpdateCounters(IRule rule, ICounter<string> symWeights, ICounter<string> symCounts)
		{
			string label = stateIndex.Get(rule.Parent());
			string basicCat = BasicCategory(label);
			symWeights.IncrementCount(basicCat, Math.Exp(rule.Score()));
			symCounts.IncrementCount(basicCat);
		}

		private float SmoothRuleWeight(IRule rule, ICounter<string> symWeights, ICounter<string> symCounts)
		{
			string label = stateIndex.Get(rule.Parent());
			string basicCat = BasicCategory(label);
			double pSum = symWeights.GetCount(basicCat);
			double n = symCounts.GetCount(basicCat);
			double pRule = Math.Exp(rule.Score());
			double pSmooth = (1.0 - Alpha) * pRule;
			pSmooth += Alpha * (pSum / n);
			pSmooth = Math.Log(pSmooth);
			return (float)pSmooth;
		}

		private int PostBasicCategoryIndex(string category)
		{
			bool sawAtZero = false;
			string seenAtZero = "\u0000";
			int i;
			for (i = 0; i < category.Length; i++)
			{
				string ch = Sharpen.Runtime.Substring(category, i, i + 1);
				if (annoteChars.Contains(ch))
				{
					if (i == 0)
					{
						sawAtZero = true;
						seenAtZero = ch;
					}
					else
					{
						if (sawAtZero && ch == seenAtZero)
						{
							sawAtZero = false;
						}
						else
						{
							break;
						}
					}
				}
			}
			return i;
		}

		public virtual string BasicCategory(string category)
		{
			if (category == null)
			{
				return null;
			}
			else
			{
				string basicCat = Sharpen.Runtime.Substring(category, 0, PostBasicCategoryIndex(category));
				//wsg2011: Tried adding the context of synthetic nodes to the basic category, but this lowered F1.
				//      if(String.valueOf(category.charAt(0)).equals(SYNTH_NODE_MARK)) {
				//        Matcher m = pContext.matcher(category);
				//        if(m.find()) {
				//          String context = m.group(1);
				//          basicCat = basicCat + context;
				//        
				//        } else {
				//          throw new RuntimeException(String.format("%s: Synthetic label lacks context: %s",this.getClass().getName(),category));
				//        }
				//      } 
				return basicCat;
			}
		}
	}
}
