

namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>Indicates that this function can build a clique potential function for external use</summary>
	/// <author>Mengqiu Wang</author>
	public interface IHasCliquePotentialFunction
	{
		ICliquePotentialFunction GetCliquePotentialFunction(double[] x);
	}
}
