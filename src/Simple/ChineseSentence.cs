using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Pipeline;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Simple
{
	/// <summary>
	/// A
	/// <see cref="Sentence"/>
	/// , but in Chinese.
	/// </summary>
	/// <author><a href="mailto:angeli@cs.stanford.edu">Gabor Angeli</a></author>
	public class ChineseSentence : Sentence
	{
		private sealed class _Properties_20 : Properties
		{
			public _Properties_20()
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
					this.SetProperty("ssplit.isOneSentence", "true");
					this.SetProperty("tokenize.class", "PTBTokenizer");
					this.SetProperty("tokenize.language", "zh");
				}
			}
		}

		/// <summary>A properties object for creating a document from a single sentence.</summary>
		/// <remarks>
		/// A properties object for creating a document from a single sentence. Used in the constructor
		/// <see cref="Sentence.Sentence(string)"/>
		/// 
		/// </remarks>
		internal static Properties SingleSentenceDocument = new _Properties_20();

		private sealed class _Properties_34 : Properties
		{
			public _Properties_34()
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
					this.SetProperty("ssplit.isOneSentence", "true");
					this.SetProperty("tokenize.class", "WhitespaceTokenizer");
					this.SetProperty("tokenize.language", "zh");
					this.SetProperty("tokenize.whitespace", "true");
				}
			}
		}

		/// <summary>A properties object for creating a document from a single tokenized sentence.</summary>
		private static Properties SingleSentenceTokenizedDocument = new _Properties_34();

		public ChineseSentence(string text)
			: base(new ChineseDocument(text), SingleSentenceDocument)
		{
		}

		public ChineseSentence(IList<string> tokens)
			: base(null, tokens, SingleSentenceTokenizedDocument)
		{
		}

		public ChineseSentence(CoreNLPProtos.Sentence proto)
			: base(null, proto, SingleSentenceDocument)
		{
		}
		// redundant?
	}
}
