using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DE.Jollyday;
using DE.Jollyday.Config;
using DE.Jollyday.Impl;
using DE.Jollyday.Parameter;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Net;
using Java.Util;
using Org.Joda.Time;
using Sharpen;

namespace Edu.Stanford.Nlp.Time
{
	/// <summary>
	/// Wrapper around jollyday library so we can hook in holiday
	/// configurations from jollyday with SUTime.
	/// </summary>
	/// <author>Angel Chang</author>
	public class JollyDayHolidays : Env.IBinder
	{
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(JollyDayHolidays));

		private HolidayManager holidayManager;

		private IDictionary<string, JollyDayHolidays.JollyHoliday> holidays;

		private string varPrefix = "JH_";

		// import de.jollyday.configuration.ConfigurationProvider;
		// import java.net.MalformedURLException;
		// private CollectionValuedMap<String, JollyHoliday> holidays;
		public virtual void Init(string prefix, Properties props)
		{
			string xmlPath = props.GetProperty(prefix + "xml", "edu/stanford/nlp/models/sutime/jollyday/Holidays_sutime.xml");
			string xmlPathType = props.GetProperty(prefix + "pathtype", "classpath");
			varPrefix = props.GetProperty(prefix + "prefix", varPrefix);
			logger.Info("Initializing JollyDayHoliday for SUTime from " + xmlPathType + ' ' + xmlPath + " as " + prefix);
			Properties managerProps = new Properties();
			managerProps.SetProperty("manager.impl", "edu.stanford.nlp.time.JollyDayHolidays$MyXMLManager");
			try
			{
				URL holidayXmlUrl;
				if (Sharpen.Runtime.EqualsIgnoreCase(xmlPathType, "classpath"))
				{
					holidayXmlUrl = GetType().GetClassLoader().GetResource(xmlPath);
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(xmlPathType, "file"))
					{
						holidayXmlUrl = new URL("file:///" + xmlPath);
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(xmlPathType, "url"))
						{
							holidayXmlUrl = new URL(xmlPath);
						}
						else
						{
							throw new ArgumentException("Unsupported " + prefix + "pathtype = " + xmlPathType);
						}
					}
				}
				UrlManagerParameter ump = new UrlManagerParameter(holidayXmlUrl, managerProps);
				holidayManager = HolidayManager.GetInstance(ump);
			}
			catch (MalformedURLException e)
			{
				throw new Exception(e);
			}
			if (!(holidayManager is JollyDayHolidays.MyXMLManager))
			{
				throw new AssertionError("Did not get back JollyDayHolidays$MyXMLManager");
			}
			Configuration config = ((JollyDayHolidays.MyXMLManager)holidayManager).GetConfiguration();
			holidays = GetAllHolidaysMap(config);
		}

		public virtual void Bind(Env env)
		{
			if (holidays != null)
			{
				foreach (KeyValuePair<string, JollyDayHolidays.JollyHoliday> holidayEntry in holidays)
				{
					JollyDayHolidays.JollyHoliday jh = holidayEntry.Value;
					env.Bind(varPrefix + holidayEntry.Key, jh);
				}
			}
		}

		public virtual IDictionary<string, JollyDayHolidays.JollyHoliday> GetAllHolidaysMap(ICollection<Holiday> allHolidays)
		{
			IDictionary<string, JollyDayHolidays.JollyHoliday> map = Generics.NewHashMap();
			foreach (Holiday h in allHolidays)
			{
				string descKey = h.GetDescriptionPropertiesKey();
				if (descKey != null)
				{
					descKey = descKey.ReplaceAll(".*\\.", string.Empty);
					JollyDayHolidays.JollyHoliday jh = new JollyDayHolidays.JollyHoliday(descKey, holidayManager, h);
					map[jh.label] = jh;
				}
			}
			return map;
		}

		public virtual IDictionary<string, JollyDayHolidays.JollyHoliday> GetAllHolidaysMap(Configuration config)
		{
			ICollection<Holiday> s = GetAllHolidays(config);
			return GetAllHolidaysMap(s);
		}

		public virtual CollectionValuedMap<string, JollyDayHolidays.JollyHoliday> GetAllHolidaysCVMap(ICollection<Holiday> allHolidays)
		{
			CollectionValuedMap<string, JollyDayHolidays.JollyHoliday> map = new CollectionValuedMap<string, JollyDayHolidays.JollyHoliday>();
			foreach (Holiday h in allHolidays)
			{
				string descKey = h.GetDescriptionPropertiesKey();
				if (descKey != null)
				{
					descKey = descKey.ReplaceAll(".*\\.", string.Empty);
					JollyDayHolidays.JollyHoliday jh = new JollyDayHolidays.JollyHoliday(descKey, holidayManager, h);
					map.Add(jh.label, jh);
				}
			}
			return map;
		}

		public virtual CollectionValuedMap<string, JollyDayHolidays.JollyHoliday> GetAllHolidaysCVMap(Configuration config)
		{
			ICollection<Holiday> s = GetAllHolidays(config);
			return GetAllHolidaysCVMap(s);
		}

		public static void GetAllHolidays(Holidays holidays, ICollection<Holiday> allHolidays)
		{
			foreach (MethodInfo m in holidays.GetType().GetMethods())
			{
				if (IsGetter(m) && m.ReturnType == typeof(IList))
				{
					try
					{
						IList<Holiday> l = (IList<Holiday>)m.Invoke(holidays);
						Sharpen.Collections.AddAll(allHolidays, l);
					}
					catch (Exception e)
					{
						throw new Exception("Cannot create set of holidays.", e);
					}
				}
			}
		}

		public static void GetAllHolidays(Configuration config, ICollection<Holiday> allHolidays)
		{
			Holidays holidays = config.GetHolidays();
			GetAllHolidays(holidays, allHolidays);
			IList<Configuration> subConfigs = config.GetSubConfigurations();
			foreach (Configuration c in subConfigs)
			{
				GetAllHolidays(c, allHolidays);
			}
		}

		public static ICollection<Holiday> GetAllHolidays(Configuration config)
		{
			ICollection<Holiday> allHolidays = Generics.NewHashSet();
			GetAllHolidays(config, allHolidays);
			return allHolidays;
		}

		private static bool IsGetter(MethodInfo method)
		{
			return method.Name.StartsWith("get") && method.GetParameterTypes().Length == 0 && !typeof(void).Equals(method.ReturnType);
		}

		public class MyXMLManager : DefaultHolidayManager
		{
			public virtual Configuration GetConfiguration()
			{
				return configuration;
			}
		}

		[System.Serializable]
		public class JollyHoliday : SUTime.Time
		{
			private const long serialVersionUID = -1479143694893729803L;

			private readonly HolidayManager holidayManager;

			private readonly Holiday @base;

			private readonly string label;

			public JollyHoliday(string label, HolidayManager holidayManager, Holiday @base)
			{
				this.label = label;
				this.holidayManager = holidayManager;
				this.@base = @base;
			}

			public override string ToFormattedString(int flags)
			{
				if (GetTimeLabel() != null)
				{
					return GetTimeLabel();
				}
				if ((flags & SUTime.FormatIso) != 0)
				{
					return null;
				}
				return label;
			}

			public override bool IsGrounded()
			{
				return false;
			}

			public override SUTime.Time GetTime()
			{
				return this;
			}

			// TODO: compute duration/range => uncertainty of this time
			public override SUTime.Duration GetDuration()
			{
				return SUTime.DurationNone;
			}

			public override SUTime.Range GetRange(int flags, SUTime.Duration granularity)
			{
				return new SUTime.Range(this, this);
			}

			public override string ToISOString()
			{
				return @base.ToString();
			}

			protected internal override SUTime.Time Intersect(SUTime.Time t)
			{
				SUTime.Time resolved = ((SUTime.Time)Resolve(t, 0));
				if (resolved != this)
				{
					return resolved.Intersect(t);
				}
				else
				{
					return base.Intersect(t);
				}
			}

			private SUTime.Time ResolveWithYear(int year)
			{
				// TODO: If we knew location of article, can use that information to resolve holidays better
				ICollection<Holiday> holidays = holidayManager.GetHolidays(year);
				// Try to find this holiday
				foreach (Holiday h in holidays)
				{
					if (h.GetPropertiesKey().Equals(@base.GetDescriptionPropertiesKey()))
					{
						return new SUTime.PartialTime(this, new Partial(h.GetDate()));
					}
				}
				return null;
			}

			public override SUTime.Temporal Resolve(SUTime.Time t, int flags)
			{
				Partial p = (t != null) ? t.GetJodaTimePartial() : null;
				if (p != null)
				{
					if (JodaTimeUtils.HasField(p, DateTimeFieldType.Year()))
					{
						int year = p.Get(DateTimeFieldType.Year());
						SUTime.Time resolved = ResolveWithYear(year);
						if (resolved != null)
						{
							return resolved;
						}
					}
				}
				return this;
			}

			public override SUTime.Temporal Next()
			{
				// TODO: Handle holidays that are not yearly
				return new SUTime.RelativeTime(new SUTime.RelativeTime(SUTime.TemporalOp.Next, SUTime.Year, SUTime.ResolveToFuture), SUTime.TemporalOp.Intersect, this);
			}

			public override SUTime.Temporal Prev()
			{
				// TODO: Handle holidays that are not yearly
				return new SUTime.RelativeTime(new SUTime.RelativeTime(SUTime.TemporalOp.Prev, SUTime.Year, SUTime.ResolveToPast), SUTime.TemporalOp.Intersect, this);
			}

			public override SUTime.Time Add(SUTime.Duration offset)
			{
				return new SUTime.RelativeTime(this, SUTime.TemporalOp.OffsetExact, offset);
			}
		}
	}
}
