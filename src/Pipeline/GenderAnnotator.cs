using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>This class adds gender information (MALE / FEMALE) to entity mentions as GenderAnnotations.</summary>
	/// <remarks>
	/// This class adds gender information (MALE / FEMALE) to entity mentions as GenderAnnotations.
	/// The default is to use name lists from our KBP system.
	/// </remarks>
	/// <author>jebolton</author>
	public class GenderAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.GenderAnnotator));

		/// <summary>paths to lists of male and female first names</summary>
		public static string MaleFirstNamesPath = "edu/stanford/nlp/models/gender/male_first_names.txt";

		public static string FemaleFirstNamesPath = "edu/stanford/nlp/models/gender/female_first_names.txt";

		/// <summary>HashSets mapping names to potential genders</summary>
		public HashSet<string> maleNames = new HashSet<string>();

		public HashSet<string> femaleNames = new HashSet<string>();

		public virtual void LoadGenderNames(HashSet<string> genderSet, string filePath)
		{
			IList<string> nameFileEntries = IOUtils.LinesFromFile(filePath);
			foreach (string nameCSV in nameFileEntries)
			{
				string[] namesForThisLine = nameCSV.Split(",");
				foreach (string name in namesForThisLine)
				{
					genderSet.Add(name.ToLower());
				}
			}
		}

		public virtual void AnnotateEntityMention(ICoreMap entityMention, string gender)
		{
			// annotate the entity mention
			entityMention.Set(typeof(CoreAnnotations.GenderAnnotation), gender);
			// annotate each token of the entity mention
			foreach (CoreLabel token in entityMention.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				token.Set(typeof(CoreAnnotations.GenderAnnotation), gender);
			}
		}

		public GenderAnnotator(string annotatorName, Properties props)
		{
			// load the male and female names
			MaleFirstNamesPath = props.GetProperty("gender.maleNamesFile", MaleFirstNamesPath);
			FemaleFirstNamesPath = props.GetProperty("gender.femaleNamesFile", FemaleFirstNamesPath);
			LoadGenderNames(maleNames, MaleFirstNamesPath);
			LoadGenderNames(femaleNames, FemaleFirstNamesPath);
		}

		public virtual void Annotate(Annotation annotation)
		{
			// iterate through each sentence, iterate through each entity mention in the sentence
			foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (ICoreMap entityMention in sentence.Get(typeof(CoreAnnotations.MentionsAnnotation)))
				{
					// if the entityMention is of type PERSON, see if name is in one of the lists for male and female names
					// annotate the entity mention's CoreMap
					if (entityMention.Get(typeof(CoreAnnotations.EntityTypeAnnotation)).Equals("PERSON"))
					{
						CoreLabel firstName = entityMention.Get(typeof(CoreAnnotations.TokensAnnotation))[0];
						if (maleNames.Contains(firstName.Word().ToLower()))
						{
							AnnotateEntityMention(entityMention, "MALE");
						}
						else
						{
							if (femaleNames.Contains(firstName.Word().ToLower()))
							{
								AnnotateEntityMention(entityMention, "FEMALE");
							}
						}
					}
				}
			}
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.MentionsAnnotation
				), typeof(CoreAnnotations.EntityTypeAnnotation))));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.Singleton(typeof(CoreAnnotations.GenderAnnotation));
		}
	}
}
