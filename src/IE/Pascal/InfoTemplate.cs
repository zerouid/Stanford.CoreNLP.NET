using Sharpen;

namespace Edu.Stanford.Nlp.IE.Pascal
{
	/// <summary>
	/// A partial
	/// <see cref="PascalTemplate"/>
	/// .
	/// Holds URL, acronym, and name template fields.
	/// </summary>
	/// <author>Chris Cox</author>
	public class InfoTemplate
	{
		internal string whomepage = "null";

		internal string wacronym = "null";

		internal string wname = "null";

		internal string chomepage = "null";

		internal string cacronym = "null";

		internal string cname = "null";

		public InfoTemplate(string whomepage, string wacronym, string wname, string chomepage, string cacronym, string cname, CliqueTemplates ct)
		{
			if (whomepage != null)
			{
				this.whomepage = whomepage;
			}
			if (wacronym != null)
			{
				this.wacronym = PascalTemplate.StemAcronym(wacronym, ct);
			}
			if (wname != null)
			{
				this.wname = wname;
			}
			if (chomepage != null)
			{
				this.chomepage = chomepage;
			}
			if (cacronym != null)
			{
				this.cacronym = PascalTemplate.StemAcronym(cacronym, ct);
			}
			if (cname != null)
			{
				this.cname = cname;
			}
		}

		public override int GetHashCode()
		{
			int tally = 31;
			int n = 7;
			tally = whomepage.GetHashCode() + n * wacronym.GetHashCode() + n * n * wname.GetHashCode();
			tally += (chomepage.GetHashCode() + n * cacronym.GetHashCode() + n * n * cname.GetHashCode());
			return tally;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (!(obj is Edu.Stanford.Nlp.IE.Pascal.InfoTemplate))
			{
				return false;
			}
			Edu.Stanford.Nlp.IE.Pascal.InfoTemplate i = (Edu.Stanford.Nlp.IE.Pascal.InfoTemplate)obj;
			return (whomepage.Equals(i.whomepage) && wacronym.Equals(i.wacronym) && wname.Equals(i.wname) && chomepage.Equals(i.chomepage) && cacronym.Equals(i.cacronym) && cname.Equals(i.cname));
		}

		public override string ToString()
		{
			return ("W_URL: " + whomepage + " W_ACRO: " + wacronym + " W_NAME: " + wname + "\nC_URL: " + chomepage + " C_ACRO: " + cacronym + " C_NAME: " + cname);
		}
	}
}
