

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>Indicates that an minimizer supports grouping features for g-lasso or ae-lasso</summary>
	/// <author>Mengqiu Wang</author>
	public interface IHasFeatureGrouping
	{
		int[][] GetFeatureGrouping();
	}
}
