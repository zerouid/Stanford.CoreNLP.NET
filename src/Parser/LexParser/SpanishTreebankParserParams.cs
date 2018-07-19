using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Spanish;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>TreebankLangParserParams for the AnCora corpus.</summary>
	/// <remarks>
	/// TreebankLangParserParams for the AnCora corpus. This package assumes
	/// that the provided trees are in PTB format, read from the initial
	/// AnCora XML with
	/// <see cref="Edu.Stanford.Nlp.Trees.International.Spanish.SpanishXMLTreeReader"/>
	/// and preprocessed with
	/// <see cref="Edu.Stanford.Nlp.International.Spanish.Pipeline.MultiWordPreprocessor"/>
	/// .
	/// </remarks>
	/// <author>Jon Gauthier</author>
	[System.Serializable]
	public class SpanishTreebankParserParams : TregexPoweredTreebankParserParams
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.SpanishTreebankParserParams));

		private const long serialVersionUID = -8734165273482119424L;

		private readonly StringBuilder optionsString;

		private IHeadFinder headFinder;

		public SpanishTreebankParserParams()
			: base(new SpanishTreebankLanguagePack())
		{
			SetInputEncoding(TreebankLanguagePack().GetEncoding());
			SetHeadFinder(new SpanishHeadFinder());
			optionsString = new StringBuilder();
			optionsString.Append(GetType().GetSimpleName() + "\n");
			BuildAnnotations();
		}

		private const string PoderForm = "(?iu)^(?:pued(?:o|[ea][sn]?)|" + "pod(?:e[dr]|ido|[ea]mos|[éá]is|r(?:é(?:is)?|á[sn]?|emos)|r?ía(?:s|mos|is|n)?)|" + "pud(?:[eo]|i(?:ste(?:is)?|mos|eron|er[ea](?:[sn]|is)?|ér[ea]mos|endo)))$";

		/// <summary>Forms of hacer which may lead time expressions</summary>
		private const string HacerTimeForm = "(?iu)^(?:hac(?:er|ía))$";

		private void BuildAnnotations()
		{
			// +.25 F1
			annotations["-markInf"] = new Pair("/^(S|grup\\.verb|infinitiu|gerundi)/ < @infinitiu", new TregexPoweredTreebankParserParams.SimpleStringFunction("-infinitive"));
			annotations["-markGer"] = new Pair("/^(S|grup\\.verb|infinitiu|gerundi)/ < @gerundi", new TregexPoweredTreebankParserParams.SimpleStringFunction("-gerund"));
			// +.04 F1
			annotations["-markRelative"] = new Pair("@S <, @relatiu", new TregexPoweredTreebankParserParams.SimpleStringFunction("-relative"));
			// Negative F1; unused in default config
			annotations["-markPPHeads"] = new Pair("@sp", new TregexPoweredTreebankParserParams.AnnotateHeadFunction(headFinder));
			// +.1 F1
			annotations["-markComo"] = new Pair("@cs < /(?iu)^como$/", new TregexPoweredTreebankParserParams.SimpleStringFunction("[como]"));
			annotations["-markSpecHeads"] = new Pair("@spec", new TregexPoweredTreebankParserParams.AnnotateHeadFunction(headFinder));
			// +.32 F1
			annotations["-markSingleChildNPs"] = new Pair("/^(sn|grup\\.nom)/ <: __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-singleChild"));
			// +.05 F1
			annotations["-markPPFriendlyVerbs"] = new Pair("/^v/ > /^grup\\.prep/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-PPFriendly"));
			// +.46 F1
			annotations["-markConjTypes"] = new Pair("@conj <: /^c[cs]/=c", new SpanishTreebankParserParams.MarkConjTypeFunction());
			// +.09 F1
			annotations["-markPronounNPs"] = new Pair("/^(sn|grup\\.nom)/ <<: /^p[0p]/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-pronoun"));
			// +1.39 F1
			annotations["-markParticipleAdjs"] = new Pair("@aq0000 < /(?iu)([aeií]d|puest|biert|vist|(ben|mal)dit|[fh]ech|scrit|muert|[sv]uelt|[rl]ect|" + "frit|^(rot|dich|impres|desnud|sujet|exent))[oa]s?$/", new TregexPoweredTreebankParserParams.SimpleStringFunction
				("-part"));
			// Negative F1; unused in default config
			annotations["-markSentenceInitialClauses"] = new Pair("@S !, __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-init"));
			// Insignificant F1; unused in default config
			annotations["-markPoder"] = new Pair(string.Format("/^(infinitiu|gerundi|grup\\.verb)/ <<: /%s/", PoderForm), new TregexPoweredTreebankParserParams.SimpleStringFunction("-poder"));
			// +.29 F1
			annotations["-markBaseNPs"] = new Pair("/^grup\\.nom/ !< (__ < (__ < __))", new TregexPoweredTreebankParserParams.SimpleStringFunction("-base"));
			// +.17 F1
			annotations["-markVerbless"] = new Pair("@S|sentence !<< /^(v|participi$)/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-verbless"));
			// +.23 F1
			annotations["-markDominatesVerb"] = new Pair("__ << (/^(v|participi$)/ < __)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-dominatesV"));
			// Negative F1 -- not used by default
			annotations["-markNonRecSPs"] = new Pair("@sp !<< @sp", new TregexPoweredTreebankParserParams.SimpleStringFunction("-nonRec"));
			// In right-recursive verb phrases, mark the prefix of the first verb on its tag.
			// This annotation tries to capture the fact that only a few roots are ever really part of
			// these constructions: poder, deber, ir, etc.
			annotations["-markRightRecVPPrefixes"] = new Pair("/^v/ $+ @infinitiu|gerundi >, /^(grup.verb|infinitiu|gerundi)/", new SpanishTreebankParserParams.MarkPrefixFunction(3));
			// Negative F1 -- not used by default
			annotations["-markParentheticalNPs"] = new Pair("@sn <<, fpa <<` fpt", new TregexPoweredTreebankParserParams.SimpleStringFunction("-paren"));
			annotations["-markNumericNPs"] = new Pair("@sn << (/^z/ < __) !<< @sn", new TregexPoweredTreebankParserParams.SimpleStringFunction("-num"));
			// Negative F1 -- not used by default
			annotations["-markCoordinatedNPs"] = new Pair("@sn <, (/^(sn|grup\\.nom)/ $+ (@conj < /^(cc|grup\\.cc)/ $+ /^(sn|grup\\.nom)/=last))" + "<` =last", new TregexPoweredTreebankParserParams.SimpleStringFunction("-coord"));
			annotations["-markHacerTime"] = new Pair(string.Format("/^vm/ < /%s/ $+ /^d/", HacerTimeForm), new TregexPoweredTreebankParserParams.SimpleStringFunction("-hacerTime"));
			CompileAnnotations(headFinder);
		}

		/// <summary>Mark `conj` constituents with their `cc` / `cs` child.</summary>
		[System.Serializable]
		private class MarkConjTypeFunction : ISerializableFunction<TregexMatcher, string>
		{
			private const long serialVersionUID = 403406212736445856L;

			public virtual string Apply(TregexMatcher m)
			{
				string type = m.GetNode("c").Value().ToUpper();
				return "-conj" + type;
			}
		}

		/// <summary>Mark a tag with a prefix of its constituent word.</summary>
		[System.Serializable]
		private class MarkPrefixFunction : ISerializableFunction<TregexMatcher, string>
		{
			private const long serialVersionUID = -3275700521562916350L;

			private const int DefaultPrefixLength = 3;

			private readonly int prefixLength;

			public MarkPrefixFunction()
				: this(DefaultPrefixLength)
			{
			}

			public MarkPrefixFunction(int prefixLength)
			{
				this.prefixLength = prefixLength;
			}

			public virtual string Apply(TregexMatcher m)
			{
				Tree tagNode = m.GetMatch();
				string yield = tagNode.FirstChild().Value();
				string prefix = Sharpen.Runtime.Substring(yield, 0, Math.Min(yield.Length, prefixLength));
				return "[p," + prefix + ']';
			}
		}

		/// <summary>Features which should be enabled by default.</summary>
		/// <seealso cref="BuildAnnotations()"/>
		protected internal override string[] BaselineAnnotationFeatures()
		{
			return new string[] { "-markInf", "-markGer", "-markRightRecVPPrefixes", "-markSingleChildNPs", "-markBaseNPs", "-markPronounNPs", "-markRelative", "-markComo", "-markSpecHeads", "-markPPFriendlyVerbs", "-markParticipleAdjs", "-markHacerTime"
				, "-markConjTypes", "-markVerbless", "-markDominatesVerb" };
		}

		// verb phrase annotations
		// noun phrase annotations
		// "-markCoordinatedNPs",
		// "-markParentheticalNPs",
		// "-markNumericNPs",
		// prepositional phrase annotations
		// "-markNonRecSPs", negative F1!
		// "-markPPHeads", negative F1!
		// clause annotations
		/* "-markSentenceInitialClauses", */
		// lexical / word- or tag-level annotations
		/* "-markPoder", */
		// conjunction annotations
		// sentence annotations
		public override IHeadFinder HeadFinder()
		{
			return headFinder;
		}

		public override IHeadFinder TypedDependencyHeadFinder()
		{
			// Not supported
			return null;
		}

		public override ILexicon Lex(Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			// Override unknown word model
			if (op.lexOptions.uwModelTrainer == null)
			{
				op.lexOptions.uwModelTrainer = "edu.stanford.nlp.parser.lexparser.SpanishUnknownWordModelTrainer";
			}
			return new BaseLexicon(op, wordIndex, tagIndex);
		}

		public override string[] SisterSplitters()
		{
			return new string[0];
		}

		public override ITreeTransformer Collinizer()
		{
			return new TreeCollinizer(TreebankLanguagePack());
		}

		public override ITreeTransformer CollinizerEvalb()
		{
			return new TreeCollinizer(TreebankLanguagePack());
		}

		public override DiskTreebank DiskTreebank()
		{
			return new DiskTreebank(TreeReaderFactory(), inputEncoding);
		}

		public override MemoryTreebank MemoryTreebank()
		{
			return new MemoryTreebank(TreeReaderFactory(), inputEncoding);
		}

		/// <summary>Set language-specific options according to flags.</summary>
		/// <remarks>
		/// Set language-specific options according to flags. This routine should process the option starting in args[i] (which
		/// might potentially be several arguments long if it takes arguments). It should return the index after the last index
		/// it consumed in processing.  In particular, if it cannot process the current option, the return value should be i.
		/// <p/>
		/// Generic options are processed separately by
		/// <see cref="Options.SetOption(string[], int)"/>
		/// , and implementations of this
		/// method do not have to worry about them. The Options class handles routing options. TreebankParserParams that extend
		/// this class should call super when overriding this method.
		/// </remarks>
		/// <param name="args"/>
		/// <param name="i"/>
		public override int SetOptionFlag(string[] args, int i)
		{
			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-headFinder") && (i + 1 < args.Length))
			{
				try
				{
					IHeadFinder hf = (IHeadFinder)System.Activator.CreateInstance(Sharpen.Runtime.GetType(args[i + 1]));
					SetHeadFinder(hf);
					optionsString.Append("HeadFinder: " + args[i + 1] + "\n");
				}
				catch (Exception e)
				{
					log.Info(e);
					log.Info(this.GetType().FullName + ": Could not load head finder " + args[i + 1]);
				}
				i += 2;
			}
			return i;
		}

		public override ITreeReaderFactory TreeReaderFactory()
		{
			return new SpanishTreeReaderFactory();
		}

		public override IList<IHasWord> DefaultTestSentence()
		{
			string[] sent = new string[] { "Ésto", "es", "sólo", "una", "prueba", "." };
			return SentenceUtils.ToWordList(sent);
		}

		public override void Display()
		{
			log.Info(optionsString.ToString());
			base.Display();
		}

		public virtual void SetHeadFinder(IHeadFinder hf)
		{
			headFinder = hf;
			// Regenerate annotation patterns
			CompileAnnotations(headFinder);
		}
	}
}
