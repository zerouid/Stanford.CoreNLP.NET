using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>An interface for training an UnknownWordModel.</summary>
	/// <remarks>
	/// An interface for training an UnknownWordModel.  Once initialized,
	/// you can feed it trees and then call finishTraining to get the
	/// UnknownWordModel.
	/// </remarks>
	/// <author>John Bauer</author>
	public interface IUnknownWordModelTrainer
	{
		/// <summary>
		/// Initialize the trainer with a few of the data structures it needs
		/// to train.
		/// </summary>
		/// <remarks>
		/// Initialize the trainer with a few of the data structures it needs
		/// to train.  Also, it is necessary to estimate the number of trees
		/// that it will be given, as many of the UWMs switch training modes
		/// after seeing a fraction of the trees.
		/// This is an initialization method and not part of the constructor
		/// because these Trainers are generally loaded by reflection, and
		/// making this a method instead of a constructor lets the compiler
		/// catch silly errors.
		/// </remarks>
		void InitializeTraining(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex, double totalTrees);

		/// <summary>Tallies statistics for this particular collection of trees.</summary>
		/// <remarks>
		/// Tallies statistics for this particular collection of trees.  Can
		/// be called multiple times.
		/// </remarks>
		void Train(ICollection<Tree> trees);

		/// <summary>Tallies statistics for a weighted collection of trees.</summary>
		/// <remarks>
		/// Tallies statistics for a weighted collection of trees.  Can
		/// be called multiple times.
		/// </remarks>
		void Train(ICollection<Tree> trees, double weight);

		/// <summary>Tallies statistics for a single tree.</summary>
		/// <remarks>
		/// Tallies statistics for a single tree.
		/// Can be called multiple times.
		/// </remarks>
		void Train(Tree tree, double weight);

		/// <summary>Tallies statistics for a single word.</summary>
		/// <remarks>
		/// Tallies statistics for a single word.
		/// Can be called multiple times.
		/// </remarks>
		void Train(TaggedWord tw, int loc, double weight);

		/// <summary>
		/// Maintains a (real-valued) count of how many (weighted) trees have
		/// been read in.
		/// </summary>
		/// <remarks>
		/// Maintains a (real-valued) count of how many (weighted) trees have
		/// been read in. Can be called multiple times.
		/// </remarks>
		/// <param name="weight">The weight of trees additionally trained on</param>
		void IncrementTreesRead(double weight);

		/// <summary>Returns the trained UWM.</summary>
		/// <remarks>
		/// Returns the trained UWM.  Many of the subclasses build exactly
		/// one model, and some of the finishTraining methods manipulate the
		/// data in permanent ways, so this should only be called once
		/// </remarks>
		IUnknownWordModel FinishTraining();
	}

	public static class UnknownWordModelTrainerConstants
	{
		public const string unknown = "UNK";

		public const int nullWord = -1;

		public const short nullTag = -1;

		public const IntTaggedWord NullItw = new IntTaggedWord(UnknownWordModelTrainerConstants.nullWord, UnknownWordModelTrainerConstants.nullTag);
	}
}
