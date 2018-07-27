using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;


using Org.W3c.Dom;


namespace Edu.Stanford.Nlp.Time
{
	/// <summary>Stores one TIMEX3 expression.</summary>
	/// <remarks>
	/// Stores one TIMEX3 expression.  This class is used for both TimeAnnotator and
	/// GUTimeAnnotator for storing information for TIMEX3 tags.
	/// <p>
	/// Example text with TIMEX3 annotation:<br />
	/// <code>In Washington &lt;TIMEX3 tid="t1" TYPE="DATE" VAL="PRESENT_REF"
	/// temporalFunction="true" valueFromFunction="tf1"
	/// anchorTimeID="t0"&gt;today&lt;/TIMEX3&gt;, the Federal Aviation Administration
	/// released air traffic control tapes from the night the TWA Flight eight
	/// hundred went down.
	/// </code>
	/// <p>
	/// <br />
	/// TIMEX3 specification:
	/// <br />
	/// <pre><code>
	/// attributes ::= tid type [functionInDocument] [beginPoint] [endPoint]
	/// [quant] [freq] [temporalFunction] (value | valueFromFunction)
	/// [mod] [anchorTimeID] [comment]
	/// tid ::= ID
	/// {tid ::= TimeID
	/// TimeID ::= t<integer>}
	/// type ::= 'DATE' | 'TIME' | 'DURATION' | 'SET'
	/// beginPoint ::= IDREF
	/// {beginPoint ::= TimeID}
	/// endPoint ::= IDREF
	/// {endPoint ::= TimeID}
	/// quant ::= CDATA
	/// freq ::= Duration
	/// functionInDocument ::= 'CREATION_TIME' | 'EXPIRATION_TIME' | 'MODIFICATION_TIME' |
	/// 'PUBLICATION_TIME' | 'RELEASE_TIME'| 'RECEPTION_TIME' |
	/// 'NONE' {default, if absent, is 'NONE'}
	/// temporalFunction ::= 'true' | 'false' {default, if absent, is 'false'}
	/// {temporalFunction ::= boolean}
	/// value ::= Duration | Date | Time | WeekDate | WeekTime | Season | PartOfYear | PaPrFu
	/// valueFromFunction ::= IDREF
	/// {valueFromFunction ::= TemporalFunctionID
	/// TemporalFunctionID ::= tf<integer>}
	/// mod ::= 'BEFORE' | 'AFTER' | 'ON_OR_BEFORE' | 'ON_OR_AFTER' |'LESS_THAN' | 'MORE_THAN' |
	/// 'EQUAL_OR_LESS' | 'EQUAL_OR_MORE' | 'START' | 'MID' | 'END' | 'APPROX'
	/// anchorTimeID ::= IDREF
	/// {anchorTimeID ::= TimeID}
	/// comment ::= CDATA
	/// </code></pre>
	/// <p>
	/// References
	/// <br />
	/// Guidelines: <a href="http://www.timeml.org/tempeval2/tempeval2-trial/guidelines/timex3guidelines-072009.pdf">
	/// http://www.timeml.org/tempeval2/tempeval2-trial/guidelines/timex3guidelines-072009.pdf</a>
	/// <br />
	/// Specifications: <a href="http://www.timeml.org/site/publications/timeMLdocs/timeml_1.2.1.html#timex3">
	/// http://www.timeml.org/site/publications/timeMLdocs/timeml_1.2.1.html#timex3</a>
	/// <br />
	/// XSD: <a href="http://www.timeml.org/timeMLdocs/TimeML.xsd">http://www.timeml.org/timeMLdocs/TimeML.xsd</a>
	/// </remarks>
	[System.Serializable]
	public class Timex
	{
		private const long serialVersionUID = 385847729549981302L;

		/// <summary>XML representation of the TIMEX tag</summary>
		private string xml;

		/// <summary>TIMEX3 value attribute - Time value (given in extended ISO 8601 format).</summary>
		private string val;

		/// <summary>Alternate representation for time value (not part of TIMEX3 standard).</summary>
		/// <remarks>
		/// Alternate representation for time value (not part of TIMEX3 standard).
		/// used when value of the time expression cannot be expressed as a standard TIMEX3 value.
		/// </remarks>
		private string altVal;

		/// <summary>Actual text that make up the time expression</summary>
		private string text;

		/// <summary>TIMEX3 type attribute - Type of the time expression (DATE, TIME, DURATION, or SET)</summary>
		private string type;

		/// <summary>TIMEX3 tid attribute - TimeID.</summary>
		/// <remarks>
		/// TIMEX3 tid attribute - TimeID.  ID to identify this time expression.
		/// Should have the format of
		/// <c>t&lt;integer&gt;</c>
		/// </remarks>
		private string tid;

		/// <summary>
		/// TIMEX3 beginPoint attribute - integer indicating the TimeID of the begin time
		/// that anchors this duration/range (-1 is not present).
		/// </summary>
		private int beginPoint;

		/// <summary>
		/// TIMEX3 beginPoint attribute - integer indicating the TimeID of the end time
		/// that anchors this duration/range (-1 is not present).
		/// </summary>
		private int endPoint;

		/// <summary>
		/// Range begin/end/duration
		/// (this is not part of the timex standard and is typically null, available if sutime.includeRange is true)
		/// </summary>
		private Timex.Range range;

		[System.Serializable]
		public class Range
		{
			private const long serialVersionUID = 1L;

			public string begin;

			public string end;

			public string duration;

			public Range(string begin, string end, string duration)
			{
				// TODO: maybe its easier if these are just strings...
				this.begin = begin;
				this.end = end;
				this.duration = duration;
			}
		}

		public virtual string Value()
		{
			return val;
		}

		public virtual string AltVal()
		{
			return altVal;
		}

		public virtual string Text()
		{
			return text;
		}

		public virtual string TimexType()
		{
			return type;
		}

		public virtual string Tid()
		{
			return tid;
		}

		public virtual Timex.Range Range()
		{
			return range;
		}

		public Timex()
		{
		}

		public Timex(IElement element)
		{
			this.val = null;
			this.beginPoint = -1;
			this.endPoint = -1;
			/*
			* ByteArrayOutputStream os = new ByteArrayOutputStream(); Serializer ser =
			* new Serializer(os, "UTF-8"); ser.setIndent(2); // this is the default in
			* JDOM so let's keep the same ser.setMaxLength(0); // no line wrapping for
			* content ser.write(new Document(element));
			*/
			Init(element);
		}

		public Timex(string val)
			: this(null, val)
		{
		}

		public Timex(string type, string val)
		{
			this.val = val;
			this.type = type;
			this.beginPoint = -1;
			this.endPoint = -1;
			this.xml = (val == null ? "<TIMEX3/>" : string.Format("<TIMEX3 VAL=\"%s\" TYPE=\"%s\"/>", this.val, this.type));
		}

		public Timex(string type, string val, string altVal, string tid, string text, int beginPoint, int endPoint)
		{
			this.type = type;
			this.val = val;
			this.altVal = altVal;
			this.tid = tid;
			this.text = text;
			this.beginPoint = beginPoint;
			this.endPoint = endPoint;
			this.xml = (val == null ? "<TIMEX3/>" : string.Format("<TIMEX3 tid=\"%s\" type=\"%s\" value=\"%s\">", this.tid, this.type, this.val) + this.text + "</TIMEX3>");
		}

		private void Init(IElement element)
		{
			Init(XMLUtils.NodeToString(element, false), element);
		}

		private void Init(string xml, IElement element)
		{
			this.xml = xml;
			this.text = element.GetTextContent();
			// Mandatory attributes
			this.tid = XMLUtils.GetAttribute(element, "tid");
			this.val = XMLUtils.GetAttribute(element, "VAL");
			if (this.val == null)
			{
				this.val = XMLUtils.GetAttribute(element, "value");
			}
			this.altVal = XMLUtils.GetAttribute(element, "alt_value");
			this.type = XMLUtils.GetAttribute(element, "type");
			if (type == null)
			{
				this.type = XMLUtils.GetAttribute(element, "TYPE");
			}
			// if (this.type != null) {
			// this.type = this.type.intern();
			// }
			// Optional attributes
			string beginPoint = XMLUtils.GetAttribute(element, "beginPoint");
			this.beginPoint = (beginPoint == null || beginPoint.Length == 0) ? -1 : System.Convert.ToInt32(Sharpen.Runtime.Substring(beginPoint, 1));
			string endPoint = XMLUtils.GetAttribute(element, "endPoint");
			this.endPoint = (endPoint == null || endPoint.Length == 0) ? -1 : System.Convert.ToInt32(Sharpen.Runtime.Substring(endPoint, 1));
			// Optional range
			string rangeStr = XMLUtils.GetAttribute(element, "range");
			if (rangeStr != null)
			{
				if (rangeStr.StartsWith("(") && rangeStr.EndsWith(")"))
				{
					rangeStr = Sharpen.Runtime.Substring(rangeStr, 1, rangeStr.Length - 1);
				}
				string[] parts = rangeStr.Split(",");
				this.range = new Timex.Range(parts.Length > 0 ? parts[0] : string.Empty, parts.Length > 1 ? parts[1] : string.Empty, parts.Length > 2 ? parts[2] : string.Empty);
			}
		}

		public virtual int BeginPoint()
		{
			return beginPoint;
		}

		public virtual int EndPoint()
		{
			return endPoint;
		}

		public override string ToString()
		{
			return (this.xml != null) ? this.xml : this.val;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (o == null || GetType() != o.GetType())
			{
				return false;
			}
			Timex timex = (Timex)o;
			if (beginPoint != timex.beginPoint)
			{
				return false;
			}
			if (endPoint != timex.endPoint)
			{
				return false;
			}
			if (type != null ? !type.Equals(timex.type) : timex.type != null)
			{
				return false;
			}
			if (val != null ? !val.Equals(timex.val) : timex.val != null)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result = val != null ? val.GetHashCode() : 0;
			result = 31 * result + (type != null ? type.GetHashCode() : 0);
			result = 31 * result + beginPoint;
			result = 31 * result + endPoint;
			return result;
		}

		public virtual IElement ToXmlElement()
		{
			IElement element = XMLUtils.CreateElement("TIMEX3");
			if (tid != null)
			{
				element.SetAttribute("tid", tid);
			}
			if (Value() != null)
			{
				element.SetAttribute("value", val);
			}
			if (altVal != null)
			{
				element.SetAttribute("altVal", altVal);
			}
			if (type != null)
			{
				element.SetAttribute("type", type);
			}
			if (beginPoint != -1)
			{
				element.SetAttribute("beginPoint", "t" + beginPoint.ToString());
			}
			if (endPoint != -1)
			{
				element.SetAttribute("endPoint", "t" + endPoint.ToString());
			}
			if (text != null)
			{
				element.SetTextContent(text);
			}
			return element;
		}

		// Used to create timex from XML (mainly for testing)
		public static Timex FromXml(string xml)
		{
			IElement element = XMLUtils.ParseElement(xml);
			if ("TIMEX3".Equals(element.GetNodeName()))
			{
				Timex t = new Timex();
				//      t.init(xml, element);
				// Doesn't preserve original input xml
				// Will reorder attributes of xml so can match xml of test timex and actual timex
				// (for which we can't control the order of the attributes now we don't use nu.xom...)
				t.Init(element);
				return t;
			}
			else
			{
				throw new ArgumentException("Invalid timex xml: " + xml);
			}
		}

		public static Timex FromMap(string text, IDictionary<string, string> map)
		{
			try
			{
				IElement element = XMLUtils.CreateElement("TIMEX3");
				foreach (KeyValuePair<string, string> entry in map)
				{
					if (entry.Value != null)
					{
						element.SetAttribute(entry.Key, entry.Value);
					}
				}
				element.SetTextContent(text);
				return new Timex(element);
			}
			catch (Exception ex)
			{
				throw new Exception(ex);
			}
		}

		/// <summary>Gets the Calendar matching the year, month and day of this Timex.</summary>
		/// <returns>The matching Calendar.</returns>
		public virtual Calendar GetDate()
		{
			if (Pattern.Matches("\\d\\d\\d\\d-\\d\\d-\\d\\d", this.val))
			{
				int year = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 0, 4));
				int month = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 5, 7));
				int day = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 8, 10));
				return MakeCalendar(year, month, day);
			}
			else
			{
				if (Pattern.Matches("\\d\\d\\d\\d\\d\\d\\d\\d", this.val))
				{
					int year = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 0, 4));
					int month = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 4, 6));
					int day = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 6, 8));
					return MakeCalendar(year, month, day);
				}
			}
			throw new NotSupportedException(string.Format("%s is not a fully specified date", this));
		}

		/// <summary>Gets two Calendars, marking the beginning and ending of this Timex's range.</summary>
		/// <returns>The begin point and end point Calendars.</returns>
		public virtual Pair<Calendar, Calendar> GetRange()
		{
			return this.GetRange(null);
		}

		/// <summary>Gets two Calendars, marking the beginning and ending of this Timex's range.</summary>
		/// <param name="documentTime">
		/// The time the document containing this Timex was written. (Not
		/// necessary for resolving all Timex expressions. This may be
		/// <see langword="null"/>
		/// , but then relative time expressions cannot be
		/// resolved.)
		/// </param>
		/// <returns>The begin point and end point Calendars.</returns>
		public virtual Pair<Calendar, Calendar> GetRange(Timex documentTime)
		{
			if (this.val == null)
			{
				throw new NotSupportedException("no value specified for " + this);
			}
			else
			{
				// YYYYMMDD or YYYYMMDDT... where the time is concatenated directly with the
				// date
				if (val.Length >= 8 && Pattern.Matches("\\d\\d\\d\\d\\d\\d\\d\\d", Sharpen.Runtime.Substring(this.val, 0, 8)))
				{
					int year = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 0, 4));
					int month = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 4, 6));
					int day = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 6, 8));
					return new Pair<Calendar, Calendar>(MakeCalendar(year, month, day), MakeCalendar(year, month, day));
				}
				else
				{
					// YYYY-MM-DD or YYYY-MM-DDT...
					if (val.Length >= 10 && Pattern.Matches("\\d\\d\\d\\d-\\d\\d-\\d\\d", Sharpen.Runtime.Substring(this.val, 0, 10)))
					{
						int year = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 0, 4));
						int month = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 5, 7));
						int day = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 8, 10));
						return new Pair<Calendar, Calendar>(MakeCalendar(year, month, day), MakeCalendar(year, month, day));
					}
					else
					{
						// YYYYMMDDL+
						if (Pattern.Matches("\\d\\d\\d\\d\\d\\d\\d\\d[A-Z]+", this.val))
						{
							int year = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 0, 4));
							int month = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 4, 6));
							int day = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 6, 8));
							return new Pair<Calendar, Calendar>(MakeCalendar(year, month, day), MakeCalendar(year, month, day));
						}
						else
						{
							// YYYYMM or YYYYMMT...
							if (val.Length >= 6 && Pattern.Matches("\\d\\d\\d\\d\\d\\d", Sharpen.Runtime.Substring(this.val, 0, 6)))
							{
								int year = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 0, 4));
								int month = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 4, 6));
								Calendar begin = MakeCalendar(year, month, 1);
								int lastDay = begin.GetActualMaximum(Calendar.Date);
								Calendar end = MakeCalendar(year, month, lastDay);
								return new Pair<Calendar, Calendar>(begin, end);
							}
							else
							{
								// YYYY-MM or YYYY-MMT...
								if (val.Length >= 7 && Pattern.Matches("\\d\\d\\d\\d-\\d\\d", Sharpen.Runtime.Substring(this.val, 0, 7)))
								{
									int year = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 0, 4));
									int month = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 5, 7));
									Calendar begin = MakeCalendar(year, month, 1);
									int lastDay = begin.GetActualMaximum(Calendar.Date);
									Calendar end = MakeCalendar(year, month, lastDay);
									return new Pair<Calendar, Calendar>(begin, end);
								}
								else
								{
									// YYYY or YYYYT...
									if (val.Length >= 4 && Pattern.Matches("\\d\\d\\d\\d", Sharpen.Runtime.Substring(this.val, 0, 4)))
									{
										int year = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 0, 4));
										return new Pair<Calendar, Calendar>(MakeCalendar(year, 1, 1), MakeCalendar(year, 12, 31));
									}
								}
							}
						}
					}
				}
			}
			// PDDY
			if (Pattern.Matches("P\\d+Y", this.val) && documentTime != null)
			{
				Calendar rc = documentTime.GetDate();
				int yearRange = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 1, this.val.Length - 1));
				// in the future
				if (this.beginPoint < this.endPoint)
				{
					Calendar start = CopyCalendar(rc);
					Calendar end = CopyCalendar(rc);
					end.Add(Calendar.Year, yearRange);
					return new Pair<Calendar, Calendar>(start, end);
				}
				else
				{
					// in the past
					if (this.beginPoint > this.endPoint)
					{
						Calendar start = CopyCalendar(rc);
						Calendar end = CopyCalendar(rc);
						start.Add(Calendar.Year, 0 - yearRange);
						return new Pair<Calendar, Calendar>(start, end);
					}
				}
				throw new Exception("begin and end are equal " + this);
			}
			// PDDM
			if (Pattern.Matches("P\\d+M", this.val) && documentTime != null)
			{
				Calendar rc = documentTime.GetDate();
				int monthRange = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 1, this.val.Length - 1));
				// in the future
				if (this.beginPoint < this.endPoint)
				{
					Calendar start = CopyCalendar(rc);
					Calendar end = CopyCalendar(rc);
					end.Add(Calendar.Month, monthRange);
					return new Pair<Calendar, Calendar>(start, end);
				}
				// in the past
				if (this.beginPoint > this.endPoint)
				{
					Calendar start = CopyCalendar(rc);
					Calendar end = CopyCalendar(rc);
					start.Add(Calendar.Month, 0 - monthRange);
					return new Pair<Calendar, Calendar>(start, end);
				}
				throw new Exception("begin and end are equal " + this);
			}
			// PDDD
			if (Pattern.Matches("P\\d+D", this.val) && documentTime != null)
			{
				Calendar rc = documentTime.GetDate();
				int dayRange = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 1, this.val.Length - 1));
				// in the future
				if (this.beginPoint < this.endPoint)
				{
					Calendar start = CopyCalendar(rc);
					Calendar end = CopyCalendar(rc);
					end.Add(Calendar.DayOfMonth, dayRange);
					return new Pair<Calendar, Calendar>(start, end);
				}
				// in the past
				if (this.beginPoint > this.endPoint)
				{
					Calendar start = CopyCalendar(rc);
					Calendar end = CopyCalendar(rc);
					start.Add(Calendar.DayOfMonth, 0 - dayRange);
					return new Pair<Calendar, Calendar>(start, end);
				}
				throw new Exception("begin and end are equal " + this);
			}
			// YYYYSP
			if (Pattern.Matches("\\d+SP", this.val))
			{
				int year = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 0, 4));
				Calendar start = MakeCalendar(year, 2, 1);
				Calendar end = MakeCalendar(year, 4, 31);
				return new Pair<Calendar, Calendar>(start, end);
			}
			// YYYYSU
			if (Pattern.Matches("\\d+SU", this.val))
			{
				int year = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 0, 4));
				Calendar start = MakeCalendar(year, 5, 1);
				Calendar end = MakeCalendar(year, 7, 31);
				return new Pair<Calendar, Calendar>(start, end);
			}
			// YYYYFA
			if (Pattern.Matches("\\d+FA", this.val))
			{
				int year = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 0, 4));
				Calendar start = MakeCalendar(year, 8, 1);
				Calendar end = MakeCalendar(year, 10, 31);
				return new Pair<Calendar, Calendar>(start, end);
			}
			// YYYYWI
			if (Pattern.Matches("\\d+WI", this.val))
			{
				int year = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 0, 4));
				Calendar start = MakeCalendar(year, 11, 1);
				Calendar end = MakeCalendar(year + 1, 1, 29);
				return new Pair<Calendar, Calendar>(start, end);
			}
			// YYYYWDD
			if (Pattern.Matches("\\d\\d\\d\\dW\\d+", this.val))
			{
				int year = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 0, 4));
				int week = System.Convert.ToInt32(Sharpen.Runtime.Substring(this.val, 5));
				int startDay = (week - 1) * 7;
				int endDay = startDay + 6;
				Calendar start = MakeCalendar(year, startDay);
				Calendar end = MakeCalendar(year, endDay);
				return new Pair<Calendar, Calendar>(start, end);
			}
			// PRESENT_REF
			if (this.val.Equals("PRESENT_REF"))
			{
				Calendar rc = documentTime.GetDate();
				// todo: This case doesn't check for documentTime being null and will NPE
				Calendar start = CopyCalendar(rc);
				Calendar end = CopyCalendar(rc);
				return new Pair<Calendar, Calendar>(start, end);
			}
			throw new Exception(string.Format("unknown value \"%s\" in %s", this.val, this));
		}

		private static Calendar MakeCalendar(int year, int month, int day)
		{
			Calendar date = Calendar.GetInstance();
			date.Clear();
			date.Set(year, month - 1, day, 0, 0, 0);
			return date;
		}

		private static Calendar MakeCalendar(int year, int dayOfYear)
		{
			Calendar date = Calendar.GetInstance();
			date.Clear();
			date.Set(Calendar.Year, year);
			date.Set(Calendar.DayOfYear, dayOfYear);
			return date;
		}

		private static Calendar CopyCalendar(Calendar c)
		{
			Calendar date = Calendar.GetInstance();
			date.Clear();
			date.Set(c.Get(Calendar.Year), c.Get(Calendar.Month), c.Get(Calendar.DayOfMonth), c.Get(Calendar.HourOfDay), c.Get(Calendar.Minute), c.Get(Calendar.Second));
			return date;
		}
	}
}
