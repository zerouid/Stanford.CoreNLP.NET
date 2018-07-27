using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// An interface for outputting CoreNLP Annotations to different output
	/// formats.
	/// </summary>
	/// <remarks>
	/// An interface for outputting CoreNLP Annotations to different output
	/// formats.
	/// These are intended to be for more or less human consumption (or for transferring
	/// to other applications) -- that is, there output is not intended to be read back into
	/// CoreNLP losslessly.
	/// For lossless (or near lossless) serialization,
	/// see
	/// <see cref="AnnotationSerializer"/>
	/// ; e.g.,
	/// <see cref="ProtobufAnnotationSerializer"/>
	/// .
	/// </remarks>
	/// <seealso cref="XMLOutputter"/>
	/// <seealso cref="JSONOutputter"/>
	/// <author>Gabor Angeli</author>
	public abstract class AnnotationOutputter
	{
		internal static readonly TreePrint DefaultConstituentTreePrinter = new TreePrint("penn");

		private static readonly AnnotationOutputter.Options DefaultOptions = new AnnotationOutputter.Options();

		public class Options
		{
			/// <summary>Should the document text be included as part of the XML output</summary>
			public bool includeText = false;

			/// <summary>Should a small window of context be provided with each coreference mention</summary>
			public int coreferenceContextSize = 0;

			/// <summary>The output encoding to use</summary>
			public string encoding = "UTF-8";

			/// <summary>How to print a constituent tree</summary>
			public TreePrint constituentTreePrinter = DefaultConstituentTreePrinter;

			/// <summary>If false, will print only non-singleton entities</summary>
			public bool printSingletons = false;

			/// <summary>If false, try to compress whitespace as much as possible.</summary>
			/// <remarks>If false, try to compress whitespace as much as possible. This is particularly useful for sending over the wire.</remarks>
			public bool pretty = true;

			public double relationsBeam = 0.0;

			public double beamPrintingOption = 0.0;
			// IMPORTANT: must come after DEFAULT_CONSTITUENCY_TREE_PRINTER
		}

		/// <exception cref="System.IO.IOException"/>
		public abstract void Print(Annotation doc, OutputStream target, AnnotationOutputter.Options options);

		/// <exception cref="System.IO.IOException"/>
		public virtual void Print(Annotation annotation, OutputStream os)
		{
			Print(annotation, os, DefaultOptions);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void Print(Annotation annotation, OutputStream os, StanfordCoreNLP pipeline)
		{
			Print(annotation, os, GetOptions(pipeline));
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual string Print(Annotation ann, AnnotationOutputter.Options options)
		{
			ByteArrayOutputStream os = new ByteArrayOutputStream();
			Print(ann, os, options);
			os.Close();
			return Sharpen.Runtime.GetStringForBytes(os.ToByteArray());
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual string Print(Annotation ann)
		{
			return Print(ann, DefaultOptions);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual string Print(Annotation ann, StanfordCoreNLP pipeline)
		{
			return Print(ann, GetOptions(pipeline));
		}

		/// <summary>Populates options from StanfordCoreNLP pipeline.</summary>
		public static AnnotationOutputter.Options GetOptions(StanfordCoreNLP pipeline)
		{
			AnnotationOutputter.Options options = new AnnotationOutputter.Options();
			options.relationsBeam = pipeline.GetBeamPrintingOption();
			options.constituentTreePrinter = pipeline.GetConstituentTreePrinter();
			options.encoding = pipeline.GetEncoding();
			options.printSingletons = pipeline.GetPrintSingletons();
			options.beamPrintingOption = pipeline.GetBeamPrintingOption();
			options.pretty = pipeline.GetPrettyPrint();
			options.includeText = pipeline.GetIncludeText();
			return options;
		}
	}
}
