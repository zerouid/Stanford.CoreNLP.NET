using System.Collections.Generic;
using System.IO;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Pascal
{
	/// <author>Jamie Nicolson</author>
	public class Prior
	{
		private IDictionary<string, int> fieldIndices;

		private string[] indexFields;

		private double[] matrix;

		/// <exception cref="System.IO.IOException"/>
		public Prior(BufferedReader reader)
		{
			// Map<String, int> maps field names to indexes in the matrix
			// n-dimensional boolean matrix. There will be 2^n entries in the matrix.
			string line;
			line = reader.ReadLine();
			if (line == null)
			{
				throw new IOException();
			}
			indexFields = line.Split("\\s+");
			fieldIndices = new Dictionary<string, int>();
			for (int i = 0; i < indexFields.Length; ++i)
			{
				fieldIndices[indexFields[i]] = int.Parse(i);
			}
			if (indexFields.Length < 1 || indexFields.Length > 31)
			{
				throw new IOException("Invalid number of fields, should be >=1 and <= 31");
			}
			int matrixSize = 1 << indexFields.Length;
			matrix = new double[matrixSize];
			int matrixIdx = 0;
			while (matrixIdx < matrix.Length && (line = reader.ReadLine()) != null)
			{
				string[] tokens = line.Split("\\s+");
				for (int t = 0; matrixIdx < matrix.Length && t < tokens.Length; ++t)
				{
					matrix[matrixIdx++] = double.ParseDouble(tokens[t]);
				}
			}
		}

		/// <summary><c>Map&lt;String, boolean&gt;</c></summary>
		public virtual double Get(ISet presentFields)
		{
			int index = 0;
			foreach (string field in indexFields)
			{
				index *= 2;
				if (presentFields.Contains(field))
				{
					++index;
				}
			}
			return matrix[index];
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			BufferedReader br = new BufferedReader(new FileReader("/tmp/acstats"));
			Edu.Stanford.Nlp.IE.Pascal.Prior p = new Edu.Stanford.Nlp.IE.Pascal.Prior(br);
			HashSet<string> hs = new HashSet<string>();
			hs.Add("workshopname");
			//hs.add("workshopacronym");
			double d = p.Get(hs);
			System.Console.Out.WriteLine("d is " + d);
		}
	}
}
