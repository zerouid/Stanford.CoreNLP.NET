using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Patterns.Surface
{
	/// <summary>Created by sonalg on 10/22/14.</summary>
	public class PatternsForEachTokenDB<E> : PatternsForEachToken<E>
		where E : Pattern
	{
		internal bool createTable = false;

		internal bool deleteExisting = false;

		internal string tableName = null;

		internal string patternindicesTable = "patternindices";

		internal bool deleteDBResourcesOnExit = true;

		public PatternsForEachTokenDB(Properties props, IDictionary<string, IDictionary<int, ICollection<E>>> pats)
		{
			ArgumentParser.FillOptions(this, props);
			ArgumentParser.FillOptions(typeof(SQLConnection), props);
			System.Diagnostics.Debug.Assert(tableName != null, "tableName property is null!");
			tableName = tableName.ToLower();
			if (createTable && !deleteExisting)
			{
				throw new Exception("Cannot have createTable as true and deleteExisting as false!");
			}
			if (createTable)
			{
				CreateTable();
				CreateUpsertFunction();
			}
			else
			{
				System.Diagnostics.Debug.Assert(DBTableExists(), "Table " + tableName + " does not exists. Pass createTable=true to create a new table");
			}
			if (pats != null)
			{
				AddPatterns(pats);
			}
		}

		public PatternsForEachTokenDB(Properties props)
			: this(props, null)
		{
		}

		internal virtual void CreateTable()
		{
			string query = string.Empty;
			try
			{
				IConnection conn = SQLConnection.GetConnection();
				if (DBTableExists())
				{
					if (deleteExisting)
					{
						System.Console.Out.WriteLine("deleting table " + tableName);
						IStatement stmt = conn.CreateStatement();
						query = "drop table " + tableName;
						stmt.Execute(query);
						stmt.Close();
						IStatement stmtindex = conn.CreateStatement();
						query = "DROP INDEX IF EXISTS " + tableName + "_index";
						stmtindex.Execute(query);
						stmtindex.Close();
					}
				}
				System.Console.Out.WriteLine("creating table " + tableName);
				IStatement stmt_1 = conn.CreateStatement();
				//query = "create table  IF NOT EXISTS " + tableName + " (\"sentid\" text, \"tokenid\" int, \"patterns\" bytea); ";
				query = "create table IF NOT EXISTS " + tableName + " (sentid text, patterns bytea); ";
				stmt_1.Execute(query);
				stmt_1.Close();
				conn.Close();
			}
			catch (SQLException e)
			{
				throw new Exception("Error executing query " + query + "\n" + e);
			}
		}

		public override void AddPatterns(IDictionary<string, IDictionary<int, ICollection<E>>> pats)
		{
			try
			{
				IConnection conn = null;
				IPreparedStatement pstmt = null;
				conn = SQLConnection.GetConnection();
				pstmt = GetPreparedStmt(conn);
				foreach (KeyValuePair<string, IDictionary<int, ICollection<E>>> en in pats)
				{
					AddPattern(en.Key, en.Value, pstmt);
					pstmt.AddBatch();
				}
				pstmt.ExecuteBatch();
				conn.Commit();
				pstmt.Close();
				conn.Close();
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		public override void AddPatterns(string id, IDictionary<int, ICollection<E>> p)
		{
			try
			{
				IPreparedStatement pstmt = null;
				IConnection conn = null;
				conn = SQLConnection.GetConnection();
				pstmt = GetPreparedStmt(conn);
				AddPattern(id, p, pstmt);
				pstmt.Execute();
				conn.Commit();
				pstmt.Close();
				conn.Close();
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		/*
		public void addPatterns(String id, Map<Integer, Set<Integer>> p, PreparedStatement pstmt) throws IOException, SQLException {
		for (Map.Entry<Integer, Set<Integer>> en2 : p.entrySet()) {
		addPattern(id, en2.getKey(), en2.getValue(), pstmt);
		if(useDBForTokenPatterns)
		pstmt.addBatch();
		}
		}
		*/
		/*
		public void addPatterns(String sentId, int tokenId, Set<Integer> patterns) throws SQLException, IOException{
		PreparedStatement pstmt = null;
		Connection conn= null;
		if(useDBForTokenPatterns) {
		conn = SQLConnection.getConnection();
		pstmt = getPreparedStmt(conn);
		}
		
		addPattern(sentId, tokenId, patterns, pstmt);
		
		if(useDBForTokenPatterns){
		pstmt.execute();
		conn.commit();
		pstmt.close();
		conn.close();
		}
		}
		*/
		/*
		private void addPattern(String sentId, int tokenId, Set<Integer> patterns, PreparedStatement pstmt) throws SQLException, IOException {
		
		if(pstmt != null){
		//      ByteArrayOutputStream baos = new ByteArrayOutputStream();
		//      ObjectOutputStream oos = new ObjectOutputStream(baos);
		//      oos.writeObject(patterns);
		//      byte[] patsAsBytes = baos.toByteArray();
		//      ByteArrayInputStream bais = new ByteArrayInputStream(patsAsBytes);
		//      pstmt.setBinaryStream(1, bais, patsAsBytes.length);
		//      pstmt.setObject(2, sentId);
		//      pstmt.setInt(3, tokenId);
		//      pstmt.setString(4,sentId);
		//      pstmt.setInt(5, tokenId);
		//      ByteArrayOutputStream baos2 = new ByteArrayOutputStream();
		//      ObjectOutputStream oos2 = new ObjectOutputStream(baos2);
		//      oos2.writeObject(patterns);
		//      byte[] patsAsBytes2 = baos2.toByteArray();
		//      ByteArrayInputStream bais2 = new ByteArrayInputStream(patsAsBytes2);
		//      pstmt.setBinaryStream(6, bais2, patsAsBytes2.length);
		//      pstmt.setString(7,sentId);
		//      pstmt.setInt(8, tokenId);
		
		ByteArrayOutputStream baos = new ByteArrayOutputStream();
		ObjectOutputStream oos = new ObjectOutputStream(baos);
		oos.writeObject(patterns);
		byte[] patsAsBytes = baos.toByteArray();
		ByteArrayInputStream bais = new ByteArrayInputStream(patsAsBytes);
		pstmt.setBinaryStream(3, bais, patsAsBytes.length);
		pstmt.setObject(1, sentId);
		pstmt.setInt(2, tokenId);
		
		
		} else{
		if(!patternsForEachToken.containsKey(sentId))
		patternsForEachToken.put(sentId, new ConcurrentHashMap<Integer, Set<Integer>>());
		patternsForEachToken.get(sentId).put(tokenId, patterns);
		}
		}*/
		/// <exception cref="Java.Sql.SQLException"/>
		/// <exception cref="System.IO.IOException"/>
		private void AddPattern(string sentId, IDictionary<int, ICollection<E>> patterns, IPreparedStatement pstmt)
		{
			if (pstmt != null)
			{
				ByteArrayOutputStream baos = new ByteArrayOutputStream();
				ObjectOutputStream oos = new ObjectOutputStream(baos);
				oos.WriteObject(patterns);
				byte[] patsAsBytes = baos.ToByteArray();
				ByteArrayInputStream bais = new ByteArrayInputStream(patsAsBytes);
				pstmt.SetBinaryStream(2, bais, patsAsBytes.Length);
				pstmt.SetObject(1, sentId);
			}
		}

		//pstmt.setInt(2, tokenId);
		public virtual void CreateUpsertFunction()
		{
			try
			{
				IConnection conn = SQLConnection.GetConnection();
				string s = "CREATE OR REPLACE FUNCTION upsert_patterns(sentid1 text, pats1 bytea) RETURNS VOID AS $$\n" + "DECLARE\n" + "BEGIN\n" + "    UPDATE " + tableName + " SET patterns = pats1 WHERE sentid = sentid1;\n" + "    IF NOT FOUND THEN\n" + "    INSERT INTO "
					 + tableName + "  values (sentid1, pats1);\n" + "    END IF;\n" + "END;\n" + "$$ LANGUAGE 'plpgsql';\n";
				IStatement st = conn.CreateStatement();
				st.Execute(s);
				conn.Close();
			}
			catch (SQLException e)
			{
				throw new Exception(e);
			}
		}

		/// <exception cref="Java.Sql.SQLException"/>
		public virtual void CreateUpsertFunctionPatternIndex()
		{
			IConnection conn = SQLConnection.GetConnection();
			string s = "CREATE OR REPLACE FUNCTION upsert_patternindex(tablename1 text, index1 bytea) RETURNS VOID AS $$\n" + "DECLARE\n" + "BEGIN\n" + "    UPDATE " + patternindicesTable + " SET index = index1 WHERE  tablename = tablename1;\n" + "    IF NOT FOUND THEN\n"
				 + "    INSERT INTO " + patternindicesTable + "  values (tablename1, index1);\n" + "    END IF;\n" + "END;\n" + "$$ LANGUAGE 'plpgsql';\n";
			IStatement st = conn.CreateStatement();
			st.Execute(s);
			conn.Close();
		}

		/// <exception cref="Java.Sql.SQLException"/>
		private IPreparedStatement GetPreparedStmt(IConnection conn)
		{
			conn.SetAutoCommit(false);
			//return conn.prepareStatement("UPDATE " + tableName + " SET patterns = ? WHERE sentid = ? and tokenid = ?; " +
			//  "INSERT INTO " + tableName + " (sentid, tokenid, patterns) (SELECT ?,?,? WHERE NOT EXISTS (SELECT sentid FROM " + tableName + " WHERE sentid  =? and tokenid=?));");
			//  return conn.prepareStatement("INSERT INTO " + tableName + " (sentid, tokenid, patterns) (SELECT ?,?,? WHERE NOT EXISTS (SELECT sentid FROM " + tableName + " WHERE sentid  =? and tokenid=?))");
			return conn.PrepareStatement("select upsert_patterns(?,?)");
		}

		/*
		public Set<Integer> getPatterns(String sentId, Integer tokenId) throws SQLException, IOException, ClassNotFoundException {
		if(useDBForTokenPatterns){
		Connection conn = SQLConnection.getConnection();
		
		String query = "Select patterns from " + tableName + " where sentid=\'" + sentId + "\' and tokenid = " + tokenId;
		Statement stmt = conn.createStatement();
		ResultSet rs = stmt.executeQuery(query);
		Set<Integer> pats = null;
		if(rs.next()){
		byte[] st = (byte[]) rs.getObject(1);
		ByteArrayInputStream baip = new ByteArrayInputStream(st);
		ObjectInputStream ois = new ObjectInputStream(baip);
		pats = (Set<Integer>) ois.readObject();
		
		}
		conn.close();
		return pats;
		}
		else
		return patternsForEachToken.get(sentId).get(tokenId);
		}*/
		public override IDictionary<int, ICollection<E>> GetPatternsForAllTokens(string sentId)
		{
			try
			{
				IConnection conn = SQLConnection.GetConnection();
				//Map<Integer, Set<Integer>> pats = new ConcurrentHashMap<Integer, Set<Integer>>();
				string query = "Select patterns from " + tableName + " where sentid=\'" + sentId + "\'";
				IStatement stmt = conn.CreateStatement();
				IResultSet rs = stmt.ExecuteQuery(query);
				IDictionary<int, ICollection<E>> patsToken = new Dictionary<int, ICollection<E>>();
				if (rs.Next())
				{
					byte[] st = (byte[])rs.GetObject(1);
					ByteArrayInputStream baip = new ByteArrayInputStream(st);
					ObjectInputStream ois = new ObjectInputStream(baip);
					patsToken = (IDictionary<int, ICollection<E>>)ois.ReadObject();
				}
				//pats.put(rs.getInt("tokenid"), patsToken);
				conn.Close();
				return patsToken;
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		public override bool Save(string dir)
		{
			//nothing to do
			return false;
		}

		public override void SetupSearch()
		{
		}

		//nothing to do
		public virtual bool ContainsSentId(string sentId)
		{
			try
			{
				IConnection conn = SQLConnection.GetConnection();
				string query = "Select tokenid from " + tableName + " where sentid=\'" + sentId + "\' limit 1";
				IStatement stmt = conn.CreateStatement();
				IResultSet rs = stmt.ExecuteQuery(query);
				bool contains = false;
				while (rs.Next())
				{
					contains = true;
					break;
				}
				conn.Close();
				return contains;
			}
			catch (SQLException e)
			{
				throw new Exception(e);
			}
		}

		public override void CreateIndexIfUsingDBAndNotExists()
		{
			try
			{
				Redwood.Log(Redwood.Dbg, "Creating index for " + tableName);
				IConnection conn = SQLConnection.GetConnection();
				IStatement stmt = conn.CreateStatement();
				bool doesnotexist = false;
				//check if the index already exists
				try
				{
					IStatement stmt2 = conn.CreateStatement();
					string query = "SELECT '" + tableName + "_index'::regclass";
					stmt2.Execute(query);
				}
				catch (SQLException)
				{
					doesnotexist = true;
				}
				if (doesnotexist)
				{
					string indexquery = "create index CONCURRENTLY " + tableName + "_index on " + tableName + " using hash(\"sentid\") ";
					stmt.Execute(indexquery);
					Redwood.Log(Redwood.Dbg, "Done creating index for " + tableName);
				}
			}
			catch (SQLException e)
			{
				throw new Exception(e);
			}
		}

		//  /**
		//   * not yet supported if backed by DB
		//   * @return
		//   */
		//  public Set<Map.Entry<String, Map<Integer, Set<Integer>>>> entrySet() {
		//    if(!useDBForTokenPatterns)
		//      return patternsForEachToken.entrySet();
		//    else
		//      //not yet supported if backed by DB
		//      throw new UnsupportedOperationException();
		//  }
		public virtual bool DBTableExists()
		{
			try
			{
				IConnection conn = null;
				conn = SQLConnection.GetConnection();
				IDatabaseMetaData dbm = conn.GetMetaData();
				IResultSet tables = dbm.GetTables(null, null, tableName, null);
				if (tables.Next())
				{
					System.Console.Out.WriteLine("Found table " + tableName);
					conn.Close();
					return true;
				}
				conn.Close();
				return false;
			}
			catch (SQLException e)
			{
				throw new Exception(e);
			}
		}

		public const int SingleBatch = 1;

		public const int SmallBatch = 4;

		public const int MediumBatch = 11;

		public const int LargeBatch = 51;

		//
		//  @Override
		//  public ConcurrentHashIndex<SurfacePattern> readPatternIndex(String dir){
		//    //dir parameter is not used!
		//    try{
		//      Connection conn = SQLConnection.getConnection();
		//      //Map<Integer, Set<Integer>> pats = new ConcurrentHashMap<Integer, Set<Integer>>();
		//      String query = "Select index from " + patternindicesTable + " where tablename=\'" + tableName + "\'";
		//      Statement stmt = conn.createStatement();
		//      ResultSet rs = stmt.executeQuery(query);
		//      ConcurrentHashIndex<SurfacePattern> index = null;
		//      if(rs.next()){
		//        byte[] st = (byte[]) rs.getObject(1);
		//        ByteArrayInputStream baip = new ByteArrayInputStream(st);
		//        ObjectInputStream ois = new ObjectInputStream(baip);
		//        index  = (ConcurrentHashIndex<SurfacePattern>) ois.readObject();
		//      }
		//      assert index != null;
		//      return index;
		//    }catch(SQLException e){
		//      throw new RuntimeException(e);
		//    } catch (ClassNotFoundException e) {
		//      throw new RuntimeException(e);
		//    } catch (IOException e) {
		//      throw new RuntimeException(e);
		//    }
		//  }
		//
		//  @Override
		//  public void savePatternIndex(ConcurrentHashIndex<SurfacePattern> index, String file) {
		//    try {
		//      createUpsertFunctionPatternIndex();
		//      Connection conn = SQLConnection.getConnection();
		//      PreparedStatement  st = conn.prepareStatement("select upsert_patternindex(?,?)");
		//      st.setString(1,tableName);
		//      ByteArrayOutputStream baos = new ByteArrayOutputStream();
		//      ObjectOutputStream oos = new ObjectOutputStream(baos);
		//      oos.writeObject(index);
		//      byte[] patsAsBytes = baos.toByteArray();
		//      ByteArrayInputStream bais = new ByteArrayInputStream(patsAsBytes);
		//      st.setBinaryStream(2, bais, patsAsBytes.length);
		//      st.execute();
		//      st.close();
		//      conn.close();
		//      System.out.println("Saved the pattern hash index for " + tableName + " in DB table " + patternindicesTable);
		//    }catch (SQLException e){
		//      throw new RuntimeException(e);
		//    } catch (IOException e) {
		//      throw new RuntimeException(e);
		//    }
		//  }
		//batch processing below is copied from Java Ranch
		//TODO: make this into an iterator!!
		public override IDictionary<string, IDictionary<int, ICollection<E>>> GetPatternsForAllTokens(ICollection<string> sampledSentIds)
		{
			try
			{
				IDictionary<string, IDictionary<int, ICollection<E>>> pats = new Dictionary<string, IDictionary<int, ICollection<E>>>();
				IConnection conn = SQLConnection.GetConnection();
				IEnumerator<string> iter = sampledSentIds.GetEnumerator();
				int totalNumberOfValuesLeftToBatch = sampledSentIds.Count;
				while (totalNumberOfValuesLeftToBatch > 0)
				{
					int batchSize = SingleBatch;
					if (totalNumberOfValuesLeftToBatch >= LargeBatch)
					{
						batchSize = LargeBatch;
					}
					else
					{
						if (totalNumberOfValuesLeftToBatch >= MediumBatch)
						{
							batchSize = MediumBatch;
						}
						else
						{
							if (totalNumberOfValuesLeftToBatch >= SmallBatch)
							{
								batchSize = SmallBatch;
							}
						}
					}
					totalNumberOfValuesLeftToBatch -= batchSize;
					StringBuilder inClause = new StringBuilder();
					for (int i = 0; i < batchSize; i++)
					{
						inClause.Append('?');
						if (i != batchSize - 1)
						{
							inClause.Append(',');
						}
					}
					IPreparedStatement stmt = conn.PrepareStatement("select sentid, patterns from " + tableName + " where sentid in (" + inClause.ToString() + ")");
					for (int i_1 = 0; i_1 < batchSize && iter.MoveNext(); i_1++)
					{
						stmt.SetString(i_1 + 1, iter.Current);
					}
					// or whatever values you are trying to query by
					stmt.Execute();
					IResultSet rs = stmt.GetResultSet();
					while (rs.Next())
					{
						string sentid = rs.GetString(1);
						byte[] st = (byte[])rs.GetObject(2);
						ByteArrayInputStream baip = new ByteArrayInputStream(st);
						ObjectInputStream ois = new ObjectInputStream(baip);
						pats[sentid] = (IDictionary<int, ICollection<E>>)ois.ReadObject();
					}
				}
				conn.Close();
				return pats;
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		public override void Close()
		{
		}

		//nothing to do
		public override void Load(string allPatternsDir)
		{
		}

		//nothing to do
		internal override int Size()
		{
			//TODO: NOT IMPLEMENTED
			return int.MaxValue;
		}
	}
}
