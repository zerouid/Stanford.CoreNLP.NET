using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic.Pipeline
{
	/// <summary>
	/// Wrapper class for using the ATBCorrector class with TreebankPipeline's
	/// TVISITOR parameter.
	/// </summary>
	/// <author>Spence Green</author>
	public class ATBCorrectorVisitor : ITreeVisitor
	{
		private readonly ITreeTransformer atbCorrector = new ATBCorrector();

		public virtual void VisitTree(Tree t)
		{
			atbCorrector.TransformTree(t);
		}
	}
}
