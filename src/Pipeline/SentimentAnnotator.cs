using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Neural.Rnn;
using Edu.Stanford.Nlp.Sentiment;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// This annotator attaches a binarized tree with sentiment annotations
	/// to each sentence.
	/// </summary>
	/// <remarks>
	/// This annotator attaches a binarized tree with sentiment annotations
	/// to each sentence.  It requires there to already be binarized trees
	/// attached to the sentence, which is best done in the
	/// ParserAnnotator.
	/// The tree will be attached to each sentence in the
	/// SentencesAnnotation via the SentimentCoreAnnotations.SentimentAnnotatedTree
	/// annotation.  The class name for the top level class is also set
	/// using the SentimentCoreAnnotations.SentimentClass annotation.
	/// The reason the decision was made to do the binarization in the
	/// ParserAnnotator is because it may require specific options set in
	/// the parser.  An alternative would be to do the binarization here,
	/// which would require at a minimum the HeadFinder used in the parser.
	/// </remarks>
	/// <author>John Bauer</author>
	public class SentimentAnnotator : IAnnotator
	{
		private const string DefaultModel = "edu/stanford/nlp/models/sentiment/sentiment.ser.gz";

		private readonly string modelPath;

		private readonly SentimentModel model;

		private readonly CollapseUnaryTransformer transformer = new CollapseUnaryTransformer();

		public SentimentAnnotator(string name, Properties props)
		{
			this.modelPath = props.GetProperty(name + ".model", DefaultModel);
			if (modelPath == null)
			{
				throw new ArgumentException("No model specified for Sentiment annotator");
			}
			this.model = SentimentModel.LoadSerialized(modelPath);
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.EmptySet();
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(TreeCoreAnnotations.TreeAnnotation), typeof(TreeCoreAnnotations.BinarizedTreeAnnotation), typeof(CoreAnnotations.CategoryAnnotation
				))));
		}

		public virtual void Annotate(Annotation annotation)
		{
			if (annotation.ContainsKey(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				// TODO: parallelize
				IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
				foreach (ICoreMap sentence in sentences)
				{
					Tree binarized = sentence.Get(typeof(TreeCoreAnnotations.BinarizedTreeAnnotation));
					if (binarized == null)
					{
						throw new AssertionError("Binarized sentences not built by parser");
					}
					Tree collapsedUnary = transformer.TransformTree(binarized);
					SentimentCostAndGradient scorer = new SentimentCostAndGradient(model, null);
					scorer.ForwardPropagateTree(collapsedUnary);
					sentence.Set(typeof(SentimentCoreAnnotations.SentimentAnnotatedTree), collapsedUnary);
					int sentiment = RNNCoreAnnotations.GetPredictedClass(collapsedUnary);
					sentence.Set(typeof(SentimentCoreAnnotations.SentimentClass), SentimentUtils.SentimentString(model, sentiment));
					Tree tree = sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
					if (tree != null)
					{
						collapsedUnary.SetSpans();
						// map the sentiment annotations onto the tree
						IDictionary<IntPair, string> spanSentiment = Generics.NewHashMap();
						foreach (Tree bt in collapsedUnary)
						{
							IntPair p = bt.GetSpan();
							int sen = RNNCoreAnnotations.GetPredictedClass(bt);
							string sentStr = SentimentUtils.SentimentString(model, sen);
							if (!spanSentiment.Contains(p))
							{
								// we'll take the first = highest one discovered
								spanSentiment[p] = sentStr;
							}
						}
						if (((CoreLabel)tree.Label()).ContainsKey(typeof(CoreAnnotations.SpanAnnotation)))
						{
							throw new InvalidOperationException("This code assumes you don't have SpanAnnotation");
						}
						tree.SetSpans();
						foreach (Tree t in tree)
						{
							IntPair p = t.GetSpan();
							string str = spanSentiment[p];
							if (str != null)
							{
								CoreLabel cl = (CoreLabel)t.Label();
								cl.Set(typeof(SentimentCoreAnnotations.SentimentClass), str);
								cl.Remove(typeof(CoreAnnotations.SpanAnnotation));
							}
						}
					}
				}
			}
			else
			{
				throw new Exception("unable to find sentences in: " + annotation);
			}
		}
	}
}
