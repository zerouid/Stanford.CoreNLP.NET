using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns.Dep;
using Edu.Stanford.Nlp.Patterns.Surface;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Patterns
{
	/// <summary>Created by sonalg on 11/2/14.</summary>
	[NUnit.Framework.TestFixture]
	public class CreatePatternsTest
	{
		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void Test()
		{
			Properties props = new Properties();
			props.SetProperty("patternType", "DEP");
			ConstantsAndVariables constvars = new ConstantsAndVariables(props, new HashSet<string>(), new Dictionary<string, Type>());
			CreatePatterns<DepPattern> createPatterns = new CreatePatterns<DepPattern>(props, constvars);
			IDictionary<string, DataInstance> sents = new Dictionary<string, DataInstance>();
			ICoreMap m = new ArrayCoreMap();
			string text = "We present a paper that focuses on semantic graphs applied to language.";
			string graphString = "[present/VBP-2 nsubj>We/PRP-1 dobj>[paper/NN-4 det>a/DT-3] ccomp>[applied/VBN-10 mark>that/IN-5 nsubj>[focuses/NN-6 nmod:on>[graphs/NNS-9 amod>semantic/JJ-8]] nmod:to>language/NN-12]]";
			SemanticGraph graph = SemanticGraph.ValueOf(graphString);
			//String phrase = "semantic graphs";
			IList<string> tokens = Arrays.AsList(new string[] { "We", "present", "a", "paper", "that", "focuses", "on", "semantic", "graphs", "applied", "to", "language" });
			m.Set(typeof(CoreAnnotations.TokensAnnotation), tokens.Stream().Map(null).Collect(Collectors.ToList()));
			m.Set(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation), graph);
			sents["sent1"] = DataInstance.GetNewInstance(PatternFactory.PatternType.Dep, m);
			createPatterns.GetAllPatterns(sents, props, ConstantsAndVariables.PatternForEachTokenWay.Memory);
			System.Console.Out.WriteLine("graph is " + graph);
			System.Console.Out.WriteLine(PatternsForEachTokenInMemory.patternsForEachToken);
		}
	}
}
