/* TregexParser.java */
/* Generated By:JavaCC: Do not edit this line. TregexParser.java */
using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Trees.Tregex
{
	internal class TregexParser : ITregexParserConstants
	{
		private bool underNegation = false;

		private Func<string, string> basicCatFunction = TregexPatternCompiler.DefaultBasicCatFunction;

		private IHeadFinder headFinder = TregexPatternCompiler.DefaultHeadFinder;

		private ICollection<string> knownVariables = Generics.NewHashSet();

		public TregexParser(Reader stream, Func<string, string> basicCatFunction, IHeadFinder headFinder)
			: this(stream)
		{
			// all generated classes are in this package
			// this is so we can tell, at any point during the parse
			// whether we are under a negation, which we need to know
			// because labeling nodes under negation is illegal
			// keep track of which variables we've seen, so that we can reject
			// some nonsense patterns such as ones that reset variables or link
			// to variables that haven't been set
			this.basicCatFunction = basicCatFunction;
			this.headFinder = headFinder;
		}

		// TODO: IDENTIFIER should not allow | after the first character, but
		// it breaks some | queries to allow it.  We should fix that.
		// the grammar starts here
		// each of these BNF rules will be converted into a function
		// first expr is return val- passed up the tree after a production
		/// <exception cref="Edu.Stanford.Nlp.Trees.Tregex.ParseException"/>
		public TregexPattern Root()
		{
			TregexPattern node;
			IList<TregexPattern> nodes = Generics.NewArrayList();
			// a local variable
			node = SubNode(Edu.Stanford.Nlp.Trees.Tregex.Relation.Root);
			nodes.Add(node);
			while (true)
			{
				if (Jj_2_1(2))
				{
				}
				else
				{
					goto label_1_break;
				}
				Jj_consume_token(12);
				node = SubNode(Edu.Stanford.Nlp.Trees.Tregex.Relation.Root);
				nodes.Add(node);
label_1_continue: ;
			}
label_1_break: ;
			Jj_consume_token(13);
			if (nodes.Count == 1)
			{
				{
					if (string.Empty != null)
					{
						return nodes[0];
					}
				}
			}
			else
			{
				{
					if (string.Empty != null)
					{
						return new CoordinationPattern(nodes, false);
					}
				}
			}
			throw new Exception("Missing return statement in function");
		}

		// passing arguments down the tree - in this case the relation that
		// pertains to this node gets passed all the way down to the Description node
		/// <exception cref="Edu.Stanford.Nlp.Trees.Tregex.ParseException"/>
		public DescriptionPattern Node(Edu.Stanford.Nlp.Trees.Tregex.Relation r)
		{
			DescriptionPattern node;
			switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
			{
				case 14:
				{
					Jj_consume_token(14);
					node = SubNode(r);
					Jj_consume_token(15);
					break;
				}

				case TregexParserConstantsConstants.Identifier:
				case TregexParserConstantsConstants.Blank:
				case TregexParserConstantsConstants.Regex:
				case 16:
				case 17:
				case 20:
				case 21:
				{
					node = ModDescription(r);
					break;
				}

				default:
				{
					jj_la1[0] = jj_gen;
					Jj_consume_token(-1);
					throw new ParseException();
				}
			}
			{
				if (string.Empty != null)
				{
					return node;
				}
			}
			throw new Exception("Missing return statement in function");
		}

		/// <exception cref="Edu.Stanford.Nlp.Trees.Tregex.ParseException"/>
		public DescriptionPattern SubNode(Edu.Stanford.Nlp.Trees.Tregex.Relation r)
		{
			DescriptionPattern result = null;
			TregexPattern child = null;
			switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
			{
				case 14:
				{
					Jj_consume_token(14);
					result = SubNode(r);
					Jj_consume_token(15);
					switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
					{
						case TregexParserConstantsConstants.Relation:
						case TregexParserConstantsConstants.MultiRelation:
						case TregexParserConstantsConstants.RelWStrArg:
						case 14:
						case 16:
						case 23:
						case 24:
						{
							child = ChildrenDisj();
							break;
						}

						default:
						{
							jj_la1[1] = jj_gen;
							break;
						}
					}
					if (child != null)
					{
						IList<TregexPattern> newChildren = new List<TregexPattern>();
						Sharpen.Collections.AddAll(newChildren, result.GetChildren());
						newChildren.Add(child);
						result.SetChild(new CoordinationPattern(newChildren, true));
					}
					{
						if (string.Empty != null)
						{
							return result;
						}
					}
					break;
				}

				case TregexParserConstantsConstants.Identifier:
				case TregexParserConstantsConstants.Blank:
				case TregexParserConstantsConstants.Regex:
				case 16:
				case 17:
				case 20:
				case 21:
				{
					result = ModDescription(r);
					switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
					{
						case TregexParserConstantsConstants.Relation:
						case TregexParserConstantsConstants.MultiRelation:
						case TregexParserConstantsConstants.RelWStrArg:
						case 14:
						case 16:
						case 23:
						case 24:
						{
							child = ChildrenDisj();
							break;
						}

						default:
						{
							jj_la1[2] = jj_gen;
							break;
						}
					}
					if (child != null)
					{
						result.SetChild(child);
					}
					{
						if (string.Empty != null)
						{
							return result;
						}
					}
					break;
				}

				default:
				{
					jj_la1[3] = jj_gen;
					Jj_consume_token(-1);
					throw new ParseException();
				}
			}
			throw new Exception("Missing return statement in function");
		}

		/// <exception cref="Edu.Stanford.Nlp.Trees.Tregex.ParseException"/>
		public DescriptionPattern ModDescription(Edu.Stanford.Nlp.Trees.Tregex.Relation r)
		{
			DescriptionPattern node;
			bool neg = false;
			bool cat = false;
			switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
			{
				case 16:
				{
					Jj_consume_token(16);
					neg = true;
					break;
				}

				default:
				{
					jj_la1[4] = jj_gen;
					break;
				}
			}
			switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
			{
				case 17:
				{
					Jj_consume_token(17);
					cat = true;
					break;
				}

				default:
				{
					jj_la1[5] = jj_gen;
					break;
				}
			}
			node = Description(r, neg, cat);
			{
				if (string.Empty != null)
				{
					return node;
				}
			}
			throw new Exception("Missing return statement in function");
		}

		/// <exception cref="Edu.Stanford.Nlp.Trees.Tregex.ParseException"/>
		public DescriptionPattern Description(Edu.Stanford.Nlp.Trees.Tregex.Relation r, bool negateDesc, bool cat)
		{
			Token desc = null;
			Token name = null;
			Token linkedName = null;
			bool link = false;
			Token groupNum;
			Token groupVar;
			IList<Pair<int, string>> varGroups = new List<Pair<int, string>>();
			switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
			{
				case TregexParserConstantsConstants.Identifier:
				case TregexParserConstantsConstants.Blank:
				case TregexParserConstantsConstants.Regex:
				{
					switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
					{
						case TregexParserConstantsConstants.Identifier:
						{
							desc = Jj_consume_token(TregexParserConstantsConstants.Identifier);
							break;
						}

						case TregexParserConstantsConstants.Regex:
						{
							desc = Jj_consume_token(TregexParserConstantsConstants.Regex);
							break;
						}

						case TregexParserConstantsConstants.Blank:
						{
							desc = Jj_consume_token(TregexParserConstantsConstants.Blank);
							break;
						}

						default:
						{
							jj_la1[6] = jj_gen;
							Jj_consume_token(-1);
							throw new ParseException();
						}
					}
					while (true)
					{
						switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
						{
							case 18:
							{
								break;
							}

							default:
							{
								jj_la1[7] = jj_gen;
								goto label_2_break;
							}
						}
						Jj_consume_token(18);
						groupNum = Jj_consume_token(TregexParserConstantsConstants.Number);
						Jj_consume_token(19);
						groupVar = Jj_consume_token(TregexParserConstantsConstants.Identifier);
						varGroups.Add(new Pair<int, string>(System.Convert.ToInt32(groupNum.image), groupVar.image));
label_2_continue: ;
					}
label_2_break: ;
					switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
					{
						case 20:
						{
							Jj_consume_token(20);
							name = Jj_consume_token(TregexParserConstantsConstants.Identifier);
							if (knownVariables.Contains(name.image))
							{
								{
									if (true)
									{
										throw new ParseException("Variable " + name.image + " has been declared twice, which makes no sense");
									}
								}
							}
							else
							{
								knownVariables.Add(name.image);
							}
							if (underNegation)
							{
								if (true)
								{
									throw new ParseException("No named tregex nodes allowed in the scope of negation.");
								}
							}
							break;
						}

						default:
						{
							jj_la1[8] = jj_gen;
							break;
						}
					}
					break;
				}

				case 21:
				{
					Jj_consume_token(21);
					linkedName = Jj_consume_token(TregexParserConstantsConstants.Identifier);
					switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
					{
						case 20:
						{
							Jj_consume_token(20);
							name = Jj_consume_token(TregexParserConstantsConstants.Identifier);
							break;
						}

						default:
						{
							jj_la1[9] = jj_gen;
							break;
						}
					}
					if (!knownVariables.Contains(linkedName.image))
					{
						{
							if (true)
							{
								throw new ParseException("Variable " + linkedName.image + " was referenced before it was declared");
							}
						}
					}
					if (name != null)
					{
						if (knownVariables.Contains(name.image))
						{
							{
								if (true)
								{
									throw new ParseException("Variable " + name.image + " has been declared twice, which makes no sense");
								}
							}
						}
						else
						{
							knownVariables.Add(name.image);
						}
					}
					link = true;
					break;
				}

				case 20:
				{
					Jj_consume_token(20);
					name = Jj_consume_token(TregexParserConstantsConstants.Identifier);
					if (!knownVariables.Contains(name.image))
					{
						{
							if (true)
							{
								throw new ParseException("Variable " + name.image + " was referenced before it was declared");
							}
						}
					}
					break;
				}

				default:
				{
					jj_la1[10] = jj_gen;
					Jj_consume_token(-1);
					throw new ParseException();
				}
			}
			DescriptionPattern ret = new DescriptionPattern(r, negateDesc, desc != null ? desc.image : null, name != null ? name.image : null, cat, basicCatFunction, varGroups, link, linkedName != null ? linkedName.image : null);
			{
				if (string.Empty != null)
				{
					return ret;
				}
			}
			throw new Exception("Missing return statement in function");
		}

		/// <exception cref="Edu.Stanford.Nlp.Trees.Tregex.ParseException"/>
		public TregexPattern ChildrenDisj()
		{
			TregexPattern child;
			IList<TregexPattern> children = new List<TregexPattern>();
			// When we keep track of the known variables to assert that
			// variables are not redefined, or that links are only set to known
			// variables, we want to separate those done in different parts of the
			// disjunction.  Variables set in one part won't be set in the next
			// part if it gets there, since disjunctions exit once known.
			ICollection<string> originalKnownVariables = Generics.NewHashSet(knownVariables);
			// However, we want to keep track of all the known variables, so that after
			// the disjunction is over, we know them all.
			ICollection<string> allKnownVariables = Generics.NewHashSet(knownVariables);
			child = ChildrenConj();
			children.Add(child);
			Sharpen.Collections.AddAll(allKnownVariables, knownVariables);
			while (true)
			{
				if (Jj_2_2(2))
				{
				}
				else
				{
					goto label_3_break;
				}
				knownVariables = Generics.NewHashSet(originalKnownVariables);
				Jj_consume_token(12);
				child = ChildrenConj();
				children.Add(child);
				Sharpen.Collections.AddAll(allKnownVariables, knownVariables);
label_3_continue: ;
			}
label_3_break: ;
			knownVariables = allKnownVariables;
			if (children.Count == 1)
			{
				if (string.Empty != null)
				{
					return child;
				}
			}
			else
			{
				if (string.Empty != null)
				{
					return new CoordinationPattern(children, false);
				}
			}
			throw new Exception("Missing return statement in function");
		}

		/// <exception cref="Edu.Stanford.Nlp.Trees.Tregex.ParseException"/>
		public TregexPattern ChildrenConj()
		{
			TregexPattern child;
			IList<TregexPattern> children = new List<TregexPattern>();
			child = ModChild();
			children.Add(child);
			while (true)
			{
				switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
				{
					case TregexParserConstantsConstants.Relation:
					case TregexParserConstantsConstants.MultiRelation:
					case TregexParserConstantsConstants.RelWStrArg:
					case 14:
					case 16:
					case 22:
					case 23:
					case 24:
					{
						break;
					}

					default:
					{
						jj_la1[11] = jj_gen;
						goto label_4_break;
					}
				}
				switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
				{
					case 22:
					{
						Jj_consume_token(22);
						break;
					}

					default:
					{
						jj_la1[12] = jj_gen;
						break;
					}
				}
				child = ModChild();
				children.Add(child);
label_4_continue: ;
			}
label_4_break: ;
			if (children.Count == 1)
			{
				if (string.Empty != null)
				{
					return child;
				}
			}
			else
			{
				if (string.Empty != null)
				{
					return new CoordinationPattern(children, true);
				}
			}
			throw new Exception("Missing return statement in function");
		}

		/// <exception cref="Edu.Stanford.Nlp.Trees.Tregex.ParseException"/>
		public TregexPattern ModChild()
		{
			TregexPattern child;
			bool startUnderNeg;
			switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
			{
				case TregexParserConstantsConstants.Relation:
				case TregexParserConstantsConstants.MultiRelation:
				case TregexParserConstantsConstants.RelWStrArg:
				case 14:
				case 24:
				{
					child = Child();
					break;
				}

				case 16:
				{
					Jj_consume_token(16);
					startUnderNeg = underNegation;
					underNegation = true;
					child = ModChild();
					underNegation = startUnderNeg;
					child.Negate();
					break;
				}

				case 23:
				{
					Jj_consume_token(23);
					child = Child();
					child.MakeOptional();
					break;
				}

				default:
				{
					jj_la1[13] = jj_gen;
					Jj_consume_token(-1);
					throw new ParseException();
				}
			}
			{
				if (string.Empty != null)
				{
					return child;
				}
			}
			throw new Exception("Missing return statement in function");
		}

		/// <exception cref="Edu.Stanford.Nlp.Trees.Tregex.ParseException"/>
		public TregexPattern Child()
		{
			TregexPattern child;
			switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
			{
				case 24:
				{
					Jj_consume_token(24);
					child = ChildrenDisj();
					Jj_consume_token(25);
					break;
				}

				case 14:
				{
					Jj_consume_token(14);
					child = ChildrenDisj();
					Jj_consume_token(15);
					break;
				}

				case TregexParserConstantsConstants.Relation:
				case TregexParserConstantsConstants.MultiRelation:
				case TregexParserConstantsConstants.RelWStrArg:
				{
					child = Relation();
					break;
				}

				default:
				{
					jj_la1[14] = jj_gen;
					Jj_consume_token(-1);
					throw new ParseException();
				}
			}
			{
				if (string.Empty != null)
				{
					return child;
				}
			}
			throw new Exception("Missing return statement in function");
		}

		/// <exception cref="Edu.Stanford.Nlp.Trees.Tregex.ParseException"/>
		public TregexPattern Relation()
		{
			Token t;
			Token strArg = null;
			Token numArg = null;
			Token negation = null;
			Token cat = null;
			// the easiest way to check if an optional production was used
			// is to set the token to null and then check it later
			Edu.Stanford.Nlp.Trees.Tregex.Relation r;
			DescriptionPattern child;
			IList<DescriptionPattern> children = Generics.NewArrayList();
			switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
			{
				case TregexParserConstantsConstants.Relation:
				case TregexParserConstantsConstants.RelWStrArg:
				{
					switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
					{
						case TregexParserConstantsConstants.Relation:
						{
							t = Jj_consume_token(TregexParserConstantsConstants.Relation);
							switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
							{
								case TregexParserConstantsConstants.Number:
								{
									numArg = Jj_consume_token(TregexParserConstantsConstants.Number);
									break;
								}

								default:
								{
									jj_la1[15] = jj_gen;
									break;
								}
							}
							break;
						}

						case TregexParserConstantsConstants.RelWStrArg:
						{
							t = Jj_consume_token(TregexParserConstantsConstants.RelWStrArg);
							switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
							{
								case 14:
								{
									Jj_consume_token(14);
									switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
									{
										case 16:
										{
											negation = Jj_consume_token(16);
											break;
										}

										default:
										{
											jj_la1[16] = jj_gen;
											break;
										}
									}
									switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
									{
										case 17:
										{
											cat = Jj_consume_token(17);
											break;
										}

										default:
										{
											jj_la1[17] = jj_gen;
											break;
										}
									}
									switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
									{
										case TregexParserConstantsConstants.Regex:
										{
											strArg = Jj_consume_token(TregexParserConstantsConstants.Regex);
											break;
										}

										case TregexParserConstantsConstants.Identifier:
										{
											strArg = Jj_consume_token(TregexParserConstantsConstants.Identifier);
											break;
										}

										case TregexParserConstantsConstants.Blank:
										{
											strArg = Jj_consume_token(TregexParserConstantsConstants.Blank);
											break;
										}

										default:
										{
											jj_la1[18] = jj_gen;
											Jj_consume_token(-1);
											throw new ParseException();
										}
									}
									Jj_consume_token(15);
									break;
								}

								case 24:
								{
									Jj_consume_token(24);
									switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
									{
										case 16:
										{
											negation = Jj_consume_token(16);
											break;
										}

										default:
										{
											jj_la1[19] = jj_gen;
											break;
										}
									}
									switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
									{
										case 17:
										{
											cat = Jj_consume_token(17);
											break;
										}

										default:
										{
											jj_la1[20] = jj_gen;
											break;
										}
									}
									switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
									{
										case TregexParserConstantsConstants.Regex:
										{
											strArg = Jj_consume_token(TregexParserConstantsConstants.Regex);
											break;
										}

										case TregexParserConstantsConstants.Identifier:
										{
											strArg = Jj_consume_token(TregexParserConstantsConstants.Identifier);
											break;
										}

										case TregexParserConstantsConstants.Blank:
										{
											strArg = Jj_consume_token(TregexParserConstantsConstants.Blank);
											break;
										}

										default:
										{
											jj_la1[21] = jj_gen;
											Jj_consume_token(-1);
											throw new ParseException();
										}
									}
									Jj_consume_token(25);
									break;
								}

								case TregexParserConstantsConstants.Regex:
								case 16:
								{
									switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
									{
										case 16:
										{
											negation = Jj_consume_token(16);
											break;
										}

										default:
										{
											jj_la1[22] = jj_gen;
											break;
										}
									}
									strArg = Jj_consume_token(TregexParserConstantsConstants.Regex);
									break;
								}

								default:
								{
									jj_la1[23] = jj_gen;
									Jj_consume_token(-1);
									throw new ParseException();
								}
							}
							break;
						}

						default:
						{
							jj_la1[24] = jj_gen;
							Jj_consume_token(-1);
							throw new ParseException();
						}
					}
					if (strArg != null)
					{
						string negStr = negation == null ? string.Empty : "!";
						string catStr = cat == null ? string.Empty : "@";
						r = Edu.Stanford.Nlp.Trees.Tregex.Relation.GetRelation(t.image, negStr + catStr + strArg.image, basicCatFunction, headFinder);
					}
					else
					{
						if (numArg != null)
						{
							if (t.image.EndsWith("-"))
							{
								t.image = Sharpen.Runtime.Substring(t.image, 0, t.image.Length - 1);
								numArg.image = "-" + numArg.image;
							}
							r = Edu.Stanford.Nlp.Trees.Tregex.Relation.GetRelation(t.image, numArg.image, basicCatFunction, headFinder);
						}
						else
						{
							r = Edu.Stanford.Nlp.Trees.Tregex.Relation.GetRelation(t.image, basicCatFunction, headFinder);
						}
					}
					child = Node(r);
					{
						if (string.Empty != null)
						{
							return child;
						}
					}
					break;
				}

				case TregexParserConstantsConstants.MultiRelation:
				{
					t = Jj_consume_token(TregexParserConstantsConstants.MultiRelation);
					Jj_consume_token(26);
					switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
					{
						case TregexParserConstantsConstants.Identifier:
						case TregexParserConstantsConstants.Blank:
						case TregexParserConstantsConstants.Regex:
						case 14:
						case 16:
						case 17:
						case 20:
						case 21:
						{
							child = Node(null);
							children.Add(child);
							while (true)
							{
								switch ((jj_ntk == -1) ? Jj_ntk_f() : jj_ntk)
								{
									case 27:
									{
										break;
									}

									default:
									{
										jj_la1[25] = jj_gen;
										goto label_5_break;
									}
								}
								Jj_consume_token(27);
								child = Node(null);
								children.Add(child);
label_5_continue: ;
							}
label_5_break: ;
							break;
						}

						default:
						{
							jj_la1[26] = jj_gen;
							break;
						}
					}
					Jj_consume_token(28);
					{
						if (string.Empty != null)
						{
							return Edu.Stanford.Nlp.Trees.Tregex.Relation.ConstructMultiRelation(t.image, children, basicCatFunction, headFinder);
						}
					}
					break;
				}

				default:
				{
					jj_la1[27] = jj_gen;
					Jj_consume_token(-1);
					throw new ParseException();
				}
			}
			throw new Exception("Missing return statement in function");
		}

		private bool Jj_2_1(int xla)
		{
			jj_la = xla;
			jj_lastpos = jj_scanpos = token;
			try
			{
				return !Jj_3_1();
			}
			catch (TregexParser.LookaheadSuccess)
			{
				return true;
			}
			finally
			{
				Jj_save(0, xla);
			}
		}

		private bool Jj_2_2(int xla)
		{
			jj_la = xla;
			jj_lastpos = jj_scanpos = token;
			try
			{
				return !Jj_3_2();
			}
			catch (TregexParser.LookaheadSuccess)
			{
				return true;
			}
			finally
			{
				Jj_save(1, xla);
			}
		}

		private bool Jj_3R_25()
		{
			Token xsp;
			xsp = jj_scanpos;
			if (Jj_3R_26())
			{
				jj_scanpos = xsp;
				if (Jj_3R_27())
				{
					return true;
				}
			}
			return false;
		}

		private bool Jj_3R_9()
		{
			if (Jj_3R_11())
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_24()
		{
			if (Jj_3R_25())
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_23()
		{
			if (Jj_scan_token(14))
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_20()
		{
			if (Jj_scan_token(21))
			{
				return true;
			}
			return false;
		}

		private bool Jj_3_2()
		{
			if (Jj_scan_token(12))
			{
				return true;
			}
			if (Jj_3R_7())
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_22()
		{
			if (Jj_scan_token(24))
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_16()
		{
			if (Jj_scan_token(17))
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_18()
		{
			Token xsp;
			xsp = jj_scanpos;
			if (Jj_3R_22())
			{
				jj_scanpos = xsp;
				if (Jj_3R_23())
				{
					jj_scanpos = xsp;
					if (Jj_3R_24())
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool Jj_3R_8()
		{
			if (Jj_scan_token(14))
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_6()
		{
			Token xsp;
			xsp = jj_scanpos;
			if (Jj_3R_8())
			{
				jj_scanpos = xsp;
				if (Jj_3R_9())
				{
					return true;
				}
			}
			return false;
		}

		private bool Jj_3R_14()
		{
			if (Jj_scan_token(23))
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_27()
		{
			if (Jj_scan_token(TregexParserConstantsConstants.MultiRelation))
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_19()
		{
			Token xsp;
			xsp = jj_scanpos;
			if (Jj_scan_token(8))
			{
				jj_scanpos = xsp;
				if (Jj_scan_token(10))
				{
					jj_scanpos = xsp;
					if (Jj_scan_token(9))
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool Jj_3R_13()
		{
			if (Jj_scan_token(16))
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_17()
		{
			Token xsp;
			xsp = jj_scanpos;
			if (Jj_3R_19())
			{
				jj_scanpos = xsp;
				if (Jj_3R_20())
				{
					jj_scanpos = xsp;
					if (Jj_3R_21())
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool Jj_3R_12()
		{
			if (Jj_3R_18())
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_10()
		{
			Token xsp;
			xsp = jj_scanpos;
			if (Jj_3R_12())
			{
				jj_scanpos = xsp;
				if (Jj_3R_13())
				{
					jj_scanpos = xsp;
					if (Jj_3R_14())
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool Jj_3R_15()
		{
			if (Jj_scan_token(16))
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_29()
		{
			if (Jj_scan_token(TregexParserConstantsConstants.RelWStrArg))
			{
				return true;
			}
			return false;
		}

		private bool Jj_3_1()
		{
			if (Jj_scan_token(12))
			{
				return true;
			}
			if (Jj_3R_6())
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_28()
		{
			if (Jj_scan_token(TregexParserConstantsConstants.Relation))
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_11()
		{
			Token xsp;
			xsp = jj_scanpos;
			if (Jj_3R_15())
			{
				jj_scanpos = xsp;
			}
			xsp = jj_scanpos;
			if (Jj_3R_16())
			{
				jj_scanpos = xsp;
			}
			if (Jj_3R_17())
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_21()
		{
			if (Jj_scan_token(20))
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_7()
		{
			if (Jj_3R_10())
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_26()
		{
			Token xsp;
			xsp = jj_scanpos;
			if (Jj_3R_28())
			{
				jj_scanpos = xsp;
				if (Jj_3R_29())
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Generated Token Manager.</summary>
		public TregexParserTokenManager token_source;

		internal SimpleCharStream jj_input_stream;

		/// <summary>Current token.</summary>
		public Token token;

		/// <summary>Next token.</summary>
		public Token jj_nt;

		private int jj_ntk;

		private Token jj_scanpos;

		private Token jj_lastpos;

		private int jj_la;

		private int jj_gen;

		private readonly int[] jj_la1 = new int[28];

		private static int[] jj_la1_0;

		static TregexParser()
		{
			Jj_la1_init_0();
		}

		private static void Jj_la1_init_0()
		{
			jj_la1_0 = new int[] { unchecked((int)(0x334700)), unchecked((int)(0x1814070)), unchecked((int)(0x1814070)), unchecked((int)(0x334700)), unchecked((int)(0x10000)), unchecked((int)(0x20000)), unchecked((int)(0x700)), unchecked((int)(0x40000))
				, unchecked((int)(0x100000)), unchecked((int)(0x100000)), unchecked((int)(0x300700)), unchecked((int)(0x1c14070)), unchecked((int)(0x400000)), unchecked((int)(0x1814070)), unchecked((int)(0x1004070)), unchecked((int)(0x80)), unchecked((int)
				(0x10000)), unchecked((int)(0x20000)), unchecked((int)(0x700)), unchecked((int)(0x10000)), unchecked((int)(0x20000)), unchecked((int)(0x700)), unchecked((int)(0x10000)), unchecked((int)(0x1014400)), unchecked((int)(0x50)), unchecked((int)(0x8000000
				)), unchecked((int)(0x334700)), unchecked((int)(0x70)) };
		}

		private readonly TregexParser.JJCalls[] jj_2_rtns = new TregexParser.JJCalls[2];

		private bool jj_rescan = false;

		private int jj_gc = 0;

		/// <summary>Constructor with InputStream.</summary>
		public TregexParser(InputStream stream)
			: this(stream, null)
		{
		}

		/// <summary>Constructor with InputStream and supplied encoding</summary>
		public TregexParser(InputStream stream, string encoding)
		{
			try
			{
				jj_input_stream = new SimpleCharStream(stream, encoding, 1, 1);
			}
			catch (UnsupportedEncodingException e)
			{
				throw new Exception(e);
			}
			token_source = new TregexParserTokenManager(jj_input_stream);
			token = new Token();
			jj_ntk = -1;
			jj_gen = 0;
			for (int i = 0; i < 28; i++)
			{
				jj_la1[i] = -1;
			}
			for (int i_1 = 0; i_1 < jj_2_rtns.Length; i_1++)
			{
				jj_2_rtns[i_1] = new TregexParser.JJCalls();
			}
		}

		/// <summary>Reinitialise.</summary>
		public virtual void ReInit(InputStream stream)
		{
			ReInit(stream, null);
		}

		/// <summary>Reinitialise.</summary>
		public virtual void ReInit(InputStream stream, string encoding)
		{
			try
			{
				jj_input_stream.ReInit(stream, encoding, 1, 1);
			}
			catch (UnsupportedEncodingException e)
			{
				throw new Exception(e);
			}
			token_source.ReInit(jj_input_stream);
			token = new Token();
			jj_ntk = -1;
			jj_gen = 0;
			for (int i = 0; i < 28; i++)
			{
				jj_la1[i] = -1;
			}
			for (int i_1 = 0; i_1 < jj_2_rtns.Length; i_1++)
			{
				jj_2_rtns[i_1] = new TregexParser.JJCalls();
			}
		}

		/// <summary>Constructor.</summary>
		public TregexParser(Reader stream)
		{
			jj_input_stream = new SimpleCharStream(stream, 1, 1);
			token_source = new TregexParserTokenManager(jj_input_stream);
			token = new Token();
			jj_ntk = -1;
			jj_gen = 0;
			for (int i = 0; i < 28; i++)
			{
				jj_la1[i] = -1;
			}
			for (int i_1 = 0; i_1 < jj_2_rtns.Length; i_1++)
			{
				jj_2_rtns[i_1] = new TregexParser.JJCalls();
			}
		}

		/// <summary>Reinitialise.</summary>
		public virtual void ReInit(Reader stream)
		{
			jj_input_stream.ReInit(stream, 1, 1);
			token_source.ReInit(jj_input_stream);
			token = new Token();
			jj_ntk = -1;
			jj_gen = 0;
			for (int i = 0; i < 28; i++)
			{
				jj_la1[i] = -1;
			}
			for (int i_1 = 0; i_1 < jj_2_rtns.Length; i_1++)
			{
				jj_2_rtns[i_1] = new TregexParser.JJCalls();
			}
		}

		/// <summary>Constructor with generated Token Manager.</summary>
		public TregexParser(TregexParserTokenManager tm)
		{
			token_source = tm;
			token = new Token();
			jj_ntk = -1;
			jj_gen = 0;
			for (int i = 0; i < 28; i++)
			{
				jj_la1[i] = -1;
			}
			for (int i_1 = 0; i_1 < jj_2_rtns.Length; i_1++)
			{
				jj_2_rtns[i_1] = new TregexParser.JJCalls();
			}
		}

		/// <summary>Reinitialise.</summary>
		public virtual void ReInit(TregexParserTokenManager tm)
		{
			token_source = tm;
			token = new Token();
			jj_ntk = -1;
			jj_gen = 0;
			for (int i = 0; i < 28; i++)
			{
				jj_la1[i] = -1;
			}
			for (int i_1 = 0; i_1 < jj_2_rtns.Length; i_1++)
			{
				jj_2_rtns[i_1] = new TregexParser.JJCalls();
			}
		}

		/// <exception cref="Edu.Stanford.Nlp.Trees.Tregex.ParseException"/>
		private Token Jj_consume_token(int kind)
		{
			Token oldToken;
			if ((oldToken = token).next != null)
			{
				token = token.next;
			}
			else
			{
				token = token.next = token_source.GetNextToken();
			}
			jj_ntk = -1;
			if (token.kind == kind)
			{
				jj_gen++;
				if (++jj_gc > 100)
				{
					jj_gc = 0;
					foreach (TregexParser.JJCalls jj_2_rtn in jj_2_rtns)
					{
						TregexParser.JJCalls c = jj_2_rtn;
						while (c != null)
						{
							if (c.gen < jj_gen)
							{
								c.first = null;
							}
							c = c.next;
						}
					}
				}
				return token;
			}
			token = oldToken;
			jj_kind = kind;
			throw GenerateParseException();
		}

		[System.Serializable]
		private sealed class LookaheadSuccess : Exception
		{
		}

		private readonly TregexParser.LookaheadSuccess jj_ls = new TregexParser.LookaheadSuccess();

		private bool Jj_scan_token(int kind)
		{
			if (jj_scanpos == jj_lastpos)
			{
				jj_la--;
				if (jj_scanpos.next == null)
				{
					jj_lastpos = jj_scanpos = jj_scanpos.next = token_source.GetNextToken();
				}
				else
				{
					jj_lastpos = jj_scanpos = jj_scanpos.next;
				}
			}
			else
			{
				jj_scanpos = jj_scanpos.next;
			}
			if (jj_rescan)
			{
				int i = 0;
				Token tok = token;
				while (tok != null && tok != jj_scanpos)
				{
					i++;
					tok = tok.next;
				}
				if (tok != null)
				{
					Jj_add_error_token(kind, i);
				}
			}
			if (jj_scanpos.kind != kind)
			{
				return true;
			}
			if (jj_la == 0 && jj_scanpos == jj_lastpos)
			{
				throw jj_ls;
			}
			return false;
		}

		/// <summary>Get the next Token.</summary>
		public Token GetNextToken()
		{
			if (token.next != null)
			{
				token = token.next;
			}
			else
			{
				token = token.next = token_source.GetNextToken();
			}
			jj_ntk = -1;
			jj_gen++;
			return token;
		}

		/// <summary>Get the specific Token.</summary>
		public Token GetToken(int index)
		{
			Token t = token;
			for (int i = 0; i < index; i++)
			{
				if (t.next != null)
				{
					t = t.next;
				}
				else
				{
					t = t.next = token_source.GetNextToken();
				}
			}
			return t;
		}

		private int Jj_ntk_f()
		{
			if ((jj_nt = token.next) == null)
			{
				return (jj_ntk = (token.next = token_source.GetNextToken()).kind);
			}
			else
			{
				return (jj_ntk = jj_nt.kind);
			}
		}

		private IList<int[]> jj_expentries = new List<int[]>();

		private int[] jj_expentry;

		private int jj_kind = -1;

		private int[] jj_lasttokens = new int[100];

		private int jj_endpos;

		private void Jj_add_error_token(int kind, int pos)
		{
			if (pos >= 100)
			{
				return;
			}
			if (pos == jj_endpos + 1)
			{
				jj_lasttokens[jj_endpos++] = kind;
			}
			else
			{
				if (jj_endpos != 0)
				{
					jj_expentry = new int[jj_endpos];
					for (int i = 0; i < jj_endpos; i++)
					{
						jj_expentry[i] = jj_lasttokens[i];
					}
					foreach (int[] jj_expentry1 in jj_expentries)
					{
						int[] oldentry = (int[])(jj_expentry1);
						if (oldentry.Length == jj_expentry.Length)
						{
							for (int i_1 = 0; i_1 < jj_expentry.Length; i_1++)
							{
								if (oldentry[i_1] != jj_expentry[i_1])
								{
									goto jj_entries_loop_continue;
								}
							}
							jj_expentries.Add(jj_expentry);
							goto jj_entries_loop_break;
						}
					}
jj_entries_loop_break: ;
					if (pos != 0)
					{
						jj_lasttokens[(jj_endpos = pos) - 1] = kind;
					}
				}
			}
		}

		/// <summary>Generate ParseException.</summary>
		public virtual ParseException GenerateParseException()
		{
			jj_expentries.Clear();
			bool[] la1tokens = new bool[29];
			if (jj_kind >= 0)
			{
				la1tokens[jj_kind] = true;
				jj_kind = -1;
			}
			for (int i = 0; i < 28; i++)
			{
				if (jj_la1[i] == jj_gen)
				{
					for (int j = 0; j < 32; j++)
					{
						if ((jj_la1_0[i] & (1 << j)) != 0)
						{
							la1tokens[j] = true;
						}
					}
				}
			}
			for (int i_1 = 0; i_1 < 29; i_1++)
			{
				if (la1tokens[i_1])
				{
					jj_expentry = new int[1];
					jj_expentry[0] = i_1;
					jj_expentries.Add(jj_expentry);
				}
			}
			jj_endpos = 0;
			Jj_rescan_token();
			Jj_add_error_token(0, 0);
			int[][] exptokseq = new int[jj_expentries.Count][];
			for (int i_2 = 0; i_2 < jj_expentries.Count; i_2++)
			{
				exptokseq[i_2] = jj_expentries[i_2];
			}
			return new ParseException(token, exptokseq, TregexParserConstantsConstants.tokenImage);
		}

		/// <summary>Enable tracing.</summary>
		public void Enable_tracing()
		{
		}

		/// <summary>Disable tracing.</summary>
		public void Disable_tracing()
		{
		}

		private void Jj_rescan_token()
		{
			jj_rescan = true;
			for (int i = 0; i < 2; i++)
			{
				try
				{
					TregexParser.JJCalls p = jj_2_rtns[i];
					do
					{
						if (p.gen > jj_gen)
						{
							jj_la = p.arg;
							jj_lastpos = jj_scanpos = p.first;
							switch (i)
							{
								case 0:
								{
									Jj_3_1();
									break;
								}

								case 1:
								{
									Jj_3_2();
									break;
								}
							}
						}
						p = p.next;
					}
					while (p != null);
				}
				catch (TregexParser.LookaheadSuccess)
				{
				}
			}
			jj_rescan = false;
		}

		private void Jj_save(int index, int xla)
		{
			TregexParser.JJCalls p = jj_2_rtns[index];
			while (p.gen > jj_gen)
			{
				if (p.next == null)
				{
					p = p.next = new TregexParser.JJCalls();
					break;
				}
				p = p.next;
			}
			p.gen = jj_gen + xla - jj_la;
			p.first = token;
			p.arg = xla;
		}

		internal sealed class JJCalls
		{
			internal int gen;

			internal Token first;

			internal int arg;

			internal TregexParser.JJCalls next;
		}
	}
}
