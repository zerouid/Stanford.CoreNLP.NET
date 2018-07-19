using Sharpen;

namespace Edu.Stanford.Nlp.IO
{
	/// <summary>Copies a serialized object from one file to another.</summary>
	/// <remarks>
	/// Copies a serialized object from one file to another.
	/// <br />
	/// Why bother?  In case you need to change the format of the
	/// serialized object, so you implement readObject() to handle the old
	/// object and want to update existing models instead of retraining
	/// them.  I've had to write this program so many times that it seemed
	/// worthwhile to just check it in.
	/// </remarks>
	/// <author>John Bauer</author>
	public class CopySerializedObject
	{
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static void Main(string[] args)
		{
			object o = IOUtils.ReadObjectFromFile(args[0]);
			IOUtils.WriteObjectToFile(o, args[1]);
		}
	}
}
