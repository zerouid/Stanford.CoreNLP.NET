using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees.UD;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// <p>Write a subset of our CoreNLP output in CoNLL-U format.</p>
	/// <p>The fields currently output are:</p>
	/// <table>
	/// <tr>
	/// <td>Field Number</td>
	/// <td>Field Name</td>
	/// <td>Description</td>
	/// </tr>
	/// <tr>
	/// <td>1</td>
	/// <td>ID</td>
	/// <td>Word index, integer starting at 1 for each new sentence; may be a range for tokens with multiple words.</td>
	/// </tr>
	/// <tr>
	/// <td>2</td>
	/// <td>FORM</td>
	/// <td>Word form or punctuation symbol.</td>
	/// </tr>
	/// <tr>
	/// <td>3</td>
	/// <td>LEMMA</td>
	/// <td>Lemma or stem of word form, or an underscore if not available.</td>
	/// </tr>
	/// <tr>
	/// <td>4</td>
	/// <td>CPOSTAG</td>
	/// <td>Universal part-of-speech tag, or underscore if not available.</td>
	/// </tr>
	/// <tr>
	/// <td>5</td>
	/// <td>POSTAG</td>
	/// <td>Language-specific part-of-speech tag, or underscore if not available.</td>
	/// </tr>
	/// <tr>
	/// <td>6</td>
	/// <td>FEATS</td>
	/// <td>List of morphological features from the universal feature inventory or from a defined language-specific extension; underscore if not available.</td>
	/// </tr>
	/// <tr>
	/// <td>7</td>
	/// <td>HEAD</td>
	/// <td>Head of the current token, which is either a value of ID or zero ('0').
	/// </summary>
	/// <remarks>
	/// <p>Write a subset of our CoreNLP output in CoNLL-U format.</p>
	/// <p>The fields currently output are:</p>
	/// <table>
	/// <tr>
	/// <td>Field Number</td>
	/// <td>Field Name</td>
	/// <td>Description</td>
	/// </tr>
	/// <tr>
	/// <td>1</td>
	/// <td>ID</td>
	/// <td>Word index, integer starting at 1 for each new sentence; may be a range for tokens with multiple words.</td>
	/// </tr>
	/// <tr>
	/// <td>2</td>
	/// <td>FORM</td>
	/// <td>Word form or punctuation symbol.</td>
	/// </tr>
	/// <tr>
	/// <td>3</td>
	/// <td>LEMMA</td>
	/// <td>Lemma or stem of word form, or an underscore if not available.</td>
	/// </tr>
	/// <tr>
	/// <td>4</td>
	/// <td>CPOSTAG</td>
	/// <td>Universal part-of-speech tag, or underscore if not available.</td>
	/// </tr>
	/// <tr>
	/// <td>5</td>
	/// <td>POSTAG</td>
	/// <td>Language-specific part-of-speech tag, or underscore if not available.</td>
	/// </tr>
	/// <tr>
	/// <td>6</td>
	/// <td>FEATS</td>
	/// <td>List of morphological features from the universal feature inventory or from a defined language-specific extension; underscore if not available.</td>
	/// </tr>
	/// <tr>
	/// <td>7</td>
	/// <td>HEAD</td>
	/// <td>Head of the current token, which is either a value of ID or zero ('0').
	/// This is underscore if not available.</td>
	/// </tr>
	/// <tr>
	/// <td>8</td>
	/// <td>DEPREL</td>
	/// <td>Dependency relation to the HEAD, or underscore if not available.</td>
	/// </tr>
	/// <tr>
	/// <td>9</td>
	/// <td>DEPS</td>
	/// <td>List of secondary dependencies</td>
	/// </tr>
	/// <tr>
	/// <td>10</td>
	/// <td>MISC</td>
	/// <td>Any other annotation</td>
	/// </tr>
	/// </table>
	/// </remarks>
	/// <author>Sebastian Schuster</author>
	/// <author>Gabor Angeli</author>
	public class CoNLLUOutputter : AnnotationOutputter
	{
		private static readonly CoNLLUDocumentWriter conllUWriter = new CoNLLUDocumentWriter();

		public CoNLLUOutputter()
		{
		}

		/// <exception cref="System.IO.IOException"/>
		public override void Print(Annotation doc, OutputStream target, AnnotationOutputter.Options options)
		{
			PrintWriter writer = new PrintWriter(IOUtils.EncodedOutputStreamWriter(target, options.encoding));
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			foreach (ICoreMap sentence in sentences)
			{
				SemanticGraph sg = sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
				if (sg != null)
				{
					writer.Print(conllUWriter.PrintSemanticGraph(sg));
				}
				else
				{
					writer.Print(conllUWriter.PrintPOSAnnotations(sentence));
				}
			}
			writer.Flush();
		}

		/// <exception cref="System.IO.IOException"/>
		public static void ConllUPrint(Annotation annotation, OutputStream os)
		{
			new Edu.Stanford.Nlp.Pipeline.CoNLLUOutputter().Print(annotation, os);
		}

		/// <exception cref="System.IO.IOException"/>
		public static void ConllUPrint(Annotation annotation, OutputStream os, StanfordCoreNLP pipeline)
		{
			new Edu.Stanford.Nlp.Pipeline.CoNLLUOutputter().Print(annotation, os, pipeline);
		}

		/// <exception cref="System.IO.IOException"/>
		public static void ConllUPrint(Annotation annotation, OutputStream os, AnnotationOutputter.Options options)
		{
			new Edu.Stanford.Nlp.Pipeline.CoNLLUOutputter().Print(annotation, os, options);
		}
	}
}
