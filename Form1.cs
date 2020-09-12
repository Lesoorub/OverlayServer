using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json.Linq;

namespace OverlayServer
{
    public partial class Form1 : Form
    {
        public static PictureBox screen;
        WebSocketServer wss = new WebSocketServer(25565);
        Screen current;
        public Form1()
        {
            TopMost = true;
            InitializeComponent();
            screen = pictureBox1;

            SetScreen(Screen.PrimaryScreen);

            wss.AddWebSocketService<General>("/");
            wss.Start();
        }

        public void SetScreen(Screen scr)
        {
            current = scr;
            this.StartPosition = FormStartPosition.Manual;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Location = new Point(current.Bounds.X, current.Bounds.Y);
            this.Size = new Size(current.Bounds.Size.Width, current.Bounds.Size.Height);
            this.ResumeLayout(false);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {

        }
        public static byte[] BitmapToByte(Bitmap bm)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                bm.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                return stream.ToArray();
            }
        }


        public class General : WebSocketBehavior
        {
            protected override void OnMessage(MessageEventArgs e)
            {
                if (e.IsText)
                {
                    // { draw, pos }
                    // pen
                    // draw = line, from = [0,0], to = [10,10]
                    // draw = clear //clear
                    // draw = rectangle, size = [10, 23]
                    // draw = ellipse, rect = [1, 1]
                    // draw = image, data = "1F45A4F..."//byte array
                    //

                    JObject json = JObject.Parse(e.Data);
                    Pen p = Pens.Red;
                    string draw = "none";
                    Point pos = new Point(0, 0);

                    if (json["pos"] != null)
                        pos = json["pos"].ToPoint();
                    if (json["draw"] != null)//{"type": "BR"}
                        draw = json["draw"].ToString();
                    if (json["pen"] != null)
                        p = new Pen(System.Drawing.ColorTranslator.FromHtml(json["pen"].ToString()));

                    using (Graphics g = screen.CreateGraphics())
                        switch (draw)
                        {
                            case "clear":
                                g.Clear(Color.Magenta);
                                break;
                            case "line":
                                g.DrawLine(p, json["from"].ToPoint(), json["to"].ToPoint());
                                break;
                            case "rectangle":
                                g.DrawRectangle(p, pos.ToRectangle(json["size"].ToPoint()));
                                break;
                            case "ellipse":
                                g.DrawEllipse(p, pos.ToRectangle(json["rect"].ToPoint()));
                                break;
                            case "image":
                                using (var ms = new System.IO.MemoryStream(StringToByteArray(json["data"].ToString())))
                                using (Bitmap bm = new Bitmap(ms))
                                    g.DrawImageUnscaledAndClipped(bm, pos.ToRectangle(bm.Size));
                                break;
                        }
                }
                else if (e.IsBinary)
                {
                    using (Graphics g = screen.CreateGraphics())
                    using (var ms = new System.IO.MemoryStream(e.RawData))
                    using (Bitmap bm = new Bitmap(ms))
                        g.DrawImageUnscaledAndClipped(bm, new Point(0,0).ToRectangle(bm.Size));
                }
            }

            public static string ByteArrayToString(byte[] ba)
            {
                return BitConverter.ToString(ba).Replace("-", "");
            }
            public static byte[] StringToByteArray(String hex)
            {
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2)
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                return bytes;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (current != null)
            {
                int pos = 0;
                for (int k = 0; k < Screen.AllScreens.Length; k++)
                    if (Screen.AllScreens[k] == current)
                        pos = k;

                if (pos + 1 < Screen.AllScreens.Length)
                    SetScreen(Screen.AllScreens[pos + 1]);
                else
                    SetScreen(Screen.AllScreens[0]);
            }
            else
            {
                current = Screen.PrimaryScreen;
            }
        }
    }
}
public static class External
{
    public static Point ToPoint(this JToken t) => new Point((int)t[0], (int)t[1]);
    public static Point Add(this Point a, Point b) => new Point(a.X + b.X, a.Y + b.Y);
    public static Point Sub(this Point a, Point b) => new Point(a.X - b.X, a.Y - b.Y);
    public static Rectangle ToRectangle(this Point a, Point size) =>
        new Rectangle(a.X, a.Y, size.X, size.Y);
    public static Rectangle ToRectangle(this Point a, Size size) =>
         new Rectangle(a.X, a.Y, size.Width, size.Height);
}