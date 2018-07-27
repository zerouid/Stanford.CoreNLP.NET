using System;
using System.Collections.Generic;
using System.Text;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.IE.Machinereading
{
	/// <author>Mason Smith</author>
	/// <author>Mihai Surdeanu</author>
	[System.Serializable]
	public class BasicRelationFeatureFactory : RelationFeatureFactory
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Machinereading.BasicRelationFeatureFactory));

		private const long serialVersionUID = -7376668998622546620L;

		private static readonly Logger logger = Logger.GetLogger(typeof(Edu.Stanford.Nlp.IE.Machinereading.BasicRelationFeatureFactory).FullName);

		protected internal static readonly IList<string> dependencyFeatures = Java.Util.Collections.UnmodifiableList(Arrays.AsList("dependency_path_lowlevel", "dependency_path_length", "dependency_path_length_binary", "verb_in_dependency_path", "dependency_path"
			, "dependency_path_words", "dependency_paths_to_verb", "dependency_path_stubs_to_verb", "dependency_path_POS_unigrams", "dependency_path_word_n_grams", "dependency_path_POS_n_grams", "dependency_path_edge_n_grams", "dependency_path_edge_lowlevel_n_grams"
			, "dependency_path_edge-node-edge-grams", "dependency_path_edge-node-edge-grams_lowlevel", "dependency_path_node-edge-node-grams", "dependency_path_node-edge-node-grams_lowlevel", "dependency_path_directed_bigrams", "dependency_path_edge_unigrams"
			, "dependency_path_trigger"));

		protected internal IList<string> featureList;

		public BasicRelationFeatureFactory(params string[] featureList)
		{
			// XXX convert to BasicRelationFeatureFactory, make RelationFeatureFactory an interface
			this.doNotLexicalizeFirstArg = false;
			this.dependencyType = RelationFeatureFactory.DEPENDENCY_TYPE.CollapsedCcprocessed;
			this.featureList = Java.Util.Collections.UnmodifiableList(Arrays.AsList(featureList));
		}

		static BasicRelationFeatureFactory()
		{
			logger.SetLevel(Level.Info);
		}

		public override IDatum<string, string> CreateDatum(RelationMention rel)
		{
			return CreateDatum(rel, (Logger)null);
		}

		public virtual IDatum<string, string> CreateDatum(RelationMention rel, Logger logger)
		{
			ICounter<string> features = new ClassicCounter<string>();
			if (rel.GetArgs().Count != 2)
			{
				return null;
			}
			AddFeatures(features, rel, featureList, logger);
			string labelString = rel.GetType();
			return new RVFDatum<string, string>(features, labelString);
		}

		public override IDatum<string, string> CreateTestDatum(RelationMention rel, Logger logger)
		{
			return CreateDatum(rel, logger);
		}

		public override IDatum<string, string> CreateDatum(RelationMention rel, string positiveLabel)
		{
			ICounter<string> features = new ClassicCounter<string>();
			if (rel.GetArgs().Count != 2)
			{
				return null;
			}
			AddFeatures(features, rel, featureList);
			string labelString = rel.GetType();
			if (!labelString.Equals(positiveLabel))
			{
				labelString = RelationMention.Unrelated;
			}
			return new RVFDatum<string, string>(features, labelString);
		}

		public virtual bool AddFeatures(ICounter<string> features, RelationMention rel, IList<string> types)
		{
			return AddFeatures(features, rel, types, null);
		}

		/// <summary>
		/// Creates all features for the datum corresponding to this relation mention
		/// Note: this assumes binary relations where both arguments are EntityMention
		/// </summary>
		/// <param name="features">Stores all features</param>
		/// <param name="rel">The mention</param>
		/// <param name="types">Comma separated list of feature classes to use</param>
		public virtual bool AddFeatures(ICounter<string> features, RelationMention rel, IList<string> types, Logger logger)
		{
			// sanity checks: must have two arguments, and each must be an entity mention
			if (rel.GetArgs().Count != 2)
			{
				return false;
			}
			if (!(rel.GetArg(0) is EntityMention))
			{
				return false;
			}
			if (!(rel.GetArg(1) is EntityMention))
			{
				return false;
			}
			EntityMention arg0 = (EntityMention)rel.GetArg(0);
			EntityMention arg1 = (EntityMention)rel.GetArg(1);
			Tree tree = rel.GetSentence().Get(typeof(TreeCoreAnnotations.TreeAnnotation));
			if (tree == null)
			{
				throw new Exception("ERROR: Relation extraction requires full syntactic analysis!");
			}
			IList<Tree> leaves = tree.GetLeaves();
			IList<CoreLabel> tokens = rel.GetSentence().Get(typeof(CoreAnnotations.TokensAnnotation));
			// this assumes that both args are in the same sentence as the relation object
			// let's check for this to be safe
			ICoreMap relSentence = rel.GetSentence();
			ICoreMap arg0Sentence = arg0.GetSentence();
			ICoreMap arg1Sentence = arg1.GetSentence();
			if (arg0Sentence != relSentence)
			{
				log.Info("WARNING: Found relation with arg0 in a different sentence: " + rel);
				log.Info("Relation sentence: " + relSentence.Get(typeof(CoreAnnotations.TextAnnotation)));
				log.Info("Arg0 sentence: " + arg0Sentence.Get(typeof(CoreAnnotations.TextAnnotation)));
				return false;
			}
			if (arg1Sentence != relSentence)
			{
				log.Info("WARNING: Found relation with arg1 in a different sentence: " + rel);
				log.Info("Relation sentence: " + relSentence.Get(typeof(CoreAnnotations.TextAnnotation)));
				log.Info("Arg1 sentence: " + arg1Sentence.Get(typeof(CoreAnnotations.TextAnnotation)));
				return false;
			}
			// Checklist keeps track of which features have been handled by an if clause
			// Should be empty after all the clauses have been gone through.
			IList<string> checklist = new List<string>(types);
			// arg_type: concatenation of the entity types of the args, e.g.
			// "arg1type=Loc_and_arg2type=Org"
			// arg_subtype: similar, for entity subtypes
			if (UsingFeature(types, checklist, "arg_type"))
			{
				features.SetCount("arg1type=" + arg0.GetType() + "_and_arg2type=" + arg1.GetType(), 1.0);
			}
			if (UsingFeature(types, checklist, "arg_subtype"))
			{
				features.SetCount("arg1subtype=" + arg0.GetSubType() + "_and_arg2subtype=" + arg1.GetSubType(), 1.0);
			}
			// arg_order: which arg comes first in the sentence
			if (UsingFeature(types, checklist, "arg_order"))
			{
				if (arg0.GetSyntacticHeadTokenPosition() < arg1.GetSyntacticHeadTokenPosition())
				{
					features.SetCount("arg1BeforeArg2", 1.0);
				}
			}
			// same_head: whether the two args share the same syntactic head token
			if (UsingFeature(types, checklist, "same_head"))
			{
				if (arg0.GetSyntacticHeadTokenPosition() == arg1.GetSyntacticHeadTokenPosition())
				{
					features.SetCount("arguments_have_same_head", 1.0);
				}
			}
			// full_tree_path: Path from one arg to the other in the phrase structure tree,
			// e.g., NNP -> PP -> NN <- NNP
			if (UsingFeature(types, checklist, "full_tree_path"))
			{
				//log.info("ARG0: " + arg0);
				//log.info("ARG0 HEAD: " + arg0.getSyntacticHeadTokenPosition());
				//log.info("TREE: " + tree);
				//log.info("SENTENCE: " + sentToString(arg0.getSentence()));
				if (arg0.GetSyntacticHeadTokenPosition() < leaves.Count && arg1.GetSyntacticHeadTokenPosition() < leaves.Count)
				{
					Tree arg0preterm = leaves[arg0.GetSyntacticHeadTokenPosition()].Parent(tree);
					Tree arg1preterm = leaves[arg1.GetSyntacticHeadTokenPosition()].Parent(tree);
					Tree join = tree.JoinNode(arg0preterm, arg1preterm);
					StringBuilder pathStringBuilder = new StringBuilder();
					IList<Tree> pathUp = join.DominationPath(arg0preterm);
					Java.Util.Collections.Reverse(pathUp);
					foreach (Tree node in pathUp)
					{
						if (node != join)
						{
							pathStringBuilder.Append(node.Label().Value() + " <- ");
						}
					}
					foreach (Tree node_1 in join.DominationPath(arg1preterm))
					{
						pathStringBuilder.Append(((node_1 == join) ? string.Empty : " -> ") + node_1.Label().Value());
					}
					string pathString = pathStringBuilder.ToString();
					if (logger != null && !rel.GetType().Equals(RelationMention.Unrelated))
					{
						logger.Info("full_tree_path: " + pathString);
					}
					features.SetCount("treepath:" + pathString, 1.0);
				}
				else
				{
					log.Info("WARNING: found weird argument offsets. Most likely because arguments appear in different sentences than the relation:");
					log.Info("ARG0: " + arg0);
					log.Info("ARG0 HEAD: " + arg0.GetSyntacticHeadTokenPosition());
					log.Info("ARG0 SENTENCE: " + SentToString(arg0.GetSentence()));
					log.Info("ARG1: " + arg1);
					log.Info("ARG1 HEAD: " + arg1.GetSyntacticHeadTokenPosition());
					log.Info("ARG1 SENTENCE: " + SentToString(arg1.GetSentence()));
					log.Info("RELATION TREE: " + tree);
				}
			}
			int pathLength = tree.PathNodeToNode(tree.GetLeaves()[arg0.GetSyntacticHeadTokenPosition()], tree.GetLeaves()[arg1.GetSyntacticHeadTokenPosition()]).Count;
			// path_length: Length of the path in the phrase structure parse tree, integer-valued feature
			if (UsingFeature(types, checklist, "path_length"))
			{
				features.SetCount("path_length", pathLength);
			}
			// path_length_binary: Length of the path in the phrase structure parse tree, binary features
			if (UsingFeature(types, checklist, "path_length_binary"))
			{
				features.SetCount("path_length_" + pathLength, 1.0);
			}
			/* entity_order
			* This tells you for each of the two args
			* whether there are other entities before or after that arg.
			* In particular, it can tell whether an arg is the first entity of its type in the sentence
			* (which can be useful for example for telling the gameWinner and gameLoser in NFL).
			* TODO: restrict this feature so that it only looks for
			* entities of the same type?
			* */
			if (UsingFeature(types, checklist, "entity_order"))
			{
				for (int i = 0; i < rel.GetArgs().Count; i++)
				{
					// We already checked the class of the args at the beginning of the method
					EntityMention arg = (EntityMention)rel.GetArgs()[i];
					if (rel.GetSentence().Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation)) != null)
					{
						// may be null due to annotation error
						foreach (EntityMention otherArg in rel.GetSentence().Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation)))
						{
							string feature;
							if (otherArg.GetSyntacticHeadTokenPosition() > arg.GetSyntacticHeadTokenPosition())
							{
								feature = "arg" + i + "_before_" + otherArg.GetType();
								features.SetCount(feature, 1.0);
							}
							if (otherArg.GetSyntacticHeadTokenPosition() < arg.GetSyntacticHeadTokenPosition())
							{
								feature = "arg" + i + "_after_" + otherArg.GetType();
								features.SetCount(feature, 1.0);
							}
						}
					}
				}
			}
			// surface_distance: Number of tokens in the sentence between the two words, integer-valued feature
			int surfaceDistance = Math.Abs(arg0.GetSyntacticHeadTokenPosition() - arg1.GetSyntacticHeadTokenPosition());
			if (UsingFeature(types, checklist, "surface_distance"))
			{
				features.SetCount("surface_distance", surfaceDistance);
			}
			// surface_distance_binary: Number of tokens in the sentence between the two words, binary features
			if (UsingFeature(types, checklist, "surface_distance_binary"))
			{
				features.SetCount("surface_distance_" + surfaceDistance, 1.0);
			}
			// surface_distance_bins: number of tokens between the two args, binned to several intervals
			if (UsingFeature(types, checklist, "surface_distance_bins"))
			{
				if (surfaceDistance < 4)
				{
					features.SetCount("surface_distance_bin" + surfaceDistance, 1.0);
				}
				else
				{
					if (surfaceDistance < 6)
					{
						features.SetCount("surface_distance_bin_lt6", 1.0);
					}
					else
					{
						if (surfaceDistance < 10)
						{
							features.SetCount("surface_distance_bin_lt10", 1.0);
						}
						else
						{
							features.SetCount("surface_distance_bin_ge10", 1.0);
						}
					}
				}
			}
			// separate_surface_windows: windows of 1,2,3 tokens before and after args, for each arg separately
			// Separate features are generated for windows to the left and to the right of the args.
			// Features are concatenations of words in the window (or NULL for sentence boundary).
			//
			// conjunction_surface_windows: concatenation of the windows of the two args
			//
			// separate_surface_windows_POS: windows of POS tags of size 1,2,3 for each arg
			//
			// conjunction_surface_windows_POS: concatenation of windows of the args
			IList<EntityMention> args = new List<EntityMention>();
			args.Add(arg0);
			args.Add(arg1);
			for (int windowSize = 1; windowSize <= 3; windowSize++)
			{
				string[] leftWindow;
				string[] rightWindow;
				string[] leftWindowPOS;
				string[] rightWindowPOS;
				leftWindow = new string[2];
				rightWindow = new string[2];
				leftWindowPOS = new string[2];
				rightWindowPOS = new string[2];
				for (int argn = 0; argn <= 1; argn++)
				{
					int ind = args[argn].GetSyntacticHeadTokenPosition();
					for (int winnum = 1; winnum <= windowSize; winnum++)
					{
						int windex = ind - winnum;
						if (windex > 0)
						{
							leftWindow[argn] = leaves[windex].Label().Value() + "_" + leftWindow[argn];
							leftWindowPOS[argn] = leaves[windex].Parent(tree).Label().Value() + "_" + leftWindowPOS[argn];
						}
						else
						{
							leftWindow[argn] = "NULL_" + leftWindow[argn];
							leftWindowPOS[argn] = "NULL_" + leftWindowPOS[argn];
						}
						windex = ind + winnum;
						if (windex < leaves.Count)
						{
							rightWindow[argn] = rightWindow[argn] + "_" + leaves[windex].Label().Value();
							rightWindowPOS[argn] = rightWindowPOS[argn] + "_" + leaves[windex].Parent(tree).Label().Value();
						}
						else
						{
							rightWindow[argn] = rightWindow[argn] + "_NULL";
							rightWindowPOS[argn] = rightWindowPOS[argn] + "_NULL";
						}
					}
					if (UsingFeature(types, checklist, "separate_surface_windows"))
					{
						features.SetCount("left_window_" + windowSize + "_arg_" + argn + ": " + leftWindow[argn], 1.0);
						features.SetCount("left_window_" + windowSize + "_POS_arg_" + argn + ": " + leftWindowPOS[argn], 1.0);
					}
					if (UsingFeature(types, checklist, "separate_surface_windows_POS"))
					{
						features.SetCount("right_window_" + windowSize + "_arg_" + argn + ": " + rightWindow[argn], 1.0);
						features.SetCount("right_window_" + windowSize + "_POS_arg_" + argn + ": " + rightWindowPOS[argn], 1.0);
					}
				}
				if (UsingFeature(types, checklist, "conjunction_surface_windows"))
				{
					features.SetCount("left_windows_" + windowSize + ": " + leftWindow[0] + "__" + leftWindow[1], 1.0);
					features.SetCount("right_windows_" + windowSize + ": " + rightWindow[0] + "__" + rightWindow[1], 1.0);
				}
				if (UsingFeature(types, checklist, "conjunction_surface_windows_POS"))
				{
					features.SetCount("left_windows_" + windowSize + "_POS: " + leftWindowPOS[0] + "__" + leftWindowPOS[1], 1.0);
					features.SetCount("right_windows_" + windowSize + "_POS: " + rightWindowPOS[0] + "__" + rightWindowPOS[1], 1.0);
				}
			}
			// arg_words:  The actual arg tokens as separate features, and concatenated
			string word0 = leaves[arg0.GetSyntacticHeadTokenPosition()].Label().Value();
			string word1 = leaves[arg1.GetSyntacticHeadTokenPosition()].Label().Value();
			if (UsingFeature(types, checklist, "arg_words"))
			{
				if (doNotLexicalizeFirstArg == false)
				{
					features.SetCount("word_arg0: " + word0, 1.0);
				}
				features.SetCount("word_arg1: " + word1, 1.0);
				if (doNotLexicalizeFirstArg == false)
				{
					features.SetCount("words: " + word0 + "__" + word1, 1.0);
				}
			}
			// arg_POS:  POS tags of the args, as separate features and concatenated
			string pos0 = leaves[arg0.GetSyntacticHeadTokenPosition()].Parent(tree).Label().Value();
			string pos1 = leaves[arg1.GetSyntacticHeadTokenPosition()].Parent(tree).Label().Value();
			if (UsingFeature(types, checklist, "arg_POS"))
			{
				features.SetCount("POS_arg0: " + pos0, 1.0);
				features.SetCount("POS_arg1: " + pos1, 1.0);
				features.SetCount("POSs: " + pos0 + "__" + pos1, 1.0);
			}
			// adjacent_words: words immediately to the left and right of the args
			if (UsingFeature(types, checklist, "adjacent_words"))
			{
				for (int i = 0; i < rel.GetArgs().Count; i++)
				{
					Span s = ((EntityMention)rel.GetArg(i)).GetHead();
					if (s.Start() > 0)
					{
						string v = tokens[s.Start() - 1].Word();
						features.SetCount("leftarg" + i + "-" + v, 1.0);
					}
					if (s.End() < tokens.Count)
					{
						string v = tokens[s.End()].Word();
						features.SetCount("rightarg" + i + "-" + v, 1.0);
					}
				}
			}
			// entities_between_args:  binary feature for each type specifying whether there is an entity of that type in the sentence
			// between the two args.
			// e.g. "entity_between_args: Loc" means there is at least one entity of type Loc between the two args
			if (UsingFeature(types, checklist, "entities_between_args"))
			{
				ICoreMap sent = rel.GetSentence();
				if (sent == null)
				{
					throw new Exception("NULL sentence for relation " + rel);
				}
				IList<EntityMention> relArgs = sent.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
				if (relArgs != null)
				{
					// may be null due to annotation errors!
					foreach (EntityMention arg in relArgs)
					{
						if ((arg.GetSyntacticHeadTokenPosition() > arg0.GetSyntacticHeadTokenPosition() && arg.GetSyntacticHeadTokenPosition() < arg1.GetSyntacticHeadTokenPosition()) || (arg.GetSyntacticHeadTokenPosition() > arg1.GetSyntacticHeadTokenPosition() && 
							arg.GetSyntacticHeadTokenPosition() < arg0.GetSyntacticHeadTokenPosition()))
						{
							features.SetCount("entity_between_args: " + arg.GetType(), 1.0);
						}
					}
				}
			}
			// entity_counts: For each type, the total number of entities of that type in the sentence (integer-valued feature)
			// entity_counts_binary: Counts of entity types as binary features.
			ICounter<string> typeCounts = new ClassicCounter<string>();
			if (rel.GetSentence().Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation)) != null)
			{
				// may be null due to annotation errors!
				foreach (EntityMention arg in rel.GetSentence().Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation)))
				{
					typeCounts.IncrementCount(arg.GetType());
				}
				foreach (string type in typeCounts.KeySet())
				{
					if (UsingFeature(types, checklist, "entity_counts"))
					{
						features.SetCount("entity_counts_" + type, typeCounts.GetCount(type));
					}
					if (UsingFeature(types, checklist, "entity_counts_binary"))
					{
						features.SetCount("entity_counts_" + type + ": " + typeCounts.GetCount(type), 1.0);
					}
				}
			}
			// surface_path: concatenation of tokens between the two args
			// surface_path_POS: concatenation of POS tags between the args
			// surface_path_selective: concatenation of tokens between the args which are nouns or verbs
			StringBuilder sb = new StringBuilder();
			StringBuilder sbPOS = new StringBuilder();
			StringBuilder sbSelective = new StringBuilder();
			for (int i_1 = Math.Min(arg0.GetSyntacticHeadTokenPosition(), arg1.GetSyntacticHeadTokenPosition()) + 1; i_1 < Math.Max(arg0.GetSyntacticHeadTokenPosition(), arg1.GetSyntacticHeadTokenPosition()); i_1++)
			{
				string word = leaves[i_1].Label().Value();
				sb.Append(word + "_");
				string pos = leaves[i_1].Parent(tree).Label().Value();
				sbPOS.Append(pos + "_");
				if (pos.Equals("NN") || pos.Equals("NNS") || pos.Equals("NNP") || pos.Equals("NNPS") || pos.Equals("VB") || pos.Equals("VBN") || pos.Equals("VBD") || pos.Equals("VBG") || pos.Equals("VBP") || pos.Equals("VBZ"))
				{
					sbSelective.Append(word + "_");
				}
			}
			if (UsingFeature(types, checklist, "surface_path"))
			{
				features.SetCount("surface_path: " + sb, 1.0);
			}
			if (UsingFeature(types, checklist, "surface_path_POS"))
			{
				features.SetCount("surface_path_POS: " + sbPOS, 1.0);
			}
			if (UsingFeature(types, checklist, "surface_path_selective"))
			{
				features.SetCount("surface_path_selective: " + sbSelective, 1.0);
			}
			int swStart;
			int swEnd;
			// must be initialized below
			if (arg0.GetSyntacticHeadTokenPosition() < arg1.GetSyntacticHeadTokenPosition())
			{
				swStart = arg0.GetExtentTokenEnd();
				swEnd = arg1.GetExtentTokenStart();
			}
			else
			{
				swStart = arg1.GetExtentTokenEnd();
				swEnd = arg0.GetExtentTokenStart();
			}
			// span_words_unigrams: words that appear in between the two arguments
			if (UsingFeature(types, checklist, "span_words_unigrams"))
			{
				for (int i = swStart; i_1 < swEnd; i_1++)
				{
					features.SetCount("span_word:" + tokens[i_1].Word(), 1.0);
				}
			}
			// span_words_bigrams: bigrams of words that appear in between the two arguments
			if (UsingFeature(types, checklist, "span_words_bigrams"))
			{
				for (int i = swStart; i_1 < swEnd - 1; i_1++)
				{
					features.SetCount("span_bigram:" + tokens[i_1].Word() + "-" + tokens[i_1 + 1].Word(), 1.0);
				}
			}
			if (UsingFeature(types, checklist, "span_words_trigger"))
			{
				for (int i = swStart; i_1 < swEnd; i_1++)
				{
					string trigger = tokens[i_1].Get(typeof(MachineReadingAnnotations.TriggerAnnotation));
					if (trigger != null && trigger.StartsWith("B-"))
					{
						features.IncrementCount("span_words_trigger=" + Sharpen.Runtime.Substring(trigger, 2));
					}
				}
			}
			if (UsingFeature(types, checklist, "arg2_number"))
			{
				if (arg1.GetType().Equals("NUMBER"))
				{
					try
					{
						int value = System.Convert.ToInt32(arg1.GetValue());
						if (2 <= value && value <= 100)
						{
							features.SetCount("arg2_number", 1.0);
						}
						if (2 <= value && value <= 19)
						{
							features.SetCount("arg2_number_2", 1.0);
						}
						if (20 <= value && value <= 59)
						{
							features.SetCount("arg2_number_20", 1.0);
						}
						if (60 <= value && value <= 100)
						{
							features.SetCount("arg2_number_60", 1.0);
						}
						if (value >= 100)
						{
							features.SetCount("arg2_number_100", 1.0);
						}
					}
					catch (NumberFormatException)
					{
					}
				}
			}
			if (UsingFeature(types, checklist, "arg2_date"))
			{
				if (arg1.GetType().Equals("DATE"))
				{
					try
					{
						int value = System.Convert.ToInt32(arg1.GetValue());
						if (0 <= value && value <= 2010)
						{
							features.SetCount("arg2_date", 1.0);
						}
						if (0 <= value && value <= 999)
						{
							features.SetCount("arg2_date_0", 1.0);
						}
						if (1000 <= value && value <= 1599)
						{
							features.SetCount("arg2_date_1000", 1.0);
						}
						if (1600 <= value && value <= 1799)
						{
							features.SetCount("arg2_date_1600", 1.0);
						}
						if (1800 <= value && value <= 1899)
						{
							features.SetCount("arg2_date_1800", 1.0);
						}
						if (1900 <= value && value <= 1999)
						{
							features.SetCount("arg2_date_1900", 1.0);
						}
						if (value >= 2000)
						{
							features.SetCount("arg2_date_2000", 1.0);
						}
					}
					catch (NumberFormatException)
					{
					}
				}
			}
			if (UsingFeature(types, checklist, "arg_gender"))
			{
				bool arg0Male = false;
				bool arg0Female = false;
				bool arg1Male = false;
				bool arg1Female = false;
				System.Console.Out.WriteLine("Adding gender annotations!");
				int index = arg0.GetExtentTokenStart();
				string gender = tokens[index].Get(typeof(MachineReadingAnnotations.GenderAnnotation));
				System.Console.Out.WriteLine(tokens[index].Word() + " -- " + gender);
				if (gender.Equals("MALE"))
				{
					arg0Male = true;
				}
				else
				{
					if (gender.Equals("FEMALE"))
					{
						arg0Female = true;
					}
				}
				index = arg1.GetExtentTokenStart();
				gender = tokens[index].Get(typeof(MachineReadingAnnotations.GenderAnnotation));
				if (gender.Equals("MALE"))
				{
					arg1Male = true;
				}
				else
				{
					if (gender.Equals("FEMALE"))
					{
						arg1Female = true;
					}
				}
				if (arg0Male)
				{
					features.SetCount("arg1_male", 1.0);
				}
				if (arg0Female)
				{
					features.SetCount("arg1_female", 1.0);
				}
				if (arg1Male)
				{
					features.SetCount("arg2_male", 1.0);
				}
				if (arg1Female)
				{
					features.SetCount("arg2_female", 1.0);
				}
				if ((arg0Male && arg1Male) || (arg0Female && arg1Female))
				{
					features.SetCount("arg_same_gender", 1.0);
				}
				if ((arg0Male && arg1Female) || (arg0Female && arg1Male))
				{
					features.SetCount("arg_different_gender", 1.0);
				}
			}
			IList<string> tempDepFeatures = new List<string>(dependencyFeatures);
			if (tempDepFeatures.RemoveAll(types) || types.Contains("all"))
			{
				// dependencyFeatures contains at least one of the features listed in types
				AddDependencyPathFeatures(features, rel, arg0, arg1, types, checklist, logger);
			}
			if (!checklist.IsEmpty() && !checklist.Contains("all"))
			{
				throw new AssertionError("RelationFeatureFactory: features not handled: " + checklist);
			}
			IList<string> featureList = new List<string>(features.KeySet());
			featureList.Sort();
			//    for (String feature : featureList) {
			//      logger.info(feature+"\n"+"count="+features.getCount(feature));
			//    }
			return true;
		}

		internal virtual string SentToString(ICoreMap sentence)
		{
			StringBuilder os = new StringBuilder();
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			if (tokens != null)
			{
				bool first = true;
				foreach (CoreLabel token in tokens)
				{
					if (!first)
					{
						os.Append(" ");
					}
					os.Append(token.Word());
					first = false;
				}
			}
			return os.ToString();
		}

		protected internal virtual void AddDependencyPathFeatures(ICounter<string> features, RelationMention rel, EntityMention arg0, EntityMention arg1, IList<string> types, IList<string> checklist, Logger logger)
		{
			SemanticGraph graph = null;
			if (dependencyType == null)
			{
				dependencyType = RelationFeatureFactory.DEPENDENCY_TYPE.CollapsedCcprocessed;
			}
			// needed for backwards compatibility. old serialized models don't have it
			if (dependencyType == RelationFeatureFactory.DEPENDENCY_TYPE.CollapsedCcprocessed)
			{
				graph = rel.GetSentence().Get(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation));
			}
			else
			{
				if (dependencyType == RelationFeatureFactory.DEPENDENCY_TYPE.Collapsed)
				{
					graph = rel.GetSentence().Get(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation));
				}
				else
				{
					if (dependencyType == RelationFeatureFactory.DEPENDENCY_TYPE.Basic)
					{
						graph = rel.GetSentence().Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
					}
					else
					{
						throw new Exception("ERROR: unknown dependency type: " + dependencyType);
					}
				}
			}
			if (graph == null)
			{
				Tree tree = rel.GetSentence().Get(typeof(TreeCoreAnnotations.TreeAnnotation));
				if (tree == null)
				{
					log.Info("WARNING: found sentence without TreeAnnotation. Skipped dependency-path features.");
					return;
				}
				try
				{
					graph = SemanticGraphFactory.MakeFromTree(tree, SemanticGraphFactory.Mode.Collapsed, GrammaticalStructure.Extras.None, null, true);
				}
				catch (Exception e)
				{
					log.Info("WARNING: failed to generate dependencies from tree " + tree.ToString());
					Sharpen.Runtime.PrintStackTrace(e);
					log.Info("Skipped dependency-path features.");
					return;
				}
			}
			IndexedWord node0 = graph.GetNodeByIndexSafe(arg0.GetSyntacticHeadTokenPosition() + 1);
			IndexedWord node1 = graph.GetNodeByIndexSafe(arg1.GetSyntacticHeadTokenPosition() + 1);
			if (node0 == null)
			{
				checklist.RemoveAll(dependencyFeatures);
				return;
			}
			if (node1 == null)
			{
				checklist.RemoveAll(dependencyFeatures);
				return;
			}
			IList<SemanticGraphEdge> edgePath = graph.GetShortestUndirectedPathEdges(node0, node1);
			IList<IndexedWord> pathNodes = graph.GetShortestUndirectedPathNodes(node0, node1);
			if (edgePath == null)
			{
				checklist.RemoveAll(dependencyFeatures);
				return;
			}
			if (pathNodes == null || pathNodes.Count <= 1)
			{
				// arguments have the same head.
				checklist.RemoveAll(dependencyFeatures);
				return;
			}
			// dependency_path: Concatenation of relations in the path between the args in the dependency graph, including directions
			// e.g. "subj->  <-prep_in  <-mod"
			// dependency_path_lowlevel: Same but with finer-grained syntactic relations
			// e.g. "nsubj->  <-prep_in  <-nn"
			if (UsingFeature(types, checklist, "dependency_path"))
			{
				features.SetCount("dependency_path:" + GeneralizedDependencyPath(edgePath, node0), 1.0);
			}
			if (UsingFeature(types, checklist, "dependency_path_lowlevel"))
			{
				string depLowLevel = DependencyPath(edgePath, node0);
				if (logger != null && !rel.GetType().Equals(RelationMention.Unrelated))
				{
					logger.Info("dependency_path_lowlevel: " + depLowLevel);
				}
				features.SetCount("dependency_path_lowlevel:" + depLowLevel, 1.0);
			}
			IList<string> pathLemmas = new List<string>();
			IList<string> noArgPathLemmas = new List<string>();
			// do not add to pathLemmas words that belong to one of the two args
			ICollection<int> indecesToSkip = new HashSet<int>();
			for (int i = arg0.GetExtentTokenStart(); i < arg0.GetExtentTokenEnd(); i++)
			{
				indecesToSkip.Add(i + 1);
			}
			for (int i_1 = arg1.GetExtentTokenStart(); i_1 < arg1.GetExtentTokenEnd(); i_1++)
			{
				indecesToSkip.Add(i_1 + 1);
			}
			foreach (IndexedWord node in pathNodes)
			{
				pathLemmas.Add(Morphology.LemmaStatic(node.Value(), node.Tag(), true));
				if (!indecesToSkip.Contains(node.Index()))
				{
					noArgPathLemmas.Add(Morphology.LemmaStatic(node.Value(), node.Tag(), true));
				}
			}
			// Verb-based features
			// These features were designed on the assumption that verbs are often trigger words
			// (specifically with the "Kill" relation from Roth CONLL04 in mind)
			// but they didn't end up boosting performance on Roth CONLL04, so they may not be necessary.
			//
			// dependency_paths_to_verb: for each verb in the dependency path,
			// the path to the left of the (lemmatized) verb, to the right, and both, e.g.
			// "subj-> be"
			// "be  <-prep_in  <-mod"
			// "subj->  be  <-prep_in  <-mod"
			// (Higher level relations used as opposed to "lowlevel" finer grained relations)
			if (UsingFeature(types, checklist, "dependency_paths_to_verb"))
			{
				foreach (IndexedWord node_1 in pathNodes)
				{
					if (node_1.Tag().Contains("VB"))
					{
						if (node_1.Equals(node0) || node_1.Equals(node1))
						{
							continue;
						}
						string lemma = Morphology.LemmaStatic(node_1.Value(), node_1.Tag(), true);
						string node1Path = GeneralizedDependencyPath(graph.GetShortestUndirectedPathEdges(node_1, node1), node_1);
						string node0Path = GeneralizedDependencyPath(graph.GetShortestUndirectedPathEdges(node0, node_1), node0);
						features.SetCount("dependency_paths_to_verb:" + node0Path + " " + lemma, 1.0);
						features.SetCount("dependency_paths_to_verb:" + lemma + " " + node1Path, 1.0);
						features.SetCount("dependency_paths_to_verb:" + node0Path + " " + lemma + " " + node1Path, 1.0);
					}
				}
			}
			// dependency_path_stubs_to_verb:
			// For each verb in the dependency path,
			// the verb concatenated with the first (high-level) relation in the path from arg0;
			// the verb concatenated with the first relation in the path from arg1,
			// and the verb concatenated with both relations.  E.g. (same arguments and sentence as example above)
			// "stub: subj->  be"
			// "stub: be  <-mod"
			// "stub: subj->  be  <-mod"
			if (UsingFeature(types, checklist, "dependency_path_stubs_to_verb"))
			{
				foreach (IndexedWord node_1 in pathNodes)
				{
					SemanticGraphEdge edge0 = edgePath[0];
					SemanticGraphEdge edge1 = edgePath[edgePath.Count - 1];
					if (node_1.Tag().Contains("VB"))
					{
						if (node_1.Equals(node0) || node_1.Equals(node1))
						{
							continue;
						}
						string lemma = Morphology.LemmaStatic(node_1.Value(), node_1.Tag(), true);
						string edge0str;
						string edge1str;
						if (node0.Equals(edge0.GetGovernor()))
						{
							edge0str = "<-" + GeneralizeRelation(edge0.GetRelation());
						}
						else
						{
							edge0str = GeneralizeRelation(edge0.GetRelation()) + "->";
						}
						if (node1.Equals(edge1.GetGovernor()))
						{
							edge1str = GeneralizeRelation(edge1.GetRelation()) + "->";
						}
						else
						{
							edge1str = "<-" + GeneralizeRelation(edge1.GetRelation());
						}
						features.SetCount("stub: " + edge0str + " " + lemma, 1.0);
						features.SetCount("stub: " + lemma + edge1str, 1.0);
						features.SetCount("stub: " + edge0str + " " + lemma + " " + edge1str, 1.0);
					}
				}
			}
			if (UsingFeature(types, checklist, "verb_in_dependency_path"))
			{
				foreach (IndexedWord node_1 in pathNodes)
				{
					if (node_1.Tag().Contains("VB"))
					{
						if (node_1.Equals(node0) || node_1.Equals(node1))
						{
							continue;
						}
						SemanticGraphEdge rightEdge = graph.GetShortestUndirectedPathEdges(node_1, node1)[0];
						SemanticGraphEdge leftEdge = graph.GetShortestUndirectedPathEdges(node_1, node0)[0];
						string rightRelation;
						string leftRelation;
						bool governsLeft = false;
						bool governsRight = false;
						if (node_1.Equals(rightEdge.GetGovernor()))
						{
							rightRelation = " <-" + GeneralizeRelation(rightEdge.GetRelation());
							governsRight = true;
						}
						else
						{
							rightRelation = GeneralizeRelation(rightEdge.GetRelation()) + "-> ";
						}
						if (node_1.Equals(leftEdge.GetGovernor()))
						{
							leftRelation = GeneralizeRelation(leftEdge.GetRelation()) + "-> ";
							governsLeft = true;
						}
						else
						{
							leftRelation = " <-" + GeneralizeRelation(leftEdge.GetRelation());
						}
						string lemma = Morphology.LemmaStatic(node_1.Value(), node_1.Tag(), true);
						if (governsLeft || governsRight)
						{
						}
						if (governsLeft)
						{
							features.SetCount("verb: " + leftRelation + lemma, 1.0);
						}
						if (governsRight)
						{
							features.SetCount("verb: " + lemma + rightRelation, 1.0);
						}
						if (governsLeft && governsRight)
						{
							features.SetCount("verb: " + leftRelation + lemma + rightRelation, 1.0);
						}
					}
				}
			}
			// FEATURES FROM BJORNE ET AL., BIONLP'09
			// dependency_path_words: generates a feature for each word in the dependency path (lemmatized)
			// dependency_path_POS_unigrams: generates a feature for the POS tag of each word in the dependency path
			if (UsingFeature(types, checklist, "dependency_path_words"))
			{
				foreach (string lemma in noArgPathLemmas)
				{
					features.SetCount("word_in_dependency_path:" + lemma, 1.0);
				}
			}
			if (UsingFeature(types, checklist, "dependency_path_POS_unigrams"))
			{
				foreach (IndexedWord node_1 in pathNodes)
				{
					if (!node_1.Equals(node0) && !node_1.Equals(node1))
					{
						features.SetCount("POS_in_dependency_path: " + node_1.Tag(), 1.0);
					}
				}
			}
			// dependency_path_word_n_grams: n-grams of words (lemmatized) in the dependency path, n=2,3,4
			// dependency_path_POS_n_grams: n-grams of POS tags of words in the dependency path, n=2,3,4
			for (int node_2 = 0; node_2 < pathNodes.Count; node_2++)
			{
				for (int n = 2; n <= 4; n++)
				{
					if (node_2 + n > pathNodes.Count)
					{
						break;
					}
					StringBuilder sb = new StringBuilder();
					StringBuilder sbPOS = new StringBuilder();
					for (int elt = node_2; elt < node_2 + n; elt++)
					{
						sb.Append(pathLemmas[elt]);
						sb.Append("_");
						sbPOS.Append(pathNodes[elt].Tag());
						sbPOS.Append("_");
					}
					if (UsingFeature(types, checklist, "dependency_path_word_n_grams"))
					{
						features.SetCount("dependency_path_" + n + "-gram: " + sb, 1.0);
					}
					if (UsingFeature(types, checklist, "dependency_path_POS_n_grams"))
					{
						features.SetCount("dependency_path_POS_" + n + "-gram: " + sbPOS, 1.0);
					}
				}
			}
			// dependency_path_edge_n_grams: n_grams of relations (high-level) in the dependency path, undirected, n=2,3,4
			// e.g. "subj -- prep_in -- mod"
			// dependency_path_edge_lowlevel_n_grams: similar, for fine-grained relations
			//
			// dependency_path_node-edge-node-grams: trigrams consisting of adjacent words (lemmatized) in the dependency path
			// and the relation between them (undirected)
			// dependency_path_node-edge-node-grams_lowlevel: same, using fine-grained relations
			//
			// dependency_path_edge-node-edge-grams: trigrams consisting of words (lemmatized) in the dependency path
			// and the incoming and outgoing relations (undirected)
			// e.g. "subj -- television -- mod"
			// dependency_path_edge-node-edge-grams_lowlevel: same, using fine-grained relations
			//
			// dependency_path_directed_bigrams: consecutive words in the dependency path (lemmatized) and the direction
			// of the dependency between them
			// e.g. "Theatre -> exhibit"
			//
			// dependency_path_edge_unigrams: feature for each (fine-grained) relation in the dependency path,
			// with its direction in the path and whether it's at the left end, right end, or interior of the path.
			// e.g. "prep_at ->  - leftmost"
			for (int edge = 0; edge < edgePath.Count; edge++)
			{
				if (UsingFeature(types, checklist, "dependency_path_edge_n_grams") || UsingFeature(types, checklist, "dependency_path_edge_lowlevel_n_grams"))
				{
					for (int n = 2; n <= 4; n++)
					{
						if (edge + n > edgePath.Count)
						{
							break;
						}
						StringBuilder sbRelsHi = new StringBuilder();
						StringBuilder sbRelsLo = new StringBuilder();
						for (int elt = edge; elt < edge + n; elt++)
						{
							GrammaticalRelation gr = edgePath[elt].GetRelation();
							sbRelsHi.Append(GeneralizeRelation(gr));
							sbRelsHi.Append("_");
							sbRelsLo.Append(gr);
							sbRelsLo.Append("_");
						}
						if (UsingFeature(types, checklist, "dependency_path_edge_n_grams"))
						{
							features.SetCount("dependency_path_edge_" + n + "-gram: " + sbRelsHi, 1.0);
						}
						if (UsingFeature(types, checklist, "dependency_path_edge_lowlevel_n_grams"))
						{
							features.SetCount("dependency_path_edge_lowlevel_" + n + "-gram: " + sbRelsLo, 1.0);
						}
					}
				}
				if (UsingFeature(types, checklist, "dependency_path_node-edge-node-grams"))
				{
					features.SetCount("dependency_path_node-edge-node-gram: " + pathLemmas[edge] + " -- " + GeneralizeRelation(edgePath[edge].GetRelation()) + " -- " + pathLemmas[edge + 1], 1.0);
				}
				if (UsingFeature(types, checklist, "dependency_path_node-edge-node-grams_lowlevel"))
				{
					features.SetCount("dependency_path_node-edge-node-gram_lowlevel: " + pathLemmas[edge] + " -- " + edgePath[edge].GetRelation() + " -- " + pathLemmas[edge + 1], 1.0);
				}
				if (UsingFeature(types, checklist, "dependency_path_edge-node-edge-grams") && edge > 0)
				{
					features.SetCount("dependency_path_edge-node-edge-gram: " + GeneralizeRelation(edgePath[edge - 1].GetRelation()) + " -- " + pathLemmas[edge] + " -- " + GeneralizeRelation(edgePath[edge].GetRelation()), 1.0);
				}
				if (UsingFeature(types, checklist, "dependency_path_edge-node-edge-grams_lowlevel") && edge > 0)
				{
					features.SetCount("dependency_path_edge-node-edge-gram_lowlevel: " + edgePath[edge - 1].GetRelation() + " -- " + pathLemmas[edge] + " -- " + edgePath[edge].GetRelation(), 1.0);
				}
				string dir = pathNodes[edge].Equals(edgePath[edge].GetDependent()) ? " -> " : " <- ";
				if (UsingFeature(types, checklist, "dependency_path_directed_bigrams"))
				{
					features.SetCount("dependency_path_directed_bigram: " + pathLemmas[edge] + dir + pathLemmas[edge + 1], 1.0);
				}
				if (UsingFeature(types, checklist, "dependency_path_edge_unigrams"))
				{
					features.SetCount("dependency_path_edge_unigram: " + edgePath[edge].GetRelation() + dir + (edge == 0 ? " - leftmost" : edge == edgePath.Count - 1 ? " - rightmost" : " - interior"), 1.0);
				}
			}
			// dependency_path_length: number of edges in the path between args in the dependency graph, integer-valued
			// dependency_path_length_binary: same, as binary features
			if (UsingFeature(types, checklist, "dependency_path_length"))
			{
				features.SetCount("dependency_path_length", edgePath.Count);
			}
			if (UsingFeature(types, checklist, "dependency_path_length_binary"))
			{
				features.SetCount("dependency_path_length_" + new DecimalFormat("00").Format(edgePath.Count), 1.0);
			}
			if (UsingFeature(types, checklist, "dependency_path_trigger"))
			{
				IList<CoreLabel> tokens = rel.GetSentence().Get(typeof(CoreAnnotations.TokensAnnotation));
				foreach (IndexedWord node_1 in pathNodes)
				{
					int index = node_1.Index();
					if (indecesToSkip.Contains(index))
					{
						continue;
					}
					string trigger = tokens[index - 1].Get(typeof(MachineReadingAnnotations.TriggerAnnotation));
					if (trigger != null && trigger.StartsWith("B-"))
					{
						features.IncrementCount("dependency_path_trigger=" + Sharpen.Runtime.Substring(trigger, 2));
					}
				}
			}
		}

		/// <summary>
		/// Helper method that checks if a feature type "type" is present in the list of features "types"
		/// and removes it from "checklist"
		/// </summary>
		/// <param name="types"/>
		/// <param name="checklist"/>
		/// <param name="type"/>
		/// <returns>true if types contains type</returns>
		protected internal static bool UsingFeature(IList<string> types, IList<string> checklist, string type)
		{
			checklist.Remove(type);
			return types.Contains(type) || types.Contains("all");
		}

		protected internal static GrammaticalRelation GeneralizeRelation(GrammaticalRelation gr)
		{
			GrammaticalRelation[] GeneralRelations = new GrammaticalRelation[] { EnglishGrammaticalRelations.Subject, EnglishGrammaticalRelations.Complement, EnglishGrammaticalRelations.Conjunct, EnglishGrammaticalRelations.Modifier };
			foreach (GrammaticalRelation generalGR in GeneralRelations)
			{
				if (generalGR.IsAncestor(gr))
				{
					return generalGR;
				}
			}
			return gr;
		}

		/*
		* Under construction
		*/
		public static IList<string> DependencyPathAsList(IList<SemanticGraphEdge> edgePath, IndexedWord node, bool generalize)
		{
			if (edgePath == null)
			{
				return null;
			}
			IList<string> path = new List<string>();
			foreach (SemanticGraphEdge edge in edgePath)
			{
				IndexedWord nextNode;
				GrammaticalRelation relation;
				if (generalize)
				{
					relation = GeneralizeRelation(edge.GetRelation());
				}
				else
				{
					relation = edge.GetRelation();
				}
				if (node.Equals(edge.GetDependent()))
				{
					string v = string.Intern((relation + "->"));
					path.Add(v);
					nextNode = edge.GetGovernor();
				}
				else
				{
					string v = string.Intern(("<-" + relation));
					path.Add(v);
					nextNode = edge.GetDependent();
				}
				node = nextNode;
			}
			return path;
		}

		public static string DependencyPath(IList<SemanticGraphEdge> edgePath, IndexedWord node)
		{
			// the extra spaces are to maintain compatibility with existing relation extraction models
			return " " + StringUtils.Join(DependencyPathAsList(edgePath, node, false), "  ") + " ";
		}

		public static string GeneralizedDependencyPath(IList<SemanticGraphEdge> edgePath, IndexedWord node)
		{
			// the extra spaces are to maintain compatibility with existing relation extraction models
			return " " + StringUtils.Join(DependencyPathAsList(edgePath, node, true), "  ") + " ";
		}

		public override ICollection<string> GetFeatures(RelationMention rel, string featureType)
		{
			ICounter<string> features = new ClassicCounter<string>();
			IList<string> singleton = new List<string>();
			singleton.Add(featureType);
			AddFeatures(features, rel, singleton);
			return features.KeySet();
		}

		public override string GetFeature(RelationMention rel, string featureType)
		{
			ICollection<string> features = GetFeatures(rel, featureType);
			if (features.Count == 0)
			{
				return string.Empty;
			}
			else
			{
				return features.GetEnumerator().Current;
			}
		}
	}
}
