//
// Annotation -- annotation protocol used by StanfordCoreNLP
// Copyright (c) 2009-2010 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//
using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>An annotation representing a span of text in a document.</summary>
	/// <remarks>
	/// An annotation representing a span of text in a document.
	/// Basically just an implementation of CoreMap that knows about text.
	/// You're meant to use the annotation keys in CoreAnnotations for common
	/// cases, but can define bespoke ones for unusual annotations.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	/// <author>Anna Rafferty</author>
	/// <author>bethard</author>
	[System.Serializable]
	public class Annotation : ArrayCoreMap
	{
		/// <summary>SerialUID</summary>
		private const long serialVersionUID = 1L;

		/// <summary>Copy constructor.</summary>
		/// <param name="map">The new Annotation copies this one.</param>
		public Annotation(Edu.Stanford.Nlp.Pipeline.Annotation map)
			: base(map)
		{
		}

		/// <summary>Copies the map, but not a deep copy.</summary>
		/// <returns>The copy</returns>
		public virtual Edu.Stanford.Nlp.Pipeline.Annotation Copy()
		{
			return new Edu.Stanford.Nlp.Pipeline.Annotation(this);
		}

		/// <summary>
		/// The text becomes the CoreAnnotations.TextAnnotation of the newly
		/// created Annotation.
		/// </summary>
		public Annotation(string text)
		{
			this.Set(typeof(CoreAnnotations.TextAnnotation), text);
		}

		/// <summary>
		/// The basic toString() method of an Annotation simply
		/// prints out the text over which any annotations have
		/// been made (TextAnnotation).
		/// </summary>
		/// <remarks>
		/// The basic toString() method of an Annotation simply
		/// prints out the text over which any annotations have
		/// been made (TextAnnotation). To print all the
		/// Annotation keys, use
		/// <c>toShorterString();</c>
		/// .
		/// </remarks>
		/// <returns>The text underlying this Annotation</returns>
		public override string ToString()
		{
			return this.Get(typeof(CoreAnnotations.TextAnnotation));
		}

		/// <summary>Make a new Annotation from a List of tokenized sentences.</summary>
		public Annotation(IList<ICoreMap> sentences)
			: base()
		{
			this.Set(typeof(CoreAnnotations.SentencesAnnotation), sentences);
			IList<CoreLabel> tokens = new List<CoreLabel>();
			StringBuilder text = new StringBuilder();
			foreach (ICoreMap sentence in sentences)
			{
				IList<CoreLabel> sentenceTokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
				Sharpen.Collections.AddAll(tokens, sentenceTokens);
				if (sentence.ContainsKey(typeof(CoreAnnotations.TextAnnotation)))
				{
					text.Append(sentence.Get(typeof(CoreAnnotations.TextAnnotation)));
				}
				else
				{
					// If there is no text in the sentence, fake it as best as we can
					if (text.Length > 0)
					{
						text.Append('\n');
					}
					text.Append(SentenceUtils.ListToString(sentenceTokens));
				}
			}
			this.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
			this.Set(typeof(CoreAnnotations.TextAnnotation), text.ToString());
		}

		[Obsolete]
		public Annotation()
			: base(12)
		{
		}
		// ==================
		// Old Deprecated API
		// ==================
	}
}
