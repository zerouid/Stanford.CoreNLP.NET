using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Loglinear.Model;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Inference
{
	/// <summary>Created on 8/11/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// This is instantiated once per model, so that it can keep caches of important stuff like messages and
	/// local factors during many game playing sample steps. It assumes that the model that is passed in is by-reference,
	/// and that it can change between inference calls in small ways, so that cacheing of some results is worthwhile.
	/// </author>
	public class CliqueTree
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Loglinear.Inference.CliqueTree));

		private GraphicalModel model;

		private ConcatVector weights;

		public const string VariableObservedValue = "inference.CliqueTree.VARIABLE_OBSERVED_VALUE";

		private const bool CacheMessages = true;

		/// <summary>Create an Inference object for a given set of weights, and a model.</summary>
		/// <remarks>
		/// Create an Inference object for a given set of weights, and a model.
		/// <p>
		/// The object is around to facilitate cacheing as an eventual optimization, when models are changing in minor ways
		/// and inference is required several times. Work is done lazily, so is left until actual inference is requested.
		/// </remarks>
		/// <param name="model">the model to be computed over, subject to change in the future</param>
		/// <param name="weights">
		/// the weights to dot product with model features to get log-linear factors, is cloned internally so
		/// that no changes to the weights vector will be reflected by the CliqueTree. If you want to change
		/// the weights, you must create a new CliqueTree.
		/// </param>
		public CliqueTree(GraphicalModel model, ConcatVector weights)
		{
			// This is the metadata key for the model to store an observed value for a variable, as an int
			this.model = model;
			this.weights = weights.DeepClone();
		}

		/// <summary>Little data structure for passing around the results of marginal computations.</summary>
		public class MarginalResult
		{
			public double[][] marginals;

			public double partitionFunction;

			public IDictionary<GraphicalModel.Factor, TableFactor> jointMarginals;

			public MarginalResult(double[][] marginals, double partitionFunction, IDictionary<GraphicalModel.Factor, TableFactor> jointMarginals)
			{
				this.marginals = marginals;
				this.partitionFunction = partitionFunction;
				this.jointMarginals = jointMarginals;
			}
		}

		/// <summary>This assumes that factors represent joint probabilities.</summary>
		/// <returns>global marginals</returns>
		public virtual CliqueTree.MarginalResult CalculateMarginals()
		{
			return MessagePassing(CliqueTree.MarginalizationMethod.Sum, true);
		}

		/// <summary>
		/// This will calculate marginals, but skip the stuff that is created for gradient descent: joint marginals and
		/// partition functions.
		/// </summary>
		/// <remarks>
		/// This will calculate marginals, but skip the stuff that is created for gradient descent: joint marginals and
		/// partition functions. This makes it much faster. It is thus appropriate for gameplayer style work, where many
		/// samples need to be drawn with the same marginals.
		/// </remarks>
		/// <returns>an array, indexed first by variable, then by variable assignment, of global probability</returns>
		public virtual double[][] CalculateMarginalsJustSingletons()
		{
			CliqueTree.MarginalResult result = MessagePassing(CliqueTree.MarginalizationMethod.Sum, false);
			return result.marginals;
		}

		/// <summary>This assumes that factors represent joint probabilities.</summary>
		/// <returns>an array, indexed by variable, of maximum likelihood assignments</returns>
		public virtual int[] CalculateMAP()
		{
			double[][] mapMarginals = MessagePassing(CliqueTree.MarginalizationMethod.Max, false).marginals;
			int[] result = new int[mapMarginals.Length];
			for (int i = 0; i < result.Length; i++)
			{
				if (mapMarginals[i] != null)
				{
					for (int j = 0; j < mapMarginals[i].Length; j++)
					{
						if (mapMarginals[i][j] > mapMarginals[i][result[i]])
						{
							result[i] = j;
						}
					}
				}
				// If there is no factor touching an observed variable, the resulting MAP won't reference the variable
				// observation since message passing won't touch the variable index
				if (model.GetVariableMetaDataByReference(i).Contains(VariableObservedValue))
				{
					result[i] = System.Convert.ToInt32(model.GetVariableMetaDataByReference(i)[VariableObservedValue]);
				}
			}
			return result;
		}

		private enum MarginalizationMethod
		{
			Sum,
			Max
		}

		private IdentityHashMap<GraphicalModel.Factor, CliqueTree.CachedFactorWithObservations> cachedFactors = new IdentityHashMap<GraphicalModel.Factor, CliqueTree.CachedFactorWithObservations>();

		private class CachedFactorWithObservations
		{
			internal TableFactor cachedFactor;

			internal int[] observations;

			internal bool impossibleObservation;
			////////////////////////////////////////////////////////////////////////////
			// PRIVATE IMPLEMENTATION
			////////////////////////////////////////////////////////////////////////////
			// OPTIMIZATION:
			// cache the creation of TableFactors, to avoid redundant dot products
		}

		private TableFactor[] cachedCliqueList;

		private TableFactor[][] cachedMessages;

		private bool[][] cachedBackwardPassedMessages;

		// OPTIMIZATION:
		// cache the last list of factors, and the last set of messages passed, in case we can recycle some
		/// <summary>Does tree shaped message passing.</summary>
		/// <remarks>
		/// Does tree shaped message passing. The algorithm calls for first passing down to the leaves, then passing back up
		/// to the root.
		/// </remarks>
		/// <param name="marginalize">the method for marginalization, controls MAP or marginals</param>
		/// <returns>the marginal messages</returns>
		private CliqueTree.MarginalResult MessagePassing(CliqueTree.MarginalizationMethod marginalize, bool includeJointMarginalsAndPartition)
		{
			// Using the behavior of brute force factor multiplication as ground truth, the desired
			// outcome of marginal calculation with an impossible factor is a uniform probability dist.,
			// since we have a resulting factor of all 0s. That is of course assuming that normalizing
			// all 0s gives you uniform, which is not real math, but that's a useful tolerance to include, so we do.
			bool impossibleObservationMade = false;
			// Message passing will look at fully observed cliques as non-entities, but their
			// log-likelihood (the log-likelihood of the single observed value) is still relevant for the
			// partition function.
			double partitionFunction = 1.0;
			if (includeJointMarginalsAndPartition)
			{
				foreach (GraphicalModel.Factor f in model.factors)
				{
					foreach (int n in f.neigborIndices)
					{
						if (!model.GetVariableMetaDataByReference(n).Contains(VariableObservedValue))
						{
							goto outer_continue;
						}
					}
					int[] assignment = new int[f.neigborIndices.Length];
					for (int i = 0; i < f.neigborIndices.Length; i++)
					{
						assignment[i] = System.Convert.ToInt32(model.GetVariableMetaDataByReference(f.neigborIndices[i])[VariableObservedValue]);
					}
					double assignmentValue = f.featuresTable.GetAssignmentValue(assignment).Get().DotProduct(weights);
					if (double.IsInfinite(assignmentValue))
					{
						impossibleObservationMade = true;
					}
					else
					{
						partitionFunction *= Math.Exp(assignmentValue);
					}
				}
outer_break: ;
			}
			// Create the cliques by multiplying out table factors
			// TODO:OPT This could be made more efficient by observing first, then dot product
			IList<TableFactor> cliquesList = new List<TableFactor>();
			IDictionary<int, GraphicalModel.Factor> cliqueToFactor = new Dictionary<int, GraphicalModel.Factor>();
			int numFactorsCached = 0;
			foreach (GraphicalModel.Factor f_1 in model.factors)
			{
				bool allObserved = true;
				int maxVar = 0;
				foreach (int n in f_1.neigborIndices)
				{
					if (!model.GetVariableMetaDataByReference(n).Contains(VariableObservedValue))
					{
						allObserved = false;
					}
					if (n > maxVar)
					{
						maxVar = n;
					}
				}
				if (allObserved)
				{
					continue;
				}
				TableFactor clique = null;
				// Retrieve cache if exists and none of the observations have changed
				if (cachedFactors.Contains(f_1))
				{
					CliqueTree.CachedFactorWithObservations obs = cachedFactors[f_1];
					bool allConsistent = true;
					for (int i = 0; i < f_1.neigborIndices.Length; i++)
					{
						int n_1 = f_1.neigborIndices[i];
						if (model.GetVariableMetaDataByReference(n_1).Contains(VariableObservedValue) && (obs.observations[i] == -1 || System.Convert.ToInt32(model.GetVariableMetaDataByReference(n_1)[VariableObservedValue]) != obs.observations[i]))
						{
							allConsistent = false;
							break;
						}
						// NOTE: This disqualifies lots of stuff for some reason...
						if (!model.GetVariableMetaDataByReference(n_1).Contains(VariableObservedValue) && (obs.observations[i] != -1))
						{
							allConsistent = false;
							break;
						}
					}
					if (allConsistent)
					{
						clique = obs.cachedFactor;
						numFactorsCached++;
						if (obs.impossibleObservation)
						{
							impossibleObservationMade = true;
						}
					}
				}
				// Otherwise make a new cache
				if (clique == null)
				{
					int[] observations = new int[f_1.neigborIndices.Length];
					for (int i = 0; i < observations.Length; i++)
					{
						IDictionary<string, string> metadata = model.GetVariableMetaDataByReference(f_1.neigborIndices[i]);
						if (metadata.Contains(VariableObservedValue))
						{
							int value = System.Convert.ToInt32(metadata[VariableObservedValue]);
							observations[i] = value;
						}
						else
						{
							observations[i] = -1;
						}
					}
					clique = new TableFactor(weights, f_1, observations);
					CliqueTree.CachedFactorWithObservations cache = new CliqueTree.CachedFactorWithObservations();
					cache.cachedFactor = clique;
					cache.observations = observations;
					// Check for an impossible observation
					bool nonZeroValue = false;
					foreach (int[] assignment in clique)
					{
						if (clique.GetAssignmentValue(assignment) > 0)
						{
							nonZeroValue = true;
							break;
						}
					}
					if (!nonZeroValue)
					{
						impossibleObservationMade = true;
						cache.impossibleObservation = true;
					}
					cachedFactors[f_1] = cache;
				}
				cliqueToFactor[cliquesList.Count] = f_1;
				cliquesList.Add(clique);
			}
			TableFactor[] cliques = Sharpen.Collections.ToArray(cliquesList, new TableFactor[cliquesList.Count]);
			// If we made any impossible observations, we can just return a uniform distribution for all the variables that
			// weren't observed, since that's the semantically correct thing to do (our 'probability' is broken at this
			// point).
			if (impossibleObservationMade)
			{
				int maxVar = 0;
				foreach (TableFactor c in cliques)
				{
					foreach (int i in c.neighborIndices)
					{
						if (i > maxVar)
						{
							maxVar = i;
						}
					}
				}
				double[][] result = new double[maxVar + 1][];
				foreach (TableFactor c_1 in cliques)
				{
					for (int i = 0; i < c_1.neighborIndices.Length; i++)
					{
						result[c_1.neighborIndices[i]] = new double[c_1.GetDimensions()[i]];
						for (int j = 0; j < result[c_1.neighborIndices[i]].Length; j++)
						{
							result[c_1.neighborIndices[i]][j] = 1.0 / result[c_1.neighborIndices[i]].Length;
						}
					}
				}
				// Create a bunch of uniform joint marginals, constrained by observations, and fill up the joint marginals
				// with them
				IDictionary<GraphicalModel.Factor, TableFactor> jointMarginals = new IdentityHashMap<GraphicalModel.Factor, TableFactor>();
				if (includeJointMarginalsAndPartition)
				{
					foreach (GraphicalModel.Factor f in model.factors)
					{
						TableFactor uniformZero = new TableFactor(f_1.neigborIndices, f_1.featuresTable.GetDimensions());
						foreach (int[] assignment in uniformZero)
						{
							uniformZero.SetAssignmentValue(assignment, 0.0);
						}
						jointMarginals[f_1] = uniformZero;
					}
				}
				return new CliqueTree.MarginalResult(result, 1.0, jointMarginals);
			}
			// Find the largest contained variable, so that we can size arrays appropriately
			int maxVar_1 = 0;
			foreach (GraphicalModel.Factor fac in model.factors)
			{
				foreach (int i in fac.neigborIndices)
				{
					if (i > maxVar_1)
					{
						maxVar_1 = i;
					}
				}
			}
			// Indexed by (start-clique, end-clique), this array will remain mostly null in most graphs
			TableFactor[][] messages = new TableFactor[cliques.Length][];
			// OPTIMIZATION:
			// check if we've only added one factor since the last time we ran marginal inference. If that's the case, we
			// can use the new factor as the root, all the messages passed in from the leaves will not have changed. That
			// means we can cut message passing computation in half.
			bool[][] backwardPassedMessages = new bool[cliques.Length][];
			int forceRootForCachedMessagePassing = -1;
			int[] cachedCliquesBackPointers = null;
			if (CacheMessages && (numFactorsCached == cliques.Length - 1) && (numFactorsCached > 0))
			{
				cachedCliquesBackPointers = new int[cliques.Length];
				// Sometimes we'll have cached versions of the factors, but they're from inference steps a long time ago, so we
				// don't get consistent backpointers to our cache of factors. This is a flag to indicate if this happens.
				bool backPointersConsistent = true;
				// Calculate the correspondence between the old cliques list and the new cliques list
				for (int i = 0; i < cliques.Length; i++)
				{
					cachedCliquesBackPointers[i] = -1;
					for (int j = 0; j < cachedCliqueList.Length; j++)
					{
						if (cliques[i] == cachedCliqueList[j])
						{
							cachedCliquesBackPointers[i] = j;
							break;
						}
					}
					if (cachedCliquesBackPointers[i] == -1)
					{
						if (forceRootForCachedMessagePassing != -1)
						{
							backPointersConsistent = false;
							break;
						}
						forceRootForCachedMessagePassing = i;
					}
				}
				if (!backPointersConsistent)
				{
					forceRootForCachedMessagePassing = -1;
				}
			}
			// Create the data structures to hold the tree pattern
			bool[] visited = new bool[cliques.Length];
			int numVisited = 0;
			int[] visitedOrder = new int[cliques.Length];
			int[] parent = new int[cliques.Length];
			for (int i_1 = 0; i_1 < parent.Length; i_1++)
			{
				parent[i_1] = -1;
			}
			// Figure out which cliques are connected to which trees. This is important for calculating the partition
			// function later, since each tree will converge to its own partition function by multiplication, and we will
			// need to multiply the partition function of each of the trees to get the global one.
			int[] trees = new int[cliques.Length];
			// Forward pass, record a BFS forest pattern that we can use for message passing
			int treeIndex = -1;
			bool[] seenVariable = new bool[maxVar_1 + 1];
			while (numVisited < cliques.Length)
			{
				treeIndex++;
				// Pick the largest connected graph remaining as the root for message passing
				int root = -1;
				// OPTIMIZATION: if there's a forced root for message passing (a node that we just added) then make it the
				// root
				if (CacheMessages && forceRootForCachedMessagePassing != -1 && !visited[forceRootForCachedMessagePassing])
				{
					root = forceRootForCachedMessagePassing;
				}
				else
				{
					for (int i = 0; i_1 < cliques.Length; i_1++)
					{
						if (!visited[i_1] && (root == -1 || cliques[i_1].neighborIndices.Length > cliques[root].neighborIndices.Length))
						{
							root = i_1;
						}
					}
				}
				System.Diagnostics.Debug.Assert((root != -1));
				IQueue<int> toVisit = new ArrayDeque<int>();
				toVisit.Add(root);
				bool[] toVisitArray = new bool[cliques.Length];
				toVisitArray[root] = true;
				while (toVisit.Count > 0)
				{
					int cursor = toVisit.Poll();
					// toVisitArray[cursor] = false;
					trees[cursor] = treeIndex;
					if (visited[cursor])
					{
						log.Info("Visited contains: " + cursor);
						log.Info("Visited: " + Arrays.ToString(visited));
						log.Info("To visit: " + toVisit);
					}
					System.Diagnostics.Debug.Assert((!visited[cursor]));
					visited[cursor] = true;
					visitedOrder[numVisited] = cursor;
					foreach (int i in cliques[cursor].neighborIndices)
					{
						seenVariable[i_1] = true;
					}
					numVisited++;
					for (int i_2 = 0; i_2 < cliques.Length; i_2++)
					{
						if (i_2 == cursor)
						{
							continue;
						}
						if (i_2 == parent[cursor])
						{
							continue;
						}
						if (DomainsOverlap(cliques[cursor], cliques[i_2]))
						{
							// Make sure that for every variable that we've already seen somewhere in the graph, if it's
							// in the child, it's in the parent. Otherwise we'll break the property of continuous
							// transmission of information about variables through messages.
							foreach (int child in cliques[i_2].neighborIndices)
							{
								if (seenVariable[child])
								{
									foreach (int j in cliques[cursor].neighborIndices)
									{
										if (j == child)
										{
											goto childNeighborLoop_continue;
										}
									}
									// If we get here it means that this clique is not good as a child, since we can't pass
									// it all the information it needs from other elements of the tree
									goto childLoop_continue;
								}
							}
childNeighborLoop_break: ;
							if (parent[i_2] == -1 && !visited[i_2])
							{
								if (!toVisitArray[i_2])
								{
									toVisit.Add(i_2);
									toVisitArray[i_2] = true;
									foreach (int j in cliques[i_2].neighborIndices)
									{
										seenVariable[j] = true;
									}
								}
								parent[i_2] = cursor;
							}
						}
childLoop_continue: ;
					}
childLoop_break: ;
				}
				// No cycles in the tree
				System.Diagnostics.Debug.Assert((parent[root] == -1));
			}
			System.Diagnostics.Debug.Assert((numVisited == cliques.Length));
			// Backward pass, run the visited list in reverse
			for (int i_3 = numVisited - 1; i_3 >= 0; i_3--)
			{
				int cursor = visitedOrder[i_3];
				if (parent[cursor] == -1)
				{
					continue;
				}
				backwardPassedMessages[cursor][parent[cursor]] = true;
				// OPTIMIZATION:
				// if these conditions are met we can avoid calculating the message, and instead retrieve from the cache,
				// since they should be the same
				if (CacheMessages && forceRootForCachedMessagePassing != -1 && cachedCliquesBackPointers[cursor] != -1 && cachedCliquesBackPointers[parent[cursor]] != -1 && cachedMessages[cachedCliquesBackPointers[cursor]][cachedCliquesBackPointers[parent[cursor
					]]] != null && cachedBackwardPassedMessages[cachedCliquesBackPointers[cursor]][cachedCliquesBackPointers[parent[cursor]]])
				{
					messages[cursor][parent[cursor]] = cachedMessages[cachedCliquesBackPointers[cursor]][cachedCliquesBackPointers[parent[cursor]]];
				}
				else
				{
					// Calculate the message to the clique's parent, given all incoming messages so far
					TableFactor message = cliques[cursor];
					for (int k = 0; k < cliques.Length; k++)
					{
						if (k == parent[cursor])
						{
							continue;
						}
						if (messages[k][cursor] != null)
						{
							message = message.Multiply(messages[k][cursor]);
						}
					}
					messages[cursor][parent[cursor]] = MarginalizeMessage(message, cliques[parent[cursor]].neighborIndices, marginalize);
					// Invalidate any cached outgoing messages
					if (CacheMessages && forceRootForCachedMessagePassing != -1 && cachedCliquesBackPointers[parent[cursor]] != -1)
					{
						for (int k_1 = 0; k_1 < cachedCliqueList.Length; k_1++)
						{
							cachedMessages[cachedCliquesBackPointers[parent[cursor]]][k_1] = null;
						}
					}
				}
			}
			// Forward pass, run the visited list forward
			for (int i_4 = 0; i_4 < numVisited; i_4++)
			{
				int cursor = visitedOrder[i_4];
				for (int j = 0; j < cliques.Length; j++)
				{
					if (parent[j] != cursor)
					{
						continue;
					}
					TableFactor message = cliques[cursor];
					for (int k = 0; k < cliques.Length; k++)
					{
						if (k == j)
						{
							continue;
						}
						if (messages[k][cursor] != null)
						{
							message = message.Multiply(messages[k][cursor]);
						}
					}
					messages[cursor][j] = MarginalizeMessage(message, cliques[j].neighborIndices, marginalize);
				}
			}
			// OPTIMIZATION:
			// cache the messages, and the current list of cliques
			cachedCliqueList = cliques;
			cachedMessages = messages;
			cachedBackwardPassedMessages = backwardPassedMessages;
			// Calculate final marginals for each variable
			double[][] marginals = new double[maxVar_1 + 1][];
			// Include observed variables as deterministic
			foreach (GraphicalModel.Factor fac_1 in model.factors)
			{
				for (int i = 0; i_4 < fac_1.neigborIndices.Length; i_4++)
				{
					int n = fac_1.neigborIndices[i_4];
					if (model.GetVariableMetaDataByReference(n).Contains(VariableObservedValue))
					{
						double[] deterministic = new double[fac_1.featuresTable.GetDimensions()[i_4]];
						int assignment = System.Convert.ToInt32(model.GetVariableMetaDataByReference(n)[VariableObservedValue]);
						if (assignment > deterministic.Length)
						{
							throw new InvalidOperationException("Variable " + n + ": Can't have as assignment (" + assignment + ") that is out of bounds for dimension size (" + deterministic.Length + ")");
						}
						deterministic[assignment] = 1.0;
						marginals[n] = deterministic;
					}
				}
			}
			IDictionary<GraphicalModel.Factor, TableFactor> jointMarginals_1 = new IdentityHashMap<GraphicalModel.Factor, TableFactor>();
			if (marginalize == CliqueTree.MarginalizationMethod.Sum && includeJointMarginalsAndPartition)
			{
				bool[] partitionIncludesTrees = new bool[treeIndex + 1];
				double[] treePartitionFunctions = new double[treeIndex + 1];
				for (int i = 0; i_4 < cliques.Length; i_4++)
				{
					TableFactor convergedClique = cliques[i_4];
					for (int j = 0; j < cliques.Length; j++)
					{
						if (i_4 == j)
						{
							continue;
						}
						if (messages[j][i_4] == null)
						{
							continue;
						}
						convergedClique = convergedClique.Multiply(messages[j][i_4]);
					}
					// Calculate the partition function when we're calculating marginals
					// We need one contribution per tree in our forest graph
					if (!partitionIncludesTrees[trees[i_4]])
					{
						partitionIncludesTrees[trees[i_4]] = true;
						treePartitionFunctions[trees[i_4]] = convergedClique.ValueSum();
						partitionFunction *= treePartitionFunctions[trees[i_4]];
					}
					else
					{
						// This is all just an elaborate assert
						// Check that our partition function is the same as the trees we're attached to, or with %.1, for numerical reasons.
						// Sometimes the partition function will explode in value, which can make a non-%-based assert worthless here
						if (AssertsEnabled() && !TableFactor.UseExpApprox)
						{
							double valueSum = convergedClique.ValueSum();
							if (double.IsFinite(valueSum) && double.IsFinite(treePartitionFunctions[trees[i_4]]))
							{
								if (Math.Abs(treePartitionFunctions[trees[i_4]] - valueSum) >= 1.0e-3 * treePartitionFunctions[trees[i_4]])
								{
									log.Info("Different partition functions for tree " + trees[i_4] + ": ");
									log.Info("Pre-existing for tree: " + treePartitionFunctions[trees[i_4]]);
									log.Info("This clique for tree: " + valueSum);
								}
								System.Diagnostics.Debug.Assert((Math.Abs(treePartitionFunctions[trees[i_4]] - valueSum) < 1.0e-3 * treePartitionFunctions[trees[i_4]]));
							}
						}
					}
					// Calculate the factor this clique corresponds to, and put in an entry for joint marginals
					GraphicalModel.Factor f = cliqueToFactor[i_4];
					System.Diagnostics.Debug.Assert((f_1 != null));
					if (!jointMarginals_1.Contains(f_1))
					{
						int[] observedAssignments = GetObservedAssignments(f_1);
						// Collect back pointers and check if this factor matches the clique we're using
						int[] backPointers = new int[observedAssignments.Length];
						int cursor = 0;
						for (int j_1 = 0; j_1 < observedAssignments.Length; j_1++)
						{
							if (observedAssignments[j_1] == -1)
							{
								backPointers[j_1] = cursor;
								cursor++;
							}
							else
							{
								// This is not strictly necessary but will trigger array OOB exception if things go wrong, so is nice
								backPointers[j_1] = -1;
							}
						}
						double sum = convergedClique.ValueSum();
						TableFactor jointMarginal = new TableFactor(f_1.neigborIndices, f_1.featuresTable.GetDimensions());
						// OPTIMIZATION:
						// Rather than use the standard iterator, which creates lots of int[] arrays on the heap, which need to be GC'd,
						// we use the fast version that just mutates one array. Since this is read once for us here, this is ideal.
						IEnumerator<int[]> fastPassByReferenceIterator = convergedClique.FastPassByReferenceIterator();
						int[] assignment = fastPassByReferenceIterator.Current;
						while (true)
						{
							if (backPointers.Length == assignment.Length)
							{
								jointMarginal.SetAssignmentValue(assignment, convergedClique.GetAssignmentValue(assignment) / sum);
							}
							else
							{
								int[] jointAssignment = new int[backPointers.Length];
								for (int j_2 = 0; j_2 < jointAssignment.Length; j_2++)
								{
									if (observedAssignments[j_2] != -1)
									{
										jointAssignment[j_2] = observedAssignments[j_2];
									}
									else
									{
										jointAssignment[j_2] = assignment[backPointers[j_2]];
									}
								}
								jointMarginal.SetAssignmentValue(jointAssignment, convergedClique.GetAssignmentValue(assignment) / sum);
							}
							// Set the assignment arrays correctly
							if (fastPassByReferenceIterator.MoveNext())
							{
								fastPassByReferenceIterator.Current;
							}
							else
							{
								break;
							}
						}
						jointMarginals_1[f_1] = jointMarginal;
					}
					bool anyNull = false;
					for (int j_3 = 0; j_3 < convergedClique.neighborIndices.Length; j_3++)
					{
						int k = convergedClique.neighborIndices[j_3];
						if (marginals[k] == null)
						{
							anyNull = true;
						}
					}
					if (anyNull)
					{
						double[][] cliqueMarginals = null;
						switch (marginalize)
						{
							case CliqueTree.MarginalizationMethod.Sum:
							{
								cliqueMarginals = convergedClique.GetSummedMarginals();
								break;
							}

							case CliqueTree.MarginalizationMethod.Max:
							{
								cliqueMarginals = convergedClique.GetMaxedMarginals();
								break;
							}
						}
						for (int j_1 = 0; j_1 < convergedClique.neighborIndices.Length; j_1++)
						{
							int k = convergedClique.neighborIndices[j_1];
							if (marginals[k] == null)
							{
								marginals[k] = cliqueMarginals[j_1];
							}
						}
					}
				}
			}
			else
			{
				// If we don't care about joint marginals, we can be careful about not calculating more cliques than we need to,
				// by explicitly sorting by which cliques are most profitable to calculate over. In this way we can avoid, in
				// the case of a chain CRF, calculating almost half the joint factors.
				// First do a pass where we only calculate all-null neighbors
				for (int i = 0; i_4 < cliques.Length; i_4++)
				{
					bool allNull = true;
					foreach (int k in cliques[i_4].neighborIndices)
					{
						if (marginals[k] != null)
						{
							allNull = false;
						}
					}
					if (allNull)
					{
						TableFactor convergedClique = cliques[i_4];
						for (int j = 0; j < cliques.Length; j++)
						{
							if (i_4 == j)
							{
								continue;
							}
							if (messages[j][i_4] == null)
							{
								continue;
							}
							convergedClique = convergedClique.Multiply(messages[j][i_4]);
						}
						double[][] cliqueMarginals = null;
						switch (marginalize)
						{
							case CliqueTree.MarginalizationMethod.Sum:
							{
								cliqueMarginals = convergedClique.GetSummedMarginals();
								break;
							}

							case CliqueTree.MarginalizationMethod.Max:
							{
								cliqueMarginals = convergedClique.GetMaxedMarginals();
								break;
							}
						}
						for (int j_1 = 0; j_1 < convergedClique.neighborIndices.Length; j_1++)
						{
							int k_1 = convergedClique.neighborIndices[j_1];
							if (marginals[k_1] == null)
							{
								marginals[k_1] = cliqueMarginals[j_1];
							}
						}
					}
				}
				// Now we calculate any remaining cliques with any non-null variables
				for (int i_2 = 0; i_2 < cliques.Length; i_2++)
				{
					bool anyNull = false;
					for (int j = 0; j < cliques[i_2].neighborIndices.Length; j++)
					{
						int k = cliques[i_2].neighborIndices[j];
						if (marginals[k] == null)
						{
							anyNull = true;
						}
					}
					if (anyNull)
					{
						TableFactor convergedClique = cliques[i_2];
						for (int j_1 = 0; j_1 < cliques.Length; j_1++)
						{
							if (i_2 == j_1)
							{
								continue;
							}
							if (messages[j_1][i_2] == null)
							{
								continue;
							}
							convergedClique = convergedClique.Multiply(messages[j_1][i_2]);
						}
						double[][] cliqueMarginals = null;
						switch (marginalize)
						{
							case CliqueTree.MarginalizationMethod.Sum:
							{
								cliqueMarginals = convergedClique.GetSummedMarginals();
								break;
							}

							case CliqueTree.MarginalizationMethod.Max:
							{
								cliqueMarginals = convergedClique.GetMaxedMarginals();
								break;
							}
						}
						for (int j_2 = 0; j_2 < convergedClique.neighborIndices.Length; j_2++)
						{
							int k = convergedClique.neighborIndices[j_2];
							if (marginals[k] == null)
							{
								marginals[k] = cliqueMarginals[j_2];
							}
						}
					}
				}
			}
			// Add any factors to the joint marginal map that were fully observed and so didn't get cliques
			if (marginalize == CliqueTree.MarginalizationMethod.Sum && includeJointMarginalsAndPartition)
			{
				foreach (GraphicalModel.Factor f in model.factors)
				{
					if (!jointMarginals_1.Contains(f_1))
					{
						// This implies that every variable in the factor is observed. If that's the case, we need to construct
						// a one hot TableFactor representing the deterministic distribution.
						TableFactor deterministicJointMarginal = new TableFactor(f_1.neigborIndices, f_1.featuresTable.GetDimensions());
						int[] observedAssignment = GetObservedAssignments(f_1);
						foreach (int i in observedAssignment)
						{
							System.Diagnostics.Debug.Assert((i_4 != -1));
						}
						deterministicJointMarginal.SetAssignmentValue(observedAssignment, 1.0);
						jointMarginals_1[f_1] = deterministicJointMarginal;
					}
				}
			}
			return new CliqueTree.MarginalResult(marginals, partitionFunction, jointMarginals_1);
		}

		private int[] GetObservedAssignments(GraphicalModel.Factor f)
		{
			int[] observedAssignments = new int[f.neigborIndices.Length];
			for (int i = 0; i < observedAssignments.Length; i++)
			{
				if (model.GetVariableMetaDataByReference(f.neigborIndices[i]).Contains(VariableObservedValue))
				{
					observedAssignments[i] = System.Convert.ToInt32(model.GetVariableMetaDataByReference(f.neigborIndices[i])[VariableObservedValue]);
				}
				else
				{
					observedAssignments[i] = -1;
				}
			}
			return observedAssignments;
		}

		/// <summary>This is a key step in message passing.</summary>
		/// <remarks>
		/// This is a key step in message passing. When we are calculating a message, we want to marginalize out all variables
		/// not relevant to the recipient of the message. This function does that.
		/// </remarks>
		/// <param name="message">the message to marginalize</param>
		/// <param name="relevant">the variables that are relevant</param>
		/// <param name="marginalize">whether to use sum of max marginalization, for marginal or MAP inference</param>
		/// <returns>the marginalized message</returns>
		private static TableFactor MarginalizeMessage(TableFactor message, int[] relevant, CliqueTree.MarginalizationMethod marginalize)
		{
			TableFactor result = message;
			foreach (int i in message.neighborIndices)
			{
				bool contains = false;
				foreach (int j in relevant)
				{
					if (i == j)
					{
						contains = true;
						break;
					}
				}
				if (!contains)
				{
					switch (marginalize)
					{
						case CliqueTree.MarginalizationMethod.Sum:
						{
							result = result.SumOut(i);
							break;
						}

						case CliqueTree.MarginalizationMethod.Max:
						{
							result = result.MaxOut(i);
							break;
						}
					}
				}
			}
			return result;
		}

		/// <summary>Just a quick inline to check if two factors have overlapping domains.</summary>
		/// <remarks>
		/// Just a quick inline to check if two factors have overlapping domains. Since factor neighbor sets are super small,
		/// this n^2 algorithm is fine.
		/// </remarks>
		/// <param name="f1">first factor to compare</param>
		/// <param name="f2">second factor to compare</param>
		/// <returns>whether their domains overlap</returns>
		private static bool DomainsOverlap(TableFactor f1, TableFactor f2)
		{
			foreach (int n1 in f1.neighborIndices)
			{
				foreach (int n2 in f2.neighborIndices)
				{
					if (n1 == n2)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool AssertsEnabled()
		{
			bool assertsEnabled = false;
			System.Diagnostics.Debug.Assert((assertsEnabled = true));
			// intentional side effect
			return assertsEnabled;
		}
	}
}
