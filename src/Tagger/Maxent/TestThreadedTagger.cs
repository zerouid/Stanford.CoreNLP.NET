// TestThreadedTagger -- StanfordMaxEnt, A Maximum Entropy Toolkit
// Copyright (c) 2002-2011 Leland Stanford Junior University
//
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
//    http://www-nlp.stanford.edu/software/tagger.shtml
using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>First, this runs a tagger once to see what results it comes up with.</summary>
	/// <remarks>
	/// First, this runs a tagger once to see what results it comes up with.
	/// Then it runs the same tagger in two separate threads to make sure the results are the same.
	/// The results are printed to stdout; the user is expected to verify they are as expected.
	/// Normally you would run MaxentTagger with command line arguments such as:
	/// -model ../data/tagger/my-left3words-distsim-wsj-0-18.tagger
	/// -testFile ../data/tagger/test-wsj-19-21 -verboseResults false
	/// If you provide the same arguments to this program, it will first
	/// run the given tagger on the given test file once to establish the
	/// "baseline" results.  It will then run the same tagger in more than
	/// one thread at the same time; the output for both threads should be
	/// the same if the MaxentTagger is re-entrant.  The number of threads
	/// to be run can be specified with -numThreads; the default is
	/// DEFAULT_NUM_THREADS.
	/// You can also provide multiple models.  After performing that test
	/// on model1, it will then run the same test file on model2, model3,
	/// etc to establish baseline results for that tagger.  After that, it
	/// runs both taggers at the same time.  The taggers should be
	/// completely separate structures.  In other words, the second tagger
	/// should not have clobbered any static state initialized by the first
	/// tagger.  Thus, the results of the two simultaneous taggers should
	/// be the same as the two taggers' baselines.
	/// Example arguments for the more complicated test:
	/// -model1 ../data/pos-tagger/newmodels/left3words-distsim-wsj-0-18.tagger
	/// -model2 ../data/pos-tagger/newmodels/left3words-wsj-0-18.tagger
	/// -testFile ../data/pos-tagger/training/english/test-wsj-19-21
	/// -verboseResults false
	/// </remarks>
	/// <author>John Bauer</author>
	internal class TestThreadedTagger
	{
		/// <summary>Default number of threads to launch in the first test.</summary>
		/// <remarks>
		/// Default number of threads to launch in the first test.
		/// Can be specified with -numThreads.
		/// </remarks>
		internal const int DefaultNumThreads = 2;

		internal const string ThreadFlag = "numThreads";

		private TestThreadedTagger()
		{
		}

		/// <summary>This internal class takes a config, a tagger, and a thread name.</summary>
		/// <remarks>
		/// This internal class takes a config, a tagger, and a thread name.
		/// The "run" method then runs the given tagger on the data file
		/// specified in the config.
		/// </remarks>
		private class TaggerThread : Thread
		{
			private readonly MaxentTagger tagger;

			private readonly string threadName;

			private string resultsString = string.Empty;

			// static methods
			public virtual string GetResultsString()
			{
				return resultsString;
			}

			internal TaggerThread(MaxentTagger tagger, string name)
			{
				this.tagger = tagger;
				this.threadName = name;
			}

			public override void Run()
			{
				try
				{
					Timing t = new Timing();
					TestClassifier testClassifier = new TestClassifier(tagger);
					long millis = t.Stop();
					resultsString = testClassifier.ResultsString(tagger);
					System.Console.Out.WriteLine("Thread " + threadName + " took " + millis + " milliseconds to tag " + testClassifier.GetNumWords() + " words.\n" + resultsString);
				}
				catch (IOException e)
				{
					throw new Exception(e);
				}
			}
		}

		// end class TaggerThread
		public static void CompareResults(string results, string baseline)
		{
			if (!results.Equals(baseline))
			{
				throw new Exception("Results different from expected baseline");
			}
		}

		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			RunThreadedTest(props);
		}

		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.Exception"/>
		public static void RunThreadedTest(Properties props)
		{
			List<Properties> configs = new List<Properties>();
			List<MaxentTagger> taggers = new List<MaxentTagger>();
			int numThreads = DefaultNumThreads;
			// let the user specify how many threads to run in the first test case
			if (props.GetProperty(ThreadFlag) != null)
			{
				numThreads = int.Parse(props.GetProperty(ThreadFlag));
			}
			// read in each of the taggers specified on the command line
			System.Console.Out.WriteLine();
			System.Console.Out.WriteLine("Loading taggers...");
			System.Console.Out.WriteLine();
			if (props.GetProperty("model") != null)
			{
				configs.Add(props);
				taggers.Add(new MaxentTagger(configs[0].GetProperty("model"), configs[0]));
			}
			else
			{
				int taggerNum = 1;
				string taggerName = "model" + taggerNum;
				while (props.GetProperty(taggerName) != null)
				{
					Properties newProps = new Properties();
					newProps.PutAll(props);
					newProps.SetProperty("model", props.GetProperty(taggerName));
					configs.Add(newProps);
					taggers.Add(new MaxentTagger(configs[taggerNum - 1].GetProperty("model"), configs[taggerNum - 1]));
					++taggerNum;
					taggerName = "model" + taggerNum;
				}
			}
			// no models at all => bad
			if (taggers.IsEmpty())
			{
				throw new ArgumentException("Please specify at least one of " + "-model or -model1");
			}
			System.Console.Out.WriteLine();
			System.Console.Out.WriteLine("Running the baseline results for tagger 1");
			System.Console.Out.WriteLine();
			// run baseline results for the first tagger model
			TestThreadedTagger.TaggerThread baselineThread = new TestThreadedTagger.TaggerThread(taggers[0], "BaseResults-1");
			baselineThread.Start();
			baselineThread.Join();
			List<string> baselineResults = new List<string>();
			baselineResults.Add(baselineThread.GetResultsString());
			System.Console.Out.WriteLine();
			System.Console.Out.WriteLine("Running " + numThreads + " threads of tagger 1");
			System.Console.Out.WriteLine();
			// run the first tagger in X separate threads at the same time
			// at the end of this test, those X threads should produce the same results
			List<TestThreadedTagger.TaggerThread> threads = new List<TestThreadedTagger.TaggerThread>();
			for (int i = 0; i < numThreads; ++i)
			{
				threads.Add(new TestThreadedTagger.TaggerThread(taggers[0], "Simultaneous-" + (i + 1)));
			}
			foreach (TestThreadedTagger.TaggerThread thread in threads)
			{
				thread.Start();
			}
			foreach (TestThreadedTagger.TaggerThread thread_1 in threads)
			{
				thread_1.Join();
				CompareResults(thread_1.GetResultsString(), baselineResults[0]);
			}
			// if we have more than one model...
			if (taggers.Count > 1)
			{
				// first, produce baseline results for the other models
				// do this one thread at a time so we know there are no
				// thread-related screwups
				// TODO: would iterables be cleaner?
				for (int i_1 = 1; i_1 < taggers.Count; ++i_1)
				{
					System.Console.Out.WriteLine();
					System.Console.Out.WriteLine("Running the baseline results for tagger " + (i_1 + 1));
					System.Console.Out.WriteLine();
					baselineThread = new TestThreadedTagger.TaggerThread(taggers[i_1], "BaseResults-" + (i_1 + 1));
					baselineThread.Start();
					baselineThread.Join();
					baselineResults.Add(baselineThread.GetResultsString());
				}
				System.Console.Out.WriteLine();
				System.Console.Out.WriteLine("Running " + taggers.Count + " threads of different taggers");
				System.Console.Out.WriteLine();
				// now, run the X models at the same time.  there used to be a
				// whole bunch of static state in the tagger, which used to mean
				// such a thing was not be possible to do.  now that should not
				// be a problem any more
				threads.Clear();
				for (int i_2 = 0; i_2 < taggers.Count; ++i_2)
				{
					threads.Add(new TestThreadedTagger.TaggerThread(taggers[i_2], "DifferentTaggers-" + (i_2 + 1)));
				}
				foreach (TestThreadedTagger.TaggerThread thread_2 in threads)
				{
					thread_2.Start();
				}
				for (int i_3 = 0; i_3 < taggers.Count; ++i_3)
				{
					TestThreadedTagger.TaggerThread thread_3 = threads[i_3];
					thread_3.Join();
					CompareResults(thread_3.GetResultsString(), baselineResults[i_3]);
				}
			}
			System.Console.Out.WriteLine("Done!");
		}
	}
}
