using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.IE.Regexp
{
	/// <summary>
	/// A sequence classifier that labels tokens with types based on a simple manual mapping from
	/// regular expressions to the types of the entities they are meant to describe.
	/// </summary>
	/// <remarks>
	/// A sequence classifier that labels tokens with types based on a simple manual mapping from
	/// regular expressions to the types of the entities they are meant to describe.
	/// The user provides a file formatted as follows:
	/// <pre>
	/// regex1    TYPE    overwritableType1,Type2...    priority
	/// regex2    TYPE    overwritableType1,Type2...    priority
	/// ...
	/// </pre>
	/// where each argument is tab-separated, and the last two arguments are optional. Several regexes can be
	/// associated with a single type. In the case where multiple regexes match a phrase, the priority ranking
	/// is used to choose between the possible types. This classifier is designed to be used as part of a full
	/// NER system to label entities that don't fall into the usual NER categories. It only records the label
	/// if the token has not already been NER-annotated, or it has been annotated but the NER-type has been
	/// designated overwritable (the third argument).  Note that this is evaluated token-wise in this classifier,
	/// and so it may assign a label against a token sequence that is partly background and partly overwritable.
	/// (In contrast, RegexNERAnnotator doesn't allow this.)
	/// It assigns labels to AnswerAnnotation, while checking for existing labels in NamedEntityTagAnnotation.
	/// The first column regex may be a sequence of regex, each separated by whitespace (matching "\\s+").
	/// The regex will match if the successive regex match a sequence of tokens in the input.
	/// Spaces can only be used to separate regular expression tokens; within tokens \\s or similar non-space
	/// representations need to be used instead.
	/// Notes: Following Java regex conventions, some characters in the file need to be escaped. Only a single
	/// backslash should be used though, as these are not String literals. The input to RegexNER will have
	/// already been tokenized.  So, for example, with our usual English tokenization, things like genitives
	/// and commas at the end of words will be separated in the input and matched as a separate token.
	/// This class isn't implemented very efficiently, since every regex is evaluated at every token position.
	/// So it can and does get quite slow if you have a lot of patterns in your NER rules.
	/// <c>TokensRegex</c>
	/// is a more general framework to provide the functionality of this class.
	/// But at present we still use this class.
	/// </remarks>
	/// <author>jtibs</author>
	/// <author>Mihai</author>
	public class RegexNERSequenceClassifier : AbstractSequenceClassifier<CoreLabel>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Regexp.RegexNERSequenceClassifier));

		private readonly IList<RegexNERSequenceClassifier.Entry> entries;

		private readonly ICollection<string> myLabels;

		private readonly bool ignoreCase;

		private readonly Pattern validPosPattern;

		public const string DefaultValidPos = "^(NN|JJ)";

		public RegexNERSequenceClassifier(string mapping, bool ignoreCase, bool overwriteMyLabels)
			: this(mapping, ignoreCase, overwriteMyLabels, DefaultValidPos)
		{
		}

		/// <summary>Make a new instance of this classifier.</summary>
		/// <remarks>
		/// Make a new instance of this classifier. The ignoreCase option allows case-insensitive
		/// regular expression matching, allowing the idea that the provided file might just
		/// be a manual list of the possible entities for each type.
		/// </remarks>
		/// <param name="mapping">A String describing a file/classpath/URI for the RegexNER patterns</param>
		/// <param name="ignoreCase">The regex in the mapping file should be compiled ignoring case</param>
		/// <param name="overwriteMyLabels">
		/// If true, this classifier overwrites NE labels generated through
		/// this regex NER. This is necessary because sometimes the
		/// RegexNERSequenceClassifier is run successively over the same
		/// text (e.g., to overwrite some older annotations).
		/// </param>
		/// <param name="validPosRegex">
		/// May be null or an empty String, in which case any (or no) POS is valid
		/// in matching. Otherwise, this is a regex which is matched with find()
		/// [not matches()] and which must be matched by the POS of at least one
		/// word in the sequence for it to be labeled via any matching rules.
		/// (Note that this is a postfilter; using this will not speed up matching.)
		/// </param>
		public RegexNERSequenceClassifier(string mapping, bool ignoreCase, bool overwriteMyLabels, string validPosRegex)
			: base(new Properties())
		{
			// Make this a property?  (But already done as a property at CoreNLP level.)
			// ms: but really this should be rewritten from scratch
			//     we should have a language to specify regexes over *tokens*, where each token could be a regular Java regex (over words, POSs, etc.)
			if (validPosRegex != null && !validPosRegex.Equals(string.Empty))
			{
				validPosPattern = Pattern.Compile(validPosRegex);
			}
			else
			{
				validPosPattern = null;
			}
			try
			{
				using (BufferedReader rd = IOUtils.ReaderFromString(mapping))
				{
					entries = ReadEntries(rd, ignoreCase);
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException("Couldn't read RegexNER from " + mapping, e);
			}
			this.ignoreCase = ignoreCase;
			myLabels = Generics.NewHashSet();
			// Can always override background or none.
			myLabels.Add(flags.backgroundSymbol);
			myLabels.Add(null);
			if (overwriteMyLabels)
			{
				foreach (RegexNERSequenceClassifier.Entry entry in entries)
				{
					myLabels.Add(entry.type);
				}
			}
		}

		/// <summary>Make a new instance of this classifier.</summary>
		/// <remarks>
		/// Make a new instance of this classifier. The ignoreCase option allows case-insensitive
		/// regular expression matching, allowing the idea that the provided file might just
		/// be a manual list of the possible entities for each type.
		/// </remarks>
		/// <param name="reader">A Reader for the RegexNER patterns</param>
		/// <param name="ignoreCase">The regex in the mapping file should be compiled ignoring case</param>
		/// <param name="overwriteMyLabels">
		/// If true, this classifier overwrites NE labels generated through
		/// this regex NER. This is necessary because sometimes the
		/// RegexNERSequenceClassifier is run successively over the same
		/// text (e.g., to overwrite some older annotations).
		/// </param>
		/// <param name="validPosRegex">
		/// May be null or an empty String, in which case any (or no) POS is valid
		/// in matching. Otherwise, this is a regex, and only words with a POS that
		/// match the regex will be labeled via any matching rules.
		/// </param>
		public RegexNERSequenceClassifier(BufferedReader reader, bool ignoreCase, bool overwriteMyLabels, string validPosRegex)
			: base(new Properties())
		{
			// log.info("RegexNER using labels: " +  myLabels);
			if (validPosRegex != null && !validPosRegex.Equals(string.Empty))
			{
				validPosPattern = Pattern.Compile(validPosRegex);
			}
			else
			{
				validPosPattern = null;
			}
			try
			{
				entries = ReadEntries(reader, ignoreCase);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException("Couldn't read RegexNER from reader", e);
			}
			this.ignoreCase = ignoreCase;
			myLabels = Generics.NewHashSet();
			// Can always override background or none.
			myLabels.Add(flags.backgroundSymbol);
			myLabels.Add(null);
			if (overwriteMyLabels)
			{
				foreach (RegexNERSequenceClassifier.Entry entry in entries)
				{
					myLabels.Add(entry.type);
				}
			}
		}

		private class Entry : IComparable<RegexNERSequenceClassifier.Entry>
		{
			public IList<Pattern> regex;

			public IList<string> exact = new List<string>();

			public string type;

			public ICollection<string> overwritableTypes;

			public double priority;

			public Entry(IList<Pattern> regex, string type, ICollection<string> overwritableTypes, double priority)
			{
				// log.info("RegexNER using labels: " + myLabels);
				// the regex, tokenized by splitting on white space
				// the associated type
				this.regex = regex;
				this.type = string.Intern(type);
				this.overwritableTypes = overwritableTypes;
				this.priority = priority;
				// Efficiency shortcut
				foreach (Pattern p in regex)
				{
					if (p.ToString().Matches("[a-zA-Z0-9]+"))
					{
						exact.Add(p.ToString());
					}
					else
					{
						exact.Add(null);
					}
				}
			}

			/// <summary>
			/// If the given priorities are equal, an entry whose regex has more tokens is assigned
			/// a higher priority.
			/// </summary>
			/// <remarks>
			/// If the given priorities are equal, an entry whose regex has more tokens is assigned
			/// a higher priority. This implementation is not fine-grained enough to be consistent with equals.
			/// </remarks>
			public virtual int CompareTo(RegexNERSequenceClassifier.Entry other)
			{
				if (this.priority > other.priority)
				{
					return -1;
				}
				if (this.priority < other.priority)
				{
					return 1;
				}
				return other.regex.Count - this.regex.Count;
			}

			public override string ToString()
			{
				return "Entry{" + regex + ' ' + type + ' ' + overwritableTypes + ' ' + priority + '}';
			}
		}

		private bool ContainsValidPos(IList<CoreLabel> tokens, int start, int end)
		{
			if (validPosPattern == null)
			{
				return true;
			}
			// log.info("CHECKING " + start + " " + end);
			for (int i = start; i < end; i++)
			{
				// log.info("TAG = " + tokens.get(i).tag());
				if (tokens[i].Tag() == null)
				{
					throw new ArgumentException("RegexNER was asked to check for valid tags on an untagged sequence. Either tag the sequence, perhaps with the pos annotator, or create RegexNER with an empty validPosPattern, perhaps with the property regexner.validpospattern"
						);
				}
				Matcher m = validPosPattern.Matcher(tokens[i].Tag());
				if (m.Find())
				{
					return true;
				}
			}
			return false;
		}

		public override IList<CoreLabel> Classify(IList<CoreLabel> document)
		{
			// This is pretty deathly slow. It loops over each entry, and then loops over each document token for it.
			// We could gain by compiling into disjunctions patterns for the same class with the same priorities and restrictions?
			foreach (RegexNERSequenceClassifier.Entry entry in entries)
			{
				int start = 0;
				// the index of the token from which we begin our search each iteration
				while (true)
				{
					// only search the part of the document that we haven't yet considered
					// log.info("REGEX FIND MATCH FOR " + entry.regex.toString());
					start = FindStartIndex(entry, document, start, myLabels, this.ignoreCase);
					if (start < 0)
					{
						break;
					}
					// no match found
					// make sure we annotate only valid POS tags
					if (ContainsValidPos(document, start, start + entry.regex.Count))
					{
						// annotate each matching token
						for (int i = start; i < start + entry.regex.Count; i++)
						{
							CoreLabel token = document[i];
							token.Set(typeof(CoreAnnotations.AnswerAnnotation), entry.type);
						}
					}
					start++;
				}
			}
			return document;
		}

		/// <summary>
		/// Creates a combined list of Entries using the provided mapping file, and sorts them by
		/// first by priority, then the number of tokens in the regex.
		/// </summary>
		/// <param name="mapping">The Reader containing RegexNER mappings. It's lines are counted from 1</param>
		/// <returns>a sorted list of Entries</returns>
		/// <exception cref="System.IO.IOException"/>
		private static IList<RegexNERSequenceClassifier.Entry> ReadEntries(BufferedReader mapping, bool ignoreCase)
		{
			IList<RegexNERSequenceClassifier.Entry> entries = new List<RegexNERSequenceClassifier.Entry>();
			int lineCount = 0;
			for (string line; (line = mapping.ReadLine()) != null; )
			{
				lineCount++;
				string[] split = line.Split("\t");
				if (split.Length < 2 || split.Length > 4)
				{
					throw new ArgumentException("Provided mapping file is in wrong format: " + line);
				}
				string[] regexes = split[0].Trim().Split("\\s+");
				string type = split[1].Trim();
				ICollection<string> overwritableTypes = Generics.NewHashSet();
				double priority = 0.0;
				IList<Pattern> tokens = new List<Pattern>();
				if (split.Length >= 3)
				{
					Sharpen.Collections.AddAll(overwritableTypes, Arrays.AsList(split[2].Trim().Split(",")));
				}
				if (split.Length == 4)
				{
					try
					{
						priority = double.Parse(split[3].Trim());
					}
					catch (NumberFormatException e)
					{
						throw new ArgumentException("ERROR: Invalid line " + lineCount + " in regexner file " + mapping + ": \"" + line + "\"!", e);
					}
				}
				try
				{
					foreach (string str in regexes)
					{
						if (ignoreCase)
						{
							tokens.Add(Pattern.Compile(str, Pattern.CaseInsensitive | Pattern.UnicodeCase));
						}
						else
						{
							tokens.Add(Pattern.Compile(str));
						}
					}
				}
				catch (PatternSyntaxException e)
				{
					throw new ArgumentException("ERROR: Invalid line " + lineCount + " in regexner file " + mapping + ": \"" + line + "\"!", e);
				}
				entries.Add(new RegexNERSequenceClassifier.Entry(tokens, type, overwritableTypes, priority));
			}
			entries.Sort();
			// log.info("Read these entries:");
			// log.info(entries);
			return entries;
		}

		/// <summary>
		/// Checks if the entry's regex sequence is contained in the tokenized document, starting the search
		/// from index searchStart.
		/// </summary>
		/// <remarks>
		/// Checks if the entry's regex sequence is contained in the tokenized document, starting the search
		/// from index searchStart. Also requires that each token's current NER-type be overwritable,
		/// and that each token has not yet been Answer-annotated.
		/// </remarks>
		/// <param name="entry"/>
		/// <param name="document"/>
		/// <returns>on success, the index of the first token in the matching sequence, otherwise -1</returns>
		private static int FindStartIndex(RegexNERSequenceClassifier.Entry entry, IList<CoreLabel> document, int searchStart, ICollection<string> myLabels, bool ignoreCase)
		{
			IList<Pattern> regex = entry.regex;
			int rSize = regex.Count;
			// log.info("REGEX FIND MATCH FOR " + regex.toString() + " length: " + rSize);
			for (int start = searchStart; start <= end; start++)
			{
				bool failed = false;
				for (int i = 0; i < rSize; i++)
				{
					Pattern pattern = regex[i];
					string exact = entry.exact[i];
					CoreLabel token = document[start + i];
					string NERType = token.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
					string currentType = token.Get(typeof(CoreAnnotations.AnswerAnnotation));
					if (currentType != null || (exact != null && !(ignoreCase ? Sharpen.Runtime.EqualsIgnoreCase(exact, token.Word()) : exact.Equals(token.Word()))) || !(entry.overwritableTypes.Contains(NERType) || myLabels.Contains(NERType)) || !pattern.Matcher
						(token.Word()).Matches())
					{
						// last, as this is likely the expensive operation
						failed = true;
						break;
					}
				}
				if (!failed)
				{
					// log.info("MATCHED REGEX:");
					// for(int i = start; i < start + regex.size(); i ++) log.info(" " + document.get(i).word());
					// log.info();
					return start;
				}
			}
			return -1;
		}

		public override IList<CoreLabel> ClassifyWithGlobalInformation(IList<CoreLabel> tokenSeq, ICoreMap doc, ICoreMap sent)
		{
			return Classify(tokenSeq);
		}

		// these methods are not implemented for a rule-based sequence classifier
		public override void Train(ICollection<IList<CoreLabel>> docs, IDocumentReaderAndWriter<CoreLabel> readerAndWriter)
		{
		}

		public override void SerializeClassifier(string serializePath)
		{
		}

		public override void SerializeClassifier(ObjectOutputStream oos)
		{
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public override void LoadClassifier(ObjectInputStream @in, Properties props)
		{
		}
	}
}
