using System;
using System.Collections.Generic;


using Org.Ejml.Simple;


namespace Edu.Stanford.Nlp.Neural
{
	/// <summary>
	/// This class defines a block tensor, somewhat like a three
	/// dimensional matrix.
	/// </summary>
	/// <remarks>
	/// This class defines a block tensor, somewhat like a three
	/// dimensional matrix.  This can be created in various ways, such as
	/// by providing an array of SimpleMatrix slices, by providing the
	/// initial size to create a 0-initialized tensor, or by creating a
	/// random matrix.
	/// </remarks>
	/// <author>John Bauer</author>
	/// <author>Richard Socher</author>
	[System.Serializable]
	public class SimpleTensor
	{
		private readonly SimpleMatrix[] slices;

		private readonly int numRows;

		private readonly int numCols;

		private readonly int numSlices;

		/// <summary>Creates a zero initialized tensor</summary>
		public SimpleTensor(int numRows, int numCols, int numSlices)
		{
			slices = new SimpleMatrix[numSlices];
			for (int i = 0; i < numSlices; ++i)
			{
				slices[i] = new SimpleMatrix(numRows, numCols);
			}
			this.numRows = numRows;
			this.numCols = numCols;
			this.numSlices = numSlices;
		}

		/// <summary>Copies the data in the slices.</summary>
		/// <remarks>
		/// Copies the data in the slices.  Slices are copied rather than
		/// reusing the original SimpleMatrix objects.  Each slice must be
		/// the same size.
		/// </remarks>
		public SimpleTensor(SimpleMatrix[] slices)
		{
			this.numRows = slices[0].NumRows();
			this.numCols = slices[0].NumCols();
			this.numSlices = slices.Length;
			this.slices = new SimpleMatrix[slices.Length];
			for (int i = 0; i < numSlices; ++i)
			{
				if (slices[i].NumRows() != numRows || slices[i].NumCols() != numCols)
				{
					throw new ArgumentException("Slice " + i + " has matrix dimensions " + slices[i].NumRows() + "," + slices[i].NumCols() + ", expected " + numRows + "," + numCols);
				}
				this.slices[i] = new SimpleMatrix(slices[i]);
			}
		}

		/// <summary>
		/// Returns a randomly initialized tensor with values draft from the
		/// uniform distribution between minValue and maxValue.
		/// </summary>
		public static Edu.Stanford.Nlp.Neural.SimpleTensor Random(int numRows, int numCols, int numSlices, double minValue, double maxValue, Java.Util.Random rand)
		{
			Edu.Stanford.Nlp.Neural.SimpleTensor tensor = new Edu.Stanford.Nlp.Neural.SimpleTensor(numRows, numCols, numSlices);
			for (int i = 0; i < numSlices; ++i)
			{
				tensor.slices[i] = SimpleMatrix.Random(numRows, numCols, minValue, maxValue, rand);
			}
			return tensor;
		}

		/// <summary>Number of rows in the tensor</summary>
		public virtual int NumRows()
		{
			return numRows;
		}

		/// <summary>Number of columns in the tensor</summary>
		public virtual int NumCols()
		{
			return numCols;
		}

		/// <summary>Number of slices in the tensor</summary>
		public virtual int NumSlices()
		{
			return numSlices;
		}

		/// <summary>Total number of elements in the tensor</summary>
		public virtual int GetNumElements()
		{
			return numRows * numCols * numSlices;
		}

		public virtual void Set(double value)
		{
			for (int slice = 0; slice < numSlices; ++slice)
			{
				slices[slice].Set(value);
			}
		}

		/// <summary>
		/// Returns a new tensor which has the values of the original tensor
		/// scaled by
		/// <paramref name="scaling"/>
		/// .  The original object is
		/// unaffected.
		/// </summary>
		public virtual Edu.Stanford.Nlp.Neural.SimpleTensor Scale(double scaling)
		{
			Edu.Stanford.Nlp.Neural.SimpleTensor result = new Edu.Stanford.Nlp.Neural.SimpleTensor(numRows, numCols, numSlices);
			for (int slice = 0; slice < numSlices; ++slice)
			{
				result.slices[slice] = slices[slice].Scale(scaling);
			}
			return result;
		}

		/// <summary>
		/// Returns
		/// <paramref name="other"/>
		/// added to the current object, which is unaffected.
		/// </summary>
		public virtual Edu.Stanford.Nlp.Neural.SimpleTensor Plus(Edu.Stanford.Nlp.Neural.SimpleTensor other)
		{
			if (other.numRows != numRows || other.numCols != numCols || other.numSlices != numSlices)
			{
				throw new ArgumentException("Sizes of tensors do not match.  Our size: " + numRows + "," + numCols + "," + numSlices + "; other size " + other.numRows + "," + other.numCols + "," + other.numSlices);
			}
			Edu.Stanford.Nlp.Neural.SimpleTensor result = new Edu.Stanford.Nlp.Neural.SimpleTensor(numRows, numCols, numSlices);
			for (int i = 0; i < numSlices; ++i)
			{
				result.slices[i] = slices[i].Plus(other.slices[i]);
			}
			return result;
		}

		/// <summary>Performs elementwise multiplication on the tensors.</summary>
		/// <remarks>
		/// Performs elementwise multiplication on the tensors.  The original
		/// objects are unaffected.
		/// </remarks>
		public virtual Edu.Stanford.Nlp.Neural.SimpleTensor ElementMult(Edu.Stanford.Nlp.Neural.SimpleTensor other)
		{
			if (other.numRows != numRows || other.numCols != numCols || other.numSlices != numSlices)
			{
				throw new ArgumentException("Sizes of tensors do not match.  Our size: " + numRows + "," + numCols + "," + numSlices + "; other size " + other.numRows + "," + other.numCols + "," + other.numSlices);
			}
			Edu.Stanford.Nlp.Neural.SimpleTensor result = new Edu.Stanford.Nlp.Neural.SimpleTensor(numRows, numCols, numSlices);
			for (int i = 0; i < numSlices; ++i)
			{
				result.slices[i] = slices[i].ElementMult(other.slices[i]);
			}
			return result;
		}

		/// <summary>Returns the sum of all elements in the tensor.</summary>
		public virtual double ElementSum()
		{
			double sum = 0.0;
			foreach (SimpleMatrix slice in slices)
			{
				sum += slice.ElementSum();
			}
			return sum;
		}

		/// <summary>
		/// Use the given
		/// <paramref name="matrix"/>
		/// in place of
		/// <paramref name="slice"/>
		/// .
		/// Does not copy the
		/// <paramref name="matrix"/>
		/// , but rather uses the actual object.
		/// </summary>
		public virtual void SetSlice(int slice, SimpleMatrix matrix)
		{
			if (slice < 0 || slice >= numSlices)
			{
				throw new ArgumentException("Unexpected slice number " + slice + " for tensor with " + numSlices + " slices");
			}
			if (matrix.NumCols() != numCols)
			{
				throw new ArgumentException("Incompatible matrix size.  Has " + matrix.NumCols() + " columns, tensor has " + numCols);
			}
			if (matrix.NumRows() != numRows)
			{
				throw new ArgumentException("Incompatible matrix size.  Has " + matrix.NumRows() + " columns, tensor has " + numRows);
			}
			slices[slice] = matrix;
		}

		/// <summary>
		/// Returns the SimpleMatrix at
		/// <paramref name="slice"/>
		/// .
		/// <br />
		/// The actual slice is returned - do not alter this unless you know what you are doing.
		/// </summary>
		public virtual SimpleMatrix GetSlice(int slice)
		{
			if (slice < 0 || slice >= numSlices)
			{
				throw new ArgumentException("Unexpected slice number " + slice + " for tensor with " + numSlices + " slices");
			}
			return slices[slice];
		}

		/// <summary>
		/// Returns a column vector where each entry is the nth bilinear
		/// product of the nth slices of the two tensors.
		/// </summary>
		public virtual SimpleMatrix BilinearProducts(SimpleMatrix @in)
		{
			if (@in.NumCols() != 1)
			{
				throw new AssertionError("Expected a column vector");
			}
			if (@in.NumRows() != numCols)
			{
				throw new AssertionError("Number of rows in the input does not match number of columns in tensor");
			}
			if (numRows != numCols)
			{
				throw new AssertionError("Can only perform this operation on a SimpleTensor with square slices");
			}
			SimpleMatrix inT = @in.Transpose();
			SimpleMatrix @out = new SimpleMatrix(numSlices, 1);
			for (int slice = 0; slice < numSlices; ++slice)
			{
				double result = inT.Mult(slices[slice]).Mult(@in).Get(0);
				@out.Set(slice, result);
			}
			return @out;
		}

		/// <summary>Returns true iff every element of the tensor is 0</summary>
		public virtual bool IsZero()
		{
			for (int i = 0; i < numSlices; ++i)
			{
				if (!NeuralUtils.IsZero(slices[i]))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Returns an iterator over the
		/// <c>SimpleMatrix</c>
		/// objects contained in the tensor.
		/// </summary>
		public virtual IEnumerator<SimpleMatrix> IteratorSimpleMatrix()
		{
			return Arrays.AsList(slices).GetEnumerator();
		}

		/// <summary>
		/// Returns an Iterator which returns the SimpleMatrices represented
		/// by an Iterator over tensors.
		/// </summary>
		/// <remarks>
		/// Returns an Iterator which returns the SimpleMatrices represented
		/// by an Iterator over tensors.  This is useful for if you want to
		/// perform some operation on each of the SimpleMatrix slices, such
		/// as turning them into a parameter stack.
		/// </remarks>
		public static IEnumerator<SimpleMatrix> IteratorSimpleMatrix(IEnumerator<Edu.Stanford.Nlp.Neural.SimpleTensor> tensors)
		{
			return new SimpleTensor.SimpleMatrixIteratorWrapper(tensors);
		}

		private class SimpleMatrixIteratorWrapper : IEnumerator<SimpleMatrix>
		{
			internal IEnumerator<SimpleTensor> tensors;

			internal IEnumerator<SimpleMatrix> currentIterator;

			public SimpleMatrixIteratorWrapper(IEnumerator<SimpleTensor> tensors)
			{
				this.tensors = tensors;
				AdvanceIterator();
			}

			public virtual bool MoveNext()
			{
				if (currentIterator == null)
				{
					return false;
				}
				if (currentIterator.MoveNext())
				{
					return true;
				}
				AdvanceIterator();
				return (currentIterator != null);
			}

			public virtual SimpleMatrix Current
			{
				get
				{
					if (currentIterator != null && currentIterator.MoveNext())
					{
						return currentIterator.Current;
					}
					AdvanceIterator();
					if (currentIterator != null)
					{
						return currentIterator.Current;
					}
					throw new NoSuchElementException();
				}
			}

			private void AdvanceIterator()
			{
				if (currentIterator != null && currentIterator.MoveNext())
				{
					return;
				}
				while (tensors.MoveNext())
				{
					currentIterator = tensors.Current.IteratorSimpleMatrix();
					if (currentIterator.MoveNext())
					{
						return;
					}
				}
				currentIterator = null;
			}

			public virtual void Remove()
			{
				throw new NotSupportedException();
			}
		}

		public override string ToString()
		{
			StringBuilder result = new StringBuilder();
			for (int slice = 0; slice < numSlices; ++slice)
			{
				result.Append("Slice ").Append(slice).Append("\n");
				result.Append(slices[slice]);
			}
			return result.ToString();
		}

		/// <summary>Output the tensor one slice at a time.</summary>
		/// <remarks>
		/// Output the tensor one slice at a time.  Each number is output
		/// with the format given, so for example "%f"
		/// </remarks>
		public virtual string ToString(string format)
		{
			StringBuilder result = new StringBuilder();
			for (int slice = 0; slice < numSlices; ++slice)
			{
				result.Append("Slice ").Append(slice).Append("\n");
				result.Append(NeuralUtils.ToString(slices[slice], format));
			}
			return result.ToString();
		}

		private const long serialVersionUID = 1;
	}
}
