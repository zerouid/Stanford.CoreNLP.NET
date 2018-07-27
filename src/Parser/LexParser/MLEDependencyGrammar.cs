using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	[System.Serializable]
	public class MLEDependencyGrammar : AbstractDependencyGrammar
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.MLEDependencyGrammar));

		internal readonly bool useSmoothTagProjection;

		internal readonly bool useUnigramWordSmoothing;

		internal const bool Debug = false;

		protected internal int numWordTokens;

		/// <summary>
		/// Stores all the counts for dependencies (with and without the word
		/// being a wildcard) in the reduced tag space.
		/// </summary>
		protected internal ClassicCounter<IntDependency> argCounter;

		protected internal ClassicCounter<IntDependency> stopCounter;

		/// <summary>
		/// Bayesian m-estimate prior for aT given hTWd against base distribution
		/// of aT given hTd.
		/// </summary>
		/// <remarks>
		/// Bayesian m-estimate prior for aT given hTWd against base distribution
		/// of aT given hTd.
		/// TODO: Note that these values are overwritten in the constructor. Find what is best and then maybe remove these defaults!
		/// </remarks>
		public double smooth_aT_hTWd = 32.0;

		/// <summary>
		/// Bayesian m-estimate prior for aTW given hTWd against base distribution
		/// of aTW given hTd.
		/// </summary>
		public double smooth_aTW_hTWd = 16.0;

		public double smooth_stop = 4.0;

		/// <summary>
		/// Interpolation between model that directly predicts aTW and model
		/// that predicts aT and then aW given aT.
		/// </summary>
		/// <remarks>
		/// Interpolation between model that directly predicts aTW and model
		/// that predicts aT and then aW given aT.  This percent of the mass
		/// is on the model directly predicting aTW.
		/// </remarks>
		public double interp = 0.6;

		public double smooth_aTW_aT = 96.0;

		public double smooth_aTW_hTd = 32.0;

		public double smooth_aT_hTd = 32.0;

		public double smooth_aPTW_aPT = 16.0;

		public MLEDependencyGrammar(ITreebankLangParserParams tlpParams, bool directional, bool distance, bool coarseDistance, bool basicCategoryTagsInDependencyGrammar, Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
			: this(basicCategoryTagsInDependencyGrammar ? new BasicCategoryTagProjection(tlpParams.TreebankLanguagePack()) : new TestTagProjection(), tlpParams, directional, distance, coarseDistance, op, wordIndex, tagIndex)
		{
		}

		public MLEDependencyGrammar(ITagProjection tagProjection, ITreebankLangParserParams tlpParams, bool directional, bool useDistance, bool useCoarseDistance, Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
			: base(tlpParams.TreebankLanguagePack(), tagProjection, directional, useDistance, useCoarseDistance, op, wordIndex, tagIndex)
		{
			// reduced tag space
			//  public double distanceDecay = 0.0;
			// extra smoothing hyperparameters for tag projection backoff.  Only used if useSmoothTagProjection is true.
			// back off Bayesian m-estimate of aTW given aT to aPTW given aPT
			// back off Bayesian m-estimate of aTW_hTd to aPTW_hPTd (?? guessed, not tuned)
			// back off Bayesian m-estimate of aT_hTd to aPT_hPTd (?? guessed, not tuned)
			// back off word prediction from tag to projected tag (only used if useUnigramWordSmoothing is true)
			useSmoothTagProjection = op.useSmoothTagProjection;
			useUnigramWordSmoothing = op.useUnigramWordSmoothing;
			argCounter = new ClassicCounter<IntDependency>();
			stopCounter = new ClassicCounter<IntDependency>();
			double[] smoothParams = tlpParams.MLEDependencyGrammarSmoothingParams();
			smooth_aT_hTWd = smoothParams[0];
			smooth_aTW_hTWd = smoothParams[1];
			smooth_stop = smoothParams[2];
			interp = smoothParams[3];
			// cdm added Jan 2007 to play with dep grammar smoothing.  Integrate this better if we keep it!
			smoothTP = new BasicCategoryTagProjection(tlpParams.TreebankLanguagePack());
		}

		public override string ToString()
		{
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(2);
			StringBuilder sb = new StringBuilder(2000);
			string cl = GetType().FullName;
			sb.Append(Sharpen.Runtime.Substring(cl, cl.LastIndexOf('.') + 1)).Append("[tagbins=");
			sb.Append(numTagBins).Append(",wordTokens=").Append(numWordTokens).Append("; head -> arg\n");
			//    for (Iterator dI = coreDependencies.keySet().iterator(); dI.hasNext();) {
			//      IntDependency d = (IntDependency) dI.next();
			//      double count = coreDependencies.getCount(d);
			//      sb.append(d + " count " + nf.format(count));
			//      if (dI.hasNext()) {
			//        sb.append(",");
			//      }
			//      sb.append("\n");
			//    }
			sb.Append("]");
			return sb.ToString();
		}

		public virtual bool PruneTW(IntTaggedWord argTW)
		{
			string[] punctTags = tlp.PunctuationTags();
			foreach (string punctTag in punctTags)
			{
				if (argTW.tag == tagIndex.IndexOf(punctTag))
				{
					return true;
				}
			}
			return false;
		}

		internal class EndHead
		{
			public int end;

			public int head;
		}

		/// <summary>Adds dependencies to list depList.</summary>
		/// <remarks>
		/// Adds dependencies to list depList.  These are in terms of the original
		/// tag set not the reduced (projected) tag set.
		/// </remarks>
		protected internal static MLEDependencyGrammar.EndHead TreeToDependencyHelper(Tree tree, IList<IntDependency> depList, int loc, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			//       try {
			// 	PrintWriter pw = new PrintWriter(new OutputStreamWriter(System.out,"GB18030"),true);
			// 	tree.pennPrint(pw);
			//       }
			//       catch (UnsupportedEncodingException e) {}
			if (tree.IsLeaf() || tree.IsPreTerminal())
			{
				MLEDependencyGrammar.EndHead tempEndHead = new MLEDependencyGrammar.EndHead();
				tempEndHead.head = loc;
				tempEndHead.end = loc + 1;
				return tempEndHead;
			}
			Tree[] kids = tree.Children();
			if (kids.Length == 1)
			{
				return TreeToDependencyHelper(kids[0], depList, loc, wordIndex, tagIndex);
			}
			MLEDependencyGrammar.EndHead tempEndHead_1 = TreeToDependencyHelper(kids[0], depList, loc, wordIndex, tagIndex);
			int lHead = tempEndHead_1.head;
			int split = tempEndHead_1.end;
			tempEndHead_1 = TreeToDependencyHelper(kids[1], depList, tempEndHead_1.end, wordIndex, tagIndex);
			int end = tempEndHead_1.end;
			int rHead = tempEndHead_1.head;
			string hTag = ((IHasTag)tree.Label()).Tag();
			string lTag = ((IHasTag)kids[0].Label()).Tag();
			string rTag = ((IHasTag)kids[1].Label()).Tag();
			string hWord = ((IHasWord)tree.Label()).Word();
			string lWord = ((IHasWord)kids[0].Label()).Word();
			string rWord = ((IHasWord)kids[1].Label()).Word();
			bool leftHeaded = hWord.Equals(lWord);
			string aTag = (leftHeaded ? rTag : lTag);
			string aWord = (leftHeaded ? rWord : lWord);
			int hT = tagIndex.IndexOf(hTag);
			int aT = tagIndex.IndexOf(aTag);
			int hW = (wordIndex.Contains(hWord) ? wordIndex.IndexOf(hWord) : wordIndex.IndexOf(LexiconConstants.UnknownWord));
			int aW = (wordIndex.Contains(aWord) ? wordIndex.IndexOf(aWord) : wordIndex.IndexOf(LexiconConstants.UnknownWord));
			int head = (leftHeaded ? lHead : rHead);
			int arg = (leftHeaded ? rHead : lHead);
			IntDependency dependency = new IntDependency(hW, hT, aW, aT, leftHeaded, (leftHeaded ? split - head - 1 : head - split));
			depList.Add(dependency);
			IntDependency stopL = new IntDependency(aW, aT, IntTaggedWord.StopWordInt, IntTaggedWord.StopTagInt, false, (leftHeaded ? arg - split : arg - loc));
			depList.Add(stopL);
			IntDependency stopR = new IntDependency(aW, aT, IntTaggedWord.StopWordInt, IntTaggedWord.StopTagInt, true, (leftHeaded ? end - arg - 1 : split - arg - 1));
			depList.Add(stopR);
			//System.out.println("Adding: "+dependency+" at "+tree.label());
			tempEndHead_1.head = head;
			return tempEndHead_1;
		}

		public virtual void DumpSizes()
		{
			//    System.out.println("core dep " + coreDependencies.size());
			System.Console.Out.WriteLine("arg counter " + argCounter.Size());
			System.Console.Out.WriteLine("stop counter " + stopCounter.Size());
		}

		/// <summary>Returns the List of dependencies for a binarized Tree.</summary>
		/// <remarks>
		/// Returns the List of dependencies for a binarized Tree.
		/// In this tree, one of the two children always equals the head.
		/// The dependencies are in terms of
		/// the original tag set not the reduced (projected) tag set.
		/// </remarks>
		/// <param name="tree">A tree to be analyzed as dependencies</param>
		/// <returns>The list of dependencies in the tree (int format)</returns>
		public static IList<IntDependency> TreeToDependencyList(Tree tree, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			IList<IntDependency> depList = new List<IntDependency>();
			TreeToDependencyHelper(tree, depList, 0, wordIndex, tagIndex);
			return depList;
		}

		public virtual double ScoreAll(ICollection<IntDependency> deps)
		{
			double totalScore = 0.0;
			foreach (IntDependency d in deps)
			{
				//if (d.head.word == wordIndex.indexOf("via") ||
				//          d.arg.word == wordIndex.indexOf("via"))
				//System.out.println(d+" at "+score(d));
				double score = Score(d);
				if (score > double.NegativeInfinity)
				{
					totalScore += score;
				}
			}
			return totalScore;
		}

		/// <summary>
		/// Tune the smoothing and interpolation parameters of the dependency
		/// grammar based on a tuning treebank.
		/// </summary>
		/// <param name="trees">A Collection of Trees for setting parameters</param>
		public override void Tune(ICollection<Tree> trees)
		{
			IList<IntDependency> deps = new List<IntDependency>();
			foreach (Tree tree in trees)
			{
				Sharpen.Collections.AddAll(deps, TreeToDependencyList(tree, wordIndex, tagIndex));
			}
			double bestScore = double.NegativeInfinity;
			double bestSmooth_stop = 0.0;
			double bestSmooth_aTW_hTWd = 0.0;
			double bestSmooth_aT_hTWd = 0.0;
			double bestInterp = 0.0;
			log.Info("Tuning smooth_stop...");
			for (smooth_stop = 1.0 / 100.0; smooth_stop < 100.0; smooth_stop *= 1.25)
			{
				double totalScore = 0.0;
				foreach (IntDependency dep in deps)
				{
					if (!RootTW(dep.head))
					{
						double stopProb = GetStopProb(dep);
						if (!dep.arg.Equals(stopTW))
						{
							stopProb = 1.0 - stopProb;
						}
						if (stopProb > 0.0)
						{
							totalScore += Math.Log(stopProb);
						}
					}
				}
				if (totalScore > bestScore)
				{
					bestScore = totalScore;
					bestSmooth_stop = smooth_stop;
				}
			}
			smooth_stop = bestSmooth_stop;
			log.Info("Tuning selected smooth_stop: " + smooth_stop);
			for (IEnumerator<IntDependency> iter = deps.GetEnumerator(); iter.MoveNext(); )
			{
				IntDependency dep = iter.Current;
				if (dep.arg.Equals(stopTW))
				{
					iter.Remove();
				}
			}
			log.Info("Tuning other parameters...");
			if (!useSmoothTagProjection)
			{
				bestScore = double.NegativeInfinity;
				for (smooth_aTW_hTWd = 0.5; smooth_aTW_hTWd < 100.0; smooth_aTW_hTWd *= 1.25)
				{
					log.Info(".");
					for (smooth_aT_hTWd = 0.5; smooth_aT_hTWd < 100.0; smooth_aT_hTWd *= 1.25)
					{
						for (interp = 0.02; interp < 1.0; interp += 0.02)
						{
							double totalScore = 0.0;
							foreach (IntDependency dep in deps)
							{
								double score = Score(dep);
								if (score > double.NegativeInfinity)
								{
									totalScore += score;
								}
							}
							if (totalScore > bestScore)
							{
								bestScore = totalScore;
								bestInterp = interp;
								bestSmooth_aTW_hTWd = smooth_aTW_hTWd;
								bestSmooth_aT_hTWd = smooth_aT_hTWd;
								log.Info("Current best interp: " + interp + " with score " + totalScore);
							}
						}
					}
				}
				smooth_aTW_hTWd = bestSmooth_aTW_hTWd;
				smooth_aT_hTWd = bestSmooth_aT_hTWd;
				interp = bestInterp;
			}
			else
			{
				// for useSmoothTagProjection
				double bestSmooth_aTW_aT = 0.0;
				double bestSmooth_aTW_hTd = 0.0;
				double bestSmooth_aT_hTd = 0.0;
				bestScore = double.NegativeInfinity;
				for (smooth_aTW_hTWd = 1.125; smooth_aTW_hTWd < 100.0; smooth_aTW_hTWd *= 1.5)
				{
					log.Info("#");
					for (smooth_aT_hTWd = 1.125; smooth_aT_hTWd < 100.0; smooth_aT_hTWd *= 1.5)
					{
						log.Info(":");
						for (smooth_aTW_aT = 1.125; smooth_aTW_aT < 200.0; smooth_aTW_aT *= 1.5)
						{
							log.Info(".");
							for (smooth_aTW_hTd = 1.125; smooth_aTW_hTd < 100.0; smooth_aTW_hTd *= 1.5)
							{
								for (smooth_aT_hTd = 1.125; smooth_aT_hTd < 100.0; smooth_aT_hTd *= 1.5)
								{
									for (interp = 0.2; interp <= 0.8; interp += 0.02)
									{
										double totalScore = 0.0;
										foreach (IntDependency dep in deps)
										{
											double score = Score(dep);
											if (score > double.NegativeInfinity)
											{
												totalScore += score;
											}
										}
										if (totalScore > bestScore)
										{
											bestScore = totalScore;
											bestInterp = interp;
											bestSmooth_aTW_hTWd = smooth_aTW_hTWd;
											bestSmooth_aT_hTWd = smooth_aT_hTWd;
											bestSmooth_aTW_aT = smooth_aTW_aT;
											bestSmooth_aTW_hTd = smooth_aTW_hTd;
											bestSmooth_aT_hTd = smooth_aT_hTd;
											log.Info("Current best interp: " + interp + " with score " + totalScore);
										}
									}
								}
							}
						}
					}
					log.Info();
				}
				smooth_aTW_hTWd = bestSmooth_aTW_hTWd;
				smooth_aT_hTWd = bestSmooth_aT_hTWd;
				smooth_aTW_aT = bestSmooth_aTW_aT;
				smooth_aTW_hTd = bestSmooth_aTW_hTd;
				smooth_aT_hTd = bestSmooth_aT_hTd;
				interp = bestInterp;
			}
			log.Info("\nTuning selected smooth_aTW_hTWd: " + smooth_aTW_hTWd + " smooth_aT_hTWd: " + smooth_aT_hTWd + " interp: " + interp + " smooth_aTW_aT: " + smooth_aTW_aT + " smooth_aTW_hTd: " + smooth_aTW_hTd + " smooth_aT_hTd: " + smooth_aT_hTd);
		}

		/// <summary>Add this dependency with the given count to the grammar.</summary>
		/// <remarks>
		/// Add this dependency with the given count to the grammar.
		/// This is the main entry point of MLEDependencyGrammarExtractor.
		/// This is a dependency represented in the full tag space.
		/// </remarks>
		public virtual void AddRule(IntDependency dependency, double count)
		{
			if (!directional)
			{
				dependency = new IntDependency(dependency.head, dependency.arg, false, dependency.distance);
			}
			//    coreDependencies.incrementCount(dependency, count);
			/*new IntDependency(dependency.head.word,
			dependency.head.tag,
			dependency.arg.word,
			dependency.arg.tag,
			dependency.leftHeaded,
			dependency.distance), count);
			*/
			ExpandDependency(dependency, count);
		}

		/// <summary>The indices of this list are in the tag binned space.</summary>
		[System.NonSerialized]
		protected internal IList<IntTaggedWord> tagITWList = null;

		// log.info("stopCounter: " + stopCounter);
		// log.info("argCounter: " + argCounter);
		//new ArrayList();
		/// <summary>
		/// This maps from a tag to a cached IntTagWord that represents the
		/// tag by having the wildcard word ANY_WORD_INT and  the tag in the
		/// reduced tag space.
		/// </summary>
		/// <remarks>
		/// This maps from a tag to a cached IntTagWord that represents the
		/// tag by having the wildcard word ANY_WORD_INT and  the tag in the
		/// reduced tag space.
		/// The argument is in terms of the full tag space; internally this
		/// function maps to the reduced space.
		/// </remarks>
		/// <param name="tag">short representation of tag in full tag space</param>
		/// <returns>an IntTaggedWord in the reduced tag space</returns>
		private IntTaggedWord GetCachedITW(short tag)
		{
			// The +2 below is because -1 and -2 are used with special meanings (see IntTaggedWord).
			if (tagITWList == null)
			{
				tagITWList = new List<IntTaggedWord>(numTagBins + 2);
				for (int i = 0; i < numTagBins + 2; i++)
				{
					tagITWList.Add(i, null);
				}
			}
			IntTaggedWord headT = tagITWList[TagBin(tag) + 2];
			if (headT == null)
			{
				headT = new IntTaggedWord(IntTaggedWord.AnyWordInt, TagBin(tag));
				tagITWList.Set(TagBin(tag) + 2, headT);
			}
			return headT;
		}

		/// <summary>The dependency arg is still in the full tag space.</summary>
		/// <param name="dependency">An opbserved dependency</param>
		/// <param name="count">The weight of the dependency</param>
		protected internal virtual void ExpandDependency(IntDependency dependency, double count)
		{
			//if (Test.prunePunc && pruneTW(dependency.arg))
			//  return;
			if (dependency.head == null || dependency.arg == null)
			{
				return;
			}
			if (dependency.arg.word != IntTaggedWord.StopWordInt)
			{
				ExpandArg(dependency, ValenceBin(dependency.distance), count);
			}
			ExpandStop(dependency, DistanceBin(dependency.distance), count, true);
		}

		private ITagProjection smoothTP;

		private IIndex<string> smoothTPIndex;

		private const string TpPrefix = ".*TP*.";

		private short TagProject(short tag)
		{
			if (smoothTPIndex == null)
			{
				smoothTPIndex = new HashIndex<string>(tagIndex);
			}
			if (tag < 0)
			{
				return tag;
			}
			else
			{
				string tagStr = smoothTPIndex.Get(tag);
				string binStr = TpPrefix + smoothTP.Project(tagStr);
				return (short)smoothTPIndex.AddToIndex(binStr);
			}
		}

		/// <summary>Collect counts for a non-STOP dependent.</summary>
		/// <remarks>
		/// Collect counts for a non-STOP dependent.
		/// The dependency arg is still in the full tag space.
		/// </remarks>
		/// <param name="dependency">A non-stop dependency</param>
		/// <param name="valBinDist">A binned distance</param>
		/// <param name="count">The weight with which to add this dependency</param>
		private void ExpandArg(IntDependency dependency, short valBinDist, double count)
		{
			IntTaggedWord headT = GetCachedITW(dependency.head.tag);
			IntTaggedWord argT = GetCachedITW(dependency.arg.tag);
			IntTaggedWord head = new IntTaggedWord(dependency.head.word, TagBin(dependency.head.tag));
			//dependency.head;
			IntTaggedWord arg = new IntTaggedWord(dependency.arg.word, TagBin(dependency.arg.tag));
			//dependency.arg;
			bool leftHeaded = dependency.leftHeaded;
			// argCounter stores stuff in both the original and the reduced tag space???
			argCounter.IncrementCount(Intern(head, arg, leftHeaded, valBinDist), count);
			argCounter.IncrementCount(Intern(headT, arg, leftHeaded, valBinDist), count);
			argCounter.IncrementCount(Intern(head, argT, leftHeaded, valBinDist), count);
			argCounter.IncrementCount(Intern(headT, argT, leftHeaded, valBinDist), count);
			argCounter.IncrementCount(Intern(head, wildTW, leftHeaded, valBinDist), count);
			argCounter.IncrementCount(Intern(headT, wildTW, leftHeaded, valBinDist), count);
			// the WILD head stats are always directionless and not useDistance!
			argCounter.IncrementCount(Intern(wildTW, arg, false, (short)-1), count);
			argCounter.IncrementCount(Intern(wildTW, argT, false, (short)-1), count);
			if (useSmoothTagProjection)
			{
				// added stuff to do more smoothing.  CDM Jan 2007
				IntTaggedWord headP = new IntTaggedWord(dependency.head.word, TagProject(dependency.head.tag));
				IntTaggedWord headTP = new IntTaggedWord(IntTaggedWord.AnyWordInt, TagProject(dependency.head.tag));
				IntTaggedWord argP = new IntTaggedWord(dependency.arg.word, TagProject(dependency.arg.tag));
				IntTaggedWord argTP = new IntTaggedWord(IntTaggedWord.AnyWordInt, TagProject(dependency.arg.tag));
				argCounter.IncrementCount(Intern(headP, argP, leftHeaded, valBinDist), count);
				argCounter.IncrementCount(Intern(headTP, argP, leftHeaded, valBinDist), count);
				argCounter.IncrementCount(Intern(headP, argTP, leftHeaded, valBinDist), count);
				argCounter.IncrementCount(Intern(headTP, argTP, leftHeaded, valBinDist), count);
				argCounter.IncrementCount(Intern(headP, wildTW, leftHeaded, valBinDist), count);
				argCounter.IncrementCount(Intern(headTP, wildTW, leftHeaded, valBinDist), count);
				// the WILD head stats are always directionless and not useDistance!
				argCounter.IncrementCount(Intern(wildTW, argP, false, (short)-1), count);
				argCounter.IncrementCount(Intern(wildTW, argTP, false, (short)-1), count);
				argCounter.IncrementCount(Intern(wildTW, new IntTaggedWord(dependency.head.word, IntTaggedWord.AnyTagInt), false, (short)-1), count);
			}
			numWordTokens++;
		}

		private void ExpandStop(IntDependency dependency, short distBinDist, double count, bool wildForStop)
		{
			IntTaggedWord headT = GetCachedITW(dependency.head.tag);
			IntTaggedWord head = new IntTaggedWord(dependency.head.word, TagBin(dependency.head.tag));
			//dependency.head;
			IntTaggedWord arg = new IntTaggedWord(dependency.arg.word, TagBin(dependency.arg.tag));
			//dependency.arg;
			bool leftHeaded = dependency.leftHeaded;
			if (arg.word == IntTaggedWord.StopWordInt)
			{
				stopCounter.IncrementCount(Intern(head, arg, leftHeaded, distBinDist), count);
				stopCounter.IncrementCount(Intern(headT, arg, leftHeaded, distBinDist), count);
			}
			if (wildForStop || arg.word != IntTaggedWord.StopWordInt)
			{
				stopCounter.IncrementCount(Intern(head, wildTW, leftHeaded, distBinDist), count);
				stopCounter.IncrementCount(Intern(headT, wildTW, leftHeaded, distBinDist), count);
			}
		}

		public virtual double CountHistory(IntDependency dependency)
		{
			IntDependency temp = new IntDependency(dependency.head.word, TagBin(dependency.head.tag), wildTW.word, wildTW.tag, dependency.leftHeaded, ValenceBin(dependency.distance));
			return argCounter.GetCount(temp);
		}

		/// <summary>Score a tag binned dependency.</summary>
		public override double ScoreTB(IntDependency dependency)
		{
			return op.testOptions.depWeight * Math.Log(ProbTB(dependency));
		}

		private const bool verbose = false;

		protected internal const double MinProbability = 1e-40;

		/// <summary>
		/// Calculate the probability of a dependency as a real probability between
		/// 0 and 1 inclusive.
		/// </summary>
		/// <param name="dependency">
		/// The dependency for which the probability is to be
		/// calculated.   The tags in this dependency are in the reduced
		/// TagProjection space.
		/// </param>
		/// <returns>The probability of the dependency</returns>
		protected internal virtual double ProbTB(IntDependency dependency)
		{
			// System.out.println("tagIndex: " + tagIndex);
			bool leftHeaded = dependency.leftHeaded && directional;
			int hW = dependency.head.word;
			int aW = dependency.arg.word;
			short hT = dependency.head.tag;
			short aT = dependency.arg.tag;
			IntTaggedWord aTW = dependency.arg;
			IntTaggedWord hTW = dependency.head;
			bool isRoot = RootTW(dependency.head);
			double pb_stop_hTWds;
			if (isRoot)
			{
				pb_stop_hTWds = 0.0;
			}
			else
			{
				pb_stop_hTWds = GetStopProb(dependency);
			}
			if (dependency.arg.word == IntTaggedWord.StopWordInt)
			{
				// did we generate stop?
				return pb_stop_hTWds;
			}
			double pb_go_hTWds = 1.0 - pb_stop_hTWds;
			// generate the argument
			short binDistance = ValenceBin(dependency.distance);
			// KEY:
			// c_     count of (read as joint count of first and second)
			// p_     MLE prob of (or MAP if useSmoothTagProjection)
			// pb_    MAP prob of (read as prob of first given second thing)
			// a      arg
			// h      head
			// T      tag
			// PT     projected tag
			// W      word
			// d      direction
			// ds     distance (implicit: there when direction is mentioned!)
			IntTaggedWord anyHead = new IntTaggedWord(IntTaggedWord.AnyWordInt, dependency.head.tag);
			IntTaggedWord anyArg = new IntTaggedWord(IntTaggedWord.AnyWordInt, dependency.arg.tag);
			IntTaggedWord anyTagArg = new IntTaggedWord(dependency.arg.word, IntTaggedWord.AnyTagInt);
			IntDependency temp = new IntDependency(dependency.head, dependency.arg, leftHeaded, binDistance);
			double c_aTW_hTWd = argCounter.GetCount(temp);
			temp = new IntDependency(dependency.head, anyArg, leftHeaded, binDistance);
			double c_aT_hTWd = argCounter.GetCount(temp);
			temp = new IntDependency(dependency.head, wildTW, leftHeaded, binDistance);
			double c_hTWd = argCounter.GetCount(temp);
			temp = new IntDependency(anyHead, dependency.arg, leftHeaded, binDistance);
			double c_aTW_hTd = argCounter.GetCount(temp);
			temp = new IntDependency(anyHead, anyArg, leftHeaded, binDistance);
			double c_aT_hTd = argCounter.GetCount(temp);
			temp = new IntDependency(anyHead, wildTW, leftHeaded, binDistance);
			double c_hTd = argCounter.GetCount(temp);
			// for smooth tag projection
			short aPT = short.MinValue;
			double c_aPTW_hPTd = double.NaN;
			double c_aPT_hPTd = double.NaN;
			double c_hPTd = double.NaN;
			double c_aPTW_aPT = double.NaN;
			double c_aPT = double.NaN;
			if (useSmoothTagProjection)
			{
				aPT = TagProject(dependency.arg.tag);
				short hPT = TagProject(dependency.head.tag);
				IntTaggedWord projectedArg = new IntTaggedWord(dependency.arg.word, aPT);
				IntTaggedWord projectedAnyHead = new IntTaggedWord(IntTaggedWord.AnyWordInt, hPT);
				IntTaggedWord projectedAnyArg = new IntTaggedWord(IntTaggedWord.AnyWordInt, aPT);
				temp = new IntDependency(projectedAnyHead, projectedArg, leftHeaded, binDistance);
				c_aPTW_hPTd = argCounter.GetCount(temp);
				temp = new IntDependency(projectedAnyHead, projectedAnyArg, leftHeaded, binDistance);
				c_aPT_hPTd = argCounter.GetCount(temp);
				temp = new IntDependency(projectedAnyHead, wildTW, leftHeaded, binDistance);
				c_hPTd = argCounter.GetCount(temp);
				temp = new IntDependency(wildTW, projectedArg, false, IntDependency.AnyDistanceInt);
				c_aPTW_aPT = argCounter.GetCount(temp);
				temp = new IntDependency(wildTW, projectedAnyArg, false, IntDependency.AnyDistanceInt);
				c_aPT = argCounter.GetCount(temp);
			}
			// wild head is always directionless and no use distance
			temp = new IntDependency(wildTW, dependency.arg, false, IntDependency.AnyDistanceInt);
			double c_aTW = argCounter.GetCount(temp);
			temp = new IntDependency(wildTW, anyArg, false, IntDependency.AnyDistanceInt);
			double c_aT = argCounter.GetCount(temp);
			temp = new IntDependency(wildTW, anyTagArg, false, IntDependency.AnyDistanceInt);
			double c_aW = argCounter.GetCount(temp);
			// do the Bayesian magic
			// MLE probs
			double p_aTW_hTd;
			double p_aT_hTd;
			double p_aTW_aT;
			double p_aW;
			double p_aPTW_aPT;
			double p_aPTW_hPTd;
			double p_aPT_hPTd;
			// backoffs either mle or themselves bayesian smoothed depending on useSmoothTagProjection
			if (useSmoothTagProjection)
			{
				if (useUnigramWordSmoothing)
				{
					p_aW = c_aW > 0.0 ? (c_aW / numWordTokens) : 1.0;
					// NEED this 1.0 for unknown words!!!
					p_aPTW_aPT = (c_aPTW_aPT + smooth_aPTW_aPT * p_aW) / (c_aPT + smooth_aPTW_aPT);
				}
				else
				{
					p_aPTW_aPT = c_aPTW_aPT > 0.0 ? (c_aPTW_aPT / c_aPT) : 1.0;
				}
				// NEED this 1.0 for unknown words!!!
				p_aTW_aT = (c_aTW + smooth_aTW_aT * p_aPTW_aPT) / (c_aT + smooth_aTW_aT);
				p_aPTW_hPTd = c_hPTd > 0.0 ? (c_aPTW_hPTd / c_hPTd) : 0.0;
				p_aTW_hTd = (c_aTW_hTd + smooth_aTW_hTd * p_aPTW_hPTd) / (c_hTd + smooth_aTW_hTd);
				p_aPT_hPTd = c_hPTd > 0.0 ? (c_aPT_hPTd / c_hPTd) : 0.0;
				p_aT_hTd = (c_aT_hTd + smooth_aT_hTd * p_aPT_hPTd) / (c_hTd + smooth_aT_hTd);
			}
			else
			{
				// here word generation isn't smoothed - can't get previously unseen word with tag.  Ugh.
				if (op.testOptions.useLexiconToScoreDependencyPwGt)
				{
					// We don't know the position.  Now -1 means average over 0 and 1.
					p_aTW_aT = dependency.leftHeaded ? Math.Exp(lex.Score(dependency.arg, 1, wordIndex.Get(dependency.arg.word), null)) : Math.Exp(lex.Score(dependency.arg, -1, wordIndex.Get(dependency.arg.word), null));
				}
				else
				{
					// double oldScore = c_aTW > 0.0 ? (c_aTW / c_aT) : 1.0;
					// if (oldScore == 1.0) {
					//  log.info("#### arg=" + dependency.arg + " score=" + p_aTW_aT +
					//                      " oldScore=" + oldScore + " c_aTW=" + c_aTW + " c_aW=" + c_aW);
					// }
					p_aTW_aT = c_aTW > 0.0 ? (c_aTW / c_aT) : 1.0;
				}
				p_aTW_hTd = c_hTd > 0.0 ? (c_aTW_hTd / c_hTd) : 0.0;
				p_aT_hTd = c_hTd > 0.0 ? (c_aT_hTd / c_hTd) : 0.0;
			}
			double pb_aTW_hTWd = (c_aTW_hTWd + smooth_aTW_hTWd * p_aTW_hTd) / (c_hTWd + smooth_aTW_hTWd);
			double pb_aT_hTWd = (c_aT_hTWd + smooth_aT_hTWd * p_aT_hTd) / (c_hTWd + smooth_aT_hTWd);
			double score = (interp * pb_aTW_hTWd + (1.0 - interp) * p_aTW_aT * pb_aT_hTWd) * pb_go_hTWds;
			if (op.testOptions.prunePunc && PruneTW(aTW))
			{
				return 1.0;
			}
			if (double.IsNaN(score))
			{
				score = 0.0;
			}
			//if (op.testOptions.rightBonus && ! dependency.leftHeaded)
			//  score -= 0.2;
			if (score < MinProbability)
			{
				score = 0.0;
			}
			return score;
		}

		/// <summary>
		/// Return the probability (as a real number between 0 and 1) of stopping
		/// rather than generating another argument at this position.
		/// </summary>
		/// <param name="dependency">
		/// The dependency used as the basis for stopping on.
		/// Tags are assumed to be in the TagProjection space.
		/// </param>
		/// <returns>The probability of generating this stop probability</returns>
		protected internal virtual double GetStopProb(IntDependency dependency)
		{
			short binDistance = DistanceBin(dependency.distance);
			IntTaggedWord unknownHead = new IntTaggedWord(-1, dependency.head.tag);
			IntTaggedWord anyHead = new IntTaggedWord(IntTaggedWord.AnyWordInt, dependency.head.tag);
			IntDependency temp = new IntDependency(dependency.head, stopTW, dependency.leftHeaded, binDistance);
			double c_stop_hTWds = stopCounter.GetCount(temp);
			temp = new IntDependency(unknownHead, stopTW, dependency.leftHeaded, binDistance);
			double c_stop_hTds = stopCounter.GetCount(temp);
			temp = new IntDependency(dependency.head, wildTW, dependency.leftHeaded, binDistance);
			double c_hTWds = stopCounter.GetCount(temp);
			temp = new IntDependency(anyHead, wildTW, dependency.leftHeaded, binDistance);
			double c_hTds = stopCounter.GetCount(temp);
			double p_stop_hTds = (c_hTds > 0.0 ? c_stop_hTds / c_hTds : 1.0);
			double pb_stop_hTWds = (c_stop_hTWds + smooth_stop * p_stop_hTds) / (c_hTWds + smooth_stop);
			return pb_stop_hTWds;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private void ReadObject(ObjectInputStream stream)
		{
			stream.DefaultReadObject();
			//    log.info("Before decompression:");
			//    log.info("arg size: " + argCounter.size() + "  total: " + argCounter.totalCount());
			//    log.info("stop size: " + stopCounter.size() + "  total: " + stopCounter.totalCount());
			ClassicCounter<IntDependency> compressedArgC = argCounter;
			argCounter = new ClassicCounter<IntDependency>();
			ClassicCounter<IntDependency> compressedStopC = stopCounter;
			stopCounter = new ClassicCounter<IntDependency>();
			foreach (IntDependency d in compressedArgC.KeySet())
			{
				double count = compressedArgC.GetCount(d);
				ExpandArg(d, d.distance, count);
			}
			foreach (IntDependency d_1 in compressedStopC.KeySet())
			{
				double count = compressedStopC.GetCount(d_1);
				ExpandStop(d_1, d_1.distance, count, false);
			}
			//    log.info("After decompression:");
			//    log.info("arg size: " + argCounter.size() + "  total: " + argCounter.totalCount());
			//    log.info("stop size: " + stopCounter.size() + "  total: " + stopCounter.totalCount());
			expandDependencyMap = null;
		}

		/// <exception cref="System.IO.IOException"/>
		private void WriteObject(ObjectOutputStream stream)
		{
			//    log.info("\nBefore compression:");
			//    log.info("arg size: " + argCounter.size() + "  total: " + argCounter.totalCount());
			//    log.info("stop size: " + stopCounter.size() + "  total: " + stopCounter.totalCount());
			ClassicCounter<IntDependency> fullArgCounter = argCounter;
			argCounter = new ClassicCounter<IntDependency>();
			foreach (IntDependency dependency in fullArgCounter.KeySet())
			{
				if (dependency.head != wildTW && dependency.arg != wildTW && dependency.head.word != -1 && dependency.arg.word != -1)
				{
					argCounter.IncrementCount(dependency, fullArgCounter.GetCount(dependency));
				}
			}
			ClassicCounter<IntDependency> fullStopCounter = stopCounter;
			stopCounter = new ClassicCounter<IntDependency>();
			foreach (IntDependency dependency_1 in fullStopCounter.KeySet())
			{
				if (dependency_1.head.word != -1)
				{
					stopCounter.IncrementCount(dependency_1, fullStopCounter.GetCount(dependency_1));
				}
			}
			//    log.info("After compression:");
			//    log.info("arg size: " + argCounter.size() + "  total: " + argCounter.totalCount());
			//    log.info("stop size: " + stopCounter.size() + "  total: " + stopCounter.totalCount());
			stream.DefaultWriteObject();
			argCounter = fullArgCounter;
			stopCounter = fullStopCounter;
		}

		/// <summary>
		/// Populates data in this DependencyGrammar from the character stream
		/// given by the Reader r.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public override void ReadData(BufferedReader @in)
		{
			string Left = "left";
			int lineNum = 1;
			// all lines have one rule per line
			bool doingStop = false;
			for (string line = @in.ReadLine(); line != null && line.Length > 0; line = @in.ReadLine())
			{
				try
				{
					if (line.Equals("BEGIN_STOP"))
					{
						doingStop = true;
						continue;
					}
					string[] fields = StringUtils.SplitOnCharWithQuoting(line, ' ', '\"', '\\');
					// split on spaces, quote with doublequote, and escape with backslash
					//        System.out.println("fields:\n" + fields[0] + "\n" + fields[1] + "\n" + fields[2] + "\n" + fields[3] + "\n" + fields[4] + "\n" + fields[5]);
					short distance = (short)System.Convert.ToInt32(fields[4]);
					IntTaggedWord tempHead = new IntTaggedWord(fields[0], '/', wordIndex, tagIndex);
					IntTaggedWord tempArg = new IntTaggedWord(fields[2], '/', wordIndex, tagIndex);
					IntDependency tempDependency = new IntDependency(tempHead, tempArg, fields[3].Equals(Left), distance);
					double count = double.ParseDouble(fields[5]);
					if (doingStop)
					{
						ExpandStop(tempDependency, distance, count, false);
					}
					else
					{
						ExpandArg(tempDependency, distance, count);
					}
				}
				catch (Exception e)
				{
					IOException ioe = new IOException("Error on line " + lineNum + ": " + line);
					ioe.InitCause(e);
					throw ioe;
				}
				//      System.out.println("read line " + lineNum + ": " + line);
				lineNum++;
			}
		}

		/// <summary>Writes out data from this Object to the Writer w.</summary>
		/// <exception cref="System.IO.IOException"/>
		public override void WriteData(PrintWriter @out)
		{
			// all lines have one rule per line
			foreach (IntDependency dependency in argCounter.KeySet())
			{
				if (dependency.head != wildTW && dependency.arg != wildTW && dependency.head.word != -1 && dependency.arg.word != -1)
				{
					double count = argCounter.GetCount(dependency);
					@out.Println(dependency.ToString(wordIndex, tagIndex) + " " + count);
				}
			}
			@out.Println("BEGIN_STOP");
			foreach (IntDependency dependency_1 in stopCounter.KeySet())
			{
				if (dependency_1.head.word != -1)
				{
					double count = stopCounter.GetCount(dependency_1);
					@out.Println(dependency_1.ToString(wordIndex, tagIndex) + " " + count);
				}
			}
			@out.Flush();
		}

		private const long serialVersionUID = 1L;
	}
}
