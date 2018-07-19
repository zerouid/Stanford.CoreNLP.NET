using System;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// The goal of this class is to make it easier to load stuff by
	/// reflection.
	/// </summary>
	/// <remarks>
	/// The goal of this class is to make it easier to load stuff by
	/// reflection.  You can hide all of the ugly exception catching, etc
	/// by using the static methods in this class.
	/// </remarks>
	/// <author>John Bauer</author>
	/// <author>Gabor Angeli (changed)</author>
	public class ReflectionLoading
	{
		private ReflectionLoading()
		{
		}

		// static methods only
		/// <summary>Create an object of type T by calling the class constructor with the given arguments.</summary>
		/// <remarks>
		/// Create an object of type T by calling the class constructor with the given arguments.
		/// You can use this as follows:
		/// <br />
		/// <c>String s = ReflectionLoading.loadByReflection("java.lang.String", "foo");</c>
		/// <br />
		/// <c>String s = ReflectionLoading.loadByReflection("java.lang.String");</c>
		/// <br />
		/// Note that this uses generics for convenience, but this does
		/// nothing for compile-time error checking.  You can do:
		/// <br />
		/// <c>Integer i = ReflectionLoading.loadByReflection("java.lang.String");</c>
		/// <br />
		/// and it will compile just fine, but will result in a ClassCastException.
		/// </remarks>
		public static T LoadByReflection<T>(string className, params object[] arguments)
		{
			try
			{
				return (T)new MetaClass(className).CreateInstance(arguments);
			}
			catch (Exception e)
			{
				throw new ReflectionLoading.ReflectionLoadingException("Error creating " + className, e);
			}
		}

		/// <summary>
		/// This class encapsulates all of the exceptions that can be thrown
		/// when loading something by reflection.
		/// </summary>
		[System.Serializable]
		public class ReflectionLoadingException : Exception
		{
			private const long serialVersionUID = -3324911744277952585L;

			public ReflectionLoadingException(string message, Exception reason)
				: base(message, reason)
			{
			}
		}
	}
}
