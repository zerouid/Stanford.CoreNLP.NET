using System.Collections.Generic;
using Edu.Stanford.Nlp.Loglinear.Model;
using Java.IO;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Storage
{
	/// <summary>Created on 10/17/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// The idea here is pretty straightforward, but requires some explanation.
	/// <p>
	/// GraphicalModels are great for storing lots of metadata about the model, though storing full featurizations can be a
	/// bit slow.
	/// <p>
	/// With a ModelBatch, you can get your models from anywhere, and after running LENSE on them (which will add lots of
	/// annotations, potentially) you can write those models to disk in a big fat batch. Those models you've stored can be
	/// stored without featurizing them, as long as you keep enough metadata to be able to featurize later. Then when you
	/// load a batch from disk to run simulations, you can try out different feature sets and gameplayers, all while keeping
	/// the beautifully precomputed metadata for the model (including instructions for querying, and the query logs).
	/// </author>
	[System.Serializable]
	public class ModelBatch : List<GraphicalModel>
	{
		/// <summary>Creates an empty ModelBatch</summary>
		public ModelBatch()
		{
		}

		/// <summary>This loads a model batch from a file, then closes the file handler.</summary>
		/// <remarks>This loads a model batch from a file, then closes the file handler. Just a convenience.</remarks>
		/// <param name="filename">the file to load from</param>
		/// <exception cref="System.IO.IOException"/>
		public ModelBatch(string filename)
			: this(filename, null)
		{
		}

		/// <summary>This loads a model batch from a file, then closes the file handler.</summary>
		/// <remarks>This loads a model batch from a file, then closes the file handler. Just a convenience.</remarks>
		/// <param name="filename">the file to load from</param>
		/// <param name="featurizer">
		/// a function that gets run on every GraphicalModel, and has a chance to edit them (eg by adding
		/// or changing features)
		/// </param>
		/// <exception cref="System.IO.IOException"/>
		public ModelBatch(string filename, IConsumer<GraphicalModel> featurizer)
		{
			InputStream @is = new FileInputStream(filename);
			ReadFrom(@is, featurizer);
			@is.Close();
		}

		/// <summary>Load a batch of models from disk, without specifying a function to re-featurize those models.</summary>
		/// <param name="inputStream">the inputstream to load from</param>
		/// <exception cref="System.IO.IOException"/>
		public ModelBatch(InputStream inputStream)
			: this(inputStream, null)
		{
		}

		/// <summary>
		/// Load a batch of models from disk, while running the function "featurizer" on each of the models before adding it
		/// to the batch.
		/// </summary>
		/// <remarks>
		/// Load a batch of models from disk, while running the function "featurizer" on each of the models before adding it
		/// to the batch. This gives the loader a chance to experiment with new featurization techniques.
		/// </remarks>
		/// <param name="inputStream">the input stream to load from</param>
		/// <param name="featurizer">
		/// a function that gets run on every GraphicalModel, and has a chance to edit them (eg by adding
		/// or changing features)
		/// </param>
		/// <exception cref="System.IO.IOException"/>
		public ModelBatch(InputStream inputStream, IConsumer<GraphicalModel> featurizer)
		{
			ReadFrom(inputStream, featurizer);
		}

		/// <summary>
		/// Load a batch of models from disk, while running the function "featurizer" on each of the models before adding it
		/// to the batch.
		/// </summary>
		/// <remarks>
		/// Load a batch of models from disk, while running the function "featurizer" on each of the models before adding it
		/// to the batch. This gives the loader a chance to experiment with new featurization techniques.
		/// </remarks>
		/// <param name="inputStream">the input stream to load from</param>
		/// <param name="featurizer">
		/// a function that gets run on every GraphicalModel, and has a chance to edit them (eg by adding
		/// or changing features)
		/// </param>
		/// <exception cref="System.IO.IOException"/>
		private void ReadFrom(InputStream inputStream, IConsumer<GraphicalModel> featurizer)
		{
			GraphicalModel read;
			while ((read = GraphicalModel.ReadFromStream(inputStream)) != null)
			{
				featurizer.Accept(read);
				Add(read);
			}
		}

		/// <summary>Convenience function to write the current state of the modelBatch out to a file, including all factors.</summary>
		/// <remarks>
		/// Convenience function to write the current state of the modelBatch out to a file, including all factors.
		/// <p>
		/// WARNING: These files can get quite large, if you're using large embeddings as features.
		/// </remarks>
		/// <param name="filename">the file to write the batch to</param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void WriteToFile(string filename)
		{
			FileOutputStream fos = new FileOutputStream(filename);
			WriteToStream(fos);
			fos.Close();
		}

		/// <summary>Convenience function to write the current state of the modelBatch out to a file, without factors.</summary>
		/// <param name="filename">the file to write the batch to</param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void WriteToFileWithoutFactors(string filename)
		{
			FileOutputStream fos = new FileOutputStream(filename);
			WriteToStreamWithoutFactors(fos);
			fos.Close();
		}

		/// <summary>This writes the entire batch, including all factors, to the given output stream.</summary>
		/// <remarks>
		/// This writes the entire batch, including all factors, to the given output stream.
		/// <p>
		/// WARNING: These files can get quite large, if you're using large embeddings as features.
		/// </remarks>
		/// <param name="outputStream">the outputstream to write our files to</param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void WriteToStream(OutputStream outputStream)
		{
			foreach (GraphicalModel model in this)
			{
				model.WriteToStream(outputStream);
			}
		}

		/// <summary>
		/// This writes the whole batch, WITHOUT FACTORS, which means that anyone loading this batch will need to include
		/// their own featurizer.
		/// </summary>
		/// <remarks>
		/// This writes the whole batch, WITHOUT FACTORS, which means that anyone loading this batch will need to include
		/// their own featurizer. Make sure that you have sufficient metadata to be able to do full featurizations.
		/// </remarks>
		/// <param name="outputStream">the outputstream to write our files to</param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void WriteToStreamWithoutFactors(OutputStream outputStream)
		{
			ICollection<GraphicalModel.Factor> emptySet = new HashSet<GraphicalModel.Factor>();
			foreach (GraphicalModel model in this)
			{
				ICollection<GraphicalModel.Factor> cachedFactors = model.factors;
				model.factors = emptySet;
				model.WriteToStream(outputStream);
				model.factors = cachedFactors;
			}
		}
	}
}
