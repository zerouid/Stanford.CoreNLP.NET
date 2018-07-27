using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Quoteattribution
{
	/// <author>Michael Fang, Grace Muzny</author>
	public class QuoteAttributionEvaluation
	{
		private static readonly string[] mentionKeyOrder = new string[] { "trigram CVQ", "trigram VCQ", "trigram PVQ", "trigram VPQ", "trigram QVC", "trigram QCV", "trigram QVP", "trigram QPV", "Deterministic depparse", "Deterministic oneNameSentence"
			, "Deterministic Vocative -- name", "Deterministic Vocative -- animate noun", "Deterministic endQuoteClosestBefore", "Deterministic one speaker sentence", "supervised", "conv", "loose", null };

		private static readonly string[] speakerKeyOrder = new string[] { "automatic name", "coref", "Baseline Top conversation - prev", "Baseline Top conversation - next", "Baseline Top family animate", "Baseline Top" };

		private enum Result
		{
			Skipped,
			Correct,
			Incorrect
		}

		//these are hardcoded in the order we wish the results to be presented.
		private static string OutputMapResultsDefaultKeys(IDictionary<string, ICounter<QuoteAttributionEvaluation.Result>> tagResults, string[] keyOrder)
		{
			StringBuilder output = new StringBuilder();
			QuoteAttributionEvaluation.Result[] order = new QuoteAttributionEvaluation.Result[] { QuoteAttributionEvaluation.Result.Correct, QuoteAttributionEvaluation.Result.Incorrect, QuoteAttributionEvaluation.Result.Skipped };
			foreach (string tag in keyOrder)
			{
				ICounter<QuoteAttributionEvaluation.Result> resultsCounter = tagResults[tag];
				if (resultsCounter == null)
				{
					continue;
				}
				if (tag == null)
				{
					output.Append("No label" + "\t");
				}
				else
				{
					output.Append(tag + "\t");
				}
				foreach (QuoteAttributionEvaluation.Result result in order)
				{
					output.Append(result.ToString() + "\t" + resultsCounter.GetCount(result) + "\t");
				}
				//append total and precision
				double numCorrect = resultsCounter.GetCount(QuoteAttributionEvaluation.Result.Correct);
				double numIncorrect = resultsCounter.GetCount(QuoteAttributionEvaluation.Result.Incorrect);
				double total = numCorrect + numIncorrect;
				double precision = (total == 0) ? 0 : numCorrect / total;
				output.Append(total + "\t" + precision + "\n");
			}
			return output.ToString();
		}

		private static int GetQuoteChapter(Annotation doc, ICoreMap quote)
		{
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			return sentences[quote.Get(typeof(CoreAnnotations.SentenceBeginAnnotation))].Get(typeof(ChapterAnnotator.ChapterAnnotation));
		}

		private static void Evaluate(Annotation doc, IList<XMLToAnnotation.GoldQuoteInfo> goldList)
		{
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			IDictionary<string, ICounter<QuoteAttributionEvaluation.Result>> mentionPredTypeResults = new Dictionary<string, ICounter<QuoteAttributionEvaluation.Result>>();
			IDictionary<string, ICounter<QuoteAttributionEvaluation.Result>> speakerPredTypeResults = new Dictionary<string, ICounter<QuoteAttributionEvaluation.Result>>();
			ICounter<QuoteAttributionEvaluation.Result> mentionResults = new ClassicCounter<QuoteAttributionEvaluation.Result>();
			ICounter<QuoteAttributionEvaluation.Result> speakerResults = new ClassicCounter<QuoteAttributionEvaluation.Result>();
			//aggregate counts
			for (int i = 0; i < quotes.Count; i++)
			{
				ICoreMap quote = quotes[i];
				XMLToAnnotation.GoldQuoteInfo gold = goldList[i];
				if (gold.speaker.Equals("UNSURE") || gold.speaker.Equals("NOTANUTTERANCE") || gold.mentionStartTokenIndex == -1)
				{
					continue;
				}
				string speakerPred = quote.Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation));
				int mentionBeginPred = quote.Get(typeof(QuoteAttributionAnnotator.MentionBeginAnnotation));
				int mentionEndPred = quote.Get(typeof(QuoteAttributionAnnotator.MentionEndAnnotation));
				QuoteAttributionEvaluation.Result mentionResult;
				if (mentionBeginPred == null)
				{
					mentionResult = QuoteAttributionEvaluation.Result.Skipped;
				}
				else
				{
					if ((gold.mentionStartTokenIndex <= mentionBeginPred && gold.mentionEndTokenIndex >= mentionEndPred) || (gold.mentionStartTokenIndex <= mentionEndPred && gold.mentionEndTokenIndex >= mentionEndPred))
					{
						mentionResult = QuoteAttributionEvaluation.Result.Correct;
					}
					else
					{
						mentionResult = QuoteAttributionEvaluation.Result.Incorrect;
					}
				}
				QuoteAttributionEvaluation.Result speakerResult;
				if (speakerPred == null)
				{
					speakerResult = QuoteAttributionEvaluation.Result.Skipped;
				}
				else
				{
					if (speakerPred.Equals(gold.speaker))
					{
						speakerResult = QuoteAttributionEvaluation.Result.Correct;
					}
					else
					{
						speakerResult = QuoteAttributionEvaluation.Result.Incorrect;
					}
				}
				bool verbose = true;
				if (verbose)
				{
					if (!mentionResult.Equals(QuoteAttributionEvaluation.Result.Correct) || !speakerResult.Equals(QuoteAttributionEvaluation.Result.Correct))
					{
						System.Console.Out.WriteLine("====");
						System.Console.Out.WriteLine("Id: " + i + " Quote: " + quote.Get(typeof(CoreAnnotations.TextAnnotation)));
						System.Console.Out.WriteLine("Speaker: " + goldList[i].speaker + " Predicted: " + quote.Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation)) + " " + speakerResult.ToString());
						System.Console.Out.WriteLine("Speaker Tag: " + quote.Get(typeof(QuoteAttributionAnnotator.SpeakerSieveAnnotation)));
						System.Console.Out.WriteLine("Gold Mention: " + gold.mention);
						// + " context: " + tokenRangeToString(goldRangeExtended, doc));
						if (mentionResult.Equals(QuoteAttributionEvaluation.Result.Incorrect))
						{
							System.Console.Out.WriteLine("Predicted Mention: " + quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) + " INCORRECT");
							System.Console.Out.WriteLine("Mention Tag: " + quote.Get(typeof(QuoteAttributionAnnotator.MentionSieveAnnotation)));
						}
						else
						{
							if (mentionResult.Equals(QuoteAttributionEvaluation.Result.Skipped))
							{
								System.Console.Out.WriteLine("Mention SKIPPED");
							}
							else
							{
								System.Console.Out.WriteLine("Gold Mention: " + quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) + " CORRECT");
								System.Console.Out.WriteLine("Mention tag: " + quote.Get(typeof(QuoteAttributionAnnotator.MentionSieveAnnotation)));
							}
						}
					}
					else
					{
						System.Console.Out.WriteLine("====");
						System.Console.Out.WriteLine("Id: " + i + " Quote: " + quote.Get(typeof(CoreAnnotations.TextAnnotation)));
						System.Console.Out.WriteLine("Mention Tag: " + quote.Get(typeof(QuoteAttributionAnnotator.MentionSieveAnnotation)) + " Speaker Tag: " + quote.Get(typeof(QuoteAttributionAnnotator.SpeakerSieveAnnotation)));
						System.Console.Out.WriteLine("Speaker: " + quote.Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation)) + " Mention: " + quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)));
						System.Console.Out.WriteLine("ALL CORRECT");
					}
				}
				mentionResults.IncrementCount(mentionResult);
				speakerResults.IncrementCount(speakerResult);
				mentionPredTypeResults.PutIfAbsent(quote.Get(typeof(QuoteAttributionAnnotator.MentionSieveAnnotation)), new ClassicCounter<QuoteAttributionEvaluation.Result>());
				mentionPredTypeResults[quote.Get(typeof(QuoteAttributionAnnotator.MentionSieveAnnotation))].IncrementCount(mentionResult);
				speakerPredTypeResults.PutIfAbsent(quote.Get(typeof(QuoteAttributionAnnotator.SpeakerSieveAnnotation)), new ClassicCounter<QuoteAttributionEvaluation.Result>());
				speakerPredTypeResults[quote.Get(typeof(QuoteAttributionAnnotator.SpeakerSieveAnnotation))].IncrementCount(speakerResult);
			}
			//output results
			double mCorrect = mentionResults.GetCount(QuoteAttributionEvaluation.Result.Correct);
			double mIncorrect = mentionResults.GetCount(QuoteAttributionEvaluation.Result.Incorrect);
			double mSkipped = mentionResults.GetCount(QuoteAttributionEvaluation.Result.Skipped);
			double mPrecision = mCorrect / (mCorrect + mIncorrect);
			double mRecall = mCorrect / (mCorrect + mSkipped);
			double mF1 = (2 * (mPrecision * mRecall) / (mPrecision + mRecall));
			double mAccuracy = mCorrect / (mCorrect + mIncorrect + mSkipped);
			double sCorrect = speakerResults.GetCount(QuoteAttributionEvaluation.Result.Correct);
			double sIncorrect = speakerResults.GetCount(QuoteAttributionEvaluation.Result.Incorrect);
			double sSkipped = speakerResults.GetCount(QuoteAttributionEvaluation.Result.Skipped);
			double sPrecision = sCorrect / (sCorrect + sIncorrect);
			double sRecall = sCorrect / (sCorrect + sSkipped);
			double sF1 = (2 * (sPrecision * sRecall) / (sPrecision + sRecall));
			double sAccuracy = sCorrect / (sCorrect + sIncorrect + sSkipped);
			System.Console.Out.WriteLine(OutputMapResultsDefaultKeys(mentionPredTypeResults, mentionKeyOrder));
			System.Console.Out.WriteLine(OutputMapResultsDefaultKeys(speakerPredTypeResults, speakerKeyOrder));
			System.Console.Out.Printf("Mention C:%d\tI:%d\tS:%d\tP:%.3f\tR:%.3f\tF1:%.3f\tA:%.3f\t\tSpeaker C:%d\tI:%d\tS:%d\tP:%.3f\tR:%.3f\tF1:%.3f\tA:%.3f\n", (int)mCorrect, (int)mIncorrect, (int)mSkipped, mPrecision, mRecall, mF1, mAccuracy, (int)sCorrect
				, (int)sIncorrect, (int)sSkipped, sPrecision, sRecall, sF1, sAccuracy);
		}

		/// <summary>Usage: java QuoteAttributionEvaluation path_to_properties_file</summary>
		/// <param name="args"/>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			// make the first argument one for a base directory
			if (args.Length != 1)
			{
				System.Console.Out.WriteLine("Usage: java QuoteAttributionEvaluation path_to_properties_file");
				System.Environment.Exit(1);
			}
			string specificFile = args[0];
			System.Console.Out.WriteLine("Using properties file: " + specificFile);
			Properties props = StringUtils.PropFileToProperties(specificFile);
			//convert XML file to (1) the Annotation (data.doc) (2) a list of people in the text (data.personList)
			// and (3) the gold info to be used by evaluate (data.goldList).
			XMLToAnnotation.Data data = XMLToAnnotation.ReadXMLFormat(props.GetProperty("file"));
			Properties annotatorProps = new Properties();
			//    XMLToAnnotation.writeCharacterList("characterListPP.txt", data.personList); //Use this to write the person list to a file
			annotatorProps.SetProperty("charactersPath", props.GetProperty("charactersPath"));
			annotatorProps.SetProperty("booknlpCoref", props.GetProperty("booknlpCoref"));
			annotatorProps.SetProperty("familyWordsFile", props.GetProperty("familyWordsFile"));
			annotatorProps.SetProperty("animacyWordsFile", props.GetProperty("animacyWordsFile"));
			annotatorProps.SetProperty("genderNamesFile", props.GetProperty("genderNamesFile"));
			annotatorProps.SetProperty("modelPath", props.GetProperty("modelPath"));
			QuoteAttributionAnnotator qaa = new QuoteAttributionAnnotator(annotatorProps);
			qaa.Annotate(data.doc);
			Evaluate(data.doc, data.goldList);
		}
	}
}
