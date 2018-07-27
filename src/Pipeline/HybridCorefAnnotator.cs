using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Coref.Hybrid;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Pipeline
{
	public class HybridCorefAnnotator : TextAnnotationCreator, IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.HybridCorefAnnotator));

		private const bool Verbose = false;

		private readonly HybridCorefSystem corefSystem;

		private readonly bool OldFormat;

		public HybridCorefAnnotator(Properties props)
		{
			// for backward compatibility
			try
			{
				// Load the default properties
				Properties corefProps = new Properties();
				try
				{
					using (BufferedReader reader = IOUtils.ReaderFromString("edu/stanford/nlp/hcoref/properties/coref-default-dep.properties"))
					{
						corefProps.Load(reader);
					}
				}
				catch (IOException)
				{
				}
				// Add passed properties
				IEnumeration<object> keys = props.Keys;
				while (keys.MoveNext())
				{
					string key = keys.Current.ToString();
					corefProps.SetProperty(key, props.GetProperty(key));
				}
				// Create coref system
				corefSystem = new HybridCorefSystem(corefProps);
				OldFormat = bool.ParseBoolean(props.GetProperty("oldCorefFormat", "false"));
			}
			catch (Exception e)
			{
				log.Error("cannot create HybridCorefAnnotator!");
				Sharpen.Runtime.PrintStackTrace(e);
				throw new Exception(e);
			}
		}

		public virtual void Annotate(Annotation annotation)
		{
			try
			{
				if (!annotation.ContainsKey(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					log.Error("this coreference resolution system requires SentencesAnnotation!");
					return;
				}
				if (HasSpeakerAnnotations(annotation))
				{
					annotation.Set(typeof(CoreAnnotations.UseMarkedDiscourseAnnotation), true);
				}
				Document corefDoc = corefSystem.docMaker.MakeDocument(annotation);
				IDictionary<int, CorefChain> result = corefSystem.Coref(corefDoc);
				annotation.Set(typeof(CorefCoreAnnotations.CorefChainAnnotation), result);
				// for backward compatibility
				if (OldFormat)
				{
					AnnotateOldFormat(result, corefDoc);
				}
			}
			catch (Exception e)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		public static IList<Pair<IntTuple, IntTuple>> GetLinks(IDictionary<int, CorefChain> result)
		{
			IList<Pair<IntTuple, IntTuple>> links = new List<Pair<IntTuple, IntTuple>>();
			CorefChain.CorefMentionComparator comparator = new CorefChain.CorefMentionComparator();
			foreach (CorefChain c in result.Values)
			{
				IList<CorefChain.CorefMention> s = c.GetMentionsInTextualOrder();
				foreach (CorefChain.CorefMention m1 in s)
				{
					foreach (CorefChain.CorefMention m2 in s)
					{
						if (comparator.Compare(m1, m2) == 1)
						{
							links.Add(new Pair<IntTuple, IntTuple>(m1.position, m2.position));
						}
					}
				}
			}
			return links;
		}

		private static void AnnotateOldFormat(IDictionary<int, CorefChain> result, Document corefDoc)
		{
			IList<Pair<IntTuple, IntTuple>> links = GetLinks(result);
			Annotation annotation = corefDoc.annotation;
			//
			// save the coref output as CorefGraphAnnotation
			//
			// this graph is stored in CorefGraphAnnotation -- the raw links found by the coref system
			IList<Pair<IntTuple, IntTuple>> graph = new List<Pair<IntTuple, IntTuple>>();
			foreach (Pair<IntTuple, IntTuple> link in links)
			{
				//
				// Note: all offsets in the graph start at 1 (not at 0!)
				//       we do this for consistency reasons, as indices for syntactic dependencies start at 1
				//
				int srcSent = link.first.Get(0);
				int srcTok = corefDoc.GetOrderedMentions()[srcSent - 1][link.first.Get(1) - 1].headIndex + 1;
				int dstSent = link.second.Get(0);
				int dstTok = corefDoc.GetOrderedMentions()[dstSent - 1][link.second.Get(1) - 1].headIndex + 1;
				IntTuple dst = new IntTuple(2);
				dst.Set(0, dstSent);
				dst.Set(1, dstTok);
				IntTuple src = new IntTuple(2);
				src.Set(0, srcSent);
				src.Set(1, srcTok);
				graph.Add(new Pair<IntTuple, IntTuple>(src, dst));
			}
			annotation.Set(typeof(CorefCoreAnnotations.CorefGraphAnnotation), graph);
			foreach (CorefChain corefChain in result.Values)
			{
				if (corefChain.GetMentionsInTextualOrder().Count < 2)
				{
					continue;
				}
				ICollection<CoreLabel> coreferentTokens = Generics.NewHashSet();
				foreach (CorefChain.CorefMention mention in corefChain.GetMentionsInTextualOrder())
				{
					ICoreMap sentence = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation))[mention.sentNum - 1];
					CoreLabel token = sentence.Get(typeof(CoreAnnotations.TokensAnnotation))[mention.headIndex - 1];
					coreferentTokens.Add(token);
				}
				foreach (CoreLabel token_1 in coreferentTokens)
				{
					token_1.Set(typeof(CorefCoreAnnotations.CorefClusterAnnotation), coreferentTokens);
				}
			}
		}

		private static bool HasSpeakerAnnotations(Annotation annotation)
		{
			foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (CoreLabel t in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					if (t.Get(typeof(CoreAnnotations.SpeakerAnnotation)) != null)
					{
						return true;
					}
				}
			}
			return false;
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation
				), typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation), typeof(CorefCoreAnnotations.CorefMentionsAnnotation))));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.Singleton(typeof(CorefCoreAnnotations.CorefChainAnnotation));
		}

		private static Annotation TestEnglish()
		{
			string text = "Barack Obama is the president of United States. He visited California last week.";
			return TestAnnoation(text, new string[] { "-props", "edu/stanford/nlp/hcoref/properties/coref-default-dep.properties" });
		}

		private static Annotation TestChinese()
		{
			//    String text = "中国武道太学和中国书道太学成立。新华社北京９月１日电。旨在振兴中华文化于"
			//        + "国际的中国武道太学和中国书道太学今天在北京成立。上述两所太学是在国家体委、"
			//        + "文化部、中国武术研究院、中国艺术研究院的关杯和支持下，在台湾著名企业家、书"
			//        + "画家、艺术品收藏家李志仁先生倡议和出资下，经国家教委和北京市成人教育局批准"
			//        + "而成立的。李志仁先生在台湾有“笔墨大王”之称，近几年先后出资一千万元新台币"
			//        + "，在中国大陆老、少、边、穷地区建立了百所小学，受到海内外人士的称赞。（完）\n";
			string text = "俄罗斯 航空 公司 一 名 官员 在 ９号 说 ， 米洛舍维奇 的 儿子 马可·米洛舍维奇 ９号 早上 持 外交 护照 从 俄国 首都 莫斯科 搭机 飞往 中国 大陆 北京 ， 可是 就 在 稍后 就 返回 莫斯科 。 这 名 俄国 航空 公司 官员 说 马可 是 因为 护照 问题 而 在 北京 机场 被 中共 遣返 莫斯科 。 北京 机场 方面 的 这 项 举动 清楚 显示 中共 有意 放弃 在 总统 大选 落败 的 前 南斯拉夫 总统 米洛舍维奇 ， 因此 他 在 南斯拉夫 受到 民众 厌恶 的 儿子 马可 才 会 在 北京 机场 被 中共 当局 送回 莫斯科 。 马可 持 外交 护照 能够 顺利 搭机 离开 莫斯科 ， 但是 却 在 北京 受阻 ， 可 算是 踢到 了 铁板 。 可是 这 项 消息 和 先前 外界 谣传 中共 当局 准备 提供 米洛舍维奇 和 他 的 家人 安全 庇护所 有 着 很 大 的 出入 ， 一般 认为 在 去年 米洛舍维奇 挥兵 攻打 科索沃 境内 阿尔巴尼亚 一 分离主义 分子 的 时候 ， 强力 反对 北约 组织 攻击 南斯拉夫 的 中共 ， 会 全力 保护 米洛舍维奇 和 他 的 家人 及 亲信 。 可是 从 ９号 马可 被 送回 莫斯科 一 事 看 起来 ， 中共 很 可能 会 放弃 米洛舍维奇 。";
			return TestAnnoation(text, new string[] { "-props", "edu/stanford/nlp/hcoref/properties/zh-dcoref-default.properties" });
		}

		private static Annotation TestAnnoation(string text, string[] args)
		{
			Annotation document = new Annotation(text);
			Properties props = StringUtils.ArgsToProperties(args);
			StanfordCoreNLP corenlp = new StanfordCoreNLP(props);
			corenlp.Annotate(document);
			Edu.Stanford.Nlp.Pipeline.HybridCorefAnnotator hcoref = new Edu.Stanford.Nlp.Pipeline.HybridCorefAnnotator(props);
			hcoref.Annotate(document);
			return document;
		}

		public static void Main(string[] args)
		{
			//    String text = "Since the implementation of the Individual Visit Scheme between Hong Kong and the mainland , more and more mainland tourists are coming to visit Hong Kong. "
			//                  +"From the beginning up till now , more than seven million individual tourists , have come to Hong Kong. "
			//                  +"Well , we now , er , believe more will be coming . "
			//                  +"At this point , it has been about two years . "
			//                  +"Also , the current number of 34 cities will be increased . "
			//                  +"Hong Kong was developed from a fishing harbor one hundred years ago to become today 's international metropolis . "
			//                  +"Here , eastern and western cultures have gathered , and the new and the old coexist . "
			//                  +"When in Hong Kong , you can wander among skyscrapers , heartily enjoy shopping sprees in well - known stores and malls for goods from various countries , and taste delicious snacks from all over the world at tea shops or at street stands in Mong Kok . "
			//                  +"You can go to burn incense and make a vow at the Repulse Bay , where all deities gather . "
			//                  +"You can enjoy the most charming sun - filled sandy beaches in Hong Kong. "
			//                  +"You can ascend Victoria Peak to get a panoramic view of Victoria Harbor 's beautiful scenery . "
			//                  +"Or hop onto a trolley with over a century of history , and feel the city 's blend of the old and the modern in slow motion .";
			//
			Annotation document = TestChinese();
			System.Console.Out.WriteLine(document.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation)));
			log.Info();
		}
	}
}
