using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Awt;
using Sharpen;

namespace Edu.Stanford.Nlp.Swing
{
	/// <summary>Detects which Fonts can be used to display unicode characters in a given language.</summary>
	/// <author>Huy Nguyen (htnguyen@cs.stanford.edu)</author>
	/// <author>Christopher Manning</author>
	public class FontDetector
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Swing.FontDetector));

		public const int NumLanguages = 2;

		public const int Chinese = 0;

		public const int Arabic = 1;

		private static readonly string[][] unicodeRanges = new string[NumLanguages][];

		static FontDetector()
		{
			unicodeRanges[Chinese] = new string[] { "\u3001", "\uFF01", "\uFFEE", "\u0374", "\u3126" };
			// The U+FB50-U+FDFF range is Arabic Presentation forms A.
			// The U+FE70=U+FEFE range is Araic presentation forms B. We probably won't need them.
			unicodeRanges[Arabic] = new string[] { "\uFB50", "\uFE70" };
		}

		private FontDetector()
		{
		}

		/// <summary>Returns which Fonts on the system can display the sample string.</summary>
		/// <param name="language">the numerical code for the language to check</param>
		/// <returns>a list of Fonts which can display the sample String</returns>
		public static IList<Font> SupportedFonts(int language)
		{
			if (language < 0 || language > NumLanguages)
			{
				throw new ArgumentException();
			}
			IList<Font> fonts = new List<Font>();
			Font[] systemFonts = GraphicsEnvironment.GetLocalGraphicsEnvironment().GetAllFonts();
			foreach (Font systemFont in systemFonts)
			{
				bool canDisplay = true;
				for (int j = 0; j < unicodeRanges[language].Length; j++)
				{
					if (systemFont.CanDisplayUpTo(unicodeRanges[language][j]) != -1)
					{
						canDisplay = false;
						break;
					}
				}
				if (canDisplay)
				{
					fonts.Add(systemFont);
				}
			}
			return fonts;
		}

		public static bool HasFont(string fontName)
		{
			Font[] systemFonts = GraphicsEnvironment.GetLocalGraphicsEnvironment().GetAllFonts();
			foreach (Font systemFont in systemFonts)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(systemFont.GetName(), fontName))
				{
					return true;
				}
			}
			return false;
		}

		public static void Main(string[] args)
		{
			IList<Font> fonts = SupportedFonts(Arabic);
			log.Info("Has MS Mincho? " + HasFont("MS Mincho"));
			foreach (Font font in fonts)
			{
				System.Console.Out.WriteLine(font.GetName());
			}
		}
	}
}
