using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Simple
{
	/// <summary>A sentence running with the Chinese models.</summary>
	/// <author><a href="mailto:angeli@cs.stanford.edu">Gabor Angeli</a></author>
	public class ChineseDocument : Document
	{
		/// <summary>
		/// The default
		/// <see cref="Edu.Stanford.Nlp.Pipeline.ChineseSegmenterAnnotator"/>
		/// implementation
		/// </summary>
		private static readonly Lazy<IAnnotator> chineseSegmenter = Lazy.Of(null);

		private sealed class _Properties_33 : Properties
		{
			public _Properties_33()
			{
				{
					try
					{
						using (InputStream @is = IOUtils.GetInputStreamFromURLOrClasspathOrFileSystem("edu/stanford/nlp/pipeline/StanfordCoreNLP-chinese.properties"))
						{
							this.Load(@is);
						}
					}
					catch (IOException e)
					{
						throw new RuntimeIOException(e);
					}
					this.SetProperty("language", "chinese");
					this.SetProperty("annotators", string.Empty);
					this.SetProperty("parse.binaryTrees", "true");
				}
			}
		}

		/// <summary>
		/// The empty
		/// <see cref="Java.Util.Properties"/>
		/// object, for use with creating default annotators.
		/// </summary>
		internal static readonly Properties EmptyProps = new _Properties_33();

		/// <summary>Create a new document from the passed in text.</summary>
		/// <param name="text">The text of the document.</param>
		public ChineseDocument(string text)
			: base(Edu.Stanford.Nlp.Simple.ChineseDocument.EmptyProps, text)
		{
		}

		/// <summary>Convert a CoreNLP Annotation object to a Document.</summary>
		/// <param name="ann">The CoreNLP Annotation object.</param>
		public ChineseDocument(Annotation ann)
			: base(Edu.Stanford.Nlp.Simple.ChineseDocument.EmptyProps, ann)
		{
		}

		/// <summary>Create a Document object from a read Protocol Buffer.</summary>
		/// <seealso cref="Document.Serialize()"/>
		/// <param name="proto">The protocol buffer representing this document.</param>
		public ChineseDocument(CoreNLPProtos.Document proto)
			: base(Edu.Stanford.Nlp.Simple.ChineseDocument.EmptyProps, proto)
		{
		}

		/// <summary>Create a new chinese document from the passed in text and the given properties.</summary>
		/// <param name="text">The text of the document.</param>
		protected internal ChineseDocument(Properties props, string text)
			: base(props, text)
		{
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IList<Sentence> Sentences(Properties props)
		{
			return this.Sentences(props, chineseSegmenter.Get());
		}

		/// <summary>
		/// &lt;&lt;&lt;&lt;&lt;&lt;&lt; HEAD
		/// No lemma annotator for Chinese -- set the lemma to be the word.
		/// </summary>
		/// <seealso cref="Document.RunLemma(Java.Util.Properties)"/>
		internal override Document RunLemma(Properties props)
		{
			return MockLemma(props);
		}

		/// <summary>No sentiment analysis implemented for Chinese.</summary>
		/// <seealso cref="Document.RunSentiment(Java.Util.Properties)"/>
		internal override Document RunSentiment(Properties props)
		{
			throw new ArgumentException("Sentiment analysis is not implemented for Chinese");
		}

		/// <summary>
		/// The Neural Dependency Parser doesn't support Chinese yet, so back off to running the
		/// constituency parser instead.
		/// </summary>
		internal override Document RunDepparse(Properties props)
		{
			// TODO(danqi; from Gabor): remove this method when we have a trained NNDep model
			return RunParse(props);
		}
	}
}
