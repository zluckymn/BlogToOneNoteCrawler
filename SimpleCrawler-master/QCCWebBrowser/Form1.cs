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
using System.Timers;
using System.Threading;

using System.Web;
using Yinhe.ProcessingCenter;
using SimpleCrawler.Demo;
using LibCurlNet;
using System.Net;
using HtmlAgilityPack;
using Yinhe.ProcessingCenter.DataRule;
using Newtonsoft.Json.Linq;
using mshtml;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using System.Collections.Concurrent;
using Helpers;

namespace QCCWebBrowser
{
    public partial class Form1 : Form
    {
        #region 变量
        List<BsonDocument> cityList = new List<BsonDocument>();
        private BloomFilter<string> urlFilter = new BloomFilter<string>(9900000);
        
        private bool USEWEBPROXY = true;
        string proxyHost = "http://proxy.abuyun.com";
        string proxyPort = "9010";
        // 代理隧道验证信息
        //string proxyUser = "H1880S335RB41F8P";
        //string proxyPass = "ECB2CD5B9D783F4E";
        //string proxyUser = "H1538UM3D6R2133P";//"H1880S335RB41F8P";////HVW8J9B1F7K4W83P
        //string proxyPass = "511AF06ABED1E7AE";//"ECB2CD5B9D783F4E";////C835A336CD070F9D
        string proxyUser = "HE0X2CG05WM7953P";//"H1880S335RB41F8P";////HVW8J9B1F7K4W83P
        string proxyPass = "544B04D565F22D97";//"ECB2CD5B9D783F4E";////C835A336CD070F9D
        WebProxy getWebProxy = new WebProxy();
        private BloomFilter<string> existGuidList = new BloomFilter<string>(9000000);
        private BloomFilter<string> existNameList = new BloomFilter<string>(9000000);
        private const string siteUserNameElm = "nameNormal";
        private const string sitePwdElm = "pwdNormal";
        private static string ip = "59.61.72.34";
        private static string enterpriseIp = "59.61.72.34";//企业库所在ip跨库处理
        private static int port = 37088;//企业库所在ip跨库处理
        private static string connStr = string.Format("mongodb://MZsa:MZdba@{0}:{1}/SimpleCrawler", ip, port);
        private static string curQCCProvinceCode = string.Empty;//qichacha省代码
        private static string curQCCCityCode = string.Empty;//qichacha城市代码

        private static string enterpriseNameConnStr = "mongodb://MZsa:MZdba@192.168.1.114/CompanyHY";
        // private static string connStr = "mongodb://MZsa:MZdba@59.61.72.34:37088/Shared";
        static DataOperation dataop = new DataOperation(new MongoOperation(connStr));//主数据库
        static MongoOperation _mongoDBOp = new MongoOperation(connStr);

        private const bool NEEDRECORDURL = false;//爬取是否记录url
        private static string enterpriseConnStr = string.Format("mongodb://MZsa:MZdba@{0}:{1}/SimpleCrawler", enterpriseIp, port);
        static DataOperation enterpriseDataop = new DataOperation(new MongoOperation(enterpriseConnStr));//主数据库
        static MongoOperation _enterpriseMongoDBOp = new MongoOperation(enterpriseConnStr);
        private static CrawlSettings Settings = new CrawlSettings();
        Dictionary<string, string> cityNameDic = new Dictionary<string, string>();
        private static bool canNextUrl = true;
        Uri curUri = null;
        string siteIndexUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
        string validUrl = "http://www.qichacha.com/company/network/e6c8b0b6-a2b7-4ab3-8403-3ec6215d683b?name=%E6%B5%99%E6%B1%9F%E6%B7%98%E5%AE%9D%E5%A4%A7%E5%AD%A6%E6%9C%89%E9%99%90%E5%85%AC%E5%8F%B8";
        string curTimerElapse = string.Empty;
        List<BsonDocument> allAccountList = new List<BsonDocument>();//账号列表
        List<BsonDocument> allDeviceAccountList = new List<BsonDocument>();//设备账号列表
        List<BsonDocument> allAccountHashMapList = new List<BsonDocument>();//账号APp加密列表

        Dictionary<string, string> EnterpriseInfoMapDic = new Dictionary<string, string>();
        Dictionary<string, string> EnterpriseInfoMapDic_App = new Dictionary<string, string>();
        PassGeetestHelper geetestHelper = new PassGeetestHelper();
        public static bool hasReceiveData = true;//是否获取到数据
        public static int PassInValidTimes = 1;//过验证码失败次数
        public static int PassSuccessTimes = 2;//过验证码成功次数,保护账号 2代表1次
        public static int GetAccessRetryTimes = 4;//过验证码成功次数,保护账号 2代表1次
        public static DateTime LastGetPassGeetestTime = DateTime.MinValue;  //上次获取geetest的时间 
        public static int GeetestMaxSpanSecond = 10;  //上次获取geetest的时间 秒
        private string cityNameStr = "";
        private string tempTableName = "SQ_xian_Company";//企业黄页目录表
        //List<string> existGuidList = new List<string>();

        //string[] enterpriseInfoUrlType = new string[] { "base","touzi","susong", "finance", "run", "report", "assets" };
        string[] enterpriseInfoUrlType = new string[] { "base" };
        System.Timers.Timer aTimer = new System.Timers.Timer();
        System.Timers.Timer autoRestartTimer = new System.Timers.Timer();
        HttpInput hi = new HttpInput();
        SimpleCrawler.HttpHelper http = new SimpleCrawler.HttpHelper();
        bool waitBrowerMouseUpResponse = false;
        SearchType searchType = SearchType.UpdateEnterpriseInfo;
        AccountRegisterType accountRegisterType = AccountRegisterType.QiChaCha;
        UrlSpliteMode urlSplitMode = UrlSpliteMode.DateFirst;//默认
        private static object lockPassGeek = new object();
        private static object lockRefressToken = new object();
        private static object locakTimeGear = new object();
        private System.Windows.Forms.HtmlDocument documentText;
        static string curHtml = string.Empty;
        List<BsonDocument> allSubFactoryList = new List<BsonDocument>();//子类
        //线程安全的字典统计
        ConcurrentDictionary<string, decimal> keyWordCountDic = new ConcurrentDictionary<string, decimal>();
        public enum SearchType
        {
            /// <summary>
            /// 更新企业信息
            /// </summary>
            [EnumDescription("UpdateEnterpriseInfo")]
            UpdateEnterpriseInfo = 0,
            /// <summary>
            /// 搜索企业Guid
            /// </summary>
            [EnumDescription("EnterpriseGuid")]
            EnterpriseGuid = 1,
            /// <summary>
            /// 通过分类搜索企业Guid
            /// </summary>
            [EnumDescription("EnterpriseGuidByType")]
            EnterpriseGuidByType = 2,
            /// <summary>
            /// 通过城市分类搜索企业Guid
            /// </summary>
            [EnumDescription("EnterpriseGuidByCity")]
            EnterpriseGuidByCity = 3,
            /// <summary>
            /// 获取企业关键字
            /// </summary>
            [EnumDescription("EnterpriseGuidByKeyWordEnhence")]
            EnterpriseGuidByKeyWordEnhence = 4,
            /// <summary>
            /// 通过地区分类关键字搜索企业Guid
            /// </summary>
            [EnumDescription("EnterpriseGuidByKeyWord")]
            EnterpriseGuidByKeyWord = 5,
            [EnumDescription("EnterpriseGuidByKeyWord_APP")]
            EnterpriseGuidByKeyWord_APP = 6,
        }
        /// <summary>
        /// 账号注册类型
        /// </summary>
        public enum AccountRegisterType
        {
            [EnumDescription("QiChaCha")]
            QiChaCha = 0,
            [EnumDescription("LandFang")]
            LandFang = 1,
        }

        /// <summary>
        /// url分解模式，防止2016-02-01有 45条数据 而此时 的区间为2016-01-01 20161231 此处需要循环非常多次，而如果切换模式则进行只要一次
        /// </summary>
        public enum UrlSpliteMode
        {
            /// <summary>
            ///日期优先
            /// </summary>
            [EnumDescription("DateFirst")]
            DateFirst = 0,
            /// <summary>
            /// 注册资金优先
            /// </summary>
            [EnumDescription("RecpiFirst")]
            RecpiFirst = 1,
            /// <summary>
            /// 子分类优先爬去
            /// </summary>
            [EnumDescription("SubFacortyFist")]
            SubFacortyFist = 2,

        }
        //搜索关键字初始化
        public Dictionary<string, string> statusCode = new Dictionary<string, string>();
        public Dictionary<string, string> registCapi = new Dictionary<string, string>();
        public Dictionary<string, string> startdate = new Dictionary<string, string>();
        public List<SearchKeyCondition> searchKeyConditonList = new List<SearchKeyCondition>();
        /// <summary>
        /// 关键字爬取状态
        /// </summary>
        public class SearchKeyCondition
        {
            public string searchKey { get; set; }
            public UrlSpliteMode mode { get; set; }
            public int recordCount { get; set; }//记录个数
            public int hitCount { get; set; }//命中个数
        }
        public const string DATATABLENAME = "QCCEnterpriseKey";
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableName
        {
            get {

                if (!string.IsNullOrEmpty(EnterpriseKeySuffixTxt.Text)) {
                    return DATATABLENAME + "_" + EnterpriseKeySuffixTxt.Text;
                }
                else {
                    return DATATABLENAME;
                }

            }

        }

        /// <summary>
        /// 返回
        /// </summary>
        public static string DataTableNameURL
        {
            get { return DATATABLENAME + "URL"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public static string DataTableNameList
        {
            get { return "QCCEnterprise"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public static string DataTableAccount
        {
            get { return "QCCAccount"; }

        }
        public static string LandFangDataTableAccount
        {
            get { return "LandFangAccount"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public static string DataTableHolder
        {
            get { return "QCCEnterpriseHolder"; }

        }
        /// <summary>
        /// 股东
        /// </summary>
        public static string DataTableShareHolder
        {
            get { return "QCCEnterpriseShareHolder"; }

        }


        /// <summary>
        /// 返回
        /// </summary>
        public static string DataTableRelation
        {
            get { return "QCCEnterpriseRelation"; }

        }
        /// <summary>
        /// html地址
        /// </summary>
        public static string DataTableHtml
        {
            get { return "QCCEnterpriseHtml"; }

        }
        /// <summary>
        /// 关键字数据库
        /// </summary>
        public static string DataTableKeyWord
        {
            get { return "QiXinEnterpriseKeyWord"; }

        }

        /// <summary>
        /// 关键字数据库,查找过的地区+关键字组合
        /// </summary>
        public static string DataTableCityKeyWord
        {
            get { return DATATABLENAME + "CityKeyWord"; }

        }
        public static string DataTableScopeKeyWord
        {
            get { return DATATABLENAME + "ScopeKeyWord"; }

        }
        public static string DataTableEnterriseKeyWord
        {
            get {
                return "QiXinEnterpriseKeyWord";//1000多
                //return "QCCEnterpriseKeyWord";//企业最下级
            }

        }


        /// <summary>
        /// 关键字匹配增加数量
        /// </summary>
        public static string DataTableEnterriseKeyWordCount
        {
            get
            {
                return "QCCEnterpriseKeyWordCount";//1000多
            }

        }

        public static string DataTableCity
        {
            get { return DATATABLENAME + "City"; }

        }

        public static string DataTableIndustry
        {
            get { return DATATABLENAME + "Industry"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public static string DataTableNameKeyWordURL
        {
            get { return DATATABLENAME + "KeyWordURL"; }

        }
        public static string DataTableNameKeyWordURLAPP
        {
            get { return DATATABLENAME + "KeyWordURLAPP"; }

        }
        /// <summary>
        /// 超时失败的url，同一时间进行重新爬取
        /// </summary>
        public static string DataTableNameErrorUrl
        {
            get { return DATATABLENAME + "ErrorUrl"; }
        }
        /// <summary>
        ///  hash对应
        /// </summary>
        public static string DataTableAccountHashMap
        {
            get { return DATATABLENAME + "HashMap"; }
        }

        /// <summary>
        /// QCCApp设备账号
        /// </summary>
        public static string QCCDeviceAccount
        {
            get { return "QCCDeviceAccount"; }
        }

        public static CrawlSettings curCrawlSettings
        {
            get { return Settings; }
        }
        /// <summary>
        /// 载入app设定
        /// </summary>
        public bool SetSetting(string deviceId, string timestamp, string sign, string refleshToken, string accessToken)
        {
            Settings.neeedChangeAccount = false;
            DeviceAccountRelease(Settings.DeviceId);//当前设备注销
            if (!string.IsNullOrEmpty(deviceId))
            {
                Settings.DeviceId = deviceId.Trim();
            }
            if (!string.IsNullOrEmpty(timestamp))
            {
                Settings.timestamp = timestamp.Trim();
            }
            if (!string.IsNullOrEmpty(sign))
            {
                Settings.sign = sign.Trim();
            }

            Settings.RefleshToken = refleshToken.Trim();
            Settings.AccessToken = accessToken.Trim();
            DeviceAccountApply(Settings.DeviceId);//当前设备登陆
            if (string.IsNullOrEmpty(Settings.AccessToken) || true)
            {
                var result = RefreshToken(true);//强制刷新
                if (result.Contains("成功"))
                {

                    ShowMessageInfo(string.Format("DeviceId:{0},timestamp:{1},sign:{2},RefleshToken:{3},AccessToken:{4}", Settings.DeviceId, Settings.timestamp, Settings.sign, Settings.RefleshToken, Settings.AccessToken), false);
                    return true;
                }
            }
            return false;

        }
        public bool SetSetting(BsonDocument deviceAccount)
        {
            string deviceId = deviceAccount.Text("deviceId");
            string timestamp = deviceAccount.Text("timestamp");
            string sign = deviceAccount.Text("sign");
            string refleshToken = deviceAccount.Text("refleshToken");
            string accessToken = deviceAccount.Text("accessToken");

            if (deviceAccount != null) {
                return SetSetting(deviceId, timestamp, sign, refleshToken, accessToken);
            }
            return false;
        }
        /// <summary>
        /// 获取设备列表
        /// </summary>
        public List<BsonDocument> GetAppDeviceAccount
        {
            get { return allDeviceAccountList; }
        }
        #endregion
        public Form1()
        {
            InitializeComponent();
        }
        #region 初始化待处理的数据
        private BsonDocument curCity = null;

        private void initCurCity()
        {
            cityNameStr = "";
            // var cityNameStr = "上海,北京,成都,福州,广州,杭州,黄山,济南,龙岩,南昌,南京,宁波,泉州,深圳,苏州,武汉,西安,厦门,大连,长沙,合肥,镇江,宁波,中山,郑州,昆明,江苏,重庆";//北京,广州,上海
            var cityNameList = cityNameStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (comboBox2.SelectedIndex != -1)
            {
                cityNameStr = comboBox2.SelectedItem.ToString();
            }

            //修正后通过企查查 内部城市地址进行查找
            var cityList = dataop.FindAll(DataTableCity).ToList();
            curCity = cityList.Where(c => c.Text("name").Contains(cityNameStr)).FirstOrDefault();
            if (curCity == null || string.IsNullOrEmpty(cityNameStr))
            {
                ShowMessageInfo("无城市");
                this.Text = "无城市";
                return;
            }
           // SetEnterpriseDataOP("192.168.1.124");//默认
            SetEnterpriseDataOP("59.61.72.38");//默认
            //if (!string.IsNullOrEmpty(curCity.Text("enterpriseIp")))
            //{
            //    SetEnterpriseDataOP(curCity.Text("enterpriseIp"));
            //}
            //else
            //{
            //    //  SetEnterpriseDataOP(ip);//默认
            //    SetEnterpriseDataOP("192.168.1.124");//默认
            //}
            if (searchType == SearchType.EnterpriseGuidByKeyWord_APP)
            {
                this.guardTimer.Interval = 60000;
            }
        }
        /// <summary>
        /// 中转地址
        /// </summary>
        private void InitialEnterpriseData()
        {
            initCurCity();

            if (UrlQueue.Instance.Count > 0) return;
            switch (searchType)
            {
                case SearchType.EnterpriseGuid:
                    InitialEnterpriseSearch();
                    break;
                case SearchType.EnterpriseGuidByKeyWord:
                    InitialEnterpriseGuidByKeyWord();
                    break;

                case SearchType.EnterpriseGuidByType:
                    InitialEnterpriseSearchByType();
                    break;
                case SearchType.EnterpriseGuidByCity:
                    InitialEnterpriseGuidByCity();
                    break;

                case SearchType.EnterpriseGuidByKeyWordEnhence:
                    InitialEnterpriseGuidByKeyWordEnhence();
                    break;
                case SearchType.EnterpriseGuidByKeyWord_APP:
                    InitialEnterpriseGuidByKeyWord_App();
                    break;
                case SearchType.UpdateEnterpriseInfo:
                default:
                    InitialEnterpriseInfo();
                    break;
            }



        }

        /// <summary>
        /// 初始化待转化的企业名称
        /// http://www.qichacha.com/service/getRiskInfo?eid=aacadab2-4e8c-416d-b1d9-8dc68d65c6e7&_=1469612689725 风险信息
        /// http://www.qichacha.com/service/getAbilityInfo?eid=aacadab2-4e8c-416d-b1d9-8dc68d65c6e7&_=1469612689726 知识产权
        /// http://www.qichacha.com/service/getInvestedCompaniesById?eid=aacadab2-4e8c-416d-b1d9-8dc68d65c6e7&_=1469612689727 对外投资
        /// http://www.qichacha.com/service/getAnnualReport?eid=aacadab2-4e8c-416d-b1d9-8dc68d65c6e7&_=1469612689728 企业年报
        /// http://www.qichacha.com/service/getOperationInfo?eid=aacadab2-4e8c-416d-b1d9-8dc68d65c6e7&_=1469612689729 经营信息
        /// http://www.qichacha.com/service/getRootNodeInfoByEnterpriseId?enterpriseId={0}&_={1}//企业脉络图
        /// 
        /// </summary>
        private void InitialEnterpriseInfo()
        {
            Settings.MaxReTryTimes = 3;
            //if (DateTime.Now.Hour <= 12 || DateTime.Now.Hour >= 20)//下午12点到8点不过滤
            //{
            //    Settings.MaxAccountCrawlerCount = 500;
            //}
            //Settings.MaxAccountCrawlerCount = 100;
            var takeCount = 0;
            if (!string.IsNullOrEmpty(this.textBox3.Text))
            {
                int.TryParse(this.textBox3.Text.Trim(), out takeCount);
            }
            if (takeCount <= 0)
            {
                takeCount = 100;
            }

            //获取key!=1的 或者没有hasPrblem字段的优先,这些是从enterprise地块中优先提取出来的
            var cityName = cityNameStr;

            var cityQuery = Query.EQ("cityName", cityName);
            if (cityName == "地块企业")
            {
                cityQuery = Query.EQ("isLandEnterprise", "1");
            }
            var filterQuery = Query.And(Query.NE("status", "吊销"), Query.NE("status", "注销"));
            var contentQuery = Query.Or(Query.NE("isUserUpdate", "1"));
            var allEnterpriseList = new List<BsonDocument>();
            var rand = new Random();

            var totalCount = 0;
            if (!string.IsNullOrEmpty(cityName))
            {
                totalCount = enterpriseDataop.FindCount(DataTableName, Query.And(contentQuery, cityQuery, filterQuery));
                var count = rand.Next(0, totalCount);

                if (count <= 100)
                {
                    count = 0;
                }
                //allEnterpriseList = dataop.FindAllByQuery(DataTableName, Query.And(contentQuery, cityQuery, filterQuery)).SetFields("name", "guid").Skip(count).Take(takeCount).ToList();
                allEnterpriseList = enterpriseDataop.FindLimitByQuery(DataTableName, Query.And(contentQuery, cityQuery, filterQuery), new SortByDocument(), count, takeCount).SetFields("name", "guid").ToList();
            }
            else
            {
                allEnterpriseList = new List<BsonDocument>();

                //totalCount = dataop.FindCount(DataTableName, Query.And(contentQuery, filterQuery));
                //var count = rand.Next(0, totalCount);
                //if (count <= 100)
                //{
                //    count = 0;
                //}
                //// allEnterpriseList = dataop.FindAllByQuery(DataTableName, Query.And(contentQuery, filterQuery)).SetFields("name", "guid").Skip(count).Take(takeCount).ToList();
                //allEnterpriseList = dataop.FindLimitByQuery(DataTableName, Query.And(contentQuery, filterQuery), new SortByDocument(), count, takeCount).SetFields("name", "guid").ToList();
                /////totalCount= dataop.FindCount(DataTableName, Query.And(contentQuery, filterQuery));
            }
            if (!string.IsNullOrEmpty(cityName))
                this.Text = string.Format("{0}剩余个数：{1}条数据", cityName, totalCount);


            if (allEnterpriseList.Count() > 0)
            {
                foreach (var enterprise in allEnterpriseList)
                {
                    var enName = enterprise.Text("name");
                    if (string.IsNullOrEmpty(enName))
                    {
                        enName = "****";
                    }
                    else
                    {
                        enName = HttpUtility.UrlEncode(enterprise.Text("name"));
                    }
                    if (enName.Length <= 3)//过滤人名
                    {
                        continue;
                    }
                    //UrlQueue.Instance.EnQueue(new UrlInfo(string.Format("http://www.qichacha.com{0}", enterprise.Text("url"))) { Depth = 1 });
                    foreach (var urlType in enterpriseInfoUrlType)
                    {
                        var otherInfoUrl = string.Format("http://www.qichacha.com/company_getinfos?unique={0}&companyname={1}&tab={2}", enterprise.Text("guid"), enName, urlType);
                        UrlQueue.Instance.EnQueue(new UrlInfo(otherInfoUrl) { Depth = 1 });
                    }
                    //http://www.qichacha.com/more_findmuhou?keyNo=c29fb59a50a8d6f0cab90a2dac54cbf8//幕后关系
                    //http://www.qichacha.com/cms_map?keyNo=c29fb59a50a8d6f0cab90a2dac54cbf8&upstreamCount=1&downstreamCount=1//qiyeguanxi
                    //var backDetailInfoUrl = string.Format("http://www.qichacha.com/more_findmuhou?keyNo={0}", enterprise.Text("guid"));
                    ////var detailInfoUrl = string.Format("http://www.qichacha.com/cms_map?keyNo={0}&upstreamCount=1&downstreamCount=1", enterprise.Text("guid"));
                    ////UrlQueue.Instance.EnQueue(new UrlInfo(detailInfoUrl) { Depth = 1 });
                    //UrlQueue.Instance.EnQueue(new UrlInfo(backDetailInfoUrl) { Depth = 1 });
                }

            }
            else
            {
                if (autoChangeAccountCHK.Checked)
                {
                    Application.Exit();
                }
                //MessageBox.Show("无数据");
                ShowMessageInfo("无数据");
            }

        }

        /// <summary>
        /// 初始化待转化的企业名称http://www.qichacha.com/company/{0}
        /// </summary>
        private void InitialEnterpriseGuidByKeyWordEnhence()
        {
            Settings.MaxReTryTimes = 100;
            var enterpriseDataop = new DataOperation(new MongoOperation(enterpriseNameConnStr));
            var cityName = cityNameStr;

            if (string.IsNullOrEmpty(cityName))
            {
                MessageBox.Show("请选择城市");
            }
            var cityQuery = Query.EQ("cityName", cityName);
            //var hitAllEnterpriseList = dataop.FindAllByQuery(DataTableName, cityQuery).SetFields( "guid").ToList();

            //foreach (var guid in hitAllEnterpriseList.Select(c => c.Text("guid")).ToList())
            //{
            //    if (!existGuidList.Contains(guid))
            //    {
            //        existGuidList.Add(guid);
            //        //return;//测试只添加一次
            //    }
            //}
            // var nameList = hitAllEnterpriseList.Select(c => c.Text("name")).ToList();


            //foreach (var name in nameList)
            //{
            //    if (!existNameList.Contains(name))
            //    {
            //        existNameList.Add(name);
            //        //return;//测试只添加一次
            //    }
            //}
            if (cityNameStr == "烟台")
            {
                tempTableName = "SQ_yantai_Company";
            }
            var allEnterpriseList = new List<BsonDocument>();
            switch (cityNameStr)
            {
                case "烟台":
                    tempTableName = "SQ_yantai_Company";
                    break;
                case "西安":
                    tempTableName = "QCCEnterprise_XiAn";//"SQ_xian_Company";
                    enterpriseDataop = dataop;
                    break;

                case "北京": tempTableName = "SQ_beijing_Company"; break;

                case "成都": tempTableName = "SQ_chengdu_Company"; break;

                case "深圳": tempTableName = "SQ_shen_Company"; break;

                case "佛山":
                    tempTableName = "QCCEnterprise_FoShan";
                    enterpriseDataop = dataop; break;


            }
            allEnterpriseList = enterpriseDataop.FindAllByQuery(tempTableName, Query.Exists("isSearched", false)).SetFields("name").ToList();
            if (allEnterpriseList.Count() > 0)
            {

                foreach (var enterprise in allEnterpriseList.Where(c => !existNameList.Contains(c.Text("name"))))
                {
                    var guidUrl = string.Format("http://www.qichacha.com/gongsi_getList?key={0}", enterprise.Text("name"));
                    UrlQueue.Instance.EnQueue(new UrlInfo(guidUrl) { Depth = 1, PostData = string.Format("key={0}&type=0", enterprise.Text("name")) });
                }
            }
            else
            {
                MessageBox.Show("无数据");
            }
        }



        /// <summary>
        /// 初始化待转化的企业名称
        /// </summary>
        private void InitialEnterpriseSearch()
        {
            var allEnterpriseList = enterpriseDataop.FindAll(DataTableName).SetFields("name", "guid").ToList();

            var existNameList = allEnterpriseList.Select(c => c.Text("name")).ToList();



            foreach (var enterprise in allEnterpriseList)
            {
                if (!existGuidList.Contains(enterprise.Text("guid")))
                {
                    existGuidList.Add(enterprise.Text("guid"));
                    //return;//测试只添加一次
                }
            }
            //var cityNameStr = "上海,北京,成都,福州,广州,杭州,黄山,济南,龙岩,南昌,南京,宁波,泉州,深圳,苏州,武汉,西安,厦门,大连,长沙,合肥,镇江,宁波,中山,郑州,昆明,江苏,重庆";
            //var cityNameStr = "北京";
            //// var cityNameStr = "上海,北京,成都,福州,广州,杭州,黄山,济南,龙岩,南昌,南京,宁波,泉州,深圳,苏州,武汉,西安,厦门,大连,长沙,合肥,镇江,宁波,中山,郑州,昆明,江苏,重庆";//北京,广州,上海
            var cityNameList = cityNameStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (comboBox2.SelectedIndex != -1)
            {
                cityNameList = new List<string>() { cityNameStr };
            }
            var allNeedEnterpriseList = dataop.FindAllByQuery(DataTableNameList,
                Query.And(Query.In("城市", cityNameList.Select(c => (BsonValue)c)),
                Query.EQ("isFirst", "1"), Query.NE("status", "1"), Query.NE("isSearched", "1"))).SetFields("name").Take(200).Select(c => c.Text("name")).ToList();

            //过滤已存在的对象
            allNeedEnterpriseList = allNeedEnterpriseList.Where(c => !existNameList.Contains(c)).ToList();



            Console.WriteLine("初始化数据");
            var updateSB = new StringBuilder();
            foreach (string enterpriseName in allNeedEnterpriseList.Where(c => c.Length > 3))
            {
                //if (allEnterpriseList.Where(c => c.Text("name") == enterpriseName.Trim()).Count() > 0) continue;
                //var enterPriseNameArray = enterpriseName.Split(new string[] { ",","、","，","和"},StringSplitOptions.RemoveEmptyEntries);
                //foreach(var name in enterPriseNameArray){
                enterpriseName.Replace("（）", "");
                var url = string.Format("http://www.qichacha.com/search?key={0}&type=enterprise&source=&isGlobal=Y", HttpUtility.UrlEncode(enterpriseName));
                UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });

            }
            if (UrlQueue.Instance.Count <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
                MessageBox.Show("无数据");

            }
        }


        /// <summary>
        /// 通过关键字+地区进行公司获取
        /// http://www.qichacha.com/search?key=%E7%83%9F%E5%8F%B0+%E9%A3%9F%E5%93%81%E6%B7%BB%E5%8A%A0%E5%89%82&type=enterprise&source=&isGlobal=Y
        /// </summary>
        private void InitialEnterpriseGuidByKeyWord()
        {
            Console.WriteLine("初始化数据");
            Settings.MaxReTryTimes = 100;//尝试最大个数
            if (DateTime.Now.Hour <= 14 || DateTime.Now.Hour >= 20)//下午2点到8点不过滤
            {
                Settings.MaxAccountCrawlerCount = 500;
            }
            //var allEnterpriseList = dataop.FindAll(DataTableName).SetFields("guid").Select(c => c.Text("guid")).ToList(); ;


            //foreach(var guid in allEnterpriseList) { 
            //  if (!existGuidList.Contains(guid))
            //    {
            //        existGuidList.Add(guid);
            //        //return;//测试只添加一次
            //    }
            //}

            //var cityNameStr = "上海,北京,成都,福州,广州,杭州,黄山,济南,龙岩,南昌,南京,宁波,泉州,深圳,苏州,武汉,西安,厦门,大连,长沙,合肥,镇江,宁波,中山,郑州,昆明,江苏,重庆";
            if (curCity == null) return;
            var curProvince = cityList.Where(c => c.Text("code") == curCity.Text("provinceCode")).FirstOrDefault();
            var typeList = dataop.FindAll(DataTableIndustry).ToList();//遍历所有的父分类与子分类
            if (NEEDRECORDURL)
            {
                var allHitUrlList = dataop.FindAllByQuery(DataTableNameKeyWordURL, Query.And(Query.EQ("province", curCity.Text("provinceCode")), Query.EQ("cityCode", curCity.Text("code")))).ToList();//获取执行过的url
                                                                                                                                                                                                       //那些超出1000的url 和第一页需要重新进行读取一次，而且分页内容可过滤
                                                                                                                                                                                                       // foreach (var hitUrl in allHitUrlList.Where(c=>c.Int("isSplit")!=1&& c.Int("isFirstPage") != 1))
                foreach (var hitUrl in allHitUrlList.Where(c => c.Int("isSplit") != 1))
                {
                    var url = hitUrl.Text("url");
                    if (url.EndsWith("&p=1") || !url.Contains("&p="))
                    {

                    }
                    else
                    {
                        if (!urlFilter.Contains(hitUrl.Text("url")))
                        {
                            urlFilter.Add(hitUrl.Text("url"));

                        }
                    }
                }
            }
            foreach (var type in typeList.Where(c => c.Int("type") == 1).OrderByDescending(c => c.Int("isImportant")).ThenByDescending(c => c.Text("parentCode")))
            {
                //var parentType = typeList.Where(c => c.Text("code") == type.Text("parentCode")).FirstOrDefault();
                //if (parentType == null) {
                //    continue;
                //}
                var province = curCity.Text("provinceCode");
                var cityCode = curCity.Text("code");
                if (curProvince == null)
                {
                    province = cityCode;
                    cityCode = string.Empty;
                }
                var industryCode = type.Text("parentCode");
                var subIndustryCode = type.Text("code");
                var typeName = type.Text("name");
                var typeNameList = new List<string>();
                var typeNameArr = typeName.Split(new string[] { "和", "、" }, StringSplitOptions.RemoveEmptyEntries);
                if (typeNameArr.Count() > 1)
                {
                    foreach (var _type in typeNameArr)
                    {
                        typeNameList.Add(_type.TrimEnd(new char[] { '业' }));
                    }
                }
                else
                {
                    typeNameList.Add(typeName.TrimEnd(new char[] { '业' }));
                }
                foreach (var _typeName in typeNameList.Distinct())
                {
                    var curTypeName = _typeName;
                    if (curTypeName.Length == 1)
                    {
                        curTypeName += "业";
                    }
                    //需要限制住大类防止url爬取过多，因为如房地产会导致其他大类里面有很多，只要限制住大类即可，其他的由其他的关键字爬取
                    //优化爬取效率
                    var url = string.Format("http://www.qichacha.com/search_index?key={0}{1}&index=0&statusCode=&registCapiBegin=&registCapiEnd=&sortField=&isSortAsc=&province={2}&startDateBegin=&startDateEnd=&cityCode={3}&industryCode={4}&subIndustryCode={5}&ajaxflag=true&p=1", "", HttpUtility.UrlEncode(curTypeName), province, cityCode, industryCode, "");
                    if (!urlFilter.Contains(url))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });
                    }
                }
                //return;
            }
            //房地产测试用例
            // var _url = "http://www.qichacha.com/search_index?key=%E6%88%BF%E5%9C%B0%E4%BA%A7&index=0&statusCode=&registCapiBegin=&registCapiEnd=&sortField=&isSortAsc=&province=SAX&startDateBegin=&startDateEnd=&cityCode=1&industryCode=K&subIndustryCode=&ajaxflag=true&p=1";
            //if (!urlFilter.Contains(_url))
            //{
            //    UrlQueue.Instance.EnQueue(new UrlInfo(_url) { Depth = 1 });
            //}
            if (UrlQueue.Instance.Count <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
                MessageBox.Show("无数据");

            }
        }



        /// <summary>
        /// 初始化待转化的企业名称
        /// </summary>
        private void InitialEnterpriseSearchByType()
        {
            //var allEnterpriseList = dataop.FindAll(DataTableName).SetFields("guid").Select(c => c.Text("guid")).ToList();

            //foreach (var guid in allEnterpriseList)
            //{
            //    if (!existGuidList.Contains(guid))
            //    {
            //        existGuidList.Add(guid);
            //        //return;//测试只添加一次
            //    }
            //}

            var index = 0;
            for (char charc = 'A'; charc <= 'T'; charc++)
            {
                Thread.Sleep(500);
                validUrl = string.Format("http://www.qichacha.com/gongsi_industry?industryCode={0}&industryorder={1}", charc.ToString(), index++);

                if (!validUrl.ToString().Contains("gongsi_industry")) return;


                var industryCode = GetUrlParam(validUrl, "industryCode");//行业代码
                if (string.IsNullOrEmpty(industryCode)) return;
                var industryorder = GetUrlParam(validUrl, "industryorder");//行业代码
                                                                           //获取当前页面的分页信息

                HttpResult result = GetHttpHtml(new UrlInfo(validUrl));
                var args = new DataReceivedEventArgs() { Depth = 1, Html = result.Html, IpProx = null, Url = validUrl };
                var subIndustryCodeList = new List<string>();

                if (!IPLimitProcess(args) && result.StatusCode == HttpStatusCode.OK)
                {
                    HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    htmlDoc.LoadHtml(args.Html);
                    var curUpdateBson = new BsonDocument();
                    var curType = htmlDoc.DocumentNode.SelectSingleNode("//dl[@class='filter-tag clearfix']/dd/a[@class='current']");
                    if (curType != null)
                    {

                        var curFCodeBson = new BsonDocument();
                        curFCodeBson.Add("code", charc.ToString());
                        curFCodeBson.Add("name", curType.InnerText.Trim());
                        curFCodeBson.Add("type", "0");
                        //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curFCodeBson, Name = DataTableIndustry, Type = StorageType.Insert  });
                    }

                    var firstDl = htmlDoc.DocumentNode.SelectNodes("//dl[@class='filter-tag clearfix']").Where(c => c.InnerHtml.Contains("行业大类")).FirstOrDefault();
                    if (firstDl == null) return;

                    var searchResult = firstDl.ChildNodes.Where(c => c.Name == "dd");
                    foreach (var ddNode in searchResult)
                    {
                        var aNode = ddNode.ChildNodes.Where(c => c.Name == "a").FirstOrDefault();
                        if (aNode == null) continue;
                        var url = aNode.Attributes["href"] != null ? aNode.Attributes["href"].Value : string.Empty;
                        //获取
                        var subIndustryCode = GetUrlParam(url, "subIndustryCode");//行业代码

                        if (!string.IsNullOrEmpty(subIndustryCode))
                        {
                            subIndustryCodeList.Add(subIndustryCode);
                        }
                        var curCodeBson = new BsonDocument();
                        curCodeBson.Add("code", subIndustryCode);
                        curCodeBson.Add("parentCode", charc.ToString());
                        curCodeBson.Add("name", aNode.InnerText.Trim());
                        curCodeBson.Add("type", "1");
                        //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curCodeBson, Name = DataTableIndustry, Type = StorageType.Insert });
                    }

                }

                // StartDBChangeProcessQuick();
                Console.WriteLine("初始化数据");
                var updateSB = new StringBuilder();
                var pageCount = 500;
                foreach (var subIndustryCode in subIndustryCodeList)
                {
                    for (var i = 1; i <= pageCount; i++)//当前行业大类分页
                    {

                        var url = string.Format("http://www.qichacha.com/gongsi_industry_industryCode_{0}_subIndustryCode_{1}_industryorder_{2}_p_{3}.shtml", industryCode, subIndustryCode, industryorder, i);
                        //var url = string.Format("{0}?page={1}", typeNameStr, i);
                        //UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });

                    }
                }
            }
            if (UrlQueue.Instance.Count <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
                MessageBox.Show("无数据");

            }
        }

        /// <summary>
        /// 初始化待转化的企业名称
        /// </summary>
        private void InitialEnterpriseGuidByCity()
        {
            if (cityNameDic.Count() <= 0)
            {
                //cityNameDic.Add("AH", "安徽");
                //cityNameDic.Add("BJ", "北京");
                //cityNameDic.Add("GD", "广东");
                //cityNameDic.Add("FJ", "福建");
                //cityNameDic.Add("GS", "甘肃");
                //cityNameDic.Add("CQ", "重庆");
                //cityNameDic.Add("GX", "广西");
                //cityNameDic.Add("GZ", "贵州");
                //cityNameDic.Add("HAIN", "海南");
                //cityNameDic.Add("HB", "河北");
                //cityNameDic.Add("HLJ", "黑龙江");
                //cityNameDic.Add("HUB", "湖北");
                //cityNameDic.Add("HUN", "湖南");
                //cityNameDic.Add("JS", "江苏");
                //cityNameDic.Add("JX", "江西");
                //cityNameDic.Add("JL", "吉林");
                //cityNameDic.Add("LN", "辽宁");
                //cityNameDic.Add("NMG", "内蒙古");
                //cityNameDic.Add("NX", "宁夏");
                //cityNameDic.Add("QH", "青海");
                //cityNameDic.Add("SD", "山东");
                //cityNameDic.Add("SH", "上海");
                //cityNameDic.Add("SX", "山西");
                //cityNameDic.Add("SAX", "陕西");
                //cityNameDic.Add("SC", "四川");
                //cityNameDic.Add("TJ", "天津");
                //cityNameDic.Add("XJ", "新疆");
                //cityNameDic.Add("XZ", "西藏");
                //cityNameDic.Add("YN", "云南");
                //cityNameDic.Add("ZJ", "浙江");
                //cityNameDic.Add("CN", "总局");
                cityNameDic.Add("HEN", "河南");
                // StartDBChangeProcessQuick();
            }
            var allEnterpriseList = enterpriseDataop.FindAllByQuery(DataTableName, Query.EQ("cityName", cityNameStr)).SetFields("guid").Select(c => c.Text("guid")).ToList();

            foreach (var guid in allEnterpriseList)
            {
                if (!existGuidList.Contains(guid))
                {
                    existGuidList.Add(guid);
                    //return;//测试只添加一次
                }
            }
            if (!validUrl.ToString().Contains("gongsi_area_prov")) return;


            var pageCount = 500;
            Console.WriteLine("初始化数据");
            var updateSB = new StringBuilder();
            foreach (var dic in cityNameDic)
            {
                var typeName = dic.Key;

                //  var url = string.Format("http://www.qichacha.com/search_index?key=%E6%88%BF%E5%9C%B0%E4%BA%A7&index=0&statusCode=&registCapiBegin=&registCapiEnd=&sortField=&isSortAsc=&province={0}&startDateBegin=&startDateEnd=&industryCode=I&subIndustryCode=&ajaxflag=true&p=1", typeName);
                //  var url = string.Format("http://www.qichacha.com/gongsi_area_prov_{0}_p_{1}.shtml", typeName, i);
                // var url = typeNameStr;
                // UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });


            }


            if (UrlQueue.Instance.Count <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
                MessageBox.Show("无数据");

            }
        }

        /// <summary>
        /// 通过关键字+地区进行公司获取
        /// </summary>
        private void InitialEnterpriseGuidByKeyWord_App()
        {
            if (StringQueue.Instance.Count > 0)
            {
                return;
            }
            Console.WriteLine("初始化数据");
            Settings.MaxReTryTimes = 100;//尝试最大个数
            //Settings.MaxAccountCrawlerCount = 200;//最大化利用账号
            //this.textBox5.Text = "10000";
            //var allEnterpriseList = dataop.FindAll(DataTableName).SetFields("guid").Select(c => c.Text("guid")).ToList(); ;


            //foreach(var guid in allEnterpriseList) { 
            //  if (!existGuidList.Contains(guid))
            //    {
            //        existGuidList.Add(guid);
            //        //return;//测试只添加一次
            //    }
            //}

            //var cityNameStr = "上海,北京,成都,福州,广州,杭州,黄山,济南,龙岩,南昌,南京,宁波,泉州,深圳,苏州,武汉,西安,厦门,大连,长沙,合肥,镇江,宁波,中山,郑州,昆明,江苏,重庆";

            if (curCity == null)
            {
                ShowMessageInfo("请先初始化城市");
                return;
            }
            var curProvince = cityList.Where(c => c.Text("code") == curCity.Text("provinceCode")).FirstOrDefault();
            var typeList = dataop.FindAll(DataTableIndustry).ToList();//遍历所有的父分类与子分类
            allSubFactoryList = typeList.Where(c => c.Int("type") == 1).ToList();//获取子类
            if (NEEDRECORDURL)
            {                                                     //因为app只能取40个一页需要条件筛选到最后，并且超出个数需要后续通过vip账号进行重复爬取                                                                                                                                                                              // foreach (var hitUrl in allHitUrlList.Where(c=>c.Int("isSplit")!=1&& c.Int("isFirstPage") != 1))
                var allHitUrlList = dataop.FindAllByQuery(DataTableNameKeyWordURLAPP, Query.And(Query.EQ("province", curCity.Text("provinceCode")), Query.EQ("cityCode", curCity.Text("code")), Query.EQ("isApp", "1"))).ToList();//获取执行过的url
                                                                                                                                                                                                                                  //那些超出1000的url 和第一页需要重新进行读取一次，而且分页内容可过滤
                                                                                                                                                                                                                                  //app只能查找第一页                                                                                                                                                                                      // foreach (var hitUrl in allHitUrlList.Where(c=>c.Int("isSplit")!=1&& c.Int("isFirstPage") != 1))
                foreach (var hitUrl in allHitUrlList.Where(c => c.Int("isSplit") != 1 && c.Int("recordCount") <= 40))
                {

                    if (!urlFilter.Contains(hitUrl.Text("url")))
                    {
                        urlFilter.Add(hitUrl.Text("url"));
                    }

                }
            }

            var typeNameList = new List<string>();
            var province = curCity.Text("provinceCode");
            var cityCode = curCity.Text("code");
            if (curProvince == null)
            {
                province = cityCode;
                cityCode = string.Empty;
            }

            curQCCProvinceCode = province;
            curQCCCityCode = cityCode;
            #region QCC实时关键字初始化

            //foreach (var type in typeList.Where(c => c.Int("type") == 1).OrderBy(c => c.Text("parentCode")))
            //{
            //    // break;
            //    //var parentType = typeList.Where(c => c.Text("code") == type.Text("parentCode")).FirstOrDefault();
            //    //if (parentType == null) {
            //    //    continue;
            //    //}

            //    var industryCode = type.Text("parentCode");
            //    var subIndustryCode = type.Text("code");
            //    var typeName = type.Text("name");

            //    var typeNameArr = typeName.Split(new string[] { "和", "、" }, StringSplitOptions.RemoveEmptyEntries);
            //    if (typeNameArr.Count() > 1)
            //    {
            //        foreach (var _type in typeNameArr)
            //        {
            //            typeNameList.Add(_type.TrimEnd(new char[] { '业' }));
            //        }
            //    }
            //    else
            //    {
            //        typeNameList.Add(typeName.TrimEnd(new char[] { '业' }));
            //    }

            //    //return;
            //}
            ///是否限制split次数
            if(this.splitLimitChk.Checked == true) { 
            typeNameList = dataop.FindFieldsByQuery(DataTableEnterriseKeyWord,null,new string[] { "keyWord","count"}).Where(c=>c.Int("count")>0).OrderByDescending(c => c.Int("count")).Select(c => c.Text("keyWord")).ToList();
            }
            else {
                var cityNameQueryList = new List<string>() { "南昌", "沈阳", "大连" };
                // typeNameList = dataop.FindFieldsByQuery(DataTableEnterriseKeyWordCount, Query.EQ("cityName", "广州"), new string[] { "keyWord", "count" }).Where(c => c.Int("count") > 20).OrderByDescending(c => c.Int("count")).Select(c => c.Text("keyWord")).ToList();
                typeNameList = dataop.FindFieldsByQuery(DataTableEnterriseKeyWordCount, Query.In("cityName", cityNameQueryList.Select(c=>(BsonValue)c)), new string[] { "keyWord", "count" }).Where(c => c.Int("count") >1).OrderByDescending(c => c.Int("count")).Select(c => c.Text("keyWord")).Distinct().ToList();
            }
            InitKeyWordHitCount(cityNameStr);
            // typeNameList = new List<string>() { "建筑", "餐饮", "服务", "代理", "服装", "化工", "电力", "木材", "广告", "项目咨询","装饰装修" }; 
            // typeNameList = dataop.FindAll(DataTableScopeKeyWord).Select(c => c.Text("keyWord")).ToList();
            //typeNameList =new List<string>() { "化妆用具","化妆品","化妆品毛刷","皮具","内衣","鞋帽","服饰、手袋、手机配件、塑胶制品、包装制品 };
            // typeNameList.Add("母婴");
            foreach (var _typeName in typeNameList.Distinct())
            {
              

                if (!this.singalKeyWordCHK.Checked)//是否单个单个运行
                {
                    //按时间逆序
                    //var url = string.Format("https://app.qichacha.net/app/v1/base/advancedSearch?searchKey={0}&searchIndex=multicondition&province={1}&cityCode={2}&statusCode=&registCapiBegin=&registCapiEnd=&isSortAsc=false&sortField=startdate&startDateBegin=&startDateEnd=&industryCode={3}&subIndustryCode={4}&p=1", curTypeName, province, cityCode, "", "");
                    var url = InitalQCCAppUrlByKeyWord(_typeName);
                    if (!urlFilter.Contains(url))
                    {
                        var urlInfo = new UrlInfo(url) { Depth = 1 };
                        ///是否限制split次数
                        if (this.splitLimitChk.Checked == true)
                        {
                            urlInfo.UrlSplitTimes = -3;//只能拆分三次
                        }
                       
                        UrlQueue.Instance.EnQueue(urlInfo);
                    }
                }
                else
                {
                    StringQueue.Instance.EnQueue(_typeName);
                }
            }
             

            //  StartDBChangeProcessQuick();
            #endregion
            //武汉5000条数据测试用例\
            // MessageBox.Show("泉州测试用例");
            //var _url = "https://app.qichacha.net/app/v1/base/advancedSearch?searchKey={%22scope%22%3a%22%e5%85%b6%e4%bb%96%e9%87%87%e7%9f%bf%22%2c%22opername%22%3a%22%e5%85%b6%e4%bb%96%e9%87%87%e7%9f%bf%22%2c%22featurelist%22%3a%22%e5%85%b6%e4%bb%96%e9%87%87%e7%9f%bf%22%2c%22address%22%3a%22%e5%85%b6%e4%bb%96%e9%87%87%e7%9f%bf%22%2c%22name%22%3a%22%e5%85%b6%e4%bb%96%e9%87%87%e7%9f%bf%22}&searchIndex=multicondition&province=FJ&cityCode=5&statusCode=&registCapiBegin=&registCapiEnd=&isSortAsc=false&startDateBegin=&startDateEnd=&industryCode=&subIndustryCode=&timestamp=1475551026186&sign=38c2d1af098cda090241832505e6cb74cde22be8&p=1";
            //  var _url = "https://app.qichacha.net/app/v1/base/advancedSearch?searchKey={%22scope%22:%22%E6%AF%8D%E5%A9%B4%22,%22opername%22:%22%E6%AF%8D%E5%A9%B4%22,%22featurelist%22:%22%E6%AF%8D%E5%A9%B4%22,%22address%22:%22%E6%AF%8D%E5%A9%B4%22,%22name%22:%22%E6%AF%8D%E5%A9%B4%22}&searchIndex=multicondition&province=FJ&cityCode=5&pageIndex=1&isSortAsc=false&industryCode=&subIndustryCode=&timestamp=1475645368972&sign=15df1c178fa82b7b7b26d0653afc6eb95c98a7fa";//母婴
            //if (!urlFilter.Contains(_url))
            //{
            //    UrlQueue.Instance.EnQueue(new UrlInfo(_url) { Depth = 1 });
            //}
            if (UrlQueue.Instance.Count <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
                MessageBox.Show("无数据");

            }
        }
        #endregion
        #region 数据处理

        // HttpHelper http = new HttpHelper();
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            var curLoginAccountObj = new BsonDocument();
            ///重载uri

            try
            {
                GetSimulateCookies();

                switch (searchType)
                {
                    case SearchType.EnterpriseGuid:
                        DataReceiveSearchGuid(args);
                        break;
                    case SearchType.EnterpriseGuidByKeyWord:
                        DataReceiveEnterpriseGuidByKeyWord(args);
                        break;
                    case SearchType.EnterpriseGuidByType:
                        DataReceiveSearchGuidByType(args);
                        break;
                    case SearchType.EnterpriseGuidByCity:
                        DataReceiveSearchGuidByCity(args);
                        break;
                    case SearchType.EnterpriseGuidByKeyWordEnhence:
                        DataReceiveEnterpriseGuidByKeyWordEnhence(args);
                        break;
                    case SearchType.EnterpriseGuidByKeyWord_APP:
                        DataReceiveEnterpriseGuidByKeyWord_APP(args);
                        break;
                    case SearchType.UpdateEnterpriseInfo:
                    default:
                        DataReceiveEnterpriseInfo(args);
                        break;
                }

                ///账号爬取统计，防止被封，查看爬去个数
                if (searchType != SearchType.EnterpriseGuidByKeyWord_APP)
                {
                    curLoginAccountObj = allAccountList.Where(c => c.Text("name") == Settings.LoginAccount).FirstOrDefault();
                }
                else
                {
                    curLoginAccountObj = allDeviceAccountList.Where(c => c.Text("deviceId") == Settings.DeviceId).FirstOrDefault();
                }
                if (curLoginAccountObj != null)
                {
                    var columnName = string.Format("{0}_add", searchType);
                    var curAddional = curLoginAccountObj.Int(columnName);
                    curLoginAccountObj.Set(columnName, curAddional + 1);
                    if (Settings.MaxAccountCrawlerCount > 0 && curAddional >= Settings.MaxAccountCrawlerCount)
                    {
                        Settings.neeedChangeAccount = true;//达到数量需要切换账号
                    }
                    ShowAccountInfo(curLoginAccountObj);
                }
            }
            catch (Exception ex)
            {

                UrlQueue.Instance.EnQueue(new UrlInfo(args.Url) { Depth = args.Depth + Settings.MaxReTryTimes / 10 });
                ShowMessageInfo(ex.Message + args.Url);
            }

        }
        public void DataReceiveEnterpriseInfo(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var curUpdateBson = new BsonDocument();
            var guid = string.Empty;
            guid = GetUrlParam(args.Url, "keyNo");//获取脉络图方式;
            var infoType = GetUrlParam(args.Url, "tab");//获取脉络图方式
            if (string.IsNullOrEmpty(guid))
            {
                guid = GetUrlParam(args.Url, "unique");
            }
            if (string.IsNullOrEmpty(guid))
            {
                guid = GetGuidFromUrl(args.Url);
            }
            var message = string.Format("详细信息{0}获取成功剩余url{1}\r{2}", guid, UrlQueue.Instance.Count, args.Url);
            //获取企业信息http://www.qichacha.com/service/getRootNodeInfoByEnterpriseId?enterpriseId=1b9df7af-e7b3-4d45-93ce-8acf02534adb&_=1466587526737
            if (!string.IsNullOrEmpty(guid))
            {

                #region 基本信息
                if (args.Url.Contains("getinfos") || args.Url.Contains("firm_"))
                {

                    var companyInfo = htmlDoc.DocumentNode.SelectSingleNode("//ul[@class='company-base']");
                    if (companyInfo == null)
                    {
                        //if (args.Depth >= 3)
                        //{
                        //    return;
                        //}
                        //timerStop();
                        //this.webBrowser.Invoke(new Action(() =>
                        //{
                        //    try
                        //    {
                        //        this.webBrowser.Refresh();
                        //    }
                        //    catch (Exception ex)
                        //    { }
                        //}));

                        ShowMessageInfo("无数据信息" + args.Url);
                        return;
                    }
                    curUpdateBson.Set("isUserUpdate", "1");
                    //公司详情
                    var liList = companyInfo.ChildNodes.Where(c => c.Name == "li").ToList();
                    foreach (var li in liList)
                    {

                        var labelText = li.InnerText;
                        var firstIndex = labelText.IndexOf("：");
                        if (firstIndex == -1) continue;
                        var value = labelText.Substring(firstIndex + 1, labelText.Length - firstIndex - 1);
                        var columnName = labelText.Substring(0, firstIndex).Replace(":", "").Trim();
                        if (EnterpriseInfoMapDic.ContainsKey(columnName))
                        {
                            curUpdateBson.Set(EnterpriseInfoMapDic[columnName], value.Trim());
                        }
                        message += labelText + " ";
                    }


                    #endregion
                    //其他信息
                    var otherInfoList = htmlDoc.DocumentNode.SelectNodes("//section[@class='panel b-a clear']");
                    if (otherInfoList != null)
                    {
                        #region 股东信息
                        //股东
                        var shareHolder = otherInfoList.Where(c => c.InnerText.Contains("股东信息")).FirstOrDefault();
                        if (shareHolder != null)
                        {
                            var shareHolderList = new List<BsonDocument>();
                            foreach (var div in shareHolder.ChildNodes.Where(c => c.Attributes["class"] != null && c.Attributes["class"].Value != "panel-heading b-b"))
                            {

                                if (!string.IsNullOrEmpty(div.InnerText))
                                {
                                    var curBson = new BsonDocument();
                                    curBson.Add("name", div.InnerText.Replace("\n", " ").Trim());
                                    shareHolderList.Add(curBson);
                                }
                            }
                            if (shareHolderList.Count() > 0)
                            {
                                curUpdateBson.Set("shareHolder", shareHolderList.ToJson());
                            }
                        }
                        #endregion
                        #region 高管
                        var holder = otherInfoList.Where(c => c.InnerText.Contains("主要人员")).FirstOrDefault();

                        if (holder != null)
                        {
                            var holderList = new List<BsonDocument>();
                            foreach (var div in holder.ChildNodes.Where(c => c.Attributes["class"] != null && c.Attributes["class"].Value != "panel-heading b-b"))
                            {

                                if (!string.IsNullOrEmpty(div.InnerText))
                                {
                                    var curBson = new BsonDocument();
                                    var splitArray = div.InnerText.Split(new string[] { " ", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                                    if (splitArray.Length >= 2)
                                    {
                                        curBson.Add("name", splitArray[0].Trim());
                                        curBson.Add("type", splitArray[1].Trim());
                                        holderList.Add(curBson);
                                    }
                                    else
                                    {
                                        if (splitArray.Length >= 1)
                                        {
                                            if (!string.IsNullOrEmpty(splitArray[0]))
                                                curBson.Add("name", splitArray[0].Trim());
                                        }
                                        else
                                        {
                                            curBson.Add("name", div.InnerText.Trim());
                                        }
                                    }

                                }
                            }
                            if (holderList.Count() > 0)
                            {
                                curUpdateBson.Set("holder", holderList.ToJson());
                            }
                        }
                        #endregion
                        #region 变更记录
                        var changeRecord = otherInfoList.Where(c => c.InnerText.Contains("变更记录")).FirstOrDefault();
                        if (changeRecord != null)
                        {
                            var changeRecordList = new List<BsonDocument>();
                            foreach (var div in changeRecord.ChildNodes.Where(c => c.Attributes["class"] != null && c.Attributes["class"].Value != "panel-heading b-b"))
                            {

                                if (!string.IsNullOrEmpty(div.InnerText))
                                {
                                    var curBson = new BsonDocument();
                                    curBson.Set("remark", div.InnerText.Trim());
                                    changeRecordList.Add(curBson);
                                }
                            }
                            if (changeRecordList.Count() > 0)
                            {
                                curUpdateBson.Set("changeRecord", changeRecordList.ToJson());
                            }


                        }
                        #endregion
                    }
                }
                else
                {
                    if (hmtl.Length >= 150)
                    {
                        curUpdateBson.Set("detailInfo", hmtl);
                    }
                    else
                    {
                        curUpdateBson.Set("detailInfo", "");
                    }
                }


                ShowMessageInfo(message);
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Update, Query = Query.EQ("guid", guid) });
            }

        }

        /// <summary>
        /// 封号几率大 且信息顺序调换
        /// </summary>
        /// <param name="args"></param>
        public void DataReceiveEnterpriseGuidByKeyWordEnhence(DataReceivedEventArgs args)
        {

            if (args.Html == "null")
            {
                return;
            }
            var content = FromUnicodeString(args.Html).Replace("<em>", "").Replace("</em>", "");
            if (string.IsNullOrEmpty(content)) return;
            try
            {

                content = "{\"result\":" + content + "}";
                JObject jsonObj = JObject.Parse(content);
                if (jsonObj == null) return;
                var cityName = cityNameStr;
                var key = GetUrlParam(args.Url, "key");
                var enterpriseList = jsonObj["result"];
                //MessageBox.Show(dataInfo["info"]["id"].ToString());
                var index = 1;
                foreach (var enterpriseObj in enterpriseList)
                {
                    var curUpdateBson = new BsonDocument();


                    var guid = enterpriseObj["KeyNo"].ToString();
                    var name = enterpriseObj["Name"].ToString();
                    var message = string.Format("详细信息{0}_{1}获取成功剩余url{2}\r", guid, name, UrlQueue.Instance.Count);

                    if (!string.IsNullOrEmpty(guid) && !existGuidList.Contains(guid) && !ExistGuid(guid))
                    {
                        curUpdateBson.Add("isLandEnterprise", "1");//匹配
                        if (index == 1)
                            curUpdateBson.Add("similar", index.ToString());//匹配
                        curUpdateBson.Add("key", key);//匹配
                        curUpdateBson.Set("guid", guid);
                        curUpdateBson.Set("name", name);
                        curUpdateBson.Set("cityName", cityName);
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Insert });
                        existGuidList.Add(guid);
                    }
                    else
                    {
                        var updateBosn = new BsonDocument().Add("isLandEnterprise", "1");//地块信息
                        var hitDistinct = cityList.Where(c => name.Contains(c.Text("name").Replace("市", "").Replace("省", ""))).ToList();
                        var hitProvince = hitDistinct.Where(c => c.Int("type") == 0).FirstOrDefault();
                        var provinceName = hitProvince != null ? hitProvince.Text("name") : "";
                        var hitCityName = hitDistinct.Where(c => c.Int("type") == 1).FirstOrDefault();
                        if (hitCityName != null)
                        {
                            var tempCityName = hitCityName.Text("name").Replace("市", "");
                            if (cityName != tempCityName)
                            {
                                updateBosn.Add("cityName", tempCityName);
                            }
                        }
                        else
                        {
                            if (provinceName == "北京" || provinceName == "上海")
                            {
                                updateBosn.Add("cityName", provinceName);
                            }
                            else
                            {
                                updateBosn.Add("provinceName", provinceName);
                            }
                        }
                        if (index == 1)
                            updateBosn.Add("similar", index.ToString());//匹配
                        updateBosn.Add("key", key);//匹配
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBosn, Query = Query.EQ("guid", guid), Name = DataTableName, Type = StorageType.Update });
                        //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("cityName", cityName),Query=Query.EQ("guid", guid), Name = DataTableName, Type = StorageType.Update });
                        message += "exists";
                    }
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isSearched", "1"), Query = Query.EQ("name", key), Name = "QCCEnterprise_XiAn", Type = StorageType.Update });
                    ShowMessageInfo(message);
                    index++;
                }
            }
            catch (Exception ex)
            {
                ShowMessageInfo(ex.Message);
            }

        }

        private void DealInfo(HtmlNode info, ref BsonDocument updateBson)
        {

            if (info == null) return;
            var table = info.SelectSingleNode("./div/div[1]/div/table");
            if (table == null) return;
            var hitTrList = table.ChildNodes.Where(c => c.Name == "tr").ToList();
            foreach (var tr in hitTrList)
            {
                var hitTdList = tr.ChildNodes.Where(c => c.Name == "td").ToList();
                var columnName = string.Empty;
                var columnValue = string.Empty;
                foreach (var td in hitTdList)
                {
                    if (string.IsNullOrEmpty(columnName))
                    {
                        var tempColumnName = td.InnerText.Replace("：", "").Trim();
                        if (EnterpriseInfoMapDic.ContainsKey(tempColumnName))
                        {
                            columnName = EnterpriseInfoMapDic[tempColumnName];
                            continue;
                        }


                    }
                    if (columnName != null && string.IsNullOrEmpty(columnValue))
                    {
                        columnValue = td.InnerText.Trim();
                        if (columnName == "oper_name")
                        {
                            columnValue = columnValue.Replace("法人对外投资", "").Replace("&nbsp;", "").Trim();
                        }
                        updateBson.Set(columnName, columnValue);
                        columnName = string.Empty;
                        columnValue = string.Empty;
                        continue;
                    }
                    columnName = string.Empty;
                    columnValue = string.Empty;


                }
            }
            //股东信息
            var shareHolder = info.SelectSingleNode("./div/div[2]/div[2]/table/tbody");
            if (shareHolder != null)
            {
                var hitHolderTrList = shareHolder.ChildNodes.Where(c => c.Name == "tr").ToList();
                var holderList = new List<BsonDocument>();
                foreach (var tr in hitHolderTrList)
                {
                    var holderBson = new BsonDocument();
                    var hitTdList = tr.ChildNodes.Where(c => c.Name == "td").ToList();
                    if (hitTdList.Count == 4)
                    {
                        holderBson.Add("type", hitTdList[0].InnerText);
                        holderBson.Add("name", hitTdList[1].InnerText);
                        holderBson.Add("Subscription", hitTdList[2].InnerText);
                        holderBson.Add("paidInCapital", hitTdList[3].InnerText);
                        holderList.Add(holderBson);
                    }

                }
                updateBson.Set("shareHolder", holderList.ToJson());
            }
            //主管
            var personUl = info.SelectSingleNode("./div/div[3]/div[2]/ul");
            var personList = new List<BsonDocument>();
            if (personUl != null)
            {

                var personDivList = personUl.ChildNodes.Where(c => c.Name == "li").ToList();
                foreach (var peronLi in personDivList)
                {
                    var holderBson = new BsonDocument();
                    var span = peronLi.ChildNodes.Where(c => c.Name == "span").ToList();
                    if (span.Count == 2)
                    {
                        var nameSpan = span[1].SelectSingleNode("./a/span");
                        if (nameSpan != null)
                        {
                            holderBson.Add("type", span[0].InnerText);
                            holderBson.Add("name", nameSpan.InnerText);
                        }
                    }
                    personList.Add(holderBson);
                }
                updateBson.Set("holder", personList.ToJson());
            }
            //分支机构需要分页
            var branchesCompanyDiv = info.SelectSingleNode("./div/div[4]/div[2]/table/tbody[1]");
            if (branchesCompanyDiv != null)
            {
                var hitHolderTrList = branchesCompanyDiv.ChildNodes.Where(c => c.Name == "tr").ToList();
                var holderList = new List<BsonDocument>();
                foreach (var tr in hitHolderTrList)
                {
                    var holderBson = new BsonDocument();
                    var hitTdList = tr.ChildNodes.Where(c => c.Name == "td").ToList();
                    if (hitTdList.Count == 4)
                    {
                        holderBson.Add("name", hitTdList[0].InnerText);
                        holderBson.Add("oper-name", hitTdList[1].InnerText);
                        holderBson.Add("reg_capi", hitTdList[2].InnerText);
                        holderBson.Add("date", hitTdList[3].InnerText);
                        holderList.Add(holderBson);
                    }
                }
                updateBson.Set("branchesCompany", holderList.ToJson());
            }
            //分页个数
            var branchesPageDiv = info.SelectSingleNode("./div/div[4]/div[2]/div");
            if (branchesPageDiv != null)
            {
                var itemCountAttr = branchesPageDiv.Attributes["data-pager-total-items"];
                var itemCount = 0;
                if (itemCountAttr != null)
                {
                    if (int.TryParse(itemCountAttr.Value, out itemCount))
                    {
                        //获取分页
                        long pageCount;
                        if (itemCount % 5 == 0)
                            pageCount = itemCount / 5;
                        else
                            pageCount = itemCount / 5 + 1;
                        updateBson.Set("branchesCompanyPageCount", pageCount.ToString());
                    }
                    if (itemCountAttr != null)
                    {
                        updateBson.Set("branchesCompanyItemCount", itemCountAttr.Value);

                    }
                }
            }

        }
        private void DealOtherInfo(HtmlNode info, ref BsonDocument updateBson, string name)
        {
            if (info != null)
            {
                updateBson.Set(name, info.InnerHtml);
            }
        }
        /// <summary>
        /// 获取企业对应guid
        /// </summary>
        /// <param name="args"></param>
        public void DataReceiveSearchGuid(DataReceivedEventArgs args)
        {

            var hmtl = args.Html;
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var curUpdateBson = new BsonDocument();
            var oldName = string.Empty;
            var queryStr = GetQueryString(args.Url);
            var oldBsonDocument = new BsonDocument();
            if (!string.IsNullOrEmpty(queryStr))
            {
                var dic = HttpUtility.ParseQueryString(queryStr);
                var serchKey = dic["key"] != null ? dic["key"].ToString() : string.Empty;
                oldName = HttpUtility.UrlDecode(serchKey);
                curUpdateBson.Add("oldName", oldName);
                var tempOldName = oldName.Replace("&NBSP;", "&nbsp;");
                // oldBsonDocument.Add("isSearched", "1");
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isSearched", "1"), Query = Query.EQ("name", oldName), Name = DataTableNameList, Type = StorageType.Update });
                ShowMessageInfo(string.Format("当前对象:{0}剩余url{1}", oldName, UrlQueue.Instance.Count));
            }

            var searchResult = htmlDoc.DocumentNode.SelectSingleNode("//a[@class='search-result-company-name']");
            if (searchResult == null) return;
            var enterpriseName = searchResult.InnerText;
            var url = searchResult.Attributes["href"] != null ? searchResult.Attributes["href"].Value : string.Empty;
            if (string.IsNullOrEmpty(url)) return;
            curUpdateBson.Add("name", enterpriseName);
            curUpdateBson.Add("url", string.Format("http://www.qichacha.com{0}", url));
            ///company/fc0de68c-acff-4e5e-9444-7ed41761c2f5
            var startIndex = url.LastIndexOf("/");
            if (startIndex == -1) return;
            var guid = url.Substring(startIndex + 1, url.Length - startIndex - 1);
            curUpdateBson.Add("guid", guid);
            //获取企业信息http://www.qichacha.com/service/getRootNodeInfoByEnterpriseId?enterpriseId=1b9df7af-e7b3-4d45-93ce-8acf02534adb&_=1466587526737
            if (!string.IsNullOrEmpty(guid) && !existGuidList.Contains(guid))
            {
                existGuidList.Add(guid);
                var message = string.Format("详细信息{0}:{1}获取成功剩余url{2}\r", guid, oldName, UrlQueue.Instance.Count);
                ShowMessageInfo(message);
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Insert });

                //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("status", "1"), Query = Query.EQ("name", enterpriseName), Name = DataTableNameList, Type = StorageType.Update });
            }
            else
            {
                ShowMessageInfo(string.Format("guid:{0}{1}已存在或者无法添加剩余url{2}\r", guid, oldName, UrlQueue.Instance.Count));

            }
            if (!string.IsNullOrEmpty(guid) && !string.IsNullOrEmpty(oldName))
            {
                oldBsonDocument.Add("guid", guid);
                oldBsonDocument.Add("searchName", enterpriseName);
                oldBsonDocument.Set("status", "1");
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = oldBsonDocument, Query = Query.EQ("name", oldName), Name = DataTableNameList, Type = StorageType.Update });
                // ShowMessageInfo(string.Format("guid:{0}{1}已存在或者无法添加剩余url{2}\r", guid, oldName, UrlQueue.Instance.Count));
            }

        }

        /// <summary>
        /// 通过关键字+分类获取企业对应guid
        /// </summary>
        /// <param name="args"></param>
        public void DataReceiveEnterpriseGuidByKeyWord(DataReceivedEventArgs args)
        {

            var cityName = cityNameStr;
            var hmtl = args.Html;
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var firstRecordCountSpan = htmlDoc.DocumentNode.SelectSingleNode("//span[@class='text-danger']");
            if (firstRecordCountSpan == null)
            {
                ShowMessageInfo("找不到firstRecordCountSpan对象" + args.Url + args.Html);
                return;
            }
            var firstRecordCount = 0;
            if (!int.TryParse(firstRecordCountSpan.InnerText.Replace("+", "").Trim(), out firstRecordCount))
            {
                ShowMessageInfo(firstRecordCountSpan.InnerText);
                return;
            }
            if (firstRecordCount == 0 && args.Depth <= Settings.MaxReTryTimes)//多尝试防止有时候出现为0情况
            {
                UrlQueue.Instance.EnQueue(new UrlInfo(args.Url) { Depth = args.Depth + Settings.MaxReTryTimes / 3 });
            }
            var industryCode = GetUrlParam(args.Url, "industryCode");
            var subIndustryCode = GetUrlParam(args.Url, "subIndustryCode");
            var province = GetUrlParam(args.Url, "province");
            var cityCode = GetUrlParam(args.Url, "cityCode");
            var urlBson = new BsonDocument();
            urlBson.Add("industryCode", industryCode);
            urlBson.Add("subIndustryCode", subIndustryCode);
            urlBson.Add("province", province);
            urlBson.Add("cityCode", cityCode);
            urlBson.Add("recordCount", firstRecordCount.ToString());
            urlBson.Add("url", args.Url);
            var hasExistUrl = ExistUrl(args.Url);
            if (firstRecordCount > 1000 && IsKeyWordUrlSplited(args, firstRecordCount, htmlDoc))
            {
                if (NEEDRECORDURL && !hasExistUrl)
                {
                    DBChangeQueue.Instance.EnQueue(new StorageData()
                    {
                        Document = urlBson.Add("isSplited", "1"),
                        Name = DataTableNameKeyWordURL,
                        Type = StorageType.Insert
                    });
                }
                ShowMessageInfo(string.Format("当前记录:{0}过大进行分支处理剩余url:{1}", firstRecordCount, UrlQueue.Instance.Count));
                return;
            }
            else
            {
                if (NEEDRECORDURL && !hasExistUrl)
                {
                    if (args.Url.EndsWith("&p=1") || !args.Url.Contains("&p="))
                    {
                        urlBson.Add("isFirstPage", "1");
                    }

                    DBChangeQueue.Instance.EnQueue(new StorageData()
                    {
                        Document = urlBson,
                        Name = DataTableNameKeyWordURL,
                        Type = StorageType.Insert
                    });
                }
            }
            var recountText = firstRecordCountSpan.InnerText;
            var searchDiv = htmlDoc.DocumentNode.SelectNodes("//tr[@class='table-search-list']");
            if (searchDiv == null)
            {
                ShowMessageInfo("找不到searchDiv对象" + args.Url + firstRecordCountSpan.InnerText + "剩余url" + UrlQueue.Instance.Count);
                return;
            }

            var keyWord = string.Empty;
            var queryStr = GetQueryString(args.Url);
            var oldBsonDocument = new BsonDocument();
            var page = string.Empty;
            #region 分页处理
            if (!string.IsNullOrEmpty(queryStr))
            {
                var dic = HttpUtility.ParseQueryString(queryStr);
                var serchKey = dic["key"] != null ? dic["key"].ToString() : string.Empty;
                keyWord = HttpUtility.UrlDecode(serchKey).Replace(cityName, "").Trim();
                //页数关键字
                page = dic["p"] != null ? dic["p"].ToString() : string.Empty;
                if (string.IsNullOrEmpty(page) || page == "1")//首页
                {    //获取分页
                    int pageCount = 1; ;
                    //获取分页
                    var pageDivList = htmlDoc.DocumentNode.SelectNodes("//ul[@class='pagination pagination-md']/li/a");
                    if (pageDivList != null)
                    {
                        var pageDiv = pageDivList.Where(c => c.Id == "ajaxpage").LastOrDefault();
                        if (pageDiv != null)
                        {
                            var pageCountStr = pageDiv.InnerText.Replace("...", "").Trim();
                            if (int.TryParse(pageCountStr, out pageCount))
                            {

                                //添加到待处理列表
                                for (var i = 2; i <= pageCount; i++)
                                {
                                    var url = string.Empty;
                                    if (args.Url.EndsWith("&p=1"))
                                    {
                                        url = args.Url.Replace("&p=1", string.Format("&p={0}", i));
                                    }
                                    else
                                    {
                                        url = string.Format("{0}&p={2}", args.Url, i);
                                    }

                                    if (!urlFilter.Contains(url))
                                    {
                                        UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });
                                    }
                                }
                            }
                        }
                    }
                    else
                    {//只有一页

                        //DBChangeQueue.Instance.EnQueue(new StorageData()
                        //{
                        //    Document = new BsonDocument().Add("execPage", "1"),
                        //    Name = DataTableCityKeyWord,
                        //    Query = Query.And(Query.EQ("cityName", cityName), Query.EQ("keyWord", keyWord)),
                        //    Type = StorageType.Update
                        //});
                    }

                    //DBChangeQueue.Instance.EnQueue(new StorageData()
                    //{
                    //    Document = new BsonDocument().Add("keyWord", keyWord).Add("cityName", cityName).Add("pageCount", pageCount).Add("execPage", "1"),
                    //    Name = DataTableCityKeyWord,
                    //    Type = StorageType.Insert
                    //});

                }
                else
                {//分页
                    //DBChangeQueue.Instance.EnQueue(new StorageData()
                    //{
                    //    Document = new BsonDocument().Add("execPage", page),
                    //    Name = DataTableCityKeyWord,
                    //    Query = Query.And(Query.EQ("cityName", cityName), Query.EQ("keyWord", keyWord)),
                    //    Type = StorageType.Update
                    //});
                }

            }
            #endregion 
            var SB = new StringBuilder();
            var existCount = 0;
            var addCount = 0;
            #region 遍历处理list
            foreach (var hitDiv in searchDiv)
            {
                var curUpdateBson = new BsonDocument();
                curUpdateBson.Add("cityName", cityName);
                var searchResult = hitDiv.SelectSingleNode("./td[@class='tp2']/span/a");
                if (searchResult == null) continue;
                var enterpriseName = searchResult.InnerText;
                var url = searchResult.Attributes["href"] != null ? searchResult.Attributes["href"].Value : string.Empty;
                if (string.IsNullOrEmpty(url)) continue;
                curUpdateBson.Add("name", enterpriseName);
                curUpdateBson.Add("url", string.Format("http://www.qichacha.com{0}", url));
                var guid = GetGuidFromUrl(url);
                if (string.IsNullOrEmpty(guid)) continue;
                curUpdateBson.Add("guid", guid);
                var oper_nameDiv = hitDiv.SelectSingleNode("./td[@class='tp2']/small[1]");
                if (oper_nameDiv != null)
                {
                    curUpdateBson.Add("oper_name", oper_nameDiv.InnerText.Replace("企业法人", "").Trim());
                }
                var telDiv = hitDiv.SelectSingleNode("./td[@class='tp2']/small[2]");
                if (telDiv != null)
                {
                    curUpdateBson.Add("telephone", telDiv.InnerText.Trim());
                }
                var domainDiv = hitDiv.SelectSingleNode("./td[@class='tp2']/small[3]");
                if (domainDiv != null)
                {
                    curUpdateBson.Add("domain", domainDiv.InnerText.Trim());
                }
                var cpiDescDiv = hitDiv.SelectSingleNode("./td[@class='tp3 text-center']");
                if (cpiDescDiv != null)
                {
                    curUpdateBson.Add("reg_capi_desc", cpiDescDiv.InnerText.Trim());
                }
                var dateDiv = hitDiv.SelectSingleNode("./td[@class='tp4 text-center']");
                if (cpiDescDiv != null)
                {
                    curUpdateBson.Add("date", dateDiv.InnerText.Trim());
                }
                var statusDiv = hitDiv.SelectSingleNode("./td[@class='tp5 text-center']");
                if (statusDiv != null)
                {
                    curUpdateBson.Add("status", statusDiv.InnerText.Trim());
                }
                if (existGuidList.Contains(guid))
                {
                    existCount++;
                }
                else
                {
                    existGuidList.Add(guid);
                    //获取企业信息http://www.qichacha.com/service/getRootNodeInfoByEnterpriseId?enterpriseId=1b9df7af-e7b3-4d45-93ce-8acf02534adb&_=1466587526737
                    if (!string.IsNullOrEmpty(guid) && !ExistGuid(guid))
                    {

                        SB.AppendFormat("获得对象{0}:{1}\r", guid, enterpriseName, UrlQueue.Instance.Count);
                        addCount++;
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Insert });
                        //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("status", "1"), Query = Query.EQ("name", enterpriseName), Name = DataTableNameList, Type = StorageType.Update });
                    }
                    else
                    {
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("cityName", cityName), Query = Query.EQ("guid", guid), Name = DataTableName, Type = StorageType.Update });
                        //ShowMessageInfo(string.Format("guid:{0}{1}已存在或者无法添加剩余url{2}\r", guid, oldName, UrlQueue.Instance.Count));
                        existCount++;
                    }
                }

            }
            #endregion
            ShowMessageInfo(string.Format("剩余url{4}当前：{5}添加:{0} 已存在:{1}当前url:{3} 详细:{2}", addCount, existCount, SB.ToString(), HttpUtility.UrlDecode(queryStr), UrlQueue.Instance.Count, firstRecordCount.ToString()));
            if (hasExistUrl && addCount > 0)//url存在 而且又有新纪录
            {
                var curUrl = args.Url;
                if (curUrl.EndsWith("&p=1"))
                {
                    curUrl = curUrl.Replace("&p=1", "&p=2");//企查查新增了企业尝试欻寻后一页
                }
                else
                {
                    var curPageIndex = 0;
                    if (int.TryParse(page, out curPageIndex))
                    {
                        var beginStr = string.Format("&p={0}", curPageIndex);
                        if (curPageIndex + 1 <= 100)
                        {
                            var endStr = string.Format("&p={0}", curPageIndex + 1);
                            curUrl = curUrl.Replace(beginStr, endStr);//企查查新增了企业尝试欻寻后一页
                            UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));
                        }
                    }

                }

                UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
            }

        }
        /// <summary>
        /// 通过判断firstRecordCount超过1000 需要进行条件过滤
        /// statusCode=&registCapiBegin=&registCapiEnd=&sortField=&isSortAsc=&province=&startDateBegin=&startDateEnd=&cityCode=&industryCode=I&subIndustryCode=63&ajaxflag=true&p=1
        /// </summary>
        /// <param name="args"></param>
        /// <param name="firstRecordCountSpan"></param>
        /// <returns></returns>
        private bool IsKeyWordUrlSplited(DataReceivedEventArgs args, int firstRecordCount, HtmlAgilityPack.HtmlDocument htmlDoc)
        {
            if (firstRecordCount <= 1000) return false;
            //registcapiNew 注册资本//startdateNew 成立日期//statuscodeNew 企业状态 industrycodeNew 行业门类
            var registCapiBeginParam = GetUrlParam(args.Url, "registCapiBegin");
            var registCapiEndParam = GetUrlParam(args.Url, "registCapiEnd");
            var statusCodeParam = GetUrlParam(args.Url, "statusCode");
            var startDateBeginParam = GetUrlParam(args.Url, "startDateBegin");
            var startDateEndParam = GetUrlParam(args.Url, "startDateEnd");
            var industryCode = GetUrlParam(args.Url, "industryCode");
            var subIndustryCode = GetUrlParam(args.Url, "subIndustryCode");
            try
            {
                #region 行业门类大类
                var industryDataValueList = GetFilterDataValue(htmlDoc, "industrycodeNew");
                if (industryDataValueList.Count() > 0 && string.IsNullOrEmpty(industryCode))
                {
                    foreach (var dataValueDic in industryDataValueList)//2016
                    {
                        var dataValue = dataValueDic.Key;
                        var curUrl = args.Url.Replace("&industryCode=", string.Format("&industryCode={0}", dataValue));
                        if (!urlFilter.Contains(curUrl))//防止重复爬取
                        {
                            UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                            urlFilter.Add(curUrl);
                        }
                    }
                    return true;

                }
                #endregion


                #region 成立日期分支筛选
                var dateDataValueList = GetFilterDataValue(htmlDoc, "startdateNew");
                if (dateDataValueList.Count() > 0 && string.IsNullOrEmpty(startDateBeginParam) && string.IsNullOrEmpty(startDateEndParam))
                {
                    foreach (var dataValueDic in dateDataValueList)//2016
                    {
                        var dataValue = dataValueDic.Key;
                        var dataCount = dataValueDic.Value;
                        if (dataCount <= 3000)
                        {
                            #region 按年分解 默认
                            var startDateBegin = string.Format("{0}0101", dataValue);
                            var startDateEnd = string.Format("{0}1231", dataValue);
                            var curUrl = args.Url.Replace("&startDateBegin=", string.Format("&startDateBegin={0}", startDateBegin));
                            curUrl = curUrl.Replace("&startDateEnd=", string.Format("&startDateEnd={0}", startDateEnd));
                            if (!urlFilter.Contains(curUrl))//防止重复爬取
                            {
                                UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                                urlFilter.Add(curUrl);
                            }
                            #endregion
                        }
                        else
                        {
                            ///此处都是大于1000
                            if (dataCount <= 12000)//按月进行分解
                            {
                                #region 按月分解
                                DateTime beginDate;
                                if (!DateTime.TryParse(string.Format("{0}-01-01", dataValue), out beginDate))
                                {
                                    continue;
                                }

                                for (var i = 1; i <= 12; i++)
                                {
                                    var startDateBegin = beginDate.ToString("yyyyMMdd");
                                    var nextMonthBegin = beginDate.AddMonths(1);
                                    var startDateEnd = nextMonthBegin.AddDays(-1).ToString("yyyyMMdd");
                                    var curUrl = args.Url.Replace("&startDateBegin=", string.Format("&startDateBegin={0}", startDateBegin));
                                    curUrl = curUrl.Replace("&startDateEnd=", string.Format("&startDateEnd={0}", startDateEnd));
                                    if (!urlFilter.Contains(curUrl))//防止重复爬取
                                    {
                                        UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                                        urlFilter.Add(curUrl);
                                    }
                                    beginDate = nextMonthBegin;
                                }
                                #endregion
                            }
                            else
                            {
                                #region 按天分解
                                DateTime beginDate;
                                if (!DateTime.TryParse(string.Format("{0}-01-01", dataValue), out beginDate))
                                {
                                    continue;
                                }

                                for (var i = 1; i <= 365; i++)
                                {

                                    var startDateBegin = beginDate.ToString("yyyyMMdd");
                                    var nextDayBegin = beginDate.AddDays(1);
                                    var startDateEnd = nextDayBegin.AddDays(-1).ToString("yyyyMMdd");
                                    var curUrl = args.Url.Replace("&startDateBegin=", string.Format("&startDateBegin={0}", startDateBegin));
                                    curUrl = curUrl.Replace("&startDateEnd=", string.Format("&startDateEnd={0}", startDateEnd));
                                    if (!urlFilter.Contains(curUrl))//防止重复爬取
                                    {
                                        UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                                        urlFilter.Add(curUrl);
                                    }
                                    beginDate = beginDate.AddDays(2);
                                    if (beginDate.Year.ToString() != "dataValue")
                                    {
                                        break;
                                    }
                                }
                                #endregion
                            }
                        }
                    }
                    return true;
                }
                #endregion


                #region 注册资本分支筛选
                var dataValueList = GetFilterDataValue(htmlDoc, "registcapiNew");
                if (dataValueList.Count() > 0 && string.IsNullOrEmpty(registCapiBeginParam) && string.IsNullOrEmpty(registCapiEndParam))
                {
                    foreach (var dataValueDic in dataValueList)
                    {
                        var dataValue = dataValueDic.Key;
                        var dataValueArray = dataValue.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                        if (dataValueArray.Length >= 2)
                        {
                            var registCapiBegin = dataValueArray[0];
                            var registCapiEnd = dataValueArray[1];
                            if (registCapiEnd == "0")
                            {
                                registCapiEnd = string.Empty;
                            }
                            var curUrl = args.Url.Replace("&registCapiBegin=", string.Format("&registCapiBegin={0}", registCapiBegin));
                            curUrl = curUrl.Replace("&registCapiEnd=", string.Format("&registCapiEnd={0}", registCapiEnd));
                            if (!urlFilter.Contains(curUrl))//防止重复爬取
                            {
                                UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                                urlFilter.Add(curUrl);
                            }
                        }
                    }
                    return true;
                }
                #endregion

                #region 企业状态筛选
                var statusDataValueList = GetFilterDataValue(htmlDoc, "statuscodeNew");
                if (statusDataValueList.Count() > 0 && string.IsNullOrEmpty(statusCodeParam))
                {
                    foreach (var dataValueDic in statusDataValueList)//2016
                    {
                        var statusCode = dataValueDic.Key;
                        var curUrl = args.Url.Replace("&statusCode=", string.Format("&statusCode={0}", statusCode));
                        if (!urlFilter.Contains(curUrl))//防止重复爬取
                        {
                            UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                            urlFilter.Add(curUrl);
                        }
                    }
                    return true;
                }
                #endregion

                #region 行业门类大类
                var subIndustryDataValueList = GetFilterDataValue(htmlDoc, "subindustrycodeNew");
                if (subIndustryDataValueList.Count() > 0 && string.IsNullOrEmpty(subIndustryCode))
                {
                    foreach (var dataValueDic in subIndustryDataValueList)//2016
                    {
                        var dataValue = dataValueDic.Key;
                        var curUrl = args.Url.Replace("&subIndustryCode=", string.Format("&subIndustryCode={0}", dataValue));
                        if (!urlFilter.Contains(curUrl))//防止重复爬取
                        {
                            UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                            urlFilter.Add(curUrl);
                        }
                    }
                    return true;

                }
                #endregion
            }
            catch (Exception ex)
            {

            }
            return false;
        }
        private List<string> curAddUrlList = new List<string>();

        /// <summary>
        /// 添加修正当个数大于=5000的时候进行url笛卡儿增加url 减少url的访问个数
        /// </summary>
        /// <param name="args"></param>
        /// <param name="firstRecordCount"></param>
        /// <param name="htmlDoc"></param>
        /// <returns></returns>
        private bool IsKeyWordUrlSplited_APP(DataReceivedEventArgs args, int firstRecordCount, JToken htmlDoc)
        {

            if (firstRecordCount < 40) return false;
             //registcapiNew 注册资本//startdateNew 成立日期//statuscodeNew 企业状态 industrycodeNew 行业门类
            var registCapiBeginParam = GetUrlParam(args.Url, "registCapiBegin");
            var registCapiEndParam = GetUrlParam(args.Url, "registCapiEnd");
            var statusCodeParam = GetUrlParam(args.Url, "statusCode");
            var startDateBeginParam = GetUrlParam(args.Url, "startDateBegin");
            var startDateEndParam = GetUrlParam(args.Url, "startDateEnd");
            var industryCode = GetUrlParam(args.Url, "industryCode");
            var subIndustryCode = GetUrlParam(args.Url, "subIndustryCode");
            try
            {
                #region 行业门类大类 可能导致超出url 计算 需要contains去除
                var industryDataValueList = GetFilterDataValue_App(htmlDoc, "industrycode");
                if (industryDataValueList.Count() > 0 && string.IsNullOrEmpty(industryCode))
                {
                    foreach (var dataValueDic in industryDataValueList)//2016
                    {
                        var dataValue = dataValueDic.Key;
                        var curUrl = args.Url.Replace("&industryCode=", string.Format("&industryCode={0}", dataValue));
                        if (!urlFilter.Contains(curUrl))//防止重复爬取
                        {
                            UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                            urlFilter.Add(curUrl);
                            curAddUrlList.Add(curUrl);
                        }
                    }
                    return true;

                }

                #endregion
                #region 成立日期分支筛选
                var dateDataValueList = GetFilterDataValue_App(htmlDoc, "startdateyear", firstRecordCount);
                if (dateDataValueList.Count() > 0 && string.IsNullOrEmpty(startDateBeginParam) && string.IsNullOrEmpty(startDateEndParam))
                {
                    foreach (var dataValueDic in dateDataValueList)//2016
                    {
                        var dataValue = dataValueDic.Key;
                        var dataCount = dataValueDic.Value;
                        //var startDateBegin = string.Format("{0}0101", dataValue);
                        //var startDateEnd = string.Format("{0}1231", dataValue);
                        //if (dataValue == "0")
                        //{
                        //    startDateBegin = "0";
                        //    startDateEnd = "0";
                        //}
                        if (!args.Url.Contains("startDateBegin"))
                        {
                            args.Url += "&startDateBegin=";
                        }
                        if (!args.Url.Contains("startDateEnd"))
                        {
                            args.Url += "&startDateEnd=";
                        }

                        if (dataCount <= 3000 || dataValue == "0")//20个
                        {
                            #region 按年分解 默认
                            var startDateBegin = string.Format("{0}0101", dataValue);
                            var startDateEnd = string.Format("{0}1231", dataValue);
                            if (dataValue == "0")
                            {
                                startDateBegin = "0";
                                startDateEnd = "0";
                            }
                            var curUrl = args.Url.Replace("&startDateBegin=", string.Format("&startDateBegin={0}", startDateBegin));
                            curUrl = curUrl.Replace("&startDateEnd=", string.Format("&startDateEnd={0}", startDateEnd));
                            if (!urlFilter.Contains(curUrl))//防止重复爬取
                            {
                                UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                                urlFilter.Add(curUrl);
                                curAddUrlList.Add(curUrl);
                            }
                            #endregion
                        }
                        else
                        {
                            ///此处都是大于3000
                            if (dataCount < 5000)//按月进行分解
                            {
                                #region 按月分解
                                DateTime beginDate;
                                if (!DateTime.TryParse(string.Format("{0}-01-01", dataValue), out beginDate))
                                {
                                    continue;
                                }

                                for (var i = 1; i <= 12; i++)
                                {
                                    var startDateBegin = beginDate.ToString("yyyyMMdd");
                                    var nextMonthBegin = beginDate.AddMonths(1);
                                    var startDateEnd = nextMonthBegin.AddDays(-1).ToString("yyyyMMdd");
                                    var curUrl = args.Url.Replace("&startDateBegin=", string.Format("&startDateBegin={0}", startDateBegin));
                                    curUrl = curUrl.Replace("&startDateEnd=", string.Format("&startDateEnd={0}", startDateEnd));


                                    if (!urlFilter.Contains(curUrl))//防止重复爬取
                                    {
                                        UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                                        urlFilter.Add(curUrl);
                                        curAddUrlList.Add(curUrl);
                                    }

                                    beginDate = nextMonthBegin;
                                }
                                #endregion

                            }
                            else
                            {
                                #region 按天分解
                                DateTime beginDate;
                                if (!DateTime.TryParse(string.Format("{0}-01-01", dataValue), out beginDate))
                                {
                                    continue;
                                }

                                for (var i = 1; i <= 366; i++)
                                {

                                    var startDateBegin = beginDate.ToString("yyyyMMdd");
                                    var nextDayBegin = beginDate.AddDays(1);
                                    var startDateEnd = nextDayBegin.AddDays(-1).ToString("yyyyMMdd");
                                    var curUrl = args.Url.Replace("&startDateBegin=", string.Format("&startDateBegin={0}", startDateBegin));
                                    curUrl = curUrl.Replace("&startDateEnd=", string.Format("&startDateEnd={0}", startDateEnd));
                                    if (!urlFilter.Contains(curUrl))//防止重复爬取
                                    {
                                        UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                                        urlFilter.Add(curUrl);
                                        curAddUrlList.Add(curUrl);
                                    }

                                    beginDate = beginDate.AddDays(2);
                                    if (beginDate.Year.ToString() != dataValue)
                                    {
                                        break;
                                    }
                                }
                                #endregion
                            }
                        }

                        //var curUrl = args.Url.Replace("&startDateBegin=", string.Format("&startDateBegin={0}", startDateBegin));
                        //curUrl = curUrl.Replace("&startDateEnd=", string.Format("&startDateEnd={0}", startDateEnd));
                        //if (dataCount >= 500 && (firstRecordCount / dateDataValueList.Count()) >= 500)//继续往下分解
                        //{
                        //    var curArg = new DataReceivedEventArgs() { Url = curUrl };
                        //    return IsKeyWordUrlSplited_APP(curArg, dataCount, htmlDoc);
                        //}
                        //else
                        //{
                        //    if (!urlFilter.Contains(curUrl))//防止重复爬取
                        //    {
                        //        UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                        //        urlFilter.Add(curUrl);
                        //    }
                        //}
                    }
                    return true;
                }
                #endregion

                #region 注册资本分支筛选
                var dataValueList = GetFilterDataValue_App(htmlDoc, "registcapi", firstRecordCount);
                if (dataValueList.Count() > 0 && string.IsNullOrEmpty(registCapiBeginParam) && string.IsNullOrEmpty(registCapiEndParam))
                {
                    foreach (var dataValueDic in dataValueList)
                    {

                        var dataValue = dataValueDic.Key;
                        var dataValueArray = dataValue.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                        if (dataValueArray.Length >= 2)
                        {
                            var registCapiBegin = dataValueArray[0];
                            var registCapiEnd = dataValueArray[1];
                            if (registCapiBegin == "0")
                            {
                                registCapiBegin = string.Empty;
                            }
                            if (registCapiEnd == "0")
                            {
                                registCapiEnd = string.Empty;
                            }
                            ///2个都为空情况防止无限循环
                            if (string.IsNullOrEmpty(registCapiBegin) && string.IsNullOrEmpty(registCapiEnd))
                            {
                                continue;
                            }
                            if (!args.Url.Contains("registCapiBegin"))
                            {
                                args.Url += "&registCapiBegin=";
                            }
                            if (!args.Url.Contains("registCapiEnd"))
                            {
                                args.Url += "&registCapiEnd=";
                            }
                            var curUrl = args.Url;

                            curUrl = curUrl.Replace("&registCapiBegin=", string.Format("&registCapiBegin={0}", registCapiBegin));
                            curUrl = curUrl.Replace("&registCapiEnd=", string.Format("&registCapiEnd={0}", registCapiEnd));
                            if (!urlFilter.Contains(curUrl))//防止重复爬取
                            {
                                UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                                urlFilter.Add(curUrl);
                                curAddUrlList.Add(curUrl);
                            }
                        }
                    }
                    return true;
                }
                #endregion
                #region 企业状态筛选 无法使用筛选
                //var statusDataValueList = GetFilterDataValue_App(htmlDoc, "statuscode", firstRecordCount);
                //if (statusDataValueList.Count() > 0 && string.IsNullOrEmpty(statusCodeParam))
                //{
                //    foreach (var statusCodeDic in statusDataValueList)//2016
                //    {
                //        var statusCode = statusCodeDic.Key;
                //        var dataCount= statusCodeDic.Value;
                //        if (!args.Url.Contains("statusCode"))
                //        {
                //            args.Url += "&statusCode=";
                //        }
                //        var curUrl = args.Url.Replace("&statusCode=", string.Format("&statusCode={0}", statusCode));
                //        if (dataCount >= 500 && (firstRecordCount / statusDataValueList.Count()) >= 500)//继续往下分解
                //        {
                //            var curArg = new DataReceivedEventArgs() { Url = curUrl };
                //            return IsKeyWordUrlSplited_APP(curArg, dataCount, htmlDoc);
                //        }
                //        else
                //        {
                //            if (!urlFilter.Contains(curUrl))//防止重复爬取
                //            {
                //                UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                //                urlFilter.Add(curUrl);
                //            }
                //        }
                //    }
                //    return true;
                //}
                #endregion

            }
            catch (Exception ex)
            {

            }
            ///此处有个情况 当 2016-08-21中有46条数据，而次url为 -2016-08-01 2016-08-30 此处需要读取超级多次，2倍后10次内需要重复读取，而只要随意换算条件即可正确算出url
            ///好的读取顺序可能会导致减少url的读取防止账号浪费
            ///再次分解时间

            var searchKey = GetUrlParam(args.Url, "searchKey");
            var hitSearchKeyConditionObj = searchKeyConditonList.Where(c => c.searchKey == searchKey).FirstOrDefault();
            if (hitSearchKeyConditionObj == null)
            {
                searchKeyConditonList.Add(new SearchKeyCondition() { recordCount = firstRecordCount, searchKey = searchKey, mode = urlSplitMode });
            }
            else
            {

                if (hitSearchKeyConditionObj.recordCount == firstRecordCount)
                {
                    hitSearchKeyConditionObj.hitCount = hitSearchKeyConditionObj.hitCount + 1;
                }
                else
                {
                    hitSearchKeyConditionObj.recordCount = firstRecordCount;
                }
            }
            var _urlSplitMode = urlSplitMode;
            if (hitSearchKeyConditionObj != null)
            {
                _urlSplitMode = hitSearchKeyConditionObj.mode;//默认
                if (hitSearchKeyConditionObj.hitCount >= 3)//该模式下连续个数命中个数，表示需要切换分解模式
                {
                    _urlSplitMode = GetNextSplitMode(hitSearchKeyConditionObj.mode);
                }
            }
            try
            {
                var splitStatus = SplitModeActive(_urlSplitMode, args.Url, startDateBeginParam, startDateEndParam, registCapiBeginParam, registCapiEndParam, subIndustryCode, htmlDoc);
                if (splitStatus == true) { return splitStatus; }
            }
            catch (Exception ex)
            {
                throw new Exception("SplitModeActive" + ex.Message);
            }
            //表示不用vip不能获取 通过升序降序来获得尽可能全的列表
            //sortField = startdate & isSortAsc = true 升序
            //sortField = startdate & isSortAsc = false 降序
            //sortField = registcapi & isSortAsc = true 注册资本
            //sortField = registcapi & isSortAsc = false 注册资本
            //var registCapiBeginParam = GetUrlParam(args.Url, "registCapiBegin");
            //var registCapiEndParam = GetUrlParam(args.Url, "registCapiEnd");
            //var statusCodeParam = GetUrlParam(args.Url, "statusCode");
            //var startDateBeginParam = GetUrlParam(args.Url, "startDateBegin");
            //var startDateEndParam = GetUrlParam(args.Url, "startDateEnd");
            //var industryCode = GetUrlParam(args.Url, "industryCode");
            //var subIndustryCode = GetUrlParam(args.Url, "subIndustryCode");
            ///如何防止重复循环获取
            if ((registCapiBeginParam == registCapiEndParam || (registCapiBeginParam == "" || registCapiEndParam == "" || registCapiEndParam == "50000")) && startDateBeginParam == startDateEndParam)
            {
                //遵循先升序后降序
                var sortFieldParam = GetUrlParam(args.Url, "sortField");//排序规则
                var isSortAscParam = GetUrlParam(args.Url, "isSortAsc");//降序规则
                if (string.IsNullOrEmpty(sortFieldParam) && (isSortAscParam == "false" || string.IsNullOrEmpty(isSortAscParam)))//默认状态
                {
                    ShowMessageInfo("record数量过大开始进行调整数据爬去" + firstRecordCount.ToString());
                    ///修改整体排序方式先将url还原会最原始情况，然后修改各种排序方式
                    var _curUrl = args.Url;
                    _curUrl = ReplaceUrlParam(_curUrl, "registCapiBegin", registCapiBeginParam, "");
                    _curUrl = ReplaceUrlParam(_curUrl, "registCapiEnd", registCapiEndParam, "");
                    _curUrl = ReplaceUrlParam(_curUrl, "startDateBegin", startDateBeginParam, "");
                    _curUrl = ReplaceUrlParam(_curUrl, "startDateEnd", startDateEndParam, "");
                    _curUrl = ReplaceUrlParam(_curUrl, "subIndustryCode", subIndustryCode, "");
                    //此处为_curUrl初始化为urlsortField="" isSortAscParam="false" 然后尝试不同情况进行排序
                    var changeSort = "true";
                    if (isSortAscParam == "true")
                    {
                        changeSort = "false";
                    }
                    else
                    {
                        changeSort = "true";
                    }
                    var sortAscUrl = ReplaceUrlParam(_curUrl, "isSortAsc", isSortAscParam, changeSort);//整体正序
                    var startdateSortAscUrl = ReplaceUrlParam(_curUrl, "sortField", sortFieldParam, "startdate");//时间逆序
                    var startdateSortDescUrl = ReplaceUrlParam(startdateSortAscUrl, "isSortAsc", isSortAscParam, changeSort);
                    var registcapSortAscUrl = ReplaceUrlParam(_curUrl, "sortField", sortFieldParam, "startdate");//时间逆序
                    var registcapSortDescUrl = ReplaceUrlParam(registcapSortAscUrl, "isSortAsc", isSortAscParam, changeSort);

                    AddUrlQueue(sortAscUrl);
                    //AddUrlQueue(registcapSortDescUrl);
                    //AddUrlQueue(startdateSortAscUrl); 
                    //AddUrlQueue(startdateSortDescUrl);
                    //AddUrlQueue(registcapSortAscUrl);
                    curAddUrlList.Add(sortAscUrl);
                    //curAddUrlList.Add(registcapSortDescUrl);
                    //curAddUrlList.Add(startdateSortAscUrl);
                    //curAddUrlList.Add(startdateSortDescUrl);
                    //curAddUrlList.Add(registcapSortAscUrl);
                    return true;
                }
                return false;
            }



            return true;
        }
        #region 分解模式

        private UrlSpliteMode GetNextSplitMode(UrlSpliteMode oldSplitMode)
        {
            switch (oldSplitMode)
            {
                case UrlSpliteMode.DateFirst://日期优先
                    return UrlSpliteMode.RecpiFirst;
                case UrlSpliteMode.RecpiFirst://日期优先
                    return UrlSpliteMode.SubFacortyFist;
                case UrlSpliteMode.SubFacortyFist://日期优先
                    return UrlSpliteMode.DateFirst;

            }
            return UrlSpliteMode.DateFirst;
        }
        /// <summary>
        /// 随机调用分解模式
        /// </summary>
        /// <returns></returns>
        private bool SplitModeActive(UrlSpliteMode _urlSplitMode, string argUrl, string startDateBeginParam, string startDateEndParam, string registCapiBeginParam, string registCapiEndParam, string subIndustryCode, JToken htmlDoc)
        {
            bool dateSplitStatus;
            bool capiSplitStatus;
            bool subFactorySplitStatus;
            //考虑每个关键字有一定的分解模式，当某个关键字模式连续3次个数一样，则切换其他分解模式
            switch (_urlSplitMode) {
                case UrlSpliteMode.DateFirst://日期优先
                    dateSplitStatus = SplitUrlByDateAgain(argUrl, startDateBeginParam, startDateEndParam);
                    if (dateSplitStatus == true) return dateSplitStatus;

                    ///再次分解注册资金
                    capiSplitStatus = SplitUrlByRegCapiAgain(argUrl, registCapiBeginParam, registCapiEndParam);
                    if (capiSplitStatus == true) return capiSplitStatus;

                    ///再次分解子类
                    subFactorySplitStatus = SplitUrlBySubFactory(argUrl, subIndustryCode, htmlDoc);
                    if (subFactorySplitStatus == true) return subFactorySplitStatus;
                    break;
                case UrlSpliteMode.RecpiFirst://注册资金优先
                                              ///再次分解注册资金
                    capiSplitStatus = SplitUrlByRegCapiAgain(argUrl, registCapiBeginParam, registCapiEndParam);
                    if (capiSplitStatus == true) return capiSplitStatus;
                    ///再次分解子类
                    subFactorySplitStatus = SplitUrlBySubFactory(argUrl, subIndustryCode, htmlDoc);
                    if (subFactorySplitStatus == true) return subFactorySplitStatus;
                    dateSplitStatus = SplitUrlByDateAgain(argUrl, startDateBeginParam, startDateEndParam);
                    if (dateSplitStatus == true) return dateSplitStatus;

                    break;
                case UrlSpliteMode.SubFacortyFist://注册资金优先  ///再次分解注册资金

                    subFactorySplitStatus = SplitUrlBySubFactory(argUrl, subIndustryCode, htmlDoc);
                    if (subFactorySplitStatus == true) return subFactorySplitStatus;

                    ///再次分解子类
                    dateSplitStatus = SplitUrlByDateAgain(argUrl, startDateBeginParam, startDateEndParam);
                    if (dateSplitStatus == true) return dateSplitStatus;
                    capiSplitStatus = SplitUrlByRegCapiAgain(argUrl, registCapiBeginParam, registCapiEndParam);
                    if (capiSplitStatus == true) return capiSplitStatus;

                    break;
            }
            return false;
        }

        #endregion

        /// <summary>
        /// url再次分解时间
        /// </summary>
        /// <param name="argUrl"></param>
        /// <param name="startDateBeginParam"></param>
        /// <param name="startDateEndParam"></param>
        /// <returns></returns>
        private bool SplitUrlByDateAgain(string argUrl, string startDateBeginParam, string startDateEndParam)
        {
            try
            {
                #region 此处再次分解时间
                //var startDateBeginParam = GetUrlParam(args.Url, "startDateBegin");
                //var startDateEndParam = GetUrlParam(args.Url, "startDateEnd");
                if (!string.IsNullOrEmpty(startDateBeginParam) && startDateBeginParam != "0")
                {
                    DateTime _startDateBegin; DateTime _startDateEnd;

                    _startDateBegin = DateTime.ParseExact(startDateBeginParam, "yyyyMMdd", null);
                    _startDateEnd = DateTime.ParseExact(startDateEndParam, "yyyyMMdd", null);

                    if (_startDateBegin != null && _startDateEnd != null)
                    {

                        var CountSpan = (_startDateEnd - _startDateBegin).Days;
                        // var RecordPage = firstRecordCount / 40;// 还需要几条url 100条？
                        //if (CountSpan <= 0) return false;    
                        var addPlus = CountSpan / 2;
                        if (addPlus <= 0)
                        {
                            addPlus = 1;
                        }
                        var hasAdd = false;
                        //差距在10天内的直接遍历所有天数 减少url读取
                        if (CountSpan > 0 && CountSpan <= 5)//差距只有1 5-6情况
                        {
                            if (CountSpan == 1)
                            {
                                var _BegincurUrl = argUrl;
                                // _BegincurUrl = ReplaceUrlParam(_BegincurUrl, "startDateBegin", , "");
                                _BegincurUrl = ReplaceUrlParam(_BegincurUrl, "startDateEnd", startDateEndParam, startDateBeginParam);
                                AddUrlQueue(_BegincurUrl);
                                curAddUrlList.Add(_BegincurUrl);
                                var _endCurUrl = argUrl;
                                _endCurUrl = ReplaceUrlParam(_endCurUrl, "startDateBegin", startDateBeginParam, startDateEndParam);
                                AddUrlQueue(_endCurUrl);
                                curAddUrlList.Add(_endCurUrl);
                                hasAdd = true;
                            }
                            else
                            {
                                ///时间只能坐落在具体日期上
                                for (var tempDate = _startDateBegin; tempDate <= _startDateEnd; tempDate = tempDate.AddDays(1))
                                {

                                    var _curUrl = argUrl;
                                    _curUrl = _curUrl.Replace("&startDateBegin=" + startDateBeginParam, string.Format("&startDateBegin={0}", tempDate.ToString("yyyyMMdd")));
                                    _curUrl = _curUrl.Replace("&startDateEnd=" + startDateEndParam, string.Format("&startDateEnd={0}", tempDate.ToString("yyyyMMdd")));
                                    if (!urlFilter.Contains(_curUrl))//防止重复爬取
                                    {
                                        UrlQueue.Instance.EnQueue(new UrlInfo(_curUrl));//添加条件过滤
                                        urlFilter.Add(_curUrl);
                                        curAddUrlList.Add(_curUrl);
                                        if (!hasAdd)
                                        {
                                            hasAdd = true;
                                        }
                                    }
                                }

                            }
                        }
                        else
                        {
                            while (_startDateBegin <= _startDateEnd)//50-59
                            {

                                var tempCapEnd = _startDateBegin.AddDays(addPlus);
                                if (tempCapEnd > _startDateEnd)
                                {
                                    tempCapEnd = _startDateEnd;
                                }
                                var _curUrl = argUrl;
                                _curUrl = _curUrl.Replace("&startDateBegin=" + startDateBeginParam, string.Format("&startDateBegin={0}", _startDateBegin.ToString("yyyyMMdd")));
                                _curUrl = _curUrl.Replace("&startDateEnd=" + startDateEndParam, string.Format("&startDateEnd={0}", tempCapEnd.ToString("yyyyMMdd")));
                                if (!urlFilter.Contains(_curUrl))//防止重复爬取
                                {
                                    UrlQueue.Instance.EnQueue(new UrlInfo(_curUrl));//添加条件过滤
                                    urlFilter.Add(_curUrl);
                                    curAddUrlList.Add(_curUrl);
                                    if (!hasAdd)
                                    {
                                        hasAdd = true;
                                    }
                                }
                                _startDateBegin = tempCapEnd.AddDays(addPlus);

                            }
                        }

                        if (hasAdd)//此处不想等时候扔不等于已经有url添加了此处需要跳出
                        {
                            return true;
                        }

                    }
                }
                #endregion
            }
            catch (Exception ex)
            {

            }
            return false;
        }

        /// <summary>
        ///  url再次分解注册资金
        /// </summary>
        /// <param name="argUrl"></param>
        /// <param name="registCapiBeginParam"></param>
        /// <param name="registCapiEndParam"></param>
        /// <returns></returns>
        private bool SplitUrlByRegCapiAgain(string argUrl, string registCapiBeginParam, string registCapiEndParam)
        {
            try
            {
                #region 将registCapiBeginParam 继续分解 此处可能firstRecordCount还有3000左右 
                //此处需要注意传入0-1 5000-0的情况如何继续分解
                var _registCapiBegin = 0;
                var _registCapiEnd = 0;
                if (string.IsNullOrEmpty(registCapiBeginParam))//0-1
                {
                    registCapiBeginParam = "0";
                }
                if (string.IsNullOrEmpty(registCapiEndParam))
                {
                    registCapiEndParam = "50000";//5亿 10倍
                }
                if (!int.TryParse(registCapiBeginParam, out _registCapiBegin))
                {
                    //return true;
                }
                if (!int.TryParse(registCapiEndParam, out _registCapiEnd))
                {
                    //return true;
                }
                var countSpan = _registCapiEnd - _registCapiBegin;
                //var recordPage = firstRecordCount / 40;// 还需要几条url 100条？
                //if (countSpan <= 0) return true;
                var addPlus = countSpan / 2;
                if (addPlus <= 0)
                {
                    addPlus = 1;
                }
                var hasAdd = false;
                if (countSpan <= 5)//差距只有1 5-6情况 0-1 需要防止为空的情况
                {
                    if (countSpan <= 1)
                    {
                        if (countSpan <= 0) return true;
                        var _BegincurUrl = argUrl;
                        // _BegincurUrl = ReplaceUrlParam(_BegincurUrl, "startDateBegin", , "");
                        if (registCapiBeginParam != "")
                        {
                            _BegincurUrl = ReplaceUrlParam(_BegincurUrl, "registCapiEnd", registCapiEndParam, registCapiBeginParam);
                            AddUrlQueue(_BegincurUrl);
                            curAddUrlList.Add(_BegincurUrl);
                            hasAdd = true;
                        }

                        if (registCapiEndParam != "")
                        {
                            var _endCurUrl = argUrl;
                            _endCurUrl = ReplaceUrlParam(_endCurUrl, "registCapiBegin", registCapiBeginParam, registCapiEndParam);
                            AddUrlQueue(_endCurUrl);
                            curAddUrlList.Add(_endCurUrl);
                            hasAdd = true;
                        }
                    }
                    else
                    {
                        //6-8= 6-7 7-8  需要 6,7,8 需要进行验证
                        for (var tempCapi = _registCapiBegin; tempCapi <= _registCapiEnd; tempCapi++)
                        {


                            var _curUrl = argUrl;
                            _curUrl = _curUrl.Replace("&registCapiBegin=" + registCapiBeginParam, string.Format("&registCapiBegin={0}", tempCapi));
                            _curUrl = _curUrl.Replace("&registCapiEnd=" + registCapiEndParam, string.Format("&registCapiEnd={0}", tempCapi));
                            if (!urlFilter.Contains(_curUrl))//防止重复爬取
                            {
                                UrlQueue.Instance.EnQueue(new UrlInfo(_curUrl));//添加条件过滤
                                urlFilter.Add(_curUrl);
                                curAddUrlList.Add(_curUrl);
                                if (!hasAdd)
                                {
                                    hasAdd = true;
                                }
                            }
                        }
                    }

                }
                else
                {
                    while (_registCapiBegin <= _registCapiEnd)//50-59 0-9  5000-0
                    {

                        var tempCapEnd = _registCapiBegin + addPlus;
                        if (tempCapEnd > _registCapiEnd)
                        {
                            tempCapEnd = _registCapiEnd;
                        }
                        var _curUrl = argUrl;
                        _curUrl = _curUrl.Replace("&registCapiBegin=" + registCapiBeginParam, string.Format("&registCapiBegin={0}", _registCapiBegin));
                        _curUrl = _curUrl.Replace("&registCapiEnd=" + registCapiEndParam, string.Format("&registCapiEnd={0}", tempCapEnd));
                        if (!urlFilter.Contains(_curUrl))//防止重复爬取
                        {
                            UrlQueue.Instance.EnQueue(new UrlInfo(_curUrl));//添加条件过滤
                            urlFilter.Add(_curUrl);
                            curAddUrlList.Add(_curUrl);
                            if (!hasAdd)
                            {
                                hasAdd = true;
                            }
                        }
                        _registCapiBegin = tempCapEnd + addPlus;

                    }
                }
                #endregion

                if (hasAdd)//此处不想等时候扔不等于已经有url添加了此处需要跳出
                {
                    return true;
                }

            }
            catch (Exception ex)
            {

            }
            return false;
        }

        /// <summary>
        /// 按照子类分解，可能出现销售下面子类过多的问题比如一下子31个导致url过多
        /// </summary>
        /// <param name="argUrl"></param>
        /// <param name="subIndustryCode"></param>
        /// <param name="htmlDoc"></param>
        /// <returns></returns>
        private bool SplitUrlBySubFactory(string argUrl, string subIndustryCode, JToken htmlDoc)
        {
            #region 行业门类子类
            var subIndustryDataValueList = GetFilterDataValue_App(htmlDoc, "subindustrycode");
            if (subIndustryDataValueList.Count() > 0 && string.IsNullOrEmpty(subIndustryCode))
            {
                foreach (var dataValueDic in subIndustryDataValueList)//2016
                {
                    var dataValue = dataValueDic.Key;
                    var dataCount = dataValueDic.Value;
                    if (!argUrl.Contains("subIndustryCode"))
                    {
                        argUrl += "&subIndustryCode=";
                    }
                    var curUrl = argUrl.Replace("&subIndustryCode=", string.Format("&subIndustryCode={0}", dataValue));

                    if (!urlFilter.Contains(curUrl))//防止重复爬取
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                        urlFilter.Add(curUrl);
                        curAddUrlList.Add(curUrl);
                    }

                }
                return true;

            }
            return false;
            #endregion
        }
        /// <summary>
        /// url参数替换替换
        /// </summary>
        /// <param name="_curUrl"></param>
        /// <returns></returns>
        private string ReplaceUrlParam(string _curUrl, string parameName, string oldValue, string newValue)
        {
            var oldOne = string.Format("&{0}={1}", parameName, oldValue);
            var newOne = string.Format("&{0}={1}", parameName, newValue);
            if (!string.IsNullOrEmpty(oldOne))
            {
                var newUrl = _curUrl.Replace(oldOne, newOne);
                return newUrl;
            }
            else
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// 添加爬取队列
        /// </summary>
        /// <param name="_curUrl"></param>
        /// <returns></returns>
        private bool AddUrlQueue(string _curUrl)
        {
            if (!urlFilter.Contains(_curUrl))//防止重复爬取
            {
                UrlQueue.Instance.EnQueue(new UrlInfo(_curUrl));//添加条件过滤
                urlFilter.Add(_curUrl);
                return true;
            }
            return false;
        }
        /// <summary>
        /// 获取分支条件,模拟资本与 增加注册资本与时间细化
        /// </summary>
        /// <param name="htmlDoc"></param>
        /// <returns></returns>
        private Dictionary<string, int> GetFilterDataValue(HtmlAgilityPack.HtmlDocument htmlDoc, string name)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            var registcapiNewDl = htmlDoc.GetElementbyId(name);
            if (registcapiNewDl != null)
            {
                var aNodeList = registcapiNewDl.SelectNodes("./dd/a");
                if (aNodeList == null)
                    aNodeList = registcapiNewDl.SelectNodes("./div/dd/a");
                if (aNodeList == null) return result;

                foreach (var aNode in aNodeList)
                {
                    if (aNode.Attributes["data-value"] == null) continue;
                    var dataValue = aNode.Attributes["data-value"].Value;
                    var dataCount = GetSearchKeyConditionRecordCount(aNode.InnerText);

                    if (!string.IsNullOrEmpty(dataValue))//1-499
                    {
                        result.Add(dataValue, dataCount);
                    }
                }
            }
            return result;
        }
        /// <summary>
        ///  租赁和商务服务业&nbsp;(49953),获取49953这个个数
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public int GetSearchKeyConditionRecordCount(string str)
        {
            var recordCountStr = Toolslib.Str.Sub(str, "(", ")");
            var recordCount = 0;
            if (int.TryParse(recordCountStr, out recordCount))
            {
                return recordCount;
            }
            return 0;
        }

        private Dictionary<string, int> GetFilterDataValue_App(JToken groupItems, string name, int firstRecordCount = 0)
        {

            // List<string> result = new List<string>();
            Dictionary<string, int> result = new Dictionary<string, int>();
            {

                try
                {
                    switch (name)
                    {
                        case "registcapi":

                            #region  注册资本初始化
                            if (firstRecordCount >= 10000)
                            {
                                result.Add("0-1", 1);
                                result.Add("1-2", 1);
                                result.Add("3-5", 1);
                                result.Add("5-9", 1);
                                result.Add("10-19", 1);
                                result.Add("20-29", 1);
                                result.Add("30-39", 1);
                                result.Add("40-49", 1);
                                result.Add("50-99", 1);
                                result.Add("100-199", 1);
                                result.Add("200-299", 1);
                                result.Add("300-399", 1);
                                result.Add("400-499", 1);
                                result.Add("500-599", 1);
                                result.Add("600-699", 1);
                                result.Add("700-799", 1);
                                result.Add("700-899", 1);
                                result.Add("900-999", 1);
                                result.Add("1000-1999", 1);
                                result.Add("2000-2999", 1);
                                result.Add("3000-3999", 1);
                                result.Add("4000-4999", 1);
                                result.Add("5000-0", 1);
                            }
                            else
                            {
                                result.Add("0-1", 1);
                                result.Add("2-9", 1);
                                result.Add("10-99", 1);
                                result.Add("100-499", 1);
                                result.Add("500-999", 1);
                                result.Add("1000-4999", 1);
                                result.Add("5000-0", 1);
                            }
                            #endregion
                            break;
                        case "subindustrycode"://从数据库中读取子类

                            if (allSubFactoryList.Count <= 0)
                            {
                                var typeList = dataop.FindAll(DataTableIndustry).ToList();//遍历所有的父分类与子分类
                                allSubFactoryList = typeList.Where(c => c.Int("type") == 1).ToList();//获取子类
                            }

                            var hitParentGourp = groupItems.Where(c => c["Key"].ToString() == "industrycode").FirstOrDefault();
                            if (hitParentGourp != null)
                            {
                                if (hitParentGourp["Items"].Count() > 0)
                                {
                                    var hitParent = hitParentGourp["Items"].FirstOrDefault();
                                    var hitSubFactoryDic = allSubFactoryList.Where(c => c.Text("parentCode") == hitParent["Value"].ToString()).Select(c => c.Text("code")).ToDictionary(c => c, d => 1);
                                    return hitSubFactoryDic;
                                }
                            }


                            break;
                        default:

                            var hitGourp = groupItems.Where(c => c["Key"].ToString() == name).FirstOrDefault();
                            if (hitGourp != null)
                            {
                                foreach (var item in hitGourp["Items"])
                                {
                                    var curCount = 0;
                                    if (int.TryParse(item["Count"].ToString(), out curCount))
                                    {
                                        if (item["Count"].ToString() != "0")
                                        {
                                            result.Add(item["Value"].ToString(), curCount);
                                        }
                                    }
                                }

                            }

                            break;

                    }
                }
                catch (Exception ex)
                {
                    ShowMessageInfo("12");
                }
                return result;
            }
        }


        /// <summary>
        ///  获取国民经济分类下的企业列表
        /// </summary>
        /// <param name="args"></param>
        public void DataReceiveSearchGuidByType(DataReceivedEventArgs args)
        {
            int addCount = 0, updateCount = 0, existCount = 0;
            //var sb = new StringBuilder();
            var hmtl = args.Html;
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(args.Html);

            var oldName = string.Empty;
            var queryStr = GetQueryString(args.Url);
            var typeCode = GetSearchUrlCode(args.Url);

            var searchResult = htmlDoc.DocumentNode.SelectNodes("//section[@class='panel panel-default']");
            if (searchResult == null) return;

            foreach (var enterpriseNode in searchResult)
            {
                var curUpdateBson = new BsonDocument();

                var aNode = enterpriseNode.SelectSingleNode("./a");
                var url = aNode.Attributes["href"] != null ? aNode.Attributes["href"].Value : string.Empty;
                if (string.IsNullOrEmpty(url)) continue;
                var guid = GetGuidFromUrl(url);
                var infoNode = aNode.SelectSingleNode("./span[@class='clear']");
                if (infoNode == null) continue;
                var nameNode = infoNode.SelectSingleNode("./span[@class='name']");
                if (nameNode == null) continue;
                var enterpriseName = nameNode.InnerText;
                var statusNode = infoNode.SelectSingleNode("./span[@class='label label-success m-l-xs']");
                if (statusNode != null)
                {
                    curUpdateBson.Add("status", statusNode.InnerText);
                }

                curUpdateBson.Add("name", enterpriseName);
                curUpdateBson.Add("url", url);
                curUpdateBson.Set("typeCode", typeCode);
                var companyInfo = infoNode.SelectSingleNode("./small[1]");
                if (companyInfo != null)
                    curUpdateBson.Set("companyInfo", companyInfo.InnerText);

                var address = infoNode.SelectSingleNode("./small[2]");
                if (address != null)
                    curUpdateBson.Set("address", address.InnerText);
                //获取企业信息http://www.qichacha.com/service/getRootNodeInfoByEnterpriseId?enterpriseId=1b9df7af-e7b3-4d45-93ce-8acf02534adb&_=1466587526737
                if (!string.IsNullOrEmpty(guid) && !existGuidList.Contains(guid))
                {
                    curUpdateBson.Set("guid", guid);
                    existGuidList.Add(guid);
                    // var message = string.Format("详细信息{0}:{1}获取成功剩余url{2}\r", guid, oldName, UrlQueue.Instance.Count);

                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Insert });
                    addCount++;
                    //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("status", "1"), Query = Query.EQ("name", enterpriseName), Name = DataTableNameList, Type = StorageType.Update });
                }
                else
                {
                    //ShowMessageInfo(string.Format("guid:{0}{1}已存在或者无法添加剩余url{2}\r", guid, oldName, UrlQueue.Instance.Count));
                    updateCount++;
                }

            }
            var str = "解析" + HttpUtility.UrlDecode(args.Url) + "\r";
            ShowMessageInfo(str + string.Format("addCount:{0} updateCount:{1} existCount:{2} 剩余url:{3} ", addCount, updateCount, existCount, UrlQueue.Instance.Count));


        }
        /// <summary>
        /// 用与查找城市
        /// </summary>
        /// <param name="args"></param>
        public void DataReceiveSearchGuidByCity_abort(DataReceivedEventArgs args)
        {
            int addCount = 0, updateCount = 0, existCount = 0;
            //var sb = new StringBuilder();
            var hmtl = args.Html;
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var province = GetUrlParam(args.Url, "province");//获得省份

            try
            {
                var searchResult = htmlDoc.GetElementbyId("cityNew");
                if (searchResult != null)
                {
                    var aNodes = searchResult.SelectNodes("./dd/a");
                    if (aNodes == null)
                    {
                        return;
                    }
                    foreach (var aNode in aNodes)
                    {
                        if (aNode.Attributes["data-value"] != null)
                        {
                            var curUpdateBson = new BsonDocument();
                            var cityName = aNode.InnerText.Trim();
                            var cityCode = aNode.Attributes["data-value"].Value;
                            curUpdateBson.Add("name", cityName);
                            curUpdateBson.Add("code", cityCode);
                            curUpdateBson.Add("provinceCode", province);
                            curUpdateBson.Add("type", "1");
                            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableCity, Type = StorageType.Insert });
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }


        }

        /// <summary>
        ///  获取国民经济分类下的企业列表
        /// </summary>
        /// <param name="args"></param>
        public void DataReceiveSearchGuidByCity(DataReceivedEventArgs args)
        {
            int addCount = 0, updateCount = 0, existCount = 0;
            //var sb = new StringBuilder();
            var hmtl = args.Html;
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(args.Html);

            var oldName = string.Empty;
            var queryStr = GetQueryString(args.Url);
            var cityCode = GetSearchUrlCityCode(args.Url);
            var curProvinceName = cityNameDic.ContainsKey(cityCode) ? cityNameDic[cityCode] : string.Empty;
            var searchResult = htmlDoc.DocumentNode.SelectNodes("//section[@class='panel panel-default']");
            if (searchResult == null) return;

            foreach (var enterpriseNode in searchResult)
            {
                var curUpdateBson = new BsonDocument();
                if (!string.IsNullOrEmpty(curProvinceName))
                {
                    curUpdateBson.Add("provinceName", curProvinceName);
                }
                var aNode = enterpriseNode.SelectSingleNode("./a");
                var url = aNode.Attributes["href"] != null ? aNode.Attributes["href"].Value : string.Empty;
                if (string.IsNullOrEmpty(url)) continue;
                var guid = GetGuidFromUrl(url);
                var infoNode = aNode.SelectSingleNode("./span[@class='clear']");
                if (infoNode == null) continue;
                var nameNode = infoNode.SelectSingleNode("./span[@class='name']");
                if (nameNode == null) continue;
                var enterpriseName = nameNode.InnerText;
                var statusNode = infoNode.SelectSingleNode("./span[@class='label label-success m-l-xs']");
                if (statusNode != null)
                {
                    curUpdateBson.Add("status", statusNode.InnerText);
                }

                curUpdateBson.Add("name", enterpriseName);
                curUpdateBson.Add("url", url);

                var companyInfo = infoNode.SelectSingleNode("./small[1]");
                if (companyInfo != null)
                    curUpdateBson.Set("companyInfo", companyInfo.InnerText);

                var address = infoNode.SelectSingleNode("./small[2]");
                if (address != null)
                    curUpdateBson.Set("address", address.InnerText);
                //获取企业信息http://www.qichacha.com/service/getRootNodeInfoByEnterpriseId?enterpriseId=1b9df7af-e7b3-4d45-93ce-8acf02534adb&_=1466587526737
                if (!string.IsNullOrEmpty(guid) && !existGuidList.Contains(guid))
                {
                    curUpdateBson.Set("guid", guid);
                    existGuidList.Add(guid);
                    // var message = string.Format("详细信息{0}:{1}获取成功剩余url{2}\r", guid, oldName, UrlQueue.Instance.Count);

                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Insert });
                    addCount++;
                    //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("status", "1"), Query = Query.EQ("name", enterpriseName), Name = DataTableNameList, Type = StorageType.Update });
                }
                else
                {
                    //ShowMessageInfo(string.Format("guid:{0}{1}已存在或者无法添加剩余url{2}\r", guid, oldName, UrlQueue.Instance.Count));
                    updateCount++;
                }

            }
            var str = "解析" + HttpUtility.UrlDecode(args.Url) + "\r";
            ShowMessageInfo(str + string.Format("addCount:{0} updateCount:{1} existCount:{2} 剩余url:{3} ", addCount, updateCount, existCount, UrlQueue.Instance.Count));


        }


        /// <summary>
        /// 通过关键字+分类获取企业对应guid
        /// </summary>
        /// <param name="args"></param>
        public void DataReceiveEnterpriseGuidByKeyWord_APP(DataReceivedEventArgs args)
        {


            var cityName = cityNameStr;
            var hmtl = args.Html;
            if (!args.Html.Contains("成功"))
            {
                UrlQueue.Instance.EnQueue(new UrlInfo(args.Url) { Depth = args.Depth + 1 });//尝试五次
            }

            JObject jsonObj = JObject.Parse(hmtl);
            if (jsonObj == null) return;
            var message = jsonObj["message"];


            if (message != null && !message.ToString().Contains("成功"))
            {
                ShowMessageInfo("获取失败" + hmtl);
                return;
            }

            var resultObj = jsonObj["result"];
            if (resultObj == null) return;
            var PagingObj = resultObj["Paging"];
            if (PagingObj == null) return;

            var itemObj = resultObj["GroupItems"];
            if (itemObj == null)
            {
                return;
            }

            var firstRecordCount = 0;
            if (!int.TryParse(PagingObj["TotalRecords"].ToString(), out firstRecordCount))
            {
                return;
            }
            if (firstRecordCount == 0 && args.Depth <= Settings.MaxReTryTimes)//多尝试防止有时候出现为0情况
            {
                UrlQueue.Instance.EnQueue(new UrlInfo(args.Url) { Depth = args.Depth + Settings.MaxReTryTimes / 2 });
            }
            var industryCode = GetUrlParam(args.Url, "industryCode");
            var subIndustryCode = GetUrlParam(args.Url, "subIndustryCode");
            var province = GetUrlParam(args.Url, "province");
            var cityCode = GetUrlParam(args.Url, "cityCode");
            var urlBson = new BsonDocument();
            urlBson.Add("industryCode", industryCode);
            urlBson.Add("subIndustryCode", subIndustryCode);
            urlBson.Add("province", province);
            urlBson.Add("cityCode", cityCode);
            urlBson.Add("recordCount", firstRecordCount.ToString());
            urlBson.Add("url", args.Url);
            urlBson.Add("isApp", "1");
            //var hasExistUrl = ExistUrl(args.Url);
            var hasExistUrl = false;
            curAddUrlList = new List<string>();
            if (args.urlInfo==null||args.urlInfo.UrlSplitTimes != -1)//2017.6.13 新增url拆分限制次数，为-1时候不进行拆分
            {
                try
                {
                    if (firstRecordCount >= 40 && IsKeyWordUrlSplited_APP(args, firstRecordCount, itemObj))
                    {
                        //if (!hasExistUrl)
                        //{
                        //    DBChangeQueue.Instance.EnQueue(new StorageData()
                        //    {
                        //        Document = urlBson.Add("isSplited", "1"),
                        //        Name = DataTableNameKeyWordURLAPP,
                        //        Type = StorageType.Insert
                        //    });
                        //}
                        ShowMessageInfo(string.Format("当前记录:{0}过大进行分支处理剩余url:{1}", firstRecordCount, UrlQueue.Instance.Count));
                        //return;
                    }
                    else
                    {
                        if (firstRecordCount >= 40)
                        { //大于四十又没得分页,后续通过调整排序进行数值查找
                            urlBson.Add("needVipDeal", "1");//需要vip账号进行处理
                            if (!ExistUrl(args.Url, true))
                            {
                                DBChangeQueue.Instance.EnQueue(new StorageData()
                                {
                                    Document = urlBson,
                                    Name = DataTableNameKeyWordURLAPP,
                                    Type = StorageType.Insert
                                });
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex + "IsKeyWordUrlSplited_APP");
                }
                if (args.urlInfo.UrlSplitTimes< -1)
                {
                    args.urlInfo.UrlSplitTimes++;
                }
            }
            #region 查看新增url
            //var finalResult = new List<string>();
            //var curRegistCapiBeginParam = GetUrlParam(args.Url, "registCapiBegin");
            //var curregistCapiEndParam = GetUrlParam(args.Url, "registCapiEnd");
            //var cursubIndustryCode = GetUrlParam(args.Url, "subIndustryCode");
            //var curstartDateBeginParam = GetUrlParam(args.Url, "startDateBegin");
            //var curstartDateEndParam = GetUrlParam(args.Url, "startDateEnd");
            //var curResult = string.Format(firstRecordCount.ToString()+"registCapiBegin={0}&registCapiEnd={1}&startDateBegin={2}&startDateEnd={3}", curRegistCapiBeginParam, curregistCapiEndParam, curstartDateBeginParam, curstartDateEndParam);
            //if (!string.IsNullOrEmpty(cursubIndustryCode))
            //{
            //    curResult += string.Format("&subIndustryCode=" + cursubIndustryCode);
            //}
            //finalResult.Add(curResult);
            //foreach (var url in curAddUrlList)
            //{
            //    var registCapiBeginParam = GetUrlParam(url, "registCapiBegin");
            //    var registCapiEndParam = GetUrlParam(url, "registCapiEnd");
            //    var _subIndustryCode = GetUrlParam(args.Url, "subIndustryCode");
            //    var startDateBeginParam = GetUrlParam(url, "startDateBegin");
            //    var startDateEndParam = GetUrlParam(url, "startDateEnd");
            //    var result = string.Format("registCapiBegin={0}&registCapiEnd={1}&startDateBegin={2}&startDateEnd={3}", registCapiBeginParam, registCapiEndParam, startDateBeginParam, startDateEndParam);
            //    if (!string.IsNullOrEmpty(_subIndustryCode))
            //    {
            //        result += string.Format("&subIndustryCode=" + _subIndustryCode);
            //    }
            //    finalResult.Add(result);
            //}
            #endregion
            var curAccount = allAccountList.Where(c => c.Text("name") == Settings.LoginAccount).FirstOrDefault();
            if (curAccount != null && curAccount.Int("isSuperVip") == 1)
            { //superVip才能进行分页主力
                long pageCount;
                if (firstRecordCount % 40 == 0)
                    pageCount = firstRecordCount / 40;
                else
                    pageCount = firstRecordCount / 40 + 1;
                //获取分页数目
                for (var _index = 2; _index <= pageCount; _index++)
                {
                    var url = string.Empty;
                    if (args.Url.EndsWith("&p=1"))
                    {
                        url = args.Url.Replace("&p=1", string.Format("&p={0}", _index));
                    }
                    else
                    {
                        url = string.Format("{0}&p={2}", args.Url, _index);
                    }
                }
            }
            var keyWord = string.Empty;
            var queryStr = GetQueryString(args.Url);
            var oldBsonDocument = new BsonDocument();
            var page = string.Empty;

            var SB = new StringBuilder();
            var existCount = 0;
            var addCount = 0;
            #region 遍历处理list
            foreach (var enterpriseObj in resultObj["Result"])
            {
                var curUpdateBson = new BsonDocument();
                var guid = enterpriseObj["KeyNo"].ToString();
                var name = enterpriseObj["Name"].ToString();
                var domain = string.Empty;

                try
                {
                    if (enterpriseObj["Industry"] != null && !string.IsNullOrEmpty(enterpriseObj["Industry"].ToString()))
                    {
                        domain = enterpriseObj["Industry"] != null ? enterpriseObj["Industry"]["SubIndustry"].ToString() : string.Empty;
                        if (string.IsNullOrEmpty(domain) && enterpriseObj["Industry"] != null && enterpriseObj["Industry"]["Industry"] != null)
                        {
                            domain = enterpriseObj["Industry"]["Industry"].ToString();
                        }
                    }
                    else
                    {
                        //if (enterpriseObj["Scope"]!=null)
                        domain = "";
                    }
                }
                catch (Exception ex)
                {
                    domain = enterpriseObj["Scope"].ToString();
                }
                message = string.Format("详细信息{0}_{1}获取成功剩余url{2}\r", guid, name, UrlQueue.Instance.Count);
                curUpdateBson.Set("domain", domain);
                //设定所属城市
                curUpdateBson.Set("cityName", cityName);
                foreach (var keyMap in EnterpriseInfoMapDic_App)//遍历字段
                {

                    if (enterpriseObj[keyMap.Key] != null)
                    {
                        curUpdateBson.Set(keyMap.Value, enterpriseObj[keyMap.Key].ToString());
                    }
                }
                if (curUpdateBson.Count() > 0)
                {
                    curUpdateBson.Set("isUserUpdate", "1");
                }
                if (existGuidList.Contains(guid))
                {
                    existCount++;
                }
                else {//代表还未添加
                    existGuidList.Add(guid);
                    if (!string.IsNullOrEmpty(guid) && !ExistGuid(guid))
                    {
                        addCount++;
                        SB.AppendFormat("获得对象{0}:{1}\r", guid, name, UrlQueue.Instance.Count);
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Insert });
                    }
                    else
                    {
                        existCount++;
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Query = Query.EQ("guid", guid), Type = StorageType.Update });
                        existGuidList.Add(guid);

                    }
                }
            }
            #endregion
            var searchKey = GetUrlParam(args.Url, "searchKey");
            var keyWordStr = Toolslib.Str.Sub(searchKey, "\"scope\":\"", "\"");
            var curKeyWordCount=SetKeyWordHitCount(keyWordStr, addCount);
            decimal updateCount = decimal.Parse(existCount.ToString()) / 10000000;

            curKeyWordCount= SetKeyWordHitCount(keyWordStr, updateCount);//设置更新个数
            ShowMessageInfo(string.Format("关键字已经添加:{6}| 当前：{5}添加:{0} 已存在:{1}剩余url{4} 详细:{2}当前url:{3}", addCount, existCount, SB.ToString(), HttpUtility.UrlDecode(queryStr), UrlQueue.Instance.Count, firstRecordCount.ToString(), curKeyWordCount));


        }
        #endregion
        #region 基础方法 

        /// <summary>
        /// 通过关键字生成QCC
        /// </summary>
        /// <param name="_typeName"></param>
        /// <param name="province"></param>
        /// <param name="cityCode"></param>
        private string InitalQCCAppUrlByKeyWord(string _typeName)
        {
            var province = curQCCProvinceCode;
            var cityCode = curQCCCityCode;
            var curTypeName = _typeName;
            if (curTypeName.Length == 1)
            {
                curTypeName += "业";
            }
            //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("name", curTypeName), Name = DataTableScopeKeyWord, Type = StorageType.Insert  });
            //需要限制住大类防止url爬取过多，因为如房地产会导致其他大类里面有很多，只要限制住大类即可，其他的由其他的关键字爬取
            //优化爬取效率
            var _curTypeName = string.Format("\"scope\":\"{0}\",", curTypeName);
            _curTypeName += string.Format("\"opername\":\"{0}\",", curTypeName);
            _curTypeName += string.Format("\"featurelist\":\"{0}\",", curTypeName);
            _curTypeName += string.Format("\"address\":\"{0}\",", curTypeName);
            _curTypeName += string.Format("\"name\":\"{0}\"", curTypeName);
            curTypeName = "{" + HttpUtility.UrlEncode(_curTypeName) + "}";


            //{% 22scope % 22:% 22taobao % 22,
            //% 22opername % 22:% 22taobao % 22,
            //% 22featurelist % 22:% 22taobao % 22,
            //% 22address % 22:% 22taobao % 22,
            //% 22name % 22:% 22taobao % 22}
            // var url = string.Format("https://app.qichacha.net/app/v1/base/advancedSearch?searchKey={0}&searchIndex=default&province={1}&cityCode={2}&statusCode=&registCapiBegin=&registCapiEnd=&isSortAsc=false&startDateBegin=&startDateEnd=&industryCode={3}&subIndustryCode={4}&timestamp={5}&sign={6}&p=1", curTypeName, province, cityCode, industryCode, "", Settings.timestamp, Settings.sign);
            //按时间逆序
            var url = string.Format("https://app.qichacha.net/app/v1/base/advancedSearch?searchKey={0}&searchIndex=multicondition&province={1}&cityCode={2}&statusCode=&registCapiBegin=&registCapiEnd=&isSortAsc=false&sortField=startdate&startDateBegin=&startDateEnd=&industryCode={3}&subIndustryCode={4}&p=1", curTypeName, province, cityCode, "", "");
            return url;
        }

        /// <summary>
        /// 保存匹配值到数据库
        /// </summary>
        private void SaveKeyWordHitCount()
        {
            try
            {
                foreach (var keyWordDic in keyWordCountDic)
                {
                    var existObj = keyWordHitCountList.Where(c => c.Text("keyWord") == keyWordDic.Key).FirstOrDefault();
                    var splitArr = keyWordDic.Value.ToString().Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                    var addCount = 0;
                    var updateCount = 0;
                    try
                    {
                        if (splitArr.Length >= 2)
                        {
                            addCount = int.Parse(splitArr[0]);
                            updateCount = int.Parse(splitArr[1]);
                        }
                        else
                        {
                            addCount = (int)keyWordDic.Value;
                        }
                    }
                    catch (InvalidCastException ex)
                    {
                        ShowMessageInfo("SaveKeyWordHitCount" + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        ShowMessageInfo("SaveKeyWordHitCount" + ex.Message);
                    }
                    if (existObj != null)
                    {
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("count", (int)keyWordDic.Value).Add("addCount", addCount).Add("updateCount", updateCount), Query = Query.EQ("_id", ObjectId.Parse(existObj.Text("_id"))), Name = DataTableEnterriseKeyWordCount, Type = StorageType.Update });
                    }
                    else
                    {
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("cityName", cityNameStr).Add("keyWord", keyWordDic.Key).Add("count", (int)keyWordDic.Value).Add("addCount", addCount).Add("updateCount", updateCount), Name = DataTableEnterriseKeyWordCount, Type = StorageType.Insert });
                    }

                }
                StartDBChangeProcessQuick();
            }
            catch (Exception ex)
            {
                ShowMessageInfo("SaveKeyWordHitCount" + ex.Message);
            }
        }
        List<BsonDocument> keyWordHitCountList = new List<BsonDocument>();
        /// <summary>
        /// 初始化关键字匹配
        /// </summary>
        /// <param name="cityNameStr"></param>
        private void InitKeyWordHitCount(string cityNameStr)
        {
            #region 关键字匹配
             keyWordHitCountList = dataop.FindAllByQuery(DataTableEnterriseKeyWordCount, Query.EQ("cityName", cityNameStr)).ToList();
            foreach (var _keyWordDoc in keyWordHitCountList)
            {
                if (keyWordCountDic.ContainsKey(_keyWordDoc.Text("keyWord")))
                {
                    keyWordCountDic[_keyWordDoc.Text("keyWord")] = _keyWordDoc.Decimal("count");
                }
                else
                {
                    keyWordCountDic.TryAdd(_keyWordDoc.Text("keyWord"), _keyWordDoc.Decimal("count"));
                }
            }
            #endregion
        }
        /// <summary>
        /// 匹配关键字匹配增加的记录个数 为12345.44312 标示增加12345 更新44312
        /// </summary>
        private decimal SetKeyWordHitCount(string keyWord,decimal  addCount)
        {
            try
            {
                if (keyWordCountDic.ContainsKey(keyWord))
                {
                    keyWordCountDic[keyWord] = keyWordCountDic[keyWord] + addCount;
                    return keyWordCountDic[keyWord];
                }
                else
                {
                    keyWordCountDic.TryAdd(keyWord, addCount);
                    return keyWordCountDic[keyWord];
                }
               
            }
            catch (Exception ex)
            {
                return addCount;
                ShowMessageInfo("SetKeyWordHitCount"+ex.Message);
            }
        }
        
        /// <summary>
        /// 设置企业连接字符串
        /// </summary>
        /// <param name="_enterpriseIp"></param>
        private void SetEnterpriseDataOP(string _enterpriseIp)
        {
           
            enterpriseIp = _enterpriseIp;
            enterpriseConnStr = string.Format("mongodb://MZsa:MZdba@{0}:{1}/SimpleCrawler", _enterpriseIp,port);
            enterpriseDataop = new DataOperation(new MongoOperation(enterpriseConnStr));//主数据库
            _enterpriseMongoDBOp = new MongoOperation(enterpriseConnStr);
            ShowMessageInfo(string.Format("当前 ip:{0}", _enterpriseIp));
        }
        private bool ExistGuid(string guid)
        {
            return enterpriseDataop.FindCount(DataTableName, Query.EQ("guid", guid)) > 0;
        }
        private bool ExistUrl(string url,bool forceQuery=false)
        {
            if (!NEEDRECORDURL&& forceQuery==false)
            {
                return false;
            }
            else
            {
                return dataop.FindCount(DataTableNameKeyWordURL, Query.EQ("url", url)) > 0;
            }
        }
        /// 刷新账号
        /// </summary>
        public void ReloadLoginAccount()
        {
            this.comboBox1.Items.Clear();
            var tempAccountList = new List<BsonDocument>();
            var takeCount = 100;
            var query = Query.And(Query.NE("isInvalid", "1"), Query.NE("isBusy", "1"), Query.NE("status", "1"));
            var totalCount = dataop.FindCount(DataTableAccount, query);
            var rand = new Random();
            var count = rand.Next(0, totalCount);
            if (count <= 100)
            {
                count = 0;
            }
            if (allAccountList.Count <= 0)
            {
                allAccountList = dataop.FindLimitByQuery(DataTableAccount, query, new SortByDocument(), count, takeCount).OrderBy(c => c.Int("isUsed") + c.Int("isBusy")).ThenBy(c => c.Int("UpdateEnterpriseInfo") + c.Int("EnterpriseGuidByKeyWord")).ThenBy(c => c.Date("updateDate")).ToList();

            }
            else
            {
                allAccountList = dataop.FindLimitByQuery(DataTableAccount, query, new SortByDocument(), count, takeCount).OrderBy(c => c.Int("isUsed") + c.Int("isBusy")).ThenBy(c => c.Int("UpdateEnterpriseInfo") + c.Int("EnterpriseGuidByKeyWord")).ThenBy(c => c.Date("updateDate")).ToList();

            }
            tempAccountList = allAccountList;
            foreach (var account in tempAccountList)
            {

                var accountName = string.Empty;
                if (account.Int("isUsed") == 1)
                {
                    accountName = string.Format("{0}_占用", account.Text("name"));
                }
                else
                {
                    accountName = string.Format("{0}", account.Text("name"));
                }
                if (account.Int("isBusy") == 1)
                {
                    accountName = string.Format("{0}_频繁", accountName);
                }
                this.comboBox1.Items.Add(accountName);
            }

            ///获取设备列表
            // allDeviceAccountList = dataop.FindAllByQuery(QCCDeviceAccount, Query.And(Query.NE("isValid", "1"), Query.NE("status", "1"), Query.NE("isUse", "1"), Query.NE("isBusy", "1"))).Where(c => c.Int("EnterpriseGuidByKeyWord_APP") <= Settings.MaxAccountCrawlerCount / 2).OrderBy(c => c.Int("EnterpriseGuidByKeyWordApp")).ThenBy(c => c.Date("updateDate")).ToList();
            allDeviceAccountList = dataop.FindAllByQuery(QCCDeviceAccount, Query.And(Query.NE("isInvalid", "1"),Query.NE("status","1"))).OrderBy(c => c.Int("EnterpriseGuidByKeyWordApp")).ThenBy(c => c.Date("updateDate")).ToList();
        }
        private void ShowAccountInfo(BsonDocument curLoginAccountObj)
        {
            Type searchTypeEnum = typeof(SearchType);
            var sb = new StringBuilder();
            // foreach (string _searchType in Enum.GetNames(searchTypeEnum))
            {
                var _searchType = searchType.ToString();
                //Console.WriteLine("{0,-11}= {1}", s, Enum.Format(searchTypeEnum, Enum.Parse(searchTypeEnum, s), "s"));
                var addColumnName = string.Format("{0}_add", _searchType);
                var addTotalColumnName = string.Format("{0}_total", _searchType);
                var curAddional = curLoginAccountObj.Int(addColumnName);
                var initial = curLoginAccountObj.Int(_searchType);
                var all = curLoginAccountObj.Int(addTotalColumnName);
                if (curAddional != 0 || initial != 0 || all != 0)
                {
                    if (searchType == SearchType.EnterpriseGuidByKeyWord_APP)
                    {
                        sb.AppendFormat("{0}:{1}【add:{2}_{3}】 ", "app", initial, curAddional, all);
                    }
                    else
                    {
                        sb.AppendFormat("{0}:{1}【add:{2}_{3}】 ", _searchType, initial, curAddional, all);
                    }
                }
            }
            accountInfoTxt.BeginInvoke(new Action(() =>
            {
                this.accountInfoTxt.Text = sb.ToString();
            }));
        }
        private void ShowAccountInfo()
        {
            var curLoginAccountObj = allAccountList.Where(c => c.Text("name") == Settings.LoginAccount).FirstOrDefault();
            if (curLoginAccountObj != null)
            {
                ShowAccountInfo(curLoginAccountObj);
            }
        }
        public string GetString(JToken node, string columnName)
        {


            if (node != null && node.ToString().Contains(string.Format("\"{0}\":", columnName)))
            {
                return node[columnName].ToString();
            }
            else
            {
                return string.Empty;
            }

        }

        /// <summary>
        /// 执行可执行文件
        /// </summary>
        /// <param name="exeFilePath"></param>
        /// <param name="Arguments"></param>
        public static string ExecProcess(string exeFilePath, string Arguments = "")
        {

            // 执行exe文件
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = exeFilePath;
            // 不显示闪烁窗口
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(exeFilePath);
            // 注意，参数需用引号括起来，因为路径中可能有空格
            if (!string.IsNullOrEmpty(Arguments))
            {
                process.StartInfo.Arguments = Arguments;
            }
            try
            {
                process.Start();


            }
            catch (OutOfMemoryException ex)
            {
                return ex.Message;

            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                if (process != null)
                    process.Close();

            }
            return string.Empty;
        }
        /// <summary>
        /// 模拟js方法
        /// </summary>
        /// <returns></returns>
        public long GetTimeLikeJS()

        {

            long lLeft = 621355968000000000;

            DateTime dt = DateTime.Now;

            long Sticks = (dt.Ticks - lLeft) / 10000;

            return Sticks;

        }



        /// <summary>
        /// 获取url对应查询参数
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetGuidFromUrl(string url)
        {
            var beginStrIndex = url.LastIndexOf("_");
            var endStrIndex = url.LastIndexOf(".");
            if (beginStrIndex != -1 && endStrIndex != -1)
            {
                if (beginStrIndex > endStrIndex)
                {
                    var temtp = beginStrIndex;
                    beginStrIndex = endStrIndex;
                    endStrIndex = temtp;
                }
                var queryStr = url.Substring(beginStrIndex + 1, endStrIndex - beginStrIndex - 1);
                return queryStr;
            }
            return string.Empty;
        }


        /// <summary>
        /// 获取url对应查询参数
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetUrlParam(string url, string name)
        {
            var queryStr = GetQueryString(url);
            var dic = HttpUtility.ParseQueryString(queryStr);
            var industryCode = dic[name] != null ? dic[name].ToString() : string.Empty;//行业代码
            return industryCode;
        }


        /// <summary>
        /// 获取url对应查询参数
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetQueryString(string url)
        {
            var queryStrIndex = url.IndexOf("?");
            if (queryStrIndex != -1)
            {
                var queryStr = url.Substring(queryStrIndex + 1, url.Length - queryStrIndex - 1);
                return queryStr;
            }
            return string.Empty;
        }

        private static string GetSearchUrlCode(string url)
        {
            var endStr = "_industryorder";
            var beginStr = "industryCode_";
            var endStrIndex = url.LastIndexOf(endStr);
            var beginStrIndex = url.IndexOf(beginStr);
            if (beginStrIndex != -1 && endStrIndex != -1)
            {
                //
                var queryStr = url.Substring(beginStrIndex + beginStr.Length, endStrIndex - beginStrIndex - beginStr.Length);
                return queryStr.Replace("subIndustryCode", "").Replace("_", "");
            }
            return string.Empty;
            //industryCode_A_subIndustryCode_1_industryorder
        }
        private static string GetSearchUrlCityCode(string url)
        {
            var endStr = "_p";
            var beginStr = "prov_";
            var endStrIndex = url.LastIndexOf(endStr);
            var beginStrIndex = url.IndexOf(beginStr);
            if (beginStrIndex != -1 && endStrIndex != -1)
            {
                //
                var queryStr = url.Substring(beginStrIndex + beginStr.Length, endStrIndex - beginStrIndex - beginStr.Length);
                return queryStr.Replace("subIndustryCode", "").Replace("_", "");
            }
            return string.Empty;
            //industryCode_A_subIndustryCode_1_industryorder
        }
        /// <summary>
        /// 获取url对应查询参数
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetQuerySearchTypeString(string url)
        {
            var queryStrIndex = url.LastIndexOf("/");
            if (queryStrIndex != -1)
            {
                var queryStr = url.Substring(queryStrIndex + 1, url.Length - queryStrIndex - 1);
                return queryStr;
            }
            return string.Empty;
        }


        public static bool SimulateLogin()
        {
            Settings.SimulateCookies = "pgv_pvid=1513639250; aliyungf_tc=AQAAADPRHnGrvwQAIkg9OwOqtkYJbU4N; oldFlag=1; CNZZDATA1259577625=112366950-1466409958-%7C1466415358; hide-index-popup=1; hide-download-panel=1; _alicdn_sec=576ba5f9a986fb4802dacf51bc99b1e76724f58e; connect.sid=s%3AeYWXycPKai63BYTmB9d6h-0IM_R2kp6n.EUgfW0AmJ6GB%2F0TamTi4tT53QK4OR4yQtU1I3Ba8Ryo; userKey=QXBAdmin-Web2.0_N3iUdNobAoys4M395Pk5v%2F6Zxcwjt1tiCqeSf3X3ZnI%3D; userValue=bea26f0d-e414-168a-0fe2-b8eb4278ab07; Hm_lvt_52d64b8d3f6d42a2e416d59635df3f71=1464663982,1464775028,1464776749,1465799273; Hm_lpvt_52d64b8d3f6d42a2e416d59635df3f71=1466672591";//设置cookie值
            return true;
        }

        public bool UrlContentLimit(string html, string url = "", int depth = 3)
        {
          
            if (html.StartsWith("<script>"))
            {
                return true;
                //todo:AccountBusy
                //AccountBusy
            }
            if (searchType == SearchType.EnterpriseGuidByKeyWord_APP)
            {
                var status = Toolslib.Str.Sub(html, "message\":\"", "\"");
                if (status.Contains("成功"))
                {
                    return false;
                }
                if ((html.Contains("异常访问") || html.Contains("失败") || html.Contains("失效")))
                {
                    return true;
                }


                // GeetestChartAutoLoin();
                // return false;
            }
            if (searchType == SearchType.EnterpriseGuidByKeyWord || searchType == SearchType.EnterpriseGuid)
            {

                if (string.IsNullOrEmpty(html) || !html.Contains("table-search-list"))
                {
                    //if (depth < 2) { //尝试3次
                    // UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth= depth + 1});//有几率出现爬取为空情况
                    //}
                    return false;
                }
                return false;

            }
            else if (searchType == SearchType.EnterpriseGuidByKeyWordEnhence)
            {
                if (html == "null" || html == "") return false;
            }
            if (searchType == SearchType.UpdateEnterpriseInfo && !html.Contains("基本信息") && !url.Contains("more_findmuhou"))
            {
                if (string.IsNullOrEmpty(html) || html == "尝试三次后无数据无法处理") return false;
                if (html.StartsWith("<script>"))
                {
                    return true;
                    //todo:AccountBusy
                    //AccountBusy
                }
                return true;
            }
            if (html.Length <= 10 || html.Contains("您使用验证码过于频繁") || html.Contains("请求的网址（URL）无法获取"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static int limitTimes = 0;//用于判断是否连续出错，爬取过程中有机率出现验证码干扰
        /// <summary>
        /// IP限定处理，ip被限制 账号被限制跳转处理 直接返回true 代表需要进行重新添加url验证
        /// </summary>
        /// <param name="args"></param>
        public bool IPLimitProcess(DataReceivedEventArgs args)
        {

            if (UrlContentLimit(args.Html, args.Url, args.Depth))
            {
                try
                {

                    //出错次数过多肯呢个到职被异常
                    if (searchType != SearchType.EnterpriseGuidByKeyWord_APP)
                    {
                        if (string.IsNullOrEmpty(args.Html)) return true;//等待期间内的url 不限制
                        if (searchType == SearchType.UpdateEnterpriseInfo)
                        {
                            if (limitTimes++ < 10) return true;//连续出现2次验证码 才继续往下走
                        }
                        limitTimes = 0;
                        ///先验证是否获取真的取不到到数据index_verify爬取过程几率中断
                        var timeSpan = DateTime.Now - Settings.LastAvaiableTime;
                        if (timeSpan.TotalSeconds < 5)//没限制 15秒没取到数据
                        {
                            return true;
                        }
                    }
                    else
                    {
                        //if (args.Html.Contains("成功")&& Settings.neeedChangeAccount)
                        //{
                        //    this.richTextBoxInfo.Text += string.Format("账号爬取达到上限{0},请切换账号", Settings.MaxAccountCrawlerCount);
                        //    AutoChangeAccount();
                        //    //return false;
                        //}
                    }
                    timerStop();
                    //this.webBrowser.Refresh();
                    //自动过验证码
                    webBrowser.BeginInvoke(new Action(() =>
                    {
                        // this.accountInfoTxt.Text += string.Format("上次有效获取时间:{0}", Settings.LastAvaiableTime.ToString("HH:mm:ss"));
                        try
                        {
                            // this.webBrowser.Refresh();
                            this.webBrowser.Navigate(addCredsToUri(curUri));
                        }
                        catch (Exception ex)
                        {
                            this.richTextBoxInfo.Text += "刷新浏览器失败";
                            // ShowMessageInfo("刷新浏览器失败", true);
                        }


                        //this.richTextBoxInfo.Document.Blocks.Clear();
                        //this.richTextBoxInfo.AppendText(string.Format("当前url:{0}剩余url:{1}", this.curUri.ToString(), UrlQueue.Instance.Count));
                        //this.richTextBoxInfo.AppendText("正在检测是否自动验证!");
                        if (Settings.neeedChangeAccount)
                        {
                            this.richTextBoxInfo.Text += string.Format("账号爬取达到上限{0},请切换账号", Settings.MaxAccountCrawlerCount);
                            AutoChangeAccount();
                        }
                        else
                        {
                            if (PassEnterpriseInfoGeetestChart())
                            {
                                //waitBrowerMouseUpResponse = true;
                                // Thread.Sleep(1000);
                                timerStart();
                            }
                            else
                            {
                                //ShowMessageInfo("正在刷新浏览器，请点击重新爬取", true);
                                this.richTextBoxInfo.Text += "正在刷新浏览器，请点击重新爬取";
                                waitBrowerMouseUpResponse = true;
                                ///是否获得焦点
                                if (this.checkBox.Checked)
                                {
                                    this.Activate();
                                }
                            }

                            ///重载uri
                            if (curUri != null)
                            {
                                var cookies = FullWebBrowserCookie.GetCookieInternal(curUri, false);
                                Settings.SimulateCookies = cookies;
                            }
                        }

                    }));
                }
                catch (Exception ex)
                {
                    ShowMessageInfo(ex.Message);
                    //MessageBox.Show(ex.Message);
                }
                return true;
            }
           
            if (Settings.neeedChangeAccount)
            {
                this.richTextBoxInfo.Text += string.Format("账号爬取达到上限{0},请切换账号", Settings.MaxAccountCrawlerCount);
                AutoChangeAccount();
            }
            Settings.LastAvaiableTime = DateTime.Now;
            //ShowMessageInfo("无效内容"+args.Url, true);

            return false;
        }

        public void ShowMessageInfo(string str, bool isAppend = false)
        {
            richTextBoxInfo.BeginInvoke(new Action(() =>
            {
                if (isAppend == false)
                {
                    this.richTextBoxInfo.Clear();
                }
               this.richTextBoxInfo.AppendText(proxyUser+ enterpriseIp + " 剩余更新队列：" + DBChangeQueue.Instance.Count+"剩余关键字:"+StringQueue.Instance.Count+" ");
               this.richTextBoxInfo.AppendText( str);

            })
           );
        }


        /// <summary>
        /// 对需要更新的队列数据更新操作进行批量处理,可考虑异步执行
        /// </summary>
        //private static void StartDBChangeProcess()
        //{

        //     return;
        //    List<StorageData> updateList = new List<StorageData>();
        //    while (DBChangeQueue.Instance.Count > 0 && updateList.Count() <= 50)
        //    {
        //        var curStorage = DBChangeQueue.Instance.DeQueue();
        //        if (curStorage != null)
        //        {
        //            updateList.Add(curStorage);
        //        }
        //    }
        //    if (updateList.Count() > 0)
        //    {
        //        // Task.Run(() => { 
        //        var result = dataop.BatchSaveStorageData(updateList);
        //        if (result.Status != Status.Successful)//出错进行重新添加处理
        //        {
        //            if (!result.Message.Contains("memory"))
        //            {
        //                foreach (var storageData in updateList)
        //                {
        //                    DBChangeQueue.Instance.EnQueue(storageData);
        //                }
        //            }
        //        }
        //        //  }
        //        // );
        //    }
        //    if (DBChangeQueue.Instance.Count > 0)
        //    {
        //        StartDBChangeProcess();
        //    }
        //}

        /// <summary>
        /// 对需要更新的队列数据更新操作进行批量处理,可考虑异步执行
        /// </summary>
        private void StartDBChangeProcessQuick()
        {
            var limitCount = Settings.DBSaveCountLimit;
            if (limitCount <= 0)
            {
                limitCount = 20;
            }
            if (UrlQueue.Instance.Count >= 10 && DBChangeQueue.Instance.Count < limitCount) return;
            var curDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var result = new InvokeResult();
            List<StorageData> updateList = new List<StorageData>();

            {

                var temp = DBChangeQueue.Instance.DeQueue();
                if (temp != null)
                {
                    var insertDoc = temp.Document;
                    var curMongoDBOP = _mongoDBOp;
                    if (temp.Name == DataTableName)//企业实体
                    {
                        curMongoDBOP = _enterpriseMongoDBOp;
                    }
                    switch (temp.Type)
                    {
                        case StorageType.Insert:
                            insertDoc.Set("createDate", curDate);      //添加时,默认增加创建时间
                            insertDoc.Set("createUserId", "1");
                            //更新用户
                            //if (insertDoc.Contains("underTable") == false) insertDoc.Add("underTable", temp.Name);
                            //insertDoc.Set("updateDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));      //更新时间
                            //                                                                                // insertDoc.Set("updateUserId", "1");
                            result = curMongoDBOP.Save(temp.Name, insertDoc); ;
                            break;
                        case StorageType.Update:
                            insertDoc.Set("updateDate", curDate);      //更新时间
                            // insertDoc.Set("updateUserId", "1");
                            result = curMongoDBOP.Save(temp.Name, temp.Query, insertDoc);
                            break;
                        case StorageType.Delete:
                            result = curMongoDBOP.Delete(temp.Name, temp.Query);
                            break;
                    }
                    //logInfo1.Info("");
                    if (result.Status == Status.Failed)
                    {
                        //DBChangeQueue.Instance.EnQueue(temp);//重新添加保存
                        //  throw new Exception(result.Message);
                    }

                }

            }

            if (DBChangeQueue.Instance.Count > 0)
            {
                StartDBChangeProcessQuick();
            }
            if (DBChangeQueue.Instance.Count >= 100)
            {
                ShowMessageInfo(DBChangeQueue.Instance.Count.ToString(), true);

            }
        }



        private void timerStart()
        {

            if (aTimer.Enabled == false)
            {

                var curElapse = 1000;

                if (!string.IsNullOrEmpty(curTimerElapse))
                {
                    int.TryParse(curTimerElapse.Trim(), out curElapse);
                }
                var rand = new Random();
                //var minElapse = 1000;
                var curInterVal = 1000;
                if (curElapse <= 1000)
                {
                    curInterVal = curElapse;
                }
                else
                {
                    curInterVal = rand.Next(curElapse / 2, curElapse);
                }




                aTimer.Interval = curInterVal;
                aTimer.Enabled = true;
                aTimer.Start();
                waitBrowerMouseUpResponse = false;

                ShowMessageInfo("计时器开始");

                GetSimulateCookies();

            }

        }

        private void GetSimulateCookies()
        {
            webBrowser.BeginInvoke(new Action(() =>
            {
                ///重载uri
                if (curUri != null)
                {
                    var cookies = FullWebBrowserCookie.GetCookieInternal(curUri, false);
                    Settings.SimulateCookies = cookies;
                }
            })
           );
        }
        private void timerStop()
        {
            if (aTimer.Enabled == true)
            {

                aTimer.Stop();
                aTimer.Enabled = false;
                waitBrowerMouseUpResponse = true;
                ShowMessageInfo("计时器结束", true);

            }

        }

        #region 浏览器方法
        public static void FillField(object doc, string id, string value)
        {
            var element = findElementByID(doc, id);
            element.setAttribute("value", value);
        }

        public static void ClickButton(object doc, string id)
        {
            var element = findElementByID(doc, id);
            element.click();
        }

        private static IHTMLElement findElementByID(object doc, string id)
        {
            IHTMLDocument2 thisDoc;
            if (!(doc is IHTMLDocument2))
                return null;
            else
                thisDoc = (IHTMLDocument2)doc;

            var element = thisDoc.all.OfType<IHTMLElement>()
                .Where(n => n != null && n.id != null)
                .Where(e => e.id == id).First();
            return element;
        }
        private static void ExecuteScript(object doc, string js)
        {
            IHTMLDocument2 thisDoc;
            if (!(doc is IHTMLDocument2))
                return;
            else
                thisDoc = (IHTMLDocument2)doc;
            thisDoc.parentWindow.execScript(js);
        }
        #endregion


        /// <summary>
        /// 过企业信息chart验证码
        /// </summary>
        /// <returns></returns>
        private bool PassEnterpriseInfoGeetestChart(bool forecePass = false)
        {
            if (Settings.neeedChangeAccount && searchType == SearchType.EnterpriseGuidByKeyWord_APP)
            {
                AutoChangeAccount();//自动切换账号;
                return false;
            }
            //一个时刻只能一个实例运行
            lock (lockPassGeek)
            {
                if (aTimer.Enabled == false)
                {
                    if (!forecePass)
                    {
                        if ((DateTime.Now - LastGetPassGeetestTime).TotalSeconds < GeetestMaxSpanSecond)//10秒尝试一次过验证码
                        {

                            //ShowMessageInfo("间隔时间太短无法进行自动过验证码");
                            return false;
                        }
                        if ((PassInValidTimes <= 0 || PassSuccessTimes <= 0))
                        {
                            if (checkBox1.Checked)
                                checkBox1.Checked = false;
                            AutoChangeAccount();//自动切换账号
                            return false;
                        }
                    }

                    LastGetPassGeetestTime = DateTime.Now;
                    if (USEWEBPROXY)
                    {
                        hi.ProxyIP = GetWebProxyString();
                    }
                    geetestHelper = new PassGeetestHelper();
                    var validUrl = "http://www.qichacha.com/index_verifyAction";
                    var postFormat = "geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}%7Cjordan&type=companyview";
                    bool result = false;

                    switch (searchType)
                    {
                        case SearchType.UpdateEnterpriseInfo:
                            break;
                        case SearchType.EnterpriseGuidByKeyWord:
                            geetestHelper.GetCapUrl = "http://www.qichacha.com/index_getcap?rand=t={0}&_={0}";
                            postFormat = "geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}%7Cjordan&type=companysearch";
                            break;
                        case SearchType.EnterpriseGuidByKeyWordEnhence:
                            validUrl = "http://www.qichacha.com/service/gtvalidate";
                            postFormat += "&requestType=company_detail";
                            break;
                        default:
                            return false;
                    }

                    if (forecePass || this.checkBox1.Checked)//是否强制过验证码
                    {
                        this.richTextBoxInfo.Invoke(new Action(() =>
                        {
                            this.richTextBoxInfo.AppendText("等待过验证码");
                            var passResult = geetestHelper.PassGeetest(hi, postFormat, validUrl, Settings.SimulateCookies);
                            result = passResult.Status;
                            this.richTextBoxInfo.AppendText("已自动过验证码");
                            this.textBox4.Text = passResult.LastPoint;
                            //this.webBrowser.Navigate(addCredsToUri(curUri));
                        }));

                        //this.webBrowser.Refresh();
                    }
                    else
                    {
                        return false;
                    }
                    if (result == false)
                    {
                        PassInValidTimes--;
                        return result;
                    }
                    else
                    {
                        PassSuccessTimes--;
                    }
                    ///验证是否获取到数据
                    HttpResult curResult = GetHttpHtml(new UrlInfo(this.curUri.ToString()));
                    if (UrlContentLimit(curResult.Html))
                    {
                        checkBox1.Checked = false;
                        PassSuccessTimes = 0;
                        return false;
                    }
                    if (PassInValidTimes <= 0 || PassSuccessTimes <= 0)//失败20次后进行屏蔽
                    {
                        checkBox1.Checked = false;

                    }
                    waitBrowerMouseUpResponse = false;
                    return result;

                }
            }
            return false;
        }
        /// <summary>
        /// 自动登陆
        /// </summary>
        /// <returns></returns>
        private bool GeetestChartAutoLoin()
        {
            try
            {
                return AliyunAutoLoin();
            }
            catch (Exception ex)
            {
                return false;
            }
            ///app登陆模式
            if (searchType == SearchType.EnterpriseGuidByKeyWord_APP)
            {
                return AppAutoLoin();
            }
            if (USEWEBPROXY)
            {
                hi.ProxyIP = GetWebProxyString();
            }
            geetestHelper = new PassGeetestHelper();
            var nameNormal = this.textBox1.Text.Trim();
            var pwdNormal = this.textBox2.Text.Trim();
            if (string.IsNullOrEmpty(nameNormal) || string.IsNullOrEmpty(pwdNormal))
            {
                return false;
            }
            var validUrl = "";
            var postFormat = "";
            bool result = false;
            this.richTextBoxInfo.AppendText("等待过验证码");
            // var postFormat = "geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}%7Cjordan&requestType=search_enterprise";
            var passResult = geetestHelper.PassGeetest(hi, postFormat, validUrl, Settings.SimulateCookies);
            result = passResult.Status;
            // this.richTextBoxInfo.Document.Blocks.Clear();
            this.richTextBoxInfo.AppendText("已自动过验证码");
            this.textBox4.Text = passResult.LastPoint;
            //this.webBrowser.Refresh();

            if (passResult.Status)
            {

                hi.Url = "http://www.qichacha.com/user_loginaction";
                hi.Refer = "http://www.qichacha.com/user_login";
                hi.PostData = string.Format("nameNormal={0}&pwdNormal={1}&geetest_challenge={2}&geetest_validate={3}&geetest_seccode={3}%7Cjordan", nameNormal, pwdNormal, passResult.Challenge, passResult.ValidCode);

                var ho = HttpManager.Instance.ProcessRequest(hi);
                if (ho.IsOK)
                {
                    if (ho.TxtData.Contains("true"))
                    {
                        LastGetPassGeetestTime = DateTime.Now;
                        Settings.SimulateCookies = ho.Cookies;
                        Console.WriteLine("过验证码模拟登陆成功");
                        webBrowser.Navigate(addCredsToUri(siteIndexUrl));
                        return true;
                    }
                    if (ho.TxtData.Contains("密码错误"))
                    {
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("errorPassWord", "1").Add("isInvalid", "1"), Query = Query.EQ("name", Settings.LoginAccount), Name = DataTableAccount, Type = StorageType.Update });
                        StartDBChangeProcessQuick();
                    }
                }
                var resultText = geetestHelper.GetLastPoint(hi);
                this.textBox4.Text = resultText;

            }
            return false;
        }
        public bool AutoLogout()
        {
            //this.checkBoxGuard.Checked = false;
            timerStop();
            HttpResult result = new HttpResult();
            try
            {
                var item = new HttpItem()
                {
                    URL = "http://www.qichacha.com/user_logout",
                    Method = "get",//URL     可选项 默认为Get   
                    ContentType = "text/html",//返回类型    可选项有默认值 
                    Timeout = Settings.Timeout,
                    Cookie = Settings.SimulateCookies
                };



                result = http.GetHtml(item);

            }
            catch (WebException ex)
            {

            }
            catch (TimeoutException ex)
            {

            }
            catch (Exception ex)
            {

            }


            if (result.StatusCode == HttpStatusCode.OK)
            {
                ShowMessageInfo("退出成功", true);
                // this.webBrowser.Navigate(addCredsToUri(this.textBox.Text));
                return true;
            }
            else
            {
                ShowMessageInfo("退出失败", true);
                return false;
            }
        }
        public int maxFailLoginTimes = 3;
        /// <summary>
        /// aliyun自动登陆
        /// </summary>
        /// <returns></returns>
        private bool AliyunAutoLoin()
        {
            var timeSpan = DateTime.Now - Settings.LastLoginTime;
            var secdLimit = 10;
            if (autoChangeAccountCHK.Checked)
            {
                secdLimit = 60;
            }
            if (timeSpan.TotalSeconds < secdLimit)//没限制 15秒没取到数据
            {
                ShowMessageInfo(secdLimit.ToString() + "秒内无法重复进行登陆");
                return false;
            }
            Settings.LastLoginTime = DateTime.Now;
            if (autoChangeAccountCHK.Checked)
            {
                if (maxFailLoginTimes <= 0)
                {
                    ShowMessageInfo("登陆失败太多");
                    this.autoChangeAccountCHK.Checked = false;
                    this.checkBoxGuard.Checked = false;
                    timerStop(); checkBox1.Checked = false;
                    return false;
                    // MessageBox.Show("登陆失败太多");


                    //Application.Exit();
                }
            }
            ///app登陆模式
            if (searchType == SearchType.EnterpriseGuidByKeyWord_APP)
            {
                return AppAutoLoin();
            }
            if (USEWEBPROXY)
            {
                //  hi.ProxyIP = GetWebProxyString();
            }
            geetestHelper = new PassGeetestHelper();
            var nameNormal = this.textBox1.Text.Trim();
            var pwdNormal = this.textBox2.Text.Trim();
            if (string.IsNullOrEmpty(nameNormal) || string.IsNullOrEmpty(pwdNormal))
            {
                return false;
            }
            var validUrl = "";
            var postFormat = "";
            bool result = false;
            this.richTextBoxInfo.AppendText("等待过验证码");

            //hi.Url = "http://www.qichacha.com/user_login";
            //var ho = HttpManager.Instance.ProcessRequest(hi);
            //if (ho.IsOK)
            //{
            //   // Settings.SimulateCookies = ho.Cookies;
            //}
            hi.Url = "http://101.200.187.122:9600/passali/fuckali.oko?uid=01161add5a3c4c55bd9c133baa9effd0&data=QNYX|login|http://www.qichacha.com/user_login";
            hi.Refer = "http://www.qichacha.com/user_login";
            var ho = HttpManager.Instance.ProcessRequest(hi);


            // this.textBox4.Text = passResult.LastPoint;
            //this.webBrowser.Refresh();
            if (ho.IsOK)
            {
                //var _url = ho.TxtData;
                // var _result= GetHttpHtml(new UrlInfo(_url), "http://www.qichacha.com/user_login");
                //if (_result.StatusCode==HttpStatusCode.OK)
                {
                    //csessionid
                    var csessionid_one = Toolslib.Str.Sub(ho.TxtData, "csessionid\":\"", "\"");
                    var sig_one = Toolslib.Str.Sub(ho.TxtData, "value\":\"", "\"");
                    var endIndex = ho.TxtData.IndexOf("|");
                    if (endIndex == -1) return false;
                    var token_one = ho.TxtData.Substring(0, endIndex);
                    // hi.Dispose();
                    //HttpManager.Instance.InitWebClient(hi, true, 30, 30);
                    // hi = new HttpInput();
                    hi.Url = "http://www.qichacha.com/user_loginaction";
                    hi.Refer = "http://www.qichacha.com/user_login";
                    // hi.PostData = string.Format("nameNormal={0}&pwdNormal={1}&geetest_challenge={2}&geetest_validate={3}&geetest_seccode={3}%7Cjordan", nameNormal, pwdNormal, passResult.Challenge, passResult.ValidCode);
                    hi.PostData = string.Format("nameNormal={0}&pwdNormal={1}&csessionid_one={2}&sig_one={3}&token_one={4}&scene_one=login", nameNormal, pwdNormal, csessionid_one, sig_one, token_one);
                    //nameNormal=18575103574&pwdNormal=gfky001&csessionid_one=ADTqX0-X4LmWXM8QxtRMLdzAfT3y03Qk16lweN_sMLQX07QT6YBjpNhwqF9K5Xsp&sig_one=04z7oPfC6qImzjEBAkepuALZSMOGIGJfnt9rzbBHHr4gXSkvPS4cgMLsbPLLo2rWbXegQWqqcOb6DdJ9Np4J56RvCNW0_dAarX8jDz3u2vyDV8NbUYkYapyCZbc44FcwNT5DwzRk9TqDxKAxstnjJryHbW1ZvgPfq58FZp4yxSbVc&token_one=QNYX%3A1473643040596%3A0.009048926136596136&scene_one=login

                    //ho = HttpManager.Instance.ProcessRequest(hi);
                    var tempResult = GetPostDataLogin(new UrlInfo(hi.Url) { PostData = hi.PostData }, hi.Refer, true);
                    var resultHtml = tempResult.Html;
                    // var resultHtml = ho.TxtData;
                    if (tempResult.StatusCode == HttpStatusCode.OK)
                    {

                        //var cookies = tempResult.Cookie;
                        if (resultHtml.Contains("true"))
                        {
                            Settings.SimulateCookies = tempResult.Cookie;
                            LastGetPassGeetestTime = DateTime.Now;
                            ShowMessageInfo("过验证码模拟登陆成功");
                            Settings.LastLoginTime = DateTime.Now;
                            webBrowser.Navigate(addCredsToUri(siteIndexUrl));
                            maxFailLoginTimes = 3;//重置
                            return true;
                        }
                    }
                    else
                    {
                        ShowMessageInfo(resultHtml);
                    }
                    var resultText = geetestHelper.GetLastPoint(hi);
                    this.textBox4.Text = resultText;
                }
            }
            maxFailLoginTimes--;
            return false;
        }
        /// <summary>
        /// app模拟登陆
        /// </summary>
        /// <returns></returns>
        private bool AppAutoLoin()
        {
            RefreshToken();
            var phoneNum = this.textBox1.Text.Trim();
            var pwdNormal = this.textBox2.Text.Trim();
            var hashPwd = string.Empty;//加密后的hash
            var hitHashObj = allAccountHashMapList.Where(c => c.Text("password") == pwdNormal).FirstOrDefault();
            if (hitHashObj != null)
            {
                hashPwd = hitHashObj.Text("hash");
                var _url = new UrlInfo("https://app.qichacha.net/app/v1/admin/login");
                _url.PostData = string.Format("loginType=2&accountType=1&account={0}&password={1}&identifyCode=&key=&token=&timestamp={2}&sign={3}", phoneNum, hashPwd, Settings.timestamp, Settings.sign);
                var result = GetPostDataAPP(_url);
                if (result.StatusCode == HttpStatusCode.OK && result.Html.Contains("成功"))
                {
                    var token = Toolslib.Str.Sub(result.Html, "access_token\":\"", "\"");
                    if (!string.IsNullOrEmpty(token))
                        Settings.AccessToken = token;
                    ShowMessageInfo(result.Html, true);
                    return true;
                }
            }
            ShowMessageInfo(pwdNormal + "密码无对应hash值", true);
            return false;
        }

        /// <summary>
        /// 返回代理服务器
        /// </summary>
        /// <returns></returns>
        public WebProxy GetWebProxy()
        {
            // 设置代理服务器
            var proxy = new WebProxy();
            proxy.Address = new Uri(string.Format("{0}:{1}", proxyHost, proxyPort));
            proxy.Credentials = new NetworkCredential(proxyUser, proxyPass);
            return proxy;
        }
        public string GetWebProxyString()
        {
            if (!USEWEBPROXY) { return string.Empty; }
            return string.Format("{0}:{1}@{2}:{3}", proxyUser, proxyPass, proxyHost.Replace("http://", ""), proxyPort);
        }
        public string GetWebBrowserProxyString()
        {
            if (!USEWEBPROXY) { return string.Empty; }
            return string.Format("{0}:{1}", proxyHost, proxyPort);
        }
        public string ForceChangeProxy()
        {
            ShowMessageInfo("正在进行IP切换");
            var result = GetHttpHtml(new UrlInfo("http://proxy.abuyun.com/switch-ip"));
            if (result.StatusCode == HttpStatusCode.OK)
            {
                ShowMessageInfo("切换ip为" + result.Html, true);
                return result.Html;
            }
            else
            {
                ShowMessageInfo("获取ip切换url失败");
                return string.Empty;
            }
        }
        /// <summary>
        /// 账号申请
        /// </summary>
        private void AccountApply(string loginAccount)
        {
            if (string.IsNullOrEmpty(loginAccount)) return;
            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isUsed", "1"), Query = Query.EQ("name", loginAccount), Name = DataTableAccount, Type = StorageType.Update });
            StartDBChangeProcessQuick();
        }

        private void DeviceAccountApply(string loginAccount)
        {
            if (string.IsNullOrEmpty(loginAccount)) return;
            Settings.neeedChangeAccount = false;
            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isUsed", "1"), Query = Query.EQ("deviceId", loginAccount), Name = QCCDeviceAccount, Type = StorageType.Update });
            StartDBChangeProcessQuick();
        }
        /// <summary>
        /// 设备账号注销
        /// </summary>
        /// <param name="loginAccount"></param>
        private void DeviceAccountRelease(string loginAccount)
        {

            if (string.IsNullOrEmpty(loginAccount)) return;

            var updateBson = new BsonDocument().Add("isUsed", "0");
            ///账号爬取统计，防止被封，查看爬去个数
            var curLoginAccountObj = allDeviceAccountList.Where(c => c.Text("name") == loginAccount).FirstOrDefault();
            if (curLoginAccountObj != null)
            {


                //Console.WriteLine("{0,-11}= {1}", s, Enum.Format(searchTypeEnum, Enum.Parse(searchTypeEnum, s), "s"));
                var columnName = string.Format("{0}_add", searchType.ToString());
                var curAddional = curLoginAccountObj.Int(columnName);
                if (curAddional != 0)
                {
                    var finalResult = curLoginAccountObj.Int(searchType.ToString()) + curAddional;
                    updateBson.Set(searchType.ToString(), finalResult.ToString());//增加值
                    curLoginAccountObj.Set(columnName, "0");
                    curLoginAccountObj.Set(searchType.ToString(), finalResult.ToString());// 当前值
                }




                ///设置为已完成当前任务
                if (Settings.MaxAccountCrawlerCount >0 && curLoginAccountObj.Int(searchType.ToString()) > Settings.MaxAccountCrawlerCount)
                {
                    updateBson.Add("status", "1");
                    updateBson.Add("maxOrverLoad", "1");
                }

            }
             
            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBson, Query = Query.EQ("deviceId", loginAccount), Name = QCCDeviceAccount, Type = StorageType.Update });
            //保存统计
            SaveKeyWordHitCount();
            InitKeyWordHitCount(cityNameStr);
            StartDBChangeProcessQuick();
        }

        /// <summary>
        /// 账号释放
        /// </summary>
        private void AccountRelease(string loginAccount)
        {

            if (string.IsNullOrEmpty(loginAccount)) return;

            var updateBson = new BsonDocument().Add("isUsed", "0");
            ///账号爬取统计，防止被封，查看爬去个数
            var curLoginAccountObj = allAccountList.Where(c => c.Text("name") == loginAccount).FirstOrDefault();
            if (curLoginAccountObj != null)
            {
                ///设置为异常
                if (autoChangeAccountCHK.Checked)
                {
                    if (curLoginAccountObj.Int(searchType.ToString() + "_add") < 10 && curLoginAccountObj.Int(searchType.ToString()) < 10)
                    {

                        var busyCount = curLoginAccountObj.Int("isBusyCount") + 1;
                        updateBson.Add("isBusyCount", busyCount.ToString());
                        if (busyCount >= 3)
                        {
                            updateBson.Add("isBusy", "1");
                        }

                    }


                }
                Type searchTypeEnum = typeof(SearchType);
                foreach (string _searchType in Enum.GetNames(searchTypeEnum))
                {
                    //Console.WriteLine("{0,-11}= {1}", s, Enum.Format(searchTypeEnum, Enum.Parse(searchTypeEnum, s), "s"));
                    var columnName = string.Format("{0}_add", _searchType);
                    var curAddional = curLoginAccountObj.Int(columnName);
                    if (curAddional != 0)
                    {
                        var finalResult = curLoginAccountObj.Int(_searchType) + curAddional;
                        updateBson.Set(_searchType, finalResult.ToString());//增加值
                        curLoginAccountObj.Set(columnName, "0");
                        curLoginAccountObj.Set(_searchType, finalResult.ToString());// 当前值
                    }

                }


                ///设置为已完成当前任务
                if (Settings.MaxAccountCrawlerCount >= 0 && curLoginAccountObj.Int(searchType.ToString()) >= Settings.MaxAccountCrawlerCount)
                {
                    updateBson.Add("status", "1");
                }

            }
            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBson, Query = Query.EQ("name", loginAccount), Name = DataTableAccount, Type = StorageType.Update });
            StartDBChangeProcessQuick();
        }

        /// <summary>
        /// 切换App设备账号
        /// </summary>
        public void AutoChangeDeviceAccount()
        {

            autoChangeAccountCHK.Invoke(new Action(() =>
            {

                if (autoChangeAccountCHK.Checked == true)
                {

                    if (allDeviceAccountList.Count() > 0)
                    {
                        var oldDeviceId = Settings.DeviceId;
                        var hitAccountList = allDeviceAccountList.Where(c => c.Text("deviceId") != Settings.DeviceId).ToList();
                        #region 随机获取数据
                        var rnd = new Random();
                        var maxLength = (hitAccountList.Count() - 1) / 10;
                        if (hitAccountList.Count() < 10)
                        {
                            maxLength = hitAccountList.Count() - 1;
                        }
                        if (maxLength > 100)
                        {
                            maxLength = 100;
                        }
                        if (maxLength <= 0)
                        {
                            maxLength = 1;
                        }
                        #endregion
                        var skipCount = rnd.Next(0, maxLength);
                        var nextAccount = hitAccountList.Skip(skipCount).FirstOrDefault();
                        if (nextAccount != null)
                        {
                             
                            //checkBox1.Checked = false;
                            checkBoxGuard.Checked = true;
                            if (SetSetting(nextAccount))//可能失败
                            {
                                  // ForceChangeProxy();//手动切换ip
                                ReloadLoginAccount();//放在后面进行账号读取，防止账号获得数据但是没有进行其获取 的数据被重置
                                                     //GeetestChartAutoLoin();
                                timerStart();
                            }

                        }
                    }
                    else
                    {
                        autoChangeAccountCHK.Checked = false;//取消自动切换账户
                        checkBoxGuard.Checked = false;
                        ShowMessageInfo("无可用账户");
                    }
                }

            }));

        }

        /// <summary>
        /// 自动切换账号 并登陆,设定自动切换账号间隔，与使用前先进行刷新ip操作
        /// </summary>
        public void AutoChangeAccount()
        {
            //lock (lockChangeAccount)//2017.1.11可能导致死锁，或者同时进去
            {
                if (!ContinueMethodByBusyGear("AutoChangeAccount",2)) {
                   
                   return;
                }
                timerStop();
                ///切换app设备账号，后续需要登陆在使用账号登陆
                if (searchType == SearchType.EnterpriseGuidByKeyWord_APP)
                {

                    AutoChangeDeviceAccount();
                    return;

                }

                autoChangeAccountCHK.Invoke(new Action(() =>
                {

                    if (autoChangeAccountCHK.Checked == true)
                    {

                        if (allAccountList.Count() > 0)
                        {

                            var hitAccountList = allAccountList.Where(c => c.Int("isBusy") == 0 && c.Int("isUsed") == 0 && c.Text("name") != Settings.LoginAccount && c.Int("status") == 0).ToList();
                        #region 随机获取数据
                        var rnd = new Random();
                            var maxLength = (hitAccountList.Count() - 1) / 10;
                            if (hitAccountList.Count() < 10)
                            {
                                maxLength = hitAccountList.Count() - 1;
                            }
                            if (maxLength > 50)//前50里面变换
                        {
                                maxLength = 50;
                            }
                        #endregion
                        var skipCount = rnd.Next(0, maxLength);
                            var nextAccount = hitAccountList.Skip(skipCount).FirstOrDefault();
                            if (nextAccount != null)
                            {
                                var accountName = string.Empty;
                                if (nextAccount.Int("isUsed") == 1)
                                {
                                    accountName = string.Format("{0}_占用", nextAccount.Text("name"));
                                }
                                else
                                {
                                    accountName = string.Format("{0}", nextAccount.Text("name"));
                                }
                                if (nextAccount.Int("isBusy") == 1)
                                {
                                    accountName = string.Format("{0}_频繁", accountName);
                                }
                                var index = this.comboBox1.Items.IndexOf(accountName);
                                if (index != -1)
                                {
                                    this.comboBox1.SelectedIndex = index;
                                    checkBox1.Checked = true;
                                    checkBoxGuard.Checked = true;
                                // ForceChangeProxy();//手动切换ip
                                ReloadLoginAccount();//放在后面进行账号读取，防止账号获得数据但是没有进行其获取 的数据被重置
                                if (!GeetestChartAutoLoin())//模拟自动登陆,登陆失败
                                {
                                        allAccountList.Remove(nextAccount);

                                    }


                                }
                            }
                        }
                        else
                        {
                            autoChangeAccountCHK.Checked = false;//取消自动切换账户
                        checkBoxGuard.Checked = false;
                            ShowMessageInfo("无可用账户");
                        }
                    }

                }));
            }
        }

        public string ToUnicodeString(string str)
        {
            StringBuilder strResult = new StringBuilder();
            if (!string.IsNullOrEmpty(str))
            {
                for (int i = 0; i < str.Length; i++)
                {
                    strResult.Append("\\u");
                    strResult.Append(((int)str[i]).ToString("x"));
                }
            }
            return strResult.ToString();
        }

        public string FromUnicodeString(string str)
        {
            //最直接的方法Regex.Unescape(str);
            StringBuilder strResult = new StringBuilder();
            if (!string.IsNullOrEmpty(str))
            {
                string[] strlist = str.Replace("\\", "").Split('u');
                try
                {
                    for (int i = 1; i < strlist.Length; i++)
                    {
                        int charCode = Convert.ToInt32(strlist[i], 16);
                        strResult.Append((char)charCode);
                    }
                }
                catch (FormatException ex)
                {
                    return Regex.Unescape(str);
                }
            }
            return strResult.ToString();
        }
        /// <summary>
        /// 获取QCCpost 搜索关键字
        /// </summary>
        /// <param name="keyWord"></param>
        /// <returns></returns>
        public HttpResult GetPostData(UrlInfo curUrlObj, string refer = "", bool useProxy = true)
        {
            http = new SimpleCrawler.HttpHelper();
            if (searchType == SearchType.EnterpriseGuidByKeyWord_APP)
            {
                return GetPostDataAPP(curUrlObj);
            }
            //创建Httphelper参数对象
            HttpItem item = new HttpItem()
            {
                URL = curUrlObj.UrlString,//URL     必需项    

                ContentType = "application/x-www-form-urlencoded; charset=UTF-8",//返回类型    可选项有默认值 

                Timeout = 1500,
                Accept = "*/*",
                Encoding = null,//编码格式（utf-8,gb2312,gbk）     可选项 默认类会自动识别
                                //Encoding = Encoding.Default,
                Method = "post",//URL     可选项 默认为Get
                                //Timeout = 100000,//连接超时时间     可选项默认为100000
                                //ReadWriteTimeout = 30000,//写入Post数据超时时间     可选项默认为30000
                                //IsToLower = false,//得到的HTML代码是否转成小写     可选项默认转小写
                                //Cookie = "",//字符串Cookie     可选项
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko",//用户的浏览器类型，版本，操作系统     可选项有默认值
                Referer = "http://www.qichacha.com/",//来源URL     可选项
                Postdata = curUrlObj.PostData,
                Allowautoredirect = true,
                Cookie = Settings.SimulateCookies
            };
            item.PostEncoding = System.Text.Encoding.GetEncoding("utf-8");
            if (!string.IsNullOrEmpty(refer))
            {
                item.Referer = refer;
            }
            if (USEWEBPROXY && useProxy)
            {
                item.WebProxy = getWebProxy;
            }

            //item.Header.Add("Accept-Encoding", "gzip, deflate");
            //item.Header.Add("Accept-Language", "zh-CN");
            //item.Header.Add("charset", "UTF-8");
            //item.Header.Add("X-Requested-With", "XMLHttpRequest");
            //请求的返回值对象
            var result = http.GetHtml(item);
            return result;
        }
        /// <summary>
        /// app post获取
        /// </summary>
        /// <param name="curUrlObj"></param>
        /// <returns></returns>
        public HttpResult GetPostDataAPP(UrlInfo curUrlObj, bool tryAgain = false)
        {

            //请求的返回值对象
            try
            {
                var result = GetPostDataFix(curUrlObj);

                return result;
            }
            catch (Exception ex)
            {
                //if (tryAgain==false) { 
                //RefreshToken();//刷新token
                //return GetPostDataAPP(curUrlObj,true);
                //}
                //else { 
                return new HttpResult() { Html = "", StatusCode = HttpStatusCode.BadRequest };
                //}

            }
        }
        public HttpResult GetPostDataFix(UrlInfo curUrlObj)
        {

            //string.Format("refreshToken=f128619e442a6efe44c1544b4c926824&timestamp=1473757386869&appId=80c9ef0fb86369cd25f90af27ef53a9e&sign=a5ae576bcddcba5df634f041995e45cd54b255e6");
            hi.Url = curUrlObj.UrlString.ToString();
            //hi.Refer = "http://app.qichacha.net";
            hi.PostData = curUrlObj.PostData;
            hi.UserAgent = "okhttp/3.2.0";
            hi.HeaderSet("Content-Type", "application/x-www-form-urlencoded");
            // hi.HeaderSet("Content-Length","154");
            // hi.HeaderSet("Connection","Keep-Alive");
            hi.HeaderSet("Accept-Encoding", "gzip");
            if (string.IsNullOrEmpty(Settings.AccessToken))
            {
                RefreshToken();
            }
            hi.HeaderSet("Authorization", string.Format("Bearer {0}", Settings.AccessToken));
            var ho = LibCurlNet.HttpManager.Instance.ProcessRequest(hi);
            if (ho.IsOK)
            {

                if (!ho.TxtData.Contains("成功"))
                {
                    if (ho.TxtData.Contains("异常"))
                    {
                        timerStop();
                        ShowMessageInfo(ho.TxtData, true);
                        //10秒内不能重复执行
                        if (ContinueMethodByBusyGear("DeviceIsInvalidSetting",5))
                        {
                            //RefreshToken();//可能出现情况频率太高情况 在更换设备Id后还有许多异常的url访问未返回，当设备Id访问成功后
                            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isInvalid", "1"), Query = Query.EQ("deviceId", Settings.DeviceId), Name = QCCDeviceAccount, Type = StorageType.Update });
                            StartDBChangeProcessQuick();
                        }
                        
                        if (autoChangeAccountCHK.Checked)
                            {
                                AutoChangeAccount();
                            }
                            else
                            {
                                timerStop();
                                this.checkBoxGuard.Checked = false;
                                guardTimer.Stop();
                            }
                       
                    }
                    else
                    if (ho.TxtData.Contains("失效") || ho.TxtData.Contains("签名失败") || ho.TxtData.Contains("非法请求"))
                    {
                        timerStop();
                        if (ContinueMethodByBusyGear("DeviceRefreshToken", 5))//防止重复率刷新请求
                        {
                            var result = RefreshToken();
                            if (result.Contains("成功"))
                            {
                                timerStart();
                            }
                        }
                    }
                }

                return new HttpResult() { StatusCode = HttpStatusCode.OK, Html = ho.TxtData };
            }
            else
            {
                return new HttpResult() { StatusCode = HttpStatusCode.Forbidden };
            }

        }
        /// <summary>
        /// 调度器列表
        /// </summary>
        private ConcurrentDictionary<string,DateTime> MehodBusyGearList = new ConcurrentDictionary<string,DateTime>();
        /// <summary>
        /// 防止因为异步访问某个时间太频繁添加的调度器，及需要某个时间段内才能第二次访问
        /// </summary>
        /// <param name="key"></param>
        /// <param name="secondsSpan"></param>
        /// <returns></returns>
        private bool ContinueMethodByBusyGear(string key,int secondsSpan=5)
        {
            lock (locakTimeGear)
            {
                try
                {
                    var lastAccessTime = DateTime.Now.AddSeconds(-secondsSpan);
                    if (MehodBusyGearList.ContainsKey(key))
                    {
                        lastAccessTime = MehodBusyGearList[key];
                    }
                    var timeSpan = DateTime.Now - lastAccessTime;
                    if (timeSpan.TotalSeconds < secondsSpan)//没限制 15秒没取到数据
                    {
                        ShowMessageInfo(string.Format("{0}s内无法重复进行{1}操作", secondsSpan, key));
                        return false;
                    }
                    if (MehodBusyGearList.ContainsKey(key))
                    {
                        MehodBusyGearList[key] = DateTime.Now;
                    }
                    else
                    {
                        MehodBusyGearList.TryAdd(key, DateTime.Now);
                    }

                }
                catch (Exception ex)
                {
                    //throw new Exception(ex.Message + "MethodBusyGear");
                    ShowMessageInfo(ex.Message + "MethodBusyGear");
                }
            }
            return true;
        }

        /// <summary>
        /// 修复Url,此处因为其curl的timestamp 与sign值 每个账号需要的只不一样，因此需要进行替换,才会不限制
        /// </summary>
        /// <param name="curUrlObj"></param>
        /// <returns></returns>
        public string FixUrlSignStr(UrlInfo curUrlObj)
        {
            var url = curUrlObj.UrlString;
            //补齐参数
            if (searchType == SearchType.EnterpriseGuidByKeyWord_APP) { 
                var _timestamp = GetUrlParam(curUrlObj.UrlString, "timestamp");
                var _sign = GetUrlParam(curUrlObj.UrlString, "sign");
                var _token = GetUrlParam(curUrlObj.UrlString, "token");
                if (url.Contains("&timestamp=") && _timestamp != Settings.timestamp)
                {

                    url = url.Replace("&timestamp=" + _timestamp, "&timestamp=" + Settings.timestamp);
                }
                else
                {
                    url = url + "&timestamp=" + Settings.timestamp;
                }

                if (url.Contains("&sign=") && _sign != Settings.sign)
                {

                    url = url.Replace("&sign=" + _sign, "&sign=" + Settings.sign);
                }
                else
                {
                    url = url + "&sign=" + Settings.sign;
                }

                //if (url.Contains("&token=") && _token != Settings.AccessToken)
                //{

                //    url = url.Replace("&token=" + _token, "&token=" + Settings.AccessToken);
                //}
                //else
                //{
                //    url = url + "&token=" + Settings.AccessToken;
                //}
                 
               
            }
            //if (curUrlObj.UrlString.Contains("-")) { 

            //    try
            //    {
            //        var startDateBeginParam = GetUrlParam(curUrlObj.UrlString, "startDateBegin");
            //        var startDateEndParam = GetUrlParam(curUrlObj.UrlString, "startDateEnd");
            //        var _startDateBegin = DateTime.Parse(startDateBeginParam);
            //        var _startDateEnd = DateTime.Parse(startDateEndParam);
            //        url = url.Replace("&startDateBegin=" + startDateBeginParam, string.Format("&startDateBegin={0}", _startDateBegin.ToString("yyyyMMdd")));
            //        url = url.Replace("&startDateEnd=" + startDateEndParam, string.Format("&startDateEnd={0}", _startDateEnd.ToString("yyyyMMdd")));
            //    }
            //    catch (Exception ex)
            //    {
            //        ShowMessageInfo(ex.Message, true);
            //    }
            //}
            return url;
        }
        /// <summary>
        /// 清理标识防止url 重复添加执行
        /// </summary>
        /// <param name="curUrlObj"></param>
        /// <returns></returns>
        public string ClearUrlSignStr(UrlInfo curUrlObj)
        {
            var url = curUrlObj.UrlString;
            //补齐参数
            if (searchType == SearchType.EnterpriseGuidByKeyWord_APP)
            {
                var _timestamp = GetUrlParam(curUrlObj.UrlString, "timestamp");
                var _sign = GetUrlParam(curUrlObj.UrlString, "sign");
                var _token = GetUrlParam(curUrlObj.UrlString, "token");
                if (!string.IsNullOrEmpty(_timestamp))
                {
                    url = url.Replace("&timestamp="+_timestamp, "");
                }
                if (!string.IsNullOrEmpty(_sign))
                {
                    url = url.Replace("&sign=" + _sign, "");
                }
                if (!string.IsNullOrEmpty(_token))
                {
                    url = url.Replace("&token=" + _token,"");
                }
            }
            
            return url;
        }
        /// <summary>
        /// 判断 url 是否同步
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private bool IsSameUrlSign(string url)
        {

            var _timestamp = GetUrlParam(url, "timestamp");
            var _sign = GetUrlParam(url, "sign");
            return _timestamp == Settings.timestamp && _sign == Settings.sign;
        }

        public HttpResult GetHttpHtmlAPP(UrlInfo curUrlObj, bool tryAgain = false)
        {
            http = new SimpleCrawler.HttpHelper();
            var url = FixUrlSignStr(curUrlObj);
            var item = new SimpleCrawler.HttpItem()
            {
                URL = url,
                Method = "get",//URL     可选项 默认为Get   
                               // ContentType = "text/html",//返回类型    可选项有默认值 
                UserAgent = "okhttp/3.2.0",
                ContentType = "application/x-www-form-urlencoded",
            };

            // item.Header.Add("Content-Type", "application/x-www-form-urlencoded");
            // hi.HeaderSet("Content-Length","154");
            // hi.HeaderSet("Connection","Keep-Alive");

            item.Header.Add("Accept-Encoding", "gzip");
            if (string.IsNullOrEmpty(Settings.AccessToken))
            {
                RefreshToken();
            }
            item.Header.Add("Authorization", string.Format("Bearer {0}", Settings.AccessToken));
            if (USEWEBPROXY)
            {
                item.WebProxy = GetWebProxy();
            }
            var result = http.GetHtml(item);
            if (!result.Html.Contains("成功"))
            {
                if (result.Html.Contains("异常"))
                {
                    if (IsSameUrlSign(url) == false)//只尝试一次
                    {
                        ShowMessageInfo("当前url与settings.sign不一致 进行重试");
                        return GetHttpHtmlAPP(curUrlObj);
                    }
                    timerStop();//防止重复
                    ShowMessageInfo(result.Html, true);
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isInvalid", "1"), Query = Query.EQ("deviceId", Settings.DeviceId), Name = QCCDeviceAccount, Type = StorageType.Update });
                    StartDBChangeProcessQuick();
                    if (autoChangeAccountCHK.Checked)
                    {
                        
                           AutoChangeAccount();
                       
                    }
                    else
                    {
                        timerStop();
                        this.checkBoxGuard.Checked = false;
                        guardTimer.Stop();
                    }

                }
                else
                if (result.Html.Contains("失效") || result.Html.Contains("签名失败") || result.Html.Contains("非法"))
                {
                    timerStop();
                    var tempResult = RefreshToken();
                    if (tempResult.Contains("成功"))
                    {
                        timerStart();
                    }
                }
            }

            //return new HttpResult() { StatusCode = HttpStatusCode.OK, Html = result.Html };
            return result;
            //return GetPostDataFix(curUrlObj);

        }
        public HttpResult GetPostDataLogin(UrlInfo curUrlObj, string refer = "", bool useProxy = true)
        {
            if (searchType == SearchType.EnterpriseGuidByKeyWord_APP)
            {
                return GetPostDataAPP(curUrlObj);
            }
            //创建Httphelper参数对象
            HttpItem item = new HttpItem()
            {
                URL = curUrlObj.UrlString,//URL     必需项    

                ContentType = "application/x-www-form-urlencoded; charset=UTF-8",//返回类型    可选项有默认值 

                Timeout = 2500,
                Accept = "application/json, text/javascript, */*; q=0.01",
                Encoding = null,//编码格式（utf-8,gb2312,gbk）     可选项 默认类会自动识别
                                //Encoding = Encoding.Default,
                Method = "post",//URL     可选项 默认为Get
                                //Timeout = 100000,//连接超时时间     可选项默认为100000
                                //ReadWriteTimeout = 30000,//写入Post数据超时时间     可选项默认为30000
                                //IsToLower = false,//得到的HTML代码是否转成小写     可选项默认转小写
                                //Cookie = "",//字符串Cookie     可选项
                UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; InfoPath.2; .NET4.0C; .NET4.0E)",//用户的浏览器类型，版本，操作系统     可选项有默认值
                Referer = "http://www.qichacha.com/user_login",//来源URL     可选项
                Postdata = curUrlObj.PostData,
                Allowautoredirect = true,
                Cookie = Settings.SimulateCookies
            };
            //item.PostEncoding = System.Text.Encoding.GetEncoding("utf-8");
            item.Header.Add("Accept-Encoding", "gzip, deflate");
            item.Header.Add("X-Requested-With", "XMLHttpRequest");

            if (USEWEBPROXY && useProxy)
            {
                item.WebProxy = getWebProxy;
            }
            item.Header.Add("Pragma", "no-cache");
            //item.Header.Add("Accept-Encoding", "gzip, deflate");
            item.Header.Add("Accept-Language", "zh-CN");
            //item.Header.Add("charset", "UTF-8");
            //item.Header.Add("X-Requested-With", "XMLHttpRequest");

            //请求的返回值对象
            var result = http.GetHtml(item);
            return result;
        }
        /// <summary>
        /// 返回请求数据
        /// </summary>
        /// <param name="curUrlObj"></param>
        /// <returns></returns>
        public HttpResult GetHttpHtml(UrlInfo curUrlObj, string refer = "")
        {
            http = new SimpleCrawler.HttpHelper();
            if (searchType == SearchType.EnterpriseGuidByKeyWord_APP)
            {
                return GetHttpHtmlAPP(curUrlObj);
            }
            HttpResult result = new HttpResult();
            try
            {
                var item = new HttpItem()
                {
                    URL = curUrlObj.UrlString,
                    Method = "get",//URL     可选项 默认为Get   
                    ContentType = "text/html",//返回类型    可选项有默认值 
                    Timeout = Settings.Timeout,
                    Cookie = Settings.SimulateCookies
                };
                if (!string.IsNullOrEmpty(refer))
                {
                    item.Referer = refer;
                }
                if (USEWEBPROXY)
                {
                    item.WebProxy = getWebProxy;
                }



                result = http.GetHtml(item);

            }
            catch (WebException ex)
            {

            }
            catch (TimeoutException ex)
            {

            }
            catch (Exception ex)
            {

            }
            return result;
        }
        #endregion
        #region 控件事件
        //void Form1_NewWindow3(ref object ppDisp, ref bool Cancel, uint dwFlags, string bstrUrlContext, string bstrUrl)
        //{
        //    Cancel = true;
        //    webBrowser.Navigate(bstrUrl);
        //}
        private void Form1_Load(object sender, EventArgs e)
        {
           
            Settings.RefleshToken = "e2fa7f033b967be1cf5a2031488c9bc5";
            Settings.AccessToken = "ZWZjM2JiN2EtODM5My00NDMxLWE4N2ItYzVjMTY5Y2U2N2Yx";
            Settings.AppId = "80c9ef0fb86369cd25f90af27ef53a9e";
            Settings.DeviceId = "UygxNMGUAhsBAFbml5Qf92IW";//VymdBRAVA88DAMMz1sdnhOMT 
            Settings.timestamp = "1473261086387";
            Settings.sign = "bbc3e042182e49616adf674f2867d7558cd37d8c";
            Settings.LastAvaiableTokenTime = DateTime.Now.AddSeconds(-10);
            if (!String.IsNullOrEmpty(PublicSettings.Default.PubilcUserName))
                this.textBox1.Text = PublicSettings.Default.PubilcUserName;
            if (!String.IsNullOrEmpty(PublicSettings.Default.PubilcPassword))
                this.textBox2.Text = PublicSettings.Default.PubilcPassword;
            if (!String.IsNullOrEmpty(PublicSettings.Default.PubilcTakeCount))
                this.textBox3.Text = PublicSettings.Default.PubilcTakeCount;
            if (!String.IsNullOrEmpty(PublicSettings.Default.PubilcTimerElapse))
                this.textBox5.Text = PublicSettings.Default.PubilcTimerElapse;
            if (!String.IsNullOrEmpty(PublicSettings.Default.PublicProxyUid))
                this.ipProxyTxt.Text = PublicSettings.Default.PublicProxyUid;
            if (!String.IsNullOrEmpty(PublicSettings.Default.PublicProxyPwd))
                this.ipProxyTxt2.Text = PublicSettings.Default.PublicProxyPwd;
            allAccountHashMapList = dataop.FindAll(DataTableAccountHashMap).ToList();
            cityList = dataop.FindAll(DataTableCity).ToList();
            MongoConnectionStringBuilder builder = new MongoConnectionStringBuilder();
            builder.Server = new MongoServerAddress(ip, port);
            builder.DatabaseName = "SimpleCrawler";
            builder.Username = "MZsa";
            builder.Password = "MZdba";
            builder.SocketTimeout = new TimeSpan(00, 01, 59);
            dataop = new DataOperation(new MongoOperation(builder));
            _mongoDBOp = new MongoOperation(builder);

            if (USEWEBPROXY)
            {
                var webProxyStr = GetWebBrowserProxyString();
                getWebProxy = GetWebProxy();
                if (!string.IsNullOrEmpty(ipProxyTxt.Text) && !string.IsNullOrEmpty(ipProxyTxt2.Text))
                {
                    proxyUser = ipProxyTxt.Text;
                    proxyPass = ipProxyTxt2.Text;
                    webProxyStr = GetWebBrowserProxyString();
                    getWebProxy = GetWebProxy();
                    if (UseProxyCHK.Checked)
                    {
                        var ipSetting = new IEProxy(webProxyStr);
                        ipSetting.RefreshIESettings();
                    }
                }

            }
            //WebBrowserProxy.SetProxy("223.95.113.239:80");
            Settings.DBSaveCountLimit = 1;
            geetestHelper.GetCapUrl = "http://www.qichacha.com/index_getcap?rand={0}";
            this.webBrowser.Navigate(addCredsToUri(this.textBox.Text));
            //this.webBrowser.Navigate("about:blank");
            //(this.webBrowser.ActiveXInstance as SHDocVw.WebBrowser).NewWindow3  += new SHDocVw.DWebBrowserEvents2_NewWindow3EventHandler(Form1_NewWindow3);
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 1000;    // 1秒 = 1000毫秒,修改成可随机
            aTimer.Enabled = false;

            autoRestartTimer.Elapsed += new ElapsedEventHandler(OnAutoStartTimedEvent);
            autoRestartTimer.Interval = 4000;    // 1秒 = 1000毫秒
            autoRestartTimer.Enabled = false;

            //守护timer 模拟单击事件


            //InitialEnterpriseData();
            //aTimer.Enabled = true;
            this.comboBox.Items.Add("企业Info");//0s
            this.comboBox.Items.Add("企业Guid");//1
            this.comboBox.Items.Add("企业分类Guid");//2
            this.comboBox.Items.Add("企业城市分类Guid");//3
            this.comboBox.Items.Add("企业关键字强化无验证");////4http://www.qichacha.com/company/00bc8987-6200-47a2-88fb-c0be54b43808
            this.comboBox.Items.Add("企业城市分类关键字Guid");////4http://www.qichacha.com/search?key=%E5%8C%97%E4%BA%AC++%E9%A3%9F%E5%93%81%E6%B7%BB%E5%8A%A0%E5%89%82&type=enterprise&source=&isGlobal=Y
            this.comboBox.Items.Add("APP破解城市分类关键字Guid_APP");//6
             //var cityNameStr = "地块企业,佛山,北京,西安,烟台,上海,深圳,成都,福州,广州,杭州,黄山,济南,龙岩,南昌,南京,宁波,泉州,苏州,武汉,厦门,大连,长沙,合肥,镇江,宁波,中山,郑州,昆明,江苏,重庆";
            var cityNameStr = "太原,兰州,长春,海口,北海,南宁,保定,南京,苏州,常州,无锡,南通,西安,烟台,佛山,泉州,北京,上海,广州,深圳,成都,昆明,大连,青岛,哈尔滨,沈阳,日照,南宁,武汉,长沙,合肥,济南,郑州,南昌,天津,杭州,兰州,长春,海口,西宁,石家庄,宁波,贵阳,西宁,乌鲁木齐,呼和浩特,银川,拉萨,福州,厦门,漳州,莆田,三明,南平,龙岩,宁德市,宁德地区,东莞,重庆,嘉兴,惠州,珠海,汕头,中山,湛江,泉州,龙岩,南通,常州,镇江,连云港,舟山,黄山,烟台,";
           // var cityNameStr = "广州,韶关,深圳,珠海,汕头,佛山,江门,湛江,茂名,肇庆,惠州,梅州,汕尾,河源,阳江,清远,东莞,中山,潮州,揭阳,云浮";

            foreach (var cityName in cityNameStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries))
            {
                this.comboBox2.Items.Add(cityName);
            }
            HttpManager.Instance.InitWebClient(hi, true, 30, 30);
            hi.CurlObject.SetOpt(LibCurlNet.CURLoption.CURLOPT_PROXY, GetWebProxyString());
            Random rand = new Random(Environment.TickCount);
            #region 字段映射表
            EnterpriseInfoMapDic.Add("统一社会信用代码", "credit_no");
            EnterpriseInfoMapDic.Add("组织机构代码", "org_no");
            EnterpriseInfoMapDic.Add("注册号", "reg_no");
            EnterpriseInfoMapDic.Add("经营状态", "status");
            EnterpriseInfoMapDic.Add("公司类型", "type");
            EnterpriseInfoMapDic.Add("成立日期", "date");
            EnterpriseInfoMapDic.Add("法定代表", "oper_name");
            EnterpriseInfoMapDic.Add("营业期限", "limitDate");
            EnterpriseInfoMapDic.Add("注册资本", "reg_capi_desc");
            EnterpriseInfoMapDic.Add("所属行业", "domain");
            EnterpriseInfoMapDic.Add("英文名", "engName");
            EnterpriseInfoMapDic.Add("发照日期", "issueDate");
            EnterpriseInfoMapDic.Add("登记机关", "registrar");
            EnterpriseInfoMapDic.Add("企业地址", "address");
            EnterpriseInfoMapDic.Add("经营范围", "operationDomain");
            #endregion
            #region    APP字段映射表
            EnterpriseInfoMapDic_App.Add("KeyNo", "guid");
            EnterpriseInfoMapDic_App.Add("Name", "name");
            EnterpriseInfoMapDic_App.Add("CreditCode", "credit_no");

            EnterpriseInfoMapDic_App.Add("OrgNo", "org_no");
            EnterpriseInfoMapDic_App.Add("No", "reg_no");
            EnterpriseInfoMapDic_App.Add("Status", "status");
            EnterpriseInfoMapDic_App.Add("ShortStatus", "shortStatus");
            EnterpriseInfoMapDic_App.Add("EconKind", "type");
            EnterpriseInfoMapDic_App.Add("StartDate", "date");
            EnterpriseInfoMapDic_App.Add("EndDate", "limitDate");
            EnterpriseInfoMapDic_App.Add("OperName", "oper_name");
            EnterpriseInfoMapDic_App.Add("RegistCapi", "reg_capi_desc");
            //EnterpriseInfoMapDic_App.Add("所属行业", "domain");//所属行业需要解析

            EnterpriseInfoMapDic_App.Add("BelongOrg", "registrar");
            EnterpriseInfoMapDic_App.Add("Address", "address");
            EnterpriseInfoMapDic_App.Add("Scope", "operationDomain");
            EnterpriseInfoMapDic_App.Add("OriginalName", "originalName");
            EnterpriseInfoMapDic_App.Add("ContactNumber", "telephone");
            EnterpriseInfoMapDic_App.Add("Email", "email");
            EnterpriseInfoMapDic_App.Add("WebSite", "webSite");
            EnterpriseInfoMapDic_App.Add("ImageUrl", "imgUrl");
            EnterpriseInfoMapDic_App.Add("EnglishName", "engName");
            #endregion

            Settings.MaxReTryTimes = 100;//尝试最大个数

            ReloadLoginAccount();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            var passResult = geetestHelper.GetLastPoint(hi);
            this.textBox4.Text = passResult;
            //if (this.checkBox1.Checked)
            //{
            //    if (!this.autoChangeAccountCHK.Checked)
            //    {
            //       // MessageBox.Show(string.Format("请注意，目前剩余{0}点，该功能只支持自动获取详细信息，使用该功能会进行扣费10条数据扣点,成功1000次或者失败20次会自动停止改功能", passResult));
            //    }
            //    // this.textBox5.Text = "2000";
            //}
            //else
            //{

            //}

        }

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            var cookies = FullWebBrowserCookie.GetCookieInternal(e.Url, false);
            this.richTextBox.Clear();
            this.richTextBox.AppendText(cookies);
            Settings.SimulateCookies = cookies;
            curUri = e.Url;
            documentText = webBrowser.Document;
            // timerStart();
            //获取cookie
            if (e.Url.AbsoluteUri.Contains("login"))
            {
                //填写表单


                HtmlElement loginname = documentText.All[siteUserNameElm];
                HtmlElement loginPW = documentText.All[sitePwdElm];

                var userNameTxt = this.textBox1.Text;
                var passwordTxt = this.textBox2.Text;
                if (loginname != null)

                    loginname.SetAttribute("value", userNameTxt);
                if (loginPW != null)
                    loginPW.SetAttribute("value", passwordTxt);


            }



            // documentText.MouseUp += new HtmlElementEventHandler(webBrowser_MouseUP);

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("是否确定关闭", "提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                SaveKeyWordHitCount();
                AccountRelease(Settings.LoginAccount);

                PublicSettings.Default.PubilcUserName = this.textBox1.Text.Trim();
                PublicSettings.Default.PubilcPassword = this.textBox2.Text.Trim();
                PublicSettings.Default.PubilcTakeCount = this.textBox3.Text.Trim();
                PublicSettings.Default.PubilcTimerElapse = this.textBox5.Text.Trim();
                PublicSettings.Default.PublicProxyUid = this.ipProxyTxt.Text.Trim();
                PublicSettings.Default.PublicProxyPwd = this.ipProxyTxt2.Text.Trim();
                PublicSettings.Default.Save();
            }
            else
            {
                e.Cancel=true;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //执行父父目录的自动更新程序
            var curDir = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            if (curDir != null && curDir.Parent != null)
            {
                System.IO.FileInfo hitWEBSiteUpdate = curDir.Parent.GetFiles().Where(c => c.Name == "WEBSiteUpdate.exe").FirstOrDefault();
                var parent = curDir.Parent;
                var maxLevel = 4;
                while (hitWEBSiteUpdate == null && parent != null && maxLevel >= 0)
                {
                    hitWEBSiteUpdate = parent.GetFiles().Where(c => c.Name == "WEBSiteUpdate.exe").FirstOrDefault();
                    parent = parent.Parent;
                    maxLevel--;
                }
                if (hitWEBSiteUpdate != null)
                {
                    var thread = new Thread(delegate ()
                    {
                        ExecProcess(hitWEBSiteUpdate.FullName);
                    });
                    thread.Start();
                }
            }
            if (hi != null)
            {
                hi.Dispose();
            }
        }
        /// <summary>
        /// 定时timer时间
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                if (autoChangeAccountCHK.Checked)//自动切换开启中
                {
                    if (DateTime.Now.Hour >= 123)//11点进行关闭
                    {
                        Application.Exit();
                    }
                }
                if (UrlQueue.Instance.Count > 0)
                {
                    var maxRetryTimes = Settings.MaxReTryTimes != 0 ? Settings.MaxReTryTimes : 3;
                    var curUrlObj = UrlQueue.Instance.DeQueue();
                    if (curUrlObj != null&&!string.IsNullOrEmpty(curUrlObj.UrlString))
                    {
                        Settings.CurUrl = curUrlObj.UrlString;
                        var result = new HttpResult();
                        try
                        {
                            if (!string.IsNullOrEmpty(curUrlObj.PostData))
                            {
                                result = GetPostData(curUrlObj);

                            }
                            else
                            {
                                result = GetHttpHtml(curUrlObj);
                            }
                        }
                        catch (Exception ex)//异重试常超时
                        {
                            timerStop();
                            curUrlObj.Depth = curUrlObj.Depth + Settings.MaxReTryTimes / 2;
                            UrlQueue.Instance.EnQueue(curUrlObj);
                            return;
                        }

                        if (curUrlObj.Depth >= maxRetryTimes)//尝试超过三次,用于企业信息json查找
                        {
                            ///添加错误的url列表
                            if (UrlContentLimit(result.Html) && searchType != SearchType.UpdateEnterpriseInfo)
                            {
                                //var errorBosn = new BsonDocument().Add("url", curUrlObj.UrlString).Add("html", result.Html).Add("searchType", searchType.ToString());
                                //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = errorBosn, Name = DataTableNameErrorUrl, Type = StorageType.Insert });
                                //StartDBChangeProcessQuick();
                            }
                            if (result.Html == "{}")
                            {
                                var message = "{ \"status\": 1, \"data\": { } }";
                                result.Html = message;
                                result.StatusCode = HttpStatusCode.OK;
                            }
                            else if (result.Html == "")
                            {
                                var message = "尝试三次后无数据无法处理";
                                result.Html = message;
                                result.StatusCode = HttpStatusCode.OK;
                            }
                            else if (searchType == SearchType.EnterpriseGuidByKeyWord)
                            {
                                var message = "尝试三次后无数据无法处理";
                                result.Html = message;
                                result.StatusCode = HttpStatusCode.OK;
                            }


                        }
                        //清理标识 防止重复添加
                        var curUrl=ClearUrlSignStr(curUrlObj);
                        var args = new DataReceivedEventArgs() { Depth = curUrlObj.Depth, Html = result.Html, IpProx = null, Url = curUrl,urlInfo= curUrlObj };

                        if (result.StatusCode == HttpStatusCode.OK && !result.Html.Contains("Service Unavailable") && !IPLimitProcess(args))
                        {
                            DataReceive(args);
                            StartDBChangeProcessQuick();
                            //AutoChangeAccount();
                            //StartDBChangeProcess();
                            //StartDBChangeProcessQuick();
                        }
                        else
                        {
                            curUrlObj.Depth = curUrlObj.Depth + 1;
                            UrlQueue.Instance.EnQueue(curUrlObj);//重试
                        }


                    }

                }
                else
                {
                  
                   
                    ShowMessageInfo("正在尝试提取关键字");
                    if (StringQueue.Instance.Count > 0&& UrlQueue.Instance.Count<=0)
                    {
                        ///5秒内只能执行一次防止重复太多
                        if (ContinueMethodByBusyGear("InitalQCCAppUrlByKeyWord",2))
                        {
                            timerStop();
                            var curKeyWord = StringQueue.Instance.DeQueue();
                            if (!string.IsNullOrEmpty(curKeyWord))
                            {
                                var url = InitalQCCAppUrlByKeyWord(curKeyWord);
                                if (!urlFilter.Contains(url))
                                {
                                    UrlQueue.Instance.EnQueue(new UrlInfo(url));
                                    ShowMessageInfo("成功提取关键字");
                                    timerStart();
                                }
                            }
                        }
                       
                    }
                    else
                    {
                      
                        waitBrowerMouseUpResponse = false;
                        ShowMessageInfo("当前数据更新结束，请单击crawler重新获取");
                        if (checkBoxGuard.Checked && searchType == SearchType.UpdateEnterpriseInfo)
                        {
                            startCrawlerBtn.Invoke(new Action(() => { startCrawlerBtn_Click(source, e); }));

                        }
                    }
                }
            }
            catch (Exception  ex)
            {
                //UrlQueue.Instance.EnQueue()
                //MessageBox.Show(ex.Message);
                ShowMessageInfo(ex.Message);
                timerStop();
            }
        }
        /// <summary>
        /// 监听自动重现开始事件
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnAutoStartTimedEvent(object source, ElapsedEventArgs e)
        {
            if (waitBrowerMouseUpResponse && documentText != null && documentText.Body != null)
            {
                var captCha = documentText.Body.InnerHtml.Contains("captcha - box");
                if (!captCha)
                {
                    ShowMessageInfo("自动开始执行");
                    timerStart();
                    autoRestartTimer.Stop();
                }

            }
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string text = (sender as ComboBox).SelectedItem as string;
            if (text == null) return;
            if (!text.Contains("占用"))
            {
                AccountRelease(Settings.LoginAccount);//释放原有账号
            }
            else
            {
                if (!autoChangeAccountCHK.Checked)
                {
                    MessageBox.Show("该账号可能被占用请慎用，强制登陆会其他用户会退出登录");
                }

            }

            text = text.Replace("_占用", "").Replace("_频繁", "");
            this.textBox1.Text = text;
            Settings.LoginAccount = text;
            Settings.neeedChangeAccount = false;
            AccountApply(Settings.LoginAccount);
            var hitAccountObj = allAccountList.Where(c => c.Text("name") == text.Trim()).FirstOrDefault();
            if (hitAccountObj != null)
            {
                this.textBox2.Text = hitAccountObj.Text("password");

            }

            ShowAccountInfo();
            PassInValidTimes = 1;
            PassSuccessTimes = 2;
            checkBox1.Checked = false;
            timerStop();
            //ReloadLoginAccount();
        }

        #endregion

        private void webBrowser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            curUri = e.Url;
            if (curUri != null)
            {
                var cookies = FullWebBrowserCookie.GetCookieInternal(curUri, false);
                Settings.SimulateCookies = cookies;
            }


        }

        /// <summary>
        /// 拉动验证码后事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void webBrowser_MouseUP(object sender, EventArgs e)
        {

            if (waitBrowerMouseUpResponse && aTimer.Enabled == false)
            {

                // var captCha   = (mshtml.IHTMLElement)documentText.all.item("captcha - box", 0);
                // mshtml.IHTMLDocument2 doc2 = (mshtml.IHTMLDocument2)webBrowser.Document;
                if (documentText != null && documentText.Body != null && documentText.Body.InnerHtml != null)
                {
                    // curHtml = documentText.body.innerHTML;
                    // if (captCha!=null)
                    {
                        autoRestartTimer.Start();
                    }

                }
            }
            ///重载uri
            if (curUri != null)
            {
                var cookies = FullWebBrowserCookie.GetCookieInternal(curUri, false);
                Settings.SimulateCookies = cookies;
            }
        }

        private void TestIpConnection()
        {

            //var result = GetHttpHtml(new UrlInfo(siteIndexUrl));
            //if (result.StatusCode == HttpStatusCode.OK)
            //{
            if (!string.IsNullOrEmpty(ipProxyTxt.Text) && !string.IsNullOrEmpty(ipProxyTxt2.Text))
            {
                proxyUser = ipProxyTxt.Text.Trim();
                proxyPass = ipProxyTxt2.Text.Trim();
                getWebProxy = GetWebProxy();
                MessageBox.Show("代理更新");
            }

            var webProxyStr = GetWebBrowserProxyString();
            if (UseProxyCHK.Checked)
            {
                var ipSetting = new IEProxy(webProxyStr);
                ipSetting.RefreshIESettings();
                webBrowser.Navigate(addCredsToUri("http://ip.cn"));
            }
            else
            {
                if (string.IsNullOrEmpty(ipProxyTxt.Text) && string.IsNullOrEmpty(ipProxyTxt2.Text))
                {
                    var ipSetting = new IEProxy(webProxyStr);
                    ipSetting.DisableIEProxy();
                }
            }


            //}
            //else
            //{
            //    MessageBox.Show("连接失败");
            //}
        }
        private void startCrawlerBtn_Click(object sender, EventArgs e)
        {
            ///重载uri
            if (curUri != null)
            {
                var cookies = FullWebBrowserCookie.GetCookieInternal(curUri, false);
                Settings.SimulateCookies = cookies;
            }
            Settings.LoginAccount = this.textBox1.Text;
            Settings.neeedChangeAccount = false;
            curTimerElapse = this.textBox5.Text.Trim();
            //ReloadLoginAccount();
            switch (this.comboBox.SelectedIndex)
            {
                //通过城市更新企业信息
                case (int)SearchType.EnterpriseGuidByCity:
                    searchType = SearchType.EnterpriseGuidByCity;
                    if (this.curUri != null && this.curUri.ToString().Contains("gongsi_area_prov"))
                    {
                        validUrl = this.curUri.ToString();
                    }
                    else
                    {
                        validUrl = "http://www.qichacha.com/gongsi_area_prov_AH_p_1.shtml";
                    }
                    break;

                //更新企业信息
                case (int)SearchType.EnterpriseGuidByType:
                    searchType = SearchType.EnterpriseGuidByType;
                    if (this.curUri != null && this.curUri.ToString().Contains("gongsi_industry"))
                    {
                        validUrl = this.curUri.ToString();
                    }
                    else
                    {
                        validUrl = "http://www.qichacha.com/gongsi_industry?industryCode=A&subIndustryCode=1&industryorder=0";
                    }
                    break;
                case (int)SearchType.EnterpriseGuid:
                    searchType = SearchType.EnterpriseGuid;
                    validUrl = "http://www.qichacha.com/search?key=%E5%B7%A7%E5%90%88&type=enterprise&method=all";
                    break;
                case (int)SearchType.EnterpriseGuidByKeyWord:
                    searchType = SearchType.EnterpriseGuidByKeyWord;
                    validUrl = "http://www.qichacha.com/search?key=%E5%B7%A7%E5%90%88&type=enterprise&method=all&1=2";
                    break;
                case (int)SearchType.EnterpriseGuidByKeyWordEnhence:
                    searchType = SearchType.EnterpriseGuidByKeyWordEnhence;
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    break;
                case (int)SearchType.EnterpriseGuidByKeyWord_APP:
                    searchType = SearchType.EnterpriseGuidByKeyWord_APP;
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    //Settings.MaxAccountCrawlerCount = 0;
                    break;
                case (int)SearchType.UpdateEnterpriseInfo:
                default:
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    searchType = SearchType.UpdateEnterpriseInfo;
                    break;

            }
            siteIndexUrl = validUrl;
            curUri = new Uri(siteIndexUrl);
            if (UrlQueue.Instance.Count <= 0)
            {
                this.webBrowser.Navigate(addCredsToUri(validUrl));

                InitialEnterpriseData();
            }

            if (aTimer.Enabled == false)
            {
                timerStart();
                // this.button1.Name = "stopCrawler";
            }
            else
            {
                timerStop();
                //this.button1. = "StartCrawler";
            }
        }

        private void setBusyBtn_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Settings.LoginAccount))
            {
                var updateBosn = new BsonDocument();
                var curLoginAccountObj = allAccountList.Where(c => c.Text("name") == Settings.LoginAccount).FirstOrDefault();
                if (curLoginAccountObj != null)
                {
                    if (curLoginAccountObj.Int("isBusy") == 1)
                    {
                        updateBosn.Set("isBusy", "0");
                        MessageBox.Show("成功设为正常账号");
                    }
                    else
                    {
                        updateBosn.Set("isBusy", "1").Add("status", "1");
                        MessageBox.Show("成功设为频繁账号");
                    }
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBosn, Query = Query.EQ("name", Settings.LoginAccount), Name = DataTableAccount, Type = StorageType.Update });
                    StartDBChangeProcessQuick();
                    //ReloadLoginAccount();

                }

            }
        }

        private void delBtn_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Settings.LoginAccount))
            {
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isInvalid", "1"), Query = Query.EQ("name", Settings.LoginAccount), Name = DataTableAccount, Type = StorageType.Update });
                StartDBChangeProcessQuick();
                ReloadLoginAccount();
                MessageBox.Show("成功设为无效账号");
            }
        }

        private void searchBtn_Click(object sender, EventArgs e)
        {
            var canJump = true;
            if (this.textBox.Text.Contains("user_login") && documentText != null && documentText.Body != null && documentText.Body.InnerHtml.Contains("退出"))
            {
                if (this.checkBoxGuard.Checked == true)
                {
                    this.checkBoxGuard.Checked = false;//关掉守护进程
                    guardTimer.Stop();
                }
                canJump = AutoLogout();
            }
            if (canJump)
            {
                this.webBrowser.Navigate(addCredsToUri(this.textBox.Text));
                waitBrowerMouseUpResponse = false;
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (!GeetestChartAutoLoin())
            {
                ShowMessageInfo("2次登陆时间果断或者密码错误模拟登陆失败，请尝试切换账号");
            }

        }

        private void guardTimer_Tick(object sender, EventArgs e)
        {
            if (aTimer.Enabled == false)
            {

                timerStart();
                webBrowser.BeginInvoke(new Action(() =>
                {
                    ///重载uri
                    if (curUri != null)
                    {
                        var cookies = FullWebBrowserCookie.GetCookieInternal(curUri, false);
                        this.richTextBox.Clear();
                        this.richTextBox.AppendText("重载" + cookies);
                        Settings.SimulateCookies = cookies;
                    }
                }));
                if (UserProxyChk.Checked)
                {
                    ShowMessageInfo("开始切换ip");
                    var result = GetHttpHtml(new UrlInfo("http://proxy.abuyun.com/switch-ip"));
                }
            }
        }

        private void checkBoxGuard_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBoxGuard.Checked)
            {
                guardTimer.Enabled = true;
                guardTimer.Start();
            }
            else
            {

                guardTimer.Enabled = false;
                guardTimer.Stop();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
            //JObject jsonObj = JObject.Parse(this.EnterpriseKeySuffixTxt.Text);

            //return;
            var allBusyAccountList = dataop.FindAllByQuery(DataTableAccount, Query.And(Query.EQ("isBusy", "1"), Query.NE("isInvalid", "1"))).OrderBy(c => c.Int("isUsed") + c.Int("isBusy")).ThenBy(c => c.Int("UpdateEnterpriseInfo")).ThenBy(c => c.Date("updateDate")).ToList();
            var curTestUrl = "http://www.qichacha.com/company_getinfos?unique=2fc830656f9b1e6078a8e52983c128cc&companyname=%E9%9D%92%E5%B2%9B%E5%A4%A9%E8%AF%9A%E8%81%94%E5%90%88%E5%88%9B%E6%84%8F%E6%96%87%E5%8C%96%E9%9B%86%E5%9B%A2%E6%9C%89%E9%99%90%E5%85%AC%E5%8F%B8&tab=base";
            this.curUri = new Uri(curTestUrl);
            searchType = SearchType.UpdateEnterpriseInfo;
            var index = 1;
            foreach (var account in allBusyAccountList)
            {
                index++;
                this.textBox1.Text = account.Text("name");
                this.textBox2.Text = account.Text("password");
                //模拟登陆
                if (GeetestChartAutoLoin())
                {

                    var result = GetHttpHtml(new UrlInfo(curTestUrl));
                    if (result.StatusCode == HttpStatusCode.OK && !result.Html.Contains("Service Unavailable"))
                    {
                        if (!UrlContentLimit(result.Html))
                        {
                            //无限制
                            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isBusy", "0"), Query = Query.EQ("name", account.Text("name")), Name = DataTableAccount, Type = StorageType.Update });
                            StartDBChangeProcessQuick();
                        }
                        else
                        {
                            //continue;
                            //过验证码
                            if (PassEnterpriseInfoGeetestChart(true))
                            {
                                //无限制
                                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isBusy", "0"), Query = Query.EQ("name", account.Text("name")), Name = DataTableAccount, Type = StorageType.Update });
                                StartDBChangeProcessQuick();
                            }
                            else
                            {

                                //无限制
                                // DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isInvalid", "1"), Query = Query.EQ("name", account.Text("name")), Name = DataTableAccount, Type = StorageType.Update });
                                // StartDBChangeProcessQuick();
                            }

                        }
                    }
                }

            }


        }

        private void button3_Click(object sender, EventArgs e)
        {
            TestIpConnection();
        }

        private void ipProxyTxt_TextChanged(object sender, EventArgs e)
        {
            if (ipProxyTxt.Text.Contains("\t") || ipProxyTxt.Text.Contains(" "))
            {
                ipProxyTxt.Text = ipProxyTxt.Text.Replace("\t", ":").Replace(" ", ":");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            PassEnterpriseInfoGeetestChart(true);
        }

        private void webBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            //UriBuilder uriSite = new UriBuilder(e.Url);
            /////ip地址代理
            //if (string.IsNullOrEmpty(uriSite.UserName))
            //{
            //    var curUri = addCredsToUri(e.Url);
            //    webBrowser.Url = curUri;
            //    webBrowser.Navigate(curUri);
            //    //HttpWebRequest myRequest = (HttpWebRequest)HttpWebRequest.Create(currentUri);
            //    //myRequest.Proxy = GetWebProxy();
            //    //HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();
            //    //webBrowser.DocumentStream = myResponse.GetResponseStream();
            //    e.Cancel = true;
            //}
            if (e.Url.AbsoluteUri == "http://www.qichacha.com/" && SearchType.EnterpriseGuidByKeyWordEnhence != searchType)
            {
                webBrowser.Navigate(siteIndexUrl);
            }

        }
        /// <summary>
        /// 是否打勾CHK 判断浏览器是否使用代理
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public Uri addCredsToUri(Uri u)
        {
            if (UseProxyCHK.Checked == true && USEWEBPROXY && !string.IsNullOrEmpty(proxyUser) && !string.IsNullOrEmpty(proxyPass))
            {

                UriBuilder uriSite = new UriBuilder(u);
                uriSite.UserName = proxyUser;
                uriSite.Password = proxyPass;
                return uriSite.Uri;
            }
            else
            {
                return u;
            }

        }
        /// <summary>
        /// 是否打勾CHK 判断浏览器是否使用代理
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public string addCredsToUri(string u)
        {
            if (UseProxyCHK.Checked == true && USEWEBPROXY && !string.IsNullOrEmpty(proxyUser) && !string.IsNullOrEmpty(proxyPass))
            {
                UriBuilder uriSite = new UriBuilder(u);
                uriSite.UserName = proxyUser;
                uriSite.Password = proxyPass;
                return uriSite.Uri.ToString();
            }
            else
            {
                return u;
            }
        }

        private void UserProxyChk_CheckedChanged(object sender, EventArgs e)
        {

            USEWEBPROXY = UserProxyChk.Checked;

        }

        #region 模拟自动注册
        #region 变量初始化
        GeetestResult phoneRegResult = new GeetestResult();//电话模拟发送验证码返回结果
        string lastPhone = string.Empty;//上次电话号码
        string lastPhoneCode = string.Empty;//上次电话验证码
        Dictionary<string, string> AccountDic = new Dictionary<string, string>();
        int maxPhoneCodeTryTimes = 7;//尝试9次失败后重新读取电话
        int curPhoneCodeTryTimes = 0;//当前尝试次数
        int maxPhoneTryTimes = 10;//连续10次没取到号码
        int curPhoneTryTimes = 0;//当前尝试次数
        #endregion

        /// <summary>
        /// 定时模拟获取用户号码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AccountPhoneCodeSenerTimer_Tick(object sender, EventArgs e)
        {
            AccountPhoneCodeSenerTimerStatusChange(false);//关闭电话号码获取
            //定时检测电话号码变换，并进行模拟发送验证码
            var phone = GetPhone();
            if (!phone.Contains("正在") && !string.IsNullOrEmpty(phone) && phone != lastPhoneCode && !AccountDic.ContainsKey(phone))//成功获取到电话号码
            {
                curPhoneTryTimes = 0;
                ShowMessageInfo("获得电话" + phone, true);
                lastPhone = phone;
                phoneRegResult = SendPhoneCode(phone);//成功
                if (phoneRegResult.Status)
                {
                    ShowMessageInfo(string.Format("给手机发送验证码成功，等待接受验证码 {0}|{1}", phoneRegResult.Challenge, phoneRegResult.ValidCode), true);
                }
                AccountRegTimerStatusChange(true);//开始等待验证码发送与读取
            }
            else
            {
                curPhoneTryTimes++;
                if (curPhoneTryTimes >= maxPhoneTryTimes)
                {
                    ReleaseAllButtonClick();
                    curPhoneTryTimes = 0;
                    //AccountPhoneCodeSenerTimerStatusChange(false);
                    //AccountRegTimerStatusChange(false);
                    ShowMessageInfo("无法获得号码，可能账号无重置");

                }
                else
                {
                    AccountPhoneCodeSenerTimerStatusChange(true);//重新启动电话号码获取
                }
            }
        }
        public void AccountRegToDB(string name, string passWord)
        {
            var curAccountBson = new BsonDocument();
            switch (accountRegisterType) {
                case AccountRegisterType.QiChaCha:
                 
                    curAccountBson.Add("name", lastPhone);
                    curAccountBson.Add("password", passWord);
                    curAccountBson.Add("autReg", "1");
                    curAccountBson.Add("handReg", "1");
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curAccountBson, Name = DataTableAccount, Type = StorageType.Insert });
                    break;
                case AccountRegisterType.LandFang:
                  
                    curAccountBson.Add("userName", lastPhone);
                    curAccountBson.Add("passWord", passWord);
                    curAccountBson.Add("autReg", "1");
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curAccountBson, Name = LandFangDataTableAccount, Type = StorageType.Insert });

                    break;




            }
            
            
            StartDBChangeProcessQuick();

        }
        /// <summary>
        ///  定时模拟获取验证码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AccountRegTimer_Tick_1(object sender, EventArgs e)
        {
            var phoneCode = GetPhoneCode();//获取验证码

            var passWord = "qwer1234";
            //if (!string.IsNullOrEmpty(textBox2.Text))
            //{
            //    passWord = textBox2.Text.Trim();
            //}
            //定时监测验证码是否改变，并进行模拟注册账号
            if (!string.IsNullOrEmpty(phoneCode) && phoneCode != lastPhoneCode)
            {
                lastPhoneCode = phoneCode;
                var status = AccountReg(phoneRegResult, lastPhone, passWord, phoneCode);
                if (status)
                {
                    ShowMessageInfo("获得电话验证码" + phoneCode, true);
                    if (!AccountDic.ContainsKey(lastPhone))
                    {
                        // lastPhoneCode = phoneCode;
                        AccountDic.Add(lastPhone, passWord);
                        var curAccountBson = new BsonDocument();
                        curAccountBson.Add("name", lastPhone);
                        curAccountBson.Add("password", passWord);
                        curAccountBson.Add("autReg", "1");
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curAccountBson, Name = DataTableAccount, Type = StorageType.Insert });
                        StartDBChangeProcessQuick();
                        // MessageBox.Show(status.ToString());
                        ShowMessageInfo(string.Format("注册成功{0}:{1}", lastPhone, passWord));
                        //AutoLogout();
                        curPhoneCodeTryTimes = 0;
                        AccountRegTimerStatusChange(false);//停止获取验证码
                        PhoneBtnClick();//重新获取号码
                        AccountPhoneCodeSenerTimerStatusChange(true);//重新获得手机号发送验证码

                    }
                }
                else //注册不成功
                {
                    addIgnoreButtonClick();//拉入黑名单
                    ShowMessageInfo(string.Format("注册失败{0}:{1}，即将重新获取号码", lastPhone, passWord));
                    curPhoneCodeTryTimes = 0;
                    AccountRegTimerStatusChange(false);//停止获取验证码
                    PhoneBtnClick();//重新获取号码
                    AccountPhoneCodeSenerTimerStatusChange(true);//重新获得手机号发送验证码
                }

            }
            else
            {
                curPhoneCodeTryTimes += 1;//重试获取phoneCode次数

                if (curPhoneCodeTryTimes >= maxPhoneCodeTryTimes)//超过重试次数
                {
                    //regForm.Close();
                    //regForm = null;
                    addIgnoreButtonClick();//拉入黑名单
                    curPhoneCodeTryTimes = 0;
                    AccountRegTimerStatusChange(false);//停止获取验证码
                    PhoneBtnClick();//重新获取号码
                    AccountPhoneCodeSenerTimerStatusChange(true);//重新获得手机号发送验证码

                }
            }
        }

        #region 电话网页验证操作
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
                ShowMessageInfo("当前页面未展示");
            }
        }
        public void PhoneBtnClick()
        {
            HtmlClick("GetPhoneButton");
        }
        public UserRegisterForm regForm;
        /// <summary>
        /// 释放号码
        /// </summary>
        public void ReleaseAllButtonClick()
        {

            HtmlClick("ReleaseAllButton");
        }
        /// <summary>
        /// 拉入黑名单
        /// </summary>
        public void addIgnoreButtonClick()
        {

            HtmlClick("addIgnoreButton");
            ShowMessageInfo(lastPhone + "被拉入黑名单", true);
        }

        /// <summary>
        /// 获取电话号码,通过发送代码，进行验证码处理
        /// </summary>
        /// <returns></returns>
        public string GetPhone()
        {
            // return "15959266823";
            //var curUrlInfo = new UrlInfo("http://iii.51ym.me:8088/UserInterface.aspx?action=getmobile&itemid=3970&token=9a8562729e72fe4eb861cddbc57e08dd&jsoncallback=jQuery111105649001472009196_1472625308752&_=1472625308754 ");
            //var result = GetHttpHtml(curUrlInfo);
            //if (result.StatusCode == HttpStatusCode.OK && result.Html.Contains("success"))
            //{
            //    var beginIndex = result.Html.IndexOf("|");
            //    var endIndex= result.Html.LastIndexOf("\"");
            //    var curPhone = result.Html.Substring(beginIndex+1, endIndex - beginIndex-1);
            //    return result.Html.Replace("content=success|", "");
            //}
            documentText = webBrowser.Document;
            if (documentText != null)
            {

                var loginname = documentText.GetElementById("PhoneBox");
                if (loginname != null)
                    return loginname.GetAttribute("value");
            }
            return string.Empty;
        }
        /// <summary>
        /// 获取电话验证码
        /// </summary>
        /// <returns></returns>
        public string GetPhoneCode()
        {
            //return "123456";
            //if (!string.IsNullOrEmpty(lastPhone)) { 
            //    var curUrlInfo = new UrlInfo("http://iii.51ym.me:8088/UserInterface.aspx?action=getsms&itemid=3970&mobile="+lastPhone+ "&release=1&token=9a8562729e72fe4eb861cddbc57e08dd&jsoncallback=jQuery11110017071419883310623_1472608357688&_=1472608357712");
            //    var result = GetHttpHtml(curUrlInfo);
            //    if (result.StatusCode == HttpStatusCode.OK && result.Html.Contains("success"))
            //    {
            //        return result.Html.Replace("content=success|", "");
            //    }
            //}
            var value = string.Empty;
            documentText = webBrowser.Document;
            if (documentText != null)
            {

                var loginname = documentText.GetElementById("CodeBox");
                if (loginname != null)
                    value = loginname.GetAttribute("value");

                if (string.IsNullOrEmpty(value))
                {
                    var RemarksBox = documentText.GetElementById("RemarksBox");
                    if (RemarksBox != null)
                    {
                        var result = RemarksBox.GetAttribute("value");
                        // && result.Contains("企查查网站")添加landFang注册去掉
                        if (!string.IsNullOrEmpty(result))
                        {
                            var str = "您的验证码是";
                            var startIndex = result.IndexOf(str);
                            var endIndex = result.IndexOf("。");
                            if (startIndex != -1 && endIndex != -1)
                            {
                                value = result.Substring(startIndex + str.Length, endIndex - startIndex - str.Length);
                            }

                        }
                    }
                }
                return value;

            }
            return string.Empty;
        }
        #endregion
        #region timer控制器
        /// <summary>
        /// 发送验证码timer
        /// </summary>
        /// <param name="hasStart"></param>
        private void AccountPhoneCodeSenerTimerStatusChange(bool hasStart)
        {
            if (hasStart && !this.AccountPhoneCodeSenderTimer.Enabled)
            {
                this.AccountPhoneCodeSenderTimer.Enabled = true;
                this.AccountPhoneCodeSenderTimer.Start();
                return;
            }
            if (!hasStart && this.AccountPhoneCodeSenderTimer.Enabled)
            {
                this.AccountPhoneCodeSenderTimer.Enabled = false;
                this.AccountPhoneCodeSenderTimer.Stop();
                return;
            }
        }
        /// <summary>
        /// 模拟注册tiemr
        /// </summary>
        /// <param name="hasStart"></param>
        private void AccountRegTimerStatusChange(bool hasStart)
        {
            if (hasStart && !this.AccountRegTimer.Enabled)
            {
                this.AccountRegTimer.Enabled = true;
                this.AccountRegTimer.Start();
                return;
            }
            if (!hasStart && this.AccountRegTimer.Enabled)
            {
                this.AccountRegTimer.Enabled = false;
                this.AccountRegTimer.Stop();
                return;
            }
        }

        #endregion
        #region 模拟发送验证码
        public bool regFormIsClose = true;
        /// <summary>
        /// 过企业信息chart验证码
        /// </summary>
        /// <returns></returns>
        public GeetestResult SendPhoneCode(string phoneNum)
        {
            ///通过调用窗体手动注册



            //if (regForm == null || regFormIsClose == true)
            //{
            //    regForm = new UserRegisterForm(this);

            //}

            //if (regForm != null) { 
            //    regForm.PhoneNum = phoneNum;
            //    regForm.PassWord = "qwer1234";
            //    regForm.Show();
            //}
            //else
            //{
            //    regFormIsClose = true;
            //}

            //return new GeetestResult() { Status = true };
            switch(accountRegisterType)
            {
                case AccountRegisterType.QiChaCha: return AppSendPhoneCode(phoneNum);
                case AccountRegisterType.LandFang: return LandFangSendPhoneCode(phoneNum);
            }
        //一个时刻只能一个实例运行
        var geetestHelper = new PassGeetestHelper();
            var validUrl = "";
            var postFormat = "geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}%7Cjordan";
            geetestHelper.GetCapUrl = "http://www.qichacha.com/index_getcap?rand=t={0}&_={0}";
            var passResult = geetestHelper.PassGeetest(hi, postFormat, validUrl, "");
            if (passResult.Status)
            {
                hi.Url = "http://www.qichacha.com/user_regmobileCode";
                hi.Refer = "http://www.qichacha.com/user_login";
                hi.PostData = string.Format("phone={0}&type={1}&geetest_challenge={2}&geetest_validate={3}&geetest_seccode={3}%7Cjordan", phoneNum, 3, passResult.Challenge, passResult.ValidCode);
                var ho = LibCurlNet.HttpManager.Instance.ProcessRequest(hi);
                if (ho.IsOK)
                {
                    return passResult;
                }

            }
            return new GeetestResult();
        }


        /// <summary>
        /// 自动登陆
        /// </summary>
        /// <returns></returns>
        private bool AccountReg(GeetestResult passResult, string phone, string pswd, string mobilecode)
        {
            switch (accountRegisterType)
            {
                case AccountRegisterType.QiChaCha: return AppAccountReg(passResult, phone, pswd, mobilecode);
                case AccountRegisterType.LandFang: return LandFangAccountReg(passResult, phone, pswd, mobilecode);
            }
           
            var geetestHelper = new PassGeetestHelper();

            if (passResult.Status)
            {

                hi.Url = "http://www.qichacha.com/user_registAction";
                hi.Refer = "http://www.qichacha.com/user_login";
                hi.PostData = string.Format("phone={0}&pswd={1}&geetest_challenge={2}&geetest_validate={3}&geetest_seccode={3}%7Cjordan&mobilecode={4}", phone, pswd, passResult.Challenge, passResult.ValidCode, mobilecode);
                var ho = LibCurlNet.HttpManager.Instance.ProcessRequest(hi);
                if (ho.IsOK)
                {
                    if (ho.TxtData.Contains("true"))
                    {

                        return true;
                    }
                }


            }
            return false;
        }
        /// <summary>
        /// app手机发送验证码
        /// </summary>
        /// <param name="phoneNum"></param>
        /// <returns></returns>
        public GeetestResult AppSendPhoneCode(string phoneNum)
        {
            if (string.IsNullOrEmpty(Settings.AccessToken))
            {
                RefreshToken();
            }

            //一个时刻只能一个实例运行

            var passResult = new GeetestResult() { Status = false };
            if (true)
            {
                var _url = new UrlInfo("https://app.qichacha.net/app/v1/admin/sendValidateToken");
                _url.PostData = string.Format("account={0}&timestamp={1}&sign={2}", phoneNum, Settings.timestamp, Settings.sign);
                var result = GetPostDataAPP(_url);
                if (result.StatusCode == HttpStatusCode.OK && result.Html.Contains("请查收"))
                {
                    passResult = new GeetestResult() { Status = true };
                    return passResult;
                }


            }
            return new GeetestResult();
        }
        /// <summary>
        /// landFang手机发送验证码
        /// </summary>
        /// <param name="phoneNum"></param>
        /// <returns></returns>
        public GeetestResult LandFangSendPhoneCode(string phoneNum)
        {
            
            //一个时刻只能一个实例运行

            var passResult = new GeetestResult() { Status = false };
            if (true)
            {
                var encodePhone = HttpUtility.UrlEncode(DESCryptDecodeHelper.Decode(phoneNum, "soufunss"));
                var _url = new UrlInfo(string.Format("https://appapi.3g.fang.com/LandApp/SendSMS?isencrypt=20150303&messagename=CheckMobile&mode=reg&imei=133524413725754&mobile={0}&wirelesscode=92eaf0edb80cd27ec5c0a81defef85c6&r=jh7XxIIb7YE%3D", encodePhone));
                hi.Url = _url.UrlString;
                hi.UserAgent = "android_tudi%7EGT-P5210%7E4.2.2";
                hi.HeaderSet("Accept-Encoding", "gzip");
                // hi.HeaderSet("Content-Length","154");
                // hi.HeaderSet("Connection","Keep-Alive");
                hi.HeaderSet("imei", "133524413725754");
                hi.HeaderSet("Host", "appapi.3g.fang.com");
                hi.HeaderSet("version", "2.5.0");
                hi.HeaderSet("user-agent", "android_tudi%7EGT-P5210%7E4.2.2");
                //hi.HeaderSet("user-agent", "android_tudi%7EGT-P5210%7E4.2.2");
                hi.HeaderSet("ispos", "1");
                hi.HeaderSet("app_name", "android_tudi");
                hi.HeaderSet("iscard", "1");
                hi.HeaderSet("connmode", "Wifi");
                hi.HeaderSet("model", "GT-P5210");
                hi.HeaderSet("posmode", "gps%2Cwifi");
                hi.HeaderSet("company", "-10000");
                var ho = LibCurlNet.HttpManager.Instance.ProcessRequest(hi);
                if (ho.IsOK && ho.TxtData.Contains("成功"))
                {
                    passResult = new GeetestResult() { Status = true };
                    return passResult;
                }


            }
            return new GeetestResult();
        }


        /// <summary>
        /// app账号注册,因为账号加密所以密码固定
        /// </summary>
        /// <param name="passResult"></param>
        /// <param name="phone"></param>
        /// <param name="pswd">qwer1234</param>
        /// <param name="mobilecode"></param>
        /// <returns></returns>
        private bool AppAccountReg(GeetestResult passResult, string phone, string pswd, string mobilecode)
        {
            if (string.IsNullOrEmpty(Settings.AccessToken))
            {
                RefreshToken();
            }
            var geetestHelper = new PassGeetestHelper();

            if (passResult.Status)
            {

                var _url = new UrlInfo("https://app.qichacha.net/app/v1/admin/register");
                _url.PostData = string.Format("account={0}&password=b412a4532991798fcddf698e31125c03&identifyCode={1}&timestamp={2}&sign={3}", phone, mobilecode, Settings.timestamp, Settings.sign);
                var result = GetPostDataAPP(_url);
                if (result.StatusCode == HttpStatusCode.OK && result.Html.Contains("成功"))
                {
                    return true;
                }
                else
                {
                    RefreshToken();
                    result = GetPostDataAPP(_url);
                    if (result.StatusCode == HttpStatusCode.OK && result.Html.Contains("成功"))
                    {
                        return true;
                    }
                    ShowMessageInfo(result.Html, true);
                }


            }

            return false;
        }

        private bool LandFangAccountReg(GeetestResult passResult, string phone, string pswd, string mobilecode)
        {
                var encodePhone = HttpUtility.UrlEncode(DESCryptDecodeHelper.Decode(phone, "soufunss"));
                var _url = new UrlInfo(string.Format("https://appapi.3g.fang.com/LandApp/checkcode?isencrypt=20150303&code={0}&mobile={1}&mode=reg&imei=133524413725754&wirelesscode=0a249689678fd9b0ca090788eb7a7495&r=HxK0lKsP6Vg%3D", mobilecode, encodePhone));
                hi.Url = _url.UrlString;
                hi.UserAgent = "android_tudi%7EGT-P5210%7E4.2.2";
                hi.HeaderSet("Accept-Encoding", "gzip");
                // hi.HeaderSet("Content-Length","154");
                // hi.HeaderSet("Connection","Keep-Alive");
                hi.HeaderSet("imei", "133524413725754");
                hi.HeaderSet("Host", "appapi.3g.fang.com");
                hi.HeaderSet("version", "2.5.0");
                hi.HeaderSet("user-agent", "android_tudi%7EGT-P5210%7E4.2.2");
                //hi.HeaderSet("user-agent", "android_tudi%7EGT-P5210%7E4.2.2");
                hi.HeaderSet("ispos", "1");
                hi.HeaderSet("app_name", "android_tudi");
                hi.HeaderSet("iscard", "1");
                hi.HeaderSet("connmode", "Wifi");
                hi.HeaderSet("model", "GT-P5210");
                hi.HeaderSet("posmode", "gps%2Cwifi");
                hi.HeaderSet("company", "-10000");
                var ho = LibCurlNet.HttpManager.Instance.ProcessRequest(hi);
                if (ho.IsOK && ho.TxtData.Contains("成功"))
                {
                    var userName= Toolslib.Str.Sub(ho.TxtData, "username\":\"", "\"");//用户名
                    var issuccess = Toolslib.Str.Sub(ho.TxtData, "issuccess\":\"", "\"");//成功代码
                return true;
                }
                return false;
        }
        


        /// <summary>
        /// 保存刷新后token
        /// </summary>
        public void SaveDeviceToken()
        {
            var updateBson = new BsonDocument();
            updateBson.Add("refreshToken", Settings.RefleshToken);
            updateBson.Add("accessToken", Settings.AccessToken);
            DBChangeQueue.Instance.EnQueue(new StorageData() { Name = QCCDeviceAccount, Document = updateBson, Type = StorageType.Update, Query = Query.EQ("deviceId", Settings.DeviceId) });
            StartDBChangeProcessQuick();
        }

        /// <summary>
        /// 刷新tokenrefresh_token=6b1b70165f328de31036d7b4e5731e96
        /// </summary>
        /// <returns></returns>
        public string RefreshToken(bool forceRefresh = false)
        {
            if (!forceRefresh)
            {
                var timeSpan = DateTime.Now - Settings.LastAvaiableTokenTime;
                if (timeSpan.TotalSeconds < 20)//没限制 5秒没取到数据
                {
                    return string.Empty;
                }
            }
            lock (lockRefressToken)
            {
                Settings.LastAvaiableTokenTime = DateTime.Now;
                if (string.IsNullOrEmpty(Settings.RefleshToken))
                {

                    return GetAccessToken();
                }
                ShowMessageInfo("开始进行RefreshToken", true);

                hi.Url = "https://app.qichacha.net/app/v1/admin/refreshToken";
                //hi.Refer = "http://app.qichacha.net";
                hi.PostData = string.Format("refreshToken={0}&timestamp={1}&appId={2}&sign={3}", Settings.RefleshToken, Settings.timestamp, Settings.AppId, Settings.sign); ;
                hi.UserAgent = "okhttp/3.2.0";
                hi.HeaderSet("Content-Type", "application/x-www-form-urlencoded");
                // hi.HeaderSet("Content-Length","154");
                // hi.HeaderSet("Connection","Keep-Alive");
                hi.HeaderSet("Accept-Encoding", "gzip");
                hi.HeaderSet("Authorization", Settings.AccessToken);
                // hi.HeaderSet("Authorization", string.Format("Bearer {0}", Settings.AccessToken));
                var ho = LibCurlNet.HttpManager.Instance.ProcessRequest(hi);

                if (ho.IsOK && ho.TxtData.Contains("成功"))
                {
                    var token = Toolslib.Str.Sub(ho.TxtData, "access_token\":\"", "\"");
                    if (!string.IsNullOrEmpty(token))
                        Settings.AccessToken = token;
                    ShowMessageInfo("操作成功开始保存RefreshToken" + Settings.AccessToken, true);
                    SaveDeviceToken();
                    return ho.TxtData;
                }
                else
                {
                    return GetAccessToken();

                }

            }
            //return string.Empty;
        }

        /// <summary>
        /// 刷新tokenrefresh_token=6b1b70165f328de31036d7b4e5731e96
        /// </summary>
        /// <returns></returns>
        public string GetAccessToken()
        {

            hi.Url = "https://app.qichacha.net/app/v1/admin/getAccessToken";
            //hi.Refer = "http://app.qichacha.net";
            hi.PostData = string.Format("appId={0}&deviceId={1}&version=9.2.0&deviceType=android&os=&timestamp={2}&sign={3}", Settings.AppId, Settings.DeviceId, Settings.timestamp, Settings.sign);
            hi.UserAgent = "okhttp/3.2.0";
            hi.HeaderSet("Content-Type", "application/x-www-form-urlencoded");
            // hi.HeaderSet("Content-Length","154");
            // hi.HeaderSet("Connection","Keep-Alive");
            hi.HeaderSet("Accept-Encoding", "gzip");
            hi.HeaderSet("Authorization", string.Format("Bearer {0}", Settings.AccessToken));
            var ho = LibCurlNet.HttpManager.Instance.ProcessRequest(hi);
            if (ho.IsOK && ho.TxtData.Contains("成功"))
            {
                var token = Toolslib.Str.Sub(ho.TxtData, "access_token\":\"", "\"");
                if (!string.IsNullOrEmpty(token))
                    Settings.AccessToken = token;

                var refleshToken = Toolslib.Str.Sub(ho.TxtData, "refresh_token\":\"", "\"");
                if (!string.IsNullOrEmpty(refleshToken))
                    Settings.RefleshToken = refleshToken;
                SaveDeviceToken();
                return ho.TxtData;
            }
            ShowMessageInfo("AccessToken获取失败:" + ho.TxtData, true);
            if (GetAccessRetryTimes <= 0)
            {
                this.checkBoxGuard.Checked = false;
                guardTimer.Stop();
                GetAccessRetryTimes = 4;
            }
            else
            {
                GetAccessRetryTimes--;
            }
       
            timerStop();

            return string.Empty;
        }
        /// <summary>
        /// 修复为使用代理
        /// </summary>
        /// <returns></returns>
        public string GetAccessToken_abort()
        {
            var url = "https://app.qichacha.net/app/v1/admin/getAccessToken";
            var postData = string.Format("appId={0}&deviceId={1}&version=9.2.0&deviceType=android&os=&timestamp={2}&sign={3}", Settings.AppId, Settings.DeviceId, Settings.timestamp, Settings.sign); ;
            //var result = GetPostData(new UrlInfo(url) { PostData = postData });
            SimpleCrawler.HttpItem item = new SimpleCrawler.HttpItem()
            {
                URL = url,//URL     必需项    

                ContentType = "application/x-www-form-urlencoded",//返回类型    可选项有默认值 

                Timeout = 1500,
                Accept = "*/*",
                // Encoding = null,//编码格式（utf-8,gb2312,gbk）     可选项 默认类会自动识别
                //Encoding = Encoding.Default,
                Method = "Post",//URL     可选项 默认为Get
                                //Timeout = 100000,//连接超时时间     可选项默认为100000
                                //ReadWriteTimeout = 30000,//写入Post数据超时时间     可选项默认为30000
                                //IsToLower = false,//得到的HTML代码是否转成小写     可选项默认转小写
                                //Cookie = "",//字符串Cookie     可选项
                UserAgent = "okhttp/3.2.0",//用户的浏览器类型，版本，操作系统     可选项有默认值
                                           //Referer = "app.qichacha.net",//来源URL     可选项
                Postdata = postData,
                // Allowautoredirect = true,
                // Cookie = Settings.SimulateCookies
            };


            item.Header.Add("Accept-Encoding", "gzip");
            // item.Header.Add("Host", "app.qichacha.net");
            item.Header.Add("Authorization", Settings.AccessToken);
            //item.Header.Add("Accept-Language", "zh-CN");
            item.Header.Add("charset", "UTF-8");
            //item.Header.Add("X-Requested-With", "XMLHttpRequest");
            //请求的返回值对象
            item.WebProxy = GetWebProxy();
            var result = http.GetHtml(item);
            if (result.Html.Contains("成功"))
            {
                var token = Toolslib.Str.Sub(result.Html, "access_token\":\"", "\"");
                if (!string.IsNullOrEmpty(token))
                    Settings.AccessToken = token;

                var refleshToken = Toolslib.Str.Sub(result.Html, "refresh_token\":\"", "\"");
                if (!string.IsNullOrEmpty(refleshToken))
                    Settings.RefleshToken = refleshToken;
                SaveDeviceToken();
                return result.Html;
            }

         
            ShowMessageInfo("AccessToken获取失败:" + result.Html, true);
            this.checkBoxGuard.Checked = false;
            guardTimer.Stop();
            timerStop();

            return string.Empty;
        }

        /// <summary>
        /// 修复为使用代理
        /// </summary>
        /// <param name="forceRefresh"></param>
        /// <returns></returns>
        public string RefreshToken_abort(bool forceRefresh = false)
        {
            if (!forceRefresh)
            {
                var timeSpan = DateTime.Now - Settings.LastAvaiableTokenTime;
                if (timeSpan.TotalSeconds < 20)//没限制 5秒没取到数据
                {
                    return string.Empty;
                }
            }
            lock (lockRefressToken)
            {
                Settings.LastAvaiableTokenTime = DateTime.Now;
                if (string.IsNullOrEmpty(Settings.RefleshToken))
                {

                    return GetAccessToken();
                }
                ShowMessageInfo("开始进行RefreshToken", true);

               var url = "https://app.qichacha.net/app/v1/admin/refreshToken";
                //hi.Refer = "http://app.qichacha.net";
               var postData = string.Format("refreshToken={0}&timestamp={1}&appId={2}&sign={3}", Settings.RefleshToken, Settings.timestamp, Settings.AppId, Settings.sign); ;

                SimpleCrawler.HttpItem item = new SimpleCrawler.HttpItem()
                {
                    URL = url,//URL     必需项    

                    ContentType = "application/x-www-form-urlencoded",//返回类型    可选项有默认值 

                    Timeout = 1500,
                    Accept = "*/*",
                    // Encoding = null,//编码格式（utf-8,gb2312,gbk）     可选项 默认类会自动识别
                    //Encoding = Encoding.Default,
                    Method = "post",//URL     可选项 默认为Get
                                    //Timeout = 100000,//连接超时时间     可选项默认为100000
                                    //ReadWriteTimeout = 30000,//写入Post数据超时时间     可选项默认为30000
                                    //IsToLower = false,//得到的HTML代码是否转成小写     可选项默认转小写
                                    //Cookie = "",//字符串Cookie     可选项
                    UserAgent = "okhttp/3.2.0",//用户的浏览器类型，版本，操作系统     可选项有默认值
                                               //Referer = "app.qichacha.net",//来源URL     可选项
                    Postdata = postData,
                    // Allowautoredirect = true,
                    // Cookie = Settings.SimulateCookies
                };


                item.Header.Add("Accept-Encoding", "gzip");
                // item.Header.Add("Host", "app.qichacha.net");
                item.Header.Add("Authorization", Settings.AccessToken);
                //item.Header.Add("Accept-Language", "zh-CN");
                item.Header.Add("charset", "UTF-8");
                //item.Header.Add("X-Requested-With", "XMLHttpRequest");
                //请求的返回值对象
                item.WebProxy = GetWebProxy();
                var result = http.GetHtml(item);

                if (result.Html.Contains("成功"))
                {
                    var token = Toolslib.Str.Sub(result.Html, "access_token\":\"", "\"");
                    if (!string.IsNullOrEmpty(token))
                        Settings.AccessToken = token;
                    ShowMessageInfo("操作成功开始保存RefreshToken" + Settings.AccessToken, true);
                    SaveDeviceToken();
                    return result.Html;
                }
                else
                {
                    return GetAccessToken();

                }

            }
            //return string.Empty;
        }
        #endregion

        #endregion

        private void button5_Click(object sender, EventArgs e)
        {
            accountRegisterType = AccountRegisterType.QiChaCha;
            if (documentText != null && documentText.Body != null && documentText.Body.InnerHtml.Contains("退出"))
            {
                // AutoLogout();
            }
            if (curUri == null || curUri.ToString().Contains("www.51ym.me"))
            {
                RefreshToken();
                PhoneBtnClick();
                //webBrowser.Navigate("http://www.51ym.me/User/MobileSMSCode.aspx");
                AccountPhoneCodeSenerTimerStatusChange(true);//开始获取电话
            }
            else
            {
                MessageBox.Show("请先登录网站www.51ym.me并登陆");
                webBrowser.Navigate("http://www.51ym.me/User/MobileSMSCode.aspx");
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {

            ChangeIp();
            // MessageBox.Show(result.Html);
        }

        private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (this.comboBox.SelectedIndex)
            {
                //通过城市更新企业信息
                case (int)SearchType.EnterpriseGuidByCity:
                    searchType = SearchType.EnterpriseGuidByCity;
                    if (this.curUri != null && this.curUri.ToString().Contains("gongsi_area_prov"))
                    {
                        validUrl = this.curUri.ToString();
                    }
                    else
                    {
                        validUrl = "http://www.qichacha.com/gongsi_area_prov_AH_p_1.shtml";
                    }
                    break;

                //更新企业信息
                case (int)SearchType.EnterpriseGuidByType:
                    searchType = SearchType.EnterpriseGuidByType;
                    if (this.curUri != null && this.curUri.ToString().Contains("gongsi_industry"))
                    {
                        validUrl = this.curUri.ToString();
                    }
                    else
                    {
                        validUrl = "http://www.qichacha.com/gongsi_industry?industryCode=A&subIndustryCode=1&industryorder=0";
                    }
                    break;
                case (int)SearchType.EnterpriseGuid:
                    searchType = SearchType.EnterpriseGuid;
                    validUrl = "http://www.qichacha.com/search?key=%E5%B7%A7%E5%90%88&type=enterprise&method=all";
                    break;
                case (int)SearchType.EnterpriseGuidByKeyWord:
                    searchType = SearchType.EnterpriseGuidByKeyWord;
                    validUrl = "http://www.qichacha.com/search?key=%E5%B7%A7%E5%90%88&type=enterprise&method=all&1=2";
                    break;
                case (int)SearchType.EnterpriseGuidByKeyWordEnhence:
                    searchType = SearchType.EnterpriseGuidByKeyWordEnhence;
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    break;
                case (int)SearchType.EnterpriseGuidByKeyWord_APP:
                    searchType = SearchType.EnterpriseGuidByKeyWord_APP;
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    break;
                case (int)SearchType.UpdateEnterpriseInfo:
                default:
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    searchType = SearchType.UpdateEnterpriseInfo;
                    break;

            }

        }

        private void button7_Click(object sender, EventArgs e)
        {
            List<string> QueueList = new List<string>();
            if (UrlQueue.Instance.Count > 0)
            {
                while (UrlQueue.Instance.Count > 0)
                {
                    QueueList.Add(UrlQueue.Instance.DeQueue().UrlString);
                }
                SerializerXml<List<string>> serial = new SerializerXml<List<string>>(QueueList);
                serial.BuildXml("UrlQueue.xml");
            }

            List<string> keyWordQueueList = new List<string>();
            if (StringQueue.Instance.Count > 0)
            {
                while (StringQueue.Instance.Count > 0)
                {
                    keyWordQueueList.Add(StringQueue.Instance.DeQueue());
                }
                SerializerXml<List<string>> keyWordSerial = new SerializerXml<List<string>>(keyWordQueueList);
                keyWordSerial.BuildXml("UrlQueueKeyWord.xml");
            }
            
            MessageBox.Show(string.Format("url:{0},KeyWord:{1}",QueueList.Count.ToString(), keyWordQueueList.Count()));


        }

        private void button8_Click(object sender, EventArgs e)
        {
            initCurCity();//初始化城市,初始化enterpriseKey的ip
            if (curCity == null)
            {
                MessageBox.Show("请先初始化城市");
                return;
            }
            List<string> QueueList2 = new List<string>();
            SerializerXml<List<string>> serial2 = new SerializerXml<List<string>>(QueueList2);
            QueueList2 = serial2.BuildObject("UrlQueue.xml");
            foreach (var queue in QueueList2)
            {
                UrlQueue.Instance.EnQueue(new UrlInfo(HttpUtility.UrlDecode(queue)));
                if (!urlFilter.Contains(queue))
                {
                    urlFilter.Add(queue);
                }
            }

            List<string> keyWordQueueList = new List<string>();
            SerializerXml<List<string>> keyWordserial = new SerializerXml<List<string>>(keyWordQueueList);
            keyWordQueueList = serial2.BuildObject("UrlQueueKeyWord.xml");
            foreach (var keyWord in keyWordQueueList)
            {
                StringQueue.Instance.EnQueue(keyWord);
            }
            MessageBox.Show("url:{0}keyWord:{1}",UrlQueue.Instance.Count.ToString());
        }

        private void ipChangeTimer_Tick(object sender, EventArgs e)
        {
            if (UserProxyChk.Checked)
            {
                ShowMessageInfo("开始切换ip" + GetWebProxyString());
                ChangeIp();

            }
        }
        private void ChangeIp()
        {
            SimpleCrawler.HttpResult result = new SimpleCrawler.HttpResult();
            try
            {
                var item = new SimpleCrawler.HttpItem()
                {
                    URL = "http://proxy.abuyun.com/switch-ip",
                    Method = "get",//URL     可选项 默认为Get   
                    // ContentType = "text/html",//返回类型    可选项有默认值 
                    UserAgent = "okhttp/3.2.0",
                    ContentType = "application/x-www-form-urlencoded",
                };

                // item.Header.Add("Content-Type", "application/x-www-form-urlencoded");
                // hi.HeaderSet("Content-Length","154");
                // hi.HeaderSet("Connection","Keep-Alive");
                item.Header.Add("Proxy-Switch-Ip", "yes");
                item.WebProxy = GetWebProxy();
                result = http.GetHtml(item);
               // ShowMessageInfo(result.Html);
            }
            catch (WebException ex)
            {

            }
            catch (TimeoutException ex)
            {

            }
            catch (Exception ex)
            {

            }
        }

        private void autoChangeAccountCHK_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (ipChangeTimer.Enabled == false)
            {
                ipChangeTimer.Enabled = true;
                ipChangeTimer.Start();
                ShowMessageInfo("ip切换开启");
            }
            else
            {
                ipChangeTimer.Enabled = false;
                ipChangeTimer.Stop();
                ShowMessageInfo("ip切换结束");
            }

        }

        private void button10_Click(object sender, EventArgs e)
        {
            //Settings.AccessToken = richTextBoxInfo.Text;
            var curSettingForm = new SettingsForm(this);
            curSettingForm.Show();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            LandFangSendPhoneCode("17080371218");
            return;
            accountRegisterType = AccountRegisterType.LandFang;
            if (documentText != null && documentText.Body != null && documentText.Body.InnerHtml.Contains("退出"))
            {
                // AutoLogout();
            }
           
            if (curUri == null || curUri.ToString().Contains("www.51ym.me"))
            {
                //RefreshToken();
                PhoneBtnClick();
                //webBrowser.Navigate("http://www.51ym.me/User/MobileSMSCode.aspx");
                AccountPhoneCodeSenerTimerStatusChange(true);//开始获取电话
            }
            else
            {
                MessageBox.Show("请先登录网站www.51ym.me并登陆");
                webBrowser.Navigate("http://www.51ym.me/User/MobileSMSCode.aspx");
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            timerStop();
            while (UrlQueue.Instance.Count > 0)
            {
                 UrlQueue.Instance.DeQueue();
            }
            if (StringQueue.Instance.Count > 0)
            {
                ShowMessageInfo("正在尝试提取关键字");
                var curKeyWord = StringQueue.Instance.DeQueue();
                var url = InitalQCCAppUrlByKeyWord(curKeyWord);
                if (!urlFilter.Contains(url))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(url));
                    timerStart();
                }

            }

        }
    }
}
