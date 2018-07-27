//
// StanfordCoreNLP -- a suite of NLP tools
// Copyright (c) 2009-2010 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Coref.Data
{
	/// <summary>One mention for the SieveCoreferenceSystem.</summary>
	/// <author>Jenny Finkel, Karthik Raghunathan, Heeyoung Lee, Marta Recasens</author>
	[System.Serializable]
	public class Mention : ICoreAnnotation<Edu.Stanford.Nlp.Coref.Data.Mention>
	{
		private const long serialVersionUID = -7524485803945717057L;

		public Mention()
		{
		}

		public Mention(int mentionID, int startIndex, int endIndex, IList<CoreLabel> sentenceWords, SemanticGraph basicDependency, SemanticGraph enhancedDependency)
		{
			this.mentionID = mentionID;
			this.startIndex = startIndex;
			this.endIndex = endIndex;
			this.sentenceWords = sentenceWords;
			this.basicDependency = basicDependency;
			this.enhancedDependency = enhancedDependency;
		}

		public Mention(int mentionID, int startIndex, int endIndex, IList<CoreLabel> sentenceWords, SemanticGraph basicDependency, SemanticGraph enhancedDependency, IList<CoreLabel> mentionSpan)
			: this(mentionID, startIndex, endIndex, sentenceWords, basicDependency, enhancedDependency)
		{
			this.originalSpan = mentionSpan;
		}

		public Mention(int mentionID, int startIndex, int endIndex, IList<CoreLabel> sentenceWords, SemanticGraph basicDependency, SemanticGraph enhancedDependency, IList<CoreLabel> mentionSpan, Tree mentionTree)
			: this(mentionID, startIndex, endIndex, sentenceWords, basicDependency, enhancedDependency, mentionSpan)
		{
			this.mentionSubTree = mentionTree;
		}

		public Dictionaries.MentionType mentionType;

		public Dictionaries.Number number;

		public Dictionaries.Gender gender;

		public Dictionaries.Animacy animacy;

		public Dictionaries.Person person;

		public string headString;

		public string nerString;

		public int startIndex;

		public int endIndex;

		public int headIndex;

		public int mentionID = -1;

		public int originalRef = -1;

		public IndexedWord headIndexedWord;

		public int goldCorefClusterID = -1;

		public int corefClusterID = -1;

		public int mentionNum;

		public int sentNum = -1;

		public int utter = -1;

		public int paragraph = -1;

		public bool isSubject;

		public bool isDirectObject;

		public bool isIndirectObject;

		public bool isPrepositionObject;

		public IndexedWord dependingVerb;

		public bool hasTwin = false;

		public bool generic = false;

		public bool isSingleton;

		public IList<CoreLabel> sentenceWords;

		public IList<CoreLabel> originalSpan;

		public Tree mentionSubTree;

		public Tree contextParseTree;

		public CoreLabel headWord;

		public SemanticGraph basicDependency;

		public SemanticGraph enhancedDependency;

		public ICollection<string> dependents = Generics.NewHashSet();

		public IList<string> preprocessedTerms;

		public object synsets;

		/// <summary>Set of other mentions in the same sentence that are syntactic appositions to this</summary>
		public ICollection<Edu.Stanford.Nlp.Coref.Data.Mention> appositions = null;

		public ICollection<Edu.Stanford.Nlp.Coref.Data.Mention> predicateNominatives = null;

		public ICollection<Edu.Stanford.Nlp.Coref.Data.Mention> relativePronouns = null;

		/// <summary>Set of other mentions in the same sentence that below to this list</summary>
		public ICollection<Edu.Stanford.Nlp.Coref.Data.Mention> listMembers = null;

		/// <summary>Set of other mentions in the same sentence that I am a member of</summary>
		public ICollection<Edu.Stanford.Nlp.Coref.Data.Mention> belongToLists = null;

		public SpeakerInfo speakerInfo;

		[System.NonSerialized]
		private string spanString = null;

		[System.NonSerialized]
		private string lowercaseNormalizedSpanString = null;

		public IntCounter<int> antecedentOrdering = new IntCounter<int>();

		// generic pronoun or generic noun (bare plurals)
		// Mention is identified as being this speaker....
		public virtual Type GetType()
		{
			return typeof(Edu.Stanford.Nlp.Coref.Data.Mention);
		}

		public virtual bool IsPronominal()
		{
			return mentionType == Dictionaries.MentionType.Pronominal;
		}

		public override string ToString()
		{
			return SpanToString();
		}

		public virtual string SpanToString()
		{
			//    synchronized(this) {
			if (spanString == null)
			{
				StringBuilder os = new StringBuilder();
				for (int i = 0; i < originalSpan.Count; i++)
				{
					if (i > 0)
					{
						os.Append(" ");
					}
					os.Append(originalSpan[i].Get(typeof(CoreAnnotations.TextAnnotation)));
				}
				spanString = os.ToString();
			}
			//    }
			return spanString;
		}

		public virtual string LowercaseNormalizedSpanString()
		{
			//    synchronized(this) {
			if (lowercaseNormalizedSpanString == null)
			{
				// We always normalize to lowercase!!!
				lowercaseNormalizedSpanString = SpanToString().ToLower();
			}
			//    }
			return lowercaseNormalizedSpanString;
		}

		// Retrieves part of the span that corresponds to the NER (going out from head)
		public virtual IList<CoreLabel> NerTokens()
		{
			if (nerString == null || "O".Equals(nerString))
			{
				return null;
			}
			int start = headIndex - startIndex;
			int end = headIndex - startIndex + 1;
			while (start > 0)
			{
				CoreLabel prev = originalSpan[start - 1];
				if (nerString.Equals(prev.Ner()))
				{
					start--;
				}
				else
				{
					break;
				}
			}
			while (end < originalSpan.Count)
			{
				CoreLabel next = originalSpan[end];
				if (nerString.Equals(next.Ner()))
				{
					end++;
				}
				else
				{
					break;
				}
			}
			return originalSpan.SubList(start, end);
		}

		// Retrieves part of the span that corresponds to the NER (going out from head)
		public virtual string NerName()
		{
			IList<CoreLabel> t = NerTokens();
			return (t != null) ? StringUtils.JoinWords(t, " ") : null;
		}

		/// <summary>
		/// Set attributes of a mention:
		/// head string, mention type, NER label, Number, Gender, Animacy
		/// </summary>
		/// <exception cref="System.Exception"/>
		public virtual void Process(Dictionaries dict, Semantics semantics)
		{
			SetHeadString();
			SetType(dict);
			SetNERString();
			IList<string> mStr = GetMentionString();
			SetNumber(dict);
			SetGender(dict, GetGender(dict, mStr));
			SetAnimacy(dict);
			SetPerson(dict);
			SetDiscourse();
			if (semantics != null)
			{
				SetSemantics(dict, semantics);
			}
		}

		/// <exception cref="System.Exception"/>
		public virtual void Process(Dictionaries dict, Semantics semantics, LogisticClassifier<string, string> singletonPredictor)
		{
			Process(dict, semantics);
			if (singletonPredictor != null)
			{
				SetSingleton(singletonPredictor, dict);
			}
		}

		private void SetSingleton(LogisticClassifier<string, string> predictor, Dictionaries dict)
		{
			double coreference_score = predictor.ProbabilityOf(new BasicDatum<string, string>(GetSingletonFeatures(dict), "1"));
			if (coreference_score < 0.2)
			{
				this.isSingleton = true;
			}
		}

		/// <summary>
		/// Returns the features used by the singleton predictor (logistic
		/// classifier) to decide whether the mention belongs to a singleton entity
		/// </summary>
		public virtual List<string> GetSingletonFeatures(Dictionaries dict)
		{
			List<string> features = new List<string>();
			features.Add(mentionType.ToString());
			features.Add(nerString);
			features.Add(animacy.ToString());
			int personNum = 3;
			if (person.Equals(Dictionaries.Person.I) || person.Equals(Dictionaries.Person.We))
			{
				personNum = 1;
			}
			if (person.Equals(Dictionaries.Person.You))
			{
				personNum = 2;
			}
			if (person.Equals(Dictionaries.Person.Unknown))
			{
				personNum = 0;
			}
			features.Add(personNum.ToString());
			features.Add(number.ToString());
			features.Add(GetPosition());
			features.Add(GetRelation());
			features.Add(GetQuantification(dict));
			features.Add(GetModifiers(dict).ToString());
			features.Add(GetNegation(dict).ToString());
			features.Add(GetModal(dict).ToString());
			features.Add(GetReportEmbedding(dict).ToString());
			features.Add(GetCoordination().ToString());
			return features;
		}

		private IList<string> GetMentionString()
		{
			IList<string> mStr = new List<string>();
			foreach (CoreLabel l in this.originalSpan)
			{
				mStr.Add(l.Get(typeof(CoreAnnotations.TextAnnotation)).ToLower());
				if (l == this.headWord)
				{
					break;
				}
			}
			// remove words after headword
			return mStr;
		}

		private Dictionaries.Gender GetGender(Dictionaries dict, IList<string> mStr)
		{
			int len = mStr.Count;
			char firstLetter = headWord.Get(typeof(CoreAnnotations.TextAnnotation))[0];
			if (len > 1 && char.IsUpperCase(firstLetter) && nerString.StartsWith("PER"))
			{
				int firstNameIdx = len - 2;
				string secondToLast = mStr[firstNameIdx];
				if (firstNameIdx > 1 && (secondToLast.Length == 1 || (secondToLast.Length == 2 && secondToLast.EndsWith("."))))
				{
					firstNameIdx--;
				}
				for (int i = 0; i <= firstNameIdx; i++)
				{
					if (dict.genderNumber.Contains(mStr.SubList(i, len)))
					{
						return dict.genderNumber[mStr.SubList(i, len)];
					}
				}
				// find converted string with ! (e.g., "dr. martin luther king jr. boulevard" -> "dr. !")
				IList<string> convertedStr = new List<string>(2);
				convertedStr.Add(mStr[firstNameIdx]);
				convertedStr.Add("!");
				if (dict.genderNumber.Contains(convertedStr))
				{
					return dict.genderNumber[convertedStr];
				}
				if (dict.genderNumber.Contains(mStr.SubList(firstNameIdx, firstNameIdx + 1)))
				{
					return dict.genderNumber[mStr.SubList(firstNameIdx, firstNameIdx + 1)];
				}
			}
			if (mStr.Count > 0 && dict.genderNumber.Contains(mStr.SubList(len - 1, len)))
			{
				return dict.genderNumber[mStr.SubList(len - 1, len)];
			}
			return null;
		}

		private void SetDiscourse()
		{
			// utter = headWord.get(CoreAnnotations.UtteranceAnnotation.class);
			Pair<IndexedWord, string> verbDependency = FindDependentVerb(this);
			string dep = verbDependency.Second();
			dependingVerb = verbDependency.First();
			isSubject = false;
			isDirectObject = false;
			isIndirectObject = false;
			isPrepositionObject = false;
			if (dep != null)
			{
				switch (dep)
				{
					case "nsubj":
					case "csubj":
					{
						isSubject = true;
						break;
					}

					case "dobj":
					case "nsubjpass":
					case "nsubj:pass":
					{
						isDirectObject = true;
						break;
					}

					case "iobj":
					{
						isIndirectObject = true;
						break;
					}

					default:
					{
						if (dep.StartsWith("nmod") && !dep.Equals("nmod:npmod") && !dep.Equals("nmod:tmod") && !dep.Equals("nmod:poss") && !dep.Equals("nmod:agent"))
						{
							isPrepositionObject = true;
						}
						break;
					}
				}
			}
		}

		private void SetPerson(Dictionaries dict)
		{
			// only do for pronoun
			if (!this.IsPronominal())
			{
				person = Dictionaries.Person.Unknown;
				return;
			}
			string spanToString = this.SpanToString().ToLower();
			if (dict.firstPersonPronouns.Contains(spanToString))
			{
				if (number == Dictionaries.Number.Singular)
				{
					person = Dictionaries.Person.I;
				}
				else
				{
					if (number == Dictionaries.Number.Plural)
					{
						person = Dictionaries.Person.We;
					}
					else
					{
						person = Dictionaries.Person.Unknown;
					}
				}
			}
			else
			{
				if (dict.secondPersonPronouns.Contains(spanToString))
				{
					person = Dictionaries.Person.You;
				}
				else
				{
					if (dict.thirdPersonPronouns.Contains(spanToString))
					{
						if (gender == Dictionaries.Gender.Male && number == Dictionaries.Number.Singular)
						{
							person = Dictionaries.Person.He;
						}
						else
						{
							if (gender == Dictionaries.Gender.Female && number == Dictionaries.Number.Singular)
							{
								person = Dictionaries.Person.She;
							}
							else
							{
								if ((gender == Dictionaries.Gender.Neutral || animacy == Dictionaries.Animacy.Inanimate) && number == Dictionaries.Number.Singular)
								{
									person = Dictionaries.Person.It;
								}
								else
								{
									if (number == Dictionaries.Number.Plural)
									{
										person = Dictionaries.Person.They;
									}
									else
									{
										person = Dictionaries.Person.Unknown;
									}
								}
							}
						}
					}
					else
					{
						person = Dictionaries.Person.Unknown;
					}
				}
			}
		}

		/// <exception cref="System.Exception"/>
		private void SetSemantics(Dictionaries dict, Semantics semantics)
		{
			preprocessedTerms = this.PreprocessSearchTerm();
			if (dict.statesAbbreviation.Contains(this.SpanToString()))
			{
				// states abbreviations
				preprocessedTerms = new List<string>();
				preprocessedTerms.Add(dict.statesAbbreviation[this.SpanToString()]);
			}
			MethodInfo meth = Sharpen.Runtime.GetDeclaredMethod(semantics.wordnet.GetType(), "findSynset", typeof(IList));
			synsets = meth.Invoke(semantics.wordnet, new object[] { preprocessedTerms });
			if (this.IsPronominal())
			{
				return;
			}
		}

		/// <summary>Check list member? True if this mention is inside the other mention and the other mention is a list</summary>
		public virtual bool IsListMemberOf(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			if (this.Equals(m))
			{
				return false;
			}
			if (m.mentionType == Dictionaries.MentionType.List && this.mentionType == Dictionaries.MentionType.List)
			{
				return false;
			}
			// Don't handle nested lists
			if (m.mentionType == Dictionaries.MentionType.List)
			{
				return this.IncludedIn(m);
			}
			return false;
		}

		public virtual void AddListMember(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			if (listMembers == null)
			{
				listMembers = Generics.NewHashSet();
			}
			listMembers.Add(m);
		}

		public virtual void AddBelongsToList(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			if (belongToLists == null)
			{
				belongToLists = Generics.NewHashSet();
			}
			belongToLists.Add(m);
		}

		public virtual bool IsMemberOfSameList(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			ICollection<Edu.Stanford.Nlp.Coref.Data.Mention> l1 = belongToLists;
			ICollection<Edu.Stanford.Nlp.Coref.Data.Mention> l2 = m.belongToLists;
			if (l1 != null && l2 != null && CollectionUtils.ContainsAny(l1, l2))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		private bool IsListLike()
		{
			// See if this mention looks to be a conjunction of things
			// Check for "or" and "and" and ","
			int commas = 0;
			//    boolean firstLabelLike = false;
			//    if (originalSpan.size() > 1) {
			//      String w = originalSpan.get(1).word();
			//      firstLabelLike = (w.equals(":") || w.equals("-"));
			//    }
			string mentionSpanString = SpanToString();
			string subTreeSpanString = StringUtils.JoinWords(mentionSubTree.YieldWords(), " ");
			if (subTreeSpanString.Equals(mentionSpanString))
			{
				// subtree represents this mention well....
				IList<Tree> children = mentionSubTree.GetChildrenAsList();
				foreach (Tree t in children)
				{
					string label = t.Value();
					string ner = null;
					if (t.IsLeaf())
					{
						ner = ((CoreLabel)t.GetLeaves()[0].Label()).Ner();
					}
					if ("CC".Equals(label))
					{
						// Check NER type
						if (ner == null || "O".Equals(ner))
						{
							return true;
						}
					}
					else
					{
						if (label.Equals(","))
						{
							if (ner == null || "O".Equals(ner))
							{
								commas++;
							}
						}
					}
				}
			}
			if (commas <= 2)
			{
				// look at the string for and/or
				bool first = true;
				foreach (CoreLabel t in originalSpan)
				{
					string tag = t.Tag();
					string ner = t.Ner();
					string w = t.Word();
					if (tag.Equals("TO") || tag.Equals("IN") || tag.StartsWith("VB"))
					{
						// prepositions and verbs are too hard for us
						return false;
					}
					if (!first)
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(w, "and") || Sharpen.Runtime.EqualsIgnoreCase(w, "or"))
						{
							// Check NER type
							if (ner == null || "O".Equals(ner))
							{
								return true;
							}
						}
					}
					first = false;
				}
			}
			return (commas > 2);
		}

		private bool IsListLikeByDependency()
		{
			if (this.headIndexedWord == null)
			{
				return false;
			}
			// probably parser error: default is not LIST
			IndexedWord conj = this.basicDependency.GetChildWithReln(this.headIndexedWord, UniversalEnglishGrammaticalRelations.Conjunct);
			bool hasConjunction = (conj != null);
			bool conjInMention = (hasConjunction) ? this.startIndex < conj.Index() - 1 && conj.Index() - 1 < this.endIndex : false;
			return conjInMention;
		}

		private void SetType(Dictionaries dict)
		{
			if ((this.mentionSubTree != null && IsListLike()) || (this.mentionSubTree == null && IsListLikeByDependency()))
			{
				mentionType = Dictionaries.MentionType.List;
			}
			else
			{
				//Redwood.log("debug-mention", "IS LIST: " + this);
				if (headWord.ContainsKey(typeof(CoreAnnotations.EntityTypeAnnotation)))
				{
					// ACE gold mention type
					if (headWord.Get(typeof(CoreAnnotations.EntityTypeAnnotation)).Equals("PRO"))
					{
						mentionType = Dictionaries.MentionType.Pronominal;
					}
					else
					{
						if (headWord.Get(typeof(CoreAnnotations.EntityTypeAnnotation)).Equals("NAM"))
						{
							mentionType = Dictionaries.MentionType.Proper;
						}
						else
						{
							mentionType = Dictionaries.MentionType.Nominal;
						}
					}
				}
				else
				{
					// MUC
					if (!headWord.ContainsKey(typeof(CoreAnnotations.NamedEntityTagAnnotation)))
					{
						// temporary fix
						mentionType = Dictionaries.MentionType.Nominal;
					}
					else
					{
						//Redwood.log("debug-mention", "no NamedEntityTagAnnotation: "+headWord);
						if (headWord.Tag().StartsWith("PRP") || headWord.Tag().StartsWith("PN") || (originalSpan.Count == 1 && headWord.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)).Equals("O") && (dict.allPronouns.Contains(headString) || dict.relativePronouns
							.Contains(headString))))
						{
							mentionType = Dictionaries.MentionType.Pronominal;
						}
						else
						{
							if (!headWord.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)).Equals("O") || headWord.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).StartsWith("NNP"))
							{
								mentionType = Dictionaries.MentionType.Proper;
							}
							else
							{
								mentionType = Dictionaries.MentionType.Nominal;
							}
						}
					}
				}
			}
		}

		private void SetGender(Dictionaries dict, Dictionaries.Gender genderNumberResult)
		{
			gender = Dictionaries.Gender.Unknown;
			if (genderNumberResult != null && this.number != Dictionaries.Number.Plural)
			{
				gender = genderNumberResult;
			}
			if (mentionType == Dictionaries.MentionType.Pronominal)
			{
				if (dict.malePronouns.Contains(headString))
				{
					gender = Dictionaries.Gender.Male;
				}
				else
				{
					if (dict.femalePronouns.Contains(headString))
					{
						gender = Dictionaries.Gender.Female;
					}
				}
			}
			else
			{
				// Bergsma or user provided list
				if (gender == Dictionaries.Gender.Unknown)
				{
					if ("PERSON".Equals(nerString) || "PER".Equals(nerString))
					{
						// Try to get gender of the named entity
						// Start with first name until we get gender...
						IList<CoreLabel> nerToks = NerTokens();
						foreach (CoreLabel t in nerToks)
						{
							string name = t.Word().ToLower();
							if (dict.maleWords.Contains(name))
							{
								gender = Dictionaries.Gender.Male;
								break;
							}
							else
							{
								if (dict.femaleWords.Contains(name))
								{
									gender = Dictionaries.Gender.Female;
									break;
								}
							}
						}
					}
					else
					{
						if (dict.maleWords.Contains(headString))
						{
							gender = Dictionaries.Gender.Male;
						}
						else
						{
							if (dict.femaleWords.Contains(headString))
							{
								gender = Dictionaries.Gender.Female;
							}
							else
							{
								if (dict.neutralWords.Contains(headString))
								{
									gender = Dictionaries.Gender.Neutral;
								}
							}
						}
					}
				}
			}
		}

		protected internal virtual void SetNumber(Dictionaries dict)
		{
			if (mentionType == Dictionaries.MentionType.Pronominal)
			{
				if (dict.pluralPronouns.Contains(headString))
				{
					number = Dictionaries.Number.Plural;
				}
				else
				{
					if (dict.singularPronouns.Contains(headString))
					{
						number = Dictionaries.Number.Singular;
					}
					else
					{
						number = Dictionaries.Number.Unknown;
					}
				}
			}
			else
			{
				if (mentionType == Dictionaries.MentionType.List)
				{
					number = Dictionaries.Number.Plural;
				}
				else
				{
					if (!nerString.Equals("O") && mentionType != Dictionaries.MentionType.Nominal)
					{
						// Check to see if this is a list of things
						if (!(nerString.Equals("ORGANIZATION") || nerString.StartsWith("ORG")))
						{
							number = Dictionaries.Number.Singular;
						}
						else
						{
							// ORGs can be both plural and singular
							number = Dictionaries.Number.Unknown;
						}
					}
					else
					{
						string tag = headWord.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
						if (tag.StartsWith("N") && tag.EndsWith("S"))
						{
							number = Dictionaries.Number.Plural;
						}
						else
						{
							if (tag.StartsWith("N"))
							{
								number = Dictionaries.Number.Singular;
							}
							else
							{
								number = Dictionaries.Number.Unknown;
							}
						}
					}
				}
			}
			if (mentionType != Dictionaries.MentionType.Pronominal)
			{
				if (number == Dictionaries.Number.Unknown)
				{
					if (dict.singularWords.Contains(headString))
					{
						number = Dictionaries.Number.Singular;
					}
					else
					{
						if (dict.pluralWords.Contains(headString))
						{
							number = Dictionaries.Number.Plural;
						}
					}
				}
			}
		}

		//      // replace this with LIST mention type
		//      if(Constants.USE_CONSTITUENT) {
		//        final String enumerationPattern = "NP < (NP=tmp $.. (/,|CC/ $.. NP))";
		//
		//        TregexPattern tgrepPattern = TregexPattern.compile(enumerationPattern);
		//        TregexMatcher m = tgrepPattern.matcher(this.mentionSubTree);
		//        while (m.find()) {
		//          //        Tree t = m.getMatch();
		//          if(this.mentionSubTree==m.getNode("tmp")
		//              && this.spanToString().toLowerCase().contains(" and ")) {
		//            number = Number.PLURAL;
		//          }
		//        }
		//      }
		private void SetAnimacy(Dictionaries dict)
		{
			if (mentionType == Dictionaries.MentionType.Pronominal)
			{
				if (dict.animatePronouns.Contains(headString))
				{
					animacy = Dictionaries.Animacy.Animate;
				}
				else
				{
					if (dict.inanimatePronouns.Contains(headString))
					{
						animacy = Dictionaries.Animacy.Inanimate;
					}
					else
					{
						animacy = Dictionaries.Animacy.Unknown;
					}
				}
			}
			else
			{
				switch (nerString)
				{
					case "PERSON":
					case "PER":
					case "PERS":
					{
						animacy = Dictionaries.Animacy.Animate;
						break;
					}

					case "LOCATION":
					case "LOC":
					{
						animacy = Dictionaries.Animacy.Inanimate;
						break;
					}

					case "MONEY":
					{
						animacy = Dictionaries.Animacy.Inanimate;
						break;
					}

					case "NUMBER":
					{
						animacy = Dictionaries.Animacy.Inanimate;
						break;
					}

					case "PERCENT":
					{
						animacy = Dictionaries.Animacy.Inanimate;
						break;
					}

					case "DATE":
					{
						animacy = Dictionaries.Animacy.Inanimate;
						break;
					}

					case "TIME":
					{
						animacy = Dictionaries.Animacy.Inanimate;
						break;
					}

					case "MISC":
					{
						animacy = Dictionaries.Animacy.Unknown;
						break;
					}

					case "VEH":
					case "VEHICLE":
					{
						animacy = Dictionaries.Animacy.Unknown;
						break;
					}

					case "FAC":
					case "FACILITY":
					{
						animacy = Dictionaries.Animacy.Inanimate;
						break;
					}

					case "GPE":
					{
						animacy = Dictionaries.Animacy.Inanimate;
						break;
					}

					case "WEA":
					case "WEAPON":
					{
						animacy = Dictionaries.Animacy.Inanimate;
						break;
					}

					case "ORG":
					case "ORGANIZATION":
					{
						animacy = Dictionaries.Animacy.Inanimate;
						break;
					}

					default:
					{
						animacy = Dictionaries.Animacy.Unknown;
						break;
					}
				}
				// Better heuristics using DekangLin:
				if (animacy == Dictionaries.Animacy.Unknown)
				{
					if (dict.animateWords.Contains(headString))
					{
						animacy = Dictionaries.Animacy.Animate;
					}
					else
					{
						if (dict.inanimateWords.Contains(headString))
						{
							animacy = Dictionaries.Animacy.Inanimate;
						}
					}
				}
			}
		}

		private static readonly string[] commonNESuffixes = new string[] { "Corp", "Co", "Inc", "Ltd" };

		private static bool KnownSuffix(string s)
		{
			if (s.EndsWith("."))
			{
				s = Sharpen.Runtime.Substring(s, 0, s.Length - 1);
			}
			foreach (string suff in commonNESuffixes)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(suff, s))
				{
					return true;
				}
			}
			return false;
		}

		private void SetHeadString()
		{
			this.headString = headWord.Get(typeof(CoreAnnotations.TextAnnotation)).ToLower();
			string ner = headWord.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
			if (ner != null && !ner.Equals("O"))
			{
				// make sure that the head of a NE is not a known suffix, e.g., Corp.
				int start = headIndex - startIndex;
				if (originalSpan.Count > 0 && start >= originalSpan.Count)
				{
					throw new Exception("Invalid start index " + start + "=" + headIndex + "-" + startIndex + ": originalSpan=[" + StringUtils.JoinWords(originalSpan, " ") + "], head=" + headWord);
				}
				while (start >= 0)
				{
					string head = originalSpan.Count > 0 ? originalSpan[start].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
					if (KnownSuffix(head))
					{
						start--;
					}
					else
					{
						this.headString = head;
						this.headWord = originalSpan[start];
						this.headIndex = startIndex + start;
						break;
					}
				}
			}
			this.headIndexedWord = basicDependency.GetNodeByIndexSafe(headWord.Index());
		}

		private void SetNERString()
		{
			if (headWord.ContainsKey(typeof(CoreAnnotations.EntityTypeAnnotation)))
			{
				// ACE
				if (headWord.ContainsKey(typeof(CoreAnnotations.NamedEntityTagAnnotation)) && headWord.Get(typeof(CoreAnnotations.EntityTypeAnnotation)).Equals("NAM"))
				{
					this.nerString = headWord.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
				}
				else
				{
					this.nerString = "O";
				}
			}
			else
			{
				// MUC
				if (headWord.ContainsKey(typeof(CoreAnnotations.NamedEntityTagAnnotation)))
				{
					this.nerString = headWord.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
				}
				else
				{
					this.nerString = "O";
				}
			}
		}

		public virtual bool SameSentence(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			return m.sentenceWords == sentenceWords;
		}

		private static bool Included(CoreLabel small, IList<CoreLabel> big)
		{
			if (small.Tag().Equals("NNP"))
			{
				foreach (CoreLabel w in big)
				{
					if (small.Word().Equals(w.Word()) || small.Word().Length > 2 && w.Word().StartsWith(small.Word()))
					{
						return true;
					}
				}
			}
			return false;
		}

		public virtual bool HeadsAgree(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			// we allow same-type NEs to not match perfectly, but rather one could be included in the other, e.g., "George" -> "George Bush"
			if (!nerString.Equals("O") && !m.nerString.Equals("O") && nerString.Equals(m.nerString) && (Included(headWord, m.originalSpan) || Included(m.headWord, originalSpan)))
			{
				return true;
			}
			return headString.Equals(m.headString);
		}

		public virtual bool NumbersAgree(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			return NumbersAgree(m, false);
		}

		private bool NumbersAgree(Edu.Stanford.Nlp.Coref.Data.Mention m, bool strict)
		{
			if (strict)
			{
				return number == m.number;
			}
			else
			{
				return number == Dictionaries.Number.Unknown || m.number == Dictionaries.Number.Unknown || number == m.number;
			}
		}

		public virtual bool GendersAgree(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			return GendersAgree(m, false);
		}

		public virtual bool GendersAgree(Edu.Stanford.Nlp.Coref.Data.Mention m, bool strict)
		{
			if (strict)
			{
				return gender == m.gender;
			}
			else
			{
				return gender == Dictionaries.Gender.Unknown || m.gender == Dictionaries.Gender.Unknown || gender == m.gender;
			}
		}

		public virtual bool AnimaciesAgree(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			return AnimaciesAgree(m, false);
		}

		public virtual bool AnimaciesAgree(Edu.Stanford.Nlp.Coref.Data.Mention m, bool strict)
		{
			if (strict)
			{
				return animacy == m.animacy;
			}
			else
			{
				return animacy == Dictionaries.Animacy.Unknown || m.animacy == Dictionaries.Animacy.Unknown || animacy == m.animacy;
			}
		}

		public virtual bool EntityTypesAgree(Edu.Stanford.Nlp.Coref.Data.Mention m, Dictionaries dict)
		{
			return EntityTypesAgree(m, dict, false);
		}

		public virtual bool EntityTypesAgree(Edu.Stanford.Nlp.Coref.Data.Mention m, Dictionaries dict, bool strict)
		{
			if (strict)
			{
				return nerString.Equals(m.nerString);
			}
			else
			{
				if (IsPronominal())
				{
					if (nerString.Contains("-") || m.nerString.Contains("-"))
					{
						//for ACE with gold NE
						if (m.nerString.Equals("O"))
						{
							return true;
						}
						else
						{
							if (m.nerString.StartsWith("ORG"))
							{
								return dict.organizationPronouns.Contains(headString);
							}
							else
							{
								if (m.nerString.StartsWith("PER"))
								{
									return dict.personPronouns.Contains(headString);
								}
								else
								{
									if (m.nerString.StartsWith("LOC"))
									{
										return dict.locationPronouns.Contains(headString);
									}
									else
									{
										if (m.nerString.StartsWith("GPE"))
										{
											return dict.GPEPronouns.Contains(headString);
										}
										else
										{
											if (m.nerString.StartsWith("VEH") || m.nerString.StartsWith("FAC") || m.nerString.StartsWith("WEA"))
											{
												return dict.facilityVehicleWeaponPronouns.Contains(headString);
											}
											else
											{
												return false;
											}
										}
									}
								}
							}
						}
					}
					else
					{
						switch (m.nerString)
						{
							case "O":
							{
								// ACE w/o gold NE or MUC
								return true;
							}

							case "MISC":
							{
								return true;
							}

							case "ORGANIZATION":
							{
								return dict.organizationPronouns.Contains(headString);
							}

							case "PERSON":
							{
								return dict.personPronouns.Contains(headString);
							}

							case "LOCATION":
							{
								return dict.locationPronouns.Contains(headString);
							}

							case "DATE":
							case "TIME":
							{
								return dict.dateTimePronouns.Contains(headString);
							}

							case "MONEY":
							case "PERCENT":
							case "NUMBER":
							{
								return dict.moneyPercentNumberPronouns.Contains(headString);
							}

							default:
							{
								return false;
							}
						}
					}
				}
				return nerString.Equals("O") || m.nerString.Equals("O") || nerString.Equals(m.nerString);
			}
		}

		/// <summary>Verifies if this mention's tree is dominated by the tree of the given mention</summary>
		public virtual bool IncludedIn(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			if (!m.SameSentence(this))
			{
				return false;
			}
			if (this.startIndex < m.startIndex || this.endIndex > m.endIndex)
			{
				return false;
			}
			return true;
		}

		/// <summary>Detects if the mention and candidate antecedent agree on all attributes respectively.</summary>
		/// <param name="potentialAntecedent"/>
		/// <returns>true if all attributes agree between both mention and candidate, else false.</returns>
		public virtual bool AttributesAgree(Edu.Stanford.Nlp.Coref.Data.Mention potentialAntecedent, Dictionaries dict)
		{
			return (this.AnimaciesAgree(potentialAntecedent) && this.EntityTypesAgree(potentialAntecedent, dict) && this.GendersAgree(potentialAntecedent) && this.NumbersAgree(potentialAntecedent));
		}

		/// <summary>Find apposition</summary>
		public virtual void AddApposition(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			if (appositions == null)
			{
				appositions = Generics.NewHashSet();
			}
			appositions.Add(m);
		}

		/// <summary>Check apposition</summary>
		public virtual bool IsApposition(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			if (appositions != null && appositions.Contains(m))
			{
				return true;
			}
			return false;
		}

		/// <summary>Find predicate nominatives</summary>
		public virtual void AddPredicateNominatives(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			if (predicateNominatives == null)
			{
				predicateNominatives = Generics.NewHashSet();
			}
			predicateNominatives.Add(m);
		}

		/// <summary>Check predicate nominatives</summary>
		public virtual bool IsPredicateNominatives(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			if (predicateNominatives != null && predicateNominatives.Contains(m))
			{
				return true;
			}
			return false;
		}

		/// <summary>Find relative pronouns</summary>
		public virtual void AddRelativePronoun(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			if (relativePronouns == null)
			{
				relativePronouns = Generics.NewHashSet();
			}
			relativePronouns.Add(m);
		}

		/// <summary>Find which mention appears first in a document</summary>
		public virtual bool AppearEarlierThan(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			if (this.sentNum < m.sentNum)
			{
				return true;
			}
			else
			{
				if (this.sentNum > m.sentNum)
				{
					return false;
				}
				else
				{
					if (this.startIndex < m.startIndex)
					{
						return true;
					}
					else
					{
						if (this.startIndex > m.startIndex)
						{
							return false;
						}
						else
						{
							if (this.endIndex > m.endIndex)
							{
								return true;
							}
							else
							{
								if (this.endIndex < m.endIndex)
								{
									return false;
								}
								else
								{
									if (this.headIndex != m.headIndex)
									{
										// Meaningless, but an arbitrary tiebreaker
										return this.headIndex < m.headIndex;
									}
									else
									{
										if (this.mentionType != m.mentionType)
										{
											// Meaningless, but an arbitrary tiebreaker
											return this.mentionType.representativeness > m.mentionType.representativeness;
										}
										else
										{
											// Meaningless, but an arbitrary tiebreaker
											return this.GetHashCode() < m.GetHashCode();
										}
									}
								}
							}
						}
					}
				}
			}
		}

		public virtual string LongestNNPEndsWithHead()
		{
			string ret = string.Empty;
			for (int i = headIndex; i >= startIndex; i--)
			{
				string pos = sentenceWords[i].Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
				if (!pos.StartsWith("NNP"))
				{
					break;
				}
				if (!ret.Equals(string.Empty))
				{
					ret = " " + ret;
				}
				ret = sentenceWords[i].Get(typeof(CoreAnnotations.TextAnnotation)) + ret;
			}
			return ret;
		}

		public virtual string LowestNPIncludesHead()
		{
			string ret = string.Empty;
			Tree head = this.contextParseTree.GetLeaves()[this.headIndex];
			Tree lowestNP = head;
			string s;
			while (true)
			{
				if (lowestNP == null)
				{
					return ret;
				}
				s = ((CoreLabel)lowestNP.Label()).Get(typeof(CoreAnnotations.ValueAnnotation));
				if (s.Equals("NP") || s.Equals("ROOT"))
				{
					break;
				}
				lowestNP = lowestNP.Ancestor(1, this.contextParseTree);
			}
			if (s.Equals("ROOT"))
			{
				lowestNP = head;
			}
			foreach (Tree t in lowestNP.GetLeaves())
			{
				if (!ret.Equals(string.Empty))
				{
					ret = ret + " ";
				}
				ret = ret + ((CoreLabel)t.Label()).Get(typeof(CoreAnnotations.TextAnnotation));
			}
			if (!this.SpanToString().Contains(ret))
			{
				return this.sentenceWords[this.headIndex].Get(typeof(CoreAnnotations.TextAnnotation));
			}
			return ret;
		}

		public virtual string StringWithoutArticle(string str)
		{
			string ret = (str == null) ? this.SpanToString() : str;
			if (ret.StartsWith("a ") || ret.StartsWith("A "))
			{
				return Sharpen.Runtime.Substring(ret, 2);
			}
			else
			{
				if (ret.StartsWith("an ") || ret.StartsWith("An "))
				{
					return Sharpen.Runtime.Substring(ret, 3);
				}
				else
				{
					if (ret.StartsWith("the ") || ret.StartsWith("The "))
					{
						return Sharpen.Runtime.Substring(ret, 4);
					}
				}
			}
			return ret;
		}

		public virtual IList<string> PreprocessSearchTerm()
		{
			IList<string> searchTerms = new List<string>();
			string[] terms = new string[4];
			terms[0] = this.StringWithoutArticle(this.RemovePhraseAfterHead());
			terms[1] = this.StringWithoutArticle(this.LowestNPIncludesHead());
			terms[2] = this.StringWithoutArticle(this.LongestNNPEndsWithHead());
			terms[3] = this.headString;
			foreach (string term in terms)
			{
				if (term.Contains("\""))
				{
					term = term.Replace("\"", "\\\"");
				}
				if (term.Contains("("))
				{
					term = term.Replace("(", "\\(");
				}
				if (term.Contains(")"))
				{
					term = term.Replace(")", "\\)");
				}
				if (term.Contains("!"))
				{
					term = term.Replace("!", "\\!");
				}
				if (term.Contains(":"))
				{
					term = term.Replace(":", "\\:");
				}
				if (term.Contains("+"))
				{
					term = term.Replace("+", "\\+");
				}
				if (term.Contains("-"))
				{
					term = term.Replace("-", "\\-");
				}
				if (term.Contains("~"))
				{
					term = term.Replace("~", "\\~");
				}
				if (term.Contains("*"))
				{
					term = term.Replace("*", "\\*");
				}
				if (term.Contains("["))
				{
					term = term.Replace("[", "\\[");
				}
				if (term.Contains("]"))
				{
					term = term.Replace("]", "\\]");
				}
				if (term.Contains("^"))
				{
					term = term.Replace("^", "\\^");
				}
				if (term.Equals(string.Empty))
				{
					continue;
				}
				if (term.Equals(string.Empty) || searchTerms.Contains(term))
				{
					continue;
				}
				if (term.Equals(terms[3]) && !terms[2].Equals(string.Empty))
				{
					continue;
				}
				searchTerms.Add(term);
			}
			return searchTerms;
		}

		public static string BuildQueryText(IList<string> terms)
		{
			string query = string.Empty;
			foreach (string t in terms)
			{
				query += t + " ";
			}
			return query.Trim();
		}

		/// <summary>Remove any clause after headword</summary>
		public virtual string RemovePhraseAfterHead()
		{
			string removed = string.Empty;
			int posComma = -1;
			int posWH = -1;
			for (int i = 0; i < this.originalSpan.Count; i++)
			{
				CoreLabel w = this.originalSpan[i];
				if (posComma == -1 && w.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).Equals(","))
				{
					posComma = this.startIndex + i;
				}
				if (posWH == -1 && w.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).StartsWith("W"))
				{
					posWH = this.startIndex + i;
				}
			}
			if (posComma != -1 && this.headIndex < posComma)
			{
				StringBuilder os = new StringBuilder();
				for (int i_1 = 0; i_1 < posComma - this.startIndex; i_1++)
				{
					if (i_1 > 0)
					{
						os.Append(" ");
					}
					os.Append(this.originalSpan[i_1].Get(typeof(CoreAnnotations.TextAnnotation)));
				}
				removed = os.ToString();
			}
			if (posComma == -1 && posWH != -1 && this.headIndex < posWH)
			{
				StringBuilder os = new StringBuilder();
				for (int i_1 = 0; i_1 < posWH - this.startIndex; i_1++)
				{
					if (i_1 > 0)
					{
						os.Append(" ");
					}
					os.Append(this.originalSpan[i_1].Get(typeof(CoreAnnotations.TextAnnotation)));
				}
				removed = os.ToString();
			}
			if (posComma == -1 && posWH == -1)
			{
				removed = this.SpanToString();
			}
			return removed;
		}

		public static string RemoveParenthesis(string text)
		{
			if (text.Split("\\(").Length > 0)
			{
				return text.Split("\\(")[0].Trim();
			}
			else
			{
				return string.Empty;
			}
		}

		// the mention is 'the + commonNoun' form
		protected internal virtual bool IsTheCommonNoun()
		{
			if (this.mentionType == Dictionaries.MentionType.Nominal && this.SpanToString().ToLower().StartsWith("the ") && this.SpanToString().Split(" ").Length == 2)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		private static Pair<IndexedWord, string> FindDependentVerb(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			if (m.enhancedDependency.GetRoots().Count == 0)
			{
				return new Pair<IndexedWord, string>();
			}
			// would be nice to condense this pattern, but sadly =reln
			// always uses the last relation in the sequence, not the first
			SemgrexPattern pattern = SemgrexPattern.Compile("{idx:" + (m.headIndex + 1) + "} [ <=reln {tag:/^V.*/}=verb | <=reln ({} << {tag:/^V.*/}=verb) ]");
			SemgrexMatcher matcher = pattern.Matcher(m.enhancedDependency);
			while (matcher.Find())
			{
				return Pair.MakePair(matcher.GetNode("verb"), matcher.GetRelnString("reln"));
			}
			return new Pair<IndexedWord, string>();
		}

		/// <summary>Returns true if this mention is contained inside m.</summary>
		/// <remarks>Returns true if this mention is contained inside m. That is, it is a subspan of the same sentence.</remarks>
		public virtual bool InsideIn(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			return this.sentNum == m.sentNum && m.startIndex <= this.startIndex && this.endIndex <= m.endIndex;
		}

		public virtual bool MoreRepresentativeThan(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			if (m == null)
			{
				return true;
			}
			if (mentionType.representativeness > m.mentionType.representativeness)
			{
				return true;
			}
			else
			{
				if (m.mentionType.representativeness > mentionType.representativeness)
				{
					return false;
				}
				else
				{
					// pick mention with better NER
					if (nerString != null && m.nerString == null)
					{
						return true;
					}
					if (nerString == null && m.nerString != null)
					{
						return false;
					}
					if (nerString != null && !nerString.Equals(m.nerString))
					{
						if ("O".Equals(m.nerString))
						{
							return true;
						}
						if ("O".Equals(nerString))
						{
							return false;
						}
						if ("MISC".Equals(m.nerString))
						{
							return true;
						}
						if ("MISC".Equals(nerString))
						{
							return false;
						}
					}
					// Ensure that both NER tags are neither MISC nor O, or are both not existent
					System.Diagnostics.Debug.Assert(nerString == null || nerString.Equals(m.nerString) || (!nerString.Equals("O") && !nerString.Equals("MISC") && !m.nerString.Equals("O") && !m.nerString.Equals("MISC")));
					// Return larger headIndex - startIndex
					if (headIndex - startIndex > m.headIndex - m.startIndex)
					{
						return true;
					}
					else
					{
						if (headIndex - startIndex < m.headIndex - m.startIndex)
						{
							return false;
						}
						else
						{
							// Return earlier sentence number
							if (sentNum < m.sentNum)
							{
								return true;
							}
							else
							{
								if (sentNum > m.sentNum)
								{
									return false;
								}
								else
								{
									// Return earlier head index
									if (headIndex < m.headIndex)
									{
										return true;
									}
									else
									{
										if (headIndex > m.headIndex)
										{
											return false;
										}
										else
										{
											// If the mentions are short, take the longer one
											if (originalSpan.Count <= 5 && originalSpan.Count > m.originalSpan.Count)
											{
												return true;
											}
											else
											{
												if (originalSpan.Count <= 5 && originalSpan.Count < m.originalSpan.Count)
												{
													return false;
												}
												else
												{
													// If the mentions are long, take the shorter one (we're getting into the realm of nonsense by here)
													if (originalSpan.Count < m.originalSpan.Count)
													{
														return true;
													}
													else
													{
														if (originalSpan.Count > m.originalSpan.Count)
														{
															return false;
														}
														else
														{
															throw new InvalidOperationException("Comparing a mention with itself for representativeness");
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
			}
		}

		// Returns filtered premodifiers (no determiners or numerals)
		public virtual List<List<IndexedWord>> GetPremodifiers()
		{
			List<List<IndexedWord>> premod = new List<List<IndexedWord>>();
			if (headIndexedWord == null)
			{
				return premod;
			}
			foreach (Pair<GrammaticalRelation, IndexedWord> child in enhancedDependency.ChildPairs(headIndexedWord))
			{
				string function = child.First().GetShortName();
				if (child.Second().Index() < headWord.Index() && !child.second.Tag().Equals("DT") && !child.second.Tag().Equals("WRB") && !function.EndsWith("det") && !function.Equals("nummod") && !function.StartsWith("acl") && !function.StartsWith("advcl")
					 && !function.Equals("punct"))
				{
					List<IndexedWord> phrase = new List<IndexedWord>(enhancedDependency.Descendants(child.Second()));
					phrase.Sort();
					premod.Add(phrase);
				}
			}
			return premod;
		}

		// Returns filtered postmodifiers (no relative, -ed or -ing clauses)
		public virtual List<List<IndexedWord>> GetPostmodifiers()
		{
			List<List<IndexedWord>> postmod = new List<List<IndexedWord>>();
			if (headIndexedWord == null)
			{
				return postmod;
			}
			foreach (Pair<GrammaticalRelation, IndexedWord> child in enhancedDependency.ChildPairs(headIndexedWord))
			{
				string function = child.First().GetShortName();
				if (child.Second().Index() > headWord.Index() && !function.EndsWith("det") && !function.Equals("nummod") && !function.StartsWith("acl") && !function.StartsWith("advcl") && !function.Equals("punct") && !(function.Equals("case") && enhancedDependency
					.Descendants(child.Second()).Count == 1 && child.second.Tag().Equals("POS")))
				{
					//possessive clitic
					List<IndexedWord> phrase = new List<IndexedWord>(enhancedDependency.Descendants(child.Second()));
					phrase.Sort();
					postmod.Add(phrase);
				}
			}
			return postmod;
		}

		public virtual string[] GetSplitPattern()
		{
			List<List<IndexedWord>> premodifiers = GetPremodifiers();
			string[] components = new string[4];
			components[0] = headWord.Lemma();
			if (premodifiers.Count == 0)
			{
				components[1] = headWord.Lemma();
				components[2] = headWord.Lemma();
			}
			else
			{
				if (premodifiers.Count == 1)
				{
					List<IAbstractCoreLabel> premod = Generics.NewArrayList();
					Sharpen.Collections.AddAll(premod, premodifiers[premodifiers.Count - 1]);
					premod.Add(headWord);
					components[1] = GetPattern(premod);
					components[2] = GetPattern(premod);
				}
				else
				{
					List<IAbstractCoreLabel> premod1 = Generics.NewArrayList();
					Sharpen.Collections.AddAll(premod1, premodifiers[premodifiers.Count - 1]);
					premod1.Add(headWord);
					components[1] = GetPattern(premod1);
					List<IAbstractCoreLabel> premod2 = Generics.NewArrayList();
					foreach (List<IndexedWord> premodifier in premodifiers)
					{
						Sharpen.Collections.AddAll(premod2, premodifier);
					}
					premod2.Add(headWord);
					components[2] = GetPattern(premod2);
				}
			}
			components[3] = GetPattern();
			return components;
		}

		public virtual string GetPattern()
		{
			List<IAbstractCoreLabel> pattern = Generics.NewArrayList();
			foreach (List<IndexedWord> premodifier in GetPremodifiers())
			{
				Sharpen.Collections.AddAll(pattern, premodifier);
			}
			pattern.Add(headWord);
			foreach (List<IndexedWord> postmodifier in GetPostmodifiers())
			{
				Sharpen.Collections.AddAll(pattern, postmodifier);
			}
			return GetPattern(pattern);
		}

		public virtual string GetPattern(IList<IAbstractCoreLabel> pTokens)
		{
			List<string> phrase_string = new List<string>();
			string ne = string.Empty;
			foreach (IAbstractCoreLabel token in pTokens)
			{
				if (token.Index() == headWord.Index())
				{
					phrase_string.Add(token.Lemma());
					ne = string.Empty;
				}
				else
				{
					if ((token.Lemma().Equals("and") || StringUtils.IsPunct(token.Lemma())) && pTokens.Count > pTokens.IndexOf(token) + 1 && pTokens.IndexOf(token) > 0 && pTokens[pTokens.IndexOf(token) + 1].Ner().Equals(pTokens[pTokens.IndexOf(token) - 1].Ner()
						))
					{
					}
					else
					{
						if (token.Index() == headWord.Index() - 1 && token.Ner().Equals(nerString))
						{
							phrase_string.Add(token.Lemma());
							ne = string.Empty;
						}
						else
						{
							if (!token.Ner().Equals("O"))
							{
								if (!token.Ner().Equals(ne))
								{
									ne = token.Ner();
									phrase_string.Add("<" + ne + ">");
								}
							}
							else
							{
								phrase_string.Add(token.Lemma());
								ne = string.Empty;
							}
						}
					}
				}
			}
			return StringUtils.Join(phrase_string);
		}

		public virtual bool IsCoordinated()
		{
			if (headIndexedWord == null)
			{
				return false;
			}
			foreach (Pair<GrammaticalRelation, IndexedWord> child in enhancedDependency.ChildPairs(headIndexedWord))
			{
				if (child.First().GetShortName().Equals("cc"))
				{
					return true;
				}
			}
			return false;
		}

		private static IList<string> GetContextHelper<_T0>(IList<_T0> words)
			where _T0 : IAbstractCoreLabel
		{
			IList<IList<IAbstractCoreLabel>> namedEntities = Generics.NewArrayList();
			IList<IAbstractCoreLabel> ne = Generics.NewArrayList();
			string previousNEType = string.Empty;
			int previousNEIndex = -1;
			for (int i = 0; i < wSize; i++)
			{
				IAbstractCoreLabel word = words[i];
				if (word.Ner() != null && !word.Ner().Equals("O"))
				{
					if (!word.Ner().Equals(previousNEType) || previousNEIndex != i - 1)
					{
						// todo [cdm 2017]: What is the contract for this method? This looks buggy! The first entity found may be lost; a final empty one may be added....
						ne = Generics.NewArrayList();
						namedEntities.Add(ne);
					}
					ne.Add(word);
					previousNEType = word.Ner();
					previousNEIndex = i;
				}
			}
			IList<string> neStrings = new List<string>();
			ICollection<string> hs = Generics.NewHashSet();
			foreach (IList<IAbstractCoreLabel> namedEntity in namedEntities)
			{
				string ne_str = StringUtils.JoinWords(namedEntity, " ");
				hs.Add(ne_str);
			}
			Sharpen.Collections.AddAll(neStrings, hs);
			return neStrings;
		}

		public virtual IList<string> GetContext()
		{
			return GetContextHelper(sentenceWords);
		}

		public virtual IList<string> GetPremodifierContext()
		{
			IList<string> neStrings = new List<string>();
			foreach (IList<IndexedWord> words in GetPremodifiers())
			{
				Sharpen.Collections.AddAll(neStrings, GetContextHelper(words));
			}
			return neStrings;
		}

		/// <summary>Check relative pronouns</summary>
		public virtual bool IsRelativePronoun(Edu.Stanford.Nlp.Coref.Data.Mention m)
		{
			return relativePronouns != null && relativePronouns.Contains(m);
		}

		public virtual bool IsRoleAppositive(Edu.Stanford.Nlp.Coref.Data.Mention m, Dictionaries dict)
		{
			string thisString = this.SpanToString();
			string thisStringLower = this.LowercaseNormalizedSpanString();
			if (this.IsPronominal() || dict.allPronouns.Contains(thisStringLower))
			{
				return false;
			}
			if (!m.nerString.StartsWith("PER") && !m.nerString.Equals("O"))
			{
				return false;
			}
			if (!this.nerString.StartsWith("PER") && !this.nerString.Equals("O"))
			{
				return false;
			}
			if (!SameSentence(m) || !m.SpanToString().StartsWith(thisString))
			{
				return false;
			}
			if (m.SpanToString().Contains("'") || m.SpanToString().Contains(" and "))
			{
				return false;
			}
			if (!AnimaciesAgree(m) || this.animacy == Dictionaries.Animacy.Inanimate || this.gender == Dictionaries.Gender.Neutral || m.gender == Dictionaries.Gender.Neutral || !this.NumbersAgree(m))
			{
				return false;
			}
			if (dict.demonymSet.Contains(thisStringLower) || dict.demonymSet.Contains(m.LowercaseNormalizedSpanString()))
			{
				return false;
			}
			return true;
		}

		public virtual bool IsDemonym(Edu.Stanford.Nlp.Coref.Data.Mention m, Dictionaries dict)
		{
			string thisCasedString = this.SpanToString();
			string antCasedString = m.SpanToString();
			// The US state matching part (only) is done cased
			string thisNormed = dict.LookupCanonicalAmericanStateName(thisCasedString);
			string antNormed = dict.LookupCanonicalAmericanStateName(antCasedString);
			if (thisNormed != null && thisNormed.Equals(antNormed))
			{
				return true;
			}
			// The rest is done uncased
			string thisString = thisCasedString.ToLower(Locale.English);
			string antString = antCasedString.ToLower(Locale.English);
			if (thisString.StartsWith("the "))
			{
				thisString = Sharpen.Runtime.Substring(thisString, 4);
			}
			if (antString.StartsWith("the "))
			{
				antString = Sharpen.Runtime.Substring(antString, 4);
			}
			ICollection<string> thisDemonyms = dict.GetDemonyms(thisString);
			ICollection<string> antDemonyms = dict.GetDemonyms(antString);
			if (thisDemonyms.Contains(antString) || antDemonyms.Contains(thisString))
			{
				return true;
			}
			return false;
		}

		public virtual string GetPosition()
		{
			int size = sentenceWords.Count;
			if (headIndex == 0)
			{
				return "first";
			}
			else
			{
				if (headIndex == size - 1)
				{
					return "last";
				}
				else
				{
					if (headIndex > 0 && headIndex < size / 3)
					{
						return "begin";
					}
					else
					{
						if (headIndex >= size / 3 && headIndex < 2 * size / 3)
						{
							return "middle";
						}
						else
						{
							if (headIndex >= 2 * size / 3 && headIndex < size - 1)
							{
								return "end";
							}
						}
					}
				}
			}
			return null;
		}

		private IndexedWord headParent;

		private IndexedWord GetHeadParent()
		{
			return headParent == null ? (headParent = enhancedDependency.GetParent(headIndexedWord)) : headParent;
		}

		private ICollection<IndexedWord> headChildren;

		private ICollection<IndexedWord> GetHeadChildren()
		{
			return headChildren == null ? (headChildren = enhancedDependency.GetChildList(headIndexedWord)) : headChildren;
		}

		private ICollection<IndexedWord> headSiblings;

		private ICollection<IndexedWord> GetHeadSiblings()
		{
			return headSiblings == null ? (headSiblings = enhancedDependency.GetSiblings(headIndexedWord)) : headSiblings;
		}

		private IList<IndexedWord> headPathToRoot;

		private IList<IndexedWord> GetHeadPathToRoot()
		{
			return headPathToRoot == null ? (headPathToRoot = enhancedDependency.GetPathToRoot(headIndexedWord)) : headPathToRoot;
		}

		public virtual string GetRelation()
		{
			if (headIndexedWord == null)
			{
				return null;
			}
			if (enhancedDependency.GetRoots().IsEmpty())
			{
				return null;
			}
			// root relation
			if (enhancedDependency.GetFirstRoot().Equals(headIndexedWord))
			{
				return "root";
			}
			if (!enhancedDependency.ContainsVertex(GetHeadParent()))
			{
				return null;
			}
			GrammaticalRelation relation = enhancedDependency.Reln(GetHeadParent(), headIndexedWord);
			// adjunct relations
			if ((relation.ToString().StartsWith("nmod") && GetHeadChildren().Stream().AnyMatch(null)) || relation == UniversalEnglishGrammaticalRelations.TemporalModifier || relation == UniversalEnglishGrammaticalRelations.AdvClauseModifier || relation 
				== UniversalEnglishGrammaticalRelations.AdverbialModifier || relation == UniversalEnglishGrammaticalRelations.NominalModifier)
			{
				return "adjunct";
			}
			// subject relations
			if (relation == UniversalEnglishGrammaticalRelations.NominalSubject || relation == UniversalEnglishGrammaticalRelations.ClausalSubject)
			{
				return "subject";
			}
			if (relation == UniversalEnglishGrammaticalRelations.NominalPassiveSubject || relation == UniversalEnglishGrammaticalRelations.ClausalPassiveSubject)
			{
				return "subject";
			}
			// verbal argument relations
			if (relation == UniversalEnglishGrammaticalRelations.ClausalComplement || relation == UniversalEnglishGrammaticalRelations.XclausalComplement || relation == UniversalEnglishGrammaticalRelations.Agent || relation == UniversalEnglishGrammaticalRelations
				.DirectObject || relation == UniversalEnglishGrammaticalRelations.IndirectObject)
			{
				return "verbArg";
			}
			// noun argument relations
			if (relation == UniversalEnglishGrammaticalRelations.RelativeClauseModifier || relation == UniversalEnglishGrammaticalRelations.CompoundModifier || relation == UniversalEnglishGrammaticalRelations.AdjectivalModifier || relation == UniversalEnglishGrammaticalRelations
				.AppositionalModifier || relation == UniversalEnglishGrammaticalRelations.PossessionModifier)
			{
				//
				return "nounArg";
			}
			return null;
		}

		public virtual int GetModifiers(Dictionaries dict)
		{
			if (headIndexedWord == null)
			{
				return 0;
			}
			int count = 0;
			IList<Pair<GrammaticalRelation, IndexedWord>> childPairs = enhancedDependency.ChildPairs(headIndexedWord);
			foreach (Pair<GrammaticalRelation, IndexedWord> childPair in childPairs)
			{
				GrammaticalRelation gr = childPair.first;
				IndexedWord word = childPair.second;
				if (gr == UniversalEnglishGrammaticalRelations.AdjectivalModifier || gr == UniversalEnglishGrammaticalRelations.RelativeClauseModifier || gr.ToString().StartsWith("prep_"))
				{
					count++;
				}
				// add possessive if not a personal determiner
				if (gr == UniversalEnglishGrammaticalRelations.PossessionModifier && !dict.determiners.Contains(word.Lemma()))
				{
					count++;
				}
			}
			return count;
		}

		public virtual string GetQuantification(Dictionaries dict)
		{
			if (headIndexedWord == null)
			{
				return null;
			}
			if (!nerString.Equals("O"))
			{
				return "definite";
			}
			ICollection<IndexedWord> quant = enhancedDependency.GetChildrenWithReln(headIndexedWord, UniversalEnglishGrammaticalRelations.Determiner);
			ICollection<IndexedWord> poss = enhancedDependency.GetChildrenWithReln(headIndexedWord, UniversalEnglishGrammaticalRelations.PossessionModifier);
			if (!quant.IsEmpty())
			{
				foreach (IndexedWord word in quant)
				{
					string det = word.Lemma();
					if (dict.determiners.Contains(det))
					{
						return "definite";
					}
					else
					{
						if (dict.quantifiers2.Contains(det))
						{
							return "quantified";
						}
					}
				}
			}
			else
			{
				if (!poss.IsEmpty())
				{
					return "definite";
				}
				else
				{
					quant = enhancedDependency.GetChildrenWithReln(headIndexedWord, UniversalEnglishGrammaticalRelations.NumericModifier);
					if (!quant.IsEmpty())
					{
						return "quantified";
					}
				}
			}
			return "indefinite";
		}

		public virtual int GetNegation(Dictionaries dict)
		{
			if (headIndexedWord == null)
			{
				return 0;
			}
			// direct negation in a child
			ICollection<IndexedWord> children = enhancedDependency.GetChildren(headIndexedWord);
			foreach (IndexedWord child in children)
			{
				if (dict.negations.Contains(child.Lemma()))
				{
					return 1;
				}
			}
			// or has a sibling
			foreach (IndexedWord sibling in GetHeadSiblings())
			{
				if (dict.negations.Contains(sibling.Lemma()) && !enhancedDependency.HasParentWithReln(headIndexedWord, UniversalEnglishGrammaticalRelations.NominalSubject))
				{
					return 1;
				}
			}
			// check the parent
			IList<Pair<GrammaticalRelation, IndexedWord>> parentPairs = enhancedDependency.ParentPairs(headIndexedWord);
			if (!parentPairs.IsEmpty())
			{
				Pair<GrammaticalRelation, IndexedWord> parentPair = parentPairs[0];
				GrammaticalRelation gr = parentPair.first;
				// check negative prepositions
				if (dict.neg_relations.Contains(gr.ToString()))
				{
					return 1;
				}
			}
			return 0;
		}

		public virtual int GetModal(Dictionaries dict)
		{
			if (headIndexedWord == null)
			{
				return 0;
			}
			// direct modal in a child
			ICollection<IndexedWord> children = enhancedDependency.GetChildren(headIndexedWord);
			foreach (IndexedWord child in children)
			{
				if (dict.modals.Contains(child.Lemma()))
				{
					return 1;
				}
			}
			// check the parent
			IndexedWord parent = GetHeadParent();
			if (parent != null)
			{
				if (dict.modals.Contains(parent.Lemma()))
				{
					return 1;
				}
				// check the children of the parent (that is needed for modal auxiliaries)
				IndexedWord child_1 = enhancedDependency.GetChildWithReln(parent, UniversalEnglishGrammaticalRelations.AuxModifier);
				if (!enhancedDependency.HasParentWithReln(headIndexedWord, UniversalEnglishGrammaticalRelations.NominalSubject) && child_1 != null && dict.modals.Contains(child_1.Lemma()))
				{
					return 1;
				}
			}
			// look at the path to root
			IList<IndexedWord> path = GetHeadPathToRoot();
			if (path == null)
			{
				return 0;
			}
			foreach (IndexedWord word in path)
			{
				if (dict.modals.Contains(word.Lemma()))
				{
					return 1;
				}
			}
			return 0;
		}

		public virtual int GetReportEmbedding(Dictionaries dict)
		{
			if (headIndexedWord == null)
			{
				return 0;
			}
			// check adverbial clause with marker "as"
			foreach (IndexedWord sibling in GetHeadSiblings())
			{
				if (dict.reportVerb.Contains(sibling.Lemma()) && enhancedDependency.HasParentWithReln(sibling, UniversalEnglishGrammaticalRelations.AdvClauseModifier))
				{
					IndexedWord marker = enhancedDependency.GetChildWithReln(sibling, UniversalEnglishGrammaticalRelations.Marker);
					if (marker != null && marker.Lemma().Equals("as"))
					{
						return 1;
					}
				}
			}
			// look at the path to root
			IList<IndexedWord> path = GetHeadPathToRoot();
			if (path == null)
			{
				return 0;
			}
			bool isSubject = false;
			// if the node itself is a subject, we will not take into account its parent in the path
			if (enhancedDependency.HasParentWithReln(headIndexedWord, UniversalEnglishGrammaticalRelations.NominalSubject))
			{
				isSubject = true;
			}
			foreach (IndexedWord word in path)
			{
				if (!isSubject && (dict.reportVerb.Contains(word.Lemma()) || dict.reportNoun.Contains(word.Lemma())))
				{
					return 1;
				}
				// check how to put isSubject
				isSubject = enhancedDependency.HasParentWithReln(word, UniversalEnglishGrammaticalRelations.NominalSubject);
			}
			return 0;
		}

		public virtual int GetCoordination()
		{
			if (headIndexedWord == null)
			{
				return 0;
			}
			ICollection<GrammaticalRelation> relations = enhancedDependency.ChildRelns(headIndexedWord);
			foreach (GrammaticalRelation rel in relations)
			{
				if (rel.ToString().StartsWith("conj:"))
				{
					return 1;
				}
			}
			ICollection<GrammaticalRelation> parent_relations = enhancedDependency.Relns(headIndexedWord);
			foreach (GrammaticalRelation rel_1 in parent_relations)
			{
				if (rel_1.ToString().StartsWith("conj:"))
				{
					return 1;
				}
			}
			return 0;
		}

		public override bool Equals(object obj)
		{
			if (obj == this)
			{
				return true;
			}
			if (obj == null)
			{
				return false;
			}
			if (obj.GetType() != GetType())
			{
				return false;
			}
			Edu.Stanford.Nlp.Coref.Data.Mention rhs = (Edu.Stanford.Nlp.Coref.Data.Mention)obj;
			if (!Objects.Equals(mentionType, rhs.mentionType))
			{
				return false;
			}
			if (!Objects.Equals(number, rhs.number))
			{
				return false;
			}
			if (!Objects.Equals(gender, rhs.gender))
			{
				return false;
			}
			if (!Objects.Equals(animacy, rhs.animacy))
			{
				return false;
			}
			if (!Objects.Equals(person, rhs.person))
			{
				return false;
			}
			if (!Objects.Equals(headString, rhs.headString))
			{
				return false;
			}
			if (!Objects.Equals(nerString, rhs.nerString))
			{
				return false;
			}
			if (startIndex != rhs.startIndex)
			{
				return false;
			}
			if (endIndex != rhs.endIndex)
			{
				return false;
			}
			if (headIndex != rhs.headIndex)
			{
				return false;
			}
			if (mentionID != rhs.mentionID)
			{
				return false;
			}
			if (originalRef != rhs.originalRef)
			{
				return false;
			}
			if (!Objects.Equals(headIndexedWord, rhs.headIndexedWord))
			{
				return false;
			}
			if (!Objects.Equals(dependingVerb, rhs.dependingVerb))
			{
				return false;
			}
			if (!Objects.Equals(headWord, rhs.headWord))
			{
				return false;
			}
			if (goldCorefClusterID != rhs.goldCorefClusterID)
			{
				return false;
			}
			if (corefClusterID != rhs.corefClusterID)
			{
				return false;
			}
			if (mentionNum != rhs.mentionNum)
			{
				return false;
			}
			if (sentNum != rhs.sentNum)
			{
				return false;
			}
			if (utter != rhs.utter)
			{
				return false;
			}
			if (paragraph != rhs.paragraph)
			{
				return false;
			}
			if (isSubject != rhs.isSubject)
			{
				return false;
			}
			if (isDirectObject != rhs.isDirectObject)
			{
				return false;
			}
			if (isIndirectObject != rhs.isIndirectObject)
			{
				return false;
			}
			if (isPrepositionObject != rhs.isPrepositionObject)
			{
				return false;
			}
			if (hasTwin != rhs.hasTwin)
			{
				return false;
			}
			if (generic != rhs.generic)
			{
				return false;
			}
			if (isSingleton != rhs.isSingleton)
			{
				return false;
			}
			if (!Objects.Equals(originalSpan, rhs.originalSpan))
			{
				return false;
			}
			if (!Objects.Equals(sentenceWords, rhs.sentenceWords))
			{
				return false;
			}
			if (!Objects.Equals(basicDependency, rhs.basicDependency))
			{
				return false;
			}
			if (!Objects.Equals(enhancedDependency, rhs.enhancedDependency))
			{
				return false;
			}
			if (!Objects.Equals(contextParseTree, rhs.contextParseTree))
			{
				return false;
			}
			if (!Objects.Equals(dependents, rhs.dependents))
			{
				return false;
			}
			if (!Objects.Equals(preprocessedTerms, rhs.preprocessedTerms))
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result = 49;
			int c = 0;
			c += startIndex;
			c += endIndex;
			result = (37 * result) + c;
			return result;
		}
	}
}
