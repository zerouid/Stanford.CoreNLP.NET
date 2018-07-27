using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;


namespace Edu.Stanford.Nlp.IE.Pascal
{
	/// <summary>Template information and counters corresponding to sampling on one document.</summary>
	/// <remarks>
	/// Template information and counters corresponding to sampling on one document.
	/// As an alternative to reading a document labelling into a full
	/// <see cref="PascalTemplate"/>
	/// we can read it into partial templates which contain only strictly related information,
	/// (See
	/// <see cref="DateTemplate"/>
	/// and
	/// <see cref="InfoTemplate"/>
	/// ).
	/// </remarks>
	/// <author>Chris Cox</author>
	public class CliqueTemplates
	{
		public Hashtable stemmedAcronymIndex = new Hashtable();

		public Hashtable inverseAcronymMap = new Hashtable();

		public List<string> urls = null;

		public ClassicCounter dateCliqueCounter = new ClassicCounter();

		public ClassicCounter locationCliqueCounter = new ClassicCounter();

		public ClassicCounter workshopInfoCliqueCounter = new ClassicCounter();
	}
}
