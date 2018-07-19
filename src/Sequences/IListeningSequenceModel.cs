using Sharpen;

namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>
	/// This is simply a conjunctive interface for something that is
	/// a SequenceModel and a SequenceListener.
	/// </summary>
	/// <remarks>
	/// This is simply a conjunctive interface for something that is
	/// a SequenceModel and a SequenceListener. This is useful to have
	/// because models used in Gibbs sampling have to implement both
	/// these interfaces.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public interface IListeningSequenceModel : ISequenceModel, ISequenceListener
	{
	}
}
