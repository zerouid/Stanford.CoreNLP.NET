using System.Collections.Generic;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A quintuple (length five) of ordered objects.</summary>
	/// <author>Spence Green</author>
	/// <?/>
	/// <?/>
	/// <?/>
	/// <?/>
	[System.Serializable]
	public class Quintuple<T1, T2, T3, T4, T5> : IComparable<Edu.Stanford.Nlp.Util.Quintuple<T1, T2, T3, T4, T5>>, IPrettyLoggable
	{
		private const long serialVersionUID = 6295043666955910662L;

		public T1 first;

		public T2 second;

		public T3 third;

		public T4 fourth;

		public T5 fifth;

		public Quintuple(T1 first, T2 second, T3 third, T4 fourth, T5 fifth)
		{
			this.first = first;
			this.second = second;
			this.third = third;
			this.fourth = fourth;
			this.fifth = fifth;
		}

		public virtual T1 First()
		{
			return first;
		}

		public virtual T2 Second()
		{
			return second;
		}

		public virtual T3 Third()
		{
			return third;
		}

		public virtual T4 Fourth()
		{
			return fourth;
		}

		public virtual T5 Fifth()
		{
			return fifth;
		}

		public virtual void SetFirst(T1 o)
		{
			first = o;
		}

		public virtual void SetSecond(T2 o)
		{
			second = o;
		}

		public virtual void SetThird(T3 o)
		{
			third = o;
		}

		public virtual void SetFourth(T4 o)
		{
			fourth = o;
		}

		public virtual void SetFifth(T5 fifth)
		{
			this.fifth = fifth;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Util.Quintuple))
			{
				return false;
			}
			Edu.Stanford.Nlp.Util.Quintuple<T1, T2, T3, T4, T5> quadruple = ErasureUtils.UncheckedCast(o);
			if (first != null ? !first.Equals(quadruple.first) : quadruple.first != null)
			{
				return false;
			}
			if (second != null ? !second.Equals(quadruple.second) : quadruple.second != null)
			{
				return false;
			}
			if (third != null ? !third.Equals(quadruple.third) : quadruple.third != null)
			{
				return false;
			}
			if (fourth != null ? !fourth.Equals(quadruple.fourth) : quadruple.fourth != null)
			{
				return false;
			}
			if (fifth != null ? !fifth.Equals(quadruple.fifth) : quadruple.fifth != null)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result = 17;
			result = (first != null ? first.GetHashCode() : 0);
			result = 29 * result + (second != null ? second.GetHashCode() : 0);
			result = 29 * result + (third != null ? third.GetHashCode() : 0);
			result = 29 * result + (fourth != null ? fourth.GetHashCode() : 0);
			result = 29 * result + (fifth != null ? fifth.GetHashCode() : 0);
			return result;
		}

		public override string ToString()
		{
			return "(" + first + "," + second + "," + third + "," + fourth + "," + fifth + ")";
		}

		/// <summary>Returns a Quadruple constructed from T1, T2, T3, and T4.</summary>
		/// <remarks>
		/// Returns a Quadruple constructed from T1, T2, T3, and T4. Convenience
		/// method; the compiler will disambiguate the classes used for you so that you
		/// don't have to write out potentially long class names.
		/// </remarks>
		public static Edu.Stanford.Nlp.Util.Quintuple<T1, T2, T3, T4, T5> MakeQuadruple<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
		{
			return new Edu.Stanford.Nlp.Util.Quintuple<T1, T2, T3, T4, T5>(t1, t2, t3, t4, t5);
		}

		public virtual IList<object> AsList()
		{
			return CollectionUtils.MakeList(first, second, third, fourth, fifth);
		}

		public virtual int CompareTo(Edu.Stanford.Nlp.Util.Quintuple<T1, T2, T3, T4, T5> another)
		{
			int comp = ((IComparable<T1>)First()).CompareTo(another.First());
			if (comp != 0)
			{
				return comp;
			}
			else
			{
				comp = ((IComparable<T2>)Second()).CompareTo(another.Second());
				if (comp != 0)
				{
					return comp;
				}
				else
				{
					comp = ((IComparable<T3>)Third()).CompareTo(another.Third());
					if (comp != 0)
					{
						return comp;
					}
					else
					{
						comp = ((IComparable<T4>)Fourth()).CompareTo(another.Fourth());
						if (comp != 0)
						{
							return comp;
						}
						else
						{
							return ((IComparable<T5>)Fifth()).CompareTo(another.Fifth());
						}
					}
				}
			}
		}

		/// <summary><inheritDoc/></summary>
		public virtual void PrettyLog(Redwood.RedwoodChannels channels, string description)
		{
			PrettyLogger.Log(channels, description, this.AsList());
		}
	}
}
