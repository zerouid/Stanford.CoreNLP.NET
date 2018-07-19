using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Neural
{
	/// <summary>Manages the properties for training and running neural coreference systems.</summary>
	/// <author>Kevin Clark</author>
	public class NeuralCorefProperties
	{
		public static double Greedyness(Properties props)
		{
			return PropertiesUtils.GetDouble(props, "coref.neural.greedyness", 0.5);
		}

		public static string ModelPath(Properties props)
		{
			string defaultPath = "edu/stanford/nlp/models/coref/neural/" + (CorefProperties.GetLanguage(props) == Locale.Chinese ? "chinese" : "english") + (CorefProperties.Conll(props) ? "-model-conll" : "-model-default") + ".ser.gz";
			return PropertiesUtils.GetString(props, "coref.neural.modelPath", defaultPath);
		}

		public static string PretrainedEmbeddingsPath(Properties props)
		{
			string defaultPath = "edu/stanford/nlp/models/coref/neural/" + (CorefProperties.GetLanguage(props) == Locale.Chinese ? "chinese" : "english") + "-embeddings.ser.gz";
			return PropertiesUtils.GetString(props, "coref.neural.embeddingsPath", defaultPath);
		}
	}
}
