using System;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A boolean, but for three-valued logics (true / false / unknown).</summary>
	/// <remarks>
	/// A boolean, but for three-valued logics (true / false / unknown).
	/// For most use cases, you can probably use the static values for TRUE, FALSE, and UNKNOWN.
	/// </remarks>
	/// <author>Gabor Angeli</author>
	[System.Serializable]
	public class Trilean
	{
		private const long serialVersionUID = 42L;

		/// <summary>
		/// 0 = false
		/// 1 = true
		/// 2 = unknown
		/// </summary>
		private readonly byte value;

		/// <summary>Construct a new Trilean value.</summary>
		/// <param name="isTrue">Set to true if the value is true. Set to false if the value is false or unknown.</param>
		/// <param name="isFalse">Set to true if the value is false. Set to false if the value is true or unknown.</param>
		public Trilean(bool isTrue, bool isFalse)
		{
			if (isTrue && isFalse)
			{
				throw new ArgumentException("Value cannot be both true and false.");
			}
			if (isTrue)
			{
				value = 1;
			}
			else
			{
				if (isFalse)
				{
					value = 0;
				}
				else
				{
					value = 2;
				}
			}
		}

		/// <summary>The copy constructor.</summary>
		/// <param name="other">The value to copy from.</param>
		public Trilean(Edu.Stanford.Nlp.Util.Trilean other)
		{
			this.value = other.value;
		}

		/// <summary>Returns true if this Trilean is true, and false if it is false or unknown.</summary>
		public virtual bool IsTrue()
		{
			return value == 1;
		}

		/// <summary>Returns true if this Trilean is false, and false if it is true or unknown.</summary>
		public virtual bool IsFalse()
		{
			return value == 0;
		}

		/// <summary>Returns true if this Trilean is either true or false, and false if it is unknown.</summary>
		public virtual bool IsKnown()
		{
			return value != 2;
		}

		/// <summary>Returns true if this Trilean is neither true or false, and false if it is either true or false.</summary>
		public virtual bool IsUnknown()
		{
			return value == 2;
		}

		/// <summary>Convert this Trilean to a boolean, with a specified default value if the truth value is unknown.</summary>
		/// <param name="valueForUnknown">The default value to use if the value of this Trilean is unknown.</param>
		/// <returns>The boolean value of this Trilean.</returns>
		public virtual bool ToBoolean(bool valueForUnknown)
		{
			switch (value)
			{
				case 1:
				{
					return true;
				}

				case 0:
				{
					return false;
				}

				case 2:
				{
					return valueForUnknown;
				}

				default:
				{
					throw new InvalidOperationException("Something went very very wrong.");
				}
			}
		}

		/// <summary>Convert this Trilean to a Boolean, or null if the value is not known.</summary>
		/// <returns>Either True, False, or null.</returns>
		public virtual bool ToBooleanOrNull()
		{
			switch (value)
			{
				case 1:
				{
					return true;
				}

				case 0:
				{
					return false;
				}

				case 2:
				{
					return null;
				}

				default:
				{
					throw new InvalidOperationException("Something went very very wrong.");
				}
			}
		}

		/// <summary>Returns the logical and of this and the other value.</summary>
		/// <param name="other">The value to and this value with.</param>
		public virtual Edu.Stanford.Nlp.Util.Trilean And(Edu.Stanford.Nlp.Util.Trilean other)
		{
			if (this.value == 0 || other.value == 0)
			{
				return False;
			}
			else
			{
				if (this.value == 2 || other.value == 2)
				{
					return Unknown;
				}
				else
				{
					return True;
				}
			}
		}

		/// <summary>Returns the logical or of this and the other value.</summary>
		/// <param name="other">The value to or this value with.</param>
		public virtual Edu.Stanford.Nlp.Util.Trilean Or(Edu.Stanford.Nlp.Util.Trilean other)
		{
			if (this.value == 1 || other.value == 1)
			{
				return True;
			}
			else
			{
				if (this.value == 2 || other.value == 2)
				{
					return Unknown;
				}
				else
				{
					return False;
				}
			}
		}

		/// <summary>Returns the logical not of this value.</summary>
		public virtual Edu.Stanford.Nlp.Util.Trilean Not()
		{
			switch (value)
			{
				case 0:
				{
					return True;
				}

				case 1:
				{
					return False;
				}

				case 2:
				{
					return Unknown;
				}

				default:
				{
					throw new InvalidOperationException("Something went very very wrong.");
				}
			}
		}

		/// <summary>Returns whether this Trilean is equal either to the given Trilean, or the given Boolean.</summary>
		public override bool Equals(object other)
		{
			if (other is Edu.Stanford.Nlp.Util.Trilean)
			{
				return ((Edu.Stanford.Nlp.Util.Trilean)other).value == this.value;
			}
			else
			{
				if (other is bool)
				{
					return From(((bool)other)).value == this.value;
				}
				else
				{
					return false;
				}
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// <p>
		/// Implementation note: this hash code should be consistent with
		/// <see cref="bool.GetHashCode()"/>
		/// .
		/// </p>
		/// </summary>
		public override int GetHashCode()
		{
			if (this.IsTrue())
			{
				return bool.HashCode(true);
			}
			else
			{
				if (this.IsFalse())
				{
					return bool.HashCode(false);
				}
				else
				{
					return byte.HashCode(value);
				}
			}
		}

		/// <summary>Returns a String representation of this Trilean: either "true", "false", or "unknown".</summary>
		public override string ToString()
		{
			if (IsTrue())
			{
				return "true";
			}
			else
			{
				if (IsFalse())
				{
					return "false";
				}
				else
				{
					return "unknown";
				}
			}
		}

		/// <summary>Create the Trilean value for the given Boolean</summary>
		/// <param name="bool">
		/// The boolean to parse, into either
		/// <see cref="True"/>
		/// or
		/// <see cref="False"/>
		/// .
		/// </param>
		/// <returns>
		/// One of
		/// <see cref="True"/>
		/// or
		/// <see cref="False"/>
		/// .
		/// </returns>
		public static Edu.Stanford.Nlp.Util.Trilean From(bool @bool)
		{
			if (@bool)
			{
				return True;
			}
			else
			{
				return False;
			}
		}

		public static Edu.Stanford.Nlp.Util.Trilean FromString(string value)
		{
			switch (value.ToLower())
			{
				case "true":
				case "t":
				{
					return True;
				}

				case "false":
				case "f":
				{
					return False;
				}

				case "unknown":
				case "unk":
				case "u":
				{
					return Unknown;
				}

				default:
				{
					throw new ArgumentException("Cannot parse Trilean from string: " + value);
				}
			}
		}

		/// <summary>The static value for True</summary>
		public static Edu.Stanford.Nlp.Util.Trilean True = new Edu.Stanford.Nlp.Util.Trilean(true, false);

		/// <summary>The static value for False</summary>
		public static Edu.Stanford.Nlp.Util.Trilean False = new Edu.Stanford.Nlp.Util.Trilean(false, true);

		/// <summary>The static value for Unknown (neither true or false)</summary>
		public static Edu.Stanford.Nlp.Util.Trilean Unknown = new Edu.Stanford.Nlp.Util.Trilean(false, false);
	}
}
