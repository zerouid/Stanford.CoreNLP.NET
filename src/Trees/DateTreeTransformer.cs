using Edu.Stanford.Nlp.Trees.Tregex;


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Flattens the following two structures:
	/// <br />
	/// (NP (NP (NNP Month) (CD Day) )
	/// (, ,)
	/// (NP (CD Year) ))
	/// <br />
	/// becomes
	/// <br />
	/// (NP (NNP Month) (CD Day) (, ,) (CD Year) )
	/// <br />
	/// (NP (NP (NNP Month) )
	/// (NP (CD Year) ))
	/// <br />
	/// becomes
	/// <br />
	/// (NP (NNP Month) (CD Year))
	/// </summary>
	/// <author>John Bauer</author>
	public class DateTreeTransformer : ITreeTransformer
	{
		internal const string MonthRegex = "January|February|March|April|May|June|July|August|September|October|November|December|Jan\\.|Feb\\.|Mar\\.|Apr\\.|Aug\\.|Sep\\.|Sept\\.|Oct\\.|Nov\\.|Dec\\.";

		internal static readonly TregexPattern tregexMonthYear = TregexPatternCompiler.defaultCompiler.Compile("NP=root <1 (NP <: (NNP=month <: /" + MonthRegex + "/)) <2 (NP=yearnp <: (CD=year <: __)) : =root <- =yearnp");

		internal static readonly TregexPattern tregexMonthDayYear = TregexPatternCompiler.defaultCompiler.Compile("NP=root <1 (NP=monthdayroot <1 (NNP=month <: /" + MonthRegex + "/) <2 (CD=day <: __)) <2 (/^,$/=comma <: /^,$/) <3 (NP=yearroot <: (CD=year <: __)) : (=root <- =yearroot) : (=monthdayroot <- =day)"
			);

		public virtual Tree TransformTree(Tree t)
		{
			TregexMatcher matcher = tregexMonthYear.Matcher(t);
			while (matcher.Find())
			{
				Tree root = matcher.GetNode("root");
				Tree month = matcher.GetNode("month");
				Tree year = matcher.GetNode("year");
				Tree[] children = new Tree[] { month, year };
				root.SetChildren(children);
				matcher = tregexMonthYear.Matcher(t);
			}
			matcher = tregexMonthDayYear.Matcher(t);
			while (matcher.Find())
			{
				Tree root = matcher.GetNode("root");
				Tree month = matcher.GetNode("month");
				Tree day = matcher.GetNode("day");
				Tree comma = matcher.GetNode("comma");
				Tree year = matcher.GetNode("year");
				Tree[] children = new Tree[] { month, day, comma, year };
				root.SetChildren(children);
				matcher = tregexMonthDayYear.Matcher(t);
			}
			return t;
		}
	}
}
