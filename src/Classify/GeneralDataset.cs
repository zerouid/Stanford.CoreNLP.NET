using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// The purpose of this interface is to unify
	/// <see cref="Dataset{L, F}"/>
	/// and
	/// <see cref="RVFDataset{L, F}"/>
	/// .
	/// <p>
	/// Note: Despite these being value classes, at present there are no equals() and hashCode() methods
	/// defined so you just get the default ones from Object, so different objects aren't equal.
	/// </p>
	/// </summary>
	/// <author>Kristina Toutanova (kristina@cs.stanford.edu)</author>
	/// <author>Anna Rafferty (various refactoring with subclasses)</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <author>
	/// Ramesh Nallapati (nmramesh@cs.stanford.edu)
	/// (added an abstract method getDatum, July 17th, 2008)
	/// </author>
	/// <?/>
	/// <?/>
	[System.Serializable]
	public abstract class GeneralDataset<L, F> : IEnumerable<RVFDatum<L, F>>
	{
		private const long serialVersionUID = 19157757130054829L;

		public IIndex<L> labelIndex;

		public IIndex<F> featureIndex;

		protected internal int[] labels;

		protected internal int[][] data;

		protected internal int size;

		public GeneralDataset()
		{
		}

		public virtual IIndex<L> LabelIndex()
		{
			return labelIndex;
		}

		public virtual IIndex<F> FeatureIndex()
		{
			return featureIndex;
		}

		public virtual int NumFeatures()
		{
			return featureIndex.Size();
		}

		public virtual int NumClasses()
		{
			return labelIndex.Size();
		}

		public virtual int[] GetLabelsArray()
		{
			labels = TrimToSize(labels);
			return labels;
		}

		public virtual int[][] GetDataArray()
		{
			data = TrimToSize(data);
			return data;
		}

		public abstract double[][] GetValuesArray();

		/// <summary>Resets the Dataset so that it is empty and ready to collect data.</summary>
		public virtual void Clear()
		{
			Clear(10);
		}

		/// <summary>Resets the Dataset so that it is empty and ready to collect data.</summary>
		/// <param name="numDatums">initial capacity of dataset</param>
		public virtual void Clear(int numDatums)
		{
			Initialize(numDatums);
		}

		/// <summary>
		/// This method takes care of resetting values of the dataset
		/// such that it is empty with an initial capacity of numDatums.
		/// </summary>
		/// <remarks>
		/// This method takes care of resetting values of the dataset
		/// such that it is empty with an initial capacity of numDatums.
		/// Should be accessed only by appropriate methods within the class,
		/// such as clear(), which take care of other parts of the emptying of data.
		/// </remarks>
		/// <param name="numDatums">initial capacity of dataset</param>
		protected internal abstract void Initialize(int numDatums);

		public abstract RVFDatum<L, F> GetRVFDatum(int index);

		public abstract IDatum<L, F> GetDatum(int index);

		public abstract void Add(IDatum<L, F> d);

		/// <summary>Get the total count (over all data instances) of each feature</summary>
		/// <returns>an array containing the counts (indexed by index)</returns>
		public virtual float[] GetFeatureCounts()
		{
			float[] counts = new float[featureIndex.Size()];
			for (int i = 0; i < m; i++)
			{
				for (int j = 0; j < n; j++)
				{
					counts[data[i][j]] += 1.0;
				}
			}
			return counts;
		}

		/// <summary>Applies a feature count threshold to the Dataset.</summary>
		/// <remarks>
		/// Applies a feature count threshold to the Dataset.  All features that
		/// occur fewer than <i>k</i> times are expunged.
		/// </remarks>
		public virtual void ApplyFeatureCountThreshold(int k)
		{
			float[] counts = GetFeatureCounts();
			IIndex<F> newFeatureIndex = new HashIndex<F>();
			int[] featMap = new int[featureIndex.Size()];
			for (int i = 0; i < featMap.Length; i++)
			{
				F feat = featureIndex.Get(i);
				if (counts[i] >= k)
				{
					int newIndex = newFeatureIndex.Size();
					newFeatureIndex.Add(feat);
					featMap[i] = newIndex;
				}
				else
				{
					featMap[i] = -1;
				}
			}
			// featureIndex.remove(feat);
			featureIndex = newFeatureIndex;
			// counts = null; // This is unnecessary; JVM can clean it up
			for (int i_1 = 0; i_1 < size; i_1++)
			{
				IList<int> featList = new List<int>(data[i_1].Length);
				for (int j = 0; j < data[i_1].Length; j++)
				{
					if (featMap[data[i_1][j]] >= 0)
					{
						featList.Add(featMap[data[i_1][j]]);
					}
				}
				data[i_1] = new int[featList.Count];
				for (int j_1 = 0; j_1 < data[i_1].Length; j_1++)
				{
					data[i_1][j_1] = featList[j_1];
				}
			}
		}

		/// <summary>Retains the given features in the Dataset.</summary>
		/// <remarks>
		/// Retains the given features in the Dataset.  All features that
		/// do not occur in features are expunged.
		/// </remarks>
		public virtual void RetainFeatures(ICollection<F> features)
		{
			//float[] counts = getFeatureCounts();
			IIndex<F> newFeatureIndex = new HashIndex<F>();
			int[] featMap = new int[featureIndex.Size()];
			for (int i = 0; i < featMap.Length; i++)
			{
				F feat = featureIndex.Get(i);
				if (features.Contains(feat))
				{
					int newIndex = newFeatureIndex.Size();
					newFeatureIndex.Add(feat);
					featMap[i] = newIndex;
				}
				else
				{
					featMap[i] = -1;
				}
			}
			// featureIndex.remove(feat);
			featureIndex = newFeatureIndex;
			// counts = null; // This is unnecessary; JVM can clean it up
			for (int i_1 = 0; i_1 < size; i_1++)
			{
				IList<int> featList = new List<int>(data[i_1].Length);
				for (int j = 0; j < data[i_1].Length; j++)
				{
					if (featMap[data[i_1][j]] >= 0)
					{
						featList.Add(featMap[data[i_1][j]]);
					}
				}
				data[i_1] = new int[featList.Count];
				for (int j_1 = 0; j_1 < data[i_1].Length; j_1++)
				{
					data[i_1][j_1] = featList[j_1];
				}
			}
		}

		/// <summary>Applies a max feature count threshold to the Dataset.</summary>
		/// <remarks>
		/// Applies a max feature count threshold to the Dataset.  All features that
		/// occur greater than <i>k</i> times are expunged.
		/// </remarks>
		public virtual void ApplyFeatureMaxCountThreshold(int k)
		{
			float[] counts = GetFeatureCounts();
			HashIndex<F> newFeatureIndex = new HashIndex<F>();
			int[] featMap = new int[featureIndex.Size()];
			for (int i = 0; i < featMap.Length; i++)
			{
				F feat = featureIndex.Get(i);
				if (counts[i] <= k)
				{
					int newIndex = newFeatureIndex.Count;
					newFeatureIndex.Add(feat);
					featMap[i] = newIndex;
				}
				else
				{
					featMap[i] = -1;
				}
			}
			// featureIndex.remove(feat);
			featureIndex = newFeatureIndex;
			// counts = null; // This is unnecessary; JVM can clean it up
			for (int i_1 = 0; i_1 < size; i_1++)
			{
				IList<int> featList = new List<int>(data[i_1].Length);
				for (int j = 0; j < data[i_1].Length; j++)
				{
					if (featMap[data[i_1][j]] >= 0)
					{
						featList.Add(featMap[data[i_1][j]]);
					}
				}
				data[i_1] = new int[featList.Count];
				for (int j_1 = 0; j_1 < data[i_1].Length; j_1++)
				{
					data[i_1][j_1] = featList[j_1];
				}
			}
		}

		/// <summary>returns the number of feature tokens in the Dataset.</summary>
		public virtual int NumFeatureTokens()
		{
			int x = 0;
			for (int i = 0; i < m; i++)
			{
				x += data[i].Length;
			}
			return x;
		}

		/// <summary>returns the number of distinct feature types in the Dataset.</summary>
		public virtual int NumFeatureTypes()
		{
			return featureIndex.Size();
		}

		/// <summary>Adds all Datums in the given collection of data to this dataset</summary>
		/// <param name="data">collection of datums you would like to add to the dataset</param>
		public virtual void AddAll<_T0>(IEnumerable<_T0> data)
			where _T0 : IDatum<L, F>
		{
			foreach (IDatum<L, F> d in data)
			{
				Add(d);
			}
		}

		/// <summary>Divide out a (devtest) split of the dataset versus the rest of it (as a training set).</summary>
		/// <param name="start">Begin devtest with this index (inclusive)</param>
		/// <param name="end">End devtest before this index (exclusive)</param>
		/// <returns>
		/// A Pair of data sets, the first being the remainder of size this.size() - (end-start)
		/// and the second being of size (end-start)
		/// </returns>
		public abstract Pair<Edu.Stanford.Nlp.Classify.GeneralDataset<L, F>, Edu.Stanford.Nlp.Classify.GeneralDataset<L, F>> Split(int start, int end);

		/// <summary>Divide out a (devtest) split from the start of the dataset and the rest of it (as a training set).</summary>
		/// <param name="fractionSplit">The first fractionSplit of datums (rounded down) will be the second split</param>
		/// <returns>
		/// A Pair of data sets, the first being the remainder of size ceiling(this.size() * (1-p)) drawn
		/// from the end of the dataset and the second of size floor(this.size() * p) drawn from the
		/// start of the dataset.
		/// </returns>
		public abstract Pair<Edu.Stanford.Nlp.Classify.GeneralDataset<L, F>, Edu.Stanford.Nlp.Classify.GeneralDataset<L, F>> Split(double fractionSplit);

		/// <summary>Divide out a (devtest) split of the dataset versus the rest of it (as a training set).</summary>
		/// <param name="fold">The number of this fold (must be between 0 and (numFolds - 1)</param>
		/// <param name="numFolds">
		/// The number of folds to divide the data into (must be greater than or equal to the
		/// size of the data set)
		/// </param>
		/// <returns>
		/// A Pair of data sets, the first being roughly (numFolds-1)/numFolds of the data items
		/// (for use as training data_, and the second being 1/numFolds of the data, taken from the
		/// fold<sup>th</sup> part of the data (for use as devTest data)
		/// </returns>
		public virtual Pair<Edu.Stanford.Nlp.Classify.GeneralDataset<L, F>, Edu.Stanford.Nlp.Classify.GeneralDataset<L, F>> SplitOutFold(int fold, int numFolds)
		{
			if (numFolds < 2 || numFolds > Size() || fold < 0 || fold >= numFolds)
			{
				throw new ArgumentException("Illegal request for fold " + fold + " of " + numFolds + " on data set of size " + Size());
			}
			int normalFoldSize = Size() / numFolds;
			int start = normalFoldSize * fold;
			int end = start + normalFoldSize;
			if (fold == (numFolds - 1))
			{
				end = Size();
			}
			return Split(start, end);
		}

		/// <summary>
		/// Returns the number of examples (
		/// <see cref="Edu.Stanford.Nlp.Ling.IDatum{L, F}"/>
		/// s) in the Dataset.
		/// </summary>
		public virtual int Size()
		{
			return size;
		}

		protected internal virtual void TrimData()
		{
			data = TrimToSize(data);
		}

		protected internal virtual void TrimLabels()
		{
			labels = TrimToSize(labels);
		}

		protected internal virtual int[] TrimToSize(int[] i)
		{
			int[] newI = new int[size];
			lock (typeof(Runtime))
			{
				System.Array.Copy(i, 0, newI, 0, size);
			}
			return newI;
		}

		protected internal virtual int[][] TrimToSize(int[][] i)
		{
			int[][] newI = new int[size][];
			lock (typeof(Runtime))
			{
				System.Array.Copy(i, 0, newI, 0, size);
			}
			return newI;
		}

		protected internal virtual double[][] TrimToSize(double[][] i)
		{
			double[][] newI = new double[size][];
			lock (typeof(Runtime))
			{
				System.Array.Copy(i, 0, newI, 0, size);
			}
			return newI;
		}

		/// <summary>Randomizes the data array in place.</summary>
		/// <remarks>
		/// Randomizes the data array in place.
		/// Note: this cannot change the values array or the datum weights,
		/// so redefine this for RVFDataset and WeightedDataset!
		/// This uses the Fisher-Yates (or Durstenfeld-Knuth) shuffle, which is unbiased.
		/// The same algorithm is used by shuffle() in j.u.Collections, and so you should get compatible
		/// results if using it on a Collection with the same seed (as of JDK1.7, at least).
		/// </remarks>
		/// <param name="randomSeed">A seed for the Random object (allows you to reproduce the same ordering)</param>
		public virtual void Randomize(long randomSeed)
		{
			// todo: Probably should be renamed 'shuffle' to be consistent with Java Collections API
			Random rand = new Random(randomSeed);
			for (int j = size - 1; j > 0; j--)
			{
				// swap each item with some lower numbered item
				int randIndex = rand.NextInt(j);
				int[] tmp = data[randIndex];
				data[randIndex] = data[j];
				data[j] = tmp;
				int tmpl = labels[randIndex];
				labels[randIndex] = labels[j];
				labels[j] = tmpl;
			}
		}

		/// <summary>Randomizes the data array in place.</summary>
		/// <remarks>
		/// Randomizes the data array in place.
		/// Note: this cannot change the values array or the datum weights,
		/// so redefine this for RVFDataset and WeightedDataset!
		/// This uses the Fisher-Yates (or Durstenfeld-Knuth) shuffle, which is unbiased.
		/// The same algorithm is used by shuffle() in j.u.Collections, and so you should get compatible
		/// results if using it on a Collection with the same seed (as of JDK1.7, at least).
		/// </remarks>
		/// <param name="randomSeed">A seed for the Random object (allows you to reproduce the same ordering)</param>
		public virtual void ShuffleWithSideInformation<E>(long randomSeed, IList<E> sideInformation)
		{
			if (size != sideInformation.Count)
			{
				throw new ArgumentException("shuffleWithSideInformation: sideInformation not of same size as Dataset");
			}
			Random rand = new Random(randomSeed);
			for (int j = size - 1; j > 0; j--)
			{
				// swap each item with some lower numbered item
				int randIndex = rand.NextInt(j);
				int[] tmp = data[randIndex];
				data[randIndex] = data[j];
				data[j] = tmp;
				int tmpl = labels[randIndex];
				labels[randIndex] = labels[j];
				labels[j] = tmpl;
				E tmpE = sideInformation[randIndex];
				sideInformation.Set(randIndex, sideInformation[j]);
				sideInformation.Set(j, tmpE);
			}
		}

		public virtual Edu.Stanford.Nlp.Classify.GeneralDataset<L, F> SampleDataset(long randomSeed, double sampleFrac, bool sampleWithReplacement)
		{
			int sampleSize = (int)(this.Size() * sampleFrac);
			Random rand = new Random(randomSeed);
			Edu.Stanford.Nlp.Classify.GeneralDataset<L, F> subset;
			if (this is RVFDataset)
			{
				subset = new RVFDataset<L, F>();
			}
			else
			{
				if (this is Dataset)
				{
					subset = new Dataset<L, F>();
				}
				else
				{
					throw new Exception("Can't handle this type of GeneralDataset.");
				}
			}
			if (sampleWithReplacement)
			{
				for (int i = 0; i < sampleSize; i++)
				{
					int datumNum = rand.NextInt(this.Size());
					subset.Add(this.GetDatum(datumNum));
				}
			}
			else
			{
				ICollection<int> indicedSampled = Generics.NewHashSet();
				while (subset.Size() < sampleSize)
				{
					int datumNum = rand.NextInt(this.Size());
					if (!indicedSampled.Contains(datumNum))
					{
						subset.Add(this.GetDatum(datumNum));
						indicedSampled.Add(datumNum);
					}
				}
			}
			return subset;
		}

		/// <summary>Print some statistics summarizing the dataset</summary>
		public abstract void SummaryStatistics();

		/// <summary>Returns an iterator over the class labels of the Dataset</summary>
		/// <returns>An iterator over the class labels of the Dataset</returns>
		public virtual IEnumerator<L> LabelIterator()
		{
			return labelIndex.GetEnumerator();
		}

		/// <param name="dataset"/>
		/// <returns>
		/// a new GeneralDataset whose features and ids map exactly to those of this GeneralDataset.
		/// Useful when two Datasets are created independently and one wants to train a model on one dataset and test on the other. -Ramesh.
		/// </returns>
		public virtual Edu.Stanford.Nlp.Classify.GeneralDataset<L, F> MapDataset(Edu.Stanford.Nlp.Classify.GeneralDataset<L, F> dataset)
		{
			Edu.Stanford.Nlp.Classify.GeneralDataset<L, F> newDataset;
			if (dataset is RVFDataset)
			{
				newDataset = new RVFDataset<L, F>(this.featureIndex, this.labelIndex);
			}
			else
			{
				newDataset = new Dataset<L, F>(this.featureIndex, this.labelIndex);
			}
			this.featureIndex.Lock();
			this.labelIndex.Lock();
			//System.out.println("inside mapDataset: dataset size:"+dataset.size());
			for (int i = 0; i < dataset.Size(); i++)
			{
				//System.out.println("inside mapDataset: adding datum number"+i);
				newDataset.Add(dataset.GetDatum(i));
			}
			//System.out.println("old Dataset stats: numData:"+dataset.size()+" numfeatures:"+dataset.featureIndex().size()+" numlabels:"+dataset.labelIndex.size());
			//System.out.println("new Dataset stats: numData:"+newDataset.size()+" numfeatures:"+newDataset.featureIndex().size()+" numlabels:"+newDataset.labelIndex.size());
			//System.out.println("this dataset stats: numData:"+size()+" numfeatures:"+featureIndex().size()+" numlabels:"+labelIndex.size());
			this.featureIndex.Unlock();
			this.labelIndex.Unlock();
			return newDataset;
		}

		public static IDatum<L2, F> MapDatum<L, L2, F>(IDatum<L, F> d, IDictionary<L, L2> labelMapping, L2 defaultLabel)
		{
			// TODO: How to copy datum?
			L2 newLabel = labelMapping[d.Label()];
			if (newLabel == null)
			{
				newLabel = defaultLabel;
			}
			if (d is RVFDatum)
			{
				return new RVFDatum<L2, F>(((RVFDatum<L, F>)d).AsFeaturesCounter(), newLabel);
			}
			else
			{
				return new BasicDatum<L2, F>(d.AsFeatures(), newLabel);
			}
		}

		/// <param name="dataset"/>
		/// <returns>a new GeneralDataset whose features and ids map exactly to those of this GeneralDataset. But labels are converted to be another set of labels</returns>
		public virtual Edu.Stanford.Nlp.Classify.GeneralDataset<L2, F> MapDataset<L2>(Edu.Stanford.Nlp.Classify.GeneralDataset<L, F> dataset, IIndex<L2> newLabelIndex, IDictionary<L, L2> labelMapping, L2 defaultLabel)
		{
			Edu.Stanford.Nlp.Classify.GeneralDataset<L2, F> newDataset;
			if (dataset is RVFDataset)
			{
				newDataset = new RVFDataset<L2, F>(this.featureIndex, newLabelIndex);
			}
			else
			{
				newDataset = new Dataset<L2, F>(this.featureIndex, newLabelIndex);
			}
			this.featureIndex.Lock();
			this.labelIndex.Lock();
			//System.out.println("inside mapDataset: dataset size:"+dataset.size());
			for (int i = 0; i < dataset.Size(); i++)
			{
				//System.out.println("inside mapDataset: adding datum number"+i);
				IDatum<L, F> d = dataset.GetDatum(i);
				IDatum<L2, F> d2 = MapDatum(d, labelMapping, defaultLabel);
				newDataset.Add(d2);
			}
			//System.out.println("old Dataset stats: numData:"+dataset.size()+" numfeatures:"+dataset.featureIndex().size()+" numlabels:"+dataset.labelIndex.size());
			//System.out.println("new Dataset stats: numData:"+newDataset.size()+" numfeatures:"+newDataset.featureIndex().size()+" numlabels:"+newDataset.labelIndex.size());
			//System.out.println("this dataset stats: numData:"+size()+" numfeatures:"+featureIndex().size()+" numlabels:"+labelIndex.size());
			this.featureIndex.Unlock();
			this.labelIndex.Unlock();
			return newDataset;
		}

		/// <summary>Dumps the Dataset as a training/test file for SVMLight.</summary>
		/// <remarks>
		/// Dumps the Dataset as a training/test file for SVMLight. <br />
		/// class [fno:val]+
		/// The features must occur in consecutive order.
		/// </remarks>
		public virtual void PrintSVMLightFormat()
		{
			PrintSVMLightFormat(new PrintWriter(System.Console.Out));
		}

		/// <summary>Maps our labels to labels that are compatible with svm_light</summary>
		/// <returns>array of strings</returns>
		public virtual string[] MakeSvmLabelMap()
		{
			string[] labelMap = new string[NumClasses()];
			if (NumClasses() > 2)
			{
				for (int i = 0; i < labelMap.Length; i++)
				{
					labelMap[i] = (i + 1).ToString();
				}
			}
			else
			{
				labelMap = new string[] { "+1", "-1" };
			}
			return labelMap;
		}

		// todo: Fix javadoc, have unit tested
		/// <summary>Print SVM Light Format file.</summary>
		/// <remarks>
		/// Print SVM Light Format file.
		/// The following comments are no longer applicable because I am
		/// now printing out the exact labelID for each example. -Ramesh (nmramesh@cs.stanford.edu) 12/17/2009.
		/// If the Dataset has more than 2 classes, then it
		/// prints using the label index (+1) (for svm_struct).  If it is 2 classes, then the labelIndex.get(0)
		/// is mapped to +1 and labelIndex.get(1) is mapped to -1 (for svm_light).
		/// </remarks>
		public virtual void PrintSVMLightFormat(PrintWriter pw)
		{
			//assumes each data item has a few features on, and sorts the feature keys while collecting the values in a counter
			// old comment:
			// the following code commented out by Ramesh (nmramesh@cs.stanford.edu) 12/17/2009.
			// why not simply print the exact id of the label instead of mapping to some values??
			// new comment:
			// mihai: we NEED this, because svm_light has special conventions not supported by default by our labels,
			//        e.g., in a multiclass setting it assumes that labels start at 1 whereas our labels start at 0 (08/31/2010)
			string[] labelMap = MakeSvmLabelMap();
			for (int i = 0; i < size; i++)
			{
				RVFDatum<L, F> d = GetRVFDatum(i);
				ICounter<F> c = d.AsFeaturesCounter();
				ClassicCounter<int> printC = new ClassicCounter<int>();
				foreach (F f in c.KeySet())
				{
					printC.SetCount(featureIndex.IndexOf(f), c.GetCount(f));
				}
				int[] features = Sharpen.Collections.ToArray(printC.KeySet(), new int[printC.KeySet().Count]);
				Arrays.Sort(features);
				StringBuilder sb = new StringBuilder();
				sb.Append(labelMap[labels[i]]).Append(' ');
				// sb.append(labels[i]).append(' '); // commented out by mihai: labels[i] breaks svm_light conventions!
				/* Old code: assumes that F is Integer....
				*
				for (int f: features) {
				sb.append((f + 1)).append(":").append(c.getCount(f)).append(" ");
				}
				*/
				//I think this is what was meant (using printC rather than c), but not sure
				// ~Sarah Spikes (sdspikes@cs.stanford.edu)
				foreach (int f_1 in features)
				{
					sb.Append((f_1 + 1)).Append(':').Append(printC.GetCount(f_1)).Append(' ');
				}
				pw.Println(sb.ToString());
			}
		}

		public virtual IEnumerator<RVFDatum<L, F>> GetEnumerator()
		{
			return new _IEnumerator_596(this);
		}

		private sealed class _IEnumerator_596 : IEnumerator<RVFDatum<L, F>>
		{
			public _IEnumerator_596(GeneralDataset<L, F> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private int id;

			// = 0;
			public bool MoveNext()
			{
				return this.id < this._enclosing.Size();
			}

			public RVFDatum<L, F> Current
			{
				get
				{
					if (this.id >= this._enclosing.Size())
					{
						throw new NoSuchElementException();
					}
					return this._enclosing.GetRVFDatum(this.id++);
				}
			}

			public void Remove()
			{
				throw new NotSupportedException();
			}

			private readonly GeneralDataset<L, F> _enclosing;
		}

		public virtual ClassicCounter<L> NumDatumsPerLabel()
		{
			labels = TrimToSize(labels);
			ClassicCounter<L> numDatums = new ClassicCounter<L>();
			foreach (int i in labels)
			{
				numDatums.IncrementCount(labelIndex.Get(i));
			}
			return numDatums;
		}

		/// <summary>
		/// Prints the sparse feature matrix using
		/// <see cref="GeneralDataset{L, F}.PrintSparseFeatureMatrix(Java.IO.PrintWriter)"/>
		/// to
		/// <see cref="System.Console.Out">System.out</see>
		/// .
		/// </summary>
		public abstract void PrintSparseFeatureMatrix();

		/// <summary>prints a sparse feature matrix representation of the Dataset.</summary>
		/// <remarks>
		/// prints a sparse feature matrix representation of the Dataset.  Prints the actual
		/// <see cref="object.ToString()"/>
		/// representations of features.
		/// </remarks>
		public abstract void PrintSparseFeatureMatrix(PrintWriter pw);
	}
}
