using System;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Util.Zip;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Serializes Annotation objects using the default Java serializer</summary>
	public class GenericAnnotationSerializer : AnnotationSerializer
	{
		internal bool compress = false;

		public GenericAnnotationSerializer(bool compress)
		{
			this.compress = compress;
		}

		public GenericAnnotationSerializer()
			: this(false)
		{
		}

		/// <summary>Turns out, an ObjectOutputStream cannot append to a file.</summary>
		/// <remarks>Turns out, an ObjectOutputStream cannot append to a file. This is dumb.</remarks>
		public class AppendingObjectOutputStream : ObjectOutputStream
		{
			/// <exception cref="System.IO.IOException"/>
			public AppendingObjectOutputStream(OutputStream @out)
				: base(@out)
			{
			}

			/// <exception cref="System.IO.IOException"/>
			protected override void WriteStreamHeader()
			{
				// do not write a header, but reset
				Reset();
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public override OutputStream Write(Annotation corpus, OutputStream os)
		{
			if (os is GenericAnnotationSerializer.AppendingObjectOutputStream)
			{
				((GenericAnnotationSerializer.AppendingObjectOutputStream)os).WriteObject(corpus);
				return os;
			}
			else
			{
				if (os is ObjectOutputStream)
				{
					ObjectOutputStream objectOutput = new GenericAnnotationSerializer.AppendingObjectOutputStream(compress ? new GZIPOutputStream(os) : os);
					objectOutput.WriteObject(corpus);
					return objectOutput;
				}
				else
				{
					ObjectOutputStream objectOutput = new ObjectOutputStream(compress ? new GZIPOutputStream(os) : os);
					objectOutput.WriteObject(corpus);
					return objectOutput;
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.InvalidCastException"/>
		public override Pair<Annotation, InputStream> Read(InputStream @is)
		{
			ObjectInputStream objectInput;
			if (@is is ObjectInputStream)
			{
				objectInput = (ObjectInputStream)@is;
			}
			else
			{
				objectInput = new ObjectInputStream(compress ? new GZIPInputStream(@is) : @is);
			}
			object annotation = objectInput.ReadObject();
			if (annotation == null)
			{
				return null;
			}
			if (!(annotation is Annotation))
			{
				throw new InvalidCastException("ERROR: Serialized data does not contain an Annotation!");
			}
			return Pair.MakePair((Annotation)annotation, (InputStream)objectInput);
		}
	}
}
