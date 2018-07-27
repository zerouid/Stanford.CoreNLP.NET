using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>A class capable of computing the best sequence given a SequenceModel.</summary>
	/// <remarks>
	/// A class capable of computing the best sequence given a SequenceModel.
	/// Uses beam search.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Teg Grenager (grenager@stanford.edu)</author>
	public class BeamBestSequenceFinder : IBestSequenceFinder
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Sequences.BeamBestSequenceFinder));

		private static int[] tmp = null;

		private class TagSeq : IScored
		{
			private class TagList
			{
				internal int tag = -1;

				internal BeamBestSequenceFinder.TagSeq.TagList last = null;
				// todo [CDM 2013]: AFAICS, this class doesn't actually work correctly AND gives nondeterministic answers. See the commented out test in BestSequenceFinderTest
			}

			private double score = 0.0;

			public virtual double Score()
			{
				return score;
			}

			private int size = 0;

			public virtual int Size()
			{
				return size;
			}

			private BeamBestSequenceFinder.TagSeq.TagList info = null;

			public virtual int[] TmpTags(int count, int s)
			{
				if (tmp == null || tmp.Length < s)
				{
					//tmp = new int[1024*128];
					tmp = new int[s];
				}
				BeamBestSequenceFinder.TagSeq.TagList tl = info;
				int i = Size() - 1;
				while (tl != null && count >= 0)
				{
					tmp[i] = tl.tag;
					i--;
					count--;
					tl = tl.last;
				}
				return tmp;
			}

			public virtual int[] Tags()
			{
				int[] t = new int[Size()];
				int i = Size() - 1;
				for (BeamBestSequenceFinder.TagSeq.TagList tl = info; tl != null; tl = tl.last)
				{
					t[i] = tl.tag;
					i--;
				}
				return t;
			}

			public virtual void ExtendWith(int tag)
			{
				BeamBestSequenceFinder.TagSeq.TagList last = info;
				info = new BeamBestSequenceFinder.TagSeq.TagList();
				info.tag = tag;
				info.last = last;
				size++;
			}

			public virtual void ExtendWith(int tag, ISequenceModel ts, int s)
			{
				ExtendWith(tag);
				int[] tags = TmpTags(ts.LeftWindow() + 1 + ts.RightWindow(), s);
				score += ts.ScoreOf(tags, Size() - ts.RightWindow() - 1);
			}

			//for (int i=0; i<tags.length; i++)
			//System.out.print(tags[i]+" ");
			//System.out.println("\nWith "+tag+" score was "+score);
			public virtual BeamBestSequenceFinder.TagSeq Tclone()
			{
				BeamBestSequenceFinder.TagSeq o = new BeamBestSequenceFinder.TagSeq();
				o.info = info;
				o.size = size;
				o.score = score;
				return o;
			}
		}

		private int beamSize;

		private bool exhaustiveStart;

		private bool recenter = true;

		// end class TagSeq
		public virtual int[] BestSequence(ISequenceModel ts)
		{
			return BestSequence(ts, (1024 * 128));
		}

		public virtual int[] BestSequence(ISequenceModel ts, int size)
		{
			// Set up tag options
			int length = ts.Length();
			int leftWindow = ts.LeftWindow();
			int rightWindow = ts.RightWindow();
			int padLength = length + leftWindow + rightWindow;
			int[][] tags = new int[padLength][];
			int[] tagNum = new int[padLength];
			for (int pos = 0; pos < padLength; pos++)
			{
				tags[pos] = ts.GetPossibleValues(pos);
				tagNum[pos] = tags[pos].Length;
			}
			Beam newBeam = new Beam(beamSize, ScoredComparator.AscendingComparator);
			BeamBestSequenceFinder.TagSeq initSeq = new BeamBestSequenceFinder.TagSeq();
			newBeam.Add(initSeq);
			for (int pos_1 = 0; pos_1 < padLength; pos_1++)
			{
				if (Thread.Interrupted())
				{
					// Allow interrupting
					throw new RuntimeInterruptedException();
				}
				//System.out.println("scoring word " + pos + " / " + (leftWindow + length) + ", tagNum = " + tagNum[pos] + "...");
				//System.out.flush();
				Beam oldBeam = newBeam;
				if (pos_1 < leftWindow + rightWindow && exhaustiveStart)
				{
					newBeam = new Beam(100000, ScoredComparator.AscendingComparator);
				}
				else
				{
					newBeam = new Beam(beamSize, ScoredComparator.AscendingComparator);
				}
				// each hypothesis gets extended and beamed
				foreach (object anOldBeam in oldBeam)
				{
					if (Thread.Interrupted())
					{
						// Allow interrupting
						throw new RuntimeInterruptedException();
					}
					// System.out.print("#"); System.out.flush();
					BeamBestSequenceFinder.TagSeq tagSeq = (BeamBestSequenceFinder.TagSeq)anOldBeam;
					for (int nextTagNum = 0; nextTagNum < tagNum[pos_1]; nextTagNum++)
					{
						BeamBestSequenceFinder.TagSeq nextSeq = tagSeq.Tclone();
						if (pos_1 >= leftWindow + rightWindow)
						{
							nextSeq.ExtendWith(tags[pos_1][nextTagNum], ts, size);
						}
						else
						{
							nextSeq.ExtendWith(tags[pos_1][nextTagNum]);
						}
						//System.out.println("Created: "+nextSeq.score()+" %% "+arrayToString(nextSeq.tags(), nextSeq.size()));
						newBeam.Add(nextSeq);
					}
				}
				//		System.out.println("Beam size: "+newBeam.size()+" of "+beamSize);
				//System.out.println("Best is: "+((Scored)newBeam.iterator().next()).score());
				// System.out.println(" done");
				if (recenter)
				{
					double max = double.NegativeInfinity;
					foreach (object aNewBeam1 in newBeam)
					{
						BeamBestSequenceFinder.TagSeq tagSeq = (BeamBestSequenceFinder.TagSeq)aNewBeam1;
						if (tagSeq.score > max)
						{
							max = tagSeq.score;
						}
					}
					foreach (object aNewBeam in newBeam)
					{
						BeamBestSequenceFinder.TagSeq tagSeq = (BeamBestSequenceFinder.TagSeq)aNewBeam;
						tagSeq.score -= max;
					}
				}
			}
			try
			{
				BeamBestSequenceFinder.TagSeq bestSeq = (BeamBestSequenceFinder.TagSeq)newBeam.GetEnumerator().Current;
				int[] seq = bestSeq.Tags();
				return seq;
			}
			catch (NoSuchElementException)
			{
				log.Info("Beam empty -- no best sequence.");
				return null;
			}
		}

		public BeamBestSequenceFinder(int beamSize)
			: this(beamSize, false, false)
		{
		}

		public BeamBestSequenceFinder(int beamSize, bool exhaustiveStart)
			: this(beamSize, exhaustiveStart, false)
		{
		}

		public BeamBestSequenceFinder(int beamSize, bool exhaustiveStart, bool recenter)
		{
			/*
			int[] tempTags = new int[padLength];
			
			// Set up product space sizes
			int[] productSizes = new int[padLength];
			
			int curProduct = 1;
			for (int i=0; i<leftWindow+rightWindow; i++)
			curProduct *= tagNum[i];
			for (int pos = leftWindow+rightWindow; pos < padLength; pos++) {
			if (pos > leftWindow+rightWindow)
			curProduct /= tagNum[pos-leftWindow-rightWindow-1]; // shift off
			curProduct *= tagNum[pos]; // shift on
			productSizes[pos-rightWindow] = curProduct;
			}
			
			// Score all of each window's options
			double[][] windowScore = new double[padLength][];
			for (int pos=leftWindow; pos<leftWindow+length; pos++) {
			windowScore[pos] = new double[productSizes[pos]];
			Arrays.fill(tempTags,tags[0][0]);
			for (int product=0; product<productSizes[pos]; product++) {
			int p = product;
			int shift = 1;
			for (int curPos = pos+rightWindow; curPos >= pos-leftWindow; curPos--) {
			tempTags[curPos] = tags[curPos][p % tagNum[curPos]];
			p /= tagNum[curPos];
			if (curPos > pos)
			shift *= tagNum[curPos];
			}
			if (tempTags[pos] == tags[pos][0]) {
			// get all tags at once
			double[] scores = ts.scoresOf(tempTags, pos);
			// fill in the relevant windowScores
			for (int t = 0; t < tagNum[pos]; t++) {
			windowScore[pos][product+t*shift] = scores[t];
			}
			}
			}
			}
			
			
			// Set up score and backtrace arrays
			double[][] score = new double[padLength][];
			int[][] trace = new int[padLength][];
			for (int pos=0; pos<padLength; pos++) {
			score[pos] = new double[productSizes[pos]];
			trace[pos] = new int[productSizes[pos]];
			}
			
			// Do forward Viterbi algorithm
			
			// loop over the classification spot
			//log.info();
			for (int pos=leftWindow; pos<length+leftWindow; pos++) {
			//log.info(".");
			// loop over window product types
			for (int product=0; product<productSizes[pos]; product++) {
			// check for initial spot
			if (pos==leftWindow) {
			// no predecessor type
			score[pos][product] = windowScore[pos][product];
			trace[pos][product] = -1;
			} else {
			// loop over possible predecessor types
			score[pos][product] = Double.NEGATIVE_INFINITY;
			trace[pos][product] = -1;
			int sharedProduct = product / tagNum[pos+rightWindow];
			int factor = productSizes[pos] / tagNum[pos+rightWindow];
			for (int newTagNum=0; newTagNum<tagNum[pos-leftWindow-1]; newTagNum++) {
			int predProduct = newTagNum*factor+sharedProduct;
			double predScore = score[pos-1][predProduct]+windowScore[pos][product];
			if (predScore > score[pos][product]) {
			score[pos][product] = predScore;
			trace[pos][product] = predProduct;
			}
			}
			}
			}
			}
			
			// Project the actual tag sequence
			double bestFinalScore = Double.NEGATIVE_INFINITY;
			int bestCurrentProduct = -1;
			for (int product=0; product<productSizes[leftWindow+length-1]; product++) {
			if (score[leftWindow+length-1][product] > bestFinalScore) {
			bestCurrentProduct = product;
			bestFinalScore = score[leftWindow+length-1][product];
			}
			}
			int lastProduct = bestCurrentProduct;
			for (int last=padLength-1; last>=length-1; last--) {
			tempTags[last] = tags[last][lastProduct % tagNum[last]];
			lastProduct /= tagNum[last];
			}
			for (int pos=leftWindow+length-2; pos>=leftWindow; pos--) {
			int bestNextProduct = bestCurrentProduct;
			bestCurrentProduct = trace[pos+1][bestNextProduct];
			tempTags[pos-leftWindow] = tags[pos-leftWindow][bestCurrentProduct / (productSizes[pos]/tagNum[pos-leftWindow])];
			}
			return tempTags;
			*/
			/*
			public int[] bestSequenceOld(TagScorer ts) {
			
			// Set up tag options
			int length = ts.length();
			int leftWindow = ts.leftWindow();
			int rightWindow = ts.rightWindow();
			int padLength = length+leftWindow+rightWindow;
			int[][] tags = new int[padLength][];
			int[] tagNum = new int[padLength];
			for (int pos = 0; pos < padLength; pos++) {
			tags[pos] = ts.tagsAt(pos);
			tagNum[pos] = tags[pos].length;
			}
			
			int[] tempTags = new int[padLength];
			
			// Set up product space sizes
			int[] productSizes = new int[padLength];
			
			int curProduct = 1;
			for (int i=0; i<leftWindow+rightWindow; i++)
			curProduct *= tagNum[i];
			for (int pos = leftWindow+rightWindow; pos < padLength; pos++) {
			if (pos > leftWindow+rightWindow)
			curProduct /= tagNum[pos-leftWindow-rightWindow-1]; // shift off
			curProduct *= tagNum[pos]; // shift on
			productSizes[pos-rightWindow] = curProduct;
			}
			
			// Score all of each window's options
			double[][] windowScore = new double[padLength][];
			for (int pos=leftWindow; pos<leftWindow+length; pos++) {
			windowScore[pos] = new double[productSizes[pos]];
			Arrays.fill(tempTags,tags[0][0]);
			for (int product=0; product<productSizes[pos]; product++) {
			int p = product;
			for (int curPos = pos+rightWindow; curPos >= pos-leftWindow; curPos--) {
			tempTags[curPos] = tags[curPos][p % tagNum[curPos]];
			p /= tagNum[curPos];
			}
			windowScore[pos][product] = ts.scoreOf(tempTags, pos);
			}
			}
			
			
			// Set up score and backtrace arrays
			double[][] score = new double[padLength][];
			int[][] trace = new int[padLength][];
			for (int pos=0; pos<padLength; pos++) {
			score[pos] = new double[productSizes[pos]];
			trace[pos] = new int[productSizes[pos]];
			}
			
			// Do forward Viterbi algorithm
			
			// loop over the classification spot
			//log.info();
			for (int pos=leftWindow; pos<length+leftWindow; pos++) {
			//log.info(".");
			// loop over window product types
			for (int product=0; product<productSizes[pos]; product++) {
			// check for initial spot
			if (pos==leftWindow) {
			// no predecessor type
			score[pos][product] = windowScore[pos][product];
			trace[pos][product] = -1;
			} else {
			// loop over possible predecessor types
			score[pos][product] = Double.NEGATIVE_INFINITY;
			trace[pos][product] = -1;
			int sharedProduct = product / tagNum[pos+rightWindow];
			int factor = productSizes[pos] / tagNum[pos+rightWindow];
			for (int newTagNum=0; newTagNum<tagNum[pos-leftWindow-1]; newTagNum++) {
			int predProduct = newTagNum*factor+sharedProduct;
			double predScore = score[pos-1][predProduct]+windowScore[pos][product];
			if (predScore > score[pos][product]) {
			score[pos][product] = predScore;
			trace[pos][product] = predProduct;
			}
			}
			}
			}
			}
			
			// Project the actual tag sequence
			double bestFinalScore = Double.NEGATIVE_INFINITY;
			int bestCurrentProduct = -1;
			for (int product=0; product<productSizes[leftWindow+length-1]; product++) {
			if (score[leftWindow+length-1][product] > bestFinalScore) {
			bestCurrentProduct = product;
			bestFinalScore = score[leftWindow+length-1][product];
			}
			}
			int lastProduct = bestCurrentProduct;
			for (int last=padLength-1; last>=length-1; last--) {
			tempTags[last] = tags[last][lastProduct % tagNum[last]];
			lastProduct /= tagNum[last];
			}
			for (int pos=leftWindow+length-2; pos>=leftWindow; pos--) {
			int bestNextProduct = bestCurrentProduct;
			bestCurrentProduct = trace[pos+1][bestNextProduct];
			tempTags[pos-leftWindow] = tags[pos-leftWindow][bestCurrentProduct / (productSizes[pos]/tagNum[pos-leftWindow])];
			}
			return tempTags;
			}
			*/
			this.exhaustiveStart = exhaustiveStart;
			this.beamSize = beamSize;
			this.recenter = recenter;
		}
	}
}
