using Helper;
using Helper.Tree;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
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
        StringBuilder treeSb = new StringBuilder();
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
            this.enterprisePortTxt.Text = Form1.enterprisePort.ToString();
            this.globalDBNameTxt.Text = Form1.globalDBName;
            this.isProvinceCHK.Checked = Form1.IsProvince;
            this.onlyDateUpdateCHK.Checked = Form1.OnlyDateUpdate;
            this.industryCHK.Checked = Form1.IndustrySearch;
            this.GRegistCapiBeginTxt.Text = Form1.GRegistCapiBegin;
            this.GRegistCapiEndTxt.Text = Form1.GRegistCapiEnd;
            this.industryCodeTXT.Text = Form1.SearchIndustryCodeLimit;
            this.subIndustryCodeTXT.Text = Form1.SearchSubIndustryCodeLimit;
            this.SearchTakeCountLimitTXT.Text = Form1.SearchTakeCountLimitPerUrl.ToString();
            this.SearchCustomCategoryNameTXT.Text = Form1.SearchCustomCategoryName;
            this.keyWordMaxCountTxt.Text = Form1.SearchKeyWordTakeCountLimit.ToString();
            this.globalDBHasPassWordChk.Checked = Form1.globalDBHasPassWord;
            
         
            this.deviceTableTxt.Text = Form1.qccDeviceAccountName;
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
            richTextBox2.Text = string.Empty;
            foreach (BsonDocument account in this.mainForm.invalidAccountList.OrderBy(c=>c.Text("account")))
            {

                richTextBox2.Text += $"{account.Text("account")} {account.Text("password") }\n";
            }



        }
        private void InitialTree(SimpleTreeNode<BsonDocument> node,TreeNode curNode)
        {
            try
            {
                var nodeName= $"{node.Data.Text("name")}({node.Data.Text("parentRecordCount")})"; 
                var parent = new TreeNode(nodeName);
                if (curNode == null)
                {
                    this.treeView1.Nodes.Add(parent);
                }
                else
                {
                    curNode.Nodes.Add(parent);

                }

                var preFix = "|";
                for (var index = 0; index <= node.Level; index++)
                {
                    preFix += "--";
                }

                treeSb.AppendLine(preFix + nodeName);

                var childrenList = node.Children;
                if (childrenList == null || childrenList.Count <= 0) return;
                foreach (var child in childrenList)
                {
                    TreeNode childTreeNode = new TreeNode(child.Data.Text("name"));
                    //parent.Nodes.Add(childTreeNode);
                    childrenList = child.Children;
                    InitialTree(child, parent);//获取子
                }
            }
            catch (Exception ex)
            {
                mainForm.ShowMessageInfo(ex.Message);
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
            if (!string.IsNullOrEmpty(this.enterprisePortTxt.Text))
            {
                if (int.TryParse(this.enterprisePortTxt.Text.Trim(), out int port))
                {
                    Form1.enterprisePort = port;
                }
            }
            Form1.IsProvince = this.isProvinceCHK.Checked;
            Form1.OnlyDateUpdate = this.onlyDateUpdateCHK.Checked;
            Form1.GRegistCapiBegin = this.GRegistCapiBeginTxt.Text;
            Form1.GRegistCapiEnd = this.GRegistCapiEndTxt.Text;
            Form1.IsMoreDetailInfo = this.moreDetailInfoCHK.Checked;
            Form1.IndustrySearch = this.industryCHK.Checked;
            Form1.globalDBHasPassWord= this.globalDBHasPassWordChk.Checked;
            Form1.SearchIndustryCodeLimit = this.industryCodeTXT.Text.Trim();
            Form1.SearchSubIndustryCodeLimit = this.subIndustryCodeTXT.Text.Trim();
            Form1.SearchTakeCountLimitPerUrl=int.Parse(this.SearchTakeCountLimitTXT.Text);
            Form1.SearchCustomCategoryName= this.SearchCustomCategoryNameTXT.Text;
            Form1.SearchKeyWordTakeCountLimit= int.Parse(this.keyWordMaxCountTxt.Text);
            Form1.globalDBName = globalDBNameTxt.Text;
            Form1.qccDeviceAccountName = this.deviceTableTxt.Text;
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

        private void button4_Click(object sender, EventArgs e)
        {
            mainForm.ReloadCaculateAccessToken();
            this.Close();
            
           
           
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex ==2 )
            {
                this.treeView1.Nodes.Clear();
                treeSb.Clear();
                InitialTree(mainForm.globalUrlSplitTreeNode, null);
                this.richTextBox1.Text = treeSb.ToString();
            }
            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            this.globalDBHasPassWordChk.Checked=!this.globalDBHasPassWordChk.Checked;
            if (this.globalDBHasPassWordChk.Checked)
            {
                this.enterprisePortTxt.Text = "37088";
                this.globalDBNameTxt.Text = "SimpleCrawler";

            }
            else
            {
                this.enterprisePortTxt.Text = "37888";
                this.globalDBNameTxt.Text = "LandFang";
            }
            
            
           
          
            
        }

        private void button6_Click(object sender, EventArgs e)
        {
            var lfOp = MongoOpCollection.GetNew121MongoOp_MT("LandFang");
            var hitResult = lfOp.FindAll("QCCEnterpriseKey_OtherEnterprise_Land_Relation", Query.EQ("isHouse", 1)).ToList();
            var sb = hitResult.Select(c => c.Text("name")).Distinct().ToList();
            var content = string.Join("\n", sb);
            this.keyWordRTxt.Text = content;
        }
    }
}
