/* Generated By:JavaCC: Do not edit this line. TokenSequenceParserTokenManager.java */
using System.IO;


namespace Edu.Stanford.Nlp.Ling.Tokensregex.Parser
{
	/// <summary>Token Manager.</summary>
	internal class TokenSequenceParserTokenManager : ITokenSequenceParserConstants
	{
		/// <summary>Debug output.</summary>
		public TextWriter debugStream = System.Console.Out;

		// all generated classes are in this package
		//imports
		/// <summary>Set debug output.</summary>
		public virtual void SetDebugStream(TextWriter ds)
		{
			debugStream = ds;
		}

		private int JjStopStringLiteralDfa_0(int pos, long active0)
		{
			switch (pos)
			{
				case 0:
				{
					if ((active0 & unchecked((long)(0x20000020800000L))) != 0L)
					{
						return 50;
					}
					if ((active0 & unchecked((long)(0x1000000000000L))) != 0L)
					{
						return 52;
					}
					if ((active0 & unchecked((long)(0x18000000L))) != 0L)
					{
						jjmatchedKind = 7;
						return 53;
					}
					if ((active0 & unchecked((long)(0x100000000000L))) != 0L)
					{
						return 54;
					}
					if ((active0 & unchecked((long)(0x20000000000L))) != 0L)
					{
						return 34;
					}
					return -1;
				}

				case 1:
				{
					if ((active0 & unchecked((long)(0x18000000L))) != 0L)
					{
						jjmatchedKind = 7;
						jjmatchedPos = 1;
						return 53;
					}
					if ((active0 & unchecked((long)(0x20000000000000L))) != 0L)
					{
						jjmatchedKind = 19;
						jjmatchedPos = 1;
						return -1;
					}
					return -1;
				}

				case 2:
				{
					if ((active0 & unchecked((long)(0x20000000000000L))) != 0L)
					{
						if (jjmatchedPos < 1)
						{
							jjmatchedKind = 19;
							jjmatchedPos = 1;
						}
						return -1;
					}
					if ((active0 & unchecked((long)(0x18000000L))) != 0L)
					{
						jjmatchedKind = 7;
						jjmatchedPos = 2;
						return 53;
					}
					return -1;
				}

				case 3:
				{
					if ((active0 & unchecked((long)(0x18000000L))) != 0L)
					{
						jjmatchedKind = 7;
						jjmatchedPos = 3;
						return 53;
					}
					return -1;
				}

				case 4:
				{
					if ((active0 & unchecked((long)(0x8000000L))) != 0L)
					{
						jjmatchedKind = 7;
						jjmatchedPos = 4;
						return 53;
					}
					return -1;
				}

				case 5:
				{
					if ((active0 & unchecked((long)(0x8000000L))) != 0L)
					{
						jjmatchedKind = 7;
						jjmatchedPos = 5;
						return 53;
					}
					return -1;
				}

				default:
				{
					return -1;
				}
			}
		}

		private int JjStartNfa_0(int pos, long active0)
		{
			return JjMoveNfa_0(JjStopStringLiteralDfa_0(pos, active0), pos + 1);
		}

		private int JjStopAtPos(int pos, int kind)
		{
			jjmatchedKind = kind;
			jjmatchedPos = pos;
			return pos + 1;
		}

		private int JjMoveStringLiteralDfa0_0()
		{
			switch (curChar)
			{
				case 33:
				{
					return JjStartNfaWithStates_0(0, 48, 52);
				}

				case 36:
				{
					return JjStartNfaWithStates_0(0, 41, 34);
				}

				case 38:
				{
					jjmatchedKind = 46;
					return JjMoveStringLiteralDfa1_0(unchecked((long)(0x40001000000000L)));
				}

				case 40:
				{
					jjmatchedKind = 25;
					return JjMoveStringLiteralDfa1_0(unchecked((long)(0x10000000000000L)));
				}

				case 41:
				{
					return JjStopAtPos(0, 26);
				}

				case 42:
				{
					return JjStopAtPos(0, 42);
				}

				case 43:
				{
					return JjStartNfaWithStates_0(0, 44, 54);
				}

				case 44:
				{
					return JjStopAtPos(0, 33);
				}

				case 46:
				{
					return JjStopAtPos(0, 35);
				}

				case 58:
				{
					jjmatchedKind = 34;
					return JjMoveStringLiteralDfa1_0(unchecked((long)(0x800c000000000L)));
				}

				case 59:
				{
					return JjStopAtPos(0, 30);
				}

				case 61:
				{
					jjmatchedKind = 29;
					return JjMoveStringLiteralDfa1_0(unchecked((long)(0x20000000800000L)));
				}

				case 63:
				{
					jjmatchedKind = 43;
					return JjMoveStringLiteralDfa1_0(unchecked((long)(0x800000000000L)));
				}

				case 91:
				{
					return JjStopAtPos(0, 31);
				}

				case 93:
				{
					return JjStopAtPos(0, 32);
				}

				case 94:
				{
					return JjStopAtPos(0, 40);
				}

				case 116:
				{
					return JjMoveStringLiteralDfa1_0(unchecked((long)(0x18000000L)));
				}

				case 123:
				{
					jjmatchedKind = 22;
					return JjMoveStringLiteralDfa1_0(unchecked((long)(0x2000000000000L)));
				}

				case 124:
				{
					jjmatchedKind = 45;
					return JjMoveStringLiteralDfa1_0(unchecked((long)(0x2000000000L)));
				}

				case 125:
				{
					jjmatchedKind = 24;
					return JjMoveStringLiteralDfa1_0(unchecked((long)(0x4000000000000L)));
				}

				default:
				{
					return JjMoveNfa_0(5, 0);
				}
			}
		}

		private int JjMoveStringLiteralDfa1_0(long active0)
		{
			try
			{
				curChar = input_stream.ReadChar();
			}
			catch (IOException)
			{
				JjStopStringLiteralDfa_0(0, active0);
				return 1;
			}
			switch (curChar)
			{
				case 38:
				{
					if ((active0 & unchecked((long)(0x1000000000L))) != 0L)
					{
						return JjStopAtPos(1, 36);
					}
					break;
				}

				case 58:
				{
					if ((active0 & unchecked((long)(0x800000000000L))) != 0L)
					{
						return JjStopAtPos(1, 47);
					}
					else
					{
						if ((active0 & unchecked((long)(0x8000000000000L))) != 0L)
						{
							return JjStopAtPos(1, 51);
						}
					}
					break;
				}

				case 61:
				{
					return JjMoveStringLiteralDfa2_0(active0, unchecked((long)(0x20000000000000L)));
				}

				case 62:
				{
					if ((active0 & unchecked((long)(0x800000L))) != 0L)
					{
						return JjStopAtPos(1, 23);
					}
					break;
				}

				case 63:
				{
					return JjMoveStringLiteralDfa2_0(active0, unchecked((long)(0x10000000000000L)));
				}

				case 97:
				{
					return JjMoveStringLiteralDfa2_0(active0, unchecked((long)(0x40000000000000L)));
				}

				case 99:
				{
					return JjMoveStringLiteralDfa2_0(active0, unchecked((long)(0x4000000000L)));
				}

				case 101:
				{
					return JjMoveStringLiteralDfa2_0(active0, unchecked((long)(0x8010000000L)));
				}

				case 111:
				{
					return JjMoveStringLiteralDfa2_0(active0, unchecked((long)(0x8000000L)));
				}

				case 123:
				{
					if ((active0 & unchecked((long)(0x2000000000000L))) != 0L)
					{
						return JjStopAtPos(1, 49);
					}
					break;
				}

				case 124:
				{
					if ((active0 & unchecked((long)(0x2000000000L))) != 0L)
					{
						return JjStopAtPos(1, 37);
					}
					break;
				}

				case 125:
				{
					if ((active0 & unchecked((long)(0x4000000000000L))) != 0L)
					{
						return JjStopAtPos(1, 50);
					}
					break;
				}

				default:
				{
					break;
				}
			}
			return JjStartNfa_0(0, active0);
		}

		private int JjMoveStringLiteralDfa2_0(long old0, long active0)
		{
			if (((active0 &= old0)) == 0L)
			{
				return JjStartNfa_0(0, old0);
			}
			try
			{
				curChar = input_stream.ReadChar();
			}
			catch (IOException)
			{
				JjStopStringLiteralDfa_0(1, active0);
				return 2;
			}
			switch (curChar)
			{
				case 62:
				{
					if ((active0 & unchecked((long)(0x20000000000000L))) != 0L)
					{
						return JjStopAtPos(2, 53);
					}
					break;
				}

				case 97:
				{
					return JjMoveStringLiteralDfa3_0(active0, unchecked((long)(0x4000000000L)));
				}

				case 107:
				{
					return JjMoveStringLiteralDfa3_0(active0, unchecked((long)(0x8000000L)));
				}

				case 108:
				{
					return JjMoveStringLiteralDfa3_0(active0, unchecked((long)(0x8000000000L)));
				}

				case 109:
				{
					return JjMoveStringLiteralDfa3_0(active0, unchecked((long)(0x10000000000000L)));
				}

				case 110:
				{
					return JjMoveStringLiteralDfa3_0(active0, unchecked((long)(0x40000000000000L)));
				}

				case 120:
				{
					return JjMoveStringLiteralDfa3_0(active0, unchecked((long)(0x10000000L)));
				}

				default:
				{
					break;
				}
			}
			return JjStartNfa_0(1, active0);
		}

		private int JjMoveStringLiteralDfa3_0(long old0, long active0)
		{
			if (((active0 &= old0)) == 0L)
			{
				return JjStartNfa_0(1, old0);
			}
			try
			{
				curChar = input_stream.ReadChar();
			}
			catch (IOException)
			{
				JjStopStringLiteralDfa_0(2, active0);
				return 3;
			}
			switch (curChar)
			{
				case 41:
				{
					if ((active0 & unchecked((long)(0x10000000000000L))) != 0L)
					{
						return JjStopAtPos(3, 52);
					}
					break;
				}

				case 101:
				{
					return JjMoveStringLiteralDfa4_0(active0, unchecked((long)(0x8000000L)));
				}

				case 110:
				{
					return JjMoveStringLiteralDfa4_0(active0, unchecked((long)(0x40000000000000L)));
				}

				case 115:
				{
					return JjMoveStringLiteralDfa4_0(active0, unchecked((long)(0xc000000000L)));
				}

				case 116:
				{
					return JjMoveStringLiteralDfa4_0(active0, unchecked((long)(0x10000000L)));
				}

				default:
				{
					break;
				}
			}
			return JjStartNfa_0(2, active0);
		}

		private int JjMoveStringLiteralDfa4_0(long old0, long active0)
		{
			if (((active0 &= old0)) == 0L)
			{
				return JjStartNfa_0(2, old0);
			}
			try
			{
				curChar = input_stream.ReadChar();
			}
			catch (IOException)
			{
				JjStopStringLiteralDfa_0(3, active0);
				return 4;
			}
			switch (curChar)
			{
				case 58:
				{
					if ((active0 & unchecked((long)(0x10000000L))) != 0L)
					{
						return JjStopAtPos(4, 28);
					}
					break;
				}

				case 101:
				{
					if ((active0 & unchecked((long)(0x4000000000L))) != 0L)
					{
						return JjStopAtPos(4, 38);
					}
					else
					{
						if ((active0 & unchecked((long)(0x8000000000L))) != 0L)
						{
							return JjStopAtPos(4, 39);
						}
					}
					break;
				}

				case 110:
				{
					return JjMoveStringLiteralDfa5_0(active0, unchecked((long)(0x8000000L)));
				}

				case 111:
				{
					return JjMoveStringLiteralDfa5_0(active0, unchecked((long)(0x40000000000000L)));
				}

				default:
				{
					break;
				}
			}
			return JjStartNfa_0(3, active0);
		}

		private int JjMoveStringLiteralDfa5_0(long old0, long active0)
		{
			if (((active0 &= old0)) == 0L)
			{
				return JjStartNfa_0(3, old0);
			}
			try
			{
				curChar = input_stream.ReadChar();
			}
			catch (IOException)
			{
				JjStopStringLiteralDfa_0(4, active0);
				return 5;
			}
			switch (curChar)
			{
				case 115:
				{
					return JjMoveStringLiteralDfa6_0(active0, unchecked((long)(0x8000000L)));
				}

				case 116:
				{
					return JjMoveStringLiteralDfa6_0(active0, unchecked((long)(0x40000000000000L)));
				}

				default:
				{
					break;
				}
			}
			return JjStartNfa_0(4, active0);
		}

		private int JjMoveStringLiteralDfa6_0(long old0, long active0)
		{
			if (((active0 &= old0)) == 0L)
			{
				return JjStartNfa_0(4, old0);
			}
			try
			{
				curChar = input_stream.ReadChar();
			}
			catch (IOException)
			{
				JjStopStringLiteralDfa_0(5, active0);
				return 6;
			}
			switch (curChar)
			{
				case 58:
				{
					if ((active0 & unchecked((long)(0x8000000L))) != 0L)
					{
						return JjStopAtPos(6, 27);
					}
					break;
				}

				case 97:
				{
					return JjMoveStringLiteralDfa7_0(active0, unchecked((long)(0x40000000000000L)));
				}

				default:
				{
					break;
				}
			}
			return JjStartNfa_0(5, active0);
		}

		private int JjMoveStringLiteralDfa7_0(long old0, long active0)
		{
			if (((active0 &= old0)) == 0L)
			{
				return JjStartNfa_0(5, old0);
			}
			try
			{
				curChar = input_stream.ReadChar();
			}
			catch (IOException)
			{
				JjStopStringLiteralDfa_0(6, active0);
				return 7;
			}
			switch (curChar)
			{
				case 116:
				{
					return JjMoveStringLiteralDfa8_0(active0, unchecked((long)(0x40000000000000L)));
				}

				default:
				{
					break;
				}
			}
			return JjStartNfa_0(6, active0);
		}

		private int JjMoveStringLiteralDfa8_0(long old0, long active0)
		{
			if (((active0 &= old0)) == 0L)
			{
				return JjStartNfa_0(6, old0);
			}
			try
			{
				curChar = input_stream.ReadChar();
			}
			catch (IOException)
			{
				JjStopStringLiteralDfa_0(7, active0);
				return 8;
			}
			switch (curChar)
			{
				case 101:
				{
					if ((active0 & unchecked((long)(0x40000000000000L))) != 0L)
					{
						return JjStopAtPos(8, 54);
					}
					break;
				}

				default:
				{
					break;
				}
			}
			return JjStartNfa_0(7, active0);
		}

		private int JjStartNfaWithStates_0(int pos, int kind, int state)
		{
			jjmatchedKind = kind;
			jjmatchedPos = pos;
			try
			{
				curChar = input_stream.ReadChar();
			}
			catch (IOException)
			{
				return pos + 1;
			}
			return JjMoveNfa_0(state, pos + 1);
		}

		internal static readonly long[] jjbitVec0 = new long[] { unchecked((long)(0xfffffffffffffffeL)), unchecked((long)(0xffffffffffffffffL)), unchecked((long)(0xffffffffffffffffL)), unchecked((long)(0xffffffffffffffffL)) };

		internal static readonly long[] jjbitVec2 = new long[] { unchecked((long)(0x0L)), unchecked((long)(0x0L)), unchecked((long)(0xffffffffffffffffL)), unchecked((long)(0xffffffffffffffffL)) };

		private int JjMoveNfa_0(int startState, int curPos)
		{
			int startsAt = 0;
			jjnewStateCnt = 53;
			int i = 1;
			jjstateSet[0] = startState;
			int kind = unchecked((int)(0x7fffffff));
			for (; ; )
			{
				if (++jjround == unchecked((int)(0x7fffffff)))
				{
					ReInitRounds();
				}
				if (curChar < 64)
				{
					long l = 1L << curChar;
					do
					{
						switch (jjstateSet[--i])
						{
							case 53:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) != 0L)
								{
									if (kind > 21)
									{
										kind = 21;
									}
									JjCheckNAdd(32);
								}
								if ((unchecked((long)(0x3ff000000000000L)) & l) != 0L)
								{
									if (kind > 7)
									{
										kind = 7;
									}
									JjCheckNAdd(12);
								}
								break;
							}

							case 5:
							{
								if ((unchecked((long)(0x3ff200000000000L)) & l) != 0L)
								{
									JjCheckNAddTwoStates(15, 16);
								}
								else
								{
									if ((unchecked((long)(0x5000000000000000L)) & l) != 0L)
									{
										if (kind > 19)
										{
											kind = 19;
										}
									}
									else
									{
										if (curChar == 33)
										{
											JjCheckNAddTwoStates(28, 52);
										}
										else
										{
											if (curChar == 61)
											{
												JjCheckNAddTwoStates(28, 50);
											}
											else
											{
												if (curChar == 36)
												{
													JjAddStates(0, 1);
												}
												else
												{
													if (curChar == 34)
													{
														JjCheckNAddStates(2, 4);
													}
													else
													{
														if (curChar == 47)
														{
															JjCheckNAddStates(5, 7);
														}
														else
														{
															if (curChar == 35)
															{
																JjCheckNAddStates(8, 10);
															}
														}
													}
												}
											}
										}
									}
								}
								if ((unchecked((long)(0x3ff000000000000L)) & l) != 0L)
								{
									if (kind > 13)
									{
										kind = 13;
									}
									JjCheckNAddStates(11, 14);
								}
								else
								{
									if ((unchecked((long)(0x280000000000L)) & l) != 0L)
									{
										JjCheckNAddStates(15, 17);
									}
									else
									{
										if (curChar == 36)
										{
											JjCheckNAddTwoStates(34, 36);
										}
										else
										{
											if (curChar == 62)
											{
												JjCheckNAdd(28);
											}
											else
											{
												if (curChar == 60)
												{
													JjCheckNAdd(28);
												}
												else
												{
													if (curChar == 47)
													{
														jjstateSet[jjnewStateCnt++] = 0;
													}
												}
											}
										}
									}
								}
								if ((unchecked((long)(0x3ff000000000000L)) & l) != 0L)
								{
									if (kind > 21)
									{
										kind = 21;
									}
									JjCheckNAdd(32);
								}
								break;
							}

							case 34:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) != 0L)
								{
									if (kind > 9)
									{
										kind = 9;
									}
									JjCheckNAdd(36);
								}
								else
								{
									if (curChar == 36)
									{
										JjCheckNAdd(42);
									}
								}
								if (curChar == 36)
								{
									jjstateSet[jjnewStateCnt++] = 39;
								}
								break;
							}

							case 50:
							case 28:
							{
								if (curChar == 61 && kind > 19)
								{
									kind = 19;
								}
								break;
							}

							case 54:
							{
								if ((unchecked((long)(0x3ff200000000000L)) & l) != 0L)
								{
									JjCheckNAddTwoStates(15, 16);
								}
								if ((unchecked((long)(0x3ff000000000000L)) & l) != 0L)
								{
									JjCheckNAddTwoStates(46, 47);
								}
								if ((unchecked((long)(0x3ff000000000000L)) & l) != 0L)
								{
									if (kind > 14)
									{
										kind = 14;
									}
									JjCheckNAdd(45);
								}
								break;
							}

							case 52:
							{
								if (curChar == 61)
								{
									if (kind > 20)
									{
										kind = 20;
									}
								}
								if (curChar == 61)
								{
									if (kind > 19)
									{
										kind = 19;
									}
								}
								break;
							}

							case 0:
							{
								if (curChar == 47)
								{
									JjCheckNAddStates(18, 20);
								}
								break;
							}

							case 1:
							{
								if ((unchecked((long)(0xffffffffffffdbffL)) & l) != 0L)
								{
									JjCheckNAddStates(18, 20);
								}
								break;
							}

							case 2:
							{
								if ((unchecked((long)(0x2400L)) & l) != 0L && kind > 5)
								{
									kind = 5;
								}
								break;
							}

							case 3:
							{
								if (curChar == 10 && kind > 5)
								{
									kind = 5;
								}
								break;
							}

							case 4:
							{
								if (curChar == 13)
								{
									jjstateSet[jjnewStateCnt++] = 3;
								}
								break;
							}

							case 6:
							{
								if (curChar == 35)
								{
									JjCheckNAddStates(8, 10);
								}
								break;
							}

							case 7:
							{
								if ((unchecked((long)(0xffffffffffffdbffL)) & l) != 0L)
								{
									JjCheckNAddStates(8, 10);
								}
								break;
							}

							case 8:
							{
								if ((unchecked((long)(0x2400L)) & l) != 0L && kind > 6)
								{
									kind = 6;
								}
								break;
							}

							case 9:
							{
								if (curChar == 10 && kind > 6)
								{
									kind = 6;
								}
								break;
							}

							case 10:
							{
								if (curChar == 13)
								{
									jjstateSet[jjnewStateCnt++] = 9;
								}
								break;
							}

							case 12:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 7)
								{
									kind = 7;
								}
								JjCheckNAdd(12);
								break;
							}

							case 14:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 12)
								{
									kind = 12;
								}
								jjstateSet[jjnewStateCnt++] = 14;
								break;
							}

							case 15:
							{
								if ((unchecked((long)(0x3ff200000000000L)) & l) != 0L)
								{
									JjCheckNAddTwoStates(15, 16);
								}
								break;
							}

							case 16:
							{
								if (curChar == 46)
								{
									JjCheckNAdd(17);
								}
								break;
							}

							case 17:
							{
								if ((unchecked((long)(0x3ff200000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 16)
								{
									kind = 16;
								}
								JjCheckNAdd(17);
								break;
							}

							case 18:
							case 19:
							{
								if (curChar == 47)
								{
									JjCheckNAddStates(5, 7);
								}
								break;
							}

							case 21:
							{
								if ((unchecked((long)(0xffff7fffffffdbffL)) & l) != 0L)
								{
									JjCheckNAddStates(5, 7);
								}
								break;
							}

							case 22:
							{
								if (curChar == 47 && kind > 17)
								{
									kind = 17;
								}
								break;
							}

							case 23:
							case 24:
							{
								if (curChar == 34)
								{
									JjCheckNAddStates(2, 4);
								}
								break;
							}

							case 26:
							{
								if ((unchecked((long)(0xfffffffbffffdbffL)) & l) != 0L)
								{
									JjCheckNAddStates(2, 4);
								}
								break;
							}

							case 27:
							{
								if (curChar == 34 && kind > 18)
								{
									kind = 18;
								}
								break;
							}

							case 29:
							{
								if (curChar == 60)
								{
									JjCheckNAdd(28);
								}
								break;
							}

							case 30:
							{
								if (curChar == 62)
								{
									JjCheckNAdd(28);
								}
								break;
							}

							case 31:
							{
								if ((unchecked((long)(0x5000000000000000L)) & l) != 0L && kind > 19)
								{
									kind = 19;
								}
								break;
							}

							case 32:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 21)
								{
									kind = 21;
								}
								JjCheckNAdd(32);
								break;
							}

							case 33:
							{
								if (curChar == 36)
								{
									JjCheckNAddTwoStates(34, 36);
								}
								break;
							}

							case 35:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 8)
								{
									kind = 8;
								}
								jjstateSet[jjnewStateCnt++] = 35;
								break;
							}

							case 36:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 9)
								{
									kind = 9;
								}
								JjCheckNAdd(36);
								break;
							}

							case 37:
							{
								if (curChar == 36)
								{
									JjAddStates(0, 1);
								}
								break;
							}

							case 38:
							{
								if (curChar == 36)
								{
									jjstateSet[jjnewStateCnt++] = 39;
								}
								break;
							}

							case 40:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 10)
								{
									kind = 10;
								}
								jjstateSet[jjnewStateCnt++] = 40;
								break;
							}

							case 41:
							{
								if (curChar == 36)
								{
									JjCheckNAdd(42);
								}
								break;
							}

							case 42:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 11)
								{
									kind = 11;
								}
								JjCheckNAdd(42);
								break;
							}

							case 43:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 13)
								{
									kind = 13;
								}
								JjCheckNAddStates(11, 14);
								break;
							}

							case 44:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 13)
								{
									kind = 13;
								}
								JjCheckNAdd(44);
								break;
							}

							case 45:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 14)
								{
									kind = 14;
								}
								JjCheckNAdd(45);
								break;
							}

							case 46:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) != 0L)
								{
									JjCheckNAddTwoStates(46, 47);
								}
								break;
							}

							case 48:
							{
								if ((unchecked((long)(0x280000000000L)) & l) != 0L)
								{
									JjCheckNAddStates(15, 17);
								}
								break;
							}

							case 49:
							{
								if (curChar == 61)
								{
									JjCheckNAddTwoStates(28, 50);
								}
								break;
							}

							case 51:
							{
								if (curChar == 33)
								{
									JjCheckNAddTwoStates(28, 52);
								}
								break;
							}

							default:
							{
								break;
							}
						}
					}
					while (i != startsAt);
				}
				else
				{
					if (curChar < 128)
					{
						long l = 1L << (curChar & 0x3f);
						do
						{
							switch (jjstateSet[--i])
							{
								case 53:
								{
									if ((unchecked((long)(0x7fffffe87fffffeL)) & l) != 0L)
									{
										if (kind > 21)
										{
											kind = 21;
										}
										JjCheckNAdd(32);
									}
									if ((unchecked((long)(0x7fffffe87fffffeL)) & l) != 0L)
									{
										if (kind > 7)
										{
											kind = 7;
										}
										JjCheckNAdd(12);
									}
									break;
								}

								case 5:
								{
									if ((unchecked((long)(0x7fffffe87fffffeL)) & l) != 0L)
									{
										if (kind > 21)
										{
											kind = 21;
										}
										JjCheckNAdd(32);
									}
									else
									{
										if (curChar == 92)
										{
											jjstateSet[jjnewStateCnt++] = 14;
										}
									}
									if ((unchecked((long)(0x7fffffe87fffffeL)) & l) != 0L)
									{
										if (kind > 7)
										{
											kind = 7;
										}
										JjCheckNAdd(12);
									}
									if (curChar == 69)
									{
										JjCheckNAddTwoStates(15, 16);
									}
									break;
								}

								case 34:
								case 35:
								{
									if ((unchecked((long)(0x7fffffe87fffffeL)) & l) == 0L)
									{
										break;
									}
									if (kind > 8)
									{
										kind = 8;
									}
									JjCheckNAdd(35);
									break;
								}

								case 50:
								{
									if (curChar == 126 && kind > 20)
									{
										kind = 20;
									}
									break;
								}

								case 54:
								case 15:
								{
									if (curChar == 69)
									{
										JjCheckNAddTwoStates(15, 16);
									}
									break;
								}

								case 1:
								{
									JjAddStates(18, 20);
									break;
								}

								case 7:
								{
									JjAddStates(8, 10);
									break;
								}

								case 11:
								{
									if ((unchecked((long)(0x7fffffe87fffffeL)) & l) == 0L)
									{
										break;
									}
									if (kind > 7)
									{
										kind = 7;
									}
									JjCheckNAdd(12);
									break;
								}

								case 12:
								{
									if ((unchecked((long)(0x7fffffe87fffffeL)) & l) == 0L)
									{
										break;
									}
									if (kind > 7)
									{
										kind = 7;
									}
									JjCheckNAdd(12);
									break;
								}

								case 13:
								{
									if (curChar == 92)
									{
										jjstateSet[jjnewStateCnt++] = 14;
									}
									break;
								}

								case 17:
								{
									if (curChar != 69)
									{
										break;
									}
									if (kind > 16)
									{
										kind = 16;
									}
									jjstateSet[jjnewStateCnt++] = 17;
									break;
								}

								case 20:
								{
									if (curChar == 92)
									{
										jjstateSet[jjnewStateCnt++] = 19;
									}
									break;
								}

								case 21:
								{
									JjAddStates(5, 7);
									break;
								}

								case 25:
								{
									if (curChar == 92)
									{
										jjstateSet[jjnewStateCnt++] = 24;
									}
									break;
								}

								case 26:
								{
									JjAddStates(2, 4);
									break;
								}

								case 32:
								{
									if ((unchecked((long)(0x7fffffe87fffffeL)) & l) == 0L)
									{
										break;
									}
									if (kind > 21)
									{
										kind = 21;
									}
									JjCheckNAdd(32);
									break;
								}

								case 39:
								case 40:
								{
									if ((unchecked((long)(0x7fffffe87fffffeL)) & l) == 0L)
									{
										break;
									}
									if (kind > 10)
									{
										kind = 10;
									}
									JjCheckNAdd(40);
									break;
								}

								case 47:
								{
									if (curChar == 76 && kind > 15)
									{
										kind = 15;
									}
									break;
								}

								default:
								{
									break;
								}
							}
						}
						while (i != startsAt);
					}
					else
					{
						int hiByte = ((int)curChar) >> 8;
						int i1 = hiByte >> 6;
						long l1 = 1L << (hiByte & 0x3f);
						int i2 = (curChar & unchecked((int)(0xff))) >> 6;
						long l2 = 1L << (curChar & 0x3f);
						do
						{
							switch (jjstateSet[--i])
							{
								case 1:
								{
									if (JjCanMove_0(hiByte, i1, i2, l1, l2))
									{
										JjAddStates(18, 20);
									}
									break;
								}

								case 7:
								{
									if (JjCanMove_0(hiByte, i1, i2, l1, l2))
									{
										JjAddStates(8, 10);
									}
									break;
								}

								case 21:
								{
									if (JjCanMove_0(hiByte, i1, i2, l1, l2))
									{
										JjAddStates(5, 7);
									}
									break;
								}

								case 26:
								{
									if (JjCanMove_0(hiByte, i1, i2, l1, l2))
									{
										JjAddStates(2, 4);
									}
									break;
								}

								default:
								{
									break;
								}
							}
						}
						while (i != startsAt);
					}
				}
				if (kind != unchecked((int)(0x7fffffff)))
				{
					jjmatchedKind = kind;
					jjmatchedPos = curPos;
					kind = unchecked((int)(0x7fffffff));
				}
				++curPos;
				if ((i = jjnewStateCnt) == (startsAt = 53 - (jjnewStateCnt = startsAt)))
				{
					return curPos;
				}
				try
				{
					curChar = input_stream.ReadChar();
				}
				catch (IOException)
				{
					return curPos;
				}
			}
		}

		internal static readonly int[] jjnextStates = new int[] { 38, 41, 25, 26, 27, 20, 21, 22, 7, 8, 10, 44, 45, 46, 47, 45, 46, 15, 1, 2, 4 };

		private static bool JjCanMove_0(int hiByte, int i1, int i2, long l1, long l2)
		{
			switch (hiByte)
			{
				case 0:
				{
					return ((jjbitVec2[i2] & l2) != 0L);
				}

				default:
				{
					if ((jjbitVec0[i1] & l1) != 0L)
					{
						return true;
					}
					return false;
				}
			}
		}

		/// <summary>Token literal values.</summary>
		public static readonly string[] jjstrLiteralImages = new string[] { string.Empty, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "\xad", "\x4b\x4c", "\xaf", "\x32"
			, "\x33", "\xa4\x9d\x99\x91\x9c\xa3\x48", "\xa4\x91\xaa\xa4\x48", "\x4b", "\x49", "\x85", "\x87", "\x36", "\x48", "\x38", "\x2e\x2e", "\xae\xae", "\x48\x8f\x8d\xa3\x91", "\x48\x91\x9a\xa3\x91", "\x88", "\x2c", "\x34", "\x4d", "\x35", "\xae"
			, "\x2e", "\x4d\x48", "\x29", "\xad\xad", "\xaf\xaf", "\x48\x48", "\x32\x4d\x9b\x33", "\x4b\x4b\x4c", "\x2e\x8d\x9c\x9c\x9d\xa4\x8d\xa4\x91" };

		/// <summary>Lexer state names.</summary>
		public static readonly string[] lexStateNames = new string[] { "DEFAULT" };

		internal static readonly long[] jjtoToken = new long[] { unchecked((long)(0x7fffffffffff81L)) };

		internal static readonly long[] jjtoSkip = new long[] { unchecked((long)(0x7eL)) };

		protected internal SimpleCharStream input_stream;

		private readonly int[] jjrounds = new int[53];

		private readonly int[] jjstateSet = new int[106];

		protected internal char curChar;

		/// <summary>Constructor.</summary>
		public TokenSequenceParserTokenManager(SimpleCharStream stream)
		{
			input_stream = stream;
		}

		/// <summary>Constructor.</summary>
		public TokenSequenceParserTokenManager(SimpleCharStream stream, int lexState)
			: this(stream)
		{
			SwitchTo(lexState);
		}

		/// <summary>Reinitialise parser.</summary>
		public virtual void ReInit(SimpleCharStream stream)
		{
			jjmatchedPos = jjnewStateCnt = 0;
			curLexState = defaultLexState;
			input_stream = stream;
			ReInitRounds();
		}

		private void ReInitRounds()
		{
			int i;
			jjround = unchecked((int)(0x80000001));
			for (i = 53; i-- > 0; )
			{
				jjrounds[i] = unchecked((int)(0x80000000));
			}
		}

		/// <summary>Reinitialise parser.</summary>
		public virtual void ReInit(SimpleCharStream stream, int lexState)
		{
			ReInit(stream);
			SwitchTo(lexState);
		}

		/// <summary>Switch to specified lex state.</summary>
		public virtual void SwitchTo(int lexState)
		{
			if (lexState >= 1 || lexState < 0)
			{
				throw new TokenMgrError("Error: Ignoring invalid lexical state : " + lexState + ". State unchanged.", TokenMgrError.InvalidLexicalState);
			}
			else
			{
				curLexState = lexState;
			}
		}

		protected internal virtual Token JjFillToken()
		{
			Token t;
			string curTokenImage;
			int beginLine;
			int endLine;
			int beginColumn;
			int endColumn;
			string im = jjstrLiteralImages[jjmatchedKind];
			curTokenImage = (im == null) ? input_stream.GetImage() : im;
			beginLine = input_stream.GetBeginLine();
			beginColumn = input_stream.GetBeginColumn();
			endLine = input_stream.GetEndLine();
			endColumn = input_stream.GetEndColumn();
			t = Token.NewToken(jjmatchedKind, curTokenImage);
			t.beginLine = beginLine;
			t.endLine = endLine;
			t.beginColumn = beginColumn;
			t.endColumn = endColumn;
			return t;
		}

		internal int curLexState = 0;

		internal int defaultLexState = 0;

		internal int jjnewStateCnt;

		internal int jjround;

		internal int jjmatchedPos;

		internal int jjmatchedKind;

		/// <summary>Get the next Token.</summary>
		public virtual Token GetNextToken()
		{
			Token matchedToken;
			int curPos = 0;
			for (; ; )
			{
				try
				{
					curChar = input_stream.BeginToken();
				}
				catch (IOException)
				{
					jjmatchedKind = 0;
					matchedToken = JjFillToken();
					return matchedToken;
				}
				try
				{
					input_stream.Backup(0);
					while (curChar <= 32 && (unchecked((long)(0x100002600L)) & (1L << curChar)) != 0L)
					{
						curChar = input_stream.BeginToken();
					}
				}
				catch (IOException)
				{
					goto EOFLoop_continue;
				}
				jjmatchedKind = unchecked((int)(0x7fffffff));
				jjmatchedPos = 0;
				curPos = JjMoveStringLiteralDfa0_0();
				if (jjmatchedKind != unchecked((int)(0x7fffffff)))
				{
					if (jjmatchedPos + 1 < curPos)
					{
						input_stream.Backup(curPos - jjmatchedPos - 1);
					}
					if ((jjtoToken[jjmatchedKind >> 6] & (1L << (jjmatchedKind & 0x3f))) != 0L)
					{
						matchedToken = JjFillToken();
						return matchedToken;
					}
					else
					{
						goto EOFLoop_continue;
					}
				}
				int error_line = input_stream.GetEndLine();
				int error_column = input_stream.GetEndColumn();
				string error_after = null;
				bool EOFSeen = false;
				try
				{
					input_stream.ReadChar();
					input_stream.Backup(1);
				}
				catch (IOException)
				{
					EOFSeen = true;
					error_after = curPos <= 1 ? string.Empty : input_stream.GetImage();
					if (curChar == '\n' || curChar == '\r')
					{
						error_line++;
						error_column = 0;
					}
					else
					{
						error_column++;
					}
				}
				if (!EOFSeen)
				{
					input_stream.Backup(1);
					error_after = curPos <= 1 ? string.Empty : input_stream.GetImage();
				}
				throw new TokenMgrError(EOFSeen, curLexState, error_line, error_column, error_after, curChar, TokenMgrError.LexicalError);
EOFLoop_continue: ;
			}
EOFLoop_break: ;
		}

		private void JjCheckNAdd(int state)
		{
			if (jjrounds[state] != jjround)
			{
				jjstateSet[jjnewStateCnt++] = state;
				jjrounds[state] = jjround;
			}
		}

		private void JjAddStates(int start, int end)
		{
			do
			{
				jjstateSet[jjnewStateCnt++] = jjnextStates[start];
			}
			while (start++ != end);
		}

		private void JjCheckNAddTwoStates(int state1, int state2)
		{
			JjCheckNAdd(state1);
			JjCheckNAdd(state2);
		}

		private void JjCheckNAddStates(int start, int end)
		{
			do
			{
				JjCheckNAdd(jjnextStates[start]);
			}
			while (start++ != end);
		}
	}
}
