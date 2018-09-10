using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// An interfacing class for
	/// <see cref="IClassifierFactory{L, F, C}"/>
	/// that incrementally
	/// builds a more memory-efficient representation of a
	/// <see cref="System.Collections.IList{E}"/>
	/// of
	/// <see cref="Edu.Stanford.Nlp.Ling.IDatum{L, F}"/>
	/// objects for the purposes of training a
	/// <see cref="IClassifier{L, F}"/>
	/// with a
	/// <see cref="IClassifierFactory{L, F, C}"/>
	/// .
	/// </summary>
	/// <author>Roger Levy (rog@stanford.edu)</author>
	/// <author>Anna Rafferty (various refactoring with GeneralDataset/RVFDataset)</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (templatization)</author>
	/// <author>
	/// nmramesh@cs.stanford.edu
	/// <see cref="Dataset{L, F}.GetL1NormalizedTFIDFDatum(Edu.Stanford.Nlp.Ling.IDatum{L, F}, Edu.Stanford.Nlp.Stats.ICounter{E})">and #getL1NormalizedTFIDFDataset()</see>
	/// </author>
	/// <?/>
	/// <?/>
	[System.Serializable]
	public class Dataset<L, F> : GeneralDataset<L, F>
	{
		private const long serialVersionUID = -3883164942879961091L;

		internal static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Classify.Dataset<L,F>));

		public Dataset()
			: this(10)
		{
		}

		public Dataset(int numDatums)
		{
			Initialize(numDatums);
		}

		public Dataset(int numDatums, IIndex<F> featureIndex, IIndex<L> labelIndex)
		{
			Initialize(numDatums);
			this.featureIndex = featureIndex;
			this.labelIndex = labelIndex;
		}

		public Dataset(IIndex<F> featureIndex, IIndex<L> labelIndex)
			: this(10, featureIndex, labelIndex)
		{
		}

		/// <summary>Constructor that fully specifies a Dataset.</summary>
		/// <remarks>Constructor that fully specifies a Dataset.  Needed this for MulticlassDataset.</remarks>
		public Dataset(IIndex<L> labelIndex, int[] labels, IIndex<F> featureIndex, int[][] data)
			: this(labelIndex, labels, featureIndex, data, data.Length)
		{
		}

		/// <summary>Constructor that fully specifies a Dataset.</summary>
		/// <remarks>Constructor that fully specifies a Dataset.  Needed this for MulticlassDataset.</remarks>
		public Dataset(IIndex<L> labelIndex, int[] labels, IIndex<F> featureIndex, int[][] data, int size)
		{
			this.labelIndex = labelIndex;
			this.labels = labels;
			this.featureIndex = featureIndex;
			this.data = data;
			this.size = size;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override Pair<GeneralDataset<L, F>, GeneralDataset<L, F>> Split(double percentDev)
		{
			return Split(0, (int)(percentDev * Size()));
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override Pair<GeneralDataset<L, F>, GeneralDataset<L, F>> Split(int start, int end)
		{
			int devSize = end - start;
			int trainSize = Size() - devSize;
			int[][] devData = new int[devSize][];
			int[] devLabels = new int[devSize];
			int[][] trainData = new int[trainSize][];
			int[] trainLabels = new int[trainSize];
			lock (typeof(Runtime))
			{
				System.Array.Copy(data, start, devData, 0, devSize);
				System.Array.Copy(labels, start, devLabels, 0, devSize);
				System.Array.Copy(data, 0, trainData, 0, start);
				System.Array.Copy(data, end, trainData, start, Size() - end);
				System.Array.Copy(labels, 0, trainLabels, 0, start);
				System.Array.Copy(labels, end, trainLabels, start, Size() - end);
			}
			if (this is WeightedDataset<object, object>)
			{
				float[] trainWeights = new float[trainSize];
				float[] devWeights = new float[devSize];
				WeightedDataset<L, F> w = (WeightedDataset<L, F>)this;
				lock (typeof(Runtime))
				{
					System.Array.Copy(w.weights, start, devWeights, 0, devSize);
					System.Array.Copy(w.weights, 0, trainWeights, 0, start);
					System.Array.Copy(w.weights, end, trainWeights, start, Size() - end);
				}
				WeightedDataset<L, F> dev = new WeightedDataset<L, F>(labelIndex, devLabels, featureIndex, devData, devSize, devWeights);
				WeightedDataset<L, F> train = new WeightedDataset<L, F>(labelIndex, trainLabels, featureIndex, trainData, trainSize, trainWeights);
				return new Pair<GeneralDataset<L, F>, GeneralDataset<L, F>>(train, dev);
			}
			Edu.Stanford.Nlp.Classify.Dataset<L, F> dev_1 = new Edu.Stanford.Nlp.Classify.Dataset<L, F>(labelIndex, devLabels, featureIndex, devData, devSize);
			Edu.Stanford.Nlp.Classify.Dataset<L, F> train_1 = new Edu.Stanford.Nlp.Classify.Dataset<L, F>(labelIndex, trainLabels, featureIndex, trainData, trainSize);
			return new Pair<GeneralDataset<L, F>, GeneralDataset<L, F>>(train_1, dev_1);
		}

		public virtual Edu.Stanford.Nlp.Classify.Dataset<L, F> GetRandomSubDataset(double p, int seed)
		{
			int newSize = (int)(p * Size());
			ICollection<int> indicesToKeep = Generics.NewHashSet();
			Random r = new Random(seed);
			int s = Size();
			while (indicesToKeep.Count < newSize)
			{
				indicesToKeep.Add(r.NextInt(s));
			}
			int[][] newData = new int[newSize][];
			int[] newLabels = new int[newSize];
			int i = 0;
			foreach (int j in indicesToKeep)
			{
				newData[i] = data[j];
				newLabels[i] = labels[j];
				i++;
			}
			return new Edu.Stanford.Nlp.Classify.Dataset<L, F>(labelIndex, newLabels, featureIndex, newData);
		}

		public override double[][] GetValuesArray()
		{
			return null;
		}

		/// <summary>Constructs a Dataset by reading in a file in SVM light format.</summary>
		public static Edu.Stanford.Nlp.Classify.Dataset<string, string> ReadSVMLightFormat(string filename)
		{
			return ReadSVMLightFormat(filename, new HashIndex<string>(), new HashIndex<string>());
		}

		/// <summary>Constructs a Dataset by reading in a file in SVM light format.</summary>
		/// <remarks>
		/// Constructs a Dataset by reading in a file in SVM light format.
		/// The lines parameter is filled with the lines of the file for further processing
		/// (if lines is null, it is assumed no line information is desired)
		/// </remarks>
		public static Edu.Stanford.Nlp.Classify.Dataset<string, string> ReadSVMLightFormat(string filename, IList<string> lines)
		{
			return ReadSVMLightFormat(filename, new HashIndex<string>(), new HashIndex<string>(), lines);
		}

		/// <summary>Constructs a Dataset by reading in a file in SVM light format.</summary>
		/// <remarks>
		/// Constructs a Dataset by reading in a file in SVM light format.
		/// the created dataset has the same feature and label index as given
		/// </remarks>
		public static Edu.Stanford.Nlp.Classify.Dataset<string, string> ReadSVMLightFormat(string filename, IIndex<string> featureIndex, IIndex<string> labelIndex)
		{
			return ReadSVMLightFormat(filename, featureIndex, labelIndex, null);
		}

		/// <summary>Constructs a Dataset by reading in a file in SVM light format.</summary>
		/// <remarks>
		/// Constructs a Dataset by reading in a file in SVM light format.
		/// the created dataset has the same feature and label index as given
		/// </remarks>
		public static Edu.Stanford.Nlp.Classify.Dataset<string, string> ReadSVMLightFormat(string filename, IIndex<string> featureIndex, IIndex<string> labelIndex, IList<string> lines)
		{
			Edu.Stanford.Nlp.Classify.Dataset<string, string> dataset;
			try
			{
				dataset = new Edu.Stanford.Nlp.Classify.Dataset<string, string>(10, featureIndex, labelIndex);
				foreach (string line in ObjectBank.GetLineIterator(new File(filename)))
				{
					if (lines != null)
					{
						lines.Add(line);
					}
					dataset.Add(SvmLightLineToDatum(line));
				}
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
			return dataset;
		}

		private static int line1 = 0;

		public static IDatum<string, string> SvmLightLineToDatum(string l)
		{
			line1++;
			l = l.ReplaceAll("#.*", string.Empty);
			// remove any trailing comments
			string[] line = l.Split("\\s+");
			ICollection<string> features = new List<string>();
			for (int i = 1; i < line.Length; i++)
			{
				string[] f = line[i].Split(":");
				if (f.Length != 2)
				{
					logger.Info("Dataset error: line " + line1);
				}
				int val = (int)double.Parse(f[1]);
				for (int j = 0; j < val; j++)
				{
					features.Add(f[0]);
				}
			}
			features.Add(int.MaxValue.ToString());
			// a constant feature for a class
			IDatum<string, string> d = new BasicDatum<string, string>(features, line[0]);
			return d;
		}

		/// <summary>Get Number of datums a given feature appears in.</summary>
		public virtual ICounter<F> GetFeatureCounter()
		{
			ICounter<F> featureCounts = new ClassicCounter<F>();
			for (int i = 0; i < this.Size(); i++)
			{
				BasicDatum<L, F> datum = (BasicDatum<L, F>)GetDatum(i);
				ICollection<F> featureSet = Generics.NewHashSet(datum.AsFeatures());
				foreach (F key in featureSet)
				{
					featureCounts.IncrementCount(key, 1.0);
				}
			}
			return featureCounts;
		}

		/// <summary>Method to convert features from counts to L1-normalized TFIDF based features</summary>
		/// <param name="datum">with a collection of features.</param>
		/// <param name="featureDocCounts">a counter of doc-count for each feature.</param>
		/// <returns>RVFDatum with l1-normalized tf-idf features.</returns>
		public virtual RVFDatum<L, F> GetL1NormalizedTFIDFDatum(IDatum<L, F> datum, ICounter<F> featureDocCounts)
		{
			ICounter<F> tfidfFeatures = new ClassicCounter<F>();
			foreach (F feature in datum.AsFeatures())
			{
				if (featureDocCounts.ContainsKey(feature))
				{
					tfidfFeatures.IncrementCount(feature, 1.0);
				}
			}
			double l1norm = 0;
			foreach (F feature_1 in tfidfFeatures.KeySet())
			{
				double idf = Math.Log(((double)(this.Size() + 1)) / (featureDocCounts.GetCount(feature_1) + 0.5));
				double tf = tfidfFeatures.GetCount(feature_1);
				tfidfFeatures.SetCount(feature_1, tf * idf);
				l1norm += tf * idf;
			}
			foreach (F feature_2 in tfidfFeatures.KeySet())
			{
				double tfidf = tfidfFeatures.GetCount(feature_2);
				tfidfFeatures.SetCount(feature_2, tfidf / l1norm);
			}
			RVFDatum<L, F> rvfDatum = new RVFDatum<L, F>(tfidfFeatures, datum.Label());
			return rvfDatum;
		}

		/// <summary>Method to convert this dataset to RVFDataset using L1-normalized TF-IDF features</summary>
		/// <returns>RVFDataset</returns>
		public virtual RVFDataset<L, F> GetL1NormalizedTFIDFDataset()
		{
			RVFDataset<L, F> rvfDataset = new RVFDataset<L, F>(this.Size(), this.featureIndex, this.labelIndex);
			ICounter<F> featureDocCounts = GetFeatureCounter();
			for (int i = 0; i < this.Size(); i++)
			{
				IDatum<L, F> datum = this.GetDatum(i);
				RVFDatum<L, F> rvfDatum = GetL1NormalizedTFIDFDatum(datum, featureDocCounts);
				rvfDataset.Add(rvfDatum);
			}
			return rvfDataset;
		}

		public override void Add(IDatum<L, F> d)
		{
			Add(d.AsFeatures(), d.Label());
		}

		public virtual void Add(ICollection<F> features, L label)
		{
			Add(features, label, true);
		}

		public virtual void Add(ICollection<F> features, L label, bool addNewFeatures)
		{
			EnsureSize();
			AddLabel(label);
			AddFeatures(features, addNewFeatures);
			size++;
		}

		/// <summary>
		/// Adds a datums defined by feature indices and label index
		/// Careful with this one! Make sure that all indices are valid!
		/// </summary>
		/// <param name="features"/>
		/// <param name="label"/>
		public virtual void Add(int[] features, int label)
		{
			EnsureSize();
			AddLabelIndex(label);
			AddFeatureIndices(features);
			size++;
		}

		protected internal virtual void EnsureSize()
		{
			if (labels.Length == size)
			{
				int[] newLabels = new int[size * 2];
				int[][] newData = new int[size * 2][];
				lock (typeof(Runtime))
				{
					System.Array.Copy(labels, 0, newLabels, 0, size);
					System.Array.Copy(data, 0, newData, 0, size);
				}
				labels = newLabels;
				data = newData;
			}
		}

		protected internal virtual void AddLabel(L label)
		{
			labelIndex.Add(label);
			labels[size] = labelIndex.IndexOf(label);
		}

		protected internal virtual void AddLabelIndex(int label)
		{
			labels[size] = label;
		}

		protected internal virtual void AddFeatures(ICollection<F> features)
		{
			AddFeatures(features, true);
		}

		protected internal virtual void AddFeatures(ICollection<F> features, bool addNewFeatures)
		{
			int[] intFeatures = new int[features.Count];
			int j = 0;
			foreach (F feature in features)
			{
				if (addNewFeatures)
				{
					featureIndex.Add(feature);
				}
				int index = featureIndex.IndexOf(feature);
				if (index >= 0)
				{
					intFeatures[j] = featureIndex.IndexOf(feature);
					j++;
				}
			}
			data[size] = new int[j];
			lock (typeof(Runtime))
			{
				System.Array.Copy(intFeatures, 0, data[size], 0, j);
			}
		}

		protected internal virtual void AddFeatureIndices(int[] features)
		{
			data[size] = features;
		}

		protected internal sealed override void Initialize(int numDatums)
		{
			labelIndex = new HashIndex<L>();
			featureIndex = new HashIndex<F>();
			labels = new int[numDatums];
			data = new int[numDatums][];
			size = 0;
		}

		/// <returns>the index-ed datum</returns>
		public override IDatum<L, F> GetDatum(int index)
		{
			return new BasicDatum<L, F>(featureIndex.Objects(data[index]), labelIndex.Get(labels[index]));
		}

		/// <returns>the index-ed datum</returns>
		public override RVFDatum<L, F> GetRVFDatum(int index)
		{
			ClassicCounter<F> c = new ClassicCounter<F>();
			foreach (F key in featureIndex.Objects(data[index]))
			{
				c.IncrementCount(key);
			}
			return new RVFDatum<L, F>(c, labelIndex.Get(labels[index]));
		}

		/// <summary>Prints some summary statistics to stderr for the Dataset.</summary>
		public override void SummaryStatistics()
		{
			logger.Info(ToSummaryStatistics());
		}

		/// <summary>A String that is multiple lines of text giving summary statistics.</summary>
		/// <remarks>
		/// A String that is multiple lines of text giving summary statistics.
		/// (It does not end with a newline, though.)
		/// </remarks>
		/// <returns>A textual summary of the Dataset</returns>
		public virtual string ToSummaryStatistics()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("numDatums: ").Append(size).Append('\n');
			sb.Append("numDatumsPerLabel: ").Append(this.NumDatumsPerLabel()).Append('\n');
			sb.Append("numLabels: ").Append(labelIndex.Size()).Append(" [");
			IEnumerator<L> iter = labelIndex.GetEnumerator();
			while (iter.MoveNext())
			{
				sb.Append(iter.Current);
				if (iter.MoveNext())
				{
					sb.Append(", ");
				}
			}
			sb.Append("]\n");
			sb.Append("numFeatures (Phi(X) types): ").Append(featureIndex.Size()).Append(" [");
			int sz = Math.Min(5, featureIndex.Size());
			for (int i = 0; i < sz; i++)
			{
				if (i > 0)
				{
					sb.Append(", ");
				}
				sb.Append(featureIndex.Get(i));
			}
			if (sz < featureIndex.Size())
			{
				sb.Append(", ...");
			}
			sb.Append(']');
			return sb.ToString();
		}

		/// <summary>Applies feature count thresholds to the Dataset.</summary>
		/// <remarks>
		/// Applies feature count thresholds to the Dataset.
		/// Only features that match pattern_i and occur at
		/// least threshold_i times (for some i) are kept.
		/// </remarks>
		/// <param name="thresholds">a list of pattern, threshold pairs</param>
		public virtual void ApplyFeatureCountThreshold(IList<Pair<Pattern, int>> thresholds)
		{
			// get feature counts
			float[] counts = GetFeatureCounts();
			// build a new featureIndex
			IIndex<F> newFeatureIndex = new HashIndex<F>();
			LOOP_continue:
			foreach (F f in featureIndex)
			{
				foreach (Pair<Pattern, int> threshold in thresholds)
				{
					Pattern p = threshold.First();
					Matcher m = p.Matcher(f.ToString());
					if (m.Matches())
					{
						if (counts[featureIndex.IndexOf(f)] >= threshold.second)
						{
							newFeatureIndex.Add(f);
						}
						goto LOOP_continue;
					}
				}
				// we only get here if it didn't match anything on the list
				newFeatureIndex.Add(f);
			}
			
			counts = null;
			int[] featMap = new int[featureIndex.Size()];
			for (int i = 0; i < featMap.Length; i++)
			{
				featMap[i] = newFeatureIndex.IndexOf(featureIndex.Get(i));
			}
			featureIndex = null;
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
			featureIndex = newFeatureIndex;
		}

		/// <summary>prints the full feature matrix in tab-delimited form.</summary>
		/// <remarks>
		/// prints the full feature matrix in tab-delimited form.  These can be BIG
		/// matrices, so be careful!
		/// </remarks>
		public virtual void PrintFullFeatureMatrix(StreamWriter pw)
		{
			string sep = "\t";
			for (int i = 0; i < featureIndex.Size(); i++)
			{
				pw.Print(sep + featureIndex.Get(i));
			}
			pw.Println();
			for (int i_1 = 0; i_1 < labels.Length; i_1++)
			{
				pw.Print(labelIndex.Get(i_1));
				ICollection<int> feats = Generics.NewHashSet();
				for (int j = 0; j < data[i_1].Length; j++)
				{
					int feature = data[i_1][j];
					feats.Add(int.Parse(feature));
				}
				for (int j_1 = 0; j_1 < featureIndex.Size(); j_1++)
				{
					if (feats.Contains(int.Parse(j_1)))
					{
						pw.Print(sep + '1');
					}
					else
					{
						pw.Print(sep + '0');
					}
				}
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override void PrintSparseFeatureMatrix()
		{
			PrintSparseFeatureMatrix(new PrintWriter(System.Console.Out, true));
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override void PrintSparseFeatureMatrix(StreamWriter pw)
		{
			string sep = "\t";
			for (int i = 0; i < size; i++)
			{
				pw.Print(labelIndex.Get(labels[i]));
				int[] datum = data[i];
				foreach (int j in datum)
				{
					pw.Print(sep + featureIndex.Get(j));
				}
				pw.Println();
			}
		}

		public virtual void ChangeLabelIndex(IIndex<L> newLabelIndex)
		{
			labels = TrimToSize(labels);
			for (int i = 0; i < labels.Length; i++)
			{
				labels[i] = newLabelIndex.IndexOf(labelIndex.Get(labels[i]));
			}
			labelIndex = newLabelIndex;
		}

		public virtual void ChangeFeatureIndex(IIndex<F> newFeatureIndex)
		{
			data = TrimToSize(data);
			labels = TrimToSize(labels);
			int[][] newData = new int[data.Length][];
			for (int i = 0; i < data.Length; i++)
			{
				int[] newD = new int[data[i].Length];
				int k = 0;
				for (int j = 0; j < data[i].Length; j++)
				{
					int newIndex = newFeatureIndex.IndexOf(featureIndex.Get(data[i][j]));
					if (newIndex >= 0)
					{
						newD[k++] = newIndex;
					}
				}
				newData[i] = new int[k];
				lock (typeof(Runtime))
				{
					System.Array.Copy(newD, 0, newData[i], 0, k);
				}
			}
			data = newData;
			featureIndex = newFeatureIndex;
		}

		public virtual void SelectFeaturesBinaryInformationGain(int numFeatures)
		{
			double[] scores = GetInformationGains();
			SelectFeatures(numFeatures, scores);
		}

		/// <summary>Generic method to select features based on the feature scores vector provided as an argument.</summary>
		/// <param name="numFeatures">number of features to be selected.</param>
		/// <param name="scores">a vector of size total number of features in the data.</param>
		public virtual void SelectFeatures(int numFeatures, double[] scores)
		{
			IList<ScoredObject<F>> scoredFeatures = new List<ScoredObject<F>>();
			for (int i = 0; i < scores.Length; i++)
			{
				scoredFeatures.Add(new ScoredObject<F>(featureIndex.Get(i), scores[i]));
			}
			scoredFeatures.Sort(ScoredComparator.DescendingComparator);
			IIndex<F> newFeatureIndex = new HashIndex<F>();
			for (int i_1 = 0; i_1 < scoredFeatures.Count && i_1 < numFeatures; i_1++)
			{
				newFeatureIndex.Add(scoredFeatures[i_1].Object());
			}
			//logger.info(scoredFeatures.get(i));
			for (int i_2 = 0; i_2 < size; i_2++)
			{
				int[] newData = new int[data[i_2].Length];
				int curIndex = 0;
				for (int j = 0; j < data[i_2].Length; j++)
				{
					int index;
					if ((index = newFeatureIndex.IndexOf(featureIndex.Get(data[i_2][j]))) != -1)
					{
						newData[curIndex++] = index;
					}
				}
				int[] newDataTrimmed = new int[curIndex];
				lock (typeof(Runtime))
				{
					System.Array.Copy(newData, 0, newDataTrimmed, 0, curIndex);
				}
				data[i_2] = newDataTrimmed;
			}
			featureIndex = newFeatureIndex;
		}

		public virtual double[] GetInformationGains()
		{
			//    assert size > 0;
			//    data = trimToSize(data);  // Don't need to trim to size, and trimming is dangerous the dataset is empty (you can't add to it thereafter)
			labels = TrimToSize(labels);
			// counts the number of times word X is present
			ClassicCounter<F> featureCounter = new ClassicCounter<F>();
			// counts the number of time a document has label Y
			ClassicCounter<L> labelCounter = new ClassicCounter<L>();
			// counts the number of times the document has label Y given word X is present
			TwoDimensionalCounter<F, L> condCounter = new TwoDimensionalCounter<F, L>();
			for (int i = 0; i < labels.Length; i++)
			{
				labelCounter.IncrementCount(labelIndex.Get(labels[i]));
				// convert the document to binary feature representation
				bool[] doc = new bool[featureIndex.Size()];
				//logger.info(i);
				for (int j = 0; j < data[i].Length; j++)
				{
					doc[data[i][j]] = true;
				}
				for (int j_1 = 0; j_1 < doc.Length; j_1++)
				{
					if (doc[j_1])
					{
						featureCounter.IncrementCount(featureIndex.Get(j_1));
						condCounter.IncrementCount(featureIndex.Get(j_1), labelIndex.Get(labels[i]), 1.0);
					}
				}
			}
			double entropy = 0.0;
			for (int i_1 = 0; i_1 < labelIndex.Size(); i_1++)
			{
				double labelCount = labelCounter.GetCount(labelIndex.Get(i_1));
				double p = labelCount / Size();
				entropy -= p * (Math.Log(p) / Math.Log(2));
			}
			double[] ig = new double[featureIndex.Size()];
			Arrays.Fill(ig, entropy);
			for (int i_2 = 0; i_2 < featureIndex.Size(); i_2++)
			{
				F feature = featureIndex.Get(i_2);
				double featureCount = featureCounter.GetCount(feature);
				double notFeatureCount = Size() - featureCount;
				double pFeature = featureCount / Size();
				double pNotFeature = (1.0 - pFeature);
				if (featureCount == 0)
				{
					ig[i_2] = 0;
					continue;
				}
				if (notFeatureCount == 0)
				{
					ig[i_2] = 0;
					continue;
				}
				double sumFeature = 0.0;
				double sumNotFeature = 0.0;
				for (int j = 0; j < labelIndex.Size(); j++)
				{
					L label = labelIndex.Get(j);
					double featureLabelCount = condCounter.GetCount(feature, label);
					double notFeatureLabelCount = Size() - featureLabelCount;
					// yes, these dont sum to 1.  that is correct.
					// one is the prob of the label, given that the
					// feature is present, and the other is the prob
					// of the label given that the feature is absent
					double p = featureLabelCount / featureCount;
					double pNot = notFeatureLabelCount / notFeatureCount;
					if (featureLabelCount != 0)
					{
						sumFeature += p * (Math.Log(p) / Math.Log(2));
					}
					if (notFeatureLabelCount != 0)
					{
						sumNotFeature += pNot * (Math.Log(pNot) / Math.Log(2));
					}
				}
				//System.out.println(pNot+" "+(Math.log(pNot)/Math.log(2)));
				//logger.info(pFeature+" * "+sumFeature+" = +"+);
				//logger.info("^ "+pNotFeature+" "+sumNotFeature);
				ig[i_2] += pFeature * sumFeature + pNotFeature * sumNotFeature;
			}
			/* earlier the line above used to be: ig[i] = pFeature*sumFeature + pNotFeature*sumNotFeature;
			* This completely ignored the entropy term computed above. So added the "+=" to take that into account.
			* -Ramesh (nmramesh@cs.stanford.edu)
			*/
			return ig;
		}

		public virtual void UpdateLabels(int[] labels)
		{
			if (labels.Length != Size())
			{
				throw new ArgumentException("size of labels array does not match dataset size");
			}
			this.labels = labels;
		}

		public override string ToString()
		{
			return "Dataset of size " + size;
		}

		public virtual string ToSummaryString()
		{
			StringWriter sw = new StringWriter();
			PrintWriter pw = new PrintWriter(sw);
			pw.Println("Number of data points: " + Size());
			pw.Println("Number of active feature tokens: " + NumFeatureTokens());
			pw.Println("Number of active feature types:" + NumFeatureTypes());
			return pw.ToString();
		}

		/// <summary>Need to sort the counter by feature keys and dump it</summary>
		public static void PrintSVMLightFormat(StreamWriter pw, ClassicCounter<int> c, int classNo)
		{
			int[] features = Sharpen.Collections.ToArray(c.KeySet(), new int[c.KeySet().Count]);
			Arrays.Sort(features);
			StringBuilder sb = new StringBuilder();
			sb.Append(classNo);
			sb.Append(' ');
			foreach (int f in features)
			{
				sb.Append(f + 1).Append(':').Append(c.GetCount(f)).Append(' ');
			}
			pw.Println(sb.ToString());
		}
	}
}
