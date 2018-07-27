

namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>Indicates that a class supports "pretty logging".</summary>
	/// <remarks>
	/// Indicates that a class supports "pretty logging". Pretty logging is a type of
	/// pretty-printing that uses the Redwood logging system to structure itself.
	/// When pretty logging the contents of your object, you should check to see if
	/// each object (call it <code>obj</code>) is dispatchable with
	/// <code>PrettyLogger.dispatchable(obj)</code> if you don't know their type. If
	/// true, you should call <code>channels.prettyLog(obj)</code> to pretty log it.
	/// Otherwise, use its <code>toString()</code> method.
	/// </remarks>
	/// <seealso cref="PrettyLogger"/>
	/// <author>David McClosky</author>
	public interface IPrettyLoggable
	{
		/// <summary>Pretty logs the current object to specific Redwood channels.</summary>
		/// <param name="channels">
		/// the channels which should be logged to -- all logging calls should
		/// use logging methods on the channels (e.g. channels.log(), etc.)
		/// </param>
		/// <param name="description">
		/// The description of the object. It will potentially identify the
		/// object's functional role or (failing that) its class. This is
		/// typically used as a track name surrounding the contents of this
		/// object.
		/// </param>
		void PrettyLog(Redwood.RedwoodChannels channels, string description);
	}
}
