using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;




namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// Stemmer, implementing the Porter Stemming Algorithm
	/// <p/>
	/// The Stemmer class transforms a word into its root form.
	/// </summary>
	/// <remarks>
	/// Stemmer, implementing the Porter Stemming Algorithm
	/// <p/>
	/// The Stemmer class transforms a word into its root form.  The input
	/// word can be provided a character at time (by calling add()), or at once
	/// by calling one of the various stem(something) methods.
	/// </remarks>
	/// <author>Sepandar Kamvar (sdkamvar@stanford.edu)</author>
	public class Stemmer : Func<Word, Word>
	{
		private char[] b;

		private int i;

		private int i_end;

		private int j;

		private int k;

		private const int Inc = 50;

		public Stemmer()
		{
			/* offset into b */
			/* offset to end of stemmed word */
			/* unit of size whereby b is increased */
			b = new char[Inc];
			i = 0;
			i_end = 0;
		}

		/// <summary>Add a character to the word being stemmed.</summary>
		/// <remarks>
		/// Add a character to the word being stemmed.  When you are finished
		/// adding characters, you can call stem(void) to stem the word.
		/// </remarks>
		private void Add(char ch)
		{
			if (i == b.Length)
			{
				char[] new_b = new char[i + Inc];
				for (int c = 0; c < i; c++)
				{
					new_b[c] = b[c];
				}
				b = new_b;
			}
			b[i++] = ch;
		}

		/// <summary>
		/// After a word has been stemmed, it can be retrieved by toString(),
		/// or a reference to the internal buffer can be retrieved by getResultBuffer
		/// and getResultLength (which is generally more efficient.)
		/// </summary>
		public override string ToString()
		{
			return new string(b, 0, i_end);
		}

		/* cons(i) is true <=> b[i] is a consonant. */
		private bool Cons(int i)
		{
			switch (b[i])
			{
				case 'a':
				case 'e':
				case 'i':
				case 'o':
				case 'u':
				{
					return false;
				}

				case 'y':
				{
					return (i == 0) ? true : !Cons(i - 1);
				}

				default:
				{
					return true;
				}
			}
		}

		/* m() measures the number of consonant sequences between 0 and j. if c is
		a consonant sequence and v a vowel sequence, and <..> indicates arbitrary
		presence,
		
		<c><v>       gives 0
		<c>vc<v>     gives 1
		<c>vcvc<v>   gives 2
		<c>vcvcvc<v> gives 3
		....
		*/
		private int M()
		{
			int n = 0;
			int i = 0;
			while (true)
			{
				if (i > j)
				{
					return n;
				}
				if (!Cons(i))
				{
					break;
				}
				i++;
			}
			i++;
			while (true)
			{
				while (true)
				{
					if (i > j)
					{
						return n;
					}
					if (Cons(i))
					{
						break;
					}
					i++;
				}
				i++;
				n++;
				while (true)
				{
					if (i > j)
					{
						return n;
					}
					if (!Cons(i))
					{
						break;
					}
					i++;
				}
				i++;
			}
		}

		/* vowelinstem() is true <=> 0,...j contains a vowel */
		private bool Vowelinstem()
		{
			int i;
			for (i = 0; i <= j; i++)
			{
				if (!Cons(i))
				{
					return true;
				}
			}
			return false;
		}

		/* doublec(j) is true <=> j,(j-1) contain a double consonant. */
		private bool Doublec(int j)
		{
			if (j < 1)
			{
				return false;
			}
			if (b[j] != b[j - 1])
			{
				return false;
			}
			return Cons(j);
		}

		/* cvc(i) is true <=> i-2,i-1,i has the form consonant - vowel - consonant
		and also if the second c is not w,x or y. this is used when trying to
		restore an e at the end of a short word. e.g.
		
		cav(e), lov(e), hop(e), crim(e), but
		snow, box, tray.
		
		*/
		private bool Cvc(int i)
		{
			if (i < 2 || !Cons(i) || Cons(i - 1) || !Cons(i - 2))
			{
				return false;
			}
			{
				int ch = b[i];
				if (ch == 'w' || ch == 'x' || ch == 'y')
				{
					return false;
				}
			}
			return true;
		}

		private bool Ends(string s)
		{
			int l = s.Length;
			int o = k - l + 1;
			if (o < 0)
			{
				return false;
			}
			for (int i = 0; i < l; i++)
			{
				if (b[o + i] != s[i])
				{
					return false;
				}
			}
			j = k - l;
			return true;
		}

		/* setto(s) sets (j+1),...k to the characters in the string s, readjusting
		k. */
		private void Setto(string s)
		{
			int l = s.Length;
			int o = j + 1;
			for (int i = 0; i < l; i++)
			{
				b[o + i] = s[i];
			}
			k = j + l;
		}

		/* r(s) is used further down. */
		private void R(string s)
		{
			if (M() > 0)
			{
				Setto(s);
			}
		}

		/* step1() gets rid of plurals and -ed or -ing. e.g.
		
		caresses  ->  caress
		ponies    ->  poni
		ties      ->  ti
		caress    ->  caress
		cats      ->  cat
		
		feed      ->  feed
		agreed    ->  agree
		disabled  ->  disable
		
		matting   ->  mat
		mating    ->  mate
		meeting   ->  meet
		milling   ->  mill
		messing   ->  mess
		
		meetings  ->  meet
		
		*/
		private void Step1()
		{
			if (b[k] == 's')
			{
				if (Ends("sses"))
				{
					k -= 2;
				}
				else
				{
					if (Ends("ies"))
					{
						Setto("i");
					}
					else
					{
						if (b[k - 1] != 's')
						{
							k--;
						}
					}
				}
			}
			if (Ends("eed"))
			{
				if (M() > 0)
				{
					k--;
				}
			}
			else
			{
				if ((Ends("ed") || Ends("ing")) && Vowelinstem())
				{
					k = j;
					if (Ends("at"))
					{
						Setto("ate");
					}
					else
					{
						if (Ends("bl"))
						{
							Setto("ble");
						}
						else
						{
							if (Ends("iz"))
							{
								Setto("ize");
							}
							else
							{
								if (Doublec(k))
								{
									k--;
									{
										int ch = b[k];
										if (ch == 'l' || ch == 's' || ch == 'z')
										{
											k++;
										}
									}
								}
								else
								{
									if (M() == 1 && Cvc(k))
									{
										Setto("e");
									}
								}
							}
						}
					}
				}
			}
		}

		/* step2() turns terminal y to i when there is another vowel in the stem. */
		private void Step2()
		{
			if (Ends("y") && Vowelinstem())
			{
				b[k] = 'i';
			}
		}

		/* step3() maps double suffices to single ones. so -ization ( = -ize plus
		-ation) maps to -ize etc. note that the string before the suffix must give
		m() > 0. */
		private void Step3()
		{
			if (k == 0)
			{
				return;
			}
			switch (b[k - 1])
			{
				case 'a':
				{
					/* For Bug 1 */
					if (Ends("ational"))
					{
						R("ate");
						break;
					}
					if (Ends("tional"))
					{
						R("tion");
						break;
					}
					break;
				}

				case 'c':
				{
					if (Ends("enci"))
					{
						R("ence");
						break;
					}
					if (Ends("anci"))
					{
						R("ance");
						break;
					}
					break;
				}

				case 'e':
				{
					if (Ends("izer"))
					{
						R("ize");
						break;
					}
					break;
				}

				case 'l':
				{
					if (Ends("bli"))
					{
						R("ble");
						break;
					}
					if (Ends("alli"))
					{
						R("al");
						break;
					}
					if (Ends("entli"))
					{
						R("ent");
						break;
					}
					if (Ends("eli"))
					{
						R("e");
						break;
					}
					if (Ends("ousli"))
					{
						R("ous");
						break;
					}
					break;
				}

				case 'o':
				{
					if (Ends("ization"))
					{
						R("ize");
						break;
					}
					if (Ends("ation"))
					{
						R("ate");
						break;
					}
					if (Ends("ator"))
					{
						R("ate");
						break;
					}
					break;
				}

				case 's':
				{
					if (Ends("alism"))
					{
						R("al");
						break;
					}
					if (Ends("iveness"))
					{
						R("ive");
						break;
					}
					if (Ends("fulness"))
					{
						R("ful");
						break;
					}
					if (Ends("ousness"))
					{
						R("ous");
						break;
					}
					break;
				}

				case 't':
				{
					if (Ends("aliti"))
					{
						R("al");
						break;
					}
					if (Ends("iviti"))
					{
						R("ive");
						break;
					}
					if (Ends("biliti"))
					{
						R("ble");
						break;
					}
					break;
				}

				case 'g':
				{
					if (Ends("logi"))
					{
						R("log");
						break;
					}
					break;
				}
			}
		}

		/* step4() deals with -ic-, -full, -ness etc. similar strategy to step3. */
		private void Step4()
		{
			switch (b[k])
			{
				case 'e':
				{
					if (Ends("icate"))
					{
						R("ic");
						break;
					}
					if (Ends("ative"))
					{
						R(string.Empty);
						break;
					}
					if (Ends("alize"))
					{
						R("al");
						break;
					}
					break;
				}

				case 'i':
				{
					if (Ends("iciti"))
					{
						R("ic");
						break;
					}
					break;
				}

				case 'l':
				{
					if (Ends("ical"))
					{
						R("ic");
						break;
					}
					if (Ends("ful"))
					{
						R(string.Empty);
						break;
					}
					break;
				}

				case 's':
				{
					if (Ends("ness"))
					{
						R(string.Empty);
						break;
					}
					break;
				}
			}
		}

		/* step5() takes off -ant, -ence etc., in context <c>vcvc<v>. */
		private void Step5()
		{
			if (k == 0)
			{
				return;
			}
			switch (b[k - 1])
			{
				case 'a':
				{
					/* for Bug 1 */
					if (Ends("al"))
					{
						break;
					}
					return;
				}

				case 'c':
				{
					if (Ends("ance"))
					{
						break;
					}
					if (Ends("ence"))
					{
						break;
					}
					return;
				}

				case 'e':
				{
					if (Ends("er"))
					{
						break;
					}
					return;
				}

				case 'i':
				{
					if (Ends("ic"))
					{
						break;
					}
					return;
				}

				case 'l':
				{
					if (Ends("able"))
					{
						break;
					}
					if (Ends("ible"))
					{
						break;
					}
					return;
				}

				case 'n':
				{
					if (Ends("ant"))
					{
						break;
					}
					if (Ends("ement"))
					{
						break;
					}
					if (Ends("ment"))
					{
						break;
					}
					/* element etc. not stripped before the m */
					if (Ends("ent"))
					{
						break;
					}
					return;
				}

				case 'o':
				{
					if (Ends("ion") && j >= 0 && (b[j] == 's' || b[j] == 't'))
					{
						break;
					}
					/* j >= 0 fixes Bug 2 */
					if (Ends("ou"))
					{
						break;
					}
					return;
				}

				case 's':
				{
					/* takes care of -ous */
					if (Ends("ism"))
					{
						break;
					}
					return;
				}

				case 't':
				{
					if (Ends("ate"))
					{
						break;
					}
					if (Ends("iti"))
					{
						break;
					}
					return;
				}

				case 'u':
				{
					if (Ends("ous"))
					{
						break;
					}
					return;
				}

				case 'v':
				{
					if (Ends("ive"))
					{
						break;
					}
					return;
				}

				case 'z':
				{
					if (Ends("ize"))
					{
						break;
					}
					return;
				}

				default:
				{
					return;
				}
			}
			if (M() > 1)
			{
				k = j;
			}
		}

		/* step6() removes a final -e if m() > 1. */
		private void Step6()
		{
			j = k;
			if (b[k] == 'e')
			{
				int a = M();
				if (a > 1 || a == 1 && !Cvc(k - 1))
				{
					k--;
				}
			}
			if (b[k] == 'l' && Doublec(k) && M() > 1)
			{
				k--;
			}
		}

		/// <summary>Stem the word placed into the Stemmer buffer through calls to add().</summary>
		/// <remarks>
		/// Stem the word placed into the Stemmer buffer through calls to add().
		/// Returns true if the stemming process resulted in a word different
		/// from the input.  You can retrieve the result with
		/// getResultLength()/getResultBuffer() or toString().
		/// </remarks>
		private void Stem()
		{
			k = i - 1;
			if (k > 1)
			{
				Step1();
				Step2();
				Step3();
				Step4();
				Step5();
				Step6();
			}
			i_end = k + 1;
			i = 0;
		}

		/// <summary>Test program for demonstrating the Stemmer.</summary>
		/// <remarks>
		/// Test program for demonstrating the Stemmer.  It reads text from a
		/// a list of files, stems each word, and writes the result to standard
		/// output. Note that the word stemmed is expected to be in lower case:
		/// forcing lower case must be done outside the Stemmer class.
		/// Usage: Stemmer file-name file-name ...
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.Process.Stemmer s = new Edu.Stanford.Nlp.Process.Stemmer();
			if (args[0].Equals("-file"))
			{
				IEnumerator<Word> it = PTBTokenizer.NewPTBTokenizer(new InputStreamReader(new FileInputStream(args[1]), "utf-8"));
				while (it.MoveNext())
				{
					Word token = it.Current;
					System.Console.Out.Write(s.Stem(token.Word()));
					System.Console.Out.Write(' ');
				}
			}
			else
			{
				foreach (string arg in args)
				{
					System.Console.Out.Write(s.Stem(arg));
					System.Console.Out.Write(' ');
				}
			}
			System.Console.Out.WriteLine();
		}

		/// <summary>Stems <code>s</code> and returns stemmed <code>String</code>.</summary>
		public virtual string Stem(string s)
		{
			char[] characters = s.ToCharArray();
			foreach (char character in characters)
			{
				Add(character);
			}
			Stem();
			return ToString();
		}

		/// <summary>Stems <code>w</code> and returns stemmed <code>Word</code>.</summary>
		public virtual Word Stem(Word w)
		{
			return (new Word(Stem(w.Word())));
		}

		/// <summary>
		/// Stems <code>word</code> (which must be a <code>Word</code>,
		/// or else
		/// a ClassCastException will be thrown, and returns stemmed
		/// <code>Word</code>.
		/// </summary>
		public virtual Word Apply(Word word)
		{
			return Stem(word);
		}
	}
}
