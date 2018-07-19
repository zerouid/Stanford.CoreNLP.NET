using System;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Coref.Hybrid;
using Edu.Stanford.Nlp.Coref.Neural;
using Edu.Stanford.Nlp.Coref.Statistical;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref
{
	/// <summary>
	/// A CorefAlgorithms makes coreference decisions on the provided
	/// <see cref="Edu.Stanford.Nlp.Coref.Data.Document"/>
	/// after
	/// mention detection has been performed.
	/// </summary>
	/// <author>Kevin Clark</author>
	public interface ICorefAlgorithm
	{
		void RunCoref(Document document);

		ICorefAlgorithm FromProps(Properties props, Dictionaries dictionaries);
	}
}
