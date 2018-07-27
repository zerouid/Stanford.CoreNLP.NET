using System.Collections;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.IE.Pascal
{
	/// <summary>Maps non-background Pascal fields to strings.</summary>
	/// <author>Chris Cox</author>
	public class PascalTemplate
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Pascal.PascalTemplate));

		public static readonly string[] fields = new string[] { "workshoppapersubmissiondate", "workshopnotificationofacceptancedate", "workshopcamerareadycopydate", "workshopdate", "workshoplocation", "workshopacronym", "workshophomepage", "workshopname"
			, "conferenceacronym", "conferencehomepage", "conferencename", "0" };

		public const string BackgroundSymbol = "0";

		private static readonly IIndex<string> fieldIndices;

		static PascalTemplate()
		{
			//dates
			//location
			//workshop info
			//conference info
			//background symbol
			fieldIndices = new HashIndex<string>();
			foreach (string field in fields)
			{
				fieldIndices.Add(field);
			}
		}

		private readonly string[] values;

		public PascalTemplate()
		{
			values = new string[fields.Length];
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = null;
			}
		}

		public PascalTemplate(Edu.Stanford.Nlp.IE.Pascal.PascalTemplate pt)
		{
			//copy constructor
			this.values = new string[fields.Length];
			for (int i = 0; i < values.Length; i++)
			{
				if (pt.values[i] == null)
				{
					this.values[i] = null;
				}
				else
				{
					this.values[i] = pt.values[i];
				}
			}
		}

		private static Pattern acronymPattern = Pattern.Compile("([ \r-/a-zA-Z]+?)(?:[ -'*\t\r\n\f0-9]*)", Pattern.Dotall);

		/*
		* Acronym stemming and matching fields
		*/
		public static bool AcronymMatch(string s1, string s2, Hashtable stemmedAcronymIndex)
		{
			log.Info("Testing match:" + s1 + " : " + s2);
			string stem1 = (string)stemmedAcronymIndex[s1];
			string stem2 = (string)stemmedAcronymIndex[s2];
			log.Info("Got stems:" + s1 + " : " + s2);
			return stem1.Equals(stem2);
		}

		public static string StemAcronym(string s, CliqueTemplates ct)
		{
			if (ct.stemmedAcronymIndex.Contains(s))
			{
				return (string)ct.stemmedAcronymIndex[s];
			}
			Matcher matcher = acronymPattern.Matcher(s);
			if (!matcher.Matches() || Sharpen.Runtime.EqualsIgnoreCase(s, "www"))
			{
				log.Info("Not a valid acronym: " + s);
				return "null";
			}
			string stemmed = matcher.Group(1).ToLower();
			if (stemmed.EndsWith("-"))
			{
				stemmed = Sharpen.Runtime.Substring(stemmed, 0, stemmed.Length - 1);
			}
			ct.stemmedAcronymIndex[s] = stemmed;
			log.Info("Stemmed: " + s + " to: " + stemmed);
			if (ct.inverseAcronymMap.Contains(stemmed))
			{
				HashSet set = (HashSet)ct.inverseAcronymMap[stemmed];
				set.Add(s);
			}
			else
			{
				HashSet set = new HashSet();
				set.Add(s);
				ct.inverseAcronymMap[stemmed] = set;
			}
			return stemmed;
		}

		/// <summary>Merges partial (clique) templates into a full one.</summary>
		/// <param name="dt">date template</param>
		/// <param name="location">location</param>
		/// <param name="wi">workshop/conference info template</param>
		/// <returns>
		/// the
		/// <see cref="PascalTemplate"/>
		/// resulting from this merge.
		/// </returns>
		public static Edu.Stanford.Nlp.IE.Pascal.PascalTemplate MergeCliqueTemplates(DateTemplate dt, string location, InfoTemplate wi)
		{
			Edu.Stanford.Nlp.IE.Pascal.PascalTemplate pt = new Edu.Stanford.Nlp.IE.Pascal.PascalTemplate();
			pt.SetValue("workshopnotificationofacceptancedate", dt.noadate);
			pt.SetValue("workshopcamerareadycopydate", dt.crcdate);
			pt.SetValue("workshopdate", dt.workdate);
			pt.SetValue("workshoppapersubmissiondate", dt.subdate);
			pt.SetValue("workshoplocation", location);
			pt.SetValue("workshopacronym", wi.wacronym);
			pt.SetValue("workshophomepage", wi.whomepage);
			pt.SetValue("workshopname", wi.wname);
			pt.SetValue("conferenceacronym", wi.cacronym);
			pt.SetValue("conferencehomepage", wi.chomepage);
			pt.SetValue("conferencename", wi.cname);
			return pt;
		}

		/// <summary>Sets template values.</summary>
		/// <param name="fieldName">(i.e. workshopname, workshopdate)</param>
		public virtual void SetValue(string fieldName, string value)
		{
			int index = GetFieldIndex(fieldName);
			System.Diagnostics.Debug.Assert((index != -1));
			values[index] = value;
		}

		public virtual void SetValue(int index, string value)
		{
			if (index != values.Length - 1)
			{
				values[index] = value;
			}
		}

		public virtual string GetValue(string fieldName)
		{
			int i = GetFieldIndex(fieldName);
			if (i == -1 || i == values.Length - 1)
			{
				return null;
			}
			else
			{
				return values[i];
			}
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (!(obj is Edu.Stanford.Nlp.IE.Pascal.PascalTemplate))
			{
				return false;
			}
			Edu.Stanford.Nlp.IE.Pascal.PascalTemplate pt = (Edu.Stanford.Nlp.IE.Pascal.PascalTemplate)obj;
			string[] values2 = pt.values;
			if (values.Length != values2.Length)
			{
				return false;
			}
			for (int i = 0; i < values.Length - 1; i++)
			{
				if (values[i] == null)
				{
					if (values2[i] != null)
					{
						return false;
					}
				}
				else
				{
					if (values2[i] == null)
					{
						return false;
					}
					if (!values2[i].Equals(values[i]))
					{
						return false;
					}
				}
			}
			return true;
		}

		public override int GetHashCode()
		{
			int tally = 37;
			for (int i = 0; i < values.Length - 1; i++)
			{
				int n;
				if (values[i] == null)
				{
					n = 11;
				}
				else
				{
					n = values[i].GetHashCode();
				}
				tally = 17 * tally + n;
			}
			return tally;
		}

		/// <param name="tag">field name (i.e. workshopdate, workshoplocation)</param>
		/// <returns>
		/// the reference of that field in the underlying
		/// <see cref="Edu.Stanford.Nlp.Util.IIndex{E}"/>
		/// </returns>
		public static int GetFieldIndex(string tag)
		{
			return (fieldIndices.IndexOf(tag));
		}

		/// <summary>
		/// Should be passed a <code>Counter[]</code>, each entry of which
		/// keeps scores for possibilities in that template slot.
		/// </summary>
		/// <remarks>
		/// Should be passed a <code>Counter[]</code>, each entry of which
		/// keeps scores for possibilities in that template slot.  The counter
		/// for each template value is incremented by the corresponding score of
		/// this PascalTemplate.
		/// </remarks>
		/// <param name="fieldValueCounter">an array of counters, each of which holds label possibilities for one field</param>
		/// <param name="score">increment counts by this much.</param>
		public virtual void WriteToFieldValueCounter(ICounter<string>[] fieldValueCounter, double score)
		{
			for (int i = 0; i < fields.Length; i++)
			{
				if ((values[i] != null) && !values[i].Equals("NULL"))
				{
					fieldValueCounter[i].IncrementCount(values[i], score);
				}
			}
		}

		/// <summary>
		/// Divides this template into partial templates, and updates the counts of these
		/// partial templates in the
		/// <see cref="CliqueTemplates"/>
		/// object.
		/// </summary>
		/// <param name="ct">the partial templates counter object</param>
		/// <param name="score">increment counts by this much</param>
		public virtual void UnpackToCliqueTemplates(CliqueTemplates ct, double score)
		{
			ct.dateCliqueCounter.IncrementCount(new DateTemplate(values[0], values[1], values[2], values[3]), score);
			if (values[4] != null)
			{
				ct.locationCliqueCounter.IncrementCount(values[4], score);
			}
			ct.workshopInfoCliqueCounter.IncrementCount(new InfoTemplate(values[6], values[5], values[7], values[9], values[8], values[10], ct), score);
		}

		public virtual void Print()
		{
			log.Info("PascalTemplate: ");
			log.Info(this.ToString());
		}

		public override string ToString()
		{
			string str = "\n====================\n";
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i] != null)
				{
					if (!(Sharpen.Runtime.EqualsIgnoreCase(values[i], "NULL")))
					{
						str = str.Concat(fields[i] + " : " + values[i] + "\n");
					}
				}
			}
			return str;
		}
	}
}
