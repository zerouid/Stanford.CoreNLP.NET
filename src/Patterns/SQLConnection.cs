using Java.Sql;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns
{
	/// <summary>Created by sonalg on 10/8/14.</summary>
	public class SQLConnection
	{
		public static string dbLocation;

		public static string dbusername;

		public static string dbpassword;

		public static string host;

		/// <exception cref="Java.Sql.SQLException"/>
		public static IConnection GetConnection()
		{
			//System.out.println("username is " + dbusername + " and location is " + dbLocation);
			return DriverManager.GetConnection(dbLocation + "?host=" + host + "&user=" + dbusername + "&password=" + dbpassword + "&characterEncoding=utf-8&" + "useUnicode=true");
		}
	}
}
