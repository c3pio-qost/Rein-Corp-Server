using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.Threading;
using EncoderType = System.Drawing.Imaging.Encoder;

namespace RCServer
{
    class RCScreen
    {
        #region Global Vars
        private static IPEndPoint Remote_Point;
        private static EncoderParameter Quality_Parameter;
        private static EncoderType Quality_Encoder = EncoderType.Quality;
        public static UdpClient Screen_Server;
        public static Global.ScreenPart[] Screen_Map = new Global.ScreenPart[64];
        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out Global.CursorInfo pci);
        
        #endregion

        public static bool compare_bitmap(Bitmap bmp1,Bitmap bmp2)
        {
            if (bmp1.Size == bmp2.Size)
            {
                for (int i = 0; i < bmp1.Width; i++)
                    for (int j = 0; j < bmp2.Height; j++)
                        if (bmp1.GetPixel(i, j) != bmp2.GetPixel(i, j)) return false;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void send_to<T>(UdpClient client, IPEndPoint remote_point, T msg)
        {
            XmlSerializer msg_serializer = new XmlSerializer(typeof(T));
            try
            {
                using (MemoryStream tmp_stream = new MemoryStream())
                {
                    msg_serializer.Serialize(tmp_stream, msg);
                    client.Send(tmp_stream.ToArray(), (int)tmp_stream.Length, remote_point);
                }
            }
            catch (SocketException ex) {  }
        }

        public static T receive_from<T>(UdpClient client, ref IPEndPoint remote_point)
        {
            try
            {
                byte[] buffer = client.Receive(ref remote_point);
                XmlSerializer msg_serializer = new XmlSerializer(typeof(T));
                using (MemoryStream tmpStream = new MemoryStream(buffer))
                {
                    return (T)msg_serializer.Deserialize(tmpStream);
                }
            }
            catch (SocketException ex) {  return default(T); }
            catch (Exception ex) {  return default(T); }
        }

        private static ImageCodecInfo get_encoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private static byte[] bitmap_to_buff(Bitmap sending_bitmap)
        {
            ImageCodecInfo jgp_encoder = get_encoder(ImageFormat.Jpeg);
            EncoderType compression_encoder = System.Drawing.Imaging.Encoder.Compression;
            EncoderParameters encoder_parameters = new EncoderParameters(2);
            EncoderParameter compression_parameter = new EncoderParameter(
                compression_encoder, 
                100L
                );                                
            encoder_parameters.Param[0] = compression_parameter;
            encoder_parameters.Param[1] = Quality_Parameter;
            using (MemoryStream stream = new MemoryStream())
            {
                sending_bitmap.Save(stream, jgp_encoder, encoder_parameters);
                return stream.ToArray();
            }
        }

        public static Bitmap get_screen_with_cursor()
        {
            Graphics surface;
            Bitmap bitmap_canvas = new Bitmap(
                Screen.PrimaryScreen.Bounds.Width, 
                Screen.PrimaryScreen.Bounds.Height,
                PixelFormat.Format32bppRgb
                );
            surface = Graphics.FromImage(bitmap_canvas);
            surface.CopyFromScreen(
                Screen.PrimaryScreen.Bounds.X, 
                Screen.PrimaryScreen.Bounds.Y,
                0, 
                0, 
                Screen.PrimaryScreen.Bounds.Size, 
                CopyPixelOperation.SourceCopy
                );
            Global.CursorInfo cursor_info;
            cursor_info.cbSize = Marshal.SizeOf(typeof(Global.CursorInfo));
            GetCursorInfo(out cursor_info);
            Cursor cursor = new Cursor(cursor_info.hCursor);
            Rectangle rect = new Rectangle(
                cursor_info.ptScreenPos.X, 
                cursor_info.ptScreenPos.Y, 
                cursor.Size.Width, 
                cursor.Size.Height
                );
            cursor.Draw(surface, rect);
            return bitmap_canvas;
        }

        public static void realtime_send_screen()
        {
            Screen_Server = new UdpClient();
            Remote_Point = new IPEndPoint(RCConnect.Accepted_Client, RCConnect.Screen_Port);
            while (true)
            {
                try
                {
                    Bitmap bitmap_canvas = get_screen_with_cursor();
                    int array_index = 0;

                    for (int i = 0; i < 8; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            Screen_Map[array_index].Point.X = i * bitmap_canvas.Width / 8;
                            Screen_Map[array_index].Point.Y = j * bitmap_canvas.Height / 8;
                            Bitmap part_of_screen = new Bitmap(
                                bitmap_canvas.Width / 8, 
                                bitmap_canvas.Height / 8,
                                PixelFormat.Format32bppRgb
                                );
                            Rectangle part_bound = new Rectangle(
                                Screen_Map[array_index].Point.X, 
                                Screen_Map[array_index].Point.Y,
                                bitmap_canvas.Width / 8, 
                                bitmap_canvas.Height / 8);
                            Graphics surface = Graphics.FromImage(part_of_screen);
                            surface.DrawImage(bitmap_canvas, 0, 0, part_bound, GraphicsUnit.Pixel);
                            Screen_Map[array_index].Buffer = bitmap_to_buff(part_of_screen);
                            send_to<Global.ScreenPart>(Screen_Server, Remote_Point, Screen_Map[array_index]);
                            array_index++;
                            Thread.Sleep(10);
                        }
                    }
                }
                catch (SocketException ex) {  }
                catch (Exception ex) {  }
            }
        }

        public static void select_mode(object _quality)
        {
            byte quality = (byte)_quality;
            switch (quality)
            {
                case 1:
                    Quality_Parameter = new EncoderParameter(Quality_Encoder, 10L);
                    break;
                case 2:
                    Quality_Parameter = new EncoderParameter(Quality_Encoder, 20L);
                    break;
                case 3:
                    Quality_Parameter = new EncoderParameter(Quality_Encoder, 30L);
                    break;
                case 4:
                    Quality_Parameter = new EncoderParameter(Quality_Encoder, 40L);
                    break;
                case 5:
                    Quality_Parameter = new EncoderParameter(Quality_Encoder, 50L);
                    break;
                case 6:
                    Quality_Parameter = new EncoderParameter(Quality_Encoder, 60L);
                    break;
                case 7:
                    Quality_Parameter = new EncoderParameter(Quality_Encoder, 70L);
                    break;
                case 8:
                    Quality_Parameter = new EncoderParameter(Quality_Encoder, 80L);
                    break;
                case 9:
                    Quality_Parameter = new EncoderParameter(Quality_Encoder, 90L);
                    break;
                case 10:
                    Quality_Parameter = new EncoderParameter(Quality_Encoder, 100L);
                    break;
                default:
                    break;
            }
            realtime_send_screen();
        }

        private static Bitmap make_thumb_screenshot()
        {
            Bitmap bitmap_canvas = get_screen_with_cursor();
            Rectangle destination_rect = new Rectangle(
                0, 
                0, 
                (int)(bitmap_canvas.Width * ((float)150 / bitmap_canvas.Height)), 
                150
                );
            Bitmap temp_bitmap = new Bitmap(destination_rect.Width, destination_rect.Height);
            Graphics surface = Graphics.FromImage(temp_bitmap);
            surface.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
            surface.DrawImage(bitmap_canvas, destination_rect);
            return temp_bitmap;
        }

        private static void send_screen()
        {
            try
            {
                byte[] buffer = bitmap_to_buff(make_thumb_screenshot());
                Screen_Server.Send(buffer, buffer.Length, Remote_Point);
            }
            catch (SocketException ex) { }
            catch (Exception ex) { }
        }

        public static void start_UDP_listener(object port)
        {
            while (true)
            {
                Screen_Server = new UdpClient((int)port);
                
                Remote_Point = null;
                RCConnect.UDP_Start_Wait.Set();
                Screen_Server.Client.ReceiveTimeout = 300000;
                Global.ServiceMessageStruct msg = receive_from<Global.ServiceMessageStruct>(
                    Screen_Server, 
                    ref Remote_Point
                    );
                if (Remote_Point == null)
                {
                    RCConnect.Pass_Accept = false;
                    RCConnect.Accepted_Client = null;
                    Screen_Server.Close();
                    RCConnect.thr_Recieve_Query.Abort();
                }
                if (Remote_Point.Address.Equals(RCConnect.Accepted_Client))
                {
                    if (Encoding.UTF8.GetString(msg.Msg) == "GET")
                    {
                        Quality_Parameter = new EncoderParameter(Quality_Encoder, 35L);
                        send_screen();
                    }
                }
                Screen_Server.Close();
            }
        }

    }
}