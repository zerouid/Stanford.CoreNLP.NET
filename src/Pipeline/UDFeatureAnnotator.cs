using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.UD;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Extracts universal dependencies features from a tree</summary>
	/// <author>Sebastian Schuster</author>
	public class UDFeatureAnnotator : SentenceAnnotator
	{
		private UniversalDependenciesFeatureAnnotator featureAnnotator;

		public UDFeatureAnnotator()
		{
			try
			{
				this.featureAnnotator = new UniversalDependenciesFeatureAnnotator();
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
		}

		protected internal override int NThreads()
		{
			return 1;
		}

		protected internal override long MaxTime()
		{
			return 0;
		}

		protected internal override void DoOneSentence(Annotation annotation, ICoreMap sentence)
		{
			SemanticGraph sg = sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			Tree t = sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
			featureAnnotator.AddFeatures(sg, t, false, true);
		}

		protected internal override void DoOneFailedSentence(Annotation annotation, ICoreMap sentence)
		{
		}

		//do nothing
		public override ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.Singleton(typeof(CoreAnnotations.CoNLLUFeats));
		}

		public override ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(Java.Util.Collections.Singleton(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation)));
		}
	}
}
