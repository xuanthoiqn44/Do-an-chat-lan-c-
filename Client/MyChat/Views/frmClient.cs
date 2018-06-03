using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MyChat
{
    public partial class frmClient : Form
    {
        #region Khai Báo biến
        /// <summary>
        /// Di chuyển form
        /// </summary>
        private bool drag = false;
        private Point dragCursor, dragForm;
        private string name_conversation = "";
        Conversation conversation = new Conversation();
        StreamReader sr;
        string btn_name = "";
        string btn_flash = "";
        string type = "";
        Sender sender = new Sender();
        Thread t;
        TcpClient server;
        bool _focus = true;
        NetworkStream ns ;
        List<string> lstFileName = new List<string>();

        private const int BufferSize = 1024;
        byte[] SendingBuffer;
        string name = "";
        string name2 = "";
        #endregion
        List<Sender> lstsd = new List<Sender>();
        List<Conversation> listchat = new List<Conversation>();
        
        #region Hàm tự tạo
        /// <summary>
        /// Gửi tin nhắn thông thường
        /// </summary>
        /// <param name="content"></param>
        void SendMessage(string content, string path,string type, string cvs)
        {
            if (server.Connected)
            {
                Message mes = new Message();
                mes.Sender = Sender.Me;
                mes.Content = content;
                mes.Time = DateTime.Now;
                if (cvs == "")
                {
                    conversation.AddMessage(mes);
                    RefreshWeb("");

                }else
                {
                    getconversation(cvs).AddMessage(mes);
                    RefreshWeb(cvs);
                }
                // Gửi cho server
                try
                {
                    ns = server.GetStream();
                    StreamWriter sw = new StreamWriter(ns);
                    if (path == "")
                    {
                        if (cvs != "")
                        {
                            sw.WriteLine(type+";"+ cvs +";"+ content);      // Gửi tin nhắn
                            sw.Flush();
                        }
                        else
                        {
                            sw.WriteLine(content);      // Gửi tin nhắn
                            sw.Flush();
                        }
                    }
                    else        // Gửi tập tin
                    {
                        DoSendFile(sw, path, type,cvs);
                    }
                    return;
                }
                catch { }
            }
            // Nếu ko ghi được
            while (!server.Connected)
            {
                // Thử kết nối lại
                if (MessageBox.Show("Mất kết nối tới server\nThử kết nối lại?", "Lỗi", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    try
                    {
                        server = new TcpClient("localhost", 9090);
                    }
                    catch { }
                else
                    break;
            }
        }
        /// <summary>
        /// Thực hiện gửi file tới server
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="path"></param>
        void DoSendFile(StreamWriter sw, string path,string type, string name_cvs)
        {
            pnlFile.Visible = true;
            Cursor = Cursors.WaitCursor;
            if (type == "private")
            {
                sw.WriteLine("file"+";"+type+";"+name_cvs);
                sw.Flush();
            }
            else
            {
                sw.WriteLine("file" + ";" + type + ";" + name_cvs);
                sw.Flush();
            }
            sw.WriteLine(Path.GetFileName(path));
            sw.Flush();

            FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read);
            int numPackets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(file.Length) / Convert.ToDouble(BufferSize)));
            prgFile.Maximum = numPackets;
            prgFile.Value = 0;
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

                if (prgFile.Value >= prgFile.Maximum)
                    prgFile.Value = prgFile.Minimum;
                prgFile.PerformStep();
                lblFile.Text = "Đã gửi " + prgFile.Value + "/" + prgFile.Maximum;
            }
            file.Close();
            Cursor = Cursors.Default;
            pnlFile.Visible = false;
        }
        /// <summary>
        /// Gửi hình ảnh & tập tin đính kèm
        /// </summary>
        /// <param name="path"></param>
        void SendImage(string path,string type, string name_cvs)
        {
            Uri url = new Uri(path);
            string address = url.AbsoluteUri;
            string extension = Path.GetExtension(path).ToLower();
            string content = "<a href='" + url.PathAndQuery.Replace(":", "(~*)") + "'>" + ((extension == ".jpg" || extension == ".png") ? "<img src='" + address + "' style='max-width:300px'/><br/>" : "") + "<b>" + Path.GetFileName(path) + "</b></a> (" + FileSize.SizeSuffix(new FileInfo(path).Length) + ")";

            SendMessage(content, path, type, name_cvs);
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
        /// Lắng nghe từ server
        /// </summary>
        void Listening()
        {
            ns = server.GetStream();
            sr = new StreamReader(ns);
            string s;
            while (server.Connected)
            {
                try
                {
                    s = sr.ReadLine();
                    string[] s1 = s.Split(';');
                    if (s != null)
                    {
                        this.Invoke(new Action(() =>
                        {
                            Flash();
                        }));
                        switch (s1[0])
                        {
                            case "list":
                                addlistclient(s1);
                                break;
                            case "private":
                                this.Invoke(new MethodInvoker(delegate ()
                                {
                                    if (btn_name == s1[1])
                                    {
                                        add_list_wait_conversation(s1[1], s1[2], s1[1]);
                                        RefreshWeb(s1[1]);
                                    }
                                    else
                                    {
                                        timer(s1[1]);
                                        add_list_wait_conversation(s1[1], s1[2],s1[1]);
                                    }
                                }));
                                break;
                            case "public":
                                this.Invoke(new MethodInvoker(delegate ()
                                {
                                    if (btn_name == s1[1])
                                    {
                                        add_list_wait_conversation(s1[1], s1[2], s1[3]);
                                        RefreshWeb(s1[1]);
                                    }
                                    else
                                    {
                                        timer("button1");
                                        add_list_wait_conversation(s1[1], s1[2], s1[3]);
                                    }
                                    
                                }));
                                break;
                            case "file":
                                    add_file_into_conversation(sr, s1[1], s1[2]);
                                break;
                        }
                    }
                }
                catch { }
                //Nếu ko đọc được hoặc nội dung đọc về là null
                while (!server.Connected)
                {
                    // Thử kết nối lại
                    if (MessageBox.Show("Mất kết nối tới server\nThử kết nối lại?", "Lỗi", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                        try
                        {
                            server = new TcpClient(server.ToString(), 9090);
                        }
                        catch { }
                    else
                    {
                        Thread.CurrentThread.Abort();       // Ngắt kết nối
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Thực hiện nhận file
        /// </summary>
        /// <param name="sr"></param>
        /// <returns>FileName đã nhận</returns>
        void add_file_into_conversation(StreamReader sr, string type,string name_sender)
        {
            string s = DoReciveFile(sr); 
            
            if (type =="private")
            {
                if (btn_name == name_sender)
                {
                    add_list_wait_conversation(name_sender, s, name_sender);
                    RefreshWeb(name_sender);
                }
                else
                {
                    timer(name_sender);
                    add_list_wait_conversation(name_sender, s, name_sender);
                }
            }
            else if (type == "public")
            {
                if (btn_name == type)
                {
                    add_list_wait_conversation(type, s, name_sender);
                    RefreshWeb(type);
                }
                else
                {
                    timer("button1");
                    add_list_wait_conversation(type, s, name_sender);
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
            string s = "<a href='" + url.PathAndQuery.Replace(":", "(~*)") + "'>" + ((extension == ".jpg" || extension == ".png") ? "<img src='" + address + "' style='max-width:300px'/><br/>" : "") + "<b>" + filename + "</b></a> (" + FileSize.SizeSuffix(new FileInfo(filename).Length) + ")";
            return s;
        }

        /// <summary>
        /// Nạp lại nội dung tin nhắn
        /// </summary>
        void RefreshWeb(string cvs)
        {
            // Chưa scroll xuống cuối được => có thể phải thêm nút js
            this.webMain.Invoke(new MethodInvoker(delegate ()
            {
                if (cvs == "")
                {
                    webMain.Document.Write(conversation.GetHTML);
                    webMain.Refresh();
                }
                else
                {
                    webMain.Document.OpenNew(true);
                    webMain.Document.Write(getconversation(cvs).GetHTML);
                    webMain.Refresh();
                }
                
            }));
        }
        #endregion

        #region Forms
        public frmClient()
        {
            InitializeComponent();
            //this.Invoke(new MethodInvoker(delegate() {  })) ;
            
        }

        private void frmClient_Load(object sender, EventArgs e)
        {
            //lblTitle.Text = this.Text = na;
            AttemptLogin();
            txtMessage.Focus();
            this.Padding = new Padding(1);//Setting.TcpServer;
            server = new TcpClient();
            server.Connect("xuanthoi", 9090);
            //AttemptLogin();
            //Setting.TcpServer = server;
            
            this.sender = new Sender() { Address = "localhost", Port = 9090, Tcp = server };
            
            t = new Thread(Listening);
            t.Start();
            NetworkStream ns = server.GetStream();
            StreamWriter sw = new StreamWriter(ns);
            sw.WriteLine("nick_name;" + name);
            sw.Flush();
        }

        private void frmClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            SendMessage("exit;", "","","");
            foreach (var f in lstFileName)
            {
                try
                {
                    this.Invoke(new MethodInvoker(delegate ()
                    {
                        if (File.Exists(f))
                            File.Delete(f);
                    }));
                }
                catch { }
            }
            t.Abort();
            server.Close();
        }

        private void frmClient_Deactived(object sender, EventArgs e)
        {
            BackColor = Color.DimGray;
            _focus = false;
        }

        private void frmClient_Activated(object sender, EventArgs e)
        {
            BackColor = Color.DarkOrange;
            txtMessage.Focus();

            _focus = true;
            FlashWindow.Stop(this);
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
        #endregion

        #region Move form
        private void frmClient_MouseDown(object sender, MouseEventArgs e)
        {
            drag = true;
            dragCursor = Cursor.Position;
            dragForm = this.Location;
        }

        private void frmClient_MouseMove(object sender, MouseEventArgs e)
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

        private void frmClient_MouseUp(object sender, MouseEventArgs e)
        {
            drag = false;
        }
        #endregion

        #region Controls
        private void btnExit_Click(object sender, EventArgs e)
        {
            //Thread.CurrentThread.Abort();       // Ngắt kết nối
            Application.Exit();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (txtMessage.Text != "")
            {
                if (name_conversation != "")
                {
                    SendMessage(txtMessage.Text, "",type, name_conversation);
                    txtMessage.Text = "";
                    txtMessage.Focus();
                }
                else
                {
                    SendMessage(txtMessage.Text, "","", "");
                    txtMessage.Text = "";
                    txtMessage.Focus();
                }
            }
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
                btnSend.PerformClick();

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
            WindowState = FormWindowState.Minimized;
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

        private void btnImage_Click(object sender, EventArgs e)
        {
            OpenFileDialog dia = new OpenFileDialog();
            dia.RestoreDirectory = true;
            dia.Title = "Chọn tập tin đính kèm";
            if (dia.ShowDialog() == DialogResult.OK)
                SendImage(dia.FileName,type, btn_name );
        }
        #endregion

        public delegate void getname(string name);
        void AttemptLogin()
        {
            Login frmLogin = new Login();
            frmLogin.StartPosition = FormStartPosition.CenterParent;
            frmLogin.get_name = new Login.getname(get_name);
            frmLogin.ShowDialog(this);
            //frmLogin.Dispose();
            //this.Close();
        }
        private void get_name(string name)
        {
            this.name = name;
            lblTitle.Text = this.name;
        }
        private void addlistclient(string[] s)
        {
            lstsd = new List<Sender>();
            if (this.InvokeRequired)
                Invoke(new Action(() =>
                {
                    if (s.Length == 0)
                    {
                        paneluser.Visible = false;
                        return;
                    }
                    paneluser.SuspendLayout();
                    paneluser.Controls.Clear();
                    paneluser.AutoScroll = false;
                    paneluser.VerticalScroll.Visible = false;
                    foreach (string sender in s)
                    {
                        if (sender != "" && sender != "list")
                        {
                            Sender sd = new Sender();
                            if (sd.NickName != sender)
                            {
                                sd = new Sender();
                                sd.NickName = sender;
                                if (sd.But != null)
                                {
                                    toolTip1.SetToolTip(sd.But, sd.NickName);
                                    // "Địa chỉ " + sender.Address + " cổng " + sender.Port);
                                    paneluser.Controls.Add(sd.But);
                                    sd.call = new Sender.CallBack(addconversation);      // Khi sender thực hiện delegate nó sẽ add vao list conversation
                                    lstsd.Add(sd);
                                }
                            }
                        }
                    }

                    paneluser.ResumeLayout();
                    paneluser.Visible = true;
                    // Update the form
                    this.PerformLayout();
                }));
        }
        public void addconversation(string name, string type_)
        {
            if (btn_flash == name)
            {
                timer1.Stop();
                if (type_ == "public")
                {
                    lblTitle.Text = this.name + " : " + "Bạn đang chat public với mọi người";
                }
                else
                {
                    lblTitle.Text = this.name + " : " + "Bạn đang chat với - " + name;
                }

                if (checklistchatexist(name))
                {
                    Conversation cs = new Conversation();
                    cs.Name_cvs = name;
                    listchat.Add(cs);
                    webMain.Document.Write(cs.GetHTML);
                    webMain.Refresh();
                    name_conversation = name;
                    type = type_;
                    btn_name = name;
                }
                else
                {
                    webMain.Document.Write(getconversation(name).GetHTML);
                    webMain.Refresh();
                    name_conversation = name;
                    type = type_;
                    btn_name = name;
                }
            }
            else
            {
                if (type_ == "public")
                {
                    lblTitle.Text = this.name + " : " + "Bạn đang chat public với mọi người";
                }
                else
                {
                    lblTitle.Text = this.name + " : " + "Bạn đang chat với - " + name;
                }

                if (checklistchatexist(name))
                {
                    Conversation cs = new Conversation();
                    cs.Name_cvs = name;
                    listchat.Add(cs);
                    webMain.Document.Write(cs.GetHTML);
                    webMain.Refresh();
                    name_conversation = name;
                    type = type_;
                    btn_name = name;
                }
                else
                {
                    webMain.Document.Write(getconversation(name).GetHTML);
                    webMain.Refresh();
                    name_conversation = name;
                    type = type_;
                    btn_name = name;
                }
            }
        }
        public void add_list_wait_conversation(string name, string mess, string sender)
        {
            if (checklistchatexist(name))
            {
                Conversation cs = new Conversation();
                cs.Name_cvs = name;
                listchat.Add(cs);
                cs.AddMessage(new Message() { Content = mess, Time = DateTime.Now, Sender = new Sender() { NickName = sender } });
            }
            else
            {
                Conversation cs = new Conversation();
                cs = getconversation(name);
                cs.AddMessage(new Message() { Content = mess, Time = DateTime.Now, Sender = new Sender() { NickName = sender } });
            }
        }
        public bool checklistchatexist(string name)
        {
            bool check = true;
            foreach (var cs in listchat)
            {
                if (cs.Name_cvs == name)
                {
                    return false;
                }
            }
            return check;
        }
        private Conversation getconversation(string name)
        {
            Conversation cs = new Conversation();
            foreach (var cvs in listchat)
            {
                if (cvs.Name_cvs == name)
                {
                    cs = cvs;
                    break;
                }
            }
            return cs;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            btn_name = "button1";
            button1.BackColor = Color.PaleTurquoise;
            timer1.Stop();
            addconversation("public","public");
            lblTitle.Text = this.name + " : " + "Bạn đang chat public với mọi người";
            //type = "public;";
        }
        private void timer(string btn_name)
        {
            this.Invoke(new MethodInvoker(delegate ()
            {
                this.btn_flash = btn_name;
                timer1.Enabled = true;
                timer1.Interval = 500;
                timer1.Start();
            }));
            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            flash_button(btn_flash);
        }
        private void flash_button(string btn_name)
        {
            bool flag = true;
            Random ran = new Random();
            foreach (var item in lstsd)
            {
                if (item.NickName == btn_name)
                {
                    item.But.BackColor = Color.FromArgb(ran.Next(100, 240), ran.Next(100, 240), ran.Next(100, 240));
                    flag = false;
                }
            }
            if (flag)
            {
                button1.BackColor = Color.FromArgb(ran.Next(100, 240), ran.Next(100, 240), ran.Next(100, 240));
            }
            paneluser.Update();
            
        }

        private Sender get_sender(string name)
        {
            foreach (var sender in lstsd)
            {
                if (sender.NickName == name)
                {
                    return sender;
                }
            }
            return null;
        }
    }
}
