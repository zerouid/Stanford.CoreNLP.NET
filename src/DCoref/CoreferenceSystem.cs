using System.Collections.Generic;

namespace Edu.Stanford.Nlp.Dcoref
{
	/// <summary>abstract class for coreference resolution system</summary>
	/// <author>heeyoung</author>
	public abstract class CoreferenceSystem
	{
		// todo [cdm 2017]: JointCorefSystem is the only coref system that has every implemented this. Just disband it. Nothing uses it.
		/// <exception cref="System.Exception"/>
		public abstract IDictionary<int, CorefChain> Coref(Document document);
	}
}
