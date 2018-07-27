using Edu.Stanford.Nlp.Parser.Lexparser;


namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	[System.Serializable]
	public class ShiftReduceTrainOptions : TrainOptions
	{
		/// <summary>
		/// If set to 0, training outputs the last model produced, regardless
		/// of its score.
		/// </summary>
		/// <remarks>
		/// If set to 0, training outputs the last model produced, regardless
		/// of its score.  Otherwise it takes the best k models and averages
		/// them together.
		/// </remarks>
		public int averagedModels = 8;

		/// <summary>
		/// Cross-validate over the number of models to average, using the
		/// dev set, to figure out which number between 1 and averagedModels
		/// we actually want to use
		/// </summary>
		public bool cvAveragedModels = true;

		public enum TrainingMethod
		{
			EarlyTermination,
			Gold,
			Oracle,
			ReorderOracle,
			Beam,
			ReorderBeam
		}

		public ShiftReduceTrainOptions.TrainingMethod trainingMethod = ShiftReduceTrainOptions.TrainingMethod.EarlyTermination;

		public const int DefaultBeamSize = 4;

		public int beamSize = 0;

		/// <summary>How many times a feature must be seen when training.</summary>
		/// <remarks>How many times a feature must be seen when training.  Less than this and it is filtered.</remarks>
		public int featureFrequencyCutoff = 0;

		/// <summary>Saves intermediate models, but that takes up a lot of space</summary>
		public bool saveIntermediateModels = false;

		/// <summary>If we cut off features with featureFrequencyCutoff, this retrains with only the existing features</summary>
		public bool retrainAfterCutoff = true;

		/// <summary>Does not seem to help...</summary>
		/// <remarks>Does not seem to help... perhaps there is a logic bug in how to compensate for missed binary transitions</remarks>
		public bool oracleShiftToBinary = false;

		/// <summary>Does help, but makes the models much bigger for a miniscule gain</summary>
		public bool oracleBinaryToShift = false;

		/// <summary>If positive, every 10 iterations, multiply the learning rate by this amount.</summary>
		public double decayLearningRate = 0.0;

		private const long serialVersionUID = -8158249539308373819L;
		// version id randomly chosen by forgetting to set the version id when serializing models
	}
}
