using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Character-level utilities.</summary>
	/// <author>Dan Klein</author>
	/// <author>Spence Green</author>
	public sealed class Characters
	{
		/// <summary>Only static methods</summary>
		private Characters()
		{
		}

		// TODO(spenceg) This method used to cache the lookup, in this package,
		// but actually the valueOf method performs internal caching. This method
		// should be removed.
		public static char GetCharacter(char c)
		{
			return char.ValueOf(c);
		}

		/// <summary>Map a String to an array of type Character.</summary>
		/// <param name="s">The String to map</param>
		/// <returns>An array of Character</returns>
		public static char[] AsCharacterArray(string s)
		{
			char[] split = new char[s.Length];
			for (int i = 0; i < split.Length; i++)
			{
				split[i] = GetCharacter(s[i]);
			}
			return split;
		}

		/// <summary>
		/// Returns a string representation of a character's unicode
		/// block.
		/// </summary>
		/// <param name="c"/>
		/// <returns/>
		public static string UnicodeBlockStringOf(char c)
		{
			Character.Subset block = Character.UnicodeBlock.Of(c);
			return block == null ? "Undefined" : block.ToString();
		}

		/// <summary>
		/// Returns true if a character is punctuation, and false
		/// otherwise.
		/// </summary>
		/// <param name="c"/>
		/// <returns/>
		public static bool IsPunctuation(char c)
		{
			int cType = char.GetType(c);
			return cType == char.StartPunctuation || cType == char.EndPunctuation || cType == char.OtherPunctuation || cType == char.ConnectorPunctuation || cType == char.DashPunctuation || cType == char.InitialQuotePunctuation || cType == char.FinalQuotePunctuation;
		}

		/// <summary>
		/// Returns true if a character is a symbol, and false
		/// otherwise.
		/// </summary>
		/// <param name="c"/>
		/// <returns/>
		public static bool IsSymbol(char c)
		{
			int cType = char.GetType(c);
			return cType == char.MathSymbol || cType == char.CurrencySymbol || cType == char.ModifierSymbol || cType == char.OtherSymbol;
		}

		/// <summary>
		/// Returns true if a character is a control character, and
		/// false otherwise.
		/// </summary>
		/// <param name="c"/>
		/// <returns/>
		public static bool IsControl(char c)
		{
			return char.GetType(c) == char.Control;
		}
	}
}
