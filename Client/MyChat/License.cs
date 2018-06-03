using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FoxLearn.License;

namespace MyChat
{
    public partial class License : Form
    {
        public License()
        {
            InitializeComponent();
        }
        const int ProductCode = 1;
        private void buttontrial_Click(object sender, EventArgs e)
        {
            KeyManager km = new KeyManager(ComputerInfo.GetComputerId());
            KeyValuesClass kv;
            string productkey = string.Empty;
            kv = new KeyValuesClass()
            {
                Type = LicenseType.TRIAL,
                Header = Convert.ToByte(9),
                Footer = Convert.ToByte(6),
                ProductCode = (byte)ProductCode,
                Edition = Edition.ENTERPRISE,
                Version = 1,
                Expiration = DateTime.Now.Date.AddDays(30),
            };
            if (!km.GenerateKey(kv, ref productkey))
                MessageBox.Show("Loi!");
            //luu key
            if (km.ValidKey(ref productkey))
            {
                kv = new KeyValuesClass();
                if (km.DisassembleKey(productkey, ref kv))
                {
                    LicenseInfo lic = new LicenseInfo();
                    lic.ProductKey = productkey;
                    lic.FullName = Environment.MachineName;
                    if (kv.Type == LicenseType.TRIAL)
                    {
                        lic.Day = kv.Expiration.Day;
                        lic.Month = kv.Expiration.Month;
                        lic.Year = kv.Expiration.Year;
                    }
                    km.SaveSuretyFile(string.Format(@"{0}\Key.lic", Application.StartupPath), lic);
                    MessageBox.Show("Congratulations,SuccessFully Register.", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }

            }
            else
                MessageBox.Show("Your Product Key is invalid", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //Application.Run(new frmClient());
            frmClient frmclient = new frmClient();
            frmclient.Show();
        }
    }
}
