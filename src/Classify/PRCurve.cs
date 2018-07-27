using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// A class to create recall-precision curves given scores
	/// used to fit the best monotonic function for logistic regression and SVMs.
	/// </summary>
	/// <author>Kristina Toutanova</author>
	/// <version>May 23, 2005</version>
	public class PRCurve
	{
		internal double[] scores;

		internal int[] classes;

		internal int[] guesses;

		internal int[] numpositive;

		internal int[] numnegative;

		internal static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Classify.PRCurve));

		/// <summary>reads scores with classes from a file, sorts by score and creates the arrays</summary>
		public PRCurve(string filename)
		{
			//sorted scores
			// the class of example i
			// the guess of example i according to the argmax
			// number positive in the i-th highest scores
			// number negative in the i-th lowest scores
			try
			{
				List<Pair<double, int>> dataScores = new List<Pair<double, int>>();
				foreach (string line in ObjectBank.GetLineIterator(new File(filename)))
				{
					IList<string> elems = StringUtils.Split(line);
					Pair<double, int> p = new Pair<double, int>(double.ValueOf(elems[0]), int.Parse(elems[1]));
					dataScores.Add(p);
				}
				Init(dataScores);
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/// <summary>reads scores with classes from a file, sorts by score and creates the arrays</summary>
		public PRCurve(string filename, bool svm)
		{
			try
			{
				List<Pair<double, int>> dataScores = new List<Pair<double, int>>();
				foreach (string line in ObjectBank.GetLineIterator(new File(filename)))
				{
					IList<string> elems = StringUtils.Split(line);
					int cls = double.ValueOf(elems[0]);
					if (cls == -1)
					{
						cls = 0;
					}
					double score = double.ValueOf(elems[1]) + 0.5;
					Pair<double, int> p = new Pair<double, int>(score, int.Parse(cls));
					dataScores.Add(p);
				}
				Init(dataScores);
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		public virtual double OptimalAccuracy()
		{
			return Precision(NumSamples()) / (double)NumSamples();
		}

		public virtual double Accuracy()
		{
			return LogPrecision(NumSamples()) / (double)NumSamples();
		}

		public PRCurve(IList<Pair<double, int>> dataScores)
		{
			Init(dataScores);
		}

		public virtual void Init(IList<Pair<double, int>> dataScores)
		{
			IPriorityQueue<Pair<int, Pair<double, int>>> q = new BinaryHeapPriorityQueue<Pair<int, Pair<double, int>>>();
			for (int i = 0; i < dataScores.Count; i++)
			{
				q.Add(new Pair<int, Pair<double, int>>(int.Parse(i), dataScores[i]), -dataScores[i].First());
			}
			IList<Pair<int, Pair<double, int>>> sorted = q.ToSortedList();
			scores = new double[sorted.Count];
			classes = new int[sorted.Count];
			logger.Info("incoming size " + dataScores.Count + " resulting " + sorted.Count);
			for (int i_1 = 0; i_1 < sorted.Count; i_1++)
			{
				Pair<double, int> next = sorted[i_1].Second();
				scores[i_1] = next.First();
				classes[i_1] = next.Second();
			}
			Init();
		}

		public virtual void InitMC(List<Triple<double, int, int>> dataScores)
		{
			IPriorityQueue<Pair<int, Triple<double, int, int>>> q = new BinaryHeapPriorityQueue<Pair<int, Triple<double, int, int>>>();
			for (int i = 0; i < dataScores.Count; i++)
			{
				q.Add(new Pair<int, Triple<double, int, int>>(int.Parse(i), dataScores[i]), -dataScores[i].First());
			}
			IList<Pair<int, Triple<double, int, int>>> sorted = q.ToSortedList();
			scores = new double[sorted.Count];
			classes = new int[sorted.Count];
			guesses = new int[sorted.Count];
			logger.Info("incoming size " + dataScores.Count + " resulting " + sorted.Count);
			for (int i_1 = 0; i_1 < sorted.Count; i_1++)
			{
				Triple<double, int, int> next = sorted[i_1].Second();
				scores[i_1] = next.First();
				classes[i_1] = next.Second();
				guesses[i_1] = next.Third();
			}
			Init();
		}

		/// <summary>initialize the numpositive and the numnegative arrays</summary>
		internal virtual void Init()
		{
			numnegative = new int[NumSamples() + 1];
			numpositive = new int[NumSamples() + 1];
			numnegative[0] = 0;
			numpositive[0] = 0;
			int num = NumSamples();
			for (int i = 1; i <= num; i++)
			{
				numnegative[i] = numnegative[i - 1] + (classes[i - 1] == 0 ? 1 : 0);
			}
			for (int i_1 = 1; i_1 <= num; i_1++)
			{
				numpositive[i_1] = numpositive[i_1 - 1] + (classes[num - i_1] == 0 ? 0 : 1);
			}
			logger.Info("total positive " + numpositive[num] + " total negative " + numnegative[num] + " total " + num);
			for (int i_2 = 1; i_2 < numpositive.Length; i_2++)
			{
			}
		}

		//System.out.println(i + " positive " + numpositive[i] + " negative " + numnegative[i] + " classes " + classes[i - 1] + " " + classes[num - i]);
		internal virtual int NumSamples()
		{
			return scores.Length;
		}

		/// <summary>what is the best precision at the given recall</summary>
		public virtual int Precision(int recall)
		{
			int optimum = 0;
			for (int right = 0; right <= recall; right++)
			{
				int candidate = numpositive[right] + numnegative[recall - right];
				if (candidate > optimum)
				{
					optimum = candidate;
				}
			}
			return optimum;
		}

		public static double F1(int tp, int fp, int fn)
		{
			double prec = 1;
			double recall = 1;
			if (tp + fp > 0)
			{
				prec = tp / (double)(tp + fp);
			}
			if (tp + fn > 0)
			{
				recall = tp / (double)(tp + fn);
			}
			return 2 * prec * recall / (prec + recall);
		}

		/// <summary>the f-measure if we just guess as negative the first numleft and guess as positive the last numright</summary>
		public virtual double Fmeasure(int numleft, int numright)
		{
			int tp = numpositive[numright];
			int fp = numright - tp;
			int fn = numleft - numnegative[numleft];
			return F1(tp, fp, fn);
		}

		/// <summary>
		/// what is the precision at this recall if we look at the score as the probability of class 1 given x
		/// as if coming from logistic regression
		/// </summary>
		public virtual int LogPrecision(int recall)
		{
			int totaltaken = 0;
			int rightIndex = NumSamples() - 1;
			//next right candidate
			int leftIndex = 0;
			//next left candidate
			int totalcorrect = 0;
			while (totaltaken < recall)
			{
				double confr = Math.Abs(scores[rightIndex] - .5);
				double confl = Math.Abs(scores[leftIndex] - .5);
				int chosen = leftIndex;
				if (confr > confl)
				{
					chosen = rightIndex;
					rightIndex--;
				}
				else
				{
					leftIndex++;
				}
				//logger.info("chose "+chosen+" score "+scores[chosen]+" class "+classes[chosen]+" correct "+correct(scores[chosen],classes[chosen]));
				if ((scores[chosen] >= .5) && (classes[chosen] == 1))
				{
					totalcorrect++;
				}
				if ((scores[chosen] < .5) && (classes[chosen] == 0))
				{
					totalcorrect++;
				}
				totaltaken++;
			}
			return totalcorrect;
		}

		/// <summary>
		/// what is the optimal f-measure we can achieve given recall guesses
		/// using the optimal monotonic function
		/// </summary>
		public virtual double OptFmeasure(int recall)
		{
			double max = 0;
			for (int i = 0; i < (recall + 1); i++)
			{
				double f = Fmeasure(i, recall - i);
				if (f > max)
				{
					max = f;
				}
			}
			return max;
		}

		public virtual double OpFmeasure()
		{
			return OptFmeasure(NumSamples());
		}

		/// <summary>
		/// what is the f-measure at this recall if we look at the score as the probability of class 1 given x
		/// as if coming from logistic regression same as logPrecision but calculating f-measure
		/// </summary>
		/// <param name="recall">make this many guesses for which we are most confident</param>
		public virtual double Fmeasure(int recall)
		{
			int totaltaken = 0;
			int rightIndex = NumSamples() - 1;
			//next right candidate
			int leftIndex = 0;
			//next left candidate
			int tp = 0;
			int fp = 0;
			int fn = 0;
			while (totaltaken < recall)
			{
				double confr = Math.Abs(scores[rightIndex] - .5);
				double confl = Math.Abs(scores[leftIndex] - .5);
				int chosen = leftIndex;
				if (confr > confl)
				{
					chosen = rightIndex;
					rightIndex--;
				}
				else
				{
					leftIndex++;
				}
				//logger.info("chose "+chosen+" score "+scores[chosen]+" class "+classes[chosen]+" correct "+correct(scores[chosen],classes[chosen]));
				if ((scores[chosen] >= .5))
				{
					if (classes[chosen] == 1)
					{
						tp++;
					}
					else
					{
						fp++;
					}
				}
				if ((scores[chosen] < .5))
				{
					if (classes[chosen] == 1)
					{
						fn++;
					}
				}
				totaltaken++;
			}
			return F1(tp, fp, fn);
		}

		/// <summary>assuming the scores are probability of 1 given x</summary>
		public virtual double LogLikelihood()
		{
			double loglik = 0;
			for (int i = 0; i < scores.Length; i++)
			{
				loglik += Math.Log(classes[i] == 0 ? 1 - scores[i] : scores[i]);
			}
			return loglik;
		}

		/// <summary>confidence weighted accuracy assuming the scores are probabilities and using .5 as treshold</summary>
		public virtual double Cwa()
		{
			double acc = 0;
			for (int recall = 1; recall <= NumSamples(); recall++)
			{
				acc += LogPrecision(recall) / (double)recall;
			}
			return acc / NumSamples();
		}

		/// <summary>confidence weighted accuracy assuming the scores are probabilities and using .5 as treshold</summary>
		public virtual int[] CwaArray()
		{
			int[] arr = new int[NumSamples()];
			for (int recall = 1; recall <= NumSamples(); recall++)
			{
				arr[recall - 1] = LogPrecision(recall);
			}
			return arr;
		}

		/// <summary>confidence weighted accuracy assuming the scores are probabilities and using .5 as threshold</summary>
		public virtual int[] OptimalCwaArray()
		{
			int[] arr = new int[NumSamples()];
			for (int recall = 1; recall <= NumSamples(); recall++)
			{
				arr[recall - 1] = Precision(recall);
			}
			return arr;
		}

		/// <summary>optimal confidence weighted accuracy assuming for each recall we can fit an optimal monotonic function</summary>
		public virtual double OptimalCwa()
		{
			double acc = 0;
			for (int recall = 1; recall <= NumSamples(); recall++)
			{
				acc += Precision(recall) / (double)recall;
			}
			return acc / NumSamples();
		}

		public static bool Correct(double score, int cls)
		{
			return ((score >= .5) && (cls == 1)) || ((score < .5) && (cls == 0));
		}

		public static void Main(string[] args)
		{
			IPriorityQueue<string> q = new BinaryHeapPriorityQueue<string>();
			q.Add("bla", 2);
			q.Add("bla3", 2);
			logger.Info("size of q " + q.Count);
			Edu.Stanford.Nlp.Classify.PRCurve pr = new Edu.Stanford.Nlp.Classify.PRCurve("c:/data0204/precsvm", true);
			logger.Info("acc " + pr.Accuracy() + " opt " + pr.OptimalAccuracy() + " cwa " + pr.Cwa() + " optcwa " + pr.OptimalCwa());
			for (int r = 1; r <= pr.NumSamples(); r++)
			{
				logger.Info("optimal precision at recall " + r + " " + pr.Precision(r));
				logger.Info("model precision at recall " + r + " " + pr.LogPrecision(r));
			}
		}
	}
}
