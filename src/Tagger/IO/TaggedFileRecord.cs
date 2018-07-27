using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Tagger.Maxent;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Tagger.IO
{
	/// <summary>Parses and specifies all the details for how to read some POS tagging data.</summary>
	/// <remarks>
	/// Parses and specifies all the details for how to read some POS tagging data.
	/// The options for this class are documented in MaxentTagger, unlder the trainFile property.
	/// </remarks>
	/// <author>John Bauer</author>
	public class TaggedFileRecord
	{
		public enum Format
		{
			Text,
			Tsv,
			Trees
		}

		internal readonly string file;

		internal readonly TaggedFileRecord.Format format;

		internal readonly string encoding;

		internal readonly string tagSeparator;

		internal readonly ITreeTransformer treeTransformer;

		internal readonly TreeNormalizer treeNormalizer;

		internal readonly NumberRangesFileFilter treeRange;

		internal readonly IPredicate<Tree> treeFilter;

		internal readonly int wordColumn;

		internal readonly int tagColumn;

		internal readonly ITreeReaderFactory trf;

		private TaggedFileRecord(string file, TaggedFileRecord.Format format, string encoding, string tagSeparator, ITreeTransformer treeTransformer, TreeNormalizer treeNormalizer, ITreeReaderFactory trf, NumberRangesFileFilter treeRange, IPredicate
			<Tree> treeFilter, int wordColumn, int tagColumn)
		{
			// represents a tokenized file separated by text
			// represents a tsv file such as a conll file
			// represents a file in PTB format
			this.file = file;
			this.format = format;
			this.encoding = encoding;
			this.tagSeparator = tagSeparator;
			this.treeTransformer = treeTransformer;
			this.treeNormalizer = treeNormalizer;
			this.treeRange = treeRange;
			this.treeFilter = treeFilter;
			this.wordColumn = wordColumn;
			this.tagColumn = tagColumn;
			this.trf = trf;
		}

		public const string Format = "format";

		public const string Encoding = "encoding";

		public const string TagSeparator = "tagSeparator";

		public const string TreeTransformer = "treeTransformer";

		public const string TreeNormalizer = "treeNormalizer";

		public const string TreeRange = "treeRange";

		public const string TreeFilter = "treeFilter";

		public const string WordColumn = "wordColumn";

		public const string TagColumn = "tagColumn";

		public const string TreeReader = "trf";

		public override string ToString()
		{
			StringBuilder s = new StringBuilder();
			s.Append(Format + "=" + format);
			s.Append("," + Encoding + "=" + encoding);
			s.Append("," + TagSeparator + "=" + tagSeparator);
			if (treeTransformer != null)
			{
				s.Append("," + TreeTransformer + "=" + treeTransformer.GetType().FullName);
			}
			if (trf != null)
			{
				s.Append("," + TreeReader + "=" + trf.GetType().FullName);
			}
			if (treeNormalizer != null)
			{
				s.Append("," + TreeNormalizer + "=" + treeNormalizer.GetType().FullName);
			}
			if (treeRange != null)
			{
				s.Append("," + TreeRange + "=" + treeRange.ToString().ReplaceAll(",", ":"));
			}
			if (treeRange != null)
			{
				s.Append("," + TreeFilter + "=" + treeFilter.GetType().ToString());
			}
			if (wordColumn != null)
			{
				s.Append("," + WordColumn + "=" + wordColumn);
			}
			if (tagColumn != null)
			{
				s.Append("," + TagColumn + "=" + tagColumn);
			}
			return s.ToString();
		}

		public virtual string Filename()
		{
			return file;
		}

		public virtual ITaggedFileReader Reader()
		{
			switch (format)
			{
				case TaggedFileRecord.Format.Text:
				{
					return new TextTaggedFileReader(this);
				}

				case TaggedFileRecord.Format.Trees:
				{
					return new TreeTaggedFileReader(this);
				}

				case TaggedFileRecord.Format.Tsv:
				{
					return new TSVTaggedFileReader(this);
				}

				default:
				{
					throw new ArgumentException("Unknown format " + format);
				}
			}
		}

		public static IList<Edu.Stanford.Nlp.Tagger.IO.TaggedFileRecord> CreateRecords(Properties config, string description)
		{
			string[] pieces = description.Split(";");
			IList<Edu.Stanford.Nlp.Tagger.IO.TaggedFileRecord> records = new List<Edu.Stanford.Nlp.Tagger.IO.TaggedFileRecord>();
			foreach (string piece in pieces)
			{
				records.Add(CreateRecord(config, piece));
			}
			return records;
		}

		public static Edu.Stanford.Nlp.Tagger.IO.TaggedFileRecord CreateRecord(Properties config, string description)
		{
			string[] pieces = description.Split(",");
			if (pieces.Length == 1)
			{
				return new Edu.Stanford.Nlp.Tagger.IO.TaggedFileRecord(description, TaggedFileRecord.Format.Text, GetEncoding(config), GetTagSeparator(config), null, null, null, null, null, null, null);
			}
			string[] args = new string[pieces.Length - 1];
			System.Array.Copy(pieces, 0, args, 0, pieces.Length - 1);
			string file = pieces[pieces.Length - 1];
			TaggedFileRecord.Format format = TaggedFileRecord.Format.Text;
			string encoding = GetEncoding(config);
			string tagSeparator = GetTagSeparator(config);
			ITreeTransformer treeTransformer = null;
			TreeNormalizer treeNormalizer = null;
			ITreeReaderFactory trf = null;
			NumberRangesFileFilter treeRange = null;
			IPredicate<Tree> treeFilter = null;
			int wordColumn = null;
			int tagColumn = null;
			foreach (string arg in args)
			{
				string[] argPieces = arg.Split("=", 2);
				if (argPieces.Length != 2)
				{
					throw new ArgumentException("TaggedFileRecord argument " + arg + " has an unexpected number of =s");
				}
				if (Sharpen.Runtime.EqualsIgnoreCase(argPieces[0], Format))
				{
					format = TaggedFileRecord.Format.ValueOf(argPieces[1]);
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(argPieces[0], Encoding))
					{
						encoding = argPieces[1];
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(argPieces[0], TagSeparator))
						{
							tagSeparator = argPieces[1];
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(argPieces[0], TreeTransformer))
							{
								treeTransformer = ReflectionLoading.LoadByReflection(argPieces[1]);
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(argPieces[0], TreeNormalizer))
								{
									treeNormalizer = ReflectionLoading.LoadByReflection(argPieces[1]);
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(argPieces[0], TreeReader))
									{
										trf = ReflectionLoading.LoadByReflection(argPieces[1]);
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(argPieces[0], TreeRange))
										{
											string range = argPieces[1].ReplaceAll(":", ",");
											treeRange = new NumberRangesFileFilter(range, true);
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(argPieces[0], TreeFilter))
											{
												treeFilter = ReflectionLoading.LoadByReflection(argPieces[1]);
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(argPieces[0], WordColumn))
												{
													wordColumn = int.Parse(argPieces[1]);
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(argPieces[0], TagColumn))
													{
														tagColumn = int.Parse(argPieces[1]);
													}
													else
													{
														throw new ArgumentException("TaggedFileRecord argument " + argPieces[0] + " is unknown");
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return new Edu.Stanford.Nlp.Tagger.IO.TaggedFileRecord(file, format, encoding, tagSeparator, treeTransformer, treeNormalizer, trf, treeRange, treeFilter, wordColumn, tagColumn);
		}

		public static string GetEncoding(Properties config)
		{
			string encoding = config.GetProperty(TaggerConfig.EncodingProperty);
			if (encoding == null)
			{
				return TaggerConfig.Encoding;
			}
			return encoding;
		}

		public static string GetTagSeparator(Properties config)
		{
			string tagSeparator = config.GetProperty(TaggerConfig.TagSeparatorProperty);
			if (tagSeparator == null)
			{
				return TaggerConfig.TagSeparator;
			}
			return tagSeparator;
		}
	}
}
