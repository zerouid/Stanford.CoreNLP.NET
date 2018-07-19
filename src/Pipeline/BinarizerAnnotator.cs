using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// This annotator takes unbinarized trees (from the parser annotator
	/// or elsewhere) and binarizes them in the attachment.
	/// </summary>
	/// <remarks>
	/// This annotator takes unbinarized trees (from the parser annotator
	/// or elsewhere) and binarizes them in the attachment.
	/// <p>
	/// Note that this functionality is also built in to the
	/// ParserAnnotator.  However, this can be used in situations where the
	/// trees come from somewhere other than the parser.  Conversely, the
	/// ParserAnnotator may have more options for the binarizer which are
	/// not implemented here.
	/// </remarks>
	/// <author>John Bauer</author>
	public class BinarizerAnnotator : IAnnotator
	{
		private const string DefaultTlppClass = "edu.stanford.nlp.parser.lexparser.EnglishTreebankParserParams";

		private readonly TreeBinarizer binarizer;

		private readonly string tlppClass;

		public BinarizerAnnotator(string annotatorName, Properties props)
		{
			this.tlppClass = props.GetProperty(annotatorName + ".tlppClass", DefaultTlppClass);
			ITreebankLangParserParams tlpp = ReflectionLoading.LoadByReflection(tlppClass);
			this.binarizer = TreeBinarizer.SimpleTreeBinarizer(tlpp.HeadFinder(), tlpp.TreebankLanguagePack());
		}

		public virtual string Signature(string annotatorName, Properties props)
		{
			// String tlppClass = props.getProperty(annotatorName + ".tlppClass", DEFAULT_TLPP_CLASS);
			return tlppClass;
		}

		public virtual void Annotate(Annotation annotation)
		{
			if (annotation.ContainsKey(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					DoOneSentence(sentence);
				}
			}
			else
			{
				throw new Exception("unable to find sentences in: " + annotation);
			}
		}

		private void DoOneSentence(ICoreMap sentence)
		{
			Tree tree = sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
			Tree binarized;
			if (IsBinarized(tree))
			{
				binarized = tree;
			}
			else
			{
				binarized = binarizer.TransformTree(tree);
			}
			Edu.Stanford.Nlp.Trees.Trees.ConvertToCoreLabels(binarized);
			sentence.Set(typeof(TreeCoreAnnotations.BinarizedTreeAnnotation), binarized);
		}

		/// <summary>Recursively check that a tree is not already binarized.</summary>
		private static bool IsBinarized(Tree tree)
		{
			if (tree.IsLeaf())
			{
				return true;
			}
			if (tree.Children().Length > 2)
			{
				return false;
			}
			foreach (Tree child in tree.Children())
			{
				if (!IsBinarized(child))
				{
					return false;
				}
			}
			return true;
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(TreeCoreAnnotations.TreeAnnotation))));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.Singleton(typeof(TreeCoreAnnotations.BinarizedTreeAnnotation));
		}
	}
}
