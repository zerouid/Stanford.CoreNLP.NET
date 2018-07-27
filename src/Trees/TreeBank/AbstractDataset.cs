using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Trees.Treebank
{
	/// <author>Spence Green</author>
	public abstract class AbstractDataset : IDataset
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.Treebank.AbstractDataset));

		protected internal readonly IList<string> outputFileList;

		protected internal IMapper posMapper = null;

		protected internal string posMapOptions = string.Empty;

		protected internal IMapper lexMapper = null;

		protected internal string lexMapOptions = string.Empty;

		protected internal Dataset.Encoding encoding = Dataset.Encoding.Utf8;

		protected internal readonly IList<File> pathsToData;

		protected internal readonly IList<File> pathsToMappings;

		protected internal IFileFilter splitFilter = null;

		protected internal bool addDeterminer = false;

		protected internal bool removeDashTags = false;

		protected internal bool addRoot = false;

		protected internal bool removeEscapeTokens = false;

		protected internal int maxLen = int.MaxValue;

		protected internal string morphDelim = null;

		protected internal ITreeVisitor customTreeVisitor = null;

		protected internal string outFileName;

		protected internal string flatFileName;

		protected internal bool makeFlatFile = false;

		protected internal readonly Pattern fileNameNormalizer = Pattern.Compile("\\s+");

		protected internal Edu.Stanford.Nlp.Trees.Treebank treebank;

		protected internal readonly ICollection<string> configuredOptions;

		protected internal readonly ICollection<string> requiredOptions;

		protected internal readonly StringBuilder toStringBuffer;

		protected internal string treeFileExtension = "tree";

		/// <summary>Provides access for sub-classes to the data set parameters</summary>
		protected internal Properties options;

		public AbstractDataset()
		{
			//Current LDC releases use this extension
			outputFileList = new List<string>();
			pathsToData = new List<File>();
			pathsToMappings = new List<File>();
			toStringBuffer = new StringBuilder();
			//Read the raw file as UTF-8 irrespective of output encoding
			//    treebank = new DiskTreebank(new ArabicTreeReaderFactory.ArabicRawTreeReaderFactory(true), "UTF-8");
			configuredOptions = Generics.NewHashSet();
			requiredOptions = Generics.NewHashSet();
			requiredOptions.Add(ConfigParser.paramName);
			requiredOptions.Add(ConfigParser.paramPath);
			requiredOptions.Add(ConfigParser.paramEncode);
		}

		public abstract void Build();

		private IMapper LoadMapper(string className)
		{
			IMapper m = null;
			try
			{
				Type c = ClassLoader.GetSystemClassLoader().LoadClass(className);
				m = (IMapper)System.Activator.CreateInstance(c);
			}
			catch (TypeLoadException)
			{
				System.Console.Error.Printf("%s: Mapper type %s does not exist\n", this.GetType().FullName, className);
			}
			catch (InstantiationException e)
			{
				System.Console.Error.Printf("%s: Unable to instantiate mapper type %s\n", this.GetType().FullName, className);
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (MemberAccessException)
			{
				System.Console.Error.Printf("%s: Unable to access mapper type %s\n", this.GetType().FullName, className);
			}
			return m;
		}

		public virtual bool SetOptions(Properties opts)
		{
			options = opts;
			IList<string> sortedKeys = new List<string>(opts.StringPropertyNames());
			sortedKeys.Sort();
			foreach (string param in sortedKeys)
			{
				string value = opts.GetProperty(param);
				configuredOptions.Add(param);
				//Make matchers for the pre-fix parameters
				Matcher pathMatcher = ConfigParser.matchPath.Matcher(param);
				Matcher mapMatcher = ConfigParser.matchMapping.Matcher(param);
				if (pathMatcher.LookingAt())
				{
					pathsToData.Add(new File(value));
					configuredOptions.Add(ConfigParser.paramPath);
				}
				else
				{
					if (mapMatcher.LookingAt())
					{
						pathsToMappings.Add(new File(value));
						configuredOptions.Add(ConfigParser.paramMapping);
					}
					else
					{
						if (param.Equals(ConfigParser.paramEncode))
						{
							encoding = Dataset.Encoding.ValueOf(value);
						}
						else
						{
							if (param.Equals(ConfigParser.paramName))
							{
								Matcher inThisFilename = fileNameNormalizer.Matcher(value.Trim());
								outFileName = inThisFilename.ReplaceAll("-");
								toStringBuffer.Append(string.Format("Dataset Name: %s\n", value.Trim()));
							}
							else
							{
								if (param.Equals(ConfigParser.paramDT))
								{
									addDeterminer = bool.ParseBoolean(value);
								}
								else
								{
									if (param.Equals(ConfigParser.paramSplit))
									{
										ICollection<string> sm = BuildSplitMap(value);
										splitFilter = new AbstractDataset.SplitFilter(sm);
									}
									else
									{
										if (param.Equals(ConfigParser.paramFlat) && bool.ParseBoolean(value))
										{
											makeFlatFile = true;
										}
										else
										{
											if (param.Equals(ConfigParser.paramFileExt))
											{
												treeFileExtension = value;
											}
											else
											{
												if (param.Equals(ConfigParser.paramLexMapper))
												{
													lexMapper = LoadMapper(value);
												}
												else
												{
													if (param.Equals(ConfigParser.paramNoDashTags))
													{
														removeDashTags = bool.ParseBoolean(value);
													}
													else
													{
														if (param.Equals(ConfigParser.paramAddRoot))
														{
															addRoot = bool.ParseBoolean(value);
														}
														else
														{
															if (param.Equals(ConfigParser.paramUnEscape))
															{
																removeEscapeTokens = true;
															}
															else
															{
																if (param.Equals(ConfigParser.paramLexMapOptions))
																{
																	lexMapOptions = value;
																}
																else
																{
																	if (param.Equals(ConfigParser.paramPosMapper))
																	{
																		posMapper = LoadMapper(value);
																	}
																	else
																	{
																		if (param.Equals(ConfigParser.paramPosMapOptions))
																		{
																			posMapOptions = value;
																		}
																		else
																		{
																			if (param.Equals(ConfigParser.paramMaxLen))
																			{
																				maxLen = System.Convert.ToInt32(value);
																			}
																			else
																			{
																				if (param.Equals(ConfigParser.paramMorph))
																				{
																					morphDelim = value;
																				}
																				else
																				{
																					if (param.Equals(ConfigParser.paramTransform))
																					{
																						customTreeVisitor = LoadTreeVistor(value);
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			if (!configuredOptions.ContainsAll(requiredOptions))
			{
				return false;
			}
			//Finalize the output file names
			if (encoding == Dataset.Encoding.Utf8)
			{
				outFileName += ".utf8";
			}
			else
			{
				outFileName += ".bw";
			}
			string outputPath = opts.GetProperty(ConfigParser.paramOutputPath);
			if (outputPath != null)
			{
				outFileName = outputPath + File.separator + outFileName;
			}
			if (makeFlatFile)
			{
				flatFileName = outFileName + ".flat.txt";
			}
			outFileName += ".txt";
			return true;
		}

		private static ITreeVisitor LoadTreeVistor(string value)
		{
			try
			{
				Type c = ClassLoader.GetSystemClassLoader().LoadClass(value);
				return (ITreeVisitor)System.Activator.CreateInstance(c);
			}
			catch (ReflectiveOperationException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			return null;
		}

		protected internal virtual ICollection<string> BuildSplitMap(string path)
		{
			path = DataFilePaths.Convert(path);
			ICollection<string> fileSet = Generics.NewHashSet();
			LineNumberReader reader = null;
			try
			{
				reader = new LineNumberReader(new FileReader(path));
				while (reader.Ready())
				{
					string line = reader.ReadLine();
					fileSet.Add(line.Trim());
				}
				reader.Close();
			}
			catch (FileNotFoundException)
			{
				System.Console.Error.Printf("%s: Could not open split file %s\n", this.GetType().FullName, path);
			}
			catch (IOException)
			{
				System.Console.Error.Printf("%s: Error reading split file %s (line %d)\n", this.GetType().FullName, path, reader.GetLineNumber());
			}
			return fileSet;
		}

		//Filenames of the stuff that was created
		public virtual IList<string> GetFilenames()
		{
			return Java.Util.Collections.UnmodifiableList(outputFileList);
		}

		public override string ToString()
		{
			return toStringBuffer.ToString();
		}

		protected internal class SplitFilter : IFileFilter
		{
			private readonly ICollection<string> filterSet;

			public SplitFilter(ICollection<string> sm)
			{
				/*
				* Accepts a filename if it is present in <code>filterMap</code>. Rejects the filename otherwise.
				*/
				filterSet = sm;
			}

			public virtual bool Accept(File f)
			{
				return filterSet.Contains(f.GetName());
			}
		}
	}
}
