using System.Collections.Generic;



namespace Edu.Stanford.Nlp.Quoteattribution
{
	/// <summary>Created by michaelf on 12/20/15.</summary>
	public class Person
	{
		public enum Gender
		{
			Male,
			Female,
			Unk
		}

		public string name;

		public ICollection<string> aliases;

		public Person.Gender gender;

		public Person(string name, string gender, IList<string> aliases)
		{
			this.name = name;
			if (gender.ToLower().StartsWith("m"))
			{
				this.gender = Person.Gender.Male;
			}
			else
			{
				if (gender.ToLower().StartsWith("f"))
				{
					this.gender = Person.Gender.Female;
				}
				else
				{
					this.gender = Person.Gender.Unk;
				}
			}
			if (aliases != null)
			{
				this.aliases = new HashSet<string>(aliases);
			}
			else
			{
				this.aliases = new HashSet<string>();
			}
			this.aliases.Add(name);
		}

		public virtual bool Contains(string name)
		{
			return aliases.Contains(name);
		}
	}
}
