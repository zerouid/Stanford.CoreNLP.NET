using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// An interfacing class for
	/// <see cref="IClassifierFactory{L, F, C}"/>
	/// that incrementally builds
	/// a more memory-efficient representation of a
	/// <see cref="System.Collections.IList{E}"/>
	/// of
	/// <see cref="Edu.Stanford.Nlp.Ling.RVFDatum{L, F}"/>
	/// objects for the purposes of training a
	/// <see cref="IClassifier{L, F}"/>
	/// with a
	/// <see cref="IClassifierFactory{L, F, C}"/>
	/// .
	/// </summary>
	/// <author>Jenny Finkel (jrfinkel@stanford.edu)</author>
	/// <author>Rajat Raina (added methods to record data sources and ids)</author>
	/// <author>Anna Rafferty (various refactoring with GeneralDataset/Dataset)</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <?/>
	/// <?/>
	[System.Serializable]
	public class RVFDataset<L, F> : GeneralDataset<L, F>
	{
		private const long serialVersionUID = -3841757837680266182L;

		private double[][] values;

		private double[] minValues;

		private double[] maxValues;

		internal double[] means;

		internal double[] stdevs;

		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Classify.RVFDataset));

		private List<Pair<string, string>> sourcesAndIds;

		public RVFDataset()
			: this(10)
		{
		}

		public RVFDataset(int numDatums, IIndex<F> featureIndex, IIndex<L> labelIndex)
			: this(numDatums)
		{
			// implements Iterable<RVFDatum<L, F>>, Serializable
			// [datumIndex][i] values of features listed in int[][] data
			// = null; //stores the minValues of all features
			// for normalization.
			// = null; //stores the maxValues of all features
			// for normalization.
			// means and stdevs of features, used for
			/*
			* Store source and id of each datum; optional, and not fully supported.
			*/
			this.labelIndex = labelIndex;
			this.featureIndex = featureIndex;
		}

		public RVFDataset(IIndex<F> featureIndex, IIndex<L> labelIndex)
			: this(10)
		{
			this.labelIndex = labelIndex;
			this.featureIndex = featureIndex;
		}

		public RVFDataset(int numDatums)
		{
			Initialize(numDatums);
		}

		/// <summary>Constructor that fully specifies a Dataset.</summary>
		/// <remarks>
		/// Constructor that fully specifies a Dataset. Needed this for
		/// MulticlassDataset.
		/// </remarks>
		public RVFDataset(IIndex<L> labelIndex, int[] labels, IIndex<F> featureIndex, int[][] data, double[][] values)
		{
			this.labelIndex = labelIndex;
			this.labels = labels;
			this.featureIndex = featureIndex;
			this.data = data;
			this.values = values;
			this.size = labels.Length;
		}

		public override Pair<GeneralDataset<L, F>, GeneralDataset<L, F>> Split(double percentDev)
		{
			int devSize = (int)(percentDev * Size());
			int trainSize = Size() - devSize;
			int[][] devData = new int[devSize][];
			double[][] devValues = new double[devSize][];
			int[] devLabels = new int[devSize];
			int[][] trainData = new int[trainSize][];
			double[][] trainValues = new double[trainSize][];
			int[] trainLabels = new int[trainSize];
			lock (typeof(Runtime))
			{
				System.Array.Copy(data, 0, devData, 0, devSize);
				System.Array.Copy(values, 0, devValues, 0, devSize);
				System.Array.Copy(labels, 0, devLabels, 0, devSize);
				System.Array.Copy(data, devSize, trainData, 0, trainSize);
				System.Array.Copy(values, devSize, trainValues, 0, trainSize);
				System.Array.Copy(labels, devSize, trainLabels, 0, trainSize);
			}
			Edu.Stanford.Nlp.Classify.RVFDataset<L, F> dev = new Edu.Stanford.Nlp.Classify.RVFDataset<L, F>(labelIndex, devLabels, featureIndex, devData, devValues);
			Edu.Stanford.Nlp.Classify.RVFDataset<L, F> train = new Edu.Stanford.Nlp.Classify.RVFDataset<L, F>(labelIndex, trainLabels, featureIndex, trainData, trainValues);
			return new Pair<GeneralDataset<L, F>, GeneralDataset<L, F>>(train, dev);
		}

		public virtual void ScaleFeaturesGaussian()
		{
			means = new double[this.NumFeatures()];
			// Arrays.fill(means, 0); // not needed; Java arrays zero initialized
			for (int i = 0; i < this.Size(); i++)
			{
				for (int j = 0; j < data[i].Length; j++)
				{
					means[data[i][j]] += values[i][j];
				}
			}
			ArrayMath.MultiplyInPlace(means, 1.0 / this.Size());
			stdevs = new double[this.NumFeatures()];
			// Arrays.fill(stdevs, 0); // not needed; Java arrays zero initialized
			double[] deltaX = new double[this.NumFeatures()];
			for (int i_1 = 0; i_1 < this.Size(); i_1++)
			{
				for (int f = 0; f < this.NumFeatures(); f++)
				{
					deltaX[f] = -means[f];
				}
				for (int j = 0; j < data[i_1].Length; j++)
				{
					deltaX[data[i_1][j]] += values[i_1][j];
				}
				for (int f_1 = 0; f_1 < this.NumFeatures(); f_1++)
				{
					stdevs[f_1] += deltaX[f_1] * deltaX[f_1];
				}
			}
			for (int f_2 = 0; f_2 < this.NumFeatures(); f_2++)
			{
				stdevs[f_2] /= (this.Size() - 1);
				stdevs[f_2] = System.Math.Sqrt(stdevs[f_2]);
			}
			for (int i_2 = 0; i_2 < this.Size(); i_2++)
			{
				for (int j = 0; j < data[i_2].Length; j++)
				{
					int fID = data[i_2][j];
					if (stdevs[fID] != 0)
					{
						values[i_2][j] = (values[i_2][j] - means[fID]) / stdevs[fID];
					}
				}
			}
		}

		/// <summary>
		/// Scales feature values linearly such that each feature value lies between 0
		/// and 1.
		/// </summary>
		public virtual void ScaleFeatures()
		{
			// TODO: should also implement a method that scales the features using the
			// mean and std.
			minValues = new double[featureIndex.Size()];
			maxValues = new double[featureIndex.Size()];
			Arrays.Fill(minValues, double.PositiveInfinity);
			Arrays.Fill(maxValues, double.NegativeInfinity);
			// first identify the max and min values for each feature.
			// System.out.printf("number of datums: %d dataset size: %d\n",data.length,size());
			for (int i = 0; i < Size(); i++)
			{
				// System.out.printf("datum %d length %d\n", i,data[i].length);
				for (int j = 0; j < data[i].Length; j++)
				{
					int f = data[i][j];
					if (values[i][j] < minValues[f])
					{
						minValues[f] = values[i][j];
					}
					if (values[i][j] > maxValues[f])
					{
						maxValues[f] = values[i][j];
					}
				}
			}
			for (int f_1 = 0; f_1 < featureIndex.Size(); f_1++)
			{
				if (minValues[f_1] == double.PositiveInfinity)
				{
					throw new Exception("minValue for feature " + f_1 + " not assigned. ");
				}
				if (maxValues[f_1] == double.NegativeInfinity)
				{
					throw new Exception("maxValue for feature " + f_1 + " not assigned.");
				}
			}
			// now scale each value such that it's between 0 and 1.
			for (int i_1 = 0; i_1 < Size(); i_1++)
			{
				for (int j = 0; j < data[i_1].Length; j++)
				{
					int f = data[i_1][j];
					if (minValues[f_1] != maxValues[f_1])
					{
						// the equality can happen for binary
						// features which always take the value
						// of 1.0
						values[i_1][j] = (values[i_1][j] - minValues[f_1]) / (maxValues[f_1] - minValues[f_1]);
					}
				}
			}
		}

		/*
		for(int f = 0; f < featureIndex.size(); f++){
		if(minValues[f] == maxValues[f])
		throw new RuntimeException("minValue for feature "+f+" is equal to maxValue:"+minValues[f]);
		}
		*/
		/// <summary>Checks if the dataset has any unbounded values.</summary>
		/// <remarks>
		/// Checks if the dataset has any unbounded values. Always good to use this
		/// before training a model on the dataset. This way, one can avoid seeing the
		/// infamous 4's that get printed by the QuasiNewton Method when NaNs exist in
		/// the data! -Ramesh
		/// </remarks>
		public virtual void EnsureRealValues()
		{
			double[][] values = GetValuesArray();
			int[][] data = GetDataArray();
			for (int i = 0; i < Size(); i++)
			{
				for (int j = 0; j < values[i].Length; j++)
				{
					if (double.IsNaN(values[i][j]))
					{
						int fID = data[i][j];
						F feature = featureIndex.Get(fID);
						throw new Exception("datum " + i + " has a NaN value for feature:" + feature);
					}
					if (double.IsInfinite(values[i][j]))
					{
						int fID = data[i][j];
						F feature = featureIndex.Get(fID);
						throw new Exception("datum " + i + " has infinite value for feature:" + feature);
					}
				}
			}
		}

		/// <summary>
		/// Scales the values of each feature in each linearly using the min and max
		/// values found in the training set.
		/// </summary>
		/// <remarks>
		/// Scales the values of each feature in each linearly using the min and max
		/// values found in the training set. NOTE1: Not guaranteed to be between 0 and
		/// 1 for a test datum. NOTE2: Also filters out features from each datum that
		/// are not seen at training time.
		/// </remarks>
		/// <param name="dataset"/>
		/// <returns>a new dataset</returns>
		public virtual Edu.Stanford.Nlp.Classify.RVFDataset<L, F> ScaleDataset(Edu.Stanford.Nlp.Classify.RVFDataset<L, F> dataset)
		{
			Edu.Stanford.Nlp.Classify.RVFDataset<L, F> newDataset = new Edu.Stanford.Nlp.Classify.RVFDataset<L, F>(this.featureIndex, this.labelIndex);
			for (int i = 0; i < dataset.Size(); i++)
			{
				RVFDatum<L, F> datum = ((RVFDatum<L, F>)dataset.GetDatum(i));
				newDataset.Add(ScaleDatum(datum));
			}
			return newDataset;
		}

		/// <summary>
		/// Scales the values of each feature linearly using the min and max values
		/// found in the training set.
		/// </summary>
		/// <remarks>
		/// Scales the values of each feature linearly using the min and max values
		/// found in the training set. NOTE1: Not guaranteed to be between 0 and 1 for
		/// a test datum. NOTE2: Also filters out features from the datum that are not
		/// seen at training time.
		/// </remarks>
		/// <param name="datum"/>
		/// <returns>a new datum</returns>
		public virtual RVFDatum<L, F> ScaleDatum(RVFDatum<L, F> datum)
		{
			// scale this dataset before scaling the datum
			if (minValues == null || maxValues == null)
			{
				ScaleFeatures();
			}
			ICounter<F> scaledFeatures = new ClassicCounter<F>();
			foreach (F feature in datum.AsFeatures())
			{
				int fID = this.featureIndex.IndexOf(feature);
				if (fID >= 0)
				{
					double oldVal = datum.AsFeaturesCounter().GetCount(feature);
					double newVal;
					if (minValues[fID] != maxValues[fID])
					{
						newVal = (oldVal - minValues[fID]) / (maxValues[fID] - minValues[fID]);
					}
					else
					{
						newVal = oldVal;
					}
					scaledFeatures.IncrementCount(feature, newVal);
				}
			}
			return new RVFDatum<L, F>(scaledFeatures, datum.Label());
		}

		public virtual Edu.Stanford.Nlp.Classify.RVFDataset<L, F> ScaleDatasetGaussian(Edu.Stanford.Nlp.Classify.RVFDataset<L, F> dataset)
		{
			Edu.Stanford.Nlp.Classify.RVFDataset<L, F> newDataset = new Edu.Stanford.Nlp.Classify.RVFDataset<L, F>(this.featureIndex, this.labelIndex);
			for (int i = 0; i < dataset.Size(); i++)
			{
				RVFDatum<L, F> datum = ((RVFDatum<L, F>)dataset.GetDatum(i));
				newDataset.Add(ScaleDatumGaussian(datum));
			}
			return newDataset;
		}

		public virtual RVFDatum<L, F> ScaleDatumGaussian(RVFDatum<L, F> datum)
		{
			// scale this dataset before scaling the datum
			if (means == null || stdevs == null)
			{
				ScaleFeaturesGaussian();
			}
			ICounter<F> scaledFeatures = new ClassicCounter<F>();
			foreach (F feature in datum.AsFeatures())
			{
				int fID = this.featureIndex.IndexOf(feature);
				if (fID >= 0)
				{
					double oldVal = datum.AsFeaturesCounter().GetCount(feature);
					double newVal;
					if (stdevs[fID] != 0)
					{
						newVal = (oldVal - means[fID]) / stdevs[fID];
					}
					else
					{
						newVal = oldVal;
					}
					scaledFeatures.IncrementCount(feature, newVal);
				}
			}
			return new RVFDatum<L, F>(scaledFeatures, datum.Label());
		}

		public override Pair<GeneralDataset<L, F>, GeneralDataset<L, F>> Split(int start, int end)
		{
			int devSize = end - start;
			int trainSize = Size() - devSize;
			int[][] devData = new int[devSize][];
			double[][] devValues = new double[devSize][];
			int[] devLabels = new int[devSize];
			int[][] trainData = new int[trainSize][];
			double[][] trainValues = new double[trainSize][];
			int[] trainLabels = new int[trainSize];
			lock (typeof(Runtime))
			{
				System.Array.Copy(data, start, devData, 0, devSize);
				System.Array.Copy(values, start, devValues, 0, devSize);
				System.Array.Copy(labels, start, devLabels, 0, devSize);
				System.Array.Copy(data, 0, trainData, 0, start);
				System.Array.Copy(data, end, trainData, start, Size() - end);
				System.Array.Copy(values, 0, trainValues, 0, start);
				System.Array.Copy(values, end, trainValues, start, Size() - end);
				System.Array.Copy(labels, 0, trainLabels, 0, start);
				System.Array.Copy(labels, end, trainLabels, start, Size() - end);
			}
			if (this is WeightedRVFDataset<object, object>)
			{
				float[] trainWeights = new float[trainSize];
				float[] devWeights = new float[devSize];
				WeightedRVFDataset<L, F> w = (WeightedRVFDataset<L, F>)this;
				lock (typeof(Runtime))
				{
					System.Array.Copy(w.weights, start, devWeights, 0, devSize);
					System.Array.Copy(w.weights, 0, trainWeights, 0, start);
					System.Array.Copy(w.weights, end, trainWeights, start, Size() - end);
				}
				WeightedRVFDataset<L, F> dev = new WeightedRVFDataset<L, F>(labelIndex, devLabels, featureIndex, devData, devValues, devWeights);
				WeightedRVFDataset<L, F> train = new WeightedRVFDataset<L, F>(labelIndex, trainLabels, featureIndex, trainData, trainValues, trainWeights);
				return new Pair<GeneralDataset<L, F>, GeneralDataset<L, F>>(train, dev);
			}
			else
			{
				GeneralDataset<L, F> dev = new Edu.Stanford.Nlp.Classify.RVFDataset<L, F>(labelIndex, devLabels, featureIndex, devData, devValues);
				GeneralDataset<L, F> train = new Edu.Stanford.Nlp.Classify.RVFDataset<L, F>(labelIndex, trainLabels, featureIndex, trainData, trainValues);
				return new Pair<GeneralDataset<L, F>, GeneralDataset<L, F>>(train, dev);
			}
		}

		// TODO: Check that this does what we want for Datum other than RVFDatum
		public override void Add(IDatum<L, F> d)
		{
			// If you edit me, also take care of WeightedRVFDataset
			if (d is RVFDatum<object, object>)
			{
				AddLabel(d.Label());
				AddFeatures(((RVFDatum<L, F>)d).AsFeaturesCounter());
				size++;
			}
			else
			{
				AddLabel(d.Label());
				AddFeatures(Counters.AsCounter(d.AsFeatures()));
				size++;
			}
		}

		// If you edit me, also take care of WeightedRVFDataset
		public virtual void Add(IDatum<L, F> d, string src, string id)
		{
			if (d is RVFDatum<object, object>)
			{
				AddLabel(d.Label());
				AddFeatures(((RVFDatum<L, F>)d).AsFeaturesCounter());
				AddSourceAndId(src, id);
				size++;
			}
			else
			{
				AddLabel(d.Label());
				AddFeatures(Counters.AsCounter(d.AsFeatures()));
				AddSourceAndId(src, id);
				size++;
			}
		}

		// TODO shouldn't have both this and getRVFDatum
		public override IDatum<L, F> GetDatum(int index)
		{
			return GetRVFDatum(index);
		}

		/// <returns>
		/// the index-ed datum
		/// Note, this returns a new RVFDatum object, not the original RVFDatum
		/// that was added to the dataset.
		/// </returns>
		public override RVFDatum<L, F> GetRVFDatum(int index)
		{
			ClassicCounter<F> c = new ClassicCounter<F>();
			for (int i = 0; i < data[index].Length; i++)
			{
				c.IncrementCount(featureIndex.Get(data[index][i]), values[index][i]);
			}
			return new RVFDatum<L, F>(c, labelIndex.Get(labels[index]));
		}

		public virtual RVFDatum<L, F> GetRVFDatumWithId(int index)
		{
			RVFDatum<L, F> datum = GetRVFDatum(index);
			datum.SetID(GetRVFDatumId(index));
			return datum;
		}

		public virtual string GetRVFDatumSource(int index)
		{
			return sourcesAndIds[index].First();
		}

		public virtual string GetRVFDatumId(int index)
		{
			return sourcesAndIds[index].Second();
		}

		public virtual void AddAllWithSourcesAndIds(Edu.Stanford.Nlp.Classify.RVFDataset<L, F> data)
		{
			for (int index = 0; index < data.size; index++)
			{
				this.Add(data.GetRVFDatumWithId(index), data.GetRVFDatumSource(index), data.GetRVFDatumId(index));
			}
		}

		public override void AddAll<_T0>(IEnumerable<_T0> data)
		{
			foreach (IDatum<L, F> d in data)
			{
				this.Add(d);
			}
		}

		private void AddSourceAndId(string src, string id)
		{
			sourcesAndIds.Add(new Pair<string, string>(src, id));
		}

		private void AddLabel(L label)
		{
			if (labels.Length == size)
			{
				int[] newLabels = new int[size * 2];
				lock (typeof(Runtime))
				{
					System.Array.Copy(labels, 0, newLabels, 0, size);
				}
				labels = newLabels;
			}
			labels[size] = labelIndex.AddToIndex(label);
		}

		private void AddFeatures(ICounter<F> features)
		{
			if (data.Length == size)
			{
				int[][] newData = new int[size * 2][];
				double[][] newValues = new double[size * 2][];
				lock (typeof(Runtime))
				{
					System.Array.Copy(data, 0, newData, 0, size);
					System.Array.Copy(values, 0, newValues, 0, size);
				}
				data = newData;
				values = newValues;
			}
			IList<F> featureNames = new List<F>(features.KeySet());
			int nFeatures = featureNames.Count;
			data[size] = new int[nFeatures];
			values[size] = new double[nFeatures];
			for (int i = 0; i < nFeatures; ++i)
			{
				F feature = featureNames[i];
				int fID = featureIndex.AddToIndex(feature);
				if (fID >= 0)
				{
					data[size][i] = fID;
					values[size][i] = features.GetCount(feature);
				}
				else
				{
					// Usually a feature present at test but not training time.
					System.Diagnostics.Debug.Assert(featureIndex.IsLocked(), "Could not add feature to index: " + feature);
				}
			}
		}

		/// <summary>Resets the Dataset so that it is empty and ready to collect data.</summary>
		public override void Clear()
		{
			Clear(10);
		}

		/// <summary>Resets the Dataset so that it is empty and ready to collect data.</summary>
		public override void Clear(int numDatums)
		{
			Initialize(numDatums);
		}

		protected internal override void Initialize(int numDatums)
		{
			labelIndex = new HashIndex<L>();
			featureIndex = new HashIndex<F>();
			labels = new int[numDatums];
			data = new int[numDatums][];
			values = new double[numDatums][];
			sourcesAndIds = new List<Pair<string, string>>(numDatums);
			size = 0;
		}

		/// <summary>Prints some summary statistics to the logger for the Dataset.</summary>
		public override void SummaryStatistics()
		{
			logger.Info("numDatums: " + size);
			StringBuilder sb = new StringBuilder("numLabels: ");
			sb.Append(labelIndex.Size()).Append(" [");
			IEnumerator<L> iter = labelIndex.GetEnumerator();
			while (iter.MoveNext())
			{
				sb.Append(iter.Current);
				if (iter.MoveNext())
				{
					sb.Append(", ");
				}
			}
			sb.Append(']');
			logger.Info(sb.ToString());
			logger.Info("numFeatures (Phi(X) types): " + featureIndex.Size());
		}

		/*for(int i = 0; i < data.length; i++) {
		for(int j = 0; j < data[i].length; j++) {
		System.out.println(data[i][j]);
		}
		}*/
		/// <summary>prints the full feature matrix in tab-delimited form.</summary>
		/// <remarks>
		/// prints the full feature matrix in tab-delimited form. These can be BIG
		/// matrices, so be careful! [Can also use printFullFeatureMatrixWithValues]
		/// </remarks>
		public virtual void PrintFullFeatureMatrix(PrintWriter pw)
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
						pw.Print(sep + "1");
					}
					else
					{
						pw.Print(sep + "0");
					}
				}
				pw.Println();
			}
		}

		/// <summary>
		/// Modification of printFullFeatureMatrix to correct bugs and print values
		/// (Rajat).
		/// </summary>
		/// <remarks>
		/// Modification of printFullFeatureMatrix to correct bugs and print values
		/// (Rajat). Prints the full feature matrix in tab-delimited form. These can be
		/// BIG matrices, so be careful!
		/// </remarks>
		public virtual void PrintFullFeatureMatrixWithValues(PrintWriter pw)
		{
			string sep = "\t";
			for (int i = 0; i < featureIndex.Size(); i++)
			{
				pw.Print(sep + featureIndex.Get(i));
			}
			pw.Println();
			for (int i_1 = 0; i_1 < size; i_1++)
			{
				// changed labels.length to size
				pw.Print(labelIndex.Get(labels[i_1]));
				// changed i to labels[i]
				IDictionary<int, double> feats = Generics.NewHashMap();
				for (int j = 0; j < data[i_1].Length; j++)
				{
					int feature = data[i_1][j];
					double val = values[i_1][j];
					feats[int.Parse(feature)] = val;
				}
				for (int j_1 = 0; j_1 < featureIndex.Size(); j_1++)
				{
					if (feats.Contains(int.Parse(j_1)))
					{
						pw.Print(sep + feats[int.Parse(j_1)]);
					}
					else
					{
						pw.Print(sep);
						pw.Print(' ');
					}
				}
				pw.Println();
			}
			pw.Flush();
		}

		/// <summary>Constructs a Dataset by reading in a file in SVM light format.</summary>
		public static Edu.Stanford.Nlp.Classify.RVFDataset<string, string> ReadSVMLightFormat(string filename)
		{
			return ReadSVMLightFormat(filename, new HashIndex<string>(), new HashIndex<string>());
		}

		/// <summary>Constructs a Dataset by reading in a file in SVM light format.</summary>
		/// <remarks>
		/// Constructs a Dataset by reading in a file in SVM light format. The lines
		/// parameter is filled with the lines of the file for further processing (if
		/// lines is null, it is assumed no line information is desired)
		/// </remarks>
		public static Edu.Stanford.Nlp.Classify.RVFDataset<string, string> ReadSVMLightFormat(string filename, IList<string> lines)
		{
			return ReadSVMLightFormat(filename, new HashIndex<string>(), new HashIndex<string>(), lines);
		}

		/// <summary>Constructs a Dataset by reading in a file in SVM light format.</summary>
		/// <remarks>
		/// Constructs a Dataset by reading in a file in SVM light format. the created
		/// dataset has the same feature and label index as given
		/// </remarks>
		public static Edu.Stanford.Nlp.Classify.RVFDataset<string, string> ReadSVMLightFormat(string filename, IIndex<string> featureIndex, IIndex<string> labelIndex)
		{
			return ReadSVMLightFormat(filename, featureIndex, labelIndex, null);
		}

		/// <summary>Removes all features from the dataset that are not in featureSet.</summary>
		/// <param name="featureSet"/>
		public virtual void SelectFeaturesFromSet(ICollection<F> featureSet)
		{
			HashIndex<F> newFeatureIndex = new HashIndex<F>();
			int[] featMap = new int[featureIndex.Size()];
			Arrays.Fill(featMap, -1);
			foreach (F feature in featureSet)
			{
				int oldID = featureIndex.IndexOf(feature);
				if (oldID >= 0)
				{
					// it's a valid feature in the index
					int newID = newFeatureIndex.AddToIndex(feature);
					featMap[oldID] = newID;
				}
			}
			featureIndex = newFeatureIndex;
			for (int i = 0; i < size; i++)
			{
				IList<int> featList = new List<int>(data[i].Length);
				IList<double> valueList = new List<double>(values[i].Length);
				for (int j = 0; j < data[i].Length; j++)
				{
					if (featMap[data[i][j]] >= 0)
					{
						featList.Add(featMap[data[i][j]]);
						valueList.Add(values[i][j]);
					}
				}
				data[i] = new int[featList.Count];
				values[i] = new double[valueList.Count];
				for (int j_1 = 0; j_1 < data[i].Length; j_1++)
				{
					data[i][j_1] = featList[j_1];
					values[i][j_1] = valueList[j_1];
				}
			}
		}

		/// <summary>Applies a feature count threshold to the RVFDataset.</summary>
		/// <remarks>
		/// Applies a feature count threshold to the RVFDataset. All features that
		/// occur fewer than <i>k</i> times are expunged.
		/// </remarks>
		public override void ApplyFeatureCountThreshold(int k)
		{
			float[] counts = GetFeatureCounts();
			HashIndex<F> newFeatureIndex = new HashIndex<F>();
			int[] featMap = new int[featureIndex.Size()];
			for (int i = 0; i < featMap.Length; i++)
			{
				F feat = featureIndex.Get(i);
				if (counts[i] >= k)
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
				IList<double> valueList = new List<double>(values[i_1].Length);
				for (int j = 0; j < data[i_1].Length; j++)
				{
					if (featMap[data[i_1][j]] >= 0)
					{
						featList.Add(featMap[data[i_1][j]]);
						valueList.Add(values[i_1][j]);
					}
				}
				data[i_1] = new int[featList.Count];
				values[i_1] = new double[valueList.Count];
				for (int j_1 = 0; j_1 < data[i_1].Length; j_1++)
				{
					data[i_1][j_1] = featList[j_1];
					values[i_1][j_1] = valueList[j_1];
				}
			}
		}

		/// <summary>Applies a feature max count threshold to the RVFDataset.</summary>
		/// <remarks>
		/// Applies a feature max count threshold to the RVFDataset. All features that
		/// occur greater than <i>k</i> times are expunged.
		/// </remarks>
		public override void ApplyFeatureMaxCountThreshold(int k)
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
				IList<double> valueList = new List<double>(values[i_1].Length);
				for (int j = 0; j < data[i_1].Length; j++)
				{
					if (featMap[data[i_1][j]] >= 0)
					{
						featList.Add(featMap[data[i_1][j]]);
						valueList.Add(values[i_1][j]);
					}
				}
				data[i_1] = new int[featList.Count];
				values[i_1] = new double[valueList.Count];
				for (int j_1 = 0; j_1 < data[i_1].Length; j_1++)
				{
					data[i_1][j_1] = featList[j_1];
					values[i_1][j_1] = valueList[j_1];
				}
			}
		}

		private static Edu.Stanford.Nlp.Classify.RVFDataset<string, string> ReadSVMLightFormat(string filename, IIndex<string> featureIndex, IIndex<string> labelIndex, IList<string> lines)
		{
			BufferedReader @in = null;
			Edu.Stanford.Nlp.Classify.RVFDataset<string, string> dataset;
			try
			{
				dataset = new Edu.Stanford.Nlp.Classify.RVFDataset<string, string>(10, featureIndex, labelIndex);
				@in = IOUtils.ReaderFromString(filename);
				while (@in.Ready())
				{
					string line = @in.ReadLine();
					if (lines != null)
					{
						lines.Add(line);
					}
					dataset.Add(SvmLightLineToRVFDatum(line));
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(@in);
			}
			return dataset;
		}

		public static RVFDatum<string, string> SvmLightLineToRVFDatum(string l)
		{
			l = l.ReplaceFirst("#.*$", string.Empty);
			// remove any trailing comments
			string[] line = l.Split("\\s+");
			ClassicCounter<string> features = new ClassicCounter<string>();
			for (int i = 1; i < line.Length; i++)
			{
				string[] f = line[i].Split(":");
				if (f.Length != 2)
				{
					throw new ArgumentException("Bad data format: " + l);
				}
				double val = double.ParseDouble(f[1]);
				features.IncrementCount(f[0], val);
			}
			return new RVFDatum<string, string>(features, line[0]);
		}

		// todo [cdm 2012]: This duplicates the functionality of the methods above. Should be refactored.
		/// <summary>Read SVM-light formatted data into this dataset.</summary>
		/// <remarks>
		/// Read SVM-light formatted data into this dataset.
		/// A strict SVM-light format is expected, where labels and features are both
		/// encoded as integers. These integers are converted into the dataset label
		/// and feature types using the indexes stored in this dataset.
		/// </remarks>
		/// <param name="file">The file from which the data should be read.</param>
		public virtual void ReadSVMLightFormat(File file)
		{
			foreach (string line in IOUtils.ReadLines(file))
			{
				line = line.ReplaceAll("#.*", string.Empty);
				// remove any trailing comments
				string[] items = line.Split("\\s+");
				int label = System.Convert.ToInt32(items[0]);
				ICounter<F> features = new ClassicCounter<F>();
				for (int i = 1; i < items.Length; i++)
				{
					string[] featureItems = items[i].Split(":");
					int feature = System.Convert.ToInt32(featureItems[0]);
					double value = double.ParseDouble(featureItems[1]);
					features.IncrementCount(this.featureIndex.Get(feature), value);
				}
				this.Add(new RVFDatum<L, F>(features, this.labelIndex.Get(label)));
			}
		}

		/// <summary>Write the dataset in SVM-light format to the file.</summary>
		/// <remarks>
		/// Write the dataset in SVM-light format to the file.
		/// A strict SVM-light format will be written, where labels and features are
		/// both encoded as integers, using the label and feature indexes of this
		/// dataset. Datasets written by this method can be read by
		/// <see cref="RVFDataset{L, F}.ReadSVMLightFormat(Java.IO.File)"/>
		/// .
		/// </remarks>
		/// <param name="file">The location where the dataset should be written.</param>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public virtual void WriteSVMLightFormat(File file)
		{
			PrintWriter writer = new PrintWriter(file);
			WriteSVMLightFormat(writer);
			writer.Close();
		}

		public virtual void WriteSVMLightFormat(PrintWriter writer)
		{
			foreach (RVFDatum<L, F> datum in this)
			{
				writer.Print(this.labelIndex.IndexOf(datum.Label()));
				ICounter<F> features = datum.AsFeaturesCounter();
				foreach (F feature in features.KeySet())
				{
					double count = features.GetCount(feature);
					writer.Format(Locale.English, " %s:%f", this.featureIndex.IndexOf(feature), count);
				}
				writer.Println();
			}
		}

		/// <summary>
		/// Prints the sparse feature matrix using
		/// <see cref="RVFDataset{L, F}.PrintSparseFeatureMatrix(Java.IO.PrintWriter)"/>
		/// to
		/// <see cref="System.Console.Out">System.out</see>
		/// .
		/// </summary>
		public override void PrintSparseFeatureMatrix()
		{
			PrintSparseFeatureMatrix(new PrintWriter(System.Console.Out, true));
		}

		/// <summary>Prints a sparse feature matrix representation of the Dataset.</summary>
		/// <remarks>
		/// Prints a sparse feature matrix representation of the Dataset. Prints the
		/// actual
		/// <see cref="object.ToString()"/>
		/// representations of features.
		/// </remarks>
		public override void PrintSparseFeatureMatrix(PrintWriter pw)
		{
			string sep = "\t";
			for (int i = 0; i < size; i++)
			{
				pw.Print(labelIndex.Get(labels[i]));
				int[] datum = data[i];
				foreach (int feat in datum)
				{
					pw.Print(sep);
					pw.Print(featureIndex.Get(feat));
				}
				pw.Println();
			}
		}

		/// <summary>Prints a sparse feature-value output of the Dataset.</summary>
		/// <remarks>
		/// Prints a sparse feature-value output of the Dataset. Prints the actual
		/// <see cref="object.ToString()"/>
		/// representations of features. This is probably
		/// what you want for RVFDataset since the above two methods seem useless and
		/// unused.
		/// </remarks>
		public virtual void PrintSparseFeatureValues(PrintWriter pw)
		{
			for (int i = 0; i < size; i++)
			{
				PrintSparseFeatureValues(i, pw);
			}
		}

		/// <summary>Prints a sparse feature-value output of the Dataset.</summary>
		/// <remarks>
		/// Prints a sparse feature-value output of the Dataset. Prints the actual
		/// <see cref="object.ToString()"/>
		/// representations of features. This is probably
		/// what you want for RVFDataset since the above two methods seem useless and
		/// unused.
		/// </remarks>
		public virtual void PrintSparseFeatureValues(int datumNo, PrintWriter pw)
		{
			pw.Print(labelIndex.Get(labels[datumNo]));
			pw.Print('\t');
			pw.Println("LABEL");
			int[] datum = data[datumNo];
			double[] vals = values[datumNo];
			System.Diagnostics.Debug.Assert(datum.Length == vals.Length);
			for (int i = 0; i < datum.Length; i++)
			{
				pw.Print(featureIndex.Get(datum[i]));
				pw.Print('\t');
				pw.Println(vals[i]);
			}
			pw.Println();
		}

		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.Classify.RVFDataset<string, string> data = new Edu.Stanford.Nlp.Classify.RVFDataset<string, string>();
			ClassicCounter<string> c1 = new ClassicCounter<string>();
			c1.IncrementCount("fever", 3.5);
			c1.IncrementCount("cough", 1.1);
			c1.IncrementCount("congestion", 4.2);
			ClassicCounter<string> c2 = new ClassicCounter<string>();
			c2.IncrementCount("fever", 1.5);
			c2.IncrementCount("cough", 2.1);
			c2.IncrementCount("nausea", 3.2);
			ClassicCounter<string> c3 = new ClassicCounter<string>();
			c3.IncrementCount("cough", 2.5);
			c3.IncrementCount("congestion", 3.2);
			data.Add(new RVFDatum<string, string>(c1, "cold"));
			data.Add(new RVFDatum<string, string>(c2, "flu"));
			data.Add(new RVFDatum<string, string>(c3, "cold"));
			data.SummaryStatistics();
			LinearClassifierFactory<string, string> factory = new LinearClassifierFactory<string, string>();
			factory.UseQuasiNewton();
			LinearClassifier<string, string> c = factory.TrainClassifier(data);
			ClassicCounter<string> c4 = new ClassicCounter<string>();
			c4.IncrementCount("cough", 2.3);
			c4.IncrementCount("fever", 1.3);
			RVFDatum<string, string> datum = new RVFDatum<string, string>(c4);
			c.JustificationOf((IDatum<string, string>)datum);
		}

		public override double[][] GetValuesArray()
		{
			if (size == 0)
			{
				return new double[0][];
			}
			values = TrimToSize(values);
			data = TrimToSize(data);
			System.Diagnostics.Debug.Assert(values.Length == size);
			System.Diagnostics.Debug.Assert(values.Length == Size());
			return values;
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
			pw.Print("Number of labels: " + labelIndex.Size() + " [");
			IEnumerator<L> iter = labelIndex.GetEnumerator();
			while (iter.MoveNext())
			{
				pw.Print(iter.Current);
				if (iter.MoveNext())
				{
					pw.Print(", ");
				}
			}
			pw.Println("]");
			pw.Println("Number of features (Phi(X) types): " + featureIndex.Size());
			pw.Println("Number of active feature types: " + NumFeatureTypes());
			pw.Println("Number of active feature tokens: " + NumFeatureTokens());
			return sw.ToString();
		}

		/// <summary><inheritDoc/></summary>
		public override IEnumerator<RVFDatum<L, F>> GetEnumerator()
		{
			return new _IEnumerator_980(this);
		}

		private sealed class _IEnumerator_980 : IEnumerator<RVFDatum<L, F>>
		{
			public _IEnumerator_980(RVFDataset<L, F> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private int index;

			// = 0;
			public bool MoveNext()
			{
				return this.index < this._enclosing.size;
			}

			public RVFDatum<L, F> Current
			{
				get
				{
					if (this.index >= this._enclosing.size)
					{
						throw new NoSuchElementException();
					}
					RVFDatum<L, F> next = this._enclosing.GetRVFDatum(this.index);
					++this.index;
					return next;
				}
			}

			public void Remove()
			{
				throw new NotSupportedException();
			}

			private readonly RVFDataset<L, F> _enclosing;
		}

		/// <summary>Randomizes the data array in place.</summary>
		/// <remarks>
		/// Randomizes the data array in place. Needs to be redefined here because we
		/// need to randomize the values as well.
		/// </remarks>
		public override void Randomize(long randomSeed)
		{
			Random rand = new Random(randomSeed);
			for (int j = size - 1; j > 0; j--)
			{
				int randIndex = rand.NextInt(j);
				int[] tmp = data[randIndex];
				data[randIndex] = data[j];
				data[j] = tmp;
				int tmpl = labels[randIndex];
				labels[randIndex] = labels[j];
				labels[j] = tmpl;
				double[] tmpv = values[randIndex];
				values[randIndex] = values[j];
				values[j] = tmpv;
			}
		}

		/// <summary>Randomizes the data array in place.</summary>
		/// <remarks>
		/// Randomizes the data array in place. Needs to be redefined here because we
		/// need to randomize the values as well.
		/// </remarks>
		public override void ShuffleWithSideInformation<E>(long randomSeed, IList<E> sideInformation)
		{
			if (size != sideInformation.Count)
			{
				throw new ArgumentException("shuffleWithSideInformation: sideInformation not of same size as Dataset");
			}
			Random rand = new Random(randomSeed);
			for (int j = size - 1; j > 0; j--)
			{
				int randIndex = rand.NextInt(j);
				int[] tmp = data[randIndex];
				data[randIndex] = data[j];
				data[j] = tmp;
				int tmpl = labels[randIndex];
				labels[randIndex] = labels[j];
				labels[j] = tmpl;
				double[] tmpv = values[randIndex];
				values[randIndex] = values[j];
				values[j] = tmpv;
				E tmpE = sideInformation[randIndex];
				sideInformation.Set(randIndex, sideInformation[j]);
				sideInformation.Set(j, tmpE);
			}
		}
	}
}
