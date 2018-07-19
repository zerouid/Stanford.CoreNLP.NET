using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International.Spanish;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Spanish.Process
{
	/// <summary>A utility for preprocessing the AnCora Spanish corpus.</summary>
	/// <remarks>
	/// A utility for preprocessing the AnCora Spanish corpus.
	/// Attempts to disambiguate Spanish personal pronouns which have
	/// multiple senses:
	/// <em>me, te, se, nos, os</em>
	/// Each of these can be used as 1) an indirect object pronoun or as
	/// 2) a reflexive pronoun. (<em>me, te, nos,</em> and <em>os</em> can
	/// also be used as direct object pronouns.)
	/// For the purposes of corpus preprocessing, all we need is to
	/// distinguish between the object- and reflexive-pronoun cases.
	/// Disambiguation is done first by (dictionary-powered) heuristics, and
	/// then by brute force. The brute-force decisions are manual tags for
	/// verbs with clitic pronouns which appear in the AnCora corpus.
	/// </remarks>
	/// <author>Jon Gauthier</author>
	/// <seealso cref="Edu.Stanford.Nlp.Trees.International.Spanish.SpanishTreeNormalizer"/>
	public class AnCoraPronounDisambiguator
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(AnCoraPronounDisambiguator));

		public enum PersonalPronounType
		{
			Object,
			Reflexive,
			Unknown
		}

		private static readonly ICollection<string> ambiguousPersonalPronouns = new HashSet<string>(Arrays.AsList("me", "te", "se", "nos", "os"));

		/// <summary>
		/// The following verbs always use ambiguous pronouns in a reflexive
		/// sense in the corpus.
		/// </summary>
		private static readonly ICollection<string> alwaysReflexiveVerbs = new HashSet<string>(Arrays.AsList("acercar", "acostumbrar", "adaptar", "afeitar", "agarrar", "ahincar", "alegrar", "Anticipar", "aplicar", "aprobar", "aprovechar", "asegurar"
			, "Atreve", "bajar", "beneficiar", "callar", "casar", "cobrar", "colocar", "comer", "comportar", "comprar", "concentrar", "cuidar", "deber", "decidir", "defender", "desplazar", "detectar", "divirtiendo", "echar", "encontrar", "enfrentar", "entender"
			, "enterar", "entrometer", "equivocar", "escapar", "esconder", "esforzando", "establecer", "felicitar", "fija", "Fija", "ganar", "guarda", "guardar", "Habituar", "hacer", "imagina", "imaginar", "iniciar", "inscribir", "ir", "jode", "jugar", 
			"Levantar", "Manifestar", "mantener", "marchar", "meter", "Negar", "obsesionar", "Olvida", "Olvidar", "olvidar", "oponer", "Para", "pasar", "plantear", "poner", "pudra", "queda", "quedar", "querer", "quita", "reciclar", "reconoce", "reconstruir"
			, "recordar", "recuperar", "reencontrar", "referir", "registrar", "reincorporar", "rendir", "reservar", "retirar", "reunir", "sentar", "sentir", "someter", "subir", "tirando", "toma", "tomar", "tomen", "Une", "unir", "Ve", "vestir"));

		/// <summary>
		/// The following verbs always use ambiguous clitic pronouns in an
		/// object sense **in the corpora supported.
		/// </summary>
		/// <remarks>
		/// The following verbs always use ambiguous clitic pronouns in an
		/// object sense **in the corpora supported.
		/// This does not imply that the below verbs are only ever non-reflexive!
		/// This list may need to be revised in order to produce correct gold trees
		/// on new datasets.
		/// </remarks>
		private static readonly ICollection<string> neverReflexiveVerbs = new HashSet<string>(Arrays.AsList("abrir", "aguar", "anunciar", "arrebatando", "arruinar", "clasificar", "compensar", "compra", "comprar", "concretar", "contar", "crea", "crear"
			, "Cuente", "Decir", "decir", "deja", "digan", "devolver", "devuelve", "dirigiendo", "distraer", "enfrascar", "exigiendo", "exigir", "haz", "ignorar", "impedir", "insultar", "juzgar", "llamar", "llevando", "llevar", "manda", "mirar", "Miren"
			, "multar", "negar", "ocultando", "pagar", "patear", "pedir", "permitir", "pidiendo", "preguntar", "prevenir", "quitar", "razona", "resultar", "saca", "sacar", "saludar", "seguir", "servir", "situar", "suceder", "tener", "tutear", "utilizar"
			, "vender", "ver", "visitar"));

		/// <summary>
		/// Brute-force: based on clauses which we recognize from AnCora,
		/// dictate the type of pronoun being used
		/// Map from pair (verb, containing clause) to personal pronoun type
		/// </summary>
		private static readonly IDictionary<Pair<string, string>, AnCoraPronounDisambiguator.PersonalPronounType> bruteForceDecisions = new Dictionary<Pair<string, string>, AnCoraPronounDisambiguator.PersonalPronounType>();

		static AnCoraPronounDisambiguator()
		{
			bruteForceDecisions[new Pair<string, string>("contar", "No contarte mi vida nunca más")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("Creer", "Creerselo todo")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("creer", "creérselo todo ...")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("creer", "creerte")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("Dar", "Darte de alta ahi")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("da", "A mi dame billetes uno al lado del otro que es la forma mas líquida que uno pueda estar")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("da", "danos UNA razon UNA")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("da", "y ... dame una razon por la que hubiera matado o se hubiera comido a el compañero ?")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("dar", "darme cuenta")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("dar", "darme la enhorabuena")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("dar", "darnos cuenta")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("dar", "darselo a la doña")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("dar", "darte cuenta")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("dar", "darte de alta")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("dar", "darte vuelta en cuestiones que no tienen nada que ver con lo que comenzaste diciendo")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("dar", "podría darnos")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("dar", "puede darnos")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("decir", "suele decirnos")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("decir", "suelo decírmelo")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("dejar", "debería dejarnos faenar")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("dejar", "dejarme un intermitente encendido")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("dejar", ": dejarnos un país tan limpio en su gobierno como el cielo claro después de las tormentas mediterráneas , que inundan nuestras obras públicas sin encontrar nunca ni un solo responsable político de tanta mala gestión , ya sea la plaza de Cerdà socialista o los incendios forestales de la Generalitat"
				)] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("dejar", "podemos dejarnos adormecer")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("engañar", "engañarnos")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("estira", "=LRB= al menos estirate a los japoneses HDP !!! =RRB=")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("explica", "explicame como hago")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("explicar", "deberá explicarnos")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("liar", "liarme a tiros")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("librar", "librarme de el mismo para siempre")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("llevar", "llevarnos a una trampa en esta elección")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("manifestar", "manifestarme su solidaridad")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("manifestar", "manifestarnos sobre las circunstancias que mantienen en vilo la vida y obra de los colombianos")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("mirando", "estábamos mirándonos")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			bruteForceDecisions[new Pair<string, string>("poner", "ponerme en ascuas")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("servir", "servirme de guía")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("volver", "debe volvernos")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("volver", "deja de volverme")] = AnCoraPronounDisambiguator.PersonalPronounType.Object;
			bruteForceDecisions[new Pair<string, string>("volver", "volvernos")] = AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
		}

		/// <summary>Determine if the given pronoun can have multiple senses.</summary>
		public static bool IsAmbiguous(string pronoun)
		{
			return ambiguousPersonalPronouns.Contains(pronoun);
		}

		/// <summary>
		/// Determine whether the given clitic pronoun is an indirect object
		/// pronoun or a reflexive pronoun.
		/// </summary>
		/// <remarks>
		/// Determine whether the given clitic pronoun is an indirect object
		/// pronoun or a reflexive pronoun.
		/// This method is only defined when the pronoun is one of
		/// me, te, se, nos, os
		/// i.e., those in which the meaning is actually ambiguous.
		/// </remarks>
		/// <param name="strippedVerb">
		/// Stripped verb as returned by
		/// <see cref="Edu.Stanford.Nlp.International.Spanish.SpanishVerbStripper.SeparatePronouns(string)"/>
		/// .
		/// </param>
		/// <param name="pronounIdx">
		/// The index of the pronoun within
		/// <c>strippedVerb.getPronouns()</c>
		/// which should be
		/// disambiguated.
		/// </param>
		/// <param name="clauseYield">
		/// A string representing the yield of the
		/// clause which contains the given verb
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// If the given pronoun is
		/// not ambiguous, or its disambiguation is not supported.
		/// </exception>
		public static AnCoraPronounDisambiguator.PersonalPronounType DisambiguatePersonalPronoun(SpanishVerbStripper.StrippedVerb strippedVerb, int pronounIdx, string clauseYield)
		{
			IList<string> pronouns = strippedVerb.GetPronouns();
			string pronoun = pronouns[pronounIdx].ToLower();
			if (!ambiguousPersonalPronouns.Contains(pronoun))
			{
				throw new ArgumentException("We don't support disambiguating pronoun '" + pronoun + "'");
			}
			if (pronouns.Count == 1 && Sharpen.Runtime.EqualsIgnoreCase(pronoun, "se"))
			{
				return AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			}
			string verb = strippedVerb.GetStem();
			if (alwaysReflexiveVerbs.Contains(verb))
			{
				return AnCoraPronounDisambiguator.PersonalPronounType.Reflexive;
			}
			else
			{
				if (neverReflexiveVerbs.Contains(verb))
				{
					return AnCoraPronounDisambiguator.PersonalPronounType.Object;
				}
			}
			Pair<string, string> bruteForceKey = new Pair<string, string>(verb, clauseYield);
			if (bruteForceDecisions.Contains(bruteForceKey))
			{
				return bruteForceDecisions[bruteForceKey];
			}
			// Log this instance where a clitic pronoun could not be disambiguated.
			log.Info("Failed to disambiguate: " + verb + "\nContaining clause:\t" + clauseYield + "\n");
			return AnCoraPronounDisambiguator.PersonalPronounType.Unknown;
		}
	}
}
