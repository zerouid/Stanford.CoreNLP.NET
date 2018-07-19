using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Neural;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util;
using Org.Ejml.Simple;
using Sharpen;

namespace Edu.Stanford.Nlp.Sentiment
{
	[System.Serializable]
	public class SentimentModel
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Sentiment.SentimentModel));

		/// <summary>Nx2N+1, where N is the size of the word vectors</summary>
		public readonly TwoDimensionalMap<string, string, SimpleMatrix> binaryTransform;

		/// <summary>2Nx2NxN, where N is the size of the word vectors</summary>
		public readonly TwoDimensionalMap<string, string, SimpleTensor> binaryTensors;

		/// <summary>CxN+1, where N = size of word vectors, C is the number of classes</summary>
		public readonly TwoDimensionalMap<string, string, SimpleMatrix> binaryClassification;

		/// <summary>CxN+1, where N = size of word vectors, C is the number of classes</summary>
		public readonly IDictionary<string, SimpleMatrix> unaryClassification;

		/// <summary>Map from vocabulary words to word vectors.</summary>
		/// <seealso cref="GetWordVector(string)"></seealso>
		public IDictionary<string, SimpleMatrix> wordVectors;

		/// <summary>How many classes the RNN is built to test against</summary>
		public readonly int numClasses;

		/// <summary>Dimension of hidden layers, size of word vectors, etc</summary>
		public readonly int numHid;

		/// <summary>
		/// Cached here for easy calculation of the model size;
		/// TwoDimensionalMap does not return that in O(1) time
		/// </summary>
		public readonly int numBinaryMatrices;

		/// <summary>How many elements a transformation matrix has</summary>
		public readonly int binaryTransformSize;

		/// <summary>How many elements the binary transformation tensors have</summary>
		public readonly int binaryTensorSize;

		/// <summary>How many elements a classification matrix has</summary>
		public readonly int binaryClassificationSize;

		/// <summary>
		/// Cached here for easy calculation of the model size;
		/// TwoDimensionalMap does not return that in O(1) time
		/// </summary>
		public readonly int numUnaryMatrices;

		/// <summary>How many elements a classification matrix has</summary>
		public readonly int unaryClassificationSize;

		/// <summary>we just keep this here for convenience</summary>
		[System.NonSerialized]
		internal SimpleMatrix identity;

		/// <summary>A random number generator - keeping it here lets us reproduce results</summary>
		internal readonly Random rand;

		internal const string UnknownWord = "*UNK*";

		/// <summary>Will store various options specific to this model</summary>
		internal readonly RNNOptions op;

		/*
		// An example of how you could read in old models with readObject to fix the serialization
		// You would first read in the old model, then reserialize it
		private void readObject(ObjectInputStream in)
		throws IOException, ClassNotFoundException
		{
		ObjectInputStream.GetField fields = in.readFields();
		binaryTransform = ErasureUtils.uncheckedCast(fields.get("binaryTransform", null));
		
		// transform binaryTensors
		binaryTensors = TwoDimensionalMap.treeMap();
		TwoDimensionalMap<String, String, edu.stanford.nlp.rnn.SimpleTensor> oldTensors = ErasureUtils.uncheckedCast(fields.get("binaryTensors", null));
		for (String first : oldTensors.firstKeySet()) {
		for (String second : oldTensors.get(first).keySet()) {
		binaryTensors.put(first, second, new SimpleTensor(oldTensors.get(first, second).slices));
		}
		}
		
		binaryClassification = ErasureUtils.uncheckedCast(fields.get("binaryClassification", null));
		unaryClassification = ErasureUtils.uncheckedCast(fields.get("unaryClassification", null));
		wordVectors = ErasureUtils.uncheckedCast(fields.get("wordVectors", null));
		
		if (fields.defaulted("numClasses")) {
		throw new RuntimeException();
		}
		numClasses = fields.get("numClasses", 0);
		
		if (fields.defaulted("numHid")) {
		throw new RuntimeException();
		}
		numHid = fields.get("numHid", 0);
		
		if (fields.defaulted("numBinaryMatrices")) {
		throw new RuntimeException();
		}
		numBinaryMatrices = fields.get("numBinaryMatrices", 0);
		
		if (fields.defaulted("binaryTransformSize")) {
		throw new RuntimeException();
		}
		binaryTransformSize = fields.get("binaryTransformSize", 0);
		
		if (fields.defaulted("binaryTensorSize")) {
		throw new RuntimeException();
		}
		binaryTensorSize = fields.get("binaryTensorSize", 0);
		
		if (fields.defaulted("binaryClassificationSize")) {
		throw new RuntimeException();
		}
		binaryClassificationSize = fields.get("binaryClassificationSize", 0);
		
		if (fields.defaulted("numUnaryMatrices")) {
		throw new RuntimeException();
		}
		numUnaryMatrices = fields.get("numUnaryMatrices", 0);
		
		if (fields.defaulted("unaryClassificationSize")) {
		throw new RuntimeException();
		}
		unaryClassificationSize = fields.get("unaryClassificationSize", 0);
		
		rand = ErasureUtils.uncheckedCast(fields.get("rand", null));
		op = ErasureUtils.uncheckedCast(fields.get("op", null));
		op.classNames = op.DEFAULT_CLASS_NAMES;
		op.equivalenceClasses = op.APPROXIMATE_EQUIVALENCE_CLASSES;
		op.equivalenceClassNames = op.DEFAULT_EQUIVALENCE_CLASS_NAMES;
		}
		*/
		/// <summary>
		/// Given single matrices and sets of options, create the
		/// corresponding SentimentModel.
		/// </summary>
		/// <remarks>
		/// Given single matrices and sets of options, create the
		/// corresponding SentimentModel.  Useful for creating a Java version
		/// of a model trained in some other manner, such as using the
		/// original Matlab code.
		/// </remarks>
		internal static Edu.Stanford.Nlp.Sentiment.SentimentModel ModelFromMatrices(SimpleMatrix W, SimpleMatrix Wcat, SimpleTensor Wt, IDictionary<string, SimpleMatrix> wordVectors, RNNOptions op)
		{
			if (!op.combineClassification || !op.simplifiedModel)
			{
				throw new ArgumentException("Can only create a model using this method if combineClassification and simplifiedModel are turned on");
			}
			TwoDimensionalMap<string, string, SimpleMatrix> binaryTransform = TwoDimensionalMap.TreeMap();
			binaryTransform.Put(string.Empty, string.Empty, W);
			TwoDimensionalMap<string, string, SimpleTensor> binaryTensors = TwoDimensionalMap.TreeMap();
			binaryTensors.Put(string.Empty, string.Empty, Wt);
			TwoDimensionalMap<string, string, SimpleMatrix> binaryClassification = TwoDimensionalMap.TreeMap();
			IDictionary<string, SimpleMatrix> unaryClassification = Generics.NewTreeMap();
			unaryClassification[string.Empty] = Wcat;
			return new Edu.Stanford.Nlp.Sentiment.SentimentModel(binaryTransform, binaryTensors, binaryClassification, unaryClassification, wordVectors, op);
		}

		private SentimentModel(TwoDimensionalMap<string, string, SimpleMatrix> binaryTransform, TwoDimensionalMap<string, string, SimpleTensor> binaryTensors, TwoDimensionalMap<string, string, SimpleMatrix> binaryClassification, IDictionary<string, 
			SimpleMatrix> unaryClassification, IDictionary<string, SimpleMatrix> wordVectors, RNNOptions op)
		{
			this.op = op;
			this.binaryTransform = binaryTransform;
			this.binaryTensors = binaryTensors;
			this.binaryClassification = binaryClassification;
			this.unaryClassification = unaryClassification;
			this.wordVectors = wordVectors;
			this.numClasses = op.numClasses;
			if (op.numHid <= 0)
			{
				int nh = 0;
				foreach (SimpleMatrix wv in wordVectors.Values)
				{
					nh = wv.GetNumElements();
				}
				this.numHid = nh;
			}
			else
			{
				this.numHid = op.numHid;
			}
			this.numBinaryMatrices = binaryTransform.Size();
			binaryTransformSize = numHid * (2 * numHid + 1);
			if (op.useTensors)
			{
				binaryTensorSize = numHid * numHid * numHid * 4;
			}
			else
			{
				binaryTensorSize = 0;
			}
			binaryClassificationSize = (op.combineClassification) ? 0 : numClasses * (numHid + 1);
			numUnaryMatrices = unaryClassification.Count;
			unaryClassificationSize = numClasses * (numHid + 1);
			rand = new Random(op.randomSeed);
			identity = SimpleMatrix.Identity(numHid);
		}

		/// <summary>The traditional way of initializing an empty model suitable for training.</summary>
		public SentimentModel(RNNOptions op, IList<Tree> trainingTrees)
		{
			this.op = op;
			rand = new Random(op.randomSeed);
			if (op.randomWordVectors)
			{
				InitRandomWordVectors(trainingTrees);
			}
			else
			{
				ReadWordVectors();
			}
			if (op.numHid > 0)
			{
				this.numHid = op.numHid;
			}
			else
			{
				int size = 0;
				foreach (SimpleMatrix vector in wordVectors.Values)
				{
					size = vector.GetNumElements();
					break;
				}
				this.numHid = size;
			}
			TwoDimensionalSet<string, string> binaryProductions = TwoDimensionalSet.HashSet();
			if (op.simplifiedModel)
			{
				binaryProductions.Add(string.Empty, string.Empty);
			}
			else
			{
				// TODO
				// figure out what binary productions we have in these trees
				// Note: the current sentiment training data does not actually
				// have any constituent labels
				throw new NotSupportedException("Not yet implemented");
			}
			ICollection<string> unaryProductions = Generics.NewHashSet();
			if (op.simplifiedModel)
			{
				unaryProductions.Add(string.Empty);
			}
			else
			{
				// TODO
				// figure out what unary productions we have in these trees (preterminals only, after the collapsing)
				throw new NotSupportedException("Not yet implemented");
			}
			this.numClasses = op.numClasses;
			identity = SimpleMatrix.Identity(numHid);
			binaryTransform = TwoDimensionalMap.TreeMap();
			binaryTensors = TwoDimensionalMap.TreeMap();
			binaryClassification = TwoDimensionalMap.TreeMap();
			// When making a flat model (no symantic untying) the
			// basicCategory function will return the same basic category for
			// all labels, so all entries will map to the same matrix
			foreach (Pair<string, string> binary in binaryProductions)
			{
				string left = BasicCategory(binary.first);
				string right = BasicCategory(binary.second);
				if (binaryTransform.Contains(left, right))
				{
					continue;
				}
				binaryTransform.Put(left, right, RandomTransformMatrix());
				if (op.useTensors)
				{
					binaryTensors.Put(left, right, RandomBinaryTensor());
				}
				if (!op.combineClassification)
				{
					binaryClassification.Put(left, right, RandomClassificationMatrix());
				}
			}
			numBinaryMatrices = binaryTransform.Size();
			binaryTransformSize = numHid * (2 * numHid + 1);
			if (op.useTensors)
			{
				binaryTensorSize = numHid * numHid * numHid * 4;
			}
			else
			{
				binaryTensorSize = 0;
			}
			binaryClassificationSize = (op.combineClassification) ? 0 : numClasses * (numHid + 1);
			unaryClassification = Generics.NewTreeMap();
			// When making a flat model (no symantic untying) the
			// basicCategory function will return the same basic category for
			// all labels, so all entries will map to the same matrix
			foreach (string unary in unaryProductions)
			{
				unary = BasicCategory(unary);
				if (unaryClassification.Contains(unary))
				{
					continue;
				}
				unaryClassification[unary] = RandomClassificationMatrix();
			}
			numUnaryMatrices = unaryClassification.Count;
			unaryClassificationSize = numClasses * (numHid + 1);
		}

		//log.info(this);
		/// <summary>Dumps *all* the matrices in a mostly readable format.</summary>
		public override string ToString()
		{
			StringBuilder output = new StringBuilder();
			if (binaryTransform.Size() > 0)
			{
				if (binaryTransform.Size() == 1)
				{
					output.Append("Binary transform matrix\n");
				}
				else
				{
					output.Append("Binary transform matrices\n");
				}
				foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> matrix in binaryTransform)
				{
					if (!matrix.GetFirstKey().Equals(string.Empty) || !matrix.GetSecondKey().Equals(string.Empty))
					{
						output.Append(matrix.GetFirstKey() + " " + matrix.GetSecondKey() + ":\n");
					}
					output.Append(NeuralUtils.ToString(matrix.GetValue(), "%.8f"));
				}
			}
			if (binaryTensors.Size() > 0)
			{
				if (binaryTensors.Size() == 1)
				{
					output.Append("Binary transform tensor\n");
				}
				else
				{
					output.Append("Binary transform tensors\n");
				}
				foreach (TwoDimensionalMap.Entry<string, string, SimpleTensor> matrix in binaryTensors)
				{
					if (!matrix.GetFirstKey().Equals(string.Empty) || !matrix.GetSecondKey().Equals(string.Empty))
					{
						output.Append(matrix.GetFirstKey() + " " + matrix.GetSecondKey() + ":\n");
					}
					output.Append(matrix.GetValue().ToString("%.8f"));
				}
			}
			if (binaryClassification.Size() > 0)
			{
				if (binaryClassification.Size() == 1)
				{
					output.Append("Binary classification matrix\n");
				}
				else
				{
					output.Append("Binary classification matrices\n");
				}
				foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> matrix in binaryClassification)
				{
					if (!matrix.GetFirstKey().Equals(string.Empty) || !matrix.GetSecondKey().Equals(string.Empty))
					{
						output.Append(matrix.GetFirstKey() + " " + matrix.GetSecondKey() + ":\n");
					}
					output.Append(NeuralUtils.ToString(matrix.GetValue(), "%.8f"));
				}
			}
			if (unaryClassification.Count > 0)
			{
				if (unaryClassification.Count == 1)
				{
					output.Append("Unary classification matrix\n");
				}
				else
				{
					output.Append("Unary classification matrices\n");
				}
				foreach (KeyValuePair<string, SimpleMatrix> matrix in unaryClassification)
				{
					if (!matrix.Key.Equals(string.Empty))
					{
						output.Append(matrix.Key + ":\n");
					}
					output.Append(NeuralUtils.ToString(matrix.Value, "%.8f"));
				}
			}
			output.Append("Word vectors\n");
			foreach (KeyValuePair<string, SimpleMatrix> matrix_1 in wordVectors)
			{
				output.Append("'" + matrix_1.Key + "'");
				output.Append("\n");
				output.Append(NeuralUtils.ToString(matrix_1.Value, "%.8f"));
				output.Append("\n");
			}
			return output.ToString();
		}

		internal virtual SimpleTensor RandomBinaryTensor()
		{
			double range = 1.0 / (4.0 * numHid);
			SimpleTensor tensor = SimpleTensor.Random(numHid * 2, numHid * 2, numHid, -range, range, rand);
			return tensor.Scale(op.trainOptions.scalingForInit);
		}

		internal virtual SimpleMatrix RandomTransformMatrix()
		{
			SimpleMatrix binary = new SimpleMatrix(numHid, numHid * 2 + 1);
			// bias column values are initialized zero
			binary.InsertIntoThis(0, 0, RandomTransformBlock());
			binary.InsertIntoThis(0, numHid, RandomTransformBlock());
			return binary.Scale(op.trainOptions.scalingForInit);
		}

		internal virtual SimpleMatrix RandomTransformBlock()
		{
			double range = 1.0 / (Math.Sqrt((double)numHid) * 2.0);
			return SimpleMatrix.Random(numHid, numHid, -range, range, rand).Plus(identity);
		}

		/// <summary>Returns matrices of the right size for either binary or unary (terminal) classification</summary>
		internal virtual SimpleMatrix RandomClassificationMatrix()
		{
			SimpleMatrix score = new SimpleMatrix(numClasses, numHid + 1);
			double range = 1.0 / (Math.Sqrt((double)numHid));
			score.InsertIntoThis(0, 0, SimpleMatrix.Random(numClasses, numHid, -range, range, rand));
			// bias column goes from 0 to 1 initially
			score.InsertIntoThis(0, numHid, SimpleMatrix.Random(numClasses, 1, 0.0, 1.0, rand));
			return score.Scale(op.trainOptions.scalingForInit);
		}

		internal virtual SimpleMatrix RandomWordVector()
		{
			return RandomWordVector(op.numHid, rand);
		}

		internal static SimpleMatrix RandomWordVector(int size, Random rand)
		{
			return NeuralUtils.RandomGaussian(size, 1, rand).Scale(0.1);
		}

		internal virtual void InitRandomWordVectors(IList<Tree> trainingTrees)
		{
			if (op.numHid == 0)
			{
				throw new Exception("Cannot create random word vectors for an unknown numHid");
			}
			ICollection<string> words = Generics.NewHashSet();
			words.Add(UnknownWord);
			foreach (Tree tree in trainingTrees)
			{
				IList<Tree> leaves = tree.GetLeaves();
				foreach (Tree leaf in leaves)
				{
					string word = leaf.Label().Value();
					if (op.lowercaseWordVectors)
					{
						word = word.ToLower();
					}
					words.Add(word);
				}
			}
			this.wordVectors = Generics.NewTreeMap();
			foreach (string word_1 in words)
			{
				SimpleMatrix vector = RandomWordVector();
				wordVectors[word_1] = vector;
			}
		}

		internal virtual void ReadWordVectors()
		{
			Embedding embedding = new Embedding(op.wordVectors, op.numHid);
			this.wordVectors = Generics.NewTreeMap();
			//    Map<String, SimpleMatrix> rawWordVectors = NeuralUtils.readRawWordVectors(op.wordVectors, op.numHid);
			//    for (String word : rawWordVectors.keySet()) {
			foreach (string word in embedding.KeySet())
			{
				// TODO: factor out unknown word vector code from DVParser
				wordVectors[word] = embedding.Get(word);
			}
			string unkWord = op.unkWord;
			SimpleMatrix unknownWordVector = wordVectors[unkWord];
			wordVectors[UnknownWord] = unknownWordVector;
			if (unknownWordVector == null)
			{
				throw new Exception("Unknown word vector not specified in the word vector file");
			}
		}

		public virtual int TotalParamSize()
		{
			int totalSize = 0;
			// binaryTensorSize was set to 0 if useTensors=false
			totalSize = numBinaryMatrices * (binaryTransformSize + binaryClassificationSize + binaryTensorSize);
			totalSize += numUnaryMatrices * unaryClassificationSize;
			totalSize += wordVectors.Count * numHid;
			return totalSize;
		}

		public virtual double[] ParamsToVector()
		{
			int totalSize = TotalParamSize();
			return NeuralUtils.ParamsToVector(totalSize, binaryTransform.ValueIterator(), binaryClassification.ValueIterator(), SimpleTensor.IteratorSimpleMatrix(binaryTensors.ValueIterator()), unaryClassification.Values.GetEnumerator(), wordVectors.Values
				.GetEnumerator());
		}

		public virtual void VectorToParams(double[] theta)
		{
			NeuralUtils.VectorToParams(theta, binaryTransform.ValueIterator(), binaryClassification.ValueIterator(), SimpleTensor.IteratorSimpleMatrix(binaryTensors.ValueIterator()), unaryClassification.Values.GetEnumerator(), wordVectors.Values.GetEnumerator
				());
		}

		// TODO: combine this and getClassWForNode?
		public virtual SimpleMatrix GetWForNode(Tree node)
		{
			if (node.Children().Length == 2)
			{
				string leftLabel = node.Children()[0].Value();
				string leftBasic = BasicCategory(leftLabel);
				string rightLabel = node.Children()[1].Value();
				string rightBasic = BasicCategory(rightLabel);
				return binaryTransform.Get(leftBasic, rightBasic);
			}
			else
			{
				if (node.Children().Length == 1)
				{
					throw new AssertionError("No unary transform matrices, only unary classification");
				}
				else
				{
					throw new AssertionError("Unexpected tree children size of " + node.Children().Length);
				}
			}
		}

		public virtual SimpleTensor GetTensorForNode(Tree node)
		{
			if (!op.useTensors)
			{
				throw new AssertionError("Not using tensors");
			}
			if (node.Children().Length == 2)
			{
				string leftLabel = node.Children()[0].Value();
				string leftBasic = BasicCategory(leftLabel);
				string rightLabel = node.Children()[1].Value();
				string rightBasic = BasicCategory(rightLabel);
				return binaryTensors.Get(leftBasic, rightBasic);
			}
			else
			{
				if (node.Children().Length == 1)
				{
					throw new AssertionError("No unary transform matrices, only unary classification");
				}
				else
				{
					throw new AssertionError("Unexpected tree children size of " + node.Children().Length);
				}
			}
		}

		public virtual SimpleMatrix GetClassWForNode(Tree node)
		{
			if (op.combineClassification)
			{
				return unaryClassification[string.Empty];
			}
			else
			{
				if (node.Children().Length == 2)
				{
					string leftLabel = node.Children()[0].Value();
					string leftBasic = BasicCategory(leftLabel);
					string rightLabel = node.Children()[1].Value();
					string rightBasic = BasicCategory(rightLabel);
					return binaryClassification.Get(leftBasic, rightBasic);
				}
				else
				{
					if (node.Children().Length == 1)
					{
						string unaryLabel = node.Children()[0].Value();
						string unaryBasic = BasicCategory(unaryLabel);
						return unaryClassification[unaryBasic];
					}
					else
					{
						throw new AssertionError("Unexpected tree children size of " + node.Children().Length);
					}
				}
			}
		}

		/// <summary>Retrieve a learned word vector for the given word.</summary>
		/// <remarks>
		/// Retrieve a learned word vector for the given word.
		/// If the word is OOV, returns a vector associated with an
		/// <c>&lt;unk&gt;</c>
		/// term.
		/// </remarks>
		public virtual SimpleMatrix GetWordVector(string word)
		{
			return wordVectors[GetVocabWord(word)];
		}

		/// <summary>Get the known vocabulary word associated with the given word.</summary>
		/// <returns>
		/// The form of the given word known by the model, or
		/// <see cref="UnknownWord"/>
		/// if this word has not been observed
		/// </returns>
		public virtual string GetVocabWord(string word)
		{
			if (op.lowercaseWordVectors)
			{
				word = word.ToLower();
			}
			if (wordVectors.Contains(word))
			{
				return word;
			}
			// TODO: go through unknown words here
			return UnknownWord;
		}

		public virtual string BasicCategory(string category)
		{
			if (op.simplifiedModel)
			{
				return string.Empty;
			}
			string basic = op.langpack.BasicCategory(category);
			if (basic.Length > 0 && basic[0] == '@')
			{
				basic = Sharpen.Runtime.Substring(basic, 1);
			}
			return basic;
		}

		public virtual SimpleMatrix GetUnaryClassification(string category)
		{
			category = BasicCategory(category);
			return unaryClassification[category];
		}

		public virtual SimpleMatrix GetBinaryClassification(string left, string right)
		{
			if (op.combineClassification)
			{
				return unaryClassification[string.Empty];
			}
			else
			{
				left = BasicCategory(left);
				right = BasicCategory(right);
				return binaryClassification.Get(left, right);
			}
		}

		public virtual SimpleMatrix GetBinaryTransform(string left, string right)
		{
			left = BasicCategory(left);
			right = BasicCategory(right);
			return binaryTransform.Get(left, right);
		}

		public virtual SimpleTensor GetBinaryTensor(string left, string right)
		{
			left = BasicCategory(left);
			right = BasicCategory(right);
			return binaryTensors.Get(left, right);
		}

		public virtual void SaveSerialized(string path)
		{
			try
			{
				IOUtils.WriteObjectToFile(this, path);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public static Edu.Stanford.Nlp.Sentiment.SentimentModel LoadSerialized(string path)
		{
			try
			{
				return IOUtils.ReadObjectFromURLOrClasspathOrFileSystem(path);
			}
			catch (Exception e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public virtual void PrintParamInformation(int index)
		{
			int curIndex = 0;
			foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> entry in binaryTransform)
			{
				if (curIndex <= index && curIndex + entry.GetValue().GetNumElements() > index)
				{
					log.Info("Index " + index + " is element " + (index - curIndex) + " of binaryTransform \"" + entry.GetFirstKey() + "," + entry.GetSecondKey() + "\"");
					return;
				}
				else
				{
					curIndex += entry.GetValue().GetNumElements();
				}
			}
			foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> entry_1 in binaryClassification)
			{
				if (curIndex <= index && curIndex + entry_1.GetValue().GetNumElements() > index)
				{
					log.Info("Index " + index + " is element " + (index - curIndex) + " of binaryClassification \"" + entry_1.GetFirstKey() + "," + entry_1.GetSecondKey() + "\"");
					return;
				}
				else
				{
					curIndex += entry_1.GetValue().GetNumElements();
				}
			}
			foreach (TwoDimensionalMap.Entry<string, string, SimpleTensor> entry_2 in binaryTensors)
			{
				if (curIndex <= index && curIndex + entry_2.GetValue().GetNumElements() > index)
				{
					log.Info("Index " + index + " is element " + (index - curIndex) + " of binaryTensor \"" + entry_2.GetFirstKey() + "," + entry_2.GetSecondKey() + "\"");
					return;
				}
				else
				{
					curIndex += entry_2.GetValue().GetNumElements();
				}
			}
			foreach (KeyValuePair<string, SimpleMatrix> entry_3 in unaryClassification)
			{
				if (curIndex <= index && curIndex + entry_3.Value.GetNumElements() > index)
				{
					log.Info("Index " + index + " is element " + (index - curIndex) + " of unaryClassification \"" + entry_3.Key + "\"");
					return;
				}
				else
				{
					curIndex += entry_3.Value.GetNumElements();
				}
			}
			foreach (KeyValuePair<string, SimpleMatrix> entry_4 in wordVectors)
			{
				if (curIndex <= index && curIndex + entry_4.Value.GetNumElements() > index)
				{
					log.Info("Index " + index + " is element " + (index - curIndex) + " of wordVector \"" + entry_4.Key + "\"");
					return;
				}
				else
				{
					curIndex += entry_4.Value.GetNumElements();
				}
			}
			log.Info("Index " + index + " is beyond the length of the parameters; total parameter space was " + TotalParamSize());
		}

		private const long serialVersionUID = 1;
	}
}
