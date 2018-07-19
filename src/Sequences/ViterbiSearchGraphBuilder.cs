using Edu.Stanford.Nlp.Fsm;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Sequences
{
	/// <author>Michel Galley</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) - cleanup and filling in types</author>
	public class ViterbiSearchGraphBuilder
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(ViterbiSearchGraphBuilder));

		public static DFSA<string, int> GetGraph(ISequenceModel ts, IIndex<string> classIndex)
		{
			DFSA<string, int> viterbiSearchGraph = new DFSA<string, int>(null);
			// Set up tag options
			int length = ts.Length();
			int leftWindow = ts.LeftWindow();
			int rightWindow = ts.RightWindow();
			System.Diagnostics.Debug.Assert((rightWindow == 0));
			int padLength = length + leftWindow + rightWindow;
			// NOTE: tags[i][j]  : i is index into pos, and j into product
			int[][] tags = new int[padLength][];
			int[] tagNum = new int[padLength];
			for (int pos = 0; pos < padLength; pos++)
			{
				tags[pos] = ts.GetPossibleValues(pos);
				tagNum[pos] = tags[pos].Length;
			}
			// Set up Viterbi search graph:
			DFSAState<string, int>[][] graphStates = null;
			DFSAState<string, int> startState = null;
			DFSAState<string, int> endState = null;
			if (viterbiSearchGraph != null)
			{
				int stateId = -1;
				startState = new DFSAState<string, int>(++stateId, viterbiSearchGraph, 0.0);
				viterbiSearchGraph.SetInitialState(startState);
				graphStates = new DFSAState[length][];
				for (int pos_1 = 0; pos_1 < length; ++pos_1)
				{
					//System.err.printf("%d states at pos %d\n",tags[pos].length,pos);
					graphStates[pos_1] = new DFSAState[tags[pos_1].Length];
					for (int product = 0; product < tags[pos_1].Length; ++product)
					{
						graphStates[pos_1][product] = new DFSAState<string, int>(++stateId, viterbiSearchGraph);
					}
				}
				// Accepting state:
				endState = new DFSAState<string, int>(++stateId, viterbiSearchGraph, 0.0);
				endState.SetAccepting(true);
			}
			int[] tempTags = new int[padLength];
			// Set up product space sizes
			int[] productSizes = new int[padLength];
			int curProduct = 1;
			for (int i = 0; i < leftWindow; i++)
			{
				curProduct *= tagNum[i];
			}
			for (int pos_2 = leftWindow; pos_2 < padLength; pos_2++)
			{
				if (pos_2 > leftWindow + rightWindow)
				{
					curProduct /= tagNum[pos_2 - leftWindow - rightWindow - 1];
				}
				// shift off
				curProduct *= tagNum[pos_2];
				// shift on
				productSizes[pos_2 - rightWindow] = curProduct;
			}
			double[][] windowScore = new double[padLength][];
			// Score all of each window's options
			for (int pos_3 = leftWindow; pos_3 < leftWindow + length; pos_3++)
			{
				windowScore[pos_3] = new double[productSizes[pos_3]];
				Arrays.Fill(tempTags, tags[0][0]);
				for (int product = 0; product < productSizes[pos_3]; product++)
				{
					int p = product;
					int shift = 1;
					for (int curPos = pos_3; curPos >= pos_3 - leftWindow; curPos--)
					{
						tempTags[curPos] = tags[curPos][p % tagNum[curPos]];
						p /= tagNum[curPos];
						if (curPos > pos_3)
						{
							shift *= tagNum[curPos];
						}
					}
					if (tempTags[pos_3] == tags[pos_3][0])
					{
						// get all tags at once
						double[] scores = ts.ScoresOf(tempTags, pos_3);
						// fill in the relevant windowScores
						for (int t = 0; t < tagNum[pos_3]; t++)
						{
							windowScore[pos_3][product + t * shift] = scores[t];
						}
					}
				}
			}
			// loop over the classification spot
			for (int pos_4 = leftWindow; pos_4 < length + leftWindow; pos_4++)
			{
				// loop over window product types
				for (int product = 0; product < productSizes[pos_4]; product++)
				{
					if (pos_4 == leftWindow)
					{
						// all nodes in the first spot link to startState:
						int curTag = tags[pos_4][product % tagNum[pos_4]];
						//System.err.printf("pos=%d, product=%d, tag=%d score=%.3f\n",pos,product,curTag,windowScore[pos][product]);
						DFSATransition<string, int> tr = new DFSATransition<string, int>(string.Empty, startState, graphStates[pos_4][product], classIndex.Get(curTag), string.Empty, -windowScore[pos_4][product]);
						startState.AddTransition(tr);
					}
					else
					{
						int sharedProduct = product / tagNum[pos_4 + rightWindow];
						int factor = productSizes[pos_4] / tagNum[pos_4 + rightWindow];
						for (int newTagNum = 0; newTagNum < tagNum[pos_4 - leftWindow - 1]; newTagNum++)
						{
							int predProduct = newTagNum * factor + sharedProduct;
							int predTag = tags[pos_4 - 1][predProduct % tagNum[pos_4 - 1]];
							int curTag = tags[pos_4][product % tagNum[pos_4]];
							//log.info("pos: "+pos);
							//log.info("product: "+product);
							//System.err.printf("pos=%d-%d, product=%d-%d, tag=%d-%d score=%.3f\n",pos-1,pos,predProduct,product,predTag,curTag,
							//  windowScore[pos][product]);
							DFSAState<string, int> sourceState = graphStates[pos_4 - leftWindow][predTag];
							DFSAState<string, int> destState = (pos_4 - leftWindow + 1 == graphStates.Length) ? endState : graphStates[pos_4 - leftWindow + 1][curTag];
							DFSATransition<string, int> tr = new DFSATransition<string, int>(string.Empty, sourceState, destState, classIndex.Get(curTag), string.Empty, -windowScore[pos_4][product]);
							graphStates[pos_4 - leftWindow][predTag].AddTransition(tr);
						}
					}
				}
			}
			return viterbiSearchGraph;
		}
	}
}
