using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>Constrains test-time inference to labels observed in training.</summary>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class LabelDictionary
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.LabelDictionary));

		private const long serialVersionUID = 6790400453922524056L;

		private readonly bool Debug = false;

		/// <summary>Initial capacity of the bookkeeping data structures.</summary>
		private readonly int DefaultCapacity = 30000;

		private ICounter<string> observationCounts;

		private IDictionary<string, ICollection<string>> observedLabels;

		private IIndex<string> observationIndex;

		private int[][] labelDictionary;

		/// <summary>Constructor.</summary>
		public LabelDictionary()
		{
			// Bookkeeping
			// Final data structure
			this.observationCounts = new ClassicCounter<string>(DefaultCapacity);
			this.observedLabels = Generics.NewHashMap(DefaultCapacity);
		}

		/// <summary>Increment counts for an observation/label pair.</summary>
		/// <param name="observation"/>
		/// <param name="label"/>
		public virtual void Increment(string observation, string label)
		{
			if (labelDictionary != null)
			{
				throw new Exception("Label dictionary is already locked.");
			}
			observationCounts.IncrementCount(observation);
			if (!observedLabels.Contains(observation))
			{
				observedLabels[observation] = new HashSet<string>();
			}
			observedLabels[observation].Add(string.Intern(label));
		}

		/// <summary>True if this observation is constrained, and false otherwise.</summary>
		public virtual bool IsConstrained(string observation)
		{
			return observationIndex.IndexOf(observation) >= 0;
		}

		/// <summary>Get the allowed label set for an observation.</summary>
		/// <param name="observation"/>
		/// <returns>The allowed label set, or null if the observation is unconstrained.</returns>
		public virtual int[] GetConstrainedSet(string observation)
		{
			int i = observationIndex.IndexOf(observation);
			return i >= 0 ? labelDictionary[i] : null;
		}

		/// <summary>Setup the constrained label sets and free bookkeeping resources.</summary>
		/// <param name="threshold"/>
		/// <param name="labelIndex"/>
		public virtual void Lock(int threshold, IIndex<string> labelIndex)
		{
			if (labelDictionary != null)
			{
				throw new Exception("Label dictionary is already locked");
			}
			log.Info("Label dictionary enabled");
			System.Console.Error.Printf("#observations: %d%n", (int)observationCounts.TotalCount());
			Counters.RetainAbove(observationCounts, threshold);
			ICollection<string> constrainedObservations = observationCounts.KeySet();
			labelDictionary = new int[constrainedObservations.Count][];
			observationIndex = new HashIndex<string>(constrainedObservations.Count);
			foreach (string observation in constrainedObservations)
			{
				int i = observationIndex.AddToIndex(observation);
				System.Diagnostics.Debug.Assert(i < labelDictionary.Length);
				ICollection<string> allowedLabels = observedLabels[observation];
				labelDictionary[i] = new int[allowedLabels.Count];
				int j = 0;
				foreach (string label in allowedLabels)
				{
					labelDictionary[i][j++] = labelIndex.IndexOf(label);
				}
			}
			observationIndex.Lock();
			System.Console.Error.Printf("#constraints: %d%n", labelDictionary.Length);
			// Free bookkeeping data structures
			observationCounts = null;
			observedLabels = null;
		}
	}
}
