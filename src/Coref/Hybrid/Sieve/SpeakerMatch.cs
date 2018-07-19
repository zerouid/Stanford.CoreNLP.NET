using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	[System.Serializable]
	public class SpeakerMatch : DeterministicCorefSieve
	{
		public SpeakerMatch()
			: base()
		{
			flags.UseSpeakermatch = true;
		}
	}
}
