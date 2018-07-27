using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Write a subset of our CoreNLP output in CoNLL format.</summary>
	/// <remarks>
	/// Write a subset of our CoreNLP output in CoNLL format.
	/// The output can be customized to write any set of keys available with names as defined by AnnotationLookup,
	/// and in addition these specials: ID (token index in sentence, numbering from 1).
	/// The default fields currently output are:
	/// <table>
	/// <tr>
	/// <td>Field Number</td>
	/// <td>Field Name</td>
	/// <td>Description</td>
	/// </tr>
	/// <tr>
	/// <td>1</td>
	/// <td>ID (idx)</td>
	/// <td>Token Counter, starting at 1 for each new sentence.</td>
	/// </tr>
	/// <tr>
	/// <td>2</td>
	/// <td>FORM (word)</td>
	/// <td>Word form or punctuation symbol.</td>
	/// </tr>
	/// <tr>
	/// <td>3</td>
	/// <td>LEMMA (lemma)</td>
	/// <td>Lemma of word form, or an underscore if not available.</td>
	/// </tr>
	/// <tr>
	/// <td>4</td>
	/// <td>POSTAG (pos)</td>
	/// <td>Fine-grained part-of-speech tag, or underscore if not available.</td>
	/// </tr>
	/// <tr>
	/// <td>5</td>
	/// <td>NER (ner)</td>
	/// <td>Named Entity tag, or underscore if not available.</td>
	/// </tr>
	/// <tr>
	/// <td>6</td>
	/// <td>HEAD (headidx)</td>
	/// <td>Head of the current token, which is either a value of ID or zero ('0').
	/// This is underscore if not available.</td>
	/// </tr>
	/// <tr>
	/// <td>7</td>
	/// <td>DEPREL (deprel)</td>
	/// <td>Dependency relation to the HEAD, or underscore if not available.</td>
	/// </tr>
	/// </table>
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class CoNLLOutputter : AnnotationOutputter
	{
		private const string NullPlaceholder = "_";

		private const string DefaultKeys = "idx,word,lemma,pos,ner,headidx,deprel";

		private readonly IList<Type> keysToPrint;

		public CoNLLOutputter()
			: this(null)
		{
		}

		public CoNLLOutputter(string keys)
		{
			if (keys == null)
			{
				keys = DefaultKeys;
			}
			string[] keyArray = keys.Split(" *, *");
			IList<Type> keyList = new List<Type>();
			foreach (string key in keyArray)
			{
				keyList.Add(AnnotationLookup.ToCoreKey(key));
			}
			keysToPrint = keyList;
		}

		private static string OrNeg(int @in)
		{
			if (@in < 0)
			{
				return NullPlaceholder;
			}
			else
			{
				return int.ToString(@in);
			}
		}

		private static string OrNull(object @in)
		{
			if (@in == null)
			{
				return NullPlaceholder;
			}
			else
			{
				return @in.ToString();
			}
		}

		/// <summary>Produce a line of the CoNLL output.</summary>
		private string Line(int index, CoreLabel token, int head, string deprel)
		{
			List<string> fields = new List<string>(keysToPrint.Count);
			foreach (Type keyClass in keysToPrint)
			{
				if (keyClass.Equals(typeof(CoreAnnotations.IndexAnnotation)))
				{
					fields.Add(OrNull(index));
				}
				else
				{
					if (keyClass.Equals(typeof(CoreAnnotations.CoNLLDepTypeAnnotation)))
					{
						fields.Add(OrNull(deprel));
					}
					else
					{
						if (keyClass.Equals(typeof(CoreAnnotations.CoNLLDepParentIndexAnnotation)))
						{
							fields.Add(OrNeg(head));
						}
						else
						{
							fields.Add(OrNull(token.Get((Type)keyClass)));
						}
					}
				}
			}
			/*
			fields.add(Integer.toString(index)); // 1
			fields.add(orNull(token.word()));    // 2
			fields.add(orNull(token.lemma()));   // 3
			fields.add(orNull(token.tag()));     // 4
			fields.add(orNull(token.ner()));     // 5
			if (head >= 0) {
			fields.add(Integer.toString(head));  // 6
			fields.add(deprel);                  // 7
			} else {
			fields.add(NULL_PLACEHOLDER);
			fields.add(NULL_PLACEHOLDER);
			}
			*/
			return StringUtils.Join(fields, "\t");
		}

		/// <summary>Print an Annotation to an output stream.</summary>
		/// <remarks>
		/// Print an Annotation to an output stream.
		/// The target OutputStream is assumed to already by buffered.
		/// </remarks>
		/// <param name="doc"/>
		/// <param name="target"/>
		/// <param name="options"/>
		/// <exception cref="System.IO.IOException"/>
		public override void Print(Annotation doc, OutputStream target, AnnotationOutputter.Options options)
		{
			PrintWriter writer = new PrintWriter(IOUtils.EncodedOutputStreamWriter(target, options.encoding));
			// vv A bunch of nonsense to get tokens vv
			if (doc.Get(typeof(CoreAnnotations.SentencesAnnotation)) != null)
			{
				foreach (ICoreMap sentence in doc.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					if (sentence.Get(typeof(CoreAnnotations.TokensAnnotation)) != null)
					{
						IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
						SemanticGraph depTree = sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
						for (int i = 0; i < tokens.Count; ++i)
						{
							// ^^ end nonsense to get tokens ^^
							// Try to get the incoming dependency edge
							int head = -1;
							string deprel = null;
							if (depTree != null)
							{
								ICollection<int> rootSet = depTree.GetRoots().Stream().Map(null).Collect(Collectors.ToSet());
								IndexedWord node = depTree.GetNodeByIndexSafe(i + 1);
								if (node != null)
								{
									IList<SemanticGraphEdge> edgeList = depTree.GetIncomingEdgesSorted(node);
									if (!edgeList.IsEmpty())
									{
										System.Diagnostics.Debug.Assert(edgeList.Count == 1);
										head = edgeList[0].GetGovernor().Index();
										deprel = edgeList[0].GetRelation().ToString();
									}
									else
									{
										if (rootSet.Contains(i + 1))
										{
											head = 0;
											deprel = "ROOT";
										}
									}
								}
							}
							// Write the token
							writer.Print(Line(i + 1, tokens[i], head, deprel));
							writer.Println();
						}
					}
					writer.Println();
				}
			}
			// extra blank line at end of sentence
			writer.Flush();
		}

		/// <exception cref="System.IO.IOException"/>
		public static void ConllPrint(Annotation annotation, OutputStream os)
		{
			new Edu.Stanford.Nlp.Pipeline.CoNLLOutputter().Print(annotation, os);
		}

		/// <exception cref="System.IO.IOException"/>
		public static void ConllPrint(Annotation annotation, OutputStream os, StanfordCoreNLP pipeline)
		{
			new Edu.Stanford.Nlp.Pipeline.CoNLLOutputter().Print(annotation, os, pipeline);
		}

		/// <exception cref="System.IO.IOException"/>
		public static void ConllPrint(Annotation annotation, OutputStream os, AnnotationOutputter.Options options)
		{
			new Edu.Stanford.Nlp.Pipeline.CoNLLOutputter().Print(annotation, os, options);
		}
	}
}
