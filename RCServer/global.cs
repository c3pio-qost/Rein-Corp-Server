using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Net.Sockets;

namespace RCServer
{
    public class Global
    {
        public static string[] SysMsg = { "CNT", "PAS", "SYN","ERR","INF","SCR" };

        [Serializable]
        public struct ScreenPart
        {
            public Point Point;
            public byte[] Buffer;
        }

        public class ServerRarams
        {
            public int Port;
            public Action<TcpClient> RaiseFunc;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public class SysMessageStruct
        {
            public byte[] Msg ;
            public byte Flag ;
            public byte[] Info;
            public SysMessageStruct()
            {
				Msg = new byte[3];
				Flag = new byte();
				Info = new byte[32];
            }
            public SysMessageStruct(string msg, byte flag, string info)
            {
                Flag = flag;
                if (info.Length <= 32)
                {
                    Info = Encoding.UTF8.GetBytes(info);
                }
                else
                {
                    Info = Encoding.UTF8.GetBytes(info.Substring(0, 32));
                }
                if (msg.Length <= 3)
                {
                    Msg = Encoding.UTF8.GetBytes(msg);
                }
                else
                {
                    Msg = Encoding.UTF8.GetBytes(msg.Substring(0, 3));
                }
            }
            public SysMessageStruct(string msg, byte flag)
            {
                Flag = flag;
                Info = new byte[32];
                Array.Clear(Info, 0, 32);
                if (msg.Length <= 3)
                {
                    Msg = Encoding.UTF8.GetBytes(msg);
                }
                else
                {
                    Msg = Encoding.UTF8.GetBytes(msg.Substring(0, 3));
                }
            }
        }

        public class ServiceMessageStruct
        {
            public byte[] Msg;
            public byte Flag;
            public ServiceMessageStruct()
            {
                Msg = new byte[3];
                Flag = new byte();
            }
            public ServiceMessageStruct(string msg, byte flag)
            {
                Flag = flag;
                if (msg.Length <= 3)
                {
                    Msg = Encoding.UTF8.GetBytes(msg);
                }
                else
                {
                    Msg = Encoding.UTF8.GetBytes(msg.Substring(0, 3));
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CursorInfo
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public Point ptScreenPos;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct KTransferStruct
        {
            public short VKode;
            public int Flag;
            
            public KTransferStruct(short v_kode, int flag)
            {
                VKode = v_kode;
                Flag = 2;
                if ((flag == 256) || (flag == 260)) Flag = 0;
                //if ((flag == 257) || (flag == 261)) Flag = 2;
            }
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct MTransferStruct
        {
            public double XCoord;
            public double YCoord;
            public int Flag;
            public int Data;

            public MTransferStruct(double x_coord, double y_coord, int flag, int data)
            {
                XCoord = x_coord;
                YCoord = y_coord;
                Flag = flag;
                Data = data;
            }
        }
        public enum InputType
        {
            MOUSE = 0,
            KEYBOARD = 1,
            HARDWARE = 2,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public int type;
            public INPUTUNION inputUnion;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUTUNION
        {
            // Fields
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public short wVk;
            public short wScan;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }
    }
}
