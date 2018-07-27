using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.IE.Machinereading.Structure
{
	/// <summary>
	/// Stores the offsets for a span of text
	/// Offsets may indicate either token or byte positions
	/// Start is inclusive, end is exclusive
	/// </summary>
	/// <author>Mihai</author>
	[System.Serializable]
	public class Span : IEnumerable<int>
	{
		private const long serialVersionUID = -3861451490217976693L;

		private int start;

		private int end;

		/// <summary>For Kryo serializer</summary>
		private Span()
		{
		}

		/// <summary>This assumes that s &lt;= e.</summary>
		/// <remarks>This assumes that s &lt;= e.  Use fromValues if you can't guarantee this.</remarks>
		public Span(int s, int e)
		{
			start = s;
			end = e;
		}

		/// <summary>Creates a span that encloses all spans in the argument list.</summary>
		/// <remarks>Creates a span that encloses all spans in the argument list.  Behavior is undefined if given no arguments.</remarks>
		public Span(params Edu.Stanford.Nlp.IE.Machinereading.Structure.Span[] spans)
			: this(int.MaxValue, int.MinValue)
		{
			foreach (Edu.Stanford.Nlp.IE.Machinereading.Structure.Span span in spans)
			{
				ExpandToInclude(span);
			}
		}

		/// <summary>Safe way to construct Spans if you're not sure which value is higher.</summary>
		public static Edu.Stanford.Nlp.IE.Machinereading.Structure.Span FromValues(int val1, int val2)
		{
			if (val1 <= val2)
			{
				return new Edu.Stanford.Nlp.IE.Machinereading.Structure.Span(val1, val2);
			}
			else
			{
				return new Edu.Stanford.Nlp.IE.Machinereading.Structure.Span(val2, val1);
			}
		}

		public static Edu.Stanford.Nlp.IE.Machinereading.Structure.Span FromValues(params object[] values)
		{
			if (values.Length == 1)
			{
				return FromValues(values[0], values[0] is Number ? ((Number)values[0]) + 1 : System.Convert.ToInt32(values[0].ToString()) + 1);
			}
			if (values.Length != 2)
			{
				throw new ArgumentException("fromValues() must take an array with 2 elements");
			}
			int val1;
			if (values[0] is Number)
			{
				val1 = ((Number)values[0]);
			}
			else
			{
				if (values[0] is string)
				{
					val1 = System.Convert.ToInt32((string)values[0]);
				}
				else
				{
					throw new ArgumentException("Unknown value for span: " + values[0]);
				}
			}
			int val2;
			if (values[1] is Number)
			{
				val2 = ((Number)values[1]);
			}
			else
			{
				if (values[0] is string)
				{
					val2 = System.Convert.ToInt32((string)values[1]);
				}
				else
				{
					throw new ArgumentException("Unknown value for span: " + values[1]);
				}
			}
			return FromValues(val1, val2);
		}

		public virtual int Start()
		{
			return start;
		}

		public virtual int End()
		{
			return end;
		}

		public virtual void SetStart(int s)
		{
			start = s;
		}

		public virtual void SetEnd(int e)
		{
			end = e;
		}

		public override bool Equals(object other)
		{
			if (!(other is Edu.Stanford.Nlp.IE.Machinereading.Structure.Span))
			{
				return false;
			}
			Edu.Stanford.Nlp.IE.Machinereading.Structure.Span otherSpan = (Edu.Stanford.Nlp.IE.Machinereading.Structure.Span)other;
			return start == otherSpan.start && end == otherSpan.end;
		}

		public override int GetHashCode()
		{
			return (new Pair<int, int>(start, end)).GetHashCode();
		}

		public override string ToString()
		{
			return "[" + start + "," + end + ")";
		}

		public virtual void ExpandToInclude(Edu.Stanford.Nlp.IE.Machinereading.Structure.Span otherSpan)
		{
			if (otherSpan.Start() < start)
			{
				SetStart(otherSpan.Start());
			}
			if (otherSpan.End() > end)
			{
				SetEnd(otherSpan.End());
			}
		}

		/// <summary>Returns true if this span contains otherSpan.</summary>
		/// <remarks>Returns true if this span contains otherSpan.  Endpoints on spans may match.</remarks>
		public virtual bool Contains(Edu.Stanford.Nlp.IE.Machinereading.Structure.Span otherSpan)
		{
			return this.start <= otherSpan.start && otherSpan.end <= this.end;
		}

		/// <summary>Returns true if i is inside this span.</summary>
		/// <remarks>Returns true if i is inside this span.  Note that the start is inclusive and the end is exclusive.</remarks>
		public virtual bool Contains(int i)
		{
			return this.start <= i && i < this.end;
		}

		/// <summary>Returns true if this span ends before the otherSpan starts.</summary>
		/// <exception cref="System.ArgumentException">if either span contains the other span</exception>
		public virtual bool IsBefore(Edu.Stanford.Nlp.IE.Machinereading.Structure.Span otherSpan)
		{
			if (this.Contains(otherSpan) || otherSpan.Contains(this))
			{
				throw new ArgumentException("Span " + ToString() + " contains otherSpan " + otherSpan + " (or vice versa)");
			}
			return this.end <= otherSpan.start;
		}

		/// <summary>Returns true if this span starts after the otherSpan's end.</summary>
		/// <exception cref="System.ArgumentException">if either span contains the other span</exception>
		public virtual bool IsAfter(Edu.Stanford.Nlp.IE.Machinereading.Structure.Span otherSpan)
		{
			if (this.Contains(otherSpan) || otherSpan.Contains(this))
			{
				throw new ArgumentException("Span " + ToString() + " contains otherSpan " + otherSpan + " (or vice versa)");
			}
			return this.start >= otherSpan.end;
		}

		/// <summary>Move a span by the given amount.</summary>
		/// <remarks>Move a span by the given amount. Useful for, e.g., switching between 0- and 1-indexed spans.</remarks>
		/// <param name="diff">The difference to ADD to both the beginning and end of the span. So, -1 moves the span left by one.</param>
		/// <returns>A new span, offset by the given difference.</returns>
		public virtual Edu.Stanford.Nlp.IE.Machinereading.Structure.Span Translate(int diff)
		{
			return new Edu.Stanford.Nlp.IE.Machinereading.Structure.Span(start + diff, end + diff);
		}

		/// <summary>Convert an end-exclusive span to an end-inclusive span.</summary>
		public virtual Edu.Stanford.Nlp.IE.Machinereading.Structure.Span ToInclusive()
		{
			System.Diagnostics.Debug.Assert(end > start);
			return new Edu.Stanford.Nlp.IE.Machinereading.Structure.Span(start, end - 1);
		}

		/// <summary>Convert an end-inclusive span to an end-exclusive span.</summary>
		public virtual Edu.Stanford.Nlp.IE.Machinereading.Structure.Span ToExclusive()
		{
			return new Edu.Stanford.Nlp.IE.Machinereading.Structure.Span(start, end + 1);
		}

		public virtual IEnumerator<int> GetEnumerator()
		{
			return new _IEnumerator_171(this);
		}

		private sealed class _IEnumerator_171 : IEnumerator<int>
		{
			public _IEnumerator_171(Span _enclosing)
			{
				this._enclosing = _enclosing;
				this.nextIndex = this._enclosing.start;
			}

			internal int nextIndex;

			public bool MoveNext()
			{
				return this.nextIndex < this._enclosing.end;
			}

			public int Current
			{
				get
				{
					if (!this.MoveNext())
					{
						throw new NoSuchElementException();
					}
					this.nextIndex += 1;
					return this.nextIndex - 1;
				}
			}

			public void Remove()
			{
				throw new NotSupportedException();
			}

			private readonly Span _enclosing;
		}

		public virtual int Size()
		{
			return end - start;
		}

		public static bool Overlaps(Edu.Stanford.Nlp.IE.Machinereading.Structure.Span spanA, Edu.Stanford.Nlp.IE.Machinereading.Structure.Span spanB)
		{
			return spanA.Contains(spanB) || spanB.Contains(spanA) || (spanA.end > spanB.end && spanA.start < spanB.end) || (spanB.end > spanA.end && spanB.start < spanA.end) || spanA.Equals(spanB);
		}

		public static int Overlap(Edu.Stanford.Nlp.IE.Machinereading.Structure.Span spanA, Edu.Stanford.Nlp.IE.Machinereading.Structure.Span spanB)
		{
			if (spanA.Contains(spanB))
			{
				return Math.Min(spanA.end - spanA.start, spanB.end - spanB.start);
			}
			else
			{
				if (spanA.Equals(spanB))
				{
					return spanA.end - spanA.start;
				}
				else
				{
					if ((spanA.end > spanB.end && spanA.start < spanB.end) || (spanB.end > spanA.end && spanB.start < spanA.end))
					{
						return Math.Min(spanA.end, spanB.end) - Math.Max(spanA.start, spanB.start);
					}
					else
					{
						return 0;
					}
				}
			}
		}

		public static bool Overlaps(Edu.Stanford.Nlp.IE.Machinereading.Structure.Span spanA, ICollection<Edu.Stanford.Nlp.IE.Machinereading.Structure.Span> spanB)
		{
			foreach (Edu.Stanford.Nlp.IE.Machinereading.Structure.Span candidate in spanB)
			{
				if (Overlaps(spanA, candidate))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Returns the smallest distance between two spans.</summary>
		public static int Distance(Edu.Stanford.Nlp.IE.Machinereading.Structure.Span a, Edu.Stanford.Nlp.IE.Machinereading.Structure.Span b)
		{
			if (Edu.Stanford.Nlp.IE.Machinereading.Structure.Span.Overlaps(a, b))
			{
				return 0;
			}
			else
			{
				if (a.Contains(b) || b.Contains(a))
				{
					return 0;
				}
				else
				{
					if (a.IsBefore(b))
					{
						return b.start - a.end;
					}
					else
					{
						if (b.IsBefore(a))
						{
							return a.start - b.end;
						}
						else
						{
							throw new InvalidOperationException("This should be impossible...");
						}
					}
				}
			}
		}

		/// <summary>A silly translation between a pair and a span.</summary>
		public static Edu.Stanford.Nlp.IE.Machinereading.Structure.Span FromPair(Pair<int, int> span)
		{
			return FromValues(span.first, span.second);
		}

		/// <summary>Another silly translation between a pair and a span.</summary>
		public static Edu.Stanford.Nlp.IE.Machinereading.Structure.Span FromPair(IntPair span)
		{
			return FromValues(span.GetSource(), span.GetTarget());
		}

		/// <summary>A silly translation between a pair and a span.</summary>
		public static Edu.Stanford.Nlp.IE.Machinereading.Structure.Span FromPairOneIndexed(Pair<int, int> span)
		{
			return FromValues(span.first - 1, span.second - 1);
		}

		/// <summary>The union of two spans.</summary>
		/// <remarks>The union of two spans. That is, the minimal span that contains both.</remarks>
		public static Edu.Stanford.Nlp.IE.Machinereading.Structure.Span Union(Edu.Stanford.Nlp.IE.Machinereading.Structure.Span a, Edu.Stanford.Nlp.IE.Machinereading.Structure.Span b)
		{
			return Edu.Stanford.Nlp.IE.Machinereading.Structure.Span.FromValues(Math.Min(a.start, b.start), Math.Max(a.end, b.end));
		}
	}
}
