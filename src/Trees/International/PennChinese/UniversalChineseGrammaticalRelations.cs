// Stanford Dependencies - Code for producing and using Stanford dependencies.
// Copyright © 2005-2014 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//    parser-support@lists.stanford.edu
//    http://nlp.stanford.edu/software/stanford-dependencies.shtml
using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Concurrent.Locks;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>
	/// ChineseGrammaticalRelations is a
	/// set of
	/// <see cref="Edu.Stanford.Nlp.Trees.GrammaticalRelation"/>
	/// objects for the Chinese language.
	/// Examples are from CTB_001.fid
	/// TODO(pliang): need to take some of these relations and move them into a
	/// Universal Stanford Dependencies class (e.g., dep, arg, mod).
	/// Currently, we have an external data structure that stores information about
	/// whether a relation is universal or not, but that should probably be moved
	/// into GrammaticalRelation.
	/// TODO(pliang): add an option to produce trees which use only the USD
	/// relations rather than the more specialized Chinese ones.
	/// </summary>
	/// <author>Galen Andrew</author>
	/// <author>Pi-Chuan Chang</author>
	/// <author>Huihsin Tseng</author>
	/// <author>Marie-Catherine de Marneffe</author>
	/// <author>Percy Liang</author>
	/// <author>Peng Qi</author>
	/// <seealso cref="Edu.Stanford.Nlp.Trees.GrammaticalStructure"/>
	/// <seealso cref="Edu.Stanford.Nlp.Trees.GrammaticalRelation"/>
	/// <seealso cref="UniversalChineseGrammaticalStructure"/>
	public class UniversalChineseGrammaticalRelations
	{
		/// <summary>
		/// This class is just a holder for static classes
		/// that act a bit like an enum.
		/// </summary>
		private UniversalChineseGrammaticalRelations()
		{
		}

		private static readonly TregexPatternCompiler tregexCompiler = new TregexPatternCompiler((IHeadFinder)null);

		private const string CommaPattern = "/^,|，$/";

		private const string ModalPattern = "/^(可(以|能)?)|能够?|应该?|将要?|必须|会$/";

		private const string LocationNouns = "/^((东|西|南|北)(边|侧|部|岸|麓|畔))|附近|近?旁|旁?边$/";

		// By setting the HeadFinder to null, we find out right away at
		// runtime if we have incorrectly set the HeadFinder for the
		// dependency tregexes
		/// <summary>Return an unmodifiable list of grammatical relations.</summary>
		/// <remarks>
		/// Return an unmodifiable list of grammatical relations.
		/// Note: the list can still be modified by others, so you
		/// should still get a lock with
		/// <c>valuesLock()</c>
		/// before
		/// iterating over this list.
		/// </remarks>
		/// <returns>A list of grammatical relations</returns>
		public static IList<GrammaticalRelation> Values()
		{
			return Java.Util.Collections.UnmodifiableList(values);
		}

		private static readonly IReadWriteLock valuesLock = new ReentrantReadWriteLock();

		public static ILock ValuesLock()
		{
			return valuesLock.ReadLock();
		}

		public static GrammaticalRelation ValueOf(string s)
		{
			return GrammaticalRelation.ValueOf(s, Values(), ValuesLock());
		}

		/// <summary>The "argument" (arg) grammatical relation (abstract).</summary>
		/// <remarks>
		/// The "argument" (arg) grammatical relation (abstract).
		/// Arguments are required by their heads.
		/// </remarks>
		public static readonly GrammaticalRelation Argument = new GrammaticalRelation(Language.UniversalChinese, "arg", "argument", GrammaticalRelation.Dependent);

		/// <summary>The "subject" (subj) grammatical relation (abstract).</summary>
		public static readonly GrammaticalRelation Subject = new GrammaticalRelation(Language.UniversalChinese, "subj", "subject", Argument);

		/// <summary>The "nominal subject" (nsubj) grammatical relation.</summary>
		/// <remarks>
		/// The "nominal subject" (nsubj) grammatical relation.  A nominal subject is
		/// a subject which is an noun phrase.
		/// <p>
		/// <code>
		/// <pre>
		/// Input:
		/// (ROOT
		/// (IP
		/// (NP
		/// (NP (NR 上海) (NR 浦东))
		/// (NP (NN 开发)
		/// (CC 与)
		/// (NN 法制) (NN 建设)))
		/// (VP (VV 同步))))
		/// Output:
		/// nsubj(同步, 建设)
		/// </pre>
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation NominalSubject = new GrammaticalRelation(Language.UniversalChinese, "nsubj", "nominal subject", Subject, "IP|NP", tregexCompiler, "IP <( ( NP|QP=target!< NT ) $+ ( /^VP|VCD|IP/  !< VE !<VC !<SB !<LB !<:NP !<:PP )) !$- BA"
			, "IP <( ( NP|QP=target!< NT ) $+ (PU (<: " + CommaPattern + " $+ ( /^VP|VCD|IP/  !< VE !<VC !<SB !<LB !<:NP !<:PP )))) !$- BA", "IP <( ( NP|QP=target!< NT ) $+ (LCP ($+ ( /^VP|VCD|IP/  !< VE !<VC !<SB !<LB !<:NP !<:PP )))) !$- BA", "NP !$+ VP < ( (  NP|DP|QP=target !< NT ) $+ ( /^VP|VCD/ !<VE !< VC !<SB !<LB))"
			, "IP < (/^NP/=target $+ (VP < VC))");

		/// <summary>The "nominal passive subject" (nsubjpass) grammatical relation.</summary>
		/// <remarks>
		/// The "nominal passive subject" (nsubjpass) grammatical relation.
		/// The noun is the subject of a passive sentence.
		/// The passive marker in Chinese is "被".
		/// <p>
		/// <code>
		/// <pre>
		/// Input:
		/// (IP
		/// (NP (NN 镍))
		/// (VP (SB 被)
		/// (VP (VV 称作)
		/// (NP (PU “)
		/// (DNP
		/// (NP
		/// (ADJP (JJ 现代))
		/// (NP (NN 工业)))
		/// (DEG 的))
		/// (NP (NN 维生素))
		/// (PU ”)))))
		/// Output:
		/// nsubjpass(称作-3, 镍-1)
		/// </pre>
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation NominalPassiveSubject = new GrammaticalRelation(Language.UniversalChinese, "nsubjpass", "nominal passive subject", NominalSubject, "IP", tregexCompiler, "IP < (NP=target $+ (VP|IP < SB|LB))");

		/// <summary>The "clausal subject" grammatical relation.</summary>
		/// <remarks>
		/// The "clausal subject" grammatical relation.  A clausal subject is
		/// a subject which is a clause.
		/// <p /> Examples:
		/// <code>
		/// <pre>
		/// </pre>
		/// </code>
		/// <p />
		/// Note: This one might not exist in Chinese, or very rare.
		/// cdm 2016: There are a few CP-SBJ in the CTB like this one:
		/// 我 估计 [CP-SBJ 他 欺负 别人 的 ] 多
		/// but it doesn't seem like there would be any way to detect them without using -SBJ
		/// </remarks>
		public static readonly GrammaticalRelation ClausalSubject = new GrammaticalRelation(Language.UniversalChinese, "csubj", "clausal subject", Subject, "IP|VP", tregexCompiler, "IP|VP < ( /^IP(-SBJ)?/ < NP|QP|LCP $+ VP=target )", "IP|VP < ( /^IP(-SBJ)?/ < NP|QP|LCP $+ (PU $+ VP=target ))"
			);

		/// <summary>The "complement" (comp) grammatical relation.</summary>
		public static readonly GrammaticalRelation Complement = new GrammaticalRelation(Language.UniversalChinese, "comp", "complement", Argument);

		/// <summary>The "object" (obj) grammatical relation.</summary>
		public static readonly GrammaticalRelation Object = new GrammaticalRelation(Language.UniversalChinese, "obj", "object", Complement);

		/// <summary>The "direct object" (dobj) grammatical relation.</summary>
		/// <remarks>
		/// The "direct object" (dobj) grammatical relation.
		/// <p>
		/// <code>
		/// <pre>
		/// Input:
		/// (IP
		/// (NP (NR 上海) (NR 浦东))
		/// (VP
		/// (VCD (VV 颁布) (VV 实行))
		/// (AS 了)
		/// (QP (CD 七十一)
		/// (CLP (M 件)))
		/// (NP (NN 法规性) (NN 文件))))
		/// In recent years Shanghai 's Pudong has promulgated and implemented
		/// some regulatory documents.
		/// Output:
		/// dobj(颁布, 文件)
		/// </pre>
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation DirectObject = new GrammaticalRelation(Language.UniversalChinese, "dobj", "direct object", Object, "CP|VP", tregexCompiler, "VP < ( /^V*/ $+ NP|DP=target ) !< VC ", "VP < ( /^V*/ $+ (AS $+ NP|DP=target) ) !< VC "
			, " VP < ( /^V*/ $+ NP|DP=target ! $+ NP|DP) !< VC ", "CP < (IP $++ NP=target ) !<< VC");

		/// <summary>The "indirect object" (iobj) grammatical relation.</summary>
		public static readonly GrammaticalRelation IndirectObject = new GrammaticalRelation(Language.UniversalChinese, "iobj", "indirect object", Object, "VP", tregexCompiler, " CP !> VP < ( VV $+ ( NP|DP|QP|CLP=target . NP|DP ) )");

		/// <summary>The "clausal complement" (ccomp) grammatical relation.</summary>
		/// <remarks>
		/// The "clausal complement" (ccomp) grammatical relation.
		/// <p>
		/// <code>
		/// <pre>
		/// Input:
		/// (IP
		/// (VP
		/// (VP
		/// (ADVP (AD 一))
		/// (VP (VV 出现)))
		/// (VP
		/// (ADVP (AD 就))
		/// (VP (SB 被)
		/// (VP (VV 纳入)
		/// (NP (NN 法制) (NN 轨道)))))))))))
		/// Output:
		/// ccomp(出现, 纳入)
		/// </pre>
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation ClausalComplement = new GrammaticalRelation(Language.UniversalChinese, "ccomp", "clausal complement", Complement, "VP|ADJP|IP", tregexCompiler, "  VP  < (VV|VC|VRD|VCD|VSB|VE $++ IP|VP|VRD|VCD|VSB|CP=target)  !< NP|QP|LCP  > IP|VP "
			, "VP < (VV $+ NP $++ IP=target)");

		/// <summary>The "xclausal complement" (xcomp) grammatical relation.</summary>
		public static readonly GrammaticalRelation XclausalComplement = new GrammaticalRelation(Language.UniversalChinese, "xcomp", "xclausal complement", Complement, "VP", tregexCompiler, "VP < (VV=target $+ VP !< " + ModalPattern + ")");

		/// <summary>The "modifier" (mod) grammatical relation (abstract).</summary>
		public static readonly GrammaticalRelation Modifier = new GrammaticalRelation(Language.UniversalChinese, "mod", "modifier", GrammaticalRelation.Dependent);

		/// <summary>The "number modifier" (nummod) grammatical relation.</summary>
		/// <remarks>
		/// The "number modifier" (nummod) grammatical relation.
		/// <p>
		/// <pre>
		/// <code>
		/// Input:
		/// (NP
		/// (NP (NN 拆迁) (NN 工作))
		/// (QP (CD 若干))
		/// (NP (NN 规定)))
		/// Output:
		/// nummod(规定-48, 若干-47)
		/// </code>
		/// </pre>
		/// </remarks>
		public static readonly GrammaticalRelation NumericModifier = new GrammaticalRelation(Language.UniversalChinese, "nummod", "numeric modifier", Modifier, "QP|NP|DP", tregexCompiler, "NP|QP < ( QP  =target << M $++ NN|NP|QP)", "NP|QP < ( DNP=target < (QP < CD !< OD) !< JJ|ADJP $++ NP|QP )"
			);

		/// <summary>The "appositional modifier" (appos) grammatical relation (abstract).</summary>
		public static readonly GrammaticalRelation AppositionalModifier = new GrammaticalRelation(Language.UniversalChinese, "appos", "appositional modifier", Modifier, "NP", tregexCompiler, "NP < (/^NP(-APP)?$/=target !<<- " + LocationNouns + " !< NT !<: NR $+ (NP <: NR !$+ __))"
			);

		public static readonly GrammaticalRelation Parataxis = new GrammaticalRelation(Language.UniversalChinese, "parataxis", "parataxis", GrammaticalRelation.Dependent);

		/// <summary>The "parenthetical modifier" (prnmod) grammatical relation (Chinese-specific).</summary>
		public static readonly GrammaticalRelation ParentheticalModifier = new GrammaticalRelation(Language.UniversalChinese, "parataxis:prnmod", "parenthetical modifier", Parataxis, "NP", tregexCompiler, "NP < PRN=target ");

		/// <summary>The "noun modifier" grammatical relation.</summary>
		public static readonly GrammaticalRelation NounModifier = new GrammaticalRelation(Language.UniversalChinese, "nmod", "noun modifier", Modifier, "NP", tregexCompiler, "NP < (NP=target < NR !$+ PU|CC|NP|NN $++ NP|PRN)", "NP < (NP=target $+ (NP <: NR)) [$- P|LC | $+ P|LC]"
			, "NP|QP < ( DNP =target < (NP < NT) $++ NP|QP )", "NP|QP < ( DNP =target < LCP|PP $++ NP|QP )");

		/// <summary>The "range" grammatical relation (Chinese only).</summary>
		/// <remarks>
		/// The "range" grammatical relation (Chinese only).  The indirect
		/// object of a VP is the quantifier phrase which is the (dative) object
		/// of the verb.<p>
		/// <p>
		/// <code>
		/// <pre>
		/// Input:
		/// (VP (VV 成交)
		/// (NP (NN 药品))
		/// (QP (CD 一亿多)
		/// (CLP (M 元))))
		/// Output:
		/// range(成交, 元)
		/// </pre>
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation Range = new GrammaticalRelation(Language.UniversalChinese, "nmod:range", "range", NounModifier, "VP", tregexCompiler, "VP < ( NP|DP|QP $+ DP|QP=target)", "VP < ( VV $+ QP=target )");

		public static readonly GrammaticalRelation PossessiveModifier = new GrammaticalRelation(Language.UniversalChinese, "nmod:poss", "possessive modifier", NounModifier, "NP", tregexCompiler, "NP < (PN=target $+ NN)");

		/// <summary>The "temporal modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "temporal modifier" grammatical relation.
		/// (IP
		/// (VP
		/// (NP (NT 以前))
		/// (ADVP (AD 不))
		/// (ADVP (AD 曾))
		/// (VP (VV 遇到) (AS 过))))
		/// (VP
		/// (LCP
		/// (NP (NT 近年))
		/// (LC 来))
		/// (VP
		/// (VCD (VV 颁布) (VV 实行))
		/// <c>tmod</c>
		/// (遇到, 以前)
		/// </remarks>
		public static readonly GrammaticalRelation TemporalModifier = new GrammaticalRelation(Language.UniversalChinese, "nmod:tmod", "temporal modifier", NounModifier, "VP|IP", tregexCompiler, "VP|IP < (NP=target < NT $++ VP)");

		public static readonly GrammaticalRelation ClausalModifier = new GrammaticalRelation(Language.UniversalChinese, "acl", "clausal modifier of noun", Modifier, "NP", tregexCompiler, "NP  < ( CP=target $++ NP << VV)", "NP < IP=target ");

		/// <summary>The "adjective modifier" (amod) grammatical relation.</summary>
		/// <remarks>
		/// The "adjective modifier" (amod) grammatical relation.
		/// <p>
		/// <pre>
		/// <code>
		/// Input:
		/// (NP
		/// (ADJP (JJ 新))
		/// (NP (NN 情况)))
		/// Output:
		/// amod(情况-34, 新-33)
		/// </code>
		/// </pre>
		/// </remarks>
		public static readonly GrammaticalRelation AdjectivalModifier = new GrammaticalRelation(Language.UniversalChinese, "amod", "adjectival modifier", Modifier, "NP|CLP|QP", tregexCompiler, "NP|CLP|QP < (ADJP=target $++ NP|CLP|QP ) ", "NP  $++ (CP=target << VA !<< VV) > NP "
			, "NP  < ( CP=target $++ NP << VA !<< VV)", "NP|QP < ( DNP=target < JJ|ADJP !< NP|QP $++ NP|QP )");

		/// <summary>The "ordinal modifier" (ordmod) grammatical relation.</summary>
		public static readonly GrammaticalRelation OrdinalModifier = new GrammaticalRelation(Language.UniversalChinese, "amod:ordmod", "ordinal numeric modifier", AdjectivalModifier, "NP|QP", tregexCompiler, "NP < (QP=target < OD !< CLP)", "NP|QP < ( DNP=target < (QP < OD !< CD) !< JJ|ADJP $++ NP|QP )"
			);

		/// <summary>The "determiner modifier" (det) grammatical relation.</summary>
		/// <remarks>
		/// The "determiner modifier" (det) grammatical relation.
		/// <p>
		/// <pre>
		/// <code>
		/// Input:
		/// (NP (DP (DT 这些))
		/// (NP (NN 经济) (NN 活动)))
		/// Output:
		/// det(活动-61, 这些-59)
		/// </code>
		/// </pre>
		/// </remarks>
		public static readonly GrammaticalRelation Determiner = new GrammaticalRelation(Language.UniversalChinese, "det", "determiner", Modifier, "^NP|DP", tregexCompiler, "/^NP/ < (DP=target $++ NP )");

		/// <summary>The "negative modifier" (neg) grammatical relation.</summary>
		/// <remarks>
		/// The "negative modifier" (neg) grammatical relation.
		/// <p>
		/// <pre>
		/// <code>
		/// Input:
		/// (VP
		/// (NP (NT 以前))
		/// (ADVP (AD 不))
		/// (ADVP (AD 曾))
		/// (VP (VV 遇到) (AS 过))))
		/// Output:
		/// neg(遇到-30, 不-28)
		/// </code>
		/// </pre>
		/// </remarks>
		public static readonly GrammaticalRelation NegationModifier = new GrammaticalRelation(Language.UniversalChinese, "neg", "negation modifier", Modifier, "VP|ADJP|IP", tregexCompiler, "VP|ADJP|IP < (AD|VV=target < /^(\\u4e0d|\\u6CA1|\\u6CA1\\u6709)$/)"
			, "VP|ADJP|IP < (ADVP|VV=target < (AD < /^(\\u4e0d|\\u6CA1|\\u6CA1\\u6709)$/))");

		/// <summary>The "adverbial modifier" (advmod) grammatical relation.</summary>
		/// <remarks>
		/// The "adverbial modifier" (advmod) grammatical relation.
		/// <p>
		/// <pre>
		/// <code>
		/// Input:
		/// (VP
		/// (ADVP (AD 基本))
		/// (VP (VV 做到) (AS 了)
		/// Output:
		/// advmod(做到-74, 基本-73)
		/// </code>
		/// </pre>
		/// </remarks>
		public static readonly GrammaticalRelation AdverbialModifier = new GrammaticalRelation(Language.UniversalChinese, "advmod", "adverbial modifier", Modifier, "VP|ADJP|IP|CP|PP|NP|QP", tregexCompiler, "VP|ADJP|IP|CP|PP|NP < (ADVP=target !< (AD < /^(\\u4e0d|\\u6CA1|\\u6CA1\\u6709)$/))"
			, "VP|ADJP < AD|CS=target", "QP < (ADVP=target $+ QP)", "QP < ( QP $+ ADVP=target)");

		public static readonly GrammaticalRelation AdvClausalModifier = new GrammaticalRelation(Language.UniversalChinese, "advcl", "clausal adverb", AdverbialModifier);

		/// <summary>The "dvp modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "dvp modifier" grammatical relation.
		/// <p>
		/// <code>
		/// <pre>
		/// Input:
		/// (VP (DVP
		/// (VP (VA 简单))
		/// (DEV 的))
		/// (VP (VV 采取) ...))
		/// Output:
		/// dvpmod(采取-9, 简单-7)
		/// </pre>
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation DvpmModifier = new GrammaticalRelation(Language.UniversalChinese, "advmod:dvp", "dvp modifier", AdverbialModifier, "VP", tregexCompiler, " VP < ( DVP=target $+ VP) ");

		/// <summary>The "auxiliary" (aux) grammatical relation.</summary>
		public static readonly GrammaticalRelation AuxModifier = new GrammaticalRelation(Language.UniversalChinese, "aux", "auxiliary (example: should[modifier] leave[head])", GrammaticalRelation.Dependent, "VP", tregexCompiler);

		/// <summary>The "modal" grammatical relation.</summary>
		/// <remarks>
		/// The "modal" grammatical relation.
		/// (IP
		/// (NP (NN 利益))
		/// (VP (VV 能)
		/// (VP (VV 得到)
		/// (NP (NN 保障)))))))))
		/// <code> mmod </code> (得到-64, 能-63)
		/// </remarks>
		public static readonly GrammaticalRelation ModalVerb = new GrammaticalRelation(Language.UniversalChinese, "aux:modal", "modal verb", AuxModifier, "VP", tregexCompiler, "VP < ( VV=target < " + ModalPattern + " !< /^没有$/ $+ VP|VRD )");

		/// <summary>The "aspect marker" grammatical relation.</summary>
		/// <remarks>
		/// The "aspect marker" grammatical relation.
		/// (VP
		/// (ADVP (AD 基本))
		/// (VP (VV 做到) (AS 了)
		/// <code> asp </code> (做到,了)
		/// </remarks>
		public static readonly GrammaticalRelation AspectMarker = new GrammaticalRelation(Language.UniversalChinese, "aux:asp", "aspect", AuxModifier, "VP", tregexCompiler, "VP < ( /^V*/ $+ AS=target)");

		/// <summary>The "auxiliary passive" (auxpass) grammatical relation.</summary>
		public static readonly GrammaticalRelation AuxPassiveModifier = new GrammaticalRelation(Language.UniversalChinese, "auxpass", "auxiliary passive", Modifier, "VP", tregexCompiler, "VP < SB|LB=target");

		/// <summary>The "copula" grammatical relation.</summary>
		/// <remarks>
		/// The "copula" grammatical relation.
		/// <p>
		/// <pre>
		/// <code>
		/// Input:
		/// (IP (NP (NR 浦东))
		/// (VP (VC 是)
		/// (NP (NN 工程)))))
		/// Output (formerly reverse(attr)):
		/// cop(工程,是)
		/// </code>
		/// </pre>
		/// </remarks>
		public static readonly GrammaticalRelation Copula = new GrammaticalRelation(Language.UniversalChinese, "cop", "copula", GrammaticalRelation.Dependent, "VP", tregexCompiler, " VP < VC=target");

		/// <summary>The "marker" (mark) grammatical relation.</summary>
		/// <remarks>
		/// The "marker" (mark) grammatical relation.  A marker is the word
		/// introducing a finite clause subordinate to another clause.
		/// <p>
		/// <pre>
		/// <code>
		/// Input:
		/// (PP (P 因为)
		/// (IP
		/// (VP
		/// (VP
		/// (ADVP (AD 一))
		/// (VP (VV 开始)))
		/// (VP
		/// (ADVP (AD 就))
		/// (ADVP (AD 比较))
		/// (VP (VA 规范))))))
		/// Output (formerly reverse(pccomp)):
		/// mark(开始-20,因为-18)
		/// Input:
		/// (LCP (IP (NP-SBJ (-NONE- *pro*))
		/// (VP (VV 积累) (AS 了) (NP-OBJ (NN 经验)))) (LC 以后))
		/// Output (formerly reverse(lccomp)):
		/// mark(积累, 以后)
		/// Input:
		/// (CP
		/// (IP
		/// (VP
		/// (VP (VV 振兴)
		/// (NP (NR 上海)))
		/// (PU ，)
		/// (VP (VV 建设)
		/// (NP
		/// (NP (NN 现代化))
		/// (NP (NN 经济) (PU 、) (NN 贸易) (PU 、) (NN 金融))
		/// (NP (NN 中心))))))
		/// (DEC 的))
		/// Output (formerly cpm):
		/// mark(振兴, 的)
		/// Input:
		/// (DVP
		/// (VP (VA 简单))
		/// (DEV 的))
		/// Output (formerly dvpm):
		/// mark(简单-7, 的-8)
		/// </code>
		/// </pre>
		/// </remarks>
		public static readonly GrammaticalRelation Mark = new GrammaticalRelation(Language.UniversalChinese, "mark", "marker (examples: that[modifier] expanded[head]; 开发/expand[head] 浦东/Pudong 的[modifier])", GrammaticalRelation.Dependent, "^PP|^LCP|^CP|^DVP"
			, tregexCompiler, "/^PP/ < (P=target $+ VP)", "/^LCP/ < (P=target $+ VP)", "/^CP/ < (__  $++ DEC=target)", "DVP < (__ $+ DEV=target)");

		/// <summary>The "punctuation" grammatical relation.</summary>
		/// <remarks>
		/// The "punctuation" grammatical relation.  This is used for any piece of
		/// punctuation in a clause, if punctuation is being retained in the
		/// typed dependencies.
		/// </remarks>
		public static readonly GrammaticalRelation Punctuation = new GrammaticalRelation(Language.UniversalChinese, "punct", "punctuation", GrammaticalRelation.Dependent, ".*", tregexCompiler, "__ < PU=target");

		/// <summary>The "compound" grammatical relation (abstract).</summary>
		public static readonly GrammaticalRelation Compound = new GrammaticalRelation(Language.UniversalChinese, "compound", "compound (examples: phone book, three thousand)", Argument);

		/// <summary>The "noun compound" (nn) grammatical relation.</summary>
		/// <remarks>
		/// The "noun compound" (nn) grammatical relation.
		/// Example:
		/// (ROOT
		/// (IP
		/// (NP
		/// (NP (NR 上海) (NR 浦东))
		/// (NP (NN 开发)
		/// (CC 与)
		/// (NN 法制) (NN 建设)))
		/// (VP (VV 同步))))
		/// <code> compound:nn </code> (浦东, 上海)
		/// </remarks>
		public static readonly GrammaticalRelation NounCompound = new GrammaticalRelation(Language.UniversalChinese, "compound:nn", "noun compound", Compound, "^NP", tregexCompiler, "NP < (NN|NR|NT=target [$+ NN|NT $- NN|NP | $+ (NN|NT $+ NN|NP|NR)])"
			, "NP < (NN|NR|NT=target !$+ PU|CC|DNP $++ NN|NT)", "NP < (NN|NR|NT $+ FW=target)", "NP < (NP=target !< NR !$+ PU|CC|DNP $++ (NP|PRN !< NR|QP))", "NP < (NP=target < NR $+ (NP [<<# NR | $+ NR|NN | $+ (__ <<# NR) | $+ /^[^N]/]))", "NP < (NP=target < NN !< NR $+ (NP < NN|NT))"
			);

		/// <summary>The "name" grammatical relation.</summary>
		public static readonly GrammaticalRelation Name = new GrammaticalRelation(Language.UniversalChinese, "name", "name", Compound, "^NP", tregexCompiler, "NP < (NR=target $+ NR)");

		/// <summary>The "coordinated verb compound" grammatical relation.</summary>
		/// <remarks>
		/// The "coordinated verb compound" grammatical relation.
		/// (VCD (VV 颁布) (VV 实行))
		/// comod(颁布-5, 实行-6)
		/// </remarks>
		public static readonly GrammaticalRelation VerbCompound = new GrammaticalRelation(Language.UniversalChinese, "compound:vc", "coordinated verb compound", Compound, "VCD|VSB", tregexCompiler, "VCD < ( VV|VA $+  VV|VA=target)", "VSB < ( VV|VA=target $+  VV|VA)"
			);

		/// <summary>The "conjunct" (conj) grammatical relation.</summary>
		/// <remarks>
		/// The "conjunct" (conj) grammatical relation.
		/// <p>
		/// <code>
		/// <pre>
		/// Input:
		/// (ROOT
		/// (IP
		/// (NP
		/// (NP (NR 上海) (NR 浦东))
		/// (NP (NN 开发)
		/// (CC 与)
		/// (NN 法制) (NN 建设)))
		/// (VP (VV 同步))))
		/// The development of Shanghai 's Pudong is in step with the establishment
		/// of its legal system.
		/// Output:
		/// conj(建设, 开发) [should be reversed]
		/// </pre>
		/// </code>
		/// TODO(pliang): make first item the head and the subsequent ones modifiers.
		/// </remarks>
		public static readonly GrammaticalRelation Conjunct = new GrammaticalRelation(Language.UniversalChinese, "conj", "conjunct", GrammaticalRelation.Dependent, "FRAG|INC|IP|VP|NP|ADJP|PP|ADVP|UCP", tregexCompiler, "NP|ADJP|PP|ADVP|UCP < (!PU|CC=target $+ CC)"
			, "VP < (!PU|CC=target !$- VP $+ CC)", "VP|NP|ADJP|PP|ADVP|UCP < ( __=target $+ PU $+ CC)", "VP   < ( /^V/=target  $+ ((PU < 、) $+ /^V/))", "NP   < ( /^N/=target  $+ ((PU < 、) $+ /^N/))", "ADJP < ( JJ|ADJP=target  $+ ((PU < 、) $+ JJ|ADJP))"
			, "PP   < ( /^P/=target  $+ ((PU < 、) $+ /^P/))", "ADVP < ( /^AD/ $+ ((PU < 、) $+ /^AD/=target))", "UCP  < ( !PU|CC=target    $+ (PU < 、) )", "PP < (PP $+ PP=target )", "NP <( NP=target $+ ((PU < 、) $+ NP) )", "NP <( NN|NR|NT|PN=target $+ ((PU < ，|、) $+ NN|NR|NT|PN) )"
			, "VP < (CC $+ VV=target)", "FRAG|INC|IP|VP < (VP  < VV|VC|VRD|VCD|VE|VA < NP|QP|LCP  $ IP|VP|VRD|VCD|VE|VC|VA=target)  ", "IP|VP < ( IP < NP|QP|LCP $ IP=target )", "IP|VP < ( VP $ VP=target )");

		/// <summary>The "coordination" grammatical relation.</summary>
		/// <remarks>
		/// The "coordination" grammatical relation.
		/// A coordination is the relation between
		/// an element and a conjunction.<p>
		/// <code>
		/// <pre>
		/// Input:
		/// (ROOT
		/// (IP
		/// (NP
		/// (NP (NR 上海) (NR 浦东))
		/// (NP (NN 开发)
		/// (CC 与)
		/// (NN 法制) (NN 建设)))
		/// (VP (VV 同步))))
		/// Output:
		/// cc(建设, 与) [should be cc(开发, 与)]
		/// </pre>
		/// </code>
		/// TODO(pliang): by convention, the first item in the coordination should be
		/// chosen, but currently, it's the head, which happens to be the last.
		/// </remarks>
		public static readonly GrammaticalRelation Coordination = new GrammaticalRelation(Language.UniversalChinese, "cc", "coordination", GrammaticalRelation.Dependent, "VP|NP|ADJP|PP|ADVP|UCP|IP|QP", tregexCompiler, "VP|NP|ADJP|PP|ADVP|UCP|IP|QP < (CC=target)"
			);

		/// <summary>The "case" grammatical relation.</summary>
		/// <remarks>
		/// The "case" grammatical relation.
		/// This covers prepositions, localizers, and associative markers.
		/// <p>
		/// <pre>
		/// <code>
		/// Input:
		/// (PP (P 根据)
		/// (NP
		/// (DNP
		/// (NP
		/// (NP (NN 国家))
		/// (CC 和)
		/// (NP (NR 上海市)))
		/// (DEG 的))
		/// (ADJP (JJ 有关))
		/// (NP (NN 规定))))
		/// Output (formerly reverse(pobj)):
		/// case(规定-19, 根据-13)
		/// Input:
		/// (LCP
		/// (NP (NT 近年))
		/// (LC 来))
		/// Output (formerly reverse(lobj)):
		/// case(近年-3, 来-4)
		/// Input:
		/// (NP (DNP
		/// (NP (NP (NR 浦东))
		/// (NP (NN 开发)))
		/// (DEG 的))
		/// (ADJP (JJ 有序))
		/// (NP (NN 进行)))
		/// Output (formerly reverse(assm)):
		/// case(开发-31, 的-32)
		/// Input:
		/// (PP (P 在)
		/// (LCP
		/// (NP
		/// (DP (DT 这)
		/// (CLP (M 片)))
		/// (NP (NN 热土)))
		/// (LC 上)))
		/// Output (formerly reverse(plmod)):
		/// case(热土, 在)
		/// </code>
		/// </pre>
		/// </remarks>
		public static readonly GrammaticalRelation Case = new GrammaticalRelation(Language.UniversalChinese, "case", "case marking (examples: Chair[head] 's[modifier], 根据/according[modifier] ... 规定/rule[head]; 近年/this year[head] 来[modifier])", GrammaticalRelation
			.Dependent, "^PP|^LCP|^DNP", tregexCompiler, "/^PP/ < P=target", "/^LCP/ < LC=target", "/^DNP/ < DEG=target", "PP < ( P=target $++ LCP )");

		/// <summary>The "associative modifier" (nmod:assmod) grammatical relation (Chinese-specific).</summary>
		/// <remarks>
		/// The "associative modifier" (nmod:assmod) grammatical relation (Chinese-specific).
		/// See "case" for example.
		/// </remarks>
		public static readonly GrammaticalRelation AssociativeModifier = new GrammaticalRelation(Language.UniversalChinese, "nmod:assmod", "associative modifier (examples: 上海市/Shanghai[modifier] 的 规定/law[head])", NounModifier, "NP|QP|DNP", tregexCompiler
			, "NP|QP < ( DNP =target < (NP !< NT) $++ NP|QP ) ", "NP|DNP < (/^NP(-PN)?$/=target < NR $+ (NP !<<# NR !$+ NR|NN !$+ (__ <<# NR) !$+ /^[^N]/) !$- NP|NN)", "NP < (NP=target !< NR !$+ PU|CC $++ (NP|PRN < QP))");

		/// <summary>The "nominal topic" (nmod:topic) grammatical relation (Chinese-specific).</summary>
		/// <remarks>
		/// The "nominal topic" (nmod:topic) grammatical relation (Chinese-specific).
		/// Example:
		/// <code>
		/// Input:
		/// (IP (NP-TPC (NP-APP (ADJP (JJ 现任))
		/// (NP (NN 总统)))
		/// (NP-PN (NR 米洛舍维奇)))
		/// (NP-TMP (NT ２００１年))
		/// (NP-SBJ (NN 总统)
		/// (NN 任期))
		/// (VP (VV 到期)))
		/// Output:
		/// nmod:topic(到期, 米洛舍维奇)
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation NominalTopicModifier = new GrammaticalRelation(Language.UniversalChinese, "nmod:topic", "nominal topic", NounModifier, "IP", tregexCompiler, "IP < (/^NP.*(-TPC)?/=target $++ (NP $+ VP) !< NT)");

		/// <summary>The "localizer complement" grammatical relation.</summary>
		/// <remarks>
		/// The "localizer complement" grammatical relation.
		/// (VP (VV 占)
		/// (LCP
		/// (QP (CD 九成))
		/// (LC 以上)))
		/// (PU ，)
		/// (vp (VV 达)
		/// (QP (CD 四百三十八点八亿)
		/// (CLP (M 美元))))
		/// <code> loc </code> (占-11, 以上-13)
		/// </remarks>
		public static readonly GrammaticalRelation LocalizerComplement = new GrammaticalRelation(Language.UniversalChinese, "advmod:loc", "localizer complement", AdverbialModifier, "VP|IP", tregexCompiler, "VP|IP < (LCP=target !< IP) ");

		public static readonly GrammaticalRelation ClausalLocalizerComplement = new GrammaticalRelation(Language.UniversalChinese, "advcl:loc", "localizer complement", AdvClausalModifier, "VP|IP", tregexCompiler, "VP|IP < (LCP=target < IP) ");

		/// <summary>The "resultative complement" grammatical relation.</summary>
		public static readonly GrammaticalRelation ResultativeComplement = new GrammaticalRelation(Language.UniversalChinese, "advmod:rcomp", "result verb", AdverbialModifier, "VRD", tregexCompiler, "VRD < ( /V*/ $+ /V*/=target )");

		/// <summary>The "ba" grammatical relation.</summary>
		public static readonly GrammaticalRelation Ba = new GrammaticalRelation(Language.UniversalChinese, "aux:ba", "ba", AuxModifier, "VP|IP", tregexCompiler, "VP|IP < BA=target ");

		/// <summary>The "classifier marker" grammatical relation.</summary>
		/// <remarks>
		/// The "classifier marker" grammatical relation.
		/// <p>
		/// <code>
		/// <pre>
		/// Input:
		/// ((QP (CD 七十一)
		/// (CLP (M 件)))
		/// (NP (NN 法规性) (NN 文件)))
		/// Output:
		/// mark:clf(七十一, 件)
		/// </pre>
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation ClassifierModifier = new GrammaticalRelation(Language.UniversalChinese, "mark:clf", "classifier marker", Mark, "QP|DP", tregexCompiler, "QP < M=target", "QP < CLP=target", "DP < ( DT $+ CLP=target )"
			);

		/// <summary>The "prepositional modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "prepositional modifier" grammatical relation.
		/// (IP
		/// (PP (P 对)
		/// (NP (PN 此)))
		/// (PU ，)
		/// (NP (NR 浦东))
		/// (VP
		/// (VP
		/// (ADVP (AD 不))
		/// (VP (VC 是)
		/// (VP
		/// (DVP
		/// (VP (VA 简单))
		/// (DEV 的))
		/// (VP (VV 采取)
		/// <code> nmod </code> (采取-9, 此-1)
		/// </remarks>
		public static readonly GrammaticalRelation PrepositionalModifier = new GrammaticalRelation(Language.UniversalChinese, "nmod:prep", "prepositional modifier", NounModifier, "^NP|VP|IP", tregexCompiler, "/^NP/ < /^PP/=target", "VP < /^PP/=target"
			, "IP < /^PP/=target ");

		/// <summary>The "participial modifier" (prtmod) grammatical relation.</summary>
		public static readonly GrammaticalRelation PartVerb = new GrammaticalRelation(Language.UniversalChinese, "aux:prtmod", "particle verb", AuxModifier, "VP|IP", tregexCompiler, "VP|IP < ( MSP=target )");

		/// <summary>The "etc" grammatical relation.</summary>
		/// <remarks>
		/// The "etc" grammatical relation.
		/// (NP
		/// (NP (NN 经济) (PU 、) (NN 贸易) (PU 、) (NN 建设) (PU 、) (NN 规划) (PU 、) (NN 科技) (PU 、) (NN 文教) (ETC 等))
		/// (NP (NN 领域)))
		/// <code> etc </code> (办法-70, 等-71)
		/// </remarks>
		public static readonly GrammaticalRelation Etc = new GrammaticalRelation(Language.UniversalChinese, "etc", "ETC", Modifier, "^NP", tregexCompiler, "/^NP/ < (NN|NR . ETC=target)");

		/// <summary>The "xsubj" grammatical relation, replaced with "nsubj:xsubj".</summary>
		/// <remarks>
		/// The "xsubj" grammatical relation, replaced with "nsubj:xsubj".
		/// (IP
		/// (NP (PN 有些))
		/// (VP
		/// (VP
		/// (ADVP (AD 还))
		/// (ADVP (AD 只))
		/// (VP (VC 是)
		/// (NP
		/// (ADJP (JJ 暂行))
		/// (NP (NN 规定)))))
		/// (PU ，)
		/// (VP (VV 有待)
		/// (IP
		/// (VP
		/// (PP (P 在)
		/// (LCP
		/// (NP (NN 实践))
		/// (LC 中)))
		/// (ADVP (AD 逐步))
		/// (VP (VV 完善))))))))))
		/// <code> nsubj </code> (完善-26, 规定-14)
		/// </remarks>
		public static readonly GrammaticalRelation ControlledSubject = new GrammaticalRelation(Language.UniversalChinese, "nsubj:xsubj", "controlled subject", NominalSubject, "VP", tregexCompiler, "VP !< NP < VP > (IP !$- NP !< NP !>> (VP < VC ) >+(VP) (VP $-- NP=target))"
			);

		/// <summary>The "discourse" (discourse) grammatical relation.</summary>
		public static readonly GrammaticalRelation Discourse = new GrammaticalRelation(Language.UniversalChinese, "discourse", "discourse", Argument, "CP", tregexCompiler, "CP < SP=target");

		private static readonly GrammaticalRelation chineseOnly = null;

		private static readonly GrammaticalRelation[] rawValues = new GrammaticalRelation[] { GrammaticalRelation.Dependent, Argument, Subject, NominalSubject, NominalPassiveSubject, ClausalSubject, Complement, Object, DirectObject, IndirectObject, 
			ClausalComplement, XclausalComplement, Modifier, NumericModifier, OrdinalModifier, chineseOnly, AppositionalModifier, ParentheticalModifier, chineseOnly, NounModifier, Range, chineseOnly, AssociativeModifier, chineseOnly, TemporalModifier, 
			chineseOnly, PossessiveModifier, NominalTopicModifier, chineseOnly, AdjectivalModifier, Determiner, NegationModifier, ClausalModifier, AdverbialModifier, DvpmModifier, chineseOnly, AdvClausalModifier, ClausalLocalizerComplement, chineseOnly
			, AuxModifier, ModalVerb, chineseOnly, AspectMarker, chineseOnly, AuxPassiveModifier, Copula, Mark, ClassifierModifier, chineseOnly, Punctuation, Compound, NounCompound, chineseOnly, VerbCompound, chineseOnly, Name, Conjunct, Coordination, 
			Case, Discourse, LocalizerComplement, chineseOnly, ResultativeComplement, chineseOnly, Ba, chineseOnly, PrepositionalModifier, chineseOnly, PartVerb, chineseOnly, Etc, chineseOnly, ControlledSubject, chineseOnly };

		private static readonly IList<GrammaticalRelation> values = new List<GrammaticalRelation>();

		private static readonly IList<GrammaticalRelation> synchronizedValues = Java.Util.Collections.SynchronizedList(values);

		public static readonly ICollection<GrammaticalRelation> universalValues = new HashSet<GrammaticalRelation>();

		public static readonly IDictionary<string, GrammaticalRelation> shortNameToGRel = new ConcurrentHashMap<string, GrammaticalRelation>();

		static UniversalChineseGrammaticalRelations()
		{
			////////////////////////////////////////////////////////////
			// ARGUMENT relations
			////////////////////////////////////////////////////////////
			// Handle the case where the subject and object is separated by a comma
			// Handle the case where the subject and object is separated by a LCP
			// There are a number of cases of NP-SBJ not under IP, and we should try to get some of them as this
			// pattern does. There are others under CP, especially CP-CND
			// Go over copula
			// 进入/VV 了/AS 夏季/NN
			//        "  VP|IP <  ( VV|VC|VRD|VCD !$+  NP|QP|LCP ) > (IP   < IP|VP|VRD|VCD=target)   "
			//          "VP < (S=target < (VP !<, TO|VBG) !$-- NP)",
			// pichuan: this is difficult to recognize in Chinese.
			// remove the rules since it (always) collides with ccomp
			// fixme [pengqi 2016]: this is just a temporary solution to deal with VV $+ VP structures
			//   that are clearly not aux:modal
			////////////////////////////////////////////////////////////
			// MODIFIER relations
			////////////////////////////////////////////////////////////
			// the following rule is merged into mark:clf
			//"DP < ( DT $+ CLP=target )"
			/* This rule actually matches nothing.
			There's another tmod rule. This is removed for now.
			(pichuan) Sun Mar  8 18:22:40 2009
			*/
			/*
			public static final GrammaticalRelation TEMPORAL_MODIFIER =
			new GrammaticalRelation(Language.UniversalChinese,
			"tmod", "temporal modifier",
			MODIFIER, "VP|IP|ADJP", tregexCompiler,
			new String[]{
			" VC|VE ! >> VP|ADJP < NP=target < NT",
			"VC|VE !>>IP <( NP=target < NT $++ VP !< VC|VE )"
			});
			*/
			//"NP  $++ (CP=target << VV) > NP ",
			/* merged into acl
			public static final GrammaticalRelation RELATIVE_CLAUSE_MODIFIER =
			new GrammaticalRelation(Language.UniversalChinese, "acl", "adjectival clause modifier",
			CLAUSAL_MODIFIER, "NP", tregexCompiler,
			"NP  $++ (CP=target << VV) > NP ",
			"NP  < ( CP=target $++ NP << VV)",
			"NP < IP=target ");
			*/
			/*
			* The "non-finite clause" grammatical relation.
			* This used to be verb modifier (vmod).
			*/
			/* merged into acl
			public static final GrammaticalRelation NONFINITE_CLAUSE_MODIFIER =
			new GrammaticalRelation(Language.UniversalChinese,
			"acl:nfincl", "non-finite clause modifier (examples: stores[head] based[modifier] in Boston",
			CLAUSAL_MODIFIER, "NP", tregexCompiler,
			"NP < IP=target ");
			*/
			// the following rule is merged into mark:clf
			//"QP < (OD=target $+ CLP)"
			//"DP < DT < QP=target"
			////////////////////////////////////////////////////////////
			// Special clausal dependents
			////////////////////////////////////////////////////////////
			// todo [pengqi]: using MODAL_PATTERN would render many cases of VV $+ VP
			//    as dep, need to assign a type to that structure. Also in that case
			//    need to clarify which verb is the head
			////////////////////////////////////////////////////////////
			// Other (compounding, coordination)
			////////////////////////////////////////////////////////////
			// the following rule captures some exceptions from nmod:assmod
			// Split the first rule to the second rule to avoid the duplication:
			// ccomp(前来-12, 投资-13)
			// conj(前来-12, 投资-13)
			//
			//      (IP
			//        (VP
			//          (VP (VV 前来))
			//          (VP
			//            (VCD (VV 投资) (VV 办厂)))
			//          (CC 和)
			//          (VP (VV 洽谈)
			//            (NP (NN 生意))))))
			// TODO: this following line has to be fixed.
			//       I think for now it just doesn't match anything.
			//"VP|NP|ADJP|PP|ADVP|UCP < ( __=target $+ (PU < 、) )",
			// Consider changing the rule ABOVE to these rules.
			//"ADVP < ( /^AD/=target $+ ((PU < 、) $+ /^AD/))",
			// This is for the 'conj's separated by commas.
			// For now this creates too much duplicates with 'ccomp'.
			// Need to look at more examples.
			// Original version of this did not have the outer layer of
			// the FRAG|INC|IP|VP.  This caused a bug where the basic
			// dependencies could have cycles.
			// splitting the following into two rules for accuracy
			// "IP|VP < ( IP|VP < NP|QP|LCP $ IP|VP=target )",
			// the following rule is merged into compound:nn
			//"NP < (NR=target $+ NN)",
			////////////////////////////////////////////////////////////
			// Other stuff: pliang: not sure exactly where they should go.
			////////////////////////////////////////////////////////////
			/*
			* The "prepositional localizer modifier" grammatical relation.
			* (PP (P 在)
			*     (LCP
			*       (NP
			*         (DP (DT 这)
			*             (CLP (M 片)))
			*         (NP (NN 热土)))
			*       (LC 上)))
			* plmod(在-25, 上-29)
			*/
			/*
			* pengqi Jul 2016: This shouldn't exist in UD and is replaced by case
			*
			public static final GrammaticalRelation PREPOSITIONAL_LOCALIZER_MODIFIER =
			new GrammaticalRelation(Language.UniversalChinese,
			"plmod", "prepositional localizer modifier",
			MODIFIER, "PP", tregexCompiler,
			"PP < ( P $++ LCP=target )");
			*/
			// deleted by pichuan: no real matches
			/*
			public static final GrammaticalRelation ADJECTIVAL_COMPLEMENT =
			new GrammaticalRelation(Language.UniversalChinese,
			"acomp", "adjectival complement",
			COMPLEMENT, "VP", tregexCompiler,
			new String[]{
			"VP < (ADJP=target !$-- NP)"
			});
			*/
			// Fri Feb 20 15:40:13 2009 (pichuan)
			// I think this "poss" relation is just WRONG.
			// DEC is a complementizer or a nominalizer,
			// this rule probably originally want to capture "DEG".
			// But it seems like it's covered by "assm" (associative marker).
			/*
			public static final GrammaticalRelation POSSESSION_MODIFIER =
			new GrammaticalRelation(Language.UniversalChinese,
			"poss", "possession modifier",
			MODIFIER, "NP", tregexCompiler,
			new String[]{
			"NP < ( PN=target $+ DEC $+  NP )"
			});
			*/
			// Similar to the comments to "poss",
			// I think this relation is wrong and will not appear.
			/*
			public static final GrammaticalRelation POSSESSIVE_MODIFIER =
			new GrammaticalRelation(Language.UniversalChinese, "possm", "possessive marker",
			MODIFIER, "NP", tregexCompiler,
			new String[]{
			"NP < ( PN $+ DEC=target ) "
			});
			*/
			// Universal GrammaticalRelations
			// Place-holder: put this after a relation to mark it as Chinese-only
			//CLAUSAL_PASSIVE_SUBJECT,  // Exists in Chinese?
			// Exists in Chinese?
			// Nominal heads, nominal dependents
			// Nominal heads, predicate dependents
			//NOMINALIZED_CLAUSE_MODIFIER,  // Exists in Chinese?
			// Predicate heads
			// Special clausal dependents
			//VOCATIVE,
			//DISCOURSE,
			//EXPL,
			// Other
			// Don't know what to do about these
			//PREPOSITIONAL_LOCALIZER_MODIFIER, chineseOnly,
			// Cache frequently used views of the values list
			// Map from GrammaticalRelation short names to their corresponding
			// GrammaticalRelation objects
			for (int i = 0; i < rawValues.Length; i++)
			{
				GrammaticalRelation gr = rawValues[i];
				if (gr == chineseOnly)
				{
					continue;
				}
				synchronizedValues.Add(gr);
				if (i + 1 == rawValues.Length || rawValues[i + 1] != chineseOnly)
				{
					universalValues.Add(gr);
				}
			}
			ValuesLock().Lock();
			try
			{
				foreach (GrammaticalRelation gr in Edu.Stanford.Nlp.Trees.International.Pennchinese.UniversalChineseGrammaticalRelations.Values())
				{
					shortNameToGRel[gr.GetShortName()] = gr;
				}
			}
			finally
			{
				ValuesLock().Unlock();
			}
		}

		/// <summary>Prints out the Chinese grammatical relations hierarchy.</summary>
		/// <param name="args">Args are ignored.</param>
		public static void Main(string[] args)
		{
			System.Console.Out.WriteLine(GrammaticalRelation.Dependent.ToPrettyString());
		}
	}
}
