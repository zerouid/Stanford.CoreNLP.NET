using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.IE
{
	/// <summary>
	/// This class keeps track of all labeled entities and updates
	/// its list whenever the label at a point gets changed.
	/// </summary>
	/// <remarks>
	/// This class keeps track of all labeled entities and updates
	/// its list whenever the label at a point gets changed.  This allows
	/// you to not have to regenerate the list every time, which can be quite
	/// inefficient.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	public abstract class EntityCachingAbstractSequencePrior<In> : IListeningSequenceModel
		where In : ICoreMap
	{
		protected internal int[] sequence;

		protected internal readonly int backgroundSymbol;

		protected internal readonly int numClasses;

		protected internal readonly int[] possibleValues;

		protected internal readonly IIndex<string> classIndex;

		protected internal readonly IList<In> doc;

		public EntityCachingAbstractSequencePrior(string backgroundSymbol, IIndex<string> classIndex, IList<In> doc)
		{
			this.classIndex = classIndex;
			this.backgroundSymbol = classIndex.IndexOf(backgroundSymbol);
			this.numClasses = classIndex.Size();
			this.possibleValues = new int[numClasses];
			for (int i = 0; i < numClasses; i++)
			{
				possibleValues[i] = i;
			}
			this.doc = doc;
		}

		private bool Verbose = false;

		internal Entity[] entities;

		public virtual int LeftWindow()
		{
			return int.MaxValue;
		}

		// not Markovian!
		public virtual int RightWindow()
		{
			return int.MaxValue;
		}

		// not Markovian!
		public virtual int[] GetPossibleValues(int position)
		{
			return possibleValues;
		}

		public virtual double ScoreOf(int[] sequence, int pos)
		{
			return ScoresOf(sequence, pos)[sequence[pos]];
		}

		/// <returns>the length of the sequence</returns>
		public virtual int Length()
		{
			return doc.Count;
		}

		/// <summary>get the number of classes in the sequence model.</summary>
		public virtual int GetNumClasses()
		{
			return classIndex.Size();
		}

		public virtual double[] GetConditionalDistribution(int[] sequence, int position)
		{
			double[] probs = ScoresOf(sequence, position);
			ArrayMath.LogNormalize(probs);
			probs = ArrayMath.Exp(probs);
			//System.out.println(this);
			return probs;
		}

		public virtual double[] ScoresOf(int[] sequence, int position)
		{
			double[] probs = new double[numClasses];
			int origClass = sequence[position];
			for (int label = 0; label < numClasses; label++)
			{
				sequence[position] = label;
				UpdateSequenceElement(sequence, position, 0);
				probs[label] = ScoreOf(sequence);
			}
			sequence[position] = origClass;
			//System.out.println(this);
			return probs;
		}

		public virtual void SetInitialSequence(int[] initialSequence)
		{
			this.sequence = initialSequence;
			entities = new Entity[initialSequence.Length];
			// Arrays.fill(entities, null); // not needed; Java arrays zero initialized
			for (int i = 0; i < initialSequence.Length; i++)
			{
				if (initialSequence[i] != backgroundSymbol)
				{
					Entity entity = ExtractEntity(initialSequence, i);
					AddEntityToEntitiesArray(entity);
					i += entity.words.Count - 1;
				}
			}
		}

		private void AddEntityToEntitiesArray(Entity entity)
		{
			for (int j = entity.startPosition; j < entity.startPosition + entity.words.Count; j++)
			{
				entities[j] = entity;
			}
		}

		/// <summary>
		/// extracts the entity starting at the given position
		/// and adds it to the entity list.
		/// </summary>
		/// <remarks>
		/// extracts the entity starting at the given position
		/// and adds it to the entity list.  returns the index
		/// of the last element in the entity (<b>not</b> index+1)
		/// </remarks>
		public virtual Entity ExtractEntity(int[] sequence, int position)
		{
			Entity entity = new Entity();
			entity.type = sequence[position];
			entity.startPosition = position;
			entity.words = new List<string>();
			for (; position < sequence.Length; position++)
			{
				if (sequence[position] == entity.type)
				{
					string word = doc[position].Get(typeof(CoreAnnotations.TextAnnotation));
					entity.words.Add(word);
					if (position == sequence.Length - 1)
					{
						entity.otherOccurrences = OtherOccurrences(entity);
					}
				}
				else
				{
					entity.otherOccurrences = OtherOccurrences(entity);
					break;
				}
			}
			return entity;
		}

		/// <summary>
		/// finds other locations in the sequence where the sequence of
		/// words in this entity occurs.
		/// </summary>
		public virtual int[] OtherOccurrences(Entity entity)
		{
			IList<int> other = new List<int>();
			for (int i = 0; i < doc.Count; i++)
			{
				if (i == entity.startPosition)
				{
					continue;
				}
				if (Matches(entity, i))
				{
					other.Add(int.Parse(i));
				}
			}
			return ToArray(other);
		}

		public static int[] ToArray(IList<int> list)
		{
			int[] arr = new int[list.Count];
			for (int i = 0; i < arr.Length; i++)
			{
				arr[i] = list[i];
			}
			return arr;
		}

		public virtual bool Matches(Entity entity, int position)
		{
			string word = doc[position].Get(typeof(CoreAnnotations.TextAnnotation));
			if (Sharpen.Runtime.EqualsIgnoreCase(word, entity.words[0]))
			{
				//boolean matches = true;
				for (int j = 1; j < entity.words.Count; j++)
				{
					if (position + j >= doc.Count)
					{
						return false;
					}
					string nextWord = doc[position + j].Get(typeof(CoreAnnotations.TextAnnotation));
					if (!Sharpen.Runtime.EqualsIgnoreCase(nextWord, entity.words[j]))
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}

		public virtual bool JoiningTwoEntities(int[] sequence, int position)
		{
			if (sequence[position] == backgroundSymbol)
			{
				return false;
			}
			if (position > 0 && position < sequence.Length - 1)
			{
				return (sequence[position] == sequence[position - 1] && sequence[position] == sequence[position + 1]);
			}
			return false;
		}

		public virtual bool SplittingTwoEntities(int[] sequence, int position)
		{
			if (position > 0 && position < sequence.Length - 1)
			{
				return (entities[position - 1] == entities[position + 1] && entities[position - 1] != null);
			}
			return false;
		}

		public virtual bool AppendingEntity(int[] sequence, int position)
		{
			if (position > 0)
			{
				if (entities[position - 1] == null)
				{
					return false;
				}
				Entity prev = entities[position - 1];
				return (sequence[position] == sequence[position - 1] && prev.startPosition + prev.words.Count == position);
			}
			return false;
		}

		public virtual bool PrependingEntity(int[] sequence, int position)
		{
			if (position < sequence.Length - 1)
			{
				if (entities[position + 1] == null)
				{
					return false;
				}
				return (sequence[position] == sequence[position + 1]);
			}
			return false;
		}

		public virtual bool AddingSingletonEntity(int[] sequence, int position)
		{
			if (sequence[position] == backgroundSymbol)
			{
				return false;
			}
			if (position > 0)
			{
				if (sequence[position - 1] == sequence[position])
				{
					return false;
				}
			}
			if (position < sequence.Length - 1)
			{
				if (sequence[position + 1] == sequence[position])
				{
					return false;
				}
			}
			return true;
		}

		public virtual bool RemovingEndOfEntity(int[] sequence, int position)
		{
			if (position > 0)
			{
				if (sequence[position - 1] == backgroundSymbol)
				{
					return false;
				}
				Entity prev = entities[position - 1];
				if (prev != null)
				{
					return (prev.startPosition + prev.words.Count > position);
				}
			}
			return false;
		}

		public virtual bool RemovingBeginningOfEntity(int[] sequence, int position)
		{
			if (position < sequence.Length - 1)
			{
				if (sequence[position + 1] == backgroundSymbol)
				{
					return false;
				}
				Entity next = entities[position + 1];
				if (next != null)
				{
					return (next.startPosition <= position);
				}
			}
			return false;
		}

		public virtual bool NoChange(int[] sequence, int position)
		{
			if (position > 0)
			{
				if (sequence[position - 1] == sequence[position])
				{
					return entities[position - 1] == entities[position];
				}
			}
			if (position < sequence.Length - 1)
			{
				if (sequence[position + 1] == sequence[position])
				{
					return entities[position] == entities[position + 1];
				}
			}
			// actually, can't tell.  either no change, or singleton
			// changed type
			return false;
		}

		public virtual void UpdateSequenceElement(int[] sequence, int position, int oldVal)
		{
			if (Verbose)
			{
				System.Console.Out.WriteLine("changing position " + position + " from " + classIndex.Get(oldVal) + " to " + classIndex.Get(sequence[position]));
			}
			this.sequence = sequence;
			// no change?
			if (NoChange(sequence, position))
			{
				if (Verbose)
				{
					System.Console.Out.WriteLine("no change");
				}
				if (Verbose)
				{
					System.Console.Out.WriteLine(this);
				}
				return;
			}
			else
			{
				// are we joining 2 entities?
				if (JoiningTwoEntities(sequence, position))
				{
					if (Verbose)
					{
						System.Console.Out.WriteLine("joining 2 entities");
					}
					Entity newEntity = new Entity();
					Entity prev = entities[position - 1];
					Entity next = entities[position + 1];
					newEntity.startPosition = prev.startPosition;
					newEntity.words = new List<string>();
					Sharpen.Collections.AddAll(newEntity.words, prev.words);
					string word = doc[position].Get(typeof(CoreAnnotations.TextAnnotation));
					newEntity.words.Add(word);
					Sharpen.Collections.AddAll(newEntity.words, next.words);
					newEntity.type = sequence[position];
					IList<int> other = new List<int>();
					for (int i = 0; i < prev.otherOccurrences.Length; i++)
					{
						int pos = prev.otherOccurrences[i];
						if (Matches(newEntity, pos))
						{
							other.Add(int.Parse(pos));
						}
					}
					newEntity.otherOccurrences = ToArray(other);
					AddEntityToEntitiesArray(newEntity);
					if (Verbose)
					{
						System.Console.Out.WriteLine(this);
					}
					return;
				}
				else
				{
					// are we splitting up an entity?
					if (SplittingTwoEntities(sequence, position))
					{
						if (Verbose)
						{
							System.Console.Out.WriteLine("splitting into 2 entities");
						}
						Entity entity = entities[position];
						Entity prev = new Entity();
						prev.type = entity.type;
						prev.startPosition = entity.startPosition;
						prev.words = new List<string>(entity.words.SubList(0, position - entity.startPosition));
						prev.otherOccurrences = OtherOccurrences(prev);
						AddEntityToEntitiesArray(prev);
						Entity next = new Entity();
						next.type = entity.type;
						next.startPosition = position + 1;
						next.words = new List<string>(entity.words.SubList(position - entity.startPosition + 1, entity.words.Count));
						next.otherOccurrences = OtherOccurrences(next);
						AddEntityToEntitiesArray(next);
						if (sequence[position] == backgroundSymbol)
						{
							entities[position] = null;
						}
						else
						{
							Entity newEntity = new Entity();
							newEntity.startPosition = position;
							newEntity.type = sequence[position];
							newEntity.words = new List<string>();
							string word = doc[position].Get(typeof(CoreAnnotations.TextAnnotation));
							newEntity.words.Add(word);
							newEntity.otherOccurrences = OtherOccurrences(newEntity);
							entities[position] = newEntity;
						}
						if (Verbose)
						{
							System.Console.Out.WriteLine(this);
						}
						return;
					}
					else
					{
						// are we prepending to an entity ?
						if (PrependingEntity(sequence, position))
						{
							if (Verbose)
							{
								System.Console.Out.WriteLine("prepending entity");
							}
							Entity newEntity = new Entity();
							Entity next = entities[position + 1];
							newEntity.startPosition = position;
							newEntity.words = new List<string>();
							string word = doc[position].Get(typeof(CoreAnnotations.TextAnnotation));
							newEntity.words.Add(word);
							Sharpen.Collections.AddAll(newEntity.words, next.words);
							newEntity.type = sequence[position];
							//List<Integer> other = new ArrayList<Integer>();
							newEntity.otherOccurrences = OtherOccurrences(newEntity);
							AddEntityToEntitiesArray(newEntity);
							if (RemovingEndOfEntity(sequence, position))
							{
								if (Verbose)
								{
									System.Console.Out.WriteLine(" ... and removing end of previous entity.");
								}
								Entity prev = entities[position - 1];
								prev.words.Remove(prev.words.Count - 1);
								prev.otherOccurrences = OtherOccurrences(prev);
							}
							if (Verbose)
							{
								System.Console.Out.WriteLine(this);
							}
							return;
						}
						else
						{
							// are we appending to an entity ?
							if (AppendingEntity(sequence, position))
							{
								if (Verbose)
								{
									System.Console.Out.WriteLine("appending entity");
								}
								Entity newEntity = new Entity();
								Entity prev = entities[position - 1];
								newEntity.startPosition = prev.startPosition;
								newEntity.words = new List<string>();
								Sharpen.Collections.AddAll(newEntity.words, prev.words);
								string word = doc[position].Get(typeof(CoreAnnotations.TextAnnotation));
								newEntity.words.Add(word);
								newEntity.type = sequence[position];
								IList<int> other = new List<int>();
								for (int i = 0; i < prev.otherOccurrences.Length; i++)
								{
									int pos = prev.otherOccurrences[i];
									if (Matches(newEntity, pos))
									{
										other.Add(int.Parse(pos));
									}
								}
								newEntity.otherOccurrences = ToArray(other);
								AddEntityToEntitiesArray(newEntity);
								if (RemovingBeginningOfEntity(sequence, position))
								{
									if (Verbose)
									{
										System.Console.Out.WriteLine(" ... and removing beginning of next entity.");
									}
									entities[position + 1].words.Remove(0);
									entities[position + 1].startPosition++;
								}
								if (Verbose)
								{
									System.Console.Out.WriteLine(this);
								}
								return;
							}
							else
							{
								// adding new singleton entity
								if (AddingSingletonEntity(sequence, position))
								{
									Entity newEntity = new Entity();
									if (Verbose)
									{
										System.Console.Out.WriteLine("adding singleton entity");
									}
									newEntity.startPosition = position;
									newEntity.words = new List<string>();
									string word = doc[position].Get(typeof(CoreAnnotations.TextAnnotation));
									newEntity.words.Add(word);
									newEntity.type = sequence[position];
									newEntity.otherOccurrences = OtherOccurrences(newEntity);
									AddEntityToEntitiesArray(newEntity);
									if (RemovingEndOfEntity(sequence, position))
									{
										if (Verbose)
										{
											System.Console.Out.WriteLine(" ... and removing end of previous entity.");
										}
										Entity prev = entities[position - 1];
										prev.words.Remove(prev.words.Count - 1);
										prev.otherOccurrences = OtherOccurrences(prev);
									}
									if (RemovingBeginningOfEntity(sequence, position))
									{
										if (Verbose)
										{
											System.Console.Out.WriteLine(" ... and removing beginning of next entity.");
										}
										entities[position + 1].words.Remove(0);
										entities[position + 1].startPosition++;
									}
									if (Verbose)
									{
										System.Console.Out.WriteLine(this);
									}
									return;
								}
								else
								{
									// are splitting off the prev entity?
									if (RemovingEndOfEntity(sequence, position))
									{
										if (Verbose)
										{
											System.Console.Out.WriteLine("splitting off prev entity");
										}
										Entity prev = entities[position - 1];
										prev.words.Remove(prev.words.Count - 1);
										prev.otherOccurrences = OtherOccurrences(prev);
										entities[position] = null;
									}
									else
									{
										// are we splitting off the next entity?
										if (RemovingBeginningOfEntity(sequence, position))
										{
											if (Verbose)
											{
												System.Console.Out.WriteLine("splitting off next entity");
											}
											Entity next = entities[position + 1];
											next.words.Remove(0);
											next.startPosition++;
											next.otherOccurrences = OtherOccurrences(next);
											entities[position] = null;
										}
										else
										{
											entities[position] = null;
										}
									}
								}
							}
						}
					}
				}
			}
			if (Verbose)
			{
				System.Console.Out.WriteLine(this);
			}
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < entities.Length; i++)
			{
				sb.Append(i);
				sb.Append("\t");
				string word = doc[i].Get(typeof(CoreAnnotations.TextAnnotation));
				sb.Append(word);
				sb.Append("\t");
				sb.Append(classIndex.Get(sequence[i]));
				if (entities[i] != null)
				{
					sb.Append("\t");
					sb.Append(entities[i].ToString(classIndex));
				}
				sb.Append("\n");
			}
			return sb.ToString();
		}

		public virtual string ToString(int pos)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = System.Math.Max(0, pos - 10); i < System.Math.Min(entities.Length, pos + 10); i++)
			{
				sb.Append(i);
				sb.Append("\t");
				string word = doc[i].Get(typeof(CoreAnnotations.TextAnnotation));
				sb.Append(word);
				sb.Append("\t");
				sb.Append(classIndex.Get(sequence[i]));
				if (entities[i] != null)
				{
					sb.Append("\t");
					sb.Append(entities[i].ToString(classIndex));
				}
				sb.Append("\n");
			}
			return sb.ToString();
		}

		public abstract double ScoreOf(int[] arg1);
	}

	internal class Entity
	{
		public int startPosition;

		public IList<string> words;

		public int type;

		/// <summary>
		/// the beginning index of other locations where this sequence of
		/// words appears.
		/// </summary>
		public int[] otherOccurrences;

		public virtual string ToString(IIndex<string> classIndex)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("\"");
			sb.Append(StringUtils.Join(words, " "));
			sb.Append("\" start: ");
			sb.Append(startPosition);
			sb.Append(" type: ");
			sb.Append(classIndex.Get(type));
			sb.Append(" other_occurrences: ");
			sb.Append(Arrays.ToString(otherOccurrences));
			return sb.ToString();
		}
	}
}
