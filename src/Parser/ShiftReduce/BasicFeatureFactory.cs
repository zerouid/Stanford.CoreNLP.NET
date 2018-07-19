using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	[System.Serializable]
	public class BasicFeatureFactory : FeatureFactory
	{
		public static void AddUnaryStackFeatures(IList<string> features, CoreLabel label, string conFeature, string wordTagFeature, string tagFeature, string wordConFeature, string tagConFeature)
		{
			if (label == null)
			{
				features.Add(conFeature + Null);
				return;
			}
			string constituent = GetFeatureFromCoreLabel(label, FeatureFactory.FeatureComponent.Value);
			string tag = GetFeatureFromCoreLabel(label, FeatureFactory.FeatureComponent.Headtag);
			string word = GetFeatureFromCoreLabel(label, FeatureFactory.FeatureComponent.Headword);
			features.Add(conFeature + constituent);
			features.Add(wordTagFeature + word + "-" + tag);
			features.Add(tagFeature + tag);
			features.Add(wordConFeature + word + "-" + constituent);
			features.Add(tagConFeature + tag + "-" + constituent);
		}

		public static void AddUnaryQueueFeatures(IList<string> features, CoreLabel label, string wtFeature)
		{
			if (label == null)
			{
				features.Add(wtFeature + Null);
				return;
			}
			string tag = label.Get(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation)).Value();
			string word = label.Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation)).Value();
			// TODO: check to see if this is slow because of the string concat
			features.Add(wtFeature + tag + "-" + word);
		}

		public static void AddBinaryFeatures(IList<string> features, string name1, CoreLabel label1, FeatureFactory.FeatureComponent feature11, FeatureFactory.FeatureComponent feature12, string name2, CoreLabel label2, FeatureFactory.FeatureComponent
			 feature21, FeatureFactory.FeatureComponent feature22)
		{
			if (label1 == null)
			{
				if (label2 == null)
				{
					features.Add(name1 + "n" + name2 + "n");
				}
				else
				{
					AddUnaryFeature(features, name1 + "n" + name2 + feature21.ShortName() + "-", label2, feature21);
					AddUnaryFeature(features, name1 + "n" + name2 + feature22.ShortName() + "-", label2, feature22);
				}
			}
			else
			{
				if (label2 == null)
				{
					AddUnaryFeature(features, name1 + feature11.ShortName() + name2 + "n-", label1, feature11);
					AddUnaryFeature(features, name1 + feature12.ShortName() + name2 + "n-", label1, feature12);
				}
				else
				{
					AddBinaryFeature(features, name1 + feature11.ShortName() + name2 + feature21.ShortName() + "-", label1, feature11, label2, feature21);
					AddBinaryFeature(features, name1 + feature11.ShortName() + name2 + feature22.ShortName() + "-", label1, feature11, label2, feature22);
					AddBinaryFeature(features, name1 + feature12.ShortName() + name2 + feature21.ShortName() + "-", label1, feature12, label2, feature21);
					AddBinaryFeature(features, name1 + feature12.ShortName() + name2 + feature22.ShortName() + "-", label1, feature12, label2, feature22);
				}
			}
		}

		public static void AddUnaryFeature(IList<string> features, string featureType, CoreLabel label, FeatureFactory.FeatureComponent feature)
		{
			string value = GetFeatureFromCoreLabel(label, feature);
			features.Add(featureType + value);
		}

		public static void AddBinaryFeature(IList<string> features, string featureType, CoreLabel label1, FeatureFactory.FeatureComponent feature1, CoreLabel label2, FeatureFactory.FeatureComponent feature2)
		{
			string value1 = GetFeatureFromCoreLabel(label1, feature1);
			string value2 = GetFeatureFromCoreLabel(label2, feature2);
			features.Add(featureType + value1 + "-" + value2);
		}

		public static void AddTrigramFeature(IList<string> features, string featureType, CoreLabel label1, FeatureFactory.FeatureComponent feature1, CoreLabel label2, FeatureFactory.FeatureComponent feature2, CoreLabel label3, FeatureFactory.FeatureComponent
			 feature3)
		{
			string value1 = GetFeatureFromCoreLabel(label1, feature1);
			string value2 = GetFeatureFromCoreLabel(label2, feature2);
			string value3 = GetFeatureFromCoreLabel(label3, feature3);
			features.Add(featureType + value1 + "-" + value2 + "-" + value3);
		}

		public static void AddPositionFeatures(IList<string> features, State state)
		{
			if (state.tokenPosition >= state.sentence.Count)
			{
				features.Add("QUEUE_FINISHED");
			}
			if (state.tokenPosition >= state.sentence.Count && state.stack.Size() == 1)
			{
				features.Add("QUEUE_FINISHED_STACK_SINGLETON");
			}
		}

		public static void AddSeparatorFeature(IList<string> features, string featureType, State.HeadPosition separator)
		{
			if (separator == null)
			{
				return;
			}
			features.Add(featureType + separator);
		}

		public static void AddSeparatorFeature(IList<string> features, string featureType, CoreLabel label, FeatureFactory.FeatureComponent feature, State.HeadPosition separator)
		{
			if (separator == null)
			{
				return;
			}
			string value = GetFeatureFromCoreLabel(label, feature);
			features.Add(featureType + value + "-" + separator);
		}

		public static void AddSeparatorFeature(IList<string> features, string featureType, CoreLabel label, FeatureFactory.FeatureComponent feature, bool between)
		{
			string value = GetFeatureFromCoreLabel(label, feature);
			features.Add(featureType + value + "-" + between);
		}

		public static void AddSeparatorFeature(IList<string> features, string featureType, CoreLabel label1, FeatureFactory.FeatureComponent feature1, CoreLabel label2, FeatureFactory.FeatureComponent feature2, bool between)
		{
			string value1 = GetFeatureFromCoreLabel(label1, feature1);
			string value2 = GetFeatureFromCoreLabel(label2, feature2);
			features.Add(featureType + value1 + "-" + value2 + "-" + between);
		}

		public static void AddSeparatorFeatures(IList<string> features, string name1, CoreLabel label1, string name2, CoreLabel label2, string separatorBetween, int countBetween)
		{
			if (label1 == null || label2 == null)
			{
				return;
			}
			// 0 separators is captured by the countBetween features
			if (separatorBetween != null)
			{
				string separatorBetweenName = "Sepb" + name1 + name2 + "-" + separatorBetween + "-";
				AddUnaryFeature(features, name1 + "w" + separatorBetweenName, label1, FeatureFactory.FeatureComponent.Headword);
				AddBinaryFeature(features, name1 + "wc" + separatorBetweenName, label1, FeatureFactory.FeatureComponent.Headword, label1, FeatureFactory.FeatureComponent.Value);
				AddUnaryFeature(features, name2 + "w" + separatorBetweenName, label2, FeatureFactory.FeatureComponent.Headword);
				AddBinaryFeature(features, name2 + "wc" + separatorBetweenName, label2, FeatureFactory.FeatureComponent.Headword, label2, FeatureFactory.FeatureComponent.Value);
				AddBinaryFeature(features, name1 + "c" + name2 + "c" + separatorBetweenName, label1, FeatureFactory.FeatureComponent.Value, label2, FeatureFactory.FeatureComponent.Value);
			}
			string countBetweenName = "Sepb" + name1 + name2 + "-" + countBetween + "-";
			AddUnaryFeature(features, name1 + "w" + countBetweenName, label1, FeatureFactory.FeatureComponent.Headword);
			AddBinaryFeature(features, name1 + "wc" + countBetweenName, label1, FeatureFactory.FeatureComponent.Headword, label1, FeatureFactory.FeatureComponent.Value);
			AddUnaryFeature(features, name2 + "w" + countBetweenName, label2, FeatureFactory.FeatureComponent.Headword);
			AddBinaryFeature(features, name2 + "wc" + countBetweenName, label2, FeatureFactory.FeatureComponent.Headword, label2, FeatureFactory.FeatureComponent.Value);
			AddBinaryFeature(features, name1 + "c" + name2 + "c" + countBetweenName, label1, FeatureFactory.FeatureComponent.Value, label2, FeatureFactory.FeatureComponent.Value);
		}

		public static void AddSeparatorFeatures(IList<string> features, CoreLabel s0Label, CoreLabel s1Label, State.HeadPosition s0Separator, State.HeadPosition s1Separator)
		{
			bool between = false;
			if ((s0Separator != null && (s0Separator == State.HeadPosition.Both || s0Separator == State.HeadPosition.Left)) || (s1Separator != null && (s1Separator == State.HeadPosition.Both || s1Separator == State.HeadPosition.Right)))
			{
				between = true;
			}
			AddSeparatorFeature(features, "s0sep-", s0Separator);
			AddSeparatorFeature(features, "s1sep-", s1Separator);
			AddSeparatorFeature(features, "s0ws0sep-", s0Label, FeatureFactory.FeatureComponent.Headword, s0Separator);
			AddSeparatorFeature(features, "s0ws1sep-", s0Label, FeatureFactory.FeatureComponent.Headword, s1Separator);
			AddSeparatorFeature(features, "s1ws0sep-", s1Label, FeatureFactory.FeatureComponent.Headword, s0Separator);
			AddSeparatorFeature(features, "s1ws1sep-", s1Label, FeatureFactory.FeatureComponent.Headword, s1Separator);
			AddSeparatorFeature(features, "s0cs0sep-", s0Label, FeatureFactory.FeatureComponent.Value, s0Separator);
			AddSeparatorFeature(features, "s0cs1sep-", s0Label, FeatureFactory.FeatureComponent.Value, s1Separator);
			AddSeparatorFeature(features, "s1cs0sep-", s1Label, FeatureFactory.FeatureComponent.Value, s0Separator);
			AddSeparatorFeature(features, "s1cs1sep-", s1Label, FeatureFactory.FeatureComponent.Value, s1Separator);
			AddSeparatorFeature(features, "s0ts0sep-", s0Label, FeatureFactory.FeatureComponent.Headtag, s0Separator);
			AddSeparatorFeature(features, "s0ts1sep-", s0Label, FeatureFactory.FeatureComponent.Headtag, s1Separator);
			AddSeparatorFeature(features, "s1ts0sep-", s1Label, FeatureFactory.FeatureComponent.Headtag, s0Separator);
			AddSeparatorFeature(features, "s1ts1sep-", s1Label, FeatureFactory.FeatureComponent.Headtag, s1Separator);
			if (s0Label != null && s1Label != null)
			{
				AddSeparatorFeature(features, "s0wsb-", s0Label, FeatureFactory.FeatureComponent.Headword, between);
				AddSeparatorFeature(features, "s1wsb-", s1Label, FeatureFactory.FeatureComponent.Headword, between);
				AddSeparatorFeature(features, "s0csb-", s0Label, FeatureFactory.FeatureComponent.Value, between);
				AddSeparatorFeature(features, "s1csb-", s1Label, FeatureFactory.FeatureComponent.Value, between);
				AddSeparatorFeature(features, "s0tsb-", s0Label, FeatureFactory.FeatureComponent.Headtag, between);
				AddSeparatorFeature(features, "s1tsb-", s1Label, FeatureFactory.FeatureComponent.Headtag, between);
				AddSeparatorFeature(features, "s0cs1csb-", s0Label, FeatureFactory.FeatureComponent.Value, s1Label, FeatureFactory.FeatureComponent.Value, between);
			}
		}

		/// <summary>
		/// Could potentially add the tags and words for the left and right
		/// ends of the tree.
		/// </summary>
		/// <remarks>
		/// Could potentially add the tags and words for the left and right
		/// ends of the tree.  Also adds notes about the sizes of the given
		/// tree.  However, it seems somewhat slow and doesn't help accuracy.
		/// </remarks>
		public virtual void AddEdgeFeatures(IList<string> features, State state, string nodeName, string neighborName, Tree node, Tree neighbor)
		{
			if (node == null)
			{
				return;
			}
			int left = ShiftReduceUtils.LeftIndex(node);
			int right = ShiftReduceUtils.RightIndex(node);
			// Trees of size one are already featurized
			if (right == left)
			{
				features.Add(nodeName + "SZ1");
				return;
			}
			AddUnaryQueueFeatures(features, GetCoreLabel(state.sentence[left]), nodeName + "EL-");
			AddUnaryQueueFeatures(features, GetCoreLabel(state.sentence[right]), nodeName + "ER-");
			if (neighbor != null)
			{
				AddBinaryFeatures(features, nodeName, GetCoreLabel(state.sentence[right]), FeatureFactory.FeatureComponent.Headword, FeatureFactory.FeatureComponent.Headtag, neighborName, GetCoreLabel(neighbor), FeatureFactory.FeatureComponent.Headword, FeatureFactory.FeatureComponent
					.Headtag);
			}
			if (right - left == 1)
			{
				features.Add(nodeName + "SZ2");
				return;
			}
			if (right - left == 2)
			{
				features.Add(nodeName + "SZ3");
				AddUnaryQueueFeatures(features, GetCoreLabel(state.sentence[left + 1]), nodeName + "EM-");
				return;
			}
			features.Add(nodeName + "SZB");
			AddUnaryQueueFeatures(features, GetCoreLabel(state.sentence[left + 1]), nodeName + "El-");
			AddUnaryQueueFeatures(features, GetCoreLabel(state.sentence[right - 1]), nodeName + "Er-");
		}

		/// <summary>This option also does not seem to help</summary>
		public virtual void AddEdgeFeatures2(IList<string> features, State state, string nodeName, Tree node)
		{
			if (node == null)
			{
				return;
			}
			int left = ShiftReduceUtils.LeftIndex(node);
			int right = ShiftReduceUtils.RightIndex(node);
			CoreLabel nodeLabel = GetCoreLabel(node);
			string nodeValue = GetFeatureFromCoreLabel(nodeLabel, FeatureFactory.FeatureComponent.Value) + "-";
			CoreLabel leftLabel = GetQueueLabel(state, left);
			CoreLabel rightLabel = GetQueueLabel(state, right);
			AddUnaryQueueFeatures(features, leftLabel, nodeName + "EL-" + nodeValue);
			AddUnaryQueueFeatures(features, rightLabel, nodeName + "ER-" + nodeValue);
			CoreLabel previousLabel = GetQueueLabel(state, left - 1);
			AddUnaryQueueFeatures(features, previousLabel, nodeName + "EP-" + nodeValue);
			CoreLabel nextLabel = GetQueueLabel(state, right + 1);
			AddUnaryQueueFeatures(features, nextLabel, nodeName + "EN-" + nodeValue);
		}

		/// <summary>Also did not seem to help</summary>
		public virtual void AddExtraTrigramFeatures(IList<string> features, CoreLabel s0Label, CoreLabel s1Label, CoreLabel s2Label, CoreLabel q0Label, CoreLabel q1Label)
		{
			AddTrigramFeature(features, "S0wS1wS2c-", s0Label, FeatureFactory.FeatureComponent.Headword, s1Label, FeatureFactory.FeatureComponent.Headword, s2Label, FeatureFactory.FeatureComponent.Value);
			AddTrigramFeature(features, "S0wS1cS2w-", s0Label, FeatureFactory.FeatureComponent.Headword, s1Label, FeatureFactory.FeatureComponent.Value, s2Label, FeatureFactory.FeatureComponent.Headword);
			AddTrigramFeature(features, "S0cS1wS2w-", s0Label, FeatureFactory.FeatureComponent.Value, s1Label, FeatureFactory.FeatureComponent.Headword, s2Label, FeatureFactory.FeatureComponent.Headword);
			AddTrigramFeature(features, "S0wS1wQ0t-", s0Label, FeatureFactory.FeatureComponent.Headword, s1Label, FeatureFactory.FeatureComponent.Headword, q0Label, FeatureFactory.FeatureComponent.Headtag);
			AddTrigramFeature(features, "S0wS1cQ0w-", s0Label, FeatureFactory.FeatureComponent.Headword, s1Label, FeatureFactory.FeatureComponent.Value, q0Label, FeatureFactory.FeatureComponent.Headword);
			AddTrigramFeature(features, "S0cS1wQ0w-", s0Label, FeatureFactory.FeatureComponent.Value, s1Label, FeatureFactory.FeatureComponent.Headword, q0Label, FeatureFactory.FeatureComponent.Headword);
			AddTrigramFeature(features, "S0cQ0tQ1t-", s0Label, FeatureFactory.FeatureComponent.Value, s1Label, FeatureFactory.FeatureComponent.Headtag, q0Label, FeatureFactory.FeatureComponent.Headtag);
			AddTrigramFeature(features, "S0wQ0tQ1t-", s0Label, FeatureFactory.FeatureComponent.Headword, s1Label, FeatureFactory.FeatureComponent.Headtag, q0Label, FeatureFactory.FeatureComponent.Headtag);
			AddTrigramFeature(features, "S0cQ0wQ1t-", s0Label, FeatureFactory.FeatureComponent.Value, s1Label, FeatureFactory.FeatureComponent.Headword, q0Label, FeatureFactory.FeatureComponent.Headtag);
			AddTrigramFeature(features, "S0cQ0tQ1w-", s0Label, FeatureFactory.FeatureComponent.Value, s1Label, FeatureFactory.FeatureComponent.Headtag, q0Label, FeatureFactory.FeatureComponent.Headword);
			AddTrigramFeature(features, "S0wQ0wQ1t-", s0Label, FeatureFactory.FeatureComponent.Headword, s1Label, FeatureFactory.FeatureComponent.Headword, q0Label, FeatureFactory.FeatureComponent.Headtag);
			AddTrigramFeature(features, "S0wQ0tQ1w-", s0Label, FeatureFactory.FeatureComponent.Headword, s1Label, FeatureFactory.FeatureComponent.Headtag, q0Label, FeatureFactory.FeatureComponent.Headword);
			AddTrigramFeature(features, "S0cQ0wQ1w-", s0Label, FeatureFactory.FeatureComponent.Value, s1Label, FeatureFactory.FeatureComponent.Headword, q0Label, FeatureFactory.FeatureComponent.Headword);
		}

		public override IList<string> Featurize(State state, IList<string> features)
		{
			TreeShapedStack<Tree> stack = state.stack;
			IList<Tree> sentence = state.sentence;
			int tokenPosition = state.tokenPosition;
			CoreLabel s0Label = GetStackLabel(stack, 0);
			// current top of stack
			CoreLabel s1Label = GetStackLabel(stack, 1);
			// one previous
			CoreLabel s2Label = GetStackLabel(stack, 2);
			// two previous
			CoreLabel s3Label = GetStackLabel(stack, 3);
			// three previous
			CoreLabel s0LLabel = GetStackLabel(stack, 0, FeatureFactory.Transition.Left);
			CoreLabel s0RLabel = GetStackLabel(stack, 0, FeatureFactory.Transition.Right);
			CoreLabel s0ULabel = GetStackLabel(stack, 0, FeatureFactory.Transition.Unary);
			CoreLabel s0LLLabel = GetStackLabel(stack, 0, FeatureFactory.Transition.Left, FeatureFactory.Transition.Left);
			CoreLabel s0LRLabel = GetStackLabel(stack, 0, FeatureFactory.Transition.Left, FeatureFactory.Transition.Right);
			CoreLabel s0LULabel = GetStackLabel(stack, 0, FeatureFactory.Transition.Left, FeatureFactory.Transition.Unary);
			CoreLabel s0RLLabel = GetStackLabel(stack, 0, FeatureFactory.Transition.Right, FeatureFactory.Transition.Left);
			CoreLabel s0RRLabel = GetStackLabel(stack, 0, FeatureFactory.Transition.Right, FeatureFactory.Transition.Right);
			CoreLabel s0RULabel = GetStackLabel(stack, 0, FeatureFactory.Transition.Right, FeatureFactory.Transition.Unary);
			CoreLabel s0ULLabel = GetStackLabel(stack, 0, FeatureFactory.Transition.Unary, FeatureFactory.Transition.Left);
			CoreLabel s0URLabel = GetStackLabel(stack, 0, FeatureFactory.Transition.Unary, FeatureFactory.Transition.Right);
			CoreLabel s0UULabel = GetStackLabel(stack, 0, FeatureFactory.Transition.Unary, FeatureFactory.Transition.Unary);
			CoreLabel s1LLabel = GetStackLabel(stack, 1, FeatureFactory.Transition.Left);
			CoreLabel s1RLabel = GetStackLabel(stack, 1, FeatureFactory.Transition.Right);
			CoreLabel s1ULabel = GetStackLabel(stack, 1, FeatureFactory.Transition.Unary);
			CoreLabel q0Label = GetQueueLabel(sentence, tokenPosition, 0);
			// current location in queue
			CoreLabel q1Label = GetQueueLabel(sentence, tokenPosition, 1);
			// next location in queue
			CoreLabel q2Label = GetQueueLabel(sentence, tokenPosition, 2);
			// two locations later in queue
			CoreLabel q3Label = GetQueueLabel(sentence, tokenPosition, 3);
			// three locations later in queue
			CoreLabel qP1Label = GetQueueLabel(sentence, tokenPosition, -1);
			// previous location in queue
			CoreLabel qP2Label = GetQueueLabel(sentence, tokenPosition, -2);
			// two locations prior in queue
			// It's kind of unpleasant having this magic order of feature names.
			// On the other hand, it does save some time with string concatenation.
			AddUnaryStackFeatures(features, s0Label, "S0C-", "S0WT-", "S0T-", "S0WC-", "S0TC-");
			AddUnaryStackFeatures(features, s1Label, "S1C-", "S1WT-", "S1T-", "S1WC-", "S1TC-");
			AddUnaryStackFeatures(features, s2Label, "S2C-", "S2WT-", "S2T-", "S2WC-", "S2TC-");
			AddUnaryStackFeatures(features, s3Label, "S3C-", "S3WT-", "S3T-", "S3WC-", "S3TC-");
			AddUnaryStackFeatures(features, s0LLabel, "S0LC-", "S0LWT-", "S0LT-", "S0LWC-", "S0LTC-");
			AddUnaryStackFeatures(features, s0RLabel, "S0RC-", "S0RWT-", "S0RT-", "S0RWC-", "S0RTC-");
			AddUnaryStackFeatures(features, s0ULabel, "S0UC-", "S0UWT-", "S0UT-", "S0UWC-", "S0UTC-");
			AddUnaryStackFeatures(features, s0LLLabel, "S0LLC-", "S0LLWT-", "S0LLT-", "S0LLWC-", "S0LLTC-");
			AddUnaryStackFeatures(features, s0LRLabel, "S0LRC-", "S0LRWT-", "S0LRT-", "S0LRWC-", "S0LRTC-");
			AddUnaryStackFeatures(features, s0LULabel, "S0LUC-", "S0LUWT-", "S0LUT-", "S0LUWC-", "S0LUTC-");
			AddUnaryStackFeatures(features, s0RLLabel, "S0RLC-", "S0RLWT-", "S0RLT-", "S0RLWC-", "S0RLTC-");
			AddUnaryStackFeatures(features, s0RRLabel, "S0RRC-", "S0RRWT-", "S0RRT-", "S0RRWC-", "S0RRTC-");
			AddUnaryStackFeatures(features, s0RULabel, "S0RUC-", "S0RUWT-", "S0RUT-", "S0RUWC-", "S0RUTC-");
			AddUnaryStackFeatures(features, s0ULLabel, "S0ULC-", "S0ULWT-", "S0ULT-", "S0ULWC-", "S0ULTC-");
			AddUnaryStackFeatures(features, s0URLabel, "S0URC-", "S0URWT-", "S0URT-", "S0URWC-", "S0URTC-");
			AddUnaryStackFeatures(features, s0UULabel, "S0UUC-", "S0UUWT-", "S0UUT-", "S0UUWC-", "S0UUTC-");
			AddUnaryStackFeatures(features, s1LLabel, "S1LC-", "S1LWT-", "S1LT-", "S1LWC-", "S1LTC-");
			AddUnaryStackFeatures(features, s1RLabel, "S1RC-", "S1RWT-", "S1RT-", "S1RWC-", "S1RTC-");
			AddUnaryStackFeatures(features, s1ULabel, "S1UC-", "S1UWT-", "S1UT-", "S1UWC-", "S1UTC-");
			AddUnaryQueueFeatures(features, q0Label, "Q0WT-");
			AddUnaryQueueFeatures(features, q1Label, "Q1WT-");
			AddUnaryQueueFeatures(features, q2Label, "Q2WT-");
			AddUnaryQueueFeatures(features, q3Label, "Q3WT-");
			AddUnaryQueueFeatures(features, qP1Label, "QP1WT-");
			AddUnaryQueueFeatures(features, qP2Label, "QP2WT-");
			// Figure out which are the most recent left and right node
			// attachments to the heads of the given nodes.  It seems like it
			// should be more efficient to keep track of this in the state, as
			// that would have a constant cost per transformation, but it is
			// actually faster to find it by walking down the tree each time
			CoreLabel recentL0Label = GetRecentDependent(stack, FeatureFactory.Transition.Left, 0);
			CoreLabel recentR0Label = GetRecentDependent(stack, FeatureFactory.Transition.Right, 0);
			CoreLabel recentL1Label = GetRecentDependent(stack, FeatureFactory.Transition.Left, 1);
			CoreLabel recentR1Label = GetRecentDependent(stack, FeatureFactory.Transition.Right, 1);
			AddUnaryStackFeatures(features, recentL0Label, "recL0C-", "recL0WT-", "recL0T-", "recL0WC-", "recL0TC-");
			AddUnaryStackFeatures(features, recentR0Label, "recR0C-", "recR0WT-", "recR0T-", "recR0WC-", "recR0TC-");
			AddUnaryStackFeatures(features, recentL1Label, "recL1C-", "recL1WT-", "recL1T-", "recL1WC-", "recL1TC-");
			AddUnaryStackFeatures(features, recentR1Label, "recR1C-", "recR1WT-", "recR1T-", "recR1WC-", "recR1TC-");
			AddBinaryFeatures(features, "S0", s0Label, FeatureFactory.FeatureComponent.Headword, FeatureFactory.FeatureComponent.Value, "S1", s1Label, FeatureFactory.FeatureComponent.Headword, FeatureFactory.FeatureComponent.Value);
			AddBinaryFeatures(features, "S0", s0Label, FeatureFactory.FeatureComponent.Headword, FeatureFactory.FeatureComponent.Value, "Q0", q0Label, FeatureFactory.FeatureComponent.Headword, FeatureFactory.FeatureComponent.Headtag);
			AddBinaryFeatures(features, "S1", s1Label, FeatureFactory.FeatureComponent.Headword, FeatureFactory.FeatureComponent.Value, "Q0", q0Label, FeatureFactory.FeatureComponent.Headword, FeatureFactory.FeatureComponent.Headtag);
			AddBinaryFeatures(features, "Q0", q0Label, FeatureFactory.FeatureComponent.Headword, FeatureFactory.FeatureComponent.Headtag, "Q1", q1Label, FeatureFactory.FeatureComponent.Headword, FeatureFactory.FeatureComponent.Headtag);
			AddTrigramFeature(features, "S0cS1cS2c-", s0Label, FeatureFactory.FeatureComponent.Value, s1Label, FeatureFactory.FeatureComponent.Value, s2Label, FeatureFactory.FeatureComponent.Value);
			AddTrigramFeature(features, "S0wS1cS2c-", s0Label, FeatureFactory.FeatureComponent.Headword, s1Label, FeatureFactory.FeatureComponent.Value, s2Label, FeatureFactory.FeatureComponent.Value);
			AddTrigramFeature(features, "S0cS1wS2c-", s0Label, FeatureFactory.FeatureComponent.Value, s1Label, FeatureFactory.FeatureComponent.Headword, s2Label, FeatureFactory.FeatureComponent.Value);
			AddTrigramFeature(features, "S0cS1cS2w-", s0Label, FeatureFactory.FeatureComponent.Value, s1Label, FeatureFactory.FeatureComponent.Value, s2Label, FeatureFactory.FeatureComponent.Headword);
			AddTrigramFeature(features, "S0cS1cQ0t-", s0Label, FeatureFactory.FeatureComponent.Value, s1Label, FeatureFactory.FeatureComponent.Value, q0Label, FeatureFactory.FeatureComponent.Headtag);
			AddTrigramFeature(features, "S0wS1cQ0t-", s0Label, FeatureFactory.FeatureComponent.Headword, s1Label, FeatureFactory.FeatureComponent.Value, q0Label, FeatureFactory.FeatureComponent.Headtag);
			AddTrigramFeature(features, "S0cS1wQ0t-", s0Label, FeatureFactory.FeatureComponent.Value, s1Label, FeatureFactory.FeatureComponent.Headword, q0Label, FeatureFactory.FeatureComponent.Headtag);
			AddTrigramFeature(features, "S0cS1cQ0w-", s0Label, FeatureFactory.FeatureComponent.Value, s1Label, FeatureFactory.FeatureComponent.Value, q0Label, FeatureFactory.FeatureComponent.Headword);
			AddPositionFeatures(features, state);
			// State.HeadPosition s0Separator = state.getSeparator(0);
			// State.HeadPosition s1Separator = state.getSeparator(1);
			// addSeparatorFeatures(features, s0Label, s1Label, s0Separator, s1Separator);
			Tree s0Node = state.GetStackNode(0);
			Tree s1Node = state.GetStackNode(1);
			Tree q0Node = state.GetQueueNode(0);
			AddSeparatorFeatures(features, "S0", s0Label, "S1", s1Label, state.GetSeparatorBetween(s0Node, s1Node), state.GetSeparatorCount(s0Node, s1Node));
			AddSeparatorFeatures(features, "S0", s0Label, "Q0", q0Label, state.GetSeparatorBetween(q0Node, s0Node), state.GetSeparatorCount(q0Node, s0Node));
			return features;
		}

		private const long serialVersionUID = 1;
	}
}
