using Sharpen;

namespace Edu.Stanford.Nlp.Util.Concurrent
{
	/// <summary>
	/// Objects that wish to use MulticoreWrapper for multicore support must implement
	/// this interface.
	/// </summary>
	/// <remarks>
	/// Objects that wish to use MulticoreWrapper for multicore support must implement
	/// this interface. Objects that implement this interface should, of course, be threadsafe.
	/// </remarks>
	/// <author>Spence Green</author>
	/// <?/>
	/// <?/>
	public interface IThreadsafeProcessor<I, O>
	{
		/// <summary>
		/// Set the input item that will be processed when a thread is allocated to
		/// this processor.
		/// </summary>
		/// <param name="input">the object to be processed</param>
		/// <returns>the result of the processing</returns>
		O Process(I input);

		/// <summary>Return a new threadsafe instance.</summary>
		IThreadsafeProcessor<I, O> NewInstance();
	}
}
