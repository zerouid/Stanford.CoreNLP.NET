using System;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Java.Awt;
using Java.Awt.Event;
using Java.Awt.Geom;
using Java.IO;
using Javax.Swing;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.UI
{
	/// <summary>Class for displaying a Tree.</summary>
	/// <author>Dan Klein</author>
	[System.Serializable]
	public class TreeJPanel : JPanel
	{
		protected internal int VerticalAlign = SwingConstantsConstants.Center;

		protected internal int HorizontalAlign = SwingConstantsConstants.Center;

		private int maxFontSize = 128;

		private int minFontSize = 2;

		protected internal const double sisterSkip = 2.5;

		protected internal const double parentSkip = 1.35;

		protected internal const double belowLineSkip = 0.075;

		protected internal const double aboveLineSkip = 0.075;

		protected internal Tree tree;

		public virtual Tree GetTree()
		{
			return tree;
		}

		public virtual void SetTree(Tree tree)
		{
			this.tree = tree;
			Repaint();
		}

		protected internal static string NodeToString(Tree t)
		{
			return (t == null || t.Value() == null) ? " " : t.Value();
		}

		public class WidthResult
		{
			public readonly double width;

			public readonly double nodeTab;

			public readonly double nodeCenter;

			public readonly double childTab;

			public WidthResult(double width, double nodeTab, double nodeCenter, double childTab)
			{
				// = 0.0;
				// = 0.0;
				// = 0.0;
				// = 0.0;
				this.width = width;
				this.nodeTab = nodeTab;
				this.nodeCenter = nodeCenter;
				this.childTab = childTab;
			}
		}

		protected internal static double Width(Tree tree, FontMetrics fM)
		{
			return WidthResult(tree, fM).width;
		}

		protected internal static TreeJPanel.WidthResult WidthResult(Tree tree, FontMetrics fM)
		{
			if (tree == null)
			{
				return new TreeJPanel.WidthResult(0.0, 0.0, 0.0, 0.0);
			}
			double local = fM.StringWidth(NodeToString(tree));
			if (tree.IsLeaf())
			{
				return new TreeJPanel.WidthResult(local, 0.0, local / 2.0, 0.0);
			}
			double sub = 0.0;
			double nodeCenter = 0.0;
			//double childTab = 0.0;
			for (int i = 0; i < numKids; i++)
			{
				TreeJPanel.WidthResult subWR = WidthResult(tree.GetChild(i), fM);
				if (i == 0)
				{
					nodeCenter += (sub + subWR.nodeCenter) / 2.0;
				}
				if (i == numKids - 1)
				{
					nodeCenter += (sub + subWR.nodeCenter) / 2.0;
				}
				sub += subWR.width;
				if (i < numKids - 1)
				{
					sub += sisterSkip * fM.StringWidth(" ");
				}
			}
			double localLeft = local / 2.0;
			double subLeft = nodeCenter;
			double totalLeft = Math.Max(localLeft, subLeft);
			double localRight = local / 2.0;
			double subRight = sub - nodeCenter;
			double totalRight = Math.Max(localRight, subRight);
			return new TreeJPanel.WidthResult(totalLeft + totalRight, totalLeft - localLeft, nodeCenter + totalLeft - subLeft, totalLeft - subLeft);
		}

		protected internal static double Height(Tree tree, FontMetrics fM)
		{
			if (tree == null)
			{
				return 0.0;
			}
			double depth = tree.Depth();
			return fM.GetHeight() * (1.0 + depth * (1.0 + parentSkip + aboveLineSkip + belowLineSkip));
		}

		protected internal virtual FontMetrics PickFont(Graphics2D g2, Tree tree, Dimension space)
		{
			Font font = g2.GetFont();
			string fontName = font.GetName();
			int style = font.GetStyle();
			for (int size = maxFontSize; size > minFontSize; size--)
			{
				font = new Font(fontName, style, size);
				g2.SetFont(font);
				FontMetrics fontMetrics = g2.GetFontMetrics();
				if (Height(tree, fontMetrics) > space.GetHeight())
				{
					continue;
				}
				if (Width(tree, fontMetrics) > space.GetWidth())
				{
					continue;
				}
				//System.out.println("Chose: "+size+" for space: "+space.getWidth());
				return fontMetrics;
			}
			font = new Font(fontName, style, minFontSize);
			g2.SetFont(font);
			return g2.GetFontMetrics();
		}

		private static double PaintTree(Tree t, Point2D start, Graphics2D g2, FontMetrics fM)
		{
			if (t == null)
			{
				return 0.0;
			}
			string nodeStr = NodeToString(t);
			double nodeWidth = fM.StringWidth(nodeStr);
			double nodeHeight = fM.GetHeight();
			double nodeAscent = fM.GetAscent();
			TreeJPanel.WidthResult wr = WidthResult(t, fM);
			double treeWidth = wr.width;
			double nodeTab = wr.nodeTab;
			double childTab = wr.childTab;
			double nodeCenter = wr.nodeCenter;
			//double treeHeight = height(t, fM);
			// draw root
			g2.DrawString(nodeStr, (float)(nodeTab + start.GetX()), (float)(start.GetY() + nodeAscent));
			if (t.IsLeaf())
			{
				return nodeWidth;
			}
			double layerMultiplier = (1.0 + belowLineSkip + aboveLineSkip + parentSkip);
			double layerHeight = nodeHeight * layerMultiplier;
			double childStartX = start.GetX() + childTab;
			double childStartY = start.GetY() + layerHeight;
			double lineStartX = start.GetX() + nodeCenter;
			double lineStartY = start.GetY() + nodeHeight * (1.0 + belowLineSkip);
			double lineEndY = lineStartY + nodeHeight * parentSkip;
			// recursively draw children
			for (int i = 0; i < t.Children().Length; i++)
			{
				Tree child = t.Children()[i];
				double cWidth = PaintTree(child, new Point2D.Double(childStartX, childStartY), g2, fM);
				// draw connectors
				wr = WidthResult(child, fM);
				double lineEndX = childStartX + wr.nodeCenter;
				g2.Draw(new Line2D.Double(lineStartX, lineStartY, lineEndX, lineEndY));
				childStartX += cWidth;
				if (i < t.Children().Length - 1)
				{
					childStartX += sisterSkip * fM.StringWidth(" ");
				}
			}
			return treeWidth;
		}

		protected internal virtual void SuperPaint(Graphics g)
		{
			base.PaintComponent(g);
		}

		protected override void PaintComponent(Graphics g)
		{
			SuperPaint(g);
			Graphics2D g2 = (Graphics2D)g;
			g2.SetRenderingHint(RenderingHints.KeyAntialiasing, RenderingHints.ValueAntialiasOn);
			Dimension space = GetSize();
			FontMetrics fM = PickFont(g2, tree, space);
			double width = Width(tree, fM);
			double height = Height(tree, fM);
			double startX = 0.0;
			double startY = 0.0;
			if (HorizontalAlign == SwingConstantsConstants.Center)
			{
				startX = (space.GetWidth() - width) / 2.0;
			}
			if (HorizontalAlign == SwingConstantsConstants.Right)
			{
				startX = space.GetWidth() - width;
			}
			if (VerticalAlign == SwingConstantsConstants.Center)
			{
				startY = (space.GetHeight() - height) / 2.0;
			}
			if (VerticalAlign == SwingConstantsConstants.Bottom)
			{
				startY = space.GetHeight() - height;
			}
			PaintTree(tree, new Point2D.Double(startX, startY), g2, fM);
		}

		public TreeJPanel()
			: this(SwingConstantsConstants.Center, SwingConstantsConstants.Center)
		{
		}

		public TreeJPanel(int hAlign, int vAlign)
		{
			HorizontalAlign = hAlign;
			VerticalAlign = vAlign;
			SetPreferredSize(new Dimension(400, 300));
		}

		public virtual void SetMinFontSize(int size)
		{
			minFontSize = size;
		}

		public virtual void SetMaxFontSize(int size)
		{
			maxFontSize = size;
		}

		public virtual Font PickFont()
		{
			Font font = GetFont();
			string fontName = font.GetName();
			int style = font.GetStyle();
			int size = (maxFontSize + minFontSize) / 2;
			return new Font(fontName, style, size);
		}

		public virtual Dimension GetTreeDimension(Tree tree, Font font)
		{
			FontMetrics fM = GetFontMetrics(font);
			return new Dimension((int)Width(tree, fM), (int)Height(tree, fM));
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			TreeJPanel tjp = new TreeJPanel();
			// String ptbTreeString1 = "(ROOT (S (NP (DT This)) (VP (VBZ is) (NP (DT a) (NN test))) (. .)))";
			string ptbTreeString = "(ROOT (S (NP (NNP Interactive_Tregex)) (VP (VBZ works)) (PP (IN for) (PRP me)) (. !))))";
			if (args.Length > 0)
			{
				ptbTreeString = args[0];
			}
			Tree tree = (new PennTreeReader(new StringReader(ptbTreeString), new LabeledScoredTreeFactory(new StringLabelFactory()))).ReadTree();
			tjp.SetTree(tree);
			tjp.SetBackground(Color.white);
			JFrame frame = new JFrame();
			frame.GetContentPane().Add(tjp, BorderLayout.Center);
			frame.AddWindowListener(new _WindowAdapter_256());
			frame.Pack();
			frame.SetVisible(true);
			frame.SetVisible(true);
		}

		private sealed class _WindowAdapter_256 : WindowAdapter
		{
			public _WindowAdapter_256()
			{
			}

			public override void WindowClosing(WindowEvent e)
			{
				System.Environment.Exit(0);
			}
		}
	}
}
