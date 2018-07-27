using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Parser.Nndep
{
	/// <author>Christopher Manning</author>
	internal class Example
	{
		private readonly IList<int> feature;

		private readonly IList<int> label;

		public Example(IList<int> feature, IList<int> label)
		{
			this.feature = feature;
			this.label = label;
		}

		public virtual IList<int> GetFeature()
		{
			return feature;
		}

		public virtual IList<int> GetLabel()
		{
			return label;
		}
	}
}
