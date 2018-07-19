using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading
{
	public interface ILabelValidator
	{
		bool ValidLabel(string label, ExtractionObject @object);
	}
}
