using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>
	/// Keeps track of a distributional similarity mapping, i.e., a map from
	/// word to class.
	/// </summary>
	/// <remarks>
	/// Keeps track of a distributional similarity mapping, i.e., a map from
	/// word to class.  Returns strings to save time, since that is how the
	/// results are used in the tagger.
	/// </remarks>
	[System.Serializable]
	public class Distsim
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.Distsim));

		private static readonly IDictionary<string, Edu.Stanford.Nlp.Tagger.Maxent.Distsim> lexiconMap = Generics.NewHashMap();

		private readonly IDictionary<string, string> lexicon;

		private readonly string unk;

		private bool mapdigits;

		private bool casedDistSim;

		private static readonly Pattern digits = Pattern.Compile("[0-9]");

		/// <summary>
		/// The Extractor argument extraction keeps ; together, so we use
		/// that to delimit options.
		/// </summary>
		/// <remarks>
		/// The Extractor argument extraction keeps ; together, so we use
		/// that to delimit options.  Actually, the only option supported is
		/// mapdigits, which tells the Distsim to try mapping [0-9] to 0 and
		/// requery for an unknown word with digits.
		/// </remarks>
		public Distsim(string path)
		{
			// Avoid loading the same lexicon twice but allow different lexicons
			// TODO: when loading a distsim, should we populate this map?
			// = false
			// = false;
			string[] pieces = path.Split(";");
			string filename = pieces[0];
			for (int arg = 1; arg < pieces.Length; ++arg)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(pieces[arg], "mapdigits"))
				{
					mapdigits = true;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(pieces[arg], "casedDistSim"))
					{
						casedDistSim = true;
					}
					else
					{
						throw new ArgumentException("Unknown argument " + pieces[arg]);
					}
				}
			}
			lexicon = Generics.NewHashMap();
			// todo [cdm 2016]: Note that this loads file with default file encoding rather than specifying it
			foreach (string word in ObjectBank.GetLineIterator(new File(filename)))
			{
				string[] bits = word.Split("\\s+");
				string w = bits[0];
				if (!casedDistSim)
				{
					w = w.ToLower();
				}
				lexicon[w] = bits[1];
			}
			if (lexicon.Contains("<unk>"))
			{
				unk = lexicon["<unk>"];
			}
			else
			{
				unk = "null";
			}
		}

		public static Edu.Stanford.Nlp.Tagger.Maxent.Distsim InitLexicon(string path)
		{
			lock (lexiconMap)
			{
				Edu.Stanford.Nlp.Tagger.Maxent.Distsim lex = lexiconMap[path];
				if (lex == null)
				{
					Timing timer = new Timing();
					lex = new Edu.Stanford.Nlp.Tagger.Maxent.Distsim(path);
					lexiconMap[path] = lex;
					timer.Done(log, "Loading distsim lexicon from " + path);
				}
				return lex;
			}
		}

		/// <summary>Returns the cluster for the given word as a string.</summary>
		/// <remarks>
		/// Returns the cluster for the given word as a string.  If the word
		/// is not found, but the Distsim contains default numbers and the
		/// word contains the digits 0-9, the default number is returned if
		/// found.  If the word is still unknown, the unknown word is
		/// returned ("null" if no other unknown word was specified).
		/// </remarks>
		public virtual string GetMapping(string word)
		{
			string distSim = lexicon[word.ToLower()];
			if (distSim == null && mapdigits)
			{
				Matcher matcher = digits.Matcher(word);
				if (matcher.Find())
				{
					distSim = lexicon[matcher.ReplaceAll("0")];
				}
			}
			if (distSim == null)
			{
				distSim = unk;
			}
			return distSim;
		}

		private const long serialVersionUID = 2L;
	}
}
