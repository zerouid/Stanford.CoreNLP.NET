

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Scored: This is a simple interface that says that an object can answer
	/// requests for the score, or goodness of the object.
	/// </summary>
	/// <remarks>
	/// Scored: This is a simple interface that says that an object can answer
	/// requests for the score, or goodness of the object.
	/// <p>
	/// JavaNLP includes companion classes
	/// <see cref="ScoredObject{T}"/>
	/// which is a simple
	/// composite of another object and a score, and
	/// <see cref="ScoredComparator"/>
	/// which compares Scored objects.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <version>12/4/2000</version>
	public interface IScored
	{
		/// <returns>The score of this thing.</returns>
		double Score();
	}
}
