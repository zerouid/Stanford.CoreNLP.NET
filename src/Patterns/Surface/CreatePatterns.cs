using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Java.Util.Concurrent;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns.Surface
{
	public class CreatePatterns<E>
	{
		internal ConstantsAndVariables constVars;

		/// <exception cref="System.IO.IOException"/>
		public CreatePatterns(Properties props, ConstantsAndVariables constVars)
		{
			//String channelNameLogger = "createpatterns";
			this.constVars = constVars;
			ArgumentParser.FillOptions(typeof(ConstantsAndVariables), props);
			constVars.SetUp(props);
			SetUp(props);
		}

		internal virtual void SetUp(Properties props)
		{
			ArgumentParser.FillOptions(this, props);
		}

		/// <summary>creates all patterns and saves them in the correct PatternsForEachToken* class appropriately</summary>
		/// <param name="sents"/>
		/// <param name="props"/>
		/// <param name="storePatsForEachTokenWay"/>
		public virtual void GetAllPatterns(IDictionary<string, DataInstance> sents, Properties props, ConstantsAndVariables.PatternForEachTokenWay storePatsForEachTokenWay)
		{
			//    this.patternsForEachToken = new HashMap<String, Map<Integer, Triple<Set<Integer>, Set<Integer>, Set<Integer>>>>();
			// this.patternsForEachToken = new HashMap<String, Map<Integer, Set<Integer>>>();
			DateTime startDate = new DateTime();
			IList<string> keyset = new List<string>(sents.Keys);
			int num;
			if (constVars.numThreads == 1)
			{
				num = keyset.Count;
			}
			else
			{
				num = keyset.Count / (constVars.numThreads);
			}
			IExecutorService executor = Executors.NewFixedThreadPool(constVars.numThreads);
			Redwood.Log(ConstantsAndVariables.extremedebug, "Computing all patterns. keyset size is " + keyset.Count + ". Assigning " + num + " values to each thread");
			IList<IFuture<bool>> list = new List<IFuture<bool>>();
			for (int i = 0; i < constVars.numThreads; i++)
			{
				int from = i * num;
				int to = -1;
				if (i == constVars.numThreads - 1)
				{
					to = keyset.Count;
				}
				else
				{
					to = Math.Min(keyset.Count, (i + 1) * num);
				}
				//
				//      Redwood.log(ConstantsAndVariables.extremedebug, "assigning from " + i * num
				//          + " till " + Math.min(keyset.size(), (i + 1) * num));
				IList<string> ids = keyset.SubList(from, to);
				ICallable<bool> task = new CreatePatterns.CreatePatternsThread(this, sents, ids, props, storePatsForEachTokenWay);
				IFuture<bool> submit = executor.Submit(task);
				list.Add(submit);
			}
			// Now retrieve the result
			foreach (IFuture<bool> future in list)
			{
				try
				{
					future.Get();
				}
				catch (Exception e)
				{
					//patternsForEachToken.putAll(future.get());
					executor.ShutdownNow();
					throw new Exception(e);
				}
			}
			executor.Shutdown();
			DateTime endDate = new DateTime();
			string timeTaken = GetPatternsFromDataMultiClass.ElapsedTime(startDate, endDate);
			Redwood.Log(Redwood.Dbg, "Done computing all patterns [" + timeTaken + "]");
		}

		public class CreatePatternsThread : ICallable<bool>
		{
			internal IDictionary<string, DataInstance> sents;

			internal IList<string> sentIds;

			internal PatternsForEachToken<E> patsForEach;

			public CreatePatternsThread(CreatePatterns<E> _enclosing, IDictionary<string, DataInstance> sents, IList<string> sentIds, Properties props, ConstantsAndVariables.PatternForEachTokenWay storePatsForEachToken)
			{
				this._enclosing = _enclosing;
				//return patternsForEachToken;
				//  /**
				//   * Returns null if using DB backed!!
				//   * @return
				//   */
				//  public Map<String, Map<Integer, Set<Integer>>> getPatternsForEachToken() {
				//    return patternsForEachToken;
				//  }
				//String label;
				// Class otherClass;
				//this.label = label;
				// this.otherClass = otherClass;
				this.sents = sents;
				this.sentIds = sentIds;
				this.patsForEach = PatternsForEachToken.GetPatternsInstance(props, storePatsForEachToken);
			}

			/// <exception cref="System.Exception"/>
			public virtual bool Call()
			{
				IDictionary<string, IDictionary<int, ICollection<E>>> tempPatternsForTokens = new Dictionary<string, IDictionary<int, ICollection<E>>>();
				int numSentencesInOneCommit = 0;
				foreach (string id in this.sentIds)
				{
					DataInstance sent = this.sents[id];
					if (!this._enclosing.constVars.storePatsForEachToken.Equals(ConstantsAndVariables.PatternForEachTokenWay.Memory))
					{
						tempPatternsForTokens[id] = new Dictionary<int, ICollection<E>>();
					}
					IDictionary<int, ICollection<E>> p = (IDictionary)PatternFactory.GetPatternsAroundTokens(this._enclosing.constVars.patternType, sent, ConstantsAndVariables.GetStopWords());
					//to save number of commits to the database
					if (!this._enclosing.constVars.storePatsForEachToken.Equals(ConstantsAndVariables.PatternForEachTokenWay.Memory))
					{
						tempPatternsForTokens[id] = p;
						numSentencesInOneCommit++;
						if (numSentencesInOneCommit % 1000 == 0)
						{
							this.patsForEach.AddPatterns(tempPatternsForTokens);
							tempPatternsForTokens.Clear();
							numSentencesInOneCommit = 0;
						}
					}
					else
					{
						//          patsForEach.addPatterns(id, p);
						this.patsForEach.AddPatterns(id, p);
					}
				}
				//For the remaining sentences
				if (!this._enclosing.constVars.storePatsForEachToken.Equals(ConstantsAndVariables.PatternForEachTokenWay.Memory))
				{
					this.patsForEach.AddPatterns(tempPatternsForTokens);
				}
				return true;
			}

			private readonly CreatePatterns<E> _enclosing;
		}
	}
}
