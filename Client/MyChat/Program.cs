using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FoxLearn.License;
using System.IO;

namespace MyChat
{
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// 
        /// Ngày tạo 28/03/2016
        /// Sửa cuối cùng 31/03/2016
        /// Có các tính năng:
        /// - Hiển thị web trong form
        /// - Truyền tin qua socket, hình ảnh
        /// - Nhiều client, thread
        /// - Nháy icon taskbar & hiển thị số tin nhắn
        /// - List Button
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //Setting.Port = 9090;
            //Setting.MarkSendFile = "@#$filetxt##$#@";
            ///License
            ///
            string id = ComputerInfo.GetComputerId();
            KeyManager km = new KeyManager(id);
            LicenseInfo lic = new LicenseInfo();
            int value = km.LoadSuretyFile(string.Format(@"{0}\Key.lic", Application.StartupPath), ref lic);
            string productkey = lic.ProductKey;
            if(value <= 0)
            {
                Application.Run(new License());
                //Application.Run(new frmClient());
            }
            else if(value >0)
            {
                KeyValuesClass kv = new KeyValuesClass();
                if (km.DisassembleKey(productkey, ref kv))
                {

                    //lblProductname.Text = lic.FullName;//lic.ProductKey
                    //lblProductKey.Text = lic.ProductKey; //productkey;
                    if (kv.Type == LicenseType.TRIAL)
                    {
                        int day = Convert.ToInt32((kv.Expiration - DateTime.Now.Date).Days);
                        if (day >= 0)
                        {
                            DialogResult dr = MessageBox.Show("Bạn còn: " + string.Format("{0} ngày ", (kv.Expiration - DateTime.Now.Date).Days) + "sử dụng phần mềm! \n Bạn có muốn nhập key để active","active",MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (dr == DialogResult.Yes)
                            {
                                Application.Run(new License());
                            }
                            Application.Run(new frmClient());
                        }else
                        {
                            MessageBox.Show("Bạn đã hết hạn dùng thử vui lòng nhập key để active!","Active",MessageBoxButtons.YesNo,MessageBoxIcon.Warning);
                            Application.Run(new License());
                        }
                    }
                    else
                        //lblLicenceType.Text = "Full";
                        Application.Run(new frmClient());
                }
            }
            //Application.Run(new frmClient());
            
        }
    }
}
