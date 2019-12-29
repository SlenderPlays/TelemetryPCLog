using System;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using CommandLine;

namespace TelemetryPCLog
{
	class Program
	{
		public class ArgumentBuilder
		{
			[Option('i', "infinite", Default = false, HelpText = "If set to true, it will always try to reconnect.", Required = false)]
			public bool argInfinite { get; set; }

			[Option('a', "attempts", Default = 1, HelpText = "How many times it should try to reconnect.", Required = false)]
			public int argAttempts { get; set; }


			[Option("ip", Default = "192.168.49.1", HelpText = "The IP of the Robot Controller.", Required = false)]
			public string argIp { get; set; }


			[Option('p', "port", Default = 8333, HelpText = "The port that is listening on the Robot Controller.", Required = false)]
			public int argPort { get; set; }

			[Option('d', "delay", Default = 100, HelpText = "The delay between each read in ms. Warning! For 0, it will use most of the system's resources, if you wish to let it use as much as possible, set the delay to -2, if you wish to let it read as much as it can with less resource usage, use -1!", Required = false)]
			public int argDelay { get; set; }
		}
		static NetworkStream stream;
		static bool heartbeatFailed = false;
		static Timer aTimer;

		static void Main(string[] args)
		{
			Parser.Default.ParseArguments<ArgumentBuilder>(args)
				   .WithParsed(options =>
				   {
					   aTimer = new System.Timers.Timer(10000);
					   aTimer.Enabled = false;
					   aTimer.Elapsed += SendHeartbeet;
					   aTimer.AutoReset = true;
					   Console.WriteLine("Starting connection...");

					   if (options.argInfinite)
					   {
						   ConnectForever(options.argIp, options.argPort,options.argDelay);
					   }
					   else
					   {
						   Connect(options.argIp, options.argPort, options.argAttempts, options.argDelay);
					   }
					   Console.WriteLine("Either a fatal error has occured or there have been too many attempts, restarting");
				   });
		}
		static void ConnectForever(string server, int port, int delay = 100)
		{
			while (true)
			{
				Connect(server, port, 1, delay);
				System.Threading.Thread.Sleep(500);
			}
		}
		static void Connect(string server, int port, int attempts = 1, int delay = 100)
		{
			for (int i = 0; i < attempts; i++)
			{


				try
				{
					// Create a TcpClient.
					// Note, for this client to work you need to have a TcpServer 
					// connected to the same address as specified by the server, port
					// combination.

					TcpClient client = new TcpClient(server, port);

					// Get Stream
					stream = client.GetStream();

					aTimer.Enabled = true;

					// Buffer to store the response bytes.
					byte[] data = new byte[256];

					// String to store the response ASCII representation.
					String responseString = String.Empty;

					// Read the first batch of the TcpServer response bytes.
					Console.WriteLine("Connection made!");
					heartbeatFailed = false;
					while (true)
					{
						try
						{
							if (!client.Connected || heartbeatFailed)
							{
								break;
							}
							if (delay == -1 || stream.DataAvailable)
							{
								Int32 byteResponse = stream.Read(data, 0, data.Length);
								responseString = Encoding.ASCII.GetString(data, 0, byteResponse);
								if (!String.IsNullOrWhiteSpace(responseString))
								{
									Console.Write(responseString);
								}
							}
						}
						catch (Exception e)
						{
							Console.WriteLine("[!] Error caught, trivial or undetermined error level!");
							Console.WriteLine(e.Message);
							Console.WriteLine(e.StackTrace);
						}
						if (delay >= 0)
						{
							System.Threading.Thread.Sleep(delay);
						}
					}

					Console.WriteLine("[!!!] Connection Lost!");
					// Close everything.
					aTimer.Enabled = false;
					stream.Close();
					stream = null;
					client.Close();
				}
				catch (ArgumentNullException e)
				{
					Console.WriteLine("ArgumentNullException: {0}", e);
				}
				catch (SocketException e)
				{
					//if"No connection could be made because the target machine actively refused it"")
					Console.WriteLine("SocketException: {0}", e.Message);
				}
			}
		}

		private static void SendHeartbeet(object sender, ElapsedEventArgs e)
		{
			if (!stream.CanWrite || !stream.CanRead || stream == null)
			{
				heartbeatFailed = true;
				return;
			}
			{
				try
				{
					byte[] ping = Encoding.ASCII.GetBytes("ping");
					stream.Write(ping, 0, ping.Length);
					stream.Flush();
					heartbeatFailed = false;
				}
				catch (SocketException er)
				{
					Console.WriteLine("[!] Socket Error caught during heartbeet!");
					Console.WriteLine(er.Message);
					Console.WriteLine(er.StackTrace);
					heartbeatFailed = true;
				}
				catch (Exception er)
				{
					Console.WriteLine("[!] General Error caught during heartbeet!");
					Console.WriteLine(er.Message);
					Console.WriteLine(er.StackTrace);
				}
			}
		}
	}

}
