namespace QCCWebBrowser
{
    partial class SettingsForm
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
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.statusTxt = new System.Windows.Forms.TextBox();
            this.getCountTxt = new System.Windows.Forms.TextBox();
            this.isBusyTxt = new System.Windows.Forms.TextBox();
            this.accessTokenTxt = new System.Windows.Forms.TextBox();
            this.refleshTokenTxt = new System.Windows.Forms.TextBox();
            this.signTxt = new System.Windows.Forms.TextBox();
            this.timestampTxt = new System.Windows.Forms.TextBox();
            this.deviceTxt = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(242, 310);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "保存";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 64);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "AppId：";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(115, 61);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(450, 21);
            this.textBox1.TabIndex = 2;
            this.textBox1.Text = "80c9ef0fb86369cd25f90af27ef53a9e";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.button2);
            this.groupBox1.Controls.Add(this.comboBox1);
            this.groupBox1.Controls.Add(this.label10);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.button1);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.statusTxt);
            this.groupBox1.Controls.Add(this.getCountTxt);
            this.groupBox1.Controls.Add(this.isBusyTxt);
            this.groupBox1.Controls.Add(this.accessTokenTxt);
            this.groupBox1.Controls.Add(this.refleshTokenTxt);
            this.groupBox1.Controls.Add(this.signTxt);
            this.groupBox1.Controls.Add(this.timestampTxt);
            this.groupBox1.Controls.Add(this.deviceTxt);
            this.groupBox1.Controls.Add(this.textBox1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(571, 339);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "AppSetting";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(115, 26);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(450, 20);
            this.comboBox1.TabIndex = 3;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(20, 282);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(47, 12);
            this.label10.TabIndex = 1;
            this.label10.Text = "status:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(20, 255);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(59, 12);
            this.label9.TabIndex = 1;
            this.label9.Text = "GetCount:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(20, 228);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(47, 12);
            this.label8.TabIndex = 1;
            this.label8.Text = "isBusy:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(20, 198);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(83, 12);
            this.label6.TabIndex = 1;
            this.label6.Text = "AccessToken：";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(20, 172);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(89, 12);
            this.label5.TabIndex = 1;
            this.label5.Text = "RefleshToken：";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(20, 145);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 12);
            this.label4.TabIndex = 1;
            this.label4.Text = "sign：";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(20, 118);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(71, 12);
            this.label3.TabIndex = 1;
            this.label3.Text = "timestamp：";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 91);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 12);
            this.label2.TabIndex = 1;
            this.label2.Text = "DeviceId：";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(20, 35);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(53, 12);
            this.label7.TabIndex = 1;
            this.label7.Text = "设备列表";
            // 
            // statusTxt
            // 
            this.statusTxt.Location = new System.Drawing.Point(115, 276);
            this.statusTxt.Name = "statusTxt";
            this.statusTxt.Size = new System.Drawing.Size(450, 21);
            this.statusTxt.TabIndex = 2;
            // 
            // getCountTxt
            // 
            this.getCountTxt.Location = new System.Drawing.Point(115, 249);
            this.getCountTxt.Name = "getCountTxt";
            this.getCountTxt.Size = new System.Drawing.Size(450, 21);
            this.getCountTxt.TabIndex = 2;
            // 
            // isBusyTxt
            // 
            this.isBusyTxt.Location = new System.Drawing.Point(115, 222);
            this.isBusyTxt.Name = "isBusyTxt";
            this.isBusyTxt.Size = new System.Drawing.Size(450, 21);
            this.isBusyTxt.TabIndex = 2;
            // 
            // accessTokenTxt
            // 
            this.accessTokenTxt.Location = new System.Drawing.Point(115, 195);
            this.accessTokenTxt.Name = "accessTokenTxt";
            this.accessTokenTxt.Size = new System.Drawing.Size(450, 21);
            this.accessTokenTxt.TabIndex = 2;
            // 
            // refleshTokenTxt
            // 
            this.refleshTokenTxt.Location = new System.Drawing.Point(115, 169);
            this.refleshTokenTxt.Name = "refleshTokenTxt";
            this.refleshTokenTxt.Size = new System.Drawing.Size(450, 21);
            this.refleshTokenTxt.TabIndex = 2;
            // 
            // signTxt
            // 
            this.signTxt.Location = new System.Drawing.Point(115, 142);
            this.signTxt.Name = "signTxt";
            this.signTxt.Size = new System.Drawing.Size(450, 21);
            this.signTxt.TabIndex = 2;
            // 
            // timestampTxt
            // 
            this.timestampTxt.Location = new System.Drawing.Point(115, 115);
            this.timestampTxt.Name = "timestampTxt";
            this.timestampTxt.Size = new System.Drawing.Size(450, 21);
            this.timestampTxt.TabIndex = 2;
            // 
            // deviceTxt
            // 
            this.deviceTxt.Location = new System.Drawing.Point(115, 88);
            this.deviceTxt.Name = "deviceTxt";
            this.deviceTxt.Size = new System.Drawing.Size(450, 21);
            this.deviceTxt.TabIndex = 2;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(7, 310);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(96, 23);
            this.button2.TabIndex = 4;
            this.button2.Text = "ReloadAccount";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(595, 363);
            this.Controls.Add(this.groupBox1);
            this.Name = "SettingsForm";
            this.Text = "SettingsForm";
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox refleshTokenTxt;
        private System.Windows.Forms.TextBox signTxt;
        private System.Windows.Forms.TextBox timestampTxt;
        private System.Windows.Forms.TextBox deviceTxt;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox accessTokenTxt;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox getCountTxt;
        private System.Windows.Forms.TextBox isBusyTxt;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox statusTxt;
        private System.Windows.Forms.Button button2;
    }
}