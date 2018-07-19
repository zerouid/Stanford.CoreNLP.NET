using System.IO;
using Edu.Stanford.Nlp.Tagger.Maxent;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent.Documentation
{
	/// <summary>Illustrates simple multithreading of threadsafe objects.</summary>
	/// <remarks>
	/// Illustrates simple multithreading of threadsafe objects. See
	/// the util.concurrent.MulticoreWrapperTest (unit test) for another example.
	/// </remarks>
	/// <author>Spence Green</author>
	public class MulticoreWrapperDemo
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.Documentation.MulticoreWrapperDemo));

		private MulticoreWrapperDemo()
		{
		}

		// static main
		/// <param name="args">Command-line arguments: modelFile (runs as a filter from stdin to stdout)</param>
		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				System.Console.Error.Printf("Usage: java %s model_file < input_file%n", typeof(Edu.Stanford.Nlp.Tagger.Maxent.Documentation.MulticoreWrapperDemo).FullName);
				System.Environment.Exit(-1);
			}
			try
			{
				// Load MaxentTagger, which is threadsafe
				string modelFile = args[0];
				MaxentTagger tagger = new MaxentTagger(modelFile);
				// Configure to run with 4 worker threads
				int nThreads = 4;
				MulticoreWrapper<string, string> wrapper = new MulticoreWrapper<string, string>(nThreads, new _IThreadsafeProcessor_42(tagger));
				// MaxentTagger is threadsafe
				// Submit jobs, which come from stdin
				BufferedReader br = new BufferedReader(new InputStreamReader(Runtime.@in));
				for (string line; (line = br.ReadLine()) != null; )
				{
					wrapper.Put(line);
					while (wrapper.Peek())
					{
						System.Console.Out.WriteLine(wrapper.Poll());
					}
				}
				// Finished reading the input. Wait for jobs to finish
				wrapper.Join();
				while (wrapper.Peek())
				{
					System.Console.Out.WriteLine(wrapper.Poll());
				}
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		private sealed class _IThreadsafeProcessor_42 : IThreadsafeProcessor<string, string>
		{
			public _IThreadsafeProcessor_42(MaxentTagger tagger)
			{
				this.tagger = tagger;
			}

			public string Process(string input)
			{
				return tagger.TagString(input);
			}

			public IThreadsafeProcessor<string, string> NewInstance()
			{
				return this;
			}

			private readonly MaxentTagger tagger;
		}
	}
}
