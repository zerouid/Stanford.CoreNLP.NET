// CRFClassifier -- a probabilistic (CRF) sequence model, mainly used for NER.
// Copyright (c) 2002-2016 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation,
// Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Text;
using Java.Util;
using Java.Util.Regex;
using Java.Util.Stream;
using Java.Util.Zip;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>Class for sequence classification using a Conditional Random Field model.</summary>
	/// <remarks>
	/// Class for sequence classification using a Conditional Random Field model.
	/// The code has functionality for different document formats, but when
	/// using the standard
	/// <see cref="Edu.Stanford.Nlp.Sequences.ColumnDocumentReaderAndWriter"/>
	/// for training
	/// or testing models, input files are expected to
	/// be one token per line with the columns indicating things like the word,
	/// POS, chunk, and answer class.  The default for
	/// <c>ColumnDocumentReaderAndWriter</c>
	/// training data is 3 column input,
	/// with the columns containing a word, its POS, and its gold class, but
	/// this can be specified via the
	/// <c>map</c>
	/// property.
	/// <p>
	/// When run on a file with
	/// <c>-textFile</c>
	/// or
	/// <c>-textFiles</c>
	/// ,
	/// the file is assumed to be plain English text (or perhaps simple HTML/XML),
	/// and a reasonable attempt is made at English tokenization by
	/// <see cref="Edu.Stanford.Nlp.Sequences.PlainTextDocumentReaderAndWriter{IN}"/>
	/// .  The class used to read
	/// the text can be changed with -plainTextDocumentReaderAndWriter.
	/// Extra options can be supplied to the tokenizer using the
	/// -tokenizerOptions flag.
	/// <p>
	/// To read from stdin, use the flag -readStdin.  The same
	/// reader/writer will be used as for -textFile.
	/// <p>
	/// <b>Typical command-line usage</b>
	/// <p>
	/// For running a trained model with a provided serialized classifier on a
	/// text file:
	/// <p>
	/// <c>
	/// java -mx500m edu.stanford.nlp.ie.crf.CRFClassifier -loadClassifier
	/// conll.ner.gz -textFile sampleSentences.txt
	/// </c>
	/// <p>
	/// When specifying all parameters in a properties file (train, test, or
	/// runtime):
	/// <p>
	/// <c>java -mx1g edu.stanford.nlp.ie.crf.CRFClassifier -prop propFile</c>
	/// <p>
	/// To train and test a simple NER model from the command line:
	/// <p>
	/// <c>java -mx1g edu.stanford.nlp.ie.crf.CRFClassifier -trainFile trainFile -testFile testFile -macro &gt; output</c>
	/// <p>
	/// To train with multiple files:
	/// <p>
	/// <c>java -mx1g edu.stanford.nlp.ie.crf.CRFClassifier -trainFileList file1,file2,... -testFile testFile -macro &gt; output</c>
	/// <p>
	/// To test on multiple files, use the -testFiles option and a comma
	/// separated list.
	/// <p>
	/// Features are defined by a
	/// <see cref="Edu.Stanford.Nlp.Sequences.FeatureFactory{IN}"/>
	/// .
	/// <see cref="Edu.Stanford.Nlp.IE.NERFeatureFactory{IN}"/>
	/// is used by default, and you should look
	/// there for feature templates and properties or flags that will cause
	/// certain features to be used when training an NER classifier. There
	/// are also various feature factories for Chinese word segmentation
	/// such as
	/// <see cref="Edu.Stanford.Nlp.Wordseg.ChineseSegmenterFeatureFactory{IN}"/>
	/// .
	/// Features are specified either
	/// by a Properties file (which is the recommended method) or by flags on the
	/// command line. The flags are read into a
	/// <see cref="Edu.Stanford.Nlp.Sequences.SeqClassifierFlags"/>
	/// object,
	/// which the user need not be concerned with, unless wishing to add new
	/// features.
	/// <p>
	/// CRFClassifier may also be used programmatically. When creating
	/// a new instance, you <i>must</i> specify a Properties object. You may then
	/// call train methods to train a classifier, or load a classifier. The other way
	/// to get a CRFClassifier is to deserialize one via the static
	/// <see cref="CRFClassifier{IN}.GetClassifier(string)"/>
	/// methods, which return a
	/// deserialized classifier. You may then tag (classify the items of) documents
	/// using either the assorted
	/// <c>classify()</c>
	/// methods here or the additional
	/// ones in
	/// <see cref="Edu.Stanford.Nlp.IE.AbstractSequenceClassifier{IN}"/>
	/// .
	/// Probabilities assigned by the CRF can be interrogated using either the
	/// <c>printProbsDocument()</c>
	/// or
	/// <c>getCliqueTrees()</c>
	/// methods.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	/// <author>Sonal Gupta (made the class generic)</author>
	/// <author>Mengqiu Wang (LOP implementation and non-linear CRF implementation)</author>
	public class CRFClassifier<In> : AbstractSequenceClassifier<IN>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFClassifier));

		internal IList<IIndex<CRFLabel>> labelIndices;

		internal IIndex<string> tagIndex;

		private Pair<double[][], double[][]> entityMatrices;

		internal ICliquePotentialFunction cliquePotentialFunction;

		internal IHasCliquePotentialFunction cliquePotentialFunctionHelper;

		/// <summary>Parameter weights of the classifier.</summary>
		/// <remarks>Parameter weights of the classifier.  weights[featureIndex][labelIndex]</remarks>
		internal double[][] weights;

		/// <summary>index the features of CRF</summary>
		internal IIndex<string> featureIndex;

		/// <summary>caches the featureIndex</summary>
		internal int[] map;

		internal Random random = new Random(2147483647L);

		internal IIndex<int> nodeFeatureIndicesMap;

		internal IIndex<int> edgeFeatureIndicesMap;

		private IDictionary<string, double[]> embeddings;

		/// <summary>Name of default serialized classifier resource to look for in a jar file.</summary>
		public const string DefaultClassifier = "edu/stanford/nlp/models/ner/english.all.3class.distsim.crf.ser.gz";

		private const bool Verbose = false;

		/// <summary>Fields for grouping features</summary>
		private Pattern suffixPatt = Pattern.Compile(".+?((?:-[A-Z]+)+)\\|.*C");

		private IIndex<string> templateGroupIndex;

		private IDictionary<int, int> featureIndexToTemplateIndex;

		private LabelDictionary labelDictionary;

		protected internal CRFClassifier()
			: base(new SeqClassifierFlags())
		{
		}

		public CRFClassifier(Properties props)
			: base(props)
		{
		}

		public CRFClassifier(SeqClassifierFlags flags)
			: base(flags)
		{
		}

		/// <summary>Makes a copy of the crf classifier</summary>
		public CRFClassifier(Edu.Stanford.Nlp.IE.Crf.CRFClassifier<IN> crf)
			: base(crf.flags)
		{
			// TODO(mengqiu) need to move the embedding lookup and capitalization features into a FeatureFactory
			// = null;
			// Label dictionary for fast decoding
			// List selftraindatums = new ArrayList();
			this.windowSize = crf.windowSize;
			this.featureFactories = crf.featureFactories;
			this.pad = crf.pad;
			if (crf.knownLCWords == null)
			{
				this.knownLCWords = new MaxSizeConcurrentHashSet<string>(crf.flags.maxAdditionalKnownLCWords);
			}
			else
			{
				this.knownLCWords = new MaxSizeConcurrentHashSet<string>(crf.knownLCWords);
				this.knownLCWords.SetMaxSize(this.knownLCWords.Count + crf.flags.maxAdditionalKnownLCWords);
			}
			this.featureIndex = (crf.featureIndex != null) ? new HashIndex<string>(crf.featureIndex.ObjectsList()) : null;
			this.classIndex = (crf.classIndex != null) ? new HashIndex<string>(crf.classIndex.ObjectsList()) : null;
			if (crf.labelIndices != null)
			{
				this.labelIndices = new List<IIndex<CRFLabel>>(crf.labelIndices.Count);
				for (int i = 0; i < crf.labelIndices.Count; i++)
				{
					this.labelIndices.Add((crf.labelIndices[i] != null) ? new HashIndex<CRFLabel>(crf.labelIndices[i].ObjectsList()) : null);
				}
			}
			else
			{
				this.labelIndices = null;
			}
			this.cliquePotentialFunction = crf.cliquePotentialFunction;
		}

		/// <summary>Returns the total number of weights associated with this classifier.</summary>
		/// <returns>number of weights</returns>
		public virtual int GetNumWeights()
		{
			if (weights == null)
			{
				return 0;
			}
			int numWeights = 0;
			foreach (double[] wts in weights)
			{
				numWeights += wts.Length;
			}
			return numWeights;
		}

		/// <summary>Get index of featureType for feature indexed by i.</summary>
		/// <remarks>
		/// Get index of featureType for feature indexed by i. (featureType index is
		/// used to index labelIndices to get labels.)
		/// </remarks>
		/// <param name="i">Feature index</param>
		/// <returns>index of featureType</returns>
		private int GetFeatureTypeIndex(int i)
		{
			return GetFeatureTypeIndex(featureIndex.Get(i));
		}

		/// <summary>
		/// Get index of featureType for feature based on the feature string
		/// (featureType index used to index labelIndices to get labels)
		/// </summary>
		/// <param name="feature">Feature string</param>
		/// <returns>index of featureType</returns>
		private static int GetFeatureTypeIndex(string feature)
		{
			if (feature.EndsWith("|C"))
			{
				return 0;
			}
			else
			{
				if (feature.EndsWith("|CpC"))
				{
					return 1;
				}
				else
				{
					if (feature.EndsWith("|Cp2C"))
					{
						return 2;
					}
					else
					{
						if (feature.EndsWith("|Cp3C"))
						{
							return 3;
						}
						else
						{
							if (feature.EndsWith("|Cp4C"))
							{
								return 4;
							}
							else
							{
								if (feature.EndsWith("|Cp5C"))
								{
									return 5;
								}
								else
								{
									throw new Exception("Unknown feature type " + feature);
								}
							}
						}
					}
				}
			}
		}

		/// <summary>Scales the weights of this CRFClassifier by the specified weight.</summary>
		/// <param name="scale">The scale to multiply by</param>
		public virtual void ScaleWeights(double scale)
		{
			for (int i = 0; i < weights.Length; i++)
			{
				for (int j = 0; j < weights[i].Length; j++)
				{
					weights[i][j] *= scale;
				}
			}
		}

		/// <summary>
		/// Combines weights from another crf (scaled by weight) into this CRF's
		/// weights (assumes that this CRF's indices have already been updated to
		/// include features/labels from the other crf)
		/// </summary>
		/// <param name="crf">Other CRF whose weights to combine into this CRF</param>
		/// <param name="weight">Amount to scale the other CRF's weights by</param>
		private void CombineWeights(Edu.Stanford.Nlp.IE.Crf.CRFClassifier<IN> crf, double weight)
		{
			int numFeatures = featureIndex.Size();
			int oldNumFeatures = weights.Length;
			// Create a map of other crf labels to this crf labels
			IDictionary<CRFLabel, CRFLabel> crfLabelMap = Generics.NewHashMap();
			for (int i = 0; i < crf.labelIndices.Count; i++)
			{
				for (int j = 0; j < crf.labelIndices[i].Size(); j++)
				{
					CRFLabel labels = crf.labelIndices[i].Get(j);
					int[] newLabelIndices = new int[i + 1];
					for (int ci = 0; ci <= i; ci++)
					{
						string classLabel = crf.classIndex.Get(labels.GetLabel()[ci]);
						newLabelIndices[ci] = this.classIndex.IndexOf(classLabel);
					}
					CRFLabel newLabels = new CRFLabel(newLabelIndices);
					crfLabelMap[labels] = newLabels;
					int k = this.labelIndices[i].IndexOf(newLabels);
				}
			}
			// IMPORTANT: the indexing is needed, even when not printed out!
			// log.info("LabelIndices " + i + " " + labels + ": " + j +
			// " mapped to " + k);
			// Create map of featureIndex to featureTypeIndex
			map = new int[numFeatures];
			for (int i_1 = 0; i_1 < numFeatures; i_1++)
			{
				map[i_1] = GetFeatureTypeIndex(i_1);
			}
			// Create new weights
			double[][] newWeights = new double[numFeatures][];
			for (int i_2 = 0; i_2 < numFeatures; i_2++)
			{
				int length = labelIndices[map[i_2]].Size();
				newWeights[i_2] = new double[length];
				if (i_2 < oldNumFeatures)
				{
					System.Diagnostics.Debug.Assert((length >= weights[i_2].Length));
					System.Array.Copy(weights[i_2], 0, newWeights[i_2], 0, weights[i_2].Length);
				}
			}
			weights = newWeights;
			// Get original weight indices from other crf and weight them in
			// depending on the type of the feature, different number of weights is
			// associated with it
			for (int i_3 = 0; i_3 < crf.weights.Length; i_3++)
			{
				string feature = crf.featureIndex.Get(i_3);
				int newIndex = featureIndex.IndexOf(feature);
				// Check weights are okay dimension
				if (weights[newIndex].Length < crf.weights[i_3].Length)
				{
					throw new Exception("Incompatible CRFClassifier: weight length mismatch for feature " + newIndex + ": " + featureIndex.Get(newIndex) + " (also feature " + i_3 + ": " + crf.featureIndex.Get(i_3) + ") " + ", len1=" + weights[newIndex].Length +
						 ", len2=" + crf.weights[i_3].Length);
				}
				int featureTypeIndex = map[newIndex];
				for (int j = 0; j < crf.weights[i_3].Length; j++)
				{
					CRFLabel labels = crf.labelIndices[featureTypeIndex].Get(j);
					CRFLabel newLabels = crfLabelMap[labels];
					int k = this.labelIndices[featureTypeIndex].IndexOf(newLabels);
					weights[newIndex][k] += crf.weights[i_3][j] * weight;
				}
			}
		}

		/// <summary>Combines weighted crf with this crf.</summary>
		/// <param name="crf">Other CRF whose weights to combine into this CRF</param>
		/// <param name="weight">Amount to scale the other CRF's weights by</param>
		public virtual void Combine(Edu.Stanford.Nlp.IE.Crf.CRFClassifier<IN> crf, double weight)
		{
			Timing timer = new Timing();
			// Check the CRFClassifiers are compatible
			if (!this.pad.Equals(crf.pad))
			{
				throw new Exception("Incompatible CRFClassifier: pad does not match");
			}
			if (this.windowSize != crf.windowSize)
			{
				throw new Exception("Incompatible CRFClassifier: windowSize does not match");
			}
			if (this.labelIndices.Count != crf.labelIndices.Count)
			{
				// Should match since this should be same as the windowSize
				throw new Exception("Incompatible CRFClassifier: labelIndices length does not match");
			}
			this.classIndex.AddAll(crf.classIndex.ObjectsList());
			// Combine weights of the other classifier with this classifier,
			// weighing the other classifier's weights by weight
			// First merge the feature indices
			int oldNumFeatures1 = this.featureIndex.Size();
			int oldNumFeatures2 = crf.featureIndex.Size();
			int oldNumWeights1 = this.GetNumWeights();
			int oldNumWeights2 = crf.GetNumWeights();
			this.featureIndex.AddAll(crf.featureIndex.ObjectsList());
			Sharpen.Collections.AddAll(this.knownLCWords, crf.knownLCWords);
			System.Diagnostics.Debug.Assert((weights.Length == oldNumFeatures1));
			// Combine weights of this classifier with other classifier
			for (int i = 0; i < labelIndices.Count; i++)
			{
				this.labelIndices[i].AddAll(crf.labelIndices[i].ObjectsList());
			}
			log.Info("Combining weights: will automatically match labelIndices");
			CombineWeights(crf, weight);
			int numFeatures = featureIndex.Size();
			int numWeights = GetNumWeights();
			long elapsedMs = timer.Stop();
			log.Info("numFeatures: orig1=" + oldNumFeatures1 + ", orig2=" + oldNumFeatures2 + ", combined=" + numFeatures);
			log.Info("numWeights: orig1=" + oldNumWeights1 + ", orig2=" + oldNumWeights2 + ", combined=" + numWeights);
			log.Info("Time to combine CRFClassifier: " + Timing.ToSecondsString(elapsedMs) + " seconds");
		}

		public virtual void DropFeaturesBelowThreshold(double threshold)
		{
			IIndex<string> newFeatureIndex = new HashIndex<string>();
			for (int i = 0; i < weights.Length; i++)
			{
				double smallest = weights[i][0];
				double biggest = weights[i][0];
				for (int j = 1; j < weights[i].Length; j++)
				{
					if (weights[i][j] > biggest)
					{
						biggest = weights[i][j];
					}
					if (weights[i][j] < smallest)
					{
						smallest = weights[i][j];
					}
					if (biggest - smallest > threshold)
					{
						newFeatureIndex.Add(featureIndex.Get(i));
						break;
					}
				}
			}
			int[] newMap = new int[newFeatureIndex.Size()];
			for (int i_1 = 0; i_1 < newMap.Length; i_1++)
			{
				int index = featureIndex.IndexOf(newFeatureIndex.Get(i_1));
				newMap[i_1] = map[index];
			}
			map = newMap;
			featureIndex = newFeatureIndex;
		}

		/// <summary>Convert a document List into arrays storing the data features and labels.</summary>
		/// <remarks>
		/// Convert a document List into arrays storing the data features and labels.
		/// This is used at test time.
		/// </remarks>
		/// <param name="document">Testing documents</param>
		/// <returns>
		/// A Triple, where the first element is an int[][][] representing the
		/// data, the second element is an int[] representing the labels, and
		/// the third element is a double[][][] representing the feature values (optionally null)
		/// </returns>
		public virtual Triple<int[][][], int[], double[][][]> DocumentToDataAndLabels(IList<IN> document)
		{
			int docSize = document.Count;
			// first index is position in the document also the index of the
			// clique/factor table
			// second index is the number of elements in the clique/window these
			// features are for (starting with last element)
			// third index is position of the feature in the array that holds them.
			// An element in data[j][k][m] is the feature index of the mth feature occurring in
			// position k of the jth clique
			int[][][] data = new int[docSize][][];
			double[][][] featureVals = new double[docSize][][];
			// index is the position in the document.
			// element in labels[j] is the index of the correct label (if it exists) at
			// position j of document
			int[] labels = new int[docSize];
			if (flags.useReverse)
			{
				Java.Util.Collections.Reverse(document);
			}
			// log.info("docSize:"+docSize);
			for (int j = 0; j < docSize; j++)
			{
				CRFDatum<IList<string>, CRFLabel> d = MakeDatum(document, j, featureFactories);
				IList<IList<string>> features = d.AsFeatures();
				IList<double[]> featureValList = d.AsFeatureVals();
				for (int k = 0; k < fSize; k++)
				{
					ICollection<string> cliqueFeatures = features[k];
					data[j][k] = new int[cliqueFeatures.Count];
					if (featureValList != null)
					{
						// CRFBiasedClassifier.makeDatum causes null
						featureVals[j][k] = featureValList[k];
					}
					int m = 0;
					foreach (string feature in cliqueFeatures)
					{
						int index = featureIndex.IndexOf(feature);
						if (index >= 0)
						{
							data[j][k][m] = index;
							m++;
						}
					}
					// this is where we end up when we do feature threshold cutoffs
					if (m < data[j][k].Length)
					{
						int[] f = new int[m];
						System.Array.Copy(data[j][k], 0, f, 0, m);
						data[j][k] = f;
						if (featureVals[j][k] != null)
						{
							double[] fVal = new double[m];
							System.Array.Copy(featureVals[j][k], 0, fVal, 0, m);
							featureVals[j][k] = fVal;
						}
					}
				}
				IN wi = document[j];
				labels[j] = classIndex.IndexOf(wi.Get(typeof(CoreAnnotations.AnswerAnnotation)));
			}
			if (flags.useReverse)
			{
				Java.Util.Collections.Reverse(document);
			}
			return new Triple<int[][][], int[], double[][][]>(data, labels, featureVals);
		}

		private int[][][] TransformDocData(int[][][] docData)
		{
			int[][][] transData = new int[docData.Length][][];
			for (int i = 0; i < docData.Length; i++)
			{
				transData[i] = new int[docData[i].Length][];
				for (int j = 0; j < docData[i].Length; j++)
				{
					int[] cliqueFeatures = docData[i][j];
					transData[i][j] = new int[cliqueFeatures.Length];
					for (int n = 0; n < cliqueFeatures.Length; n++)
					{
						int transFeatureIndex;
						// initialized below;
						if (j == 0)
						{
							transFeatureIndex = nodeFeatureIndicesMap.IndexOf(cliqueFeatures[n]);
							if (transFeatureIndex == -1)
							{
								throw new Exception("node cliqueFeatures[n]=" + cliqueFeatures[n] + " not found, nodeFeatureIndicesMap.size=" + nodeFeatureIndicesMap.Size());
							}
						}
						else
						{
							transFeatureIndex = edgeFeatureIndicesMap.IndexOf(cliqueFeatures[n]);
							if (transFeatureIndex == -1)
							{
								throw new Exception("edge cliqueFeatures[n]=" + cliqueFeatures[n] + " not found, edgeFeatureIndicesMap.size=" + edgeFeatureIndicesMap.Size());
							}
						}
						transData[i][j][n] = transFeatureIndex;
					}
				}
			}
			return transData;
		}

		/// <exception cref="System.Exception"/>
		public virtual void PrintLabelInformation(string testFile, IDocumentReaderAndWriter<IN> readerAndWriter)
		{
			ObjectBank<IList<IN>> documents = MakeObjectBankFromFile(testFile, readerAndWriter);
			foreach (IList<IN> document in documents)
			{
				PrintLabelValue(document);
			}
		}

		public virtual void PrintLabelValue(IList<IN> document)
		{
			if (flags.useReverse)
			{
				Java.Util.Collections.Reverse(document);
			}
			NumberFormat nf = new DecimalFormat();
			IList<string> classes = new List<string>();
			for (int i = 0; i < classIndex.Size(); i++)
			{
				classes.Add(classIndex.Get(i));
			}
			string[] columnHeaders = Sharpen.Collections.ToArray(classes, new string[classes.Count]);
			// log.info("docSize:"+docSize);
			for (int j = 0; j < document.Count; j++)
			{
				System.Console.Out.WriteLine("--== " + document[j].Get(typeof(CoreAnnotations.TextAnnotation)) + " ==--");
				IList<string[]> lines = new List<string[]>();
				IList<string> rowHeaders = new List<string>();
				IList<string> line = new List<string>();
				for (int p = 0; p < labelIndices.Count; p++)
				{
					if (j + p >= document.Count)
					{
						continue;
					}
					CRFDatum<IList<string>, CRFLabel> d = MakeDatum(document, j + p, featureFactories);
					IList<IList<string>> features = d.AsFeatures();
					for (int k = p; k < fSize; k++)
					{
						ICollection<string> cliqueFeatures = features[k];
						foreach (string feature in cliqueFeatures)
						{
							int index = featureIndex.IndexOf(feature);
							if (index >= 0)
							{
								// line.add(feature+"["+(-p)+"]");
								rowHeaders.Add(feature + '[' + (-p) + ']');
								double[] values = new double[labelIndices[0].Size()];
								foreach (CRFLabel label in labelIndices[k])
								{
									int[] l = label.GetLabel();
									double v = weights[index][labelIndices[k].IndexOf(label)];
									values[l[l.Length - 1 - p]] += v;
								}
								foreach (double value in values)
								{
									line.Add(nf.Format(value));
								}
								lines.Add(Sharpen.Collections.ToArray(line, new string[line.Count]));
								line = new List<string>();
							}
						}
					}
					// lines.add(Collections.<String>emptyList());
					System.Console.Out.WriteLine(StringUtils.MakeTextTable(Sharpen.Collections.ToArray(lines, new string[][] {  }), Sharpen.Collections.ToArray(rowHeaders, new string[rowHeaders.Count]), columnHeaders, 0, 1, true));
					System.Console.Out.WriteLine();
				}
			}
			// log.info(edu.stanford.nlp.util.StringUtils.join(lines,"\n"));
			if (flags.useReverse)
			{
				Java.Util.Collections.Reverse(document);
			}
		}

		/// <summary>Convert an ObjectBank to arrays of data features and labels.</summary>
		/// <remarks>
		/// Convert an ObjectBank to arrays of data features and labels.
		/// This version is used at training time.
		/// </remarks>
		/// <returns>
		/// A Triple, where the first element is an int[][][][] representing the
		/// data, the second element is an int[][] representing the labels, and
		/// the third element is a double[][][][] representing the feature values
		/// which could be optionally left as null.
		/// </returns>
		public virtual Triple<int[][][][], int[][], double[][][][]> DocumentsToDataAndLabels(ICollection<IList<IN>> documents)
		{
			// first index is the number of the document
			// second index is position in the document also the index of the
			// clique/factor table
			// third index is the number of elements in the clique/window these features
			// are for (starting with last element)
			// fourth index is position of the feature in the array that holds them
			// element in data[i][j][k][m] is the index of the mth feature occurring in
			// position k of the jth clique of the ith document
			// int[][][][] data = new int[documentsSize][][][];
			IList<int[][][]> data = new List<int[][][]>();
			IList<double[][][]> featureVal = new List<double[][][]>();
			// first index is the number of the document
			// second index is the position in the document
			// element in labels[i][j] is the index of the correct label (if it exists)
			// at position j in document i
			// int[][] labels = new int[documentsSize][];
			IList<int[]> labels = new List<int[]>();
			int numDatums = 0;
			foreach (IList<IN> doc in documents)
			{
				Triple<int[][][], int[], double[][][]> docTriple = DocumentToDataAndLabels(doc);
				data.Add(docTriple.First());
				labels.Add(docTriple.Second());
				if (flags.useEmbedding)
				{
					featureVal.Add(docTriple.Third());
				}
				numDatums += doc.Count;
			}
			log.Info("numClasses: " + classIndex.Size() + ' ' + classIndex);
			log.Info("numDocuments: " + data.Count);
			log.Info("numDatums: " + numDatums);
			log.Info("numFeatures: " + featureIndex.Size());
			PrintFeatures();
			double[][][][] featureValArr = null;
			if (flags.useEmbedding)
			{
				featureValArr = Sharpen.Collections.ToArray(featureVal, new double[data.Count][][][]);
			}
			return new Triple<int[][][][], int[][], double[][][][]>(Sharpen.Collections.ToArray(data, new int[data.Count][][][]), Sharpen.Collections.ToArray(labels, new int[labels.Count][]), featureValArr);
		}

		/// <summary>
		/// Convert an ObjectBank to corresponding collection of data features and
		/// labels.
		/// </summary>
		/// <remarks>
		/// Convert an ObjectBank to corresponding collection of data features and
		/// labels. This version is used at test time.
		/// </remarks>
		/// <returns>
		/// A List of pairs, one for each document, where the first element is
		/// an int[][][] representing the data and the second element is an
		/// int[] representing the labels.
		/// </returns>
		public virtual IList<Triple<int[][][], int[], double[][][]>> DocumentsToDataAndLabelsList(ICollection<IList<IN>> documents)
		{
			int numDatums = 0;
			IList<Triple<int[][][], int[], double[][][]>> docList = new List<Triple<int[][][], int[], double[][][]>>();
			foreach (IList<IN> doc in documents)
			{
				Triple<int[][][], int[], double[][][]> docTriple = DocumentToDataAndLabels(doc);
				docList.Add(docTriple);
				numDatums += doc.Count;
			}
			log.Info("numClasses: " + classIndex.Size() + ' ' + classIndex);
			log.Info("numDocuments: " + docList.Count);
			log.Info("numDatums: " + numDatums);
			log.Info("numFeatures: " + featureIndex.Size());
			return docList;
		}

		protected internal virtual void PrintFeatures()
		{
			if (flags.printFeatures == null)
			{
				return;
			}
			try
			{
				string enc = flags.inputEncoding;
				if (flags.inputEncoding == null)
				{
					log.Info("flags.inputEncoding doesn't exist, using UTF-8 as default");
					enc = "UTF-8";
				}
				PrintWriter pw = new PrintWriter(new OutputStreamWriter(new FileOutputStream("features-" + flags.printFeatures + ".txt"), enc), true);
				foreach (string feat in featureIndex)
				{
					pw.Println(feat);
				}
				pw.Close();
			}
			catch (IOException ioe)
			{
				Sharpen.Runtime.PrintStackTrace(ioe);
			}
		}

		/// <summary>
		/// This routine builds the
		/// <c>labelIndices</c>
		/// which give the
		/// empirically legal label sequences (of length (order) at most
		/// <c>windowSize</c>
		/// ) and the
		/// <c>classIndex</c>
		/// , which indexes
		/// known answer classes.
		/// </summary>
		/// <param name="ob">
		/// The training data: Read from an ObjectBank, each item in it is a
		/// <c>List&lt;CoreLabel&gt;</c>
		/// .
		/// </param>
		protected internal virtual void MakeAnswerArraysAndTagIndex(ICollection<IList<IN>> ob)
		{
			bool useFeatureCountThresh = flags.featureCountThresh > 1;
			ICollection<string>[] featureIndices = new HashSet[windowSize];
			IDictionary<string, int>[] featureCountIndices = null;
			for (int i = 0; i < windowSize; i++)
			{
				featureIndices[i] = Generics.NewHashSet();
			}
			if (useFeatureCountThresh)
			{
				featureCountIndices = new Hashtable[windowSize];
				for (int i_1 = 0; i_1 < windowSize; i_1++)
				{
					featureCountIndices[i_1] = Generics.NewHashMap();
				}
			}
			labelIndices = new List<IIndex<CRFLabel>>(windowSize);
			for (int i_2 = 0; i_2 < windowSize; i_2++)
			{
				labelIndices.Add(new HashIndex<CRFLabel>());
			}
			IIndex<CRFLabel> labelIndex = labelIndices[windowSize - 1];
			if (classIndex == null)
			{
				classIndex = new HashIndex<string>();
			}
			// classIndex.add("O");
			classIndex.Add(flags.backgroundSymbol);
			ICollection<string>[] seenBackgroundFeatures = new HashSet[2];
			seenBackgroundFeatures[0] = Generics.NewHashSet();
			seenBackgroundFeatures[1] = Generics.NewHashSet();
			int wordCount = 0;
			if (flags.labelDictionaryCutoff > 0)
			{
				this.labelDictionary = new LabelDictionary();
			}
			foreach (IList<IN> doc in ob)
			{
				if (flags.useReverse)
				{
					Java.Util.Collections.Reverse(doc);
				}
				// create the full set of labels in classIndex
				// note: update to use addAll later
				foreach (IN token in doc)
				{
					wordCount++;
					string ans = token.Get(typeof(CoreAnnotations.AnswerAnnotation));
					if (ans == null || ans.IsEmpty())
					{
						throw new ArgumentException("Word " + wordCount + " (\"" + token.Get(typeof(CoreAnnotations.TextAnnotation)) + "\") has a blank answer");
					}
					classIndex.Add(ans);
					if (labelDictionary != null)
					{
						string observation = token.Get(typeof(CoreAnnotations.TextAnnotation));
						labelDictionary.Increment(observation, ans);
					}
				}
				for (int j = 0; j < docSize; j++)
				{
					CRFDatum<IList<string>, CRFLabel> d = MakeDatum(doc, j, featureFactories);
					labelIndex.Add(d.Label());
					IList<IList<string>> features = d.AsFeatures();
					for (int k = 0; k < fSize; k++)
					{
						ICollection<string> cliqueFeatures = features[k];
						if (k < 2 && flags.removeBackgroundSingletonFeatures)
						{
							string ans = doc[j].Get(typeof(CoreAnnotations.AnswerAnnotation));
							bool background = ans.Equals(flags.backgroundSymbol);
							if (k == 1 && j > 0 && background)
							{
								ans = doc[j - 1].Get(typeof(CoreAnnotations.AnswerAnnotation));
								background = ans.Equals(flags.backgroundSymbol);
							}
							if (background)
							{
								foreach (string f in cliqueFeatures)
								{
									if (useFeatureCountThresh)
									{
										if (!featureCountIndices[k].Contains(f))
										{
											if (seenBackgroundFeatures[k].Contains(f))
											{
												seenBackgroundFeatures[k].Remove(f);
												featureCountIndices[k][f] = 1;
											}
											else
											{
												seenBackgroundFeatures[k].Add(f);
											}
										}
									}
									else
									{
										if (!featureIndices[k].Contains(f))
										{
											if (seenBackgroundFeatures[k].Contains(f))
											{
												seenBackgroundFeatures[k].Remove(f);
												featureIndices[k].Add(f);
											}
											else
											{
												seenBackgroundFeatures[k].Add(f);
											}
										}
									}
								}
							}
							else
							{
								seenBackgroundFeatures[k].RemoveAll(cliqueFeatures);
								if (useFeatureCountThresh)
								{
									IDictionary<string, int> fCountIndex = featureCountIndices[k];
									foreach (string f in cliqueFeatures)
									{
										if (fCountIndex.Contains(f))
										{
											fCountIndex[f] = fCountIndex[f] + 1;
										}
										else
										{
											fCountIndex[f] = 1;
										}
									}
								}
								else
								{
									Sharpen.Collections.AddAll(featureIndices[k], cliqueFeatures);
								}
							}
						}
						else
						{
							if (useFeatureCountThresh)
							{
								IDictionary<string, int> fCountIndex = featureCountIndices[k];
								foreach (string f in cliqueFeatures)
								{
									if (fCountIndex.Contains(f))
									{
										fCountIndex[f] = fCountIndex[f] + 1;
									}
									else
									{
										fCountIndex[f] = 1;
									}
								}
							}
							else
							{
								Sharpen.Collections.AddAll(featureIndices[k], cliqueFeatures);
							}
						}
					}
				}
				if (flags.useReverse)
				{
					Java.Util.Collections.Reverse(doc);
				}
			}
			if (useFeatureCountThresh)
			{
				int numFeatures = 0;
				for (int i_1 = 0; i_1 < windowSize; i_1++)
				{
					numFeatures += featureCountIndices[i_1].Count;
				}
				log.Info("Before feature count thresholding, numFeatures = " + numFeatures);
				for (int i_3 = 0; i_3 < windowSize; i_3++)
				{
					for (IEnumerator<KeyValuePair<string, int>> it = featureCountIndices[i_3].GetEnumerator(); it.MoveNext(); )
					{
						KeyValuePair<string, int> entry = it.Current;
						if (entry.Value < flags.featureCountThresh)
						{
							it.Remove();
						}
					}
					Sharpen.Collections.AddAll(featureIndices[i_3], featureCountIndices[i_3].Keys);
					featureCountIndices[i_3] = null;
				}
			}
			int numFeatures_1 = 0;
			for (int i_4 = 0; i_4 < windowSize; i_4++)
			{
				numFeatures_1 += featureIndices[i_4].Count;
			}
			log.Info("numFeatures = " + numFeatures_1);
			featureIndex = new HashIndex<string>();
			map = new int[numFeatures_1];
			if (flags.groupByFeatureTemplate)
			{
				templateGroupIndex = new HashIndex<string>();
				featureIndexToTemplateIndex = new Dictionary<int, int>();
			}
			for (int i_5 = 0; i_5 < windowSize; i_5++)
			{
				IIndex<int> featureIndexMap = new HashIndex<int>();
				featureIndex.AddAll(featureIndices[i_5]);
				foreach (string str in featureIndices[i_5])
				{
					int index = featureIndex.IndexOf(str);
					map[index] = i_5;
					featureIndexMap.Add(index);
					// grouping features by template
					if (flags.groupByFeatureTemplate)
					{
						Matcher m = suffixPatt.Matcher(str);
						string groupSuffix = "NoTemplate";
						if (m.Matches())
						{
							groupSuffix = m.Group(1);
						}
						groupSuffix += "-c:" + i_5;
						int groupIndex = templateGroupIndex.AddToIndex(groupSuffix);
						featureIndexToTemplateIndex[index] = groupIndex;
					}
				}
				// todo [cdm 2014]: Talk to Mengqiu about this; it seems like it only supports first order CRF
				if (i_5 == 0)
				{
					nodeFeatureIndicesMap = featureIndexMap;
				}
				else
				{
					// log.info("setting nodeFeatureIndicesMap, size="+nodeFeatureIndicesMap.size());
					edgeFeatureIndicesMap = featureIndexMap;
				}
			}
			// log.info("setting edgeFeatureIndicesMap, size="+edgeFeatureIndicesMap.size());
			if (flags.numOfFeatureSlices > 0)
			{
				log.Info("Taking " + flags.numOfFeatureSlices + " out of " + flags.totalFeatureSlice + " slices of node features for training");
				PruneNodeFeatureIndices(flags.totalFeatureSlice, flags.numOfFeatureSlices);
			}
			if (flags.useObservedSequencesOnly)
			{
				for (int i_1 = 0; i_1 < liSize; i_1++)
				{
					CRFLabel label = labelIndex.Get(i_1);
					for (int j = windowSize - 2; j >= 0; j--)
					{
						label = label.GetOneSmallerLabel();
						labelIndices[j].Add(label);
					}
				}
			}
			else
			{
				for (int i_1 = 0; i_1 < labelIndices.Count; i_1++)
				{
					labelIndices.Set(i_1, AllLabels(i_1 + 1, classIndex));
				}
			}
			if (labelDictionary != null)
			{
				labelDictionary.Lock(flags.labelDictionaryCutoff, classIndex);
			}
		}

		protected internal static IIndex<CRFLabel> AllLabels(int window, IIndex<string> classIndex)
		{
			int[] label = new int[window];
			// cdm 2005: array initialization isn't necessary: JLS (3rd ed.) 4.12.5
			// Arrays.fill(label, 0);
			int numClasses = classIndex.Size();
			IIndex<CRFLabel> labelIndex = new HashIndex<CRFLabel>();
			while (true)
			{
				CRFLabel l = new CRFLabel(label);
				labelIndex.Add(l);
				int[] label1 = new int[window];
				System.Array.Copy(label, 0, label1, 0, label.Length);
				label = label1;
				for (int j = 0; j < label.Length; j++)
				{
					label[j]++;
					if (label[j] >= numClasses)
					{
						label[j] = 0;
						if (j == label.Length - 1)
						{
							goto OUTER_break;
						}
					}
					else
					{
						break;
					}
				}
OUTER_continue: ;
			}
OUTER_break: ;
			return labelIndex;
		}

		/// <summary>
		/// Makes a CRFDatum by producing features and a label from input data at a
		/// specific position, using the provided factory.
		/// </summary>
		/// <param name="info">The input data. Particular feature factories might look for arbitrary keys in the IN items.</param>
		/// <param name="loc">The position to build a datum at</param>
		/// <param name="featureFactories">The FeatureFactories to use to extract features</param>
		/// <returns>The constructed CRFDatum</returns>
		public virtual CRFDatum<IList<string>, CRFLabel> MakeDatum(IList<IN> info, int loc, IList<FeatureFactory<IN>> featureFactories)
		{
			// pad.set(CoreAnnotations.AnswerAnnotation.class, flags.backgroundSymbol); // cdm: isn't this unnecessary, as this is how it's initialized in AbstractSequenceClassifier.reinit?
			PaddedList<IN> pInfo = new PaddedList<IN>(info, pad);
			List<IList<string>> features = new List<IList<string>>();
			IList<double[]> featureVals = new List<double[]>();
			// for (int i = 0; i < windowSize; i++) {
			// List featuresC = new ArrayList();
			// for (int j = 0; j < FeatureFactory.win[i].length; j++) {
			// featuresC.addAll(featureFactory.features(info, loc,
			// FeatureFactory.win[i][j]));
			// }
			// features.add(featuresC);
			// }
			// todo [cdm Aug 2012]: Since getCliques returns all cliques within its bounds, can't the for loop here be eliminated? But my first attempt to removed failed to produce identical results....
			ICollection<Clique> done = Generics.NewHashSet();
			for (int i = 0; i < windowSize; i++)
			{
				IList<string> featuresC = new List<string>();
				IList<Clique> windowCliques = FeatureFactory.GetCliques(i, 0);
				windowCliques.RemoveAll(done);
				Sharpen.Collections.AddAll(done, windowCliques);
				double[] featureValArr = null;
				if (flags.useEmbedding && i == 0)
				{
					// only activated for node features
					featureValArr = MakeDatumUsingEmbedding(info, loc, featureFactories, pInfo, featuresC, windowCliques);
				}
				else
				{
					foreach (Clique c in windowCliques)
					{
						foreach (FeatureFactory<IN> featureFactory in featureFactories)
						{
							Sharpen.Collections.AddAll(featuresC, featureFactory.GetCliqueFeatures(pInfo, loc, c));
						}
					}
				}
				//todo useless copy because of typing reasons
				features.Add(featuresC);
				featureVals.Add(featureValArr);
			}
			int[] labels = new int[windowSize];
			for (int i_1 = 0; i_1 < windowSize; i_1++)
			{
				string answer = pInfo[loc + i_1 - windowSize + 1].Get(typeof(CoreAnnotations.AnswerAnnotation));
				labels[i_1] = classIndex.IndexOf(answer);
			}
			PrintFeatureLists(pInfo[loc], features);
			CRFDatum<IList<string>, CRFLabel> d = new CRFDatum<IList<string>, CRFLabel>(features, new CRFLabel(labels), featureVals);
			// log.info(d);
			return d;
		}

		private double[] MakeDatumUsingEmbedding(IList<IN> info, int loc, IList<FeatureFactory<IN>> featureFactories, PaddedList<IN> pInfo, IList<string> featuresC, IList<Clique> windowCliques)
		{
			double[] featureValArr;
			IList<double[]> embeddingList = new List<double[]>();
			int concatEmbeddingLen = 0;
			string currentWord = null;
			for (int currLoc = loc - 2; currLoc <= loc + 2; currLoc++)
			{
				double[] embedding;
				// Initialized in cases below // = null;
				if (currLoc >= 0 && currLoc < info.Count)
				{
					currentWord = info[loc].Get(typeof(CoreAnnotations.TextAnnotation));
					string word = currentWord.ToLower();
					word = word.ReplaceAll("(-)?\\d+(\\.\\d*)?", "0");
					if (embeddings.Contains(word))
					{
						embedding = embeddings[word];
					}
					else
					{
						embedding = embeddings["UNKNOWN"];
					}
				}
				else
				{
					embedding = embeddings["PADDING"];
				}
				for (int e = 0; e < embedding.Length; e++)
				{
					featuresC.Add("EMBEDDING-(" + (currLoc - loc) + ")-" + e);
				}
				if (flags.addCapitalFeatures)
				{
					int numOfCapitalFeatures = 4;
					double[] newEmbedding = new double[embedding.Length + numOfCapitalFeatures];
					int currLen = embedding.Length;
					System.Array.Copy(embedding, 0, newEmbedding, 0, currLen);
					for (int e_1 = 0; e_1 < numOfCapitalFeatures; e_1++)
					{
						featuresC.Add("CAPITAL-(" + (currLoc - loc) + ")-" + e_1);
					}
					if (currLoc >= 0 && currLoc < info.Count)
					{
						// skip PADDING
						// check if word is all caps
						if (currentWord.ToUpper().Equals(currentWord))
						{
							newEmbedding[currLen] = 1;
						}
						else
						{
							currLen += 1;
							// check if word is all lower
							if (currentWord.ToLower().Equals(currentWord))
							{
								newEmbedding[currLen] = 1;
							}
							else
							{
								currLen += 1;
								// check first letter cap
								if (char.IsUpperCase(currentWord[0]))
								{
									newEmbedding[currLen] = 1;
								}
								else
								{
									currLen += 1;
									// check if at least one non-initial letter is cap
									string remainder = Sharpen.Runtime.Substring(currentWord, 1);
									if (!remainder.ToLower().Equals(remainder))
									{
										newEmbedding[currLen] = 1;
									}
								}
							}
						}
					}
					embedding = newEmbedding;
				}
				embeddingList.Add(embedding);
				concatEmbeddingLen += embedding.Length;
			}
			double[] concatEmbedding = new double[concatEmbeddingLen];
			int currPos = 0;
			foreach (double[] em in embeddingList)
			{
				System.Array.Copy(em, 0, concatEmbedding, currPos, em.Length);
				currPos += em.Length;
			}
			if (flags.prependEmbedding)
			{
				int additionalFeatureCount = 0;
				foreach (Clique c in windowCliques)
				{
					foreach (FeatureFactory<IN> featureFactory in featureFactories)
					{
						ICollection<string> fCol = featureFactory.GetCliqueFeatures(pInfo, loc, c);
						//todo useless copy because of typing reasons
						Sharpen.Collections.AddAll(featuresC, fCol);
						additionalFeatureCount += fCol.Count;
					}
				}
				featureValArr = new double[concatEmbedding.Length + additionalFeatureCount];
				System.Array.Copy(concatEmbedding, 0, featureValArr, 0, concatEmbedding.Length);
				Arrays.Fill(featureValArr, concatEmbedding.Length, featureValArr.Length, 1.0);
			}
			else
			{
				featureValArr = concatEmbedding;
			}
			if (flags.addBiasToEmbedding)
			{
				featuresC.Add("BIAS-FEATURE");
				double[] newFeatureValArr = new double[featureValArr.Length + 1];
				System.Array.Copy(featureValArr, 0, newFeatureValArr, 0, featureValArr.Length);
				newFeatureValArr[newFeatureValArr.Length - 1] = 1;
				featureValArr = newFeatureValArr;
			}
			return featureValArr;
		}

		public override void DumpFeatures(ICollection<IList<IN>> docs)
		{
			if (flags.exportFeatures != null)
			{
				Timing timer = new Timing();
				CRFFeatureExporter<IN> featureExporter = new CRFFeatureExporter<IN>(this);
				featureExporter.PrintFeatures(flags.exportFeatures, docs);
				long elapsedMs = timer.Stop();
				log.Info("Time to export features: " + Timing.ToSecondsString(elapsedMs) + " seconds");
			}
		}

		public override IList<IN> Classify(IList<IN> document)
		{
			if (flags.doGibbs)
			{
				try
				{
					return ClassifyGibbs(document);
				}
				catch (Exception e)
				{
					throw new Exception("Error running testGibbs inference!", e);
				}
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(flags.crfType, "maxent"))
				{
					return ClassifyMaxEnt(document);
				}
				else
				{
					throw new Exception("Unsupported inference type: " + flags.crfType);
				}
			}
		}

		private IList<IN> Classify(IList<IN> document, Triple<int[][][], int[], double[][][]> documentDataAndLabels)
		{
			if (flags.doGibbs)
			{
				try
				{
					return ClassifyGibbs(document, documentDataAndLabels);
				}
				catch (Exception e)
				{
					throw new Exception("Error running testGibbs inference!", e);
				}
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(flags.crfType, "maxent"))
				{
					return ClassifyMaxEnt(document, documentDataAndLabels);
				}
				else
				{
					throw new Exception("Unsupported inference type: " + flags.crfType);
				}
			}
		}

		/// <summary>This method is supposed to be used by CRFClassifierEvaluator only, should not have global visibility.</summary>
		/// <remarks>
		/// This method is supposed to be used by CRFClassifierEvaluator only, should not have global visibility.
		/// The generic
		/// <c>classifyAndWriteAnswers</c>
		/// omits the second argument
		/// <paramref name="documentDataAndLabels"/>
		/// .
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		internal virtual void ClassifyAndWriteAnswers(ICollection<IList<IN>> documents, IList<Triple<int[][][], int[], double[][][]>> documentDataAndLabels, PrintWriter printWriter, IDocumentReaderAndWriter<IN> readerAndWriter)
		{
			Timing timer = new Timing();
			ICounter<string> entityTP = new ClassicCounter<string>();
			ICounter<string> entityFP = new ClassicCounter<string>();
			ICounter<string> entityFN = new ClassicCounter<string>();
			bool resultsCounted = true;
			int numWords = 0;
			int numDocs = 0;
			foreach (IList<IN> doc in documents)
			{
				Classify(doc, documentDataAndLabels[numDocs]);
				numWords += doc.Count;
				WriteAnswers(doc, printWriter, readerAndWriter);
				resultsCounted = resultsCounted && CountResults(doc, entityTP, entityFP, entityFN);
				numDocs++;
			}
			long millis = timer.Stop();
			double wordspersec = numWords / (((double)millis) / 1000);
			NumberFormat nf = new DecimalFormat("0.00");
			// easier way!
			if (!flags.suppressTestDebug)
			{
				log.Info(StringUtils.GetShortClassName(this) + " tagged " + numWords + " words in " + numDocs + " documents at " + nf.Format(wordspersec) + " words per second.");
			}
			if (resultsCounted && !flags.suppressTestDebug)
			{
				PrintResults(entityTP, entityFP, entityFN);
			}
		}

		public override ISequenceModel GetSequenceModel(IList<IN> doc)
		{
			Triple<int[][][], int[], double[][][]> p = DocumentToDataAndLabels(doc);
			return GetSequenceModel(p, doc);
		}

		private ISequenceModel GetSequenceModel(Triple<int[][][], int[], double[][][]> documentDataAndLabels, IList<IN> document)
		{
			return labelDictionary == null ? new TestSequenceModel(GetCliqueTree(documentDataAndLabels)) : new TestSequenceModel(GetCliqueTree(documentDataAndLabels), labelDictionary, document);
		}

		protected internal virtual ICliquePotentialFunction GetCliquePotentialFunctionForTest()
		{
			if (cliquePotentialFunction == null)
			{
				cliquePotentialFunction = new LinearCliquePotentialFunction(weights);
			}
			return cliquePotentialFunction;
		}

		public virtual void UpdateWeightsForTest(double[] x)
		{
			cliquePotentialFunction = cliquePotentialFunctionHelper.GetCliquePotentialFunction(x);
		}

		/// <summary>
		/// Do standard sequence inference, using either Viterbi or Beam inference
		/// depending on the value of
		/// <c>flags.inferenceType</c>
		/// .
		/// </summary>
		/// <param name="document">
		/// Document to classify. Classification happens in place.
		/// This document is modified.
		/// </param>
		/// <returns>The classified document</returns>
		public virtual IList<IN> ClassifyMaxEnt(IList<IN> document)
		{
			if (document.IsEmpty())
			{
				return document;
			}
			ISequenceModel model = GetSequenceModel(document);
			return ClassifyMaxEnt(document, model);
		}

		private IList<IN> ClassifyMaxEnt(IList<IN> document, Triple<int[][][], int[], double[][][]> documentDataAndLabels)
		{
			if (document.IsEmpty())
			{
				return document;
			}
			ISequenceModel model = GetSequenceModel(documentDataAndLabels, document);
			return ClassifyMaxEnt(document, model);
		}

		private IList<IN> ClassifyMaxEnt(IList<IN> document, ISequenceModel model)
		{
			if (document.IsEmpty())
			{
				return document;
			}
			if (flags.inferenceType == null)
			{
				flags.inferenceType = "Viterbi";
			}
			IBestSequenceFinder tagInference;
			if (Sharpen.Runtime.EqualsIgnoreCase(flags.inferenceType, "Viterbi"))
			{
				tagInference = new ExactBestSequenceFinder();
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(flags.inferenceType, "Beam"))
				{
					tagInference = new BeamBestSequenceFinder(flags.beamSize);
				}
				else
				{
					throw new Exception("Unknown inference type: " + flags.inferenceType + ". Your options are Viterbi|Beam.");
				}
			}
			int[] bestSequence = tagInference.BestSequence(model);
			if (flags.useReverse)
			{
				Java.Util.Collections.Reverse(document);
			}
			for (int j = 0; j < docSize; j++)
			{
				IN wi = document[j];
				string guess = classIndex.Get(bestSequence[j + windowSize - 1]);
				wi.Set(typeof(CoreAnnotations.AnswerAnnotation), guess);
			}
			if (flags.useReverse)
			{
				Java.Util.Collections.Reverse(document);
			}
			return document;
		}

		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.Security.SecurityException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.ArgumentException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		public virtual IList<IN> ClassifyGibbs(IList<IN> document)
		{
			Triple<int[][][], int[], double[][][]> p = DocumentToDataAndLabels(document);
			return ClassifyGibbs(document, p);
		}

		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.Security.SecurityException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.ArgumentException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		public virtual IList<IN> ClassifyGibbs(IList<IN> document, Triple<int[][][], int[], double[][][]> documentDataAndLabels)
		{
			// log.info("Testing using Gibbs sampling.");
			IList<IN> newDocument = document;
			// reversed if necessary
			if (flags.useReverse)
			{
				Java.Util.Collections.Reverse(document);
				newDocument = new List<IN>(document);
				Java.Util.Collections.Reverse(document);
			}
			CRFCliqueTree<ICharSequence> cliqueTree = GetCliqueTree(documentDataAndLabels);
			IPriorModelFactory<IN> pmf = (IPriorModelFactory<IN>)System.Activator.CreateInstance(Sharpen.Runtime.GetType(flags.priorModelFactory));
			IListeningSequenceModel prior = pmf.GetInstance(flags.backgroundSymbol, classIndex, tagIndex, newDocument, entityMatrices, flags);
			if (!flags.useUniformPrior)
			{
				throw new Exception("no prior specified");
			}
			ISequenceModel model = new FactoredSequenceModel(cliqueTree, prior);
			ISequenceListener listener = new FactoredSequenceListener(cliqueTree, prior);
			SequenceGibbsSampler sampler = new SequenceGibbsSampler(0, 0, listener);
			int[] sequence = new int[cliqueTree.Length()];
			if (flags.initViterbi)
			{
				TestSequenceModel testSequenceModel = new TestSequenceModel(cliqueTree);
				ExactBestSequenceFinder tagInference = new ExactBestSequenceFinder();
				int[] bestSequence = tagInference.BestSequence(testSequenceModel);
				System.Array.Copy(bestSequence, windowSize - 1, sequence, 0, sequence.Length);
			}
			else
			{
				int[] initialSequence = SequenceGibbsSampler.GetRandomSequence(model);
				System.Array.Copy(initialSequence, 0, sequence, 0, sequence.Length);
			}
			sampler.verbose = 0;
			if (Sharpen.Runtime.EqualsIgnoreCase(flags.annealingType, "linear"))
			{
				sequence = sampler.FindBestUsingAnnealing(model, CoolingSchedule.GetLinearSchedule(1.0, flags.numSamples), sequence);
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(flags.annealingType, "exp") || Sharpen.Runtime.EqualsIgnoreCase(flags.annealingType, "exponential"))
				{
					sequence = sampler.FindBestUsingAnnealing(model, CoolingSchedule.GetExponentialSchedule(1.0, flags.annealingRate, flags.numSamples), sequence);
				}
				else
				{
					throw new Exception("No annealing type specified");
				}
			}
			if (flags.useReverse)
			{
				Java.Util.Collections.Reverse(document);
			}
			for (int j = 0; j < dsize; j++)
			{
				IN wi = document[j];
				if (wi == null)
				{
					throw new Exception(string.Empty);
				}
				if (classIndex == null)
				{
					throw new Exception(string.Empty);
				}
				wi.Set(typeof(CoreAnnotations.AnswerAnnotation), classIndex.Get(sequence[j]));
			}
			if (flags.useReverse)
			{
				Java.Util.Collections.Reverse(document);
			}
			return document;
		}

		/// <summary>
		/// Takes a
		/// <see cref="System.Collections.IList{E}"/>
		/// of something that extends
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// and prints
		/// the likelihood of each possible label at each point.
		/// </summary>
		/// <param name="document">
		/// A
		/// <see cref="System.Collections.IList{E}"/>
		/// of something that extends CoreMap.
		/// </param>
		/// <returns>If verboseMode is set, a Pair of Counters recording classification decisions, else null.</returns>
		public override Triple<ICounter<int>, ICounter<int>, TwoDimensionalCounter<int, string>> PrintProbsDocument(IList<IN> document)
		{
			// TODO: Probably this would really be better with 11 bins, with edge ones from 0-0.5 and 0.95-1.0, a bit like 11-point ave precision
			int numBins = 10;
			bool verbose = flags.verboseMode;
			Triple<int[][][], int[], double[][][]> p = DocumentToDataAndLabels(document);
			CRFCliqueTree<string> cliqueTree = GetCliqueTree(p);
			ICounter<int> calibration = new ClassicCounter<int>();
			ICounter<int> correctByBin = new ClassicCounter<int>();
			TwoDimensionalCounter<int, string> calibratedTokens = new TwoDimensionalCounter<int, string>();
			// for (int i = 0; i < factorTables.length; i++) {
			for (int i = 0; i < cliqueTree.Length(); i++)
			{
				IN wi = document[i];
				string token = wi.Get(typeof(CoreAnnotations.TextAnnotation));
				string goldAnswer = wi.Get(typeof(CoreAnnotations.GoldAnswerAnnotation));
				System.Console.Out.Write(token);
				System.Console.Out.Write('\t');
				System.Console.Out.Write(goldAnswer);
				double maxProb = double.NegativeInfinity;
				string bestClass = string.Empty;
				foreach (string label in classIndex)
				{
					int index = classIndex.IndexOf(label);
					// double prob = Math.pow(Math.E, factorTables[i].logProbEnd(index));
					double prob = cliqueTree.Prob(i, index);
					if (prob > maxProb)
					{
						bestClass = label;
					}
					System.Console.Out.Write('\t');
					System.Console.Out.Write(label);
					System.Console.Out.Write('=');
					System.Console.Out.Write(prob);
					if (verbose)
					{
						int binnedProb = (int)(prob * numBins);
						if (binnedProb > (numBins - 1))
						{
							binnedProb = numBins - 1;
						}
						calibration.IncrementCount(binnedProb);
						if (label.Equals(goldAnswer))
						{
							if (bestClass.Equals(goldAnswer))
							{
								correctByBin.IncrementCount(binnedProb);
							}
							if (!label.Equals(flags.backgroundSymbol))
							{
								calibratedTokens.IncrementCount(binnedProb, token);
							}
						}
					}
				}
				System.Console.Out.WriteLine();
			}
			if (verbose)
			{
				return new Triple<ICounter<int>, ICounter<int>, TwoDimensionalCounter<int, string>>(calibration, correctByBin, calibratedTokens);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Takes the file, reads it in, and prints out the likelihood of each possible
		/// label at each point.
		/// </summary>
		/// <remarks>
		/// Takes the file, reads it in, and prints out the likelihood of each possible
		/// label at each point. This gives a simple way to examine the probability
		/// distributions of the CRF. See
		/// <c>getCliqueTrees()</c>
		/// for more.
		/// </remarks>
		/// <param name="filename">The path to the specified file</param>
		public virtual void PrintFirstOrderProbs(string filename, IDocumentReaderAndWriter<IN> readerAndWriter)
		{
			// only for the OCR data does this matter
			// flags.ocrTrain = false;
			ObjectBank<IList<IN>> docs = MakeObjectBankFromFile(filename, readerAndWriter);
			PrintFirstOrderProbsDocuments(docs);
		}

		/// <summary>
		/// Takes a
		/// <see cref="System.Collections.IList{E}"/>
		/// of documents and prints the likelihood of each
		/// possible label at each point.
		/// </summary>
		/// <param name="documents">
		/// A
		/// <see cref="System.Collections.IList{E}"/>
		/// of
		/// <see cref="System.Collections.IList{E}"/>
		/// of INs.
		/// </param>
		public virtual void PrintFirstOrderProbsDocuments(ObjectBank<IList<IN>> documents)
		{
			foreach (IList<IN> doc in documents)
			{
				PrintFirstOrderProbsDocument(doc);
				System.Console.Out.WriteLine();
			}
		}

		/// <summary>Takes the file, reads it in, and prints out the factor table at each position.</summary>
		/// <param name="filename">The path to the specified file</param>
		public virtual void PrintFactorTable(string filename, IDocumentReaderAndWriter<IN> readerAndWriter)
		{
			// only for the OCR data does this matter
			// flags.ocrTrain = false;
			ObjectBank<IList<IN>> docs = MakeObjectBankFromFile(filename, readerAndWriter);
			PrintFactorTableDocuments(docs);
		}

		/// <summary>
		/// Takes a
		/// <see cref="System.Collections.IList{E}"/>
		/// of documents and prints the factor table
		/// at each point.
		/// </summary>
		/// <param name="documents">
		/// A
		/// <see cref="System.Collections.IList{E}"/>
		/// of
		/// <see cref="System.Collections.IList{E}"/>
		/// of INs.
		/// </param>
		public virtual void PrintFactorTableDocuments(ObjectBank<IList<IN>> documents)
		{
			foreach (IList<IN> doc in documents)
			{
				PrintFactorTableDocument(doc);
				System.Console.Out.WriteLine();
			}
		}

		/// <summary>
		/// Want to make arbitrary probability queries? Then this is the method for
		/// you.
		/// </summary>
		/// <remarks>
		/// Want to make arbitrary probability queries? Then this is the method for
		/// you. Given the filename, it reads it in and breaks it into documents, and
		/// then makes a CRFCliqueTree for each document. you can then ask the clique
		/// tree for marginals and conditional probabilities of almost anything you want.
		/// </remarks>
		public virtual IList<CRFCliqueTree<string>> GetCliqueTrees(string filename, IDocumentReaderAndWriter<IN> readerAndWriter)
		{
			// only for the OCR data does this matter
			// flags.ocrTrain = false;
			IList<CRFCliqueTree<string>> cts = new List<CRFCliqueTree<string>>();
			ObjectBank<IList<IN>> docs = MakeObjectBankFromFile(filename, readerAndWriter);
			foreach (IList<IN> doc in docs)
			{
				cts.Add(GetCliqueTree(doc));
			}
			return cts;
		}

		public virtual CRFCliqueTree<string> GetCliqueTree(Triple<int[][][], int[], double[][][]> p)
		{
			int[][][] data = p.First();
			double[][][] featureVal = p.Third();
			return CRFCliqueTree.GetCalibratedCliqueTree(data, labelIndices, classIndex.Size(), classIndex, flags.backgroundSymbol, GetCliquePotentialFunctionForTest(), featureVal);
		}

		public virtual CRFCliqueTree<string> GetCliqueTree(IList<IN> document)
		{
			Triple<int[][][], int[], double[][][]> p = DocumentToDataAndLabels(document);
			return GetCliqueTree(p);
		}

		/// <summary>
		/// Takes a
		/// <see cref="System.Collections.IList{E}"/>
		/// of something that extends
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// and prints
		/// the factor table at each point.
		/// </summary>
		/// <param name="document">
		/// A
		/// <see cref="System.Collections.IList{E}"/>
		/// of something that extends
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// .
		/// </param>
		public virtual void PrintFactorTableDocument(IList<IN> document)
		{
			CRFCliqueTree<string> cliqueTree = GetCliqueTree(document);
			FactorTable[] factorTables = cliqueTree.GetFactorTables();
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < factorTables.Length; i++)
			{
				IN wi = document[i];
				sb.Append(wi.Get(typeof(CoreAnnotations.TextAnnotation)));
				sb.Append('\t');
				FactorTable table = factorTables[i];
				for (int j = 0; j < table.Size(); j++)
				{
					int[] arr = table.ToArray(j);
					sb.Append(classIndex.Get(arr[0]));
					sb.Append(':');
					sb.Append(classIndex.Get(arr[1]));
					sb.Append(':');
					sb.Append(cliqueTree.LogProb(i, arr));
					sb.Append(' ');
				}
				sb.Append('\n');
			}
			System.Console.Out.Write(sb);
		}

		/// <summary>
		/// Takes a
		/// <see cref="System.Collections.IList{E}"/>
		/// of something that extends
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// and prints
		/// the likelihood of each possible label at each point.
		/// </summary>
		/// <param name="document">
		/// A
		/// <see cref="System.Collections.IList{E}"/>
		/// of something that extends
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// .
		/// </param>
		public virtual void PrintFirstOrderProbsDocument(IList<IN> document)
		{
			CRFCliqueTree<string> cliqueTree = GetCliqueTree(document);
			// for (int i = 0; i < factorTables.length; i++) {
			for (int i = 0; i < cliqueTree.Length(); i++)
			{
				IN wi = document[i];
				System.Console.Out.Write(wi.Get(typeof(CoreAnnotations.TextAnnotation)) + '\t');
				for (IEnumerator<string> iter = classIndex.GetEnumerator(); iter.MoveNext(); )
				{
					string label = iter.Current;
					int index = classIndex.IndexOf(label);
					if (i == 0)
					{
						// double prob = Math.pow(Math.E, factorTables[i].logProbEnd(index));
						double prob = cliqueTree.Prob(i, index);
						System.Console.Out.Write(label + '=' + prob);
						if (iter.MoveNext())
						{
							System.Console.Out.Write("\t");
						}
						else
						{
							System.Console.Out.Write("\n");
						}
					}
					else
					{
						for (IEnumerator<string> iter1 = classIndex.GetEnumerator(); iter1.MoveNext(); )
						{
							string label1 = iter1.Current;
							int index1 = classIndex.IndexOf(label1);
							// double prob = Math.pow(Math.E, factorTables[i].logProbEnd(new
							// int[]{index1, index}));
							double prob = cliqueTree.Prob(i, new int[] { index1, index });
							System.Console.Out.Write(label1 + '_' + label + '=' + prob);
							if (iter.MoveNext() || iter1.MoveNext())
							{
								System.Console.Out.Write("\t");
							}
							else
							{
								System.Console.Out.Write("\n");
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Load auxiliary data to be used in constructing features and labels
		/// Intended to be overridden by subclasses
		/// </summary>
		protected internal virtual ICollection<IList<IN>> LoadAuxiliaryData(ICollection<IList<IN>> docs, IDocumentReaderAndWriter<IN> readerAndWriter)
		{
			return docs;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override void Train(ICollection<IList<IN>> objectBankWrapper, IDocumentReaderAndWriter<IN> readerAndWriter)
		{
			Timing timer = new Timing();
			ICollection<IList<IN>> docs = new List<IList<IN>>();
			foreach (IList<IN> doc in objectBankWrapper)
			{
				docs.Add(doc);
			}
			if (flags.numOfSlices > 0)
			{
				log.Info("Taking " + flags.numOfSlices + " out of " + flags.totalDataSlice + " slices of data for training");
				IList<IList<IN>> docsToShuffle = new List<IList<IN>>();
				foreach (IList<IN> doc_1 in docs)
				{
					docsToShuffle.Add(doc_1);
				}
				Java.Util.Collections.Shuffle(docsToShuffle, random);
				int cutOff = (int)(docsToShuffle.Count / (flags.totalDataSlice + 0.0) * flags.numOfSlices);
				docs = docsToShuffle.SubList(0, cutOff);
			}
			ICollection<IList<IN>> totalDocs = LoadAuxiliaryData(docs, readerAndWriter);
			MakeAnswerArraysAndTagIndex(totalDocs);
			long elapsedMs = timer.Stop();
			log.Info("Time to convert docs to feature indices: " + Timing.ToSecondsString(elapsedMs) + " seconds");
			if (flags.serializeClassIndexTo != null)
			{
				timer.Start();
				SerializeClassIndex(flags.serializeClassIndexTo);
				elapsedMs = timer.Stop();
				log.Info("Time to export class index : " + Timing.ToSecondsString(elapsedMs) + " seconds");
			}
			if (flags.exportFeatures != null)
			{
				DumpFeatures(docs);
			}
			for (int i = 0; i <= flags.numTimesPruneFeatures; i++)
			{
				timer.Start();
				Triple<int[][][][], int[][], double[][][][]> dataAndLabelsAndFeatureVals = DocumentsToDataAndLabels(docs);
				elapsedMs = timer.Stop();
				log.Info("Time to convert docs to data/labels: " + Timing.ToSecondsString(elapsedMs) + " seconds");
				IEvaluator[] evaluators = null;
				if (flags.evaluateIters > 0 || flags.terminateOnEvalImprovement)
				{
					IList<IEvaluator> evaluatorList = new List<IEvaluator>();
					if (flags.useMemoryEvaluator)
					{
						evaluatorList.Add(new MemoryEvaluator());
					}
					if (flags.evaluateTrain)
					{
						CRFClassifierEvaluator<IN> crfEvaluator = new CRFClassifierEvaluator<IN>("Train set", this);
						IList<Triple<int[][][], int[], double[][][]>> trainDataAndLabels = new List<Triple<int[][][], int[], double[][][]>>();
						int[][][][] data = dataAndLabelsAndFeatureVals.First();
						int[][] labels = dataAndLabelsAndFeatureVals.Second();
						double[][][][] featureVal = dataAndLabelsAndFeatureVals.Third();
						for (int j = 0; j < data.Length; j++)
						{
							Triple<int[][][], int[], double[][][]> p = new Triple<int[][][], int[], double[][][]>(data[j], labels[j], featureVal[j]);
							trainDataAndLabels.Add(p);
						}
						crfEvaluator.SetTestData(docs, trainDataAndLabels);
						if (flags.evalCmd.Length > 0)
						{
							crfEvaluator.SetEvalCmd(flags.evalCmd);
						}
						evaluatorList.Add(crfEvaluator);
					}
					if (flags.testFile != null)
					{
						CRFClassifierEvaluator<IN> crfEvaluator = new CRFClassifierEvaluator<IN>("Test set (" + flags.testFile + ")", this);
						ObjectBank<IList<IN>> testObjBank = MakeObjectBankFromFile(flags.testFile, readerAndWriter);
						IList<IList<IN>> testDocs = new List<IList<IN>>(testObjBank);
						IList<Triple<int[][][], int[], double[][][]>> testDataAndLabels = DocumentsToDataAndLabelsList(testDocs);
						crfEvaluator.SetTestData(testDocs, testDataAndLabels);
						if (!flags.evalCmd.IsEmpty())
						{
							crfEvaluator.SetEvalCmd(flags.evalCmd);
						}
						evaluatorList.Add(crfEvaluator);
					}
					if (flags.testFiles != null)
					{
						string[] testFiles = flags.testFiles.Split(",");
						foreach (string testFile in testFiles)
						{
							CRFClassifierEvaluator<IN> crfEvaluator = new CRFClassifierEvaluator<IN>("Test set (" + testFile + ')', this);
							ObjectBank<IList<IN>> testObjBank = MakeObjectBankFromFile(testFile, readerAndWriter);
							IList<Triple<int[][][], int[], double[][][]>> testDataAndLabels = DocumentsToDataAndLabelsList(testObjBank);
							crfEvaluator.SetTestData(testObjBank, testDataAndLabels);
							if (!flags.evalCmd.IsEmpty())
							{
								crfEvaluator.SetEvalCmd(flags.evalCmd);
							}
							evaluatorList.Add(crfEvaluator);
						}
					}
					evaluators = new IEvaluator[evaluatorList.Count];
					Sharpen.Collections.ToArray(evaluatorList, evaluators);
				}
				if (flags.numTimesPruneFeatures == i)
				{
					docs = null;
				}
				// hopefully saves memory
				// save feature index to disk and read in later
				File featIndexFile = null;
				// CRFLogConditionalObjectiveFunction.featureIndex = featureIndex;
				// int numFeatures = featureIndex.size();
				if (flags.saveFeatureIndexToDisk)
				{
					try
					{
						log.Info("Writing feature index to temporary file.");
						featIndexFile = IOUtils.WriteObjectToTempFile(featureIndex, "featIndex" + i + ".tmp");
					}
					catch (IOException)
					{
						// featureIndex = null;
						throw new Exception("Could not open temporary feature index file for writing.");
					}
				}
				// first index is the number of the document
				// second index is position in the document also the index of the
				// clique/factor table
				// third index is the number of elements in the clique/window these
				// features are for (starting with last element)
				// fourth index is position of the feature in the array that holds them
				// element in data[i][j][k][m] is the index of the mth feature occurring
				// in position k of the jth clique of the ith document
				int[][][][] data_1 = dataAndLabelsAndFeatureVals.First();
				// first index is the number of the document
				// second index is the position in the document
				// element in labels[i][j] is the index of the correct label (if it
				// exists) at position j in document i
				int[][] labels_1 = dataAndLabelsAndFeatureVals.Second();
				double[][][][] featureVals = dataAndLabelsAndFeatureVals.Third();
				if (flags.loadProcessedData != null)
				{
					IList<IList<CRFDatum<ICollection<string>, string>>> processedData = LoadProcessedData(flags.loadProcessedData);
					if (processedData != null)
					{
						// enlarge the data and labels array
						int[][][][] allData = new int[data_1.Length + processedData.Count][][][];
						double[][][][] allFeatureVals = new double[featureVals.Length + processedData.Count][][][];
						int[][] allLabels = new int[labels_1.Length + processedData.Count][];
						System.Array.Copy(data_1, 0, allData, 0, data_1.Length);
						System.Array.Copy(labels_1, 0, allLabels, 0, labels_1.Length);
						System.Array.Copy(featureVals, 0, allFeatureVals, 0, featureVals.Length);
						// add to the data and labels array
						AddProcessedData(processedData, allData, allLabels, allFeatureVals, data_1.Length);
						data_1 = allData;
						labels_1 = allLabels;
						featureVals = allFeatureVals;
					}
				}
				double[] oneDimWeights = TrainWeights(data_1, labels_1, evaluators, i, featureVals);
				if (oneDimWeights != null)
				{
					this.weights = To2D(oneDimWeights, labelIndices, map);
				}
				// if (flags.useFloat) {
				//   oneDimWeights = trainWeightsUsingFloatCRF(data, labels, evaluators, i, featureVals);
				// } else if (flags.numLopExpert > 1) {
				//   oneDimWeights = trainWeightsUsingLopCRF(data, labels, evaluators, i, featureVals);
				// } else {
				//   oneDimWeights = trainWeightsUsingDoubleCRF(data, labels, evaluators, i, featureVals);
				// }
				// save feature index to disk and read in later
				if (flags.saveFeatureIndexToDisk)
				{
					try
					{
						log.Info("Reading temporary feature index file.");
						featureIndex = IOUtils.ReadObjectFromFile(featIndexFile);
					}
					catch (Exception)
					{
						throw new Exception("Could not open temporary feature index file for reading.");
					}
				}
				if (i != flags.numTimesPruneFeatures)
				{
					DropFeaturesBelowThreshold(flags.featureDiffThresh);
					log.Info("Removing features with weight below " + flags.featureDiffThresh + " and retraining...");
				}
			}
		}

		public static double[][] To2D(double[] weights, IList<IIndex<CRFLabel>> labelIndices, int[] map)
		{
			double[][] newWeights = new double[map.Length][];
			int index = 0;
			for (int i = 0; i < map.Length; i++)
			{
				newWeights[i] = new double[labelIndices[map[i]].Size()];
				System.Array.Copy(weights, index, newWeights[i], 0, labelIndices[map[i]].Size());
				index += labelIndices[map[i]].Size();
			}
			return newWeights;
		}

		protected internal virtual void PruneNodeFeatureIndices(int totalNumOfFeatureSlices, int numOfFeatureSlices)
		{
			int numOfNodeFeatures = nodeFeatureIndicesMap.Size();
			int beginIndex = 0;
			int endIndex = Math.Min((int)(numOfNodeFeatures / (totalNumOfFeatureSlices + 0.0) * numOfFeatureSlices), numOfNodeFeatures);
			IList<int> nodeFeatureOriginalIndices = nodeFeatureIndicesMap.ObjectsList();
			IList<int> edgeFeatureOriginalIndices = edgeFeatureIndicesMap.ObjectsList();
			IIndex<int> newNodeFeatureIndex = new HashIndex<int>();
			IIndex<int> newEdgeFeatureIndex = new HashIndex<int>();
			IIndex<string> newFeatureIndex = new HashIndex<string>();
			for (int i = beginIndex; i < endIndex; i++)
			{
				int oldIndex = nodeFeatureOriginalIndices[i];
				string f = featureIndex.Get(oldIndex);
				int index = newFeatureIndex.AddToIndex(f);
				newNodeFeatureIndex.Add(index);
			}
			foreach (int edgeFIndex in edgeFeatureOriginalIndices)
			{
				string f = featureIndex.Get(edgeFIndex);
				int index = newFeatureIndex.AddToIndex(f);
				newEdgeFeatureIndex.Add(index);
			}
			nodeFeatureIndicesMap = newNodeFeatureIndex;
			edgeFeatureIndicesMap = newEdgeFeatureIndex;
			int[] newMap = new int[newFeatureIndex.Size()];
			for (int i_1 = 0; i_1 < newMap.Length; i_1++)
			{
				int index = featureIndex.IndexOf(newFeatureIndex.Get(i_1));
				newMap[i_1] = map[index];
			}
			map = newMap;
			featureIndex = newFeatureIndex;
		}

		protected internal virtual CRFLogConditionalObjectiveFunction GetObjectiveFunction(int[][][][] data, int[][] labels)
		{
			return new CRFLogConditionalObjectiveFunction(data, labels, windowSize, classIndex, labelIndices, map, flags.priorType, flags.backgroundSymbol, flags.sigma, null, flags.multiThreadGrad);
		}

		protected internal virtual double[] TrainWeights(int[][][][] data, int[][] labels, IEvaluator[] evaluators, int pruneFeatureItr, double[][][][] featureVals)
		{
			CRFLogConditionalObjectiveFunction func = GetObjectiveFunction(data, labels);
			cliquePotentialFunctionHelper = func;
			// create feature grouping
			// todo [cdm 2016]: Use a CollectionValuedMap
			IDictionary<string, ICollection<int>> featureSets = null;
			if (flags.groupByOutputClass)
			{
				featureSets = new Dictionary<string, ICollection<int>>();
				if (flags.groupByFeatureTemplate)
				{
					int pIndex = 0;
					for (int fIndex = 0; fIndex < map.Length; fIndex++)
					{
						int cliqueType = map[fIndex];
						int numCliqueTypeOutputClass = labelIndices[map[fIndex]].Size();
						for (int cliqueOutClass = 0; cliqueOutClass < numCliqueTypeOutputClass; cliqueOutClass++)
						{
							string name = "c:" + cliqueType + "-o:" + cliqueOutClass + "-g:" + featureIndexToTemplateIndex[fIndex];
							if (featureSets.Contains(name))
							{
								featureSets[name].Add(pIndex);
							}
							else
							{
								ICollection<int> newSet = new HashSet<int>();
								newSet.Add(pIndex);
								featureSets[name] = newSet;
							}
							pIndex++;
						}
					}
				}
				else
				{
					int pIndex = 0;
					foreach (int cliqueType in map)
					{
						int numCliqueTypeOutputClass = labelIndices[cliqueType].Size();
						for (int cliqueOutClass = 0; cliqueOutClass < numCliqueTypeOutputClass; cliqueOutClass++)
						{
							string name = "c:" + cliqueType + "-o:" + cliqueOutClass;
							if (featureSets.Contains(name))
							{
								featureSets[name].Add(pIndex);
							}
							else
							{
								ICollection<int> newSet = new HashSet<int>();
								newSet.Add(pIndex);
								featureSets[name] = newSet;
							}
							pIndex++;
						}
					}
				}
			}
			else
			{
				if (flags.groupByFeatureTemplate)
				{
					featureSets = new Dictionary<string, ICollection<int>>();
					int pIndex = 0;
					for (int fIndex = 0; fIndex < map.Length; fIndex++)
					{
						int cliqueType = map[fIndex];
						int numCliqueTypeOutputClass = labelIndices[map[fIndex]].Size();
						for (int cliqueOutClass = 0; cliqueOutClass < numCliqueTypeOutputClass; cliqueOutClass++)
						{
							string name = "c:" + cliqueType + "-g:" + featureIndexToTemplateIndex[fIndex];
							if (featureSets.Contains(name))
							{
								featureSets[name].Add(pIndex);
							}
							else
							{
								ICollection<int> newSet = new HashSet<int>();
								newSet.Add(pIndex);
								featureSets[name] = newSet;
							}
							pIndex++;
						}
					}
				}
			}
			if (featureSets != null)
			{
				int[][] fg = new int[featureSets.Count][];
				log.Info("After feature grouping, total of " + fg.Length + " groups");
				int count = 0;
				foreach (ICollection<int> aSet in featureSets.Values)
				{
					fg[count] = new int[aSet.Count];
					int i = 0;
					foreach (int val in aSet)
					{
						fg[count][i++] = val;
					}
					count++;
				}
				func.SetFeatureGrouping(fg);
			}
			IMinimizer<IDiffFunction> minimizer = GetMinimizer(pruneFeatureItr, evaluators);
			double[] initialWeights;
			if (flags.initialWeights == null)
			{
				initialWeights = func.Initial();
			}
			else
			{
				try
				{
					log.Info("Reading initial weights from file " + flags.initialWeights);
					DataInputStream dis = IOUtils.GetDataInputStream(flags.initialWeights);
					initialWeights = ConvertByteArray.ReadDoubleArr(dis);
				}
				catch (IOException)
				{
					throw new Exception("Could not read from double initial weight file " + flags.initialWeights);
				}
			}
			log.Info("numWeights: " + initialWeights.Length);
			if (flags.testObjFunction)
			{
				StochasticDiffFunctionTester tester = new StochasticDiffFunctionTester(func);
				if (tester.TestSumOfBatches(initialWeights, 1e-4))
				{
					log.Info("Successfully tested stochastic objective function.");
				}
				else
				{
					throw new InvalidOperationException("Testing of stochastic objective function failed.");
				}
			}
			//check gradient
			if (flags.checkGradient)
			{
				if (func.GradientCheck())
				{
					log.Info("gradient check passed");
				}
				else
				{
					throw new Exception("gradient check failed");
				}
			}
			return minimizer.Minimize(func, flags.tolerance, initialWeights);
		}

		public virtual IMinimizer<IDiffFunction> GetMinimizer()
		{
			return GetMinimizer(0, null);
		}

		public virtual IMinimizer<IDiffFunction> GetMinimizer(int featurePruneIteration, IEvaluator[] evaluators)
		{
			IMinimizer<IDiffFunction> minimizer = null;
			QNMinimizer qnMinimizer = null;
			if (flags.useQN || flags.useSGDtoQN)
			{
				// share code for creation of QNMinimizer
				int qnMem;
				if (featurePruneIteration == 0)
				{
					qnMem = flags.QNsize;
				}
				else
				{
					qnMem = flags.QNsize2;
				}
				if (flags.interimOutputFreq != 0)
				{
					IFunction monitor = new ResultStoringMonitor(flags.interimOutputFreq, flags.serializeTo);
					qnMinimizer = new QNMinimizer(monitor, qnMem, flags.useRobustQN);
				}
				else
				{
					qnMinimizer = new QNMinimizer(qnMem, flags.useRobustQN);
				}
				qnMinimizer.TerminateOnMaxItr(flags.maxQNItr);
				qnMinimizer.TerminateOnEvalImprovement(flags.terminateOnEvalImprovement);
				qnMinimizer.SetTerminateOnEvalImprovementNumOfEpoch(flags.terminateOnEvalImprovementNumOfEpoch);
				qnMinimizer.SuppressTestPrompt(flags.suppressTestDebug);
				if (flags.useOWLQN)
				{
					qnMinimizer.UseOWLQN(flags.useOWLQN, flags.priorLambda);
				}
			}
			if (flags.useQN)
			{
				minimizer = qnMinimizer;
			}
			else
			{
				if (flags.useInPlaceSGD)
				{
					SGDMinimizer<IDiffFunction> sgdMinimizer = new SGDMinimizer<IDiffFunction>(flags.sigma, flags.SGDPasses, flags.tuneSampleSize, flags.stochasticBatchSize);
					if (flags.useSGDtoQN)
					{
						minimizer = new HybridMinimizer(sgdMinimizer, qnMinimizer, flags.SGDPasses);
					}
					else
					{
						minimizer = sgdMinimizer;
					}
				}
				else
				{
					if (flags.useAdaGradFOBOS)
					{
						double lambda = 0.5 / (flags.sigma * flags.sigma);
						minimizer = new SGDWithAdaGradAndFOBOS<IDiffFunction>(flags.initRate, lambda, flags.SGDPasses, flags.stochasticBatchSize, flags.priorType, flags.priorAlpha, flags.useAdaDelta, flags.useAdaDiff, flags.adaGradEps, flags.adaDeltaRho);
						((SGDWithAdaGradAndFOBOS<object>)minimizer).TerminateOnEvalImprovement(flags.terminateOnEvalImprovement);
						((SGDWithAdaGradAndFOBOS<object>)minimizer).TerminateOnAvgImprovement(flags.terminateOnAvgImprovement, flags.tolerance);
						((SGDWithAdaGradAndFOBOS<object>)minimizer).SetTerminateOnEvalImprovementNumOfEpoch(flags.terminateOnEvalImprovementNumOfEpoch);
						((SGDWithAdaGradAndFOBOS<object>)minimizer).SuppressTestPrompt(flags.suppressTestDebug);
					}
					else
					{
						if (flags.useSGDtoQN)
						{
							minimizer = new SGDToQNMinimizer(flags.initialGain, flags.stochasticBatchSize, flags.SGDPasses, flags.QNPasses, flags.SGD2QNhessSamples, flags.QNsize, flags.outputIterationsToFile);
						}
						else
						{
							if (flags.useSMD)
							{
								minimizer = new SMDMinimizer<IDiffFunction>(flags.initialGain, flags.stochasticBatchSize, flags.stochasticMethod, flags.SGDPasses);
							}
							else
							{
								if (flags.useSGD)
								{
									minimizer = new InefficientSGDMinimizer<IDiffFunction>(flags.initialGain, flags.stochasticBatchSize);
								}
								else
								{
									if (flags.useScaledSGD)
									{
										minimizer = new ScaledSGDMinimizer(flags.initialGain, flags.stochasticBatchSize, flags.SGDPasses, flags.scaledSGDMethod);
									}
									else
									{
										if (flags.l1reg > 0.0)
										{
											minimizer = ReflectionLoading.LoadByReflection("edu.stanford.nlp.optimization.OWLQNMinimizer", flags.l1reg);
										}
										else
										{
											throw new Exception("No minimizer assigned!");
										}
									}
								}
							}
						}
					}
				}
			}
			if (minimizer is IHasEvaluators)
			{
				if (minimizer is QNMinimizer)
				{
					((QNMinimizer)minimizer).SetEvaluators(flags.evaluateIters, flags.startEvaluateIters, evaluators);
				}
				else
				{
					((IHasEvaluators)minimizer).SetEvaluators(flags.evaluateIters, evaluators);
				}
			}
			return minimizer;
		}

		/// <summary>
		/// Creates a new CRFDatum from the preprocessed allData format, given the
		/// document number, position number, and a List of Object labels.
		/// </summary>
		/// <returns>A new CRFDatum</returns>
		protected internal virtual IList<CRFDatum<ICollection<string>, ICharSequence>> ExtractDatumSequence(int[][][] allData, int beginPosition, int endPosition, IList<IN> labeledWordInfos)
		{
			IList<CRFDatum<ICollection<string>, ICharSequence>> result = new List<CRFDatum<ICollection<string>, ICharSequence>>();
			int beginContext = beginPosition - windowSize + 1;
			if (beginContext < 0)
			{
				beginContext = 0;
			}
			// for the beginning context, add some dummy datums with no features!
			// TODO: is there any better way to do this?
			for (int position = beginContext; position < beginPosition; position++)
			{
				IList<ICollection<string>> cliqueFeatures = new List<ICollection<string>>();
				IList<double[]> featureVals = new List<double[]>();
				for (int i = 0; i < windowSize; i++)
				{
					// create a feature list
					cliqueFeatures.Add(Java.Util.Collections.EmptyList());
					featureVals.Add(null);
				}
				CRFDatum<ICollection<string>, string> datum = new CRFDatum<ICollection<string>, string>(cliqueFeatures, labeledWordInfos[position].Get(typeof(CoreAnnotations.AnswerAnnotation)), featureVals);
				result.Add(datum);
			}
			// now add the real datums
			for (int position_1 = beginPosition; position_1 <= endPosition; position_1++)
			{
				IList<ICollection<string>> cliqueFeatures = new List<ICollection<string>>();
				IList<double[]> featureVals = new List<double[]>();
				for (int i = 0; i < windowSize; i++)
				{
					// create a feature list
					ICollection<string> features = new List<string>();
					for (int j = 0; j < allData[position_1][i].Length; j++)
					{
						features.Add(featureIndex.Get(allData[position_1][i][j]));
					}
					cliqueFeatures.Add(features);
					featureVals.Add(null);
				}
				CRFDatum<ICollection<string>, string> datum = new CRFDatum<ICollection<string>, string>(cliqueFeatures, labeledWordInfos[position_1].Get(typeof(CoreAnnotations.AnswerAnnotation)), featureVals);
				result.Add(datum);
			}
			return result;
		}

		/// <summary>
		/// Adds the List of Lists of CRFDatums to the data and labels arrays, treating
		/// each datum as if it were its own document.
		/// </summary>
		/// <remarks>
		/// Adds the List of Lists of CRFDatums to the data and labels arrays, treating
		/// each datum as if it were its own document. Adds context labels in addition
		/// to the target label for each datum, meaning that for a particular document,
		/// the number of labels will be windowSize-1 greater than the number of
		/// datums.
		/// </remarks>
		/// <param name="processedData">A List of Lists of CRFDatums</param>
		protected internal virtual void AddProcessedData(IList<IList<CRFDatum<ICollection<string>, string>>> processedData, int[][][][] data, int[][] labels, double[][][][] featureVals, int offset)
		{
			for (int i = 0; i < pdSize; i++)
			{
				int dataIndex = i + offset;
				IList<CRFDatum<ICollection<string>, string>> document = processedData[i];
				int dsize = document.Count;
				labels[dataIndex] = new int[dsize];
				data[dataIndex] = new int[dsize][][];
				if (featureVals != null)
				{
					featureVals[dataIndex] = new double[dsize][][];
				}
				for (int j = 0; j < dsize; j++)
				{
					CRFDatum<ICollection<string>, string> crfDatum = document[j];
					// add label, they are offset by extra context
					labels[dataIndex][j] = classIndex.IndexOf(crfDatum.Label());
					// add featureVals
					IList<double[]> featureValList = null;
					if (featureVals != null)
					{
						featureValList = crfDatum.AsFeatureVals();
					}
					// add features
					IList<ICollection<string>> cliques = crfDatum.AsFeatures();
					int csize = cliques.Count;
					data[dataIndex][j] = new int[csize][];
					if (featureVals != null)
					{
						featureVals[dataIndex][j] = new double[csize][];
					}
					for (int k = 0; k < csize; k++)
					{
						ICollection<string> features = cliques[k];
						data[dataIndex][j][k] = new int[features.Count];
						if (featureVals != null)
						{
							featureVals[dataIndex][j][k] = featureValList[k];
						}
						int m = 0;
						try
						{
							foreach (string feature in features)
							{
								// log.info("feature " + feature);
								// if (featureIndex.indexOf(feature)) ;
								if (featureIndex == null)
								{
									System.Console.Out.WriteLine("Feature is NULL!");
								}
								data[dataIndex][j][k][m] = featureIndex.IndexOf(feature);
								m++;
							}
						}
						catch (Exception e)
						{
							log.Error("Add processed data failed.", e);
							log.Info(string.Format("[index=%d, j=%d, k=%d, m=%d]%n", dataIndex, j, k, m));
							log.Info("data.length                    " + data.Length);
							log.Info("data[dataIndex].length         " + data[dataIndex].Length);
							log.Info("data[dataIndex][j].length      " + data[dataIndex][j].Length);
							log.Info("data[dataIndex][j][k].length   " + data[dataIndex][j].Length);
							log.Info("data[dataIndex][j][k][m]       " + data[dataIndex][j][k][m]);
							return;
						}
					}
				}
			}
		}

		protected internal static void SaveProcessedData<_T0>(IList<_T0> datums, string filename)
		{
			log.Info("Saving processed data of size " + datums.Count + " to serialized file...");
			ObjectOutputStream oos = null;
			try
			{
				oos = new ObjectOutputStream(new FileOutputStream(filename));
				oos.WriteObject(datums);
			}
			catch (IOException)
			{
			}
			finally
			{
				// do nothing
				IOUtils.CloseIgnoringExceptions(oos);
			}
			log.Info("done.");
		}

		protected internal static IList<IList<CRFDatum<ICollection<string>, string>>> LoadProcessedData(string filename)
		{
			IList<IList<CRFDatum<ICollection<string>, string>>> result;
			try
			{
				result = IOUtils.ReadObjectFromURLOrClasspathOrFileSystem(filename);
			}
			catch (Exception e)
			{
				log.Warn(e);
				result = Java.Util.Collections.EmptyList();
			}
			log.Info("Loading processed data from serialized file ... done. Got " + result.Count + " datums.");
			return result;
		}

		/// <exception cref="System.Exception"/>
		protected internal virtual void LoadTextClassifier(BufferedReader br)
		{
			string line = br.ReadLine();
			// first line should be this format:
			// labelIndices.size()=\t%d
			string[] toks = line.Split("\\t");
			if (!toks[0].Equals("labelIndices.length="))
			{
				throw new Exception("format error");
			}
			int size = System.Convert.ToInt32(toks[1]);
			labelIndices = new List<IIndex<CRFLabel>>(size);
			for (int labelIndicesIdx = 0; labelIndicesIdx < size; labelIndicesIdx++)
			{
				line = br.ReadLine();
				// first line should be this format:
				// labelIndices.length=\t%d
				// labelIndices[0].size()=\t%d
				toks = line.Split("\\t");
				if (!(toks[0].StartsWith("labelIndices[") && toks[0].EndsWith("].size()=")))
				{
					throw new Exception("format error");
				}
				int labelIndexSize = System.Convert.ToInt32(toks[1]);
				labelIndices.Add(new HashIndex<CRFLabel>());
				int count = 0;
				while (count < labelIndexSize)
				{
					line = br.ReadLine();
					toks = line.Split("\\t");
					int idx = System.Convert.ToInt32(toks[0]);
					if (count != idx)
					{
						throw new Exception("format error");
					}
					string[] crflabelstr = toks[1].Split(" ");
					int[] crflabel = new int[crflabelstr.Length];
					for (int i = 0; i < crflabelstr.Length; i++)
					{
						crflabel[i] = System.Convert.ToInt32(crflabelstr[i]);
					}
					CRFLabel crfL = new CRFLabel(crflabel);
					labelIndices[labelIndicesIdx].Add(crfL);
					count++;
				}
			}
			foreach (IIndex<CRFLabel> index in labelIndices)
			{
				for (int j = 0; j < index.Size(); j++)
				{
					int[] label = index.Get(j).GetLabel();
					IList<int> list = new List<int>();
					foreach (int l in label)
					{
						list.Add(l);
					}
				}
			}
			line = br.ReadLine();
			toks = line.Split("\\t");
			if (!toks[0].Equals("classIndex.size()="))
			{
				throw new Exception("format error");
			}
			int classIndexSize = System.Convert.ToInt32(toks[1]);
			classIndex = new HashIndex<string>();
			int count_1 = 0;
			while (count_1 < classIndexSize)
			{
				line = br.ReadLine();
				toks = line.Split("\\t");
				int idx = System.Convert.ToInt32(toks[0]);
				if (count_1 != idx)
				{
					throw new Exception("format error");
				}
				classIndex.Add(toks[1]);
				count_1++;
			}
			line = br.ReadLine();
			toks = line.Split("\\t");
			if (!toks[0].Equals("featureIndex.size()="))
			{
				throw new Exception("format error");
			}
			int featureIndexSize = System.Convert.ToInt32(toks[1]);
			featureIndex = new HashIndex<string>();
			count_1 = 0;
			while (count_1 < featureIndexSize)
			{
				line = br.ReadLine();
				toks = line.Split("\\t");
				int idx = System.Convert.ToInt32(toks[0]);
				if (count_1 != idx)
				{
					throw new Exception("format error");
				}
				featureIndex.Add(toks[1]);
				count_1++;
			}
			line = br.ReadLine();
			if (!line.Equals("<flags>"))
			{
				throw new Exception("format error");
			}
			Properties p = new Properties();
			line = br.ReadLine();
			while (!line.Equals("</flags>"))
			{
				// log.info("DEBUG: flags line: "+line);
				string[] keyValue = line.Split("=");
				// System.err.printf("DEBUG: p.setProperty(%s,%s)%n", keyValue[0],
				// keyValue[1]);
				p.SetProperty(keyValue[0], keyValue[1]);
				line = br.ReadLine();
			}
			// log.info("DEBUG: out from flags");
			flags = new SeqClassifierFlags(p);
			if (flags.useEmbedding)
			{
				line = br.ReadLine();
				toks = line.Split("\\t");
				if (!toks[0].Equals("embeddings.size()="))
				{
					throw new Exception("format error in embeddings");
				}
				int embeddingSize = System.Convert.ToInt32(toks[1]);
				embeddings = Generics.NewHashMap(embeddingSize);
				count_1 = 0;
				while (count_1 < embeddingSize)
				{
					line = br.ReadLine().Trim();
					toks = line.Split("\\t");
					string word = toks[0];
					double[] arr = ArrayUtils.ToDoubleArray(toks[1].Split(" "));
					embeddings[word] = arr;
					count_1++;
				}
			}
			// <featureFactory>
			// edu.stanford.nlp.wordseg.Gale2007ChineseSegmenterFeatureFactory
			// </featureFactory>
			line = br.ReadLine();
			string[] featureFactoryName = line.Split(" ");
			if (featureFactoryName.Length < 2 || !featureFactoryName[0].Equals("<featureFactory>") || !featureFactoryName[featureFactoryName.Length - 1].Equals("</featureFactory>"))
			{
				throw new Exception("format error unexpected featureFactory line: " + line);
			}
			featureFactories = Generics.NewArrayList();
			for (int ff = 1; ff < featureFactoryName.Length - 1; ++ff)
			{
				FeatureFactory<IN> featureFactory = (FeatureFactory<IN>)System.Activator.CreateInstance(Sharpen.Runtime.GetType(featureFactoryName[1]));
				featureFactory.Init(flags);
				featureFactories.Add(featureFactory);
			}
			Reinit();
			// <windowSize> 2 </windowSize>
			line = br.ReadLine();
			string[] windowSizeName = line.Split(" ");
			if (!windowSizeName[0].Equals("<windowSize>") || !windowSizeName[2].Equals("</windowSize>"))
			{
				throw new Exception("format error");
			}
			windowSize = System.Convert.ToInt32(windowSizeName[1]);
			// weights.length= 2655170
			line = br.ReadLine();
			toks = line.Split("\\t");
			if (!toks[0].Equals("weights.length="))
			{
				throw new Exception("format error");
			}
			int weightsLength = System.Convert.ToInt32(toks[1]);
			weights = new double[weightsLength][];
			count_1 = 0;
			while (count_1 < weightsLength)
			{
				line = br.ReadLine();
				toks = line.Split("\\t");
				int weights2Length = System.Convert.ToInt32(toks[0]);
				weights[count_1] = new double[weights2Length];
				string[] weightsValue = toks[1].Split(" ");
				if (weights2Length != weightsValue.Length)
				{
					throw new Exception("weights format error");
				}
				for (int i2 = 0; i2 < weights2Length; i2++)
				{
					weights[count_1][i2] = double.ParseDouble(weightsValue[i2]);
				}
				count_1++;
			}
			System.Console.Error.Printf("DEBUG: double[%d][] weights loaded%n", weightsLength);
			line = br.ReadLine();
			if (line != null)
			{
				throw new Exception("weights format error");
			}
		}

		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MemberAccessException"/>
		public virtual void LoadTextClassifier(string text, Properties props)
		{
			// log.info("DEBUG: in loadTextClassifier");
			log.Info("Loading Text Classifier from " + text);
			try
			{
				using (BufferedReader br = IOUtils.ReaderFromString(text))
				{
					LoadTextClassifier(br);
				}
			}
			catch (Exception ex)
			{
				log.Info("Exception in loading text classifier from " + text, ex);
			}
		}

		/// <exception cref="System.Exception"/>
		protected internal virtual void SerializeTextClassifier(PrintWriter pw)
		{
			pw.Printf("labelIndices.length=\t%d%n", labelIndices.Count);
			for (int i = 0; i < labelIndices.Count; i++)
			{
				pw.Printf("labelIndices[%d].size()=\t%d%n", i, labelIndices[i].Size());
				for (int j = 0; j < labelIndices[i].Size(); j++)
				{
					int[] label = labelIndices[i].Get(j).GetLabel();
					IList<int> list = new List<int>();
					foreach (int l in label)
					{
						list.Add(l);
					}
					pw.Printf("%d\t%s%n", j, StringUtils.Join(list, " "));
				}
			}
			pw.Printf("classIndex.size()=\t%d%n", classIndex.Size());
			for (int i_1 = 0; i_1 < classIndex.Size(); i_1++)
			{
				pw.Printf("%d\t%s%n", i_1, classIndex.Get(i_1));
			}
			// pw.printf("</classIndex>%n");
			pw.Printf("featureIndex.size()=\t%d%n", featureIndex.Size());
			for (int i_2 = 0; i_2 < featureIndex.Size(); i_2++)
			{
				pw.Printf("%d\t%s%n", i_2, featureIndex.Get(i_2));
			}
			// pw.printf("</featureIndex>%n");
			pw.Println("<flags>");
			pw.Print(flags);
			pw.Println("</flags>");
			if (flags.useEmbedding)
			{
				pw.Printf("embeddings.size()=\t%d%n", embeddings.Count);
				foreach (string word in embeddings.Keys)
				{
					double[] arr = embeddings[word];
					double[] arrUnboxed = new double[arr.Length];
					for (int i_3 = 0; i_3 < arr.Length; i_3++)
					{
						arrUnboxed[i_3] = arr[i_3];
					}
					pw.Printf("%s\t%s%n", word, StringUtils.Join(arrUnboxed, " "));
				}
			}
			pw.Printf("<featureFactory>");
			foreach (FeatureFactory<IN> featureFactory in featureFactories)
			{
				pw.Printf(" %s ", featureFactory.GetType().FullName);
			}
			pw.Printf("</featureFactory>%n");
			pw.Printf("<windowSize> %d </windowSize>%n", windowSize);
			pw.Printf("weights.length=\t%d%n", weights.Length);
			foreach (double[] ws in weights)
			{
				List<double> list = new List<double>();
				foreach (double w in ws)
				{
					list.Add(w);
				}
				pw.Printf("%d\t%s%n", ws.Length, StringUtils.Join(list, " "));
			}
		}

		/// <summary>Serialize the model to a human readable format.</summary>
		/// <remarks>
		/// Serialize the model to a human readable format. It's not yet complete. It
		/// should now work for Chinese segmenter though. TODO: check things in
		/// serializeClassifier and add other necessary serialization back.
		/// </remarks>
		/// <param name="serializePath">File to write text format of classifier to.</param>
		public virtual void SerializeTextClassifier(string serializePath)
		{
			try
			{
				PrintWriter pw = new PrintWriter(new GZIPOutputStream(new FileOutputStream(serializePath)));
				SerializeTextClassifier(pw);
				pw.Close();
				log.Info("Serializing Text classifier to " + serializePath + "... done.");
			}
			catch (Exception e)
			{
				log.Info("Serializing Text classifier to " + serializePath + "... FAILED.", e);
			}
		}

		public virtual void SerializeClassIndex(string serializePath)
		{
			ObjectOutputStream oos = null;
			try
			{
				oos = IOUtils.WriteStreamFromString(serializePath);
				oos.WriteObject(classIndex);
				log.Info("Serializing class index to " + serializePath + "... done.");
			}
			catch (Exception e)
			{
				log.Info("Serializing class index to " + serializePath + "... FAILED.", e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(oos);
			}
		}

		public static IIndex<string> LoadClassIndexFromFile(string serializePath)
		{
			ObjectInputStream ois = null;
			IIndex<string> c = null;
			try
			{
				ois = IOUtils.ReadStreamFromString(serializePath);
				c = (IIndex<string>)ois.ReadObject();
				log.Info("Reading class index from " + serializePath + "... done.");
			}
			catch (Exception e)
			{
				log.Info("Reading class index from " + serializePath + "... FAILED.", e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(ois);
			}
			return c;
		}

		public virtual void SerializeWeights(string serializePath)
		{
			ObjectOutputStream oos = null;
			try
			{
				oos = IOUtils.WriteStreamFromString(serializePath);
				oos.WriteObject(weights);
				log.Info("Serializing weights to " + serializePath + "... done.");
			}
			catch (Exception e)
			{
				log.Info("Serializing weights to " + serializePath + "... FAILED.", e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(oos);
			}
		}

		public static double[][] LoadWeightsFromFile(string serializePath)
		{
			ObjectInputStream ois = null;
			double[][] w = null;
			try
			{
				ois = IOUtils.ReadStreamFromString(serializePath);
				w = (double[][])ois.ReadObject();
				log.Info("Reading weights from " + serializePath + "... done.");
			}
			catch (Exception e)
			{
				log.Info("Reading weights from " + serializePath + "... FAILED.", e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(ois);
			}
			return w;
		}

		public virtual void SerializeFeatureIndex(string serializePath)
		{
			ObjectOutputStream oos = null;
			try
			{
				oos = IOUtils.WriteStreamFromString(serializePath);
				oos.WriteObject(featureIndex);
				log.Info("Serializing FeatureIndex to " + serializePath + "... done.");
			}
			catch (Exception e)
			{
				log.Info("Failed");
				log.Info("Serializing FeatureIndex to " + serializePath + "... FAILED.", e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(oos);
			}
		}

		public static IIndex<string> LoadFeatureIndexFromFile(string serializePath)
		{
			ObjectInputStream ois = null;
			IIndex<string> f = null;
			try
			{
				ois = IOUtils.ReadStreamFromString(serializePath);
				f = (IIndex<string>)ois.ReadObject();
				log.Info("Reading FeatureIndex from " + serializePath + "... done.");
			}
			catch (Exception e)
			{
				log.Info("Reading FeatureIndex from " + serializePath + "... FAILED.", e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(ois);
			}
			return f;
		}

		/// <summary><inheritDoc/></summary>
		public override void SerializeClassifier(string serializePath)
		{
			ObjectOutputStream oos = null;
			try
			{
				oos = IOUtils.WriteStreamFromString(serializePath);
				SerializeClassifier(oos);
				log.Info("Serializing classifier to " + serializePath + "... done.");
			}
			catch (Exception e)
			{
				throw new RuntimeIOException("Serializing classifier to " + serializePath + "... FAILED", e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(oos);
			}
		}

		/// <summary>Serialize the classifier to the given ObjectOutputStream.</summary>
		/// <remarks>
		/// Serialize the classifier to the given ObjectOutputStream.
		/// <br />
		/// (Since the classifier is a processor, we don't want to serialize the
		/// whole classifier but just the data that represents a classifier model.)
		/// </remarks>
		public override void SerializeClassifier(ObjectOutputStream oos)
		{
			try
			{
				oos.WriteObject(labelIndices);
				oos.WriteObject(classIndex);
				oos.WriteObject(featureIndex);
				oos.WriteObject(flags);
				if (flags.useEmbedding)
				{
					oos.WriteObject(embeddings);
				}
				// For some reason, writing out the array of FeatureFactory
				// objects doesn't seem to work.  The resulting classifier
				// doesn't have the lexicon (distsim object) correctly saved.  So now custom write the list
				oos.WriteObject(featureFactories.Count);
				foreach (FeatureFactory<IN> ff in featureFactories)
				{
					oos.WriteObject(ff);
				}
				oos.WriteInt(windowSize);
				oos.WriteObject(weights);
				// oos.writeObject(WordShapeClassifier.getKnownLowerCaseWords());
				oos.WriteObject(knownLCWords);
				if (labelDictionary != null)
				{
					oos.WriteObject(labelDictionary);
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>Loads a classifier from the specified InputStream.</summary>
		/// <remarks>
		/// Loads a classifier from the specified InputStream. This version works
		/// quietly (unless VERBOSE is true). If props is non-null then any properties
		/// it specifies override those in the serialized file. However, only some
		/// properties are sensible to change (you shouldn't change how features are
		/// defined).
		/// <p>
		/// <i>Note:</i> This method does not close the ObjectInputStream. (But earlier
		/// versions of the code used to, so beware....)
		/// </remarks>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public override void LoadClassifier(ObjectInputStream ois, Properties props)
		{
			// can't have right types in deserialization
			object o = ois.ReadObject();
			// TODO: when we next break serialization, get rid of this fork and only read the List<Index> (i.e., keep first case)
			if (o is IList)
			{
				labelIndices = (IList<IIndex<CRFLabel>>)o;
			}
			else
			{
				IIndex<CRFLabel>[] indexArray = (IIndex<CRFLabel>[])o;
				labelIndices = new List<IIndex<CRFLabel>>(indexArray.Length);
				Java.Util.Collections.AddAll(labelIndices, indexArray);
			}
			classIndex = (IIndex<string>)ois.ReadObject();
			featureIndex = (IIndex<string>)ois.ReadObject();
			flags = (SeqClassifierFlags)ois.ReadObject();
			if (flags.useEmbedding)
			{
				embeddings = (IDictionary<string, double[]>)ois.ReadObject();
			}
			object featureFactory = ois.ReadObject();
			if (featureFactory is IList)
			{
				featureFactories = ErasureUtils.UncheckedCast(featureFactories);
			}
			else
			{
				//      int i = 0;
				//      for (FeatureFactory ff : featureFactories) { // XXXX
				//        System.err.println("List FF #" + i + ": " + ((NERFeatureFactory) ff).describeDistsimLexicon()); // XXXX
				//        i++;
				//      }
				if (featureFactory is FeatureFactory)
				{
					featureFactories = Generics.NewArrayList();
					featureFactories.Add((FeatureFactory<IN>)featureFactory);
				}
				else
				{
					//      System.err.println(((NERFeatureFactory) featureFactory).describeDistsimLexicon()); // XXXX
					if (featureFactory is int)
					{
						// this is the current format (2014) since writing list didn't work (see note in serializeClassifier).
						int size = (int)featureFactory;
						featureFactories = Generics.NewArrayList(size);
						for (int i = 0; i < size; ++i)
						{
							featureFactory = ois.ReadObject();
							if (!(featureFactory is FeatureFactory))
							{
								throw new RuntimeIOException("Should have FeatureFactory but got " + featureFactory.GetType());
							}
							//        System.err.println("FF #" + i + ": " + ((NERFeatureFactory) featureFactory).describeDistsimLexicon()); // XXXX
							featureFactories.Add((FeatureFactory<IN>)featureFactory);
						}
					}
				}
			}
			// log.info("properties passed into CRF's loadClassifier are:" + props);
			if (props != null)
			{
				flags.SetProperties(props, false);
			}
			windowSize = ois.ReadInt();
			weights = (double[][])ois.ReadObject();
			// WordShapeClassifier.setKnownLowerCaseWords((Set) ois.readObject());
			ICollection<string> lcWords = (ICollection<string>)ois.ReadObject();
			if (lcWords is MaxSizeConcurrentHashSet)
			{
				knownLCWords = (MaxSizeConcurrentHashSet<string>)lcWords;
			}
			else
			{
				knownLCWords = new MaxSizeConcurrentHashSet<string>(lcWords);
			}
			Reinit();
			if (flags.labelDictionaryCutoff > 0)
			{
				labelDictionary = (LabelDictionary)ois.ReadObject();
			}
		}

		/// <summary>
		/// This is used to load the default supplied classifier stored within the jar
		/// file.
		/// </summary>
		/// <remarks>
		/// This is used to load the default supplied classifier stored within the jar
		/// file. THIS FUNCTION WILL ONLY WORK IF THE CODE WAS LOADED FROM A JAR FILE
		/// WHICH HAS A SERIALIZED CLASSIFIER STORED INSIDE IT.
		/// </remarks>
		public virtual void LoadDefaultClassifier()
		{
			LoadClassifierNoExceptions(DefaultClassifier);
		}

		public virtual void LoadTagIndex()
		{
			if (tagIndex == null)
			{
				tagIndex = new HashIndex<string>();
				foreach (string tag in classIndex.ObjectsList())
				{
					string[] parts = tag.Split("-");
					// if (parts.length > 1)
					tagIndex.Add(parts[parts.Length - 1]);
				}
				tagIndex.Add(flags.backgroundSymbol);
			}
			if (flags.useNERPriorBIO)
			{
				if (entityMatrices == null)
				{
					entityMatrices = ReadEntityMatrices(flags.entityMatrix, tagIndex);
				}
			}
		}

		private static double[][] ParseMatrix(string[] lines, IIndex<string> tagIndex, int matrixSize, bool smooth)
		{
			return ParseMatrix(lines, tagIndex, matrixSize, smooth, true);
		}

		/// <returns>
		/// a matrix where each entry m[i][j] is logP(j|i)
		/// in other words, each row vector is normalized log conditional likelihood
		/// </returns>
		internal static double[][] ParseMatrix(string[] lines, IIndex<string> tagIndex, int matrixSize, bool smooth, bool useLogProb)
		{
			double[][] matrix = new double[matrixSize][];
			for (int i = 0; i < matrix.Length; i++)
			{
				matrix[i] = new double[matrixSize];
			}
			foreach (string line in lines)
			{
				string[] parts = line.Split("\t");
				foreach (string part in parts)
				{
					string[] subparts = part.Split(" ");
					string[] subsubparts = subparts[0].Split(":");
					double counts = double.ParseDouble(subparts[1]);
					if (counts == 0.0 && smooth)
					{
						// smoothing
						counts = 1.0;
					}
					int tagIndex1 = tagIndex.IndexOf(subsubparts[0]);
					int tagIndex2 = tagIndex.IndexOf(subsubparts[1]);
					matrix[tagIndex1][tagIndex2] = counts;
				}
			}
			for (int i_1 = 0; i_1 < matrix.Length; i_1++)
			{
				double sum = ArrayMath.Sum(matrix[i_1]);
				for (int j = 0; j < matrix[i_1].Length; j++)
				{
					// log conditional probability
					if (useLogProb)
					{
						matrix[i_1][j] = System.Math.Log(matrix[i_1][j] / sum);
					}
					else
					{
						matrix[i_1][j] = matrix[i_1][j] / sum;
					}
				}
			}
			return matrix;
		}

		internal static Pair<double[][], double[][]> ReadEntityMatrices(string fileName, IIndex<string> tagIndex)
		{
			int numTags = tagIndex.Size();
			int matrixSize = numTags - 1;
			string[] matrixLines = new string[matrixSize];
			string[] subMatrixLines = new string[matrixSize];
			try
			{
				using (BufferedReader br = IOUtils.ReaderFromString(fileName))
				{
					int lineCount = 0;
					for (string line; (line = br.ReadLine()) != null; )
					{
						line = line.Trim();
						if (lineCount < matrixSize)
						{
							matrixLines[lineCount] = line;
						}
						else
						{
							subMatrixLines[lineCount - matrixSize] = line;
						}
						lineCount++;
					}
				}
			}
			catch (Exception ex)
			{
				throw new RuntimeIOException(ex);
			}
			double[][] matrix = ParseMatrix(matrixLines, tagIndex, matrixSize, true);
			double[][] subMatrix = ParseMatrix(subMatrixLines, tagIndex, matrixSize, true);
			// In Jenny's paper, use the square root of non-log prob for matrix, but not for subMatrix
			for (int i = 0; i < matrix.Length; i++)
			{
				for (int j = 0; j < matrix[i].Length; j++)
				{
					matrix[i][j] = matrix[i][j] / 2;
				}
			}
			log.Info("Matrix: ");
			log.Info(ArrayUtils.ToString(matrix));
			log.Info("SubMatrix: ");
			log.Info(ArrayUtils.ToString(subMatrix));
			return new Pair<double[][], double[][]>(matrix, subMatrix);
		}

		public virtual void WriteWeights(TextWriter p)
		{
			foreach (string feature in featureIndex)
			{
				int index = featureIndex.IndexOf(feature);
				// line.add(feature+"["+(-p)+"]");
				// rowHeaders.add(feature + '[' + (-p) + ']');
				double[] v = weights[index];
				IIndex<CRFLabel> l = this.labelIndices[0];
				p.WriteLine(feature + "\t\t");
				foreach (CRFLabel label in l)
				{
					p.Write(label.ToString(classIndex) + ':' + v[l.IndexOf(label)] + '\t');
				}
				p.WriteLine();
			}
		}

		public virtual IDictionary<string, ICounter<string>> TopWeights()
		{
			IDictionary<string, ICounter<string>> w = new Dictionary<string, ICounter<string>>();
			foreach (string feature in featureIndex)
			{
				int index = featureIndex.IndexOf(feature);
				// line.add(feature+"["+(-p)+"]");
				// rowHeaders.add(feature + '[' + (-p) + ']');
				double[] v = weights[index];
				IIndex<CRFLabel> l = this.labelIndices[0];
				foreach (CRFLabel label in l)
				{
					if (!w.Contains(label.ToString(classIndex)))
					{
						w[label.ToString(classIndex)] = new ClassicCounter<string>();
					}
					w[label.ToString(classIndex)].SetCount(feature, v[l.IndexOf(label)]);
				}
			}
			return w;
		}

		/// <summary>Read real-valued vector embeddings for (lowercased) word tokens.</summary>
		/// <remarks>
		/// Read real-valued vector embeddings for (lowercased) word tokens.
		/// A lexicon is contained in the file flags.embeddingWords.
		/// The word vectors are then in the same order in the file flags.embeddingVectors.
		/// </remarks>
		/// <exception cref="System.IO.IOException">If embedding vectors canot be loaded</exception>
		private void ReadEmbeddingsData()
		{
			System.Console.Error.Printf("Reading embedding files %s and %s.%n", flags.embeddingWords, flags.embeddingVectors);
			IList<string> wordList = new List<string>();
			using (BufferedReader br = IOUtils.ReaderFromString(flags.embeddingWords))
			{
				for (string line; (line = br.ReadLine()) != null; )
				{
					wordList.Add(line.Trim());
				}
				log.Info("Found a dictionary of size " + wordList.Count);
			}
			embeddings = Generics.NewHashMap();
			using (BufferedReader br_1 = IOUtils.ReaderFromString(flags.embeddingVectors))
			{
				int count = 0;
				int vectorSize = -1;
				bool warned = false;
				for (string line; (line = br_1.ReadLine()) != null; )
				{
					double[] vector = ArrayUtils.ToDoubleArray(line.Trim().Split(" "));
					if (vectorSize < 0)
					{
						vectorSize = vector.Length;
					}
					else
					{
						if (vectorSize != vector.Length && !warned)
						{
							log.Info("Inconsistent vector lengths: " + vectorSize + " vs. " + vector.Length);
							warned = true;
						}
					}
					embeddings[wordList[count++]] = vector;
				}
				log.Info("Found " + count + " matching embeddings of dimension " + vectorSize);
			}
		}

		public override IList<IN> ClassifyWithGlobalInformation(IList<IN> tokenSeq, ICoreMap doc, ICoreMap sent)
		{
			return Classify(tokenSeq);
		}

		/// <summary>
		/// This is used to load the default supplied classifier stored within the jar
		/// file.
		/// </summary>
		/// <remarks>
		/// This is used to load the default supplied classifier stored within the jar
		/// file. THIS FUNCTION WILL ONLY WORK IF THE CODE WAS LOADED FROM A JAR FILE
		/// WHICH HAS A SERIALIZED CLASSIFIER STORED INSIDE IT.
		/// </remarks>
		public virtual void LoadDefaultClassifier(Properties props)
		{
			LoadClassifierNoExceptions(DefaultClassifier, props);
		}

		/// <summary>Used to get the default supplied classifier inside the jar file.</summary>
		/// <remarks>
		/// Used to get the default supplied classifier inside the jar file. THIS
		/// FUNCTION WILL ONLY WORK IF THE CODE WAS LOADED FROM A JAR FILE WHICH HAS A
		/// SERIALIZED CLASSIFIER STORED INSIDE IT.
		/// </remarks>
		/// <returns>The default CRFClassifier in the jar file (if there is one)</returns>
		public static Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> GetDefaultClassifier<Inn>()
			where Inn : ICoreMap
		{
			Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> crf = new Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN>();
			crf.LoadDefaultClassifier();
			return crf;
		}

		/// <summary>Used to get the default supplied classifier inside the jar file.</summary>
		/// <remarks>
		/// Used to get the default supplied classifier inside the jar file. THIS
		/// FUNCTION WILL ONLY WORK IF THE CODE WAS LOADED FROM A JAR FILE WHICH HAS A
		/// SERIALIZED CLASSIFIER STORED INSIDE IT.
		/// </remarks>
		/// <returns>The default CRFClassifier in the jar file (if there is one)</returns>
		public static Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> GetDefaultClassifier<Inn>(Properties props)
			where Inn : ICoreMap
		{
			Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> crf = new Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN>();
			crf.LoadDefaultClassifier(props);
			return crf;
		}

		/// <summary>Loads a CRF classifier from a filepath, and returns it.</summary>
		/// <param name="file">File to load classifier from</param>
		/// <returns>The CRF classifier</returns>
		/// <exception cref="System.IO.IOException">If there are problems accessing the input stream</exception>
		/// <exception cref="System.InvalidCastException">If there are problems interpreting the serialized data</exception>
		/// <exception cref="System.TypeLoadException">If there are problems interpreting the serialized data</exception>
		public static Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> GetClassifier<Inn>(File file)
			where Inn : ICoreMap
		{
			Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> crf = new Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN>();
			crf.LoadClassifier(file);
			return crf;
		}

		/// <summary>Loads a CRF classifier from an InputStream, and returns it.</summary>
		/// <remarks>
		/// Loads a CRF classifier from an InputStream, and returns it. This method
		/// does not buffer the InputStream, so you should have buffered it before
		/// calling this method.
		/// </remarks>
		/// <param name="in">InputStream to load classifier from</param>
		/// <returns>The CRF classifier</returns>
		/// <exception cref="System.IO.IOException">If there are problems accessing the input stream</exception>
		/// <exception cref="System.InvalidCastException">If there are problems interpreting the serialized data</exception>
		/// <exception cref="System.TypeLoadException">If there are problems interpreting the serialized data</exception>
		public static Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> GetClassifier<Inn>(InputStream @in)
			where Inn : ICoreMap
		{
			Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> crf = new Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN>();
			crf.LoadClassifier(@in);
			return crf;
		}

		// new method for getting a CRFClassifier from an ObjectInputStream
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> GetClassifier<Inn>(ObjectInputStream ois)
			where Inn : ICoreMap
		{
			Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> crf = new Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN>();
			crf.LoadClassifier(ois, null);
			return crf;
		}

		public static Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> GetClassifierNoExceptions<Inn>(string loadPath)
			where Inn : ICoreMap
		{
			Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> crf = new Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN>();
			crf.LoadClassifierNoExceptions(loadPath);
			return crf;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static Edu.Stanford.Nlp.IE.Crf.CRFClassifier<CoreLabel> GetClassifier(string loadPath)
		{
			Edu.Stanford.Nlp.IE.Crf.CRFClassifier<CoreLabel> crf = new Edu.Stanford.Nlp.IE.Crf.CRFClassifier<CoreLabel>();
			crf.LoadClassifier(loadPath);
			return crf;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> GetClassifier<Inn>(string loadPath, Properties props)
			where Inn : ICoreMap
		{
			Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> crf = new Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN>();
			crf.LoadClassifier(loadPath, props);
			return crf;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> GetClassifier<Inn>(ObjectInputStream ois, Properties props)
			where Inn : ICoreMap
		{
			Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN> crf = new Edu.Stanford.Nlp.IE.Crf.CRFClassifier<INN>();
			crf.LoadClassifier(ois, props);
			return crf;
		}

		private static Edu.Stanford.Nlp.IE.Crf.CRFClassifier<CoreLabel> ChooseCRFClassifier(SeqClassifierFlags flags)
		{
			Edu.Stanford.Nlp.IE.Crf.CRFClassifier<CoreLabel> crf;
			// initialized in if/else
			if (flags.useFloat)
			{
				crf = new CRFClassifierFloat<CoreLabel>(flags);
			}
			else
			{
				if (flags.nonLinearCRF)
				{
					crf = new CRFClassifierNonlinear<CoreLabel>(flags);
				}
				else
				{
					if (flags.numLopExpert > 1)
					{
						crf = new CRFClassifierWithLOP<CoreLabel>(flags);
					}
					else
					{
						if (flags.priorType.Equals("DROPOUT"))
						{
							crf = new CRFClassifierWithDropout<CoreLabel>(flags);
						}
						else
						{
							if (flags.useNoisyLabel)
							{
								crf = new CRFClassifierNoisyLabel<CoreLabel>(flags);
							}
							else
							{
								crf = new Edu.Stanford.Nlp.IE.Crf.CRFClassifier<CoreLabel>(flags);
							}
						}
					}
				}
			}
			return crf;
		}

		/// <summary>The main method.</summary>
		/// <remarks>The main method. See the class documentation.</remarks>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			StringUtils.LogInvocationString(log, args);
			Properties props = StringUtils.ArgsToProperties(args);
			SeqClassifierFlags flags = new SeqClassifierFlags(props);
			Edu.Stanford.Nlp.IE.Crf.CRFClassifier<CoreLabel> crf = ChooseCRFClassifier(flags);
			string testFile = flags.testFile;
			string testFiles = flags.testFiles;
			string textFile = flags.textFile;
			string textFiles = flags.textFiles;
			string loadPath = flags.loadClassifier;
			string loadTextPath = flags.loadTextClassifier;
			string serializeTo = flags.serializeTo;
			string serializeToText = flags.serializeToText;
			if (crf.flags.useEmbedding && crf.flags.embeddingWords != null && crf.flags.embeddingVectors != null)
			{
				crf.ReadEmbeddingsData();
			}
			if (crf.flags.loadClassIndexFrom != null)
			{
				crf.classIndex = LoadClassIndexFromFile(crf.flags.loadClassIndexFrom);
			}
			if (loadPath != null)
			{
				crf.LoadClassifierNoExceptions(loadPath, props);
			}
			else
			{
				if (loadTextPath != null)
				{
					log.Info("Warning: this is now only tested for Chinese Segmenter");
					log.Info("(Sun Dec 23 00:59:39 2007) (pichuan)");
					try
					{
						crf.LoadTextClassifier(loadTextPath, props);
					}
					catch (Exception e)
					{
						// log.info("DEBUG: out from crf.loadTextClassifier");
						throw new Exception("error loading " + loadTextPath, e);
					}
				}
				else
				{
					if (crf.flags.loadJarClassifier != null)
					{
						// legacy option support
						crf.LoadClassifierNoExceptions(crf.flags.loadJarClassifier, props);
					}
					else
					{
						if (crf.flags.trainFile != null || crf.flags.trainFileList != null)
						{
							Timing timing = new Timing();
							// temporarily unlimited size of knownLCWords
							int knownLCWordsLimit = crf.knownLCWords.GetMaxSize();
							crf.knownLCWords.SetMaxSize(-1);
							crf.Train();
							crf.knownLCWords.SetMaxSize(knownLCWordsLimit);
							timing.Done(log, "CRFClassifier training");
						}
						else
						{
							crf.LoadDefaultClassifier();
						}
					}
				}
			}
			crf.LoadTagIndex();
			if (serializeTo != null)
			{
				crf.SerializeClassifier(serializeTo);
			}
			if (crf.flags.serializeWeightsTo != null)
			{
				crf.SerializeWeights(crf.flags.serializeWeightsTo);
			}
			if (crf.flags.serializeFeatureIndexTo != null)
			{
				crf.SerializeFeatureIndex(crf.flags.serializeFeatureIndexTo);
			}
			if (serializeToText != null)
			{
				crf.SerializeTextClassifier(serializeToText);
			}
			if (testFile != null)
			{
				// todo: Change testFile to call testFiles with a singleton list
				IDocumentReaderAndWriter<CoreLabel> readerAndWriter = crf.DefaultReaderAndWriter();
				if (crf.flags.searchGraphPrefix != null)
				{
					crf.ClassifyAndWriteViterbiSearchGraph(testFile, crf.flags.searchGraphPrefix, readerAndWriter);
				}
				else
				{
					if (crf.flags.printFirstOrderProbs)
					{
						crf.PrintFirstOrderProbs(testFile, readerAndWriter);
					}
					else
					{
						if (crf.flags.printFactorTable)
						{
							crf.PrintFactorTable(testFile, readerAndWriter);
						}
						else
						{
							if (crf.flags.printProbs)
							{
								crf.PrintProbs(testFile, readerAndWriter);
							}
							else
							{
								if (crf.flags.useKBest)
								{
									int k = crf.flags.kBest;
									crf.ClassifyAndWriteAnswersKBest(testFile, k, readerAndWriter);
								}
								else
								{
									if (crf.flags.printLabelValue)
									{
										crf.PrintLabelInformation(testFile, readerAndWriter);
									}
									else
									{
										crf.ClassifyAndWriteAnswers(testFile, readerAndWriter, true);
									}
								}
							}
						}
					}
				}
			}
			if (testFiles != null)
			{
				IList<File> files = Arrays.Stream(testFiles.Split(",")).Map(null).Collect(Collectors.ToList());
				if (crf.flags.printProbs)
				{
					crf.PrintProbs(files, crf.DefaultReaderAndWriter());
				}
				else
				{
					crf.ClassifyFilesAndWriteAnswers(files, crf.DefaultReaderAndWriter(), true);
				}
			}
			if (textFile != null)
			{
				crf.ClassifyAndWriteAnswers(textFile, crf.PlainTextReaderAndWriter(), false);
			}
			if (textFiles != null)
			{
				IList<File> files = Arrays.Stream(textFiles.Split(",")).Map(null).Collect(Collectors.ToList());
				crf.ClassifyFilesAndWriteAnswers(files);
			}
			if (crf.flags.readStdin)
			{
				crf.ClassifyStdin();
			}
		}
		// end main
	}
}
