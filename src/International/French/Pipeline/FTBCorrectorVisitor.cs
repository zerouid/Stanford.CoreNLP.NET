using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.International.French.Pipeline
{
	/// <summary>
	/// Wrapper class for using the ATBCorrector class with TreebankPipeline's
	/// TVISITOR parameter.
	/// </summary>
	/// <author>Spence Green</author>
	public class FTBCorrectorVisitor : ITreeVisitor
	{
		private readonly ITreeTransformer ftbCorrector = new FTBCorrector();

		public virtual void VisitTree(Tree t)
		{
			ftbCorrector.TransformTree(t);
		}
	}
}
