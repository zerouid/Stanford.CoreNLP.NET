using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	[System.Serializable]
	public abstract class FeatureFactory
	{
		public virtual IList<string> Featurize(State state)
		{
			return Featurize(state, Generics.NewArrayList<string>(200));
		}

		public abstract IList<string> Featurize(State state, IList<string> features);

		internal enum Transition
		{
			Left,
			Right,
			Unary
		}

		[System.Serializable]
		internal sealed class FeatureComponent
		{
			public static readonly FeatureFactory.FeatureComponent Headword = new FeatureFactory.FeatureComponent("W");

			public static readonly FeatureFactory.FeatureComponent Headtag = new FeatureFactory.FeatureComponent("T");

			public static readonly FeatureFactory.FeatureComponent Value = new FeatureFactory.FeatureComponent("C");

			private readonly string shortName;

			internal FeatureComponent(string shortName)
			{
				this.shortName = shortName;
			}

			public string ShortName()
			{
				return FeatureFactory.FeatureComponent.shortName;
			}
		}

		internal const string Null = "*NULL*";

		public static string GetFeatureFromCoreLabel(CoreLabel label, FeatureFactory.FeatureComponent feature)
		{
			string value = null;
			switch (feature)
			{
				case FeatureFactory.FeatureComponent.Headword:
				{
					value = (label == null) ? Null : label.Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation)).Value();
					break;
				}

				case FeatureFactory.FeatureComponent.Headtag:
				{
					value = (label == null) ? Null : label.Get(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation)).Value();
					break;
				}

				case FeatureFactory.FeatureComponent.Value:
				{
					value = (label == null) ? Null : label.Value();
					break;
				}

				default:
				{
					throw new ArgumentException("Unexpected feature type: " + feature);
				}
			}
			return value;
		}

		public static CoreLabel GetRecentDependent(TreeShapedStack<Tree> stack, FeatureFactory.Transition transition, int nodeNum)
		{
			if (stack.Size() <= nodeNum)
			{
				return null;
			}
			for (int i = 0; i < nodeNum; ++i)
			{
				stack = stack.Pop();
			}
			Tree node = stack.Peek();
			if (node == null)
			{
				return null;
			}
			if (!(node.Label() is CoreLabel))
			{
				throw new ArgumentException("Can only featurize CoreLabel trees");
			}
			CoreLabel head = ((CoreLabel)node.Label()).Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation));
			switch (transition)
			{
				case FeatureFactory.Transition.Left:
				{
					while (true)
					{
						if (node.Children().Length == 0)
						{
							return null;
						}
						Tree child = node.Children()[0];
						if (!(child.Label() is CoreLabel))
						{
							throw new ArgumentException("Can only featurize CoreLabel trees");
						}
						if (((CoreLabel)child.Label()).Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation)) != head)
						{
							return (CoreLabel)child.Label();
						}
						node = child;
					}
					goto case FeatureFactory.Transition.Right;
				}

				case FeatureFactory.Transition.Right:
				{
					while (true)
					{
						if (node.Children().Length == 0)
						{
							return null;
						}
						if (node.Children().Length == 1)
						{
							node = node.Children()[0];
							continue;
						}
						Tree child = node.Children()[1];
						if (!(child.Label() is CoreLabel))
						{
							throw new ArgumentException("Can only featurize CoreLabel trees");
						}
						if (((CoreLabel)child.Label()).Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation)) != head)
						{
							return (CoreLabel)child.Label();
						}
						node = child;
					}
					goto default;
				}

				default:
				{
					throw new ArgumentException("Can only get left or right heads");
				}
			}
		}

		public static CoreLabel GetStackLabel(TreeShapedStack<Tree> stack, int nodeNum, params FeatureFactory.Transition[] transitions)
		{
			if (stack.Size() <= nodeNum)
			{
				return null;
			}
			for (int i = 0; i < nodeNum; ++i)
			{
				stack = stack.Pop();
			}
			Tree node = stack.Peek();
			// TODO: this is nice for code readability, but might be expensive
			foreach (FeatureFactory.Transition t in transitions)
			{
				switch (t)
				{
					case FeatureFactory.Transition.Left:
					{
						if (node.Children().Length != 2)
						{
							return null;
						}
						node = node.Children()[0];
						break;
					}

					case FeatureFactory.Transition.Right:
					{
						if (node.Children().Length != 2)
						{
							return null;
						}
						node = node.Children()[1];
						break;
					}

					case FeatureFactory.Transition.Unary:
					{
						if (node.Children().Length != 1)
						{
							return null;
						}
						node = node.Children()[0];
						break;
					}

					default:
					{
						throw new ArgumentException("Unknown transition type " + t);
					}
				}
			}
			if (!(node.Label() is CoreLabel))
			{
				throw new ArgumentException("Can only featurize CoreLabel trees");
			}
			return (CoreLabel)node.Label();
		}

		public static CoreLabel GetQueueLabel(State state, int offset)
		{
			return GetQueueLabel(state.sentence, state.tokenPosition, offset);
		}

		public static CoreLabel GetQueueLabel(IList<Tree> sentence, int tokenPosition, int offset)
		{
			if (tokenPosition + offset < 0 || tokenPosition + offset >= sentence.Count)
			{
				return null;
			}
			Tree node = sentence[tokenPosition + offset];
			if (!(node.Label() is CoreLabel))
			{
				throw new ArgumentException("Can only featurize CoreLabel trees");
			}
			return (CoreLabel)node.Label();
		}

		public static CoreLabel GetCoreLabel(Tree node)
		{
			if (!(node.Label() is CoreLabel))
			{
				throw new ArgumentException("Can only featurize CoreLabel trees");
			}
			return (CoreLabel)node.Label();
		}

		private const long serialVersionUID = -9086427962537286031L;
	}
}
