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

			[Option('d', "delay", Default = -1, HelpText = "The delay between each read in ms. Warning! If the delay is low (wiht a minimum of 0), the program can slow down your PC. If you want the program to be as fast as possible but use only as many resources as it needs, use -1. For -2 or lower, the program will use all resources but will only read when data is available. -2 or lower is not reccomended.", Required = false)]
			public int argDelay { get; set; }
		}
		static NetworkStream stream;
		static TcpClient client;
		static bool heartbeatFailed = false;
		static Timer aTimer;

		static void Main(string[] args)
		{
			Parser.Default.ParseArguments<ArgumentBuilder>(args)
				   .WithParsed(options =>
				   {
					   aTimer = new System.Timers.Timer(10000);
					   aTimer.Enabled = false;
					   aTimer.Elapsed += SendHeartbeat;
					   aTimer.AutoReset = true;
					   Console.WriteLine("Starting connection...");

					   if (options.argInfinite)
					   {
						   while (true)
						   {
							   Run(options);
						   }
					   }
					   else
					   {
						   for (int i = 0; i < options.argAttempts; i++)
						   {
							   Run(options);
						   }
					   }
					   //Console.WriteLine("Either a fatal error has occured or there have been too many attempts, restarting");
				   });
		}

		private static void Run(ArgumentBuilder options)
		{
			client = Connect(options.argIp, options.argPort);
			if (client != null)
			{
				stream = client.GetStream();

				aTimer.Enabled = true;

				Console.WriteLine("Connection made!");
				heartbeatFailed = false;

				if (options.argDelay >= 0)
				{
					// Run With Delay
					ReadWithDelay(options.argDelay);
				}
				else if (options.argDelay == -1)
				{
					// Run with .Read() function block
					ReadWithFunctionBlock();
				}
				else
				{
					// Run with .DataAvailable check
					ReadWithDataCheck();
				}
				aTimer.Enabled = false;
			}
		}

		private static TcpClient Connect(string ip, int port)
		{
			try
			{
				TcpClient client = new TcpClient(ip, port);
				return client;
			}
			catch (ArgumentNullException e)
			{
				Console.WriteLine("ArgumentNullException: {0}", e);
			}
			catch (SocketException e)
			{
				// "No connection could be made because the target machine actively refused it"")
				Console.WriteLine("SocketException: {0}", e.Message);
			}
			return null;
		}

		private static void ReadWithDataCheck()
		{
			// Buffer to store the response bytes.
			byte[] data = new byte[256];

			// String to store the response ASCII representation.
			String responseString = String.Empty;
			while (true)
			{
				try
				{
					if (!client.Connected || heartbeatFailed)
					{
						break;
					}
					if (stream.DataAvailable)
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
			}
		}

		private static void ReadWithFunctionBlock()
		{
			// Buffer to store the response bytes.
			byte[] data = new byte[256];

			// String to store the response ASCII representation.
			String responseString = String.Empty;
			while (true)
			{
				try
				{
					if (!client.Connected || heartbeatFailed)
					{
						break;
					}
					Int32 byteResponse = stream.Read(data, 0, data.Length);
					responseString = Encoding.ASCII.GetString(data, 0, byteResponse);
					if (!String.IsNullOrWhiteSpace(responseString))
					{
						Console.Write(responseString);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("[!] Error caught, trivial or undetermined error level!");
					Console.WriteLine(e.Message);
					Console.WriteLine(e.StackTrace);
				}
			}
		}

		private static void ReadWithDelay(int delay)
		{
			// Buffer to store the response bytes.
			byte[] data = new byte[256];

			// String to store the response ASCII representation.
			String responseString = String.Empty;
			while (true)
			{
				try
				{
					if (!client.Connected || heartbeatFailed)
					{
						break;
					}
					if (stream.DataAvailable)
					{
						Int32 responseSize = stream.Read(data, 0, data.Length);
						responseString = Encoding.ASCII.GetString(data, 0, responseSize);
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
				System.Threading.Thread.Sleep(delay);

			}
		}

		private static void SendHeartbeat(object sender, ElapsedEventArgs e)
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

		static void LegacyRun(string server, int port, int delay = 100)
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

				if (delay >= 0)
				{
					//Read with Delay
					while (true)
					{
						try
						{
							if (!client.Connected || heartbeatFailed)
							{
								break;
							}
							if (stream.DataAvailable)
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
						System.Threading.Thread.Sleep(delay);

					}
				}
				else if (delay == -1)
				{
					// Always read
					while (true)
					{
						try
						{
							if (!client.Connected || heartbeatFailed)
							{
								break;
							}
							Int32 byteResponse = stream.Read(data, 0, data.Length);
							responseString = Encoding.ASCII.GetString(data, 0, byteResponse);
							if (!String.IsNullOrWhiteSpace(responseString))
							{
								Console.Write(responseString);
							}
						}
						catch (Exception e)
						{
							Console.WriteLine("[!] Error caught, trivial or undetermined error level!");
							Console.WriteLine(e.Message);
							Console.WriteLine(e.StackTrace);
						}
					}
				}
				else
				{
					// delay <= -2
					// Read without delay, only when data available
					while (true)
					{
						try
						{
							if (!client.Connected || heartbeatFailed)
							{
								break;
							}
							if (stream.DataAvailable)
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
}
