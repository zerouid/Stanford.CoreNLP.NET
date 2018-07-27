/*
* 	@Author:  Danqi Chen
* 	@Email:  danqi@cs.stanford.edu
*	@Created:  2014-09-01
* 	@Last Modified:  2014-09-30
*/
using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Parser.Nndep
{
	/// <summary>Defines a list of training / testing examples in multi-class classification setting.</summary>
	/// <author>Danqi Chen</author>
	public class Dataset
	{
		internal int n;

		internal readonly int numFeatures;

		internal readonly int numLabels;

		internal readonly IList<Example> examples;

		internal Dataset(int numFeatures, int numLabels)
		{
			n = 0;
			this.numFeatures = numFeatures;
			this.numLabels = numLabels;
			examples = new List<Example>();
		}

		public virtual void AddExample(IList<int> feature, IList<int> label)
		{
			Example data = new Example(feature, label);
			n += 1;
			examples.Add(data);
		}
	}
}
