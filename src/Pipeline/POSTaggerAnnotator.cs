using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Tagger.Maxent;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Wrapper for the maxent part of speech tagger.</summary>
	/// <author>Anna Rafferty</author>
	public class POSTaggerAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.POSTaggerAnnotator));

		private readonly MaxentTagger pos;

		private readonly int maxSentenceLength;

		private readonly int nThreads;

		private readonly bool reuseTags;

		/// <summary>
		/// Create a tagger annotator using the default English tagger from the models jar
		/// (and non-verbose initialization).
		/// </summary>
		public POSTaggerAnnotator()
			: this(false)
		{
		}

		public POSTaggerAnnotator(bool verbose)
			: this(Runtime.GetProperty("pos.model", MaxentTagger.DefaultJarPath), verbose)
		{
		}

		public POSTaggerAnnotator(string posLoc, bool verbose)
			: this(posLoc, verbose, int.MaxValue, 1)
		{
		}

		/// <summary>Create a POS tagger annotator.</summary>
		/// <param name="posLoc">Location of POS tagger model (may be file path, classpath resource, or URL</param>
		/// <param name="verbose">Whether to show verbose information on model loading</param>
		/// <param name="maxSentenceLength">Sentences longer than this length will be skipped in processing</param>
		/// <param name="numThreads">The number of threads for the POS tagger annotator to use</param>
		public POSTaggerAnnotator(string posLoc, bool verbose, int maxSentenceLength, int numThreads)
			: this(LoadModel(posLoc, verbose), maxSentenceLength, numThreads)
		{
		}

		public POSTaggerAnnotator(MaxentTagger model)
			: this(model, int.MaxValue, 1)
		{
		}

		public POSTaggerAnnotator(MaxentTagger model, int maxSentenceLength, int numThreads)
		{
			this.pos = model;
			this.maxSentenceLength = maxSentenceLength;
			this.nThreads = numThreads;
			this.reuseTags = false;
		}

		public POSTaggerAnnotator(string annotatorName, Properties props)
		{
			string posLoc = props.GetProperty(annotatorName + ".model");
			if (posLoc == null)
			{
				posLoc = DefaultPaths.DefaultPosModel;
			}
			bool verbose = PropertiesUtils.GetBool(props, annotatorName + ".verbose", false);
			this.pos = LoadModel(posLoc, verbose);
			this.maxSentenceLength = PropertiesUtils.GetInt(props, annotatorName + ".maxlen", int.MaxValue);
			this.nThreads = PropertiesUtils.GetInt(props, annotatorName + ".nthreads", PropertiesUtils.GetInt(props, "nthreads", 1));
			this.reuseTags = PropertiesUtils.GetBool(props, annotatorName + ".reuseTags", false);
		}

		private static MaxentTagger LoadModel(string loc, bool verbose)
		{
			Timing timer = null;
			if (verbose)
			{
				timer = new Timing();
				timer.Doing("Loading POS Model [" + loc + ']');
			}
			MaxentTagger tagger = new MaxentTagger(loc);
			if (verbose)
			{
				timer.Done();
			}
			return tagger;
		}

		public virtual void Annotate(Annotation annotation)
		{
			// turn the annotation into a sentence
			if (annotation.ContainsKey(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				if (nThreads == 1)
				{
					foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
					{
						DoOneSentence(sentence);
					}
				}
				else
				{
					MulticoreWrapper<ICoreMap, ICoreMap> wrapper = new MulticoreWrapper<ICoreMap, ICoreMap>(nThreads, new POSTaggerAnnotator.POSTaggerProcessor(this));
					foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
					{
						wrapper.Put(sentence);
						while (wrapper.Peek())
						{
							wrapper.Poll();
						}
					}
					wrapper.Join();
					while (wrapper.Peek())
					{
						wrapper.Poll();
					}
				}
			}
			else
			{
				throw new Exception("unable to find words/tokens in: " + annotation);
			}
		}

		private class POSTaggerProcessor : IThreadsafeProcessor<ICoreMap, ICoreMap>
		{
			public virtual ICoreMap Process(ICoreMap sentence)
			{
				return this._enclosing.DoOneSentence(sentence);
			}

			public virtual IThreadsafeProcessor<ICoreMap, ICoreMap> NewInstance()
			{
				return this;
			}

			internal POSTaggerProcessor(POSTaggerAnnotator _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly POSTaggerAnnotator _enclosing;
		}

		private ICoreMap DoOneSentence(ICoreMap sentence)
		{
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<TaggedWord> tagged = null;
			if (tokens.Count <= maxSentenceLength)
			{
				try
				{
					tagged = pos.TagSentence(tokens, this.reuseTags);
				}
				catch (OutOfMemoryException e)
				{
					log.Error(e);
					// Beware that we can now get an OOM in logging, too.
					log.Warn("Tagging of sentence ran out of memory. " + "Will ignore and continue: " + SentenceUtils.ListToString(tokens));
				}
			}
			if (tagged != null)
			{
				for (int i = 0; i < sz; i++)
				{
					tokens[i].Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), tagged[i].Tag());
				}
			}
			else
			{
				foreach (CoreLabel token in tokens)
				{
					token.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "X");
				}
			}
			return sentence;
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation
				), typeof(CoreAnnotations.SentencesAnnotation))));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.Singleton(typeof(CoreAnnotations.PartOfSpeechAnnotation));
		}
	}
}
