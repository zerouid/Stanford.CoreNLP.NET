using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Text;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// A class for calculating precision and recall statistics based on
	/// comparisons between two
	/// <see cref="System.Collections.ICollection{E}"/>
	/// s.
	/// Allows flexible specification of:
	/// <p/>
	/// <ul>
	/// <li>The criterion by which to evaluate whether two Objects are equivalent
	/// for purposes of precision and recall
	/// calculation (specified by an
	/// <see cref="IEqualityChecker{T}"/>
	/// instance)
	/// <li>The criterion by which Objects are grouped into equivalence classes
	/// for purposes of calculating subclass precision
	/// and recall (specified by an
	/// <see cref="IEquivalenceClasser{IN, OUT}"/>
	/// instance)
	/// <li>Evaluation is set-based or bag-based (by default, it is set-based). For example, if a gold collection
	/// has {a,a,b} and a guess collection has {a,b}, then recall is 100% in set-based
	/// evaluation, but is 66.67% in bag-based evaluation.
	/// </ul>
	/// Note that for set-based evaluation, sets are always constructed using object equality, NOT
	/// equality on the basis of an
	/// <see cref="IEqualityChecker{T}"/>
	/// if one is given.  If set-based evaluation
	/// were conducted on the basis of an EqualityChecker, then there would be indeterminacy when it did not subsume the
	/// <see cref="IEquivalenceClasser{IN, OUT}"/>
	/// ,
	/// if one was given. For example, if objects of the form
	/// X:y were equivalence-classed by the left criterion and evaluated for equality on the right, then set-based
	/// evaluation based on the equality checker would be indeterminate for a collection of {A:a,B:a}
	/// because it would be unclear whether to use the first or second element of the collection.
	/// </summary>
	/// <author>Roger Levy</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) Attempt at templatization... this may be a failure.</author>
	public class EquivalenceClassEval<In, Out>
	{
		/// <summary>If bagEval is set to <code>true</code>, then multiple instances of the same item will not be merged.</summary>
		/// <remarks>
		/// If bagEval is set to <code>true</code>, then multiple instances of the same item will not be merged.  For example,
		/// gold (a,a,b) against guess (a,b) will be scored as 100% precision and 66.67% recall. It is <code>false</code>
		/// by default.
		/// </remarks>
		public virtual void SetBagEval(bool bagEval)
		{
			this.bagEval = bagEval;
		}

		protected internal bool bagEval = false;

		/// <summary>Maps all objects to the equivalence class <code>null</code></summary>
		public static readonly IEquivalenceClasser NullEquivalenceClasser = null;

		public static IEquivalenceClasser<T, U> NullEquivalenceClasser<T, U>()
		{
			return ErasureUtils.UncheckedCast<IEquivalenceClasser<T, U>>(NullEquivalenceClasser);
		}

		private bool verbose = false;

		internal IEquivalenceClasser<IN, OUT> eq;

		internal EquivalenceClassEval.Eval.CollectionContainsChecker<IN> checker;

		internal string summaryName;

		/// <summary>
		/// Specifies a default EquivalenceClassEval, using
		/// <see cref="object.Equals(object)"/>
		/// as equality criterion
		/// and grouping all items into the "null" equivalence class for reporting purposes
		/// </summary>
		public EquivalenceClassEval()
			: this(Edu.Stanford.Nlp.Stats.EquivalenceClassEval.NullEquivalenceClasser<IN, OUT>())
		{
		}

		/// <summary>
		/// Specifies an EquivalenceClassEval using
		/// <see cref="object.Equals(object)"/>
		/// as equality criterion
		/// and grouping all items according to the EquivalenceClasser argument.
		/// </summary>
		public EquivalenceClassEval(IEquivalenceClasser<IN, OUT> eq)
			: this(eq, string.Empty)
		{
		}

		/// <summary>
		/// Specifies an EquivalenceClassEval using the Eval.EqualityChecker argument as equality criterion
		/// and grouping all items into a single equivalence class for reporting statistics.
		/// </summary>
		public EquivalenceClassEval(EquivalenceClassEval.IEqualityChecker<IN> e)
			: this(Edu.Stanford.Nlp.Stats.EquivalenceClassEval.NullEquivalenceClasser<IN, OUT>(), e)
		{
		}

		/// <summary>
		/// Specifies an EquivalenceClassEval using
		/// <see cref="object.Equals(object)"/>
		/// as equality criterion
		/// and grouping all items according to the EquivalenceClasser argument.
		/// </summary>
		public EquivalenceClassEval(IEquivalenceClasser<IN, OUT> eq, string name)
			: this(eq, Edu.Stanford.Nlp.Stats.EquivalenceClassEval.DefaultChecker<IN>(), name)
		{
		}

		/// <summary>
		/// Specifies an EquivalenceClassEval using the Eval.EqualityChecker argument as equality criterion
		/// and grouping all items according to the EquivalenceClasser argument.
		/// </summary>
		public EquivalenceClassEval(IEquivalenceClasser<IN, OUT> eq, EquivalenceClassEval.IEqualityChecker<IN> e)
			: this(eq, e, string.Empty)
		{
		}

		/// <summary>
		/// Specifies an EquivalenceClassEval using the Eval.EqualityChecker argument as equality criterion
		/// and grouping all items according to the EquivalenceClasser argument.
		/// </summary>
		public EquivalenceClassEval(IEquivalenceClasser<IN, OUT> eq, EquivalenceClassEval.IEqualityChecker<IN> e, string summaryName)
			: this(eq, new EquivalenceClassEval.Eval.CollectionContainsChecker<IN>(e), summaryName)
		{
		}

		internal EquivalenceClassEval(IEquivalenceClasser<IN, OUT> eq, EquivalenceClassEval.Eval.CollectionContainsChecker<IN> checker, string summaryName)
		{
			{
				//Eval eval = new Eval();
				// this one is all side effects
				/* returns a Pair of each */
				/* there is some discomfort here, we should really be using an EqualityChecker for checker, but
				* I screwed up the API. */
				//   public static String formatNumber(double d) {
				//     double frac = d % 1.0;
				//     int whole = (int) Math.round(d - frac);
				//     int frac1 = (int) Math.round(frac * 1000);
				//     String prePad = "";
				//     if(whole < 1000)
				//       prePad += " ";
				//     if(whole > 100)
				//       prePad += " ";
				//     if(whole > 10)
				//       prePad += " ";
				//     return pad + whole + "." + frac1;
				//   }
				numberFormat.SetMaximumFractionDigits(4);
				numberFormat.SetMinimumFractionDigits(4);
				numberFormat.SetMinimumIntegerDigits(1);
				numberFormat.SetMaximumIntegerDigits(1);
			}
			this.eq = eq;
			this.checker = checker;
			this.summaryName = summaryName;
		}

		internal ClassicCounter<OUT> guessed = new ClassicCounter<OUT>();

		internal ClassicCounter<OUT> guessedCorrect = new ClassicCounter<OUT>();

		internal ClassicCounter<OUT> gold = new ClassicCounter<OUT>();

		internal ClassicCounter<OUT> goldCorrect = new ClassicCounter<OUT>();

		private ClassicCounter<OUT> lastPrecision = new ClassicCounter<OUT>();

		private ClassicCounter<OUT> lastRecall = new ClassicCounter<OUT>();

		private ClassicCounter<OUT> lastF1 = new ClassicCounter<OUT>();

		private ClassicCounter<OUT> previousGuessed;

		private ClassicCounter<OUT> previousGuessedCorrect;

		private ClassicCounter<OUT> previousGold;

		private ClassicCounter<OUT> previousGoldCorrect;

		/// <summary>
		/// Adds a round of evaluation between guesses and golds
		/// <see cref="System.Collections.ICollection{E}"/>
		/// s to the tabulated statistics of
		/// the evaluation.
		/// </summary>
		public virtual void Eval(ICollection<IN> guesses, ICollection<IN> golds)
		{
			Eval(guesses, golds, new PrintWriter(System.Console.Out, true));
		}

		/// <param name="guesses">Collection of guessed objects</param>
		/// <param name="golds">Collection of gold-standard objects</param>
		/// <param name="pw">
		/// 
		/// <see cref="Java.IO.PrintWriter"/>
		/// to print eval stats
		/// </param>
		public virtual void Eval(ICollection<IN> guesses, ICollection<IN> golds, PrintWriter pw)
		{
			if (verbose)
			{
				System.Console.Out.WriteLine("evaluating precision...");
			}
			Pair<ClassicCounter<OUT>, ClassicCounter<OUT>> precision = EvalPrecision(guesses, golds);
			previousGuessed = precision.First();
			Counters.AddInPlace(guessed, previousGuessed);
			previousGuessedCorrect = precision.Second();
			Counters.AddInPlace(guessedCorrect, previousGuessedCorrect);
			if (verbose)
			{
				System.Console.Out.WriteLine("evaluating recall...");
			}
			Pair<ClassicCounter<OUT>, ClassicCounter<OUT>> recall = EvalPrecision(golds, guesses);
			previousGold = recall.First();
			Counters.AddInPlace(gold, previousGold);
			previousGoldCorrect = recall.Second();
			Counters.AddInPlace(goldCorrect, previousGoldCorrect);
		}

		internal virtual Pair<ClassicCounter<OUT>, ClassicCounter<OUT>> EvalPrecision(ICollection<IN> guesses, ICollection<IN> golds)
		{
			ICollection<IN> internalGuesses = null;
			ICollection<IN> internalGolds = null;
			if (bagEval)
			{
				internalGuesses = new List<IN>(guesses.Count);
				internalGolds = new List<IN>(golds.Count);
			}
			else
			{
				internalGuesses = Generics.NewHashSet(guesses.Count);
				internalGolds = Generics.NewHashSet(golds.Count);
			}
			Sharpen.Collections.AddAll(internalGuesses, guesses);
			Sharpen.Collections.AddAll(internalGolds, golds);
			ClassicCounter<OUT> thisGuessed = new ClassicCounter<OUT>();
			ClassicCounter<OUT> thisCorrect = new ClassicCounter<OUT>();
			foreach (IN o in internalGuesses)
			{
				OUT equivalenceClass = eq.EquivalenceClass(o);
				thisGuessed.IncrementCount(equivalenceClass);
				if (checker.Contained(o, internalGolds))
				{
					thisCorrect.IncrementCount(equivalenceClass);
					RemoveItem(o, internalGolds, checker);
				}
				else
				{
					if (verbose)
					{
						System.Console.Out.WriteLine("Eval missed " + o);
					}
				}
			}
			return Generics.NewPair(thisGuessed, thisCorrect);
		}

		protected internal static void RemoveItem<T>(T o, ICollection<T> c, EquivalenceClassEval.Eval.CollectionContainsChecker<T> checker)
		{
			foreach (T o1 in c)
			{
				if (checker.Contained(o, Java.Util.Collections.Singleton(o1)))
				{
					c.Remove(o1);
					return;
				}
			}
		}

		/// <summary>
		/// Displays the cumulative results of the evaluation to
		/// <see cref="System.Console.Out"/>
		/// .
		/// </summary>
		public virtual void Display()
		{
			Display(new PrintWriter(System.Console.Out, true));
		}

		/// <summary>Displays the cumulative results of the evaluation.</summary>
		public virtual void Display(PrintWriter pw)
		{
			pw.Println("*********Final " + summaryName + " eval stats by antecedent category***********");
			ICollection<OUT> keys = Generics.NewHashSet();
			Sharpen.Collections.AddAll(keys, guessed.KeySet());
			Sharpen.Collections.AddAll(keys, gold.KeySet());
			DisplayHelper(keys, pw, guessed, guessedCorrect, gold, goldCorrect);
			pw.Println("Finished final " + summaryName + " eval stats.");
		}

		/// <summary>
		/// Displays the results of the previous Collection pair evaluation to
		/// <see cref="System.Console.Out"/>
		/// .
		/// </summary>
		public virtual void DisplayLast()
		{
			DisplayLast(new PrintWriter(System.Console.Out, true));
		}

		/// <summary>Displays the results of the previous Collection pair evaluation.</summary>
		public virtual void DisplayLast(PrintWriter pw)
		{
			ICollection<OUT> keys = Generics.NewHashSet();
			Sharpen.Collections.AddAll(keys, previousGuessed.KeySet());
			Sharpen.Collections.AddAll(keys, previousGold.KeySet());
			DisplayHelper(keys, pw, previousGuessed, previousGuessedCorrect, previousGold, previousGoldCorrect);
		}

		public virtual double Precision(OUT key)
		{
			return Percentage(key, guessed, guessedCorrect);
		}

		public virtual double Recall(OUT key)
		{
			return Percentage(key, gold, goldCorrect);
		}

		public virtual double LastPrecision(OUT key)
		{
			return Percentage(key, previousGuessed, previousGuessedCorrect);
		}

		public virtual ClassicCounter<OUT> LastPrecision()
		{
			ClassicCounter<OUT> result = new ClassicCounter<OUT>();
			Counters.AddInPlace(result, previousGuessedCorrect);
			Counters.DivideInPlace(result, previousGuessed);
			return result;
		}

		public virtual double LastRecall(OUT key)
		{
			return Percentage(key, previousGold, previousGoldCorrect);
		}

		public virtual ClassicCounter<OUT> LastRecall()
		{
			ClassicCounter<OUT> result = new ClassicCounter<OUT>();
			Counters.AddInPlace(result, previousGoldCorrect);
			Counters.DivideInPlace(result, previousGold);
			return result;
		}

		public virtual double LastNumGuessed(OUT key)
		{
			return previousGuessed.GetCount(key);
		}

		public virtual ClassicCounter<OUT> LastNumGuessed()
		{
			return previousGuessed;
		}

		public virtual ClassicCounter<OUT> LastNumGuessedCorrect()
		{
			return previousGuessedCorrect;
		}

		public virtual double LastNumGolds(OUT key)
		{
			return previousGold.GetCount(key);
		}

		public virtual ClassicCounter<OUT> LastNumGolds()
		{
			return previousGold;
		}

		public virtual ClassicCounter<OUT> LastNumGoldsCorrect()
		{
			return previousGoldCorrect;
		}

		public virtual double F1(OUT key)
		{
			return F1(Precision(key), Recall(key));
		}

		public virtual double LastF1(OUT key)
		{
			return F1(LastPrecision(key), LastRecall(key));
		}

		public virtual ClassicCounter<OUT> LastF1()
		{
			ClassicCounter<OUT> result = new ClassicCounter<OUT>();
			ICollection<OUT> keys = Sets.Union(previousGuessed.KeySet(), previousGold.KeySet());
			foreach (OUT key in keys)
			{
				result.SetCount(key, LastF1(key));
			}
			return result;
		}

		public static double F1(double precision, double recall)
		{
			return (precision == 0.0 || recall == 0.0) ? 0.0 : (2 * precision * recall) / (precision + recall);
		}

		public static ICounter<E> F1<E>(ICounter<E> precision, ICounter<E> recall)
		{
			ICounter<E> result = precision.GetFactory().Create();
			foreach (E key in Sets.Intersection(precision.KeySet(), recall.KeySet()))
			{
				result.SetCount(key, F1(precision.GetCount(key), recall.GetCount(key)));
			}
			return result;
		}

		private double Percentage(OUT key, ClassicCounter<OUT> guessed, ClassicCounter<OUT> guessedCorrect)
		{
			double thisGuessed = guessed.GetCount(key);
			double thisGuessedCorrect = guessedCorrect.GetCount(key);
			return (thisGuessed == 0.0) ? 0.0 : thisGuessedCorrect / thisGuessed;
		}

		private void DisplayHelper(ICollection<OUT> keys, PrintWriter pw, ClassicCounter<OUT> guessed, ClassicCounter<OUT> guessedCorrect, ClassicCounter<OUT> gold, ClassicCounter<OUT> goldCorrect)
		{
			IDictionary<OUT, string> pads = GetPads(keys);
			foreach (OUT key in keys)
			{
				double thisGuessed = guessed.GetCount(key);
				double thisGuessedCorrect = guessedCorrect.GetCount(key);
				double precision = (thisGuessed == 0.0) ? 0.0 : thisGuessedCorrect / thisGuessed;
				lastPrecision.SetCount(key, precision);
				double thisGold = gold.GetCount(key);
				double thisGoldCorrect = goldCorrect.GetCount(key);
				double recall = (thisGold == 0.0) ? 0.0 : thisGoldCorrect / thisGold;
				lastRecall.SetCount(key, recall);
				double f1 = F1(precision, recall);
				lastF1.SetCount(key, f1);
				string pad = pads[key];
				pw.Println(key + pad + "\t" + "P: " + FormatNumber(precision) + "\ton " + FormatCount(thisGuessed) + " objects\tR: " + FormatNumber(recall) + "\ton " + FormatCount(thisGold) + " objects\tF1: " + FormatNumber(f1));
			}
		}

		private static NumberFormat numberFormat = NumberFormat.GetNumberInstance();

		private static string FormatNumber(double d)
		{
			return numberFormat.Format(d);
		}

		private static int FormatCount(double d)
		{
			return (int)Math.Round(d);
		}

		/* find pads for each key based on length of longest key */
		private static IDictionary<OUT, string> GetPads<Out>(ICollection<OUT> keys)
		{
			IDictionary<OUT, string> pads = Generics.NewHashMap();
			int max = 0;
			foreach (OUT key in keys)
			{
				string keyString = key == null ? "null" : key.ToString();
				if (keyString.Length > max)
				{
					max = keyString.Length;
				}
			}
			foreach (OUT key_1 in keys)
			{
				string keyString = key_1 == null ? "null" : key_1.ToString();
				int diff = max - keyString.Length;
				string pad = string.Empty;
				for (int j = 0; j < diff; j++)
				{
					pad += " ";
				}
				pads[key_1] = pad;
			}
			return pads;
		}

		public static void Main(string[] args)
		{
			Pattern p = Pattern.Compile("^([^:]*):(.*)$");
			ICollection<string> guesses = Arrays.AsList(new string[] { "S:a", "S:b", "VP:c", "VP:d", "S:a" });
			ICollection<string> golds = Arrays.AsList(new string[] { "S:a", "S:b", "S:b", "VP:d", "VP:a" });
			EquivalenceClassEval.IEqualityChecker<string> e = null;
			IEquivalenceClasser<string, string> eq = null;
			Edu.Stanford.Nlp.Stats.EquivalenceClassEval<string, string> eval = new Edu.Stanford.Nlp.Stats.EquivalenceClassEval<string, string>(eq, e, "testing");
			eval.SetBagEval(false);
			eval.Eval(guesses, golds);
			eval.DisplayLast();
			eval.Display();
		}

		/// <summary>
		/// A strategy-type interface for specifying an equality criterion for pairs of
		/// <see cref="object"/>
		/// s.
		/// </summary>
		/// <author>Roger Levy</author>
		public interface IEqualityChecker<T>
		{
			/// <summary>
			/// Returns <code>true</code> iff <code>o1</code> and <code>o2</code> are equal by the desired
			/// evaluation criterion.
			/// </summary>
			bool AreEqual(T o1, T o2);
		}

		private sealed class _IEqualityChecker_471 : EquivalenceClassEval.IEqualityChecker
		{
			public _IEqualityChecker_471()
			{
			}

			public bool AreEqual(object o1, object o2)
			{
				return o1.Equals(o2);
			}
		}

		/// <summary>
		/// A default equality checker that uses
		/// <see cref="object.Equals(object)"/>
		/// to determine equality.
		/// </summary>
		public static readonly EquivalenceClassEval.IEqualityChecker DefaultChecker = new _IEqualityChecker_471();

		public static EquivalenceClassEval.IEqualityChecker<T> DefaultChecker<T>()
		{
			return DefaultChecker;
		}

		internal class Eval<T>
		{
			private bool bagEval = false;

			public Eval(EquivalenceClassEval.IEqualityChecker<T> e)
				: this(false, e)
			{
			}

			public Eval()
				: this(false)
			{
			}

			public Eval(bool bagEval)
				: this(bagEval, EquivalenceClassEval.DefaultChecker<T>())
			{
			}

			public Eval(bool bagEval, EquivalenceClassEval.IEqualityChecker<T> e)
			{
				checker = new EquivalenceClassEval.Eval.CollectionContainsChecker<T>(e);
				this.bagEval = bagEval;
			}

			internal EquivalenceClassEval.Eval.CollectionContainsChecker<T> checker;

			internal class CollectionContainsChecker<T>
			{
				internal EquivalenceClassEval.IEqualityChecker<T> e;

				public CollectionContainsChecker(EquivalenceClassEval.IEqualityChecker<T> e)
				{
					/* a filter that returns true iff the object is a collection that contains currentItem */
					this.e = e;
				}

				public virtual bool Contained(T obj, ICollection<T> coll)
				{
					foreach (T o in coll)
					{
						if (e.AreEqual(obj, o))
						{
							return true;
						}
					}
					return false;
				}
			}

			internal double guessed = 0.0;

			internal double guessedCorrect = 0.0;

			internal double gold = 0.0;

			internal double goldCorrect = 0.0;

			internal double lastPrecision;

			internal double lastRecall;

			internal double lastF1;

			// end class CollectionContainsChecker
			public virtual void Eval(ICollection<T> guesses, ICollection<T> golds)
			{
				Eval(guesses, golds, new PrintWriter(System.Console.Out, true));
			}

			// this one is all side effects
			public virtual void Eval(ICollection<T> guesses, ICollection<T> golds, PrintWriter pw)
			{
				double precision = EvalPrecision(guesses, golds);
				lastPrecision = precision;
				double recall = EvalRecall(guesses, golds);
				lastRecall = recall;
				double f1 = (2 * precision * recall) / (precision + recall);
				lastF1 = f1;
				guessed += guesses.Count;
				guessedCorrect += (guesses.Count == 0.0 ? 0.0 : precision * guesses.Count);
				gold += golds.Count;
				goldCorrect += (golds.Count == 0.0 ? 0.0 : recall * golds.Count);
				pw.Println("This example:\tP:\t" + precision + " R:\t" + recall + " F1:\t" + f1);
				double cumPrecision = guessedCorrect / guessed;
				double cumRecall = goldCorrect / gold;
				double cumF1 = (2 * cumPrecision * cumRecall) / (cumPrecision + cumRecall);
				pw.Println("Cumulative:\tP:\t" + cumPrecision + " R:\t" + cumRecall + " F1:\t" + cumF1);
			}

			// this has no side effects!
			public virtual double EvalPrecision(ICollection<T> guesses, ICollection<T> golds)
			{
				ICollection<T> internalGuesses;
				ICollection<T> internalGolds;
				if (bagEval)
				{
					internalGuesses = new List<T>(guesses.Count);
					internalGolds = new List<T>(golds.Count);
				}
				else
				{
					internalGuesses = Generics.NewHashSet(guesses.Count);
					internalGolds = Generics.NewHashSet(golds.Count);
				}
				Sharpen.Collections.AddAll(internalGuesses, guesses);
				Sharpen.Collections.AddAll(internalGolds, golds);
				double thisGuessed = 0.0;
				double thisGuessedCorrect = 0.0;
				foreach (T o in internalGuesses)
				{
					thisGuessed += 1.0;
					if (checker.Contained(o, internalGolds))
					{
						thisGuessedCorrect += 1.0;
						RemoveItem(o, internalGolds, checker);
					}
				}
				//       else
				// 	System.out.println("Precision eval missed " + o);
				return thisGuessedCorrect / thisGuessed;
			}

			// no side effects here either
			public virtual double EvalRecall(ICollection<T> guesses, ICollection<T> golds)
			{
				double thisGold = 0.0;
				double thisGoldCorrect = 0.0;
				foreach (T o in golds)
				{
					thisGold += 1.0;
					if (guesses.Contains(o))
					{
						thisGoldCorrect += 1.0;
					}
				}
				//       else
				// 	System.out.println("Recall eval missed " + o);
				return thisGoldCorrect / thisGold;
			}

			public virtual void Display()
			{
				Display(new PrintWriter(System.Console.Out, true));
			}

			public virtual void Display(PrintWriter pw)
			{
				double precision = guessedCorrect / guessed;
				double recall = goldCorrect / gold;
				double f1 = (2 * precision * recall) / (precision + recall);
				pw.Println("*********Final eval stats***********");
				pw.Println("P:\t" + precision + " R:\t" + recall + " F1:\t" + f1);
			}
		}

		public interface IFactory<In, Out>
		{
			Edu.Stanford.Nlp.Stats.EquivalenceClassEval<IN, OUT> EquivalenceClassEval();
		}

		/// <summary>
		/// returns a new
		/// <see cref="IFactory{IN, OUT}"/>
		/// instance that vends new EquivalenceClassEval instances with
		/// settings like <code>this</code>
		/// </summary>
		public virtual EquivalenceClassEval.IFactory<IN, OUT> Factory()
		{
			return new _IFactory_620(this);
		}

		private sealed class _IFactory_620 : EquivalenceClassEval.IFactory<IN, OUT>
		{
			public _IFactory_620()
			{
				this.bagEval1 = this._enclosing.bagEval;
				this.eq1 = this._enclosing.eq;
				this.checker1 = this._enclosing.checker;
				this.summaryName1 = this._enclosing.summaryName;
			}

			internal bool bagEval1;

			internal IEquivalenceClasser<IN, OUT> eq1;

			internal EquivalenceClassEval.Eval.CollectionContainsChecker<IN> checker1;

			internal string summaryName1;

			public Edu.Stanford.Nlp.Stats.EquivalenceClassEval<IN, OUT> EquivalenceClassEval()
			{
				Edu.Stanford.Nlp.Stats.EquivalenceClassEval<IN, OUT> e = new Edu.Stanford.Nlp.Stats.EquivalenceClassEval<IN, OUT>(this.eq1, this.checker1, this.summaryName1);
				e.SetBagEval(this.bagEval1);
				return e;
			}
		}
	}
}
