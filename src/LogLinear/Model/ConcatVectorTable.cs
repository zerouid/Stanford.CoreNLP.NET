using System.Collections.Generic;
using Edu.Stanford.Nlp.Loglinear.Model.Proto;
using Java.IO;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Model
{
	/// <summary>Created on 8/9/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// This is basically a type specific wrapper over NDArray
	/// </author>
	public class ConcatVectorTable : NDArray<ISupplier<ConcatVector>>
	{
		/// <summary>Constructor takes a list of neighbor variables to use for this factor.</summary>
		/// <remarks>
		/// Constructor takes a list of neighbor variables to use for this factor. This must not change after construction,
		/// and the number of states of those variables must also not change.
		/// </remarks>
		/// <param name="dimensions">list of neighbor variables assignment range sizes</param>
		public ConcatVectorTable(int[] dimensions)
			: base(dimensions)
		{
		}

		/// <summary>Convenience function to write this factor directly to a stream, encoded as proto.</summary>
		/// <remarks>Convenience function to write this factor directly to a stream, encoded as proto. Reversible with readFromStream.</remarks>
		/// <param name="stream">the stream to write to. does not flush automatically</param>
		/// <exception cref="System.IO.IOException">passed through from the stream</exception>
		public virtual void WriteToStream(OutputStream stream)
		{
			((ConcatVectorTableProto.ConcatVectorTable)GetProtoBuilder().Build()).WriteTo(stream);
		}

		/// <summary>Convenience function to read a factor (assumed serialized with proto) directly from a stream.</summary>
		/// <param name="stream">the stream to be read from</param>
		/// <returns>a new in-memory feature factor</returns>
		/// <exception cref="System.IO.IOException">passed through from the stream</exception>
		public static Edu.Stanford.Nlp.Loglinear.Model.ConcatVectorTable ReadFromStream(InputStream stream)
		{
			return ReadFromProto(ConcatVectorTableProto.ConcatVectorTable.ParseFrom(stream));
		}

		/// <summary>Returns the proto builder object for this feature factor.</summary>
		/// <remarks>
		/// Returns the proto builder object for this feature factor. Recursively constructs protos for all the concat
		/// vectors in factorTable.
		/// </remarks>
		/// <returns>proto Builder object</returns>
		public virtual ConcatVectorTableProto.ConcatVectorTable.Builder GetProtoBuilder()
		{
			ConcatVectorTableProto.ConcatVectorTable.Builder b = ConcatVectorTableProto.ConcatVectorTable.NewBuilder();
			foreach (int n in GetDimensions())
			{
				b.AddDimensionSize(n);
			}
			foreach (int[] assignment in this)
			{
				b.AddFactorTable(GetAssignmentValue(assignment).Get().GetProtoBuilder());
			}
			return b;
		}

		/// <summary>Creates a new in-memory feature factor from a proto serialization,</summary>
		/// <param name="proto">the proto object to be turned into an in-memory feature factor</param>
		/// <returns>an in-memory feature factor, complete with in-memory concat vectors</returns>
		public static Edu.Stanford.Nlp.Loglinear.Model.ConcatVectorTable ReadFromProto(ConcatVectorTableProto.ConcatVectorTable proto)
		{
			int[] neighborSizes = new int[proto.GetDimensionSizeCount()];
			for (int i = 0; i < neighborSizes.Length; i++)
			{
				neighborSizes[i] = proto.GetDimensionSize(i);
			}
			Edu.Stanford.Nlp.Loglinear.Model.ConcatVectorTable factor = new Edu.Stanford.Nlp.Loglinear.Model.ConcatVectorTable(neighborSizes);
			int i_1 = 0;
			foreach (int[] assignment in factor)
			{
				ConcatVector vector = ConcatVector.ReadFromProto(proto.GetFactorTable(i_1));
				factor.SetAssignmentValue(assignment, null);
				i_1++;
			}
			return factor;
		}

		/// <summary>
		/// Deep comparison for equality of value, plus tolerance, for every concatvector in the table, plus dimensional
		/// arrangement.
		/// </summary>
		/// <remarks>
		/// Deep comparison for equality of value, plus tolerance, for every concatvector in the table, plus dimensional
		/// arrangement. This is mostly useful for testing.
		/// </remarks>
		/// <param name="other">the vector table to compare against</param>
		/// <param name="tolerance">the tolerance to use in value comparisons</param>
		/// <returns>whether the two tables are equivalent by value</returns>
		public virtual bool ValueEquals(Edu.Stanford.Nlp.Loglinear.Model.ConcatVectorTable other, double tolerance)
		{
			if (!Arrays.Equals(other.GetDimensions(), GetDimensions()))
			{
				return false;
			}
			foreach (int[] assignment in this)
			{
				if (!GetAssignmentValue(assignment).Get().ValueEquals(other.GetAssignmentValue(assignment).Get(), tolerance))
				{
					return false;
				}
			}
			return true;
		}

		internal NDArray<ISupplier<ConcatVector>> originalThunks = null;

		/// <summary>
		/// This is an optimization that will fault all the ConcatVectors into memory, and future .get() on the Supplier objs
		/// will result in a very fast return by reference.
		/// </summary>
		/// <remarks>
		/// This is an optimization that will fault all the ConcatVectors into memory, and future .get() on the Supplier objs
		/// will result in a very fast return by reference. Basically this works by wrapping the output of the old thunks
		/// inside new, thinner closures that carry around the answer in memory. This is a no-op if vectors were already
		/// cached.
		/// </remarks>
		public virtual void CacheVectors()
		{
			if (originalThunks != null)
			{
				return;
			}
			originalThunks = new NDArray<ISupplier<ConcatVector>>(GetDimensions());
			// OPTIMIZATION:
			// Rather than use the standard iterator, which creates lots of int[] arrays on the heap, which need to be GC'd,
			// we use the fast version that just mutates one array. Since this is read once for us here, this is ideal.
			IEnumerator<int[]> fastPassByReferenceIterator = FastPassByReferenceIterator();
			int[] assignment = fastPassByReferenceIterator.Current;
			while (true)
			{
				ISupplier<ConcatVector> originalThunk = GetAssignmentValue(assignment);
				originalThunks.SetAssignmentValue(assignment, originalThunk);
				// Construct a new, thinner closure around the cached value
				ConcatVector result = originalThunk.Get();
				SetAssignmentValue(assignment, null);
				// Set the assignment arrays correctly
				if (fastPassByReferenceIterator.MoveNext())
				{
					fastPassByReferenceIterator.Current;
				}
				else
				{
					break;
				}
			}
		}

		/// <summary>
		/// This will release references to the cached ConcatVectors created by cacheVectors(), so that they can be cleaned
		/// up by the GC.
		/// </summary>
		/// <remarks>
		/// This will release references to the cached ConcatVectors created by cacheVectors(), so that they can be cleaned
		/// up by the GC. If no cache was constructed, this is a no-op.
		/// </remarks>
		public virtual void ReleaseCache()
		{
			if (originalThunks != null)
			{
				// OPTIMIZATION:
				// Rather than use the standard iterator, which creates lots of int[] arrays on the heap, which need to be GC'd,
				// we use the fast version that just mutates one array. Since this is read once for us here, this is ideal.
				IEnumerator<int[]> fastPassByReferenceIterator = FastPassByReferenceIterator();
				int[] assignment = fastPassByReferenceIterator.Current;
				while (true)
				{
					SetAssignmentValue(assignment, originalThunks.GetAssignmentValue(assignment));
					// Set the assignment arrays correctly
					if (fastPassByReferenceIterator.MoveNext())
					{
						fastPassByReferenceIterator.Current;
					}
					else
					{
						break;
					}
				}
				// Release our replicated set of original thunks
				originalThunks = null;
			}
		}

		/// <summary>Clones the table, but keeps the values by reference.</summary>
		/// <returns>a new NDArray, a perfect replica of this one</returns>
		public virtual Edu.Stanford.Nlp.Loglinear.Model.ConcatVectorTable CloneTable()
		{
			Edu.Stanford.Nlp.Loglinear.Model.ConcatVectorTable copy = new Edu.Stanford.Nlp.Loglinear.Model.ConcatVectorTable(GetDimensions().MemberwiseClone());
			// OPTIMIZATION:
			// Rather than use the standard iterator, which creates lots of int[] arrays on the heap, which need to be GC'd,
			// we use the fast version that just mutates one array. Since this is read once for us here, this is ideal.
			IEnumerator<int[]> fastPassByReferenceIterator = FastPassByReferenceIterator();
			int[] assignment = fastPassByReferenceIterator.Current;
			while (true)
			{
				copy.SetAssignmentValue(assignment, GetAssignmentValue(assignment));
				// Set the assignment arrays correctly
				if (fastPassByReferenceIterator.MoveNext())
				{
					fastPassByReferenceIterator.Current;
				}
				else
				{
					break;
				}
			}
			return copy;
		}
	}
}
