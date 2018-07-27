using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	[System.Serializable]
	internal class RandomWalk
	{
		private const long serialVersionUID = -5284941866796561664L;

		private readonly IDictionary<object, ICounter> model = Generics.NewHashMap();

		private readonly IDictionary<object, ICounter> hiddenToSeen = Generics.NewHashMap();

		private readonly IDictionary<object, ICounter> seenToHidden = Generics.NewHashMap();

		private const double Lambda = 0.01;

		/// <summary>Uses the initialized values</summary>
		public virtual double Score(object hidden, object seen)
		{
			return model[hidden].GetCount(seen) / model[hidden].TotalCount();
		}

		/* score with flexible number of steps */
		public virtual double Score(object hidden, object seen, int steps)
		{
			double total = 0;
			for (int i = 0; i <= steps; i++)
			{
				total += Math.Pow(Lambda, steps) * Step(hidden, seen, steps);
			}
			return total;
		}

		/* returns probability of hidden -> seen with <code>steps</code>
		* random walk steps */
		public virtual double Step(object hidden, object seen, int steps)
		{
			if (steps < 1)
			{
				return hiddenToSeen[hidden].GetCount(seen) / hiddenToSeen[hidden].TotalCount();
			}
			else
			{
				double total = 0;
				foreach (object seen1 in seenToHidden.Keys)
				{
					foreach (object hidden1 in hiddenToSeen.Keys)
					{
						double subtotal = hiddenToSeen[hidden].GetCount(seen1) / hiddenToSeen[hidden].TotalCount() * (seenToHidden[seen1].GetCount(hidden1) / seenToHidden[seen1].TotalCount());
						subtotal += Score(hidden1, seen, steps - 1);
						total += subtotal;
					}
				}
				return total;
			}
		}

		public virtual void Train(ICollection<Pair<object, object>> data)
		{
			foreach (Pair p in data)
			{
				object seen = p.First();
				object hidden = p.Second();
				if (!hiddenToSeen.Keys.Contains(hidden))
				{
					hiddenToSeen[hidden] = new ClassicCounter();
				}
				hiddenToSeen[hidden].IncrementCount(seen);
				if (!seenToHidden.Keys.Contains(seen))
				{
					seenToHidden[seen] = new ClassicCounter();
				}
				seenToHidden[seen].IncrementCount(hidden);
			}
		}

		/// <summary>builds a random walk model with n steps.</summary>
		/// <param name="data">A collection of seen/hidden event <code>Pair</code>s</param>
		public RandomWalk(ICollection<Pair<object, object>> data, int steps)
		{
			Train(data);
			foreach (object seen in seenToHidden.Keys)
			{
				if (!model.Contains(seen))
				{
					model[seen] = new ClassicCounter();
				}
				foreach (object hidden in hiddenToSeen.Keys)
				{
					model[seen].SetCount(hidden, Score(seen, hidden, steps));
				}
			}
		}
	}
}
