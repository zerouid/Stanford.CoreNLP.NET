using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Time;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Time.Suservlet
{
	public class SUTimePipeline
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Time.Suservlet.SUTimePipeline));

		internal readonly StanfordCoreNLP pipeline;

		public SUTimePipeline()
			: this(new Properties())
		{
		}

		public SUTimePipeline(Properties props)
		{
			// By default, we want to tokenize the text, split it into
			// sentences, and then put it through the sutime annotator.
			// We also want to pos tag it and put it through the number and
			// qen annotators.
			// Since there will be different options for the sutime annotator,
			// we will actually create a new sutime annotator for each query.
			// This should be inexpensive.
			if (props.GetProperty("annotators") == null)
			{
				props.SetProperty("annotators", "tokenize, ssplit, pos");
			}
			//      "tokenize, ssplit, pos, number, qen");
			/*    if (props.getProperty("customAnnotatorClass.number") == null) {
			props.setProperty("customAnnotatorClass.number",
			"edu.stanford.nlp.pipeline.NumberAnnotator");
			}
			if (props.getProperty("customAnnotatorClass.qen") == null) {
			props.setProperty("customAnnotatorClass.qen",
			"edu.stanford.nlp.pipeline.QuantifiableEntityNormalizingAnnotator");
			}    */
			// this replicates the tokenizer behavior in StanfordCoreNLP
			props.SetProperty("tokenize.options", "invertible,ptb3Escaping=true");
			this.pipeline = new StanfordCoreNLP(props);
		}

		public virtual bool IsDateOkay(string dateString)
		{
			return true;
		}

		// TODO: can we predict which ones it won't like?
		public virtual IAnnotator GetTimeAnnotator(string annotatorType, Properties props)
		{
			switch (annotatorType)
			{
				case "sutime":
				{
					return new TimeAnnotator("sutime", props);
				}

				case "gutime":
				{
					return new GUTimeAnnotator("gutime", props);
				}

				case "heideltime":
				{
					return new HeidelTimeAnnotator("heidelTime", props);
				}

				default:
				{
					return null;
				}
			}
		}

		public virtual Annotation Process(string sentence, string dateString, IAnnotator timeAnnotator)
		{
			log.Info("Processing text \"" + sentence + "\" with dateString = " + dateString);
			Annotation anno = new Annotation(sentence);
			if (dateString != null && !dateString.IsEmpty())
			{
				anno.Set(typeof(CoreAnnotations.DocDateAnnotation), dateString);
			}
			pipeline.Annotate(anno);
			timeAnnotator.Annotate(anno);
			return anno;
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.Time.Suservlet.SUTimePipeline pipeline = new Edu.Stanford.Nlp.Time.Suservlet.SUTimePipeline();
			IAnnotator timeAnnotator = pipeline.GetTimeAnnotator("sutime", new Properties());
			BufferedReader @is = new BufferedReader(new InputStreamReader(Runtime.@in));
			System.Console.Out.Write("> ");
			for (string line; (line = @is.ReadLine()) != null; )
			{
				Annotation ann = pipeline.Process(line, null, timeAnnotator);
				System.Console.Out.WriteLine(ann.Get(typeof(TimeAnnotations.TimexAnnotations)));
				System.Console.Out.Write("> ");
			}
		}
	}
}
