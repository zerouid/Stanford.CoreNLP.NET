using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Docreader;
using Edu.Stanford.Nlp.Pipeline;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Data
{
	/// <summary>An input document read from a input source (CoNLL, ACE, MUC, or raw text).</summary>
	/// <remarks>
	/// An input document read from a input source (CoNLL, ACE, MUC, or raw text).
	/// Stores Annotation, gold info (optional) and additional document information (optional).
	/// Used in coreference systems
	/// </remarks>
	/// <author>heeyoung</author>
	public class InputDoc
	{
		public Annotation annotation;

		/// <summary>Additional document information possibly useful for coref.</summary>
		/// <remarks>
		/// Additional document information possibly useful for coref.
		/// (e.g., is this dialog? the source of article, etc)
		/// We can use this as features for coref system.
		/// This is optional.
		/// </remarks>
		public IDictionary<string, string> docInfo;

		/// <summary>Gold mentions with coreference information for evaluation.</summary>
		/// <remarks>
		/// Gold mentions with coreference information for evaluation.
		/// This is optional.
		/// </remarks>
		public IList<IList<Mention>> goldMentions;

		/// <summary>optional for CoNLL document</summary>
		public CoNLLDocumentReader.CoNLLDocument conllDoc;

		public InputDoc(Annotation anno)
			: this(anno, null, null, null)
		{
		}

		public InputDoc(Annotation anno, IDictionary<string, string> docInfo)
			: this(anno, docInfo, null, null)
		{
		}

		public InputDoc(Annotation anno, IDictionary<string, string> docInfo, IList<IList<Mention>> goldMentions)
			: this(anno, docInfo, goldMentions, null)
		{
		}

		public InputDoc(Annotation anno, IDictionary<string, string> docInfo, IList<IList<Mention>> goldMentions, CoNLLDocumentReader.CoNLLDocument conllDoc)
		{
			this.annotation = anno;
			this.docInfo = docInfo;
			this.goldMentions = goldMentions;
			this.conllDoc = conllDoc;
		}
	}
}
