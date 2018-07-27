

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// Something that implements the
	/// <c>HasOffset</c>
	/// interface
	/// carries char offset references to an original text String.
	/// </summary>
	/// <author>Richard Eckart (Technische Universitat Darmstadt)</author>
	public interface IHasOffset
	{
		/// <summary>Return the beginning char offset of the label (or -1 if none).</summary>
		/// <remarks>
		/// Return the beginning char offset of the label (or -1 if none).
		/// Note that these are currently measured in terms of UTF-16 char offsets, not codepoints,
		/// so that when non-BMP Unicode characters are present, such a character will add 2 to
		/// the position. On the other hand, these values will work with String#substring() and
		/// you can then calculate the number of codepoints in a substring.
		/// </remarks>
		/// <returns>the beginning position for the label</returns>
		int BeginPosition();

		/// <summary>Set the beginning character offset for the label.</summary>
		/// <remarks>
		/// Set the beginning character offset for the label.
		/// Setting this key to "-1" can be used to indicate no valid value.
		/// </remarks>
		/// <param name="beginPos">The beginning position</param>
		void SetBeginPosition(int beginPos);

		/// <summary>Return the ending char offset of the label (or -1 if none).</summary>
		/// <remarks>
		/// Return the ending char offset of the label (or -1 if none).
		/// As usual in Java, this is the offset of the char <i>after</i> this token.
		/// Note that these are currently measured in terms of UTF-16 char offsets, not codepoints,
		/// so that when non-BMP Unicode characters are present, such a character will add 2 to
		/// the position. On the other hand, these values will work with String#substring() and
		/// you can then calculate the number of codepoints in a substring.
		/// </remarks>
		/// <returns>the end position for the label</returns>
		int EndPosition();

		/// <summary>Set the ending character offset of the label (or -1 if none).</summary>
		/// <param name="endPos">The end character offset for the label</param>
		void SetEndPosition(int endPos);
	}
}
