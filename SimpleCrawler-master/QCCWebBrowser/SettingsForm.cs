using MongoDB.Bson;
using SimpleCrawler;
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
            CrawlSettings settings = Form1.curCrawlSettings;
            this.deviceTxt.Text = settings.DeviceId;
            this.timestampTxt.Text = settings.timestamp;
            this.signTxt.Text = settings.sign;
            this.refleshTokenTxt.Text = settings.RefleshToken;
            this.accessTokenTxt.Text = settings.AccessToken;
            this.enterpriseIpTxt.Text = Form1.enterpriseIp;
            this.isProvinceCHK.Checked = Form1.IsProvince;
            this.onlyDateUpdateCHK.Checked = Form1.OnlyDateUpdate;
            this.industryCHK.Checked = Form1.IndustrySearch;
            this.GRegistCapiBeginTxt.Text = Form1.GRegistCapiBegin;
            this.GRegistCapiEndTxt.Text = Form1.GRegistCapiEnd;
            this.comboBox1.Items.Clear();
            foreach (BsonDocument account in this.mainForm.GetAppDeviceAccount)
            {
                int index = this.comboBox1.Items.Add(account.Text("deviceId"));
                if (account.Text("deviceId") == settings.DeviceId)
                {
                    this.comboBox1.SelectedIndex = index;
                }
            }
            if (!string.IsNullOrEmpty(Form1.SearchKeyType))
            {
                this.searchKeyTypeComBox.SelectedText = Form1.SearchKeyType;
            }
            if (Form1.PreKeyWordList.Count()>0)
            {
                this.keyWordRTxt.Text = string.Join( "\n", Form1.PreKeyWordList);
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
                BsonDocument hitDevice = (from c in this.mainForm.GetAppDeviceAccount
                                          where c.Text("deviceId") == comboBox1.Text
                                          select c).FirstOrDefault<BsonDocument>();
                if (hitDevice != null)
                {
                    this.deviceTxt.Text = hitDevice.Text("deviceId");
                    this.accessTokenTxt.Text = hitDevice.Text("accessToken");
                    this.refleshTokenTxt.Text = hitDevice.Text("refreshToken");
                    this.timestampTxt.Text = hitDevice.Text("timestamp");
                    this.signTxt.Text = hitDevice.Text("sign");
                    this.isBusyTxt.Text = hitDevice.Text("isBusy");
                    this.statusTxt.Text = hitDevice.Text("status");
                    this.getCountTxt.Text = hitDevice.Text("EnterpriseGuidByKeyWord_APP");
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

        private void button3_Click(object sender, EventArgs e)
        {
            Form1.enterpriseIp = this.enterpriseIpTxt.Text;
            Form1.IsProvince = this.isProvinceCHK.Checked;
            Form1.OnlyDateUpdate = this.onlyDateUpdateCHK.Checked;
            Form1.GRegistCapiBegin = this.GRegistCapiBeginTxt.Text;
            Form1.GRegistCapiEnd = this.GRegistCapiEndTxt.Text;
            Form1.IsMoreDetailInfo = this.moreDetailInfoCHK.Checked;
            Form1.IndustrySearch = this.industryCHK.Checked;
            var keyWordStr = this.keyWordRTxt.Text;
            if (!string.IsNullOrEmpty(keyWordStr))
            {
                var keyWordList = keyWordStr.Split(new string[] { "\r", "\n", ",", "\t", "，", "、" },StringSplitOptions.RemoveEmptyEntries);
                if (keyWordList.Count() > 0)
                {
                    Form1.PreKeyWordList = keyWordList.Distinct().ToList();
                }
            }
            this.Close();
        }

        private void searchKeyTypeComBox_SelectedIndexChanged(object sender, EventArgs e)
        {
          
            ComboBox comboBox1 = sender as ComboBox;
            if (comboBox1 != null)
            {
                Form1.SearchKeyType = comboBox1.Text;
            }
       
    }
    }
}
