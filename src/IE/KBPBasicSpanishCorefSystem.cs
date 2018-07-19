using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.IE
{
	/// <summary>Perform basic coreference for Spanish</summary>
	public class KBPBasicSpanishCorefSystem
	{
		public const string NerPerson = "PERSON";

		public const string NerOrganization = "ORGANIZATION";

		private sealed class _HashSet_19 : HashSet<string>
		{
			public _HashSet_19()
			{
				{
					// From http://en.wikipedia.org/wiki/Types_of_companies#United_Kingdom
					this.Add("cic");
					this.Add("cio");
					this.Add("general partnership");
					this.Add("llp");
					this.Add("llp.");
					this.Add("limited liability partnership");
					this.Add("lp");
					this.Add("lp.");
					this.Add("limited partnership");
					this.Add("ltd");
					this.Add("ltd.");
					this.Add("plc");
					this.Add("plc.");
					this.Add("private company limited by guarantee");
					this.Add("unlimited company");
					this.Add("sole proprietorship");
					this.Add("sole trader");
					// From http://en.wikipedia.org/wiki/Types_of_companies#United_States
					this.Add("na");
					this.Add("nt&sa");
					this.Add("federal credit union");
					this.Add("federal savings bank");
					this.Add("lllp");
					this.Add("lllp.");
					this.Add("llc");
					this.Add("llc.");
					this.Add("lc");
					this.Add("lc.");
					this.Add("ltd");
					this.Add("ltd.");
					this.Add("co");
					this.Add("co.");
					this.Add("pllc");
					this.Add("pllc.");
					this.Add("corp");
					this.Add("corp.");
					this.Add("inc");
					this.Add("inc.");
					this.Add("pc");
					this.Add("p.c.");
					this.Add("dba");
					// From state requirements section
					this.Add("corporation");
					this.Add("incorporated");
					this.Add("limited");
					this.Add("association");
					this.Add("company");
					this.Add("clib");
					this.Add("syndicate");
					this.Add("institute");
					this.Add("fund");
					this.Add("foundation");
					this.Add("club");
					this.Add("partners");
				}
			}
		}

		public static readonly ICollection<string> CorporateSuffixes = Java.Util.Collections.UnmodifiableSet(new _HashSet_19());

		public virtual IList<CoreEntityMention> WrapEntityMentions(IList<ICoreMap> entityMentions)
		{
			return entityMentions.Stream().Map(null).Collect(Collectors.ToList());
		}

		/// <summary>A utility to strip out corporate titles (e.g., "corp.", "incorporated", etc.)</summary>
		/// <param name="input">The string to strip titles from</param>
		/// <returns>A string without these titles, or the input string if not such titles exist.</returns>
		protected internal virtual string StripCorporateTitles(string input)
		{
			foreach (string suffix in CorporateSuffixes)
			{
				if (input.ToLower().EndsWith(suffix))
				{
					return Sharpen.Runtime.Substring(input, 0, input.Length - suffix.Length).Trim();
				}
			}
			return input;
		}

		public virtual string NoSpecialChars(string original)
		{
			char[] chars = original.ToCharArray();
			// Compute the size of the output
			int size = 0;
			bool isAllLowerCase = true;
			foreach (char aChar in chars)
			{
				if (aChar != '\\' && aChar != '"' && aChar != '-')
				{
					if (isAllLowerCase && !char.IsLowerCase(aChar))
					{
						isAllLowerCase = false;
					}
					size += 1;
				}
			}
			if (size == chars.Length && isAllLowerCase)
			{
				return original;
			}
			// Copy to a new String
			char[] @out = new char[size];
			int i = 0;
			foreach (char aChar_1 in chars)
			{
				if (aChar_1 != '\\' && aChar_1 != '"' && aChar_1 != '-')
				{
					@out[i] = char.ToLowerCase(aChar_1);
					i += 1;
				}
			}
			// Return
			return new string(@out);
		}

		/// <summary>see if a potential mention is longer or same length and appears earlier</summary>
		public virtual bool MoreCanonicalMention(ICoreMap entityMention, ICoreMap potentialCanonicalMention)
		{
			// text of the mentions
			string entityMentionText = entityMention.Get(typeof(CoreAnnotations.TextAnnotation));
			string potentialCanonicalMentionText = potentialCanonicalMention.Get(typeof(CoreAnnotations.TextAnnotation));
			// start positions of mentions
			int entityMentionStart = entityMention.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			int potentialCanonicalMentionStart = potentialCanonicalMention.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			if (potentialCanonicalMentionText.Length > entityMentionText.Length)
			{
				return true;
			}
			else
			{
				if (potentialCanonicalMentionText.Length == entityMentionText.Length && potentialCanonicalMentionStart < entityMentionStart)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public virtual bool FirstNameMatch(string firstNameOne, string firstNameTwo)
		{
			return Math.Min(firstNameOne.Length, firstNameTwo.Length) >= 5 && StringUtils.LevenshteinDistance(firstNameOne, firstNameTwo) < 3;
		}

		protected internal virtual bool SameEntityWithoutLinking(CoreEntityMention emOne, CoreEntityMention emTwo)
		{
			string type = emOne.EntityType();
			if (type.Equals(NerPerson) && emOne.Tokens().Count >= 2 && emTwo.Tokens().Count >= 2 && emOne.Tokens()[emOne.Tokens().Count - 1].Word().ToLower().Equals(emTwo.Tokens()[emTwo.Tokens().Count - 1].Word().ToLower()))
			{
				string firstNameOne = emOne.Tokens()[0].Word().ToLower();
				string firstNameTwo = emTwo.Tokens()[0].Word().ToLower();
				if (FirstNameMatch(firstNameOne, firstNameTwo))
				{
					return true;
				}
				else
				{
					if (emOne.Tokens().Count == 2 && emTwo.Tokens().Count == 2)
					{
						return false;
					}
				}
			}
			// Proper match score
			double matchScore = Math.Max(ApproximateEntityMatchScore(emOne.Text(), emTwo.Text()), ApproximateEntityMatchScore(emTwo.Text(), emOne.Text()));
			// Some simple cases
			if (matchScore == 1.0)
			{
				return true;
			}
			if (matchScore < 0.34)
			{
				return false;
			}
			if (type.Equals(NerPerson) && matchScore > 0.49)
			{
				// Both entities are more than one character
				if (Math.Min(emOne.Text().Length, emTwo.Text().Length) > 1)
				{
					// Last names match
					if ((emOne.Tokens().Count == 1 && emTwo.Tokens().Count > 1 && Sharpen.Runtime.EqualsIgnoreCase(emTwo.Tokens()[emTwo.Tokens().Count - 1].Word(), emOne.Tokens()[0].Word())) || (emTwo.Tokens().Count == 1 && emOne.Tokens().Count > 1 && Sharpen.Runtime.EqualsIgnoreCase
						(emOne.Tokens()[emOne.Tokens().Count - 1].Word(), emTwo.Tokens()[0].Word())))
					{
						return true;
					}
					// First names match
					if ((emOne.Tokens().Count == 1 && emTwo.Tokens().Count > 1 && Sharpen.Runtime.EqualsIgnoreCase(emTwo.Tokens()[0].Word(), emOne.Tokens()[0].Word())) || (emTwo.Tokens().Count == 1 && emOne.Tokens().Count > 1 && Sharpen.Runtime.EqualsIgnoreCase
						(emOne.Tokens()[0].Word(), emTwo.Tokens()[0].Word())))
					{
						return true;
					}
				}
				if (matchScore > 0.65)
				{
					return true;
				}
			}
			if (type == NerOrganization && matchScore > 0.79)
			{
				return true;
			}
			return false;
		}

		private bool NearExactEntityMatch(string higherGloss, string lowerGloss)
		{
			// case: slots have same relation, and that relation isn't an alternate name
			// Filter case sensitive match
			if (Sharpen.Runtime.EqualsIgnoreCase(higherGloss, lowerGloss))
			{
				return true;
			}
			else
			{
				// Ignore certain characters
				if (Sharpen.Runtime.EqualsIgnoreCase(NoSpecialChars(higherGloss), NoSpecialChars(lowerGloss)))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Approximately check if two entities are equivalent.</summary>
		/// <remarks>
		/// Approximately check if two entities are equivalent.
		/// Taken largely from
		/// edu.stanford.nlp.kbp.slotfilling.evaluate,HeuristicSlotfillPostProcessors.NoDuplicatesApproximate;
		/// </remarks>
		public virtual double ApproximateEntityMatchScore(string higherGloss, string lowerGloss)
		{
			if (NearExactEntityMatch(higherGloss, lowerGloss))
			{
				return 1.0;
			}
			string[] higherToks = StripCorporateTitles(higherGloss).Split("\\s+");
			string[] lowerToks = StripCorporateTitles(lowerGloss).Split("\\s+");
			// Case: acronyms of each other
			if (AcronymMatcher.IsAcronym(higherToks, lowerToks))
			{
				return 1.0;
			}
			int match = 0;
			// Get number of matching tokens between the two slot fills
			bool[] matchedHigherToks = new bool[higherToks.Length];
			bool[] matchedLowerToks = new bool[lowerToks.Length];
			for (int h = 0; h < higherToks.Length; ++h)
			{
				if (matchedHigherToks[h])
				{
					continue;
				}
				string higherTok = higherToks[h];
				string higherTokNoSpecialChars = NoSpecialChars(higherTok);
				bool doesMatch = false;
				for (int l = 0; l < lowerToks.Length; ++l)
				{
					if (matchedLowerToks[l])
					{
						continue;
					}
					string lowerTok = lowerToks[l];
					string lowerTokNoSpecialCars = NoSpecialChars(lowerTok);
					int minLength = Math.Min(lowerTokNoSpecialCars.Length, higherTokNoSpecialChars.Length);
					if (Sharpen.Runtime.EqualsIgnoreCase(higherTokNoSpecialChars, lowerTokNoSpecialCars) || (minLength > 5 && (higherTokNoSpecialChars.EndsWith(lowerTokNoSpecialCars) || higherTokNoSpecialChars.StartsWith(lowerTokNoSpecialCars))) || (minLength >
						 5 && (lowerTokNoSpecialCars.EndsWith(higherTokNoSpecialChars) || lowerTokNoSpecialCars.StartsWith(higherTokNoSpecialChars))) || (minLength > 5 && StringUtils.LevenshteinDistance(lowerTokNoSpecialCars, higherTokNoSpecialChars) <= 1))
					{
						// equal
						// substring
						// substring the other way
						// edit distance <= 1
						doesMatch = true;
						// a loose metric of "same token"
						matchedHigherToks[h] = true;
						matchedLowerToks[l] = true;
					}
				}
				if (doesMatch)
				{
					match += 1;
				}
			}
			return (double)match / ((double)Math.Max(higherToks.Length, lowerToks.Length));
		}

		public virtual IList<IList<ICoreMap>> ClusterEntityMentions(IList<ICoreMap> entityMentions)
		{
			IList<CoreEntityMention> wrappedEntityMentions = WrapEntityMentions(entityMentions);
			List<List<CoreEntityMention>> entityMentionClusters = new List<List<CoreEntityMention>>();
			foreach (CoreEntityMention newEM in wrappedEntityMentions)
			{
				bool clusterMatch = false;
				foreach (List<CoreEntityMention> emCluster in entityMentionClusters)
				{
					foreach (CoreEntityMention clusterEM in emCluster)
					{
						if (SameEntityWithoutLinking(newEM, clusterEM))
						{
							emCluster.Add(newEM);
							clusterMatch = true;
							break;
						}
					}
					if (clusterMatch)
					{
						break;
					}
				}
				if (!clusterMatch)
				{
					List<CoreEntityMention> newCluster = new List<CoreEntityMention>();
					newCluster.Add(newEM);
					entityMentionClusters.Add(newCluster);
				}
			}
			IList<IList<ICoreMap>> coreMapEntityMentionClusters = new List<IList<ICoreMap>>();
			foreach (List<CoreEntityMention> emCluster_1 in entityMentionClusters)
			{
				IList<ICoreMap> coreMapCluster = emCluster_1.Stream().Map(null).Collect(Collectors.ToList());
				coreMapEntityMentionClusters.Add(coreMapCluster);
			}
			return coreMapEntityMentionClusters;
		}

		public virtual ICoreMap BestEntityMention(IList<ICoreMap> entityMentionCluster)
		{
			ICoreMap bestEntityMention = null;
			foreach (ICoreMap candidateEntityMention in entityMentionCluster)
			{
				if (bestEntityMention == null)
				{
					bestEntityMention = candidateEntityMention;
					continue;
				}
				else
				{
					if (candidateEntityMention.Get(typeof(CoreAnnotations.TextAnnotation)).Length > bestEntityMention.Get(typeof(CoreAnnotations.TextAnnotation)).Length)
					{
						bestEntityMention = candidateEntityMention;
						continue;
					}
					else
					{
						if (candidateEntityMention.Get(typeof(CoreAnnotations.TextAnnotation)).Length == bestEntityMention.Get(typeof(CoreAnnotations.TextAnnotation)).Length && candidateEntityMention.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) < bestEntityMention
							.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)))
						{
							bestEntityMention = candidateEntityMention;
							continue;
						}
					}
				}
			}
			return bestEntityMention;
		}

		public virtual IDictionary<ICoreMap, ICoreMap> CreateCanonicalMentionMap(IList<IList<ICoreMap>> entityMentionClusters)
		{
			IDictionary<ICoreMap, ICoreMap> canonicalMentionMap = new Dictionary<ICoreMap, ICoreMap>();
			foreach (IList<ICoreMap> entityMentionCluster in entityMentionClusters)
			{
				ICoreMap bestEntityMention = BestEntityMention(entityMentionCluster);
				foreach (ICoreMap clusterEntityMention in entityMentionCluster)
				{
					canonicalMentionMap[clusterEntityMention] = bestEntityMention;
				}
			}
			return canonicalMentionMap;
		}

		public virtual IDictionary<ICoreMap, ICoreMap> CanonicalMentionMapFromEntityMentions(IList<ICoreMap> entityMentions)
		{
			IList<IList<ICoreMap>> entityMentionClusters = ClusterEntityMentions(entityMentions);
			return CreateCanonicalMentionMap(entityMentionClusters);
		}
	}
}
