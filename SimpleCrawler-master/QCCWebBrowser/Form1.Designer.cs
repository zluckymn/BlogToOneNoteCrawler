namespace QCCWebBrowser
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.webBrowser = new System.Windows.Forms.WebBrowser();
            this.richTextBoxInfo = new System.Windows.Forms.RichTextBox();
            this.richTextBox = new System.Windows.Forms.RichTextBox();
            this.textBox = new System.Windows.Forms.TextBox();
            this.searchBtn = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.setBusyBtn = new System.Windows.Forms.Button();
            this.delBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.startCrawlerBtn = new System.Windows.Forms.Button();
            this.checkBox = new System.Windows.Forms.CheckBox();
            this.comboBox = new System.Windows.Forms.ComboBox();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox5 = new System.Windows.Forms.TextBox();
            this.accountInfoTxt = new System.Windows.Forms.Label();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.checkBoxGuard = new System.Windows.Forms.CheckBox();
            this.guardTimer = new System.Windows.Forms.Timer(this.components);
            this.ipProxyTxt = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.autoChangeAccountCHK = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.ipProxyTxt2 = new System.Windows.Forms.TextBox();
            this.UseProxyCHK = new System.Windows.Forms.CheckBox();
            this.UserProxyChk = new System.Windows.Forms.CheckBox();
            this.button5 = new System.Windows.Forms.Button();
            this.AccountPhoneCodeSenderTimer = new System.Windows.Forms.Timer(this.components);
            this.AccountRegTimer = new System.Windows.Forms.Timer(this.components);
            this.button6 = new System.Windows.Forms.Button();
            this.button7 = new System.Windows.Forms.Button();
            this.button8 = new System.Windows.Forms.Button();
            this.ipChangeTimer = new System.Windows.Forms.Timer(this.components);
            this.button9 = new System.Windows.Forms.Button();
            this.button10 = new System.Windows.Forms.Button();
            this.button11 = new System.Windows.Forms.Button();
            this.EnterpriseKeySuffixTxt = new System.Windows.Forms.TextBox();
            this.singalKeyWordCHK = new System.Windows.Forms.CheckBox();
            this.button12 = new System.Windows.Forms.Button();
            this.splitLimitChk = new System.Windows.Forms.CheckBox();
            this.autoPassKeyWordChk = new System.Windows.Forms.CheckBox();
            this.PassKeyWordtimer = new System.Windows.Forms.Timer(this.components);
            this.AutoChangeIp = new System.Windows.Forms.CheckBox();
            this.button13 = new System.Windows.Forms.Button();
            this.KeyWordFilterTextBox = new System.Windows.Forms.TextBox();
            this.KeyWordFilterlabel = new System.Windows.Forms.Label();
            this.updateDateTxt = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.proxyListCB = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.keyWordSourceCHK = new System.Windows.Forms.CheckBox();
            this.label10 = new System.Windows.Forms.Label();
            this.MaxAccountCrawlerCountTxt = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // webBrowser
            // 
            this.webBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.webBrowser.Location = new System.Drawing.Point(-11, 71);
            this.webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser.Name = "webBrowser";
            this.webBrowser.ScriptErrorsSuppressed = true;
            this.webBrowser.Size = new System.Drawing.Size(1257, 562);
            this.webBrowser.TabIndex = 0;
            this.webBrowser.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.webBrowser_DocumentCompleted);
            this.webBrowser.Navigated += new System.Windows.Forms.WebBrowserNavigatedEventHandler(this.webBrowser_Navigated);
            this.webBrowser.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.webBrowser_Navigating);
            // 
            // richTextBoxInfo
            // 
            this.richTextBoxInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxInfo.Location = new System.Drawing.Point(-1, 656);
            this.richTextBoxInfo.Name = "richTextBoxInfo";
            this.richTextBoxInfo.Size = new System.Drawing.Size(1257, 59);
            this.richTextBoxInfo.TabIndex = 1;
            this.richTextBoxInfo.Text = "";
            // 
            // richTextBox
            // 
            this.richTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBox.Location = new System.Drawing.Point(-1, 721);
            this.richTextBox.Name = "richTextBox";
            this.richTextBox.Size = new System.Drawing.Size(1257, 17);
            this.richTextBox.TabIndex = 2;
            this.richTextBox.Text = "";
            // 
            // textBox
            // 
            this.textBox.Location = new System.Drawing.Point(-1, 2);
            this.textBox.Name = "textBox";
            this.textBox.Size = new System.Drawing.Size(200, 21);
            this.textBox.TabIndex = 3;
            this.textBox.Text = "http://www.qichacha.com/user_login";
            // 
            // searchBtn
            // 
            this.searchBtn.Location = new System.Drawing.Point(199, 1);
            this.searchBtn.Name = "searchBtn";
            this.searchBtn.Size = new System.Drawing.Size(57, 23);
            this.searchBtn.TabIndex = 4;
            this.searchBtn.Text = "search";
            this.searchBtn.UseVisualStyleBackColor = true;
            this.searchBtn.Click += new System.EventHandler(this.searchBtn_Click);
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(259, 3);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(109, 20);
            this.comboBox1.TabIndex = 5;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // setBusyBtn
            // 
            this.setBusyBtn.Location = new System.Drawing.Point(424, 0);
            this.setBusyBtn.Name = "setBusyBtn";
            this.setBusyBtn.Size = new System.Drawing.Size(40, 23);
            this.setBusyBtn.TabIndex = 6;
            this.setBusyBtn.Text = "频繁";
            this.setBusyBtn.UseVisualStyleBackColor = true;
            this.setBusyBtn.Click += new System.EventHandler(this.setBusyBtn_Click);
            // 
            // delBtn
            // 
            this.delBtn.Location = new System.Drawing.Point(465, 0);
            this.delBtn.Name = "delBtn";
            this.delBtn.Size = new System.Drawing.Size(32, 23);
            this.delBtn.TabIndex = 7;
            this.delBtn.Text = "DEL";
            this.delBtn.UseVisualStyleBackColor = true;
            this.delBtn.Click += new System.EventHandler(this.delBtn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(502, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 12);
            this.label1.TabIndex = 8;
            this.label1.Text = "account：";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(562, 2);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(83, 21);
            this.textBox1.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(647, 5);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 12);
            this.label2.TabIndex = 10;
            this.label2.Text = "password：";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(712, 2);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(69, 21);
            this.textBox2.TabIndex = 9;
            // 
            // startCrawlerBtn
            // 
            this.startCrawlerBtn.Location = new System.Drawing.Point(787, 1);
            this.startCrawlerBtn.Name = "startCrawlerBtn";
            this.startCrawlerBtn.Size = new System.Drawing.Size(86, 23);
            this.startCrawlerBtn.TabIndex = 11;
            this.startCrawlerBtn.Text = "startCrawler";
            this.startCrawlerBtn.UseVisualStyleBackColor = true;
            this.startCrawlerBtn.Click += new System.EventHandler(this.startCrawlerBtn_Click);
            // 
            // checkBox
            // 
            this.checkBox.AutoSize = true;
            this.checkBox.Location = new System.Drawing.Point(981, 3);
            this.checkBox.Name = "checkBox";
            this.checkBox.Size = new System.Drawing.Size(72, 16);
            this.checkBox.TabIndex = 12;
            this.checkBox.Text = "isActive";
            this.checkBox.UseVisualStyleBackColor = true;
            // 
            // comboBox
            // 
            this.comboBox.FormattingEnabled = true;
            this.comboBox.Location = new System.Drawing.Point(1057, 3);
            this.comboBox.Name = "comboBox";
            this.comboBox.Size = new System.Drawing.Size(113, 20);
            this.comboBox.TabIndex = 13;
            this.comboBox.SelectedIndexChanged += new System.EventHandler(this.comboBox_SelectedIndexChanged);
            // 
            // comboBox2
            // 
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new System.Drawing.Point(1176, 2);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(73, 20);
            this.comboBox2.TabIndex = 14;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(-3, 26);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(83, 12);
            this.label3.TabIndex = 8;
            this.label3.Text = "timerElapse：";
            // 
            // textBox5
            // 
            this.textBox5.Location = new System.Drawing.Point(74, 24);
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new System.Drawing.Size(41, 21);
            this.textBox5.TabIndex = 15;
            this.textBox5.Text = "100";
            // 
            // accountInfoTxt
            // 
            this.accountInfoTxt.Location = new System.Drawing.Point(124, 24);
            this.accountInfoTxt.Name = "accountInfoTxt";
            this.accountInfoTxt.Size = new System.Drawing.Size(132, 23);
            this.accountInfoTxt.TabIndex = 16;
            this.accountInfoTxt.Text = "信息";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(812, 29);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(72, 16);
            this.checkBox1.TabIndex = 12;
            this.checkBox1.Text = "autoPass";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(1019, 25);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(34, 21);
            this.textBox3.TabIndex = 9;
            this.textBox3.Text = "500";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(940, 29);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(77, 12);
            this.label5.TabIndex = 10;
            this.label5.Text = "fetchCount：";
            // 
            // textBox4
            // 
            this.textBox4.Location = new System.Drawing.Point(1108, 25);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(69, 21);
            this.textBox4.TabIndex = 9;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(1059, 30);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(47, 12);
            this.label6.TabIndex = 10;
            this.label6.Text = "point：";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(1178, 25);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(71, 23);
            this.button1.TabIndex = 17;
            this.button1.Text = "模拟登陆";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // checkBoxGuard
            // 
            this.checkBoxGuard.AutoSize = true;
            this.checkBoxGuard.Location = new System.Drawing.Point(738, 28);
            this.checkBoxGuard.Name = "checkBoxGuard";
            this.checkBoxGuard.Size = new System.Drawing.Size(72, 16);
            this.checkBoxGuard.TabIndex = 18;
            this.checkBoxGuard.Text = "守护进程";
            this.checkBoxGuard.UseVisualStyleBackColor = true;
            this.checkBoxGuard.CheckedChanged += new System.EventHandler(this.checkBoxGuard_CheckedChanged);
            // 
            // guardTimer
            // 
            this.guardTimer.Interval = 9000;
            this.guardTimer.Tick += new System.EventHandler(this.guardTimer_Tick);
            // 
            // ipProxyTxt
            // 
            this.ipProxyTxt.Location = new System.Drawing.Point(499, 26);
            this.ipProxyTxt.Name = "ipProxyTxt";
            this.ipProxyTxt.Size = new System.Drawing.Size(56, 21);
            this.ipProxyTxt.TabIndex = 19;
            this.ipProxyTxt.TextChanged += new System.EventHandler(this.ipProxyTxt_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(452, 30);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(47, 12);
            this.label4.TabIndex = 8;
            this.label4.Text = "IPuid：";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(1160, 631);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(89, 23);
            this.button2.TabIndex = 20;
            this.button2.Text = "频繁账号测试";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(687, 22);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(46, 23);
            this.button3.TabIndex = 21;
            this.button3.Text = "测试";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(885, 25);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(57, 23);
            this.button4.TabIndex = 22;
            this.button4.Text = "过验证";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // autoChangeAccountCHK
            // 
            this.autoChangeAccountCHK.AutoSize = true;
            this.autoChangeAccountCHK.Location = new System.Drawing.Point(370, 5);
            this.autoChangeAccountCHK.Name = "autoChangeAccountCHK";
            this.autoChangeAccountCHK.Size = new System.Drawing.Size(48, 16);
            this.autoChangeAccountCHK.TabIndex = 23;
            this.autoChangeAccountCHK.Text = "AUTO";
            this.autoChangeAccountCHK.UseVisualStyleBackColor = true;
            this.autoChangeAccountCHK.CheckedChanged += new System.EventHandler(this.autoChangeAccountCHK_CheckedChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(561, 30);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(47, 12);
            this.label7.TabIndex = 24;
            this.label7.Text = "IPpwd：";
            // 
            // ipProxyTxt2
            // 
            this.ipProxyTxt2.Location = new System.Drawing.Point(614, 25);
            this.ipProxyTxt2.Name = "ipProxyTxt2";
            this.ipProxyTxt2.Size = new System.Drawing.Size(67, 21);
            this.ipProxyTxt2.TabIndex = 25;
            // 
            // UseProxyCHK
            // 
            this.UseProxyCHK.AutoSize = true;
            this.UseProxyCHK.Location = new System.Drawing.Point(369, 29);
            this.UseProxyCHK.Name = "UseProxyCHK";
            this.UseProxyCHK.Size = new System.Drawing.Size(96, 16);
            this.UseProxyCHK.TabIndex = 26;
            this.UseProxyCHK.Text = "BrowserProxy";
            this.UseProxyCHK.UseVisualStyleBackColor = true;
            // 
            // UserProxyChk
            // 
            this.UserProxyChk.AutoSize = true;
            this.UserProxyChk.Checked = true;
            this.UserProxyChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UserProxyChk.Location = new System.Drawing.Point(885, 3);
            this.UserProxyChk.Name = "UserProxyChk";
            this.UserProxyChk.Size = new System.Drawing.Size(90, 16);
            this.UserProxyChk.TabIndex = 27;
            this.UserProxyChk.Text = "GlobalProxy";
            this.UserProxyChk.UseVisualStyleBackColor = true;
            this.UserProxyChk.CheckedChanged += new System.EventHandler(this.UserProxyChk_CheckedChanged);
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(1063, 631);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(96, 23);
            this.button5.TabIndex = 28;
            this.button5.Text = "QCC账号注册";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // AccountPhoneCodeSenderTimer
            // 
            this.AccountPhoneCodeSenderTimer.Interval = 4000;
            this.AccountPhoneCodeSenderTimer.Tick += new System.EventHandler(this.AccountPhoneCodeSenerTimer_Tick);
            // 
            // AccountRegTimer
            // 
            this.AccountRegTimer.Interval = 9000;
            this.AccountRegTimer.Tick += new System.EventHandler(this.AccountRegTimer_Tick_1);
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(985, 631);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(75, 23);
            this.button6.TabIndex = 29;
            this.button6.Text = "ip切换";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // button7
            // 
            this.button7.Location = new System.Drawing.Point(823, 631);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(75, 23);
            this.button7.TabIndex = 30;
            this.button7.Text = "保存队列";
            this.button7.UseVisualStyleBackColor = true;
            this.button7.Click += new System.EventHandler(this.button7_Click);
            // 
            // button8
            // 
            this.button8.Location = new System.Drawing.Point(742, 631);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(75, 23);
            this.button8.TabIndex = 31;
            this.button8.Text = "加载队列";
            this.button8.UseVisualStyleBackColor = true;
            this.button8.Click += new System.EventHandler(this.button8_Click);
            // 
            // ipChangeTimer
            // 
            this.ipChangeTimer.Interval = 6000;
            this.ipChangeTimer.Tick += new System.EventHandler(this.ipChangeTimer_Tick);
            // 
            // button9
            // 
            this.button9.Location = new System.Drawing.Point(904, 631);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(75, 23);
            this.button9.TabIndex = 32;
            this.button9.Text = "ip自动切换";
            this.button9.UseVisualStyleBackColor = true;
            this.button9.Click += new System.EventHandler(this.button9_Click);
            // 
            // button10
            // 
            this.button10.Location = new System.Drawing.Point(627, 631);
            this.button10.Name = "button10";
            this.button10.Size = new System.Drawing.Size(109, 23);
            this.button10.TabIndex = 33;
            this.button10.Text = "设置accessToken";
            this.button10.UseVisualStyleBackColor = true;
            this.button10.Click += new System.EventHandler(this.button10_Click);
            // 
            // button11
            // 
            this.button11.Location = new System.Drawing.Point(1121, 592);
            this.button11.Name = "button11";
            this.button11.Size = new System.Drawing.Size(91, 23);
            this.button11.TabIndex = 34;
            this.button11.Text = "LoadCity";
            this.button11.UseVisualStyleBackColor = true;
            this.button11.Click += new System.EventHandler(this.button11_Click);
            // 
            // EnterpriseKeySuffixTxt
            // 
            this.EnterpriseKeySuffixTxt.Location = new System.Drawing.Point(263, 26);
            this.EnterpriseKeySuffixTxt.Name = "EnterpriseKeySuffixTxt";
            this.EnterpriseKeySuffixTxt.Size = new System.Drawing.Size(100, 21);
            this.EnterpriseKeySuffixTxt.TabIndex = 35;
            this.EnterpriseKeySuffixTxt.Text = "test";
            // 
            // singalKeyWordCHK
            // 
            this.singalKeyWordCHK.AutoSize = true;
            this.singalKeyWordCHK.Checked = true;
            this.singalKeyWordCHK.CheckState = System.Windows.Forms.CheckState.Checked;
            this.singalKeyWordCHK.Location = new System.Drawing.Point(1177, 52);
            this.singalKeyWordCHK.Name = "singalKeyWordCHK";
            this.singalKeyWordCHK.Size = new System.Drawing.Size(72, 16);
            this.singalKeyWordCHK.TabIndex = 36;
            this.singalKeyWordCHK.Text = "单关键字";
            this.singalKeyWordCHK.UseVisualStyleBackColor = true;
            // 
            // button12
            // 
            this.button12.Location = new System.Drawing.Point(1097, 48);
            this.button12.Name = "button12";
            this.button12.Size = new System.Drawing.Size(75, 23);
            this.button12.TabIndex = 37;
            this.button12.Text = "下一个关键字";
            this.button12.UseVisualStyleBackColor = true;
            this.button12.Click += new System.EventHandler(this.button12_Click);
            // 
            // splitLimitChk
            // 
            this.splitLimitChk.AutoSize = true;
            this.splitLimitChk.Location = new System.Drawing.Point(885, 52);
            this.splitLimitChk.Name = "splitLimitChk";
            this.splitLimitChk.Size = new System.Drawing.Size(102, 16);
            this.splitLimitChk.TabIndex = 38;
            this.splitLimitChk.Text = "限制split次数";
            this.splitLimitChk.UseVisualStyleBackColor = true;
            // 
            // autoPassKeyWordChk
            // 
            this.autoPassKeyWordChk.AutoSize = true;
            this.autoPassKeyWordChk.Checked = true;
            this.autoPassKeyWordChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoPassKeyWordChk.Location = new System.Drawing.Point(985, 49);
            this.autoPassKeyWordChk.Name = "autoPassKeyWordChk";
            this.autoPassKeyWordChk.Size = new System.Drawing.Size(108, 16);
            this.autoPassKeyWordChk.TabIndex = 39;
            this.autoPassKeyWordChk.Text = "自动跳过关键字";
            this.autoPassKeyWordChk.UseVisualStyleBackColor = true;
            // 
            // PassKeyWordtimer
            // 
            this.PassKeyWordtimer.Enabled = true;
            this.PassKeyWordtimer.Interval = 10000;
            this.PassKeyWordtimer.Tick += new System.EventHandler(this.PassKeyWordtimer_Tick);
            // 
            // AutoChangeIp
            // 
            this.AutoChangeIp.AutoSize = true;
            this.AutoChangeIp.Checked = true;
            this.AutoChangeIp.CheckState = System.Windows.Forms.CheckState.Checked;
            this.AutoChangeIp.Location = new System.Drawing.Point(651, 50);
            this.AutoChangeIp.Name = "AutoChangeIp";
            this.AutoChangeIp.Size = new System.Drawing.Size(96, 16);
            this.AutoChangeIp.TabIndex = 12;
            this.AutoChangeIp.Text = "autoChangeIp";
            this.AutoChangeIp.UseVisualStyleBackColor = true;
            // 
            // button13
            // 
            this.button13.Location = new System.Drawing.Point(454, 631);
            this.button13.Name = "button13";
            this.button13.Size = new System.Drawing.Size(75, 23);
            this.button13.TabIndex = 40;
            this.button13.Text = "添加异常iP";
            this.button13.UseVisualStyleBackColor = true;
            this.button13.Click += new System.EventHandler(this.button13_Click);
            // 
            // KeyWordFilterTextBox
            // 
            this.KeyWordFilterTextBox.Location = new System.Drawing.Point(558, 47);
            this.KeyWordFilterTextBox.Name = "KeyWordFilterTextBox";
            this.KeyWordFilterTextBox.Size = new System.Drawing.Size(87, 21);
            this.KeyWordFilterTextBox.TabIndex = 41;
            // 
            // KeyWordFilterlabel
            // 
            this.KeyWordFilterlabel.AutoSize = true;
            this.KeyWordFilterlabel.Location = new System.Drawing.Point(452, 56);
            this.KeyWordFilterlabel.Name = "KeyWordFilterlabel";
            this.KeyWordFilterlabel.Size = new System.Drawing.Size(101, 12);
            this.KeyWordFilterlabel.TabIndex = 8;
            this.KeyWordFilterlabel.Text = "开始关键字个数：";
            // 
            // updateDateTxt
            // 
            this.updateDateTxt.Location = new System.Drawing.Point(74, 48);
            this.updateDateTxt.Name = "updateDateTxt";
            this.updateDateTxt.Size = new System.Drawing.Size(60, 21);
            this.updateDateTxt.TabIndex = 42;
            this.updateDateTxt.Text = "20171222";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(3, 51);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(65, 12);
            this.label8.TabIndex = 43;
            this.label8.Text = "更新日期：";
            // 
            // proxyListCB
            // 
            this.proxyListCB.FormattingEnabled = true;
            this.proxyListCB.Location = new System.Drawing.Point(187, 49);
            this.proxyListCB.Name = "proxyListCB";
            this.proxyListCB.Size = new System.Drawing.Size(126, 20);
            this.proxyListCB.TabIndex = 44;
            this.proxyListCB.SelectedIndexChanged += new System.EventHandler(this.proxyListCB_SelectedIndexChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(140, 54);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(41, 12);
            this.label9.TabIndex = 45;
            this.label9.Text = "代理：";
            // 
            // keyWordSourceCHK
            // 
            this.keyWordSourceCHK.AutoSize = true;
            this.keyWordSourceCHK.Checked = true;
            this.keyWordSourceCHK.CheckState = System.Windows.Forms.CheckState.Checked;
            this.keyWordSourceCHK.Location = new System.Drawing.Point(330, 53);
            this.keyWordSourceCHK.Name = "keyWordSourceCHK";
            this.keyWordSourceCHK.Size = new System.Drawing.Size(120, 16);
            this.keyWordSourceCHK.TabIndex = 47;
            this.keyWordSourceCHK.Text = "是否使用企业分类";
            this.keyWordSourceCHK.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(753, 52);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(65, 12);
            this.label10.TabIndex = 8;
            this.label10.Text = "个数限制：";
            // 
            // MaxAccountCrawlerCountTxt
            // 
            this.MaxAccountCrawlerCountTxt.Location = new System.Drawing.Point(823, 47);
            this.MaxAccountCrawlerCountTxt.Name = "MaxAccountCrawlerCountTxt";
            this.MaxAccountCrawlerCountTxt.Size = new System.Drawing.Size(61, 21);
            this.MaxAccountCrawlerCountTxt.TabIndex = 48;
            this.MaxAccountCrawlerCountTxt.Text = "150";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1258, 742);
            this.Controls.Add(this.MaxAccountCrawlerCountTxt);
            this.Controls.Add(this.keyWordSourceCHK);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.proxyListCB);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.updateDateTxt);
            this.Controls.Add(this.KeyWordFilterTextBox);
            this.Controls.Add(this.button13);
            this.Controls.Add(this.autoPassKeyWordChk);
            this.Controls.Add(this.splitLimitChk);
            this.Controls.Add(this.button12);
            this.Controls.Add(this.singalKeyWordCHK);
            this.Controls.Add(this.EnterpriseKeySuffixTxt);
            this.Controls.Add(this.button11);
            this.Controls.Add(this.button10);
            this.Controls.Add(this.button9);
            this.Controls.Add(this.button8);
            this.Controls.Add(this.button7);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.UserProxyChk);
            this.Controls.Add(this.UseProxyCHK);
            this.Controls.Add(this.ipProxyTxt2);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.autoChangeAccountCHK);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.ipProxyTxt);
            this.Controls.Add(this.checkBoxGuard);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.accountInfoTxt);
            this.Controls.Add(this.textBox5);
            this.Controls.Add(this.comboBox2);
            this.Controls.Add(this.comboBox);
            this.Controls.Add(this.AutoChangeIp);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.checkBox);
            this.Controls.Add(this.startCrawlerBtn);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox4);
            this.Controls.Add(this.textBox3);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.KeyWordFilterlabel);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.delBtn);
            this.Controls.Add(this.setBusyBtn);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.searchBtn);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.textBox);
            this.Controls.Add(this.richTextBox);
            this.Controls.Add(this.richTextBoxInfo);
            this.Controls.Add(this.webBrowser);
            this.Name = "Form1";
            this.Text = "企查查";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.WebBrowser webBrowser;
        private System.Windows.Forms.RichTextBox richTextBoxInfo;
        private System.Windows.Forms.RichTextBox richTextBox;
        private System.Windows.Forms.TextBox textBox;
        private System.Windows.Forms.Button searchBtn;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Button setBusyBtn;
        private System.Windows.Forms.Button delBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Button startCrawlerBtn;
        private System.Windows.Forms.CheckBox checkBox;
        private System.Windows.Forms.ComboBox comboBox;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox5;
        private System.Windows.Forms.Label accountInfoTxt;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox checkBoxGuard;
        private System.Windows.Forms.Timer guardTimer;
        private System.Windows.Forms.TextBox ipProxyTxt;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.CheckBox autoChangeAccountCHK;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox ipProxyTxt2;
        private System.Windows.Forms.CheckBox UseProxyCHK;
        private System.Windows.Forms.CheckBox UserProxyChk;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Timer AccountPhoneCodeSenderTimer;
        private System.Windows.Forms.Timer AccountRegTimer;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.Timer ipChangeTimer;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.Button button10;
        private System.Windows.Forms.Button button11;
        private System.Windows.Forms.TextBox EnterpriseKeySuffixTxt;
        private System.Windows.Forms.CheckBox singalKeyWordCHK;
        private System.Windows.Forms.Button button12;
        private System.Windows.Forms.CheckBox splitLimitChk;
        private System.Windows.Forms.CheckBox autoPassKeyWordChk;
        private System.Windows.Forms.Timer PassKeyWordtimer;
        private System.Windows.Forms.CheckBox AutoChangeIp;
        private System.Windows.Forms.Button button13;
        private System.Windows.Forms.TextBox KeyWordFilterTextBox;
        private System.Windows.Forms.Label KeyWordFilterlabel;
        private System.Windows.Forms.TextBox updateDateTxt;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox proxyListCB;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckBox keyWordSourceCHK;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox MaxAccountCrawlerCountTxt;
    }
}

