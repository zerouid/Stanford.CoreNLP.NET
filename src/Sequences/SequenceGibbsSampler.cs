using System.Collections;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>A Gibbs sampler for sequence models.</summary>
	/// <remarks>
	/// A Gibbs sampler for sequence models. Given a sequence model implementing the SequenceModel
	/// interface, this class is capable of
	/// sampling sequences from the distribution over sequences that it defines. It can also use
	/// this sampling procedure to find the best sequence.
	/// </remarks>
	/// <author>grenager</author>
	public class SequenceGibbsSampler : IBestSequenceFinder
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Sequences.SequenceGibbsSampler));

		private static Random random = new Random(2147483647L);

		public static int verbose = 0;

		private IList document;

		private int numSamples;

		private int sampleInterval;

		private int speedUpThreshold = -1;

		private ISequenceListener listener;

		private const int RandomSampling = 0;

		private const int SequentialSampling = 1;

		private const int ChromaticSampling = 2;

		internal EmpiricalNERPriorBIO priorEn;

		internal EmpiricalNERPriorBIO priorCh = null;

		public bool returnLastFoundSequence = false;

		private int samplingStyle;

		private int chromaticSize;

		private IList<IList<int>> partition;

		//debug
		// TODO: change so that it uses the scoresOf() method properly
		// a random number generator
		//debug
		// determines how many parallel threads to run in chromatic sampling
		public static int[] Copy(int[] a)
		{
			int[] result = new int[a.Length];
			System.Array.Copy(a, 0, result, 0, a.Length);
			return result;
		}

		public static int[] GetRandomSequence(ISequenceModel model)
		{
			int[] result = new int[model.Length()];
			for (int i = 0; i < result.Length; i++)
			{
				int[] classes = model.GetPossibleValues(i);
				result[i] = classes[random.NextInt(classes.Length)];
			}
			return result;
		}

		/// <summary>
		/// Finds the best sequence by collecting numSamples samples, scoring them, and then choosing
		/// the highest scoring sample.
		/// </summary>
		/// <returns>the array of type int representing the highest scoring sequence</returns>
		public virtual int[] BestSequence(ISequenceModel model)
		{
			int[] initialSequence = GetRandomSequence(model);
			return FindBestUsingSampling(model, numSamples, sampleInterval, initialSequence);
		}

		/// <summary>
		/// Finds the best sequence by collecting numSamples samples, scoring them, and then choosing
		/// the highest scoring sample.
		/// </summary>
		/// <returns>the array of type int representing the highest scoring sequence</returns>
		public virtual int[] FindBestUsingSampling(ISequenceModel model, int numSamples, int sampleInterval, int[] initialSequence)
		{
			IList samples = CollectSamples(model, numSamples, sampleInterval, initialSequence);
			int[] best = null;
			double bestScore = double.NegativeInfinity;
			foreach (object sample in samples)
			{
				int[] sequence = (int[])sample;
				double score = model.ScoreOf(sequence);
				if (score > bestScore)
				{
					best = sequence;
					bestScore = score;
					log.Info("found new best (" + bestScore + ")");
					log.Info(ArrayMath.ToString(best));
				}
			}
			return best;
		}

		public virtual int[] FindBestUsingAnnealing(ISequenceModel model, CoolingSchedule schedule)
		{
			int[] initialSequence = GetRandomSequence(model);
			return FindBestUsingAnnealing(model, schedule, initialSequence);
		}

		public virtual int[] FindBestUsingAnnealing(ISequenceModel model, CoolingSchedule schedule, int[] initialSequence)
		{
			if (verbose > 0)
			{
				log.Info("Doing annealing");
			}
			listener.SetInitialSequence(initialSequence);
			IList result = new ArrayList();
			// so we don't change the initial, or the one we just stored
			int[] sequence = Copy(initialSequence);
			int[] best = null;
			double bestScore = double.NegativeInfinity;
			double score = double.NegativeInfinity;
			// if (!returnLastFoundSequence) {
			//   score = model.scoreOf(sequence);
			// }
			ICollection<int> positionsChanged = null;
			if (speedUpThreshold > 0)
			{
				positionsChanged = Generics.NewHashSet();
			}
			for (int i = 0; i < schedule.NumIterations(); i++)
			{
				if (Thread.Interrupted())
				{
					// Allow interrupting the parser
					throw new RuntimeInterruptedException();
				}
				double temperature = schedule.GetTemperature(i);
				if (speedUpThreshold <= 0)
				{
					score = SampleSequenceForward(model, sequence, temperature, null);
				}
				else
				{
					// modifies tagSequence
					if (i < speedUpThreshold)
					{
						score = SampleSequenceForward(model, sequence, temperature, null);
						// modifies tagSequence
						for (int j = 0; j < sequence.Length; j++)
						{
							if (sequence[j] != initialSequence[j])
							{
								positionsChanged.Add(j);
							}
						}
					}
					else
					{
						score = SampleSequenceForward(model, sequence, temperature, positionsChanged);
					}
				}
				// modifies tagSequence
				result.Add(sequence);
				if (returnLastFoundSequence)
				{
					best = sequence;
				}
				else
				{
					// score = model.scoreOf(sequence);
					//log.info(i+" "+score+" "+Arrays.toString(sequence));
					if (score > bestScore)
					{
						best = sequence;
						bestScore = score;
					}
				}
				if (i % 50 == 0)
				{
					if (verbose > 1)
					{
						log.Info("itr " + i + ": " + bestScore + "\t");
					}
				}
				if (verbose > 0)
				{
					log.Info(".");
				}
			}
			if (verbose > 1)
			{
				log.Info();
				PrintSamples(result, System.Console.Error);
			}
			if (verbose > 0)
			{
				log.Info("done.");
			}
			//return sequence;
			return best;
		}

		/// <summary>
		/// Collects numSamples samples of sequences, from the distribution over sequences defined
		/// by the sequence model passed on construction.
		/// </summary>
		/// <remarks>
		/// Collects numSamples samples of sequences, from the distribution over sequences defined
		/// by the sequence model passed on construction.
		/// All samples collected are sampleInterval samples apart, in an attempt to reduce
		/// autocorrelation.
		/// </remarks>
		/// <returns>a List containing the sequence samples, as arrays of type int, and their scores</returns>
		public virtual IList<int[]> CollectSamples(ISequenceModel model, int numSamples, int sampleInterval)
		{
			int[] initialSequence = GetRandomSequence(model);
			return CollectSamples(model, numSamples, sampleInterval, initialSequence);
		}

		/// <summary>
		/// Collects numSamples samples of sequences, from the distribution over sequences defined
		/// by the sequence model passed on construction.
		/// </summary>
		/// <remarks>
		/// Collects numSamples samples of sequences, from the distribution over sequences defined
		/// by the sequence model passed on construction.
		/// All samples collected are sampleInterval samples apart, in an attempt to reduce
		/// autocorrelation.
		/// </remarks>
		/// <returns>a Counter containing the sequence samples, as arrays of type int, and their scores</returns>
		public virtual IList<int[]> CollectSamples(ISequenceModel model, int numSamples, int sampleInterval, int[] initialSequence)
		{
			if (verbose > 0)
			{
				log.Info("Collecting samples");
			}
			listener.SetInitialSequence(initialSequence);
			IList<int[]> result = new List<int[]>();
			int[] sequence = initialSequence;
			for (int i = 0; i < numSamples; i++)
			{
				sequence = Copy(sequence);
				// so we don't change the initial, or the one we just stored
				SampleSequenceRepeatedly(model, sequence, sampleInterval);
				// modifies tagSequence
				result.Add(sequence);
				// save it to return later
				if (verbose > 0)
				{
					log.Info(".");
				}
				System.Console.Error.Flush();
			}
			if (verbose > 1)
			{
				log.Info();
				PrintSamples(result, System.Console.Error);
			}
			if (verbose > 0)
			{
				log.Info("done.");
			}
			return result;
		}

		/// <summary>Samples the sequence repeatedly, making numSamples passes over the entire sequence.</summary>
		public virtual double SampleSequenceRepeatedly(ISequenceModel model, int[] sequence, int numSamples)
		{
			sequence = Copy(sequence);
			// so we don't change the initial, or the one we just stored
			listener.SetInitialSequence(sequence);
			double returnScore = double.NegativeInfinity;
			for (int iter = 0; iter < numSamples; iter++)
			{
				returnScore = SampleSequenceForward(model, sequence);
			}
			return returnScore;
		}

		/// <summary>Samples the sequence repeatedly, making numSamples passes over the entire sequence.</summary>
		/// <remarks>
		/// Samples the sequence repeatedly, making numSamples passes over the entire sequence.
		/// Destructively modifies the sequence in place.
		/// </remarks>
		public virtual double SampleSequenceRepeatedly(ISequenceModel model, int numSamples)
		{
			int[] sequence = GetRandomSequence(model);
			return SampleSequenceRepeatedly(model, sequence, numSamples);
		}

		/// <summary>
		/// Samples the complete sequence once in the forward direction
		/// Destructively modifies the sequence in place.
		/// </summary>
		/// <param name="sequence">the sequence to start with.</param>
		public virtual double SampleSequenceForward(ISequenceModel model, int[] sequence)
		{
			return SampleSequenceForward(model, sequence, 1.0, null);
		}

		/// <summary>
		/// Samples the complete sequence once in the forward direction
		/// Destructively modifies the sequence in place.
		/// </summary>
		/// <param name="sequence">the sequence to start with.</param>
		public virtual double SampleSequenceForward(ISequenceModel model, int[] sequence, double temperature, ICollection<int> onlySampleThesePositions)
		{
			double returnScore = double.NegativeInfinity;
			// log.info("Sampling forward");
			if (onlySampleThesePositions != null)
			{
				foreach (int pos in onlySampleThesePositions)
				{
					returnScore = SamplePosition(model, sequence, pos, temperature);
				}
			}
			else
			{
				if (samplingStyle == SequentialSampling)
				{
					for (int pos = 0; pos < sequence.Length; pos++)
					{
						returnScore = SamplePosition(model, sequence, pos, temperature);
					}
				}
				else
				{
					if (samplingStyle == RandomSampling)
					{
						foreach (int aSequence in sequence)
						{
							int pos = random.NextInt(sequence.Length);
							returnScore = SamplePosition(model, sequence, pos, temperature);
						}
					}
					else
					{
						if (samplingStyle == ChromaticSampling)
						{
							// make copies of the sequences and merge at the end
							IList<Pair<int, int>> results = new List<Pair<int, int>>();
							foreach (IList<int> indieList in partition)
							{
								if (indieList.Count <= chromaticSize)
								{
									foreach (int pos in indieList)
									{
										Pair<int, double> newPosProb = SamplePositionHelper(model, sequence, pos, temperature);
										sequence[pos] = newPosProb.First();
									}
								}
								else
								{
									MulticoreWrapper<IList<int>, IList<Pair<int, int>>> wrapper = new MulticoreWrapper<IList<int>, IList<Pair<int, int>>>(chromaticSize, new _IThreadsafeProcessor_269(this, model, sequence, temperature));
									// returns the position to sample in first place and new label in second place
									results.Clear();
									int interval = System.Math.Max(1, indieList.Count / chromaticSize);
									for (int begin = 0; end < indieListSize; begin += interval)
									{
										end = System.Math.Min(begin + interval, indieListSize);
										wrapper.Put(indieList.SubList(begin, end));
										while (wrapper.Peek())
										{
											Sharpen.Collections.AddAll(results, wrapper.Poll());
										}
									}
									wrapper.Join();
									while (wrapper.Peek())
									{
										Sharpen.Collections.AddAll(results, wrapper.Poll());
									}
									foreach (Pair<int, int> posVal in results)
									{
										sequence[posVal.First()] = posVal.Second();
									}
								}
							}
							returnScore = model.ScoreOf(sequence);
						}
					}
				}
			}
			return returnScore;
		}

		private sealed class _IThreadsafeProcessor_269 : IThreadsafeProcessor<IList<int>, IList<Pair<int, int>>>
		{
			public _IThreadsafeProcessor_269(SequenceGibbsSampler _enclosing, ISequenceModel model, int[] sequence, double temperature)
			{
				this._enclosing = _enclosing;
				this.model = model;
				this.sequence = sequence;
				this.temperature = temperature;
			}

			public IList<Pair<int, int>> Process(IList<int> posList)
			{
				IList<Pair<int, int>> allPos = new List<Pair<int, int>>(posList.Count);
				Pair<int, double> newPosProb = null;
				foreach (int pos in posList)
				{
					newPosProb = this._enclosing.SamplePositionHelper(model, sequence, pos, temperature);
					allPos.Add(new Pair<int, int>(pos, newPosProb.First()));
				}
				return allPos;
			}

			public IThreadsafeProcessor<IList<int>, IList<Pair<int, int>>> NewInstance()
			{
				return this;
			}

			private readonly SequenceGibbsSampler _enclosing;

			private readonly ISequenceModel model;

			private readonly int[] sequence;

			private readonly double temperature;
		}

		/// <summary>
		/// Samples the complete sequence once in the backward direction
		/// Destructively modifies the sequence in place.
		/// </summary>
		/// <param name="sequence">the sequence to start with.</param>
		public virtual double SampleSequenceBackward(ISequenceModel model, int[] sequence)
		{
			return SampleSequenceBackward(model, sequence, 1.0);
		}

		/// <summary>
		/// Samples the complete sequence once in the backward direction
		/// Destructively modifies the sequence in place.
		/// </summary>
		/// <param name="sequence">the sequence to start with.</param>
		public virtual double SampleSequenceBackward(ISequenceModel model, int[] sequence, double temperature)
		{
			double returnScore = double.NegativeInfinity;
			for (int pos = sequence.Length - 1; pos >= 0; pos--)
			{
				returnScore = SamplePosition(model, sequence, pos, temperature);
			}
			return returnScore;
		}

		/// <summary>Samples a single position in the sequence.</summary>
		/// <remarks>
		/// Samples a single position in the sequence.
		/// Destructively modifies the sequence in place.
		/// returns the score of the new sequence
		/// </remarks>
		/// <param name="sequence">the sequence to start with</param>
		/// <param name="pos">the position to sample.</param>
		public virtual double SamplePosition(ISequenceModel model, int[] sequence, int pos)
		{
			return SamplePosition(model, sequence, pos, 1.0);
		}

		/// <summary>Samples a single position in the sequence.</summary>
		/// <remarks>
		/// Samples a single position in the sequence.
		/// Does not modify the sequence passed in.
		/// returns the score of the new label for the position to sample
		/// </remarks>
		/// <param name="sequence">the sequence to start with</param>
		/// <param name="pos">the position to sample.</param>
		/// <param name="temperature">the temperature to control annealing</param>
		private Pair<int, double> SamplePositionHelper(ISequenceModel model, int[] sequence, int pos, double temperature)
		{
			double[] distribution = model.ScoresOf(sequence, pos);
			if (temperature != 1.0)
			{
				if (temperature == 0.0)
				{
					// set the max to 1.0
					int argmax = ArrayMath.Argmax(distribution);
					Arrays.Fill(distribution, double.NegativeInfinity);
					distribution[argmax] = 0.0;
				}
				else
				{
					// take all to a power
					// use the temperature to increase/decrease the entropy of the sampling distribution
					ArrayMath.MultiplyInPlace(distribution, 1.0 / temperature);
				}
			}
			ArrayMath.LogNormalize(distribution);
			ArrayMath.ExpInPlace(distribution);
			int newTag = ArrayMath.SampleFromDistribution(distribution, random);
			double newProb = distribution[newTag];
			return new Pair<int, double>(newTag, newProb);
		}

		/// <summary>Samples a single position in the sequence.</summary>
		/// <remarks>
		/// Samples a single position in the sequence.
		/// Destructively modifies the sequence in place.
		/// returns the score of the new sequence
		/// </remarks>
		/// <param name="sequence">the sequence to start with</param>
		/// <param name="pos">the position to sample.</param>
		/// <param name="temperature">the temperature to control annealing</param>
		public virtual double SamplePosition(ISequenceModel model, int[] sequence, int pos, double temperature)
		{
			int oldTag = sequence[pos];
			Pair<int, double> newPosProb = SamplePositionHelper(model, sequence, pos, temperature);
			int newTag = newPosProb.First();
			//    System.out.println("Sampled " + oldTag + "->" + newTag);
			sequence[pos] = newTag;
			listener.UpdateSequenceElement(sequence, pos, oldTag);
			return newPosProb.Second();
		}

		public virtual void PrintSamples(IList samples, TextWriter @out)
		{
			for (int i = 0; i < document.Count; i++)
			{
				IHasWord word = (IHasWord)document[i];
				string s = "null";
				if (word != null)
				{
					s = word.Word();
				}
				@out.Write(StringUtils.PadOrTrim(s, 10));
				foreach (object sample in samples)
				{
					int[] sequence = (int[])sample;
					@out.Write(" " + StringUtils.PadLeft(sequence[i], 2));
				}
				@out.WriteLine();
			}
		}

		/// <param name="document">the underlying document which is a list of HasWord; a slight abstraction violation, but useful for debugging!!</param>
		public SequenceGibbsSampler(int numSamples, int sampleInterval, ISequenceListener listener, IList document, bool returnLastFoundSequence, int samplingStyle, int chromaticSize, IList<IList<int>> partition, int speedUpThreshold, EmpiricalNERPriorBIO
			 priorEn, EmpiricalNERPriorBIO priorCh)
		{
			this.numSamples = numSamples;
			this.sampleInterval = sampleInterval;
			this.listener = listener;
			this.document = document;
			this.returnLastFoundSequence = returnLastFoundSequence;
			this.samplingStyle = samplingStyle;
			if (verbose > 0)
			{
				if (samplingStyle == RandomSampling)
				{
					log.Info("Using random sampling");
				}
				else
				{
					if (samplingStyle == ChromaticSampling)
					{
						log.Info("Using chromatic sampling with " + chromaticSize + " threads");
					}
					else
					{
						if (samplingStyle == SequentialSampling)
						{
							log.Info("Using sequential sampling");
						}
					}
				}
			}
			this.chromaticSize = chromaticSize;
			this.partition = partition;
			this.speedUpThreshold = speedUpThreshold;
			//debug
			this.priorEn = priorEn;
			this.priorCh = priorCh;
		}

		public SequenceGibbsSampler(int numSamples, int sampleInterval, ISequenceListener listener, IList document)
			: this(numSamples, sampleInterval, listener, document, false, 1, 0, null, -1, null, null)
		{
		}

		public SequenceGibbsSampler(int numSamples, int sampleInterval, ISequenceListener listener)
			: this(numSamples, sampleInterval, listener, null)
		{
		}

		public SequenceGibbsSampler(int numSamples, int sampleInterval, ISequenceListener listener, int samplingStyle, int chromaticSize, IList<IList<int>> partition, int speedUpThreshold, EmpiricalNERPriorBIO priorEn, EmpiricalNERPriorBIO priorCh)
			: this(numSamples, sampleInterval, listener, null, false, samplingStyle, chromaticSize, partition, speedUpThreshold, priorEn, priorCh)
		{
		}
	}
}
