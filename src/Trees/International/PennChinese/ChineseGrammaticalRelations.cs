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
	/// </summary>
	/// <author>Galen Andrew</author>
	/// <author>Pi-Chuan Chang</author>
	/// <author>Huihsin Tseng</author>
	/// <author>Marie-Catherine de Marneffe</author>
	/// <seealso cref="Edu.Stanford.Nlp.Trees.GrammaticalStructure"/>
	/// <seealso cref="Edu.Stanford.Nlp.Trees.GrammaticalRelation"/>
	/// <seealso cref="ChineseGrammaticalStructure"/>
	public class ChineseGrammaticalRelations
	{
		/// <summary>
		/// This class is just a holder for static classes
		/// that act a bit like an enum.
		/// </summary>
		private ChineseGrammaticalRelations()
		{
		}

		private static readonly TregexPatternCompiler tregexCompiler = new TregexPatternCompiler((IHeadFinder)null);

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

		public static readonly IReadWriteLock valuesLock = new ReentrantReadWriteLock();

		public static ILock ValuesLock()
		{
			return valuesLock.ReadLock();
		}

		public static GrammaticalRelation ValueOf(string s)
		{
			return GrammaticalRelation.ValueOf(s, Values(), ValuesLock());
		}

		/// <summary>The "argument" grammatical relation.</summary>
		public static readonly GrammaticalRelation Argument = new GrammaticalRelation(Language.Chinese, "arg", "argument", GrammaticalRelation.Dependent);

		/// <summary>The "conjunct" grammatical relation.</summary>
		/// <remarks>
		/// The "conjunct" grammatical relation.
		/// Example:
		/// <code>
		/// <pre>
		/// (ROOT
		/// (IP
		/// (NP
		/// (NP (NR 上海) (NR 浦东))
		/// (NP (NN 开发)
		/// (CC 与)
		/// (NN 法制) (NN 建设)))
		/// (VP (VV 同步))))
		/// "The development of Shanghai 's Pudong is in step with the
		/// establishment of its legal system"
		/// conj(建设, 开发)
		/// </pre>
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation Conjunct = new GrammaticalRelation(Language.Chinese, "conj", "conjunct", GrammaticalRelation.Dependent, "FRAG|INC|IP|VP|NP|ADJP|PP|ADVP|UCP", tregexCompiler, "NP|ADJP|PP|ADVP|UCP < (!PU=target $+ CC)"
			, "VP < (!PU=target !$- VP $+ CC)", "VP|NP|ADJP|PP|ADVP|UCP < ( __=target $+ PU $+ CC)", "VP   < ( /^V/=target  $+ ((PU < 、) $+ /^V/))", "NP   < ( /^N/=target  $+ ((PU < 、) $+ /^N/))", "ADJP < ( JJ|ADJP=target  $+ ((PU < 、) $+ JJ|ADJP))", "PP   < ( /^P/=target  $+ ((PU < 、) $+ /^P/))"
			, "ADVP < ( /^AD/ $+ ((PU < 、) $+ /^AD/=target))", "UCP  < ( __=target    $+ (PU < 、) )", "PP < (PP $+ PP=target )", "NP <( NP=target $+ ((PU < 、) $+ NP) )", "NP <( NN|NR|NT|PN=target $+ ((PU < ，|、) $+ NN|NR|NT|PN) )", "VP < (CC $+ VV=target)"
			, "FRAG|INC|IP|VP < (VP  < VV|VC|VRD|VCD|VE|VA < NP|QP|LCP  $ IP|VP|VRD|VCD|VE|VC|VA=target)  ", "IP|VP < ( IP|VP < NP|QP|LCP $ IP|VP=target )");

		/// <summary>The "copula" grammatical relation.</summary>
		public static readonly GrammaticalRelation AuxModifier = new GrammaticalRelation(Language.Chinese, "cop", "copula", GrammaticalRelation.Dependent, "VP", tregexCompiler, " VP < VC=target");

		/// <summary>The "coordination" grammatical relation.</summary>
		/// <remarks>
		/// The "coordination" grammatical relation.
		/// A coordination is the relation between
		/// an element and a conjunction.<p>
		/// <p/>
		/// Example:
		/// <code>
		/// <pre>
		/// (ROOT
		/// (IP
		/// (NP
		/// (NP (NR 上海) (NR 浦东))
		/// (NP (NN 开发)
		/// (CC 与)
		/// (NN 法制) (NN 建设)))
		/// (VP (VV 同步))))
		/// cc(建设, 与)
		/// </pre>
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation Coordination = new GrammaticalRelation(Language.Chinese, "cc", "coordination", GrammaticalRelation.Dependent, "VP|NP|ADJP|PP|ADVP|UCP|IP|QP", tregexCompiler, "VP|NP|ADJP|PP|ADVP|UCP|IP|QP < (CC=target)"
			);

		/// <summary>The "punctuation" grammatical relation.</summary>
		/// <remarks>
		/// The "punctuation" grammatical relation.  This is used for any piece of
		/// punctuation in a clause, if punctuation is being retained in the
		/// typed dependencies.
		/// </remarks>
		public static readonly GrammaticalRelation Punctuation = new GrammaticalRelation(Language.Chinese, "punct", "punctuation", GrammaticalRelation.Dependent, ".*", tregexCompiler, "__ < PU=target");

		/// <summary>The "subject" grammatical relation.</summary>
		public static readonly GrammaticalRelation Subject = new GrammaticalRelation(Language.Chinese, "subj", "subject", Argument);

		/// <summary>The "nominal subject" grammatical relation.</summary>
		/// <remarks>
		/// The "nominal subject" grammatical relation.  A nominal subject is
		/// a subject which is an noun phrase.<p>
		/// <p/>
		/// Example:
		/// <code>
		/// <pre>
		/// (ROOT
		/// (IP
		/// (NP
		/// (NP (NR 上海) (NR 浦东))
		/// (NP (NN 开发)
		/// (CC 与)
		/// (NN 法制) (NN 建设)))
		/// (VP (VV 同步))))
		/// nsubj(同步, 建设)
		/// </pre>
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation NominalSubject = new GrammaticalRelation(Language.Chinese, "nsubj", "nominal subject", Subject, "IP|VP", tregexCompiler, "IP <( ( NP|QP=target!< NT ) $++ ( /^VP|VCD|IP/  !< VE !<VC !<SB !<LB  ))", "NP !$+ VP < ( (  NP|DP|QP=target !< NT ) $+ ( /^VP|VCD/ !<VE !< VC !<SB !<LB))"
			);

		/// <summary>The "topic" grammatical relation.</summary>
		/// <remarks>
		/// The "topic" grammatical relation.
		/// Example:
		/// <code>
		/// <pre>
		/// (IP
		/// (NP (NN 建筑))
		/// (VP (VC 是)
		/// (NP
		/// (CP
		/// (IP
		/// (VP (VV 开发)
		/// (NP (NR 浦东))))
		/// (DEC 的))
		/// (QP (CD 一)
		/// (CLP (M 项)))
		/// (ADJP (JJ 主要))
		/// (NP (NN 经济) (NN 活动)))))
		/// top(是, 建筑)
		/// </pre>
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation TopSubject = new GrammaticalRelation(Language.Chinese, "top", "topic", Subject, "IP|VP", tregexCompiler, "IP|VP < ( NP|DP=target $+ ( VP < VC|VE ) )", "IP < (IP=target $+ ( VP < VC|VE))");

		/// <summary>The "nsubjpass" grammatical relation.</summary>
		/// <remarks>
		/// The "nsubjpass" grammatical relation.
		/// The noun is the subject of a passive sentence.
		/// The passive marker in Chinese is "被".
		/// <p>Example:
		/// <code>
		/// <pre>
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
		/// nsubjpass(称作-3, 镍-1)
		/// </pre>
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation NominalPassiveSubject = new GrammaticalRelation(Language.Chinese, "nsubjpass", "nominal passive subject", NominalSubject, "IP", tregexCompiler, "IP < (NP=target $+ (VP|IP < SB|LB))");

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
		/// </remarks>
		public static readonly GrammaticalRelation ClausalSubject = new GrammaticalRelation(Language.Chinese, "csubj", "clausal subject", Subject, "IP", tregexCompiler);

		/// <summary>The "comp" grammatical relation.</summary>
		public static readonly GrammaticalRelation Complement = new GrammaticalRelation(Language.Chinese, "comp", "complement", Argument);

		/// <summary>The "obj" grammatical relation.</summary>
		public static readonly GrammaticalRelation Object = new GrammaticalRelation(Language.Chinese, "obj", "object", Complement);

		/// <summary>The "direct object" grammatical relation.</summary>
		/// <remarks>
		/// The "direct object" grammatical relation.
		/// <p />Examples:
		/// <code>
		/// <pre>
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
		/// dobj(颁布, 文件)
		/// </pre>
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation DirectObject = new GrammaticalRelation(Language.Chinese, "dobj", "direct object", Object, "CP|VP", tregexCompiler, "VP < ( /^V*/ $+ NP $+ NP|DP=target ) !< VC ", " VP < ( /^V*/ $+ NP|DP=target ! $+ NP|DP) !< VC "
			, "CP < (IP $++ NP=target ) !<< VC");

		/// <summary>The "range" grammatical relation.</summary>
		/// <remarks>
		/// The "range" grammatical relation.  The indirect
		/// object of a VP is the quantifier phrase which is the (dative) object
		/// of the verb.<p>
		/// (VP (VV 成交)
		/// (NP (NN 药品))
		/// (QP (CD 一亿多)
		/// (CLP (M 元))))
		/// <code>range </code>(成交, 元)
		/// </remarks>
		public static readonly GrammaticalRelation Range = new GrammaticalRelation(Language.Chinese, "range", "range", Object, "VP", tregexCompiler, " VP < ( NP|DP|QP $+ NP|DP|QP=target)", "VP < ( VV $+ QP=target )");

		/// <summary>The "prepositional object" grammatical relation.</summary>
		/// <remarks>
		/// The "prepositional object" grammatical relation.
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
		/// Example:
		/// pobj(根据-13, 规定-19)
		/// </remarks>
		public static readonly GrammaticalRelation PrepositionalObject = new GrammaticalRelation(Language.Chinese, "pobj", "prepositional object", Object, "^PP", tregexCompiler, "/^PP/ < /^P/ < /^NP|^DP|QP/=target");

		/// <summary>The "localizer object" grammatical relation.</summary>
		/// <remarks>
		/// The "localizer object" grammatical relation.
		/// (LCP
		/// (NP (NT 近年))
		/// (LC 来))
		/// lobj(来-4, 近年-3)
		/// </remarks>
		public static readonly GrammaticalRelation TimePostposition = new GrammaticalRelation(Language.Chinese, "lobj", "localizer object", Object, "LCP", tregexCompiler, "LCP < ( NP|QP|DP=target $+ LC)");

		/// <summary>The "attributive" grammatical relation.</summary>
		/// <remarks>
		/// The "attributive" grammatical relation.
		/// (IP
		/// (NP (NR 浦东))
		/// (VP (VC 是)
		/// (NP (NN 工程)))))
		/// <code> attr </code> (是, 工程)
		/// </remarks>
		public static readonly GrammaticalRelation Attributive = new GrammaticalRelation(Language.Chinese, "attr", "attributive", Complement, "VP", tregexCompiler, "VP < /^VC$/ < NP|QP=target");

		/// <summary>The "clausal" grammatical relation.</summary>
		/// <remarks>
		/// The "clausal" grammatical relation.
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
		/// <code> ccomp </code> (出现, 纳入)
		/// </remarks>
		public static readonly GrammaticalRelation ClausalComplement = new GrammaticalRelation(Language.Chinese, "ccomp", "clausal complement", Complement, "VP|ADJP|IP", tregexCompiler, "  VP  < VV|VC|VRD|VCD  !< NP|QP|LCP  < IP|VP|VRD|VCD=target > IP|VP "
			);

		/// <summary>The "xclausal complement" grammatical relation.</summary>
		/// <remarks>
		/// The "xclausal complement" grammatical relation.
		/// Example:
		/// </remarks>
		public static readonly GrammaticalRelation XclausalComplement = new GrammaticalRelation(Language.Chinese, "xcomp", "xclausal complement", Complement, "VP|ADJP", tregexCompiler);

		/// <summary>The "cp marker" grammatical relation.</summary>
		/// <remarks>
		/// The "cp marker" grammatical relation.
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
		/// Example:
		/// <code> cpm </code> (振兴, 的)
		/// </remarks>
		public static readonly GrammaticalRelation Complementizer = new GrammaticalRelation(Language.Chinese, "cpm", "complementizer", Complement, "^CP", tregexCompiler, "/^CP/ < (__  $++ DEC=target)");

		/// <summary>The "localizer complement" grammatical relation.</summary>
		/// <remarks>
		/// The "localizer complement" grammatical relation.
		/// (VP (VV 占)
		/// (LCP
		/// (QP (CD 九成))
		/// (LC 以上)))
		/// (PU ，)
		/// (VP (VV 达)
		/// (QP (CD 四百三十八点八亿)
		/// (CLP (M 美元))))
		/// <code> loc </code> (占-11, 以上-13)
		/// </remarks>
		public static readonly GrammaticalRelation LcComplement = new GrammaticalRelation(Language.Chinese, "loc", "localizer complement", Complement, "VP|IP", tregexCompiler, "VP|IP < LCP=target ");

		/// <summary>The "resultative complement" grammatical relation.</summary>
		public static readonly GrammaticalRelation ResVerb = new GrammaticalRelation(Language.Chinese, "rcomp", "result verb", Complement, "VRD", tregexCompiler, "VRD < ( /V*/ $+ /V*/=target )");

		/// <summary>The "modifier" grammatical relation.</summary>
		public static readonly GrammaticalRelation Modifier = new GrammaticalRelation(Language.Chinese, "mod", "modifier", GrammaticalRelation.Dependent);

		/// <summary>The "coordinated verb compound" grammatical relation.</summary>
		/// <remarks>
		/// The "coordinated verb compound" grammatical relation.
		/// (VCD (VV 颁布) (VV 实行))
		/// comod(颁布-5, 实行-6)
		/// </remarks>
		public static readonly GrammaticalRelation VerbCompound = new GrammaticalRelation(Language.Chinese, "comod", "coordinated verb compound", Modifier, "VCD", tregexCompiler, "VCD < ( VV|VA $+  VV|VA=target)");

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
		public static readonly GrammaticalRelation ModalVerb = new GrammaticalRelation(Language.Chinese, "mmod", "modal verb", Modifier, "VP", tregexCompiler, "VP < ( VV=target !< /^没有$/ $+ VP|VRD )");

		/// <summary>The "passive" grammatical relation.</summary>
		public static readonly GrammaticalRelation AuxPassiveModifier = new GrammaticalRelation(Language.Chinese, "pass", "passive", Modifier, "VP", tregexCompiler, new string[] { "VP < SB|LB=target" });

		/// <summary>The "ba" grammatical relation.</summary>
		public static readonly GrammaticalRelation Ba = new GrammaticalRelation(Language.Chinese, "ba", "ba", GrammaticalRelation.Dependent, "VP|IP", tregexCompiler, "VP|IP < BA=target ");

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
		/// <code> tmod </code> (遇到, 以前)
		/// </remarks>
		public static readonly GrammaticalRelation TemporalModifier = new GrammaticalRelation(Language.Chinese, "tmod", "temporal modifier", Modifier, "VP|IP", tregexCompiler, "VP|IP < (NP=target < NT !.. /^VC$/ $++  VP)");

		/// <summary>The "temporal clause" grammatical relation.</summary>
		/// <remarks>
		/// The "temporal clause" grammatical relation.
		/// (VP(PP (P 等) (LCP (IP
		/// (VP (VV 积累) (AS 了)
		/// (NP (NN 经验))))
		/// (LC 以后)))
		/// (ADVP (AD 再))
		/// (VP (VV 制定)
		/// (NP (NN 法规) (NN 条例))))
		/// (PU ”)))
		/// (DEC 的))
		/// (NP (NN 做法)))))))
		/// <code> lccomp </code> (以后, 积累)
		/// </remarks>
		public static readonly GrammaticalRelation Time = new GrammaticalRelation(Language.Chinese, "lccomp", "clausal complement of localizer", Modifier, "LCP", tregexCompiler, "/LCP/ < ( IP=target $+ LC )");

		/// <summary>The "relative clause modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "relative clause modifier" grammatical relation.
		/// (CP (IP (VP (NP (NT 以前))
		/// (ADVP (AD 不))
		/// (ADVP (AD 曾))
		/// (VP (VV 遇到) (AS 过))))
		/// (DEC 的))
		/// (NP
		/// (NP
		/// (ADJP (JJ 新))
		/// (NP (NN 情况)))
		/// (PU 、)
		/// (NP
		/// (ADJP (JJ 新))
		/// (NP (NN 问题)))))))
		/// (PU 。)))
		/// the new problem that has not been encountered.
		/// <code> rcmod </code> (问题, 遇到)
		/// </remarks>
		public static readonly GrammaticalRelation RelativeClauseModifier = new GrammaticalRelation(Language.Chinese, "rcmod", "relative clause modifier", Modifier, "NP", tregexCompiler, "NP  $++ (CP=target ) > NP ", "NP  < ( CP=target $++ NP  )");

		/// <summary>The "number modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "number modifier" grammatical relation.
		/// (NP
		/// (NP (NN 拆迁) (NN 工作))
		/// (QP (CD 若干))
		/// (NP (NN 规定)))
		/// nummod(件-24, 七十一-23)
		/// nummod(规定-48, 若干-47)
		/// </remarks>
		public static readonly GrammaticalRelation NumericModifier = new GrammaticalRelation(Language.Chinese, "nummod", "numeric modifier", Modifier, "QP|NP", tregexCompiler, "QP < CD=target", "NP < ( QP=target !<< CLP )");

		/// <summary>The "ordnumber modifier" grammatical relation.</summary>
		public static readonly GrammaticalRelation OdnumericModifier = new GrammaticalRelation(Language.Chinese, "ordmod", "numeric modifier", Modifier, "NP|QP", tregexCompiler, "NP < QP=target < ( OD !$+ CLP )", "QP < (OD=target $+ CLP)");

		/// <summary>The "classifier modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "classifier modifier" grammatical relation.
		/// (QP (CD 七十一)
		/// (CLP (M 件)))
		/// (NP (NN 法规性) (NN 文件)))))
		/// <code> clf </code> (文件-26, 件-24)
		/// </remarks>
		public static readonly GrammaticalRelation NumberModifier = new GrammaticalRelation(Language.Chinese, "clf", "classifier modifier", Modifier, "^NP|DP|QP", tregexCompiler, "NP|QP < ( QP  =target << M $++ NN|NP|QP)", "DP < ( DT $+ CLP=target )"
			);

		/// <summary>The "noun compound modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "noun compound modifier" grammatical relation.
		/// Example:
		/// (ROOT
		/// (IP
		/// (NP
		/// (NP (NR 上海) (NR 浦东))
		/// (NP (NN 开发)
		/// (CC 与)
		/// (NN 法制) (NN 建设)))
		/// (VP (VV 同步))))
		/// <code> nn </code> (浦东, 上海)
		/// </remarks>
		public static readonly GrammaticalRelation NounCompoundModifier = new GrammaticalRelation(Language.Chinese, "nn", "nn modifier", Modifier, "^NP", tregexCompiler, "NP < (NN|NR|NT=target $+ NN|NR|NT)", "NP < (NN|NR|NT $+ FW=target)", " NP <  (NP=target !$+ PU|CC $++ NP|PRN )"
			);

		/// <summary>The "adjetive modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "adjetive modifier" grammatical relation.
		/// (NP
		/// (ADJP (JJ 新))
		/// (NP (NN 情况)))
		/// (PU 、)
		/// (NP
		/// (ADJP (JJ 新))
		/// (NP (NN 问题)))))))
		/// <code> amod </code> (情况-34, 新-33)
		/// </remarks>
		public static readonly GrammaticalRelation AdjectivalModifier = new GrammaticalRelation(Language.Chinese, "amod", "adjectival modifier", Modifier, "NP|CLP|QP", tregexCompiler, "NP|CLP|QP < (ADJP=target $++ NP|CLP|QP ) ");

		/// <summary>The "adverbial modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "adverbial modifier" grammatical relation.
		/// (VP
		/// (ADVP (AD 基本))
		/// (VP (VV 做到) (AS 了)))
		/// advmod(做到-74, 基本-73)
		/// </remarks>
		public static readonly GrammaticalRelation AdverbialModifier = new GrammaticalRelation(Language.Chinese, "advmod", "adverbial modifier", Modifier, "VP|ADJP|IP|CP|PP|NP|QP", tregexCompiler, "VP|ADJP|IP|CP|PP|NP < (ADVP=target !< (AD < /^(\\u4e0d|\\u6CA1|\\u6CA1\\u6709)$/))"
			, "VP|ADJP < AD|CS=target", "QP < (ADVP=target $+ QP)", "QP < ( QP $+ ADVP=target)");

		/// <summary>The "verb modifier" grammatical relation.</summary>
		public static readonly GrammaticalRelation IpModifier = new GrammaticalRelation(Language.Chinese, "vmod", "participle modifier", Modifier, "NP", tregexCompiler, "NP < IP=target ");

		/// <summary>The "parenthetical modifier" grammatical relation.</summary>
		public static readonly GrammaticalRelation PrnModifier = new GrammaticalRelation(Language.Chinese, "prnmod", "prn odifier", Modifier, "NP", tregexCompiler, "NP < PRN=target ");

		/// <summary>The "negative modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "negative modifier" grammatical relation.
		/// (VP
		/// (NP (NT 以前))
		/// (ADVP (AD 不))
		/// (ADVP (AD 曾))
		/// (VP (VV 遇到) (AS 过))))
		/// neg(遇到-30, 不-28)
		/// </remarks>
		public static readonly GrammaticalRelation NegationModifier = new GrammaticalRelation(Language.Chinese, "neg", "negation modifier", AdverbialModifier, "VP|ADJP|IP", tregexCompiler, "VP|ADJP|IP < (AD|VV=target < /^(\\u4e0d|\\u6CA1|\\u6CA1\\u6709)$/)"
			, "VP|ADJP|IP < (ADVP|VV=target < (AD < /^(\\u4e0d|\\u6CA1|\\u6CA1\\u6709)$/))");

		/// <summary>The "determiner modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "determiner modifier" grammatical relation.
		/// (NP
		/// (DP (DT 这些))
		/// (NP (NN 经济) (NN 活动)))
		/// det(活动-61, 这些-59)
		/// </remarks>
		public static readonly GrammaticalRelation Determiner = new GrammaticalRelation(Language.Chinese, "det", "determiner", Modifier, "^NP|DP", tregexCompiler, "/^NP/ < (DP=target $++ NP )");

		/// <summary>The "dvp marker" grammatical relation.</summary>
		/// <remarks>
		/// The "dvp marker" grammatical relation.
		/// (DVP
		/// (VP (VA 简单))
		/// (DEV 的))
		/// (VP (VV 采取)
		/// dvpm(简单-7, 的-8)
		/// </remarks>
		public static readonly GrammaticalRelation DvpModifier = new GrammaticalRelation(Language.Chinese, "dvpm", "dvp marker", Modifier, "DVP", tregexCompiler, "DVP < (__ $+ DEV=target ) ");

		/// <summary>The "dvp modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "dvp modifier" grammatical relation.
		/// <code>
		/// <pre>
		/// (ADVP (AD 不))
		/// (VP (VC 是)
		/// (VP
		/// (DVP
		/// (VP (VA 简单))
		/// (DEV 的))
		/// (VP (VV 采取)
		/// dvpmod(采取-9, 简单-7)
		/// </pre>
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation DvpmModifier = new GrammaticalRelation(Language.Chinese, "dvpmod", "dvp modifier", Modifier, "VP", tregexCompiler, " VP < ( DVP=target $+ VP) ");

		/// <summary>The "associative marker" grammatical relation.</summary>
		/// <remarks>
		/// The "associative marker" grammatical relation.
		/// <code>
		/// <pre>
		/// (NP (DNP
		/// (NP (NP (NR 浦东))
		/// (NP (NN 开发)))
		/// (DEG 的))
		/// (ADJP (JJ 有序))
		/// (NP (NN 进行)))
		/// assm(开发-31, 的-32)
		/// </pre>
		/// </code>
		/// </remarks>
		public static readonly GrammaticalRelation AssociativeModifier = new GrammaticalRelation(Language.Chinese, "assm", "associative marker", Modifier, "DNP", tregexCompiler, " DNP < ( __ $+ DEG=target ) ");

		/// <summary>The "associative modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "associative modifier" grammatical relation.
		/// (NP
		/// (NP (NR 深圳) (ETC 等))
		/// (NP (NN 特区))))
		/// (DEG 的))
		/// (NP (NN 经验) (NN 教训))))
		/// assmod(教训-40, 特区-37)
		/// </remarks>
		public static readonly GrammaticalRelation AssociativemModifier = new GrammaticalRelation(Language.Chinese, "assmod", "associative modifier", Modifier, "NP|QP", tregexCompiler, "NP|QP < ( DNP =target $++ NP|QP ) ");

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
		/// <code> prep </code> (采取-9, 对-1)
		/// </remarks>
		public static readonly GrammaticalRelation PrepositionalModifier = new GrammaticalRelation(Language.Chinese, "prep", "prepositional modifier", Modifier, "^NP|VP|IP", tregexCompiler, "/^NP/ < /^PP/=target", "VP < /^PP/=target", "IP < /^PP/=target "
			);

		/// <summary>The "clause modifier of a preposition" grammatical relation.</summary>
		/// <remarks>
		/// The "clause modifier of a preposition" grammatical relation.
		/// (PP (P 因为)
		/// (IP
		/// (VP
		/// (VP
		/// (ADVP (AD 一))
		/// (VP (VV 开始)))
		/// (VP
		/// (ADVP (AD 就))
		/// (ADVP (AD 比较))
		/// (VP (VA 规范)))))))
		/// <code> pccomp </code> (因为-18, 开始-20)
		/// </remarks>
		public static readonly GrammaticalRelation ClModifier = new GrammaticalRelation(Language.Chinese, "pccomp", "clause complement of a preposition", Modifier, "^PP|IP", tregexCompiler, "PP < (P $+ IP|VP =target)", "IP < (CP=target $++ VP)");

		/// <summary>The "prepositional localizer modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "prepositional localizer modifier" grammatical relation.
		/// (PP (P 在)
		/// (LCP
		/// (NP
		/// (DP (DT 这)
		/// (CLP (M 片)))
		/// (NP (NN 热土)))
		/// (LC 上)))
		/// plmod(在-25, 上-29)
		/// </remarks>
		public static readonly GrammaticalRelation PrepositionalLocModifier = new GrammaticalRelation(Language.Chinese, "plmod", "prepositional localizer modifier", Modifier, "PP", tregexCompiler, "PP < ( P $++ LCP=target )");

		/// <summary>The "aspect marker" grammatical relation.</summary>
		/// <remarks>
		/// The "aspect marker" grammatical relation.
		/// (VP
		/// (ADVP (AD 基本))
		/// (VP (VV 做到) (AS 了)
		/// <code> asp </code> (做到,了)
		/// </remarks>
		public static readonly GrammaticalRelation PredicateAspect = new GrammaticalRelation(Language.Chinese, "asp", "aspect", Modifier, "VP", tregexCompiler, "VP < ( /^V*/ $+ AS=target)");

		/// <summary>The "participial modifier" grammatical relation.</summary>
		public static readonly GrammaticalRelation PartVerb = new GrammaticalRelation(Language.Chinese, "prtmod", "particle verb", Modifier, "VP|IP", tregexCompiler, "VP|IP < ( MSP=target )");

		/// <summary>The "etc" grammatical relation.</summary>
		/// <remarks>
		/// The "etc" grammatical relation.
		/// (NP
		/// (NP (NN 经济) (PU 、) (NN 贸易) (PU 、) (NN 建设) (PU 、) (NN 规划) (PU 、) (NN 科技) (PU 、) (NN 文教) (ETC 等))
		/// (NP (NN 领域)))))
		/// <code> etc </code> (办法-70, 等-71)
		/// </remarks>
		public static readonly GrammaticalRelation Etc = new GrammaticalRelation(Language.Chinese, "etc", "ETC", Modifier, "^NP", tregexCompiler, "/^NP/ < (NN|NR . ETC=target)");

		/// <summary>The "semantic dependent" grammatical relation.</summary>
		public static readonly GrammaticalRelation SemanticDependent = new GrammaticalRelation(Language.Chinese, "sdep", "semantic dependent", GrammaticalRelation.Dependent);

		/// <summary>The "xsubj" grammatical relation.</summary>
		/// <remarks>
		/// The "xsubj" grammatical relation.
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
		/// <code> xsubj </code> (完善-26, 有些-14)
		/// </remarks>
		public static readonly GrammaticalRelation ControlledSubject = new GrammaticalRelation(Language.Chinese, "xsubj", "controlled subject", SemanticDependent, "VP", tregexCompiler, "VP !< NP < VP > (IP !$- NP !< NP !>> (VP < VC ) >+(VP) (VP $-- NP=target))"
			);

		private static readonly GrammaticalRelation[] rawValues = new GrammaticalRelation[] { AdjectivalModifier, AdverbialModifier, Argument, AssociativemModifier, AssociativeModifier, Attributive, AuxModifier, AuxPassiveModifier, Ba, ClausalComplement
			, ClausalSubject, ClModifier, Complement, Complementizer, Conjunct, ControlledSubject, Coordination, GrammaticalRelation.Dependent, Determiner, DirectObject, DvpmModifier, DvpModifier, Etc, GrammaticalRelation.Governor, IpModifier, LcComplement
			, ModalVerb, Modifier, NegationModifier, NominalPassiveSubject, NominalSubject, NounCompoundModifier, NumberModifier, NumericModifier, Object, OdnumericModifier, PartVerb, PredicateAspect, PrepositionalLocModifier, PrepositionalModifier, PrepositionalObject
			, PrnModifier, Punctuation, Range, RelativeClauseModifier, ResVerb, SemanticDependent, Subject, TemporalModifier, Time, TimePostposition, TopSubject, VerbCompound, XclausalComplement };

		private static readonly IList<GrammaticalRelation> values = new List<GrammaticalRelation>();

		private static readonly IList<GrammaticalRelation> synchronizedValues = Java.Util.Collections.SynchronizedList(values);

		public static readonly ICollection<GrammaticalRelation> universalValues = new HashSet<GrammaticalRelation>();

		/// <summary>
		/// Map from Chinese GrammaticalRelation short names to their corresponding
		/// GrammaticalRelation objects.
		/// </summary>
		public static readonly IDictionary<string, GrammaticalRelation> shortNameToGRel = new ConcurrentHashMap<string, GrammaticalRelation>();

		static ChineseGrammaticalRelations()
		{
			/*
			* The "predicate" grammatical relation.
			*/
			// Fri Feb 20 15:42:54 2009 (pichuan)
			// I'm surprise this relation has patterns.
			// However it doesn't seem to match anything in CTB6.
			/*
			public static final GrammaticalRelation PREDICATE =
			new GrammaticalRelation(Language.Chinese, "pred", "predicate",
			PredicateGRAnnotation.class, DEPENDENT, "IP",
			new String[]{
			" IP=target !> IP"
			});
			public static class PredicateGRAnnotation
			extends GrammaticalRelationAnnotation { }
			*/
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
			// This following rule is too general and collides with 'ccomp'.
			// Delete it for now.
			// TODO: come up with a new rule. Does this exist in Chinese?
			//"IP < (IP=target $+ ( VP !< VC))",
			/*
			* The "indirect object" grammatical relation.
			*/
			// deleted by pichuan: no real matches
			/*
			public static final GrammaticalRelation INDIRECT_OBJECT =
			new GrammaticalRelation(Language.Chinese,
			"iobj", "indirect object",
			IndirectObjectGRAnnotation.class,  OBJECT, "VP",
			new String[]{
			" CP !> VP < ( VV $+ ( NP|DP|QP|CLP=target . NP|DP ) )"
			});
			public static class IndirectObjectGRAnnotation
			extends GrammaticalRelationAnnotation { }
			*/
			//        "  VP|IP <  ( VV|VC|VRD|VCD !$+  NP|QP|LCP ) > (IP   < IP|VP|VRD|VCD=target)   "
			//          "VP < (S=target < (VP !<, TO|VBG) !$-- NP)",
			// pichuan: this is difficult to recognize in Chinese.
			// remove the rules since it (always) collides with ccomp
			// TODO: these rules seem to always collide with ccomp.
			// Is this really desirable behavior?
			//"VP !> (/^VP/ < /^VC$/ ) < (IP=target < (VP < P))",
			//"ADJP < (IP=target <, (VP <, P))",
			//"VP < (IP=target < (NP $+ NP|ADJP))",
			//"VP < (/^VC/ $+ (VP=target < VC < NP))"
			/*
			* The "adjectival complement" grammatical relation.
			* Example:
			*/
			// deleted by pichuan: no real matches
			/*
			public static final GrammaticalRelation ADJECTIVAL_COMPLEMENT =
			new GrammaticalRelation(Language.Chinese,
			"acomp", "adjectival complement",
			AdjectivalComplementGRAnnotation.class, COMPLEMENT, "VP", tregexCompiler,
			new String[]{
			"VP < (ADJP=target !$-- NP)"
			});
			public static class AdjectivalComplementGRAnnotation
			extends GrammaticalRelationAnnotation { }
			*/
			/* This rule actually matches nothing.
			There's another tmod rule. This is removed for now.
			(pichuan) Sun Mar  8 18:22:40 2009
			*/
			/*
			public static final GrammaticalRelation TEMPORAL_MODIFIER =
			new GrammaticalRelation(Language.Chinese,
			"tmod", "temporal modifier",
			TemporalModifierGRAnnotation.class, MODIFIER, "VP|IP|ADJP", tregexCompiler,
			new String[]{
			" VC|VE ! >> VP|ADJP < NP=target < NT",
			"VC|VE !>>IP <( NP=target < NT $++ VP !< VC|VE )"
			});
			public static class TemporalModifierGRAnnotation
			extends GrammaticalRelationAnnotation { }
			*/
			// pichuan: previously "tclaus"
			// TODO: we should figure out various ways to improve this pattern to
			// improve both its precision and recall
			//"DP < DT < QP=target"
			/*
			* The "possession modifier" grammatical relation.
			*/
			// Fri Feb 20 15:40:13 2009 (pichuan)
			// I think this "poss" relation is just WRONG.
			// DEC is a complementizer or a nominalizer,
			// this rule probably originally want to capture "DEG".
			// But it seems like it's covered by "assm" (associative marker).
			/*
			public static final GrammaticalRelation POSSESSION_MODIFIER =
			new GrammaticalRelation(Language.Chinese,
			"poss", "possession modifier",
			PossessionModifierGRAnnotation.class, MODIFIER, "NP", tregexCompiler,
			new String[]{
			"NP < ( PN=target $+ DEC $+  NP )"
			});
			public static class PossessionModifierGRAnnotation
			extends GrammaticalRelationAnnotation { }
			*/
			/*
			* The "possessive marker" grammatical relation.
			*/
			// Similar to the comments to "poss",
			// I think this relation is wrong and will not appear.
			/*
			public static final GrammaticalRelation POSSESSIVE_MODIFIER =
			new GrammaticalRelation(Language.Chinese, "possm", "possessive marker",
			PossessiveModifierGRAnnotation.class,
			MODIFIER, "NP", tregexCompiler,
			new String[]{
			"NP < ( PN $+ DEC=target ) "
			});
			public static class PossessiveModifierGRAnnotation
			extends GrammaticalRelationAnnotation { }
			*/
			// pichuan: previously "clmpd"
			//ADJECTIVAL_COMPLEMENT,
			//INDIRECT_OBJECT,
			//POSSESSION_MODIFIER,
			//POSSESSIVE_MODIFIER,
			//PREDICATE,
			// Cache frequently used views of the values list
			for (int i = 0; i < rawValues.Length; i++)
			{
				GrammaticalRelation gr = rawValues[i];
				synchronizedValues.Add(gr);
			}
			ValuesLock().Lock();
			try
			{
				foreach (GrammaticalRelation gr in Edu.Stanford.Nlp.Trees.International.Pennchinese.ChineseGrammaticalRelations.Values())
				{
					shortNameToGRel[gr.GetShortName()] = gr;
				}
			}
			finally
			{
				ValuesLock().Unlock();
			}
		}
	}
}
