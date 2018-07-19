using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Neural;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Java.Util.Regex;
using Org.Ejml.Data;
using Org.Ejml.Simple;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Dvparser
{
	[System.Serializable]
	public class DVModel
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Dvparser.DVModel));

		public TwoDimensionalMap<string, string, SimpleMatrix> binaryTransform;

		public IDictionary<string, SimpleMatrix> unaryTransform;

		public TwoDimensionalMap<string, string, SimpleMatrix> binaryScore;

		public IDictionary<string, SimpleMatrix> unaryScore;

		public IDictionary<string, SimpleMatrix> wordVectors;

		internal int numBinaryMatrices;

		internal int numUnaryMatrices;

		internal int binaryTransformSize;

		internal int unaryTransformSize;

		internal int binaryScoreSize;

		internal int unaryScoreSize;

		internal Options op;

		internal readonly int numCols;

		internal readonly int numRows;

		[System.NonSerialized]
		internal SimpleMatrix identity;

		internal Random rand;

		internal const string UnknownWord = "*UNK*";

		internal const string UnknownNumber = "*NUM*";

		internal const string UnknownCaps = "*CAPS*";

		internal const string UnknownChineseYear = "*ZH_YEAR*";

		internal const string UnknownChineseNumber = "*ZH_NUM*";

		internal const string UnknownChinesePercent = "*ZH_PERCENT*";

		internal const string StartWord = "*START*";

		internal const string EndWord = "*END*";

		private static readonly IFunction<SimpleMatrix, DenseMatrix64F> convertSimpleMatrix = null;

		private static readonly IFunction<DenseMatrix64F, SimpleMatrix> convertDenseMatrix = null;

		// Maps from basic category to the matrix transformation matrices for
		// binary nodes and unary nodes.
		// The indices are the children categories.  For binaryTransform, for
		// example, we have a matrix for each type of child that appears.
		// score matrices for each node type
		// cache these for easy calculation of "theta" parameter size
		// we just keep this here for convenience
		// the seed we used to use was 19580427
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private void ReadObject(ObjectInputStream @in)
		{
			@in.DefaultReadObject();
			identity = SimpleMatrix.Identity(numRows);
		}

		/// <param name="op">the parameters of the parser</param>
		public DVModel(Options op, IIndex<string> stateIndex, UnaryGrammar unaryGrammar, BinaryGrammar binaryGrammar)
		{
			this.op = op;
			rand = new Random(op.trainOptions.randomSeed);
			ReadWordVectors();
			// Binary matrices will be n*2n+1, unary matrices will be n*n+1
			numRows = op.lexOptions.numHid;
			numCols = op.lexOptions.numHid;
			// Build one matrix for each basic category.
			// We assume that each state that has the same basic
			// category is using the same transformation matrix.
			// Use TreeMap for because we want values to be
			// sorted by key later on when building theta vectors
			binaryTransform = TwoDimensionalMap.TreeMap();
			unaryTransform = Generics.NewTreeMap();
			binaryScore = TwoDimensionalMap.TreeMap();
			unaryScore = Generics.NewTreeMap();
			numBinaryMatrices = 0;
			numUnaryMatrices = 0;
			binaryTransformSize = numRows * (numCols * 2 + 1);
			unaryTransformSize = numRows * (numCols + 1);
			binaryScoreSize = numCols;
			unaryScoreSize = numCols;
			if (op.trainOptions.useContextWords)
			{
				binaryTransformSize += numRows * numCols * 2;
				unaryTransformSize += numRows * numCols * 2;
			}
			identity = SimpleMatrix.Identity(numRows);
			foreach (UnaryRule unaryRule in unaryGrammar)
			{
				// only make one matrix for each parent state, and only use the
				// basic category for that
				string childState = stateIndex.Get(unaryRule.child);
				string childBasic = BasicCategory(childState);
				AddRandomUnaryMatrix(childBasic);
			}
			foreach (BinaryRule binaryRule in binaryGrammar)
			{
				// only make one matrix for each parent state, and only use the
				// basic category for that
				string leftState = stateIndex.Get(binaryRule.leftChild);
				string leftBasic = BasicCategory(leftState);
				string rightState = stateIndex.Get(binaryRule.rightChild);
				string rightBasic = BasicCategory(rightState);
				AddRandomBinaryMatrix(leftBasic, rightBasic);
			}
		}

		public DVModel(TwoDimensionalMap<string, string, SimpleMatrix> binaryTransform, IDictionary<string, SimpleMatrix> unaryTransform, TwoDimensionalMap<string, string, SimpleMatrix> binaryScore, IDictionary<string, SimpleMatrix> unaryScore, IDictionary
			<string, SimpleMatrix> wordVectors, Options op)
		{
			this.op = op;
			this.binaryTransform = binaryTransform;
			this.unaryTransform = unaryTransform;
			this.binaryScore = binaryScore;
			this.unaryScore = unaryScore;
			this.wordVectors = wordVectors;
			this.numBinaryMatrices = binaryTransform.Size();
			this.numUnaryMatrices = unaryTransform.Count;
			if (numBinaryMatrices > 0)
			{
				this.binaryTransformSize = binaryTransform.GetEnumerator().Current.GetValue().GetNumElements();
				this.binaryScoreSize = binaryScore.GetEnumerator().Current.GetValue().GetNumElements();
			}
			else
			{
				this.binaryTransformSize = 0;
				this.binaryScoreSize = 0;
			}
			if (numUnaryMatrices > 0)
			{
				this.unaryTransformSize = unaryTransform.Values.GetEnumerator().Current.GetNumElements();
				this.unaryScoreSize = unaryScore.Values.GetEnumerator().Current.GetNumElements();
			}
			else
			{
				this.unaryTransformSize = 0;
				this.unaryScoreSize = 0;
			}
			this.numRows = op.lexOptions.numHid;
			this.numCols = op.lexOptions.numHid;
			this.identity = SimpleMatrix.Identity(numRows);
			this.rand = new Random(op.trainOptions.randomSeed);
		}

		/// <summary>Creates a random context matrix.</summary>
		/// <remarks>
		/// Creates a random context matrix.  This will be numRows x
		/// 2*numCols big.  These can be appended to the end of either a
		/// unary or binary transform matrix to get the transform matrix
		/// which uses context words.
		/// </remarks>
		private SimpleMatrix RandomContextMatrix()
		{
			SimpleMatrix matrix = new SimpleMatrix(numRows, numCols * 2);
			matrix.InsertIntoThis(0, 0, identity.Scale(op.trainOptions.scalingForInit * 0.1));
			matrix.InsertIntoThis(0, numCols, identity.Scale(op.trainOptions.scalingForInit * 0.1));
			matrix = matrix.Plus(SimpleMatrix.Random(numRows, numCols * 2, -1.0 / Math.Sqrt((double)numCols * 100.0), 1.0 / Math.Sqrt((double)numCols * 100.0), rand));
			return matrix;
		}

		/// <summary>
		/// Create a random transform matrix based on the initialization
		/// parameters.
		/// </summary>
		/// <remarks>
		/// Create a random transform matrix based on the initialization
		/// parameters.  This will be numRows x numCols big.  These can be
		/// plugged into either unary or binary transform matrices.
		/// </remarks>
		private SimpleMatrix RandomTransformMatrix()
		{
			SimpleMatrix matrix;
			switch (op.trainOptions.transformMatrixType)
			{
				case TrainOptions.TransformMatrixType.Diagonal:
				{
					matrix = SimpleMatrix.Random(numRows, numCols, -1.0 / Math.Sqrt((double)numCols * 100.0), 1.0 / Math.Sqrt((double)numCols * 100.0), rand).Plus(identity);
					break;
				}

				case TrainOptions.TransformMatrixType.Random:
				{
					matrix = SimpleMatrix.Random(numRows, numCols, -1.0 / Math.Sqrt((double)numCols), 1.0 / Math.Sqrt((double)numCols), rand);
					break;
				}

				case TrainOptions.TransformMatrixType.OffDiagonal:
				{
					matrix = SimpleMatrix.Random(numRows, numCols, -1.0 / Math.Sqrt((double)numCols * 100.0), 1.0 / Math.Sqrt((double)numCols * 100.0), rand).Plus(identity);
					for (int i = 0; i < numCols; ++i)
					{
						int x = rand.NextInt(numCols);
						int y = rand.NextInt(numCols);
						int scale = rand.NextInt(3) - 1;
						// -1, 0, or 1
						matrix.Set(x, y, matrix.Get(x, y) + scale);
					}
					break;
				}

				case TrainOptions.TransformMatrixType.RandomZeros:
				{
					matrix = SimpleMatrix.Random(numRows, numCols, -1.0 / Math.Sqrt((double)numCols * 100.0), 1.0 / Math.Sqrt((double)numCols * 100.0), rand).Plus(identity);
					for (int i_1 = 0; i_1 < numCols; ++i_1)
					{
						int x = rand.NextInt(numCols);
						int y = rand.NextInt(numCols);
						matrix.Set(x, y, 0.0);
					}
					break;
				}

				default:
				{
					throw new ArgumentException("Unexpected matrix initialization type " + op.trainOptions.transformMatrixType);
				}
			}
			return matrix;
		}

		public virtual void AddRandomUnaryMatrix(string childBasic)
		{
			if (unaryTransform[childBasic] != null)
			{
				return;
			}
			++numUnaryMatrices;
			// scoring matrix
			SimpleMatrix score = SimpleMatrix.Random(1, numCols, -1.0 / Math.Sqrt((double)numCols), 1.0 / Math.Sqrt((double)numCols), rand);
			unaryScore[childBasic] = score.Scale(op.trainOptions.scalingForInit);
			SimpleMatrix transform;
			if (op.trainOptions.useContextWords)
			{
				transform = new SimpleMatrix(numRows, numCols * 3 + 1);
				// leave room for bias term
				transform.InsertIntoThis(0, numCols + 1, RandomContextMatrix());
			}
			else
			{
				transform = new SimpleMatrix(numRows, numCols + 1);
			}
			SimpleMatrix unary = RandomTransformMatrix();
			transform.InsertIntoThis(0, 0, unary);
			unaryTransform[childBasic] = transform.Scale(op.trainOptions.scalingForInit);
		}

		public virtual void AddRandomBinaryMatrix(string leftBasic, string rightBasic)
		{
			if (binaryTransform.Get(leftBasic, rightBasic) != null)
			{
				return;
			}
			++numBinaryMatrices;
			// scoring matrix
			SimpleMatrix score = SimpleMatrix.Random(1, numCols, -1.0 / Math.Sqrt((double)numCols), 1.0 / Math.Sqrt((double)numCols), rand);
			binaryScore.Put(leftBasic, rightBasic, score.Scale(op.trainOptions.scalingForInit));
			SimpleMatrix binary;
			if (op.trainOptions.useContextWords)
			{
				binary = new SimpleMatrix(numRows, numCols * 4 + 1);
				// leave room for bias term
				binary.InsertIntoThis(0, numCols * 2 + 1, RandomContextMatrix());
			}
			else
			{
				binary = new SimpleMatrix(numRows, numCols * 2 + 1);
			}
			SimpleMatrix left = RandomTransformMatrix();
			SimpleMatrix right = RandomTransformMatrix();
			binary.InsertIntoThis(0, 0, left);
			binary.InsertIntoThis(0, numCols, right);
			binaryTransform.Put(leftBasic, rightBasic, binary.Scale(op.trainOptions.scalingForInit));
		}

		public virtual void SetRulesForTrainingSet(IList<Tree> sentences, IDictionary<Tree, byte[]> compressedTrees)
		{
			TwoDimensionalSet<string, string> binaryRules = TwoDimensionalSet.TreeSet();
			ICollection<string> unaryRules = new HashSet<string>();
			ICollection<string> words = new HashSet<string>();
			foreach (Tree sentence in sentences)
			{
				SearchRulesForBatch(binaryRules, unaryRules, words, sentence);
				foreach (Tree hypothesis in CacheParseHypotheses.ConvertToTrees(compressedTrees[sentence]))
				{
					SearchRulesForBatch(binaryRules, unaryRules, words, hypothesis);
				}
			}
			foreach (Pair<string, string> binary in binaryRules)
			{
				AddRandomBinaryMatrix(binary.first, binary.second);
			}
			foreach (string unary in unaryRules)
			{
				AddRandomUnaryMatrix(unary);
			}
			FilterRulesForBatch(binaryRules, unaryRules, words);
		}

		/// <summary>
		/// Filters the transform and score rules so that we only have the
		/// ones which appear in the trees given
		/// </summary>
		public virtual void FilterRulesForBatch(ICollection<Tree> trees)
		{
			TwoDimensionalSet<string, string> binaryRules = TwoDimensionalSet.TreeSet();
			ICollection<string> unaryRules = new HashSet<string>();
			ICollection<string> words = new HashSet<string>();
			foreach (Tree tree in trees)
			{
				SearchRulesForBatch(binaryRules, unaryRules, words, tree);
			}
			FilterRulesForBatch(binaryRules, unaryRules, words);
		}

		public virtual void FilterRulesForBatch(IDictionary<Tree, byte[]> compressedTrees)
		{
			TwoDimensionalSet<string, string> binaryRules = TwoDimensionalSet.TreeSet();
			ICollection<string> unaryRules = new HashSet<string>();
			ICollection<string> words = new HashSet<string>();
			foreach (KeyValuePair<Tree, byte[]> entry in compressedTrees)
			{
				SearchRulesForBatch(binaryRules, unaryRules, words, entry.Key);
				foreach (Tree hypothesis in CacheParseHypotheses.ConvertToTrees(entry.Value))
				{
					SearchRulesForBatch(binaryRules, unaryRules, words, hypothesis);
				}
			}
			FilterRulesForBatch(binaryRules, unaryRules, words);
		}

		public virtual void FilterRulesForBatch(TwoDimensionalSet<string, string> binaryRules, ICollection<string> unaryRules, ICollection<string> words)
		{
			TwoDimensionalMap<string, string, SimpleMatrix> newBinaryTransforms = TwoDimensionalMap.TreeMap();
			TwoDimensionalMap<string, string, SimpleMatrix> newBinaryScores = TwoDimensionalMap.TreeMap();
			foreach (Pair<string, string> binaryRule in binaryRules)
			{
				SimpleMatrix transform = binaryTransform.Get(binaryRule.First(), binaryRule.Second());
				if (transform != null)
				{
					newBinaryTransforms.Put(binaryRule.First(), binaryRule.Second(), transform);
				}
				SimpleMatrix score = binaryScore.Get(binaryRule.First(), binaryRule.Second());
				if (score != null)
				{
					newBinaryScores.Put(binaryRule.First(), binaryRule.Second(), score);
				}
				if ((transform == null && score != null) || (transform != null && score == null))
				{
					throw new AssertionError();
				}
			}
			binaryTransform = newBinaryTransforms;
			binaryScore = newBinaryScores;
			numBinaryMatrices = binaryTransform.Size();
			IDictionary<string, SimpleMatrix> newUnaryTransforms = Generics.NewTreeMap();
			IDictionary<string, SimpleMatrix> newUnaryScores = Generics.NewTreeMap();
			foreach (string unaryRule in unaryRules)
			{
				SimpleMatrix transform = unaryTransform[unaryRule];
				if (transform != null)
				{
					newUnaryTransforms[unaryRule] = transform;
				}
				SimpleMatrix score = unaryScore[unaryRule];
				if (score != null)
				{
					newUnaryScores[unaryRule] = score;
				}
				if ((transform == null && score != null) || (transform != null && score == null))
				{
					throw new AssertionError();
				}
			}
			unaryTransform = newUnaryTransforms;
			unaryScore = newUnaryScores;
			numUnaryMatrices = unaryTransform.Count;
			IDictionary<string, SimpleMatrix> newWordVectors = Generics.NewTreeMap();
			foreach (string word in words)
			{
				SimpleMatrix wordVector = wordVectors[word];
				if (wordVector != null)
				{
					newWordVectors[word] = wordVector;
				}
			}
			wordVectors = newWordVectors;
		}

		private void SearchRulesForBatch(TwoDimensionalSet<string, string> binaryRules, ICollection<string> unaryRules, ICollection<string> words, Tree tree)
		{
			if (tree.IsLeaf())
			{
				return;
			}
			if (tree.IsPreTerminal())
			{
				words.Add(GetVocabWord(tree.Children()[0].Value()));
				return;
			}
			Tree[] children = tree.Children();
			if (children.Length == 1)
			{
				unaryRules.Add(BasicCategory(children[0].Value()));
				SearchRulesForBatch(binaryRules, unaryRules, words, children[0]);
			}
			else
			{
				if (children.Length == 2)
				{
					binaryRules.Add(BasicCategory(children[0].Value()), BasicCategory(children[1].Value()));
					SearchRulesForBatch(binaryRules, unaryRules, words, children[0]);
					SearchRulesForBatch(binaryRules, unaryRules, words, children[1]);
				}
				else
				{
					throw new AssertionError("Expected a binarized tree");
				}
			}
		}

		public virtual string BasicCategory(string category)
		{
			if (op.trainOptions.dvSimplifiedModel)
			{
				return string.Empty;
			}
			else
			{
				string basic = op.Langpack().BasicCategory(category);
				// TODO: if we can figure out what is going on with the grammar
				// compaction, perhaps we don't want this any more
				if (basic.Length > 0 && basic[0] == '@')
				{
					basic = Sharpen.Runtime.Substring(basic, 1);
				}
				return basic;
			}
		}

		internal static readonly Pattern NumberPattern = Pattern.Compile("-?[0-9][-0-9,.:]*");

		internal static readonly Pattern CapsPattern = Pattern.Compile("[a-zA-Z]*[A-Z][a-zA-Z]*");

		internal static readonly Pattern ChineseYearPattern = Pattern.Compile("[〇零一二三四五六七八九０１２３４５６７８９]{4}+年");

		internal static readonly Pattern ChineseNumberPattern = Pattern.Compile("(?:[〇０零一二三四五六七八九０１２３４５６７８９十百万千亿]+[点多]?)+");

		internal static readonly Pattern ChinesePercentPattern = Pattern.Compile("百分之[〇０零一二三四五六七八九０１２３４５６７８９十点]+");

		/// <summary>Some word vectors are trained with DG representing number.</summary>
		/// <remarks>
		/// Some word vectors are trained with DG representing number.
		/// We mix all of those into the unknown number vectors.
		/// </remarks>
		internal static readonly Pattern DgPattern = Pattern.Compile(".*DG.*");

		public virtual void ReadWordVectors()
		{
			SimpleMatrix unknownNumberVector = null;
			SimpleMatrix unknownCapsVector = null;
			SimpleMatrix unknownChineseYearVector = null;
			SimpleMatrix unknownChineseNumberVector = null;
			SimpleMatrix unknownChinesePercentVector = null;
			wordVectors = Generics.NewTreeMap();
			int numberCount = 0;
			int capsCount = 0;
			int chineseYearCount = 0;
			int chineseNumberCount = 0;
			int chinesePercentCount = 0;
			//Map<String, SimpleMatrix> rawWordVectors = NeuralUtils.readRawWordVectors(op.lexOptions.wordVectorFile, op.lexOptions.numHid);
			Embedding rawWordVectors = new Embedding(op.lexOptions.wordVectorFile, op.lexOptions.numHid);
			foreach (string word in rawWordVectors.KeySet())
			{
				SimpleMatrix vector = rawWordVectors.Get(word);
				if (op.wordFunction != null)
				{
					word = op.wordFunction.Apply(word);
				}
				wordVectors[word] = vector;
				if (op.lexOptions.numHid <= 0)
				{
					op.lexOptions.numHid = vector.GetNumElements();
				}
				// TODO: factor out all of these identical blobs
				if (op.trainOptions.unknownNumberVector && (NumberPattern.Matcher(word).Matches() || DgPattern.Matcher(word).Matches()))
				{
					++numberCount;
					if (unknownNumberVector == null)
					{
						unknownNumberVector = new SimpleMatrix(vector);
					}
					else
					{
						unknownNumberVector = unknownNumberVector.Plus(vector);
					}
				}
				if (op.trainOptions.unknownCapsVector && CapsPattern.Matcher(word).Matches())
				{
					++capsCount;
					if (unknownCapsVector == null)
					{
						unknownCapsVector = new SimpleMatrix(vector);
					}
					else
					{
						unknownCapsVector = unknownCapsVector.Plus(vector);
					}
				}
				if (op.trainOptions.unknownChineseYearVector && ChineseYearPattern.Matcher(word).Matches())
				{
					++chineseYearCount;
					if (unknownChineseYearVector == null)
					{
						unknownChineseYearVector = new SimpleMatrix(vector);
					}
					else
					{
						unknownChineseYearVector = unknownChineseYearVector.Plus(vector);
					}
				}
				if (op.trainOptions.unknownChineseNumberVector && (ChineseNumberPattern.Matcher(word).Matches() || DgPattern.Matcher(word).Matches()))
				{
					++chineseNumberCount;
					if (unknownChineseNumberVector == null)
					{
						unknownChineseNumberVector = new SimpleMatrix(vector);
					}
					else
					{
						unknownChineseNumberVector = unknownChineseNumberVector.Plus(vector);
					}
				}
				if (op.trainOptions.unknownChinesePercentVector && ChinesePercentPattern.Matcher(word).Matches())
				{
					++chinesePercentCount;
					if (unknownChinesePercentVector == null)
					{
						unknownChinesePercentVector = new SimpleMatrix(vector);
					}
					else
					{
						unknownChinesePercentVector = unknownChinesePercentVector.Plus(vector);
					}
				}
			}
			string unkWord = op.trainOptions.unkWord;
			if (op.wordFunction != null)
			{
				unkWord = op.wordFunction.Apply(unkWord);
			}
			SimpleMatrix unknownWordVector = wordVectors[unkWord];
			wordVectors[UnknownWord] = unknownWordVector;
			if (unknownWordVector == null)
			{
				throw new Exception("Unknown word vector not specified in the word vector file");
			}
			if (op.trainOptions.unknownNumberVector)
			{
				if (numberCount > 0)
				{
					unknownNumberVector = unknownNumberVector.Divide(numberCount);
				}
				else
				{
					unknownNumberVector = new SimpleMatrix(unknownWordVector);
				}
				wordVectors[UnknownNumber] = unknownNumberVector;
			}
			if (op.trainOptions.unknownCapsVector)
			{
				if (capsCount > 0)
				{
					unknownCapsVector = unknownCapsVector.Divide(capsCount);
				}
				else
				{
					unknownCapsVector = new SimpleMatrix(unknownWordVector);
				}
				wordVectors[UnknownCaps] = unknownCapsVector;
			}
			if (op.trainOptions.unknownChineseYearVector)
			{
				log.Info("Matched " + chineseYearCount + " chinese year vectors");
				if (chineseYearCount > 0)
				{
					unknownChineseYearVector = unknownChineseYearVector.Divide(chineseYearCount);
				}
				else
				{
					unknownChineseYearVector = new SimpleMatrix(unknownWordVector);
				}
				wordVectors[UnknownChineseYear] = unknownChineseYearVector;
			}
			if (op.trainOptions.unknownChineseNumberVector)
			{
				log.Info("Matched " + chineseNumberCount + " chinese number vectors");
				if (chineseNumberCount > 0)
				{
					unknownChineseNumberVector = unknownChineseNumberVector.Divide(chineseNumberCount);
				}
				else
				{
					unknownChineseNumberVector = new SimpleMatrix(unknownWordVector);
				}
				wordVectors[UnknownChineseNumber] = unknownChineseNumberVector;
			}
			if (op.trainOptions.unknownChinesePercentVector)
			{
				log.Info("Matched " + chinesePercentCount + " chinese percent vectors");
				if (chinesePercentCount > 0)
				{
					unknownChinesePercentVector = unknownChinesePercentVector.Divide(chinesePercentCount);
				}
				else
				{
					unknownChinesePercentVector = new SimpleMatrix(unknownWordVector);
				}
				wordVectors[UnknownChinesePercent] = unknownChinesePercentVector;
			}
			if (op.trainOptions.useContextWords)
			{
				SimpleMatrix start = SimpleMatrix.Random(op.lexOptions.numHid, 1, -0.5, 0.5, rand);
				SimpleMatrix end = SimpleMatrix.Random(op.lexOptions.numHid, 1, -0.5, 0.5, rand);
				wordVectors[StartWord] = start;
				wordVectors[EndWord] = end;
			}
		}

		public virtual int TotalParamSize()
		{
			int totalSize = 0;
			totalSize += numBinaryMatrices * (binaryTransformSize + binaryScoreSize);
			totalSize += numUnaryMatrices * (unaryTransformSize + unaryScoreSize);
			if (op.trainOptions.trainWordVectors)
			{
				totalSize += wordVectors.Count * op.lexOptions.numHid;
			}
			return totalSize;
		}

		public virtual double[] ParamsToVector(double scale)
		{
			int totalSize = TotalParamSize();
			if (op.trainOptions.trainWordVectors)
			{
				return NeuralUtils.ParamsToVector(scale, totalSize, binaryTransform.ValueIterator(), unaryTransform.Values.GetEnumerator(), binaryScore.ValueIterator(), unaryScore.Values.GetEnumerator(), wordVectors.Values.GetEnumerator());
			}
			else
			{
				return NeuralUtils.ParamsToVector(scale, totalSize, binaryTransform.ValueIterator(), unaryTransform.Values.GetEnumerator(), binaryScore.ValueIterator(), unaryScore.Values.GetEnumerator());
			}
		}

		public virtual double[] ParamsToVector()
		{
			int totalSize = TotalParamSize();
			if (op.trainOptions.trainWordVectors)
			{
				return NeuralUtils.ParamsToVector(totalSize, binaryTransform.ValueIterator(), unaryTransform.Values.GetEnumerator(), binaryScore.ValueIterator(), unaryScore.Values.GetEnumerator(), wordVectors.Values.GetEnumerator());
			}
			else
			{
				return NeuralUtils.ParamsToVector(totalSize, binaryTransform.ValueIterator(), unaryTransform.Values.GetEnumerator(), binaryScore.ValueIterator(), unaryScore.Values.GetEnumerator());
			}
		}

		public virtual void VectorToParams(double[] theta)
		{
			if (op.trainOptions.trainWordVectors)
			{
				NeuralUtils.VectorToParams(theta, binaryTransform.ValueIterator(), unaryTransform.Values.GetEnumerator(), binaryScore.ValueIterator(), unaryScore.Values.GetEnumerator(), wordVectors.Values.GetEnumerator());
			}
			else
			{
				NeuralUtils.VectorToParams(theta, binaryTransform.ValueIterator(), unaryTransform.Values.GetEnumerator(), binaryScore.ValueIterator(), unaryScore.Values.GetEnumerator());
			}
		}

		public virtual SimpleMatrix GetWForNode(Tree node)
		{
			if (node.Children().Length == 1)
			{
				string childLabel = node.Children()[0].Value();
				string childBasic = BasicCategory(childLabel);
				return unaryTransform[childBasic];
			}
			else
			{
				if (node.Children().Length == 2)
				{
					string leftLabel = node.Children()[0].Value();
					string leftBasic = BasicCategory(leftLabel);
					string rightLabel = node.Children()[1].Value();
					string rightBasic = BasicCategory(rightLabel);
					return binaryTransform.Get(leftBasic, rightBasic);
				}
			}
			throw new AssertionError("Should only have unary or binary nodes");
		}

		public virtual SimpleMatrix GetScoreWForNode(Tree node)
		{
			if (node.Children().Length == 1)
			{
				string childLabel = node.Children()[0].Value();
				string childBasic = BasicCategory(childLabel);
				return unaryScore[childBasic];
			}
			else
			{
				if (node.Children().Length == 2)
				{
					string leftLabel = node.Children()[0].Value();
					string leftBasic = BasicCategory(leftLabel);
					string rightLabel = node.Children()[1].Value();
					string rightBasic = BasicCategory(rightLabel);
					return binaryScore.Get(leftBasic, rightBasic);
				}
			}
			throw new AssertionError("Should only have unary or binary nodes");
		}

		public virtual SimpleMatrix GetStartWordVector()
		{
			return wordVectors[StartWord];
		}

		public virtual SimpleMatrix GetEndWordVector()
		{
			return wordVectors[EndWord];
		}

		public virtual SimpleMatrix GetWordVector(string word)
		{
			return wordVectors[GetVocabWord(word)];
		}

		public virtual string GetVocabWord(string word)
		{
			if (op.wordFunction != null)
			{
				word = op.wordFunction.Apply(word);
			}
			if (op.trainOptions.lowercaseWordVectors)
			{
				word = word.ToLower();
			}
			if (wordVectors.Contains(word))
			{
				return word;
			}
			//log.info("Unknown word: [" + word + "]");
			if (op.trainOptions.unknownNumberVector && NumberPattern.Matcher(word).Matches())
			{
				return UnknownNumber;
			}
			if (op.trainOptions.unknownCapsVector && CapsPattern.Matcher(word).Matches())
			{
				return UnknownCaps;
			}
			if (op.trainOptions.unknownChineseYearVector && ChineseYearPattern.Matcher(word).Matches())
			{
				return UnknownChineseYear;
			}
			if (op.trainOptions.unknownChineseNumberVector && ChineseNumberPattern.Matcher(word).Matches())
			{
				return UnknownChineseNumber;
			}
			if (op.trainOptions.unknownChinesePercentVector && ChinesePercentPattern.Matcher(word).Matches())
			{
				return UnknownChinesePercent;
			}
			if (op.trainOptions.unknownDashedWordVectors)
			{
				int index = word.LastIndexOf('-');
				if (index >= 0 && index < word.Length)
				{
					string lastPiece = Sharpen.Runtime.Substring(word, index + 1);
					string wv = GetVocabWord(lastPiece);
					if (wv != null)
					{
						return wv;
					}
				}
			}
			return UnknownWord;
		}

		public virtual SimpleMatrix GetUnknownWordVector()
		{
			return wordVectors[UnknownWord];
		}

		public virtual void PrintMatrixNames(TextWriter @out)
		{
			@out.WriteLine("Binary matrices:");
			foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> binary in binaryTransform)
			{
				@out.WriteLine("  " + binary.GetFirstKey() + ":" + binary.GetSecondKey());
			}
			@out.WriteLine("Unary matrices:");
			foreach (string unary in unaryTransform.Keys)
			{
				@out.WriteLine("  " + unary);
			}
		}

		public virtual void PrintMatrixStats(TextWriter @out)
		{
			log.Info("Model loaded with " + numUnaryMatrices + " unary and " + numBinaryMatrices + " binary");
			foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> binary in binaryTransform)
			{
				@out.WriteLine("Binary transform " + binary.GetFirstKey() + ":" + binary.GetSecondKey());
				double normf = binary.GetValue().NormF();
				@out.WriteLine("  Total norm " + (normf * normf));
				normf = binary.GetValue().ExtractMatrix(0, op.lexOptions.numHid, 0, op.lexOptions.numHid).NormF();
				@out.WriteLine("  Left norm (" + binary.GetFirstKey() + ") " + (normf * normf));
				normf = binary.GetValue().ExtractMatrix(0, op.lexOptions.numHid, op.lexOptions.numHid, op.lexOptions.numHid * 2).NormF();
				@out.WriteLine("  Right norm (" + binary.GetSecondKey() + ") " + (normf * normf));
			}
		}

		public virtual void PrintAllMatrices(TextWriter @out)
		{
			foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> binary in binaryTransform)
			{
				@out.WriteLine("Binary transform " + binary.GetFirstKey() + ":" + binary.GetSecondKey());
				@out.WriteLine(binary.GetValue());
			}
			foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> binary_1 in binaryScore)
			{
				@out.WriteLine("Binary score " + binary_1.GetFirstKey() + ":" + binary_1.GetSecondKey());
				@out.WriteLine(binary_1.GetValue());
			}
			foreach (KeyValuePair<string, SimpleMatrix> unary in unaryTransform)
			{
				@out.WriteLine("Unary transform " + unary.Key);
				@out.WriteLine(unary.Value);
			}
			foreach (KeyValuePair<string, SimpleMatrix> unary_1 in unaryScore)
			{
				@out.WriteLine("Unary score " + unary_1.Key);
				@out.WriteLine(unary_1.Value);
			}
		}

		public virtual int BinaryTransformIndex(string leftChild, string rightChild)
		{
			int pos = 0;
			foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> binary in binaryTransform)
			{
				if (binary.GetFirstKey().Equals(leftChild) && binary.GetSecondKey().Equals(rightChild))
				{
					return pos;
				}
				pos += binary.GetValue().GetNumElements();
			}
			return -1;
		}

		public virtual int UnaryTransformIndex(string child)
		{
			int pos = binaryTransformSize * numBinaryMatrices;
			foreach (KeyValuePair<string, SimpleMatrix> unary in unaryTransform)
			{
				if (unary.Key.Equals(child))
				{
					return pos;
				}
				pos += unary.Value.GetNumElements();
			}
			return -1;
		}

		public virtual int BinaryScoreIndex(string leftChild, string rightChild)
		{
			int pos = binaryTransformSize * numBinaryMatrices + unaryTransformSize * numUnaryMatrices;
			foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> binary in binaryScore)
			{
				if (binary.GetFirstKey().Equals(leftChild) && binary.GetSecondKey().Equals(rightChild))
				{
					return pos;
				}
				pos += binary.GetValue().GetNumElements();
			}
			return -1;
		}

		public virtual int UnaryScoreIndex(string child)
		{
			int pos = (binaryTransformSize + binaryScoreSize) * numBinaryMatrices + unaryTransformSize * numUnaryMatrices;
			foreach (KeyValuePair<string, SimpleMatrix> unary in unaryScore)
			{
				if (unary.Key.Equals(child))
				{
					return pos;
				}
				pos += unary.Value.GetNumElements();
			}
			return -1;
		}

		public virtual Pair<string, string> IndexToBinaryTransform(int pos)
		{
			if (pos < numBinaryMatrices * binaryTransformSize)
			{
				foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> entry in binaryTransform)
				{
					if (binaryTransformSize < pos)
					{
						pos -= binaryTransformSize;
					}
					else
					{
						return Pair.MakePair(entry.GetFirstKey(), entry.GetSecondKey());
					}
				}
			}
			return null;
		}

		public virtual string IndexToUnaryTransform(int pos)
		{
			pos -= numBinaryMatrices * binaryTransformSize;
			if (pos < numUnaryMatrices * unaryTransformSize && pos >= 0)
			{
				foreach (KeyValuePair<string, SimpleMatrix> entry in unaryTransform)
				{
					if (unaryTransformSize < pos)
					{
						pos -= unaryTransformSize;
					}
					else
					{
						return entry.Key;
					}
				}
			}
			return null;
		}

		public virtual Pair<string, string> IndexToBinaryScore(int pos)
		{
			pos -= (numBinaryMatrices * binaryTransformSize + numUnaryMatrices * unaryTransformSize);
			if (pos < numBinaryMatrices * binaryScoreSize && pos >= 0)
			{
				foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> entry in binaryScore)
				{
					if (binaryScoreSize < pos)
					{
						pos -= binaryScoreSize;
					}
					else
					{
						return Pair.MakePair(entry.GetFirstKey(), entry.GetSecondKey());
					}
				}
			}
			return null;
		}

		public virtual string IndexToUnaryScore(int pos)
		{
			pos -= (numBinaryMatrices * (binaryTransformSize + binaryScoreSize) + numUnaryMatrices * unaryTransformSize);
			if (pos < numUnaryMatrices * unaryScoreSize && pos >= 0)
			{
				foreach (KeyValuePair<string, SimpleMatrix> entry in unaryScore)
				{
					if (unaryScoreSize < pos)
					{
						pos -= unaryScoreSize;
					}
					else
					{
						return entry.Key;
					}
				}
			}
			return null;
		}

		/// <summary>Prints to stdout the type and key for the given location in the parameter stack</summary>
		public virtual void PrintParameterType(int pos, TextWriter @out)
		{
			int originalPos = pos;
			Pair<string, string> binary = IndexToBinaryTransform(pos);
			if (binary != null)
			{
				pos = pos % binaryTransformSize;
				@out.WriteLine("Entry " + originalPos + " is entry " + pos + " of binary transform " + binary.First() + ":" + binary.Second());
				return;
			}
			string unary = IndexToUnaryTransform(pos);
			if (unary != null)
			{
				pos = (pos - numBinaryMatrices * binaryTransformSize) % unaryTransformSize;
				@out.WriteLine("Entry " + originalPos + " is entry " + pos + " of unary transform " + unary);
				return;
			}
			binary = IndexToBinaryScore(pos);
			if (binary != null)
			{
				pos = (pos - numBinaryMatrices * binaryTransformSize - numUnaryMatrices * unaryTransformSize) % binaryScoreSize;
				@out.WriteLine("Entry " + originalPos + " is entry " + pos + " of binary score " + binary.First() + ":" + binary.Second());
				return;
			}
			unary = IndexToUnaryScore(pos);
			if (unary != null)
			{
				pos = (pos - (numBinaryMatrices * (binaryTransformSize + binaryScoreSize)) - numUnaryMatrices * unaryTransformSize) % unaryScoreSize;
				@out.WriteLine("Entry " + originalPos + " is entry " + pos + " of unary score " + unary);
				return;
			}
			@out.WriteLine("Index " + originalPos + " unknown");
		}

		private const long serialVersionUID = 1;
	}
}
