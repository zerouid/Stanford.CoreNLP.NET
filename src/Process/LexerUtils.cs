using System;
using Java.Lang;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>This class contains various static utility methods invoked by JFlex lexers.</summary>
	/// <remarks>
	/// This class contains various static utility methods invoked by JFlex lexers.
	/// Having this utility code placed outside the lexers facilitates normal
	/// IDE code editing.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class LexerUtils
	{
		private LexerUtils()
		{
		}

		private static readonly Pattern CentsPattern = Pattern.Compile("\u00A2");

		private static readonly Pattern PoundPattern = Pattern.Compile("\u00A3");

		private static readonly Pattern GenericCurrencyPattern = Pattern.Compile("[\u0080\u00A4\u20A0\u20AC\u20B9]");

		private static readonly Pattern Cp1252EuroPattern = Pattern.Compile("\u0080");

		// static methods
		public static string NormalizeCurrency(string @in)
		{
			string s1 = @in;
			s1 = CentsPattern.Matcher(s1).ReplaceAll("cents");
			s1 = PoundPattern.Matcher(s1).ReplaceAll("#");
			// historically used for pound in PTB3
			s1 = GenericCurrencyPattern.Matcher(s1).ReplaceAll("\\$");
			// Euro (ECU, generic currency)  -- no good translation!
			return s1;
		}

		/// <summary>Still at least turn cp1252 euro symbol to Unicode one.</summary>
		public static string MinimallyNormalizeCurrency(string @in)
		{
			string s1 = @in;
			s1 = Cp1252EuroPattern.Matcher(s1).ReplaceAll("\u20AC");
			return s1;
		}

		public static string RemoveSoftHyphens(string @in)
		{
			// \u00AD is the soft hyphen character, which we remove, regarding it as inserted only for line-breaking
			if (@in.IndexOf('\u00AD') < 0)
			{
				// shortcut doing work
				return @in;
			}
			int length = @in.Length;
			StringBuilder @out = new StringBuilder(length - 1);
			/*
			// This isn't necessary, as BMP, low, and high surrogate encodings are disjoint!
			for (int offset = 0, cp; offset < length; offset += Character.charCount(cp)) {
			cp = in.codePointAt(offset);
			if (cp != '\u00AD') {
			out.appendCodePoint(cp);
			}
			}
			*/
			for (int i = 0; i < length; i++)
			{
				char ch = @in[i];
				if (ch != '\u00AD')
				{
					@out.Append(ch);
				}
			}
			if (@out.Length == 0)
			{
				@out.Append('-');
			}
			// don't create an empty token, put in a regular hyphen
			return @out.ToString();
		}

		/* CP1252: dagger, double dagger, per mille, bullet, small tilde, trademark */
		public static string ProcessCp1252misc(string arg)
		{
			switch (arg)
			{
				case "\u0086":
				{
					return "\u2020";
				}

				case "\u0087":
				{
					return "\u2021";
				}

				case "\u0089":
				{
					return "\u2030";
				}

				case "\u0095":
				{
					return "\u2022";
				}

				case "\u0098":
				{
					return "\u02DC";
				}

				case "\u0099":
				{
					return "\u2122";
				}

				default:
				{
					throw new ArgumentException("Bad process cp1252");
				}
			}
		}

		private static readonly Pattern AmpPattern = Pattern.Compile("(?i:&amp;)");

		/// <summary>Convert an XML-escaped ampersand back into an ampersand.</summary>
		public static string NormalizeAmp(string @in)
		{
			return AmpPattern.Matcher(@in).ReplaceAll("&");
		}
	}
}
