using System.IO;
using Java.Net;
using Sharpen;

namespace Edu.Stanford.Nlp.Net
{
	/// <summary>Contains a couple useful utility methods related to networks.</summary>
	/// <remarks>
	/// Contains a couple useful utility methods related to networks.  For
	/// example, contains a method which checks if a port is available, and
	/// contains a method which scans a range of ports until it finds an
	/// available port.
	/// <br />
	/// </remarks>
	/// <author>John Bauer</author>
	public class Ports
	{
		/// <summary>Checks to see if a specific port is available.</summary>
		/// <remarks>
		/// Checks to see if a specific port is available.
		/// <br />
		/// Source: Apache's mina project, via stack overflow
		/// http://stackoverflow.com/questions/434718/
		/// sockets-discover-port-availability-using-java
		/// </remarks>
		/// <param name="port">the port to check for availability</param>
		public static bool Available(int port)
		{
			ServerSocket ss = null;
			DatagramSocket ds = null;
			try
			{
				ss = new ServerSocket(port);
				ss.SetReuseAddress(true);
				ds = new DatagramSocket(port);
				ds.SetReuseAddress(true);
				return true;
			}
			catch (IOException)
			{
			}
			finally
			{
				if (ds != null)
				{
					ds.Close();
				}
				if (ss != null)
				{
					try
					{
						ss.Close();
					}
					catch (IOException)
					{
					}
				}
			}
			// should not be thrown
			return false;
		}

		/// <summary>Scan a range of ports to find the first available one.</summary>
		public static int FindAvailable(int min, int max)
		{
			for (int port = min; port < max; ++port)
			{
				if (Available(port))
				{
					return port;
				}
			}
			return max;
		}
	}
}
