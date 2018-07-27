using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.International.Arabic
{
	/// <summary>Manually-generated (unvocalized) word lists for different Arabic word categories.</summary>
	/// <author>Spence Green</author>
	[System.Serializable]
	public sealed class ArabicWordLists
	{
		private const long serialVersionUID = 2752179429568209320L;

		private ArabicWordLists()
		{
		}

		public static ICollection<string> GetTemporalNouns()
		{
			return Java.Util.Collections.UnmodifiableSet(tmpNouns);
		}

		public static ICollection<string> GetInnaSisters()
		{
			return Java.Util.Collections.UnmodifiableSet(innaSisters);
		}

		public static ICollection<string> GetKanSisters()
		{
			return Java.Util.Collections.UnmodifiableSet(kanSisters);
		}

		public static ICollection<string> GetDimirMunfasala()
		{
			return Java.Util.Collections.UnmodifiableSet(dimirMunfasala);
		}

		public static ICollection<string> GetDimirMutasala()
		{
			return Java.Util.Collections.UnmodifiableSet(dimirMutasala);
		}

		private static readonly ICollection<string> dimirMunfasala = Generics.NewHashSet();

		static ArabicWordLists()
		{
			dimirMunfasala.Add("انا");
			dimirMunfasala.Add("هو");
			dimirMunfasala.Add("هي");
			dimirMunfasala.Add("انت");
			//Unvocalized
			dimirMunfasala.Add("نحن");
			dimirMunfasala.Add("انتم");
			dimirMunfasala.Add("انتن");
			dimirMunfasala.Add("هما");
			dimirMunfasala.Add("هم");
			dimirMunfasala.Add("هن");
		}

		private static readonly ICollection<string> dimirMutasala = Generics.NewHashSet();

		static ArabicWordLists()
		{
			dimirMutasala.Add("ي");
			dimirMutasala.Add("ه");
			dimirMutasala.Add("ها");
			dimirMutasala.Add("ك");
			dimirMutasala.Add("كن");
			dimirMutasala.Add("كم");
			dimirMutasala.Add("نا");
			dimirMutasala.Add("هم");
			dimirMutasala.Add("هن");
			dimirMutasala.Add("هما");
		}

		private static readonly ICollection<string> innaSisters = Generics.NewHashSet();

		static ArabicWordLists()
		{
			innaSisters.Add("ان");
			innaSisters.Add("لكن");
			innaSisters.Add("لعل");
			innaSisters.Add("لان");
		}

		private static readonly ICollection<string> kanSisters = Generics.NewHashSet();

		static ArabicWordLists()
		{
			kanSisters.Add("كان");
			kanSisters.Add("كانت");
			kanSisters.Add("كنت");
			kanSisters.Add("كانوا");
			kanSisters.Add("كن");
		}

		private static readonly ICollection<string> tmpNouns = Generics.NewHashSet();

		static ArabicWordLists()
		{
			tmpNouns.Add("الان");
			tmpNouns.Add("يوم");
			tmpNouns.Add("اليوم");
			tmpNouns.Add("امس");
			tmpNouns.Add("ايام");
			tmpNouns.Add("مساء");
			tmpNouns.Add("صباحا");
			tmpNouns.Add("الصباح");
			tmpNouns.Add("الاثنين");
			tmpNouns.Add("الأثنين");
			tmpNouns.Add("الاحد");
			tmpNouns.Add("الأحد");
			tmpNouns.Add("الثلاثاء");
			tmpNouns.Add("الارباء");
			tmpNouns.Add("الخميس");
			tmpNouns.Add("الجمعة");
			tmpNouns.Add("السبت");
			tmpNouns.Add("عام");
			tmpNouns.Add("عاما");
			tmpNouns.Add("سنة");
			tmpNouns.Add("سنوات");
			tmpNouns.Add("شهر");
			tmpNouns.Add("شهور");
			tmpNouns.Add("يناير");
			tmpNouns.Add("كانون");
			//Only one part of Dec/Jan
			tmpNouns.Add("فبراير");
			tmpNouns.Add("شباط");
			tmpNouns.Add("مارس");
			tmpNouns.Add("اذار");
			tmpNouns.Add("ابريل");
			tmpNouns.Add("نيسان");
			tmpNouns.Add("مايو");
			tmpNouns.Add("ايار");
			tmpNouns.Add("يونيو");
			tmpNouns.Add("حزيران");
			tmpNouns.Add("يوليو");
			tmpNouns.Add("تموز");
			tmpNouns.Add("اغسطس");
			tmpNouns.Add("اب");
			tmpNouns.Add("سبتمبر");
			tmpNouns.Add("ايلول");
			tmpNouns.Add("اكتوبر");
			tmpNouns.Add("تشرين");
			//Only one part of Oct/Nov
			tmpNouns.Add("نوفمبر");
			tmpNouns.Add("ديسمبر");
		}
	}
}
