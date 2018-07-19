using Edu.Stanford.Nlp.Ling.Tokensregex.Types;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Pattern for matching a CoreMap using a generic expression</summary>
	/// <author>Angel Chang</author>
	[System.Serializable]
	public class CoreMapExpressionNodePattern : NodePattern<ICoreMap>
	{
		internal Env env;

		internal IExpression expression;

		public CoreMapExpressionNodePattern()
		{
		}

		public CoreMapExpressionNodePattern(Env env, IExpression expression)
		{
			this.env = env;
			this.expression = expression;
		}

		public static Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapExpressionNodePattern ValueOf(IExpression expression)
		{
			return ValueOf(null, expression);
		}

		public static Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapExpressionNodePattern ValueOf(Env env, IExpression expression)
		{
			Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapExpressionNodePattern p = new Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapExpressionNodePattern(env, expression);
			return p;
		}

		public override bool Match(ICoreMap token)
		{
			IValue v = expression.Evaluate(env, token);
			bool matched = Expressions.ConvertValueToBoolean(v, false);
			return (matched != null) ? matched : false;
		}

		public override string ToString()
		{
			return expression.ToString();
		}
	}
}
