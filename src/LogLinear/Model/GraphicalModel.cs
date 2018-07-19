using System.Collections.Generic;
using Edu.Stanford.Nlp.Loglinear.Model.Proto;
using Java.IO;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Model
{
	/// <summary>Created on 8/7/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// A basic graphical model representation: Factors and Variables. This should be a fairly familiar interface to anybody
	/// who's taken a basic PGM course (eg https://www.coursera.org/course/pgm). The key points:
	/// - Stitching together feature factors
	/// - Attaching metadata to everything, so that different sections of the program can communicate in lots of unplanned
	/// ways. For now, the planned meta-data is a lot of routing and status information to do with LENSE.
	/// <p>
	/// This is really just the data structure, and inference lives elsewhere and must use public interfaces to access these
	/// models. We just provide basic utility functions here, and barely do that, because we pass through directly to maps
	/// wherever appropriate.
	/// </author>
	public class GraphicalModel
	{
		public IDictionary<string, string> modelMetaData = new Dictionary<string, string>();

		public IList<IDictionary<string, string>> variableMetaData = new List<IDictionary<string, string>>();

		public ICollection<GraphicalModel.Factor> factors = new HashSet<GraphicalModel.Factor>();

		/// <summary>A single factor in this graphical model.</summary>
		/// <remarks>
		/// A single factor in this graphical model. ConcatVectorTable can be reused multiple times if the same graph (or different
		/// ones) and this is the glue object that tells a model where the factor lives, and what it is connected to.
		/// </remarks>
		public class Factor
		{
			public ConcatVectorTable featuresTable;

			public int[] neigborIndices;

			public IDictionary<string, string> metaData = new Dictionary<string, string>();

			/// <summary>DO NOT USE.</summary>
			/// <remarks>DO NOT USE. FOR SERIALIZATION ONLY.</remarks>
			private Factor()
			{
			}

			public Factor(ConcatVectorTable featuresTable, int[] neighborIndices)
			{
				this.featuresTable = featuresTable;
				this.neigborIndices = neighborIndices;
			}

			/// <returns>the factor meta-data, by reference</returns>
			public virtual IDictionary<string, string> GetMetaDataByReference()
			{
				return metaData;
			}

			/// <summary>Does a deep comparison, using equality with tolerance checks against the vector table of values.</summary>
			/// <param name="other">the factor to compare to</param>
			/// <param name="tolerance">the tolerance to accept in differences</param>
			/// <returns>whether the two factors are within tolerance of one another</returns>
			public virtual bool ValueEquals(GraphicalModel.Factor other, double tolerance)
			{
				return Arrays.Equals(neigborIndices, other.neigborIndices) && metaData.Equals(other.metaData) && featuresTable.ValueEquals(other.featuresTable, tolerance);
			}

			public virtual GraphicalModelProto.Factor.Builder GetProtoBuilder()
			{
				GraphicalModelProto.Factor.Builder builder = GraphicalModelProto.Factor.NewBuilder();
				foreach (int neighbor in neigborIndices)
				{
					builder.AddNeighbor(neighbor);
				}
				builder.SetFeaturesTable(featuresTable.GetProtoBuilder());
				builder.SetMetaData(GraphicalModel.GetProtoMetaDataBuilder(metaData));
				return builder;
			}

			public static GraphicalModel.Factor ReadFromProto(GraphicalModelProto.Factor proto)
			{
				GraphicalModel.Factor factor = new GraphicalModel.Factor();
				factor.featuresTable = ConcatVectorTable.ReadFromProto(proto.GetFeaturesTable());
				factor.metaData = GraphicalModel.ReadMetaDataFromProto(proto.GetMetaData());
				factor.neigborIndices = new int[proto.GetNeighborCount()];
				for (int i = 0; i < factor.neigborIndices.Length; i++)
				{
					factor.neigborIndices[i] = proto.GetNeighbor(i);
				}
				return factor;
			}

			/// <summary>Duplicates this factor.</summary>
			/// <returns>a copy of the factor</returns>
			public virtual GraphicalModel.Factor CloneFactor()
			{
				GraphicalModel.Factor clone = new GraphicalModel.Factor();
				clone.neigborIndices = neigborIndices.MemberwiseClone();
				clone.featuresTable = featuresTable.CloneTable();
				clone.metaData.PutAll(metaData);
				return clone;
			}
		}

		/// <returns>a reference to the model meta-data</returns>
		public virtual IDictionary<string, string> GetModelMetaDataByReference()
		{
			return modelMetaData;
		}

		/// <summary>Gets the metadata for a variable.</summary>
		/// <remarks>Gets the metadata for a variable. Creates blank metadata if does not exists, then returns that. Pass by reference.</remarks>
		/// <param name="variableIndex">the variable number, 0 indexed, to retrieve</param>
		/// <returns>the metadata map corresponding to that variable number</returns>
		public virtual IDictionary<string, string> GetVariableMetaDataByReference(int variableIndex)
		{
			lock (this)
			{
				while (variableIndex >= variableMetaData.Count)
				{
					variableMetaData.Add(new Dictionary<string, string>());
				}
				return variableMetaData[variableIndex];
			}
		}

		/// <summary>This is the preferred way to add factors to a graphical model.</summary>
		/// <remarks>
		/// This is the preferred way to add factors to a graphical model. Specify the neighbors, their dimensions, and a
		/// function that maps from variable assignments to ConcatVector's of features, and this function will handle the
		/// data flow of constructing and populating a factor matching those specifications.
		/// <p>
		/// IMPORTANT: assignmentFeaturizer must be REPEATABLE and NOT HAVE SIDE EFFECTS
		/// This is because it is actually stored as a lazy closure until the full featurized vector is needed, and then it
		/// is created, used, and discarded. It CAN BE CALLED MULTIPLE TIMES, and must always return the same value in order
		/// for behavior of downstream systems to be defined.
		/// </remarks>
		/// <param name="neighborIndices">the names of the variables, as indices</param>
		/// <param name="neighborDimensions">the sizes of the neighbor variables, corresponding to the order in neighborIndices</param>
		/// <param name="assignmentFeaturizer">
		/// a function that maps from an assignment to the variables, represented as an array of
		/// assignments in the same order as presented in neighborIndices, to a ConcatVector of
		/// features for that assignment.
		/// </param>
		/// <returns>a reference to the created factor. This can be safely ignored, as the factor is already saved in the model</returns>
		public virtual GraphicalModel.Factor AddFactor(int[] neighborIndices, int[] neighborDimensions, IFunction<int[], ConcatVector> assignmentFeaturizer)
		{
			ConcatVectorTable features = new ConcatVectorTable(neighborDimensions);
			foreach (int[] assignment in features)
			{
				features.SetAssignmentValue(assignment, null);
			}
			return AddFactor(features, neighborIndices);
		}

		/// <summary>
		/// Creates an instantiated factor in this graph, with neighborIndices representing the neighbor variables by integer
		/// index.
		/// </summary>
		/// <param name="featureTable">the feature table to use to drive the value of the factor</param>
		/// <param name="neighborIndices">the indices of the neighboring variables, in order</param>
		/// <returns>a reference to the created factor. This can be safely ignored, as the factor is already saved in the model</returns>
		public virtual GraphicalModel.Factor AddFactor(ConcatVectorTable featureTable, int[] neighborIndices)
		{
			System.Diagnostics.Debug.Assert((featureTable.GetDimensions().Length == neighborIndices.Length));
			GraphicalModel.Factor factor = new GraphicalModel.Factor(featureTable, neighborIndices);
			factors.Add(factor);
			return factor;
		}

		/// <returns>an array of integers, indicating variable sizes given by each of the factors in the model</returns>
		public virtual int[] GetVariableSizes()
		{
			if (factors.Count == 0)
			{
				return new int[0];
			}
			int maxVar = 0;
			foreach (GraphicalModel.Factor f in factors)
			{
				foreach (int n in f.neigborIndices)
				{
					if (n > maxVar)
					{
						maxVar = n;
					}
				}
			}
			int[] sizes = new int[maxVar + 1];
			for (int i = 0; i < sizes.Length; i++)
			{
				sizes[i] = -1;
			}
			foreach (GraphicalModel.Factor f_1 in factors)
			{
				for (int i_1 = 0; i_1 < f_1.neigborIndices.Length; i_1++)
				{
					sizes[f_1.neigborIndices[i_1]] = f_1.featuresTable.GetDimensions()[i_1];
				}
			}
			return sizes;
		}

		/// <summary>Writes the protobuf version of this graphical model to a stream.</summary>
		/// <remarks>Writes the protobuf version of this graphical model to a stream. reversible with readFromStream().</remarks>
		/// <param name="stream">the output stream to write to</param>
		/// <exception cref="System.IO.IOException">passed through from the stream</exception>
		public virtual void WriteToStream(OutputStream stream)
		{
			((GraphicalModelProto.GraphicalModel)GetProtoBuilder().Build()).WriteDelimitedTo(stream);
		}

		/// <summary>Static function to deserialize a graphical model from an input stream.</summary>
		/// <param name="stream">the stream to read from, assuming protobuf encoding</param>
		/// <returns>a new graphical model</returns>
		/// <exception cref="System.IO.IOException">passed through from the stream</exception>
		public static GraphicalModel ReadFromStream(InputStream stream)
		{
			return ReadFromProto(GraphicalModelProto.GraphicalModel.ParseDelimitedFrom(stream));
		}

		/// <returns>the proto builder corresponding to this GraphicalModel</returns>
		public virtual GraphicalModelProto.GraphicalModel.Builder GetProtoBuilder()
		{
			GraphicalModelProto.GraphicalModel.Builder builder = GraphicalModelProto.GraphicalModel.NewBuilder();
			builder.SetMetaData(GetProtoMetaDataBuilder(modelMetaData));
			foreach (IDictionary<string, string> metaData in variableMetaData)
			{
				builder.AddVariableMetaData(GetProtoMetaDataBuilder(metaData));
			}
			foreach (GraphicalModel.Factor factor in factors)
			{
				builder.AddFactor(factor.GetProtoBuilder());
			}
			return builder;
		}

		/// <summary>
		/// Recreates an in-memory GraphicalModel from a proto serialization, recursively creating all the ConcatVectorTable's
		/// and ConcatVector's in memory as well.
		/// </summary>
		/// <param name="proto">the proto to read</param>
		/// <returns>an in-memory GraphicalModel</returns>
		public static GraphicalModel ReadFromProto(GraphicalModelProto.GraphicalModel proto)
		{
			if (proto == null)
			{
				return null;
			}
			GraphicalModel model = new GraphicalModel();
			model.modelMetaData = ReadMetaDataFromProto(proto.GetMetaData());
			model.variableMetaData = new List<IDictionary<string, string>>();
			for (int i = 0; i < proto.GetVariableMetaDataCount(); i++)
			{
				model.variableMetaData.Add(ReadMetaDataFromProto(proto.GetVariableMetaData(i)));
			}
			for (int i_1 = 0; i_1 < proto.GetFactorCount(); i_1++)
			{
				model.factors.Add(GraphicalModel.Factor.ReadFromProto(proto.GetFactor(i_1)));
			}
			return model;
		}

		/// <summary>
		/// Check that two models are deeply value-equivalent, down to the concat vectors inside the factor tables, within
		/// some tolerance.
		/// </summary>
		/// <remarks>
		/// Check that two models are deeply value-equivalent, down to the concat vectors inside the factor tables, within
		/// some tolerance. Mostly useful for testing.
		/// </remarks>
		/// <param name="other">the graphical model to compare against.</param>
		/// <param name="tolerance">the tolerance to accept when comparing concat vectors for value equality.</param>
		/// <returns>whether the two models are tolerance equivalent</returns>
		public virtual bool ValueEquals(GraphicalModel other, double tolerance)
		{
			if (!modelMetaData.Equals(other.modelMetaData))
			{
				return false;
			}
			if (!variableMetaData.Equals(other.variableMetaData))
			{
				return false;
			}
			// compare factor sets for equality
			ICollection<GraphicalModel.Factor> remaining = new HashSet<GraphicalModel.Factor>();
			Sharpen.Collections.AddAll(remaining, factors);
			foreach (GraphicalModel.Factor otherFactor in other.factors)
			{
				GraphicalModel.Factor match = null;
				foreach (GraphicalModel.Factor factor in remaining)
				{
					if (factor.ValueEquals(otherFactor, tolerance))
					{
						match = factor;
						break;
					}
				}
				if (match == null)
				{
					return false;
				}
				else
				{
					remaining.Remove(match);
				}
			}
			return remaining.Count <= 0;
		}

		/// <summary>Displays a list of factors, by neighbor.</summary>
		/// <returns>a formatted list of factors, by neighbor</returns>
		public override string ToString()
		{
			string s = "{";
			foreach (GraphicalModel.Factor f in factors)
			{
				s += "\n\t" + Arrays.ToString(f.neigborIndices) + "@" + f;
			}
			s += "\n}";
			return s;
		}

		/// <summary>
		/// The point here is to allow us to save a copy of the model with a current set of factors and metadata mappings,
		/// which can come in super handy with gameplaying applications.
		/// </summary>
		/// <remarks>
		/// The point here is to allow us to save a copy of the model with a current set of factors and metadata mappings,
		/// which can come in super handy with gameplaying applications. The cloned model doesn't instantiate the feature
		/// thunks inside factors, those are just taken over individually.
		/// </remarks>
		/// <returns>a clone</returns>
		public virtual GraphicalModel CloneModel()
		{
			GraphicalModel clone = new GraphicalModel();
			clone.modelMetaData.PutAll(modelMetaData);
			for (int i = 0; i < variableMetaData.Count; i++)
			{
				if (variableMetaData[i] != null)
				{
					clone.GetVariableMetaDataByReference(i).PutAll(variableMetaData[i]);
				}
			}
			foreach (GraphicalModel.Factor f in factors)
			{
				clone.factors.Add(f.CloneFactor());
			}
			return clone;
		}

		////////////////////////////////////////////////////////////////////////////
		// PRIVATE IMPLEMENTATION
		////////////////////////////////////////////////////////////////////////////
		private static GraphicalModelProto.MetaData.Builder GetProtoMetaDataBuilder(IDictionary<string, string> metaData)
		{
			GraphicalModelProto.MetaData.Builder builder = GraphicalModelProto.MetaData.NewBuilder();
			foreach (string key in metaData.Keys)
			{
				builder.AddKey(key);
				builder.AddValue(metaData[key]);
			}
			return builder;
		}

		private static IDictionary<string, string> ReadMetaDataFromProto(GraphicalModelProto.MetaData proto)
		{
			IDictionary<string, string> metaData = new Dictionary<string, string>();
			for (int i = 0; i < proto.GetKeyCount(); i++)
			{
				metaData[proto.GetKey(i)] = proto.GetValue(i);
			}
			return metaData;
		}
	}
}
