using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Simple;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>Reads sentences from a TSV, provided a list of fields to populate.</summary>
	/// <author>Arun Chaganty</author>
	public class TSVSentenceIterator : IEnumerator<Sentence>
	{
		/// <summary>A list of possible fields in the sentence table</summary>
		[System.Serializable]
		public sealed class SentenceField
		{
			public static readonly TSVSentenceIterator.SentenceField Id = new TSVSentenceIterator.SentenceField();

			public static readonly TSVSentenceIterator.SentenceField DependenciesBasic = new TSVSentenceIterator.SentenceField();

			public static readonly TSVSentenceIterator.SentenceField DependenciesCollapsed = new TSVSentenceIterator.SentenceField();

			public static readonly TSVSentenceIterator.SentenceField DependenciesCollapsedCc = new TSVSentenceIterator.SentenceField();

			public static readonly TSVSentenceIterator.SentenceField DependenciesAlternate = new TSVSentenceIterator.SentenceField();

			public static readonly TSVSentenceIterator.SentenceField Words = new TSVSentenceIterator.SentenceField();

			public static readonly TSVSentenceIterator.SentenceField Lemmas = new TSVSentenceIterator.SentenceField();

			public static readonly TSVSentenceIterator.SentenceField PosTags = new TSVSentenceIterator.SentenceField();

			public static readonly TSVSentenceIterator.SentenceField NerTags = new TSVSentenceIterator.SentenceField();

			public static readonly TSVSentenceIterator.SentenceField DocId = new TSVSentenceIterator.SentenceField();

			public static readonly TSVSentenceIterator.SentenceField SentenceIndex = new TSVSentenceIterator.SentenceField();

			public static readonly TSVSentenceIterator.SentenceField CorpusId = new TSVSentenceIterator.SentenceField();

			public static readonly TSVSentenceIterator.SentenceField DocCharBegin = new TSVSentenceIterator.SentenceField();

			public static readonly TSVSentenceIterator.SentenceField DocCharEnd = new TSVSentenceIterator.SentenceField();

			public static readonly TSVSentenceIterator.SentenceField Gloss = new TSVSentenceIterator.SentenceField();

			public static readonly TSVSentenceIterator.SentenceField Ignore = new TSVSentenceIterator.SentenceField();

			// Ignore this field.
			public bool IsToken()
			{
				switch (this)
				{
					case TSVSentenceIterator.SentenceField.Words:
					case TSVSentenceIterator.SentenceField.Lemmas:
					case TSVSentenceIterator.SentenceField.PosTags:
					case TSVSentenceIterator.SentenceField.NerTags:
					{
						return true;
					}

					default:
					{
						return false;
					}
				}
			}
		}

		private readonly IEnumerator<IList<string>> source;

		private readonly IList<TSVSentenceIterator.SentenceField> fields;

		public TSVSentenceIterator(IEnumerator<IList<string>> recordSource, IList<TSVSentenceIterator.SentenceField> fields)
		{
			this.source = recordSource;
			this.fields = fields;
		}

		/// <summary>Populates the fields of a sentence</summary>
		/// <param name="fields"/>
		/// <param name="entries"/>
		/// <returns/>
		public static Sentence ToSentence(IList<TSVSentenceIterator.SentenceField> fields, IList<string> entries)
		{
			return new Sentence(ToCoreMap(fields, entries));
		}

		public static ICoreMap ToCoreMap(IList<TSVSentenceIterator.SentenceField> fields, IList<string> entries)
		{
			ICoreMap map = new ArrayCoreMap(fields.Count);
			Optional<IList<CoreLabel>> tokens = Optional.Empty();
			// First pass - process all token level stuff.
			foreach (Pair<TSVSentenceIterator.SentenceField, string> entry in Iterables.Zip(fields, entries))
			{
				TSVSentenceIterator.SentenceField field = entry.first;
				string value = TSVUtils.UnescapeSQL(entry.second);
				switch (field)
				{
					case TSVSentenceIterator.SentenceField.Words:
					{
						IList<string> values = TSVUtils.ParseArray(value);
						if (!tokens.IsPresent())
						{
							tokens = Optional.Of(new List<CoreLabel>(values.Count));
							for (int i = 0; i < values.Count; i++)
							{
								tokens.Get().Add(new CoreLabel());
							}
						}
						int beginChar = 0;
						for (int i_1 = 0; i_1 < values.Count; i_1++)
						{
							tokens.Get()[i_1].SetValue(values[i_1]);
							tokens.Get()[i_1].SetWord(values[i_1]);
							tokens.Get()[i_1].SetBeginPosition(beginChar);
							tokens.Get()[i_1].SetEndPosition(beginChar + values[i_1].Length);
							beginChar += values[i_1].Length + 1;
						}
						break;
					}

					case TSVSentenceIterator.SentenceField.Lemmas:
					{
						IList<string> values = TSVUtils.ParseArray(value);
						if (!tokens.IsPresent())
						{
							tokens = Optional.Of(new List<CoreLabel>(values.Count));
							for (int i = 0; i < values.Count; i++)
							{
								tokens.Get().Add(new CoreLabel());
							}
						}
						for (int i_1 = 0; i_1 < values.Count; i_1++)
						{
							tokens.Get()[i_1].SetLemma(values[i_1]);
						}
						break;
					}

					case TSVSentenceIterator.SentenceField.PosTags:
					{
						IList<string> values = TSVUtils.ParseArray(value);
						if (!tokens.IsPresent())
						{
							tokens = Optional.Of(new List<CoreLabel>(values.Count));
							for (int i = 0; i < values.Count; i++)
							{
								tokens.Get().Add(new CoreLabel());
							}
						}
						for (int i_1 = 0; i_1 < values.Count; i_1++)
						{
							tokens.Get()[i_1].SetTag(values[i_1]);
						}
						break;
					}

					case TSVSentenceIterator.SentenceField.NerTags:
					{
						IList<string> values = TSVUtils.ParseArray(value);
						if (!tokens.IsPresent())
						{
							tokens = Optional.Of(new List<CoreLabel>(values.Count));
							for (int i = 0; i < values.Count; i++)
							{
								tokens.Get().Add(new CoreLabel());
							}
						}
						for (int i_1 = 0; i_1 < values.Count; i_1++)
						{
							tokens.Get()[i_1].SetNER(values[i_1]);
						}
						break;
					}

					default:
					{
						// ignore.
						break;
					}
				}
			}
			// Document specific stuff.
			Optional<string> docId = Optional.Empty();
			Optional<string> sentenceId = Optional.Empty();
			Optional<int> sentenceIndex = Optional.Empty();
			foreach (Pair<TSVSentenceIterator.SentenceField, string> entry_1 in Iterables.Zip(fields, entries))
			{
				TSVSentenceIterator.SentenceField field = entry_1.first;
				string value = TSVUtils.UnescapeSQL(entry_1.second);
				switch (field)
				{
					case TSVSentenceIterator.SentenceField.Id:
					{
						sentenceId = Optional.Of(value);
						break;
					}

					case TSVSentenceIterator.SentenceField.DocId:
					{
						docId = Optional.Of(value);
						break;
					}

					case TSVSentenceIterator.SentenceField.SentenceIndex:
					{
						sentenceIndex = Optional.Of(System.Convert.ToInt32(value));
						break;
					}

					case TSVSentenceIterator.SentenceField.Gloss:
					{
						value = value.Replace("\\n", "\n").Replace("\\t", "\t");
						map.Set(typeof(CoreAnnotations.TextAnnotation), value);
						break;
					}

					default:
					{
						// ignore.
						break;
					}
				}
			}
			// High level document stuff
			map.Set(typeof(CoreAnnotations.SentenceIDAnnotation), sentenceId.OrElse("-1"));
			map.Set(typeof(CoreAnnotations.DocIDAnnotation), docId.OrElse("???"));
			map.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentenceIndex.OrElse(0));
			// Doc-char
			if (tokens.IsPresent())
			{
				foreach (Pair<TSVSentenceIterator.SentenceField, string> entry_2 in Iterables.Zip(fields, entries))
				{
					TSVSentenceIterator.SentenceField field = entry_2.first;
					string value = TSVUtils.UnescapeSQL(entry_2.second);
					switch (field)
					{
						case TSVSentenceIterator.SentenceField.DocCharBegin:
						{
							IList<string> values = TSVUtils.ParseArray(value);
							for (int i = 0; i < tokens.Get().Count; i++)
							{
								tokens.Get()[i].SetBeginPosition(System.Convert.ToInt32(values[i]));
							}
							break;
						}

						case TSVSentenceIterator.SentenceField.DocCharEnd:
						{
							IList<string> values = TSVUtils.ParseArray(value);
							for (int i = 0; i < tokens.Get().Count; i++)
							{
								tokens.Get()[i].SetEndPosition(System.Convert.ToInt32(values[i]));
							}
							break;
						}

						default:
						{
							// ignore.
							break;
						}
					}
				}
			}
			// Final token level stuff.
			if (tokens.IsPresent())
			{
				for (int i = 0; i < tokens.Get().Count; i++)
				{
					tokens.Get()[i].Set(typeof(CoreAnnotations.DocIDAnnotation), docId.OrElse("???"));
					tokens.Get()[i].Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentenceIndex.OrElse(-1));
					tokens.Get()[i].Set(typeof(CoreAnnotations.IndexAnnotation), i + 1);
					tokens.Get()[i].Set(typeof(CoreAnnotations.TokenBeginAnnotation), i);
					tokens.Get()[i].Set(typeof(CoreAnnotations.TokenEndAnnotation), i + 1);
				}
			}
			// Dependency trees
			if (tokens.IsPresent())
			{
				map.Set(typeof(CoreAnnotations.TokensAnnotation), tokens.Get());
				map.Set(typeof(CoreAnnotations.TokenBeginAnnotation), 0);
				map.Set(typeof(CoreAnnotations.TokenEndAnnotation), tokens.Get().Count);
				foreach (Pair<TSVSentenceIterator.SentenceField, string> entry_2 in Iterables.Zip(fields, entries))
				{
					TSVSentenceIterator.SentenceField field = entry_2.first;
					string value = TSVUtils.UnescapeSQL(entry_2.second);
					switch (field)
					{
						case TSVSentenceIterator.SentenceField.DependenciesBasic:
						{
							SemanticGraph graph = TSVUtils.ParseJsonTree(value, tokens.Get());
							map.Set(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), graph);
							//            if (!map.containsKey(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation.class))
							//              map.set(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation.class, graph);
							//            if (!map.containsKey(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation.class))
							//              map.set(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation.class, graph);
							break;
						}

						case TSVSentenceIterator.SentenceField.DependenciesCollapsed:
						{
							SemanticGraph graph = TSVUtils.ParseJsonTree(value, tokens.Get());
							map.Set(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation), graph);
							break;
						}

						case TSVSentenceIterator.SentenceField.DependenciesCollapsedCc:
						{
							SemanticGraph graph = TSVUtils.ParseJsonTree(value, tokens.Get());
							//            if (!map.containsKey(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation.class))
							//              map.set(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation.class, graph);
							//            map.set(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation.class, graph);
							map.Set(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation), graph);
							break;
						}

						case TSVSentenceIterator.SentenceField.DependenciesAlternate:
						{
							SemanticGraph graph = TSVUtils.ParseJsonTree(value, tokens.Get());
							map.Set(typeof(SemanticGraphCoreAnnotations.AlternativeDependenciesAnnotation), graph);
							break;
						}

						default:
						{
							// ignore.
							break;
						}
					}
				}
			}
			return map;
		}

		public virtual bool MoveNext()
		{
			return source.MoveNext();
		}

		public virtual Sentence Current
		{
			get
			{
				return ToSentence(fields, source.Current);
			}
		}
	}
}
