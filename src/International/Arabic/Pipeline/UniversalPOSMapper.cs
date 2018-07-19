using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.International.Arabic;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic.Pipeline
{
	/// <summary>
	/// Maps LDC-provided Bies mappings to the Universal POS tag set described in
	/// Slav Petrov, Dipanjan Das and Ryan McDonald.
	/// </summary>
	/// <remarks>
	/// Maps LDC-provided Bies mappings to the Universal POS tag set described in
	/// Slav Petrov, Dipanjan Das and Ryan McDonald. "A Universal Part-of-Speech Tagset."
	/// <p>
	/// Includes optional support for adding morphological annotations via the setup method.
	/// </remarks>
	/// <author>Spence Green</author>
	public class UniversalPOSMapper : LDCPosMapper
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Arabic.Pipeline.UniversalPOSMapper));

		private readonly IDictionary<string, string> universalMap;

		private readonly MorphoFeatureSpecification morphoSpec;

		public UniversalPOSMapper()
			: base(false)
		{
			//Don't add the determiner split
			universalMap = Generics.NewHashMap();
			morphoSpec = new ArabicMorphoFeatureSpecification();
		}

		/// <summary>First map to the LDC short tags.</summary>
		/// <remarks>
		/// First map to the LDC short tags. Then map to the Universal POS. Then add
		/// morphological annotations.
		/// </remarks>
		public override string Map(string posTag, string terminal)
		{
			string rawTag = posTag.Trim();
			string shortTag = tagsToEscape.Contains(rawTag) ? rawTag : tagMap[rawTag];
			if (shortTag == null)
			{
				System.Console.Error.Printf("%s: No LDC shortened tag for %s%n", this.GetType().FullName, rawTag);
				return rawTag;
			}
			string universalTag = universalMap[shortTag];
			if (!universalMap.Contains(shortTag))
			{
				System.Console.Error.Printf("%s: No universal tag for LDC tag %s%n", this.GetType().FullName, shortTag);
				universalTag = shortTag;
			}
			MorphoFeatures feats = new MorphoFeatures(morphoSpec.StrToFeatures(rawTag));
			string functionalTag = feats.GetTag(universalTag);
			return functionalTag;
		}

		public override void Setup(File path, params string[] options)
		{
			//Setup the Bies tag mapping
			base.Setup(path, new string[0]);
			foreach (string opt in options)
			{
				string[] optToks = opt.Split(":");
				if (optToks[0].Equals("UniversalMap") && optToks.Length == 2)
				{
					LoadUniversalMap(optToks[1]);
				}
				else
				{
					//Maybe it's a morphological feature
					//Both of these calls will throw exceptions if the feature is illegal/invalid
					MorphoFeatureSpecification.MorphoFeatureType feat = MorphoFeatureSpecification.MorphoFeatureType.ValueOf(optToks[0]);
					IList<string> featVals = morphoSpec.GetValues(feat);
					morphoSpec.Activate(feat);
				}
			}
		}

		private void LoadUniversalMap(string path)
		{
			LineNumberReader reader = null;
			try
			{
				reader = new LineNumberReader(new FileReader(path));
				for (string line; (line = reader.ReadLine()) != null; )
				{
					if (line.Trim().Equals(string.Empty))
					{
						continue;
					}
					string[] toks = line.Trim().Split("\\s+");
					if (toks.Length != 2)
					{
						throw new Exception("Invalid mapping line: " + line);
					}
					universalMap[toks[0]] = toks[1];
				}
				reader.Close();
			}
			catch (FileNotFoundException)
			{
				System.Console.Error.Printf("%s: File not found %s%n", this.GetType().FullName, path);
			}
			catch (IOException e)
			{
				int lineId = (reader == null) ? -1 : reader.GetLineNumber();
				System.Console.Error.Printf("%s: Error at line %d%n", this.GetType().FullName, lineId);
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
