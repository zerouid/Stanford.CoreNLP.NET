using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Pred
{
	/// <summary>This manages the set of available custom Node and Edge tests.</summary>
	/// <remarks>
	/// This manages the set of available custom Node and Edge tests.
	/// This is a singleton, so use <code>inst</code> to
	/// get the current instance.
	/// </remarks>
	/// <author>Eric Yeh</author>
	public class SsurgTestManager
	{
		internal IDictionary<string, Type> nodeTests = Generics.NewHashMap();

		private SsurgTestManager()
		{
			Init();
		}

		private static Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Pred.SsurgTestManager instance = null;

		/// <summary>Once initialized, registers self with default node handlers.</summary>
		private void Init()
		{
		}

		//  
		public static Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Pred.SsurgTestManager Inst()
		{
			if (instance == null)
			{
				instance = new Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Pred.SsurgTestManager();
			}
			return instance;
		}

		public virtual void RegisterNodeTest(NodeTest nodeTestObj)
		{
			nodeTests[nodeTestObj.GetID()] = nodeTestObj.GetType();
		}

		/// <summary>
		/// Given the id of the test, and the match name argument, returns a new instance
		/// of the given NodeTest, otherwise throws an exception if not available.
		/// </summary>
		/// <exception cref="System.Exception"/>
		public virtual NodeTest GetNodeTest(string id, string matchName)
		{
			NodeTest test = (NodeTest)nodeTests[id].GetConstructor(typeof(string)).NewInstance(matchName);
			return test;
		}
	}
}
