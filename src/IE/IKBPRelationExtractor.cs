using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Simple;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;








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
		Pair<string, double> Classify(IKBPRelationExtractor.KBPInput input);
	}
}

namespace Edu.Stanford.Nlp.IE.IKBPRelationExtractor
{
		/// <summary>A list of valid KBP NER tags.</summary>
		[System.Serializable]
		public sealed class NERTag
		{
			public static readonly NERTag CauseOfDeath = new NERTag("CAUSE_OF_DEATH", "COD", true);
			public static readonly NERTag City = new NERTag("CITY", "CIT", true);
			public static readonly NERTag Country = new NERTag("COUNTRY", "CRY", true);
			public static readonly NERTag CriminalCharge = new NERTag("CRIMINAL_CHARGE", "CC", true);
			public static readonly NERTag Date = new NERTag("DATE", "DT", false);
			public static readonly NERTag Ideology = new NERTag("IDEOLOGY", "IDY", true);
			public static readonly NERTag Location = new NERTag("LOCATION", "LOC", false);
			public static readonly NERTag Misc = new NERTag("MISC", "MSC", false);
			public static readonly NERTag Modifier = new NERTag("MODIFIER", "MOD", false);
			public static readonly NERTag Nationality = new NERTag("NATIONALITY", "NAT", true);
			public static readonly NERTag Number = new NERTag("NUMBER", "NUM", false);
			public static readonly NERTag Organization = new NERTag("ORGANIZATION", "ORG", false);
			public static readonly NERTag Person = new NERTag("PERSON", "PER", false);
			public static readonly NERTag Religion = new NERTag("RELIGION", "REL", true);
			public static readonly NERTag StateOrProvince = new NERTag("STATE_OR_PROVINCE", "ST", true);
			public static readonly NERTag Title = new NERTag("TITLE", "TIT", true);
			public static readonly NERTag Url = new NERTag("URL", "URL", true);
			public static readonly NERTag Duration = new NERTag("DURATION", "DUR", false);
			public static readonly NERTag Gpe = new NERTag("GPE", "GPE", false);

			public static readonly IEnumerable<NERTag> Values = new [] { CauseOfDeath, City, Country, CriminalCharge, 
																		Date, Ideology, Location, Misc, Modifier, Nationality,
																		Number, Organization, Person, Religion, StateOrProvince,
																		Title, Url, Duration, Gpe};

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
			public static NERTag FromString(string name)
			{
				// Early termination
				if (StringUtils.IsNullOrEmpty(name))
				{
					return null;
				}
				// Cycle known NER tags
				name = name.ToUpper();
				foreach (NERTag slot in NERTag.Values)
				{
					if (slot.name.Equals(name))
					{
						return slot;
					}
				}
				foreach (NERTag slot_1 in NERTag.Values)
				{
					if (slot_1.shortName.Equals(name))
					{
						return slot_1;
					}
				}
				// Some quick fixes
				return null;
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
			public static readonly RelationType PerAlternateNames = new RelationType("per:alternate_names", true, 10, NERTag.Person, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Person, NERTag.Misc }, new string[] { "NNP" }, 0.0353027270308107100);

			public static readonly RelationType PerChildren = new RelationType("per:children", true, 5, NERTag.Person, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Person }, new string[] { "NNP" }, 0.0058428110284504410);

			public static readonly RelationType PerCitiesOfResidence = new RelationType("per:cities_of_residence", true, 5, NERTag.Person, RelationType.Cardinality.List, 
				new NERTag[] { NERTag.City }, new string[] { "NNP" }, 0.0136105679675116560);

			public static readonly RelationType PerCityOfBirth = new RelationType("per:city_of_birth", true, 3, NERTag.Person, RelationType.Cardinality.Single, new NERTag
				[] { NERTag.City }, new string[] { "NNP" }, 0.0358146961159769100);

			public static readonly RelationType PerCityOfDeath = new RelationType("per:city_of_death", true, 3, NERTag.Person, RelationType.Cardinality.Single, new NERTag
				[] { NERTag.City }, new string[] { "NNP" }, 0.0102003332137774650);

			public static readonly RelationType PerCountriesOfResidence = new RelationType("per:countries_of_residence", true, 5, NERTag.Person, RelationType.Cardinality
				.List, new NERTag[] { NERTag.Country }, new string[] { "NNP" }, 0.0107788293552082020);

			public static readonly RelationType PerCountryOfBirth = new RelationType("per:country_of_birth", true, 3, NERTag.Person, RelationType.Cardinality.Single, new 
				NERTag[] { NERTag.Country }, new string[] { "NNP" }, 0.0223444134627622040);

			public static readonly RelationType PerCountryOfDeath = new RelationType("per:country_of_death", true, 3, NERTag.Person, RelationType.Cardinality.Single, new 
				NERTag[] { NERTag.Country }, new string[] { "NNP" }, 0.0060626395621941200);

			public static readonly RelationType PerEmployeeOf = new RelationType("per:employee_of", true, 10, NERTag.Person, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Organization, NERTag.Country, NERTag.StateOrProvince, NERTag.City }, new string[] { "NNP" }, 2.0335281901169719200);

			public static readonly RelationType PerLocOfBirth = new RelationType("per:LOCATION_of_birth", true, 3, NERTag.Person, RelationType.Cardinality.List, new NERTag
				[] { NERTag.City, NERTag.StateOrProvince, NERTag.Country }, new string[] { "NNP" }, 0.0165825918941120660);

			public static readonly RelationType PerLocOfDeath = new RelationType("per:LOCATION_of_death", true, 3, NERTag.Person, RelationType.Cardinality.List, new NERTag
				[] { NERTag.City, NERTag.StateOrProvince, NERTag.Country }, new string[] { "NNP" }, 0.0165825918941120660);

			public static readonly RelationType PerLocOfResidence = new RelationType("per:LOCATION_of_residence", true, 3, NERTag.Person, RelationType.Cardinality.List, 
				new NERTag[] { NERTag.StateOrProvince }, new string[] { "NNP" }, 0.0165825918941120660);

			public static readonly RelationType PerMemberOf = new RelationType("per:member_of", true, 10, NERTag.Person, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Organization }, new string[] { "NNP" }, 0.0521716745149309900);

			public static readonly RelationType PerOrigin = new RelationType("per:origin", true, 10, NERTag.Person, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Nationality, NERTag.Country }, new string[] { "NNP" }, 0.0069795559463618380);

			public static readonly RelationType PerOtherFamily = new RelationType("per:other_family", true, 5, NERTag.Person, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Person }, new string[] { "NNP" }, 2.7478566717959990E-5);

			public static readonly RelationType PerParents = new RelationType("per:parents", true, 5, NERTag.Person, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Person }, new string[] { "NNP" }, 0.0032222235077692030);

			public static readonly RelationType PerSchoolsAttended = new RelationType("per:schools_attended", true, 5, NERTag.Person, RelationType.Cardinality.List, new 
				NERTag[] { NERTag.Organization }, new string[] { "NNP" }, 0.0054696810172276150);

			public static readonly RelationType PerSiblings = new RelationType("per:siblings", true, 5, NERTag.Person, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Person }, new string[] { "NNP" }, 1.000000000000000e-99);

			public static readonly RelationType PerSpouse = new RelationType("per:spouse", true, 3, NERTag.Person, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Person }, new string[] { "NNP" }, 0.0164075968113292680);

			public static readonly RelationType PerStateOrProvincesOfBirth = new RelationType("per:stateorprovince_of_birth", true, 3, NERTag.Person, RelationType.Cardinality
				.Single, new NERTag[] { NERTag.StateOrProvince }, new string[] { "NNP" }, 0.0165825918941120660);

			public static readonly RelationType PerStateOrProvincesOfDeath = new RelationType("per:stateorprovince_of_death", true, 3, NERTag.Person, RelationType.Cardinality
				.Single, new NERTag[] { NERTag.StateOrProvince }, new string[] { "NNP" }, 0.0050083303444366030);

			public static readonly RelationType PerStateOrProvincesOfResidence = new RelationType("per:stateorprovinces_of_residence", true, 5, NERTag.Person, RelationType.Cardinality
				.List, new NERTag[] { NERTag.StateOrProvince }, new string[] { "NNP" }, 0.0066787379528178550);

			public static readonly RelationType PerAge = new RelationType("per:age", true, 3, NERTag.Person, RelationType.Cardinality.Single, new NERTag
				[] { NERTag.Number, NERTag.Duration }, new string[] { "CD", "NN" }, 0.0483159977322951300);

			public static readonly RelationType PerDateOfBirth = new RelationType("per:date_of_birth", true, 3, NERTag.Person, RelationType.Cardinality.Single, new NERTag
				[] { NERTag.Date }, new string[] { "CD", "NN" }, 0.0743584477791533200);

			public static readonly RelationType PerDateOfDeath = new RelationType("per:date_of_death", true, 3, NERTag.Person, RelationType.Cardinality.Single, new NERTag
				[] { NERTag.Date }, new string[] { "CD", "NN" }, 0.0189819046406960460);

			public static readonly RelationType PerCauseOfDeath = new RelationType("per:cause_of_death", true, 3, NERTag.Person, RelationType.Cardinality.Single, new NERTag
				[] { NERTag.CauseOfDeath }, new string[] { "NN" }, 1.0123682475037891E-5);

			public static readonly RelationType PerCharges = new RelationType("per:charges", true, 5, NERTag.Person, RelationType.Cardinality.List, new NERTag
				[] { NERTag.CriminalCharge }, new string[] { "NN" }, 3.8614617440501670E-4);

			public static readonly RelationType PerReligion = new RelationType("per:religion", true, 3, NERTag.Person, RelationType.Cardinality.Single, new NERTag
				[] { NERTag.Religion }, new string[] { "NN" }, 7.6650738739572610E-4);

			public static readonly RelationType PerTitle = new RelationType("per:title", true, 15, NERTag.Person, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Title, NERTag.Modifier }, new string[] { "NN" }, 0.0334283995325751200);

			public static readonly RelationType OrgAlternateNames = new RelationType("org:alternate_names", true, 10, NERTag.Organization, RelationType.Cardinality.List, 
				new NERTag[] { NERTag.Organization, NERTag.Misc }, new string[] { "NNP" }, 0.0552058867767352000);

			public static readonly RelationType OrgCityOfHeadquarters = new RelationType("org:city_of_headquarters", true, 3, NERTag.Organization, RelationType.Cardinality
				.Single, new NERTag[] { NERTag.City, NERTag.Location }, new string[] { "NNP" }, 0.0555949254318473740);

			public static readonly RelationType OrgCountryOfHeadquarters = new RelationType("org:country_of_headquarters", true, 3, NERTag.Organization, RelationType.Cardinality
				.Single, new NERTag[] { NERTag.Country, NERTag.Nationality }, new string[] { "NNP" }, 0.0580217167451493100);

			public static readonly RelationType OrgFoundedBy = new RelationType("org:founded_by", true, 3, NERTag.Organization, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Person, NERTag.Organization }, new string[] { "NNP" }, 0.0050806423621154450);

			public static readonly RelationType OrgLocOfHeadquarters = new RelationType("org:LOCATION_of_headquarters", true, 10, NERTag.Organization, RelationType.Cardinality
				.List, new NERTag[] { NERTag.City, NERTag.StateOrProvince, NERTag.Country }, new string[] { "NNP" }, 0.0555949254318473740);

			public static readonly RelationType OrgMemberOf = new RelationType("org:member_of", true, 20, NERTag.Organization, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Organization, NERTag.StateOrProvince, NERTag.Country }, new string[] { "NNP" }, 0.0396298781687126140);

			public static readonly RelationType OrgMembers = new RelationType("org:members", true, 20, NERTag.Organization, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Organization, NERTag.Country }, new string[] { "NNP" }, 0.0012220730987724312);

			public static readonly RelationType OrgParents = new RelationType("org:parents", true, 10, NERTag.Organization, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Organization }, new string[] { "NNP" }, 0.0550048593675880200);

			public static readonly RelationType OrgPoliticalReligiousAffiliation = new RelationType("org:political/religious_affiliation", true, 5, NERTag.Organization, RelationType.Cardinality
				.List, new NERTag[] { NERTag.Ideology, NERTag.Religion }, new string[] { "NN", "JJ" }, 0.0059266929689578970);

			public static readonly RelationType OrgShareholders = new RelationType("org:shareholders", true, 10, NERTag.Organization, RelationType.Cardinality.List, new 
				NERTag[] { NERTag.Person, NERTag.Organization }, new string[] { "NNP" }, 1.1569922828614734E-5);

			public static readonly RelationType OrgStateOrProvincesOfHeadquarters = new RelationType("org:stateorprovince_of_headquarters", true, 3, NERTag.Organization, RelationType.Cardinality
				.Single, new NERTag[] { NERTag.StateOrProvince }, new string[] { "NNP" }, 0.0312619314829170100);

			public static readonly RelationType OrgSubsidiaries = new RelationType("org:subsidiaries", true, 20, NERTag.Organization, RelationType.Cardinality.List, new 
				NERTag[] { NERTag.Organization }, new string[] { "NNP" }, 0.0162412791706679320);

			public static readonly RelationType OrgTopMembersSlashEmployees = new RelationType("org:top_members/employees", true, 10, NERTag.Organization, RelationType.Cardinality
				.List, new NERTag[] { NERTag.Person }, new string[] { "NNP" }, 0.0907168724184609800);

			public static readonly RelationType OrgDissolved = new RelationType("org:dissolved", true, 3, NERTag.Organization, RelationType.Cardinality.Single, new NERTag
				[] { NERTag.Date }, new string[] { "CD", "NN" }, 0.0023877428237553656);

			public static readonly RelationType OrgFounded = new RelationType("org:founded", true, 3, NERTag.Organization, RelationType.Cardinality.Single, new NERTag
				[] { NERTag.Date }, new string[] { "CD", "NN" }, 0.0796314401082944800);

			public static readonly RelationType OrgNumberOfEmployeesSlashMembers = new RelationType("org:number_of_employees/members", true, 3, NERTag.Organization, RelationType.Cardinality
				.Single, new NERTag[] { NERTag.Number }, new string[] { "CD", "NN" }, 0.0366274831946870950);

			public static readonly RelationType OrgWebsite = new RelationType("org:website", true, 3, NERTag.Organization, RelationType.Cardinality.Single, new NERTag
				[] { NERTag.Url }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType OrgEmployees = new RelationType("org:employees_or_members", false, 68, NERTag.Organization, RelationType.Cardinality.List
				, new NERTag[] { NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType GpeEmployees = new RelationType("gpe:employees_or_members", false, 10, NERTag.Gpe, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType OrgStudents = new RelationType("org:students", false, 50, NERTag.Organization, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType GpeBirthsInCity = new RelationType("gpe:births_in_city", false, 50, NERTag.Gpe, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType GpeBirthsInStateOrProvince = new RelationType("gpe:births_in_stateorprovince", false, 50, NERTag.Gpe, RelationType.Cardinality
				.List, new NERTag[] { NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType GpeBirthsInCountry = new RelationType("gpe:births_in_country", false, 50, NERTag.Gpe, RelationType.Cardinality.List, new 
				NERTag[] { NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType GpeResidentsInCity = new RelationType("gpe:residents_of_city", false, 50, NERTag.Gpe, RelationType.Cardinality.List, new 
				NERTag[] { NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType GpeResidentsInStateOrProvince = new RelationType("gpe:residents_of_stateorprovince", false, 50, NERTag.Gpe, RelationType.Cardinality
				.List, new NERTag[] { NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType GpeResidentsInCountry = new RelationType("gpe:residents_of_country", false, 50, NERTag.Gpe, RelationType.Cardinality.List
				, new NERTag[] { NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType GpeDeathsInCity = new RelationType("gpe:deaths_in_city", false, 50, NERTag.Gpe, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType GpeDeathsInStateOrProvince = new RelationType("gpe:deaths_in_stateorprovince", false, 50, NERTag.Gpe, RelationType.Cardinality
				.List, new NERTag[] { NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType GpeDeathsInCountry = new RelationType("gpe:deaths_in_country", false, 50, NERTag.Gpe, RelationType.Cardinality.List, new 
				NERTag[] { NERTag.Person }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType PerHoldsSharesIn = new RelationType("per:holds_shares_in", false, 10, NERTag.Person, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType GpeHoldsSharesIn = new RelationType("gpe:holds_shares_in", false, 10, NERTag.Gpe, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType OrgHoldsSharesIn = new RelationType("org:holds_shares_in", false, 10, NERTag.Organization, RelationType.Cardinality.List, 
				new NERTag[] { NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType PerOrganizationsFounded = new RelationType("per:organizations_founded", false, 3, NERTag.Person, RelationType.Cardinality
				.List, new NERTag[] { NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType GpeOrganizationsFounded = new RelationType("gpe:organizations_founded", false, 3, NERTag.Gpe, RelationType.Cardinality.List
				, new NERTag[] { NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType OrgOrganizationsFounded = new RelationType("org:organizations_founded", false, 3, NERTag.Organization, RelationType.Cardinality
				.List, new NERTag[] { NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType PerTopEmployeeOf = new RelationType("per:top_member_employee_of", false, 5, NERTag.Person, RelationType.Cardinality.List, 
				new NERTag[] { NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType GpeMemberOf = new RelationType("gpe:member_of", false, 10, NERTag.Gpe, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Organization }, new string[] { "NNP" }, 0.0396298781687126140);

			public static readonly RelationType GpeSubsidiaries = new RelationType("gpe:subsidiaries", false, 10, NERTag.Gpe, RelationType.Cardinality.List, new NERTag
				[] { NERTag.Organization }, new string[] { "NNP" }, 0.0396298781687126140);

			public static readonly RelationType GpeHeadquartersInCity = new RelationType("gpe:headquarters_in_city", false, 50, NERTag.Gpe, RelationType.Cardinality.List
				, new NERTag[] { NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType GpeHeadquartersInStateOrProvince = new RelationType("gpe:headquarters_in_stateorprovince", false, 50, NERTag.Gpe, RelationType.Cardinality
				.List, new NERTag[] { NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly RelationType GpeHeadquartersInCountry = new RelationType("gpe:headquarters_in_country", false, 50, NERTag.Gpe, RelationType.Cardinality
				.List, new NERTag[] { NERTag.Organization }, new string[] { "NNP", "NN" }, 0.0051544006201478640);

			public static readonly IEnumerable<RelationType> Values = new [] { PerAlternateNames, PerChildren, PerCitiesOfResidence, PerCityOfBirth, PerCityOfDeath,
					PerCountriesOfResidence, PerCountryOfBirth, PerCountryOfDeath, PerEmployeeOf, PerLocOfBirth,
					PerLocOfDeath, PerLocOfResidence, PerMemberOf, PerOrigin, PerOtherFamily, PerParents,
					PerSchoolsAttended, PerSiblings, PerSpouse, PerStateOrProvincesOfBirth, PerStateOrProvincesOfDeath,
					PerStateOrProvincesOfResidence, PerAge, PerDateOfBirth, PerDateOfDeath, PerCauseOfDeath, PerCharges,
					PerReligion, PerTitle, OrgAlternateNames, OrgCityOfHeadquarters, OrgCountryOfHeadquarters, OrgFoundedBy,
					OrgLocOfHeadquarters, OrgMemberOf, OrgMembers, OrgParents, OrgPoliticalReligiousAffiliation,
					OrgShareholders, OrgStateOrProvincesOfHeadquarters, OrgSubsidiaries, OrgTopMembersSlashEmployees,
					OrgDissolved, OrgFounded, OrgNumberOfEmployeesSlashMembers, OrgWebsite, OrgEmployees, GpeEmployees,
					OrgStudents, GpeBirthsInCity, GpeBirthsInStateOrProvince, GpeBirthsInCountry, GpeResidentsInCity,
					GpeResidentsInStateOrProvince, GpeResidentsInCountry, GpeDeathsInCity, GpeDeathsInStateOrProvince,
					GpeDeathsInCountry, PerHoldsSharesIn, GpeHoldsSharesIn, OrgHoldsSharesIn, PerOrganizationsFounded,
					GpeOrganizationsFounded, OrgOrganizationsFounded, PerTopEmployeeOf, GpeMemberOf, GpeSubsidiaries,
					GpeHeadquartersInCity, GpeHeadquartersInStateOrProvince, GpeHeadquartersInCountry };
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
			public readonly NERTag entityType;

			/// <summary>The cardinality of this entity.</summary>
			/// <remarks>The cardinality of this entity. That is, can multiple right arguments participate in this relation (born_in vs. lived_in)</remarks>
			public readonly RelationType.Cardinality cardinality;

			/// <summary>Valid named entity labels for the right argument to this relation</summary>
			public readonly ICollection<NERTag> validNamedEntityLabels;

			/// <summary>Valid POS [prefixes] for the right argument to this relation (e.g., can only take nouns, or can only take numbers, etc.)</summary>
			public readonly ICollection<string> validPOSPrefixes;

			/// <summary>The prior for how often this relation occurs in the training data.</summary>
			/// <remarks>
			/// The prior for how often this relation occurs in the training data.
			/// Note that this prior is not necessarily accurate for the test data.
			/// </remarks>
			public readonly double priorProbability;

			internal RelationType(string canonicalName, bool isOriginalRelation, int queryLimit, NERTag type, RelationType.Cardinality cardinality, NERTag[] validNamedEntityLabels, string[] 
				validPOSPrefixes, double priorProbability)
			{
				// Inverse types
				this.canonicalName = canonicalName;
				this.isOriginalRelation = isOriginalRelation;
				this.queryLimit = queryLimit;
				this.entityType = type;
				this.cardinality = cardinality;
				this.validNamedEntityLabels = new HashSet<NERTag>(validNamedEntityLabels);
				this.validPOSPrefixes = new HashSet<string>(validPOSPrefixes);
				this.priorProbability = priorProbability;
			}

			/// <summary>A small cache of names to relation types; we call fromString() a lot in the code, usually expecting it to be very fast</summary>
			private static readonly IDictionary<string, RelationType> cachedFromString = new Dictionary<string, RelationType>();

			/// <summary>Find the slot for a given name</summary>
			public static RelationType FromString(string name)
			{
				if (name == null)
				{
					return null;
				}
				string originalName = name;
				if (RelationType.cachedFromString.ContainsKey(name))
				{
					return RelationType.cachedFromString[name];
				}

				// Try naive
				foreach (RelationType slot in RelationType.Values)
				{
					if (slot.canonicalName.Equals(name) || slot.ToString().Equals(name))
					{
						RelationType.cachedFromString[originalName] = slot;
						return slot;
					}
				}
				// Replace slashes
				name = Regex.Replace(name.ToLower(), "[Ss][Ll][Aa][Ss][Hh]", "/");
				foreach (RelationType slot_1 in RelationType.Values)
				{
					if (string.Equals(slot_1.canonicalName, name, StringComparison.OrdinalIgnoreCase))
					{
						RelationType.cachedFromString[originalName] = slot_1;
						return slot_1;
					}
				}
				RelationType.cachedFromString[originalName] = null;
				return null;
			}

			/// <summary>Returns whether two entity types could plausibly have a relation hold between them.</summary>
			/// <remarks>
			/// Returns whether two entity types could plausibly have a relation hold between them.
			/// That is, is there a known relation type that would hold between these two entity types.
			/// </remarks>
			/// <param name="entityType">The NER tag of the entity.</param>
			/// <param name="slotValueType">The NER tag of the slot value.</param>
			/// <returns>True if there is a plausible relation which could occur between these two types.</returns>
			public static bool PlausiblyHasRelation(NERTag entityType, NERTag slotValueType)
			{
				foreach (RelationType rel in RelationType.Values)
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

			public readonly NERTag subjectType;

			public readonly NERTag objectType;

			public readonly Sentence sentence;

			public KBPInput(Span subjectSpan, Span objectSpan, NERTag subjectType, NERTag objectType, Sentence sentence)
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
				return StringUtils.Join(sentence.OriginalTexts().Skip(subjectSpan.Start()).Take(subjectSpan.End() - subjectSpan.Start()), " ");
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
		IList<Pair<KBPInput, string>> ReadDataset(File conllInputFile);

		/// <summary>A class to compute the accuracy of a relation extractor.</summary>
		public class Accuracy
		{
			private class PerRelationStat : IComparable<Accuracy.PerRelationStat>
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

				public virtual int CompareTo(Accuracy.PerRelationStat o)
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
				IList<Accuracy.PerRelationStat> stats = goldCount.KeySet().Stream().Map(null).Collect(Collectors.ToList());
				stats.Sort();
				@out.WriteLine("Per-relation Accuracy");
				foreach (Accuracy.PerRelationStat stat in stats)
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

		Accuracy ComputeAccuracy(IEnumerable<Pair<KBPInput, string>> examples, Optional<TextWriter> predictOut);
	}

	public static class KBPRelationExtractorConstants
	{
		/// <summary>The special tag for no relation.</summary>
		public const string NoRelation = "no_relation";
	}
}
