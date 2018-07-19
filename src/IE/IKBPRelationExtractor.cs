using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Simple;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Text;
using Java.Util;
using Java.Util.Concurrent.Atomic;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.IE
{
	/// <summary>An interface for a KBP-style relation extractor</summary>
	/// <author>Gabor Angeli</author>
	public interface IKBPRelationExtractor
	{
		/// <summary>
		/// Classify the given sentence into the relation it expresses, with the associated
		/// confidence.
		/// </summary>
		Pair<string, double> Classify(KBPRelationExtractor.KBPInput input);

		/// <summary>A list of valid KBP NER tags.</summary>
		[System.Serializable]
		public sealed class NERTag
		{
			public static readonly KBPRelationExtractor.NERTag CauseOfDeath = new KBPRelationExtractor.NERTag("CAUSE_OF_DEATH", "COD", true);

			public static readonly KBPRelationExtractor.NERTag City = new KBPRelationExtractor.NERTag("CITY", "CIT", true);

			public static readonly KBPRelationExtractor.NERTag Country = new KBPRelationExtractor.NERTag("COUNTRY", "CRY", true);

			public static readonly KBPRelationExtractor.NERTag CriminalCharge = new KBPRelationExtractor.NERTag("CRIMINAL_CHARGE", "CC", true);

			public static readonly KBPRelationExtractor.NERTag Date = new KBPRelationExtractor.NERTag("DATE", "DT", false);

			public static readonly KBPRelationExtractor.NERTag Ideology = new KBPRelationExtractor.NERTag("IDEOLOGY", "IDY", true);

			public static readonly KBPRelationExtractor.NERTag Location = new KBPRelationExtractor.NERTag("LOCATION", "LOC", false);

			public static readonly KBPRelationExtractor.NERTag Misc = new KBPRelationExtractor.NERTag("MISC", "MSC", false);

			public static readonly KBPRelationExtractor.NERTag Modifier = new KBPRelationExtractor.NERTag("MODIFIER", "MOD", false);

			public static readonly KBPRelationExtractor.NERTag Nationality = new KBPRelationExtractor.NERTag("NATIONALITY", "NAT", true);

			public static readonly KBPRelationExtractor.NERTag Number = new KBPRelationExtractor.NERTag("NUMBER", "NUM", false);

			public static readonly KBPRelationExtractor.NERTag Organization = new KBPRelationExtractor.NERTag("ORGANIZATION", "ORG", false);

			public static readonly KBPRelationExtractor.NERTag Person = new KBPRelationExtractor.NERTag("PERSON", "PER", false);

			public static readonly KBPRelationExtractor.NERTag Religion = new KBPRelationExtractor.NERTag("RELIGION", "REL", true);

			public static readonly KBPRelationExtractor.NERTag StateOrProvince = new KBPRelationExtractor.NERTag("STATE_OR_PROVINCE", "ST", true);

			public static readonly KBPRelationExtractor.NERTag Title = new KBPRelationExtractor.NERTag("TITLE", "TIT", true);

			public static readonly KBPRelationExtractor.NERTag Url = new KBPRelationExtractor.NERTag("URL", "URL", true);

			public static readonly KBPRelationExtractor.NERTag Duration = new KBPRelationExtractor.NERTag("DURATION", "DUR", false);

			public static readonly KBPRelationExtractor.NERTag Gpe = new KBPRelationExtractor.NERTag("GPE", "GPE", false);

			/// <summary>The full name of this NER tag, as would come out of our NER or RegexNER system</summary>
			public readonly string name;

			/// <summary>A short name for this NER tag, intended for compact serialization</summary>
			public readonly string shortName;

			/// <summary>If true, this NER tag is not in the standard NER set, but is annotated via RegexNER</summary>
			public readonly bool isRegexNERType;

			internal NERTag(string name, string shortName, bool isRegexNERType)
			{
				// ENUM_NAME        NAME           SHORT_NAME  IS_REGEXNER_TYPE
				// note: these names must be upper case
				//       furthermore, DO NOT change the short names, or else serialization may break
				// note(chaganty): This NER tag is solely used in the cold-start system for entities.
				//  SCHOOL            ("SCHOOL",            "SCH", true),
				this.name = name;
				this.shortName = shortName;
				this.isRegexNERType = isRegexNERType;
			}

			/// <summary>Find the slot for a given name</summary>
			public static Optional<KBPRelationExtractor.NERTag> FromString(string name)
			{
				// Early termination
				if (StringUtils.IsNullOrEmpty(name))
				{
					return Optional.Empty();
				}
				// Cycle known NER tags
				name = name.ToUpper();
				foreach (KBPRelationExtractor.NERTag slot in KBPRelationExtractor.NERTag.Values())
				{
					if (slot.name.Equals(name))
					{
						return Optional.Of(slot);
					}
				}
				foreach (KBPRelationExtractor.NERTag slot_1 in KBPRelationExtractor.NERTag.Values())
				{
					if (slot_1.shortName.Equals(name))
					{
						return Optional.Of(slot_1);
					}
				}
				// Some quick fixes
				return Optional.Empty();
			}
		}

		/// <summary>Known relation types (last updated for the 2013 shared task).</summary>
		/// <remarks>
		/// Known relation types (last updated for the 2013 shared task).
		/// Note that changing the constants here can have far-reaching consequences in loading serialized
		/// models, and various bits of code that have been hard-coded to these relation types (e.g., the various
		/// consistency filters).
		/// <p>
		/// <i>Note:</i> Neither per:spouse, org:founded_by, or X:organizations_founded are SINGLE relations
		/// in the spec - these are made single here because our system otherwise over-predicts them.
		/// </p>
		/// </remarks>
		/// <author>Gabor Angeli</author>
		[System.Serializable]
		public sealed class RelationType
		{
			public static readonly KBPRelationExtractor.RelationType PerAlternateNames = new KBPRelationExtractor.RelationType("per:alternate_names", true, 10, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.NERTag.Misc }, new string[] { "NNP" }, 0.0353027270308107100);

			public static readonly KBPRelationExtractor.RelationType PerChildren = new KBPRelationExtractor.RelationType("per:children", true, 5, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP" }, 0.0058428110284504410);

			public static readonly KBPRelationExtractor.RelationType PerCitiesOfResidence = new KBPRelationExtractor.RelationType("per:cities_of_residence", true, 5, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, 
				new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.City }, new string[] { "NNP" }, 0.0136105679675116560);

			public static readonly KBPRelationExtractor.RelationType PerCityOfBirth = new KBPRelationExtractor.RelationType("per:city_of_birth", true, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.Single, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.City }, new string[] { "NNP" }, 0.0358146961159769100);

			public static readonly KBPRelationExtractor.RelationType PerCityOfDeath = new KBPRelationExtractor.RelationType("per:city_of_death", true, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.Single, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.City }, new string[] { "NNP" }, 0.0102003332137774650);

			public static readonly KBPRelationExtractor.RelationType PerCountriesOfResidence = new KBPRelationExtractor.RelationType("per:countries_of_residence", true, 5, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality
				.List, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Country }, new string[] { "NNP" }, 0.0107788293552082020);

			public static readonly KBPRelationExtractor.RelationType PerCountryOfBirth = new KBPRelationExtractor.RelationType("per:country_of_birth", true, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.Single, new 
				KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Country }, new string[] { "NNP" }, 0.0223444134627622040);

			public static readonly KBPRelationExtractor.RelationType PerCountryOfDeath = new KBPRelationExtractor.RelationType("per:country_of_death", true, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.Single, new 
				KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Country }, new string[] { "NNP" }, 0.0060626395621941200);

			public static readonly KBPRelationExtractor.RelationType PerEmployeeOf = new KBPRelationExtractor.RelationType("per:employee_of", true, 10, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.NERTag.Country, KBPRelationExtractor.NERTag.StateOrProvince, KBPRelationExtractor.NERTag.City }, new string[] { "NNP" }, 2.0335281901169719200);

			public static readonly KBPRelationExtractor.RelationType PerLocOfBirth = new KBPRelationExtractor.RelationType("per:LOCATION_of_birth", true, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.City, KBPRelationExtractor.NERTag.StateOrProvince, KBPRelationExtractor.NERTag.Country }, new string[] { "NNP" }, 0.0165825918941120660);

			public static readonly KBPRelationExtractor.RelationType PerLocOfDeath = new KBPRelationExtractor.RelationType("per:LOCATION_of_death", true, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.City, KBPRelationExtractor.NERTag.StateOrProvince, KBPRelationExtractor.NERTag.Country }, new string[] { "NNP" }, 0.0165825918941120660);

			public static readonly KBPRelationExtractor.RelationType PerLocOfResidence = new KBPRelationExtractor.RelationType("per:LOCATION_of_residence", true, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, 
				new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.StateOrProvince }, new string[] { "NNP" }, 0.0165825918941120660);

			public static readonly KBPRelationExtractor.RelationType PerMemberOf = new KBPRelationExtractor.RelationType("per:member_of", true, 10, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP" }, 0.0521716745149309900);

			public static readonly KBPRelationExtractor.RelationType PerOrigin = new KBPRelationExtractor.RelationType("per:origin", true, 10, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Nationality, KBPRelationExtractor.NERTag.Country }, new string[] { "NNP" }, 0.0069795559463618380);

			public static readonly KBPRelationExtractor.RelationType PerOtherFamily = new KBPRelationExtractor.RelationType("per:other_family", true, 5, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP" }, 2.7478566717959990E-5);

			public static readonly KBPRelationExtractor.RelationType PerParents = new KBPRelationExtractor.RelationType("per:parents", true, 5, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP" }, 0.0032222235077692030);

			public static readonly KBPRelationExtractor.RelationType PerSchoolsAttended = new KBPRelationExtractor.RelationType("per:schools_attended", true, 5, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, new 
				KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP" }, 0.0054696810172276150);

			public static readonly KBPRelationExtractor.RelationType PerSiblings = new KBPRelationExtractor.RelationType("per:siblings", true, 5, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP" }, 1.000000000000000e-99);

			public static readonly KBPRelationExtractor.RelationType PerSpouse = new KBPRelationExtractor.RelationType("per:spouse", true, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP" }, 0.0164075968113292680);

			public static readonly KBPRelationExtractor.RelationType PerStateOrProvincesOfBirth = new KBPRelationExtractor.RelationType("per:stateorprovince_of_birth", true, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality
				.Single, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.StateOrProvince }, new string[] { "NNP" }, 0.0165825918941120660);

			public static readonly KBPRelationExtractor.RelationType PerStateOrProvincesOfDeath = new KBPRelationExtractor.RelationType("per:stateorprovince_of_death", true, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality
				.Single, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.StateOrProvince }, new string[] { "NNP" }, 0.0050083303444366030);

			public static readonly KBPRelationExtractor.RelationType PerStateOrProvincesOfResidence = new KBPRelationExtractor.RelationType("per:stateorprovinces_of_residence", true, 5, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality
				.List, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.StateOrProvince }, new string[] { "NNP" }, 0.0066787379528178550);

			public static readonly KBPRelationExtractor.RelationType PerAge = new KBPRelationExtractor.RelationType("per:age", true, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.Single, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Number, KBPRelationExtractor.NERTag.Duration }, new string[] { "CD", "NN" }, 0.0483159977322951300);

			public static readonly KBPRelationExtractor.RelationType PerDateOfBirth = new KBPRelationExtractor.RelationType("per:date_of_birth", true, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.Single, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Date }, new string[] { "CD", "NN" }, 0.0743584477791533200);

			public static readonly KBPRelationExtractor.RelationType PerDateOfDeath = new KBPRelationExtractor.RelationType("per:date_of_death", true, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.Single, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Date }, new string[] { "CD", "NN" }, 0.0189819046406960460);

			public static readonly KBPRelationExtractor.RelationType PerCauseOfDeath = new KBPRelationExtractor.RelationType("per:cause_of_death", true, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.Single, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.CauseOfDeath }, new string[] { "NN" }, 1.0123682475037891E-5);

			public static readonly KBPRelationExtractor.RelationType PerCharges = new KBPRelationExtractor.RelationType("per:charges", true, 5, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.CriminalCharge }, new string[] { "NN" }, 3.8614617440501670E-4);

			public static readonly KBPRelationExtractor.RelationType PerReligion = new KBPRelationExtractor.RelationType("per:religion", true, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.Single, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Religion }, new string[] { "NN" }, 7.6650738739572610E-4);

			public static readonly KBPRelationExtractor.RelationType PerTitle = new KBPRelationExtractor.RelationType("per:title", true, 15, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Title, KBPRelationExtractor.NERTag.Modifier }, new string[] { "NN" }, 0.0334283995325751200);

			public static readonly KBPRelationExtractor.RelationType OrgAlternateNames = new KBPRelationExtractor.RelationType("org:alternate_names", true, 10, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality.List, 
				new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.NERTag.Misc }, new string[] { "NNP" }, 0.0552058867767352000);

			public static readonly KBPRelationExtractor.RelationType OrgCityOfHeadquarters = new KBPRelationExtractor.RelationType("org:city_of_headquarters", true, 3, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality
				.Single, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.City, KBPRelationExtractor.NERTag.Location }, new string[] { "NNP" }, 0.0555949254318473740);

			public static readonly KBPRelationExtractor.RelationType OrgCountryOfHeadquarters = new KBPRelationExtractor.RelationType("org:country_of_headquarters", true, 3, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality
				.Single, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Country, KBPRelationExtractor.NERTag.Nationality }, new string[] { "NNP" }, 0.0580217167451493100);

			public static readonly KBPRelationExtractor.RelationType OrgFoundedBy = new KBPRelationExtractor.RelationType("org:founded_by", true, 3, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP" }, 0.0050806423621154450);

			public static readonly KBPRelationExtractor.RelationType OrgLocOfHeadquarters = new KBPRelationExtractor.RelationType("org:LOCATION_of_headquarters", true, 10, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality
				.List, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.City, KBPRelationExtractor.NERTag.StateOrProvince, KBPRelationExtractor.NERTag.Country }, new string[] { "NNP" }, 0.0555949254318473740);

			public static readonly KBPRelationExtractor.RelationType OrgMemberOf = new KBPRelationExtractor.RelationType("org:member_of", true, 20, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.NERTag.StateOrProvince, KBPRelationExtractor.NERTag.Country }, new string[] { "NNP" }, 0.0396298781687126140);

			public static readonly KBPRelationExtractor.RelationType OrgMembers = new KBPRelationExtractor.RelationType("org:members", true, 20, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.NERTag.Country }, new string[] { "NNP" }, 0.0012220730987724312);

			public static readonly KBPRelationExtractor.RelationType OrgParents = new KBPRelationExtractor.RelationType("org:parents", true, 10, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP" }, 0.0550048593675880200);

			public static readonly KBPRelationExtractor.RelationType OrgPoliticalReligiousAffiliation = new KBPRelationExtractor.RelationType("org:political/religious_affiliation", true, 5, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality
				.List, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Ideology, KBPRelationExtractor.NERTag.Religion }, new string[] { "NN", "JJ" }, 0.0059266929689578970);

			public static readonly KBPRelationExtractor.RelationType OrgShareholders = new KBPRelationExtractor.RelationType("org:shareholders", true, 10, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality.List, new 
				KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP" }, 1.1569922828614734E-5);

			public static readonly KBPRelationExtractor.RelationType OrgStateOrProvincesOfHeadquarters = new KBPRelationExtractor.RelationType("org:stateorprovince_of_headquarters", true, 3, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality
				.Single, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.StateOrProvince }, new string[] { "NNP" }, 0.0312619314829170100);

			public static readonly KBPRelationExtractor.RelationType OrgSubsidiaries = new KBPRelationExtractor.RelationType("org:subsidiaries", true, 20, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality.List, new 
				KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP" }, 0.0162412791706679320);

			public static readonly KBPRelationExtractor.RelationType OrgTopMembersSlashEmployees = new KBPRelationExtractor.RelationType("org:top_members/employees", true, 10, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality
				.List, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP" }, 0.0907168724184609800);

			public static readonly KBPRelationExtractor.RelationType OrgDissolved = new KBPRelationExtractor.RelationType("org:dissolved", true, 3, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality.Single, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Date }, new string[] { "CD", "NN" }, 0.0023877428237553656);

			public static readonly KBPRelationExtractor.RelationType OrgFounded = new KBPRelationExtractor.RelationType("org:founded", true, 3, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality.Single, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Date }, new string[] { "CD", "NN" }, 0.0796314401082944800);

			public static readonly KBPRelationExtractor.RelationType OrgNumberOfEmployeesSlashMembers = new KBPRelationExtractor.RelationType("org:number_of_employees/members", true, 3, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality
				.Single, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Number }, new string[] { "CD", "NN" }, 0.0366274831946870950);

			public static readonly KBPRelationExtractor.RelationType OrgWebsite = new KBPRelationExtractor.RelationType("org:website", true, 3, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality.Single, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Url }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType OrgEmployees = new KBPRelationExtractor.RelationType("org:employees_or_members", false, 68, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality.List
				, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType GpeEmployees = new KBPRelationExtractor.RelationType("gpe:employees_or_members", false, 10, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType OrgStudents = new KBPRelationExtractor.RelationType("org:students", false, 50, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType GpeBirthsInCity = new KBPRelationExtractor.RelationType("gpe:births_in_city", false, 50, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType GpeBirthsInStateOrProvince = new KBPRelationExtractor.RelationType("gpe:births_in_stateorprovince", false, 50, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality
				.List, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType GpeBirthsInCountry = new KBPRelationExtractor.RelationType("gpe:births_in_country", false, 50, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality.List, new 
				KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType GpeResidentsInCity = new KBPRelationExtractor.RelationType("gpe:residents_of_city", false, 50, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality.List, new 
				KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType GpeResidentsInStateOrProvince = new KBPRelationExtractor.RelationType("gpe:residents_of_stateorprovince", false, 50, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality
				.List, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType GpeResidentsInCountry = new KBPRelationExtractor.RelationType("gpe:residents_of_country", false, 50, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality.List
				, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType GpeDeathsInCity = new KBPRelationExtractor.RelationType("gpe:deaths_in_city", false, 50, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType GpeDeathsInStateOrProvince = new KBPRelationExtractor.RelationType("gpe:deaths_in_stateorprovince", false, 50, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality
				.List, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType GpeDeathsInCountry = new KBPRelationExtractor.RelationType("gpe:deaths_in_country", false, 50, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality.List, new 
				KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType PerHoldsSharesIn = new KBPRelationExtractor.RelationType("per:holds_shares_in", false, 10, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType GpeHoldsSharesIn = new KBPRelationExtractor.RelationType("gpe:holds_shares_in", false, 10, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType OrgHoldsSharesIn = new KBPRelationExtractor.RelationType("org:holds_shares_in", false, 10, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality.List, 
				new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType PerOrganizationsFounded = new KBPRelationExtractor.RelationType("per:organizations_founded", false, 3, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality
				.List, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType GpeOrganizationsFounded = new KBPRelationExtractor.RelationType("gpe:organizations_founded", false, 3, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality.List
				, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType OrgOrganizationsFounded = new KBPRelationExtractor.RelationType("org:organizations_founded", false, 3, KBPRelationExtractor.NERTag.Organization, KBPRelationExtractor.RelationType.Cardinality
				.List, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType PerTopEmployeeOf = new KBPRelationExtractor.RelationType("per:top_member_employee_of", false, 5, KBPRelationExtractor.NERTag.Person, KBPRelationExtractor.RelationType.Cardinality.List, 
				new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType GpeMemberOf = new KBPRelationExtractor.RelationType("gpe:member_of", false, 10, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP" }, 0.0396298781687126140);

			public static readonly KBPRelationExtractor.RelationType GpeSubsidiaries = new KBPRelationExtractor.RelationType("gpe:subsidiaries", false, 10, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality.List, new KBPRelationExtractor.NERTag
				[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP" }, 0.0396298781687126140);

			public static readonly KBPRelationExtractor.RelationType GpeHeadquartersInCity = new KBPRelationExtractor.RelationType("gpe:headquarters_in_city", false, 50, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality.List
				, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType GpeHeadquartersInStateOrProvince = new KBPRelationExtractor.RelationType("gpe:headquarters_in_stateorprovince", false, 50, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality
				.List, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly KBPRelationExtractor.RelationType GpeHeadquartersInCountry = new KBPRelationExtractor.RelationType("gpe:headquarters_in_country", false, 50, KBPRelationExtractor.NERTag.Gpe, KBPRelationExtractor.RelationType.Cardinality
				.List, new KBPRelationExtractor.NERTag[] { KBPRelationExtractor.NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public enum Cardinality
			{
				Single,
				List
			}

			/// <summary>A canonical name for this relation type.</summary>
			/// <remarks>
			/// A canonical name for this relation type. This is the official 2010 relation name,
			/// that has since changed.
			/// </remarks>
			public readonly string canonicalName;

			/// <summary>If true, realtation was one of the original (non-inverse) KBP relation.</summary>
			public readonly bool isOriginalRelation;

			/// <summary>A guess of the maximum number of results to query for this relation.</summary>
			/// <remarks>
			/// A guess of the maximum number of results to query for this relation.
			/// Only really relevant for cold start.
			/// </remarks>
			public readonly int queryLimit;

			/// <summary>The entity type (left arg type) associated with this relation.</summary>
			/// <remarks>The entity type (left arg type) associated with this relation. That is, either a PERSON or an ORGANIZATION "slot".</remarks>
			public readonly KBPRelationExtractor.NERTag entityType;

			/// <summary>The cardinality of this entity.</summary>
			/// <remarks>The cardinality of this entity. That is, can multiple right arguments participate in this relation (born_in vs. lived_in)</remarks>
			public readonly KBPRelationExtractor.RelationType.Cardinality cardinality;

			/// <summary>Valid named entity labels for the right argument to this relation</summary>
			public readonly ICollection<KBPRelationExtractor.NERTag> validNamedEntityLabels;

			/// <summary>Valid POS [prefixes] for the right argument to this relation (e.g., can only take nouns, or can only take numbers, etc.)</summary>
			public readonly ICollection<string> validPOSPrefixes;

			/// <summary>The prior for how often this relation occurs in the training data.</summary>
			/// <remarks>
			/// The prior for how often this relation occurs in the training data.
			/// Note that this prior is not necessarily accurate for the test data.
			/// </remarks>
			public readonly double priorProbability;

			internal RelationType(string canonicalName, bool isOriginalRelation, int queryLimit, KBPRelationExtractor.NERTag type, KBPRelationExtractor.RelationType.Cardinality cardinality, KBPRelationExtractor.NERTag[] validNamedEntityLabels, string[] 
				validPOSPrefixes, double priorProbability)
			{
				// Inverse types
				this.canonicalName = canonicalName;
				this.isOriginalRelation = isOriginalRelation;
				this.queryLimit = queryLimit;
				this.entityType = type;
				this.cardinality = cardinality;
				this.validNamedEntityLabels = new HashSet<KBPRelationExtractor.NERTag>(Arrays.AsList(validNamedEntityLabels));
				this.validPOSPrefixes = new HashSet<string>(Arrays.AsList(validPOSPrefixes));
				this.priorProbability = priorProbability;
			}

			/// <summary>A small cache of names to relation types; we call fromString() a lot in the code, usually expecting it to be very fast</summary>
			private static readonly IDictionary<string, KBPRelationExtractor.RelationType> cachedFromString = new Dictionary<string, KBPRelationExtractor.RelationType>();

			/// <summary>Find the slot for a given name</summary>
			public static Optional<KBPRelationExtractor.RelationType> FromString(string name)
			{
				if (name == null)
				{
					return Optional.Empty();
				}
				string originalName = name;
				if (KBPRelationExtractor.RelationType.cachedFromString[name] != null)
				{
					return Optional.Of(KBPRelationExtractor.RelationType.cachedFromString[name]);
				}
				if (KBPRelationExtractor.RelationType.cachedFromString.Contains(name))
				{
					return Optional.Empty();
				}
				// Try naive
				foreach (KBPRelationExtractor.RelationType slot in KBPRelationExtractor.RelationType.Values())
				{
					if (slot.canonicalName.Equals(name) || slot.ToString().Equals(name))
					{
						KBPRelationExtractor.RelationType.cachedFromString[originalName] = slot;
						return Optional.Of(slot);
					}
				}
				// Replace slashes
				name = name.ToLower().ReplaceAll("[Ss][Ll][Aa][Ss][Hh]", "/");
				foreach (KBPRelationExtractor.RelationType slot_1 in KBPRelationExtractor.RelationType.Values())
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(slot_1.canonicalName, name))
					{
						KBPRelationExtractor.RelationType.cachedFromString[originalName] = slot_1;
						return Optional.Of(slot_1);
					}
				}
				KBPRelationExtractor.RelationType.cachedFromString[originalName] = null;
				return Optional.Empty();
			}

			/// <summary>Returns whether two entity types could plausibly have a relation hold between them.</summary>
			/// <remarks>
			/// Returns whether two entity types could plausibly have a relation hold between them.
			/// That is, is there a known relation type that would hold between these two entity types.
			/// </remarks>
			/// <param name="entityType">The NER tag of the entity.</param>
			/// <param name="slotValueType">The NER tag of the slot value.</param>
			/// <returns>True if there is a plausible relation which could occur between these two types.</returns>
			public static bool PlausiblyHasRelation(KBPRelationExtractor.NERTag entityType, KBPRelationExtractor.NERTag slotValueType)
			{
				foreach (KBPRelationExtractor.RelationType rel in KBPRelationExtractor.RelationType.Values())
				{
					if (rel.entityType == entityType && rel.validNamedEntityLabels.Contains(slotValueType))
					{
						return true;
					}
				}
				return false;
			}
		}

		public class KBPInput
		{
			public readonly Span subjectSpan;

			public readonly Span objectSpan;

			public readonly KBPRelationExtractor.NERTag subjectType;

			public readonly KBPRelationExtractor.NERTag objectType;

			public readonly Sentence sentence;

			public KBPInput(Span subjectSpan, Span objectSpan, KBPRelationExtractor.NERTag subjectType, KBPRelationExtractor.NERTag objectType, Sentence sentence)
			{
				this.subjectSpan = subjectSpan;
				this.objectSpan = objectSpan;
				this.subjectType = subjectType;
				this.objectType = objectType;
				this.sentence = sentence;
			}

			public virtual Sentence GetSentence()
			{
				return sentence;
			}

			public virtual Span GetSubjectSpan()
			{
				return subjectSpan;
			}

			public virtual string GetSubjectText()
			{
				return StringUtils.Join(sentence.OriginalTexts().SubList(subjectSpan.Start(), subjectSpan.End()).Stream(), " ");
			}

			public virtual Span GetObjectSpan()
			{
				return objectSpan;
			}

			public virtual string GetObjectText()
			{
				return StringUtils.Join(sentence.OriginalTexts().SubList(objectSpan.Start(), objectSpan.End()).Stream(), " ");
			}

			public override string ToString()
			{
				return "KBPInput{" + ", subjectSpan=" + subjectSpan + ", objectSpan=" + objectSpan + ", sentence=" + sentence + '}';
			}
		}

		/// <summary>Read a dataset from a CoNLL formatted input file</summary>
		/// <param name="conllInputFile">The input file, formatted as a TSV</param>
		/// <returns>A list of examples.</returns>
		/// <exception cref="System.IO.IOException"/>
		IList<Pair<KBPRelationExtractor.KBPInput, string>> ReadDataset(File conllInputFile);

		/// <summary>A class to compute the accuracy of a relation extractor.</summary>
		public class Accuracy
		{
			private class PerRelationStat : IComparable<KBPRelationExtractor.Accuracy.PerRelationStat>
			{
				public readonly string name;

				public readonly double precision;

				public readonly double recall;

				public readonly int predictedCount;

				public readonly int goldCount;

				public PerRelationStat(string name, double precision, double recall, int predictedCount, int goldCount)
				{
					// Case: read the relation
					// Case: read a token
					// do nothing
					// Case: commit a sentence
					// (clear the variables)
					this.name = name;
					this.precision = precision;
					this.recall = recall;
					this.predictedCount = predictedCount;
					this.goldCount = goldCount;
				}

				public virtual double F1()
				{
					if (precision == 0.0 && recall == 0.0)
					{
						return 0.0;
					}
					else
					{
						return 2.0 * precision * recall / (precision + recall);
					}
				}

				public virtual int CompareTo(KBPRelationExtractor.Accuracy.PerRelationStat o)
				{
					if (this.precision < o.precision)
					{
						return -1;
					}
					else
					{
						if (this.precision > o.precision)
						{
							return 1;
						}
						else
						{
							return 0;
						}
					}
				}

				public override string ToString()
				{
					DecimalFormat df = new DecimalFormat("0.00%");
					return "[" + name + "]  pred/gold: " + predictedCount + "/" + goldCount + "  P: " + df.Format(precision) + "  R: " + df.Format(recall) + "  F1: " + df.Format(F1());
				}
			}

			private ICounter<string> correctCount = new ClassicCounter<string>();

			private ICounter<string> predictedCount = new ClassicCounter<string>();

			private ICounter<string> goldCount = new ClassicCounter<string>();

			private ICounter<string> totalCount = new ClassicCounter<string>();

			public readonly ConfusionMatrix<string> confusion = new ConfusionMatrix<string>();

			public virtual void Predict(ICollection<string> predictedRelationsRaw, ICollection<string> goldRelationsRaw)
			{
				ICollection<string> predictedRelations = new HashSet<string>(predictedRelationsRaw);
				predictedRelations.Remove(KBPRelationExtractorConstants.NoRelation);
				ICollection<string> goldRelations = new HashSet<string>(goldRelationsRaw);
				goldRelations.Remove(KBPRelationExtractorConstants.NoRelation);
				// Register the prediction
				foreach (string pred in predictedRelations)
				{
					if (goldRelations.Contains(pred))
					{
						correctCount.IncrementCount(pred);
					}
					predictedCount.IncrementCount(pred);
				}
				goldRelations.ForEach(null);
				HashSet<string> allRelations = new HashSet<string>();
				Sharpen.Collections.AddAll(allRelations, predictedRelations);
				Sharpen.Collections.AddAll(allRelations, goldRelations);
				allRelations.ForEach(null);
				// Register the confusion matrix
				if (predictedRelations.Count == 1 && goldRelations.Count == 1)
				{
					confusion.Add(predictedRelations.GetEnumerator().Current, goldRelations.GetEnumerator().Current);
				}
				if (predictedRelations.Count == 1 && goldRelations.IsEmpty())
				{
					confusion.Add(predictedRelations.GetEnumerator().Current, "NR");
				}
				if (predictedRelations.IsEmpty() && goldRelations.Count == 1)
				{
					confusion.Add("NR", goldRelations.GetEnumerator().Current);
				}
			}

			public virtual double Precision(string relation)
			{
				if (predictedCount.GetCount(relation) == 0)
				{
					return 1.0;
				}
				return correctCount.GetCount(relation) / predictedCount.GetCount(relation);
			}

			public virtual double PrecisionMicro()
			{
				if (predictedCount.TotalCount() == 0)
				{
					return 1.0;
				}
				return correctCount.TotalCount() / predictedCount.TotalCount();
			}

			public virtual double PrecisionMacro()
			{
				double sumPrecision = 0.0;
				foreach (string rel in totalCount.KeySet())
				{
					sumPrecision += Precision(rel);
				}
				return sumPrecision / ((double)totalCount.Size());
			}

			public virtual double Recall(string relation)
			{
				if (goldCount.GetCount(relation) == 0)
				{
					return 0.0;
				}
				return correctCount.GetCount(relation) / goldCount.GetCount(relation);
			}

			public virtual double RecallMicro()
			{
				if (goldCount.TotalCount() == 0)
				{
					return 0.0;
				}
				return correctCount.TotalCount() / goldCount.TotalCount();
			}

			public virtual double RecallMacro()
			{
				double sumRecall = 0.0;
				foreach (string rel in totalCount.KeySet())
				{
					sumRecall += Recall(rel);
				}
				return sumRecall / ((double)totalCount.Size());
			}

			public virtual double F1(string relation)
			{
				return 2.0 * Precision(relation) * Recall(relation) / (Precision(relation) + Recall(relation));
			}

			public virtual double F1Micro()
			{
				return 2.0 * PrecisionMicro() * RecallMicro() / (PrecisionMicro() + RecallMicro());
			}

			public virtual double F1Macro()
			{
				return 2.0 * PrecisionMacro() * RecallMacro() / (PrecisionMacro() + RecallMacro());
			}

			public virtual void DumpPerRelationStats(TextWriter @out)
			{
				IList<KBPRelationExtractor.Accuracy.PerRelationStat> stats = goldCount.KeySet().Stream().Map(null).Collect(Collectors.ToList());
				stats.Sort();
				@out.WriteLine("Per-relation Accuracy");
				foreach (KBPRelationExtractor.Accuracy.PerRelationStat stat in stats)
				{
					@out.WriteLine(stat);
				}
			}

			public virtual void DumpPerRelationStats()
			{
				DumpPerRelationStats(System.Console.Out);
			}

			public virtual void ToString(TextWriter @out)
			{
				@out.WriteLine();
				@out.WriteLine("PRECISION (micro average): " + new DecimalFormat("0.000%").Format(PrecisionMicro()));
				@out.WriteLine("RECALL    (micro average): " + new DecimalFormat("0.000%").Format(RecallMicro()));
				@out.WriteLine("F1        (micro average): " + new DecimalFormat("0.000%").Format(F1Micro()));
				@out.WriteLine();
				@out.WriteLine("PRECISION (macro average): " + new DecimalFormat("0.000%").Format(PrecisionMacro()));
				@out.WriteLine("RECALL    (macro average): " + new DecimalFormat("0.000%").Format(RecallMacro()));
				@out.WriteLine("F1        (macro average): " + new DecimalFormat("0.000%").Format(F1Macro()));
				@out.WriteLine();
			}

			public override string ToString()
			{
				ByteArrayOutputStream bs = new ByteArrayOutputStream();
				TextWriter @out = new TextWriter(bs);
				ToString(@out);
				return bs.ToString();
			}

			/// <summary>A short, single line summary of the micro-precision/recall/f1.</summary>
			public virtual string ToOneLineString()
			{
				return "P: " + new DecimalFormat("0.000%").Format(PrecisionMicro()) + "  " + "R: " + new DecimalFormat("0.000%").Format(RecallMicro()) + "  " + "F1: " + new DecimalFormat("0.000%").Format(F1Micro());
			}
		}

		KBPRelationExtractor.Accuracy ComputeAccuracy(IStream<Pair<KBPRelationExtractor.KBPInput, string>> examples, Optional<TextWriter> predictOut);
	}

	public static class KBPRelationExtractorConstants
	{
		/// <summary>The special tag for no relation.</summary>
		public const string NoRelation = "no_relation";
	}
}
