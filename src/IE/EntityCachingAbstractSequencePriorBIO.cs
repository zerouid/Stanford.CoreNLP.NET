using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.IE
{
	/// <summary>
	/// This class keeps track of all labeled entities and updates the
	/// its list whenever the label at a point gets changed.
	/// </summary>
	/// <remarks>
	/// This class keeps track of all labeled entities and updates the
	/// its list whenever the label at a point gets changed.  This allows
	/// you to not have to regenerate the list every time, which can be quite
	/// inefficient.
	/// </remarks>
	/// <author>Mengqiu Wang</author>
	public abstract class EntityCachingAbstractSequencePriorBIO<In> : IListeningSequenceModel
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.EntityCachingAbstractSequencePriorBIO));

		protected internal int[] sequence;

		protected internal readonly int backgroundSymbol;

		protected internal readonly int numClasses;

		protected internal readonly int[] possibleValues;

		protected internal readonly IIndex<string> classIndex;

		protected internal readonly IIndex<string> tagIndex;

		private readonly IList<string> wordDoc;

		public EntityCachingAbstractSequencePriorBIO(string backgroundSymbol, IIndex<string> classIndex, IIndex<string> tagIndex, IList<In> doc)
		{
			this.classIndex = classIndex;
			this.tagIndex = tagIndex;
			this.backgroundSymbol = classIndex.IndexOf(backgroundSymbol);
			this.numClasses = classIndex.Size();
			this.possibleValues = new int[numClasses];
			for (int i = 0; i < numClasses; i++)
			{
				possibleValues[i] = i;
			}
			this.wordDoc = new List<string>(doc.Count);
			foreach (IN w in doc)
			{
				wordDoc.Add(w.Get(typeof(CoreAnnotations.TextAnnotation)));
			}
		}

		private bool Verbose = false;

		internal EntityBIO[] entities;

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
			return wordDoc.Count;
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
			int oldVal = origClass;
			// if (BisequenceEmpiricalNERPrior.debugIndices.indexOf(position) != -1)
			//  EmpiricalNERPriorBIO.DEBUG = true;
			for (int label = 0; label < numClasses; label++)
			{
				if (label != origClass)
				{
					sequence[position] = label;
					UpdateSequenceElement(sequence, position, oldVal);
					probs[label] = ScoreOf(sequence);
					oldVal = label;
				}
			}
			// if (BisequenceEmpiricalNERPrior.debugIndices.indexOf(position) != -1)
			//   System.out.println(this);
			sequence[position] = origClass;
			UpdateSequenceElement(sequence, position, oldVal);
			probs[origClass] = ScoreOf(sequence);
			// EmpiricalNERPriorBIO.DEBUG = false;
			return probs;
		}

		public virtual void SetInitialSequence(int[] initialSequence)
		{
			this.sequence = initialSequence;
			entities = new EntityBIO[initialSequence.Length];
			// Arrays.fill(entities, null);  // not needed; Java arrays zero initialized
			for (int i = 0; i < initialSequence.Length; i++)
			{
				if (initialSequence[i] != backgroundSymbol)
				{
					string rawTag = classIndex.Get(sequence[i]);
					string[] parts = rawTag.Split("-");
					//TODO(mengqiu) this needs to be updated, so that initial can be I as well
					if (parts[0].Equals("B"))
					{
						// B-
						EntityBIO entity = ExtractEntity(initialSequence, i, parts[1]);
						AddEntityToEntitiesArray(entity);
						i += entity.words.Count - 1;
					}
				}
			}
		}

		private void AddEntityToEntitiesArray(EntityBIO entity)
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
		public virtual EntityBIO ExtractEntity(int[] sequence, int position, string tag)
		{
			EntityBIO entity = new EntityBIO();
			entity.type = tagIndex.IndexOf(tag);
			entity.startPosition = position;
			entity.words = new List<string>();
			entity.words.Add(wordDoc[position]);
			int pos = position + 1;
			for (; pos < sequence.Length; pos++)
			{
				string rawTag = classIndex.Get(sequence[pos]);
				string[] parts = rawTag.Split("-");
				if (parts[0].Equals("I") && parts[1].Equals(tag))
				{
					string word = wordDoc[pos];
					entity.words.Add(word);
				}
				else
				{
					break;
				}
			}
			entity.otherOccurrences = OtherOccurrences(entity);
			return entity;
		}

		/// <summary>
		/// finds other locations in the sequence where the sequence of
		/// words in this entity occurs.
		/// </summary>
		public virtual int[] OtherOccurrences(EntityBIO entity)
		{
			IList<int> other = new List<int>();
			for (int i = 0; i < wordDoc.Count; i++)
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

		public virtual bool Matches(EntityBIO entity, int position)
		{
			string word = wordDoc[position];
			if (Sharpen.Runtime.EqualsIgnoreCase(word, entity.words[0]))
			{
				for (int j = 1; j < entity.words.Count; j++)
				{
					if (position + j >= wordDoc.Count)
					{
						return false;
					}
					string nextWord = wordDoc[position + j];
					if (!Sharpen.Runtime.EqualsIgnoreCase(nextWord, entity.words[j]))
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}

		public virtual void UpdateSequenceElement(int[] sequence, int position, int oldVal)
		{
			this.sequence = sequence;
			if (sequence[position] == oldVal)
			{
				return;
			}
			if (Verbose)
			{
				log.Info("changing position " + position + " from " + classIndex.Get(oldVal) + " to " + classIndex.Get(sequence[position]));
			}
			if (sequence[position] == backgroundSymbol)
			{
				// new tag is O
				string oldRawTag = classIndex.Get(oldVal);
				string[] oldParts = oldRawTag.Split("-");
				if (oldParts[0].Equals("B"))
				{
					// old tag was a B, current entity definitely affected, also check next one
					EntityBIO entity = entities[position];
					if (entity == null)
					{
						throw new Exception("oldTag starts with B, entity at position should not be null");
					}
					// remove entities for all words affected by this entity
					for (int i = 0; i < entity.words.Count; i++)
					{
						entities[position + i] = null;
					}
				}
				else
				{
					// old tag was a I, check previous one
					if (entities[position] != null)
					{
						// this was part of an entity, shortened
						if (Verbose)
						{
							log.Info("splitting off prev entity");
						}
						EntityBIO oldEntity = entities[position];
						int oldLen = oldEntity.words.Count;
						int offset = position - oldEntity.startPosition;
						IList<string> newWords = new List<string>();
						for (int i = 0; i < offset; i++)
						{
							newWords.Add(oldEntity.words[i]);
						}
						oldEntity.words = newWords;
						oldEntity.otherOccurrences = OtherOccurrences(oldEntity);
						// need to clean any remaining entity
						for (int i_1 = 0; i_1 < oldLen - offset; i_1++)
						{
							entities[position + i_1] = null;
						}
						if (Verbose && position > 0)
						{
							log.Info("position:" + position + ", entities[position-1] = " + entities[position - 1].ToString(tagIndex));
						}
					}
				}
			}
			else
			{
				// otherwise, non-entity part I-xxx -> O, no enitty affected
				string rawTag = classIndex.Get(sequence[position]);
				string[] parts = rawTag.Split("-");
				if (parts[0].Equals("B"))
				{
					// new tag is B
					if (oldVal == backgroundSymbol)
					{
						// start a new entity, may merge with the next word
						EntityBIO entity = ExtractEntity(sequence, position, parts[1]);
						AddEntityToEntitiesArray(entity);
					}
					else
					{
						string oldRawTag = classIndex.Get(oldVal);
						string[] oldParts = oldRawTag.Split("-");
						if (oldParts[0].Equals("B"))
						{
							// was a different B-xxx
							EntityBIO oldEntity = entities[position];
							if (oldEntity.words.Count > 1)
							{
								// remove all old entity, add new singleton
								for (int i = 0; i < oldEntity.words.Count; i++)
								{
									entities[position + i] = null;
								}
								EntityBIO entity = ExtractEntity(sequence, position, parts[1]);
								AddEntityToEntitiesArray(entity);
							}
							else
							{
								// extract entity
								EntityBIO entity = ExtractEntity(sequence, position, parts[1]);
								AddEntityToEntitiesArray(entity);
							}
						}
						else
						{
							// was I
							EntityBIO oldEntity = entities[position];
							if (oldEntity != null)
							{
								// break old entity
								int oldLen = oldEntity.words.Count;
								int offset = position - oldEntity.startPosition;
								IList<string> newWords = new List<string>();
								for (int i = 0; i < offset; i++)
								{
									newWords.Add(oldEntity.words[i]);
								}
								oldEntity.words = newWords;
								oldEntity.otherOccurrences = OtherOccurrences(oldEntity);
								// need to clean any remaining entity
								for (int i_1 = 0; i_1 < oldLen - offset; i_1++)
								{
									entities[position + i_1] = null;
								}
							}
							EntityBIO entity = ExtractEntity(sequence, position, parts[1]);
							AddEntityToEntitiesArray(entity);
						}
					}
				}
				else
				{
					// new tag is I
					if (oldVal == backgroundSymbol)
					{
						// check if previous entity extends into this one
						if (position > 0)
						{
							if (entities[position - 1] != null)
							{
								string oldTag = tagIndex.Get(entities[position - 1].type);
								EntityBIO entity = ExtractEntity(sequence, position - 1 - entities[position - 1].words.Count + 1, oldTag);
								AddEntityToEntitiesArray(entity);
							}
						}
					}
					else
					{
						string oldRawTag = classIndex.Get(oldVal);
						string[] oldParts = oldRawTag.Split("-");
						if (oldParts[0].Equals("B"))
						{
							// was a B, clean the B entity first, then check if previous is an entity
							EntityBIO oldEntity = entities[position];
							for (int i = 0; i < oldEntity.words.Count; i++)
							{
								entities[position + i] = null;
							}
							if (position > 0)
							{
								if (entities[position - 1] != null)
								{
									string oldTag = tagIndex.Get(entities[position - 1].type);
									if (Verbose)
									{
										log.Info("position:" + position + ", entities[position-1] = " + entities[position - 1].ToString(tagIndex));
									}
									EntityBIO entity = ExtractEntity(sequence, position - 1 - entities[position - 1].words.Count + 1, oldTag);
									AddEntityToEntitiesArray(entity);
								}
							}
						}
						else
						{
							// was a differnt I-xxx,
							if (entities[position] != null)
							{
								// shorten the previous one, remove any additional parts
								EntityBIO oldEntity = entities[position];
								int oldLen = oldEntity.words.Count;
								int offset = position - oldEntity.startPosition;
								IList<string> newWords = new List<string>();
								for (int i = 0; i < offset; i++)
								{
									newWords.Add(oldEntity.words[i]);
								}
								oldEntity.words = newWords;
								oldEntity.otherOccurrences = OtherOccurrences(oldEntity);
								// need to clean any remaining entity
								for (int i_1 = 0; i_1 < oldLen - offset; i_1++)
								{
									entities[position + i_1] = null;
								}
							}
							else
							{
								// re-calc entity of the previous entity if exist
								if (position > 0)
								{
									if (entities[position - 1] != null)
									{
										string oldTag = tagIndex.Get(entities[position - 1].type);
										EntityBIO entity = ExtractEntity(sequence, position - 1 - entities[position - 1].words.Count + 1, oldTag);
										AddEntityToEntitiesArray(entity);
									}
								}
							}
						}
					}
				}
			}
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < entities.Length; i++)
			{
				sb.Append(i);
				sb.Append('\t');
				string word = wordDoc[i];
				sb.Append(word);
				sb.Append('\t');
				sb.Append(classIndex.Get(sequence[i]));
				if (entities[i] != null)
				{
					sb.Append('\t');
					sb.Append(entities[i].ToString(tagIndex));
				}
				sb.Append('\n');
			}
			return sb.ToString();
		}

		public virtual string ToString(int pos)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = System.Math.Max(0, pos - 3); i < System.Math.Min(entities.Length, pos + 3); i++)
			{
				sb.Append(i);
				sb.Append('\t');
				string word = wordDoc[i];
				sb.Append(word);
				sb.Append('\t');
				sb.Append(classIndex.Get(sequence[i]));
				if (entities[i] != null)
				{
					sb.Append('\t');
					sb.Append(entities[i].ToString(tagIndex));
				}
				sb.Append('\n');
			}
			return sb.ToString();
		}

		public abstract double ScoreOf(int[] arg1);
	}

	internal class EntityBIO
	{
		public int startPosition;

		public IList<string> words;

		public int type;

		/// <summary>
		/// the beginning index of other locations where this sequence of
		/// words appears.
		/// </summary>
		public int[] otherOccurrences;

		public virtual string ToString(IIndex<string> tagIndex)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append('"');
			sb.Append(StringUtils.Join(words, " "));
			sb.Append("\" start: ");
			sb.Append(startPosition);
			sb.Append(" type: ");
			sb.Append(tagIndex.Get(type));
			sb.Append(" other_occurrences: ");
			sb.Append(Arrays.ToString(otherOccurrences));
			return sb.ToString();
		}
	}
}
