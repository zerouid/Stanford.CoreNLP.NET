using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// This class is designed to apply multiple Annotators
	/// to an Annotation.
	/// </summary>
	/// <remarks>
	/// This class is designed to apply multiple Annotators
	/// to an Annotation.  The idea is that you first
	/// build up the pipeline by adding Annotators, and then
	/// you take the objects you wish to annotate and pass
	/// them in and get back in return a fully annotated object.
	/// Please see the package level javadoc for sample usage
	/// and a more complete description.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	public class AnnotationPipeline : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.AnnotationPipeline));

		protected internal const bool Time = true;

		private readonly IList<IAnnotator> annotators;

		private IList<MutableLong> accumulatedTime;

		public AnnotationPipeline(IList<IAnnotator> annotators)
		{
			this.annotators = annotators;
			int num = annotators.Count;
			accumulatedTime = new List<MutableLong>(num);
			for (int i = 0; i < num; i++)
			{
				accumulatedTime.Add(new MutableLong());
			}
		}

		public AnnotationPipeline()
			: this(new List<IAnnotator>())
		{
		}

		// It can't be a singletonList() since it isn't copied but is mutated.
		public virtual void AddAnnotator(IAnnotator annotator)
		{
			annotators.Add(annotator);
			accumulatedTime.Add(new MutableLong());
		}

		/// <summary>Run the pipeline on an input annotation.</summary>
		/// <remarks>
		/// Run the pipeline on an input annotation.
		/// The annotation is modified in place.
		/// </remarks>
		/// <param name="annotation">The input annotation, usually a raw document</param>
		public virtual void Annotate(Annotation annotation)
		{
			IEnumerator<MutableLong> it = accumulatedTime.GetEnumerator();
			Timing t = new Timing();
			foreach (IAnnotator annotator in annotators)
			{
				if (Thread.Interrupted())
				{
					// Allow interrupting
					throw new RuntimeInterruptedException();
				}
				t.Start();
				annotator.Annotate(annotation);
				long elapsed = t.Stop();
				MutableLong m = it.Current;
				m.IncValue(elapsed);
			}
		}

		/// <summary>
		/// Annotate a collection of input annotations IN PARALLEL, making use of
		/// all available cores.
		/// </summary>
		/// <param name="annotations">The input annotations to process</param>
		public virtual void Annotate(IEnumerable<Annotation> annotations)
		{
			Annotate(annotations, Runtime.GetRuntime().AvailableProcessors());
		}

		/// <summary>
		/// Annotate a collection of input annotations IN PARALLEL, making use of
		/// all available cores.
		/// </summary>
		/// <param name="annotations">The input annotations to process</param>
		/// <param name="callback">
		/// A function to be called when an annotation finishes.
		/// The return value of the callback is ignored.
		/// </param>
		public virtual void Annotate(IEnumerable<Annotation> annotations, IConsumer<Annotation> callback)
		{
			Annotate(annotations, Runtime.GetRuntime().AvailableProcessors(), callback);
		}

		/// <summary>
		/// Annotate a collection of input annotations IN PARALLEL, making use of
		/// threads given in numThreads.
		/// </summary>
		/// <param name="annotations">The input annotations to process</param>
		/// <param name="numThreads">The number of threads to run on</param>
		public virtual void Annotate(IEnumerable<Annotation> annotations, int numThreads)
		{
			Annotate(annotations, numThreads, null);
		}

		/// <summary>
		/// Annotate a collection of input annotations IN PARALLEL, making use of
		/// threads given in numThreads
		/// </summary>
		/// <param name="annotations">The input annotations to process</param>
		/// <param name="numThreads">The number of threads to run on</param>
		/// <param name="callback">
		/// A function to be called when an annotation finishes.
		/// The return value of the callback is ignored.
		/// </param>
		public virtual void Annotate(IEnumerable<Annotation> annotations, int numThreads, IConsumer<Annotation> callback)
		{
			// case: single thread (no point in spawning threads)
			if (numThreads == 1)
			{
				foreach (Annotation ann in annotations)
				{
					Annotate(ann);
					callback.Accept(ann);
				}
			}
			// Java's equivalent to ".map{ lambda(annotation) => annotate(annotation) }
			IEnumerable<IRunnable> threads = null;
			//(logging)
			//(annotate)
			//(callback)
			//(logging again)
			// Thread
			Redwood.Util.ThreadAndRun(this.GetType().GetSimpleName(), threads, numThreads);
		}

		/// <summary>Return the total pipeline annotation time in milliseconds.</summary>
		/// <returns>The total pipeline annotation time in milliseconds</returns>
		protected internal virtual long GetTotalTime()
		{
			long total = 0;
			foreach (MutableLong m in accumulatedTime)
			{
				total += m;
			}
			return total;
		}

		/// <summary>
		/// Return a String that gives detailed human-readable information about
		/// how much time was spent by each annotator and by the entire annotation
		/// pipeline.
		/// </summary>
		/// <remarks>
		/// Return a String that gives detailed human-readable information about
		/// how much time was spent by each annotator and by the entire annotation
		/// pipeline.  This String includes newline characters but does not end
		/// with one, and so it is suitable to be printed out with a
		/// <c>println()</c>
		/// .
		/// </remarks>
		/// <returns>Human readable information on time spent in processing.</returns>
		public virtual string TimingInformation()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("Annotation pipeline timing information:");
			sb.Append(IOUtils.eolChar);
			IEnumerator<MutableLong> it = accumulatedTime.GetEnumerator();
			long total = 0;
			foreach (IAnnotator annotator in annotators)
			{
				MutableLong m = it.Current;
				sb.Append(StringUtils.GetShortClassName(annotator)).Append(": ");
				sb.Append(Timing.ToSecondsString(m)).Append(" sec.");
				sb.Append(IOUtils.eolChar);
				total += m;
			}
			sb.Append("TOTAL: ").Append(Timing.ToSecondsString(total)).Append(" sec.");
			return sb.ToString();
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			ICollection<Type> satisfied = Generics.NewHashSet();
			foreach (IAnnotator annotator in annotators)
			{
				Sharpen.Collections.AddAll(satisfied, annotator.RequirementsSatisfied());
			}
			return satisfied;
		}

		public virtual ICollection<Type> Requires()
		{
			if (annotators.IsEmpty())
			{
				return Java.Util.Collections.EmptySet();
			}
			return annotators[0].Requires();
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static void Main(string[] args)
		{
			Timing tim = new Timing();
			Edu.Stanford.Nlp.Pipeline.AnnotationPipeline ap = new Edu.Stanford.Nlp.Pipeline.AnnotationPipeline();
			bool verbose = false;
			ap.AddAnnotator(new TokenizerAnnotator(verbose, "en"));
			ap.AddAnnotator(new WordsToSentencesAnnotator(verbose));
			// ap.addAnnotator(new NERCombinerAnnotator(verbose));
			// ap.addAnnotator(new OldNERAnnotator(verbose));
			// ap.addAnnotator(new NERMergingAnnotator(verbose));
			ap.AddAnnotator(new ParserAnnotator(verbose, -1));
			/*
			ap.addAnnotator(new UpdateSentenceFromParseAnnotator(verbose));
			ap.addAnnotator(new NumberAnnotator(verbose));
			ap.addAnnotator(new QuantifiableEntityNormalizingAnnotator(verbose));
			ap.addAnnotator(new StemmerAnnotator(verbose));
			ap.addAnnotator(new MorphaAnnotator(verbose));
			**/
			//    ap.addAnnotator(new SRLAnnotator());
			string text = ("USAir said in the filings that Mr. Icahn first contacted Mr. Colodny last September to discuss the benefits of combining TWA and USAir -- either by TWA's acquisition of USAir, or USAir's acquisition of TWA.");
			Annotation a = new Annotation(text);
			ap.Annotate(a);
			System.Console.Out.WriteLine(a.Get(typeof(CoreAnnotations.TokensAnnotation)));
			foreach (ICoreMap sentence in a.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				System.Console.Out.WriteLine(sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation)));
			}
			System.Console.Out.WriteLine(ap.TimingInformation());
			log.Info("Total time for AnnotationPipeline: " + tim.ToSecondsString() + " sec.");
		}
	}
}
