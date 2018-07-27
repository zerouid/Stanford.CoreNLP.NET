using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International.Arabic.Process;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>This class will add segmentation information to an Annotation.</summary>
	/// <remarks>
	/// This class will add segmentation information to an Annotation.
	/// It assumes that the original document is a List of sentences under the
	/// SentencesAnnotation.class key, and that each sentence has a
	/// TextAnnotation.class key. This Annotator adds corresponding
	/// information under a CharactersAnnotation.class key prior to segmentation,
	/// and a TokensAnnotation.class key with value of a List of CoreLabel
	/// after segmentation.
	/// Based on the ChineseSegmenterAnnotator by Pi-Chuan Chang.
	/// </remarks>
	/// <author>Will Monroe</author>
	public class ArabicSegmenterAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.ArabicSegmenterAnnotator));

		private ArabicSegmenter segmenter;

		private readonly bool Verbose;

		private const string DefaultSegLoc = "/u/nlp/data/arabic-segmenter/arabic-segmenter-atb+bn+arztrain.ser.gz";

		public ArabicSegmenterAnnotator()
			: this(DefaultSegLoc, false)
		{
		}

		public ArabicSegmenterAnnotator(bool verbose)
			: this(DefaultSegLoc, verbose)
		{
		}

		public ArabicSegmenterAnnotator(string segLoc, bool verbose)
		{
			Verbose = verbose;
			Properties props = new Properties();
			LoadModel(segLoc, props);
		}

		public ArabicSegmenterAnnotator(string name, Properties props)
		{
			string model = null;
			// Keep only the properties that apply to this annotator
			Properties modelProps = new Properties();
			string desiredKey = name + '.';
			foreach (string key in props.StringPropertyNames())
			{
				if (key.StartsWith(desiredKey))
				{
					// skip past name and the subsequent "."
					string modelKey = Sharpen.Runtime.Substring(key, desiredKey.Length);
					if (modelKey.Equals("model"))
					{
						model = props.GetProperty(key);
					}
					else
					{
						modelProps.SetProperty(modelKey, props.GetProperty(key));
					}
				}
			}
			this.Verbose = PropertiesUtils.GetBool(props, name + ".verbose", false);
			if (model == null)
			{
				throw new Exception("Expected a property " + name + ".model");
			}
			LoadModel(model, modelProps);
		}

		private void LoadModel(string segLoc)
		{
			// don't write very much, because the CRFClassifier already reports loading
			if (Verbose)
			{
				log.Info("Loading segmentation model ... ");
			}
			Properties modelProps = new Properties();
			modelProps.SetProperty("model", segLoc);
			segmenter = ArabicSegmenter.GetSegmenter(modelProps);
		}

		private void LoadModel(string segLoc, Properties props)
		{
			// don't write very much, because the CRFClassifier already reports loading
			if (Verbose)
			{
				log.Info("Loading Segmentation Model ... ");
			}
			Properties modelProps = new Properties();
			modelProps.SetProperty("model", segLoc);
			modelProps.PutAll(props);
			try
			{
				segmenter = ArabicSegmenter.GetSegmenter(modelProps);
			}
			catch (Exception e)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		public virtual void Annotate(Annotation annotation)
		{
			if (Verbose)
			{
				log.Info("Adding Segmentation annotation ... ");
			}
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			if (sentences != null)
			{
				foreach (ICoreMap sentence in sentences)
				{
					DoOneSentence(sentence);
				}
			}
			else
			{
				DoOneSentence(annotation);
			}
		}

		private void DoOneSentence(ICoreMap annotation)
		{
			string text = annotation.Get(typeof(CoreAnnotations.TextAnnotation));
			IList<CoreLabel> tokens = segmenter.SegmentStringToTokenList(text);
			annotation.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.EmptySet();
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation), typeof(CoreAnnotations.BeforeAnnotation
				), typeof(CoreAnnotations.AfterAnnotation), typeof(CoreAnnotations.TokenBeginAnnotation), typeof(CoreAnnotations.TokenEndAnnotation), typeof(CoreAnnotations.PositionAnnotation), typeof(CoreAnnotations.IndexAnnotation), typeof(CoreAnnotations.OriginalTextAnnotation
				), typeof(CoreAnnotations.ValueAnnotation)));
		}
	}
}
