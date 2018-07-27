using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>
	/// An abstract handler incorporating the logic of outputting a log message,
	/// to some source.
	/// </summary>
	/// <remarks>
	/// An abstract handler incorporating the logic of outputting a log message,
	/// to some source. This class is responsible for printing channel information,
	/// formatting tracks, and writing the actual log messages.
	/// Classes overriding this class should implement the print() method based
	/// on their output source.
	/// </remarks>
	/// <author>Gabor Angeli (angeli at cs.stanford)</author>
	public abstract class OutputHandler : LogRecordHandler
	{
		/// <summary>
		/// A list of tracks which have been started but not yet printed as no
		/// log messages are in them yet.
		/// </summary>
		protected internal LinkedList<Redwood.Record> queuedTracks = new LinkedList<Redwood.Record>();

		/// <summary>Information about the current and higher level tracks</summary>
		protected internal Stack<OutputHandler.TrackInfo> trackStack = new Stack<OutputHandler.TrackInfo>();

		/// <summary>The current track info; used to avoid trackStack.peek() calls</summary>
		protected internal OutputHandler.TrackInfo info;

		/// <summary>The tab character</summary>
		protected internal string tab = "  ";

		/// <summary>Character used to join multiple channel names</summary>
		protected internal char channelSeparatorChar = ' ';

		/// <summary>The length of the left margin in which to print channel information.</summary>
		/// <remarks>
		/// The length of the left margin in which to print channel information.
		/// If this is set to a value &lt; 3, then no channel information is printed.
		/// </remarks>
		protected internal int leftMargin = 0;

		/// <summary>
		/// Number of lines above which the closing brace of a track shows the name of the
		/// track
		/// </summary>
		protected internal int minLineCountForTrackNameReminder = 50;

		/// <summary>True if we have not printed the opening bracket for a track yet</summary>
		private bool missingOpenBracket = false;

		/// <summary>The color to use for track beginning and ends</summary>
		protected internal Color trackColor = Color.None;

		protected internal IDictionary<string, Color> channelColors = null;

		protected internal bool addRandomColors = false;

		/// <summary>The style to use for track beginning and ends</summary>
		protected internal Edu.Stanford.Nlp.Util.Logging.Style trackStyle = Edu.Stanford.Nlp.Util.Logging.Style.None;

		protected internal IDictionary<string, Edu.Stanford.Nlp.Util.Logging.Style> channelStyles = null;

		internal static Pair<string, Redwood.Flag> GetSourceStringAndLevel(object[] channel)
		{
			// Parse the channels
			Type source = null;
			// The class the message is coming from
			object backupSource = null;
			// Another identifier for the message
			Redwood.Flag flag = Redwood.Flag.Stdout;
			if (channel != null)
			{
				foreach (object c in channel)
				{
					if (c is Type)
					{
						source = (Type)c;
					}
					else
					{
						// This is a class the message is coming from
						if (c is Redwood.Flag)
						{
							if (c != Redwood.Flag.Force)
							{
								// This is a Redwood flag
								flag = (Redwood.Flag)c;
							}
						}
						else
						{
							backupSource = c;
						}
					}
				}
			}
			// This is another "source" for the log message
			// Get the sourceString. Do at end because there is then an imposed priority ordering
			string sourceString;
			if (source != null)
			{
				sourceString = source.FullName;
			}
			else
			{
				if (backupSource != null)
				{
					sourceString = backupSource.ToString();
				}
				else
				{
					sourceString = "CoreNLP";
				}
			}
			return new Pair<string, Redwood.Flag>(sourceString, flag);
		}

		/// <summary>Print a string to an output without the trailing newline.</summary>
		/// <remarks>
		/// Print a string to an output without the trailing newline.
		/// Many output handlers can get by with just implementing this method.
		/// </remarks>
		/// <param name="channel">
		/// The channels this message was printed on; in most cases
		/// an implementing handler should not have to do anything with
		/// this. The channels should not be printed here.
		/// The channels may be null.
		/// </param>
		/// <param name="line">The string to be printed.</param>
		public abstract void Print(object[] channel, string line);

		/// <summary>Color the tag for a particular channel this color</summary>
		/// <param name="channel">The channel to color</param>
		/// <param name="color">The color to use</param>
		public virtual void ColorChannel(string channel, Color color)
		{
			if (this.channelColors == null)
			{
				this.channelColors = Generics.NewHashMap();
			}
			this.channelColors[channel.ToLower(Locale.English)] = color;
		}

		/// <summary>Style the tag for a particular channel this style</summary>
		/// <param name="channel">The channel to style</param>
		/// <param name="style">The style to use</param>
		public virtual void StyleChannel(string channel, Edu.Stanford.Nlp.Util.Logging.Style style)
		{
			if (this.channelStyles == null)
			{
				this.channelStyles = Generics.NewHashMap();
			}
			this.channelStyles[channel.ToLower(Locale.English)] = style;
		}

		public virtual void SetColorChannels(bool colorChannels)
		{
			this.addRandomColors = colorChannels;
			if (colorChannels)
			{
				this.channelColors = Generics.NewHashMap();
			}
		}

		/// <summary>Style a particular String segment, according to a color and style</summary>
		/// <param name="b">The string builder to append to (for efficiency)</param>
		/// <param name="line">The String to be wrapped</param>
		/// <param name="color">The color to color as</param>
		/// <param name="style">The style to use</param>
		/// <returns>The SringBuilder b</returns>
		protected internal virtual StringBuilder Style(StringBuilder b, string line, Color color, Edu.Stanford.Nlp.Util.Logging.Style style)
		{
			if (color != Color.None || style != Edu.Stanford.Nlp.Util.Logging.Style.None)
			{
				if (Redwood.supportsAnsi && this.SupportsAnsi())
				{
					b.Append(color.ansiCode);
					b.Append(style.ansiCode);
				}
				b.Append(line);
				if (Redwood.supportsAnsi && this.SupportsAnsi())
				{
					b.Append("\x21[0m");
				}
			}
			else
			{
				b.Append(line);
			}
			return b;
		}

		/// <summary>Specify whether this output handler supports ansi output</summary>
		/// <returns>False by default, unless overwritten.</returns>
		protected internal virtual bool SupportsAnsi()
		{
			return false;
		}

		/// <summary>Format a channel</summary>
		/// <param name="b">The StringBuilder to append to</param>
		/// <param name="channelStr">
		/// The [possibly truncated and/or modified] string
		/// to actually print to the StringBuilder
		/// </param>
		/// <param name="channel">The original channel</param>
		/// <returns>|true| if the channel was printed (that is, appended to the StringBuilder)</returns>
		protected internal virtual bool FormatChannel(StringBuilder b, string channelStr, object channel)
		{
			if (this.channelColors == null && this.channelStyles == null)
			{
				//(regular concat)
				b.Append(channelStr);
			}
			else
			{
				string channelToString = channel.ToString().ToLower(Locale.English);
				//(default: no style)
				Color color = Color.None;
				Edu.Stanford.Nlp.Util.Logging.Style style = Edu.Stanford.Nlp.Util.Logging.Style.None;
				//(get color)
				if (this.channelColors != null)
				{
					Color candColor = this.channelColors[channelToString];
					if (candColor != null)
					{
						//((case: found a color))
						color = candColor;
					}
					else
					{
						if (addRandomColors)
						{
							//((case: random colors))
							color = Color.Values()[SloppyMath.PythonMod(channelToString.GetHashCode(), (Color.Values().Length - 3)) + 3];
							if (channelToString.Equals(Redwood.Err.ToString().ToLower()))
							{
								color = Color.Red;
							}
							else
							{
								if (channelToString.Equals(Redwood.Warn.ToString().ToLower()))
								{
									color = Color.Yellow;
								}
							}
							this.channelColors[channelToString] = color;
						}
					}
				}
				//(get style)
				if (this.channelStyles != null)
				{
					Edu.Stanford.Nlp.Util.Logging.Style candStyle = this.channelStyles[channelToString];
					if (candStyle != null)
					{
						style = candStyle;
					}
				}
				//(format)
				Style(b, channelStr, color, style);
			}
			return true;
		}

		// Unless this method is overwritten, channel is always printed
		private void WriteContent(int depth, object content, StringBuilder b)
		{
			if (leftMargin > 2)
			{
				b.Append(tab);
			}
			//(write tabs)
			for (int i = 0; i < depth; i++)
			{
				b.Append(tab);
			}
			//(write content)
			b.Append(content.ToString());
		}

		private void UpdateTracks(int untilDepth)
		{
			while (!queuedTracks.IsEmpty())
			{
				//(get record to update)
				Redwood.Record signal = queuedTracks.RemoveFirst();
				if (signal.depth >= untilDepth)
				{
					queuedTracks.Add(signal);
					return;
				}
				//(begin record message)
				StringBuilder b = new StringBuilder();
				if (missingOpenBracket)
				{
					b.Append("{\n");
				}
				//(write margin)
				for (int i = 0; i < leftMargin; i++)
				{
					b.Append(" ");
				}
				//(write name)
				WriteContent(signal.depth, signal.content, b);
				if (signal.content.ToString().Length > 0)
				{
					b.Append(" ");
				}
				//(print)
				Print(null, this.Style(new StringBuilder(), b.ToString(), trackColor, trackStyle).ToString());
				this.missingOpenBracket = true;
				//only set to false if actually updated track state
				//(update lines printed)
				if (info != null)
				{
					info.numElementsPrinted += 1;
				}
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IList<Redwood.Record> Handle(Redwood.Record record)
		{
			StringBuilder b = new StringBuilder(1024);
			//--Special case for Exceptions
			string[] content;
			if (record.content is Exception)
			{
				//(vars)
				IList<string> lines = new List<string>();
				//(root message)
				Exception exception = (Exception)record.content;
				lines.Add(record.content.ToString());
				StackTraceElement[] trace = exception.GetStackTrace();
				StackTraceElement topTraceElement = trace.Length > 0 ? trace[0] : null;
				foreach (StackTraceElement e in exception.GetStackTrace())
				{
					lines.Add(tab + e.ToString());
				}
				//(causes)
				while (exception.InnerException != null)
				{
					System.Console.Out.WriteLine("TOP ELEMENT: " + topTraceElement);
					//((variables))
					exception = exception.InnerException;
					trace = exception.GetStackTrace();
					lines.Add("Caused by: " + exception.GetType() + ": " + exception.Message);
					for (int i = 0; i < trace.Length; i++)
					{
						//((add trace element))
						StackTraceElement e_1 = trace[i];
						lines.Add(tab + e_1.ToString());
						//((don't print redundant elements))
						if (topTraceElement != null && e_1.GetClassName().Equals(topTraceElement.GetClassName()) && e_1.GetMethodName().Equals(topTraceElement.GetMethodName()))
						{
							lines.Add(tab + "..." + (trace.Length - i - 1) + " more");
							break;
						}
					}
					//((update top element))
					topTraceElement = trace.Length > 0 ? trace[0] : null;
				}
				//(set content array)
				content = new string[lines.Count];
				content = Sharpen.Collections.ToArray(lines, content);
			}
			else
			{
				if (record.content == null)
				{
					content = new string[] { "null" };
				}
				else
				{
					string toStr;
					if (record.content is ISupplier)
					{
						//noinspection unchecked
						toStr = ((ISupplier<object>)record.content).Get().ToString();
					}
					else
					{
						toStr = record.content.ToString();
					}
					if (toStr == null)
					{
						content = new string[] { "<null toString()>" };
					}
					else
					{
						content = toStr.Split("\n");
					}
				}
			}
			//would be nice to get rid of this 'split()' call at some point
			//--Handle Tracks
			UpdateTracks(record.depth);
			if (this.missingOpenBracket)
			{
				this.Style(b, "{\n", trackColor, trackStyle);
				this.missingOpenBracket = false;
			}
			//--Process Record
			//(variables)
			int cursorPos = 0;
			int contentLinesPrinted = 0;
			//(loop)
			Color color = Color.None;
			Edu.Stanford.Nlp.Util.Logging.Style style = Edu.Stanford.Nlp.Util.Logging.Style.None;
			//(get channels)
			List<object> printableChannels = new List<object>();
			foreach (object chan in record.Channels())
			{
				if (chan is Color)
				{
					color = (Color)chan;
				}
				else
				{
					if (chan is Edu.Stanford.Nlp.Util.Logging.Style)
					{
						style = (Edu.Stanford.Nlp.Util.Logging.Style)chan;
					}
					else
					{
						if (chan != Redwood.Force)
						{
							printableChannels.Add(chan);
						}
					}
				}
			}
			//--Write Channels
			if (leftMargin > 2)
			{
				//don't print if not enough space
				//((print channels)
				b.Append("[");
				cursorPos += 1;
				object lastChan = null;
				bool wasAnyChannelPrinted = false;
				for (int i = 0; i < printableChannels.Count; i++)
				{
					object chan_1 = printableChannels[i];
					if (chan_1.Equals(lastChan))
					{
						continue;
					}
					//skip duplicate channels
					lastChan = chan_1;
					//(get channel)
					string toPrint = chan_1.ToString();
					if (toPrint.Length > leftMargin - 1)
					{
						toPrint = Sharpen.Runtime.Substring(toPrint, 0, leftMargin - 2);
					}
					if (cursorPos + toPrint.Length >= leftMargin)
					{
						//(case: doesn't fit)
						while (cursorPos < leftMargin)
						{
							b.Append(" ");
							cursorPos += 1;
						}
						if (contentLinesPrinted < content.Length)
						{
							WriteContent(record.depth, Style(new StringBuilder(), content[contentLinesPrinted], color, style).ToString(), b);
							contentLinesPrinted += 1;
						}
						b.Append("\n ");
						cursorPos = 1;
					}
					//(print flag)
					bool wasChannelPrinted = FormatChannel(b, toPrint, chan_1);
					wasAnyChannelPrinted = wasAnyChannelPrinted || wasChannelPrinted;
					if (wasChannelPrinted && i < printableChannels.Count - 1)
					{
						b.Append(channelSeparatorChar);
						cursorPos += 1;
					}
					cursorPos += toPrint.Length;
				}
				if (wasAnyChannelPrinted)
				{
					b.Append("]");
					cursorPos += 1;
				}
				else
				{
					b.Length = b.Length - 1;
					// remove leading "["
					cursorPos -= 1;
				}
			}
			//--Content
			//(write content)
			while (contentLinesPrinted < content.Length)
			{
				while (cursorPos < leftMargin)
				{
					b.Append(" ");
					cursorPos += 1;
				}
				WriteContent(record.depth, Style(new StringBuilder(), content[contentLinesPrinted], color, style).ToString(), b);
				contentLinesPrinted += 1;
				if (contentLinesPrinted < content.Length)
				{
					b.Append("\n");
					cursorPos = 0;
				}
			}
			//(print)
			if (b.Length == 0 || b[b.Length - 1] != '\n')
			{
				b.Append("\n");
			}
			Print(record.Channels(), b.ToString());
			//--Continue
			if (info != null)
			{
				info.numElementsPrinted += 1;
			}
			List<Redwood.Record> rtn = new List<Redwood.Record>();
			rtn.Add(record);
			return rtn;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IList<Redwood.Record> SignalStartTrack(Redwood.Record signal)
		{
			//(queue track)
			this.queuedTracks.AddLast(signal);
			//(push info)
			if (info != null)
			{
				this.trackStack.Push(info);
			}
			info = new OutputHandler.TrackInfo(signal.content.ToString(), signal.timesstamp);
			//(force print)
			if (signal.Force())
			{
				UpdateTracks(signal.depth + 1);
			}
			//(return)
			return Empty;
		}

		//don't send extra records
		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IList<Redwood.Record> SignalEndTrack(int newDepth, long timeOfEnd)
		{
			//(pop info)
			OutputHandler.TrackInfo childInfo = this.info;
			if (childInfo == null)
			{
				throw new InvalidOperationException("OutputHandler received endTrack() without matching startTrack() --" + "are your handlers mis-configured?");
			}
			if (trackStack.Empty())
			{
				this.info = null;
			}
			else
			{
				this.info = this.trackStack.Pop();
				this.info.numElementsPrinted += childInfo.numElementsPrinted;
			}
			//(handle track)
			if (this.queuedTracks.IsEmpty())
			{
				StringBuilder b = new StringBuilder();
				if (!this.missingOpenBracket)
				{
					//(write margin)
					for (int i = 0; i < this.leftMargin; i++)
					{
						b.Append(' ');
					}
					//(null content)
					WriteContent(newDepth, string.Empty, b);
					//(write bracket)
					b.Append("} ");
				}
				this.missingOpenBracket = false;
				//(write matching line)
				if (childInfo.numElementsPrinted > this.minLineCountForTrackNameReminder)
				{
					b.Append("<< ").Append(childInfo.name).Append(' ');
				}
				//(write time)
				if (timeOfEnd - childInfo.beginTime > 100)
				{
					b.Append('[');
					Redwood.FormatTimeDifference(timeOfEnd - childInfo.beginTime, b);
					b.Append(']');
				}
				//(print)
				b.Append('\n');
				Print(null, this.Style(new StringBuilder(), b.ToString(), trackColor, trackStyle).ToString());
			}
			else
			{
				this.queuedTracks.RemoveLast();
			}
			return Empty;
		}

		/// <summary>
		/// Relevant information about printing the start, and particularly
		/// the end, of a track
		/// </summary>
		private class TrackInfo
		{
			public readonly long beginTime;

			public readonly string name;

			protected internal int numElementsPrinted = 0;

			private TrackInfo(string name, long timestamp)
			{
				//don't send extra records
				this.name = name;
				this.beginTime = timestamp;
			}
		}
	}
}
