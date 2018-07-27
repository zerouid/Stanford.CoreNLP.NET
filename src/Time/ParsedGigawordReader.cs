using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




using NU.Xom;


namespace Edu.Stanford.Nlp.Time
{
	/// <author>Karthik Raghunathan</author>
	public class ParsedGigawordReader : IEnumerable<Annotation>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Time.ParsedGigawordReader));

		private IEnumerable<File> files;

		public ParsedGigawordReader(File directory)
		{
			this.files = IOUtils.IterFilesRecursive(directory);
		}

		public virtual IEnumerator<Annotation> GetEnumerator()
		{
			return new _IEnumerator_46(this);
		}

		private sealed class _IEnumerator_46 : IEnumerator<Annotation>
		{
			public _IEnumerator_46()
			{
				this.readers = Iterables.Transform(this._enclosing.files, null).GetEnumerator();
				this.reader = this.FindReader();
				this.annotation = this.FindAnnotation();
			}

			private IEnumerator<BufferedReader> readers;

			private BufferedReader reader;

			private Annotation annotation;

			public bool MoveNext()
			{
				return this.annotation != null;
			}

			public Annotation Current
			{
				get
				{
					if (this.annotation == null)
					{
						throw new NoSuchElementException();
					}
					Annotation toReturn = this.annotation;
					this.annotation = this.FindAnnotation();
					return toReturn;
				}
			}

			public void Remove()
			{
				throw new NotSupportedException();
			}

			private BufferedReader FindReader()
			{
				return this.readers.MoveNext() ? this.readers.Current : null;
			}

			private Annotation FindAnnotation()
			{
				if (this.reader == null)
				{
					return null;
				}
				try
				{
					string line;
					StringBuilder doc = new StringBuilder();
					while ((line = this.reader.ReadLine()) != null)
					{
						doc.Append(line);
						doc.Append('\n');
						//            if(line.contains("<DOC id")){
						//              log.info(line);
						//            }
						if (line.Equals("</DOC>"))
						{
							break;
						}
						if (line.Contains("</DOC>"))
						{
							throw new Exception(string.Format("invalid line '%s'", line));
						}
					}
					if (line == null)
					{
						this.reader.Close();
						this.reader = this.FindReader();
					}
					string xml = doc.ToString().ReplaceAll("&", "&amp;");
					if (xml == null || xml.Equals(string.Empty))
					{
						return this.FindAnnotation();
					}
					xml = xml.ReplaceAll("num=([0-9]+) (.*)", "num=\"$1\" $2");
					xml = xml.ReplaceAll("sid=(.*)>", "sid=\"$1\">");
					xml = xml.ReplaceAll("</SENT>\n</DOC>", "</SENT>\n</TEXT>\n</DOC>");
					xml = Sharpen.Runtime.GetStringForBytes(Sharpen.Runtime.GetBytesForString(xml), "UTF8");
					//log.info("This is what goes in:\n" + xml);
					return Edu.Stanford.Nlp.Time.ParsedGigawordReader.ToAnnotation(xml);
				}
				catch (IOException e)
				{
					throw new RuntimeIOException(e);
				}
			}
		}

		private static readonly Pattern datePattern = Pattern.Compile("^\\w+_\\w+_(\\d+)\\.");

		/*
		* Old implementation based on JDOM.
		* No longer maintained due to JDOM licensing issues.
		private static Annotation toAnnotation(String xml) throws IOException {
		Element docElem;
		try {
		docElem = new SAXBuilder().build(new StringReader(xml)).getRootElement();
		} catch (JDOMException e) {
		throw new RuntimeException(String.format("error:\n%s\ninput:\n%s", e, xml));
		}
		Element textElem = docElem.getChild("TEXT");
		StringBuilder text = new StringBuilder();
		int offset = 0;
		List<CoreMap> sentences = new ArrayList<CoreMap>();
		for (Object sentObj: textElem.getChildren("SENT")) {
		CoreMap sentence = new ArrayCoreMap();
		sentence.set(CoreAnnotations.CharacterOffsetBeginAnnotation.class, offset);
		Element sentElem = (Element)sentObj;
		Tree tree = Tree.valueOf(sentElem.getText());
		List<CoreLabel> tokens = new ArrayList<CoreLabel>();
		List<Tree> preTerminals = preTerminals(tree);
		for (Tree preTerminal: preTerminals) {
		String posTag = preTerminal.value();
		for (Tree wordTree: preTerminal.children()) {
		String word = wordTree.value();
		CoreLabel token = new CoreLabel();
		token.set(CoreAnnotations.TextAnnotation.class, word);
		token.set(CoreAnnotations.TextAnnotation.class, word);
		token.set(CoreAnnotations.PartOfSpeechAnnotation.class, posTag);
		token.set(CoreAnnotations.CharacterOffsetBeginAnnotation.class, offset);
		offset += word.length();
		token.set(CoreAnnotations.CharacterOffsetEndAnnotation.class, offset);
		text.append(word);
		text.append(' ');
		offset += 1;
		tokens.add(token);
		}
		}
		if (preTerminals.size() > 0) {
		text.setCharAt(text.length() - 1, '\n');
		}
		sentence.set(CoreAnnotations.CharacterOffsetEndAnnotation.class, offset - 1);
		sentence.set(CoreAnnotations.TokensAnnotation.class, tokens);
		sentence.set(TreeCoreAnnotations.TreeAnnotation.class, tree);
		sentences.add(sentence);
		}
		
		String docID = docElem.getAttributeValue("id");
		Matcher matcher = datePattern.matcher(docID);
		matcher.find();
		Calendar docDate = new Timex(matcher.group(1)).getDate();
		
		Annotation document = new Annotation(text.toString());
		document.set(CoreAnnotations.DocIDAnnotation.class, docID);
		document.set(CoreAnnotations.CalendarAnnotation.class, docDate);
		document.set(CoreAnnotations.SentencesAnnotation.class, sentences);
		return document;
		}
		*/
		/// <exception cref="System.IO.IOException"/>
		private static Annotation ToAnnotation(string xml)
		{
			Element docElem;
			try
			{
				Builder parser = new Builder();
				StringReader @in = new StringReader(xml);
				docElem = parser.Build(@in).GetRootElement();
			}
			catch (Exception e)
			{
				throw new Exception(string.Format("error:\n%s\ninput:\n%s", e, xml));
			}
			Element textElem = docElem.GetFirstChildElement("TEXT");
			StringBuilder text = new StringBuilder();
			int offset = 0;
			IList<ICoreMap> sentences = new List<ICoreMap>();
			Elements sentenceElements = textElem.GetChildElements("SENT");
			for (int crtsent = 0; crtsent < sentenceElements.Size(); crtsent++)
			{
				Element sentElem = sentenceElements.Get(crtsent);
				ICoreMap sentence = new ArrayCoreMap();
				sentence.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), offset);
				Tree tree = Tree.ValueOf(sentElem.GetChild(0).GetValue());
				// XXX ms: is this the same as sentElem.getText() in JDOM?
				IList<CoreLabel> tokens = new List<CoreLabel>();
				IList<Tree> preTerminals = PreTerminals(tree);
				foreach (Tree preTerminal in preTerminals)
				{
					string posTag = preTerminal.Value();
					foreach (Tree wordTree in preTerminal.Children())
					{
						string word = wordTree.Value();
						CoreLabel token = new CoreLabel();
						token.Set(typeof(CoreAnnotations.TextAnnotation), word);
						token.Set(typeof(CoreAnnotations.TextAnnotation), word);
						token.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), posTag);
						token.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), offset);
						offset += word.Length;
						token.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), offset);
						text.Append(word);
						text.Append(' ');
						offset += 1;
						tokens.Add(token);
					}
				}
				if (preTerminals.Count > 0)
				{
					Sharpen.Runtime.SetCharAt(text, text.Length - 1, '\n');
				}
				sentence.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), offset - 1);
				sentence.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
				sentence.Set(typeof(TreeCoreAnnotations.TreeAnnotation), tree);
				sentences.Add(sentence);
			}
			string docID = docElem.GetAttributeValue("id");
			Matcher matcher = datePattern.Matcher(docID);
			matcher.Find();
			Calendar docDate = new Timex("DATE", matcher.Group(1)).GetDate();
			Annotation document = new Annotation(text.ToString());
			document.Set(typeof(CoreAnnotations.DocIDAnnotation), docID);
			document.Set(typeof(CoreAnnotations.CalendarAnnotation), docDate);
			document.Set(typeof(CoreAnnotations.SentencesAnnotation), sentences);
			return document;
		}

		// todo [cdm 2013]: replace the methods below with ones in Tree?
		// It depends on whether the code is somehow using preterminals with multiple children.
		private static IList<Tree> PreTerminals(Tree tree)
		{
			IList<Tree> preTerminals = new List<Tree>();
			foreach (Tree descendant in tree)
			{
				if (IsPreterminal(descendant))
				{
					preTerminals.Add(descendant);
				}
			}
			return preTerminals;
		}

		private static bool IsPreterminal(Tree tree)
		{
			if (tree.IsLeaf())
			{
				return false;
			}
			foreach (Tree child in tree.Children())
			{
				if (!child.IsLeaf())
				{
					return false;
				}
			}
			return true;
		}
	}
}
