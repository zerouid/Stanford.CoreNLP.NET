using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Java.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Roth
{
	/// <summary>
	/// A Reader designed for the relation extraction data studied in Dan Roth and Wen-tau Yih,
	/// A Linear Programming Formulation for Global Inference in Natural Language Tasks.
	/// </summary>
	/// <remarks>
	/// A Reader designed for the relation extraction data studied in Dan Roth and Wen-tau Yih,
	/// A Linear Programming Formulation for Global Inference in Natural Language Tasks. CoNLL 2004.
	/// The format is a somewhat ad-hoc tab-separated value file format.
	/// </remarks>
	/// <author>Mihai, David McClosky, and agusev</author>
	/// <author>Sonal Gupta (sonalg@stanford.edu)</author>
	public class RothCONLL04Reader : GenericDataSetReader
	{
		public RothCONLL04Reader()
			: base(null, true, true, true)
		{
			// change the logger to one from our namespace
			logger = Logger.GetLogger(typeof(Edu.Stanford.Nlp.IE.Machinereading.Domains.Roth.RothCONLL04Reader).FullName);
			// run quietly by default
			logger.SetLevel(Level.Severe);
		}

		/// <exception cref="System.IO.IOException"/>
		public override Annotation Read(string path)
		{
			Annotation doc = new Annotation(string.Empty);
			logger.Info("Reading file: " + path);
			// Each iteration through this loop processes a single sentence along with any relations in it
			for (IEnumerator<string> lineIterator = IOUtils.ReadLines(path).GetEnumerator(); lineIterator.MoveNext(); )
			{
				Annotation sentence = ReadSentence(path, lineIterator);
				AnnotationUtils.AddSentence(doc, sentence);
			}
			return doc;
		}

		private bool warnedNER;

		// = false;
		private string GetNormalizedNERTag(string ner)
		{
			if (Sharpen.Runtime.EqualsIgnoreCase(ner, "O"))
			{
				return "O";
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(ner, "Peop"))
				{
					return "PERSON";
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(ner, "Loc"))
					{
						return "LOCATION";
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(ner, "Org"))
						{
							return "ORGANIZATION";
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(ner, "Other"))
							{
								return "OTHER";
							}
							else
							{
								if (!warnedNER)
								{
									warnedNER = true;
									logger.Warning("This file contains NER tags not in the original Roth/Yih dataset, e.g.: " + ner);
								}
							}
						}
					}
				}
			}
			throw new Exception("Cannot normalize ner tag " + ner);
		}

		private Annotation ReadSentence(string docId, IEnumerator<string> lineIterator)
		{
			Annotation sentence = new Annotation(string.Empty);
			sentence.Set(typeof(CoreAnnotations.DocIDAnnotation), docId);
			sentence.Set(typeof(MachineReadingAnnotations.EntityMentionsAnnotation), new List<EntityMention>());
			// we'll need to set things like the tokens and textContent after we've
			// fully read the sentence
			// contains the full text that we've read so far
			StringBuilder textContent = new StringBuilder();
			int tokenCount = 0;
			// how many tokens we've seen so far
			IList<CoreLabel> tokens = new List<CoreLabel>();
			// when we've seen two blank lines in a row, this sentence is over (one
			// blank line separates the sentence and the relations
			int numBlankLinesSeen = 0;
			string sentenceID = null;
			// keeps tracks of entities we've seen so far for use by relations
			IDictionary<string, EntityMention> indexToEntityMention = new Dictionary<string, EntityMention>();
			while (lineIterator.MoveNext() && numBlankLinesSeen < 2)
			{
				string currentLine = lineIterator.Current;
				currentLine = currentLine.Replace("COMMA", ",");
				IList<string> pieces = StringUtils.Split(currentLine);
				string identifier;
				int size = pieces.Count;
				switch (size)
				{
					case 1:
					{
						// blank line between sentences or relations
						numBlankLinesSeen++;
						break;
					}

					case 3:
					{
						// relation
						string type = pieces[2];
						IList<ExtractionObject> args = new List<ExtractionObject>();
						EntityMention entity1 = indexToEntityMention[pieces[0]];
						EntityMention entity2 = indexToEntityMention[pieces[1]];
						args.Add(entity1);
						args.Add(entity2);
						Span span = new Span(entity1.GetExtentTokenStart(), entity2.GetExtentTokenEnd());
						// identifier = "relation" + sentenceID + "-" + sentence.getAllRelations().size();
						identifier = RelationMention.MakeUniqueId();
						RelationMention relationMention = new RelationMention(identifier, sentence, span, type, null, args);
						AnnotationUtils.AddRelationMention(sentence, relationMention);
						break;
					}

					case 9:
					{
						// token
						/*
						* Roth token lines look like this:
						*
						* 19 Peop 9 O NNP/NNP Jamal/Ghosheh O O O
						*/
						// Entities may be multiple words joined by '/'; we split these up
						IList<string> words = StringUtils.Split(pieces[5], "/");
						//List<String> postags = StringUtils.split(pieces.get(4),"/");
						string text = StringUtils.Join(words, " ");
						identifier = "entity" + pieces[0] + '-' + pieces[2];
						string nerTag = GetNormalizedNERTag(pieces[1]);
						// entity type of the word/expression
						if (sentenceID == null)
						{
							sentenceID = pieces[0];
						}
						if (!nerTag.Equals("O"))
						{
							Span extentSpan = new Span(tokenCount, tokenCount + words.Count);
							// Temporarily sets the head span to equal the extent span.
							// This is so the entity has a head (in particular, getValue() works) even if preprocessSentences isn't called.
							// The head span is later modified if preprocessSentences is called.
							EntityMention entity = new EntityMention(identifier, sentence, extentSpan, extentSpan, nerTag, null, null);
							AnnotationUtils.AddEntityMention(sentence, entity);
							// we can get by using these indices as strings since we only use them
							// as a hash key
							string index = pieces[2];
							indexToEntityMention[index] = entity;
						}
						// int i =0;
						foreach (string word in words)
						{
							CoreLabel label = new CoreLabel();
							label.SetWord(word);
							//label.setTag(postags.get(i));
							label.Set(typeof(CoreAnnotations.TextAnnotation), word);
							label.Set(typeof(CoreAnnotations.ValueAnnotation), word);
							// we don't set TokenBeginAnnotation or TokenEndAnnotation since we're
							// not keeping track of character offsets
							tokens.Add(label);
						}
						// i++;
						textContent.Append(text);
						textContent.Append(' ');
						tokenCount += words.Count;
						break;
					}
				}
			}
			sentence.Set(typeof(CoreAnnotations.TextAnnotation), textContent.ToString());
			sentence.Set(typeof(CoreAnnotations.ValueAnnotation), textContent.ToString());
			sentence.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
			sentence.Set(typeof(CoreAnnotations.SentenceIDAnnotation), sentenceID);
			return sentence;
		}

		/*
		* Gets the index of an object in a list using == to test (List.indexOf uses
		* equals() which could be problematic here)
		*/
		private static int GetIndexByObjectEquality<X>(IList<X> list, X obj)
		{
			for (int i = 0; i < sz; i++)
			{
				if (list[i] == obj)
				{
					return i;
				}
			}
			return -1;
		}

		/*
		* Sets the head word and the index for an entity, given the parse tree for
		* the sentence containing the entity.
		*
		* This code is no longer used, but I've kept it around (at least for now) as
		* reference when we modify preProcessSentences().
		*/
		private void SetHeadWord(EntityMention entity, Tree tree)
		{
			IList<Tree> leaves = tree.GetLeaves();
			Tree argRoot = tree.JoinNode(leaves[entity.GetExtentTokenStart()], leaves[entity.GetExtentTokenEnd()]);
			Tree headWordNode = argRoot.HeadTerminal(headFinder);
			int headWordIndex = GetIndexByObjectEquality(leaves, headWordNode);
			if (StringUtils.IsPunct(leaves[entity.GetExtentTokenEnd()].Label().Value().Trim()) && (headWordIndex >= entity.GetExtentTokenEnd() || headWordIndex < entity.GetExtentTokenStart()))
			{
				argRoot = tree.JoinNode(leaves[entity.GetExtentTokenStart()], leaves[entity.GetExtentTokenEnd() - 1]);
				headWordNode = argRoot.HeadTerminal(headFinder);
				headWordIndex = GetIndexByObjectEquality(leaves, headWordNode);
				if (headWordIndex >= entity.GetExtentTokenStart() && headWordIndex <= entity.GetExtentTokenEnd() - 1)
				{
					entity.SetHeadTokenPosition(headWordIndex);
					entity.SetHeadTokenSpan(new Span(headWordIndex, headWordIndex + 1));
				}
			}
			if (headWordIndex >= entity.GetExtentTokenStart() && headWordIndex <= entity.GetExtentTokenEnd())
			{
				entity.SetHeadTokenPosition(headWordIndex);
				entity.SetHeadTokenSpan(new Span(headWordIndex, headWordIndex + 1));
			}
			else
			{
				// Re-parse the argument words by themselves
				// Get the list of words in the arg by looking at the leaves between
				// arg.getExtentTokenStart() and arg.getExtentTokenEnd() inclusive
				IList<string> argWords = new List<string>();
				for (int i = entity.GetExtentTokenStart(); i <= entity.GetExtentTokenEnd(); i++)
				{
					argWords.Add(leaves[i].Label().Value());
				}
				if (StringUtils.IsPunct(argWords[argWords.Count - 1]))
				{
					argWords.Remove(argWords.Count - 1);
				}
				Tree argTree = ParseStrings(argWords);
				headWordNode = argTree.HeadTerminal(headFinder);
				headWordIndex = GetIndexByObjectEquality(argTree.GetLeaves(), headWordNode) + entity.GetExtentTokenStart();
				entity.SetHeadTokenPosition(headWordIndex);
				entity.SetHeadTokenSpan(new Span(headWordIndex, headWordIndex + 1));
			}
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			// just a simple test, to make sure stuff works
			Properties props = StringUtils.ArgsToProperties(args);
			Edu.Stanford.Nlp.IE.Machinereading.Domains.Roth.RothCONLL04Reader reader = new Edu.Stanford.Nlp.IE.Machinereading.Domains.Roth.RothCONLL04Reader();
			reader.SetLoggerLevel(Level.Info);
			reader.SetProcessor(new StanfordCoreNLP(props));
			Annotation doc = reader.Parse("/u/nlp/data/RothCONLL04/conll04.corp");
			System.Console.Out.WriteLine(AnnotationUtils.DatasetToString(doc));
		}
	}
}
