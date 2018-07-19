using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Patterns;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns.Surface
{
	/// <summary>Created by Sonal Gupta on 10/8/14.</summary>
	public abstract class PatternsForEachToken<E>
	{
		private static ConstantsAndVariables.PatternForEachTokenWay storeWay;

		public abstract void AddPatterns(IDictionary<string, IDictionary<int, ICollection<E>>> pats);

		public abstract void AddPatterns(string id, IDictionary<int, ICollection<E>> p);

		public abstract void CreateIndexIfUsingDBAndNotExists();

		public abstract IDictionary<int, ICollection<E>> GetPatternsForAllTokens(string sentId);

		public abstract bool Save(string dir);

		//  /**
		//   * Only for Lucene and DB
		//   * @return
		//   */
		//  abstract public PatternIndex readPatternIndex(String dir) throws IOException, ClassNotFoundException;
		public abstract void SetupSearch();

		internal abstract int Size();

		//abstract public void savePatternIndex(PatternIndex index, String dir) throws IOException;
		public virtual void UpdatePatterns(IDictionary<string, IDictionary<int, ICollection<E>>> tempPatsForSents)
		{
			foreach (KeyValuePair<string, IDictionary<int, ICollection<E>>> en in tempPatsForSents)
			{
				IDictionary<int, ICollection<E>> m = GetPatternsForAllTokens(en.Key);
				if (m == null)
				{
					m = new Dictionary<int, ICollection<E>>();
				}
				tempPatsForSents[en.Key].PutAll(m);
			}
			this.AddPatterns(tempPatsForSents);
			Close();
		}

		public virtual ConstantsAndVariables.PatternForEachTokenWay GetStoreWay()
		{
			return storeWay;
		}

		public static PatternsForEachToken GetPatternsInstance(Properties props, ConstantsAndVariables.PatternForEachTokenWay storePatsForEachToken)
		{
			storeWay = storePatsForEachToken;
			PatternsForEachToken p = null;
			switch (storePatsForEachToken)
			{
				case ConstantsAndVariables.PatternForEachTokenWay.Memory:
				{
					p = new PatternsForEachTokenInMemory(props);
					break;
				}

				case ConstantsAndVariables.PatternForEachTokenWay.Db:
				{
					p = new PatternsForEachTokenDB(props);
					break;
				}

				case ConstantsAndVariables.PatternForEachTokenWay.Lucene:
				{
					try
					{
						Type c = Sharpen.Runtime.GetType("edu.stanford.nlp.patterns.surface.PatternsForEachTokenLucene");
						p = (PatternsForEachToken)c.GetDeclaredConstructor(typeof(Properties)).NewInstance(props);
						break;
					}
					catch (TypeLoadException)
					{
						throw new Exception("Lucene option is not distributed (license clash). Email us if you really want it.");
					}
					catch (ReflectiveOperationException e)
					{
						throw new Exception(e);
					}
					break;
				}
			}
			return p;
		}

		//if(storePatsForEachToken.equals(DB)){}
		public abstract IDictionary<string, IDictionary<int, ICollection<E>>> GetPatternsForAllTokens(ICollection<string> sampledSentIds);

		public abstract void Close();

		public abstract void Load(string allPatternsDir);
		//  @Option(name="allPatternsFile")
		//  String allPatternsFile = null;
		//
		//  /**
		//   * If all patterns should be computed. Otherwise patterns are read from
		//   * allPatternsFile
		//   */
		//  @Option(name = "computeAllPatterns")
		//  public boolean computeAllPatterns = true;
		//Connection conn;
		//  public PatternsForEachToken(Properties props, Map<String, Map<Integer, Set<Integer>>> pats) throws SQLException, ClassNotFoundException, IOException {
		//    Execution.fillOptions(this, props);
		//
		//    if (useDBForTokenPatterns) {
		//      Execution.fillOptions(SQLConnection.class, props);
		//
		//      assert tableName != null : "tableName property is null!";
		//      tableName = tableName.toLowerCase();
		//      if (createTable && !deleteExisting)
		//        throw new RuntimeException("Cannot have createTable as true and deleteExisting as false!");
		//      if (createTable){
		//        createTable();
		//        createUpsertFunction();
		//      }else{
		//        assert DBTableExists() : "Table " + tableName + " does not exists. Pass createTable=true to create a new table";
		//      }
		//    }else
		//      patternsForEachToken = new ConcurrentHashMap<String, Map<Integer, Set<Integer>>>();
		//
		//    if(pats != null)
		//      addPatterns(pats);
		//  }
		//
		//  public PatternsForEachToken(){}
		//
		//  public PatternsForEachToken(Properties props) throws SQLException, IOException, ClassNotFoundException {
		//    this(props, null);
		//  }
		//  public void addPatterns(Map<String, Map<Integer, Set<Integer>>> pats) throws IOException, SQLException {
		//    Connection conn = null;
		//    PreparedStatement pstmt = null;
		//
		//    if(useDBForTokenPatterns) {
		//      conn = SQLConnection.getConnection();
		//      pstmt =getPreparedStmt(conn);
		//    }
		//
		//    for (Map.Entry<String, Map<Integer, Set<Integer>>> en : pats.entrySet()) {
		//      addPattern(en.getKey(), en.getValue(), pstmt);
		//      if(useDBForTokenPatterns)
		//        pstmt.addBatch();
		//    }
		//
		//    if(useDBForTokenPatterns){
		//      pstmt.executeBatch();
		//      conn.commit();
		//      pstmt.close();
		//      conn.close();
		//    }
		//  }
		//
		//  public void addPatterns(String id, Map<Integer, Set<Integer>> p) throws IOException, SQLException {
		//    PreparedStatement pstmt = null;
		//    Connection conn= null;
		//
		//    if(useDBForTokenPatterns) {
		//      conn = SQLConnection.getConnection();
		//      pstmt = getPreparedStmt(conn);
		//    }
		//
		//    addPattern(id, p, pstmt);
		//
		//    if(useDBForTokenPatterns){
		//      pstmt.execute();
		//      conn.commit();
		//      pstmt.close();
		//      conn.close();
		//    }
		//  }
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
		//  private void addPattern(String sentId, Map<Integer, Set<Integer>> patterns, PreparedStatement pstmt) throws SQLException, IOException {
		//
		//    if(pstmt != null){
		//      ByteArrayOutputStream baos = new ByteArrayOutputStream();
		//      ObjectOutputStream oos = new ObjectOutputStream(baos);
		//      oos.writeObject(patterns);
		//      byte[] patsAsBytes = baos.toByteArray();
		//      ByteArrayInputStream bais = new ByteArrayInputStream(patsAsBytes);
		//      pstmt.setBinaryStream(2, bais, patsAsBytes.length);
		//      pstmt.setObject(1, sentId);
		//      //pstmt.setInt(2, tokenId);
		//
		//
		//    } else{
		//      if(!patternsForEachToken.containsKey(sentId))
		//        patternsForEachToken.put(sentId, new ConcurrentHashMap<Integer, Set<Integer>>());
		//      patternsForEachToken.get(sentId).putAll(patterns);
		//    }
		//  }
		//
		//
		//  public void createUpsertFunction() throws SQLException {
		//    Connection conn = SQLConnection.getConnection();
		//    String s = "CREATE OR REPLACE FUNCTION upsert_patterns(sentid1 text, pats1 bytea) RETURNS VOID AS $$\n" +
		//      "DECLARE\n" +
		//      "BEGIN\n" +
		//      "    UPDATE " + tableName+ " SET patterns = pats1 WHERE sentid = sentid1;\n" +
		//      "    IF NOT FOUND THEN\n" +
		//      "    INSERT INTO " + tableName + "  values (sentid1, pats1);\n" +
		//      "    END IF;\n" +
		//      "END;\n" +
		//      "$$ LANGUAGE 'plpgsql';\n";
		//    Statement st = conn.createStatement();
		//    st.execute(s);
		//    conn.close();
		//  }
		//
		//  public void createUpsertFunctionPatternIndex() throws SQLException {
		//    Connection conn = SQLConnection.getConnection();
		//    String s = "CREATE OR REPLACE FUNCTION upsert_patternindex(tablename1 text, index1 bytea) RETURNS VOID AS $$\n" +
		//      "DECLARE\n" +
		//      "BEGIN\n" +
		//      "    UPDATE " + patternindicesTable + " SET index = index1 WHERE  tablename = tablename;\n" +
		//      "    IF NOT FOUND THEN\n" +
		//      "    INSERT INTO " + patternindicesTable + "  values (tablename1, index1);\n" +
		//      "    END IF;\n" +
		//      "END;\n" +
		//      "$$ LANGUAGE 'plpgsql';\n";
		//    Statement st = conn.createStatement();
		//    st.execute(s);
		//    conn.close();
		//  }
		//
		//
		//
		//
		//
		//
		//  private PreparedStatement getPreparedStmt(Connection conn) throws SQLException {
		//    conn.setAutoCommit(false);
		//    //return conn.prepareStatement("UPDATE " + tableName + " SET patterns = ? WHERE sentid = ? and tokenid = ?; " +
		//    //  "INSERT INTO " + tableName + " (sentid, tokenid, patterns) (SELECT ?,?,? WHERE NOT EXISTS (SELECT sentid FROM " + tableName + " WHERE sentid  =? and tokenid=?));");
		//    //  return conn.prepareStatement("INSERT INTO " + tableName + " (sentid, tokenid, patterns) (SELECT ?,?,? WHERE NOT EXISTS (SELECT sentid FROM " + tableName + " WHERE sentid  =? and tokenid=?))");
		//    return conn.prepareStatement("select upsert_patterns(?,?)");
		//  }
		//
		//
		//
		//
		///*
		//  public Set<Integer> getPatterns(String sentId, Integer tokenId) throws SQLException, IOException, ClassNotFoundException {
		//    if(useDBForTokenPatterns){
		//      Connection conn = SQLConnection.getConnection();
		//
		//      String query = "Select patterns from " + tableName + " where sentid=\'" + sentId + "\' and tokenid = " + tokenId;
		//      Statement stmt = conn.createStatement();
		//      ResultSet rs = stmt.executeQuery(query);
		//      Set<Integer> pats = null;
		//      if(rs.next()){
		//        byte[] st = (byte[]) rs.getObject(1);
		//        ByteArrayInputStream baip = new ByteArrayInputStream(st);
		//        ObjectInputStream ois = new ObjectInputStream(baip);
		//        pats = (Set<Integer>) ois.readObject();
		//
		//      }
		//      conn.close();
		//      return pats;
		//    }
		//    else
		//      return patternsForEachToken.get(sentId).get(tokenId);
		//  }*/
		//
		//
		//
		//  public Map<Integer, Set<Integer>> getPatternsForAllTokens(String sentId) throws SQLException, IOException, ClassNotFoundException {
		//    if(useDBForTokenPatterns){
		//      Connection conn = SQLConnection.getConnection();
		//      //Map<Integer, Set<Integer>> pats = new ConcurrentHashMap<Integer, Set<Integer>>();
		//      String query = "Select patterns from " + tableName + " where sentid=\'" + sentId + "\'";
		//      Statement stmt = conn.createStatement();
		//      ResultSet rs = stmt.executeQuery(query);
		//      Map<Integer, Set<Integer>> patsToken = new HashMap<Integer, Set<Integer>>();
		//      if(rs.next()){
		//        byte[] st = (byte[]) rs.getObject(1);
		//        ByteArrayInputStream baip = new ByteArrayInputStream(st);
		//        ObjectInputStream ois = new ObjectInputStream(baip);
		//        patsToken = (Map<Integer, Set<Integer>>) ois.readObject();
		//        //pats.put(rs.getInt("tokenid"), patsToken);
		//      }
		//      conn.close();
		//      return patsToken;
		//    }
		//    else
		//      return patternsForEachToken.containsKey(sentId) ? patternsForEachToken.get(sentId): Collections.emptyMap();
		//  }
		//
		//
		//
		//  boolean getUseDBForTokenPatterns(){
		//    return useDBForTokenPatterns;
		//  }
		//
		//  public boolean writePatternsIfInMemory(String allPatternsFile) throws IOException {
		//    if(!useDBForTokenPatterns)
		//    {
		//      IOUtils.writeObjectToFile(this.patternsForEachToken, allPatternsFile);
		//      return true;
		//    }
		//    return false;
		//  }
		//
		//
		//  public boolean containsSentId(String sentId) throws SQLException {
		//    if(!useDBForTokenPatterns)
		//      return this.patternsForEachToken.containsKey(sentId);
		//    else {
		//      Connection conn = SQLConnection.getConnection();
		//      String query = "Select tokenid from " + tableName + " where sentid=\'" + sentId + "\' limit 1";
		//      Statement stmt = conn.createStatement();
		//      ResultSet rs = stmt.executeQuery(query);
		//
		//      boolean contains = false;
		//
		//      while(rs.next()){
		//        contains = true;
		//        break;
		//      }
		//
		//      conn.close();
		//      return contains;
		//    }
		//  }
		//
		//  public void createIndexIfUsingDBAndNotExists(){
		//    if(useDBForTokenPatterns){
		//      try {
		//        Redwood.log(Redwood.DBG, "Creating index for " + tableName);
		//        Connection conn = SQLConnection.getConnection();
		//        Statement stmt = conn.createStatement();
		//        boolean doesnotexist = false;
		//
		//        //check if the index already exists
		//        try{
		//          Statement stmt2 = conn.createStatement();
		//          String query = "SELECT '"+tableName+"_index'::regclass";
		//          stmt2.execute(query);
		//        }catch (SQLException e){
		//          doesnotexist = true;
		//        }
		//
		//        if(doesnotexist){
		//          String indexquery ="create index CONCURRENTLY " + tableName +"_index on " + tableName+ " using hash(\"sentid\") ";
		//          stmt.execute(indexquery);
		//          Redwood.log(Redwood.DBG, "Done creating index for " + tableName);
		//        }
		//      } catch (SQLException e) {
		//        throw new RuntimeException(e);
		//      }
		//    }
		//  }
		//
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
		//
		//  public void updatePatterns(Map<String, Map<Integer, Set<Integer>>> tempPatsForSents) {
		//    try {
		//      for(Map.Entry<String, Map<Integer, Set<Integer>>> en :tempPatsForSents.entrySet()){
		//        Map<Integer, Set<Integer>> m = getPatternsForAllTokens(en.getKey());
		//        if(m == null)
		//          m = new HashMap<Integer, Set<Integer>>();
		//        //m.putAll(en.getValue());
		//        tempPatsForSents.get(en.getKey()).putAll(m);
		//      }
		//      this.addPatterns(tempPatsForSents);
		//    } catch (IOException e) {
		//      e.printStackTrace();
		//    } catch (SQLException e) {
		//      e.printStackTrace();
		//    } catch (ClassNotFoundException e) {
		//      e.printStackTrace();
		//    }
		//  }
		//
		//  public boolean DBTableExists() {
		//    try {
		//      Connection conn = null;
		//
		//      conn = SQLConnection.getConnection();
		//
		//      DatabaseMetaData dbm = conn.getMetaData();
		//      ResultSet tables = dbm.getTables(null, null, tableName, null);
		//      if (tables.next()) {
		//        System.out.println("Found table " + tableName);
		//        conn.close();
		//        return true;
		//      }
		//      conn.close();
		//      return false;
		//    }catch(SQLException e){
		//      throw new RuntimeException(e);
		//
		//    }
		//  }
		//
		//  public ConcurrentHashIndex<SurfacePattern> readPatternIndexFromDB(){
		//    try{
		//      Connection conn = SQLConnection.getConnection();
		//      //Map<Integer, Set<Integer>> pats = new ConcurrentHashMap<Integer, Set<Integer>>();
		//      String query = "Select * from " + patternindicesTable + " where tablename=\'" + tableName + "\'";
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
		//  public void savePatternIndexInDB(ConcurrentHashIndex<SurfacePattern> index) {
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
		//
		//      st.execute();
		//      st.close();
		//      conn.close();
		//    }catch (SQLException e){
		//      throw new RuntimeException(e);
		//    } catch (IOException e) {
		//      throw new RuntimeException(e);
		//    }
		//  }
	}
}
