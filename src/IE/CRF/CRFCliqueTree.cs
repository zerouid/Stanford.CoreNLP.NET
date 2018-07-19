using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>
	/// Builds a CliqueTree (an array of FactorTable) and does message passing
	/// inference along it.
	/// </summary>
	/// <?/>
	/// <author>Jenny Finkel</author>
	public class CRFCliqueTree<E> : IListeningSequenceModel
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFCliqueTree));

		private readonly FactorTable[] factorTables;

		private readonly double z;

		private readonly IIndex<E> classIndex;

		private readonly E backgroundSymbol;

		private readonly int backgroundIndex;

		private readonly int windowSize;

		private readonly int numClasses;

		private readonly int[] possibleValues;

		/// <summary>Initialize a clique tree.</summary>
		public CRFCliqueTree(FactorTable[] factorTables, IIndex<E> classIndex, E backgroundSymbol)
			: this(factorTables, classIndex, backgroundSymbol, factorTables[0].TotalMass())
		{
		}

		/// <summary>This extra constructor was added to support the CRFCliqueTreeForPartialLabels.</summary>
		internal CRFCliqueTree(FactorTable[] factorTables, IIndex<E> classIndex, E backgroundSymbol, double z)
		{
			// norm constant
			// the window size, which is also the clique size
			// the number of possible classes for each label
			this.factorTables = factorTables;
			this.z = z;
			this.classIndex = classIndex;
			this.backgroundSymbol = backgroundSymbol;
			backgroundIndex = classIndex.IndexOf(backgroundSymbol);
			windowSize = factorTables[0].WindowSize();
			numClasses = classIndex.Size();
			possibleValues = new int[numClasses];
			for (int i = 0; i < numClasses; i++)
			{
				possibleValues[i] = i;
			}
		}

		// Debug only
		// System.out.println("CRFCliqueTree constructed::numClasses: " +
		// numClasses);
		public virtual FactorTable[] GetFactorTables()
		{
			return this.factorTables;
		}

		public virtual IIndex<E> ClassIndex()
		{
			return classIndex;
		}

		// SEQUENCE MODEL METHODS
		public virtual int Length()
		{
			return factorTables.Length;
		}

		public virtual int LeftWindow()
		{
			return windowSize;
		}

		public virtual int RightWindow()
		{
			return 0;
		}

		public virtual int[] GetPossibleValues(int position)
		{
			return possibleValues;
		}

		public virtual double ScoreOf(int[] sequence, int pos)
		{
			return ScoresOf(sequence, pos)[sequence[pos]];
		}

		/// <summary>
		/// Computes the unnormalized log conditional distribution over values of the
		/// element at position pos in the sequence, conditioned on the values of the
		/// elements in all other positions of the provided sequence.
		/// </summary>
		/// <param name="sequence">the sequence containing the rest of the values to condition on</param>
		/// <param name="position">the position of the element to give a distribution for</param>
		/// <returns>
		/// an array of type double, representing a probability distribution;
		/// sums to 1.0
		/// </returns>
		public virtual double[] ScoresOf(int[] sequence, int position)
		{
			if (position >= factorTables.Length)
			{
				throw new Exception("Index out of bounds: " + position);
			}
			// DecimalFormat nf = new DecimalFormat("#0.000");
			// if (position>0 && position<sequence.length-1) System.out.println(position
			// + ": asking about " +sequence[position-1] + "(" + sequence[position] +
			// ")" + sequence[position+1]);
			double[] probThisGivenPrev = new double[numClasses];
			double[] probNextGivenThis = new double[numClasses];
			// double[] marginal = new double[numClasses]; // for debugging only
			// compute prob of this tag given the window-1 previous tags, normalized
			// extract the window-1 previous tags, pad left with background if necessary
			int prevLength = windowSize - 1;
			int[] prev = new int[prevLength + 1];
			// leave an extra element for the
			// label at this position
			int i = 0;
			for (; i < prevLength - position; i++)
			{
				// will only happen if
				// position-prevLength < 0
				prev[i] = classIndex.IndexOf(backgroundSymbol);
			}
			for (; i < prevLength; i++)
			{
				prev[i] = sequence[position - prevLength + i];
			}
			for (int label = 0; label < numClasses; label++)
			{
				prev[prev.Length - 1] = label;
				probThisGivenPrev[label] = factorTables[position].UnnormalizedLogProb(prev);
			}
			// marginal[label] = factorTables[position].logProbEnd(label); // remove:
			// for debugging only
			// ArrayMath.logNormalize(probThisGivenPrev);
			// compute the prob of the window-1 next tags given this tag
			// extract the window-1 next tags
			int nextLength = windowSize - 1;
			if (position + nextLength >= Length())
			{
				nextLength = Length() - position - 1;
			}
			FactorTable nextFactorTable = factorTables[position + nextLength];
			if (nextLength != windowSize - 1)
			{
				for (int j = 0; j < windowSize - 1 - nextLength; j++)
				{
					nextFactorTable = nextFactorTable.SumOutFront();
				}
			}
			if (nextLength == 0)
			{
				// we are asking about the prob of no sequence
				Arrays.Fill(probNextGivenThis, 1.0);
			}
			else
			{
				int[] next = new int[nextLength];
				System.Array.Copy(sequence, position + 1, next, 0, nextLength);
				for (int label_1 = 0; label_1 < numClasses; label_1++)
				{
					// ask the factor table such that pos is the first position in the
					// window
					// probNextGivenThis[label] =
					// factorTables[position+nextLength].conditionalLogProbGivenFirst(label,
					// next);
					// probNextGivenThis[label] =
					// nextFactorTable.conditionalLogProbGivenFirst(label, next);
					probNextGivenThis[label_1] = nextFactorTable.UnnormalizedConditionalLogProbGivenFirst(label_1, next);
				}
			}
			// pointwise multiply
			return ArrayMath.PairwiseAdd(probThisGivenPrev, probNextGivenThis);
		}

		/// <summary>Returns the log probability of this sequence given the CRF.</summary>
		/// <remarks>
		/// Returns the log probability of this sequence given the CRF. Does so by
		/// computing the marginal of the first windowSize tags, and then computing the
		/// conditional probability for the rest of them, conditioned on the previous
		/// tags.
		/// </remarks>
		/// <param name="sequence">The sequence to compute a score for</param>
		/// <returns>the score for the sequence</returns>
		public virtual double ScoreOf(int[] sequence)
		{
			int[] given = new int[Window() - 1];
			Arrays.Fill(given, classIndex.IndexOf(backgroundSymbol));
			double logProb = 0.0;
			for (int i = 0; i < length; i++)
			{
				int label = sequence[i];
				logProb += CondLogProbGivenPrevious(i, label, given);
				System.Array.Copy(given, 1, given, 0, given.Length - 1);
				given[given.Length - 1] = label;
			}
			return logProb;
		}

		// OTHER
		public virtual int Window()
		{
			return windowSize;
		}

		public virtual int GetNumClasses()
		{
			return numClasses;
		}

		public virtual double TotalMass()
		{
			return z;
		}

		public virtual int BackgroundIndex()
		{
			return backgroundIndex;
		}

		public virtual E BackgroundSymbol()
		{
			return backgroundSymbol;
		}

		//
		// MARGINAL PROB OF TAG AT SINGLE POSITION
		//
		public virtual double[][] LogProbTable()
		{
			double[][] result = new double[][] {  };
			for (int i = 0; i < Length(); i++)
			{
				result[i] = new double[classIndex.Size()];
				for (int j = 0; j < classIndex.Size(); j++)
				{
					result[i][j] = LogProb(i, j);
				}
			}
			return result;
		}

		/*
		* TODO(mengqiu) this function is buggy, should make sure label converts properly into int[] in cases where it's not 0-order label
		*/
		public virtual double LogProbStartPos()
		{
			double u = factorTables[0].UnnormalizedLogProbFront(backgroundIndex);
			return u - z;
		}

		public virtual double LogProb(int position, int label)
		{
			double u = factorTables[position].UnnormalizedLogProbEnd(label);
			return u - z;
		}

		public virtual double Prob(int position, int label)
		{
			return System.Math.Exp(LogProb(position, label));
		}

		public virtual double LogProb(int position, E label)
		{
			return LogProb(position, classIndex.IndexOf(label));
		}

		public virtual double Prob(int position, E label)
		{
			return System.Math.Exp(LogProb(position, label));
		}

		public virtual double[] ProbsToDoubleArr(int position)
		{
			double[] probs = new double[classIndex.Size()];
			for (int i = 0; i < sz; i++)
			{
				probs[i] = Prob(position, i);
			}
			return probs;
		}

		public virtual double[] LogProbsToDoubleArr(int position)
		{
			double[] probs = new double[classIndex.Size()];
			for (int i = 0; i < sz; i++)
			{
				probs[i] = LogProb(position, i);
			}
			return probs;
		}

		public virtual ICounter<E> Probs(int position)
		{
			ICounter<E> c = new ClassicCounter<E>();
			for (int i = 0; i < sz; i++)
			{
				E label = classIndex.Get(i);
				c.IncrementCount(label, Prob(position, i));
			}
			return c;
		}

		public virtual ICounter<E> LogProbs(int position)
		{
			ICounter<E> c = new ClassicCounter<E>();
			for (int i = 0; i < sz; i++)
			{
				E label = classIndex.Get(i);
				c.IncrementCount(label, LogProb(position, i));
			}
			return c;
		}

		//
		// MARGINAL PROBS OF TAGS AT MULTIPLE POSITIONS
		//
		/// <summary>
		/// returns the log probability for the given labels (indexed using
		/// classIndex), where the last label corresponds to the label at the specified
		/// position.
		/// </summary>
		/// <remarks>
		/// returns the log probability for the given labels (indexed using
		/// classIndex), where the last label corresponds to the label at the specified
		/// position. For instance if you called logProb(5, {1,2,3}) it will return the
		/// marginal log prob that the label at position 3 is 1, the label at position
		/// 4 is 2 and the label at position 5 is 3.
		/// </remarks>
		public virtual double LogProb(int position, int[] labels)
		{
			if (labels.Length < windowSize)
			{
				return factorTables[position].UnnormalizedLogProbEnd(labels) - z;
			}
			else
			{
				if (labels.Length == windowSize)
				{
					return factorTables[position].UnnormalizedLogProb(labels) - z;
				}
				else
				{
					int[] l = new int[windowSize];
					System.Array.Copy(labels, 0, l, 0, l.Length);
					int position1 = position - labels.Length + windowSize;
					double p = factorTables[position1].UnnormalizedLogProb(l) - z;
					l = new int[windowSize - 1];
					System.Array.Copy(labels, 1, l, 0, l.Length);
					position1++;
					for (int i = windowSize; i < labels.Length; i++)
					{
						p += CondLogProbGivenPrevious(position1++, labels[i], l);
						System.Array.Copy(l, 1, l, 0, l.Length - 1);
						l[windowSize - 2] = labels[i];
					}
					return p;
				}
			}
		}

		/// <summary>
		/// Returns the probability for the given labels (indexed using classIndex),
		/// where the last label corresponds to the label at the specified position.
		/// </summary>
		/// <remarks>
		/// Returns the probability for the given labels (indexed using classIndex),
		/// where the last label corresponds to the label at the specified position.
		/// For instance if you called prob(5, {1,2,3}) it will return the marginal
		/// prob that the label at position 3 is 1, the label at position 4 is 2 and
		/// the label at position 5 is 3.
		/// </remarks>
		public virtual double Prob(int position, int[] labels)
		{
			return System.Math.Exp(LogProb(position, labels));
		}

		/// <summary>
		/// returns the log probability for the given labels, where the last label
		/// corresponds to the label at the specified position.
		/// </summary>
		/// <remarks>
		/// returns the log probability for the given labels, where the last label
		/// corresponds to the label at the specified position. For instance if you
		/// called logProb(5, {"O", "PER", "ORG"}) it will return the marginal log prob
		/// that the label at position 3 is "O", the label at position 4 is "PER" and
		/// the label at position 5 is "ORG".
		/// </remarks>
		public virtual double LogProb(int position, E[] labels)
		{
			return LogProb(position, ObjectArrayToIntArray(labels));
		}

		/// <summary>
		/// returns the probability for the given labels, where the last label
		/// corresponds to the label at the specified position.
		/// </summary>
		/// <remarks>
		/// returns the probability for the given labels, where the last label
		/// corresponds to the label at the specified position. For instance if you
		/// called logProb(5, {"O", "PER", "ORG"}) it will return the marginal prob
		/// that the label at position 3 is "O", the label at position 4 is "PER" and
		/// the label at position 5 is "ORG".
		/// </remarks>
		public virtual double Prob(int position, E[] labels)
		{
			return System.Math.Exp(LogProb(position, labels));
		}

		public virtual GeneralizedCounter<E> LogProbs(int position, int window)
		{
			GeneralizedCounter<E> gc = new GeneralizedCounter<E>(window);
			int[] labels = new int[window];
			// cdm july 2005: below array initialization isn't necessary: JLS (3rd ed.)
			// 4.12.5
			// Arrays.fill(labels, 0);
			while (true)
			{
				IList<E> labelsList = IntArrayToListE(labels);
				gc.IncrementCount(labelsList, LogProb(position, labels));
				for (int i = 0; i < labels.Length; i++)
				{
					labels[i]++;
					if (labels[i] < numClasses)
					{
						break;
					}
					if (i == labels.Length - 1)
					{
						goto OUTER_break;
					}
					labels[i] = 0;
				}
OUTER_continue: ;
			}
OUTER_break: ;
			return gc;
		}

		public virtual GeneralizedCounter<E> Probs(int position, int window)
		{
			GeneralizedCounter<E> gc = new GeneralizedCounter<E>(window);
			int[] labels = new int[window];
			// cdm july 2005: below array initialization isn't necessary: JLS (3rd ed.)
			// 4.12.5
			// Arrays.fill(labels, 0);
			while (true)
			{
				IList<E> labelsList = IntArrayToListE(labels);
				gc.IncrementCount(labelsList, Prob(position, labels));
				for (int i = 0; i < labels.Length; i++)
				{
					labels[i]++;
					if (labels[i] < numClasses)
					{
						break;
					}
					if (i == labels.Length - 1)
					{
						goto OUTER_break;
					}
					labels[i] = 0;
				}
OUTER_continue: ;
			}
OUTER_break: ;
			return gc;
		}

		//
		// HELPER METHODS
		//
		private int[] ObjectArrayToIntArray(E[] os)
		{
			int[] @is = new int[os.Length];
			for (int i = 0; i < os.Length; i++)
			{
				@is[i] = classIndex.IndexOf(os[i]);
			}
			return @is;
		}

		private IList<E> IntArrayToListE(int[] @is)
		{
			IList<E> os = new List<E>(@is.Length);
			foreach (int i in @is)
			{
				os.Add(classIndex.Get(i));
			}
			return os;
		}

		/// <summary>
		/// Gives the probability of a tag at a single position conditioned on a
		/// sequence of previous labels.
		/// </summary>
		/// <param name="position">Index in sequence</param>
		/// <param name="label">Label of item at index</param>
		/// <param name="prevLabels">Indices of labels in previous positions</param>
		/// <returns>conditional log probability</returns>
		public virtual double CondLogProbGivenPrevious(int position, int label, int[] prevLabels)
		{
			if (prevLabels.Length + 1 == windowSize)
			{
				return factorTables[position].ConditionalLogProbGivenPrevious(prevLabels, label);
			}
			else
			{
				if (prevLabels.Length + 1 < windowSize)
				{
					FactorTable ft = factorTables[position].SumOutFront();
					while (ft.WindowSize() > prevLabels.Length + 1)
					{
						ft = ft.SumOutFront();
					}
					return ft.ConditionalLogProbGivenPrevious(prevLabels, label);
				}
				else
				{
					int[] p = new int[windowSize - 1];
					System.Array.Copy(prevLabels, prevLabels.Length - p.Length, p, 0, p.Length);
					return factorTables[position].ConditionalLogProbGivenPrevious(p, label);
				}
			}
		}

		public virtual double CondLogProbGivenPrevious(int position, E label, E[] prevLabels)
		{
			return CondLogProbGivenPrevious(position, classIndex.IndexOf(label), ObjectArrayToIntArray(prevLabels));
		}

		public virtual double CondProbGivenPrevious(int position, int label, int[] prevLabels)
		{
			return System.Math.Exp(CondLogProbGivenPrevious(position, label, prevLabels));
		}

		public virtual double CondProbGivenPrevious(int position, E label, E[] prevLabels)
		{
			return System.Math.Exp(CondLogProbGivenPrevious(position, label, prevLabels));
		}

		public virtual ICounter<E> CondLogProbsGivenPrevious(int position, int[] prevlabels)
		{
			ICounter<E> c = new ClassicCounter<E>();
			for (int i = 0; i < sz; i++)
			{
				E label = classIndex.Get(i);
				c.IncrementCount(label, CondLogProbGivenPrevious(position, i, prevlabels));
			}
			return c;
		}

		public virtual ICounter<E> CondLogProbsGivenPrevious(int position, E[] prevlabels)
		{
			ICounter<E> c = new ClassicCounter<E>();
			for (int i = 0; i < sz; i++)
			{
				E label = classIndex.Get(i);
				c.IncrementCount(label, CondLogProbGivenPrevious(position, label, prevlabels));
			}
			return c;
		}

		//
		// PROB OF TAG AT SINGLE POSITION CONDITIONED ON FOLLOWING SEQUENCE OF LABELS
		//
		public virtual double CondLogProbGivenNext(int position, int label, int[] nextLabels)
		{
			position = position + nextLabels.Length;
			if (nextLabels.Length + 1 == windowSize)
			{
				return factorTables[position].ConditionalLogProbGivenNext(nextLabels, label);
			}
			else
			{
				if (nextLabels.Length + 1 < windowSize)
				{
					FactorTable ft = factorTables[position].SumOutFront();
					while (ft.WindowSize() > nextLabels.Length + 1)
					{
						ft = ft.SumOutFront();
					}
					return ft.ConditionalLogProbGivenPrevious(nextLabels, label);
				}
				else
				{
					int[] p = new int[windowSize - 1];
					System.Array.Copy(nextLabels, 0, p, 0, p.Length);
					return factorTables[position].ConditionalLogProbGivenPrevious(p, label);
				}
			}
		}

		public virtual double CondLogProbGivenNext(int position, E label, E[] nextLabels)
		{
			return CondLogProbGivenNext(position, classIndex.IndexOf(label), ObjectArrayToIntArray(nextLabels));
		}

		public virtual double CondProbGivenNext(int position, int label, int[] nextLabels)
		{
			return System.Math.Exp(CondLogProbGivenNext(position, label, nextLabels));
		}

		public virtual double CondProbGivenNext(int position, E label, E[] nextLabels)
		{
			return System.Math.Exp(CondLogProbGivenNext(position, label, nextLabels));
		}

		public virtual ICounter<E> CondLogProbsGivenNext(int position, int[] nextlabels)
		{
			ICounter<E> c = new ClassicCounter<E>();
			for (int i = 0; i < sz; i++)
			{
				E label = classIndex.Get(i);
				c.IncrementCount(label, CondLogProbGivenNext(position, i, nextlabels));
			}
			return c;
		}

		public virtual ICounter<E> CondLogProbsGivenNext(int position, E[] nextlabels)
		{
			ICounter<E> c = new ClassicCounter<E>();
			for (int i = 0; i < sz; i++)
			{
				E label = classIndex.Get(i);
				c.IncrementCount(label, CondLogProbGivenNext(position, label, nextlabels));
			}
			return c;
		}

		//
		// PROB OF TAG AT SINGLE POSITION CONDITIONED ON PREVIOUS AND FOLLOWING
		// SEQUENCE OF LABELS
		//
		// public double condProbGivenPreviousAndNext(int position, int label, int[]
		// prevLabels, int[] nextLabels) {
		// }
		//
		// JOINT CONDITIONAL PROBS
		//
		/// <returns>a new CRFCliqueTree for the weights on the data</returns>
		public static Edu.Stanford.Nlp.IE.Crf.CRFCliqueTree<E> GetCalibratedCliqueTree<E>(int[][][] data, IList<IIndex<CRFLabel>> labelIndices, int numClasses, IIndex<E> classIndex, E backgroundSymbol, ICliquePotentialFunction cliquePotentialFunc, double
			[][][] featureVals)
		{
			FactorTable[] factorTables = new FactorTable[data.Length];
			FactorTable[] messages = new FactorTable[data.Length - 1];
			for (int i = 0; i < data.Length; i++)
			{
				double[][] featureValByCliqueSize = null;
				if (featureVals != null)
				{
					featureValByCliqueSize = featureVals[i];
				}
				factorTables[i] = GetFactorTable(data[i], labelIndices, numClasses, cliquePotentialFunc, featureValByCliqueSize, i);
				// log.info("before calibration,FT["+i+"] = " + factorTables[i].toProbString());
				if (i > 0)
				{
					messages[i - 1] = factorTables[i - 1].SumOutFront();
					// log.info("forward message, message["+(i-1)+"] = " + messages[i-1].toProbString());
					factorTables[i].MultiplyInFront(messages[i - 1]);
				}
			}
			// log.info("after forward calibration, FT["+i+"] = " + factorTables[i].toProbString());
			for (int i_1 = factorTables.Length - 2; i_1 >= 0; i_1--)
			{
				FactorTable summedOut = factorTables[i_1 + 1].SumOutEnd();
				summedOut.DivideBy(messages[i_1]);
				// log.info("backward summedOut, summedOut= " + summedOut.toProbString());
				factorTables[i_1].MultiplyInEnd(summedOut);
			}
			// log.info("after backward calibration, FT["+i+"] = " + factorTables[i].toProbString());
			return new Edu.Stanford.Nlp.IE.Crf.CRFCliqueTree<E>(factorTables, classIndex, backgroundSymbol);
		}

		/// <summary>This function assumes a LinearCliquePotentialFunction is used for wrapping the weights</summary>
		/// <returns>a new CRFCliqueTree for the weights on the data</returns>
		public static Edu.Stanford.Nlp.IE.Crf.CRFCliqueTree<E> GetCalibratedCliqueTree<E>(double[] weights, double wscale, int[][] weightIndices, int[][][] data, IList<IIndex<CRFLabel>> labelIndices, int numClasses, IIndex<E> classIndex, E backgroundSymbol
			)
		{
			FactorTable[] factorTables = new FactorTable[data.Length];
			FactorTable[] messages = new FactorTable[data.Length - 1];
			for (int i = 0; i < data.Length; i++)
			{
				factorTables[i] = GetFactorTable(weights, wscale, weightIndices, data[i], labelIndices, numClasses);
				if (i > 0)
				{
					messages[i - 1] = factorTables[i - 1].SumOutFront();
					factorTables[i].MultiplyInFront(messages[i - 1]);
				}
			}
			for (int i_1 = factorTables.Length - 2; i_1 >= 0; i_1--)
			{
				FactorTable summedOut = factorTables[i_1 + 1].SumOutEnd();
				summedOut.DivideBy(messages[i_1]);
				factorTables[i_1].MultiplyInEnd(summedOut);
			}
			return new Edu.Stanford.Nlp.IE.Crf.CRFCliqueTree<E>(factorTables, classIndex, backgroundSymbol);
		}

		private static FactorTable GetFactorTable(double[] weights, double wScale, int[][] weightIndices, int[][] data, IList<IIndex<CRFLabel>> labelIndices, int numClasses)
		{
			FactorTable factorTable = null;
			for (int j = 0; j < sz; j++)
			{
				IIndex<CRFLabel> labelIndex = labelIndices[j];
				FactorTable ft = new FactorTable(numClasses, j + 1);
				// ... and each possible labeling for that clique
				for (int k = 0; k < liSize; k++)
				{
					int[] label = labelIndex.Get(k).GetLabel();
					double weight = 0.0;
					for (int m = 0; m < data[j].Length; m++)
					{
						int wi = weightIndices[data[j][m]][k];
						weight += wScale * weights[wi];
					}
					// try{
					ft.SetValue(label, weight);
				}
				// } catch (Exception e) {
				// System.out.println("CRFCliqueTree::getFactorTable");
				// System.out.println("NumClasses: " + numClasses + " j+1: " + (j+1));
				// System.out.println("k: " + k+" label: " +label+" labelIndexSize: " +
				// labelIndex.size());
				// throw new RunTimeException(e.toString());
				// }
				if (j > 0)
				{
					ft.MultiplyInEnd(factorTable);
				}
				factorTable = ft;
			}
			return factorTable;
		}

		// static FactorTable getFactorTable(double[][] weights, int[][] data, List<Index<CRFLabel>> labelIndices, int numClasses, int posInSent) {
		//   CliquePotentialFunction cliquePotentialFunc = new LinearCliquePotentialFunction(weights);
		//   return getFactorTable(data, labelIndices, numClasses, cliquePotentialFunc, null, posInSent);
		// }
		internal static FactorTable GetFactorTable(int[][] data, IList<IIndex<CRFLabel>> labelIndices, int numClasses, ICliquePotentialFunction cliquePotentialFunc, double[][] featureValByCliqueSize, int posInSent)
		{
			FactorTable factorTable = null;
			for (int j = 0; j < sz; j++)
			{
				IIndex<CRFLabel> labelIndex = labelIndices[j];
				FactorTable ft = new FactorTable(numClasses, j + 1);
				double[] featureVal = null;
				if (featureValByCliqueSize != null)
				{
					featureVal = featureValByCliqueSize[j];
				}
				// ... and each possible labeling for that clique
				for (int k = 0; k < liSize; k++)
				{
					int[] label = labelIndex.Get(k).GetLabel();
					double cliquePotential = cliquePotentialFunc.ComputeCliquePotential(j + 1, k, data[j], featureVal, posInSent);
					// for (int m = 0; m < data[j].length; m++) {
					//   weight += weights[data[j][m]][k];
					// }
					// try{
					ft.SetValue(label, cliquePotential);
				}
				// } catch (Exception e) {
				// System.out.println("CRFCliqueTree::getFactorTable");
				// System.out.println("NumClasses: " + numClasses + " j+1: " + (j+1));
				// System.out.println("k: " + k+" label: " +label+" labelIndexSize: " +
				// labelIndex.size());
				// throw new RunTimeException(e.toString());
				// }
				if (j > 0)
				{
					ft.MultiplyInEnd(factorTable);
				}
				factorTable = ft;
			}
			return factorTable;
		}

		// SEQUENCE MODEL METHODS
		/// <summary>
		/// Computes the distribution over values of the element at position pos in the
		/// sequence, conditioned on the values of the elements in all other positions
		/// of the provided sequence.
		/// </summary>
		/// <param name="sequence">the sequence containing the rest of the values to condition on</param>
		/// <param name="position">the position of the element to give a distribution for</param>
		/// <returns>
		/// an array of type double, representing a probability distribution;
		/// sums to 1.0
		/// </returns>
		public virtual double[] GetConditionalDistribution(int[] sequence, int position)
		{
			double[] result = ScoresOf(sequence, position);
			ArrayMath.LogNormalize(result);
			// System.out.println("marginal:          " + ArrayMath.toString(marginal,
			// nf));
			// System.out.println("conditional:       " + ArrayMath.toString(result,
			// nf));
			result = ArrayMath.Exp(result);
			// System.out.println("conditional:       " + ArrayMath.toString(result,
			// nf));
			return result;
		}

		/// <summary>
		/// Informs this sequence model that the value of the element at position pos
		/// has changed.
		/// </summary>
		/// <remarks>
		/// Informs this sequence model that the value of the element at position pos
		/// has changed. This allows this sequence model to update its internal model
		/// if desired.
		/// </remarks>
		public virtual void UpdateSequenceElement(int[] sequence, int pos, int oldVal)
		{
		}

		// do nothing; we don't change this model
		/// <summary>
		/// Informs this sequence model that the value of the whole sequence is
		/// initialized to sequence
		/// </summary>
		public virtual void SetInitialSequence(int[] sequence)
		{
		}

		// do nothing
		/// <returns>
		/// the number of possible values for each element; it is assumed to be
		/// the same for the element at each position
		/// </returns>
		public virtual int GetNumValues()
		{
			return numClasses;
		}
	}
}
