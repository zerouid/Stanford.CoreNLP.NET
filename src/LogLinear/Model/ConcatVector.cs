using System;
using Edu.Stanford.Nlp.Loglinear.Model.Proto;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Model
{
	/// <summary>Created on 12/7/14.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// Implements a concat vector using an array of arrays, with all its attending resizing efficiencies, and double-pointer
	/// inefficiencies. Benchmarking from MinimalML (where I adapted this design from) shows that this is the most efficient
	/// of several strategies that can be used to implement this.
	/// <p>
	/// What is a ConcatVector? Why do I need it?
	/// <p>
	/// In short, you want this for online learning, where you may not know all your sparse features' sizes at initialization.
	/// A concat vector is a vector that behaves like a concatenation of smaller component vectors when you want a dot product.
	/// However, it never physically concatenates anything, it just dot products each component, and takes the sum. That way,
	/// if you need to expand a component during online learning, it's no problem. As an auxiliary benefit, you can specify
	/// sparse and dense components, greatly speeding up dot product calculation when you have lots of sparse features.
	/// </author>
	public class ConcatVector
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Loglinear.Model.ConcatVector));

		private double[][] pointers;

		private bool[] sparse;

		private bool[] copyOnWrite;

		/// <summary>Constructor that initializes space for this concat vector.</summary>
		/// <remarks>
		/// Constructor that initializes space for this concat vector. Don't worry, it can resize individual elements as
		/// necessary but it's most efficient if you get this right at construction.
		/// </remarks>
		/// <param name="numComponents">The number of components (usually number of features) to allocate for.</param>
		public ConcatVector(int numComponents)
		{
			pointers = new double[numComponents][];
			sparse = new bool[numComponents];
			copyOnWrite = new bool[numComponents];
		}

		/// <summary>Clone a concat vector constructor.</summary>
		/// <remarks>Clone a concat vector constructor. Marks both vectors as copyOnWrite, but makes no immediate copies.</remarks>
		/// <param name="clone">the concat vector to clone.</param>
		private ConcatVector(Edu.Stanford.Nlp.Loglinear.Model.ConcatVector clone)
		{
			pointers = new double[clone.pointers.Length][];
			copyOnWrite = new bool[clone.pointers.Length];
			for (int i = 0; i < clone.pointers.Length; i++)
			{
				if (clone.pointers[i] == null)
				{
					continue;
				}
				pointers[i] = clone.pointers[i];
				copyOnWrite[i] = true;
				clone.copyOnWrite[i] = true;
			}
			sparse = new bool[clone.pointers.Length];
			if (clone.pointers.Length > 0)
			{
				System.Array.Copy(clone.sparse, 0, sparse, 0, clone.pointers.Length);
			}
		}

		/// <summary>
		/// Creates a ConcatVector whose dimensions are the same as this one for all dense components, but is otherwise
		/// completely empty.
		/// </summary>
		/// <remarks>
		/// Creates a ConcatVector whose dimensions are the same as this one for all dense components, but is otherwise
		/// completely empty. This is useful to prevent resizing during optimizations where we're adding lots of sparse
		/// vectors.
		/// </remarks>
		/// <returns>an empty vector suitable for use as a gradient</returns>
		public virtual Edu.Stanford.Nlp.Loglinear.Model.ConcatVector NewEmptyClone()
		{
			Edu.Stanford.Nlp.Loglinear.Model.ConcatVector clone = new Edu.Stanford.Nlp.Loglinear.Model.ConcatVector(GetNumberOfComponents());
			for (int i = 0; i < pointers.Length; i++)
			{
				if (pointers[i] != null && !sparse[i])
				{
					clone.pointers[i] = new double[pointers[i].Length];
					clone.sparse[i] = false;
				}
			}
			return clone;
		}

		/// <summary>Sets a single component of the concat vector value as a dense vector.</summary>
		/// <remarks>
		/// Sets a single component of the concat vector value as a dense vector. This will make a copy of you values array,
		/// so you're free to continue mutating it.
		/// </remarks>
		/// <param name="component">the index of the component to set</param>
		/// <param name="values">the array of dense values to put into the component</param>
		public virtual void SetDenseComponent(int component, double[] values)
		{
			if (component >= pointers.Length)
			{
				IncreaseSizeTo(component + 1);
			}
			pointers[component] = values;
			sparse[component] = false;
			copyOnWrite[component] = true;
		}

		/// <summary>Sets a single component of the concat vector value as a sparse, one hot value.</summary>
		/// <param name="component">the index of the component to set</param>
		/// <param name="index">the index of the vector to one-hot</param>
		/// <param name="value">the value of that index</param>
		public virtual void SetSparseComponent(int component, int index, double value)
		{
			if (component >= pointers.Length)
			{
				IncreaseSizeTo(component + 1);
			}
			double[] sparseInfo = new double[2];
			sparseInfo[0] = index;
			sparseInfo[1] = value;
			pointers[component] = sparseInfo;
			sparse[component] = true;
			copyOnWrite[component] = false;
		}

		/// <summary>This function assumes both vectors are infinitely padded with 0s, so it won't complain if there's a dim mismatch.</summary>
		/// <remarks>
		/// This function assumes both vectors are infinitely padded with 0s, so it won't complain if there's a dim mismatch.
		/// There are no side effects.
		/// </remarks>
		/// <param name="other">the MV to dot product with</param>
		/// <returns>the dot product of this and other</returns>
		public virtual double DotProduct(Edu.Stanford.Nlp.Loglinear.Model.ConcatVector other)
		{
			if (loadedNative)
			{
				return DotProductNative(other);
			}
			else
			{
				double sum = 0.0f;
				for (int i = 0; i < Math.Min(pointers.Length, other.pointers.Length); i++)
				{
					if (pointers[i] == null || other.pointers[i] == null)
					{
						continue;
					}
					if (sparse[i] && other.sparse[i])
					{
						if ((int)pointers[i][0] == (int)other.pointers[i][0])
						{
							sum += pointers[i][1] * other.pointers[i][1];
						}
					}
					else
					{
						if (sparse[i] && !other.sparse[i])
						{
							int sparseIndex = (int)pointers[i][0];
							if (sparseIndex >= 0 && sparseIndex < other.pointers[i].Length)
							{
								sum += other.pointers[i][sparseIndex] * pointers[i][1];
							}
						}
						else
						{
							if (!sparse[i] && other.sparse[i])
							{
								int sparseIndex = (int)other.pointers[i][0];
								if (sparseIndex >= 0 && sparseIndex < pointers[i].Length)
								{
									sum += pointers[i][sparseIndex] * other.pointers[i][1];
								}
							}
							else
							{
								for (int j = 0; j < Math.Min(pointers[i].Length, other.pointers[i].Length); j++)
								{
									sum += pointers[i][j] * other.pointers[i][j];
								}
							}
						}
					}
				}
				return sum;
			}
		}

		/// <returns>a clone of this concat vector, with deep copies of datastructures</returns>
		public virtual Edu.Stanford.Nlp.Loglinear.Model.ConcatVector DeepClone()
		{
			return new Edu.Stanford.Nlp.Loglinear.Model.ConcatVector(this);
		}

		/// <summary>This will add the vector "other" to this vector, scaling other by multiple.</summary>
		/// <remarks>
		/// This will add the vector "other" to this vector, scaling other by multiple. In algebra,
		/// <p>
		/// this = this + (other * multiple)
		/// <p>
		/// The function assumes that both vectors are padded infinitely with 0s, so will scale this vector by adding components
		/// and changing component sizes (dense to bigger dense) and shapes (sparse to dense) in order to accommodate the result.
		/// </remarks>
		/// <param name="other">the vector to add to this one</param>
		/// <param name="multiple">the multiple to use</param>
		public virtual void AddVectorInPlace(Edu.Stanford.Nlp.Loglinear.Model.ConcatVector other, double multiple)
		{
			// Resize if necessary
			if (pointers == null)
			{
				pointers = new double[other.pointers.Length][];
				sparse = new bool[other.pointers.Length];
				copyOnWrite = new bool[other.pointers.Length];
			}
			else
			{
				if (pointers.Length < other.pointers.Length)
				{
					IncreaseSizeTo(other.pointers.Length);
				}
			}
			// Do the addition piece by piece
			for (int i = 0; i < other.pointers.Length; i++)
			{
				// If the other vector has no segment here, then skip
				if (other.pointers[i] == null)
				{
					continue;
				}
				// If we previously had no element here, fill it in accordingly
				if (pointers[i] == null || pointers[i].Length == 0)
				{
					sparse[i] = other.sparse[i];
					// If the multiple is one, just follow the copying procedure
					if (multiple == 1.0)
					{
						pointers[i] = other.pointers[i];
						copyOnWrite[i] = true;
						other.copyOnWrite[i] = true;
					}
					else
					{
						// Otherwise do the standard thing
						if (other.sparse[i])
						{
							pointers[i] = new double[2];
							copyOnWrite[i] = false;
							pointers[i][0] = other.pointers[i][0];
							pointers[i][1] = other.pointers[i][1] * multiple;
						}
						else
						{
							pointers[i] = new double[other.pointers[i].Length];
							copyOnWrite[i] = false;
							for (int j = 0; j < other.pointers[i].Length; j++)
							{
								pointers[i][j] = other.pointers[i][j] * multiple;
							}
						}
					}
				}
				else
				{
					// Handle rescaling on a component-by-component basis
					if (sparse[i] && !other.sparse[i])
					{
						int sparseIndex = (int)pointers[i][0];
						double sparseValue = pointers[i][1];
						sparse[i] = false;
						pointers[i] = new double[Math.Max(sparseIndex + 1, other.pointers[i].Length)];
						copyOnWrite[i] = false;
						if (sparseIndex >= 0)
						{
							pointers[i][sparseIndex] = sparseValue;
						}
						for (int j = 0; j < other.pointers[i].Length; j++)
						{
							pointers[i][j] += other.pointers[i][j] * multiple;
						}
					}
					else
					{
						if (sparse[i] && other.sparse[i])
						{
							int mySparseIndex = (int)pointers[i][0];
							int otherSparseIndex = (int)other.pointers[i][0];
							if (mySparseIndex == otherSparseIndex)
							{
								if (copyOnWrite[i])
								{
									pointers[i] = pointers[i].MemberwiseClone();
									copyOnWrite[i] = false;
								}
								pointers[i][1] += other.pointers[i][1] * multiple;
							}
							else
							{
								sparse[i] = false;
								double mySparseValue = pointers[i][1];
								pointers[i] = new double[Math.Max(mySparseIndex + 1, otherSparseIndex + 1)];
								copyOnWrite[i] = false;
								if (mySparseIndex >= 0)
								{
									pointers[i][mySparseIndex] = mySparseValue;
								}
								if (otherSparseIndex >= 0)
								{
									pointers[i][otherSparseIndex] = other.pointers[i][1] * multiple;
								}
							}
						}
						else
						{
							if (!sparse[i] && other.sparse[i])
							{
								int sparseIndex = (int)other.pointers[i][0];
								if (sparseIndex >= pointers[i].Length)
								{
									int newSize = pointers[i].Length;
									while (newSize <= sparseIndex)
									{
										newSize *= 2;
									}
									double[] denseBuf = new double[newSize];
									System.Array.Copy(pointers[i], 0, denseBuf, 0, pointers[i].Length);
									copyOnWrite[i] = false;
									pointers[i] = denseBuf;
								}
								if (sparseIndex >= 0)
								{
									if (copyOnWrite[i])
									{
										pointers[i] = pointers[i].MemberwiseClone();
										copyOnWrite[i] = false;
									}
									pointers[i][sparseIndex] += other.pointers[i][1] * multiple;
								}
							}
							else
							{
								System.Diagnostics.Debug.Assert((!sparse[i] && !other.sparse[i]));
								if (pointers[i].Length < other.pointers[i].Length)
								{
									double[] denseBuf = new double[other.pointers[i].Length];
									System.Array.Copy(pointers[i], 0, denseBuf, 0, pointers[i].Length);
									copyOnWrite[i] = false;
									pointers[i] = denseBuf;
								}
								if (copyOnWrite[i])
								{
									pointers[i] = pointers[i].MemberwiseClone();
									copyOnWrite[i] = false;
								}
								for (int j = 0; j < other.pointers[i].Length; j++)
								{
									pointers[i][j] += other.pointers[i][j] * multiple;
								}
							}
						}
					}
				}
			}
		}

		/// <summary>This will multiply the vector "other" to this vector.</summary>
		/// <remarks>
		/// This will multiply the vector "other" to this vector. It's the equivalent of the Matlab
		/// <p>
		/// this = this .* other
		/// <p>
		/// The function assumes that both vectors are padded infinitely with 0s, so will result in lots of 0s in this
		/// vector if it is longer than 'other'.
		/// </remarks>
		/// <param name="other">the vector to multiply into this one</param>
		public virtual void ElementwiseProductInPlace(Edu.Stanford.Nlp.Loglinear.Model.ConcatVector other)
		{
			for (int i = 0; i < pointers.Length; i++)
			{
				if (pointers[i] == null)
				{
					continue;
				}
				if (copyOnWrite[i])
				{
					copyOnWrite[i] = false;
					pointers[i] = pointers[i].MemberwiseClone();
				}
				if (i >= other.pointers.Length)
				{
					if (sparse[i])
					{
						pointers[i][1] = 0;
					}
					else
					{
						for (int j = 0; j < pointers[i].Length; j++)
						{
							pointers[i][j] = 0;
						}
					}
				}
				else
				{
					if (other.pointers[i] == null)
					{
						pointers[i] = null;
					}
					else
					{
						if (sparse[i] && other.sparse[i])
						{
							if ((int)pointers[i][0] == (int)other.pointers[i][0])
							{
								pointers[i][1] *= other.pointers[i][1];
							}
							else
							{
								pointers[i][1] = 0.0f;
							}
						}
						else
						{
							if (sparse[i] && !other.sparse[i])
							{
								int sparseIndex = (int)pointers[i][0];
								if (sparseIndex >= 0 && sparseIndex < other.pointers[i].Length)
								{
									pointers[i][1] *= other.pointers[i][sparseIndex];
								}
								else
								{
									pointers[i][1] = 0.0f;
								}
							}
							else
							{
								if (!sparse[i] && other.sparse[i])
								{
									int sparseIndex = (int)other.pointers[i][0];
									double sparseValue = 0.0f;
									if (sparseIndex >= 0 && sparseIndex < pointers[i].Length)
									{
										sparseValue = pointers[i][sparseIndex] * other.pointers[i][1];
									}
									sparse[i] = true;
									pointers[i] = new double[] { sparseIndex, sparseValue };
								}
								else
								{
									for (int j = 0; j < Math.Min(pointers[i].Length, other.pointers[i].Length); j++)
									{
										pointers[i][j] *= other.pointers[i][j];
									}
									for (int j_1 = other.pointers[i].Length; j_1 < pointers[i].Length; j_1++)
									{
										pointers[i][j_1] = 0.0f;
									}
								}
							}
						}
					}
				}
			}
		}

		/// <summary>Apply a function to every element of every component of this vector, and replace with the result.</summary>
		/// <param name="fn">the function to apply to every element of every component.</param>
		public virtual void MapInPlace(IDoubleUnaryOperator fn)
		{
			for (int i = 0; i < pointers.Length; i++)
			{
				if (pointers[i] == null)
				{
					continue;
				}
				if (copyOnWrite[i])
				{
					copyOnWrite[i] = false;
					pointers[i] = pointers[i].MemberwiseClone();
				}
				if (sparse[i])
				{
					pointers[i][1] = fn.ApplyAsDouble(pointers[i][1]);
				}
				else
				{
					for (int j = 0; j < pointers[i].Length; j++)
					{
						pointers[i][j] = fn.ApplyAsDouble(pointers[i][j]);
					}
				}
			}
		}

		/// <returns>the number of concatenated vectors that compose this ConcatVector</returns>
		public virtual int GetNumberOfComponents()
		{
			return pointers.Length;
		}

		/// <param name="i">the index of the component to check</param>
		/// <returns>whether component i is sparse or not</returns>
		public virtual bool IsComponentSparse(int i)
		{
			return sparse[i];
		}

		/// <summary>This function will throw an assert if the component you're requesting isn't dense</summary>
		/// <param name="i">the index of the component to look at</param>
		/// <returns>the dense array composing that component</returns>
		public virtual double[] GetDenseComponent(int i)
		{
			System.Diagnostics.Debug.Assert((!sparse[i]));
			// This will save the special case code down the line, so is worth the tiny object creation
			if (pointers[i] == null)
			{
				return new double[0];
			}
			return pointers[i];
		}

		/// <summary>This assumes infinite padding with 0s.</summary>
		/// <remarks>
		/// This assumes infinite padding with 0s. It will return you 0 if you're OOB (use getSegmentSizes() to check, if
		/// that's undesirable behavior). Otherwise it will return you the correct value.
		/// </remarks>
		/// <param name="component">the index of the component to retrieve a value from</param>
		/// <param name="offset">the offset within that component</param>
		/// <returns>the value retrieved, of 0 if OOB</returns>
		public virtual double GetValueAt(int component, int offset)
		{
			if (component < pointers.Length)
			{
				if (pointers[component] == null)
				{
					return 0;
				}
				else
				{
					if (sparse[component])
					{
						int sparseIndex = (int)pointers[component][0];
						if (sparseIndex == offset)
						{
							return pointers[component][1];
						}
					}
					else
					{
						if (offset < pointers[component].Length)
						{
							return pointers[component][offset];
						}
					}
				}
			}
			return 0;
		}

		/// <summary>Gets you the index of one hot in a component, assuming it is sparse.</summary>
		/// <remarks>Gets you the index of one hot in a component, assuming it is sparse. Throws an assert if it isn't.</remarks>
		/// <param name="component">the index of the sparse component.</param>
		/// <returns>the index of the one-hot value within that sparse component.</returns>
		public virtual int GetSparseIndex(int component)
		{
			System.Diagnostics.Debug.Assert((sparse[component]));
			return (int)pointers[component][0];
		}

		/// <summary>Writes the protobuf version of this vector to a stream.</summary>
		/// <remarks>Writes the protobuf version of this vector to a stream. reversible with readFromStream().</remarks>
		/// <param name="stream">the output stream to write to</param>
		/// <exception cref="System.IO.IOException">passed through from the stream</exception>
		public virtual void WriteToStream(OutputStream stream)
		{
			((ConcatVectorProto.ConcatVector)GetProtoBuilder().Build()).WriteDelimitedTo(stream);
		}

		/// <summary>Static function to deserialize a concat vector from an input stream.</summary>
		/// <param name="stream">the stream to read from, assuming protobuf encoding</param>
		/// <returns>a new concat vector</returns>
		/// <exception cref="System.IO.IOException">passed through from the stream</exception>
		public static Edu.Stanford.Nlp.Loglinear.Model.ConcatVector ReadFromStream(InputStream stream)
		{
			return ReadFromProto(ConcatVectorProto.ConcatVector.ParseDelimitedFrom(stream));
		}

		/// <returns>a Builder for proto serialization</returns>
		public virtual ConcatVectorProto.ConcatVector.Builder GetProtoBuilder()
		{
			ConcatVectorProto.ConcatVector.Builder m = ConcatVectorProto.ConcatVector.NewBuilder();
			for (int i = 0; i < pointers.Length; i++)
			{
				ConcatVectorProto.ConcatVector.Component.Builder c = ConcatVectorProto.ConcatVector.Component.NewBuilder();
				c.SetSparse(sparse[i]);
				// We want to keep the data array size 0 if the pointers for this component is null
				if (pointers[i] != null)
				{
					for (int j = 0; j < pointers[i].Length; j++)
					{
						c.AddData(pointers[i][j]);
					}
				}
				m.AddComponent(c);
			}
			return m;
		}

		/// <summary>Recreates an in-memory concat vector object from a Proto serialization.</summary>
		/// <param name="m">the concat vector proto</param>
		/// <returns>an in-memory concat vector object</returns>
		public static Edu.Stanford.Nlp.Loglinear.Model.ConcatVector ReadFromProto(ConcatVectorProto.ConcatVector m)
		{
			int components = m.GetComponentCount();
			Edu.Stanford.Nlp.Loglinear.Model.ConcatVector vec = new Edu.Stanford.Nlp.Loglinear.Model.ConcatVector();
			vec.pointers = new double[components][];
			vec.sparse = new bool[components];
			for (int i = 0; i < components; i++)
			{
				ConcatVectorProto.ConcatVector.Component c = m.GetComponent(i);
				vec.sparse[i] = c.GetSparse();
				int dataSize = c.GetDataCount();
				vec.pointers[i] = new double[dataSize];
				for (int j = 0; j < dataSize; j++)
				{
					vec.pointers[i][j] = c.GetData(j);
				}
			}
			return vec;
		}

		/// <summary>Compares two concat vectors by value.</summary>
		/// <remarks>
		/// Compares two concat vectors by value. This means that we're 0 padding, so a dense and sparse component might
		/// both be considered the same, if the dense array reflects the same value as the sparse array. This is pretty much
		/// only useful for testing. Since it's primarily for testing, we went with the slower, more obviously correct design.
		/// </remarks>
		/// <param name="other">the vector we're comparing to</param>
		/// <param name="tolerance">the amount any pair of values can differ before we say the two vectors are different.</param>
		/// <returns>whether the two vectors are the same</returns>
		public virtual bool ValueEquals(Edu.Stanford.Nlp.Loglinear.Model.ConcatVector other, double tolerance)
		{
			for (int i = 0; i < Math.Max(pointers.Length, other.pointers.Length); i++)
			{
				int size = 0;
				// Find the maximum non-zero element in this component
				if (i < pointers.Length && i < other.pointers.Length && pointers[i] == null && other.pointers[i] == null)
				{
					size = 0;
				}
				else
				{
					if (i >= pointers.Length || (i < pointers.Length && pointers[i] == null))
					{
						if (i >= other.pointers.Length)
						{
							size = 0;
						}
						else
						{
							if (other.sparse[i])
							{
								size = other.GetSparseIndex(i) + 1;
							}
							else
							{
								size = other.pointers[i].Length;
							}
						}
					}
					else
					{
						if (i >= other.pointers.Length || (i < other.pointers.Length && other.pointers[i] == null))
						{
							if (i >= pointers.Length)
							{
								size = 0;
							}
							else
							{
								if (sparse[i])
								{
									size = GetSparseIndex(i) + 1;
								}
								else
								{
									size = pointers[i].Length;
								}
							}
						}
						else
						{
							if (sparse[i] && GetSparseIndex(i) >= size)
							{
								size = GetSparseIndex(i) + 1;
							}
							else
							{
								if (!sparse[i] && pointers[i].Length > size)
								{
									size = pointers[i].Length;
								}
							}
							if (other.sparse[i] && other.GetSparseIndex(i) >= size)
							{
								size = other.GetSparseIndex(i) + 1;
							}
							else
							{
								if (!other.sparse[i] && other.pointers[i].Length > size)
								{
									size = other.pointers[i].Length;
								}
							}
						}
					}
				}
				for (int j = 0; j < size; j++)
				{
					if (Math.Abs(GetValueAt(i, j) - other.GetValueAt(i, j)) > tolerance)
					{
						return false;
					}
				}
			}
			return true;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("[");
			for (int i = 0; i < pointers.Length; i++)
			{
				sb.Append(" ..");
				if (pointers[i] == null)
				{
					sb.Append("0=0.0");
				}
				else
				{
					if (sparse[i])
					{
						sb.Append((int)pointers[i][0]).Append("=").Append(pointers[i][1]);
					}
					else
					{
						for (int j = 0; j < pointers[i].Length; j++)
						{
							sb.Append(pointers[i][j]);
							if (j != pointers[i].Length - 1)
							{
								sb.Append(" ");
							}
						}
					}
				}
				sb.Append("..");
			}
			sb.Append(" ]");
			return sb.ToString();
		}

		////////////////////////////////////////////////////////////////////////////
		// PRIVATE IMPLEMENTATION
		////////////////////////////////////////////////////////////////////////////
		/// <summary>This increases the length of the vector, while preserving its contents</summary>
		/// <param name="newSize">the new size to increase to. Must be larger than the current size</param>
		private void IncreaseSizeTo(int newSize)
		{
			System.Diagnostics.Debug.Assert((newSize > pointers.Length));
			double[][] pointersBuf = new double[newSize][];
			bool[] sparseBuf = new bool[newSize];
			bool[] copyOnWriteBuf = new bool[newSize];
			System.Array.Copy(pointers, 0, pointersBuf, 0, pointers.Length);
			System.Array.Copy(sparse, 0, sparseBuf, 0, pointers.Length);
			System.Array.Copy(copyOnWrite, 0, copyOnWriteBuf, 0, pointers.Length);
			pointers = pointersBuf;
			sparse = sparseBuf;
			copyOnWrite = copyOnWriteBuf;
		}

		private static bool loadedNative = false;

		// Right now I'm not loading the native library even if it's available, since the dot product "speedup" is actually
		// 10x slower. First need to diagnose if a speedup is possible by going through the JNI, which is unlikely.
		/*
		static {
		try {
		System.load(System.getProperty("user.dir")+"/src/main/c/libconcatvec.so");
		loadedNative = true;
		}
		catch (UnsatisfiedLinkError e) {
		log.info("Couldn't find the native acceleration library for ConcatVector");
		}
		}
		*/
		private double DotProductNative(Edu.Stanford.Nlp.Loglinear.Model.ConcatVector other)
		{
		}

		/// <summary>DO NOT USE.</summary>
		/// <remarks>DO NOT USE. FOR SERIALIZERS ONLY.</remarks>
		private ConcatVector()
		{
		}
	}
}
