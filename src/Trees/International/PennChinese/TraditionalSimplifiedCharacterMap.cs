using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>
	/// This class is a Function which transforms a String of traditional
	/// text into a string of simplified text.
	/// </summary>
	/// <remarks>
	/// This class is a Function which transforms a String of traditional
	/// text into a string of simplified text.  It does this by looking for
	/// and extracting all single characters from a CEDict file.
	/// <br />
	/// There are a few hardcoded translations to cover for ambiguities in
	/// the simplified translations of traditional characters.
	/// <ul>
	/// <li> 鹼: mapped to 碱, although 硷 is listed as a possibility in CEDict.
	/// <li> 於: mapped to 于, although 於 is listed as a possibility in CEDict.
	/// <li> 祇: mapped to 只, although 祇 is listed as a possibility in CEDict.
	/// <li> 彷: sometimes also 彷, but 仿 is more common.
	/// <li> 甚: sometimes also 甚, but 什 is more common.
	/// <li> 麽: can appear as 幺麽, but very rare.  Hardcoded for now
	/// unless that causes problems.
	/// </ul>
	/// </remarks>
	/// <author>John Bauer</author>
	public class TraditionalSimplifiedCharacterMap : IFunction<string, string>
	{
		internal IDictionary<string, string> map = Generics.NewHashMap();

		internal string[][] Hardcoded = new string[][] { new string[] { "鹼", "碱" }, new string[] { "於", "于" }, new string[] { "祇", "只" }, new string[] { "彷", "仿" }, new string[] { "甚", "什" }, new string[] { "麽", "么" } };

		public TraditionalSimplifiedCharacterMap()
			: this(CEDict.Path())
		{
		}

		public TraditionalSimplifiedCharacterMap(string path)
		{
			// TODO: gzipped maps might be faster
			try
			{
				FileInputStream fis = new FileInputStream(path);
				InputStreamReader isr = new InputStreamReader(fis, "utf-8");
				BufferedReader br = new BufferedReader(isr);
				Init(br);
				br.Close();
				isr.Close();
				fis.Close();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		internal virtual void Init(BufferedReader reader)
		{
			try
			{
				ICollection<string> hardcodedSet = Generics.NewHashSet();
				foreach (string[] transform in Hardcoded)
				{
					hardcodedSet.Add(transform[0]);
					string traditional = transform[0];
					string simplified = transform[1];
					map[traditional] = simplified;
				}
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					if (line.StartsWith("#"))
					{
						continue;
					}
					if (line.Length >= 3 && line[1] == ' ' && line[3] == ' ')
					{
						// We're only interested in lines that represent a single character
						string traditional = Sharpen.Runtime.Substring(line, 0, 1);
						string simplified = Sharpen.Runtime.Substring(line, 2, 3);
						// Fail on duplicates.  Only a few come up in cedict, and
						// those that do should already be accommodated
						if (map.Contains(traditional) && !hardcodedSet.Contains(traditional) && !simplified.Equals(map[traditional]))
						{
							throw new Exception("Character " + traditional + " mapped to " + simplified + " already mapped to " + map[traditional]);
						}
						map[traditional] = simplified;
					}
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public virtual string Apply(string input)
		{
			StringBuilder translated = new StringBuilder();
			for (int i = 0; i < input.Length; ++i)
			{
				string c = Sharpen.Runtime.Substring(input, i, i + 1);
				if (map.Contains(c))
				{
					translated.Append(map[c]);
				}
				else
				{
					translated.Append(c);
				}
			}
			return translated.ToString();
		}

		public virtual void TranslateLines(BufferedReader br, BufferedWriter bw)
		{
			try
			{
				string line;
				while ((line = br.ReadLine()) != null)
				{
					bw.Write(Apply(line));
					bw.NewLine();
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public virtual void TranslateFile(string input, string output)
		{
			try
			{
				FileInputStream fis = new FileInputStream(input);
				InputStreamReader isr = new InputStreamReader(fis, "utf-8");
				BufferedReader br = new BufferedReader(isr);
				FileOutputStream fos = new FileOutputStream(output);
				OutputStreamWriter osw = new OutputStreamWriter(fos, "utf-8");
				BufferedWriter bw = new BufferedWriter(osw);
				TranslateLines(br, bw);
				bw.Close();
				osw.Close();
				fos.Close();
				br.Close();
				isr.Close();
				fis.Close();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.Trees.International.Pennchinese.TraditionalSimplifiedCharacterMap mapper = new Edu.Stanford.Nlp.Trees.International.Pennchinese.TraditionalSimplifiedCharacterMap();
			mapper.TranslateFile(args[0], args[1]);
		}
	}
}
