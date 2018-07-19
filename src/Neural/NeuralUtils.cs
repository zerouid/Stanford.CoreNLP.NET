using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Org.Ejml.Ops;
using Org.Ejml.Simple;
using Sharpen;

namespace Edu.Stanford.Nlp.Neural
{
	/// <summary>
	/// Includes a bunch of utility methods usable by projects which use
	/// RNN, such as the parser and sentiment models.
	/// </summary>
	/// <remarks>
	/// Includes a bunch of utility methods usable by projects which use
	/// RNN, such as the parser and sentiment models.  Some methods convert
	/// iterators of SimpleMatrix objects to and from a vector.  Others are
	/// general utility methods on SimpleMatrix objects.
	/// </remarks>
	/// <author>John Bauer</author>
	/// <author>Richard Socher</author>
	/// <author>Thang Luong</author>
	/// <author>Kevin Clark</author>
	public class NeuralUtils
	{
		private NeuralUtils()
		{
		}

		// static methods only
		/// <summary>Convert a file into a text matrix.</summary>
		/// <remarks>
		/// Convert a file into a text matrix.  The expected format one row
		/// per line, one entry per column.  Not too efficient for large
		/// matrices, but you shouldn't store large matrices in text files
		/// anyway.  This specific format is not supported by ejml, which
		/// expects the number of rows and columns in its text matrices.
		/// </remarks>
		public static SimpleMatrix LoadTextMatrix(string path)
		{
			return ConvertTextMatrix(IOUtils.SlurpFileNoExceptions(path));
		}

		/// <summary>Convert a file into a text matrix.</summary>
		/// <remarks>
		/// Convert a file into a text matrix.  The expected format one row
		/// per line, one entry per column.  Not too efficient for large
		/// matrices, but you shouldn't store large matrices in text files
		/// anyway.  This specific format is not supported by ejml, which
		/// expects the number of rows and columns in its text matrices.
		/// </remarks>
		public static SimpleMatrix LoadTextMatrix(File file)
		{
			return ConvertTextMatrix(IOUtils.SlurpFileNoExceptions(file));
		}

		/// <summary>Convert a file into a list of matrices.</summary>
		/// <remarks>
		/// Convert a file into a list of matrices. The expected format is one row
		/// per line, one entry per column for each matrix, with each matrix separated
		/// by an empty line.
		/// </remarks>
		public static IList<SimpleMatrix> LoadTextMatrices(string path)
		{
			IList<SimpleMatrix> matrices = new List<SimpleMatrix>();
			foreach (string mString in IOUtils.StringFromFile(path).Trim().Split("\n\n"))
			{
				matrices.Add(Edu.Stanford.Nlp.Neural.NeuralUtils.ConvertTextMatrix(mString).Transpose());
			}
			return matrices;
		}

		public static SimpleMatrix ConvertTextMatrix(string text)
		{
			IList<string> lines = CollectionUtils.FilterAsList(Arrays.AsList(text.Split("\n")), new _IPredicate_68());
			int numRows = lines.Count;
			int numCols = lines[0].Trim().Split("\\s+").Length;
			double[][] data = new double[numRows][];
			for (int row = 0; row < numRows; ++row)
			{
				string line = lines[row];
				string[] pieces = line.Trim().Split("\\s+");
				if (pieces.Length != numCols)
				{
					throw new Exception("Unexpected row length in line " + row);
				}
				for (int col = 0; col < numCols; ++col)
				{
					data[row][col] = double.ValueOf(pieces[col]);
				}
			}
			return new SimpleMatrix(data);
		}

		private sealed class _IPredicate_68 : IPredicate<string>
		{
			public _IPredicate_68()
			{
			}

			public bool Test(string s)
			{
				return s.Trim().Length > 0;
			}
		}

		/// <param name="matrix">The matrix to return as a String</param>
		/// <param name="format">The format to use for each value in the matrix, eg "%f"</param>
		public static string ToString(SimpleMatrix matrix, string format)
		{
			ByteArrayOutputStream stream = new ByteArrayOutputStream();
			MatrixIO.Print(new TextWriter(stream), matrix.GetMatrix(), format);
			return stream.ToString();
		}

		/// <summary>Compute cosine distance between two column vectors.</summary>
		public static double Cosine(SimpleMatrix vector1, SimpleMatrix vector2)
		{
			return Dot(vector1, vector2) / (vector1.NormF() * vector2.NormF());
		}

		/// <summary>Compute dot product between two vectors.</summary>
		public static double Dot(SimpleMatrix vector1, SimpleMatrix vector2)
		{
			if (vector1.NumRows() == 1)
			{
				// vector1: row vector, assume that vector2 is a row vector too
				return vector1.Mult(vector2.Transpose()).Get(0);
			}
			else
			{
				if (vector1.NumCols() == 1)
				{
					// vector1: col vector, assume that vector2 is also a column vector.
					return vector1.Transpose().Mult(vector2).Get(0);
				}
				else
				{
					throw new AssertionError("Error in neural.Utils.dot: vector1 is a matrix " + vector1.NumRows() + " x " + vector1.NumCols());
				}
			}
		}

		/// <summary>
		/// Given a sequence of Iterators over SimpleMatrix, fill in all of
		/// the matrices with the entries in the theta vector.
		/// </summary>
		/// <remarks>
		/// Given a sequence of Iterators over SimpleMatrix, fill in all of
		/// the matrices with the entries in the theta vector.  Errors are
		/// thrown if the theta vector does not exactly fill the matrices.
		/// </remarks>
		[SafeVarargs]
		public static void VectorToParams(double[] theta, params IEnumerator<SimpleMatrix>[] matrices)
		{
			int index = 0;
			foreach (IEnumerator<SimpleMatrix> matrixIterator in matrices)
			{
				while (matrixIterator.MoveNext())
				{
					SimpleMatrix matrix = matrixIterator.Current;
					int numElements = matrix.GetNumElements();
					for (int i = 0; i < numElements; ++i)
					{
						matrix.Set(i, theta[index]);
						++index;
					}
				}
			}
			if (index != theta.Length)
			{
				throw new AssertionError("Did not entirely use the theta vector");
			}
		}

		/// <summary>
		/// Given a sequence of iterators over the matrices, builds a vector
		/// out of those matrices in the order given.
		/// </summary>
		/// <remarks>
		/// Given a sequence of iterators over the matrices, builds a vector
		/// out of those matrices in the order given.  Asks for an expected
		/// total size as a time savings.  AssertionError thrown if the
		/// vector sizes do not exactly match.
		/// </remarks>
		[SafeVarargs]
		public static double[] ParamsToVector(int totalSize, params IEnumerator<SimpleMatrix>[] matrices)
		{
			double[] theta = new double[totalSize];
			int index = 0;
			foreach (IEnumerator<SimpleMatrix> matrixIterator in matrices)
			{
				while (matrixIterator.MoveNext())
				{
					SimpleMatrix matrix = matrixIterator.Current;
					int numElements = matrix.GetNumElements();
					//System.out.println(Integer.toString(numElements)); // to know what matrices are
					for (int i = 0; i < numElements; ++i)
					{
						theta[index] = matrix.Get(i);
						++index;
					}
				}
			}
			if (index != totalSize)
			{
				throw new AssertionError("Did not entirely fill the theta vector: expected " + totalSize + " used " + index);
			}
			return theta;
		}

		/// <summary>
		/// Given a sequence of iterators over the matrices, builds a vector
		/// out of those matrices in the order given.
		/// </summary>
		/// <remarks>
		/// Given a sequence of iterators over the matrices, builds a vector
		/// out of those matrices in the order given.  The vector is scaled
		/// according to the <code>scale</code> parameter.  Asks for an
		/// expected total size as a time savings.  AssertionError thrown if
		/// the vector sizes do not exactly match.
		/// </remarks>
		[SafeVarargs]
		public static double[] ParamsToVector(double scale, int totalSize, params IEnumerator<SimpleMatrix>[] matrices)
		{
			double[] theta = new double[totalSize];
			int index = 0;
			foreach (IEnumerator<SimpleMatrix> matrixIterator in matrices)
			{
				while (matrixIterator.MoveNext())
				{
					SimpleMatrix matrix = matrixIterator.Current;
					int numElements = matrix.GetNumElements();
					for (int i = 0; i < numElements; ++i)
					{
						theta[index] = matrix.Get(i) * scale;
						++index;
					}
				}
			}
			if (index != totalSize)
			{
				throw new AssertionError("Did not entirely fill the theta vector: expected " + totalSize + " used " + index);
			}
			return theta;
		}

		/// <summary>Returns a sigmoid applied to the input <code>x</code>.</summary>
		public static double Sigmoid(double x)
		{
			return 1.0 / (1.0 + Math.Exp(-x));
		}

		/// <summary>Applies softmax to all of the elements of the matrix.</summary>
		/// <remarks>
		/// Applies softmax to all of the elements of the matrix.  The return
		/// matrix will have all of its elements sum to 1.  If your matrix is
		/// not already a vector, be sure this is what you actually want.
		/// </remarks>
		public static SimpleMatrix Softmax(SimpleMatrix input)
		{
			SimpleMatrix output = new SimpleMatrix(input);
			for (int i = 0; i < output.NumRows(); ++i)
			{
				for (int j = 0; j < output.NumCols(); ++j)
				{
					output.Set(i, j, Math.Exp(output.Get(i, j)));
				}
			}
			double sum = output.ElementSum();
			// will be safe, since exp should never return 0
			return output.Scale(1.0 / sum);
		}

		/// <summary>Applies ReLU to each of the entries in the matrix.</summary>
		/// <remarks>Applies ReLU to each of the entries in the matrix.  Returns a new matrix.</remarks>
		public static SimpleMatrix ElementwiseApplyReLU(SimpleMatrix input)
		{
			SimpleMatrix output = new SimpleMatrix(input);
			for (int i = 0; i < output.NumRows(); ++i)
			{
				for (int j = 0; j < output.NumCols(); ++j)
				{
					output.Set(i, j, Math.Max(0, output.Get(i, j)));
				}
			}
			return output;
		}

		/// <summary>Applies log to each of the entries in the matrix.</summary>
		/// <remarks>Applies log to each of the entries in the matrix.  Returns a new matrix.</remarks>
		public static SimpleMatrix ElementwiseApplyLog(SimpleMatrix input)
		{
			SimpleMatrix output = new SimpleMatrix(input);
			for (int i = 0; i < output.NumRows(); ++i)
			{
				for (int j = 0; j < output.NumCols(); ++j)
				{
					output.Set(i, j, Math.Log(output.Get(i, j)));
				}
			}
			return output;
		}

		/// <summary>Applies tanh to each of the entries in the matrix.</summary>
		/// <remarks>Applies tanh to each of the entries in the matrix.  Returns a new matrix.</remarks>
		public static SimpleMatrix ElementwiseApplyTanh(SimpleMatrix input)
		{
			SimpleMatrix output = new SimpleMatrix(input);
			for (int i = 0; i < output.NumRows(); ++i)
			{
				for (int j = 0; j < output.NumCols(); ++j)
				{
					output.Set(i, j, Math.Tanh(output.Get(i, j)));
				}
			}
			return output;
		}

		/// <summary>Applies the derivative of tanh to each of the elements in the vector.</summary>
		/// <remarks>Applies the derivative of tanh to each of the elements in the vector.  Returns a new matrix.</remarks>
		public static SimpleMatrix ElementwiseApplyTanhDerivative(SimpleMatrix input)
		{
			SimpleMatrix output = new SimpleMatrix(input.NumRows(), input.NumCols());
			output.Set(1.0);
			output = output.Minus(input.ElementMult(input));
			return output;
		}

		/// <summary>
		/// Concatenates several column vectors into one large column
		/// vector, adds a 1.0 at the end as a bias term
		/// </summary>
		public static SimpleMatrix ConcatenateWithBias(params SimpleMatrix[] vectors)
		{
			int size = 0;
			foreach (SimpleMatrix vector in vectors)
			{
				size += vector.NumRows();
			}
			// one extra for the bias
			size++;
			SimpleMatrix result = new SimpleMatrix(size, 1);
			int index = 0;
			foreach (SimpleMatrix vector_1 in vectors)
			{
				result.InsertIntoThis(index, 0, vector_1);
				index += vector_1.NumRows();
			}
			result.Set(index, 0, 1.0);
			return result;
		}

		/// <summary>Concatenates several column vectors into one large column vector</summary>
		public static SimpleMatrix Concatenate(params SimpleMatrix[] vectors)
		{
			int size = 0;
			foreach (SimpleMatrix vector in vectors)
			{
				size += vector.NumRows();
			}
			SimpleMatrix result = new SimpleMatrix(size, 1);
			int index = 0;
			foreach (SimpleMatrix vector_1 in vectors)
			{
				result.InsertIntoThis(index, 0, vector_1);
				index += vector_1.NumRows();
			}
			return result;
		}

		/// <summary>Returns a vector with random Gaussian values, mean 0, std 1</summary>
		public static SimpleMatrix RandomGaussian(int numRows, int numCols, Random rand)
		{
			SimpleMatrix result = new SimpleMatrix(numRows, numCols);
			for (int i = 0; i < numRows; ++i)
			{
				for (int j = 0; j < numCols; ++j)
				{
					result.Set(i, j, rand.NextGaussian());
				}
			}
			return result;
		}

		public static SimpleMatrix OneHot(int index, int size)
		{
			SimpleMatrix m = new SimpleMatrix(size, 1);
			m.Set(index, 1);
			return m;
		}

		/// <summary>Returns true iff every element of matrix is 0</summary>
		public static bool IsZero(SimpleMatrix matrix)
		{
			int size = matrix.GetNumElements();
			for (int i = 0; i < size; ++i)
			{
				if (matrix.Get(i) != 0.0)
				{
					return false;
				}
			}
			return true;
		}
	}
}
