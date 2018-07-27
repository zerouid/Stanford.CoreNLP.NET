// Stanford Parser -- a probabilistic lexicalized NL CFG parser
// Copyright (c) 2002 - 2014 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/ .
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 2A
//    Stanford CA 94305-9020
//    USA
//    parser-support@lists.stanford.edu
//    https://nlp.stanford.edu/software/srparser.html
using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Parser.Metrics;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>A shift-reduce constituency parser.</summary>
	/// <remarks>
	/// A shift-reduce constituency parser.
	/// Overview and description available at
	/// import edu.stanford.nlp.util.logging.Redwood;
	/// </remarks>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class ShiftReduceParser : ParserGrammar
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Shiftreduce.ShiftReduceParser));

		internal readonly ShiftReduceOptions op;

		internal BaseModel model;

		public ShiftReduceParser(ShiftReduceOptions op)
			: this(op, null)
		{
		}

		public ShiftReduceParser(ShiftReduceOptions op, BaseModel model)
		{
			this.op = op;
			this.model = model;
		}

		/*
		private void readObject(ObjectInputStream in)
		throws IOException, ClassNotFoundException
		{
		ObjectInputStream.GetField fields = in.readFields();
		op = ErasureUtils.uncheckedCast(fields.get("op", null));
		
		Index<Transition> transitionIndex = ErasureUtils.uncheckedCast(fields.get("transitionIndex", null));
		Set<String> knownStates = ErasureUtils.uncheckedCast(fields.get("knownStates", null));
		Set<String> rootStates = ErasureUtils.uncheckedCast(fields.get("rootStates", null));
		Set<String> rootOnlyStates = ErasureUtils.uncheckedCast(fields.get("rootOnlyStates", null));
		
		FeatureFactory featureFactory = ErasureUtils.uncheckedCast(fields.get("featureFactory", null));
		Map<String, Weight> featureWeights = ErasureUtils.uncheckedCast(fields.get("featureWeights", null));
		this.model = new PerceptronModel(op, transitionIndex, knownStates, rootStates, rootOnlyStates, featureFactory, featureWeights);
		}
		*/
		public override Options GetOp()
		{
			return op;
		}

		public override ITreebankLangParserParams GetTLPParams()
		{
			return op.tlpParams;
		}

		public override ITreebankLanguagePack TreebankLanguagePack()
		{
			return GetTLPParams().TreebankLanguagePack();
		}

		private static readonly string[] BeamFlags = new string[] { "-beamSize", "4" };

		public override string[] DefaultCoreNLPFlags()
		{
			if (op.TrainOptions().beamSize > 1)
			{
				return ArrayUtils.Concatenate(GetTLPParams().DefaultCoreNLPFlags(), BeamFlags);
			}
			else
			{
				// TODO: this may result in some options which are useless for
				// this model, such as -retainTmpSubcategories
				return GetTLPParams().DefaultCoreNLPFlags();
			}
		}

		/// <summary>Return an unmodifiableSet containing the known states (including binarization)</summary>
		public virtual ICollection<string> KnownStates()
		{
			return Java.Util.Collections.UnmodifiableSet(model.knownStates);
		}

		/// <summary>Return the Set of POS tags used in the model.</summary>
		public virtual ICollection<string> TagSet()
		{
			return model.TagSet();
		}

		public override bool RequiresTags()
		{
			return true;
		}

		public override IParserQuery ParserQuery()
		{
			return new ShiftReduceParserQuery(this);
		}

		public override Tree Parse(string sentence)
		{
			if (!GetOp().testOptions.preTag)
			{
				throw new NotSupportedException("Can only parse raw text if a tagger is specified, as the ShiftReduceParser cannot produce its own tags");
			}
			return base.Parse(sentence);
		}

		public override Tree Parse<_T0>(IList<_T0> sentence)
		{
			ShiftReduceParserQuery pq = new ShiftReduceParserQuery(this);
			if (pq.Parse(sentence))
			{
				return pq.GetBestParse();
			}
			return ParserUtils.XTree(sentence);
		}

		/// <summary>TODO: add an eval which measures transition accuracy?</summary>
		public override IList<IEval> GetExtraEvals()
		{
			return Java.Util.Collections.EmptyList();
		}

		public override IList<IParserQueryEval> GetParserQueryEvals()
		{
			if (op.TestOptions().recordBinarized == null && op.TestOptions().recordDebinarized == null)
			{
				return Java.Util.Collections.EmptyList();
			}
			IList<IParserQueryEval> evals = Generics.NewArrayList();
			if (op.TestOptions().recordBinarized != null)
			{
				evals.Add(new TreeRecorder(TreeRecorder.Mode.Binarized, op.TestOptions().recordBinarized));
			}
			if (op.TestOptions().recordDebinarized != null)
			{
				evals.Add(new TreeRecorder(TreeRecorder.Mode.Debinarized, op.TestOptions().recordDebinarized));
			}
			return evals;
		}

		public static State InitialStateFromGoldTagTree(Tree tree)
		{
			return InitialStateFromTaggedSentence(tree.TaggedYield());
		}

		public static State InitialStateFromTaggedSentence<_T0>(IList<_T0> words)
			where _T0 : IHasWord
		{
			IList<Tree> preterminals = Generics.NewArrayList();
			for (int index = 0; index < words.Count; ++index)
			{
				IHasWord hw = words[index];
				CoreLabel wordLabel;
				string tag;
				if (hw is CoreLabel)
				{
					wordLabel = (CoreLabel)hw;
					tag = wordLabel.Tag();
				}
				else
				{
					wordLabel = new CoreLabel();
					wordLabel.SetValue(hw.Word());
					wordLabel.SetWord(hw.Word());
					if (!(hw is IHasTag))
					{
						throw new ArgumentException("Expected tagged words");
					}
					tag = ((IHasTag)hw).Tag();
					wordLabel.SetTag(tag);
				}
				if (tag == null)
				{
					throw new ArgumentException("Input word not tagged");
				}
				CoreLabel tagLabel = new CoreLabel();
				tagLabel.SetValue(tag);
				// Index from 1.  Tools downstream from the parser expect that
				// Internally this parser uses the index, so we have to
				// overwrite incorrect indices if the label is already indexed
				wordLabel.SetIndex(index + 1);
				tagLabel.SetIndex(index + 1);
				LabeledScoredTreeNode wordNode = new LabeledScoredTreeNode(wordLabel);
				LabeledScoredTreeNode tagNode = new LabeledScoredTreeNode(tagLabel);
				tagNode.AddChild(wordNode);
				// TODO: can we get away with not setting these on the wordLabel?
				wordLabel.Set(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation), wordLabel);
				wordLabel.Set(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation), tagLabel);
				tagLabel.Set(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation), wordLabel);
				tagLabel.Set(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation), tagLabel);
				preterminals.Add(tagNode);
			}
			return new State(preterminals);
		}

		public static ShiftReduceOptions BuildTrainingOptions(string tlppClass, string[] args)
		{
			ShiftReduceOptions op = new ShiftReduceOptions();
			op.SetOptions("-forceTags", "-debugOutputFrequency", "1", "-quietEvaluation");
			if (tlppClass != null)
			{
				op.tlpParams = ReflectionLoading.LoadByReflection(tlppClass);
			}
			op.SetOptions(args);
			if (op.trainOptions.randomSeed == 0)
			{
				op.trainOptions.randomSeed = Runtime.NanoTime();
				log.Info("Random seed not set by options, using " + op.trainOptions.randomSeed);
			}
			return op;
		}

		public virtual Treebank ReadTreebank(string treebankPath, IFileFilter treebankFilter)
		{
			log.Info("Loading trees from " + treebankPath);
			Treebank treebank = op.tlpParams.MemoryTreebank();
			treebank.LoadPath(treebankPath, treebankFilter);
			log.Info("Read in " + treebank.Count + " trees from " + treebankPath);
			return treebank;
		}

		public virtual IList<Tree> ReadBinarizedTreebank(string treebankPath, IFileFilter treebankFilter)
		{
			Treebank treebank = ReadTreebank(treebankPath, treebankFilter);
			IList<Tree> binarized = BinarizeTreebank(treebank, op);
			log.Info("Converted trees to binarized format");
			return binarized;
		}

		public static IList<Tree> BinarizeTreebank(Treebank treebank, Options op)
		{
			TreeBinarizer binarizer = TreeBinarizer.SimpleTreeBinarizer(op.tlpParams.HeadFinder(), op.tlpParams.TreebankLanguagePack());
			BasicCategoryTreeTransformer basicTransformer = new BasicCategoryTreeTransformer(op.Langpack());
			CompositeTreeTransformer transformer = new CompositeTreeTransformer();
			transformer.AddTransformer(binarizer);
			transformer.AddTransformer(basicTransformer);
			treebank = treebank.Transform(transformer);
			IHeadFinder binaryHeadFinder = new BinaryHeadFinder(op.tlpParams.HeadFinder());
			IList<Tree> binarizedTrees = Generics.NewArrayList();
			foreach (Tree tree in treebank)
			{
				Edu.Stanford.Nlp.Trees.Trees.ConvertToCoreLabels(tree);
				tree.PercolateHeadAnnotations(binaryHeadFinder);
				// Index from 1.  Tools downstream expect index from 1, so for
				// uses internal to the srparser we have to renormalize the
				// indices, with the result that here we have to index from 1
				tree.IndexLeaves(1, true);
				binarizedTrees.Add(tree);
			}
			return binarizedTrees;
		}

		public static ICollection<string> FindKnownStates(IList<Tree> binarizedTrees)
		{
			ICollection<string> knownStates = Generics.NewHashSet();
			foreach (Tree tree in binarizedTrees)
			{
				FindKnownStates(tree, knownStates);
			}
			return Java.Util.Collections.UnmodifiableSet(knownStates);
		}

		public static void FindKnownStates(Tree tree, ICollection<string> knownStates)
		{
			if (tree.IsLeaf() || tree.IsPreTerminal())
			{
				return;
			}
			if (!ShiftReduceUtils.IsTemporary(tree))
			{
				knownStates.Add(tree.Value());
			}
			foreach (Tree child in tree.Children())
			{
				FindKnownStates(child, knownStates);
			}
		}

		// TODO: factor out the retagging?
		public static void RedoTags(Tree tree, Edu.Stanford.Nlp.Tagger.Common.Tagger tagger)
		{
			IList<Word> words = tree.YieldWords();
			IList<TaggedWord> tagged = tagger.Apply(words);
			IList<ILabel> tags = tree.PreTerminalYield();
			if (tags.Count != tagged.Count)
			{
				throw new AssertionError("Tags are not the same size");
			}
			for (int i = 0; i < tags.Count; ++i)
			{
				tags[i].SetValue(tagged[i].Tag());
			}
		}

		private class RetagProcessor : IThreadsafeProcessor<Tree, Tree>
		{
			internal Edu.Stanford.Nlp.Tagger.Common.Tagger tagger;

			public RetagProcessor(Edu.Stanford.Nlp.Tagger.Common.Tagger tagger)
			{
				this.tagger = tagger;
			}

			public virtual Tree Process(Tree tree)
			{
				RedoTags(tree, tagger);
				return tree;
			}

			public virtual ShiftReduceParser.RetagProcessor NewInstance()
			{
				// already threadsafe
				return this;
			}
		}

		public static void RedoTags(IList<Tree> trees, Edu.Stanford.Nlp.Tagger.Common.Tagger tagger, int nThreads)
		{
			if (nThreads == 1)
			{
				foreach (Tree tree in trees)
				{
					RedoTags(tree, tagger);
				}
			}
			else
			{
				MulticoreWrapper<Tree, Tree> wrapper = new MulticoreWrapper<Tree, Tree>(nThreads, new ShiftReduceParser.RetagProcessor(tagger));
				foreach (Tree tree in trees)
				{
					wrapper.Put(tree);
				}
				wrapper.Join();
			}
		}

		// trees are changed in place
		/// <summary>
		/// Get all of the states which occur at the root, even if they occur
		/// elsewhere in the tree.
		/// </summary>
		/// <remarks>
		/// Get all of the states which occur at the root, even if they occur
		/// elsewhere in the tree.  Useful for knowing when you can Finalize
		/// a tree
		/// </remarks>
		private static ICollection<string> FindRootStates(IList<Tree> trees)
		{
			ICollection<string> roots = Generics.NewHashSet();
			foreach (Tree tree in trees)
			{
				roots.Add(tree.Value());
			}
			return Java.Util.Collections.UnmodifiableSet(roots);
		}

		/// <summary>Get all of the states which *only* occur at the root.</summary>
		/// <remarks>
		/// Get all of the states which *only* occur at the root.  Useful for
		/// knowing which transitions can't be done internal to the tree
		/// </remarks>
		private static ICollection<string> FindRootOnlyStates(IList<Tree> trees, ICollection<string> rootStates)
		{
			ICollection<string> rootOnlyStates = Generics.NewHashSet(rootStates);
			foreach (Tree tree in trees)
			{
				foreach (Tree child in tree.Children())
				{
					FindRootOnlyStatesHelper(child, rootStates, rootOnlyStates);
				}
			}
			return Java.Util.Collections.UnmodifiableSet(rootOnlyStates);
		}

		private static void FindRootOnlyStatesHelper(Tree tree, ICollection<string> rootStates, ICollection<string> rootOnlyStates)
		{
			rootOnlyStates.Remove(tree.Value());
			foreach (Tree child in tree.Children())
			{
				FindRootOnlyStatesHelper(child, rootStates, rootOnlyStates);
			}
		}

		private void Train(IList<Pair<string, IFileFilter>> trainTreebankPath, Pair<string, IFileFilter> devTreebankPath, string serializedPath)
		{
			log.Info("Training method: " + op.TrainOptions().trainingMethod);
			IList<Tree> binarizedTrees = Generics.NewArrayList();
			foreach (Pair<string, IFileFilter> treebank in trainTreebankPath)
			{
				Sharpen.Collections.AddAll(binarizedTrees, ReadBinarizedTreebank(treebank.First(), treebank.Second()));
			}
			int nThreads = op.trainOptions.trainingThreads;
			nThreads = nThreads <= 0 ? Runtime.GetRuntime().AvailableProcessors() : nThreads;
			Edu.Stanford.Nlp.Tagger.Common.Tagger tagger = null;
			if (op.testOptions.preTag)
			{
				Timing retagTimer = new Timing();
				tagger = Edu.Stanford.Nlp.Tagger.Common.Tagger.LoadModel(op.testOptions.taggerSerializedFile);
				RedoTags(binarizedTrees, tagger, nThreads);
				retagTimer.Done("Retagging");
			}
			ICollection<string> knownStates = FindKnownStates(binarizedTrees);
			ICollection<string> rootStates = FindRootStates(binarizedTrees);
			ICollection<string> rootOnlyStates = FindRootOnlyStates(binarizedTrees, rootStates);
			log.Info("Known states: " + knownStates);
			log.Info("States which occur at the root: " + rootStates);
			log.Info("States which only occur at the root: " + rootStates);
			Timing transitionTimer = new Timing();
			IList<IList<ITransition>> transitionLists = CreateTransitionSequence.CreateTransitionSequences(binarizedTrees, op.compoundUnaries, rootStates, rootOnlyStates);
			IIndex<ITransition> transitionIndex = new HashIndex<ITransition>();
			foreach (IList<ITransition> transitions in transitionLists)
			{
				transitionIndex.AddAll(transitions);
			}
			transitionTimer.Done("Converting trees into transition lists");
			log.Info("Number of transitions: " + transitionIndex.Size());
			Random random = new Random(op.trainOptions.randomSeed);
			Treebank devTreebank = null;
			if (devTreebankPath != null)
			{
				devTreebank = ReadTreebank(devTreebankPath.First(), devTreebankPath.Second());
			}
			PerceptronModel newModel = new PerceptronModel(this.op, transitionIndex, knownStates, rootStates, rootOnlyStates);
			newModel.TrainModel(serializedPath, tagger, random, binarizedTrees, transitionLists, devTreebank, nThreads);
			this.model = newModel;
		}

		public override void SetOptionFlags(params string[] flags)
		{
			op.SetOptions(flags);
		}

		public static ParserGrammar LoadModel(string path, params string[] extraFlags)
		{
			ShiftReduceParser parser = IOUtils.ReadObjectAnnouncingTimingFromURLOrClasspathOrFileSystem(log, "Loading parser from serialized file", path);
			if (extraFlags.Length > 0)
			{
				parser.SetOptionFlags(extraFlags);
			}
			return parser;
		}

		public virtual void SaveModel(string path)
		{
			try
			{
				IOUtils.WriteObjectToFile(this, path);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		private static readonly string[] ForceTags = new string[] { "-forceTags" };

		public static void Main(string[] args)
		{
			IList<string> remainingArgs = Generics.NewArrayList();
			IList<Pair<string, IFileFilter>> trainTreebankPath = null;
			Pair<string, IFileFilter> testTreebankPath = null;
			Pair<string, IFileFilter> devTreebankPath = null;
			string serializedPath = null;
			string tlppClass = null;
			string continueTraining = null;
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-trainTreebank"))
				{
					if (trainTreebankPath == null)
					{
						trainTreebankPath = Generics.NewArrayList();
					}
					trainTreebankPath.Add(ArgUtils.GetTreebankDescription(args, argIndex, "-trainTreebank"));
					argIndex = argIndex + ArgUtils.NumSubArgs(args, argIndex) + 1;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-testTreebank"))
					{
						testTreebankPath = ArgUtils.GetTreebankDescription(args, argIndex, "-testTreebank");
						argIndex = argIndex + ArgUtils.NumSubArgs(args, argIndex) + 1;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-devTreebank"))
						{
							devTreebankPath = ArgUtils.GetTreebankDescription(args, argIndex, "-devTreebank");
							argIndex = argIndex + ArgUtils.NumSubArgs(args, argIndex) + 1;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-serializedPath") || Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-model"))
							{
								serializedPath = args[argIndex + 1];
								argIndex += 2;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-tlpp"))
								{
									tlppClass = args[argIndex + 1];
									argIndex += 2;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-continueTraining"))
									{
										continueTraining = args[argIndex + 1];
										argIndex += 2;
									}
									else
									{
										remainingArgs.Add(args[argIndex]);
										++argIndex;
									}
								}
							}
						}
					}
				}
			}
			string[] newArgs = new string[remainingArgs.Count];
			newArgs = Sharpen.Collections.ToArray(remainingArgs, newArgs);
			if (trainTreebankPath == null && serializedPath == null)
			{
				throw new ArgumentException("Must specify a treebank to train from with -trainTreebank or a parser to load with -serializedPath");
			}
			ShiftReduceParser parser = null;
			if (trainTreebankPath != null)
			{
				log.Info("Training ShiftReduceParser");
				log.Info("Initial arguments:");
				log.Info("   " + StringUtils.Join(args));
				if (continueTraining != null)
				{
					parser = ((ShiftReduceParser)ShiftReduceParser.LoadModel(continueTraining, ArrayUtils.Concatenate(ForceTags, newArgs)));
				}
				else
				{
					ShiftReduceOptions op = BuildTrainingOptions(tlppClass, newArgs);
					parser = new ShiftReduceParser(op);
				}
				parser.Train(trainTreebankPath, devTreebankPath, serializedPath);
				parser.SaveModel(serializedPath);
			}
			if (serializedPath != null && parser == null)
			{
				parser = ((ShiftReduceParser)ShiftReduceParser.LoadModel(serializedPath, ArrayUtils.Concatenate(ForceTags, newArgs)));
			}
			//parser.outputStats();
			if (testTreebankPath != null)
			{
				log.Info("Loading test trees from " + testTreebankPath.First());
				Treebank testTreebank = parser.op.tlpParams.MemoryTreebank();
				testTreebank.LoadPath(testTreebankPath.First(), testTreebankPath.Second());
				log.Info("Loaded " + testTreebank.Count + " trees");
				EvaluateTreebank evaluator = new EvaluateTreebank(parser.op, null, parser);
				evaluator.TestOnTreebank(testTreebank);
			}
		}

		private const long serialVersionUID = 1;
		// log.info("Input tree: " + tree);
		// log.info("Debinarized tree: " + query.getBestParse());
		// log.info("Parsed binarized tree: " + query.getBestBinarizedParse());
		// log.info("Predicted transition sequence: " + query.getBestTransitionSequence());
	}
}
