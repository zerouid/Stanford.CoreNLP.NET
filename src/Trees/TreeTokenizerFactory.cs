using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Wrapper for TreeReaderFactory.</summary>
	/// <remarks>
	/// Wrapper for TreeReaderFactory.  Any IOException in the readTree() method
	/// of the TreeReader will result in a null
	/// tree returned.
	/// </remarks>
	/// <author>Roger Levy (rog@stanford.edu)</author>
	/// <author>javanlp</author>
	[System.Serializable]
	public class TreeTokenizerFactory : ITokenizerFactory<Tree>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.TreeTokenizerFactory));

		/// <summary>Create a TreeTokenizerFactory from a TreeReaderFactory.</summary>
		public TreeTokenizerFactory(ITreeReaderFactory trf)
		{
			this.trf = trf;
		}

		private ITreeReaderFactory trf;

		/// <summary>Gets a tokenizer from a reader.</summary>
		public virtual ITokenizer<Tree> GetTokenizer(Reader r)
		{
			return new _AbstractTokenizer_33(this, r);
		}

		private sealed class _AbstractTokenizer_33 : AbstractTokenizer<Tree>
		{
			public _AbstractTokenizer_33(Reader r)
			{
				this.r = r;
				this.tr = this._enclosing.trf.NewTreeReader(r);
			}

			internal ITreeReader tr;

			protected internal override Tree GetNext()
			{
				try
				{
					return this.tr.ReadTree();
				}
				catch (IOException)
				{
					Edu.Stanford.Nlp.Trees.TreeTokenizerFactory.log.Info("Error in reading tree.");
					return null;
				}
			}

			private readonly Reader r;
		}

		public virtual ITokenizer<Tree> GetTokenizer(Reader r, string extraOptions)
		{
			// Silently ignore extra options
			return GetTokenizer(r);
		}

		/// <summary>Same as getTokenizer().</summary>
		public virtual IEnumerator<Tree> GetIterator(Reader r)
		{
			return null;
		}

		public virtual void SetOptions(string options)
		{
		}
		//Silently ignore
	}
}
