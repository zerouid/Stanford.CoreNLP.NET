using System;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	[System.Serializable]
	public class MatchException : Exception
	{
		public const long serialVersionUID = 24362462L;

		public MatchException(string m)
			: base(m)
		{
		}
	}
}
