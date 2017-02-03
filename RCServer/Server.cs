using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Xml.Serialization;
using System.IO;

namespace RCServer
{
    public partial class Server : Form
    {
        #region Globar Vars
        Thread thr_System_Command_Listener;
        #endregion

        public Server()
        {
            InitializeComponent();
        }
        private void Exit_Click(object sender, EventArgs e)
        {
            if (thr_System_Command_Listener != null)
            {
                thr_System_Command_Listener.Abort();
                //thr_System_Command_Listener.Join();
            }
            if (RCConnect.thr_Recieve_Query != null)
            {
                if (RCScreen.Screen_Server != null)
                {
                    RCScreen.Screen_Server.Close();
                }
                RCConnect.thr_Recieve_Query.Abort();
                //RCConnect.thr_Recieve_Query.Join();
            }
			Environment.Exit(0x1);
        }

        private void Server_Load(object sender, EventArgs e)
        {
            thr_System_Command_Listener = new Thread(RCConnect.system_command_istener);
            Global.ServerRarams srv_params = new Global.ServerRarams();
            srv_params.Port = 8267;
            srv_params.RaiseFunc = RCConnect.analyze_command;
            thr_System_Command_Listener.Start(srv_params);
        }

        private void Server_Activated(object sender, EventArgs e)
        {
            Hide();
        }

    }
}
