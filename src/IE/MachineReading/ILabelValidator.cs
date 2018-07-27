using Edu.Stanford.Nlp.IE.Machinereading.Structure;


namespace Edu.Stanford.Nlp.IE.Machinereading
{
	public interface ILabelValidator
	{
		bool ValidLabel(string label, ExtractionObject @object);
	}
}
