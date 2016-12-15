using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QCCWebBrowser
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }
        private Form1 mainForm;
        public SettingsForm(Form1 _mainForm)
        {
             mainForm = _mainForm;
          
            InitializeComponent();
        }
        private void SettingsForm_Load(object sender, EventArgs e)
        {
            var settings = Form1.curCrawlSettings;
            deviceTxt.Text = settings.DeviceId;
            timestampTxt.Text = settings.timestamp;
            signTxt.Text = settings.sign;
            refleshTokenTxt.Text = settings.RefleshToken;
            accessTokenTxt.Text = settings.AccessToken;
            comboBox1.Items.Clear();
            //GetAccessToken();
            foreach (var account in mainForm.GetAppDeviceAccount)
            {
                var index=this.comboBox1.Items.Add(account.Text("deviceId"));
                if (account.Text("deviceId") == settings.DeviceId)
                {
                    this.comboBox1.SelectedIndex = index;
                }
            }
        }
            
        private void button1_Click(object sender, EventArgs e)
        {
            mainForm.SetSetting(deviceTxt.Text, timestampTxt.Text, signTxt.Text, refleshTokenTxt.Text, accessTokenTxt.Text);
            this.Close();

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var comboBox1 = sender as ComboBox;
            if (comboBox1 != null)
            {
                var hitDevice = mainForm.GetAppDeviceAccount.Where(c => c.Text("deviceId") == comboBox1.Text).FirstOrDefault();
                if (hitDevice != null)
                {
                    deviceTxt.Text = hitDevice.Text("deviceId");
                    accessTokenTxt.Text = hitDevice.Text("accessToken");
                    refleshTokenTxt.Text = hitDevice.Text("refreshToken");
                    timestampTxt.Text = hitDevice.Text("timestamp");
                    signTxt.Text = hitDevice.Text("sign");
                    isBusyTxt.Text = hitDevice.Text("isBusy");
                    statusTxt.Text = hitDevice.Text("status");
                    getCountTxt.Text = hitDevice.Text("EnterpriseGuidByKeyWordApp");
                   
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("采集过程中请勿进行该操作，改功能用于异常卡死重载账号是否继续" ,"请注意",MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                mainForm.ReloadLoginAccount();
            }
          
    }
    }
}
