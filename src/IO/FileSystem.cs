using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util.Logging;







namespace Edu.Stanford.Nlp.IO
{
	/// <summary>
	/// Provides various filesystem operations common to scripting languages such
	/// as Perl and Python but not present (currently) in the Java standard libraries.
	/// </summary>
	/// <author>Spence Green</author>
	public sealed class FileSystem
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IO.FileSystem));

		private FileSystem()
		{
		}

		/// <summary>Copies a file.</summary>
		/// <remarks>Copies a file. The ordering of the parameters corresponds to the Unix cp command.</remarks>
		/// <param name="sourceFile">The file to copy.</param>
		/// <param name="destFile">The path to copy to which the file should be copied.</param>
		/// <exception cref="RuntimeIOException">If any IO problem</exception>
		public static void CopyFile(File sourceFile, File destFile)
		{
			try
			{
				if (!destFile.Exists())
				{
					destFile.CreateNewFile();
				}
			}
			catch (IOException ioe)
			{
				throw new RuntimeIOException(ioe);
			}
			try
			{
				using (FileChannel source = new FileInputStream(sourceFile).GetChannel())
				{
					using (FileChannel destination = new FileOutputStream(destFile).GetChannel())
					{
						destination.TransferFrom(source, 0, source.Size());
					}
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(string.Format("FileSystem: Error copying %s to %s%n", sourceFile.GetPath(), destFile.GetPath()), e);
			}
		}

		/// <summary>Similar to the unix gzip command, only it does not delete the file after compressing it.</summary>
		/// <param name="uncompressedFileName">The file to gzip</param>
		/// <param name="compressedFileName">The file name for the compressed file</param>
		/// <exception cref="System.IO.IOException"/>
		public static void GzipFile(File uncompressedFileName, File compressedFileName)
		{
			using (GZIPOutputStream @out = new GZIPOutputStream(new FileOutputStream(compressedFileName)))
			{
				using (FileInputStream @in = new FileInputStream(uncompressedFileName))
				{
					byte[] buf = new byte[1024];
					for (int len; (len = @in.Read(buf)) > 0; )
					{
						@out.Write(buf, 0, len);
					}
				}
			}
		}

		/// <summary>Recursively deletes a directory, including all files and sub-directories.</summary>
		/// <param name="dir">The directory to delete</param>
		/// <returns>true on success; false, otherwise.</returns>
		public static bool DeleteDir(File dir)
		{
			if (dir.IsDirectory())
			{
				string[] children = dir.List();
				if (children == null)
				{
					return false;
				}
				foreach (string aChildren in children)
				{
					bool success = DeleteDir(new File(dir, aChildren));
					if (!success)
					{
						return false;
					}
				}
			}
			return dir.Delete();
		}

		/// <summary>Returns whether a file object both exists and has contents (i.e.</summary>
		/// <remarks>Returns whether a file object both exists and has contents (i.e. the size of the file is greater than 0)</remarks>
		/// <param name="file"/>
		/// <returns>true if the file exists and is non-empty</returns>
		public static bool ExistsAndNonEmpty(File file)
		{
			if (!file.Exists())
			{
				return false;
			}
			IEnumerable<string> lines = IOUtils.ReadLines(file);
			string firstLine;
			try
			{
				firstLine = lines.GetEnumerator().Current;
			}
			catch (NoSuchElementException)
			{
				return false;
			}
			return firstLine.Length > 0;
		}

		/// <summary>Make the given directory or throw a RuntimeException</summary>
		public static void MkdirOrFail(string dir)
		{
			MkdirOrFail(new File(dir));
		}

		/// <summary>Make the given directory or throw a RuntimeException</summary>
		public static void MkdirOrFail(File dir)
		{
			if (!dir.Mkdirs())
			{
				string error = "Could not create " + dir;
				log.Info(error);
				throw new Exception(error);
			}
		}

		public static void CheckExistsOrFail(File file)
		{
			if (!file.Exists())
			{
				string error = "Output path " + file + " does not exist";
				log.Info(error);
				throw new Exception(error);
			}
		}

		public static void CheckNotExistsOrFail(File file)
		{
			if (file.Exists())
			{
				string error = "Output path " + file + " already exists";
				log.Info(error);
				throw new Exception(error);
			}
		}

		/// <summary>Unit test code</summary>
		public static void Main(string[] args)
		{
			string testDirName = "FileSystemTest";
			string testFileName = "Pair.java";
			File testDir = new File(testDirName);
			testDir.Mkdir();
			try
			{
				CopyFile(new File(testFileName), new File(testDirName + "/" + testFileName));
			}
			catch (RuntimeIOException)
			{
				log.Info("Copy failed");
				System.Environment.Exit(-1);
			}
			try
			{
				Runtime r = Runtime.GetRuntime();
				Process p = r.Exec(string.Format("tar -cf %s.tar %s", testDirName, testDirName));
				int ret_val;
				if ((ret_val = p.WaitFor()) != 0)
				{
					System.Console.Error.Printf("tar command returned %d%n", ret_val);
					System.Environment.Exit(-1);
				}
			}
			catch (IOException)
			{
				log.Info("Tar command failed");
				System.Environment.Exit(-1);
			}
			catch (Exception e)
			{
				log.Info("Tar command interrupted");
				Sharpen.Runtime.PrintStackTrace(e);
				System.Environment.Exit(-1);
			}
			try
			{
				GzipFile(new File(testDirName + ".tar"), new File(testDirName + ".tar.gz"));
			}
			catch (IOException)
			{
				log.Info("gzip command failed");
				System.Environment.Exit(-1);
			}
			bool deleteSuccess = DeleteDir(new File(testDirName));
			if (!deleteSuccess)
			{
				log.Info("Could not delete directory");
				System.Environment.Exit(-1);
			}
			System.Console.Out.WriteLine("Success!");
		}
	}
}
