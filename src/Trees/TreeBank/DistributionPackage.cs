using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Trees.Treebank
{
	/// <summary>Adds data files to a tar'd / gzip'd distribution package.</summary>
	/// <remarks>
	/// Adds data files to a tar'd / gzip'd distribution package. Data sets marked with the DISTRIB parameter
	/// in
	/// <see cref="ConfigParser"/>
	/// are added to the archive.
	/// </remarks>
	/// <author>Spence Green</author>
	public class DistributionPackage
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.Treebank.DistributionPackage));

		private readonly IList<string> distFiles;

		private string lastCreatedDistribution = "UNKNOWN";

		public DistributionPackage()
		{
			distFiles = new List<string>();
		}

		/// <summary>Adds a listing of files to the distribution archive</summary>
		/// <param name="fileList">List of full file paths</param>
		public virtual void AddFiles(IList<string> fileList)
		{
			Sharpen.Collections.AddAll(distFiles, fileList);
		}

		/// <summary>Create the distribution and name the file according to the specified parameter.</summary>
		/// <param name="distribName">The name of distribution</param>
		/// <returns>True if the distribution is built. False otherwise.</returns>
		public virtual bool Make(string distribName)
		{
			bool createdDir = (new File(distribName)).Mkdir();
			if (createdDir)
			{
				string currentFile = string.Empty;
				try
				{
					foreach (string filename in distFiles)
					{
						currentFile = filename;
						File destFile = new File(filename);
						string relativePath = distribName + "/" + destFile.GetName();
						destFile = new File(relativePath);
						FileSystem.CopyFile(new File(filename), destFile);
					}
					string tarFileName = string.Format("%s.tar", distribName);
					Runtime r = Runtime.GetRuntime();
					Process p = r.Exec(string.Format("tar -cf %s %s/", tarFileName, distribName));
					if (p.WaitFor() == 0)
					{
						File tarFile = new File(tarFileName);
						FileSystem.GzipFile(tarFile, new File(tarFileName + ".gz"));
						tarFile.Delete();
						FileSystem.DeleteDir(new File(distribName));
						lastCreatedDistribution = distribName;
						return true;
					}
					else
					{
						System.Console.Error.Printf("%s: Unable to create tar file %s\n", this.GetType().FullName, tarFileName);
					}
				}
				catch (IOException)
				{
					System.Console.Error.Printf("%s: Unable to add file %s to distribution %s\n", this.GetType().FullName, currentFile, distribName);
				}
				catch (Exception e)
				{
					System.Console.Error.Printf("%s: tar did not return from building %s.tar\n", this.GetType().FullName, distribName);
					throw new RuntimeInterruptedException(e);
				}
			}
			else
			{
				System.Console.Error.Printf("%s: Unable to create temp directory %s\n", this.GetType().FullName, distribName);
			}
			return false;
		}

		public override string ToString()
		{
			string header = string.Format("Distributable package %s (%d files)\n", lastCreatedDistribution, distFiles.Count);
			StringBuilder sb = new StringBuilder(header);
			sb.Append("--------------------------------------------------------------------\n");
			foreach (string filename in distFiles)
			{
				sb.Append(string.Format("  %s\n", filename));
			}
			return sb.ToString();
		}
	}
}
