using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Org.Ejml.Simple;


namespace Edu.Stanford.Nlp.Neural.Rnn
{
	/// <summary>Annotations used by Tree Recursive Neural Networks.</summary>
	/// <author>John Bauer</author>
	public class RNNCoreAnnotations
	{
		private RNNCoreAnnotations()
		{
		}

		/// <summary>Used to denote the vector (distributed representation) at a particular node.</summary>
		/// <remarks>
		/// Used to denote the vector (distributed representation) at a particular node.
		/// This stores a real vector that represents the semantics of a word or phrase.
		/// </remarks>
		public class NodeVector : ICoreAnnotation<SimpleMatrix>
		{
			// only static members
			public virtual Type GetType()
			{
				return typeof(SimpleMatrix);
			}
		}

		/// <summary>Get the vector (distributed representation) at a particular node.</summary>
		/// <param name="tree">The tree node</param>
		/// <returns>The vector (distributed representation) of the given tree</returns>
		public static SimpleMatrix GetNodeVector(Tree tree)
		{
			ILabel label = tree.Label();
			if (!(label is CoreLabel))
			{
				throw new ArgumentException("CoreLabels required to get the attached node vector");
			}
			return ((CoreLabel)label).Get(typeof(RNNCoreAnnotations.NodeVector));
		}

		/// <summary>Used to denote a vector of predictions at a particular node.</summary>
		/// <remarks>
		/// Used to denote a vector of predictions at a particular node.
		/// This is a vector of real values, typically the output of a softmax classification layer,
		/// which gives the probabilities of each output value.
		/// </remarks>
		public class Predictions : ICoreAnnotation<SimpleMatrix>
		{
			public virtual Type GetType()
			{
				return typeof(SimpleMatrix);
			}
		}

		public static SimpleMatrix GetPredictions(Tree tree)
		{
			ILabel label = tree.Label();
			if (!(label is CoreLabel))
			{
				throw new ArgumentException("CoreLabels required to get the attached predictions");
			}
			return ((CoreLabel)label).Get(typeof(RNNCoreAnnotations.Predictions));
		}

		public static IList<double> GetPredictionsAsStringList(Tree tree)
		{
			SimpleMatrix predictions = GetPredictions(tree);
			IList<double> listOfPredictions = new List<double>();
			for (int i = 0; i < predictions.NumRows(); i++)
			{
				listOfPredictions.Add(predictions.Get(i));
			}
			return listOfPredictions;
		}

		/// <summary>Get the argmax of the class predictions.</summary>
		/// <remarks>
		/// Get the argmax of the class predictions.
		/// The predicted classes can be an arbitrary set of non-negative integer classes,
		/// but in our current sentiment models, the values used are on a 5-point
		/// scale of 0 = very negative, 1 = negative, 2 = neutral, 3 = positive,
		/// and 4 = very positive.
		/// </remarks>
		public class PredictedClass : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>Return as an int the predicted class.</summary>
		/// <remarks>
		/// Return as an int the predicted class. If it is not defined for a node,
		/// it will return -1
		/// </remarks>
		/// <returns>Either the sentiment level or -1 if none</returns>
		public static int GetPredictedClass(Tree tree)
		{
			return GetPredictedClass(tree.Label());
		}

		/// <summary>Return as an int the predicted class.</summary>
		/// <remarks>
		/// Return as an int the predicted class. If it is not defined for a node,
		/// it will return -1
		/// </remarks>
		/// <returns>Either the sentiment level or -1 if none</returns>
		public static int GetPredictedClass(ILabel label)
		{
			if (!(label is CoreLabel))
			{
				throw new ArgumentException("CoreLabels required to get the attached predicted class");
			}
			int val = ((CoreLabel)label).Get(typeof(RNNCoreAnnotations.PredictedClass));
			return val == null ? -1 : val;
		}

		/// <summary>Return as a double the probability of the predicted class.</summary>
		/// <remarks>
		/// Return as a double the probability of the predicted class. If it is not defined for a node,
		/// it will return -1
		/// </remarks>
		/// <returns>Either the label probability or -1.0 if none</returns>
		public static double GetPredictedClassProb(ILabel label)
		{
			if (!(label is CoreLabel))
			{
				throw new ArgumentException("CoreLabels required to get the attached predicted class probability");
			}
			int val = ((CoreLabel)label).Get(typeof(RNNCoreAnnotations.PredictedClass));
			SimpleMatrix predictions = ((CoreLabel)label).Get(typeof(RNNCoreAnnotations.Predictions));
			if (val != null)
			{
				return predictions.Get(val);
			}
			else
			{
				return -1.0;
			}
		}

		/// <summary>The index of the correct class.</summary>
		public class GoldClass : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		public static int GetGoldClass(Tree tree)
		{
			ILabel label = tree.Label();
			if (!(label is CoreLabel))
			{
				throw new ArgumentException("CoreLabels required to get the attached gold class");
			}
			return ((CoreLabel)label).Get(typeof(RNNCoreAnnotations.GoldClass));
		}

		public static void SetGoldClass(Tree tree, int goldClass)
		{
			ILabel label = tree.Label();
			if (!(label is CoreLabel))
			{
				throw new ArgumentException("CoreLabels required to set the attached gold class");
			}
			((CoreLabel)label).Set(typeof(RNNCoreAnnotations.GoldClass), goldClass);
		}

		public class PredictionError : ICoreAnnotation<double>
		{
			public virtual Type GetType()
			{
				return typeof(double);
			}
		}

		public static double GetPredictionError(Tree tree)
		{
			ILabel label = tree.Label();
			if (!(label is CoreLabel))
			{
				throw new ArgumentException("CoreLabels required to get the attached prediction error");
			}
			return ((CoreLabel)label).Get(typeof(RNNCoreAnnotations.PredictionError));
		}

		public static void SetPredictionError(Tree tree, double error)
		{
			ILabel label = tree.Label();
			if (!(label is CoreLabel))
			{
				throw new ArgumentException("CoreLabels required to set the attached prediction error");
			}
			((CoreLabel)label).Set(typeof(RNNCoreAnnotations.PredictionError), error);
		}
	}
}
