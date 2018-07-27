using System;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Time
{
	/// <summary>Factory for creating TimeExpressionExtractor.</summary>
	/// <author>Angel Chang</author>
	[System.Serializable]
	public class TimeExpressionExtractorFactory : IFactory<ITimeExpressionExtractor>
	{
		private const long serialVersionUID = 7280996573587450170L;

		public const string DefaultTimeExpressionExtractorClass = "edu.stanford.nlp.time.TimeExpressionExtractorImpl";

		private readonly string timeExpressionExtractorClass;

		public static readonly bool DefaultExtractorPresent = IsDefaultExtractorPresent();

		public TimeExpressionExtractorFactory()
			: this(DefaultTimeExpressionExtractorClass)
		{
		}

		public TimeExpressionExtractorFactory(string className)
		{
			this.timeExpressionExtractorClass = className;
		}

		public virtual ITimeExpressionExtractor Create()
		{
			return Create(timeExpressionExtractorClass);
		}

		public virtual ITimeExpressionExtractor Create(string name, Properties props)
		{
			return Create(timeExpressionExtractorClass, name, props);
		}

		public static ITimeExpressionExtractor CreateExtractor()
		{
			return Create(DefaultTimeExpressionExtractorClass);
		}

		public static ITimeExpressionExtractor CreateExtractor(string name, Properties props)
		{
			return Create(DefaultTimeExpressionExtractorClass, name, props);
		}

		private static bool IsDefaultExtractorPresent()
		{
			try
			{
				Type clazz = Sharpen.Runtime.GetType(DefaultTimeExpressionExtractorClass);
			}
			catch
			{
				return false;
			}
			return true;
		}

		public static ITimeExpressionExtractor Create(string className)
		{
			return ReflectionLoading.LoadByReflection(className);
		}

		public static ITimeExpressionExtractor Create(string className, string name, Properties props)
		{
			return ReflectionLoading.LoadByReflection(className, name, props);
		}
	}
}
