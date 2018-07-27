using System;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Reads from serialized annotations</summary>
	/// <author>Angel Chang</author>
	public class SerializedAnnotationCreator : AbstractInputStreamAnnotationCreator
	{
		internal AnnotationSerializer serializer;

		public SerializedAnnotationCreator(AnnotationSerializer serializer)
		{
			this.serializer = serializer;
		}

		public SerializedAnnotationCreator(string name, Properties props)
		{
			string serializerClass = props.GetProperty(name + ".serializer");
			serializer = ReflectionLoading.LoadByReflection(serializerClass);
		}

		/// <exception cref="System.IO.IOException"/>
		public override Annotation Create(InputStream stream, string encoding)
		{
			try
			{
				Pair<Annotation, InputStream> pair = serializer.Read(stream);
				pair.second.Close();
				Annotation annotation = pair.first;
				return annotation;
			}
			catch (TypeLoadException e)
			{
				throw new Exception(e);
			}
		}
	}
}
