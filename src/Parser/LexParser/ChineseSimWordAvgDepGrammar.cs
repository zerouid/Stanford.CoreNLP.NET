using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>A Dependency grammar that smoothes by averaging over similar words.</summary>
	/// <author>Galen Andrew</author>
	/// <author>Pi-Chuan Chang</author>
	[System.Serializable]
	public class ChineseSimWordAvgDepGrammar : MLEDependencyGrammar
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.ChineseSimWordAvgDepGrammar));

		private const long serialVersionUID = -1845503582705055342L;

		private const double simSmooth = 10.0;

		private const string argHeadFile = "simWords/ArgHead.5";

		private const string headArgFile = "simWords/HeadArg.5";

		private IDictionary<Pair<int, string>, IList<Triple<int, string, double>>> simArgMap;

		private IDictionary<Pair<int, string>, IList<Triple<int, string, double>>> simHeadMap;

		private const bool debug = true;

		private const bool verbose = false;

		public ChineseSimWordAvgDepGrammar(ITreebankLangParserParams tlpParams, bool directional, bool distance, bool coarseDistance, bool basicCategoryTagsInDependencyGrammar, Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
			: base(tlpParams, directional, distance, coarseDistance, basicCategoryTagsInDependencyGrammar, op, wordIndex, tagIndex)
		{
			//private static final double MIN_PROBABILITY = Math.exp(-100.0);
			simHeadMap = GetMap(headArgFile);
			simArgMap = GetMap(argHeadFile);
		}

		public virtual IDictionary<Pair<int, string>, IList<Triple<int, string, double>>> GetMap(string filename)
		{
			IDictionary<Pair<int, string>, IList<Triple<int, string, double>>> hashMap = Generics.NewHashMap();
			try
			{
				BufferedReader wordMapBReader = new BufferedReader(new InputStreamReader(new FileInputStream(filename), "UTF-8"));
				string wordMapLine;
				Pattern linePattern = Pattern.Compile("sim\\((.+)/(.+):(.+)/(.+)\\)=(.+)");
				while ((wordMapLine = wordMapBReader.ReadLine()) != null)
				{
					Matcher m = linePattern.Matcher(wordMapLine);
					if (!m.Matches())
					{
						log.Info("Ill-formed line in similar word map file: " + wordMapLine);
						continue;
					}
					Pair<int, string> iTW = new Pair<int, string>(wordIndex.AddToIndex(m.Group(1)), m.Group(2));
					double score = double.ParseDouble(m.Group(5));
					IList<Triple<int, string, double>> tripleList = hashMap[iTW];
					if (tripleList == null)
					{
						tripleList = new List<Triple<int, string, double>>();
						hashMap[iTW] = tripleList;
					}
					tripleList.Add(new Triple<int, string, double>(wordIndex.AddToIndex(m.Group(3)), m.Group(4), score));
				}
			}
			catch (IOException)
			{
				throw new Exception("Problem reading similar words file!");
			}
			return hashMap;
		}

		public override double ScoreTB(IntDependency dependency)
		{
			//return op.testOptions.depWeight * Math.log(probSimilarWordAvg(dependency));
			return op.testOptions.depWeight * Math.Log(ProbTBwithSimWords(dependency));
		}

		public virtual void SetLex(ILexicon lex)
		{
			this.lex = lex;
		}

		private ClassicCounter<string> statsCounter = new ClassicCounter<string>();

		public virtual void DumpSimWordAvgStats()
		{
			log.Info("SimWordAvg stats:");
			log.Info(statsCounter);
		}

		/*
		** An alternative kind of smoothing.
		** The first one is "probSimilarWordAvg" implemented by Galen
		** This one is trying to modify "probTB" in MLEDependencyGrammar using the simWords list we have
		** -pichuan
		*/
		private double ProbTBwithSimWords(IntDependency dependency)
		{
			bool leftHeaded = dependency.leftHeaded && directional;
			IntTaggedWord unknownHead = new IntTaggedWord(-1, dependency.head.tag);
			IntTaggedWord unknownArg = new IntTaggedWord(-1, dependency.arg.tag);
			short distance = dependency.distance;
			// int hW = dependency.head.word;
			// int aW = dependency.arg.word;
			IntTaggedWord aTW = dependency.arg;
			// IntTaggedWord hTW = dependency.head;
			double pb_stop_hTWds = GetStopProb(dependency);
			bool isRoot = RootTW(dependency.head);
			if (dependency.arg.word == -2)
			{
				// did we generate stop?
				if (isRoot)
				{
					return 0.0;
				}
				return pb_stop_hTWds;
			}
			double pb_go_hTWds = 1.0 - pb_stop_hTWds;
			if (isRoot)
			{
				pb_go_hTWds = 1.0;
			}
			// generate the argument
			int valenceBinDistance = ValenceBin(distance);
			// KEY:
			// c_     count of
			// p_     MLE prob of
			// pb_    MAP prob of
			// a      arg
			// h      head
			// T      tag
			// W      word
			// d      direction
			// ds     distance
			IntDependency temp = new IntDependency(dependency.head, dependency.arg, leftHeaded, valenceBinDistance);
			double c_aTW_hTWd = argCounter.GetCount(temp);
			temp = new IntDependency(dependency.head, unknownArg, leftHeaded, valenceBinDistance);
			double c_aT_hTWd = argCounter.GetCount(temp);
			temp = new IntDependency(dependency.head, wildTW, leftHeaded, valenceBinDistance);
			double c_hTWd = argCounter.GetCount(temp);
			temp = new IntDependency(unknownHead, dependency.arg, leftHeaded, valenceBinDistance);
			double c_aTW_hTd = argCounter.GetCount(temp);
			temp = new IntDependency(unknownHead, unknownArg, leftHeaded, valenceBinDistance);
			double c_aT_hTd = argCounter.GetCount(temp);
			temp = new IntDependency(unknownHead, wildTW, leftHeaded, valenceBinDistance);
			double c_hTd = argCounter.GetCount(temp);
			temp = new IntDependency(wildTW, dependency.arg, false, -1);
			double c_aTW = argCounter.GetCount(temp);
			temp = new IntDependency(wildTW, unknownArg, false, -1);
			double c_aT = argCounter.GetCount(temp);
			// do the magic
			double p_aTW_hTd = (c_hTd > 0.0 ? c_aTW_hTd / c_hTd : 0.0);
			double p_aT_hTd = (c_hTd > 0.0 ? c_aT_hTd / c_hTd : 0.0);
			double p_aTW_aT = (c_aTW > 0.0 ? c_aTW / c_aT : 1.0);
			double pb_aTW_hTWd;
			// = (c_aTW_hTWd + smooth_aTW_hTWd * p_aTW_hTd) / (c_hTWd + smooth_aTW_hTWd);
			double pb_aT_hTWd = (c_aT_hTWd + smooth_aT_hTWd * p_aT_hTd) / (c_hTWd + smooth_aT_hTWd);
			double score;
			// = (interp * pb_aTW_hTWd + (1.0 - interp) * p_aTW_aT * pb_aT_hTWd) * pb_go_hTWds;
			/* smooth by simWords -pichuan */
			IList<Triple<int, string, double>> sim2arg = simArgMap[new Pair<int, string>(dependency.arg.word, StringBasicCategory(dependency.arg.tag))];
			IList<Triple<int, string, double>> sim2head = simHeadMap[new Pair<int, string>(dependency.head.word, StringBasicCategory(dependency.head.tag))];
			IList<int> simArg = new List<int>();
			IList<int> simHead = new List<int>();
			if (sim2arg != null)
			{
				foreach (Triple<int, string, double> t in sim2arg)
				{
					simArg.Add(t.first);
				}
			}
			if (sim2head != null)
			{
				foreach (Triple<int, string, double> t in sim2head)
				{
					simHead.Add(t.first);
				}
			}
			double cSim_aTW_hTd = 0;
			double cSim_hTd = 0;
			foreach (int h in simHead)
			{
				IntTaggedWord hWord = new IntTaggedWord(h, dependency.head.tag);
				temp = new IntDependency(hWord, dependency.arg, dependency.leftHeaded, dependency.distance);
				cSim_aTW_hTd += argCounter.GetCount(temp);
				temp = new IntDependency(hWord, wildTW, dependency.leftHeaded, dependency.distance);
				cSim_hTd += argCounter.GetCount(temp);
			}
			double pSim_aTW_hTd = (cSim_hTd > 0.0 ? cSim_aTW_hTd / cSim_hTd : 0.0);
			// P(Wa,Ta|Th)
			//if (simHead.size() > 0 && cSim_hTd == 0.0) {
			if (pSim_aTW_hTd > 0.0)
			{
				//System.out.println("# simHead("+dependency.head.word+"-"+wordNumberer.object(dependency.head.word)+") =\t"+cSim_hTd);
				System.Console.Out.WriteLine(dependency + "\t" + pSim_aTW_hTd);
			}
			//System.out.println(wordNumberer);
			//pb_aTW_hTWd = (c_aTW_hTWd + smooth_aTW_hTWd * pSim_aTW_hTd + smooth_aTW_hTWd * p_aTW_hTd) / (c_hTWd + smooth_aTW_hTWd + smooth_aTW_hTWd);
			//if (pSim_aTW_hTd > 0.0) {
			double smoothSim_aTW_hTWd = 17.7;
			double smooth_aTW_hTWd = 17.7 * 2;
			//smooth_aTW_hTWd = smooth_aTW_hTWd*2;
			pb_aTW_hTWd = (c_aTW_hTWd + smoothSim_aTW_hTWd * pSim_aTW_hTd + smooth_aTW_hTWd * p_aTW_hTd) / (c_hTWd + smoothSim_aTW_hTWd + smooth_aTW_hTWd);
			System.Console.Out.WriteLine(dependency);
			System.Console.Out.WriteLine(c_aTW_hTWd + " + " + smoothSim_aTW_hTWd + " * " + pSim_aTW_hTd + " + " + smooth_aTW_hTWd + " * " + p_aTW_hTd);
			System.Console.Out.WriteLine("--------------------------------  = " + pb_aTW_hTWd);
			System.Console.Out.WriteLine(c_hTWd + " + " + smoothSim_aTW_hTWd + " + " + smooth_aTW_hTWd);
			System.Console.Out.WriteLine();
			//}
			//pb_aT_hTWd = (c_aT_hTWd + smooth_aT_hTWd * p_aT_hTd) / (c_hTWd + smooth_aT_hTWd);
			score = (interp * pb_aTW_hTWd + (1.0 - interp) * p_aTW_aT * pb_aT_hTWd) * pb_go_hTWds;
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

		private double ProbSimilarWordAvg(IntDependency dep)
		{
			double regProb = ProbTB(dep);
			statsCounter.IncrementCount("total");
			IList<Triple<int, string, double>> sim2arg = simArgMap[new Pair<int, string>(dep.arg.word, StringBasicCategory(dep.arg.tag))];
			IList<Triple<int, string, double>> sim2head = simHeadMap[new Pair<int, string>(dep.head.word, StringBasicCategory(dep.head.tag))];
			if (sim2head == null && sim2arg == null)
			{
				return regProb;
			}
			double sumScores = 0;
			double sumWeights = 0;
			if (sim2head == null)
			{
				statsCounter.IncrementCount("aSim");
				foreach (Triple<int, string, double> simArg in sim2arg)
				{
					//double weight = 1 - simArg.third;
					double weight = Math.Exp(-50 * simArg.third);
					for (int tag = 0; tag < numT; tag++)
					{
						if (!StringBasicCategory(tag).Equals(simArg.second))
						{
							continue;
						}
						IntTaggedWord tempArg = new IntTaggedWord(simArg.first, tag);
						IntDependency tempDep = new IntDependency(dep.head, tempArg, dep.leftHeaded, dep.distance);
						double probArg = Math.Exp(lex.Score(tempArg, 0, wordIndex.Get(tempArg.word), null));
						if (probArg == 0.0)
						{
							continue;
						}
						sumScores += ProbTB(tempDep) * weight / probArg;
						sumWeights += weight;
					}
				}
			}
			else
			{
				if (sim2arg == null)
				{
					statsCounter.IncrementCount("hSim");
					foreach (Triple<int, string, double> simHead in sim2head)
					{
						//double weight = 1 - simHead.third;
						double weight = Math.Exp(-50 * simHead.third);
						for (int tag = 0; tag < numT; tag++)
						{
							if (!StringBasicCategory(tag).Equals(simHead.second))
							{
								continue;
							}
							IntTaggedWord tempHead = new IntTaggedWord(simHead.first, tag);
							IntDependency tempDep = new IntDependency(tempHead, dep.arg, dep.leftHeaded, dep.distance);
							sumScores += ProbTB(tempDep) * weight;
							sumWeights += weight;
						}
					}
				}
				else
				{
					statsCounter.IncrementCount("hSim");
					statsCounter.IncrementCount("aSim");
					statsCounter.IncrementCount("aSim&hSim");
					foreach (Triple<int, string, double> simArg in sim2arg)
					{
						for (int aTag = 0; aTag < numT; aTag++)
						{
							if (!StringBasicCategory(aTag).Equals(simArg.second))
							{
								continue;
							}
							IntTaggedWord tempArg = new IntTaggedWord(simArg.first, aTag);
							double probArg = Math.Exp(lex.Score(tempArg, 0, wordIndex.Get(tempArg.word), null));
							if (probArg == 0.0)
							{
								continue;
							}
							foreach (Triple<int, string, double> simHead in sim2head)
							{
								for (int hTag = 0; hTag < numT; hTag++)
								{
									if (!StringBasicCategory(hTag).Equals(simHead.second))
									{
										continue;
									}
									IntTaggedWord tempHead = new IntTaggedWord(simHead.first, aTag);
									IntDependency tempDep = new IntDependency(tempHead, tempArg, dep.leftHeaded, dep.distance);
									//double weight = (1-simHead.third) * (1-simArg.third);
									double weight = Math.Exp(-50 * simHead.third) * Math.Exp(-50 * simArg.third);
									sumScores += ProbTB(tempDep) * weight / probArg;
									sumWeights += weight;
								}
							}
						}
					}
				}
			}
			IntDependency temp = new IntDependency(dep.head, wildTW, dep.leftHeaded, dep.distance);
			double countHead = argCounter.GetCount(temp);
			double simProb;
			if (sim2arg == null)
			{
				simProb = sumScores / sumWeights;
			}
			else
			{
				double probArg = Math.Exp(lex.Score(dep.arg, 0, wordIndex.Get(dep.arg.word), null));
				simProb = probArg * sumScores / sumWeights;
			}
			if (simProb == 0)
			{
				statsCounter.IncrementCount("simProbZero");
			}
			if (regProb == 0)
			{
				//      log.info("zero reg prob");
				statsCounter.IncrementCount("regProbZero");
			}
			double smoothProb = (countHead * regProb + simSmooth * simProb) / (countHead + simSmooth);
			if (smoothProb == 0)
			{
				//      log.info("zero smooth prob");
				statsCounter.IncrementCount("smoothProbZero");
			}
			return smoothProb;
		}

		private string StringBasicCategory(int tag)
		{
			return tlp.BasicCategory(tagIndex.Get(tag));
		}
	}
}
