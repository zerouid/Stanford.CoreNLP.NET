using System.Collections.Generic;
using Edu.Stanford.Nlp.Loglinear.Model;


namespace Edu.Stanford.Nlp.Loglinear.Benchmarks
{
	/// <summary>Created on 10/23/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// This is a useful class for turning lists of tokens into a massively annotated PGM :)
	/// </author>
	public class CoNLLFeaturizer
	{
		private static string GetWordShape(string @string)
		{
			if (@string.ToUpper().Equals(@string) && @string.ToLower().Equals(@string))
			{
				return "no-case";
			}
			if (@string.ToUpper().Equals(@string))
			{
				return "upper-case";
			}
			if (@string.ToLower().Equals(@string))
			{
				return "lower-case";
			}
			if (@string.Length > 1 && char.IsUpperCase(@string[0]) && Sharpen.Runtime.Substring(@string, 1).ToLower().Equals(Sharpen.Runtime.Substring(@string, 1)))
			{
				return "capitalized";
			}
			return "mixed-case";
		}

		public static void Annotate(GraphicalModel model, IList<string> tags, ConcatVectorNamespace @namespace, IDictionary<string, double[]> embeddings)
		{
			for (int i = 0; i < model.variableMetaData.Count; i++)
			{
				IDictionary<string, string> metadata = model.GetVariableMetaDataByReference(i);
				string token = metadata["TOKEN"];
				string pos = metadata["POS"];
				string chunk = metadata["CHUNK"];
				IDictionary<string, string> leftMetadata = null;
				if (i > 0)
				{
					leftMetadata = model.GetVariableMetaDataByReference(i - 1);
				}
				string leftToken = (leftMetadata == null) ? "^" : leftMetadata["TOKEN"];
				string leftPos = (leftMetadata == null) ? "^" : leftMetadata["POS"];
				string leftChunk = (leftMetadata == null) ? "^" : leftMetadata["CHUNK"];
				IDictionary<string, string> rightMetadata = null;
				if (i < model.variableMetaData.Count - 1)
				{
					rightMetadata = model.GetVariableMetaDataByReference(i + 1);
				}
				string rightToken = (rightMetadata == null) ? "$" : rightMetadata["TOKEN"];
				string rightPos = (rightMetadata == null) ? "$" : rightMetadata["POS"];
				string rightChunk = (rightMetadata == null) ? "$" : rightMetadata["CHUNK"];
				// Add the unary factor
				GraphicalModel.Factor f = model.AddFactor(new int[] { i }, new int[] { tags.Count }, null);
				// This is the anonymous function that generates a feature vector for each assignment to the unary
				// factor
				System.Diagnostics.Debug.Assert((f.neigborIndices.Length == 1));
				System.Diagnostics.Debug.Assert((f.neigborIndices[0] == i));
				// If this is not the last variable, add a binary factor
				if (i < model.variableMetaData.Count - 1)
				{
					GraphicalModel.Factor jf = model.AddFactor(new int[] { i, i + 1 }, new int[] { tags.Count, tags.Count }, null);
					// This is the anonymous function that generates a feature vector for every joint assignment to the
					// binary factor
					System.Diagnostics.Debug.Assert((jf.neigborIndices.Length == 2));
					System.Diagnostics.Debug.Assert((jf.neigborIndices[0] == i));
					System.Diagnostics.Debug.Assert((jf.neigborIndices[1] == i + 1));
				}
			}
		}
	}
}
