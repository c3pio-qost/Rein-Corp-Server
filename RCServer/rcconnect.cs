using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Management;
using System.ComponentModel;

namespace RCServer
{
	class RCConnect
	{
		#region Globar Vars
		static string Password = "123qwe";
		public static bool Pass_Accept = false;
		public static int Timeout = 0;
		public static bool Pause_Send = false;
		public static IPAddress Accepted_Client;
		public static Thread thr_Recieve_Query;
		public static Thread thr_One_To_One;
		public static ManualResetEvent UDP_Start_Wait = new ManualResetEvent(false);
		public static int Screen_Port = 0;
		public static int Control_Port = 0;
		public static List<List<Delegate>> Delegate_list;
		public static Thread thr_Control_Server;
		#endregion

		private static T recive_from<T>(NetworkStream net_stream)
		{
			try {
				XmlSerializer msg_serializer = new XmlSerializer(typeof(T));
				byte[] buffer = new byte[1024];
				net_stream.Read(buffer, 0, 1024);
				using (MemoryStream tmp_Stream = new MemoryStream(buffer)) {
					return (T)msg_serializer.Deserialize(tmp_Stream);
				}
			}
			catch (SocketException ex) { return default(T); }
			catch (Exception ex) { return default(T); }
		}

		public static void send_to(NetworkStream net_stream, Global.SysMessageStruct msg)
		{
			XmlSerializer msg_serializer = new XmlSerializer(typeof(Global.SysMessageStruct));
			using (MemoryStream tmpStream = new MemoryStream()) {
				msg_serializer.Serialize(tmpStream, msg);
				net_stream.Write(tmpStream.ToArray(), 0, (int)tmpStream.Length);
			}
		}

		public static void send_information(NetworkStream net_stream)
		{
			send_to(
				net_stream,
				new Global.SysMessageStruct(Global.SysMsg[2], 1, Dns.GetHostName())
				);
		}

		public static void thread_killer(Thread thread)
		{
			if (thread != null) {
				thread.Abort();
				thread.Join();
				thread = null;
			}
		}

		public static void reset_all_connect()
		{
			if (RCScreen.Screen_Server != null)
				RCScreen.Screen_Server.Close();
			thread_killer(thr_Recieve_Query);
			thread_killer(thr_One_To_One);
			thread_killer(thr_Control_Server);
		}

		public static void ending_connect_and_start_UDP(NetworkStream net_stream)
		{
			send_to(
				net_stream,
				new Global.SysMessageStruct(Global.SysMsg[2], 1, Dns.GetHostName())
				);
			reset_all_connect();
			thr_Recieve_Query = new Thread(RCScreen.start_UDP_listener);
			thr_Recieve_Query.Start(5011);
			UDP_Start_Wait.WaitOne();
			send_to(net_stream, new Global.SysMessageStruct(Global.SysMsg[2], 2));
		}

		public static void disconect()
		{
			Pass_Accept = false;
			reset_all_connect();
		}

		public static List<int> get_busy_port()
		{
			List<int> port_arr = new List<int>();
			IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
			TcpConnectionInformation[] info_con = properties.GetActiveTcpConnections();
			foreach (var item in info_con) {
				if ((item.LocalEndPoint.Port > 8267) && (port_arr.IndexOf(item.LocalEndPoint.Port) < 0))
					port_arr.Add(item.LocalEndPoint.Port);
			}
			IPEndPoint[] info_t_l = properties.GetActiveTcpListeners();
			foreach (var item in info_t_l) {
				if ((item.Port > 8267) && (port_arr.IndexOf(item.Port) < 0))
					port_arr.Add(item.Port);
			}
			IPEndPoint[] info_u_l = properties.GetActiveUdpListeners();
			foreach (var item in info_u_l) {
				if ((item.Port > 8267) && (port_arr.IndexOf(item.Port) < 0))
					port_arr.Add(item.Port);
			}
			port_arr.Sort();
			return port_arr;
		}

		public static void check_port(NetworkStream net_stream, Global.SysMessageStruct msg)
		{
			bool right = false;
			List<int> busy_port = get_busy_port();
			if (Screen_Port != 0) busy_port.Add(Screen_Port);
			if (Control_Port != 0) busy_port.Add(Control_Port);
			while (!right) {
				int port = Convert.ToInt32(Encoding.UTF8.GetString(msg.Info));
				if (busy_port.IndexOf(port) > -1) {
					send_to(
						net_stream,
						new Global.SysMessageStruct(Global.SysMsg[3], 102)
						);
				}
				else {
					if (msg.Flag == 0) {
						Screen_Port = port;
					}
					if (msg.Flag == 1) {
						Control_Port = port;
					}
					send_to(
						net_stream,
						new Global.SysMessageStruct("SYN", 3)
						);
					right = true;
				}
				if (!right) {
					msg = recive_from<Global.SysMessageStruct>(net_stream);
				}
			}
		}



		public static void power_analyzer(int flag)
		{
			switch (flag) {
				case 0:  //shutdown
					reboot.halt(false, true);
					break;
				case 1:  //reboot
					reboot.halt(true, true);
					break;
				case 2:  //hibernate
					Application.SetSuspendState(PowerState.Hibernate, true, false);
					break;
				case 3:
					reboot.Lock();
					break;
			}
		}

		public static void analyze_command(TcpClient system_command_receiver)
		{
			Global.SysMessageStruct msg = new Global.SysMessageStruct();
			NetworkStream net_stream = system_command_receiver.GetStream();
			msg = recive_from<Global.SysMessageStruct>(net_stream);
			switch (Encoding.UTF8.GetString(msg.Msg)) {
				case "SHT":
					power_analyzer(msg.Flag);
					break;
				case "INF":
					send_information(net_stream);
					break;
				case "PRT":
					check_port(net_stream, msg);
					break;
				case "SCR":
					disconect();
					switch (msg.Flag) {
						case 0:
							ending_connect_and_start_UDP(net_stream);
							break;
						case 11:
							Accepted_Client = null;
							break;
						default:
							if ((msg.Flag > 0) && (msg.Flag < 11)) {
								thr_One_To_One = new Thread(RCScreen.select_mode);
								thr_One_To_One.Start(msg.Flag);
							}
							break;
					}
					break;
				case "CTR":
					disconect();
					/*switch (msg.Flag) {
						case 0:
							thr_Control_Server = new Thread(RCConnect.system_command_istener);
							Global.ServerRarams srv_params = new Global.ServerRarams();
							srv_params.Port = Control_Port;
							srv_params.RaiseFunc = RCControl.control_invoker;
							thr_Control_Server.Start(srv_params);
							break;
						case 1:
							thread_killer(thr_Control_Server);
							break;
					}*/
					break;
				case "TRM":

					break;
			}
		}

		public static void authentication(TcpClient system_command_receiver, Action<TcpClient> raise_func)
		{
			NetworkStream net_stream = system_command_receiver.GetStream();
			Global.SysMessageStruct msg = new Global.SysMessageStruct();
			send_to(net_stream, new Global.SysMessageStruct(Global.SysMsg[1], 0));
			msg = recive_from<Global.SysMessageStruct>(net_stream);
			if (Encoding.UTF8.GetString(msg.Info) == Password) {
				send_to(net_stream, new Global.SysMessageStruct(Global.SysMsg[2], 1));
				Pass_Accept = true;
				IPEndPoint tmpPoint = (IPEndPoint)system_command_receiver.Client.RemoteEndPoint;
				Accepted_Client = tmpPoint.Address;
				raise_func(system_command_receiver);
			}
			else {
				send_to(net_stream, new Global.SysMessageStruct(Global.SysMsg[3], 100));
			}
			system_command_receiver.Close();
		}

		//Main function
		public static void system_command_istener(object Args)
		{
			//Проверить запущен ли уже сервер
			TcpListener System_Command_Server = null;
			Global.ServerRarams srv_prm = (Global.ServerRarams)Args;
			try {
				IPEndPoint local_point = new IPEndPoint(IPAddress.Any, srv_prm.Port);
				System_Command_Server = new TcpListener(local_point);
				System_Command_Server.Start(1);
			}
			catch (SocketException ex) {
				MessageBox.Show("Cannot create exist End-Point");
				Environment.Exit(0x1);
			}
			while (true) {
				try {
					UDP_Start_Wait.Reset();
					TcpClient system_command_receiver = System_Command_Server.AcceptTcpClient();
					Console.WriteLine("connect");
					system_command_receiver.Client.SendTimeout = 5000;
					system_command_receiver.Client.ReceiveTimeout = 5000;
					NetworkStream net_stream = system_command_receiver.GetStream();
					Global.SysMessageStruct msg = new Global.SysMessageStruct();
					msg = recive_from<Global.SysMessageStruct>(net_stream);
					switch (Encoding.UTF8.GetString(msg.Msg)) {
						case "CNT":
							authentication(system_command_receiver, srv_prm.RaiseFunc);
							break;
						default:
							break;
					}
				}
				catch (SocketException ex) { }
				catch (Exception ex) { }
			}
		}
	}
}
