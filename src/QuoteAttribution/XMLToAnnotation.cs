using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;



using Org.W3c.Dom;


namespace Edu.Stanford.Nlp.Quoteattribution
{
	/// <summary>Created by mjfang on 12/18/16.</summary>
	public class XMLToAnnotation
	{
		public static string GetJustText(INode text)
		{
			StringBuilder sb = new StringBuilder();
			INodeList textElems = text.GetChildNodes();
			for (int i = 0; i < textElems.GetLength(); i++)
			{
				INode child = textElems.Item(i);
				string str = child.GetTextContent();
				//replace single occurrence of \n with " ", double occurrences with a single one.
				str = str.ReplaceAll("\n(?!\n)", " ");
				str = str.ReplaceAll("_", string.Empty);
				//bug fix for sentence splitting
				sb.Append(str + " ");
			}
			return sb.ToString();
		}

		//for standard annotations + quotes
		public static Properties GetProcessedCoreNLPProperties()
		{
			Properties props = new Properties();
			props.SetProperty("annotators", "tokenize, ssplit, pos, lemma, ner, depparse, quote");
			props.SetProperty("ner.useSUTime", "false");
			props.SetProperty("ner.applyNumericClassifiers", "false");
			props.SetProperty("ssplit.newlineIsSentenceBreak", "always");
			props.SetProperty("outputFormat", "serialized");
			props.SetProperty("serializer", "edu.stanford.nlp.pipeline.ProtobufAnnotationSerializer");
			props.SetProperty("threads", "1");
			return props;
		}

		public static void ProcessCoreNLPIfDoesNotExist(File processedFile, Properties coreNLPProps, string text)
		{
			if (!processedFile.Exists())
			{
				try
				{
					StanfordCoreNLP coreNLP = new StanfordCoreNLP(coreNLPProps);
					Annotation processedAnnotation = coreNLP.Process(text);
					//this document holds the split for paragraphs.
					ProtobufAnnotationSerializer pas = new ProtobufAnnotationSerializer(true);
					OutputStream fos = new BufferedOutputStream(new FileOutputStream(processedFile.GetAbsolutePath()));
					pas.Write(processedAnnotation, fos);
				}
				catch (IOException e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static Annotation GetAnnotatedFile(string text, string baseFileName, Properties props)
		{
			File processedFile = new File(baseFileName + ".ser.gz");
			ProcessCoreNLPIfDoesNotExist(processedFile, props, text);
			Annotation doc = ExtractQuotesUtil.ReadSerializedProtobufFile(processedFile);
			new QuoteAnnotator(new Properties()).Annotate(doc);
			//important! Re-annotate to take into account that certain tokens are removed in the serialization process.
			return doc;
		}

		public static IList<int> ReadConnection(string connection)
		{
			IList<int> connectionList = new List<int>();
			if (connection.Equals(string.Empty))
			{
				return connectionList;
			}
			string[] connections = connection.Split(",");
			foreach (string c in connections)
			{
				connectionList.Add(System.Convert.ToInt32(Sharpen.Runtime.Substring(c, 1)));
			}
			return connectionList;
		}

		//return index of the token that ends this block of text.
		//key assumption: blocks are delimited by tokens (i.e. no token spans two blocks.)
		public static int GetEndIndex(int startIndex, IList<CoreLabel> tokens, string text)
		{
			text = text.Trim();
			//remove newlines that may throw off text length
			int currIndex = startIndex;
			CoreLabel token = tokens[startIndex];
			int tokenBeginChar = token.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			int offset = text.IndexOf(token.Get(typeof(CoreAnnotations.OriginalTextAnnotation)));
			while (true)
			{
				int tokenEndChar = token.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
				if (tokenEndChar - tokenBeginChar == text.Length)
				{
					return currIndex;
				}
				else
				{
					if (tokenEndChar - tokenBeginChar > text.Length)
					{
						return currIndex - 1;
					}
				}
				currIndex++;
				if (currIndex == tokens.Count)
				{
					return currIndex - 1;
				}
				token = tokens[currIndex];
			}
		}

		public class GoldQuoteInfo
		{
			public int mentionStartTokenIndex;

			public int mentionEndTokenIndex;

			public string speaker;

			public string mention;

			public GoldQuoteInfo(int mentionStartTokenIndex, int mentionEndTokenIndex, string speaker, string mention)
			{
				this.mentionStartTokenIndex = mentionStartTokenIndex;
				this.mentionEndTokenIndex = mentionEndTokenIndex;
				this.speaker = speaker;
				this.mention = mention;
			}
		}

		public class Data
		{
			public IList<XMLToAnnotation.GoldQuoteInfo> goldList;

			public IList<Person> personList;

			public Annotation doc;

			public Data(IList<XMLToAnnotation.GoldQuoteInfo> goldList, IList<Person> personList, Annotation doc)
			{
				//the gold values (mention location and speaker name) of the quotes
				this.goldList = goldList;
				this.personList = personList;
				this.doc = doc;
			}
		}

		public static IList<Person> ReadXMLCharacterList(IDocument doc)
		{
			IList<Person> personList = new List<Person>();
			INodeList characters = doc.GetDocumentElement().GetElementsByTagName("characters").Item(0).GetChildNodes();
			for (int i = 0; i < characters.GetLength(); i++)
			{
				INode child = characters.Item(i);
				if (child.GetNodeName().Equals("character"))
				{
					string name = child.GetAttributes().GetNamedItem("name").GetNodeValue();
					char[] cName = name.ToCharArray();
					cName[0] = char.ToUpperCase(cName[0]);
					name = new string(cName);
					IList<string> aliases = Arrays.AsList(child.GetAttributes().GetNamedItem("aliases").GetNodeValue().Split(";"));
					string gender = (child.GetAttributes().GetNamedItem("gender") == null) ? string.Empty : child.GetAttributes().GetNamedItem("gender").GetNodeValue();
					personList.Add(new Person(child.GetAttributes().GetNamedItem("name").GetNodeValue(), gender, aliases));
				}
			}
			return personList;
		}

		//write the character list to a file to work with the annotator
		/// <exception cref="System.IO.IOException"/>
		public static void WriteCharacterList(string fileName, IList<Person> personList)
		{
			StringBuilder text = new StringBuilder();
			foreach (Person p in personList)
			{
				string gender = string.Empty;
				switch (p.gender)
				{
					case Person.Gender.Male:
					{
						gender = "M";
						break;
					}

					case Person.Gender.Female:
					{
						gender = "F";
						break;
					}

					case Person.Gender.Unk:
					{
						gender = string.Empty;
						break;
					}
				}
				text.Append(p.name + ";" + gender);
				foreach (string alias in p.aliases)
				{
					text.Append(";" + alias);
				}
				text.Append("\n");
			}
			PrintWriter pw = IOUtils.GetPrintWriter(fileName);
			pw.Print(text);
			pw.Close();
		}

		protected internal class Mention
		{
			internal string text;

			internal int begin;

			internal int end;

			public Mention(string text, int begin, int end)
			{
				this.text = text;
				this.begin = begin;
				this.end = end;
			}
		}

		/// <exception cref="System.Exception"/>
		public static XMLToAnnotation.Data ReadXMLFormat(string fileName)
		{
			//Extract character list, gold quote speaker and mention information from the XML document.
			IDocument doc = XMLUtils.ReadDocumentFromFile(fileName);
			INode text = doc.GetDocumentElement().GetElementsByTagName("text").Item(0);
			string docText = GetJustText(text);
			Annotation document = GetAnnotatedFile(docText, fileName, GetProcessedCoreNLPProperties());
			IList<ICoreMap> quotes = document.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			IList<CoreLabel> tokens = document.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<XMLToAnnotation.GoldQuoteInfo> goldList = new List<XMLToAnnotation.GoldQuoteInfo>();
			IDictionary<int, XMLToAnnotation.Mention> idToMention = new Dictionary<int, XMLToAnnotation.Mention>();
			IList<Person> personList = ReadXMLCharacterList(doc);
			IDictionary<string, IList<Person>> personMap = QuoteAttributionUtils.ReadPersonMap(personList);
			IList<Pair<int, string>> mentionIdToSpeakerList = new List<Pair<int, string>>();
			//there is at least 1 case in which the XML quote does not match up with the automatically-extracted quote. (Ex: quote by Mr. Collins that begins, "Hunsford, near Westerham, Kent, ...")
			//as the dirty solution, we treat all quotes encapsulated within an XML quote as the same speaker (although this is not 100% accurate!)
			int quoteIndex = 0;
			INodeList textElems = text.GetChildNodes();
			int tokenIndex = 0;
			for (int i = 0; i < textElems.GetLength(); i++)
			{
				INode chapterNode = textElems.Item(i);
				if (chapterNode.GetNodeName().Equals("chapter"))
				{
					INodeList chapElems = chapterNode.GetChildNodes();
					for (int j = 0; j < chapElems.GetLength(); j++)
					{
						INode child = chapElems.Item(j);
						if (child.GetNodeName().Equals("quote"))
						{
							//search for nested mentions
							INodeList quoteChildren = child.GetChildNodes();
							for (int k = 0; k < quoteChildren.GetLength(); k++)
							{
								INode quoteChild = quoteChildren.Item(k);
								if (quoteChild.GetNodeName().Equals("mention"))
								{
									string mentionText = quoteChild.GetTextContent();
									int id = System.Convert.ToInt32(Sharpen.Runtime.Substring(quoteChild.GetAttributes().GetNamedItem("id").GetTextContent(), 1));
									IList<int> connections = ReadConnection(quoteChild.GetAttributes().GetNamedItem("connection").GetNodeValue());
									int endIndex = GetEndIndex(tokenIndex, tokens, mentionText);
									//                mentions.put(id, new XMLMention(quoteChild.getTextContent(), tokenIndex, endIndex, id, connections));
									idToMention[id] = new XMLToAnnotation.Mention(mentionText, tokenIndex, endIndex);
									tokenIndex = endIndex + 1;
								}
								else
								{
									string quoteText = quoteChild.GetTextContent();
									quoteText = quoteText.ReplaceAll("\n(?!\n)", " ");
									//trim unnecessarily newlines
									quoteText = quoteText.ReplaceAll("_", string.Empty);
									tokenIndex = GetEndIndex(tokenIndex, tokens, quoteText) + 1;
								}
							}
							string quoteText_1 = child.GetTextContent();
							//              tokenIndex = getEndIndex(tokenIndex, tokens, quoteText) + 1;
							quoteText_1 = quoteText_1.ReplaceAll("\n(?!\n)", " ");
							//trim unnecessarily newlines
							quoteText_1 = quoteText_1.ReplaceAll("_", string.Empty);
							int quotationOffset = 1;
							if (quoteText_1.StartsWith("``"))
							{
								quotationOffset = 2;
							}
							IList<int> connections_1 = ReadConnection(child.GetAttributes().GetNamedItem("connection").GetTextContent());
							int id_1 = System.Convert.ToInt32(Sharpen.Runtime.Substring(child.GetAttributes().GetNamedItem("id").GetTextContent(), 1));
							int mention_id = null;
							if (connections_1.Count > 0)
							{
								mention_id = connections_1[0];
							}
							else
							{
								System.Console.Out.WriteLine("quote w/ no mention. ID: " + id_1);
							}
							//            Pair<Integer, Integer> mentionPair = idToMentionPair.get(mention_id);
							mentionIdToSpeakerList.Add(new Pair<int, string>(mention_id, child.GetAttributes().GetNamedItem("speaker").GetTextContent()));
							string annotatedQuoteText = quotes[quoteIndex].Get(typeof(CoreAnnotations.TextAnnotation));
							while (!quoteText_1.EndsWith(annotatedQuoteText))
							{
								quoteIndex++;
								annotatedQuoteText = quotes[quoteIndex].Get(typeof(CoreAnnotations.TextAnnotation));
								mentionIdToSpeakerList.Add(new Pair<int, string>(mention_id, child.GetAttributes().GetNamedItem("speaker").GetTextContent()));
							}
							//            idToMentionPair.put(id, new Pair<>(-1, -1));
							//            imention_id = connections.get(0);
							//              quotes.add(new XMLQuote(quoteText.substring(quotationOffset, quoteText.length() - quotationOffset), child.getAttributes().getNamedItem("speaker").getTextContent(), id, chapterIndex, mention_id));
							quoteIndex++;
						}
						else
						{
							if (child.GetNodeName().Equals("mention"))
							{
								string mentionText = child.GetTextContent();
								int id = System.Convert.ToInt32(Sharpen.Runtime.Substring(child.GetAttributes().GetNamedItem("id").GetTextContent(), 1));
								IList<int> connections = ReadConnection(child.GetAttributes().GetNamedItem("connection").GetNodeValue());
								int endIndex = GetEndIndex(tokenIndex, tokens, mentionText);
								idToMention[id] = new XMLToAnnotation.Mention(mentionText, tokenIndex, endIndex);
								//              mentions.put(id, new XMLMention(child.getTextContent(), tokenIndex, endIndex, id, connections));
								tokenIndex = endIndex + 1;
							}
							else
							{
								//#text
								string nodeText = child.GetTextContent();
								nodeText = nodeText.ReplaceAll("\n(?!\n)", " ");
								nodeText = nodeText.ReplaceAll("_", string.Empty);
								if (tokenIndex >= tokens.Count)
								{
									continue;
								}
								tokenIndex = GetEndIndex(tokenIndex, tokens, nodeText) + 1;
							}
						}
					}
				}
			}
			foreach (Pair<int, string> item in mentionIdToSpeakerList)
			{
				XMLToAnnotation.Mention mention = idToMention[item.first];
				if (mention == null)
				{
					goldList.Add(new XMLToAnnotation.GoldQuoteInfo(-1, -1, item.second, null));
				}
				else
				{
					goldList.Add(new XMLToAnnotation.GoldQuoteInfo(mention.begin, mention.end, item.second, mention.text));
				}
			}
			//verify
			if (document.Get(typeof(CoreAnnotations.QuotationsAnnotation)).Count != goldList.Count)
			{
				throw new Exception("Quotes size and gold size don't match!");
			}
			return new XMLToAnnotation.Data(goldList, personList, document);
		}
	}
}
