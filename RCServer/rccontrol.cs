using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Threading;

namespace RCServer
{
    class RCControl
    {
        static int[] Powers = { 513, 514, 516, 517, 519, 520, 523, 524 };
        [DllImport("user32.dll")]
        static extern UInt32 SendInput(UInt32 nInputs, [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] Global.INPUT[] pInputs, Int32 cbSize);
        static Queue<Global.MTransferStruct> Mouse_Buffer = new Queue<Global.MTransferStruct>();
        static Queue<Global.KTransferStruct> Keyboard_Buffer = new Queue<Global.KTransferStruct>();

        public static void mouse_input_handler(Global.MTransferStruct transfer_struct)
        {
            Global.INPUT[] inp = new Global.INPUT[1];
            inp[0].type = (int)Global.InputType.MOUSE;
            Global.MOUSEINPUT ms = new Global.MOUSEINPUT();
            switch (transfer_struct.Flag)
            {
                case 512:
                    ms.dx = (int)Math.Round(transfer_struct.XCoord * 65536, 0);
                    ms.dy = (int)Math.Round(transfer_struct.YCoord * 65536, 0);
                    ms.dwFlags = 0x0001 | 0x8000;
                    break;
                case 522:
                    ms.dwFlags = 0x0800;
                    ms.mouseData = transfer_struct.Data;
                    break;
                default:
                    ms.dwFlags = (int)System.Math.Pow(2, Array.IndexOf(Powers, transfer_struct.Flag) + 1);
                    break;
            }
            inp[0].inputUnion.mi = ms;
            SendInput(1, inp, Marshal.SizeOf(inp[0]));
        }

        public static void keyboard_input_handler(Global.KTransferStruct transfer_struct)
        {
            Global.INPUT[] input = new Global.INPUT[1];
            input[0].type = (int)Global.InputType.KEYBOARD;
            Global.KEYBDINPUT kb = new Global.KEYBDINPUT();
            kb.wVk = transfer_struct.VKode;
            kb.dwFlags = transfer_struct.Flag;
            input[0].inputUnion.ki = kb;
            SendInput(1, input, Marshal.SizeOf(input[0]));
        }

        public static void mouse_buffer_exec()
        {
            while(Mouse_Buffer.Count>0)
            {
                mouse_input_handler(Mouse_Buffer.Dequeue());
            }
        }

        public static void keyboard_buffer_exec()
        {
            while (Keyboard_Buffer.Count > 0)
            {
                keyboard_input_handler(Keyboard_Buffer.Dequeue());
            }
        }

        public static void control_invoker(TcpClient client)
        {
            client.Client.ReceiveTimeout = 5000;
            NetworkStream net_stream = client.GetStream();
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1024];
                    net_stream.Read(buffer, 0, 1024);
                    string rcv_msg = Encoding.UTF8.GetString(buffer);
                    string[] data = rcv_msg.Split(' ');
                    if (data[0] == "m")
                    {
                        Global.MTransferStruct msg = new Global.MTransferStruct(
                            Convert.ToDouble(data[1]),
                            Convert.ToDouble(data[2]),
                            Convert.ToInt32(data[3]),
                            Convert.ToInt32(data[4])
                            );
                        Mouse_Buffer.Enqueue(msg);
                        if (Mouse_Buffer.Count == 1)
                        {
                            Thread tmp_thr = new Thread(mouse_buffer_exec);
                            tmp_thr.Start();
                        }
                    }
                    if (data[0] == "k")
                    {
                        Global.KTransferStruct msg = new Global.KTransferStruct(
                            Convert.ToInt16(data[1]),
                            Convert.ToInt32(data[2]));
                        Keyboard_Buffer.Enqueue(msg);
                        if (Keyboard_Buffer.Count == 1)
                        {
                            Thread tmp_thr = new Thread(keyboard_buffer_exec);
                            tmp_thr.Start();
                        }
                    }
                }
            }
            catch (SocketException ex) { }
            catch (Exception ex) { }
        }
    }
}
