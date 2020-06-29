using SimpleCrawler;
using SimpleCrawler.Demo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QCCWebBrowser
{
    public partial class UserRegisterForm : Form
    {
        //public UserRegisterForm()
        //{
        //    InitializeComponent();
        //}
        /// <summary>
        /// 用户名
        /// </summary>
        public string PhoneNum { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string PassWord { get; set; }
        /// <summary>
        /// 验证码
        /// </summary>
        public string ValidCode { get; set; }

        private Form1 mainForm;

        public UserRegisterForm(Form1 _mainForm)
        {
            this.DialogResult = DialogResult.None;
            mainForm = _mainForm;
            mainForm.regFormIsClose = false;
            
            InitializeComponent();
        }
        public bool AutoLogout()
        {
            HttpResult result = new HttpResult();
            try
            {
                var item = new HttpItem()
                {
                    URL = "http://" + ConstParam.wwwurl + "/user_logout",
                    Method = "get",//URL     可选项 默认为Get   
                    ContentType = "text/html",//返回类型    可选项有默认值 
                    //Timeout = Settings.Timeout,
                    Cookie = curCookie
                };


                SimpleCrawler.HttpHelper http = new SimpleCrawler.HttpHelper();
                result = http.GetHtml(item);

            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (WebException ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (TimeoutException ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

            }


            if (result.StatusCode == HttpStatusCode.OK)
            {
               // ShowMessageInfo("退出成功");
                // this.webBrowser.Navigate(addCredsToUri(this.textBox.Text));
                return true;
            }
            else
            {
               // ShowMessageInfo("退出失败");
                return false;
            }
        }
        private void UserRegisterForm_Load(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.None;
            //InitialValue();
            this.timer1.Enabled = true;
            this.timer1.Start();
        }
        HtmlElement loginname =null ;
        HtmlElement loginPW =null ;
        HtmlElement mobilecode= null;
        HtmlDocument documentText;
        public void HtmlClick(string id)
        {
            if (documentText == null)
            {
                documentText = webBrowser.Document;
            }
            if (documentText != null)
            {
                HtmlElement GetPhoneBtn = documentText.All[id];
                if (GetPhoneBtn != null)
                {
                    GetPhoneBtn.InvokeMember("click"); ;
                }
            }
            else
            {
                //ShowMessageInfo("当前页面未展示");
            }
        }
        bool hasAllEdit = false;
        private void InitialValue()
        {
           
            // timerStart();
            //获取cookie

            //填写表单
            try
            {
                
                    documentText = webBrowser.Document;
                    loginname = documentText.All["phone"];
                    loginPW = documentText.All["pswd"];
                    mobilecode = documentText.All["mobilecode"];
                   // InitialValue();
            
               if (loginname != null && !string.IsNullOrEmpty(PhoneNum))
                    loginname.SetAttribute("value", PhoneNum); else   hasAllEdit = false;
               
                if (loginPW != null && !string.IsNullOrEmpty(PassWord))
                    loginPW.SetAttribute("value", PassWord);
                else hasAllEdit = false;
                if (mobilecode != null && !string.IsNullOrEmpty(ValidCode))
                    mobilecode.SetAttribute("value", ValidCode);
                

               if (hasAllEdit) {
                     this.timer1.Stop();
                     this.timer1.Enabled = false;
                    //    //模拟保存按钮
                    //    //HtmlClick();
                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
               // this.timer1.Stop();
               // this.timer1.Enabled = false;
            }

        }
        public string curCookie;
        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            curCookie = FullWebBrowserCookie.GetCookieInternal(e.Url, false);
            //InitialValue();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            InitialValue();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (mainForm != null)
            {
                if(!string.IsNullOrEmpty(PhoneNum)&& !string.IsNullOrEmpty(PassWord)&& !string.IsNullOrEmpty(ValidCode)) { 
                  //  mainForm.AccountRegToDB(PhoneNum, PassWord);
                    PhoneNum = string.Empty;
                    PassWord = string.Empty;
                    ValidCode = string.Empty;
                    //MessageBox.Show("账号保存成功");
                    AutoLogout();
                }
                mainForm.regFormIsClose = true;
               // mainForm.AutoLogout();
             
              
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            mainForm.regFormIsClose = true;
            this.DialogResult = DialogResult.None;
            //this.Hide();
            this.Close();
        }

        private void UserRegisterForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            mainForm.regFormIsClose = true;
            this.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            InitialValue();
        }

        private void UserRegisterForm_Activated(object sender, EventArgs e)
        {
            InitialValue();
        }
    }
}
