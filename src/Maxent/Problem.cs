using System;
using Edu.Stanford.Nlp.IO;


namespace Edu.Stanford.Nlp.Maxent
{
	/// <summary>This is a general class for a Problem to be solved by the MaxEnt toolkit.</summary>
	/// <remarks>
	/// This is a general class for a Problem to be solved by the MaxEnt toolkit.
	/// There have to be experiments and features.
	/// </remarks>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class Problem
	{
		public int exSize;

		public int fSize;

		/// <summary>This is the training data.</summary>
		public Experiments data;

		/// <summary>These are the features.</summary>
		public Features functions;

		public Problem(Experiments d, Features f)
		{
			data = d;
			functions = f;
			exSize = d.Size();
			fSize = f.Size();
		}

		public Problem()
		{
		}

		public virtual void Add(Feature f)
		{
			functions.Add(f);
			fSize++;
		}

		public virtual void RemoveLast()
		{
			functions.RemoveLast();
			fSize--;
		}

		public virtual void Print()
		{
			System.Console.Out.WriteLine(" Problem printing ");
			data.Print();
			System.Console.Out.WriteLine(" Function printing ");
			for (int i = 0; i < fSize; i++)
			{
				functions.Get(i).Print();
			}
		}

		public virtual void Print(string filename)
		{
			try
			{
				PrintFile pf = new PrintFile(filename);
				pf.WriteLine(" Problem printing ");
				data.Print(pf);
				pf.WriteLine(" Function printing ");
				for (int i = 0; i < fSize; i++)
				{
					functions.Get(i).Print(pf);
				}
			}
			catch (Exception)
			{
				System.Console.Out.WriteLine("Exception in Problem.print()");
			}
		}
		/*
		// This is broken... it's not clear what it's supposed to do, but
		// class Experiments requires a "vArray" to function correctly.
		// Otherwise you just can't run ptilde on it.  If that makes sense
		// to you, please do everyone a favor and fix this test program or
		// at least document what those fields mean.  -horatio
		public static void main(String[] args) {
		double[] f1 = {0, 1, 1, 0, 1, 1};
		double[] f2 = {1, 0, 1, 1, 0, 1};
		Experiments gophers = new Experiments();
		for (int i = 0; i < 3; i++) {
		gophers.add(new Experiments());
		}
		for (int i = 0; i < 3; i++) {
		gophers.add(new Experiments());
		}
		gophers.ptilde();
		Index<IntPair> instanceIndex = gophers.createIndex();
		Features feats = new Features();
		feats.add(new Feature(gophers, f1, instanceIndex));
		feats.add(new Feature(gophers, f2, instanceIndex));
		Problem p = new Problem(gophers, feats);
		System.out.println(p.exSize);
		System.out.println(p.functions.get(1).ftilde());
		}
		*/
	}
}
