using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MyChat
{
    public partial class frmServer : Form
    {
        #region Khai Báo biến
        /// <summary>
        /// Di chuyển form
        /// </summary>
        private bool drag = false;
        private Point dragCursor, dragForm;
        private byte[] data = new byte[1024];
        Conversation conversation = new Conversation();
        // Listener
        TcpListener tcpListen;
        TcpClient client = new TcpClient();

        List<TcpClient> lstClient = new List<TcpClient>();

        bool _running = false;
        Thread t;

        bool _focus = true;

        List<string> lstFileName = new List<string>();
        private const int BufferSize = 1024;
        byte[] SendingBuffer;
        #endregion

        #region Hàm tự tạo
        /// <summary>
        /// Gửi tin nhắn 
        /// </summary>
        /// <param name="content"></param>
        void SendMessage(string content, string path)
        {
            if (lstClient.Count == 0)        // Ko có client nào
            {
                MessageBox.Show("Chờ client kết nối đến...");
                return;
            }
            Message mes = new Message();
            mes.Sender = Sender.Me;
            mes.Content = content;
            mes.Time = DateTime.Now;
            conversation.AddMessage(mes);
            RefreshWeb();
            // Tiến hành gửi qua tcp cho các client
            for (int i = 0; i < lstClient.Count; i++)
            {
                if (lstClient[i].Connected)
                {
                    
                    NetworkStream ns = lstClient[i].GetStream();
                    StreamWriter sw = new StreamWriter(ns);
                    if (path == "")
                    {
                        sw.WriteLine(content);      // Gửi tin nhắn
                        sw.Flush();
                    }
                    else        // Gửi tập tin
                    {
                        Thread.Sleep(500);
                        DoSendFile(sw, path);
                    }
                }
                else        // Client ko kết nối nữa
                {
                    RemoveClient(lstClient[i]);
                    i--;
                }
            }

        }
        /// <summary>
        /// Thực hiện gửi file tới server
        /// Ở send server thì gửi cho nhiều client nên sẽ lâu hơn
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="path"></param>
        void DoSendFile(StreamWriter sw, string path)
        {
            pnlFile.Visible = true;
            Cursor = Cursors.WaitCursor;
            //string s = "file;";
            sw.WriteLine("file");
            sw.Flush();
            //s += Path.GetFileName(path)+";";
            sw.WriteLine(Path.GetFileName(path));
            sw.Flush();

            FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read);
            int numPackets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(file.Length) / Convert.ToDouble(BufferSize)));
            prgFile.Maximum = numPackets;
            prgFile.Value = 0;
            //s += numPackets + ";";
            sw.WriteLine(numPackets);       // Gửi số packet
            sw.Flush();

            long TotalLength = file.Length;
            int CurrentPacketLength;
            // Gửi lần lượt từng packet
            for (int i = 0; i < numPackets; i++)
            {
                if (TotalLength > BufferSize)
                {
                    CurrentPacketLength = BufferSize;
                    TotalLength = TotalLength - CurrentPacketLength;
                }
                else
                    CurrentPacketLength = (int)TotalLength;
                SendingBuffer = new byte[CurrentPacketLength];
                // Đọc từ file
                file.Read(SendingBuffer, 0, CurrentPacketLength);
                sw.WriteLine(System.Convert.ToBase64String(SendingBuffer));
                sw.Flush();
                //s += System.Convert.ToBase64String(SendingBuffer);
                
                if (prgFile.Value >= prgFile.Maximum)
                    prgFile.Value = prgFile.Minimum;
                prgFile.PerformStep();
                lblFile.Text = "Đã gửi " + prgFile.Value + "/" + prgFile.Maximum;
            }
            //sw.WriteLine(s);
                //sw.Flush();
            file.Close();
            Cursor = Cursors.Default;
            pnlFile.Visible = false;
        }
        /// <summary>
        /// Gửi hình ảnh
        /// </summary>
        /// <param name="path"></param>
        void SendImage(string path)
        {
            Uri url = new Uri(path);
            string address = url.AbsoluteUri;
            string extension = Path.GetExtension(path).ToLower();
            string content = "<a href='" + url.PathAndQuery.Replace(":", "(~*)") + "'>" + ((extension == ".jpg" || extension == ".png") ? "<img src='" + address + "' style='max-width:300px'/><br/>" : "") + "<b>" + Path.GetFileName(path) + "</b></a> (" + FileSize.SizeSuffix(new FileInfo(path).Length) + ")";

            SendMessage(content, path);
        }
        /// <summary>
        /// Điều khiển nháy icon
        /// </summary>
        void Flash()
        {
            if (!_focus)
                FlashWindow.Start(this);
        }
        /// <summary>
        /// Lắng nghe các client kết nối đến
        /// </summary>
        void Listening()
        {
            while (_running)
            {
                try
                {
                    client = tcpListen.AcceptTcpClient();
                    lstClient.Add(client);
                    Sender sender = conversation.AddClient(client);
                    
                    sender.call = new Sender.CallBack(RefreshWeb);      // Khi sender thực hiện delegate nó sẽ gọi lại Refresh Web
                    // Tạo 1 thread mới để lắng nghe client này
                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                    clientThread.Start(client);
                }
                catch { }
            }
        }
        /// <summary>
        /// Lắng nghe từng client 
        /// </summary>
        /// <param name="client"></param>
        void HandleClient(object client)
        {
            TcpClient tcpclient = (TcpClient)client;
            Sender sender = conversation.GetSender(tcpclient);
            Message mes;
            NetworkStream ns = tcpclient.GetStream();
            StreamReader sr = new StreamReader(ns);
            string s;
            while (tcpclient.Connected)
            {
                try
                {
                    s = sr.ReadLine();

                    if (s != null)
                    {
                        string[] s1 = s.Split(';');
                        switch(s1[0])
                        {
                            case "nick_name":
                                sender.NickName = s1[1];
                                LoadListSender();
                                sendlistclient();
                                break;
                            case "private":
                                forward_mess(s1, tcpclient);
                                break;
                            case "public":
                                sendmessalluser(s, sender.NickName, lstClient);
                                break;
                            case "exit":
                                RemoveClient(tcpclient);
                                Thread.CurrentThread.Abort();
                                break;
                            case "file":
                                    forward_file_to_client(sr, s1[1], s1[2], tcpclient);
                                break;
                        }
                        
                    }
                }
                catch { }
                // Ko nhận được dữ liệu nữa có nghĩa là nó ngắt kết nối rồi
                //RemoveClient(tcpclient);
                //Thread.CurrentThread.Abort();
            }
        }
        /// <summary>
        /// Thực hiện nhận file
        /// </summary>
        /// <param name="sr"></param>
        /// <returns>FileName đã nhận</returns>
        /// 
        
        void forward_file_to_client(StreamReader sr, string type, string name_cvs, TcpClient client)
        {
            //nhan file tu client
            string filename = sr.ReadLine();        // Tên file
            lstFileName.Add(filename);
            int numPacket = Convert.ToInt32(sr.ReadLine());
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    FileStream file = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);
                    for (int i = 0; i < numPacket; i++)
                    {
                        string base64 = sr.ReadLine();      // Nội dung file
                        SendingBuffer = System.Convert.FromBase64String(base64);
                        file.Write(SendingBuffer, 0, SendingBuffer.Length);
                        
                    }
                    file.Close();
                }));
            }
            string path = Directory.GetCurrentDirectory() + "/" + filename;
            //gui file
            if (type == "private")
            {
                //lay thong tin nguoi gui
                Sender sd = new Sender();
                sd = getsender(client);
                //tao ket noi voi nguoi nhan
                NetworkStream ns1 = getclient(name_cvs).GetStream();
                StreamWriter sw = new StreamWriter(ns1);
                sw.WriteLine("file;" + type + ";" + sd.NickName);
                sw.Flush();
                sw.WriteLine(filename);
                sw.Flush();
                //int numPacket = Convert.ToInt32(sr.ReadLine());
                sw.WriteLine(numPacket);
                sw.Flush();
                //gui noi dung file
                FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read);
                long TotalLength = file.Length;
                int CurrentPacketLength;
                // Gửi lần lượt từng packet
                for (int i = 0; i < numPacket; i++)
                {
                    if (TotalLength > BufferSize)
                    {
                        CurrentPacketLength = BufferSize;
                        TotalLength = TotalLength - CurrentPacketLength;
                    }
                    else
                        CurrentPacketLength = (int)TotalLength;
                    SendingBuffer = new byte[CurrentPacketLength];
                    // Đọc từ file
                    file.Read(SendingBuffer, 0, CurrentPacketLength);
                    sw.WriteLine(System.Convert.ToBase64String(SendingBuffer));
                    sw.Flush();
                }
                file.Close();
            }
            else if (type == "public")
            {
                Sender sd = new Sender();
                sd = getsender(client);
                foreach (var item in lstClient)
                {
                    if (item != client)
                    {
                        NetworkStream ns1 = item.GetStream();
                        StreamWriter sw = new StreamWriter(ns1);
                        sw.WriteLine("file;" + type + ";" + sd.NickName);
                        sw.Flush();
                        sw.WriteLine(filename);
                        sw.Flush();
                        //int numPacket = Convert.ToInt32(sr.ReadLine());
                        sw.WriteLine(numPacket);
                        sw.Flush();
                        //gui noi dung file
                        FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read);
                        long TotalLength = file.Length;
                        int CurrentPacketLength;
                        // Gửi lần lượt từng packet
                        for (int i = 0; i < numPacket; i++)
                        {
                            if (TotalLength > BufferSize)
                            {
                                CurrentPacketLength = BufferSize;
                                TotalLength = TotalLength - CurrentPacketLength;
                            }
                            else
                                CurrentPacketLength = (int)TotalLength;
                            SendingBuffer = new byte[CurrentPacketLength];
                            // Đọc từ file
                            file.Read(SendingBuffer, 0, CurrentPacketLength);
                            sw.WriteLine(System.Convert.ToBase64String(SendingBuffer));
                            sw.Flush();
                        }
                        file.Close();
                    }
                    Thread.Sleep(500);
                }
            }
        }



        string DoReciveFile(StreamReader sr)
        {
            string filename = sr.ReadLine();        // Tên file
            lstFileName.Add(filename);

            int numPacket = Convert.ToInt32(sr.ReadLine());

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    pnlFile.Visible = true;
                    Cursor = Cursors.WaitCursor;
                    prgFile.Maximum = numPacket;
                    prgFile.Value = 0;
                    FileStream file = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);
                    for (int i = 0; i < numPacket; i++)
                    {
                        string base64 = sr.ReadLine();      // Nội dung file
                        SendingBuffer = System.Convert.FromBase64String(base64);
                        file.Write(SendingBuffer, 0, SendingBuffer.Length);

                        if (prgFile.Value >= prgFile.Maximum)
                            prgFile.Value = prgFile.Minimum;
                        prgFile.PerformStep();
                        lblFile.Text = "Đã nhận " + prgFile.Value + "/" + prgFile.Maximum;
                    }
                    file.Close();
                    Cursor = Cursors.Default;
                    pnlFile.Visible = false;
                }));
            }

            string path = Directory.GetCurrentDirectory() + "/" + filename;
            Uri url = new Uri(path);
            string address = url.AbsoluteUri;
            string extension = Path.GetExtension(filename).ToLower();
            string s = "<a href='" + url.PathAndQuery.Replace(":", "(~*)") + "'>" + ((extension == ".jpg" || extension == ".png") ? "<img src='" + address + "' style='max-width:300px'/><br/>" : "") + "<b>" + Path.GetFileName(path) + "</b></a> (" + FileSize.SizeSuffix(new FileInfo(path).Length) + ")";

            return s;
        }
        /// <summary>
        /// XÓa client
        /// </summary>
        /// <param name="client"></param>
        void RemoveClient(TcpClient client)
        {
            if (lstClient.Contains(client))
                lstClient.Remove(client);
            conversation.RemoveClient(client);
            LoadListSender();
            sendlistclient();
        }
        /// <summary>
        /// Hiển thị danh sách client lên view
        /// </summary>
        void LoadListSender()
        {
            // List button
            if (_running && this.InvokeRequired)
                Invoke(new Action(() =>
                {
                    if (lstClient.Count == 0)
                    {
                        pnlSender.Visible = false;
                        return;
                    }
                    pnlSender.SuspendLayout();
                    pnlSender.Controls.Clear();
                    pnlSender.AutoScroll = false;
                    pnlSender.VerticalScroll.Visible = false;
                    foreach (var sender in conversation.Senders)
                    {
                        if (sender.But != null)
                        {
                            toolTip1.SetToolTip(sender.But, sender.NickName + sender.Address);
                                // "Địa chỉ " + sender.Address + " cổng " + sender.Port);
                            pnlSender.Controls.Add(sender.But);
                        }
                    }
                    
                    pnlSender.ResumeLayout();
                    pnlSender.Visible = true;
                    // Update the form
                    this.PerformLayout();
                }));
        }

        /// <summary>
        /// Load lại nội dung tin nhắn
        /// </summary>
        void RefreshWeb()
        {
            webMain.Invoke(new Action(() =>
            {
                webMain.Document.Write(conversation.GetHTML);
                webMain.Refresh();
            }));
        }
        #endregion

        #region Forms
        public frmServer()
        {
            InitializeComponent();
            this.Padding = new Padding(1);
            
            tcpListen = new TcpListener(IPAddress.Any, 9090);
            try
            {
                tcpListen.Start();
            }
            catch
            {
                MessageBox.Show("Không khởi tạo được kết nối", "Lỗi");
                Application.Exit();
                return;
            }
            _running = true;

            t = new Thread(Listening);
            t.Start();
        }

        private void frmServer_Load(object sender, EventArgs e)
        {
            lblTitle.Text = this.Text = "MYCHAT SERVER";

            txtMessage.Focus();
        }

        private void frmServer_Activated(object sender, EventArgs e)
        {
            BackColor = Color.ForestGreen;
            txtMessage.Focus();

            _focus = true;
            FlashWindow.Stop(this);
        }

        private void frmServer_Deactivate(object sender, EventArgs e)
        {
            BackColor = Color.DimGray;
            _focus = false;
        }

        private void frmServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var f in lstFileName)
            {
                if (File.Exists(f))
                    File.Delete(f);
            }
            t.Abort();
            _running = false;
            foreach (var client in lstClient)
            {
                client.Close();
            }
            tcpListen.Stop();
        }

        /// <summary>
        ///  Drop Shadow cho form
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x20000;
                CreateParams cp = base.CreateParams;
                // Bóng đổ
                cp.ClassStyle |= CS_DROPSHADOW;
                // Load các control cùng lúc
                cp.ExStyle |= 0x02000000; // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        #region Move form
        private void frmServer_MouseDown(object sender, MouseEventArgs e)
        {
            drag = true;
            dragCursor = Cursor.Position;
            dragForm = this.Location;
        }

        private void frmServer_MouseMove(object sender, MouseEventArgs e)
        {
            int wid = SystemInformation.VirtualScreen.Width;
            int hei = SystemInformation.VirtualScreen.Height;
            if (drag)
            {
                Point change = Point.Subtract(Cursor.Position, new Size(dragCursor));
                Point newpos = Point.Add(dragForm, new Size(change));
                if (newpos.X < 0) newpos.X = 0;
                if (newpos.Y < 0) newpos.Y = 0;
                if (newpos.X + this.Width > wid) newpos.X = wid - this.Width;
                if (newpos.Y + this.Height > hei) newpos.Y = hei - this.Height;
                this.Location = newpos;
            }
        }

        private void frmServer_MouseUp(object sender, MouseEventArgs e)
        {
            drag = false;
        }
        #endregion
        #endregion

        #region Controls
        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnImage_Click(object sender, EventArgs e)
        {
            OpenFileDialog dia = new OpenFileDialog();
            dia.RestoreDirectory = true;
            dia.Title = "Chọn tập tin đính kèm";
            if (dia.ShowDialog() == DialogResult.OK)
                SendImage(dia.FileName);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (txtMessage.Text != "")
            {
                SendMessage(txtMessage.Text, "");
                txtMessage.Text = "";
                txtMessage.Focus();
            }
        }

        private void panel4_Click(object sender, EventArgs e)
        {
            txtMessage.Focus();
        }

        private void txtMessage_Leave(object sender, EventArgs e)
        {
            txtMessage.Focus();
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void webMain_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
             e.Cancel = true;
            if (e.Url.ToString() != "about:blank")
            {
                string url = e.Url.PathAndQuery;
                frmOpenFile frm = new frmOpenFile(url);
                frm.ShowDialog();
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void pnlSender_Paint(object sender, PaintEventArgs e)
        {

        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
                btnSend.PerformClick();
        }
        #endregion
        private void sendlistclient()
        {
            this.Invoke(new MethodInvoker(delegate ()
            {
                for (int i = 0; i < lstClient.Count; i++)
                {
                    if (lstClient[i].Connected)
                    {
                        NetworkStream ns = lstClient[i].GetStream();
                        Sender sender1 = conversation.GetSender(lstClient[i]);
                        StreamWriter sw = new StreamWriter(ns);
                        string s = "list;";
                        string s1 = "";
                        foreach (var sender in conversation.Senders)
                        {
                            if (sender1.NickName != sender.NickName && sender.NickName != "Me")
                            {
                                s1 += sender.NickName + ";";
                            }

                        }
                        if (s1 != "")
                        {
                            sw.WriteLine(s + s1);
                            sw.Flush();
                        }


                    }
                    else        // Client ko kết nối nữa
                    {
                        RemoveClient(lstClient[i]);
                        i--;
                    }
                }
            }));
            
        }
        void forward_mess(string[] mess, TcpClient sender)
        {
            NetworkStream ns2 = getclient(mess[1]).GetStream();
            string name_sender = getsender(sender).NickName;
            StreamWriter sw = new StreamWriter(ns2);
            sw.WriteLine(mess[0]+";"+ name_sender+";"+mess[2]);
            sw.Flush();
        }
        void sendmessalluser(string mess,string name, List<TcpClient> client)
        {
            foreach (var item in client)
            {
                Sender sd = new Sender();
                sd = getsender(item);
                if (sd.NickName != name)
                {
                    NetworkStream ns = item.GetStream();
                    StreamWriter sw = new StreamWriter(ns);
                    sw.WriteLine(mess+";"+name);
                    sw.Flush();
                }
                
            }
        }
        private Sender getsender(TcpClient client_sender)
        {
            Sender name_sender = new Sender();
            for (int i = 0; i < lstClient.Count; i++)
            {
                if (lstClient[i].Connected)
                {
                    if (lstClient[i] == client_sender)
                    {
                        name_sender = conversation.GetSender(lstClient[i]);
                        break;
                    }

                }
                
            }
            return name_sender;
        }
        private TcpClient getclient(string name)
        {
            TcpClient client = new TcpClient();
            for (int i = 0; i < lstClient.Count; i++)
            {
                if (lstClient[i].Connected)
                {
                    if (conversation.GetSender(lstClient[i]).NickName == name)
                    {
                        client = lstClient[i];
                        break;
                    }

                }
                
            }
            return client;
        }
    }
}
