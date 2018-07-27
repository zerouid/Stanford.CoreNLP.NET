using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;








namespace Edu.Stanford.Nlp.Process
{
	/// <summary>A set of utilities for parsing TSV files into CoreMaps</summary>
	/// <author>Gabor Angeli</author>
	public class TSVUtils
	{
		private TSVUtils()
		{
		}

		// static methods
		internal static string UnescapeSQL(string input)
		{
			// If the string is quoted
			if (input.StartsWith("\"") && input.EndsWith("\""))
			{
				input = Sharpen.Runtime.Substring(input, 1, input.Length - 1);
			}
			return input.Replace("\"\"", "\"").Replace("\\\\", "\\");
		}

		/// <summary>Parse an SQL array.</summary>
		/// <remarks>
		/// Parse an SQL array.
		/// This code allows fixing "doubly escaped" quotes, but would fail if you actually
		/// wanted \\\\ to become two backslashes in a string (see the tests).
		/// It also doesn't support single quote strings in SQL, since our output/tests write single quotes without
		/// escaping or quoting, but, really you should for SQL, as I understand things....
		/// And it has special support for double double quoting an entire string ... but this disables having empty strings.
		/// I think that output comes from incorrectly not undoubling quotes in a String at an earlier stage, and so it
		/// should be fixed earlier.
		/// </remarks>
		/// <param name="array">The array to parse.</param>
		/// <returns>The parsed array, as a list.</returns>
		public static IList<string> ParseArray(string array)
		{
			// array = unescapeSQL(array);
			if (array.StartsWith("{") && array.EndsWith("}"))
			{
				array = Sharpen.Runtime.Substring(array, 1, array.Length - 1);
			}
			array = array.Replace("\\\\", "\\");
			// The questionable code for "doubly escaped" things
			char[] input = array.ToCharArray();
			IList<string> output = new List<string>();
			StringBuilder elem = new StringBuilder();
			bool inQuotes = false;
			bool escaped = false;
			bool doubledQuotes = false;
			char lastQuoteChar = '\0';
			for (int i = 0; i < input.Length; i++)
			{
				char c = input[i];
				char next = (i == input.Length - 1) ? '\0' : input[i + 1];
				if (escaped)
				{
					elem.Append(c);
					escaped = false;
				}
				else
				{
					if (c == '"')
					{
						// to support single quote escaping add:  || c == '\''
						if (!inQuotes)
						{
							inQuotes = true;
							escaped = false;
							lastQuoteChar = c;
							if (next == c)
							{
								// supporting doubling of beginning quote, expect doubling of ending, disable support for internal doubling
								i++;
								doubledQuotes = true;
							}
						}
						else
						{
							if (c == lastQuoteChar)
							{
								if (next == lastQuoteChar && !doubledQuotes)
								{
									// doubled quote escaping
									escaped = true;
								}
								else
								{
									inQuotes = false;
									escaped = false;
									if (doubledQuotes)
									{
										i++;
										doubledQuotes = false;
									}
								}
							}
							else
							{
								// different quote char, just like literal
								elem.Append(c);
							}
						}
					}
					else
					{
						if (c == '\\')
						{
							escaped = true;
						}
						else
						{
							if (inQuotes)
							{
								elem.Append(c);
							}
							else
							{
								if (c == ',')
								{
									output.Add(elem.ToString());
									elem.Length = 0;
								}
								else
								{
									// This is basically .clear()
									elem.Append(c);
								}
							}
							escaped = false;
						}
					}
				}
			}
			if (elem.Length > 0)
			{
				output.Add(elem.ToString());
			}
			return output;
		}

		private static readonly Pattern newline = Pattern.Compile("\\\\n");

		private static readonly Pattern tab = Pattern.Compile("\\\\t");

		/// <summary>Parse a CoNLL formatted tree into a SemanticGraph.</summary>
		/// <param name="conll">The CoNLL tree to parse.</param>
		/// <param name="tokens">The tokens of the sentence, to form the backing labels of the tree.</param>
		/// <returns>A semantic graph of the sentence, according to the given tree.</returns>
		public static SemanticGraph ParseTree(string conll, IList<CoreLabel> tokens)
		{
			SemanticGraph tree = new SemanticGraph();
			if (conll == null || conll.IsEmpty())
			{
				return tree;
			}
			string[] treeLines = newline.Split(conll);
			IndexedWord[] vertices = new IndexedWord[tokens.Count + 2];
			// Add edges
			foreach (string line in treeLines)
			{
				// Parse row
				string[] fields = tab.Split(line);
				int dependentIndex = System.Convert.ToInt32(fields[0]);
				if (vertices[dependentIndex] == null)
				{
					if (dependentIndex > tokens.Count)
					{
						// Bizarre mismatch in sizes; the malt parser seems to do this often
						return new SemanticGraph();
					}
					vertices[dependentIndex] = new IndexedWord(tokens[dependentIndex - 1]);
				}
				IndexedWord dependent = vertices[dependentIndex];
				int governorIndex = System.Convert.ToInt32(fields[1]);
				if (governorIndex > tokens.Count)
				{
					// Bizarre mismatch in sizes; the malt parser seems to do this often
					return new SemanticGraph();
				}
				if (vertices[governorIndex] == null && governorIndex > 0)
				{
					vertices[governorIndex] = new IndexedWord(tokens[governorIndex - 1]);
				}
				IndexedWord governor = vertices[governorIndex];
				string relation = fields[2];
				// Process row
				if (governorIndex == 0)
				{
					tree.AddRoot(dependent);
				}
				else
				{
					tree.AddVertex(dependent);
					if (!tree.ContainsVertex(governor))
					{
						tree.AddVertex(governor);
					}
					if (!"ref".Equals(relation))
					{
						tree.AddEdge(governor, dependent, GrammaticalRelation.ValueOf(Language.English, relation), double.NegativeInfinity, false);
					}
				}
			}
			return tree;
		}

		/// <summary>Parse a JSON formatted tree into a SemanticGraph.</summary>
		/// <param name="jsonString">
		/// The JSON string tree to parse, e.g:
		/// "[{\"\"dependent\"\": 7, \"\"dep\"\": \"\"root\"\", \"\"governorgloss\"\": \"\"root\"\", \"\"governor\"\": 0, \"\"dependentgloss\"\": \"\"sport\"\"}, {\"\"dependent\"\": 1, \"\"dep\"\": \"\"nsubj\"\", \"\"governorgloss\"\": \"\"sport\"\", \"\"governor\"\": 7, \"\"dependentgloss\"\": \"\"chess\"\"}, {\"\"dependent\"\": 2, \"\"dep\"\": \"\"cop\"\", \"\"governorgloss\"\": \"\"sport\"\", \"\"governor\"\": 7, \"\"dependentgloss\"\": \"\"is\"\"}, {\"\"dependent\"\": 3, \"\"dep\"\": \"\"neg\"\", \"\"governorgloss\"\": \"\"sport\"\", \"\"governor\"\": 7, \"\"dependentgloss\"\": \"\"not\"\"}, {\"\"dependent\"\": 4, \"\"dep\"\": \"\"det\"\", \"\"governorgloss\"\": \"\"sport\"\", \"\"governor\"\": 7, \"\"dependentgloss\"\": \"\"a\"\"}, {\"\"dependent\"\": 5, \"\"dep\"\": \"\"advmod\"\", \"\"governorgloss\"\": \"\"physical\"\", \"\"governor\"\": 6, \"\"dependentgloss\"\": \"\"predominantly\"\"}, {\"\"dependent\"\": 6, \"\"dep\"\": \"\"amod\"\", \"\"governorgloss\"\": \"\"sport\"\", \"\"governor\"\": 7, \"\"dependentgloss\"\": \"\"physical\"\"}, {\"\"dependent\"\": 9, \"\"dep\"\": \"\"advmod\"\", \"\"governorgloss\"\": \"\"sport\"\", \"\"governor\"\": 7, \"\"dependentgloss\"\": \"\"yet\"\"}, {\"\"dependent\"\": 10, \"\"dep\"\": \"\"nsubj\"\", \"\"governorgloss\"\": \"\"shooting\"\", \"\"governor\"\": 12, \"\"dependentgloss\"\": \"\"neither\"\"}, {\"\"dependent\"\": 11, \"\"dep\"\": \"\"cop\"\", \"\"governorgloss\"\": \"\"shooting\"\", \"\"governor\"\": 12, \"\"dependentgloss\"\": \"\"are\"\"}, {\"\"dependent\"\": 12, \"\"dep\"\": \"\"parataxis\"\", \"\"governorgloss\"\": \"\"sport\"\", \"\"governor\"\": 7, \"\"dependentgloss\"\": \"\"shooting\"\"}, {\"\"dependent\"\": 13, \"\"dep\"\": \"\"cc\"\", \"\"governorgloss\"\": \"\"shooting\"\", \"\"governor\"\": 12, \"\"dependentgloss\"\": \"\"and\"\"}, {\"\"dependent\"\": 14, \"\"dep\"\": \"\"parataxis\"\", \"\"governorgloss\"\": \"\"sport\"\", \"\"governor\"\": 7, \"\"dependentgloss\"\": \"\"curling\"\"}, {\"\"dependent\"\": 14, \"\"dep\"\": \"\"conj:and\"\", \"\"governorgloss\"\": \"\"shooting\"\", \"\"governor\"\": 12, \"\"dependentgloss\"\": \"\"curling\"\"}, {\"\"dependent\"\": 16, \"\"dep\"\": \"\"nsubjpass\"\", \"\"governorgloss\"\": \"\"nicknamed\"\", \"\"governor\"\": 23, \"\"dependentgloss\"\": \"\"which\"\"}, {\"\"dependent\"\": 18, \"\"dep\"\": \"\"case\"\", \"\"governorgloss\"\": \"\"fact\"\", \"\"governor\"\": 19, \"\"dependentgloss\"\": \"\"in\"\"}, {\"\"dependent\"\": 19, \"\"dep\"\": \"\"nmod:in\"\", \"\"governorgloss\"\": \"\"nicknamed\"\", \"\"governor\"\": 23, \"\"dependentgloss\"\": \"\"fact\"\"}, {\"\"dependent\"\": 21, \"\"dep\"\": \"\"aux\"\", \"\"governorgloss\"\": \"\"nicknamed\"\", \"\"governor\"\": 23, \"\"dependentgloss\"\": \"\"has\"\"}, {\"\"dependent\"\": 22, \"\"dep\"\": \"\"auxpass\"\", \"\"governorgloss\"\": \"\"nicknamed\"\", \"\"governor\"\": 23, \"\"dependentgloss\"\": \"\"been\"\"}, {\"\"dependent\"\": 23, \"\"dep\"\": \"\"dep\"\", \"\"governorgloss\"\": \"\"shooting\"\", \"\"governor\"\": 12, \"\"dependentgloss\"\": \"\"nicknamed\"\"}, {\"\"dependent\"\": 25, \"\"dep\"\": \"\"dobj\"\", \"\"governorgloss\"\": \"\"nicknamed\"\", \"\"governor\"\": 23, \"\"dependentgloss\"\": \"\"chess\"\"}, {\"\"dependent\"\": 26, \"\"dep\"\": \"\"case\"\", \"\"governorgloss\"\": \"\"ice\"\", \"\"governor\"\": 27, \"\"dependentgloss\"\": \"\"on\"\"}, {\"\"dependent\"\": 27, \"\"dep\"\": \"\"nmod:on\"\", \"\"governorgloss\"\": \"\"chess\"\", \"\"governor\"\": 25, \"\"dependentgloss\"\": \"\"ice\"\"}, {\"\"dependent\"\": 29, \"\"dep\"\": \"\"amod\"\", \"\"governorgloss\"\": \"\"chess\"\", \"\"governor\"\": 25, \"\"dependentgloss\"\": \"\"5\"\"}]");
		/// </param>
		/// <param name="tokens">The tokens of the sentence, to form the backing labels of the tree.</param>
		/// <returns>A semantic graph of the sentence, according to the given tree.</returns>
		public static SemanticGraph ParseJsonTree(string jsonString, IList<CoreLabel> tokens)
		{
			// Escape quoted string parts
			IJsonReader json = Javax.Json.Json.CreateReader(new StringReader(jsonString));
			SemanticGraph tree = new SemanticGraph();
			IJsonArray array = json.ReadArray();
			if (array == null || array.IsEmpty())
			{
				return tree;
			}
			IndexedWord[] vertices = new IndexedWord[tokens.Count + 2];
			// Add edges
			for (int i = 0; i < array.Count; i++)
			{
				IJsonObject entry = array.GetJsonObject(i);
				// Parse row
				int dependentIndex = entry.GetInt("dependent");
				if (vertices[dependentIndex] == null)
				{
					if (dependentIndex > tokens.Count)
					{
						// Bizarre mismatch in sizes; the malt parser seems to do this often
						return new SemanticGraph();
					}
					vertices[dependentIndex] = new IndexedWord(tokens[dependentIndex - 1]);
				}
				IndexedWord dependent = vertices[dependentIndex];
				int governorIndex = entry.GetInt("governor");
				if (governorIndex > tokens.Count)
				{
					// Bizarre mismatch in sizes; the malt parser seems to do this often
					return new SemanticGraph();
				}
				if (vertices[governorIndex] == null && governorIndex > 0)
				{
					vertices[governorIndex] = new IndexedWord(tokens[governorIndex - 1]);
				}
				IndexedWord governor = vertices[governorIndex];
				string relation = entry.GetString("dep");
				// Process row
				if (governorIndex == 0)
				{
					tree.AddRoot(dependent);
				}
				else
				{
					tree.AddVertex(dependent);
					if (!tree.ContainsVertex(governor))
					{
						tree.AddVertex(governor);
					}
					if (!"ref".Equals(relation))
					{
						tree.AddEdge(governor, dependent, GrammaticalRelation.ValueOf(Language.English, relation), double.NegativeInfinity, false);
					}
				}
			}
			return tree;
		}

		/// <summary>Create an Annotation object (with a single sentence) from the given specification.</summary>
		private static Annotation ParseSentence(Optional<string> docid, Optional<int> sentenceIndex, string gloss, IFunction<IList<CoreLabel>, SemanticGraph> tree, IFunction<IList<CoreLabel>, SemanticGraph> maltTree, IList<string> words, IList<string
			> lemmas, IList<string> pos, IList<string> ner, Optional<string> sentenceid)
		{
			// Error checks
			if (lemmas.Count != words.Count)
			{
				throw new ArgumentException("Array lengths don't match: " + words.Count + " vs " + lemmas.Count + " (sentence " + sentenceid.OrElse("???") + ")");
			}
			if (pos.Count != words.Count)
			{
				throw new ArgumentException("Array lengths don't match: " + words.Count + " vs " + pos.Count + " (sentence " + sentenceid.OrElse("???") + ")");
			}
			if (ner.Count != words.Count)
			{
				throw new ArgumentException("Array lengths don't match: " + words.Count + " vs " + ner.Count + " (sentence " + sentenceid.OrElse("???") + ")");
			}
			// Create structure
			IList<CoreLabel> tokens = new List<CoreLabel>(words.Count);
			int beginChar = 0;
			for (int i = 0; i < words.Count; ++i)
			{
				CoreLabel token = new CoreLabel(12);
				token.SetWord(words[i]);
				token.SetValue(words[i]);
				token.SetBeginPosition(beginChar);
				token.SetEndPosition(beginChar + words[i].Length);
				beginChar += words[i].Length + 1;
				token.SetLemma(lemmas[i]);
				token.SetTag(pos[i]);
				token.SetNER(ner[i]);
				token.Set(typeof(CoreAnnotations.DocIDAnnotation), docid.OrElse("???"));
				token.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentenceIndex.OrElse(-1));
				token.Set(typeof(CoreAnnotations.IndexAnnotation), i + 1);
				token.Set(typeof(CoreAnnotations.TokenBeginAnnotation), i);
				token.Set(typeof(CoreAnnotations.TokenEndAnnotation), i + 1);
				tokens.Add(token);
			}
			gloss = gloss.Replace("\\n", "\n").Replace("\\t", "\t");
			ICoreMap sentence = new ArrayCoreMap(16);
			sentence.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
			SemanticGraph graph = tree.Apply(tokens);
			sentence.Set(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), graph);
			sentence.Set(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation), graph);
			sentence.Set(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation), graph);
			SemanticGraph maltGraph = maltTree.Apply(tokens);
			sentence.Set(typeof(SemanticGraphCoreAnnotations.AlternativeDependenciesAnnotation), maltGraph);
			sentence.Set(typeof(CoreAnnotations.DocIDAnnotation), docid.OrElse("???"));
			sentence.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentenceIndex.OrElse(-1));
			sentence.Set(typeof(CoreAnnotations.TextAnnotation), gloss);
			sentence.Set(typeof(CoreAnnotations.TokenBeginAnnotation), 0);
			sentence.Set(typeof(CoreAnnotations.TokenEndAnnotation), tokens.Count);
			Annotation doc = new Annotation(gloss);
			doc.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
			doc.Set(typeof(CoreAnnotations.SentencesAnnotation), Java.Util.Collections.SingletonList(sentence));
			doc.Set(typeof(CoreAnnotations.DocIDAnnotation), docid.OrElse("???"));
			doc.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentenceIndex.OrElse(-1));
			return doc;
		}

		/// <summary>Create an Annotation object (with a single sentence) from the given specification, as Postgres would output them</summary>
		public static Annotation ParseSentence(Optional<string> docid, Optional<string> sentenceIndex, string gloss, string dependencies, string maltDependencies, string words, string lemmas, string posTags, string nerTags, Optional<string> sentenceid
			)
		{
			return ParseSentence(docid, sentenceIndex.Map(null), gloss, null, null, ParseArray(words), ParseArray(lemmas), ParseArray(posTags), ParseArray(nerTags), sentenceid);
		}
	}
}
