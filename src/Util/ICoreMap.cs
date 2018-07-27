using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Base type for all annotatable core objects.</summary>
	/// <remarks>
	/// Base type for all annotatable core objects. Should usually be instantiated as
	/// <see cref="ArrayCoreMap"/>
	/// . Many common key definitions live in
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations"/>
	/// , but others may be defined elsewhere. See
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations"/>
	/// for details.
	/// Note that implementations of this interface must take care to implement
	/// equality correctly: by default, two CoreMaps are .equal if they contain the
	/// same keys and all corresponding values are .equal. Subclasses that wish to
	/// change this behavior (such as
	/// <see cref="HashableCoreMap"/>
	/// ) must make sure that
	/// all other CoreMap implementations have a special case in their .equals to use
	/// that equality definition when appropriate. Similarly, care must be taken when
	/// defining hashcodes. The default hashcode is 37 * sum of all keys' hashcodes
	/// plus the sum of all values' hashcodes. However, use of this class as HashMap
	/// keys is discouraged because the hashcode can change over time. Consider using
	/// a
	/// <see cref="HashableCoreMap"/>
	/// .
	/// </remarks>
	/// <author>dramage</author>
	/// <author>rafferty</author>
	public interface ICoreMap : ITypesafeMap, IPrettyLoggable
	{
		/// <summary>
		/// Attempt to provide a briefer and more human readable String for the contents of
		/// a CoreMap.
		/// </summary>
		/// <remarks>
		/// Attempt to provide a briefer and more human readable String for the contents of
		/// a CoreMap.
		/// The method may not be capable of printing circular dependencies in CoreMaps.
		/// </remarks>
		/// <param name="what">
		/// An array (varargs) of Strings that say what annotation keys
		/// to print.  These need to be provided in a shortened form where you
		/// are just giving the part of the class name without package and up to
		/// "Annotation". That is,
		/// edu.stanford.nlp.ling.CoreAnnotations.PartOfSpeechAnnotation ➔ PartOfSpeech .
		/// As a special case, an empty array means to print everything, not nothing.
		/// </param>
		/// <returns>
		/// A more human readable String giving possibly partial contents of a
		/// CoreMap.
		/// </returns>
		string ToShorterString(params string[] what);
	}
}
