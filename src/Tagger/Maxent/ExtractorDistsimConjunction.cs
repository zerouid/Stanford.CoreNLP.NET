


namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>Extractor for adding a conjunction of distsim information.</summary>
	/// <author>rafferty</author>
	[System.Serializable]
	public class ExtractorDistsimConjunction : Extractor
	{
		private const long serialVersionUID = 1L;

		private readonly Distsim lexicon;

		private readonly int left;

		private readonly int right;

		private readonly string name;

		internal override string Extract(History h, PairsHolder pH)
		{
			StringBuilder sb = new StringBuilder();
			for (int j = left; j <= right; j++)
			{
				string word = pH.GetWord(h, j);
				string distSim = lexicon.GetMapping(word);
				sb.Append(distSim);
				if (j < right)
				{
					sb.Append('|');
				}
			}
			return sb.ToString();
		}

		/// <summary>Create an Extractor for conjunctions of Distsim classes</summary>
		/// <param name="distSimPath">
		/// File path. If it contains a semi-colon, the material after it is interpreted
		/// as options to the Distsim class (q.v.)
		/// </param>
		/// <param name="left">Which position to start from (normally a non-positive number)</param>
		/// <param name="right">Which position to end with (normally a non-negative number)</param>
		internal ExtractorDistsimConjunction(string distSimPath, int left, int right)
			: base()
		{
			lexicon = Distsim.InitLexicon(distSimPath);
			this.left = left;
			this.right = right;
			name = "ExtractorDistsimConjunction(" + left + ',' + right + ')';
		}

		public override string ToString()
		{
			return name;
		}

		public override bool IsLocal()
		{
			return false;
		}

		public override bool IsDynamic()
		{
			return false;
		}
	}
}
