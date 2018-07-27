using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>An encapsulation of the natural logic weights to use during forward inference.</summary>
	/// <seealso cref="ForwardEntailer"/>
	/// <author>Gabor Angeli</author>
	public class NaturalLogicWeights
	{
		private readonly IDictionary<Pair<string, string>, double> verbPPAffinity = new Dictionary<Pair<string, string>, double>();

		private readonly IDictionary<Triple<string, string, string>, double> verbSubjPPAffinity = new Dictionary<Triple<string, string, string>, double>();

		private readonly IDictionary<Quadruple<string, string, string, string>, double> verbSubjObjPPAffinity = new Dictionary<Quadruple<string, string, string, string>, double>();

		private readonly IDictionary<Quadruple<string, string, string, string>, double> verbSubjPPPPAffinity = new Dictionary<Quadruple<string, string, string, string>, double>();

		private readonly IDictionary<Quadruple<string, string, string, string>, double> verbSubjPPObjAffinity = new Dictionary<Quadruple<string, string, string, string>, double>();

		private readonly IDictionary<string, double> verbObjAffinity = new Dictionary<string, double>();

		private readonly double upperProbabilityCap;

		public NaturalLogicWeights()
		{
			this.upperProbabilityCap = 1.0;
		}

		public NaturalLogicWeights(double upperProbabilityCap)
		{
			this.upperProbabilityCap = upperProbabilityCap;
		}

		/// <exception cref="System.IO.IOException"/>
		public NaturalLogicWeights(string affinityModels, double upperProbabilityCap)
		{
			this.upperProbabilityCap = upperProbabilityCap;
			string line;
			// Simple PP attachments
			using (BufferedReader ppReader = IOUtils.ReaderFromString(affinityModels + "/pp.tab.gz", "utf8"))
			{
				while ((line = ppReader.ReadLine()) != null)
				{
					string[] fields = line.Split("\t");
					Pair<string, string> key = Pair.MakePair(string.Intern(fields[0]), string.Intern(fields[1]));
					verbPPAffinity[key] = double.ParseDouble(fields[2]);
				}
			}
			// Subj PP attachments
			using (BufferedReader subjPPReader = IOUtils.ReaderFromString(affinityModels + "/subj_pp.tab.gz", "utf8"))
			{
				while ((line = subjPPReader.ReadLine()) != null)
				{
					string[] fields = line.Split("\t");
					Triple<string, string, string> key = Triple.MakeTriple(string.Intern(fields[0]), string.Intern(fields[1]), string.Intern(fields[2]));
					verbSubjPPAffinity[key] = double.ParseDouble(fields[3]);
				}
			}
			// Subj Obj PP attachments
			using (BufferedReader subjObjPPReader = IOUtils.ReaderFromString(affinityModels + "/subj_obj_pp.tab.gz", "utf8"))
			{
				while ((line = subjObjPPReader.ReadLine()) != null)
				{
					string[] fields = line.Split("\t");
					Quadruple<string, string, string, string> key = Quadruple.MakeQuadruple(string.Intern(fields[0]), string.Intern(fields[1]), string.Intern(fields[2]), string.Intern(fields[3]));
					verbSubjObjPPAffinity[key] = double.ParseDouble(fields[4]);
				}
			}
			// Subj PP PP attachments
			using (BufferedReader subjPPPPReader = IOUtils.ReaderFromString(affinityModels + "/subj_pp_pp.tab.gz", "utf8"))
			{
				while ((line = subjPPPPReader.ReadLine()) != null)
				{
					string[] fields = line.Split("\t");
					Quadruple<string, string, string, string> key = Quadruple.MakeQuadruple(string.Intern(fields[0]), string.Intern(fields[1]), string.Intern(fields[2]), string.Intern(fields[3]));
					verbSubjPPPPAffinity[key] = double.ParseDouble(fields[4]);
				}
			}
			// Subj PP PP attachments
			using (BufferedReader subjPPObjReader = IOUtils.ReaderFromString(affinityModels + "/subj_pp_obj.tab.gz", "utf8"))
			{
				while ((line = subjPPObjReader.ReadLine()) != null)
				{
					string[] fields = line.Split("\t");
					Quadruple<string, string, string, string> key = Quadruple.MakeQuadruple(string.Intern(fields[0]), string.Intern(fields[1]), string.Intern(fields[2]), string.Intern(fields[3]));
					verbSubjPPObjAffinity[key] = double.ParseDouble(fields[4]);
				}
			}
			// Subj PP PP attachments
			using (BufferedReader objReader = IOUtils.ReaderFromString(affinityModels + "/obj.tab.gz", "utf8"))
			{
				while ((line = objReader.ReadLine()) != null)
				{
					string[] fields = line.Split("\t");
					verbObjAffinity[fields[0]] = double.ParseDouble(fields[1]);
				}
			}
		}

		public virtual double DeletionProbability(string edgeType)
		{
			// TODO(gabor) this is effectively assuming hard NatLog weights
			if (edgeType.Contains("prep"))
			{
				return 0.9;
			}
			else
			{
				if (edgeType.Contains("obj"))
				{
					return 0.0;
				}
				else
				{
					return 1.0;
				}
			}
		}

		public virtual double SubjDeletionProbability(SemanticGraphEdge edge, IEnumerable<SemanticGraphEdge> neighbors)
		{
			// Get information about the neighbors
			// (in a totally not-creepy-stalker sort of way)
			foreach (SemanticGraphEdge neighbor in neighbors)
			{
				if (neighbor != edge)
				{
					string neighborRel = neighbor.GetRelation().ToString();
					if (neighborRel.Contains("subj"))
					{
						return 1.0;
					}
				}
			}
			return 0.0;
		}

		public virtual double ObjDeletionProbability(SemanticGraphEdge edge, IEnumerable<SemanticGraphEdge> neighbors)
		{
			// Get information about the neighbors
			// (in a totally not-creepy-stalker sort of way)
			Optional<string> subj = Optional.Empty();
			Optional<string> pp = Optional.Empty();
			foreach (SemanticGraphEdge neighbor in neighbors)
			{
				if (neighbor != edge)
				{
					string neighborRel = neighbor.GetRelation().ToString();
					if (neighborRel.Contains("subj"))
					{
						subj = Optional.Of(neighbor.GetDependent().OriginalText().ToLower());
					}
					if (neighborRel.Contains("prep"))
					{
						pp = Optional.Of(neighborRel);
					}
					if (neighborRel.Contains("obj"))
					{
						return 1.0;
					}
				}
			}
			// allow deleting second object
			string obj = edge.GetDependent().OriginalText().ToLower();
			string verb = edge.GetGovernor().OriginalText().ToLower();
			// Compute the most informative drop probability we can
			double rawScore = null;
			if (subj.IsPresent())
			{
				if (pp.IsPresent())
				{
					// Case: subj+obj
					rawScore = verbSubjPPObjAffinity[Quadruple.MakeQuadruple(verb, subj.Get(), pp.Get(), obj)];
				}
			}
			if (rawScore == null)
			{
				rawScore = verbObjAffinity[verb];
			}
			if (rawScore == null)
			{
				return DeletionProbability(edge.GetRelation().ToString());
			}
			else
			{
				return 1.0 - Math.Min(1.0, rawScore / upperProbabilityCap);
			}
		}

		public virtual double PpDeletionProbability(SemanticGraphEdge edge, IEnumerable<SemanticGraphEdge> neighbors)
		{
			// Get information about the neighbors
			// (in a totally not-creepy-stalker sort of way)
			Optional<string> subj = Optional.Empty();
			Optional<string> obj = Optional.Empty();
			Optional<string> pp = Optional.Empty();
			foreach (SemanticGraphEdge neighbor in neighbors)
			{
				if (neighbor != edge)
				{
					string neighborRel = neighbor.GetRelation().ToString();
					if (neighborRel.Contains("subj"))
					{
						subj = Optional.Of(neighbor.GetDependent().OriginalText().ToLower());
					}
					if (neighborRel.Contains("obj"))
					{
						obj = Optional.Of(neighbor.GetDependent().OriginalText().ToLower());
					}
					if (neighborRel.Contains("prep"))
					{
						pp = Optional.Of(neighborRel);
					}
				}
			}
			string prep = edge.GetRelation().ToString();
			string verb = edge.GetGovernor().OriginalText().ToLower();
			// Compute the most informative drop probability we can
			double rawScore = null;
			if (subj.IsPresent())
			{
				if (obj.IsPresent())
				{
					// Case: subj+obj
					rawScore = verbSubjObjPPAffinity[Quadruple.MakeQuadruple(verb, subj.Get(), obj.Get(), prep)];
				}
				if (rawScore == null && pp.IsPresent())
				{
					// Case: subj+other_pp
					rawScore = verbSubjPPPPAffinity[Quadruple.MakeQuadruple(verb, subj.Get(), pp.Get(), prep)];
				}
				if (rawScore == null)
				{
					// Case: subj
					rawScore = verbSubjPPAffinity[Triple.MakeTriple(verb, subj.Get(), prep)];
				}
			}
			if (rawScore == null)
			{
				// Case: just the original pp
				rawScore = verbPPAffinity[Pair.MakePair(verb, prep)];
			}
			if (rawScore == null)
			{
				return DeletionProbability(prep);
			}
			else
			{
				return 1.0 - Math.Min(1.0, rawScore / upperProbabilityCap);
			}
		}

		public virtual double DeletionProbability(SemanticGraphEdge edge, IEnumerable<SemanticGraphEdge> neighbors)
		{
			string edgeRel = edge.GetRelation().ToString();
			if (edgeRel.Contains("prep"))
			{
				return PpDeletionProbability(edge, neighbors);
			}
			else
			{
				if (edgeRel.Contains("obj"))
				{
					return ObjDeletionProbability(edge, neighbors);
				}
				else
				{
					if (edgeRel.Contains("subj"))
					{
						return SubjDeletionProbability(edge, neighbors);
					}
					else
					{
						if (edgeRel.Equals("amod"))
						{
							string word = (edge.GetDependent().Lemma() != null ? edge.GetDependent().Lemma() : edge.GetDependent().Word()).ToLower();
							if (Edu.Stanford.Nlp.Naturalli.Util.PrivativeAdjectives.Contains(word))
							{
								return 0.0;
							}
							else
							{
								return 1.0;
							}
						}
						else
						{
							return DeletionProbability(edgeRel);
						}
					}
				}
			}
		}

		/*
		private double backoffEdgeProbability(String edgeRel) {
		return 1.0;  // TODO(gabor) should probably learn these...
		}
		
		public double deletionProbability(String parent, String edgeRel) {
		return deletionProbability(parent, edgeRel, false);
		}
		
		public double deletionProbability(String parent, String edgeRel, boolean isSecondaryEdgeOfType) {
		if (edgeRel.startsWith("prep")) {
		double affinity = ppAffinity.getCount(parent, edgeRel);
		if (affinity != 0.0 && !isSecondaryEdgeOfType) {
		return Math.sqrt(1.0 - Math.min(1.0, affinity));
		} else {
		return backoffEdgeProbability(edgeRel);
		}
		} else if (edgeRel.startsWith("dobj")) {
		double affinity = dobjAffinity.getCount(parent);
		if (affinity != 0.0 && !isSecondaryEdgeOfType) {
		return Math.sqrt(1.0 - Math.min(1.0, affinity));
		} else {
		return backoffEdgeProbability(edgeRel);
		}
		} else {
		return backoffEdgeProbability(edgeRel);
		}
		}
		*/
		public static Edu.Stanford.Nlp.Naturalli.NaturalLogicWeights FromString(string str)
		{
			return new Edu.Stanford.Nlp.Naturalli.NaturalLogicWeights();
		}
		// TODO(gabor)
	}
}
