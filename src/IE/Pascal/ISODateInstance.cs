using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.IE.Pascal
{
	/// <summary>
	/// Represents dates and times according to ISO8601 standard while also allowing for
	/// wild cards - e.g., can represent "21 June" without a year.
	/// </summary>
	/// <remarks>
	/// Represents dates and times according to ISO8601 standard while also allowing for
	/// wild cards - e.g., can represent "21 June" without a year.
	/// (Standard ISO8601 only allows removing less precise annotations (e.g.,
	/// 200706 rather than 20070621 but not a way to represent 0621 without a year.)
	/// Format stores date and time separately since the majority of current use
	/// cases involve only one of these items.  Standard ISO 8601 instead
	/// requires &lt;date&gt;T&lt;time&gt;.
	/// Ranges are specified within the strings via forward slash.  For example
	/// 6 June - 8 June is represented ****0606/****0608.  6 June onward is
	/// ****0606/ and until 8 June is /****0608.
	/// </remarks>
	/// <author>
	/// Anna Rafferty
	/// TODO: add time support - currently just dates are supported
	/// </author>
	public class ISODateInstance
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Pascal.ISODateInstance));

		private const bool Debug = false;

		private List<string> tokens = new List<string>();

		public const string OpenRangeAfter = "A";

		public const string OpenRangeBefore = "B";

		public const string BoundedRange = "C";

		public const string NoRange = string.Empty;

		public const int DayOfHalfMonth = 15;

		public const int LastDayOfMonth = 31;

		public const string MonthOfHalfYear = "07";

		public const string LastMonthOfYear = "12";

		/// <summary>
		/// String of the format
		/// <literal><year><month><day></literal>
		/// .  Representations
		/// by week are also allowed. If a more general field (such as year)
		/// is not specified when a less general one (such as month) is, the characters
		/// normally filled by the more general field are replaced by asterisks. For example,
		/// 21 June would be \"****0621\".  Less general fields are simply truncated;
		/// for example, June 2007 would be \"200706\".
		/// </summary>
		private string isoDate = string.Empty;

		private bool unparseable = false;

		/// <summary>
		/// Creates an empty date instance; you probably
		/// don't want this in most cases.
		/// </summary>
		public ISODateInstance()
		{
		}

		/// <summary>
		/// Takes a string that represents a date, and attempts to
		/// normalize it into ISO 8601-compatible format.
		/// </summary>
		public ISODateInstance(string date)
		{
			//each token contains some piece of the date, from our input.
			//close enough for our purposes
			//Variable for marking if we were unable to parse the string associated with this isoDate
			//private String isoTime = "";
			ExtractFields(date);
		}

		public ISODateInstance(string date, string openRangeMarker)
		{
			ExtractFields(date);
			//now process the range marker; if a range was found independently, we ignore the marker
			if (!Edu.Stanford.Nlp.IE.Pascal.ISODateInstance.NoRange.Equals(openRangeMarker) && !isoDate.Contains("/"))
			{
				if (Edu.Stanford.Nlp.IE.Pascal.ISODateInstance.OpenRangeAfter.Equals(openRangeMarker))
				{
					isoDate = isoDate + '/';
				}
				else
				{
					if (Edu.Stanford.Nlp.IE.Pascal.ISODateInstance.OpenRangeBefore.Equals(openRangeMarker))
					{
						isoDate = '/' + isoDate;
					}
				}
			}
		}

		/// <summary>Constructor for a range of dates, beginning at date start and finishing at date end</summary>
		public ISODateInstance(Edu.Stanford.Nlp.IE.Pascal.ISODateInstance start, Edu.Stanford.Nlp.IE.Pascal.ISODateInstance end)
		{
			string startString = start.GetDateString();
			if (start.IsRange())
			{
				startString = start.GetStartDate();
			}
			string endString = end.GetDateString();
			if (end.IsRange())
			{
				endString = end.GetEndDate();
			}
			isoDate = startString + '/' + endString;
			unparseable = (start.IsUnparseable() || end.IsUnparseable());
		}

		/// <summary>Construct a new ISODate based on its relation to a referenceDate.</summary>
		/// <remarks>
		/// Construct a new ISODate based on its relation to a referenceDate.
		/// relativeDate should be something like "today" or "tomorrow" or "last year"
		/// and the resulting ISODate will be the same as the referenceDate, a day later,
		/// or a year earlier, respectively.
		/// </remarks>
		public ISODateInstance(Edu.Stanford.Nlp.IE.Pascal.ISODateInstance referenceDate, string relativeDate)
		{
			Pair<ISODateInstance.DateField, int> relation = relativeDateMap[relativeDate.ToLower()];
			if (relation != null)
			{
				switch (relation.First())
				{
					case ISODateInstance.DateField.Day:
					{
						IncrementDay(referenceDate, relation);
						break;
					}

					case ISODateInstance.DateField.Month:
					{
						IncrementMonth(referenceDate, relation);
						break;
					}

					case ISODateInstance.DateField.Year:
					{
						IncrementYear(referenceDate, relation);
						break;
					}
				}
			}
		}

		private void IncrementYear(Edu.Stanford.Nlp.IE.Pascal.ISODateInstance referenceDate, Pair<ISODateInstance.DateField, int> relation)
		{
			string origDateString = referenceDate.GetStartDate();
			string yearString = Sharpen.Runtime.Substring(origDateString, 0, 4);
			if (yearString.Contains("*"))
			{
				isoDate = origDateString;
				return;
			}
			isoDate = MakeStringYearChange(origDateString, System.Convert.ToInt32(yearString) + relation.Second());
		}

		private void IncrementMonth(Edu.Stanford.Nlp.IE.Pascal.ISODateInstance referenceDate, Pair<ISODateInstance.DateField, int> relation)
		{
			string origDateString = referenceDate.GetStartDate();
			string monthString = Sharpen.Runtime.Substring(origDateString, 4, 6);
			if (monthString.Contains("*"))
			{
				isoDate = origDateString;
				return;
			}
			//Month is not a variable
			int monthNum = System.Convert.ToInt32(monthString);
			//Check if we're an edge case
			if (((monthNum + relation.Second()) > 12) || ((monthNum + relation.second) < 1))
			{
				bool decreasing = ((monthNum + relation.second) < 1);
				int newMonthNum = (monthNum + relation.Second()) % 12;
				if (newMonthNum < 0)
				{
					newMonthNum *= -1;
				}
				//Set the month appropriately
				isoDate = MakeStringMonthChange(origDateString, newMonthNum);
				//Increment the year if possible
				string yearString = Sharpen.Runtime.Substring(origDateString, 0, 4);
				if (!yearString.Contains("*"))
				{
					//How much we increment depends on above mod
					int numYearsToIncrement = (int)Math.Ceil(relation.Second() / 12.0);
					if (decreasing)
					{
						isoDate = MakeStringYearChange(isoDate, System.Convert.ToInt32(yearString) - numYearsToIncrement);
					}
					else
					{
						isoDate = MakeStringYearChange(isoDate, System.Convert.ToInt32(yearString) + numYearsToIncrement);
					}
				}
			}
			else
			{
				isoDate = MakeStringMonthChange(origDateString, (monthNum + relation.Second()));
			}
		}

		private void IncrementDay(Edu.Stanford.Nlp.IE.Pascal.ISODateInstance referenceDate, Pair<ISODateInstance.DateField, int> relation)
		{
			string origDateString = referenceDate.GetStartDate();
			string dayString = Sharpen.Runtime.Substring(origDateString, origDateString.Length - 2, origDateString.Length);
			if (dayString.Contains("*"))
			{
				isoDate = origDateString;
				return;
			}
			//Date is not a variable
			int dayNum = System.Convert.ToInt32(dayString);
			string monthString = Sharpen.Runtime.Substring(origDateString, origDateString.Length - 4, origDateString.Length - 2);
			int numDaysInMonth = 30;
			//default - assume this if month is a variable
			int monthNum = -1;
			//ie, we don't know the month yet - this remains -1 if the month is a variable
			if (!monthString.Contains("*"))
			{
				//Set appropriate numDaysInMonth and monthNum
				monthNum = System.Convert.ToInt32(monthString);
				numDaysInMonth = daysPerMonth[monthNum];
			}
			//Now, find out if we're an edge case (potential to increment month)
			if (dayNum + relation.Second() <= numDaysInMonth && dayNum + relation.Second() >= 1)
			{
				//Not an edge case - just increment the day, create a new string, and return
				dayNum += relation.Second();
				isoDate = MakeStringDayChange(origDateString, dayNum);
				return;
			}
			//Since we're an edge case, the month can't be a variable - if it is a variable, just set this to the reference string
			if (monthNum == -1)
			{
				isoDate = origDateString;
				return;
			}
			//At this point, neither our day nor our month is a variable
			isoDate = origDateString;
			bool decreasing = (dayNum + relation.Second() < 1);
			//Need to increment the month, set the date appropriately - we need the new month num to set the day appropriately, so do month first
			int newMonthNum;
			//Now, check if we're an edge case for month
			if ((monthNum + 1 > 12 && !decreasing) || (monthNum - 1 < 1 && decreasing))
			{
				//First, change the month
				if (decreasing)
				{
					newMonthNum = 12;
				}
				else
				{
					newMonthNum = 1;
				}
				//If we can, increment the year
				//TODO: fix this to work more nicely with variables and thus handle more cases
				string yearString = Sharpen.Runtime.Substring(origDateString, 0, 4);
				if (!yearString.Contains("*"))
				{
					if (decreasing)
					{
						isoDate = MakeStringYearChange(isoDate, System.Convert.ToInt32(yearString) - 1);
					}
					else
					{
						isoDate = MakeStringYearChange(isoDate, System.Convert.ToInt32(yearString) + 1);
					}
				}
			}
			else
			{
				//We're not an edge case for month - just increment
				if (decreasing)
				{
					newMonthNum = monthNum - 1;
				}
				else
				{
					newMonthNum = monthNum + 1;
				}
			}
			//do the increment
			isoDate = MakeStringMonthChange(isoDate, newMonthNum);
			int newDateNum;
			if (decreasing)
			{
				newDateNum = -relation.Second() + daysPerMonth[newMonthNum] - dayNum;
			}
			else
			{
				newDateNum = relation.Second() - dayNum + daysPerMonth[monthNum];
			}
			//Now, change the day in our original string to be appropriate
			isoDate = MakeStringDayChange(isoDate, newDateNum);
		}

		/// <summary>
		/// Changes the day portion of the origDate String to be the String
		/// value of newDay in two character format.
		/// </summary>
		/// <remarks>
		/// Changes the day portion of the origDate String to be the String
		/// value of newDay in two character format. (e.g., 9 -&gt; "09")
		/// </remarks>
		private static string MakeStringDayChange(string origDate, int newDay)
		{
			string newDayString = (newDay < 10 ? ("0" + newDay) : newDay.ToString());
			return Sharpen.Runtime.Substring(origDate, 0, origDate.Length - 2) + newDayString;
		}

		/// <summary>
		/// Changes the month portion of the origDate String to be the String
		/// value of newDay in two character format.
		/// </summary>
		/// <remarks>
		/// Changes the month portion of the origDate String to be the String
		/// value of newDay in two character format. (e.g., 9 -&gt; "09")
		/// </remarks>
		private static string MakeStringMonthChange(string origDate, int newMonth)
		{
			string newMonthString = (newMonth < 10 ? ("0" + newMonth) : newMonth.ToString());
			return Sharpen.Runtime.Substring(origDate, 0, 4) + newMonthString + Sharpen.Runtime.Substring(origDate, 6, 8);
		}

		/// <summary>
		/// Changes the year portion of the origDate String to be the String
		/// value of newDay in two character format.
		/// </summary>
		/// <remarks>
		/// Changes the year portion of the origDate String to be the String
		/// value of newDay in two character format. (e.g., 9 -&gt; "09")
		/// </remarks>
		private static string MakeStringYearChange(string origDate, int newYear)
		{
			string newYearString = newYear.ToString();
			while (newYearString.Length < 4)
			{
				newYearString = '0' + newYearString;
			}
			//we're compatible with year 1!
			return newYearString + Sharpen.Runtime.Substring(origDate, 4, origDate.Length);
		}

		/// <summary>Enum for the fields</summary>
		public enum DateField
		{
			Day,
			Month,
			Year
		}

		/// <summary>Map for mapping a relativeDate String to a pair with the field that should be modified and the amount to modify it</summary>
		public static readonly IDictionary<string, Pair<ISODateInstance.DateField, int>> relativeDateMap = Generics.NewHashMap();

		static ISODateInstance()
		{
			//Add entries to the relative datemap
			relativeDateMap["today"] = new Pair<ISODateInstance.DateField, int>(ISODateInstance.DateField.Day, 0);
			relativeDateMap["tomorrow"] = new Pair<ISODateInstance.DateField, int>(ISODateInstance.DateField.Day, 1);
			relativeDateMap["yesterday"] = new Pair<ISODateInstance.DateField, int>(ISODateInstance.DateField.Day, -1);
		}

		public static readonly IDictionary<int, int> daysPerMonth = Generics.NewHashMap();

		static ISODateInstance()
		{
			//Add month entries
			daysPerMonth[1] = 31;
			daysPerMonth[2] = 28;
			daysPerMonth[3] = 31;
			daysPerMonth[4] = 30;
			daysPerMonth[5] = 31;
			daysPerMonth[6] = 30;
			daysPerMonth[7] = 31;
			daysPerMonth[8] = 31;
			daysPerMonth[9] = 30;
			daysPerMonth[10] = 31;
			daysPerMonth[11] = 30;
			daysPerMonth[12] = 31;
		}

		/// <summary>
		/// Takes a string already formatted in ISODateInstance format
		/// (such as one previously written out using toString) and creates
		/// a new date instance from it
		/// </summary>
		public static Edu.Stanford.Nlp.IE.Pascal.ISODateInstance FromDateString(string date)
		{
			Edu.Stanford.Nlp.IE.Pascal.ISODateInstance d = new Edu.Stanford.Nlp.IE.Pascal.ISODateInstance();
			d.isoDate = date;
			return d;
		}

		public override string ToString()
		{
			return isoDate;
		}

		/// <summary>
		/// Provided for backwards compatibility with DateInstance;
		/// returns the same thing as toString()
		/// </summary>
		public virtual string GetDateString()
		{
			return this.ToString();
		}

		/// <summary>Uses regexp matching to match  month, day, and year fields.</summary>
		/// <remarks>
		/// Uses regexp matching to match  month, day, and year fields.
		/// TODO: Find a way to mark what's already been handled in the string
		/// </remarks>
		private bool ExtractFields(string inputDate)
		{
			if (tokens.Count < 2)
			{
				TokenizeDate(inputDate);
			}
			//first we see if it's a hyphen and two parseable dates - if not, we treat it as one date
			Pair<string, string> dateEndpoints = GetRangeDates(inputDate);
			if (dateEndpoints != null)
			{
				Edu.Stanford.Nlp.IE.Pascal.ISODateInstance date1 = new Edu.Stanford.Nlp.IE.Pascal.ISODateInstance(dateEndpoints.First());
				if (dateEndpoints.First().Contains(" ") && !dateEndpoints.Second().Contains(" "))
				{
					//consider whether it's a leading modifier; e.g., "June 8-10" will be split into June 8, and 10 when really we'd like June 8 and June 10
					string date = Sharpen.Runtime.Substring(dateEndpoints.First(), 0, dateEndpoints.First().IndexOf(' ')) + ' ' + dateEndpoints.Second();
					Edu.Stanford.Nlp.IE.Pascal.ISODateInstance date2 = new Edu.Stanford.Nlp.IE.Pascal.ISODateInstance(date);
					if (!date1.IsUnparseable() && !date2.IsUnparseable())
					{
						isoDate = (new Edu.Stanford.Nlp.IE.Pascal.ISODateInstance(date1, date2)).GetDateString();
						return true;
					}
				}
				Edu.Stanford.Nlp.IE.Pascal.ISODateInstance date2_1 = new Edu.Stanford.Nlp.IE.Pascal.ISODateInstance(dateEndpoints.Second());
				if (!date1.IsUnparseable() && !date2_1.IsUnparseable())
				{
					isoDate = (new Edu.Stanford.Nlp.IE.Pascal.ISODateInstance(date1, date2_1)).GetDateString();
					return true;
				}
			}
			if (ExtractYYYYMMDD(inputDate))
			{
				return true;
			}
			if (ExtractMMDDYY(inputDate))
			{
				return true;
			}
			bool passed = false;
			passed = ExtractYear(inputDate) || passed;
			passed = ExtractMonth(inputDate) || passed;
			passed = ExtractDay(inputDate) || passed;
			//slightly hacky, but check for some common modifiers that get grouped into the date
			passed = AddExtraRanges(inputDate) || passed;
			if (!passed)
			{
				//couldn't parse
				//try one more trick
				unparseable = true;
				bool weekday = ExtractWeekday(inputDate);
				if (!weekday)
				{
					isoDate = inputDate;
				}
			}
			return passed;
		}

		private static string[] rangeIndicators = new string[] { "--", "-" };

		/// <summary>Attempts to find the two sides of a range in the given string.</summary>
		/// <remarks>
		/// Attempts to find the two sides of a range in the given string.
		/// Uses rangeIndicators to find possible matches.
		/// </remarks>
		private static Pair<string, string> GetRangeDates(string inputDate)
		{
			foreach (string curIndicator in rangeIndicators)
			{
				string[] dates = inputDate.Split(curIndicator);
				if (dates.Length == 2)
				{
					return new Pair<string, string>(dates[0], dates[1]);
				}
			}
			return null;
		}

		private bool AddExtraRanges(string inputDate)
		{
			if (IsRange())
			{
				return false;
			}
			inputDate = inputDate.ToLower();
			if (inputDate.Contains("half"))
			{
				if (inputDate.Contains("first") && isoDate.Length <= 6)
				{
					string firstDate = isoDate + "01";
					string secondDate;
					if (isoDate.Length == 4)
					{
						//year
						secondDate = isoDate + MonthOfHalfYear;
					}
					else
					{
						//month
						secondDate = isoDate + DayOfHalfMonth;
					}
					isoDate = firstDate + '/' + secondDate;
					return true;
				}
				else
				{
					if (inputDate.Contains("second") && isoDate.Length <= 6)
					{
						string firstDate;
						string secondDate;
						if (isoDate.Length == 4)
						{
							//year
							firstDate = isoDate + MonthOfHalfYear;
							secondDate = isoDate + LastMonthOfYear;
							isoDate = firstDate + '/' + secondDate;
						}
						else
						{
							//month
							firstDate = isoDate + DayOfHalfMonth;
							secondDate = isoDate + LastDayOfMonth;
						}
						isoDate = firstDate + '/' + secondDate;
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Returns true iff this date represents a range
		/// The range must have at least a start or end
		/// date, but is not guaranteed to have both
		/// </summary>
		/// <returns>Whether this date represents a range</returns>
		public virtual bool IsRange()
		{
			if (unparseable)
			{
				return false;
			}
			return isoDate.Matches("/");
		}

		/// <summary>
		/// Returns true iff we were unable to parse the input
		/// String associated with this date; in that case,
		/// we just store the input string and shortcircuit
		/// all of the comparison methods
		/// </summary>
		public virtual bool IsUnparseable()
		{
			return unparseable;
		}

		/// <summary>
		/// Returns this date or if it is a range,
		/// the date the range starts.
		/// </summary>
		/// <remarks>
		/// Returns this date or if it is a range,
		/// the date the range starts.  If the date
		/// is of the form /&lt;date&gt;, "" is returned
		/// </remarks>
		/// <returns>Start date of range</returns>
		public virtual string GetStartDate()
		{
			if (!IsRange())
			{
				return isoDate;
			}
			if (isoDate.StartsWith("/"))
			{
				return string.Empty;
			}
			return isoDate.Split("/")[0];
		}

		/// <summary>
		/// Returns this date or if it is a range,
		/// the date the range ends.
		/// </summary>
		/// <remarks>
		/// Returns this date or if it is a range,
		/// the date the range ends.  If the date
		/// is of the form &lt;date&gt;/, "" is returned
		/// </remarks>
		/// <returns>End date of range</returns>
		public virtual string GetEndDate()
		{
			if (!IsRange())
			{
				return isoDate;
			}
			if (isoDate.EndsWith("/"))
			{
				return string.Empty;
			}
			string[] split = isoDate.Split("/");
			return split[split.Length - 1];
		}

		/* -------------------------- Static Comparison Methods -------------------------- */
		/// <summary>Returns true if date1 is after date2.</summary>
		/// <remarks>
		/// Returns true if date1 is after date2.
		/// Several tricky cases exist, and implementation tries to
		/// go with the common sense interpretation:
		/// When a year and a month are given for one, but only a month
		/// for the other, it is assumed that both have the same year
		/// e.g:
		/// ****12 is after 200211
		/// When a year and a month are given for one but only a year
		/// for the other, it is assumed that one of these is after the
		/// other only if the years differ, e.g.:
		/// 2003 is after 200211
		/// 2002 is not after 200211
		/// 200211 is not after 2002
		/// </remarks>
		/// <returns>Whether date2 is after date1</returns>
		internal static bool IsAfter(string date1, string date2)
		{
			if (!IsDateFormat(date1) || !IsDateFormat(date2))
			{
				return false;
			}
			bool after = true;
			//first check years
			string year = Sharpen.Runtime.Substring(date1, 0, 4);
			string yearOther = Sharpen.Runtime.Substring(date2, 0, 4);
			if (year.Contains("*") || yearOther.Contains("*"))
			{
				after = after && CheckWildcardCompatibility(year, yearOther);
			}
			else
			{
				if (System.Convert.ToInt32(year) > System.Convert.ToInt32(yearOther))
				{
					return true;
				}
				else
				{
					if (System.Convert.ToInt32(year) < System.Convert.ToInt32(yearOther))
					{
						return false;
					}
				}
			}
			if (date1.Length < 6 || date2.Length < 6)
			{
				if (year.Contains("*") || yearOther.Contains("*"))
				{
					return after;
				}
				else
				{
					return after && (System.Convert.ToInt32(year) != System.Convert.ToInt32(yearOther));
				}
			}
			//then check months
			string month = Sharpen.Runtime.Substring(date1, 4, 6);
			string monthOther = Sharpen.Runtime.Substring(date2, 4, 6);
			if (month.Contains("*") || monthOther.Contains("*"))
			{
				after = after && CheckWildcardCompatibility(month, monthOther);
			}
			else
			{
				if (System.Convert.ToInt32(month) > System.Convert.ToInt32(monthOther))
				{
					return true;
				}
				else
				{
					if (System.Convert.ToInt32(month) < System.Convert.ToInt32(monthOther))
					{
						return false;
					}
				}
			}
			if (date1.Length < 8 || date2.Length < 8)
			{
				if (month.Contains("*") || monthOther.Contains("*"))
				{
					return after;
				}
				else
				{
					return after && (System.Convert.ToInt32(month) != System.Convert.ToInt32(monthOther));
				}
			}
			//then check days
			string day = Sharpen.Runtime.Substring(date1, 6, 8);
			string dayOther = Sharpen.Runtime.Substring(date2, 6, 8);
			if (day.Contains("*") || dayOther.Contains("*"))
			{
				after = after && CheckWildcardCompatibility(day, dayOther);
			}
			else
			{
				if (System.Convert.ToInt32(day) > System.Convert.ToInt32(dayOther))
				{
					return true;
				}
				else
				{
					if (System.Convert.ToInt32(day) <= System.Convert.ToInt32(dayOther))
					{
						return false;
					}
				}
			}
			return after;
		}

		/// <summary>
		/// Right now, we say they're compatible iff one of them is all
		/// wildcards or they are equivalent
		/// </summary>
		private static bool CheckWildcardAfterCompatibility(string txt1, string txt2)
		{
			if (txt1.Length != txt2.Length)
			{
				return false;
			}
			for (int i = 0; i < txt1.Length; i++)
			{
				char t1 = txt1[i];
				char t2 = txt2[i];
				if (!(t1.Equals('*') || t2.Equals('*') || t1.Equals(t2)))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Returns true if the given txt contains only digits and "*" characters;
		/// false otherwise
		/// </summary>
		private static bool IsDateFormat(string txt)
		{
			string numberValue = txt.Replace("*", string.Empty);
			//remove wildcards
			try
			{
				System.Convert.ToInt32(numberValue);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <summary>
		/// Returns true iff date1 could represent the same value as date2
		/// e.g.
		/// </summary>
		/// <remarks>
		/// Returns true iff date1 could represent the same value as date2
		/// e.g.
		/// ****07 is compatible with 200207 (and 200207 is compatible with ****07)
		/// 200207 is compatible with 20020714 (?maybe need a better idea of use case here...)
		/// </remarks>
		public static bool IsCompatible(string date1, string date2)
		{
			bool compatible = true;
			//first check years
			compatible = compatible && IsYearCompatible(date1, date2);
			//then check months
			compatible = compatible && IsMonthCompatible(date1, date2);
			//then check days
			compatible = compatible && IsDayCompatible(date1, date2);
			return compatible;
		}

		/// <summary>
		/// Checks if the years represented by the two dates are compatible
		/// If either lacks a year, we return true.
		/// </summary>
		private static bool IsYearCompatible(string date1, string date2)
		{
			bool compatible = true;
			if (date1.Length < 4 || date2.Length < 4)
			{
				return compatible;
			}
			//first check years
			string year = Sharpen.Runtime.Substring(date1, 0, 4);
			string yearOther = Sharpen.Runtime.Substring(date2, 0, 4);
			if (year.Contains("*") || yearOther.Contains("*"))
			{
				compatible = compatible && CheckWildcardCompatibility(year, yearOther);
			}
			else
			{
				if (!year.Equals(yearOther))
				{
					return false;
				}
			}
			return compatible;
		}

		/// <summary>
		/// Checks if the months represented by the two dates are compatible
		/// If either lacks a month, we return true.
		/// </summary>
		private static bool IsMonthCompatible(string date1, string date2)
		{
			bool compatible = true;
			if (date1.Length < 6 || date2.Length < 6)
			{
				return compatible;
			}
			//then check months
			string month = Sharpen.Runtime.Substring(date1, 4, 6);
			string monthOther = Sharpen.Runtime.Substring(date2, 4, 6);
			if (month.Contains("*") || monthOther.Contains("*"))
			{
				compatible = (compatible && CheckWildcardCompatibility(month, monthOther));
			}
			else
			{
				if (!month.Equals(monthOther))
				{
					return false;
				}
			}
			return compatible;
		}

		/// <summary>
		/// Checks if the days represented by the two dates are compatible
		/// If either lacks a day, we return true.
		/// </summary>
		private static bool IsDayCompatible(string date1, string date2)
		{
			bool compatible = true;
			if (date1.Length < 8 || date2.Length < 8)
			{
				return compatible;
			}
			//then check days
			string day = Sharpen.Runtime.Substring(date1, 6, 8);
			string dayOther = Sharpen.Runtime.Substring(date2, 6, 8);
			if (day.Contains("*") || dayOther.Contains("*"))
			{
				compatible = compatible && CheckWildcardCompatibility(day, dayOther);
			}
			else
			{
				if (!day.Equals(dayOther))
				{
					return false;
				}
			}
			return compatible;
		}

		private static bool CheckWildcardCompatibility(string txt1, string txt2)
		{
			if (txt1.Length != txt2.Length)
			{
				return false;
			}
			for (int i = 0; i < txt1.Length; i++)
			{
				char t1 = txt1[i];
				char t2 = txt2[i];
				if (!(t1.Equals('*') || t2.Equals('*') || t1.Equals(t2)))
				{
					return false;
				}
			}
			return true;
		}

		/* -------------------------- Instance Comparison Methods -------------------------- */
		/// <summary>
		/// Returns true iff this date
		/// contains the date represented by other.
		/// </summary>
		/// <remarks>
		/// Returns true iff this date
		/// contains the date represented by other.
		/// A range contains a date if it
		/// is equal to or after the start date and equal to or
		/// before the end date.  For open ranges, contains
		/// is also inclusive of the one end point.
		/// </remarks>
		public virtual bool Contains(Edu.Stanford.Nlp.IE.Pascal.ISODateInstance other)
		{
			if (this.IsUnparseable() || other.IsUnparseable())
			{
				return this.isoDate.Equals(other.isoDate);
			}
			string start = this.GetStartDate();
			if (!start.Equals(string.Empty))
			{
				//we have a start date, need to make sure other is after it
				string startOther = other.GetStartDate();
				if (startOther.Equals(string.Empty))
				{
					return false;
				}
				else
				{
					//incompatible
					if (!IsAfter(startOther, start))
					{
						return false;
					}
				}
			}
			//now we've found out that the start date is appropriate, check the end date
			string end = this.GetEndDate();
			if (!end.IsEmpty())
			{
				string endOther = other.GetEndDate();
				if (endOther.IsEmpty())
				{
					return false;
				}
				else
				{
					if (!IsAfter(end, endOther))
					{
						return false;
					}
				}
			}
			return true;
		}

		//passes both start and end
		/// <summary>
		/// Returns true if this date instance is after
		/// the given dateString.
		/// </summary>
		/// <remarks>
		/// Returns true if this date instance is after
		/// the given dateString.  If this date instance
		/// is a range, then returns true only if both
		/// start and end dates are after dateString.
		/// Several tricky cases exist, and implementation tries to
		/// go with the commonsense interpretation:
		/// When a year and a month are given for one, but only a month
		/// for the other, it is assumed that both have the same year
		/// e.g:
		/// ****12 is after 200211
		/// When a year and a month are given for one but only a year
		/// for the other, it is assumed that one of these is after the
		/// other only if the years differ, e.g.:
		/// 2003 is after 200211
		/// 2002 is not after 200211
		/// 200211 is not after 2002
		/// </remarks>
		public virtual bool IsAfter(string dateString)
		{
			if (this.IsUnparseable())
			{
				return false;
			}
			if (!IsDateFormat(dateString))
			{
				return false;
			}
			return IsAfter(this.GetEndDate(), dateString);
		}

		public virtual bool IsCompatibleDate(Edu.Stanford.Nlp.IE.Pascal.ISODateInstance other)
		{
			if (this.IsUnparseable() || other.IsUnparseable())
			{
				return this.isoDate.Equals(other.isoDate);
			}
			//first see if either is a range
			if (this.IsRange())
			{
				return this.Contains(other);
			}
			else
			{
				if (other.IsRange())
				{
					return false;
				}
				else
				{
					//not compatible if other is range and this isn't
					return IsCompatible(isoDate, other.GetDateString());
				}
			}
		}

		/// <summary>Looks if the years for the two dates are compatible.</summary>
		/// <remarks>
		/// Looks if the years for the two dates are compatible.
		/// This method does not consider ranges and uses only the
		/// start date.
		/// </remarks>
		public virtual bool IsYearCompatible(Edu.Stanford.Nlp.IE.Pascal.ISODateInstance other)
		{
			if (this.IsUnparseable() || other.IsUnparseable())
			{
				return this.isoDate.Equals(other.isoDate);
			}
			return IsYearCompatible(isoDate, other.GetDateString());
		}

		/// <summary>Looks if the months for the two dates are compatible.</summary>
		/// <remarks>
		/// Looks if the months for the two dates are compatible.
		/// This method does not consider ranges and uses only the
		/// start date.
		/// </remarks>
		public virtual bool IsMonthCompatible(Edu.Stanford.Nlp.IE.Pascal.ISODateInstance other)
		{
			if (this.IsUnparseable() || other.IsUnparseable())
			{
				return this.isoDate.Equals(other.isoDate);
			}
			return IsMonthCompatible(isoDate, other.GetDateString());
		}

		/// <summary>Looks if the days for the two dates are compatible.</summary>
		/// <remarks>
		/// Looks if the days for the two dates are compatible.
		/// This method does not consider ranges and uses only the
		/// start date.
		/// </remarks>
		public virtual bool IsDayCompatible(Edu.Stanford.Nlp.IE.Pascal.ISODateInstance other)
		{
			if (this.IsUnparseable() || other.IsUnparseable())
			{
				return this.isoDate.Equals(other.isoDate);
			}
			return IsDayCompatible(isoDate, other.GetDateString());
		}

		/* -------------------------- Tokenization and Field Extraction -------------------------- */
		//These methods are taken directly from or modified slightly from {@link DateInstance}
		private void TokenizeDate(string inputDate)
		{
			tokens = new List<string>();
			Pattern pat = Pattern.Compile("[-]");
			if (inputDate == null)
			{
				System.Console.Out.WriteLine("Null input date");
			}
			Matcher m = pat.Matcher(inputDate);
			string str = m.ReplaceAll(" - ");
			str = str.ReplaceAll(",", " ");
			PTBTokenizer<Word> tokenizer = PTBTokenizer.NewPTBTokenizer(new BufferedReader(new StringReader(str)));
			while (tokenizer.MoveNext())
			{
				Word nextToken = tokenizer.Current;
				tokens.Add(nextToken.ToString());
			}
		}

		/// <summary>This method does YYYY-MM-DD style ISO date formats</summary>
		/// <returns>whether it worked.</returns>
		private bool ExtractYYYYMMDD(string inputDate)
		{
			Pattern pat = Pattern.Compile("([12][0-9]{3})[ /-]?([01]?[0-9])[ /-]([0-3]?[0-9])[ \t\r\n\f]*");
			Matcher m = pat.Matcher(inputDate);
			if (m.Matches())
			{
				string monthValue = m.Group(2);
				if (monthValue.Length < 2)
				{
					//we always use two digit months
					monthValue = '0' + monthValue;
				}
				string dayValue = m.Group(3);
				if (dayValue.Length < 2)
				{
					dayValue = '0' + dayValue;
				}
				string yearString = m.Group(1);
				isoDate = yearString + monthValue + dayValue;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Note: This method copied from
		/// <c>DateInstance</c>
		/// ; not sure how we tell that it
		/// is MMDD versus DDMM (sometimes it will be ambiguous).
		/// </summary>
		private bool ExtractMMDDYY(string inputDate)
		{
			Pattern pat = Pattern.Compile("([0-1]??[0-9])[ \t\n\r\f]*[/-][ \t\n\r\f]*([0-3]??[0-9])[ \t\r\n\f]*[/-][ \t\r\n\f]*([0-2]??[0-9]??[0-9][0-9])[ \t\r\n\f]*");
			Matcher m = pat.Matcher(inputDate);
			if (m.Matches())
			{
				string monthValue = m.Group(1);
				if (monthValue.Length < 2)
				{
					//we always use two digit months
					monthValue = '0' + monthValue;
				}
				string dayValue = m.Group(2);
				if (dayValue.Length < 2)
				{
					dayValue = '0' + dayValue;
				}
				string yearString;
				// always initialized below
				if (m.Group(3).Length == 2)
				{
					int yearInt = System.Convert.ToInt32(m.Group(3));
					//Now we add "20" or "19" to the front of the two digit year depending on its value....
					if (yearInt < 50)
					{
						yearString = "20" + m.Group(3);
					}
					else
					{
						yearString = "19" + m.Group(3);
					}
				}
				else
				{
					yearString = m.Group(3);
				}
				//lastYearSet = new Integer(yearString).intValue();
				isoDate = yearString + monthValue + dayValue;
				return true;
			}
			return false;
		}

		private Pattern re1 = Pattern.Compile("[1-2][0-9]{3}|'[0-9]{2}");

		private Pattern re2 = Pattern.Compile("[0-9][^0-9].*([0-9]{2})\\s*$");

		public virtual bool ExtractYear(string inputDate)
		{
			string extract;
			Matcher m1 = re1.Matcher(inputDate);
			Matcher m2 = re2.Matcher(inputDate);
			if (m1.Find())
			{
				extract = m1.Group(0);
			}
			else
			{
				if (m2.Find())
				{
					extract = m2.Group(1);
				}
				else
				{
					extract = FoundMiscYearPattern(inputDate);
					if (StringUtils.IsNullOrEmpty(extract))
					{
						isoDate = "****";
						return false;
					}
				}
			}
			if (!string.Empty.Equals(extract))
			{
				if (extract[0] == '\'')
				{
					extract = Sharpen.Runtime.Substring(extract, 1);
				}
				extract = extract.Trim();
				if (extract.Length == 2)
				{
					if (extract[0] < '5')
					{
						extract = "20" + extract;
					}
					else
					{
						extract = "19" + extract;
					}
				}
				if (inputDate[inputDate.Length - 1] == 's')
				{
					//decade or century marker
					if (extract[2] == '0')
					{
						//e.g., 1900s -> 1900/1999
						string endDate = int.ToString((System.Convert.ToInt32(extract) + 99));
						extract = extract + '/' + endDate;
					}
					else
					{
						//e.g., 1920s -> 1920/1929
						string endDate = int.ToString((System.Convert.ToInt32(extract) + 9));
						extract = extract + '/' + endDate;
					}
				}
				isoDate = extract;
				return true;
			}
			isoDate = "****";
			return false;
		}

		/// <summary>
		/// Tries to find a year pattern in the input string that may be somewhat
		/// odd/non-standard.
		/// </summary>
		private static string FoundMiscYearPattern(string inputDate)
		{
			string year = string.Empty;
			if (inputDate.ToLower().Contains("century"))
			{
				if (inputDate.EndsWith("A.D. "))
				{
					inputDate = Sharpen.Runtime.Substring(inputDate, 0, inputDate.Length - 5);
				}
				if (inputDate.StartsWith("late"))
				{
					inputDate = Sharpen.Runtime.Substring(inputDate, 5, inputDate.Length);
				}
				if (inputDate.StartsWith("early"))
				{
					inputDate = Sharpen.Runtime.Substring(inputDate, 6, inputDate.Length);
				}
				if (char.IsDigit(inputDate[0]))
				{
					// just parse number part, assuming last two letters are st/nd/rd
					year = QuantifiableEntityNormalizer.NormalizedNumberStringQuiet(Sharpen.Runtime.Substring(inputDate, 0, inputDate.Length - 2), 1, string.Empty, null);
					if (year == null)
					{
						year = string.Empty;
					}
					if (year.Contains("."))
					{
						//number format issue
						year = Sharpen.Runtime.Substring(year, 0, year.IndexOf('.'));
					}
					while (year.Length < 4)
					{
						year = year + '*';
					}
				}
				else
				{
					if (QuantifiableEntityNormalizer.ordinalsToValues.ContainsKey(inputDate))
					{
						year = double.ToString(QuantifiableEntityNormalizer.ordinalsToValues.GetCount(inputDate));
						while (year.Length < 4)
						{
							year = year + '*';
						}
					}
					else
					{
						year = string.Empty;
					}
				}
			}
			return year;
		}

		private static readonly Pattern[] extractorArray = new Pattern[] { Pattern.Compile("[Jj]anuary|JANUARY|[Jj]an\\.?|JAN\\.?"), Pattern.Compile("[Ff]ebruary|FEBRUARY|[Ff]eb\\.?|FEB\\.?"), Pattern.Compile("[Mm]arch|MARCH|[Mm]ar\\.?|MAR\\.?"), Pattern
			.Compile("[Aa]pril|APRIL|[Aa]pr\\.?|APR\\.?"), Pattern.Compile("[Mm]ay|MAY"), Pattern.Compile("[Jj]une|JUNE|[Jj]un\\.?|JUN\\.?"), Pattern.Compile("[Jj]uly|JULY|[Jj]ul\\.?|JUL\\.?"), Pattern.Compile("[Aa]ugust|AUGUST|[Aa]ug\\.?|AUG\\.?"), Pattern
			.Compile("[Ss]eptember|SEPTEMBER|[Ss]ept?\\.?|SEPT?\\.?"), Pattern.Compile("[Oo]ctober|OCTOBER|[Oo]ct\\.?|OCT\\.?"), Pattern.Compile("[Nn]ovember|NOVEMBER|[Nn]ov\\.?|NOV\\.?"), Pattern.Compile("[Dd]ecember|DECEMBER|[Dd]ec(?:\\.|[^aeiou]|$)|DEC(?:\\.|[^aeiou]|$)"
			) };

		// avoid matching "decades"!
		public virtual bool ExtractMonth(string inputDate)
		{
			bool foundMonth = false;
			for (int i = 0; i < 12; i++)
			{
				string extract = string.Empty;
				Matcher m = extractorArray[i].Matcher(inputDate);
				if (m.Find())
				{
					extract = m.Group(0);
				}
				if (!string.Empty.Equals(extract))
				{
					if (!foundMonth)
					{
						int monthNum = i + 1;
						if (isoDate.Length != 4)
						{
							isoDate = "****";
						}
						string month = (monthNum < 10) ? "0" + monthNum : monthNum.ToString();
						isoDate += month;
						foundMonth = true;
					}
				}
			}
			return foundMonth;
		}

		public virtual bool ExtractDay(string inputDate)
		{
			try
			{
				foreach (string extract in tokens)
				{
					if (QuantifiableEntityNormalizer.wordsToValues.ContainsKey(extract))
					{
						extract = int.ToString(double.ValueOf(QuantifiableEntityNormalizer.wordsToValues.GetCount(extract)));
					}
					else
					{
						if (QuantifiableEntityNormalizer.ordinalsToValues.ContainsKey(extract))
						{
							extract = int.ToString(double.ValueOf(QuantifiableEntityNormalizer.ordinalsToValues.GetCount(extract)));
						}
					}
					extract = extract.ReplaceAll("[^0-9]", string.Empty);
					if (!extract.IsEmpty())
					{
						long i = long.Parse(extract);
						if (i < 32L && i > 0L)
						{
							if (isoDate.Length < 6)
							{
								//should already have year and month
								if (isoDate.Length != 4)
								{
									//throw new RuntimeException("Error extracting dates; should have had month and year but didn't");
									isoDate = isoDate + "******";
								}
								else
								{
									isoDate = isoDate + "**";
								}
							}
							string day = (i < 10) ? "0" + i : i.ToString();
							isoDate = isoDate + day;
							return true;
						}
					}
				}
			}
			catch (NumberFormatException e)
			{
				log.Info("Exception in extract Day.");
				log.Info("tokens size :" + tokens.Count);
				Sharpen.Runtime.PrintStackTrace(e);
			}
			return false;
		}

		private static readonly Pattern[] weekdayArray = new Pattern[] { Pattern.Compile("[Ss]unday"), Pattern.Compile("[Mm]onday"), Pattern.Compile("[Tt]uesday"), Pattern.Compile("[Ww]ednesday"), Pattern.Compile("[Tt]hursday"), Pattern.Compile("[Ff]riday"
			), Pattern.Compile("[Ss]aturday") };

		/// <summary>This is a backup method if everything else fails.</summary>
		/// <remarks>
		/// This is a backup method if everything else fails.  It searches for named
		/// days of the week and if it finds one, it sets that as the date in lowercase form
		/// </remarks>
		public virtual bool ExtractWeekday(string inputDate)
		{
			foreach (Pattern p in weekdayArray)
			{
				Matcher m = p.Matcher(inputDate);
				if (m.Find())
				{
					string extract = m.Group(0);
					isoDate = extract.ToLower();
					return true;
				}
			}
			return false;
		}

		/// <summary>For testing only</summary>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			string dateProperty = props.GetProperty("date");
			if (dateProperty != null)
			{
				Edu.Stanford.Nlp.IE.Pascal.ISODateInstance d = new Edu.Stanford.Nlp.IE.Pascal.ISODateInstance(dateProperty);
				System.Console.Out.WriteLine(dateProperty + " processed as " + d);
			}
		}
	}
}
