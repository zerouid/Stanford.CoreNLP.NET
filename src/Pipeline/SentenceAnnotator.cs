using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Java.Lang;
using Java.Util.Concurrent;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// A parent class for annotators which might want to analyze one
	/// sentence at a time, possibly in a multithreaded manner.
	/// </summary>
	/// <remarks>
	/// A parent class for annotators which might want to analyze one
	/// sentence at a time, possibly in a multithreaded manner.
	/// TODO: also factor out the POS
	/// </remarks>
	/// <author>John Bauer</author>
	public abstract class SentenceAnnotator : IAnnotator
	{
		protected internal class AnnotatorProcessor : IThreadsafeProcessor<ICoreMap, ICoreMap>
		{
			internal readonly Annotation annotation;

			internal AnnotatorProcessor(SentenceAnnotator _enclosing, Annotation annotation)
			{
				this._enclosing = _enclosing;
				this.annotation = annotation;
			}

			public virtual ICoreMap Process(ICoreMap sentence)
			{
				this._enclosing.DoOneSentence(this.annotation, sentence);
				return sentence;
			}

			public virtual IThreadsafeProcessor<ICoreMap, ICoreMap> NewInstance()
			{
				return this;
			}

			private readonly SentenceAnnotator _enclosing;
		}

		private InterruptibleMulticoreWrapper<ICoreMap, ICoreMap> BuildWrapper(Annotation annotation)
		{
			InterruptibleMulticoreWrapper<ICoreMap, ICoreMap> wrapper = new InterruptibleMulticoreWrapper<ICoreMap, ICoreMap>(NThreads(), new SentenceAnnotator.AnnotatorProcessor(this, annotation), true, MaxTime());
			return wrapper;
		}

		public virtual void Annotate(Annotation annotation)
		{
			if (annotation.ContainsKey(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				if (NThreads() != 1 || MaxTime() > 0)
				{
					InterruptibleMulticoreWrapper<ICoreMap, ICoreMap> wrapper = BuildWrapper(annotation);
					foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
					{
						bool success = false;
						// We iterate twice for each sentence so that if we fail for
						// a sentence once, we start a new queue and try again.
						// If the sentence fails a second time we give up.
						for (int attempt = 0; attempt < 2; ++attempt)
						{
							try
							{
								wrapper.Put(sentence);
								success = true;
								break;
							}
							catch (RejectedExecutionException)
							{
								// If we time out, for now, we just throw away all jobs which were running at the time.
								// Note that in order for this to be useful, the underlying job needs to handle Thread.interrupted()
								IList<ICoreMap> failedSentences = wrapper.JoinWithTimeout();
								if (failedSentences != null)
								{
									foreach (ICoreMap failed in failedSentences)
									{
										DoOneFailedSentence(annotation, failed);
									}
								}
								// We don't wait for termination here, and perhaps this
								// is a mistake.  If the processor used does not respect
								// interruption, we could easily create many threads
								// which are all doing useless work.  However, there is
								// no clean way to interrupt the thread and then
								// guarantee it finishes without running the risk of
								// waiting forever for the thread to finish, which is
								// exactly what we don't want with the timeout.
								wrapper = BuildWrapper(annotation);
							}
						}
						if (!success)
						{
							DoOneFailedSentence(annotation, sentence);
						}
						while (wrapper.Peek())
						{
							wrapper.Poll();
						}
					}
					IList<ICoreMap> failedSentences_1 = wrapper.JoinWithTimeout();
					while (wrapper.Peek())
					{
						wrapper.Poll();
					}
					if (failedSentences_1 != null)
					{
						foreach (ICoreMap failed in failedSentences_1)
						{
							DoOneFailedSentence(annotation, failed);
						}
					}
				}
				else
				{
					foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
					{
						if (Thread.Interrupted())
						{
							throw new RuntimeInterruptedException();
						}
						DoOneSentence(annotation, sentence);
					}
				}
			}
			else
			{
				throw new Exception("unable to find sentences in: " + annotation);
			}
		}

		protected internal abstract int NThreads();

		/// <summary>The maximum time to run this annotator for, in milliseconds.</summary>
		protected internal abstract long MaxTime();

		/// <summary>annotation is included in case there is global information we care about</summary>
		protected internal abstract void DoOneSentence(Annotation annotation, ICoreMap sentence);

		/// <summary>
		/// Fills in empty annotations for trees, tags, etc if the annotator
		/// failed or timed out.
		/// </summary>
		/// <remarks>
		/// Fills in empty annotations for trees, tags, etc if the annotator
		/// failed or timed out.  Not supposed to do major processing.
		/// </remarks>
		/// <param name="annotation">The whole Annotation object, in case it is needed for context.</param>
		/// <param name="sentence">The particular sentence to process</param>
		protected internal abstract void DoOneFailedSentence(Annotation annotation, ICoreMap sentence);

		public abstract ICollection<Type> RequirementsSatisfied();

		public abstract ICollection<Type> Requires();
	}
}
