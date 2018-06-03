using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MyChat
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        public delegate void getname(string name);
        public getname get_name ;
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != string.Empty)
            {
                if (get_name != null)
                {
                    get_name.Invoke(textBox1.Text);
                    this.Close();
                }
                
            }
            else { MessageBox.Show("Vui lòng nhập tên!","Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
