

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A callback function (along the lines of Berkeley optimization repo), which is currently used in the optimization package.</summary>
	/// <remarks>
	/// A callback function (along the lines of Berkeley optimization repo), which is currently used in the optimization package.
	/// In the optimization package, it is used for passing values (newX, iteration,  newObjectiveValue, newGradient) at every iteration and
	/// then you can do whatever you want with those values.
	/// One use case is to print the values in a file; another is do some sanity check etc.
	/// *
	/// Created by sonalg on 2/4/15.
	/// </remarks>
	public abstract class CallbackFunction
	{
		public abstract void Callback(params object[] args);
	}
}
