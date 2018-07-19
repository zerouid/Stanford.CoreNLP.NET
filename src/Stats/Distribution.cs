using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Text;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// Immutable class for representing normalized, smoothed discrete distributions
	/// from
	/// <see cref="Counters"/>
	/// . Smoothed counters reserve probability mass for unseen
	/// items, so queries for the probability of unseen items will return a small
	/// positive amount.  Normalization is L1 normalization:
	/// <see cref="Distribution{E}.TotalCount()"/>
	/// should always return 1.
	/// <p>
	/// A Counter passed into a constructor is copied. This class is Serializable.
	/// </summary>
	/// <author>Galen Andrew (galand@cs.stanford.edu), Sebastian Pado</author>
	[System.Serializable]
	public class Distribution<E> : ISampler<E>, IProbabilityDistribution<E>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Stats.Distribution));

		private const long serialVersionUID = 6707148234288637809L;

		private int numberOfKeys;

		private double reservedMass;

		protected internal ICounter<E> counter;

		private const int NumEntriesInString = 20;

		private const bool verbose = false;

		// todo [cdm Apr 2013]: Make these 3 variables final and put into constructor
		public virtual ICounter<E> GetCounter()
		{
			return counter;
		}

		/// <summary>Exactly the same as sampleFrom(), needed for the Sampler interface.</summary>
		public virtual E DrawSample()
		{
			return SampleFrom();
		}

		/// <summary>A method to draw a sample, providing an own random number generator.</summary>
		/// <remarks>
		/// A method to draw a sample, providing an own random number generator.
		/// Needed for the ProbabilityDistribution interface.
		/// </remarks>
		public virtual E DrawSample(Random random)
		{
			return SampleFrom(random);
		}

		public virtual string ToString(NumberFormat nf)
		{
			return Counters.ToString(counter, nf);
		}

		public virtual double GetReservedMass()
		{
			return reservedMass;
		}

		public virtual int GetNumberOfKeys()
		{
			return numberOfKeys;
		}

		//--- cdm added Jan 2004 to help old code compile
		public virtual ICollection<E> KeySet()
		{
			return counter.KeySet();
		}

		public virtual bool ContainsKey(E key)
		{
			return counter.ContainsKey(key);
		}

		/// <summary>
		/// Returns the current count for the given key, which is 0 if it hasn't been
		/// seen before.
		/// </summary>
		/// <remarks>
		/// Returns the current count for the given key, which is 0 if it hasn't been
		/// seen before. This is a convenient version of
		/// <c>get</c>
		/// that casts
		/// and extracts the primitive value.
		/// </remarks>
		/// <param name="key">The key to look up.</param>
		/// <returns>
		/// The current count for the given key, which is 0 if it hasn't
		/// been seen before
		/// </returns>
		public virtual double GetCount(E key)
		{
			return counter.GetCount(key);
		}

		//---- end cdm added
		//--- JM added for Distributions
		/// <summary>Assuming that c has a total count &lt; 1, returns a new Distribution using the counts in c as probabilities.</summary>
		/// <remarks>
		/// Assuming that c has a total count &lt; 1, returns a new Distribution using the counts in c as probabilities.
		/// If c has a total count &gt; 1, returns a normalized distribution with no remaining mass.
		/// </remarks>
		public static Edu.Stanford.Nlp.Stats.Distribution<E> GetDistributionFromPartiallySpecifiedCounter<E>(ICounter<E> c, int numKeys)
		{
			Edu.Stanford.Nlp.Stats.Distribution<E> d;
			double total = c.TotalCount();
			if (total >= 1.0)
			{
				d = GetDistribution(c);
				d.numberOfKeys = numKeys;
			}
			else
			{
				d = new Edu.Stanford.Nlp.Stats.Distribution<E>();
				d.numberOfKeys = numKeys;
				d.counter = c;
				d.reservedMass = 1.0 - total;
			}
			return d;
		}

		//--- end JM added
		/// <param name="s">a Collection of keys.</param>
		public static Edu.Stanford.Nlp.Stats.Distribution<E> GetUniformDistribution<E>(ICollection<E> s)
		{
			Edu.Stanford.Nlp.Stats.Distribution<E> norm = new Edu.Stanford.Nlp.Stats.Distribution<E>();
			norm.counter = new ClassicCounter<E>();
			norm.numberOfKeys = s.Count;
			norm.reservedMass = 0;
			double total = s.Count;
			double count = 1.0 / total;
			foreach (E key in s)
			{
				norm.counter.SetCount(key, count);
			}
			return norm;
		}

		/// <param name="s">a Collection of keys.</param>
		public static Edu.Stanford.Nlp.Stats.Distribution<E> GetPerturbedUniformDistribution<E>(ICollection<E> s, Random r)
		{
			Edu.Stanford.Nlp.Stats.Distribution<E> norm = new Edu.Stanford.Nlp.Stats.Distribution<E>();
			norm.counter = new ClassicCounter<E>();
			norm.numberOfKeys = s.Count;
			norm.reservedMass = 0;
			double total = s.Count;
			double prob = 1.0 / total;
			double stdev = prob / 1000.0;
			foreach (E key in s)
			{
				norm.counter.SetCount(key, prob + (r.NextGaussian() * stdev));
			}
			return norm;
		}

		public static Edu.Stanford.Nlp.Stats.Distribution<E> GetPerturbedDistribution<E>(ICounter<E> wordCounter, Random r)
		{
			Edu.Stanford.Nlp.Stats.Distribution<E> norm = new Edu.Stanford.Nlp.Stats.Distribution<E>();
			norm.counter = new ClassicCounter<E>();
			norm.numberOfKeys = wordCounter.Size();
			norm.reservedMass = 0;
			double totalCount = wordCounter.TotalCount();
			double stdev = 1.0 / norm.numberOfKeys / 1000.0;
			// tiny relative to average value
			foreach (E key in wordCounter.KeySet())
			{
				double prob = wordCounter.GetCount(key) / totalCount;
				double perturbedProb = prob + (r.NextGaussian() * stdev);
				if (perturbedProb < 0.0)
				{
					perturbedProb = 0.0;
				}
				norm.counter.SetCount(key, perturbedProb);
			}
			return norm;
		}

		/// <summary>Creates a Distribution from the given counter.</summary>
		/// <remarks>
		/// Creates a Distribution from the given counter. It makes an internal
		/// copy of the counter and divides all counts by the total count.
		/// </remarks>
		/// <returns>a new Distribution</returns>
		public static Edu.Stanford.Nlp.Stats.Distribution<E> GetDistribution<E>(ICounter<E> counter)
		{
			return GetDistributionWithReservedMass(counter, 0.0);
		}

		public static Edu.Stanford.Nlp.Stats.Distribution<E> GetDistributionWithReservedMass<E>(ICounter<E> counter, double reservedMass)
		{
			Edu.Stanford.Nlp.Stats.Distribution<E> norm = new Edu.Stanford.Nlp.Stats.Distribution<E>();
			norm.counter = new ClassicCounter<E>();
			norm.numberOfKeys = counter.Size();
			norm.reservedMass = reservedMass;
			double total = counter.TotalCount() * (1 + reservedMass);
			if (total == 0.0)
			{
				total = 1.0;
			}
			foreach (E key in counter.KeySet())
			{
				double count = counter.GetCount(key) / total;
				//      if (Double.isNaN(count) || count < 0.0 || count> 1.0 ) throw new RuntimeException("count=" + counter.getCount(key) + " total=" + total);
				norm.counter.SetCount(key, count);
			}
			return norm;
		}

		/// <summary>
		/// Creates a Distribution from the given counter, ie makes an internal
		/// copy of the counter and divides all counts by the total count.
		/// </summary>
		/// <returns>a new Distribution</returns>
		public static Edu.Stanford.Nlp.Stats.Distribution<E> GetDistributionFromLogValues<E>(ICounter<E> counter)
		{
			ICounter<E> c = new ClassicCounter<E>();
			// go through once to get the max
			// shift all by max so as to minimize the possibility of underflow
			double max = Counters.Max(counter);
			// Thang 17Feb12: max should operate on counter instead of c, fixed!
			foreach (E key in counter.KeySet())
			{
				double count = Math.Exp(counter.GetCount(key) - max);
				c.SetCount(key, count);
			}
			return GetDistribution(c);
		}

		public static Edu.Stanford.Nlp.Stats.Distribution<E> AbsolutelyDiscountedDistribution<E>(ICounter<E> counter, int numberOfKeys, double discount)
		{
			Edu.Stanford.Nlp.Stats.Distribution<E> norm = new Edu.Stanford.Nlp.Stats.Distribution<E>();
			norm.counter = new ClassicCounter<E>();
			double total = counter.TotalCount();
			double reservedMass = 0.0;
			foreach (E key in counter.KeySet())
			{
				double count = counter.GetCount(key);
				if (count > discount)
				{
					double newCount = (count - discount) / total;
					norm.counter.SetCount(key, newCount);
					// a positive count left over
					//        System.out.println("seen: " + newCount);
					reservedMass += discount;
				}
				else
				{
					// count <= discount
					reservedMass += count;
				}
			}
			// if the count <= discount, don't put key in counter, and we treat it as unseen!!
			norm.numberOfKeys = numberOfKeys;
			norm.reservedMass = reservedMass / total;
			//    System.out.println("UNSEEN: " + reservedMass / total / (numberOfKeys - counter.size()));
			return norm;
		}

		/// <summary>
		/// Creates an Laplace smoothed Distribution from the given counter, ie adds one count
		/// to every item, including unseen ones, and divides by the total count.
		/// </summary>
		/// <returns>a new add-1 smoothed Distribution</returns>
		public static Edu.Stanford.Nlp.Stats.Distribution<E> LaplaceSmoothedDistribution<E>(ICounter<E> counter, int numberOfKeys)
		{
			return LaplaceSmoothedDistribution(counter, numberOfKeys, 1.0);
		}

		/// <summary>
		/// Creates a smoothed Distribution using Lidstone's law, ie adds lambda (typically
		/// between 0 and 1) to every item, including unseen ones, and divides by the total count.
		/// </summary>
		/// <returns>a new Lidstone smoothed Distribution</returns>
		public static Edu.Stanford.Nlp.Stats.Distribution<E> LaplaceSmoothedDistribution<E>(ICounter<E> counter, int numberOfKeys, double lambda)
		{
			Edu.Stanford.Nlp.Stats.Distribution<E> norm = new Edu.Stanford.Nlp.Stats.Distribution<E>();
			norm.counter = new ClassicCounter<E>();
			double total = counter.TotalCount();
			double newTotal = total + (lambda * numberOfKeys);
			double reservedMass = ((double)numberOfKeys - counter.Size()) * lambda / newTotal;
			norm.numberOfKeys = numberOfKeys;
			norm.reservedMass = reservedMass;
			foreach (E key in counter.KeySet())
			{
				double count = counter.GetCount(key);
				norm.counter.SetCount(key, (count + lambda) / newTotal);
			}
			return norm;
		}

		/// <summary>
		/// Creates a smoothed Distribution with Laplace smoothing, but assumes an explicit
		/// count of "UNKNOWN" items.
		/// </summary>
		/// <remarks>
		/// Creates a smoothed Distribution with Laplace smoothing, but assumes an explicit
		/// count of "UNKNOWN" items.  Thus anything not in the original counter will have
		/// probability zero.
		/// </remarks>
		/// <param name="counter">the counter to normalize</param>
		/// <param name="lambda">the value to add to each count</param>
		/// <param name="Unk">the UNKNOWN symbol</param>
		/// <returns>a new Laplace-smoothed distribution</returns>
		public static Edu.Stanford.Nlp.Stats.Distribution<E> LaplaceWithExplicitUnknown<E>(ICounter<E> counter, double lambda, E Unk)
		{
			Edu.Stanford.Nlp.Stats.Distribution<E> norm = new Edu.Stanford.Nlp.Stats.Distribution<E>();
			norm.counter = new ClassicCounter<E>();
			double total = counter.TotalCount() + (lambda * (counter.Size() - 1));
			norm.numberOfKeys = counter.Size();
			norm.reservedMass = 0.0;
			foreach (E key in counter.KeySet())
			{
				if (key.Equals(Unk))
				{
					norm.counter.SetCount(key, counter.GetCount(key) / total);
				}
				else
				{
					norm.counter.SetCount(key, (counter.GetCount(key) + lambda) / total);
				}
			}
			return norm;
		}

		/// <summary>Creates a Good-Turing smoothed Distribution from the given counter.</summary>
		/// <returns>a new Good-Turing smoothed Distribution.</returns>
		public static Edu.Stanford.Nlp.Stats.Distribution<E> GoodTuringSmoothedCounter<E>(ICounter<E> counter, int numberOfKeys)
		{
			// gather count-counts
			int[] countCounts = GetCountCounts(counter);
			// if count-counts are unreliable, we shouldn't be using G-T
			// revert to laplace
			for (int i = 1; i <= 10; i++)
			{
				if (countCounts[i] < 3)
				{
					return LaplaceSmoothedDistribution(counter, numberOfKeys, 0.5);
				}
			}
			double observedMass = counter.TotalCount();
			double reservedMass = countCounts[1] / observedMass;
			// calculate and cache adjusted frequencies
			// also adjusting total mass of observed items
			double[] adjustedFreq = new double[10];
			for (int freq = 1; freq < 10; freq++)
			{
				adjustedFreq[freq] = (double)(freq + 1) * (double)countCounts[freq + 1] / countCounts[freq];
				observedMass -= (freq - adjustedFreq[freq]) * countCounts[freq];
			}
			double normFactor = (1.0 - reservedMass) / observedMass;
			Edu.Stanford.Nlp.Stats.Distribution<E> norm = new Edu.Stanford.Nlp.Stats.Distribution<E>();
			norm.counter = new ClassicCounter<E>();
			// fill in the new Distribution, renormalizing as we go
			foreach (E key in counter.KeySet())
			{
				int origFreq = (int)Math.Round(counter.GetCount(key));
				if (origFreq < 10)
				{
					norm.counter.SetCount(key, adjustedFreq[origFreq] * normFactor);
				}
				else
				{
					norm.counter.SetCount(key, origFreq * normFactor);
				}
			}
			norm.numberOfKeys = numberOfKeys;
			norm.reservedMass = reservedMass;
			return norm;
		}

		/// <summary>
		/// Creates a Good-Turing smoothed Distribution from the given counter without
		/// creating any reserved mass-- instead, the special object UNK in the counter
		/// is assumed to be the count of "UNSEEN" items.
		/// </summary>
		/// <remarks>
		/// Creates a Good-Turing smoothed Distribution from the given counter without
		/// creating any reserved mass-- instead, the special object UNK in the counter
		/// is assumed to be the count of "UNSEEN" items.  Probability of objects not in
		/// original counter will be zero.
		/// </remarks>
		/// <param name="counter">the counter</param>
		/// <param name="Unk">the unknown symbol</param>
		/// <returns>a good-turing smoothed distribution</returns>
		public static Edu.Stanford.Nlp.Stats.Distribution<E> GoodTuringWithExplicitUnknown<E>(ICounter<E> counter, E Unk)
		{
			// gather count-counts
			int[] countCounts = GetCountCounts(counter);
			// if count-counts are unreliable, we shouldn't be using G-T
			// revert to laplace
			for (int i = 1; i <= 10; i++)
			{
				if (countCounts[i] < 3)
				{
					return LaplaceWithExplicitUnknown(counter, 0.5, Unk);
				}
			}
			double observedMass = counter.TotalCount();
			// calculate and cache adjusted frequencies
			// also adjusting total mass of observed items
			double[] adjustedFreq = new double[10];
			for (int freq = 1; freq < 10; freq++)
			{
				adjustedFreq[freq] = (double)(freq + 1) * (double)countCounts[freq + 1] / countCounts[freq];
				observedMass -= (freq - adjustedFreq[freq]) * countCounts[freq];
			}
			Edu.Stanford.Nlp.Stats.Distribution<E> norm = new Edu.Stanford.Nlp.Stats.Distribution<E>();
			norm.counter = new ClassicCounter<E>();
			// fill in the new Distribution, renormalizing as we go
			foreach (E key in counter.KeySet())
			{
				int origFreq = (int)Math.Round(counter.GetCount(key));
				if (origFreq < 10)
				{
					norm.counter.SetCount(key, adjustedFreq[origFreq] / observedMass);
				}
				else
				{
					norm.counter.SetCount(key, origFreq / observedMass);
				}
			}
			norm.numberOfKeys = counter.Size();
			norm.reservedMass = 0.0;
			return norm;
		}

		private static int[] GetCountCounts<E>(ICounter<E> counter)
		{
			int[] countCounts = new int[11];
			for (int i = 0; i <= 10; i++)
			{
				countCounts[i] = 0;
			}
			foreach (E key in counter.KeySet())
			{
				int count = (int)Math.Round(counter.GetCount(key));
				if (count <= 10)
				{
					countCounts[count]++;
				}
			}
			return countCounts;
		}

		// ----------------------------------------------------------------------------
		/// <summary>
		/// Creates a Distribution from the given counter using Gale &amp; Sampsons'
		/// "simple Good-Turing" smoothing.
		/// </summary>
		/// <returns>a new simple Good-Turing smoothed Distribution.</returns>
		public static Edu.Stanford.Nlp.Stats.Distribution<E> SimpleGoodTuring<E>(ICounter<E> counter, int numberOfKeys)
		{
			// check arguments
			ValidateCounter(counter);
			int numUnseen = numberOfKeys - counter.Size();
			if (numUnseen < 1)
			{
				throw new ArgumentException(string.Format("ERROR: numberOfKeys %d must be > size of counter %d!", numberOfKeys, counter.Size()));
			}
			// do smoothing
			int[][] cc = CountCounts2IntArrays(CollectCountCounts(counter));
			int[] r = cc[0];
			// counts
			int[] n = cc[1];
			// counts of counts
			Edu.Stanford.Nlp.Stats.SimpleGoodTuring sgt = new Edu.Stanford.Nlp.Stats.SimpleGoodTuring(r, n);
			// collate results
			ICounter<int> probsByCount = new ClassicCounter<int>();
			double[] probs = sgt.GetProbabilities();
			for (int i = 0; i < probs.Length; i++)
			{
				probsByCount.SetCount(r[i], probs[i]);
			}
			// make smoothed distribution
			Edu.Stanford.Nlp.Stats.Distribution<E> dist = new Edu.Stanford.Nlp.Stats.Distribution<E>();
			dist.counter = new ClassicCounter<E>();
			foreach (KeyValuePair<E, double> entry in counter.EntrySet())
			{
				E item = entry.Key;
				int count = (int)Math.Round(entry.Value);
				dist.counter.SetCount(item, probsByCount.GetCount(count));
			}
			dist.numberOfKeys = numberOfKeys;
			dist.reservedMass = sgt.GetProbabilityForUnseen();
			return dist;
		}

		/* Helper to simpleGoodTuringSmoothedCounter() */
		private static void ValidateCounter<E>(ICounter<E> counts)
		{
			foreach (KeyValuePair<E, double> entry in counts.EntrySet())
			{
				E item = entry.Key;
				double dblCount = entry.Value;
				if (dblCount == null)
				{
					throw new ArgumentException("ERROR: null count for item " + item + "!");
				}
				if (dblCount < 0)
				{
					throw new ArgumentException("ERROR: negative count " + dblCount + " for item " + item + "!");
				}
			}
		}

		/* Helper to simpleGoodTuringSmoothedCounter() */
		private static ICounter<int> CollectCountCounts<E>(ICounter<E> counts)
		{
			ICounter<int> cc = new ClassicCounter<int>();
			// counts of counts
			foreach (KeyValuePair<E, double> entry in counts.EntrySet())
			{
				//E item = entry.getKey();
				int count = (int)Math.Round(entry.Value);
				cc.IncrementCount(count);
			}
			return cc;
		}

		/* Helper to simpleGoodTuringSmoothedCounter() */
		private static int[][] CountCounts2IntArrays(ICounter<int> countCounts)
		{
			int size = countCounts.Size();
			int[][] arrays = new int[2][];
			arrays[0] = new int[size];
			// counts
			arrays[1] = new int[size];
			// count counts
			PriorityQueue<int> q = new PriorityQueue<int>(countCounts.KeySet());
			int i = 0;
			while (!q.IsEmpty())
			{
				int count = q.Poll();
				int countCount = (int)Math.Round(countCounts.GetCount(count));
				arrays[0][i] = count;
				arrays[1][i] = countCount;
				i++;
			}
			return arrays;
		}

		// ----------------------------------------------------------------------------
		/// <summary>
		/// Returns a Distribution that uses prior as a Dirichlet prior
		/// weighted by weight.
		/// </summary>
		/// <remarks>
		/// Returns a Distribution that uses prior as a Dirichlet prior
		/// weighted by weight.  Essentially adds "pseudo-counts" for each Object
		/// in prior equal to that Object's mass in prior times weight,
		/// then normalizes.
		/// <p>
		/// WARNING: If unseen item is encountered in c, total may not be 1.
		/// NOTE: This will not work if prior is a DynamicDistribution
		/// to fix this, you could add a CounterView to Distribution and use that
		/// in the linearCombination call below
		/// </remarks>
		/// <param name="weight">multiplier of prior to get "pseudo-count"</param>
		/// <returns>new Distribution</returns>
		public static Edu.Stanford.Nlp.Stats.Distribution<E> DistributionWithDirichletPrior<E>(ICounter<E> c, Edu.Stanford.Nlp.Stats.Distribution<E> prior, double weight)
		{
			Edu.Stanford.Nlp.Stats.Distribution<E> norm = new Edu.Stanford.Nlp.Stats.Distribution<E>();
			double totalWeight = c.TotalCount() + weight;
			if (prior is Distribution.DynamicDistribution)
			{
				throw new NotSupportedException("Cannot make normalized counter with Dynamic prior.");
			}
			norm.counter = Counters.LinearCombination(c, 1 / totalWeight, prior.counter, weight / totalWeight);
			norm.numberOfKeys = prior.numberOfKeys;
			norm.reservedMass = prior.reservedMass * weight / totalWeight;
			//System.out.println("totalCount: " + norm.totalCount());
			return norm;
		}

		/// <summary>
		/// Like normalizedCounterWithDirichletPrior except probabilities are
		/// computed dynamically from the counter and prior instead of all at once up front.
		/// </summary>
		/// <remarks>
		/// Like normalizedCounterWithDirichletPrior except probabilities are
		/// computed dynamically from the counter and prior instead of all at once up front.
		/// The main advantage of this is if you are making many distributions from relatively
		/// sparse counters using the same relatively dense prior, the prior is only represented
		/// once, for major memory savings.
		/// </remarks>
		/// <param name="weight">multiplier of prior to get "pseudo-count"</param>
		/// <returns>new Distribution</returns>
		public static Edu.Stanford.Nlp.Stats.Distribution<E> DynamicCounterWithDirichletPrior<E>(ICounter<E> c, Edu.Stanford.Nlp.Stats.Distribution<E> prior, double weight)
		{
			double totalWeight = c.TotalCount() + weight;
			Edu.Stanford.Nlp.Stats.Distribution<E> norm = new Distribution.DynamicDistribution<E>(prior, weight / totalWeight);
			norm.counter = new ClassicCounter<E>();
			// this might be done more efficiently with entrySet but there isn't a way to get
			// the entrySet from a Counter now.  In most cases c will be small(-ish) anyway
			foreach (E key in c.KeySet())
			{
				double count = c.GetCount(key) / totalWeight;
				prior.AddToKeySet(key);
				norm.counter.SetCount(key, count);
			}
			norm.numberOfKeys = prior.numberOfKeys;
			return norm;
		}

		[System.Serializable]
		private class DynamicDistribution<E> : Distribution<E>
		{
			private const long serialVersionUID = -6073849364871185L;

			private readonly Distribution<E> prior;

			private readonly double priorMultiplier;

			public DynamicDistribution(Distribution<E> prior, double priorMultiplier)
				: base()
			{
				this.prior = prior;
				this.priorMultiplier = priorMultiplier;
			}

			public override double ProbabilityOf(E o)
			{
				return this.counter.GetCount(o) + prior.ProbabilityOf(o) * priorMultiplier;
			}

			public override double TotalCount()
			{
				return this.counter.TotalCount() + prior.TotalCount() * priorMultiplier;
			}

			public override ICollection<E> KeySet()
			{
				return prior.KeySet();
			}

			public override void AddToKeySet(E o)
			{
				prior.AddToKeySet(o);
			}

			public override bool ContainsKey(E key)
			{
				return prior.ContainsKey(key);
			}

			public override E Argmax()
			{
				return Counters.Argmax(Counters.LinearCombination(this.counter, 1.0, prior.counter, priorMultiplier));
			}

			public override E SampleFrom()
			{
				double d = Math.Random();
				ICollection<E> s = prior.KeySet();
				foreach (E o in s)
				{
					d -= ProbabilityOf(o);
					if (d < 0)
					{
						return o;
					}
				}
				log.Error("Distribution sums to less than 1");
				log.Info("Sampled " + d + "      sum is " + TotalCount());
				throw new Exception(string.Empty);
			}
		}

		/// <summary>
		/// Maps a counter representing the linear weights of a multiclass
		/// logistic regression model to the probabilities of each class.
		/// </summary>
		public static Distribution<E> DistributionFromLogisticCounter<E>(ICounter<E> cntr)
		{
			double expSum = 0.0;
			int numKeys = 0;
			foreach (E key in cntr.KeySet())
			{
				expSum += Math.Exp(cntr.GetCount(key));
				numKeys++;
			}
			Distribution<E> probs = new Distribution<E>();
			probs.counter = new ClassicCounter<E>();
			probs.reservedMass = 0.0;
			probs.numberOfKeys = numKeys;
			foreach (E key_1 in cntr.KeySet())
			{
				probs.counter.SetCount(key_1, Math.Exp(cntr.GetCount(key_1)) / expSum);
			}
			return probs;
		}

		/// <summary>Returns an object sampled from the distribution using Math.random().</summary>
		/// <remarks>
		/// Returns an object sampled from the distribution using Math.random().
		/// There may be a faster way to do this if you need to...
		/// </remarks>
		/// <returns>a sampled object</returns>
		public virtual E SampleFrom()
		{
			return Counters.Sample(counter);
		}

		/// <summary>
		/// Returns an object sampled from the distribution using a self-provided
		/// random number generator.
		/// </summary>
		/// <returns>a sampled object</returns>
		public virtual E SampleFrom(Random random)
		{
			return Counters.Sample(counter, random);
		}

		/// <summary>Returns the normalized count of the given object.</summary>
		/// <returns>the normalized count of the object</returns>
		public virtual double ProbabilityOf(E key)
		{
			if (counter.ContainsKey(key))
			{
				return counter.GetCount(key);
			}
			else
			{
				int remainingKeys = numberOfKeys - counter.Size();
				if (remainingKeys <= 0)
				{
					return 0.0;
				}
				else
				{
					return (reservedMass / remainingKeys);
				}
			}
		}

		/// <summary>Returns the natural logarithm of the object's probability</summary>
		/// <returns>the logarithm of the normalised count (may be NaN if Pr==0.0)</returns>
		public virtual double LogProbabilityOf(E key)
		{
			double prob = ProbabilityOf(key);
			return Math.Log(prob);
		}

		public virtual E Argmax()
		{
			return Counters.Argmax(counter);
		}

		public virtual double TotalCount()
		{
			return counter.TotalCount() + reservedMass;
		}

		/// <summary>Insures that object is in keyset (with possibly zero value)</summary>
		/// <param name="o">object to put in keyset</param>
		public virtual void AddToKeySet(E o)
		{
			if (!counter.ContainsKey(o))
			{
				counter.SetCount(o, 0);
			}
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			return o is Distribution && Equals((Distribution)o);
		}

		public virtual bool Equals(Distribution<E> distribution)
		{
			if (numberOfKeys != distribution.numberOfKeys)
			{
				return false;
			}
			if (reservedMass != distribution.reservedMass)
			{
				return false;
			}
			return counter.Equals(distribution.counter);
		}

		public override int GetHashCode()
		{
			int result = numberOfKeys;
			long temp = double.DoubleToLongBits(reservedMass);
			result = 29 * result + (int)(temp ^ ((long)(((ulong)temp) >> 32)));
			result = 29 * result + counter.GetHashCode();
			return result;
		}

		private Distribution()
		{
		}

		// no public constructor; use static methods instead
		public override string ToString()
		{
			NumberFormat nf = new DecimalFormat("0.0##E0");
			IList<E> keyList = new List<E>(KeySet());
			keyList.Sort(null);
			StringBuilder sb = new StringBuilder();
			sb.Append("[");
			for (int i = 0; i < NumEntriesInString; i++)
			{
				if (keyList.Count <= i)
				{
					break;
				}
				E o = keyList[i];
				double prob = ProbabilityOf(o);
				sb.Append(o).Append(":").Append(nf.Format(prob)).Append(" ");
			}
			sb.Append("]");
			return sb.ToString();
		}

		/// <summary>For internal testing purposes only.</summary>
		public static void Main(string[] args)
		{
			ICounter<string> c2 = new ClassicCounter<string>();
			c2.IncrementCount("p", 13);
			c2.SetCount("q", 12);
			c2.SetCount("w", 5);
			c2.IncrementCount("x", 7.5);
			// System.out.println(getDistribution(c2).getCount("w") + " should be 0.13333");
			ClassicCounter<string> c = new ClassicCounter<string>();
			double p = 1000;
			string Unk = "!*UNKNOWN*!";
			ICollection<string> s = Generics.NewHashSet();
			s.Add(Unk);
			// fill counter with roughly Zipfian distribution
			//    "1" : 1000
			//    "2" :  500
			//    "3" :  333
			//       ...
			//  "UNK" :   45
			//       ...
			//  "666" :    2
			//  "667" :    1
			//       ...
			// "1000" :    1
			for (int rank = 1; rank < 2000; rank++)
			{
				string i = rank.ToString();
				c.SetCount(i, Math.Round(p / rank));
				s.Add(i);
			}
			for (int rank_1 = 2000; rank_1 <= 4000; rank_1++)
			{
				string i = rank_1.ToString();
				s.Add(i);
			}
			Distribution<string> n = GetDistribution(c);
			Distribution<string> prior = GetUniformDistribution(s);
			Distribution<string> dir1 = DistributionWithDirichletPrior(c, prior, 4000);
			Distribution<string> dir2 = DynamicCounterWithDirichletPrior(c, prior, 4000);
			Distribution<string> add1;
			Distribution<string> gt;
			if (true)
			{
				add1 = LaplaceSmoothedDistribution(c, 4000);
				gt = GoodTuringSmoothedCounter(c, 4000);
			}
			else
			{
				c.SetCount(Unk, 45);
				add1 = LaplaceWithExplicitUnknown(c, 0.5, Unk);
				gt = GoodTuringWithExplicitUnknown(c, Unk);
			}
			Distribution<string> sgt = SimpleGoodTuring(c, 4000);
			System.Console.Out.Printf("%10s %10s %10s %10s %10s %10s %10s%n", "Freq", "Norm", "Add1", "Dir1", "Dir2", "GT", "SGT");
			System.Console.Out.Printf("%10s %10s %10s %10s %10s %10s %10s%n", "----------", "----------", "----------", "----------", "----------", "----------", "----------");
			for (int i_1 = 1; i_1 < 5; i_1++)
			{
				System.Console.Out.Printf("%10d ", Math.Round(p / i_1));
				string @in = i_1.ToString();
				System.Console.Out.Printf("%10.8f ", n.ProbabilityOf(@in.ToString()));
				System.Console.Out.Printf("%10.8f ", add1.ProbabilityOf(@in));
				System.Console.Out.Printf("%10.8f ", dir1.ProbabilityOf(@in));
				System.Console.Out.Printf("%10.8f ", dir2.ProbabilityOf(@in));
				System.Console.Out.Printf("%10.8f ", gt.ProbabilityOf(@in));
				System.Console.Out.Printf("%10.8f ", sgt.ProbabilityOf(@in));
				System.Console.Out.WriteLine();
			}
			System.Console.Out.Printf("%10s %10s %10s %10s %10s %10s %10s%n", "----------", "----------", "----------", "----------", "----------", "----------", "----------");
			System.Console.Out.Printf("%10d ", 1);
			string last = 1500.ToString();
			System.Console.Out.Printf("%10.8f ", n.ProbabilityOf(last));
			System.Console.Out.Printf("%10.8f ", add1.ProbabilityOf(last));
			System.Console.Out.Printf("%10.8f ", dir1.ProbabilityOf(last));
			System.Console.Out.Printf("%10.8f ", dir2.ProbabilityOf(last));
			System.Console.Out.Printf("%10.8f ", gt.ProbabilityOf(last));
			System.Console.Out.Printf("%10.8f ", sgt.ProbabilityOf(last));
			System.Console.Out.WriteLine();
			System.Console.Out.Printf("%10s %10s %10s %10s %10s %10s %10s%n", "----------", "----------", "----------", "----------", "----------", "----------", "----------");
			System.Console.Out.Printf("%10s ", "UNK");
			System.Console.Out.Printf("%10.8f ", n.ProbabilityOf(Unk));
			System.Console.Out.Printf("%10.8f ", add1.ProbabilityOf(Unk));
			System.Console.Out.Printf("%10.8f ", dir1.ProbabilityOf(Unk));
			System.Console.Out.Printf("%10.8f ", dir2.ProbabilityOf(Unk));
			System.Console.Out.Printf("%10.8f ", gt.ProbabilityOf(Unk));
			System.Console.Out.Printf("%10.8f ", sgt.ProbabilityOf(Unk));
			System.Console.Out.WriteLine();
			System.Console.Out.Printf("%10s %10s %10s %10s %10s %10s %10s%n", "----------", "----------", "----------", "----------", "----------", "----------", "----------");
			System.Console.Out.Printf("%10s ", "RESERVE");
			System.Console.Out.Printf("%10.8f ", n.GetReservedMass());
			System.Console.Out.Printf("%10.8f ", add1.GetReservedMass());
			System.Console.Out.Printf("%10.8f ", dir1.GetReservedMass());
			System.Console.Out.Printf("%10.8f ", dir2.GetReservedMass());
			System.Console.Out.Printf("%10.8f ", gt.GetReservedMass());
			System.Console.Out.Printf("%10.8f ", sgt.GetReservedMass());
			System.Console.Out.WriteLine();
			System.Console.Out.Printf("%10s %10s %10s %10s %10s %10s %10s%n", "----------", "----------", "----------", "----------", "----------", "----------", "----------");
			System.Console.Out.Printf("%10s ", "Total");
			System.Console.Out.Printf("%10.8f ", n.TotalCount());
			System.Console.Out.Printf("%10.8f ", add1.TotalCount());
			System.Console.Out.Printf("%10.8f ", dir1.TotalCount());
			System.Console.Out.Printf("%10.8f ", dir2.TotalCount());
			System.Console.Out.Printf("%10.8f ", gt.TotalCount());
			System.Console.Out.Printf("%10.8f ", sgt.TotalCount());
			System.Console.Out.WriteLine();
		}
	}
}
