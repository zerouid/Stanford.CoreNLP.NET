using System;
using System.Collections.Generic;
using System.IO;
using Java.Awt;
using Java.Awt.Event;
using Java.Lang;
using Java.Text;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Stream;
using Javax.Swing;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>This implements a confusion table over arbitrary types of class labels.</summary>
	/// <remarks>
	/// This implements a confusion table over arbitrary types of class labels. Main
	/// routines of interest:
	/// add(guess, gold), increments the guess/gold entry in this cell by 1
	/// get(guess, gold), returns the number of entries in this cell
	/// toString(), returns printed form of the table, with marginals and
	/// contingencies for each class label
	/// Example usage:
	/// Confusion<String> myConf = new Confusion<String>();
	/// myConf.add("l1", "l1");
	/// myConf.add("l1", "l2");
	/// myConf.add("l2", "l2");
	/// System.out.println(myConf.toString());
	/// NOTES: - This sorts by the toString() of the guess and gold labels. Thus the
	/// label.toString() values should be distinct!
	/// </remarks>
	/// <author>yeh1@cs.stanford.edu</author>
	/// <?/>
	public class ConfusionMatrix<U>
	{
		private const string ClassPrefix = "C";

		private const string Format = "#.#####";

		protected internal DecimalFormat format;

		private int leftPadSize = 16;

		private int delimPadSize = 8;

		private bool useRealLabels = false;

		public ConfusionMatrix()
		{
			// classification placeholder prefix when drawing in table
			format = new DecimalFormat(Format);
		}

		public ConfusionMatrix(Locale locale)
		{
			format = new DecimalFormat(Format, new DecimalFormatSymbols(locale));
		}

		public override string ToString()
		{
			return PrintTable();
		}

		/// <summary>This sets the lefthand side pad width for displaying the text table.</summary>
		/// <param name="newPadSize"/>
		public virtual void SetLeftPadSize(int newPadSize)
		{
			this.leftPadSize = newPadSize;
		}

		/// <summary>Sets the width used to separate cells in the table.</summary>
		public virtual void SetDelimPadSize(int newPadSize)
		{
			this.delimPadSize = newPadSize;
		}

		public virtual void SetUseRealLabels(bool useRealLabels)
		{
			this.useRealLabels = useRealLabels;
		}

		/// <summary>
		/// Contingency table, listing precision ,recall, specificity, and f1 given
		/// the number of true and false positives, true and false negatives.
		/// </summary>
		/// <author>yeh1@cs.stanford.edu</author>
		public class Contingency
		{
			private double tp = 0;

			private double fp = 0;

			private double tn = 0;

			private double fn = 0;

			private double prec = 0.0;

			private double recall = 0.0;

			private double spec = 0.0;

			private double f1 = 0.0;

			public Contingency(ConfusionMatrix<U> _enclosing, int tp_, int fp_, int tn_, int fn_)
			{
				this._enclosing = _enclosing;
				this.tp = tp_;
				this.fp = fp_;
				this.tn = tn_;
				this.fn = fn_;
				this.prec = this.tp / (this.tp + this.fp);
				this.recall = this.tp / (this.tp + this.fn);
				this.spec = this.tn / (this.fp + this.tn);
				this.f1 = (2 * this.prec * this.recall) / (this.prec + this.recall);
			}

			public override string ToString()
			{
				return StringUtils.Join(Arrays.AsList("prec=" + (((this.tp + this.fp) > 0) ? this._enclosing.format.Format(this.prec) : "n/a"), "recall=" + (((this.tp + this.fn) > 0) ? this._enclosing.format.Format(this.recall) : "n/a"), "spec=" + (((this.fp
					 + this.tn) > 0) ? this._enclosing.format.Format(this.spec) : "n/a"), "f1=" + (((this.prec + this.recall) > 0) ? this._enclosing.format.Format(this.f1) : "n/a")), ", ");
			}

			public virtual double F1()
			{
				return this.f1;
			}

			public virtual double Precision()
			{
				return this.prec;
			}

			public virtual double Recall()
			{
				return this.recall;
			}

			public virtual double Spec()
			{
				return this.spec;
			}

			private readonly ConfusionMatrix<U> _enclosing;
		}

		private ConcurrentHashMap<Pair<U, U>, int> confTable = new ConcurrentHashMap<Pair<U, U>, int>();

		/// <summary>Increments the entry for this guess and gold by 1.</summary>
		public virtual void Add(U guess, U gold)
		{
			Add(guess, gold, 1);
		}

		/// <summary>Increments the entry for this guess and gold by the given increment amount.</summary>
		public virtual void Add(U guess, U gold, int increment)
		{
			lock (this)
			{
				Pair<U, U> pair = new Pair<U, U>(guess, gold);
				if (confTable.Contains(pair))
				{
					confTable[pair] = confTable[pair] + increment;
				}
				else
				{
					confTable[pair] = increment;
				}
			}
		}

		/// <summary>Retrieves the number of entries with this guess and gold.</summary>
		public virtual int Get(U guess, U gold)
		{
			Pair<U, U> pair = new Pair<U, U>(guess, gold);
			if (confTable.Contains(pair))
			{
				return confTable[pair];
			}
			else
			{
				return 0;
			}
		}

		/// <summary>
		/// Returns the set of distinct class labels
		/// entered into this confusion table.
		/// </summary>
		public virtual ICollection<U> UniqueLabels()
		{
			HashSet<U> ret = new HashSet<U>();
			foreach (Pair<U, U> pair in ((ConcurrentHashMap.KeySetView<Pair<U, U>, int>)confTable.Keys))
			{
				ret.Add(pair.First());
				ret.Add(pair.Second());
			}
			return ret;
		}

		/// <summary>
		/// Returns the contingency table for the given class label, where all other
		/// class labels are treated as negative.
		/// </summary>
		public virtual ConfusionMatrix.Contingency GetContingency(U positiveLabel)
		{
			int tp = 0;
			int fp = 0;
			int tn = 0;
			int fn = 0;
			foreach (Pair<U, U> pair in ((ConcurrentHashMap.KeySetView<Pair<U, U>, int>)confTable.Keys))
			{
				int count = confTable[pair];
				U guess = pair.First();
				U gold = pair.Second();
				bool guessP = guess.Equals(positiveLabel);
				bool goldP = gold.Equals(positiveLabel);
				if (guessP && goldP)
				{
					tp += count;
				}
				else
				{
					if (!guessP && goldP)
					{
						fn += count;
					}
					else
					{
						if (guessP && !goldP)
						{
							fp += count;
						}
						else
						{
							tn += count;
						}
					}
				}
			}
			return new ConfusionMatrix.Contingency(this, tp, fp, tn, fn);
		}

		/// <summary>Returns the current set of unique labels, sorted by their string order.</summary>
		private IList<U> SortKeys()
		{
			ICollection<U> labels = UniqueLabels();
			if (labels.Count == 0)
			{
				return Java.Util.Collections.EmptyList();
			}
			bool comparable = true;
			foreach (U label in labels)
			{
				if (!(label is IComparable))
				{
					comparable = false;
					break;
				}
			}
			if (comparable)
			{
				IList<IComparable<object>> sorted = Generics.NewArrayList();
				foreach (U label_1 in labels)
				{
					sorted.Add(ErasureUtils.UncheckedCast<IComparable<object>>(label_1));
				}
				sorted.Sort();
				IList<U> ret = Generics.NewArrayList();
				foreach (object o in sorted)
				{
					ret.Add(ErasureUtils.UncheckedCast<U>(o));
				}
				return ret;
			}
			else
			{
				List<string> names = new List<string>();
				Dictionary<string, U> lookup = new Dictionary<string, U>();
				foreach (U label_1 in labels)
				{
					names.Add(label_1.ToString());
					lookup[label_1.ToString()] = label_1;
				}
				names.Sort();
				List<U> ret = new List<U>();
				foreach (string name in names)
				{
					ret.Add(lookup[name]);
				}
				return ret;
			}
		}

		/// <summary>Marginal over the given gold, or column sum</summary>
		private int GoldMarginal(U gold)
		{
			int sum = 0;
			ICollection<U> labels = UniqueLabels();
			foreach (U guess in labels)
			{
				sum += Get(guess, gold);
			}
			return sum;
		}

		/// <summary>Marginal over given guess, or row sum</summary>
		private int GuessMarginal(U guess)
		{
			int sum = 0;
			ICollection<U> labels = UniqueLabels();
			foreach (U gold in labels)
			{
				sum += Get(guess, gold);
			}
			return sum;
		}

		private string GetPlaceHolder(int index, U label)
		{
			if (useRealLabels)
			{
				return label.ToString();
			}
			else
			{
				return ClassPrefix + (index + 1);
			}
		}

		// class name
		/// <summary>Prints the current confusion in table form to a string, with contingency</summary>
		public virtual string PrintTable()
		{
			IList<U> sortedLabels = SortKeys();
			if (confTable.Count == 0)
			{
				return "Empty table!";
			}
			StringWriter ret = new StringWriter();
			// header row (top)
			ret.Write(StringUtils.PadLeft("Guess/Gold", leftPadSize));
			for (int i = 0; i < sortedLabels.Count; i++)
			{
				string placeHolder = GetPlaceHolder(i, sortedLabels[i]);
				// placeholder
				ret.Write(StringUtils.PadLeft(placeHolder, delimPadSize));
			}
			ret.Write("    Marg. (Guess)");
			ret.Write("\n");
			// Write out contents
			for (int guessI = 0; guessI < sortedLabels.Count; guessI++)
			{
				string placeHolder = GetPlaceHolder(guessI, sortedLabels[guessI]);
				ret.Write(StringUtils.PadLeft(placeHolder, leftPadSize));
				U guess = sortedLabels[guessI];
				foreach (U gold in sortedLabels)
				{
					int value = Get(guess, gold);
					ret.Write(StringUtils.PadLeft(value.ToString(), delimPadSize));
				}
				ret.Write(StringUtils.PadLeft(GuessMarginal(guess).ToString(), delimPadSize));
				ret.Write("\n");
			}
			// Bottom row, write out marginals over golds
			ret.Write(StringUtils.PadLeft("Marg. (Gold)", leftPadSize));
			foreach (U gold_1 in sortedLabels)
			{
				ret.Write(StringUtils.PadLeft(GoldMarginal(gold_1).ToString(), delimPadSize));
			}
			// Print out key, along with contingencies
			ret.Write("\n\n");
			for (int labelI = 0; labelI < sortedLabels.Count; labelI++)
			{
				U classLabel = sortedLabels[labelI];
				string placeHolder = GetPlaceHolder(labelI, classLabel);
				ret.Write(StringUtils.PadLeft(placeHolder, leftPadSize));
				if (!useRealLabels)
				{
					ret.Write(" = ");
					ret.Write(classLabel.ToString());
				}
				ret.Write(StringUtils.PadLeft(string.Empty, delimPadSize));
				ConfusionMatrix.Contingency contingency = GetContingency(classLabel);
				ret.Write(contingency.ToString());
				ret.Write("\n");
			}
			return ret.ToString();
		}

		[System.Serializable]
		private class ConfusionGrid : Canvas
		{
			[System.Serializable]
			public class Grid : JPanel
			{
				private int columnCount = this._enclosing._enclosing.UniqueLabels().Count + 1;

				private int rowCount = this._enclosing._enclosing.UniqueLabels().Count + 1;

				private IList<Rectangle> cells;

				private Point selectedCell;

				public Grid(ConfusionGrid _enclosing)
				{
					this._enclosing = _enclosing;
					this.cells = new List<Rectangle>(this.columnCount * this.rowCount);
					MouseAdapter mouseHandler;
					mouseHandler = new _MouseAdapter_355(this);
					this.AddMouseMotionListener(mouseHandler);
				}

				private sealed class _MouseAdapter_355 : MouseAdapter
				{
					public _MouseAdapter_355(Grid _enclosing)
					{
						this._enclosing = _enclosing;
					}

					public override void MouseMoved(MouseEvent e)
					{
						int width = this._enclosing.GetWidth();
						int height = this._enclosing.GetHeight();
						int cellWidth = width / this._enclosing.columnCount;
						int cellHeight = height / this._enclosing.rowCount;
						int column = e.GetX() / cellWidth;
						int row = e.GetY() / cellHeight;
						this._enclosing.selectedCell = new Point(column, row);
						this._enclosing.Repaint();
					}

					private readonly Grid _enclosing;
				}

				public virtual void OnMouseOver(Graphics2D g2d, Rectangle cell, U guess, U gold)
				{
					// Compute values
					int x = (int)(cell.GetLocation().x + cell.GetWidth() / 5.0);
					int y = (int)(cell.GetLocation().y + cell.GetHeight() / 5.0);
					// Compute the text
					int value = this._enclosing._enclosing.confTable[Pair.MakePair(guess, gold)];
					if (value == null)
					{
						value = 0;
					}
					string text = "Guess: " + guess.ToString() + "\n" + "Gold: " + gold.ToString() + "\n" + "Value: " + value;
					// Set the font
					Font bak = g2d.GetFont();
					g2d.SetFont(bak.DeriveFont(bak.GetSize() * 2.0f));
					// Render
					g2d.SetColor(Color.White);
					g2d.Fill(cell);
					g2d.SetColor(Color.Black);
					foreach (string line in text.Split("\n"))
					{
						g2d.DrawString(line, x, y += g2d.GetFontMetrics().GetHeight());
					}
					// Reset
					g2d.SetFont(bak);
				}

				public override Dimension GetPreferredSize()
				{
					return new Dimension(800, 800);
				}

				public override void Invalidate()
				{
					this.cells.Clear();
					base.Invalidate();
				}

				protected override void PaintComponent(Graphics g)
				{
					base.PaintComponent(g);
					// Dimensions
					Graphics2D g2d = (Graphics2D)g.Create();
					g.SetFont(new Font("Arial", Font.Plain, 10));
					int width = this.GetWidth();
					int height = this.GetHeight();
					int cellWidth = width / this.columnCount;
					int cellHeight = height / this.rowCount;
					int xOffset = (width - (this.columnCount * cellWidth)) / 2;
					int yOffset = (height - (this.rowCount * cellHeight)) / 2;
					// Get label index
					IList<U> labels = this._enclosing._enclosing.UniqueLabels().Stream().Collect(Collectors.ToList());
					// Get color gradient
					int maxDiag = 0;
					int maxOffdiag = 0;
					foreach (KeyValuePair<Pair<U, U>, int> entry in this._enclosing._enclosing.confTable)
					{
						if (entry.Key.first == entry.Key.second)
						{
							maxDiag = Math.Max(maxDiag, entry.Value);
						}
						else
						{
							maxOffdiag = Math.Max(maxOffdiag, entry.Value);
						}
					}
					// Render the grid
					float[] hsb = new float[3];
					for (int row = 0; row < this.rowCount; row++)
					{
						for (int col = 0; col < this.columnCount; col++)
						{
							// Position
							int x = xOffset + (col * cellWidth);
							int y = yOffset + (row * cellHeight);
							float xCenter = xOffset + (col * cellWidth) + cellWidth / 3.0f;
							float yCenter = yOffset + (row * cellHeight) + cellHeight / 2.0f;
							// Get text + Color
							string text;
							Color bg = Color.White;
							if (row == 0 && col == 0)
							{
								text = "V guess | gold >";
							}
							else
							{
								if (row == 0)
								{
									text = labels[col - 1].ToString();
								}
								else
								{
									if (col == 0)
									{
										text = labels[row - 1].ToString();
									}
									else
									{
										// Set value
										int count = this._enclosing._enclosing.confTable[Pair.MakePair(labels[row - 1], labels[col - 1])];
										if (count == null)
										{
											count = 0;
										}
										text = string.Empty + count;
										// Get color
										if (row == col)
										{
											double percentGood = ((double)count) / ((double)maxDiag);
											hsb = Color.RGBtoHSB((int)(255 - (255.0 * percentGood)), (int)(255 - (255.0 * percentGood / 2.0)), (int)(255 - (255.0 * percentGood)), hsb);
											bg = Color.GetHSBColor(hsb[0], hsb[1], hsb[2]);
										}
										else
										{
											double percentBad = ((double)count) / ((double)maxOffdiag);
											hsb = Color.RGBtoHSB((int)(255 - (255.0 * percentBad / 2.0)), (int)(255 - (255.0 * percentBad)), (int)(255 - (255.0 * percentBad)), hsb);
											bg = Color.GetHSBColor(hsb[0], hsb[1], hsb[2]);
										}
									}
								}
							}
							// Draw
							Rectangle cell = new Rectangle(x, y, cellWidth, cellHeight);
							g2d.SetColor(bg);
							g2d.Fill(cell);
							g2d.SetColor(Color.Black);
							g2d.DrawString(text, xCenter, yCenter);
							this.cells.Add(cell);
						}
					}
					// Mouse over
					if (this.selectedCell != null && this.selectedCell.x > 0 && this.selectedCell.y > 0)
					{
						int index = this.selectedCell.x + (this.selectedCell.y * this.columnCount);
						Rectangle cell = this.cells[index];
						this.OnMouseOver(g2d, cell, labels[this.selectedCell.y - 1], labels[this.selectedCell.x - 1]);
					}
					// Clean up
					g2d.Dispose();
				}

				private readonly ConfusionGrid _enclosing;
			}

			public ConfusionGrid(ConfusionMatrix<U> _enclosing)
			{
				this._enclosing = _enclosing;
				EventQueue.InvokeLater(null);
			}

			private readonly ConfusionMatrix<U> _enclosing;
		}

		/// <summary>Show the confusion matrix in a GUI.</summary>
		public virtual void Gui()
		{
			ConfusionMatrix.ConfusionGrid gui = new ConfusionMatrix.ConfusionGrid(this);
			gui.SetVisible(true);
		}

		public static void Main(string[] args)
		{
			ConfusionMatrix<string> confusion = new ConfusionMatrix<string>();
			confusion.Add("a", "a");
			confusion.Add("a", "b");
			confusion.Add("b", "a");
			confusion.Add("a", "a");
			confusion.Add("b", "b");
			confusion.Add("b", "b");
			confusion.Add("a", "b");
			confusion.Gui();
		}
	}
}
