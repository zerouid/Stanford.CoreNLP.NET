using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// An extension of
	/// <see cref="AbstractTreebankParserParams"/>
	/// which provides support for Tregex-powered annotations.
	/// Subclasses of this class provide collections of <em>features</em>
	/// which are associated with annotation behaviors that seek out
	/// and label matching trees in some way. For example, a <em>coord</em>
	/// feature might have an annotation behavior which searches for
	/// coordinating noun phrases and labels the associated constituent
	/// with a suffix <tt>-coordinating</tt>.
	/// The "search" in this process is conducted via Tregex, and the
	/// actual annotation is done through execution of an arbitrary
	/// <see cref="Java.Util.Function.IFunction{T, R}"/>
	/// provided by the user.
	/// This class carries as inner several classes several useful common
	/// annotation functions.
	/// </summary>
	/// <seealso cref="annotations"/>
	/// <seealso cref="SimpleStringFunction"/>
	/// <author>Jon Gauthier</author>
	/// <author>Spence Green</author>
	[System.Serializable]
	public abstract class TregexPoweredTreebankParserParams : AbstractTreebankParserParams
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.TregexPoweredTreebankParserParams));

		private const long serialVersionUID = -1985603901694682420L;

		/// <summary>
		/// This data structure dictates how an arbitrary tree should be
		/// annotated.
		/// </summary>
		/// <remarks>
		/// This data structure dictates how an arbitrary tree should be
		/// annotated. Subclasses should fill out the related member
		/// <see cref="annotations"/>
		/// .
		/// It is a collection of <em>features:</em> a map from feature name
		/// to behavior, where each behavior is a tuple <tt>(t, f)</tt>.
		/// <tt>t</tt> is a Tregex pattern which matches subtrees
		/// corresponding to the feature, and <tt>f</tt> is a function which
		/// accepts such matches and generates an annotation which the matched
		/// subtree should be given.
		/// </remarks>
		/// <seealso cref="annotations"/>
		private readonly IDictionary<string, Pair<TregexPattern, IFunction<TregexMatcher, string>>> annotationPatterns = Generics.NewHashMap();

		/// <summary>
		/// This data structure dictates how an arbitrary tree should be
		/// annotated.
		/// </summary>
		/// <remarks>
		/// This data structure dictates how an arbitrary tree should be
		/// annotated.
		/// It is a collection of <em>features:</em> a map from feature name
		/// to behavior, where each behavior is a tuple <tt>(t, f)</tt>.
		/// <tt>t</tt> is a string form of a TregexPattern which matches
		/// subtrees corresponding to the feature, and <tt>f</tt> is a
		/// function which accepts such matches and generates an annotation
		/// which the matched subtree should be given.
		/// </remarks>
		/// <seealso cref="annotationPatterns"/>
		/// <seealso cref="SimpleStringFunction"/>
		protected internal readonly IDictionary<string, Pair<string, IFunction<TregexMatcher, string>>> annotations = Generics.NewHashMap();

		/// <summary>Features which should be enabled by default.</summary>
		protected internal abstract string[] BaselineAnnotationFeatures();

		/// <summary>Extra features which have been requested.</summary>
		/// <remarks>
		/// Extra features which have been requested. Use
		/// <see cref="AddFeature(string)"/>
		/// to add features.
		/// </remarks>
		private readonly ICollection<string> features;

		public TregexPoweredTreebankParserParams(ITreebankLanguagePack tlp)
			: base(tlp)
		{
			features = CollectionUtils.AsSet(BaselineAnnotationFeatures());
		}

		/// <summary>
		/// Compile the
		/// <see cref="annotations"/>
		/// collection given a
		/// particular head finder. Subclasses should call this method at
		/// least once before the class is used, and whenever the head finder
		/// is changed.
		/// </summary>
		protected internal virtual void CompileAnnotations(IHeadFinder hf)
		{
			TregexPatternCompiler compiler = new TregexPatternCompiler(hf);
			annotationPatterns.Clear();
			foreach (KeyValuePair<string, Pair<string, IFunction<TregexMatcher, string>>> annotation in annotations)
			{
				TregexPattern compiled;
				try
				{
					compiled = compiler.Compile(annotation.Value.First());
				}
				catch (TregexParseException e)
				{
					int nth = annotationPatterns.Count + 1;
					log.Info("Parse exception on annotation pattern #" + nth + " initialization: " + e);
					continue;
				}
				Pair<TregexPattern, IFunction<TregexMatcher, string>> behavior = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(compiled, annotation.Value.Second());
				annotationPatterns[annotation.Key] = behavior;
			}
		}

		/// <summary>Enable an annotation feature.</summary>
		/// <remarks>
		/// Enable an annotation feature. If the provided feature has already
		/// been enabled, this method does nothing.
		/// </remarks>
		/// <param name="featureName"/>
		/// <exception cref="System.ArgumentException">
		/// If the provided feature
		/// name is unknown (i.e., if there is no entry in the
		/// <see cref="annotations"/>
		/// collection with the same name)
		/// </exception>
		protected internal virtual void AddFeature(string featureName)
		{
			if (!annotations.Contains(featureName))
			{
				throw new ArgumentException("Invalid feature name '" + featureName + "'");
			}
			if (!annotationPatterns.Contains(featureName))
			{
				throw new Exception("Compiled patterns out of sync with annotations data structure;" + "did you call compileAnnotations?");
			}
			features.Add(featureName);
		}

		/// <summary>Disable a feature.</summary>
		/// <remarks>
		/// Disable a feature. If the feature was never enabled, this method
		/// returns without error.
		/// </remarks>
		/// <param name="featureName"/>
		protected internal virtual void RemoveFeature(string featureName)
		{
			features.Remove(featureName);
		}

		/// <summary>
		/// This method does language-specific tree transformations such as annotating particular nodes with language-relevant
		/// features.
		/// </summary>
		/// <remarks>
		/// This method does language-specific tree transformations such as annotating particular nodes with language-relevant
		/// features. Such parameterizations should be inside the specific TreebankLangParserParams class.  This method is
		/// recursively applied to each node in the tree (depth first, left-to-right), so you shouldn't write this method to
		/// apply recursively to tree members.  This method is allowed to (and in some cases does) destructively change the
		/// input tree <code>t</code>. It changes both labels and the tree shape.
		/// </remarks>
		/// <param name="t">
		/// The input tree (with non-language specific annotation already done, so you need to strip back to basic
		/// categories)
		/// </param>
		/// <param name="root">The root of the current tree (can be null for words)</param>
		/// <returns>The fully annotated tree node (with daughters still as you want them in the final result)</returns>
		public override Tree TransformTree(Tree t, Tree root)
		{
			string newCat = t.Value() + GetAnnotationString(t, root);
			t.SetValue(newCat);
			if (t.IsPreTerminal() && t.Label() is IHasTag)
			{
				((IHasTag)t.Label()).SetTag(newCat);
			}
			return t;
		}

		/// <summary>Build a string of annotations for the given tree.</summary>
		/// <param name="t">
		/// The input tree (with non-language specific annotation
		/// already done, so you need to strip back to basic categories)
		/// </param>
		/// <param name="root">The root of the current tree (can be null for words)</param>
		/// <returns>
		/// A (possibly empty) string of annotations to add to the
		/// given tree
		/// </returns>
		protected internal virtual string GetAnnotationString(Tree t, Tree root)
		{
			// Accumulate all annotations in this string
			StringBuilder annotationStr = new StringBuilder();
			foreach (string featureName in features)
			{
				Pair<TregexPattern, IFunction<TregexMatcher, string>> behavior = annotationPatterns[featureName];
				TregexMatcher m = behavior.First().Matcher(root);
				if (m.MatchesAt(t))
				{
					annotationStr.Append(behavior.Second().Apply(m));
				}
			}
			return annotationStr.ToString();
		}

		/// <summary>
		/// Output a description of the current annotation configuration to
		/// standard error.
		/// </summary>
		public override void Display()
		{
			foreach (string feature in features)
			{
				System.Console.Error.Printf("%s ", feature);
			}
			log.Info();
		}

		/// <summary>Annotates all nodes that match the tregex query with some string.</summary>
		[System.Serializable]
		protected internal class SimpleStringFunction : ISerializableFunction<TregexMatcher, string>
		{
			private const long serialVersionUID = 6958776731059724396L;

			private string annotationMark;

			public SimpleStringFunction(string annotationMark)
			{
				this.annotationMark = annotationMark;
			}

			public virtual string Apply(TregexMatcher matcher)
			{
				return annotationMark;
			}

			public override string ToString()
			{
				return "SimpleStringFunction[" + annotationMark + ']';
			}
		}

		/// <summary>Annotate a tree constituent with its lexical head.</summary>
		[System.Serializable]
		protected internal class AnnotateHeadFunction : ISerializableFunction<TregexMatcher, string>
		{
			private const long serialVersionUID = -4213299755069618322L;

			private readonly IHeadFinder headFinder;

			private bool lowerCase;

			public AnnotateHeadFunction(IHeadFinder hf)
				: this(hf, true)
			{
			}

			public AnnotateHeadFunction(IHeadFinder hf, bool lowerCase)
			{
				headFinder = hf;
				this.lowerCase = lowerCase;
			}

			public virtual string Apply(TregexMatcher matcher)
			{
				Tree matchedTree = matcher.GetMatch();
				Tree head = headFinder.DetermineHead(matchedTree);
				if (!head.IsPrePreTerminal())
				{
					return string.Empty;
				}
				Tree lexicalHead = head.FirstChild().FirstChild();
				string headValue = lexicalHead.Value();
				if (headValue != null)
				{
					if (lowerCase)
					{
						headValue = headValue.ToLower();
					}
					return '[' + headValue + ']';
				}
				else
				{
					return string.Empty;
				}
			}

			public override string ToString()
			{
				return "AnnotateHeadFunction[" + headFinder.GetType().FullName + ']';
			}
		}
	}
}
