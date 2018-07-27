

namespace Edu.Stanford.Nlp.IE.Pascal
{
	/// <summary>
	/// A partial
	/// <see cref="PascalTemplate"/>
	/// .  Holds date fields only.
	/// </summary>
	/// <author>Chris Cox</author>
	public class DateTemplate
	{
		public string subdate = "1/1/1000";

		public string noadate = "1/1/1000";

		public string crcdate = "1/1/1000";

		public string workdate = "1/1/1000";

		public DateTemplate(string subdate, string noadate, string crcdate, string workdate)
		{
			if (subdate != null)
			{
				this.subdate = subdate;
			}
			if (noadate != null)
			{
				this.noadate = noadate;
			}
			if (crcdate != null)
			{
				this.crcdate = crcdate;
			}
			if (workdate != null)
			{
				this.workdate = workdate;
			}
		}

		public override int GetHashCode()
		{
			int tally = 31;
			int n = 3;
			tally = tally + n * subdate.GetHashCode() + n * n * noadate.GetHashCode() + n * n * n * crcdate.GetHashCode() + n * workdate.GetHashCode();
			return tally;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (!(obj is Edu.Stanford.Nlp.IE.Pascal.DateTemplate))
			{
				return false;
			}
			Edu.Stanford.Nlp.IE.Pascal.DateTemplate d = (Edu.Stanford.Nlp.IE.Pascal.DateTemplate)obj;
			return (subdate.Equals(d.subdate) && noadate.Equals(d.noadate) && crcdate.Equals(d.crcdate) && workdate.Equals(d.workdate));
		}

		public override string ToString()
		{
			return (" Sub:" + subdate + " Noa:" + noadate + " Crc:" + crcdate + " Wrk:" + workdate);
		}
	}
}
