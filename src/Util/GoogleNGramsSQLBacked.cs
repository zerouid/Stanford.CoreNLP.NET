using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Sql;
using Java.Util;
using Java.Util.Zip;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>To query Google Ngrams counts from SQL in a memory efficient manner.</summary>
	/// <remarks>
	/// To query Google Ngrams counts from SQL in a memory efficient manner.
	/// To get count of a phrase, use GoogleNGramsSQLBacked.getCount(phrase). Set this class options using
	/// Execution.fillOptions(GoogleNGramsSQLBacked.class, props);
	/// </remarks>
	/// <author>Sonal Gupta</author>
	public class GoogleNGramsSQLBacked
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(GoogleNGramsSQLBacked));

		internal static bool populateTables = false;

		internal static ICollection<int> ngramsToPopulate = null;

		internal static string dataDir = "/u/nlp/scr/data/google-ngrams/data";

		internal static string googleNgram_hostname = "jonsson";

		internal static string googleNgram_dbname;

		internal static string googleNgram_username = "nlp";

		internal static string tablenamePrefix = "googlengrams_";

		internal static string escapetag = "tag";

		internal static ICollection<string> existingTablenames = null;

		internal static IConnection connection = null;

		private static string DBName;

		/// <exception cref="Java.Sql.SQLException"/>
		internal static void Connect()
		{
			if (connection == null)
			{
				System.Diagnostics.Debug.Assert(googleNgram_dbname != null, "set googleNgram_dbname variable through the properties file");
				connection = DriverManager.GetConnection("jdbc:postgresql://" + googleNgram_hostname + "/" + googleNgram_dbname, googleNgram_username, string.Empty);
			}
		}

		internal static string EscapeString(string str)
		{
			return "$" + escapetag + "$" + str + "$" + escapetag + "$";
		}

		/// <exception cref="Java.Sql.SQLException"/>
		public static bool ExistsTable(string tablename)
		{
			if (existingTablenames == null)
			{
				existingTablenames = new HashSet<string>();
				IDatabaseMetaData md = connection.GetMetaData();
				IResultSet rs = md.GetTables(null, null, "%", null);
				while (rs.Next())
				{
					existingTablenames.Add(rs.GetString(3).ToLower());
				}
			}
			return (existingTablenames.Contains(tablename.ToLower()));
		}

		/// <summary>Queries the SQL tables for the count of the phrase.</summary>
		/// <remarks>
		/// Queries the SQL tables for the count of the phrase.
		/// Returns -1 if the phrase doesn't exist
		/// </remarks>
		/// <param name="str">: phrase</param>
		/// <returns>: count, if exists. -1 if not.</returns>
		/// <exception cref="Java.Sql.SQLException"/>
		public static long GetCount(string str)
		{
			string query = null;
			try
			{
				Connect();
				str = str.Trim();
				if (str.Contains("'"))
				{
					str = StringUtils.EscapeString(str, new char[] { '\'' }, '\'');
				}
				int ngram = str.Split("\\s+").Length;
				string table = tablenamePrefix + ngram;
				if (!ExistsTable(table))
				{
					return -1;
				}
				string phrase = EscapeString(str);
				query = "select count from " + table + " where phrase='" + phrase + "';";
				IStatement stmt = connection.CreateStatement();
				IResultSet result = stmt.ExecuteQuery(query);
				if (result.Next())
				{
					return result.GetLong("count");
				}
				else
				{
					return -1;
				}
			}
			catch (SQLException e)
			{
				log.Info("Error getting count for " + str + ". The query was " + query);
				Sharpen.Runtime.PrintStackTrace(e);
				throw new Exception(e);
			}
		}

		/// <exception cref="Java.Sql.SQLException"/>
		public static IList<Pair<string, long>> GetCounts(ICollection<string> strs)
		{
			Connect();
			IList<Pair<string, long>> counts = new List<Pair<string, long>>();
			string query = string.Empty;
			foreach (string str in strs)
			{
				str = str.Trim();
				int ngram = str.Split("\\s+").Length;
				string table = tablenamePrefix + ngram;
				if (!ExistsTable(table))
				{
					counts.Add(new Pair(str, (long)-1));
					continue;
				}
				string phrase = EscapeString(str);
				query += "select count from " + table + " where phrase='" + phrase + "';";
			}
			if (query.IsEmpty())
			{
				return counts;
			}
			IPreparedStatement stmt = connection.PrepareStatement(query);
			bool isresult = stmt.Execute();
			IResultSet rs;
			IEnumerator<string> iter = strs.GetEnumerator();
			do
			{
				rs = stmt.GetResultSet();
				string ph = iter.Current;
				if (rs.Next())
				{
					counts.Add(new Pair(ph, rs.GetLong("count")));
				}
				else
				{
					counts.Add(new Pair(ph, (long)-1));
				}
				isresult = stmt.GetMoreResults();
			}
			while (isresult);
			System.Diagnostics.Debug.Assert((counts.Count == strs.Count));
			return counts;
		}

		//Adding google ngrams to the tables for the first time
		/// <exception cref="Java.Sql.SQLException"/>
		public static void PopulateTablesInSQL(string dir, ICollection<int> typesOfPhrases)
		{
			Connect();
			IStatement stmt = connection.CreateStatement();
			foreach (int n in typesOfPhrases)
			{
				string table = tablenamePrefix + n;
				if (!ExistsTable(table))
				{
					throw new Exception("Table " + table + " does not exist in the database! Run the following commands in the psql prompt:" + "create table GoogleNgrams_<NGRAM> (phrase text primary key not null, count bigint not null); create index phrase_<NGRAM> on GoogleNgrams_<NGRAM>(phrase);"
						);
				}
				foreach (string line in IOUtils.ReadLines(new File(dir + "/" + n + "gms/vocab_cs.gz"), typeof(GZIPInputStream)))
				{
					string[] tok = line.Split("\t");
					string q = "INSERT INTO " + table + " (phrase, count) VALUES (" + EscapeString(tok[0]) + " , " + tok[1] + ");";
					stmt.Execute(q);
				}
			}
		}

		/// <summary>
		/// Note that this is really really slow for ngram &gt; 1
		/// TODO: make this fast (if we had been using mysql we could have)
		/// </summary>
		public static int GetTotalCount(int ngram)
		{
			try
			{
				Connect();
				IStatement stmt = connection.CreateStatement();
				string table = tablenamePrefix + ngram;
				string q = "select count(*) from " + table + ";";
				IResultSet s = stmt.ExecuteQuery(q);
				if (s.Next())
				{
					return s.GetInt(1);
				}
				else
				{
					throw new Exception("getting table count is not working!");
				}
			}
			catch (SQLException e)
			{
				throw new Exception("getting table count is not working! " + e);
			}
		}

		/// <summary>Return rank of 1 gram in google ngeams if it is less than 20k.</summary>
		/// <remarks>Return rank of 1 gram in google ngeams if it is less than 20k. Otherwise -1.</remarks>
		public static int Get1GramRank(string str)
		{
			string query = null;
			try
			{
				Connect();
				str = str.Trim();
				if (str.Contains("'"))
				{
					str = StringUtils.EscapeString(str, new char[] { '\'' }, '\'');
				}
				int ngram = str.Split("\\s+").Length;
				if (ngram > 1)
				{
					return -1;
				}
				string table = "googlengrams_1_ranked20k";
				if (!ExistsTable(table))
				{
					return -1;
				}
				string phrase = EscapeString(str);
				query = "select rank from " + table + " where phrase='" + phrase + "';";
				IStatement stmt = connection.CreateStatement();
				IResultSet result = stmt.ExecuteQuery(query);
				if (result.Next())
				{
					return result.GetInt("rank");
				}
				else
				{
					return -1;
				}
			}
			catch (SQLException e)
			{
				log.Info("Error getting count for " + str + ". The query was " + query);
				Sharpen.Runtime.PrintStackTrace(e);
				throw new Exception(e);
			}
		}

		/// <exception cref="Java.Sql.SQLException"/>
		public static void CloseConnection()
		{
			if (connection != null)
			{
				connection.Close();
			}
			connection = null;
		}

		public static void Main(string[] args)
		{
			try
			{
				Properties props = StringUtils.ArgsToPropertiesWithResolve(args);
				ArgumentParser.FillOptions(typeof(GoogleNGramsSQLBacked), props);
				Connect();
				//if(populateTables)
				//  populateTablesInSQL(dataDir, ngramsToPopulate);
				//testing
				System.Console.Out.WriteLine("For head,the count is " + GetCount("head"));
				//System.out.println(getCount("what the heck"));
				//System.out.println(getCount("my name is john"));
				System.Console.Out.WriteLine(GetCounts(Arrays.AsList("cancer", "disease")));
				System.Console.Out.WriteLine("Get count 1 gram " + GetTotalCount(1));
				if (props.GetProperty("phrase") != null)
				{
					string p = props.GetProperty("phrase");
					System.Console.Out.WriteLine("count for phrase " + p + " is " + GetCount(p));
				}
				if (props.GetProperty("rank") != null)
				{
					string p = props.GetProperty("rank");
					System.Console.Out.WriteLine("Rank of " + p + " is " + Get1GramRank(p));
				}
				CloseConnection();
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		public static void SetDBName(string DBName)
		{
			googleNgram_dbname = DBName;
		}
	}
}
