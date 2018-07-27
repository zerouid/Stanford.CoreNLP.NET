using Edu.Stanford.Nlp.IE.Machinereading.Structure;


namespace Edu.Stanford.Nlp.IE.Machinereading
{
	[System.Serializable]
	public class NilLabelValidator : ILabelValidator
	{
		private const long serialVersionUID = 1L;

		public virtual bool ValidLabel(string label, ExtractionObject @object)
		{
			return true;
		}
	}
}
