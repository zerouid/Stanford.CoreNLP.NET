using System;
using System.Collections.Generic;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>A filter for selecting which channels are visible.</summary>
	/// <remarks>
	/// A filter for selecting which channels are visible. This class
	/// behaves as an "or" filter; that is, if any of the filters are considered
	/// valid, it allows the Record to proceed to the next handler.
	/// </remarks>
	/// <author>Gabor Angeli (angeli at cs.stanford)</author>
	public class VisibilityHandler : LogRecordHandler
	{
		private enum State
		{
			ShowAll,
			HideAll
		}

		private VisibilityHandler.State defaultState = VisibilityHandler.State.ShowAll;

		private readonly ICollection<object> deltaPool = new HashSet<object>();

		public VisibilityHandler()
		{
		}

		public VisibilityHandler(object[] channels)
		{
			// replacing with Generics.newHashSet() makes classloader go haywire?
			// default is SHOW_ALL
			if (channels.Length > 0)
			{
				defaultState = VisibilityHandler.State.HideAll;
				Java.Util.Collections.AddAll(deltaPool, channels);
			}
		}

		/// <summary>Show all of the channels.</summary>
		public virtual void ShowAll()
		{
			this.defaultState = VisibilityHandler.State.ShowAll;
			this.deltaPool.Clear();
		}

		/// <summary>Show none of the channels</summary>
		public virtual void HideAll()
		{
			this.defaultState = VisibilityHandler.State.HideAll;
			this.deltaPool.Clear();
		}

		/// <summary>
		/// Show all the channels currently being printed, in addition
		/// to a new one
		/// </summary>
		/// <param name="filter">The channel to also show</param>
		/// <returns>true if this channel was already being shown.</returns>
		public virtual bool AlsoShow(object filter)
		{
			switch (this.defaultState)
			{
				case VisibilityHandler.State.HideAll:
				{
					return this.deltaPool.Add(filter);
				}

				case VisibilityHandler.State.ShowAll:
				{
					return this.deltaPool.Remove(filter);
				}

				default:
				{
					throw new InvalidOperationException("Unknown default state setting: " + this.defaultState);
				}
			}
		}

		/// <summary>
		/// Show all the channels currently being printed, with the exception
		/// of this new one
		/// </summary>
		/// <param name="filter">The channel to also hide</param>
		/// <returns>true if this channel was already being hidden.</returns>
		public virtual bool AlsoHide(object filter)
		{
			switch (this.defaultState)
			{
				case VisibilityHandler.State.HideAll:
				{
					return this.deltaPool.Remove(filter);
				}

				case VisibilityHandler.State.ShowAll:
				{
					return this.deltaPool.Add(filter);
				}

				default:
				{
					throw new InvalidOperationException("Unknown default state setting: " + this.defaultState);
				}
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IList<Redwood.Record> Handle(Redwood.Record record)
		{
			bool isPrinting = false;
			if (record.Force())
			{
				//--Case: Force Printing
				isPrinting = true;
			}
			else
			{
				switch (this.defaultState)
				{
					case VisibilityHandler.State.HideAll:
					{
						//--Case: Filter
						//--Default False
						foreach (object tag in record.Channels())
						{
							if (this.deltaPool.Contains(tag))
							{
								isPrinting = true;
								break;
							}
						}
						break;
					}

					case VisibilityHandler.State.ShowAll:
					{
						//--Default True
						if (!this.deltaPool.IsEmpty())
						{
							// Short-circuit for efficiency
							bool somethingSeen = false;
							foreach (object tag_1 in record.Channels())
							{
								if (this.deltaPool.Contains(tag_1))
								{
									somethingSeen = true;
									break;
								}
							}
							isPrinting = !somethingSeen;
						}
						else
						{
							isPrinting = true;
						}
						break;
					}

					default:
					{
						throw new InvalidOperationException("Unknown default state setting: " + this.defaultState);
					}
				}
			}
			//--Return
			if (isPrinting)
			{
				return Java.Util.Collections.SingletonList(record);
			}
			else
			{
				return Empty;
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IList<Redwood.Record> SignalStartTrack(Redwood.Record signal)
		{
			return Empty;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IList<Redwood.Record> SignalEndTrack(int newDepth, long timeOfEnd)
		{
			return Empty;
		}
	}
}
