


namespace Edu.Stanford.Nlp.Math
{
	/// <summary>This file includes a regular expression to match numbers.</summary>
	/// <remarks>
	/// This file includes a regular expression to match numbers.  This
	/// will save quite a bit of time in places where you want to test if
	/// something is a number without wasting the time to parse it or throw
	/// an exception if it isn't.  For example, you can call isDouble() to
	/// see if a String is a double without having to try/catch the
	/// NumberFormatException that gets produced if it is not.
	/// The regular expression is conveniently provided in the javadoc for Double.
	/// http://java.sun.com/javase/6/docs/api/java/lang/Double.html
	/// </remarks>
	/// <author>
	/// John Bauer
	/// (sort of)
	/// </author>
	public class NumberMatchingRegex
	{
		private NumberMatchingRegex()
		{
		}

		internal static readonly Pattern decintPattern = Pattern.Compile("[+-]?\\d+");

		/// <summary>
		/// Tests to see if an integer is a decimal integer,
		/// perhaps starting with +/-.
		/// </summary>
		public static bool IsDecimalInteger(string @string)
		{
			return (decintPattern.Matcher(@string).Matches());
		}

		internal const string Digits = "(\\p{Digit}+)";

		internal const string HexDigits = "(\\p{XDigit}+)";

		internal const string Exp = "[eE][+-]?" + Digits;

		internal const string fpRegex = ("[\\x00-\\x20]*" + "[+-]?(" + "NaN|" + "Infinity|" + "(((" + Digits + "(\\.)?(" + Digits + "?)(" + Exp + ")?)|" + "(\\.(" + Digits + ")(" + Exp + ")?)|" + "((" + "(0[xX]" + HexDigits + "(\\.)?)|" + "(0[xX]" +
			 HexDigits + "?(\\.)" + HexDigits + ")" + ")[pP][+-]?" + Digits + "))" + "[fFdD]?))" + "[\\x00-\\x20]*");

		internal static readonly Pattern fpPattern = Pattern.Compile(fpRegex);

		// an exponent is 'e' or 'E' followed by an optionally 
		// signed decimal integer.
		// Optional leading "whitespace"
		// Optional sign character
		// "NaN" string
		// "Infinity" string
		// A decimal floating-point string representing a finite positive
		// number without a leading sign has at most five basic pieces:
		// Digits . Digits ExponentPart FloatTypeSuffix
		// 
		// Since this method allows integer-only strings as input
		// in addition to strings of floating-point literals, the
		// two sub-patterns below are simplifications of the grammar
		// productions from the Java Language Specification, 2nd 
		// edition, section 3.10.2.
		// Digits ._opt Digits_opt ExponentPart_opt FloatTypeSuffix_opt
		// . Digits ExponentPart_opt FloatTypeSuffix_opt
		// Hexadecimal strings
		// 0[xX] HexDigits ._opt BinaryExponent FloatTypeSuffix_opt
		// 0[xX] HexDigits_opt . HexDigits BinaryExponent FloatTypeSuffix_opt
		// Optional trailing "whitespace"
		/// <summary>Returns true if the number can be successfully parsed by Double.</summary>
		/// <remarks>
		/// Returns true if the number can be successfully parsed by Double.
		/// Locale specific to English and ascii numerals.
		/// </remarks>
		public static bool IsDouble(string @string)
		{
			return (fpPattern.Matcher(@string).Matches());
		}
	}
}
