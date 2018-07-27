using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>A demo illustrating how to call the OpenIE system programmatically.</summary>
	/// <remarks>
	/// A demo illustrating how to call the OpenIE system programmatically.
	/// You can call this code with:
	/// <pre>
	/// java -mx1g -cp stanford-openie.jar:stanford-openie-models.jar edu.stanford.nlp.naturalli.OpenIEDemo
	/// </pre>
	/// </remarks>
	public class OpenIEDemo
	{
		private OpenIEDemo()
		{
		}

		// static main
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			// Create the Stanford CoreNLP pipeline
			Properties props = PropertiesUtils.AsProperties("annotators", "tokenize,ssplit,pos,lemma,depparse,natlog,openie");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			// Annotate an example document.
			string text;
			if (args.Length > 0)
			{
				text = IOUtils.SlurpFile(args[0]);
			}
			else
			{
				text = "Obama was born in Hawaii. He is our president.";
			}
			Annotation doc = new Annotation(text);
			pipeline.Annotate(doc);
			// Loop over sentences in the document
			int sentNo = 0;
			foreach (ICoreMap sentence in doc.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				System.Console.Out.WriteLine("Sentence #" + ++sentNo + ": " + sentence.Get(typeof(CoreAnnotations.TextAnnotation)));
				// Print SemanticGraph
				System.Console.Out.WriteLine(sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation)).ToString(SemanticGraph.OutputFormat.List));
				// Get the OpenIE triples for the sentence
				ICollection<RelationTriple> triples = sentence.Get(typeof(NaturalLogicAnnotations.RelationTriplesAnnotation));
				// Print the triples
				foreach (RelationTriple triple in triples)
				{
					System.Console.Out.WriteLine(triple.confidence + "\t" + triple.SubjectLemmaGloss() + "\t" + triple.RelationLemmaGloss() + "\t" + triple.ObjectLemmaGloss());
				}
				// Alternately, to only run e.g., the clause splitter:
				IList<SentenceFragment> clauses = new OpenIE(props).ClausesInSentence(sentence);
				foreach (SentenceFragment clause in clauses)
				{
					System.Console.Out.WriteLine(clause.parseTree.ToString(SemanticGraph.OutputFormat.List));
				}
				System.Console.Out.WriteLine();
			}
		}
	}
}
