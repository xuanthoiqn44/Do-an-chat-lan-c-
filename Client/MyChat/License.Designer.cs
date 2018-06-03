namespace MyChat
{
    partial class License
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.buttontrial = new System.Windows.Forms.Button();
            this.buttoncheck = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxkey = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.buttontrial);
            this.groupBox1.Controls.Add(this.buttoncheck);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.textBoxkey);
            this.groupBox1.Location = new System.Drawing.Point(2, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(407, 205);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Nhap key active";
            // 
            // buttontrial
            // 
            this.buttontrial.Location = new System.Drawing.Point(287, 98);
            this.buttontrial.Name = "buttontrial";
            this.buttontrial.Size = new System.Drawing.Size(75, 34);
            this.buttontrial.TabIndex = 3;
            this.buttontrial.Text = "Trial";
            this.buttontrial.UseVisualStyleBackColor = true;
            this.buttontrial.Click += new System.EventHandler(this.buttontrial_Click);
            // 
            // buttoncheck
            // 
            this.buttoncheck.Location = new System.Drawing.Point(167, 98);
            this.buttoncheck.Name = "buttoncheck";
            this.buttoncheck.Size = new System.Drawing.Size(75, 34);
            this.buttoncheck.TabIndex = 2;
            this.buttoncheck.Text = "Active";
            this.buttoncheck.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(28, 56);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(28, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Key:";
            // 
            // textBoxkey
            // 
            this.textBoxkey.Location = new System.Drawing.Point(62, 53);
            this.textBoxkey.Name = "textBoxkey";
            this.textBoxkey.Size = new System.Drawing.Size(300, 20);
            this.textBoxkey.TabIndex = 1;
            // 
            // License
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(413, 208);
            this.Controls.Add(this.groupBox1);
            this.Name = "License";
            this.Text = "License";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button buttontrial;
        private System.Windows.Forms.Button buttoncheck;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxkey;
    }
}