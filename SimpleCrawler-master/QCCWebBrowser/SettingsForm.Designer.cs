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
            this.button2 = new System.Windows.Forms.Button();
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.button3 = new System.Windows.Forms.Button();
            this.isProvinceCHK = new System.Windows.Forms.CheckBox();
            this.onlyDateUpdateCHK = new System.Windows.Forms.CheckBox();
            this.GRegistCapiEndTxt = new System.Windows.Forms.TextBox();
            this.GRegistCapiBeginTxt = new System.Windows.Forms.TextBox();
            this.enterpriseIpTxt = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.searchKeyTypeComBox = new System.Windows.Forms.ComboBox();
            this.industryCHK = new System.Windows.Forms.CheckBox();
            this.moreDetailInfoCHK = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
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
            this.groupBox1.Location = new System.Drawing.Point(6, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(571, 339);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "AppSetting";
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
            this.label7.Size = new System.Drawing.Size(65, 12);
            this.label7.TabIndex = 1;
            this.label7.Text = "设备列表：";
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
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(595, 399);
            this.tabControl1.TabIndex = 4;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(587, 373);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "设备Key设定";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.searchKeyTypeComBox);
            this.tabPage2.Controls.Add(this.button3);
            this.tabPage2.Controls.Add(this.moreDetailInfoCHK);
            this.tabPage2.Controls.Add(this.isProvinceCHK);
            this.tabPage2.Controls.Add(this.industryCHK);
            this.tabPage2.Controls.Add(this.onlyDateUpdateCHK);
            this.tabPage2.Controls.Add(this.GRegistCapiEndTxt);
            this.tabPage2.Controls.Add(this.GRegistCapiBeginTxt);
            this.tabPage2.Controls.Add(this.enterpriseIpTxt);
            this.tabPage2.Controls.Add(this.label14);
            this.tabPage2.Controls.Add(this.label12);
            this.tabPage2.Controls.Add(this.label13);
            this.tabPage2.Controls.Add(this.label11);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(587, 373);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "全局变量";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(226, 338);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 3;
            this.button3.Text = "保存";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // isProvinceCHK
            // 
            this.isProvinceCHK.AutoSize = true;
            this.isProvinceCHK.Location = new System.Drawing.Point(283, 61);
            this.isProvinceCHK.Name = "isProvinceCHK";
            this.isProvinceCHK.Size = new System.Drawing.Size(84, 16);
            this.isProvinceCHK.TabIndex = 2;
            this.isProvinceCHK.Text = "是否省爬取";
            this.isProvinceCHK.UseVisualStyleBackColor = true;
            // 
            // onlyDateUpdateCHK
            // 
            this.onlyDateUpdateCHK.AutoSize = true;
            this.onlyDateUpdateCHK.Location = new System.Drawing.Point(138, 61);
            this.onlyDateUpdateCHK.Name = "onlyDateUpdateCHK";
            this.onlyDateUpdateCHK.Size = new System.Drawing.Size(120, 16);
            this.onlyDateUpdateCHK.TabIndex = 2;
            this.onlyDateUpdateCHK.Text = "是否进行日期更新";
            this.onlyDateUpdateCHK.UseVisualStyleBackColor = true;
            // 
            // GRegistCapiEndTxt
            // 
            this.GRegistCapiEndTxt.Location = new System.Drawing.Point(376, 80);
            this.GRegistCapiEndTxt.Name = "GRegistCapiEndTxt";
            this.GRegistCapiEndTxt.Size = new System.Drawing.Size(98, 21);
            this.GRegistCapiEndTxt.TabIndex = 1;
            // 
            // GRegistCapiBeginTxt
            // 
            this.GRegistCapiBeginTxt.Location = new System.Drawing.Point(138, 80);
            this.GRegistCapiBeginTxt.Name = "GRegistCapiBeginTxt";
            this.GRegistCapiBeginTxt.Size = new System.Drawing.Size(120, 21);
            this.GRegistCapiBeginTxt.TabIndex = 1;
            // 
            // enterpriseIpTxt
            // 
            this.enterpriseIpTxt.Location = new System.Drawing.Point(138, 13);
            this.enterpriseIpTxt.Name = "enterpriseIpTxt";
            this.enterpriseIpTxt.Size = new System.Drawing.Size(362, 21);
            this.enterpriseIpTxt.TabIndex = 1;
            this.enterpriseIpTxt.Text = "192.168.1.124";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(281, 89);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(89, 12);
            this.label14.TabIndex = 0;
            this.label14.Text = "注册金额结束：";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(18, 89);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(89, 12);
            this.label13.TabIndex = 0;
            this.label13.Text = "注册金额开始：";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(18, 16);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(89, 12);
            this.label11.TabIndex = 0;
            this.label11.Text = "enterpriseIp：";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(18, 145);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(65, 12);
            this.label12.TabIndex = 0;
            this.label12.Text = "搜索模式：";
            // 
            // searchKeyTypeComBox
            // 
            this.searchKeyTypeComBox.FormattingEnabled = true;
            this.searchKeyTypeComBox.Location = new System.Drawing.Point(138, 145);
            this.searchKeyTypeComBox.Name = "searchKeyTypeComBox";
            this.searchKeyTypeComBox.Size = new System.Drawing.Size(121, 20);
            this.searchKeyTypeComBox.TabIndex = 4;
            this.searchKeyTypeComBox.SelectedIndexChanged += new System.EventHandler(this.searchKeyTypeComBox_SelectedIndexChanged);
            // 
            // industryCHK
            // 
            this.industryCHK.AutoSize = true;
            this.industryCHK.Location = new System.Drawing.Point(283, 149);
            this.industryCHK.Name = "industryCHK";
            this.industryCHK.Size = new System.Drawing.Size(84, 16);
            this.industryCHK.TabIndex = 2;
            this.industryCHK.Text = "产业园搜索";
            this.industryCHK.UseVisualStyleBackColor = true;
            // 
            // moreDetailInfoCHK
            // 
            this.moreDetailInfoCHK.AutoSize = true;
            this.moreDetailInfoCHK.Location = new System.Drawing.Point(376, 58);
            this.moreDetailInfoCHK.Name = "moreDetailInfoCHK";
            this.moreDetailInfoCHK.Size = new System.Drawing.Size(120, 16);
            this.moreDetailInfoCHK.TabIndex = 2;
            this.moreDetailInfoCHK.Text = "是否爬取背后关系";
            this.moreDetailInfoCHK.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(595, 399);
            this.Controls.Add(this.tabControl1);
            this.Name = "SettingsForm";
            this.Text = "SettingsForm";
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
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
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TextBox enterpriseIpTxt;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckBox isProvinceCHK;
        private System.Windows.Forms.CheckBox onlyDateUpdateCHK;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.TextBox GRegistCapiEndTxt;
        private System.Windows.Forms.TextBox GRegistCapiBeginTxt;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.ComboBox searchKeyTypeComBox;
        private System.Windows.Forms.CheckBox moreDetailInfoCHK;
        private System.Windows.Forms.CheckBox industryCHK;
        private System.Windows.Forms.Label label12;
    }
}