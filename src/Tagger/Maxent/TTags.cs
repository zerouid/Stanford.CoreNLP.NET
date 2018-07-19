using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>
	/// This class holds the POS tags, assigns them unique ids, and knows which tags
	/// are open versus closed class.
	/// </summary>
	/// <remarks>
	/// This class holds the POS tags, assigns them unique ids, and knows which tags
	/// are open versus closed class.
	/// <p/>
	/// Title:        StanfordMaxEnt<p>
	/// Description:  A Maximum Entropy Toolkit<p>
	/// Company:      Stanford University<p>
	/// </remarks>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class TTags
	{
		private IIndex<string> index = new HashIndex<string>();

		private readonly ICollection<string> closed = Generics.NewHashSet();

		private ICollection<string> openTags = null;

		private readonly bool isEnglish;

		private const bool doDeterministicTagExpansion = true;

		/// <summary>
		/// If true, then the open tags are fixed and we set closed tags based on
		/// index-openTags; otherwise, we set open tags based on index-closedTags.
		/// </summary>
		private bool openFixed = false;

		/// <summary>
		/// When making a decision based on the training data as to whether a
		/// tag is closed, this is the threshold for how many tokens can be in
		/// a closed class - purposely conservative.
		/// </summary>
		/// <remarks>
		/// When making a decision based on the training data as to whether a
		/// tag is closed, this is the threshold for how many tokens can be in
		/// a closed class - purposely conservative.
		/// TODO: make this an option you can set; need to pass in TaggerConfig object and then can say = config.getClosedTagThreshold());
		/// </remarks>
		private readonly int closedTagThreshold = System.Convert.ToInt32(TaggerConfig.ClosedClassThreshold);

		/// <summary>
		/// If true, when a model is trained, all tags that had fewer tokens than
		/// closedTagThreshold will be considered closed.
		/// </summary>
		private bool learnClosedTags = false;

		public TTags()
		{
			/* cache */
			// for speed
			isEnglish = false;
		}

		internal TTags(string language)
		{
			/*
			public TTags(TaggerConfig config) {
			String[] closedArray = config.getClosedClassTags();
			String[] openArray = config.getOpenClassTags();
			if(closedArray.length > 0) {
			closed = Generics.newHashSet(Arrays.asList(closedArray));
			} else if(openArray.length > 0) {
			openTags = Generics.newHashSet(Arrays.asList(openArray));
			} else {
			learnClosedTags = config.getLearnClosedClassTags();
			closedTagThreshold = config.getClosedTagThreshold();
			}
			}
			*/
			if (Sharpen.Runtime.EqualsIgnoreCase(language, "english"))
			{
				closed.Add(".");
				closed.Add(",");
				closed.Add("``");
				closed.Add("''");
				closed.Add(":");
				closed.Add("$");
				closed.Add("EX");
				closed.Add("(");
				closed.Add(")");
				closed.Add("#");
				closed.Add("MD");
				closed.Add("CC");
				closed.Add("DT");
				closed.Add("LS");
				closed.Add("PDT");
				closed.Add("POS");
				closed.Add("PRP");
				closed.Add("PRP$");
				closed.Add("RP");
				closed.Add("TO");
				closed.Add(Edu.Stanford.Nlp.Tagger.Common.Tagger.EosTag);
				closed.Add("UH");
				closed.Add("WDT");
				closed.Add("WP");
				closed.Add("WP$");
				closed.Add("WRB");
				closed.Add("-LRB-");
				closed.Add("-RRB-");
				//  closed.add("IN");
				isEnglish = true;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(language, "polish"))
				{
					closed.Add(".");
					closed.Add(",");
					closed.Add("``");
					closed.Add("''");
					closed.Add(":");
					closed.Add("$");
					closed.Add("(");
					closed.Add(")");
					closed.Add("#");
					closed.Add("POS");
					closed.Add(Edu.Stanford.Nlp.Tagger.Common.Tagger.EosTag);
					closed.Add("ppron12");
					closed.Add("ppron3");
					closed.Add("siebie");
					closed.Add("qub");
					closed.Add("conj");
					isEnglish = false;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(language, "chinese"))
					{
						/* chinese treebank 5 tags */
						closed.Add("AS");
						closed.Add("BA");
						closed.Add("CC");
						closed.Add("CS");
						closed.Add("DEC");
						closed.Add("DEG");
						closed.Add("DER");
						closed.Add("DEV");
						closed.Add("DT");
						closed.Add("ETC");
						closed.Add("IJ");
						closed.Add("LB");
						closed.Add("LC");
						closed.Add("P");
						closed.Add("PN");
						closed.Add("PU");
						closed.Add("SB");
						closed.Add("SP");
						closed.Add("VC");
						closed.Add("VE");
						isEnglish = false;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(language, "arabic"))
						{
							// kulick tag set
							// the following tags seem to be complete sets in the training
							// data (see the comments for "german" for more info)
							closed.Add("PUNC");
							closed.Add("CC");
							closed.Add("CPRP$");
							closed.Add(Edu.Stanford.Nlp.Tagger.Common.Tagger.EosTag);
							// maybe more should still be added ... cdm jun 2006
							isEnglish = false;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(language, "german"))
							{
								// The current version of the German tagger is built with the
								// negra-tiger data set.  We use the STTS tag set.  In
								// particular, we use the version with the changes described in
								// appendix A-2 of
								// http://www.uni-potsdam.de/u/germanistik/ls_dgs/tiger1-intro.pdf
								// eg the STTS tag set with PROAV instead of PAV
								// To find the closed tags, we use lists of standard closed German
								// tags, eg
								// http://www.sfs.uni-tuebingen.de/Elwis/stts/Wortlisten/WortFormen.html
								// In other words:
								//
								// APPO APPR APPRART APZR ART KOKOM KON KOUI KOUS PDAT PDS PIAT
								// PIDAT PIS PPER PPOSAT PPOSS PRELAT PRELS PRF PROAV PTKA
								// PTKANT PTKNEG PTKVZ PTKZU PWAT PWAV PWS VAFIN VAIMP VAINF
								// VAPP VMFIN VMINF VMPP
								//
								// One issue with this is that our training data does not have
								// the complete collection of many of these closed tags.  For
								// example, words with the tag APPR show up in the test or dev
								// sets without ever showing up in the training.  Tags that
								// don't have this property:
								//
								// KOKOM PPOSS PTKA PTKNEG PWAT VAINF VAPP VMINF VMPP
								closed.Add("$,");
								closed.Add("$.");
								closed.Add("$(");
								closed.Add("--");
								// this shouldn't be a tag of the dataset, but was a conversion bug!
								closed.Add(Edu.Stanford.Nlp.Tagger.Common.Tagger.EosTag);
								closed.Add("KOKOM");
								closed.Add("PPOSS");
								closed.Add("PTKA");
								closed.Add("PTKNEG");
								closed.Add("PWAT");
								closed.Add("VAINF");
								closed.Add("VAPP");
								closed.Add("VMINF");
								closed.Add("VMPP");
								isEnglish = false;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(language, "french"))
								{
									// Using the french treebank, with Spence's adaptations of
									// Candito's treebank modifications, we get that only the
									// punctuation tags are reliably closed:
									// !, ", *, ,, -, -LRB-, -RRB-, ., ..., /, :, ;, =, ?, [, ]
									closed.Add("!");
									closed.Add("\"");
									closed.Add("*");
									closed.Add(",");
									closed.Add("-");
									closed.Add("-LRB-");
									closed.Add("-RRB-");
									closed.Add(".");
									closed.Add("...");
									closed.Add("/");
									closed.Add(":");
									closed.Add(";");
									closed.Add("=");
									closed.Add("?");
									closed.Add("[");
									closed.Add("]");
									isEnglish = false;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(language, "spanish"))
									{
										closed.Add(Edu.Stanford.Nlp.Tagger.Common.Tagger.EosTag);
										// conjunctions
										closed.Add("cc");
										closed.Add("cs");
										// punctuation
										closed.Add("faa");
										closed.Add("fat");
										closed.Add("fc");
										closed.Add("fca");
										closed.Add("fct");
										closed.Add("fd");
										closed.Add("fe");
										closed.Add("fg");
										closed.Add("fh");
										closed.Add("fia");
										closed.Add("fit");
										closed.Add("fla");
										closed.Add("flt");
										closed.Add("fp");
										closed.Add("fpa");
										closed.Add("fpt");
										closed.Add("fra");
										closed.Add("frc");
										closed.Add("fs");
										closed.Add("ft");
										closed.Add("fx");
										closed.Add("fz");
										isEnglish = false;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(language, "medpost"))
										{
											closed.Add(".");
											closed.Add(",");
											closed.Add("``");
											closed.Add("''");
											closed.Add(":");
											closed.Add("$");
											closed.Add("EX");
											closed.Add("(");
											closed.Add(")");
											closed.Add("VM");
											closed.Add("CC");
											closed.Add("DD");
											closed.Add("DB");
											closed.Add("GE");
											closed.Add("PND");
											closed.Add("PNG");
											closed.Add("TO");
											closed.Add(Edu.Stanford.Nlp.Tagger.Common.Tagger.EosTag);
											closed.Add("-LRB-");
											closed.Add("-RRB-");
											isEnglish = false;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(language, "testing"))
											{
												closed.Add(".");
												closed.Add(Edu.Stanford.Nlp.Tagger.Common.Tagger.EosTag);
												isEnglish = false;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(language, string.Empty))
												{
													isEnglish = false;
												}
												else
												{
													/* add closed-class lists for other languages here */
													throw new Exception("unknown language: " + language);
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

		/// <summary>Return the Set of tags used by this tagger (available after training the tagger).</summary>
		/// <returns>The Set of tags used by this tagger</returns>
		public virtual ICollection<string> TagSet()
		{
			return new HashSet<string>(index.ObjectsList());
		}

		/// <summary>Returns a list of all open class tags</summary>
		/// <returns>set of open tags</returns>
		public virtual ICollection<string> GetOpenTags()
		{
			if (openTags == null)
			{
				/* cache check */
				ICollection<string> open = Generics.NewHashSet();
				foreach (string tag in index)
				{
					if (!closed.Contains(tag))
					{
						open.Add(tag);
					}
				}
				openTags = open;
			}
			// if
			return openTags;
		}

		protected internal virtual int Add(string tag)
		{
			return index.AddToIndex(tag);
		}

		public virtual string GetTag(int i)
		{
			return index.Get(i);
		}

		protected internal virtual void Save(string filename, IDictionary<string, ICollection<string>> tagTokens)
		{
			try
			{
				DataOutputStream @out = IOUtils.GetDataOutputStream(filename);
				Save(@out, tagTokens);
				@out.Close();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		protected internal virtual void Save(DataOutputStream file, IDictionary<string, ICollection<string>> tagTokens)
		{
			try
			{
				file.WriteInt(index.Size());
				foreach (string item in index)
				{
					file.WriteUTF(item);
					if (learnClosedTags)
					{
						if (tagTokens[item].Count < closedTagThreshold)
						{
							MarkClosed(item);
						}
					}
					file.WriteBoolean(IsClosed(item));
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		protected internal virtual void Read(string filename)
		{
			try
			{
				DataInputStream @in = IOUtils.GetDataInputStream(filename);
				Read(@in);
				@in.Close();
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		protected internal virtual void Read(DataInputStream file)
		{
			try
			{
				int size = file.ReadInt();
				index = new HashIndex<string>();
				for (int i = 0; i < size; i++)
				{
					string tag = file.ReadUTF();
					bool inClosed = file.ReadBoolean();
					index.Add(tag);
					if (inClosed)
					{
						closed.Add(tag);
					}
				}
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		protected internal virtual bool IsClosed(string tag)
		{
			if (openFixed)
			{
				return !openTags.Contains(tag);
			}
			else
			{
				return closed.Contains(tag);
			}
		}

		internal virtual void MarkClosed(string tag)
		{
			Add(tag);
			closed.Add(tag);
		}

		public virtual void SetLearnClosedTags(bool learn)
		{
			learnClosedTags = learn;
		}

		public virtual void SetOpenClassTags(string[] openClassTags)
		{
			openTags = Generics.NewHashSet();
			Sharpen.Collections.AddAll(openTags, Arrays.AsList(openClassTags));
			foreach (string tag in openClassTags)
			{
				Add(tag);
			}
			openFixed = true;
		}

		public virtual void SetClosedClassTags(string[] closedClassTags)
		{
			foreach (string tag in closedClassTags)
			{
				MarkClosed(tag);
			}
		}

		internal virtual int GetIndex(string tag)
		{
			return index.IndexOf(tag);
		}

		public virtual int GetSize()
		{
			return index.Size();
		}

		/// <summary>Deterministically adds other possible tags for words given observed tags.</summary>
		/// <remarks>
		/// Deterministically adds other possible tags for words given observed tags.
		/// For instance, for English with the Penn POS tag, a word with the VB
		/// tag would also be expected to have the VBP tag.
		/// <p>
		/// The current implementation is a bit contorted, as it works to avoid
		/// object allocations wherever possible for maximum runtime speed. But
		/// intuitively it's just: For English (only),
		/// if the VBD tag is present but not VBN, add it, and vice versa;
		/// if the VB tag is present but not VBP, add it, and vice versa.
		/// </remarks>
		/// <param name="tags">Known possible tags for the word</param>
		/// <returns>A superset of tags</returns>
		internal virtual string[] DeterministicallyExpandTags(string[] tags)
		{
			if (isEnglish && doDeterministicTagExpansion)
			{
				bool seenVBN = false;
				bool seenVBD = false;
				bool seenVB = false;
				bool seenVBP = false;
				foreach (string tag in tags)
				{
					char ch = tag[0];
					if (ch == 'V')
					{
						switch (tag)
						{
							case "VBD":
							{
								seenVBD = true;
								break;
							}

							case "VBN":
							{
								seenVBN = true;
								break;
							}

							case "VB":
							{
								seenVB = true;
								break;
							}

							case "VBP":
							{
								seenVBP = true;
								break;
							}
						}
					}
				}
				int toAdd = 0;
				if ((seenVBN ^ seenVBD))
				{
					// ^ is xor
					toAdd++;
				}
				if (seenVB ^ seenVBP)
				{
					toAdd++;
				}
				if (toAdd > 0)
				{
					int ind = tags.Length;
					string[] newTags = new string[ind + toAdd];
					System.Array.Copy(tags, 0, newTags, 0, tags.Length);
					if (seenVBN && !seenVBD)
					{
						newTags[ind++] = "VBD";
					}
					else
					{
						if (seenVBD && !seenVBN)
						{
							newTags[ind++] = "VBN";
						}
					}
					if (seenVB && !seenVBP)
					{
						newTags[ind] = "VBP";
					}
					else
					{
						if (seenVBP && !seenVB)
						{
							newTags[ind] = "VB";
						}
					}
					return newTags;
				}
				else
				{
					return tags;
				}
			}
			else
			{
				// no tag expansion for other languages currently
				return tags;
			}
		}

		public override string ToString()
		{
			StringBuilder s = new StringBuilder();
			s.Append(index);
			s.Append(' ');
			if (openFixed)
			{
				s.Append(" OPEN:").Append(GetOpenTags());
			}
			else
			{
				s.Append(" open:").Append(GetOpenTags()).Append(" CLOSED:").Append(closed);
			}
			return s.ToString();
		}
	}
}
