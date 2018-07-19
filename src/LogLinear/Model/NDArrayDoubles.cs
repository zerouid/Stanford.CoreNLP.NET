using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Model
{
	/// <summary>Created on 9/12/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// Holds and provides access to an N-dimensional array.
	/// <p>
	/// Yes, generics will lead to unfortunate boxing and unboxing in the TableFactor case, we'll handle that if it becomes a
	/// problem.
	/// </author>
	public class NDArrayDoubles : IEnumerable<int[]>
	{
		protected internal int[] dimensions;

		protected internal double[] values;

		/// <summary>Constructor takes a list of neighbor variables to use for this factor.</summary>
		/// <remarks>
		/// Constructor takes a list of neighbor variables to use for this factor. This must not change after construction,
		/// and the number of states of those variables must also not change.
		/// </remarks>
		/// <param name="dimensions">list of neighbor variables assignment range sizes</param>
		public NDArrayDoubles(int[] dimensions)
		{
			// public data
			// OPTIMIZATION:
			// in normal NDArray this is private, but to allow for optimizations we actually leave it as protected
			foreach (int size in dimensions)
			{
				System.Diagnostics.Debug.Assert((size > 0));
			}
			this.dimensions = dimensions;
			values = new double[CombinatorialNeighborStatesCount()];
		}

		/// <summary>This is to enable the partially observed constructor for TableFactor.</summary>
		/// <remarks>
		/// This is to enable the partially observed constructor for TableFactor. It's an ugly break of modularity, but seems
		/// to be necessary if we want to keep the constructor for TableFactor with partial observations relatively simple.
		/// </remarks>
		protected internal NDArrayDoubles()
		{
		}

		/// <summary>Set a single value in the factor table.</summary>
		/// <param name="assignment">a list of variable settings, in the same order as the neighbors array of the factor</param>
		/// <param name="value">the value to put into the factor table</param>
		public virtual void SetAssignmentValue(int[] assignment, double value)
		{
			values[GetTableAccessOffset(assignment)] = value;
		}

		/// <summary>Retrieve a single value for an assignment.</summary>
		/// <param name="assignment">a list of variable settings, in the same order as the neighbors array of the factor</param>
		/// <returns>the value for the given assignment. Can be null if not been set yet.</returns>
		public virtual double GetAssignmentValue(int[] assignment)
		{
			return values[GetTableAccessOffset(assignment)];
		}

		/// <returns>the size array of the neighbors of the feature factor, passed by value to ensure immutability.</returns>
		public virtual int[] GetDimensions()
		{
			return dimensions.MemberwiseClone();
		}

		/// <summary>
		/// WARNING: This is pass by reference to avoid massive GC overload during heavy iterations, and because the standard
		/// use case is to use the assignments array as an accessor.
		/// </summary>
		/// <remarks>
		/// WARNING: This is pass by reference to avoid massive GC overload during heavy iterations, and because the standard
		/// use case is to use the assignments array as an accessor. Please, clone if you save a copy, otherwise the array
		/// will mutate underneath you.
		/// </remarks>
		/// <returns>an iterator over all possible assignments to this factor</returns>
		public virtual IEnumerator<int[]> GetEnumerator()
		{
			return new _IEnumerator_79(this);
		}

		private sealed class _IEnumerator_79 : IEnumerator<int[]>
		{
			public _IEnumerator_79()
			{
				this.@unsafe = this._enclosing.FastPassByReferenceIterator();
			}

			internal IEnumerator<int[]> @unsafe;

			public bool MoveNext()
			{
				return this.@unsafe.MoveNext();
			}

			public int[] Current
			{
				get
				{
					return this.@unsafe.Current.MemberwiseClone();
				}
			}
		}

		/// <summary>
		/// This is its own function because people will inevitably attempt this optimization of not cloning the array we
		/// hand to the iterator, to save on GC, and it should not be default behavior.
		/// </summary>
		/// <remarks>
		/// This is its own function because people will inevitably attempt this optimization of not cloning the array we
		/// hand to the iterator, to save on GC, and it should not be default behavior. If you know what you're doing, then
		/// this may be the iterator for you.
		/// </remarks>
		/// <returns>an iterator that will mutate the value it returns to you, so you must clone if you want to keep a copy</returns>
		public virtual IEnumerator<int[]> FastPassByReferenceIterator()
		{
			int[] assignments = new int[dimensions.Length];
			if (dimensions.Length > 0)
			{
				assignments[0] = -1;
			}
			return new _IEnumerator_105(this, assignments);
		}

		private sealed class _IEnumerator_105 : IEnumerator<int[]>
		{
			public _IEnumerator_105(NDArrayDoubles _enclosing, int[] assignments)
			{
				this._enclosing = _enclosing;
				this.assignments = assignments;
			}

			public bool MoveNext()
			{
				for (int i = 0; i < assignments.Length; i++)
				{
					if (assignments[i] < this._enclosing.dimensions[i] - 1)
					{
						return true;
					}
				}
				return false;
			}

			public int[] Current
			{
				get
				{
					// Add one to the first position
					assignments[0]++;
					// Carry any resulting overflow all the way to the end.
					for (int i = 0; i < assignments.Length; i++)
					{
						if (assignments[i] >= this._enclosing.dimensions[i])
						{
							assignments[i] = 0;
							if (i < assignments.Length - 1)
							{
								assignments[i + 1]++;
							}
						}
						else
						{
							break;
						}
					}
					return assignments;
				}
			}

			private readonly NDArrayDoubles _enclosing;

			private readonly int[] assignments;
		}

		/// <returns>the total number of states this factor must represent to include all neighbors.</returns>
		public virtual int CombinatorialNeighborStatesCount()
		{
			int c = 1;
			foreach (int n in dimensions)
			{
				c *= n;
			}
			return c;
		}

		////////////////////////////////////////////////////////////////////////////
		// PRIVATE IMPLEMENTATION
		////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Compute the distance into the one dimensional factorTable array that corresponds to a setting of all the
		/// neighbors of the factor.
		/// </summary>
		/// <param name="assignment">assignment indices, in same order as neighbors array</param>
		/// <returns>the offset index</returns>
		private int GetTableAccessOffset(int[] assignment)
		{
			System.Diagnostics.Debug.Assert((assignment.Length == dimensions.Length));
			int offset = 0;
			for (int i = 0; i < assignment.Length; i++)
			{
				System.Diagnostics.Debug.Assert((assignment[i] < dimensions[i]));
				offset = (offset * dimensions[i]) + assignment[i];
			}
			return offset;
		}
	}
}
