using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Patterns.Dep
{
	/// <summary>Created by sonalg on 11/1/14.</summary>
	[System.Serializable]
	public class DataInstanceDep : DataInstance
	{
		internal SemanticGraph graph;

		internal IList<CoreLabel> tokens;

		public DataInstanceDep(ICoreMap s)
		{
			graph = s.Get(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation));
			//    System.out.println("CollapsedCCProcessedDependenciesAnnotation graph is " + s.get(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation.class));
			//    System.out.println("CollapsedDependenciesAnnotation graph is " + s.get(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation.class));
			//    System.out.println("BasicDependenciesAnnotation graph is " + s.get(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation.class));
			tokens = s.Get(typeof(CoreAnnotations.TokensAnnotation));
		}

		public override IList<CoreLabel> GetTokens()
		{
			return tokens;
		}

		public virtual SemanticGraph GetGraph()
		{
			return graph;
		}

		public override string ToString()
		{
			return StringUtils.Join(tokens, " ");
		}
	}
}
