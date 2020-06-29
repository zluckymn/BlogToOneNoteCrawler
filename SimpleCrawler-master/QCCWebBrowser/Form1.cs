
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
using Helper.Tree;
using MZ.RabbitMQ;
using Helper;
using System.Globalization;

namespace QCCWebBrowser
{
    public partial class Form1 : Form
    {
        
        #region 变量

        public const bool DEBUGMODE = false;
        private List<BsonDocument> globalKeyWordList = new List<BsonDocument>();//关键字绑定
#pragma warning disable CS0169 // 从不使用字段“Form1.mq”
        MQHelper mq;
#pragma warning restore CS0169 // 从不使用字段“Form1.mq”
        //用于跟踪url拆分项，查看url拆分是否匹配
        public SimpleTreeNode<BsonDocument> globalUrlSplitTreeNode = new SimpleTreeNode<BsonDocument>(new BsonDocument().Add("name", "root"));
        List<BsonDocument> PassKeyWordFilterCondition = new List<BsonDocument>();//判断是否需要进行进入下一个keyWord
        List<BsonDocument> oldCityList = new List<BsonDocument>();//V2版本原始City表;
       public List<BsonDocument> invalidAccountList = new List<BsonDocument>();
        private BloomFilter<string> urlFilter = new BloomFilter<string>(9900000);
        private BloomFilter<string> ipLimitFilter = new BloomFilter<string>(200000);
        private BloomFilter<string> keyWordEntRelateGuidLimitFilter = new BloomFilter<string>(200000);
       
        private string curKeyWordStr = string.Empty;//当前关键字
        private bool needPassKeyWord = true;//是否建议调到下一个关键字
        private bool USEWEBPROXY = true;//是否使用代理
        public static bool OnlyDateUpdate = true;//是否使用时间更新 
        public static bool IsProvince = true;//是否使用省进行爬取
        public static string GRegistCapiBegin = "";//注册金额开始
        public static string GRegistCapiEnd = "";//注册金额开始
        public static bool saveKeyWord = true;//是否保存关键字
        
        //是否查找背后关系
        public static bool IsMoreDetailInfo = false;
        public static string SearchKeyType = "";//产业园搜索类型
        public static bool IndustrySearch = false;//是否产业园搜索
        public static string SearchIndustryCodeLimit = "";//限定行业代码
        public static string SearchSubIndustryCodeLimit = "";//限定子行业代码
        public static string SearchCustomCategoryName = "";//自定义数据源根目录名
        public static int SearchTakeCountLimitPerUrl =-1;//保存返回数据的前n条数据0代表都获取
        public static int SearchKeyWordTakeCountLimit = -1;//每个关键字最大爬取个数
        public static bool globalDBHasPassWord = true;//是否有密码
        public static List<string> PreKeyWordList = new List<string>();//预设的爬取关键字列表1

#pragma warning disable CS0414 // 字段“Form1.ChangeIpWhenInvalid”已被赋值，但从未使用过它的值
        private bool ChangeIpWhenInvalid = true;//是否自动切换IP
#pragma warning restore CS0414 // 字段“Form1.ChangeIpWhenInvalid”已被赋值，但从未使用过它的值
#pragma warning disable CS0414 // 字段“Form1.AccountMaxAddional”已被赋值，但从未使用过它的值
        private int AccountMaxAddional = 1100;//每个账号最大可爬取数量；
#pragma warning restore CS0414 // 字段“Form1.AccountMaxAddional”已被赋值，但从未使用过它的值
#pragma warning disable CS0414 // 字段“Form1.AddToRetryQueueWhenChangeAccount”已被赋值，但从未使用过它的值
        private static int AddToRetryQueueWhenChangeAccount = 20;//当切换账号的时候将前N个url加入待处理队列
#pragma warning restore CS0414 // 字段“Form1.AddToRetryQueueWhenChangeAccount”已被赋值，但从未使用过它的值
        private long AllAddCount = 0;
        string proxyHost = "http://http-cla.abuyun.com";//http://proxy.abuyun.com //http-pro.abuyun.com
        string proxyPort = "9030";
        // 代理隧道验证信息
        //string proxyUser = "H1880S335RB41F8P";
        //string proxyPass = "ECB2CD5B9D783F4E";
        //string proxyUser = ConstParam.proxyUser;//"H1880S335RB41F8P";////HVW8J9B1F7K4W83P
        //string proxyPass = ConstParam.proxyPass;//"ECB2CD5B9D783F4E";////C835A336CD070F9D
        string proxyUser = "H283EZ4CP1YFQCRC";//"H1880S335RB41F8P";////HVW8J9B1F7K4W83P
        string proxyPass = "2BAB4571505B4807";//"ECB2CD5B9D783F4E";////C835A336CD070F9D
        WebProxy getWebProxy = new WebProxy();
        private BloomFilter<string> existGuidList = new BloomFilter<string>(9000000);
        private BloomFilter<string> existNameList = new BloomFilter<string>(9000000);
        const string domainUrl = ConstParam.qUrlHttpsV2;
        //v3 有对应的countryCode 并且不封号 //v2封号 但是没有countryCode字段须有根据地址生成
        string QCCSearchUrl { get { return $"{domainUrl}/app/{(requestNewVersionCHk.Checked == false ? "v2" : "v3")}"; } } 
        // $"{domainUrl}/app/v3";//https://{domainv1}/app/v1/ 旧接口https://appv2./app/v3



        string QCCRefreshTokenUrl = ConstParam.RefreshTokenUrlV2;//$"{domainUrl}/app/v1/adin/refreshToken";
        string QCCAccessTokenUrl = ConstParam.AccessTokenUrlV2;// $"{domainUrl}/app/v1/adin/getAccessToken";
        private const string siteUserNameElm = "nameNormal";
        private const string sitePwdElm = "pwdNormal";
        private static string ip = "192.168.1.121";//192.168.1.230 设备号与企业关键字排序所在Id
        public static string enterpriseIp = "192.168.1.121";//企业库所在ip跨库处理
        private static int port = 37088;//企业库所在ip跨库处理
        public static int enterprisePort = 37088;//企业库所在ip跨库处理
        public static string globalDBName = "SimpleCrawler";//企业库所在ip跨库处理
        private static string connStr = string.Format("mongodb://MZsa:MZdba@{0}:{1}/SimpleCrawler", ip, port);
        private static string curQCCProvinceCode = string.Empty;//qichacha省代码
        private static string curQCCCityCode = string.Empty;//qichacha城市代码
        private string proxyIpDetail = String.Empty;
        //关键字查询企业所在数据库
#pragma warning disable CS0414 // 字段“Form1.enterpriseNameConnStr”已被赋值，但从未使用过它的值
        private static string enterpriseNameConnStr = "mongodb://MZsa:MZdba@192.168.1.114/CompanyHY";
#pragma warning restore CS0414 // 字段“Form1.enterpriseNameConnStr”已被赋值，但从未使用过它的值

        static DataOperation dataop = new DataOperation(new MongoOperation(connStr));//主数据库
        static MongoOperation _mongoDBOp = new MongoOperation(connStr);

        private const bool NEEDRECORDURL = false;//爬取是否记录url
        private static string enterpriseConnStr = string.Format("mongodb://MZsa:MZdba@{0}:{1}/SimpleCrawler", enterpriseIp, enterprisePort);
        static DataOperation enterpriseDataop = new DataOperation(new MongoOperation(enterpriseConnStr));//主数据库
        static MongoOperation _enterpriseMongoDBOp = new MongoOperation(enterpriseConnStr);
        private static CrawlSettings Settings = new CrawlSettings();
        Dictionary<string, string> cityNameDic = new Dictionary<string, string>();
#pragma warning disable CS0414 // 字段“Form1.canNextUrl”已被赋值，但从未使用过它的值
        private static bool canNextUrl = true;
#pragma warning restore CS0414 // 字段“Form1.canNextUrl”已被赋值，但从未使用过它的值
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
        public static int GetAccessRetryTimes = 10;//过验证码成功次数,保护账号 2代表1次
        public static DateTime LastGetPassGeetestTime = DateTime.MinValue;  //上次获取geetest的时间 
        public static int GeetestMaxSpanSecond = 10;  //上次获取geetest的时间 秒
        private string cityNameStr = "";
#pragma warning disable CS0414 // 字段“Form1.tempTableName”已被赋值，但从未使用过它的值
        private string tempTableName = "SQ_xian_Company";//企业黄页目录表
#pragma warning restore CS0414 // 字段“Form1.tempTableName”已被赋值，但从未使用过它的值
        //List<string> existGuidList = new List<string>();

        //string[] enterpriseInfoUrlType = new string[] { "base","touzi","susong", "finance", "run", "report", "assets" };
        string[] enterpriseInfoUrlType = new string[] { "base" };
        System.Timers.Timer aTimer = new System.Timers.Timer();
        System.Timers.Timer autoRestartTimer = new System.Timers.Timer();
        HttpInput hi = new HttpInput();
        SimpleCrawler.HttpHelper http = new SimpleCrawler.HttpHelper();
        bool waitBrowerMouseUpResponse = false;
        SearchType searchType = SearchType.UpdateEnterpriseInfoAPP;
        AccountRegisterType accountRegisterType = AccountRegisterType.QiChaCha;
        UrlSpliteMode urlSplitMode = UrlSpliteMode.DateFirst;//默认
        private static object lockPassGeek = new object();
        private static object lockRefressToken = new object();
        private static object locakTimeGear = new object();
        private System.Windows.Forms.HtmlDocument documentText;
        static string curHtml = string.Empty;
        List<BsonDocument> allSubFactoryList = new List<BsonDocument>();//子类
        //线程安全的字典统计
        ConcurrentDictionary<string, Tuple<int, int>> keyWordCountDic = new ConcurrentDictionary<string, Tuple<int, int>>();
        //用于统计当前已经多久次没有新增数据了
        Tuple<int, int> keyWordNoAddSeconds = new Tuple<int, int>(0, 0);
        private List<BsonDocument> allCountyCodeList = new List<BsonDocument>();
        string[] specialCity = new string[] { "北京", "天津", "上海", "重庆" };
        string[] specialProvince = new string[] { "SH", "BJ", "TJ", "CQ" };
        public List<BsonDocument> allFactoryList = new List<BsonDocument>();

        public enum SearchType
        {
            /// <summary>
            /// 更新企业信息
            /// </summary>
            [EnumDescription("UpdateEnterpriseInfoAPP")]
            UpdateEnterpriseInfoAPP = 0,
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
            [EnumDescription("EnterpriseInvent")]
            EnterpriseInvent = 7,
            [EnumDescription("ADDEnterpriseAPP")]
            ADDEnterpriseAPP = 8,
            [EnumDescription("EnterprisePositionDetailByGuid_APP")]
            EnterprisePositionDetailByGuid_APP =9,
            [EnumDescription("EnterpriseBackGroundDetailByGuid_APP")]//背后关系
            EnterpriseBackGroundDetailByGuid_APP = 10,
            
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
        #region 表名相关
        public const string DATATABLENAME = "QCCEnterpriseKey";
        public string curEnterpriseKeySuffixTxt="";
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableName
        {
            get
            {

                if (!string.IsNullOrEmpty(curEnterpriseKeySuffixTxt))
                {
                    return DATATABLENAME + "_" + curEnterpriseKeySuffixTxt;
                }
                else
                {
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
        /// 关键字匹配关联表，当某个关键字匹配匹配多个企业的时候进行关联绑定
        /// </summary>
        public static string DataTableName_KeyWordEntRelation
        {
            get { return DATATABLENAME + "KeyWordEntRelation"; }

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
            get
            {
                return "QiXinEnterpriseKeyWord";//1000多
                //return "QCCEnterpriseKeyWord";//企业最下级
            }

        }
        /// <summary>
        /// 岗位信息
        /// </summary>
        public static string DataTableEnterprisePosition
        {
            get
            {
                return "QCCEnterpriseKey_PositionDetail";//1000多
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
            get { return "QCCAccountHashMap"; }
        }
        /// <summary>
        ///  新版本对应的countyCode
        /// </summary>
        public static string DataTableCountyCode
        {
            get { return "QCCEnterpriseKeyCountyCode"; }
        }

        public static string DataTableKeyWordSearch
        {
            get { return "QCCEnterpriseKeyKeyWordSearch"; }
        }


        public string DataTableListCompany
        {
            get
            {
                return "ListedCompanyInformation";
            }
        }

        public string DataTableUnicornCompany
        {
            get
            {
                return "UnicornCompany";
            }
        }
        /// <summary>
        /// 房地产商拿地数据
        /// </summary>
        public string DataTableLandHouseRelation
        {
            get
            {
                return "QCCEnterpriseKey_House_Land_Relation";
            }
        }

        public string DataTableMoreDetailInfo
        {
            get
            {
                return "QCCEnterpriseKeyMoreDetailInfo";
            }
        }

        public string DataTableInventInfo
        {
            get
            {
                return "QCCEnterpriseKeyInventInfo";
            }
        }

        public static string qccDeviceAccountName = "QCCDeviceAccount";
        /// <summary>
        /// QCCApp设备账号
        /// </summary>
        public static string QCCDeviceAccount
        {
            get { 
                
                return qccDeviceAccountName;
           }
        }
        #endregion
        public static string LimitIpPoor
        {
            get { return "LimitIpPoor"; }
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
            SetEnterpriseDataOP(enterpriseIp);//默认
            if (USEWEBPROXY)
            {
                ChangeIp();
            }
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
            if (true || string.IsNullOrEmpty(Settings.AccessToken))
            {
                var result = RefreshToken(true);//强制刷新
                if (result.Contains("成功"))
                {

                    ShowMessageInfo(string.Format("DeviceId:{0},timestamp:{1},sign:{2},RefleshToken:{3},AccessToken:{4}", Settings.DeviceId, Settings.timestamp, Settings.sign, Settings.RefleshToken, Settings.AccessToken), false);
                    if (IsNeedLogin(this.searchType))
                    {
                       
                        ChangeRandomLoginAccount();
                       // AppAutoLoin();
                    }
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

            if (deviceAccount != null)
            {
                Settings.AccounInfo = deviceAccount.Text("updateDate");
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

        private string curModetailInfoTableName = string.Empty;
        #endregion
        public Form1()
        {
            InitializeComponent();
        }
        #region 初始化待处理的数据
        private BsonDocument curCity = null;
        private string curAreaCode = string.Empty;
        private void initCurCity()
        {
            cityNameStr = "";
            // var cityNameStr = "上海,北京,成都,福州,广州,杭州,黄山,济南,龙岩,南昌,南京,宁波,泉州,深圳,苏州,武汉,西安,厦门,大连,长沙,合肥,镇江,宁波,中山,郑州,昆明,江苏,重庆";//北京,广州,上海
            var cityNameList = cityNameStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (comboBox2.SelectedIndex != -1)
            {
                cityNameStr = comboBox2.SelectedItem.ToString();
            }
            SetEnterpriseDataOP(enterpriseIp);//默认
            //修正后通过企查查 内部城市地址进行查找
            var cityList = dataop.FindAll(DataTableCity).ToList();
            curCity = cityList.Where(c => c.Text("name").Contains(cityNameStr)).FirstOrDefault();
            if (curCity == null && !string.IsNullOrEmpty(cityNameStr))
            {
                //县级市
                var regionCode = this.allCountyCodeList.Where(c => c.Text("name").Contains(cityNameStr)).FirstOrDefault();
                if (regionCode != null)
                {
                    curAreaCode = regionCode.Text("code");
                    var cityObj_country = this.allCountyCodeList.Where(c => c.Text("code") == regionCode.Text("cityCode")).FirstOrDefault();
                    if (cityObj_country != null)
                    {
                        cityNameStr = cityObj_country.Text("name").TrimEnd("市"[0]);
                        curCity = cityList.Where(c => c.Text("name").Contains(cityNameStr)).FirstOrDefault();
                    }

                }
                // return;
            }
            if (curCity == null || string.IsNullOrEmpty(cityNameStr))
            {
                ShowMessageInfo("无城市");
                this.Text = "无城市";
            }
            // SetEnterpriseDataOP("192.168.1.124");//默认
            //if (!string.IsNullOrEmpty(curCity.Text("enterpriseIp")))
            //{
            //    SetEnterpriseDataOP(curCity.Text("enterpriseIp"));
            //}
            //else
            //{
            //    //  SetEnterpriseDataOP(ip);//默认
            //    SetEnterpriseDataOP("192.168.1.124");//默认
            //}
            if (IsAPP(searchType))
            {
                this.guardTimer.Interval = 30000;
            }
        }

        private void InitUrlSplitTreeNode()
        {
            if (singalKeyWordCHK.Checked)
            {
                globalUrlSplitTreeNode = new SimpleTreeNode<BsonDocument>(new BsonDocument().Add("name", "root"));
            }
        }
        #region CountryCode相关
        /// <summary>
        /// 通过areaCode获取当前的城市名
        /// </summary>
        /// <param name="areaCode"></param>
        /// <returns></returns>
        private string GetCountryByAreaCode(string areaCode)
        {
            string cityName = string.Empty;
            string[] separator = new string[] { "\t", "" };
            string[] areaCodeArr = areaCode.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (areaCode.Length < 6)
            {
                areaCode = areaCode.PadRight(6, '0');
            }
            if (areaCodeArr.Length >= 2)
            {
                string _areaCode = areaCodeArr[0];
                if (_areaCode.Length < 6)
                {
                    _areaCode = _areaCode.PadRight(6, '0');
                }
                var hitCurCityCode = (from c in this.allCountyCodeList
                                      where c.Text("code") == _areaCode
                                      select c).FirstOrDefault<BsonDocument>();
                if (hitCurCityCode == null)
                {
                    BsonDocument regionCode = (from c in this.allCountyCodeList
                                               where c.Text("code") == areaCodeArr[1]
                                               select c).FirstOrDefault<BsonDocument>();
                    if (regionCode != null)
                    {
                        string cityCode = regionCode.Text("cityCode");
                        hitCurCityCode = (from c in this.allCountyCodeList
                                          where c.Text("code") == cityCode
                                          select c).FirstOrDefault<BsonDocument>();
                    }
                }
                if (hitCurCityCode != null)
                {
                    cityName = hitCurCityCode.Text("name");
                }
                return cityName;
            }
            string code = areaCodeArr[0];
            if (code.Length < 6)
            {
                code = code.PadRight(6, '0');
            }
            BsonDocument curCityCode = (from c in this.allCountyCodeList
                                        where c.Text("code") == code
                                        select c).FirstOrDefault<BsonDocument>();
            if (curCityCode != null)
            {
                cityName = curCityCode.Text("name");
            }
            return cityName;
        }

        private string GetSpeicalProvinceCode(string provinceName)
        {
            if (string.IsNullOrEmpty(provinceName)) return string.Empty;
            var hitProvince = allCountyCodeList.Where(c => c.Text("name").Contains(provinceName)).FirstOrDefault();
            if (hitProvince != null)
            {
                return hitProvince.Text("code");
            }
            return string.Empty;
        }

        /// <summary>
        /// 根据城市名获取城市代码
        /// </summary>
        /// <param name="cityName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private string GetCountryCode(string cityName, string type = "1")
        {
            if (string.IsNullOrEmpty(cityName)) return string.Empty;
            // "北京", "天津", "上海", "重庆" 直接使用type=1的市会减少数据，应该返回空
            if (type == "1" || specialCity.Contains(cityName))
            {
                return string.Empty;
            }
            return GetProvinceCountryCode("", cityName, type);
        }

        /// <summary>
        /// 获取省份
        /// </summary>
        /// <param name="provinceCode"></param>
        /// <param name="cityName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private string GetProvinceCountryCode(string provinceCode, string cityName, string type = "1")
        {
            if (string.IsNullOrEmpty(cityName)) return string.Empty;

            if (allCountyCodeList.Count() <= 0)
            {
                return string.Empty;
            }
            var hitCountyCodeObj = allCountyCodeList.Where(c => (provinceCode == "" || c.Text("provinceCode") == provinceCode) && c.Text("name") == cityName || c.Text("name").Contains(cityName.TrimEnd('市')) && c.Text("type") == type).FirstOrDefault();
            if (hitCountyCodeObj == null)
            {

                hitCountyCodeObj = allCountyCodeList.Where(c => (provinceCode == "" || c.Text("provinceCode") == provinceCode) && c.Text("name") == cityName || c.Text("name").Contains(cityName.TrimEnd('市'))).FirstOrDefault();
            }

            if (hitCountyCodeObj != null)
            {
                if (hitCountyCodeObj.Text("type") != type)
                {
                    if (hitCountyCodeObj.Text("type") == "2")
                    {
                        if (type == "1")
                        {
                            return hitCountyCodeObj.Text("cityCode");
                        }
                        if (type == "0")
                        {
                            return hitCountyCodeObj.Text("provinceCode");
                        }
                    }
                    if (hitCountyCodeObj.Text("type") == "1")
                    {
                        if (type == "1")
                        {
                            return hitCountyCodeObj.Text("code");
                        }
                        if (type == "0")
                        {
                            return hitCountyCodeObj.Text("provinceCode");
                        }
                    }
                }
                return hitCountyCodeObj.Text("code");
            }
            return string.Empty;
        }

        /// <summary>
        /// 根据城市名获取城市代码获取所有的区
        /// </summary>
        /// <param name="cityName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private List<string> GetSubCountryCodeList(string provinceCode, string countryCode, string type = "2")
        {
            if (!string.IsNullOrEmpty(countryCode))
            {
                var regionCountryCodeList = allCountyCodeList.Where(c => c.Text("isCity") != "1" && c.Text("provinceCode") == provinceCode && c.Text("cityCode") == countryCode && c.Text("type") == type).Select(c => c.Text("code")).ToList();
                return regionCountryCodeList;
            }
            else //省份爬取 或者北上广深
            {
                var regionCountryCodeList = allCountyCodeList.Where(c => c.Text("isCity") != "1" && c.Text("provinceCode") == provinceCode && c.Text("type") == type).Select(c => c.Text("code")).ToList();
                return regionCountryCodeList;
            }
        }

        /// <summary>
        /// 根据countyCode获取省份代码
        /// </summary>
        /// <param name="countyCode"></param>
        /// <returns></returns>
        private string GetCountryProvinceCode(string countyCode)
        {
            BsonDocument cityCountryCode = (from c in this.allCountyCodeList
                                            where c.Text("code") == countyCode
                                            select c).FirstOrDefault<BsonDocument>();
            if (cityCountryCode != null)
            {
                return cityCountryCode.Text("provinceCode");
            }
            return string.Empty;
        }

        /// <summary>
        /// 根据地址生成areaCode字段
        /// </summary>
        /// <param name="cityName">城市名</param>
        /// <param name="registrar">注册管理局</param>
        /// <param name="address">地址</param>
        /// <returns></returns>
        private string GetCountryCodeByAddress(string provinceCode, string address, string cityName, string registrar)
        {
            var cityCode = string.Empty;
            if (string.IsNullOrEmpty(cityName))
            {
                var hitCityObj = allCountyCodeList.Where(c => c.Text("type") == "1" && c.Text("provinceCode") == provinceCode && address.Contains(c.Text("name").TrimEnd('市'))).FirstOrDefault();
                if (hitCityObj == null && !string.IsNullOrEmpty(registrar))
                {
                    hitCityObj = allCountyCodeList.Where(c => c.Text("type") == "1" && c.Text("provinceCode") == provinceCode && registrar.Contains(c.Text("name").TrimEnd('市'))).FirstOrDefault();
                }
                if (hitCityObj == null) return string.Empty;
                cityName = hitCityObj.Text("name");
                cityCode = hitCityObj.Text("code");
            }
            else
            {
                cityCode = GetProvinceCountryCode(provinceCode, cityName, "1");
            }

            if (string.IsNullOrEmpty(cityCode))
            {
                return string.Empty;
            }
            var prefixCode = allCountyCodeList.Where(c => c.Text("cityCode") == cityCode && c.Text("isCity") == "1").FirstOrDefault();
            if (prefixCode == null)
            {
                prefixCode = new BsonDocument();
            }
            //获取子区域
            var regionCountryCodeList = allCountyCodeList.Where(c => c.Text("isCity") != "1" && c.Text("cityCode") == cityCode).ToList();
            var hitRegion = regionCountryCodeList.Where(c => address.Contains(c.Text("name"))).FirstOrDefault();
            if (hitRegion != null)
            {
                return $"{prefixCode.Text("code")}\t{hitRegion.Text("code")}";
            }
            if (!string.IsNullOrEmpty(registrar))
            {
                //根据注册地
                hitRegion = regionCountryCodeList.Where(c => registrar.Contains(c.Text("name"))).FirstOrDefault();
                if (hitRegion != null)
                {
                    return $"{prefixCode.Text("code")}\t{hitRegion.Text("code")}";
                }
            }

            hitRegion = regionCountryCodeList.Where(c => address.Contains(FixRegionStr(c.Text("name")))).FirstOrDefault();
            if (hitRegion != null)
            {
                return $"{prefixCode.Text("code")}\t{hitRegion.Text("code")}";
            }
            if (!string.IsNullOrEmpty(registrar))
            {
                //根据注册地
                hitRegion = regionCountryCodeList.Where(c => registrar.Contains(FixRegionStr(c.Text("name")))).FirstOrDefault();
                if (hitRegion != null)
                {
                    return $"{prefixCode.Text("code")}\t{hitRegion.Text("code")}";
                }
            }
            //hitRegion = regionCountryCodeList.Where(c => c.Text("name")=="其他").FirstOrDefault();
            //if (hitRegion != null)
            //{
            //    return $"{prefixCode.Text("code")}\t{hitRegion.Text("code")}";
            //}
            return string.Empty;
        }


        /// <summary>
        /// 根据对象生成AreaCode
        /// </summary>
        /// <param name="provinceCode"></param>
        /// <param name="cityName"></param>
        /// <param name="enterpriseObj"></param>
        /// <returns></returns>
        private string GetCountryCodeByAddress(string provinceCode, string cityName, JToken enterpriseObj)
        {
            var address = enterpriseObj["Address"] != null ? enterpriseObj["Address"].ToString() : string.Empty;
            var registrar = enterpriseObj["BelongOrg"] != null ? enterpriseObj["BelongOrg"].ToString() : string.Empty;
            if (string.IsNullOrEmpty(provinceCode))
            {
                provinceCode = enterpriseObj["Province"] != null ? enterpriseObj["Province"].ToString() : string.Empty;
            }
            return GetCountryCodeByAddress(provinceCode, address, cityName, registrar);
        }

        #endregion
        /// <summary>
        /// 初始化url链接
        /// </summary>
        /// <param name="_typeName"></param>
        /// <param name="_cityName"></param>
        /// <returns></returns>
        private List<string> InitalQCCAppUrlByKeyWord(string _typeName, string _cityName = "")
        {
            var initialKeyWord = _typeName;//初始化关键字
            var hitKeyWordObj = GetKeyWordGuid(initialKeyWord);//关键字guid 用于获取到的数据匹配目前用于蔬菜关键字匹配
            var hitKeyWordGuid = hitKeyWordObj.Text("keyWordGuid");
            var hitKeyWordIndustryCode= hitKeyWordObj.Text("industryCode");//关键字对应行业
            InitUrlSplitTreeNode();
            _typeName = _typeName.Replace("）", "");
            string uniqueKey = string.Empty;
            if (_typeName.Contains("|H|"))
            {
                string[] separator = new string[] { "|H|" };
                string[] strArray = _typeName.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                if (strArray.Length >= 2)
                {
                    _typeName = strArray[0];
                    _cityName = strArray[1];
                    if (strArray.Length >= 3)
                    {
                        uniqueKey = strArray[2];
                    }
                }
            }
            string curTypeName = _typeName;
            if (curTypeName.Length == 1)
            {
                curTypeName = curTypeName + "业";
            }
            string _curTypeName = string.Empty;
            if (!string.IsNullOrEmpty(SearchKeyType))
            {
                _curTypeName = (_curTypeName + string.Format("\"{0}\"", SearchKeyType)) + ":" + string.Format("\"{0}\"", curTypeName);
                curTypeName = "{" + _curTypeName + "}";
            }
            else
            {
                curTypeName = HttpUtility.UrlEncode(curTypeName).ToUpper();
            }
            if (string.IsNullOrEmpty(_cityName))
            {
                _cityName = this.cityNameStr;
            }
            string countyCode = string.Empty;
            if (this.specialCity.Any<string>(c => c.Contains(_cityName)))
            {
                countyCode = this.GetCountryCode(_cityName, "1");
            }
            else
            {
                countyCode = this.GetCountryCode(_cityName, "");
            }
            string province = string.Empty;
            if (!string.IsNullOrEmpty(countyCode))
            {
                province = this.GetCountryProvinceCode(countyCode);
            }
            else
            {
                province = this.GetCountryCode(_cityName, "0");
                if (string.IsNullOrEmpty(province) && this.specialCity.Any<string>(c => c.Contains(_cityName)))
                {
                    province = GetSpeicalProvinceCode(_cityName);
                }
            }
            //2019.7.19添加如温岭县级市区县搜索支持
            if (!string.IsNullOrEmpty(curAreaCode))
            {
                countyCode = curAreaCode;
                ShowMessageInfo("正在进行县级市查找" + countyCode, true);
            }
            if ((countyCode == "") && (province == ""))
            {
                this.ShowMessageInfo(string.Format("{0}的代码为null", this.cityNameStr), false);
                //return string.Empty;
            }
            string url = string.Format(this.QCCSearchUrl + "/base/advancedSearch?searchKey={0}&searchIndex=default&province=&pageIndex=1&isSortAsc=false&startDateBegin=&startDateEnd=&registCapiBegin=&registCapiEnd=&industryV3=&industryCode=&subIndustryCode=&searchType=&countyCode=&cityCode=", new object[] { curTypeName });
            if (!string.IsNullOrEmpty(province))
            {
                url = this.ReplaceParam(url, "province", "", province);
            }
            if (IsProvince)
            {
                url = string.Format(this.QCCSearchUrl + "/base/advancedSearch?searchKey={0}&searchIndex=default&province={1}&pageIndex=1&isSortAsc=false&startDateBegin=&startDateEnd=&registCapiBegin=&registCapiEnd=&industryV3=&industryCode=&subIndustryCode=&searchType=&countyCode=&cityCode=", curTypeName, province);
            }
            else
            {
                //旧版本增加cityCode
                if (IsQCCUrlNewVersion())
                {
                    if (!string.IsNullOrEmpty(countyCode))
                    {
                        url = this.ReplaceParam(url, "countyCode", "", countyCode);
                    }
                }
                else
                {
                    //根据cityName获取city
                    var oldcityObj = oldCityList.Where(c => c.Text("name").Contains(_cityName)).FirstOrDefault();
                    if (oldcityObj != null)
                    {
                        url = this.ReplaceParam(url, "cityCode", "", oldcityObj.Text("code"));
                    }
                }
            }
            if (!string.IsNullOrEmpty(GRegistCapiBegin) || !string.IsNullOrEmpty(GRegistCapiEnd))
            {
                url = this.ReplaceParam(url, "registCapiBegin", "", GRegistCapiBegin);
                url = this.ReplaceParam(url, "registCapiEnd", "", GRegistCapiEnd);
            }
            #region 适用于旧版本的条件过滤
            if (!string.IsNullOrEmpty(hitKeyWordGuid))//附加关键字guid
            {
                url += $"&k={hitKeyWordGuid}";
            }
            var curSearchIndustryCodeLimit = SearchIndustryCodeLimit;
            if (string.IsNullOrEmpty(curSearchIndustryCodeLimit) && !string.IsNullOrEmpty(hitKeyWordIndustryCode))
            {
                curSearchIndustryCodeLimit = hitKeyWordIndustryCode;//关键字中的行业过滤
            }
          
            if (!string.IsNullOrEmpty(curSearchIndustryCodeLimit))
            {
                //2020.2.13写死industryCode
                url = this.ReplaceUrlParamEx(url, "industryCode", curSearchIndustryCodeLimit);
            }
            //后续进行子行业过滤
            if (!string.IsNullOrEmpty(SearchSubIndustryCodeLimit))
            {
                //2020.2.13写死industryCode
                url = this.ReplaceUrlParamEx(url, "subIndustryCode", SearchSubIndustryCodeLimit);
            }
            #endregion
            if (url.Contains("{"))
            {
                url = this.ReplaceParam(url, "searchIndex", "default", "multicondition");
            }
            if (!string.IsNullOrEmpty(uniqueKey))
            {
                url = url + "&uniqueKey=" + uniqueKey;
            }
            if (OnlyDateUpdate)
            {
                string endDateStr = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");//默认最新时间
                if (!string.IsNullOrEmpty(this.updateEndDateTxt.Text))
                {
                    endDateStr = this.updateEndDateTxt.Text.Trim();
                }
                string startDateStr = this.updateDateTxt.Text;
                url = this.ReplaceParam(url, "startDateBegin", "", startDateStr);
                url = this.ReplaceParam(url, "startDateEnd", "", endDateStr);
            }

            var urlList = new List<string>();


            //只展示父类,当有获得数据在进行细分
            //产业过滤.Where(c=>c.Text("code")=="K") V3版本无法在条件中获取筛选而且无返回了产业类别因此需要有点增加
            if (IsQCCUrlNewVersion())
            {
                foreach (var dataValueDic in allFactoryList)//2016
                {

                    var dataValue = dataValueDic.Text("code");
                    if (string.IsNullOrEmpty(dataValue)) continue;
                    var curUrl = url.Replace("&industryV3=&industryCode=", string.Format("&industryV3=&industryCode={0}", dataValue));
                    urlList.Add(curUrl);
                }
            }
            else
            {
                urlList.Add(url);
            }

            return urlList;
        }
        /// <summary>
        /// 通过关键字+地区进行公司获取(主要)
        /// </summary>
        private void InitialEnterpriseGuidByKeyWord_App()
        {
            if (StringQueue.Instance.Count > 0)
            {
                return;
            }
            Console.WriteLine("初始化数据");
            Settings.MaxReTryTimes = 100;//尝试最大个数
            if (!string.IsNullOrEmpty(this.MaxAccountCrawlerCountTxt.Text))
            {
                int crawlerCount = 0;
                if (int.TryParse(this.MaxAccountCrawlerCountTxt.Text, out crawlerCount))
                {//最大化利用账号
                    Settings.MaxAccountCrawlerCount = crawlerCount;
                }
            }
            if (IsMoreDetailInfo)
            {
                this.InitialEnterpriseGuidMoreDetailInfoAPP();
            }
            else if (IndustrySearch)
            {
                this.InitialEnterpriseOtherSourceAPP();
            }

            //var cityNameStr = "上海,北京,成都,福州,广州,杭州,黄山,济南,龙岩,南昌,南京,宁波,泉州,深圳,苏州,武汉,西安,厦门,大连,长沙,合肥,镇江,宁波,中山,郑州,昆明,江苏,重庆";

            if (curCity == null)
            {
                ShowMessageInfo("请先初始化城市");
                curCity = new BsonDocument();
                //return;
            }
            var curProvince = oldCityList.Where(c => c.Text("code") == curCity.Text("provinceCode")).FirstOrDefault();
            var typeList = dataop.FindAll(DataTableIndustry).ToList();//遍历所有的父分类与子分类
            allFactoryList = typeList.Where(c => c.Int("type") == 0).ToList();
            allSubFactoryList = typeList.Where(c => c.Int("type") == 1).ToList();//获取子类

            if (NEEDRECORDURL)
            {                                                     //因为app只能取40个一页需要条件筛选到最后，并且超出个数需要后续通过vip账号进行重复爬取                                                                                                                                                                              // foreach (var hitUrl in allHitUrlList.Where(c=>c.Int("isSplit")!=1&& c.Int("isFirstPage") != 1))
#pragma warning disable CS0162 // 检测到无法访问的代码
                var allHitUrlList = dataop.FindAllByQuery(DataTableNameKeyWordURLAPP, Query.And(Query.EQ("province", curCity.Text("provinceCode")), Query.EQ("cityCode", curCity.Text("code")), Query.EQ("isApp", "1"))).ToList();//获取执行过的url
#pragma warning restore CS0162 // 检测到无法访问的代码
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
            ///是否有预设的关键字
            if (PreKeyWordList.Count > 0)
            {
                typeNameList = PreKeyWordList;
            }
            else
            {
                ///是否限制split次数
                if (this.splitLimitChk.Checked == true)
                {
                    typeNameList = dataop.FindFieldsByQuery(DataTableEnterriseKeyWord, null, new string[] { "keyWord", "count" }).Where(c => c.Int("count") > 0).OrderByDescending(c => c.Int("count")).Select(c => c.Text("keyWord")).ToList();
                    typeNameList.AddRange(new string[] { "零售", "塑料", "设备", "金属" });
                }
                else
                {
                    var cityNameQueryList = new List<string>() { "重庆", "南京", "天津", "北京", "上海", "深圳", "广州" };
                    // typeNameList = dataop.FindFieldsByQuery(DataTableEnterriseKeyWordCount, Query.EQ("cityName", "广州"), new string[] { "keyWord", "count" }).Where(c => c.Int("count") > 20).OrderByDescending(c => c.Int("count")).Select(c => c.Text("keyWord")).ToList();
                    typeNameList = dataop.FindFieldsByQuery(DataTableEnterriseKeyWordCount, Query.In("cityName", cityNameQueryList.Select(c => (BsonValue)c)), new string[] { "keyWord", "count" }).Where(c => c.Int("count") > 100).OrderByDescending(c => c.Int("count")).Select(c => c.Text("keyWord")).Distinct().ToList();
                    var needAddKeyWords = new string[] { "房地产", "贸易", "交通", "轻工", "食品", "医药", "公用", "金属", "电子", "建筑材料", "化工", "金融" };
                    typeNameList.AddRange(needAddKeyWords);
                }

                InitKeyWordHitCount(cityNameStr);//初始化关键词组

                if (keyWordSourceCHK.Checked == true)//是否使用企业分类
                { //是否使用关键字数据源
                    typeNameList = typeNameList.Where(c => c != "").Take(10).ToList();//2020.1.3注释关键字当后续爬取速度过慢进行重新开放
                    // typeNameList = new List<string>();
                    //2020.1.3新增关键字用于覆盖 typeNameList.Where(c => c != "").Take(10).ToList()
                    //var customTypeNameArray = "科技,管理,零售,食品,文化,建筑,商贸,贸易,工程".SplitParam(",");
                    //typeNameList.AddRange(customTypeNameArray);//作为前面数据的补充如果没有可快速跳过
                    var tempTypeNameList = dataop.FindAll(DataTableScopeKeyWord).Select(c => c.Text("keyWord")).OrderBy(c => c.Length).ToList();
                    typeNameList.AddRange(tempTypeNameList);

                }
                else
                {

                    typeNameList = new List<string>();
                    //2020.1.3新增关键字用于覆盖 typeNameList.Where(c => c != "").Take(10).ToList()
                    var customTypeNameArray = "管理,服务,咨询,工程,销售,零售,五金,建筑餐饮,,汽车,科技,教育,装饰,金融,贸易,中心,建材,房地产开发,置业".SplitParam(",");//物流,
                    typeNameList.AddRange(customTypeNameArray);//作为前面数据的补充如果没有可快速跳过
                    var tempTypeNameList = dataop.FindAll(DataTableScopeKeyWord).Select(c => c.Text("keyWord")).OrderBy(c => c.Length).ToList();
                    typeNameList.AddRange(tempTypeNameList);
                    //var otherCustomerArray= "经营,文化, 汽车,电子,咨询,食品,教育,建筑,装饰,科技,商贸,贸易,中心,商务,技术,网络,实业,化工,金融,汽车,设备,发展,投资,传播,传媒,建材,地产,国际,农业,机械,开发,材料,设计,服饰,合作,生物,广告,专业,建设,食品,经营,机电,用品,环保,能源,艺术,健康,医疗,智能,金属,服装,化工,医药,酒店,种植,代理,连锁,美容,资源,策划,体育,生态,纺织,药房,经济,新能源,五金,培训,供应,物资,营销,机械,集团,家具,安装,包装,产品,开发区,置业,商务信息,园林,电器,研究,卫生,运输,营业,养殖,器械,商业,旅游,家政,航空,维修".SplitParam(",");
                    //typeNameList.AddRange(otherCustomerArray);
                }

                // typeNameList = new List<string>() { "德克士", "肯德基","麦当劳","华莱士","豪客来","必胜客" };
                // typeNameList = new List<string>() { "中国邮政", };
            }
            // typeNameList.Add("母婴");
            //   typeNameList = new List<string>() { "区块链培训" };
            var fetchKeyWorldCount = 0;
            int.TryParse(KeyWordFilterTextBox.Text.Trim(), out fetchKeyWorldCount);
            var skipCount = fetchKeyWorldCount == 0 ? 0 : typeNameList.Distinct().Count() - fetchKeyWorldCount;
            if (DEBUGMODE)
            {
#pragma warning disable CS0162 // 检测到无法访问的代码
                typeNameList = new List<string> { "服务" };
#pragma warning restore CS0162 // 检测到无法访问的代码
            }
          //  typeNameList = new List<string> { "服务" };
            if (useCustomerKeyWordCHK.Checked)
            {
                //自定义关键字 2020.2.13
                var customerKeyWordList = InitialCustomerKeyWord();
                typeNameList = customerKeyWordList.Select(c => c.Text("keyWord")).ToList();
                ShowMessageInfo("使用自定义数据源");
            }
            ShowMessageInfo($"正在使用详细关键字{string.Join(",", typeNameList)}",true);
            foreach (var _typeName in typeNameList.Distinct().Skip(skipCount))
            {
                if (!this.singalKeyWordCHK.Checked)//是否单个单个运行
                {
                    //按时间逆序
                
                    var urlList = InitalQCCAppUrlByKeyWord(_typeName, "");
                    foreach (var url in urlList)
                    {
                        if (!urlFilter.Contains(url))
                        {
                            var urlInfo = new UrlInfo(url) { Depth = 1 };
                            ///是否限制split次数
                            if (this.splitLimitChk.Checked == true)
                            {
                                urlInfo.UrlSplitTimes = -3;//只能拆分三次
                            }
                            UrlQueue.Instance.EnQueue(urlInfo);
                            urlFilter.Add(url);

                        }
                    }
                }
                else
                {
                    StringQueue.Instance.EnQueue(_typeName);
                }
                ///测试模式只插入一条
                if (DEBUGMODE)
                {
#pragma warning disable CS0162 // 检测到无法访问的代码
                    return;
#pragma warning restore CS0162 // 检测到无法访问的代码
                }
            }


            //  StartDBChangeProcessQuick();
            #endregion

            if (UrlQueueCount() <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
               // MessageBox.Show("无数据");

            }
        }
        /// <summary>
        /// 中转地址
        /// </summary>

        /// <summary>
        /// 初始化待转化的企业名称http://www.qichacha.com/company/{0}
        /// 通过名称获取企业Guid
        /// </summary>
        ///
        private void InitialEnterpriseGuidByKeyWordEnhence()
        {
            Settings.MaxReTryTimes = 1;

            var cityName = cityNameStr;


            var cityQuery = Query.EQ("cityName", cityName);

            var allExistEnterpriseList = enterpriseDataop.FindAll(DataTableKeyWordSearch).SetFields("guid", "key", "similar").ToList();

            foreach (var entObj in allExistEnterpriseList)
            {
                if (!existGuidList.Contains(entObj.Text("guid")))
                {
                    existGuidList.Add(entObj.Text("guid"));
                    //return;//测试只添加一次
                }
                if (!existNameList.Contains(entObj.Text("key")))
                {
                    existNameList.Add(entObj.Text("key"));
                }
            }




            //var tableName = "HuXiuProject"; //"ListedCompanyInformation companyName"; , "geographical"
            //var columnName = "地址";//
            //allEnterpriseList = enterpriseDataop.FindAllByQuery(tableName, Query.And(Query.Exists("eGuid", false), Query.Exists(columnName, true))).SetFields(columnName, "法定代表人").ToList();
            var tableName = "INNOTREE_AllInvestor"; //"ListedCompanyInformation companyName";
            var columnName = "fullName";//
            //tableName = "SiMu_Project"; //"ListedCompanyInformation companyName";
            //columnName = "epNeedCompanyName";//
            //var tableName = "UnicornCompany"; //"ListedCompanyInformation companyName";
            //var columnName = "name";//
            var allEnterpriseList = new List<BsonDocument>();
            allEnterpriseList = enterpriseDataop.FindAllByQuery(tableName, Query.And(Query.Exists(columnName, true), Query.Exists("eGuid", false))).SetFields(columnName).ToList();
            if (allEnterpriseList.Count() > 0)
            {

                foreach (var enterprise in allEnterpriseList.Where(c => !existNameList.Contains(c.Text(columnName))))
                {
                    //var companyName = enterprise.Text("companyName");
                    //var hitCompany = allExistEnterpriseList.Where(c => c.Text("key") == companyName && c.Text("similar") == "1").FirstOrDefault();
                    //if (hitCompany != null)
                    //{
                    //    DBChangeQueue.Instance.EnQueue(new StorageData() {
                    //        Document = new BsonDocument().Add("eGuid", hitCompany.Text("guid")),
                    //        Name = tableName,
                    //        Query = Query.EQ("companyName", companyName) ,  
                    //        Type = StorageType.Update});
                    //}
                    //continue;

                    var guidUrl = string.Format("https://www.qichacha.com/gongsi_getList?key={0}", enterprise.Text(columnName));
                    UrlQueue.Instance.EnQueue(new UrlInfo(guidUrl) { Depth = 1, PostData = string.Format("key={0}&type=0", enterprise.Text(columnName)), Authorization = tableName });

                }
            }
            else
            {
                ShowMessageInfo("无数据");
               // MessageBox.Show("无数据");
            }
            StartDBChangeProcessQuick();
        }
        /// <summary>
        /// 获取企业背后关系详细信息
        /// </summary>
        private void InitialEnterpriseGuidMoreDetailInfoAPP()
        {

            var takeCount = 1000;
            string keyName = "guid";
          
            QueryComplete query = Query.NE("isUpdate", 1);
            if (!int.TryParse(GRegistCapiBegin, out int capiBegin))
            {
                capiBegin = 0;
            }
            if (int.TryParse(GRegistCapiEnd, out int capiEnd))
            {

            }
            if (capiBegin != 0)
            {
                query = Query.And(query, Query.GTE("reg_capi", capiBegin));
            }
            if (capiEnd != 0)
            {
                query = Query.And(query, Query.LTE("reg_capi", capiEnd));
            }
            var curTableName = curEnterpriseKeySuffixTxt;
            if (string.IsNullOrEmpty(curTableName))
            {
                curTableName = DataTableLandHouseRelation;
            }
            else
            {
                curTableName=DataTableName;
            }
            string[] fields = new string[] { "eGuid", "guid" };
            var allEnterpriseList = enterpriseDataop.FindAllByQuery(curTableName, query).SetFields(fields);
            globalTotalCount= enterpriseDataop.FindCount(curTableName, query);
            Console.WriteLine("待处理个数:{0}", globalTotalCount);
            foreach (BsonDocument enterprise in allEnterpriseList)
            {

                    string key = enterprise.Text("eGuid");
                    if (string.IsNullOrEmpty(key))
                    {
                        key = enterprise.Text("guid");
                    }
                     
                    string backDetailInfoUrl = string.Format(domainUrl+"/app/v1/msg/getPossibleGenerateRelation?unique={0}&sign=&token=&timestamp=&from=h5", key);

                    if (!this.urlFilter.Contains(backDetailInfoUrl))
                    {
                        UrlInfo urlInfo = new UrlInfo(backDetailInfoUrl)
                        {
                            Depth = 1,
                            UniqueKey = key
                        };
                        if (this.splitLimitChk.Checked)
                        {
                            urlInfo.UrlSplitTimes = -3;
                        }
                        // UrlQueue.Instance.EnQueue(urlInfo);
                        AddUrlQueue(urlInfo);
                    }
                }
           
            if (this.UrlQueueCount() <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
               // MessageBox.Show("无数据");
            }
        }
        /// <summary>
        /// 初始化其他数据源载入用于获取企业信息
        /// </summary>
        private void InitialEnterpriseOtherSourceAPP()
        {
            Console.WriteLine("初始化数据");
            Settings.MaxReTryTimes = 100;
            if (!string.IsNullOrEmpty(this.MaxAccountCrawlerCountTxt.Text))
            {
                int crawlerCount = 0;
                if (int.TryParse(this.MaxAccountCrawlerCountTxt.Text, out crawlerCount))
                {
                    Settings.MaxAccountCrawlerCount = crawlerCount;
                }
            }
            List<BsonDocument> typeNameList = (from c in dataop.FindAll("ZS_yuanqu")
                                               where c.Int("count") <= 0
                                               orderby c.Text("provName")
                                               select c).ToList<BsonDocument>();
            int fetchKeyWorldCount = 0;
            int.TryParse(this.KeyWordFilterTextBox.Text.Trim(), out fetchKeyWorldCount);
            int skipCount = (fetchKeyWorldCount == 0) ? 0 : (typeNameList.Distinct<BsonDocument>().Count<BsonDocument>() - fetchKeyWorldCount);
            foreach (BsonDocument item in typeNameList.Distinct<BsonDocument>().Skip<BsonDocument>(skipCount))
            {
                string _typeName = item.Text("name").Replace("-", " ").Replace("(", "").Replace("（", "").Replace("）", "").Replace(")", "").Replace(".", "").Replace("。", "").Replace("\"", "").Replace("\"", "").Replace("?", "").Replace("-", "");
                string cityName = item.Text("cityName");
                string provName = item.Text("provName");
                if (_typeName.Contains("#"))
                {
                    _typeName = _typeName.Replace("&#8226;", "");
                }
                if (!_typeName.Contains("#"))
                {
                    if (_typeName.Contains(provName))
                    {
                        _typeName = _typeName.Replace(provName, "");
                    }
                    if (_typeName.Contains(cityName))
                    {
                        _typeName = _typeName.Replace(cityName, "");
                    }
                    if (string.IsNullOrEmpty(cityName) || cityName.Contains("全部"))
                    {
                        cityName = provName;
                    }
                    if (!this.singalKeyWordCHK.Checked)
                    {
                        var urlList = this.InitalQCCAppUrlByKeyWord(_typeName, item.Text("cityName"));
                        foreach (var url in urlList)
                        {
                            if (!this.urlFilter.Contains(url) && !string.IsNullOrEmpty(url))
                            {
                                UrlInfo urlInfo = new UrlInfo(url)
                                {
                                    Depth = 1,
                                    UniqueKey = item.Text("_id")
                                };
                                if (this.splitLimitChk.Checked)
                                {
                                    urlInfo.UrlSplitTimes = -3;
                                }
                                // UrlQueue.Instance.EnQueue(urlInfo);
                                AddUrlQueue(urlInfo);
                            }
                        }
                    }
                    else
                    {
                        string[] textArray1 = new string[] { _typeName, "|H|", cityName, "|H|", item.Text("_id") };
                        StringQueue.Instance.EnQueue(string.Concat(textArray1));
                    }
                }
            }
            if (this.UrlQueueCount() <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
                //MessageBox.Show("无数据");
            }
        }
        private void InitialEnterpriseInvent()
        {
            Console.WriteLine("初始化投资数据");
            Settings.MaxReTryTimes = 100;
            if (!string.IsNullOrEmpty(this.MaxAccountCrawlerCountTxt.Text))
            {
                int crawlerCount = 0;
                if (int.TryParse(this.MaxAccountCrawlerCountTxt.Text, out crawlerCount))
                {
                    Settings.MaxAccountCrawlerCount = crawlerCount;
                }
            }
            this.curModetailInfoTableName = DataTableName;
            string keyName = "guid";
            IMongoQuery[] clauses = new IMongoQuery[] { Query.NE("isInventUpdate", "4"), Query.Exists("guid", true) };
            string[] fields = new string[] { "guid" };
            List<BsonDocument> allEnterpriseList = enterpriseDataop.FindAllByQuery(this.curModetailInfoTableName, Query.And(clauses)).SetFields(fields).ToList<BsonDocument>();
            Console.WriteLine("待处理个数:{0}", allEnterpriseList.Count<BsonDocument>());
            if (allEnterpriseList.Count<BsonDocument>() > 0)
            {
                foreach (BsonDocument enterprise in allEnterpriseList)
                {
                    string key = enterprise.Text(keyName);
                    if (!string.IsNullOrEmpty(key))
                    {
                        string backDetailInfoUrl = string.Format("http://www.qichacha.com/cms_map?keyNo={0}", key);
                        if (!this.urlFilter.Contains(backDetailInfoUrl))
                        {
                            UrlInfo urlInfo = new UrlInfo(backDetailInfoUrl)
                            {
                                Depth = 1,
                                UniqueKey = key
                            };
                            if (this.splitLimitChk.Checked)
                            {
                                urlInfo.UrlSplitTimes = -3;
                            }
                            //  UrlQueue.Instance.EnQueue(urlInfo);
                            AddUrlQueue(urlInfo);
                        }
                    }
                }
            }
            if (this.UrlQueueCount() <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
               // MessageBox.Show("无数据");
            }
        }

        private int globalTotalCount = 0;
        /// <summary>
        /// 查看详情
        /// </summary>
        private void InitialEnterpriseDetailInfo_APP()
        {

            Settings.MaxReTryTimes = 100;

            var takeCount = 1000;
            string keyName = "guid";
            string keyName_bak = "eGuid";
            string[] fields = new string[] { keyName, keyName_bak };
            if (!int.TryParse(GRegistCapiBegin, out int capiBegin))
            {
                capiBegin = 0;
            }
            if (int.TryParse(GRegistCapiEnd, out int capiEnd))
            {
               
            }
            var query = Query.Or(Query.Exists("isDetailUpdate", false),Query.LTE("isDetailUpdate", 0));

            //是否过滤注销
            if (ignoreCancelChk.Checked) { 
             query = Query.And(query,Query.NE("status", "吊销"), Query.NE("status", "注销"));
            }
            if (capiBegin != 0)
            {
                query = Query.And(query, Query.GTE("reg_capi", capiBegin));
            }
            if (capiEnd != 0)
            {
                query = Query.And(query, Query.LTE("reg_capi", capiEnd));
            }
            ///优先爬取注册金额大于500w的企业详情
           // globalTotalCount = (int)_enterpriseMongoDBOp.FindCount(DataTableName, query);
            var skipCount = 0;
            //var rnd = new Random();
            //if (allCount <= 1000)
            //{
            //    skipCount = rnd.Next(allCount);
            //}
            //创建对应索引
            try
            {
                //CreateSignalIndex(_enterpriseMongoDBOp, DataTableName, "reg_capi");
                //CreateSignalIndex(_enterpriseMongoDBOp, DataTableName, "status");
                //CreateSignalIndex(_enterpriseMongoDBOp, DataTableName, "isDetailUpdate");
            }
            catch (Exception ex)
            {
                ShowMessageInfo(ex.Message);
            }
           
            var allEnterpriseList = _enterpriseMongoDBOp.FindAll(DataTableName, query).SetLimit(takeCount).SetFields(fields);
            //Console.WriteLine("待处理个数:{0}", allEnterpriseList.Count());
          
                foreach (BsonDocument enterprise in allEnterpriseList)
                {
                    string key = enterprise.Text(keyName);
                    if (string.IsNullOrEmpty(key))
                    {
                        key= enterprise.Text(keyName_bak); 
                    }
                    if (!string.IsNullOrEmpty(key))
                    {
                        string backDetailInfoUrl = $"https://{ConstParam.qUrl}/app/v1/base/getMoreEntInfo?unique={key}&sign=&token=&timestamp=&from=h5";
                        //string backDetailInfoUrl=string.Format("getMoreEntInfo")
                        if (!this.urlFilter.Contains(backDetailInfoUrl))
                        {

                            UrlInfo urlInfo = new UrlInfo(backDetailInfoUrl)
                            {
                                Depth = 1,
                                UniqueKey = key
                            };
                            if (this.splitLimitChk.Checked)
                            {
                                urlInfo.UrlSplitTimes = -3;
                            }
                            //  UrlQueue.Instance.EnQueue(urlInfo);
                            AddUrlQueue(urlInfo);
                        }
                    }
                }
         
            if (this.UrlQueueCount() <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
                //MessageBox.Show("无数据");
            }
        }


        /// <summary>
        /// 查看岗位详情
        /// </summary>
        private void InitialEnterprisePositionDetailByGuid_APP()
        {

            Settings.MaxReTryTimes = 100;
            var takeCount = 1000000;
            string keyName = "guid";
            string[] fields = new string[] { keyName,"name", "status", "reg_capi_desc" };
            if (!int.TryParse(GRegistCapiBegin, out int capiBegin))
            {
                capiBegin = 0;
            }
            if (int.TryParse(GRegistCapiEnd, out int capiEnd))
            {

            }
           // var query = Query.And(Query.EQ("cityName","合肥"),Query.Exists("isJobDetailUpdate", false));
            var query = Query.And(Query.EQ("cityName", "合肥"));
            // query = Query.And(query, Query.NE("status", "吊销"), Query.NE("status", "注销"));
            //if (capiBegin != 0)
            //{
            //    query = Query.And(query, Query.GTE("reg_capi", capiBegin));
            //}
            //if (capiEnd != 0)
            //{
            //    query = Query.And(query, Query.LTE("reg_capi", capiEnd));
            //}
            ///优先爬取注册金额大于500w的企业详情
            globalTotalCount = enterpriseDataop.FindCount(DataTableName, query);
            var skipCount = 0;
            //var rnd = new Random();
            //if (allCount <= 1000)
            //{
            //    skipCount = rnd.Next(allCount);
            //}
            //创建对应索引
            try
            {
                //CreateSignalIndex(_enterpriseMongoDBOp, DataTableName, "reg_capi");
                //CreateSignalIndex(_enterpriseMongoDBOp, DataTableName, "status");
                //CreateSignalIndex(_enterpriseMongoDBOp, DataTableName, "isDetailUpdate");
            }
            catch (Exception ex)
            {
                ShowMessageInfo(ex.Message);
            }
            var allEnterpriseList = enterpriseDataop.FindLimitByQuery(DataTableName, query, new SortByDocument() { { "_id", 1 } }, skipCount, takeCount).SetFields(fields);
            //Console.WriteLine("待处理个数:{0}", allEnterpriseList.Count());

            foreach (BsonDocument enterprise in allEnterpriseList)
            {
                var curMoney = enterprise.Text("reg_capi_desc").ToMoney();
                if (capiBegin != 0 || capiEnd != 0)
                {
                    if (capiBegin!=0&&curMoney < capiBegin )
                    {
                        continue;
                    }
                    if (capiEnd!=0&&curMoney > capiEnd)
                    {
                        continue;
                    }
                }
                if (enterprise.Text("status").Contains("销"))
                {
                    continue;
                }
                string key = enterprise.Text(keyName);
                var name = enterprise.Text("name");
                name = HttpUtility.UrlEncode(name);
                if (!string.IsNullOrEmpty(key))
                {
                    string backDetailInfoUrl = $"{domainUrl}/app/v1/other/getJobs?searchKey={name}&sign=&token=&timestamp=&from=h5&pageIndex=1";
                    if (!this.urlFilter.Contains(backDetailInfoUrl))
                    {
                        UrlInfo urlInfo = new UrlInfo(backDetailInfoUrl)
                        {
                            Depth = 1,
                            UniqueKey = key
                        };
                        if (this.splitLimitChk.Checked)
                        {
                            urlInfo.UrlSplitTimes = -3;
                        }
                        //  UrlQueue.Instance.EnQueue(urlInfo);
                        AddUrlQueue(urlInfo);
                    }
                }
            }

            if (this.UrlQueueCount() <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
                //MessageBox.Show("无数据");
            }
        }

        /// <summary>
        /// 爬取新增的企业；
        /// </summary>
        private void InitialADDEnterpriseAPP()
        {
            Console.WriteLine("初始化投资数据");
            Settings.MaxReTryTimes = 100;
            //获取省份
            var allProvinceList = oldCityList.Where(c => c.Int("type") == 0).Select(c => c.Text("code")).ToList();
            if (onlySpecialCityChk.Checked)
            {
                allProvinceList = allProvinceList.Where(c => specialProvince.Contains(c)).ToList();
            }

           
            foreach (var province in allProvinceList)//2016
            {
                if (string.IsNullOrEmpty(province)) continue;
                string backDetailInfoUrl = $"https://{ConstParam.qUrlV3}/app/v1/base/getNewCompanys?province=&cityCode=&pageIndex=1&timestamp=&sign=&platform=other&app_channel=qq";
                var curInitUrl = this.ReplaceUrlParamEx(backDetailInfoUrl, "province", province, "?");
                var curCityList = GetSubCountryCodeList(province, "", "1");//当前省对象
                if (curCityList.Count() > 0)
                {
                    foreach (var countryCode in curCityList)//2016
                    {
                        var curUrl = this.ReplaceParam(curInitUrl, "cityCode", "", countryCode);
                        AddUrlQueue(curUrl);
                    }
                }
                else
                {
                    AddUrlQueue(curInitUrl);
                }
            }
            if (this.UrlQueueCount() <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
                //MessageBox.Show("无数据");
            }
        }
        #region 不常用
        private void InitialEnterpriseData()
        {
            initCurCity();

            if (UrlQueueCount() > 0) return;
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
                case SearchType.EnterpriseInvent:
                    this.InitialEnterpriseInvent();
                    return;
                case SearchType.ADDEnterpriseAPP:
                    this.InitialADDEnterpriseAPP();
                    return;
                case SearchType.UpdateEnterpriseInfoAPP:
                    this.InitialEnterpriseDetailInfo_APP();
                    break;
                case SearchType.EnterprisePositionDetailByGuid_APP:
                    this.InitialEnterprisePositionDetailByGuid_APP();
                    break;
                case SearchType.EnterpriseBackGroundDetailByGuid_APP:
                    this.InitialEnterpriseGuidMoreDetailInfoAPP();
                    break;
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
                        //UrlQueue.Instance.EnQueue(new UrlInfo(otherInfoUrl) { Depth = 1 });
                        AddUrlQueue(new UrlInfo(otherInfoUrl) { Depth = 1 });
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
                // UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });
                AddUrlQueue(new UrlInfo(url) { Depth = 1 });
            }
            if (UrlQueueCount() <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
               // MessageBox.Show("无数据");

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
            var curProvince = oldCityList.Where(c => c.Text("code") == curCity.Text("provinceCode")).FirstOrDefault();
            var typeList = dataop.FindAll(DataTableIndustry).ToList();//遍历所有的父分类与子分类
            if (NEEDRECORDURL)
            {
#pragma warning disable CS0162 // 检测到无法访问的代码
                var allHitUrlList = dataop.FindAllByQuery(DataTableNameKeyWordURL, Query.And(Query.EQ("province", curCity.Text("provinceCode")), Query.EQ("cityCode", curCity.Text("code")))).ToList();//获取执行过的url
#pragma warning restore CS0162 // 检测到无法访问的代码
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
            if (UrlQueueCount() <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
                //MessageBox.Show("无数据");

            }
        }

        private void InitialEnterpriseGuidByKeyWordAPP()
        {
            if (StringQueue.Instance.Count > 0)
            {
                return;
            }
            Console.WriteLine("初始化数据");
            Settings.MaxReTryTimes = 100;//尝试最大个数
            if (!string.IsNullOrEmpty(this.MaxAccountCrawlerCountTxt.Text))
            {
                int crawlerCount = 0;
                if (int.TryParse(this.MaxAccountCrawlerCountTxt.Text, out crawlerCount))
                {//最大化利用账号
                    Settings.MaxAccountCrawlerCount = crawlerCount;
                }
            }

            if (curCity == null)
            {
                ShowMessageInfo("请先初始化城市");
                return;
            }
            var curProvince = oldCityList.Where(c => c.Text("code") == curCity.Text("provinceCode")).FirstOrDefault();

            //那些超出1000的url 和第一页需要重新进行读取一次，而且分页内容可过滤
            //app只能查找第一页                                                                                                                                                                                      // foreach (var hitUrl in allHitUrlList.Where(c=>c.Int("isSplit")!=1&& c.Int("isFirstPage") != 1))
            //foreach (var hitUrl in allHitUrlList.Where(c => c.Int("isSplit") != 1 && c.Int("recordCount") <= 40))
            //{

            //    if (!urlFilter.Contains(hitUrl.Text("url")))
            //    {
            //        urlFilter.Add(hitUrl.Text("url"));
            //    }

            //}


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

            ///是否限制split次数
            if (this.splitLimitChk.Checked == true)
            {
                typeNameList = dataop.FindFieldsByQuery(DataTableEnterriseKeyWord, null, new string[] { "keyWord", "count" }).Where(c => c.Int("count") > 0).OrderByDescending(c => c.Int("count")).Select(c => c.Text("keyWord")).ToList();
            }
            else
            {
                var cityNameQueryList = new List<string>() { "南昌", "沈阳", "大连", "上海", "深圳", "广州" };
                // typeNameList = dataop.FindFieldsByQuery(DataTableEnterriseKeyWordCount, Query.EQ("cityName", "广州"), new string[] { "keyWord", "count" }).Where(c => c.Int("count") > 20).OrderByDescending(c => c.Int("count")).Select(c => c.Text("keyWord")).ToList();
                typeNameList = dataop.FindFieldsByQuery(DataTableEnterriseKeyWordCount, Query.In("cityName", cityNameQueryList.Select(c => (BsonValue)c)), new string[] { "keyWord", "count" }).Where(c => c.Int("count") > 100).OrderByDescending(c => c.Int("count")).Select(c => c.Text("keyWord")).Distinct().ToList();
            }
            InitKeyWordHitCount(cityNameStr);
            // typeNameList = new List<string>() { "建筑", "餐饮", "服务", "代理", "服装", "化工", "电力", "木材", "广告", "项目咨询","装饰装修" }; 
            if (keyWordSourceCHK.Checked == true)
            { //是否使用关键字数据源
                typeNameList = typeNameList.Take(10).ToList();
                var tempTypeNameList = dataop.FindAll(DataTableScopeKeyWord).Select(c => c.Text("keyWord")).ToList();

                typeNameList.AddRange(tempTypeNameList);
            }
            // typeNameList.Add("母婴");
            var fetchKeyWorldCount = 0;
            int.TryParse(KeyWordFilterTextBox.Text.Trim(), out fetchKeyWorldCount);
            var skipCount = fetchKeyWorldCount == 0 ? 0 : typeNameList.Distinct().Count() - fetchKeyWorldCount;
            foreach (var _typeName in typeNameList.Distinct().Skip(skipCount))
            {
                if (!this.singalKeyWordCHK.Checked)//是否单个单个运行
                {
                    //按时间逆序
                     
                    var urlList = InitalQCCAppUrlByKeyWord(_typeName);
                    foreach (var url in urlList)
                    {
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
                }
                else
                {
                    StringQueue.Instance.EnQueue(_typeName);
                }
            }

            if (UrlQueueCount() <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
                //MessageBox.Show("无数据");

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
            if (UrlQueueCount() <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
               // MessageBox.Show("无数据");

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


#pragma warning disable CS0219 // 变量“pageCount”已被赋值，但从未使用过它的值
            var pageCount = 500;
#pragma warning restore CS0219 // 变量“pageCount”已被赋值，但从未使用过它的值
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


            if (UrlQueueCount() <= 0)
            {
                this.richTextBox.AppendText("无查找到数据");
               // MessageBox.Show("无数据");

            }
        }

        /// <summary>
        /// 生成自定义关键字
        /// </summary>
        /// <returns></returns>
        private List<BsonDocument> InitialCustomerKeyWord()
        {
            //蔬菜关键字
            globalKeyWordList = new List<BsonDocument>();
            var keyWordTableName = "DailyGeneralCategory";
            var rootName = SearchCustomCategoryName;//设定中输入的水果蔬菜
            var hitDailGeneralCatList = dataop.FindAll(keyWordTableName).ToList();
            var rootObj = hitDailGeneralCatList.Where(c => c.Text("name") == rootName).FirstOrDefault();
            var rootComboKeyWords = rootObj.ContainsColumn("comboKeyWord")? rootObj["comboKeyWord"] as BsonArray:new BsonArray();
            var leafList = hitDailGeneralCatList.Where(c => c.Text("rootGuid") == rootObj.Text("guid") && c.Int("level") == 2).ToList();
            foreach (var leaf in leafList)
            {
                var industryCode = leaf.Text("industryCode");//当前关键字对应行业
                var parentObj = hitDailGeneralCatList.Where(c => c.Text("guid") == leaf.Text("parentGuid")).FirstOrDefault();
                if (string.IsNullOrEmpty(industryCode)&&parentObj != null)
                {
                    industryCode = parentObj.Text("industryCode");
                }
                if (string.IsNullOrEmpty(industryCode) && rootObj != null)
                {
                    industryCode = rootObj.Text("industryCode");
                }
                var comboKeyWords = leaf.ContainsColumn("comboKeyWord") ? leaf["comboKeyWord"] as BsonArray : new BsonArray();
                if (comboKeyWords.Count <= 0)
                {
                    //父级
                   
                    if (parentObj != null)
                    {
                        comboKeyWords = parentObj.ContainsColumn("comboKeyWord") ? parentObj["comboKeyWord"] as BsonArray ?? new BsonArray(): new BsonArray();
                        if (comboKeyWords.Count <= 0)
                        {
                            comboKeyWords = rootComboKeyWords;
                        }
                    }
                }
                if (comboKeyWords.Count <= 0) { comboKeyWords.Add(""); }
                foreach (var combokeyWord in comboKeyWords)
                {
                    var keyWord = $"{leaf.Text("name")}{combokeyWord.ToString()}";
                    var keyWrodDoc = new BsonDocument();
                    keyWrodDoc.Set("keyWord", keyWord);
                    keyWrodDoc.Set("keyWordGuid", leaf.Text("guid"));
                    keyWrodDoc.Set("industryCode", industryCode);
                    globalKeyWordList.Add(keyWrodDoc);
                }
             }
            return globalKeyWordList;
        }

        /// <summary>
        /// 获取keyWordGuid
        /// </summary>
        /// <param name="keyWord"></param>
        /// <returns></returns>
        private BsonDocument GetKeyWordGuid(string keyWord)
        {
            try
            {
                if (globalKeyWordList.Count() <= 0)
                {
                    return new BsonDocument();
                }
                var hitKeyWordGuid = globalKeyWordList.Where(c => c.Text("keyWord") == keyWord).FirstOrDefault();
                if (hitKeyWordGuid != null)
                {
                    return hitKeyWordGuid;
                }
            }
            catch (Exception ex)
            {
                ShowMessageInfo(ex.Message+ "GetKeyWordGuid");
            }
            return new BsonDocument();
        }
        /// <summary>
        /// 创建索引
        /// </summary>
        /// <param name="op"></param>
        /// <param name="tableName"></param>
        /// <param name="field"></param>
        /// <param name="order"></param>
        private void CreateSignalIndex(MongoOperation op, string tableName,string field,int order=1)
        {
          
            IMongoIndexKeys keys = new IndexKeysDocument { { field, order } };
            IMongoIndexOptions options = IndexOptions.SetUnique(false).SetBackground(true);
            op.CreateIndex(DataTableName, keys, options);

        }
        #endregion
        #endregion
        #region 数据处理
        /// <summary>
        /// 通过关键字+分类获取企业对应guid（主要）
        /// </summary>
        /// <param name="args"></param>
        public void DataReceiveEnterpriseGuidByKeyWord_APP(DataReceivedEventArgs args)
        {

             
            var cityName = string.Empty;
            if (!IsProvince)
            {

                cityName = cityNameStr;
            }

            var hmtl = args.Html;
            if (!args.Html.Contains("成功"))
            {
                args.urlInfo.Depth = args.Depth + 1;
                UrlQueue.Instance.EnQueue(args.urlInfo);

            }
            JObject jsonObj = null;
            JToken resultObj = null;
            JToken PagingObj = null;
            JToken itemObj = null;
            JToken message = null;
            try
            {
                jsonObj = JObject.Parse(hmtl);
                if (jsonObj == null) return;
                message = jsonObj["message"];
                if (message != null && !message.ToString().Contains("成功"))
                {
                    ShowMessageInfo("获取失败" + hmtl);
                    return;
                }

                resultObj = jsonObj["result"];
                if (resultObj == null) return;
                PagingObj = resultObj["Paging"];
                if (PagingObj == null) return;

                itemObj = resultObj["GroupItems"];
                if (itemObj == null)
                {
                    JObject nullJsonObj = JObject.Parse("{}");
                    itemObj = nullJsonObj.Root;
                    //return;
                }
            }
            catch (Exception ex)
            {
                ShowMessageInfo("jsonParse" + ex.Message);
            }

            var firstRecordCount = 0;
            if (!int.TryParse(PagingObj["TotalRecords"].ToString(), out firstRecordCount))
            {
                return;
            }
            if (firstRecordCount == 0 && args.Depth <= Settings.MaxReTryTimes)//多尝试防止有时候出现为0情况
            {
                //UrlRetryQueue.Instance.EnQueue(new UrlInfo(args.Url) { Depth = args.Depth + Settings.MaxReTryTimes / 2+1 });
            }

            var industryCode = GetUrlParam(args.Url, "industryCode");
            var subIndustryCode = GetUrlParam(args.Url, "subIndustryCode");
            var industryV3 = GetUrlParam(args.Url, "industryV3");
            var province = GetUrlParam(args.Url, "province");
            var cityCode = GetUrlParam(args.Url, "cityCode");
            var countyCode = GetUrlParam(args.Url, "countyCode");//城市代码
            var  keyWordGuid= GetUrlParam(args.Url, "k");//关键字guid
            var searchKey = GetUrlParam(args.Url, "searchKey");
            var keyWordStr = searchKey;
            if (searchKey.Contains("scope"))
            {
                keyWordStr = Toolslib.Str.Sub(searchKey, "\"scope\":\"", "\"");
            }
            string urlUniqueKey = GetUrlParam(args.Url, "uniqueKey");
            var urlBson = new BsonDocument();
            urlBson.Add("industryCode", industryCode);
            urlBson.Add("subIndustryCode", subIndustryCode);
            urlBson.Add("province", province);
            urlBson.Add("cityCode", cityCode);
            urlBson.Add("recordCount", firstRecordCount.ToString());
            urlBson.Add("url", args.Url);
            urlBson.Add("isApp", "1");

            #region cityCode 条件cityCode过滤


            ////cityName重置
            var curCityObj = oldCityList.Where(c => c.Text("code") == cityCode && c.Text("provinceCode") == province).FirstOrDefault();
            if (curCityObj == null && specialProvince.Contains(province))
            {
                curCityObj = oldCityList.Where(c => c.Text("code") == province).FirstOrDefault();
            }
            if (curCityObj != null)
            {
                cityName = curCityObj.Text("name");
            }


            #endregion

            //var hasExistUrl = ExistUrl(args.Url);
#pragma warning disable CS0219 // 变量“hasExistUrl”已被赋值，但从未使用过它的值
            var hasExistUrl = false;
#pragma warning restore CS0219 // 变量“hasExistUrl”已被赋值，但从未使用过它的值

            if (args.urlInfo == null || args.urlInfo.UrlSplitTimes != -1)//2017.6.13 新增url拆分限制次数，为-1时候不进行拆分
            {
                try
                {
                    if (!IsQCCUrlNewVersion()&& SearchTakeCountLimitPerUrl <= 0)
                    {
                        if (IsProvince && string.IsNullOrEmpty(province) && IsProvinceUrlSplited_JsonData(args, province, "", itemObj, firstRecordCount))
                        {
                            ShowMessageInfo($"旧版本全国省份不应进行后续操作添加:{UrlQueueCount()}");
                            return;
                        }
                    }
                    
                    if (firstRecordCount > 40&& SearchTakeCountLimitPerUrl <= 0 && IsKeyWordUrlSplited_APP(args, firstRecordCount, itemObj))
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
                        ShowMessageInfo(string.Format("当前记录:{0}过大进行分支处理剩余url:{1} retryUrl:{2}", firstRecordCount, UrlQueue.Instance.Count, UrlRetryQueue.Instance.Count));
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
                if (args.urlInfo.UrlSplitTimes < -1)
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
                        url = string.Format("{0}&p={1}", args.Url, _index);
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
            var curUrlDomain = string.Empty;
            var subIndustryObj = allSubFactoryList.Where(c => c.Text("code") == industryV3).FirstOrDefault();
            if (subIndustryObj != null)
            {
                curUrlDomain = subIndustryObj.Text("name");
            }
            #region 遍历处理list
            var resultDataList = resultObj["Result"].Take(100000);
            if (SearchTakeCountLimitPerUrl >0)
            {
                resultDataList = resultDataList.Take(SearchTakeCountLimitPerUrl);
            }
            foreach (var enterpriseObj in resultDataList)
            {

                var curUpdateBson = new BsonDocument();
                var guid = enterpriseObj["KeyNo"].ToString();
                var name = FixDocStr(enterpriseObj["Name"].ToString());
                var domain = string.Empty;
                ///此处需要判断AreaCode是否存在
                var areaCode = enterpriseObj["AreaCode"] != null ? enterpriseObj["AreaCode"].ToString() : string.Empty;//4290\t429004;
                                                                                                                       //空地址补全
                if (string.IsNullOrEmpty(areaCode))
                {
                    areaCode = GetCountryCodeByAddress(province, cityName, enterpriseObj);
                    if (string.IsNullOrEmpty(areaCode))
                    {
                        ShowMessageInfo("无法获取AreaCode", false);
                    }
                    else
                    {
                        curUpdateBson.Set("AreaCode", areaCode);
                    }
                }
                var curCityName = cityName;
                var regionName = string.Empty;
                ///根据areaCode进行所在城市获取
                if (!string.IsNullOrEmpty(areaCode))
                {
                    var areaCodeArr = areaCode.Split(new string[] { "\t", "" }, StringSplitOptions.RemoveEmptyEntries);
                    if (areaCodeArr.Length >= 2)
                    {
                        var curCityCode = allCountyCodeList.Where(c => c.Text("code") == areaCodeArr[0]).FirstOrDefault();
                        if (curCityCode != null)
                        {
                            curCityName = curCityCode.Text("name");
                        }
                        var regionCode = allCountyCodeList.Where(c => c.Text("code") == areaCodeArr[1]).FirstOrDefault();
                        if (regionCode != null)
                        {
                            regionName = regionCode.Text("name");
                            if (curCityCode == null)
                            {
                                curCityCode = allCountyCodeList.Where(c => c.Text("code") == regionCode.Text("cityCode")).FirstOrDefault();
                                if (curCityCode != null)
                                {
                                    curCityName = curCityCode.Text("name");
                                }
                            }
                        }

                    }
                }
                if (string.IsNullOrEmpty(curCityName) && !string.IsNullOrEmpty(cityName))
                {
                    curCityName = cityName;
                }
                if (!string.IsNullOrEmpty(curUrlDomain))
                {
                    domain = curUrlDomain;
                }
                if (string.IsNullOrEmpty(domain))
                {
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
                        //2019.7.19 后数据接口返回的数据缺少行业范围字段
                        if (string.IsNullOrEmpty(domain))
                        {
                            var hitReason = enterpriseObj["HitReason"];
                            if (hitReason != null && hitReason["Field"] != null)
                            {
                                if (hitReason["Field"].ToString() == "经营范围")
                                {
                                    domain = this.FixDocStr(hitReason["Value"].ToString());
                                }
                            }
                        }
                    }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                    catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                    {
                        if (enterpriseObj["Scope"] != null)
                        {
                            domain = this.FixDocStr(enterpriseObj["Scope"].ToString());
                        }

                    }
                }
                message = string.Format("详细信息{0}_{1}获取成功剩余url{2} retryUrl:{3}\r", guid, name, UrlQueue.Instance.Count, UrlRetryQueue.Instance.Count);
                //curUpdateBson.Set("domain", domain);

                curUpdateBson.Set("domain", FixDocStr(domain));
                if (curCityName.EndsWith("市"))
                {
                    curCityName = curCityName.TrimEnd('市');
                }
                //设定所属城市
                curUpdateBson.Set("cityName", curCityName);
                if (!string.IsNullOrEmpty(regionName))
                {
                    curUpdateBson.Set("regionName", regionName);
                }
                curUpdateBson.Add("industryCode", industryCode);
                curUpdateBson.Add("subIndustryCode", subIndustryCode);

              
                //foreach (var keyMap in EnterpriseInfoMapDic_App)//遍历字段
                //{

                //    if (enterpriseObj[keyMap.Key] != null)
                //    {
                //        curUpdateBson.Set(keyMap.Value, enterpriseObj[keyMap.Key].ToString());
                //    }
                //}


                var entDetail = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(enterpriseObj.ToString());

               
                //增加其他字段
                foreach (var item in entDetail.Elements)
                {
                    if (EnterpriseInfoMapDic_App.ContainsKey(item.Name))
                    {
                        curUpdateBson.Set(EnterpriseInfoMapDic_App[item.Name], this.FixDocStr(entDetail.Text(item.Name)));
                    }
                    else
                    {
                        curUpdateBson.Set(item.Name, this.FixDocStr(enterpriseObj[item.Name].ToString()));
                    }// 其他字段
                }

                // curUpdateBson.Set("Industry", "");
                curUpdateBson.Set("tags", "");
                string uniqueKey = args.urlInfo.UniqueKey;
                if (!string.IsNullOrEmpty(uniqueKey))
                {
                    curUpdateBson.Set("uniqueKey", uniqueKey);
                }
                if (!string.IsNullOrEmpty(urlUniqueKey))
                {
                    curUpdateBson.Set("urlUniqueKey", urlUniqueKey);
                }
                if (curUpdateBson.Count<BsonElement>() > 0)
                {
                    curUpdateBson.Set("isUserUpdate", "1");
                }
                curUpdateBson.Set("isNewInsert", 1);//2020.4.14后新增d


                var reg_capi_desc = curUpdateBson.Text("reg_capi_desc");
                var money = reg_capi_desc.ToMoney();
                curUpdateBson.Set("reg_capi", money);//2020.4.30增加注册金额
                ///匹配的关键字
                if (PreKeyWordList.Count() > 0)
                {
                    curUpdateBson.Set("hitKeyWord", keyWordStr); 
                }
                
                if (existGuidList.Contains(guid))
                {
                   // existCount++;
                }
                else
                {//代表还未添加
                    existGuidList.Add(guid);
                    if (!string.IsNullOrEmpty(guid) && !ExistGuid(guid))
                    {
                       
                        Interlocked.Increment(ref AllAddCount);
                        addCount++;
                        SB.AppendFormat("获得对象{0}:{1}\r", guid, name);
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Insert });
                    }
                    else
                    {
                        //代表存在也需要更新数据,一次全库搜索只更新一次
                        if (existedUpdateChk.Checked == true)
                        {
                            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Query = Query.EQ("guid", guid), Type = StorageType.Update });
                        }
                        existCount++;
                        //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Query = Query.EQ("guid", guid), Type = StorageType.Update });
                        existGuidList.Add(guid);

                    }
                }
                BuildKeyWordEntRelate(keyWordGuid, guid, curUpdateBson);//建立关键字与企业关联

            }
            #endregion
          
            Settings.SearchKeyWord = keyWordStr;
            Settings.KeyWordAddCount += addCount;
            var curKeyWordCount = SetKeyWordHitCount(keyWordStr, addCount, existCount);

            // this.ShowMessageInfo(string.Format("总添加：{9} 城市:{10}是否建议跳过关键字：{7}_关键字已经添加:{6}| 当前：{5}添加:{0} 已存在:{1}剩余url{4} retryUrl:{8} 详细:{2}当前url:{3}", new object[] { addCount, existCount, SB.ToString(), HttpUtility.UrlDecode(queryStr), UrlQueue.Instance.Count, firstRecordCount.ToString(), curKeyWordCount, this.needPassKeyWord, UrlRetryQueue.Instance.Count, this.AllAddCount, cityName }), false);
            this.ShowMessageInfo($"总添加：{this.AllAddCount} 城市:{cityName}是否建议跳过关键字_{Settings.SearchKeyWord}：{this.needPassKeyWord}_关键字已经添加:{curKeyWordCount}| 当前：{firstRecordCount.ToString()}添加:{addCount} 已存在:{existCount}剩余url{UrlQueue.Instance.Count} retryUrl:{UrlRetryQueue.Instance.Count} 详细:{SB.ToString()}当前url:{HttpUtility.UrlDecode(queryStr)}", false);
            curKeyWordStr = keyWordStr;
            //拆分完后才进行判断
            // ProcessCanAutoPassKeyWord(curKeyWordCount, keyWordStr);

        }

        /// <summary>
        /// 背后关系展示
        /// </summary>
        /// <param name="args"></param>
        public void DataReceiveDetailInfo_BackGround_APP(SimpleCrawler.DataReceivedEventArgs args)
        {
            string guid = args.urlInfo.UniqueKey;
            string keyName = "guid";
            string keyName_bak = "eGuid";
            if (enterprisePort == 37888 && globalDBHasPassWord == false)
            {
                keyName = keyName_bak;
            }
            if (string.IsNullOrEmpty(guid))
            {
                this.ShowMessageInfo("传入Key为空", false);
            }
            var curTableName = curEnterpriseKeySuffixTxt;
            if (string.IsNullOrEmpty(curTableName))
            {
                curTableName = DataTableLandHouseRelation;
            }
            else
            {
                curTableName = DataTableName;
            }
            JObject jsonObj = JObject.Parse(args.Html);
            if (jsonObj != null)
            {
                try
                {
                    var result = jsonObj["result"];
                    if (result != null)
                    {
                        var updateBsonDoc = new BsonDocument();
                         
                        var bgInfoDoc = result.ToString().GetBsonDocFromJson();
                        ///推送消息队列
                        if (mqPushChk.Checked)
                        {
                            bgInfoDoc.Set("guid", guid);
                            Task.Run(async () => { await PushMessageAsync(bgInfoDoc); });
                        }
                        updateBsonDoc.Set("isUpdate", 1);
                        _enterpriseMongoDBOp.UpdateOrInsert(curTableName, Query.EQ(keyName, guid), updateBsonDoc);
                        // DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBsonDoc, Name = DataTableLandHouseRelation, Query = Query.EQ(keyName, guid), Type = StorageType.Update });
                        this.ShowMessageInfo($"{Settings.DeviceId}_{Settings.AccounInfo}总个数：{globalTotalCount}增：{AllAddCount} {guid}剩余url{UrlQueue.Instance.Count} retryUrl:{UrlRetryQueue.Instance.Count} 当前url:{HttpUtility.UrlDecode(args.Url)}", false);
                    }
                }
                catch (Exception ex)
                {
                    ShowMessageInfo(ex.Message);
                }
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
                ShowMessageInfo(UrlQueueCount().ToString() + args.urlInfo.PostData);
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
                    if (!string.IsNullOrEmpty(args.urlInfo.Authorization))
                    {
                        curUpdateBson.Add("type", args.urlInfo.Authorization);//匹配对应表
                    }
                    var guid = enterpriseObj["KeyNo"].ToString();
                    var name = enterpriseObj["Name"].ToString();
                    var reason = enterpriseObj["Reason"].ToString();
                    var operName = enterpriseObj["OperName"].ToString();
                    var message = string.Format("详细信息{0}_{1}获取成功剩余url{2}{3}\r", guid, name, UrlQueueCount(), args.Url);

                    if (!string.IsNullOrEmpty(guid))
                    {
                        curUpdateBson.Add("isLandEnterprise", "1");//匹配
                        if (key == name)
                        {
                            curUpdateBson.Add("similar", index.ToString());//第一个匹配
                        }
                        curUpdateBson.Add("key", key);//匹配
                        curUpdateBson.Set("guid", guid);
                        curUpdateBson.Set("name", name);
                        curUpdateBson.Set("reason", reason);
                        curUpdateBson.Set("operName", operName);
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableKeyWordSearch, Type = StorageType.Insert });
                        existGuidList.Add(guid);
                    }
                    else
                    {
                        //var updateBosn = new BsonDocument().Add("isLandEnterprise", "1");//地块信息
                        //var hitDistinct = cityList.Where(c => name.Contains(c.Text("name").Replace("市", "").Replace("省", ""))).ToList();
                        //var hitProvince = hitDistinct.Where(c => c.Int("type") == 0).FirstOrDefault();
                        //var provinceName = hitProvince != null ? hitProvince.Text("name") : "";
                        //var hitCityName = hitDistinct.Where(c => c.Int("type") == 1).FirstOrDefault();
                        //if (hitCityName != null)
                        //{
                        //    var tempCityName = hitCityName.Text("name").Replace("市", "");
                        //    if (cityName != tempCityName)
                        //    {
                        //        updateBosn.Add("cityName", tempCityName);
                        //    }
                        //}
                        //else
                        //{
                        //    if (provinceName == "北京" || provinceName == "上海")
                        //    {
                        //        updateBosn.Add("cityName", provinceName);
                        //    }
                        //    else
                        //    {
                        //        updateBosn.Add("provinceName", provinceName);
                        //    }
                        //}
                        //if (index == 1)
                        //    updateBosn.Add("similar", index.ToString());//匹配
                        //updateBosn.Add("key", key);//匹配
                        // DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBosn, Query = Query.EQ("guid", guid), Name = DataTableKeyWordSearch, Type = StorageType.Update });
                        //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("cityName", cityName),Query=Query.EQ("guid", guid), Name = DataTableName, Type = StorageType.Update });
                        message += "exists";
                    }
                    // DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isSearched", "1"), Query = Query.EQ("name", key), Name = "QCCEnterprise_XiAn", Type = StorageType.Update });
                    ShowMessageInfo(message);
                    index++;
                }
            }
            catch (Exception ex)
            {
                ShowMessageInfo(ex.Message);
            }

        }

        public void DataReceiveInitialEnterpriseInvent(SimpleCrawler.DataReceivedEventArgs args)
        {
            string guid = args.urlInfo.UniqueKey;
            if (string.IsNullOrEmpty(guid))
            {
                this.ShowMessageInfo("传入Key为空", false);
            }
            JObject jsonObj = JObject.Parse(args.Html);
            if (jsonObj != null)
            {
                string message = string.Empty;
                JToken resultObj = jsonObj["Result"];
                if ((resultObj == null) || !resultObj.ToString().Contains("Node"))
                {
                    args.urlInfo.Depth += Settings.MaxReTryTimes / 2;
                    UrlQueue.Instance.EnQueue(args.urlInfo);
                }
                else
                {
                    int num;
                    StorageData data;
                    StringBuilder SB = new StringBuilder();
                    int existCount = 0;
                    int addCount = 0;
                    BsonDocument curUpdateBson = new BsonDocument {
                        {
                            "inventInfo",
                            resultObj.ToString().Replace("\r\n", "")
                        }
                    };
                    string name = string.Empty;
                    message = string.Format("详细信息{0}_{1}获取成功剩余url{2} retryUrl:{3}\r", new object[] { guid, name, UrlQueue.Instance.Count, UrlRetryQueue.Instance.Count });
                    //if (curUpdateBson.Count<BsonElement>() > 0)
                    //{
                    //    curUpdateBson.Set("isUpdate", "1");
                    //}
                    if (this.existGuidList.Contains(guid))
                    {
                        num = existCount;
                        existCount = num + 1;
                    }
                    else
                    {
                        this.existGuidList.Add(guid);
                        if (!string.IsNullOrEmpty(guid) && !this.ExistGuid(this.DataTableInventInfo, guid))
                        {
                            curUpdateBson.Set("guid", guid);
                            Interlocked.Increment(ref this.AllAddCount);
                            num = addCount;
                            addCount = num + 1;
                            SB.AppendFormat("获得对象{0}\r", guid);
                            //data = new StorageData
                            //{
                            //    Document = curUpdateBson,
                            //    Name = this.DataTableInventInfo,
                            //    Type = StorageType.Insert
                            //};
                            //DBChangeQueue.Instance.EnQueue(data);

                        }
                        else
                        {
                            num = existCount;
                            existCount = num + 1;
                            this.existGuidList.Add(guid);
                        }
                    }
                    data = new StorageData
                    {
                        Document = new BsonDocument().Add("isInventUpdate", "4"),
                        Name = this.curModetailInfoTableName,
                        Query = Query.EQ("guid", guid),
                        Type = StorageType.Update
                    };
                    DBChangeQueue.Instance.EnQueue(data);

                    var bgInfoDoc = resultObj.ToString().GetBsonDocFromJson();
                    ///推送消息队列
                    if (mqPushChk.Checked)
                    {
                        bgInfoDoc.Set("guid", guid);
                        Task.Run(async () => { await PushMessageAsync(bgInfoDoc); });
                    }
                    decimal updateCount = decimal.Parse(existCount.ToString()) / 10000000M;
                    this.ShowMessageInfo(string.Format("总添加：{9}是否建议跳过关键字：{7}_关键字已经添加:{6}| 当前：{5}添加:{0} 已存在:{1}剩余url{4} retryUrl:{8} 详细:{2}当前url:{3}", new object[] { addCount, existCount, SB.ToString(), "", UrlQueue.Instance.Count, "", "", this.needPassKeyWord, UrlRetryQueue.Instance.Count, this.AllAddCount }), false);
                }
            }
        }

        /// <summary>
        /// 获取app详情
        /// </summary>
        /// <param name="args"></param>
        public void DataReceiveEnterpriseDetailInfo_APP(DataReceivedEventArgs args)
        {
            string guid = args.urlInfo.UniqueKey;
            string keyName = "guid";
            string keyName_bak = "eGuid";
            if (enterprisePort==37888&&globalDBHasPassWord==false)
            {
                keyName = keyName_bak;
            }
            if (string.IsNullOrEmpty(guid))
            {
                this.ShowMessageInfo("传入Key为空", false);
            }
            if (args.Html.Contains("该企业主体信息获取失败"))
            {
                this.ShowMessageInfo("该企业主体信息获取失败"+UrlQueue.Instance.Count.ToString(), false);
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Set("isDetailUpdate",-1), Name = DataTableName, Query = Query.EQ(keyName, guid), Type = StorageType.Update });
                return;
            }
            JObject jsonObj = JObject.Parse(args.Html);
            if (jsonObj != null)
            {
                try
                {
                    //var result = jsonObj["result"]["Company"];
                    var result = jsonObj["result"];
                    if (result != null)
                    {
                        var companyDoc = result.ToString().GetBsonDocFromJson();
                        var updateBsonDoc = new BsonDocument();
                        var paid_in_capi = result["RecCap"] != null ? result["RecCap"].ToString() : string.Empty;
                        var termStart= result["TermStart"] != null ? result["TermStart"].ToString() : string.Empty;

                        var checkDate = Toolslib.Str.Sub(args.Html, "CheckDate\":\"", "\"");
                        updateBsonDoc.Set("paid_in_capi", paid_in_capi);
                        updateBsonDoc.Set("checkDate", checkDate);
                        companyDoc.Set("paid_in_capi", paid_in_capi);
                        if (checkDate.Length >= 10&&!checkDate.Contains("-"))
                        {
                            checkDate = checkDate.ConvertTimeStampDate_Unix().ToString("yyyy-MM-dd");
                        }
                        if (termStart.Length >= 10 & !termStart.Contains("-"))
                        {
                            termStart = termStart.ConvertTimeStampDate_Unix().ToString("yyyy-MM-dd");
                            companyDoc.Set("termBuildDate", termStart);
                        }

                        companyDoc.Set("checkDate", checkDate);
                        
                        var commonList = result["CommonList"];
                        if (commonList != null)
                        {
                            var jsonStr = commonList.ToString();
                            var commonDocList = jsonStr.ToBsonDocumentList();
                            var hitCommonAttr = commonDocList.Where(c => c.Text("KeyDesc") == "参保人数").FirstOrDefault();
                            if (hitCommonAttr != null)
                            {
                                var insuredPersonsNum = hitCommonAttr.Int("Value");
                                updateBsonDoc.Set("insuredPersonsNum", insuredPersonsNum);
                                companyDoc.Set("insuredPersonsNum", insuredPersonsNum);
                            }
                        }
                        Interlocked.Increment(ref AllAddCount);
                        updateBsonDoc.Set("isDetailUpdate", 1);
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBsonDoc, Name = DataTableName, Query = Query.EQ(keyName, guid), Type = StorageType.Update });
                        this.ShowMessageInfo($"{Settings.DeviceId}_{Settings.AccounInfo}总个数：{globalTotalCount}增：{AllAddCount} {guid}剩余url{UrlQueue.Instance.Count} retryUrl:{UrlRetryQueue.Instance.Count} 当前url:{HttpUtility.UrlDecode(args.Url)}", false);

                        ///推送消息队列
                        if (mqPushChk.Checked)
                        {

                            companyDoc.Set("guid", guid);
                            Task.Run(async () => { await PushMessageAsync(companyDoc); });
                        }
                    }
                }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                {

                }
            }
        }

        /// <summary>
        /// 获取app详情
        /// </summary>
        /// <param name="args"></param>
        public void DataReceiveADDEnterprise_APP(DataReceivedEventArgs args)
        {
            var existCount = 0;
            var addCount = 0;
            var SB = new StringBuilder();
            var message = string.Empty;
            var pageIndex = GetUrlParam(args.Url, "pageIndex");
            var pageSize = 40;
            var totalRecords = 0;
            var cityCode = GetUrlParam(args.Url, "cityCode");
            var province = GetUrlParam(args.Url, "province");
            var curCount = 0;
            JObject jsonObj = JObject.Parse(args.Html);
            if (jsonObj == null) return;
            try
            {
                var resultObj = jsonObj["result"];
                if (resultObj != null)
                {
                    int.TryParse(resultObj["Paging"]["TotalRecords"].ToString(), out totalRecords);
                    curCount = resultObj["Result"].Count();
                    foreach (var enterpriseObj in resultObj["Result"])
                    {

                        var curUpdateBson = new BsonDocument();
                        var guid = enterpriseObj["KeyNo"].ToString();
                        var name = FixDocStr(enterpriseObj["Name"].ToString());
                        var domain = string.Empty;
                        ///此处需要判断AreaCode是否存在
                        var areaCode = enterpriseObj["AreaCode"] != null ? enterpriseObj["AreaCode"].ToString() : string.Empty;//4290\t429004;
                        var curCityName = string.Empty;
                        var regionName = string.Empty;
                        ///根据areaCode进行所在城市获取
                        if (!string.IsNullOrEmpty(areaCode))
                        {
                            var areaCodeArr = areaCode.Split(new string[] { "\t", "" }, StringSplitOptions.RemoveEmptyEntries);
                            if (areaCodeArr.Length >= 2)
                            {
                                var curCityCode = allCountyCodeList.Where(c => c.Text("code") == areaCodeArr[0]).FirstOrDefault();
                                if (curCityCode != null)
                                {
                                    curCityName = curCityCode.Text("name");
                                }
                                var regionCode = allCountyCodeList.Where(c => c.Text("code") == areaCodeArr[1]).FirstOrDefault();
                                if (regionCode != null)
                                {
                                    regionName = regionCode.Text("name");
                                    if (curCityCode == null)
                                    {
                                        curCityCode = allCountyCodeList.Where(c => c.Text("code") == regionCode.Text("cityCode")).FirstOrDefault();
                                        if (curCityCode != null)
                                        {
                                            curCityName = curCityCode.Text("name");
                                        }
                                    }
                                }

                            }
                        }

                        if (string.IsNullOrEmpty(domain))
                        {
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
                                //2019.7.19 后数据接口返回的数据缺少行业范围字段
                                if (string.IsNullOrEmpty(domain))
                                {
                                    var hitReason = enterpriseObj["HitReason"];
                                    if (hitReason != null && hitReason["Field"] != null)
                                    {
                                        if (hitReason["Field"].ToString() == "经营范围")
                                        {
                                            domain = this.FixDocStr(hitReason["Value"].ToString());
                                        }
                                    }
                                }
                            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                            {
                                if (enterpriseObj["Scope"] != null)
                                {
                                    domain = this.FixDocStr(enterpriseObj["Scope"].ToString());
                                }

                            }
                        }
                        message = string.Format("详细信息{0}_{1}获取成功剩余url{2} retryUrl:{3}\r", guid, name, UrlQueue.Instance.Count, UrlRetryQueue.Instance.Count);
                        //curUpdateBson.Set("domain", domain);

                        curUpdateBson.Set("domain", FixDocStr(domain));
                        if (curCityName.EndsWith("市"))
                        {
                            curCityName = curCityName.TrimEnd('市');
                        }
                        //设定所属城市
                        curUpdateBson.Set("cityName", curCityName);
                        if (!string.IsNullOrEmpty(regionName))
                        {
                            curUpdateBson.Set("regionName", regionName);
                        }
                        curUpdateBson.Set("Province", province);

                        var entDetail = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(enterpriseObj.ToString());
                        //增加其他字段
                        foreach (var item in entDetail.Elements)
                        {
                            if (EnterpriseInfoMapDic_App.ContainsKey(item.Name))
                            {
                                curUpdateBson.Set(EnterpriseInfoMapDic_App[item.Name], this.FixDocStr(entDetail.Text(item.Name)));
                            }
                            else
                            {
                                curUpdateBson.Set(item.Name, this.FixDocStr(enterpriseObj[item.Name].ToString()));
                            }// 其他字段
                        }

                        // curUpdateBson.Set("Industry", "");
                        curUpdateBson.Set("isVipAdd", 1);
                        string uniqueKey = args.urlInfo.UniqueKey;
                        if (!string.IsNullOrEmpty(uniqueKey))
                        {
                            curUpdateBson.Set("uniqueKey", uniqueKey);
                        }

                        if (curUpdateBson.Count<BsonElement>() > 0)
                        {
                            curUpdateBson.Set("isUserUpdate", "1");
                        }
                        if (existGuidList.Contains(guid))
                        {
                            existCount++;
                        }
                        else
                        {//代表还未添加
                            existGuidList.Add(guid);
                            if (!string.IsNullOrEmpty(guid) && !ExistGuid(guid))
                            {
                                Interlocked.Increment(ref AllAddCount);
                                addCount++;
                                SB.AppendFormat("获得对象{0}:{1}\r", guid, name);
                                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Insert });
                            }
                            else
                            {
                                existCount++;
                                //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Query = Query.EQ("guid", guid), Type = StorageType.Update });
                                existGuidList.Add(guid);

                            }
                        }

                    }

                }
                this.ShowMessageInfo($"当前{totalRecords}_总添加：{this.AllAddCount} 添加:{addCount} 已存在:{existCount}剩余url{UrlQueue.Instance.Count} retryUrl:{UrlRetryQueue.Instance.Count} 详细:{SB.ToString()}当前url:{HttpUtility.UrlDecode(args.Url)}", false);
                //pageindex
                //生成下一页
                //if ((string.IsNullOrEmpty("pageIndex") || pageIndex == "1") && totalRecords > pageSize)
                //{
                //    var totalPageSize = (((totalRecords - 1) / pageSize) + 1);

                //    for (var i = 2; i <= totalPageSize; i++)
                //    {
                //        var newUrl = this.ReplaceUrlParamEx(args.Url, "pageIndex", i.ToString());
                //        UrlSplitedEnqueue(args.urlInfo, newUrl, totalRecords);
                //     }
                //}
                //进入下一页

                if (curCount >= pageSize)
                {
                    var allCount = 5000;//40页
                    var allPageszie = (allCount - 1) / pageSize + 1;
                    if (int.TryParse(pageIndex, out int curPageIndex))
                    {
                        if (curPageIndex <= 1)
                        {
                            curPageIndex = 1;
                        }
                        var nextPageIndex = curPageIndex + 1;
                        if (splitLimitChk.Checked && existCount >= pageSize)
                        {
                            nextPageIndex = curPageIndex*3;//跳页采集

                        }
                        else
                        {
                            nextPageIndex = curPageIndex + 1;
                        }
                        var newUrl = this.ReplaceUrlParamEx(args.Url, "pageIndex", (curPageIndex + 1).ToString());
                        //UrlSplitedEnqueue(args.urlInfo, newUrl, totalRecords);
                        if (!urlFilter.Contains(newUrl))//防止重复爬取
                        {
                            AddRetryUrlQueue(new UrlInfo(newUrl), newUrl);
                            urlFilter.Add(newUrl);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddRetryUrlQueue(args.urlInfo,args.Url);
                ShowMessageInfo(ex.Message);
                 
            }

        }

        /// <summary>
        /// 获取app岗位详情
        /// </summary>
        /// <param name="args"></param>
        public void DataReceiveEnterprisePositionDetailByGuid_APP(DataReceivedEventArgs args)
        {
            string guid = args.urlInfo.UniqueKey;
            string keyName = "guid";
            string keyName_bak = "eGuid";
            if (enterprisePort == 37888 && globalDBHasPassWord == false)
            {
                keyName = keyName_bak;
            }
            if (string.IsNullOrEmpty(guid))
            {
                this.ShowMessageInfo("传入Key为空", false);
            }
           
            JObject jsonObj = JObject.Parse(args.Html);
            if (jsonObj != null)
            {
                try
                {
                    var result = jsonObj["result"]["Result"];
                    if (result != null)
                    {
                        var updateBsonDoc = new BsonDocument();
                        foreach (var job in result)
                        {
                            var obj = job.ToString().GetBsonDocFromJson();
                            obj.Set("eGuid", guid);
                            obj.Set("guid", obj.Text("Id"));
                            Interlocked.Increment(ref AllAddCount);
                            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = obj, Name = DataTableEnterprisePosition, Query = Query.EQ(keyName, obj.Text("Id")), Type = StorageType.Update });
                        }
                        updateBsonDoc.Set("isJobDetailUpdate", 1);
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBsonDoc, Name = DataTableName, Query = Query.EQ(keyName, guid), Type = StorageType.Update });
                        this.ShowMessageInfo($"{Settings.DeviceId}_{Settings.AccounInfo}总个数：{globalTotalCount}增：{AllAddCount} {guid}剩余url{UrlQueue.Instance.Count} retryUrl:{UrlRetryQueue.Instance.Count} 当前url:{HttpUtility.UrlDecode(args.Url)}", false);
                    }
                }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                {

                }
            }
        }
        #region 不常用
        // HttpHelper http = new HttpHelper();
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            BsonDocument curLoginAccountObj = null;
            BsonDocument curDeviceObj = null;
            ///重载uri

            try
            {
                //GetSimulateCookies();

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
                    case SearchType.EnterpriseInvent:
                        this.DataReceiveInitialEnterpriseInvent(args);
                        break;
                    case SearchType.UpdateEnterpriseInfoAPP:
                        this.DataReceiveEnterpriseDetailInfo_APP(args);
                        break;
                    case SearchType.ADDEnterpriseAPP:
                        this.DataReceiveADDEnterprise_APP(args);
                        break;
                    case SearchType.EnterprisePositionDetailByGuid_APP:
                        this.DataReceiveEnterprisePositionDetailByGuid_APP(args);
                        break;
                    case SearchType.EnterpriseBackGroundDetailByGuid_APP://背后关系
                        this.DataReceiveDetailInfo_BackGround_APP(args);
                        break;
                    default:
                        DataReceiveEnterpriseInfo(args);
                        break;
                }
                
                ///账号爬取统计，防止被封，查看爬去个数
                if (!IsAPP(searchType))
                {
                    curLoginAccountObj = allAccountList.Where(c => c.Text("name") == Settings.LoginAccount).FirstOrDefault();
                }
                else
                {
                    curDeviceObj = allDeviceAccountList.Where(c => c.Text("deviceId") == Settings.DeviceId).FirstOrDefault();
                    if (IsNeedLogin(searchType))
                    {
                        curLoginAccountObj = allAccountList.Where(c => c.Text("name") == Settings.LoginAccount).FirstOrDefault();
                    }
                }
                var maxAddionalCount = 0;
                if (curLoginAccountObj != null)
                {
                    var columnName = string.Format("{0}_add", searchType);
                    var curAddional = Settings.AccounAddCount; ;
                    try
                    {
                        curAddional = curLoginAccountObj.Int(columnName);
                        curLoginAccountObj.Set(columnName, curAddional + 1);
                        ShowAccountInfo(curDeviceObj);
                    }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                    catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                    {
                        curAddional = Settings.AccounAddCount + 1;
                        ReloadLoginAccount();
                        //  ShowMessageInfo(ex.Message + args.Url);
                    }
                    maxAddionalCount = curAddional;
                }
                 if (curDeviceObj != null)
                {
                    var columnName = string.Format("{0}_add", searchType);
                    var curAddional = Settings.AccounAddCount; ;
                    try
                    {
                        curAddional = curDeviceObj.Int(columnName);
                        curDeviceObj.Set(columnName, curAddional + 1);
                       
                      
                        ShowAccountInfo(curDeviceObj);
                    }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                    catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                    {
                        curAddional = Settings.AccounAddCount + 1;
                        ReloadLoginAccount();
                        //  ShowMessageInfo(ex.Message + args.Url);

                    }
                    maxAddionalCount = curAddional;
                }
                else {
                    Settings.AccounAddCount++;
                }
                if (maxAddionalCount == 0)
                {
                    maxAddionalCount = Settings.AccounAddCount;
                }
                if (Settings.MaxAccountCrawlerCount > 0 && maxAddionalCount >= Settings.MaxAccountCrawlerCount)
                {
                    Settings.AccounAddCount = 0;
                    Settings.neeedChangeAccount = true;//达到数量需要切换账号
                }

            }
            catch (Exception ex)
            {
                args.urlInfo.Depth = Settings.MaxReTryTimes / 10;
                // UrlQueue.Instance.EnQueue(args.urlInfo);
                AddUrlQueue(args.urlInfo);
                ShowMessageInfo(ex.Message + args.Url);
                //添加到错误数据库
                DBChangeQueue.Instance.EnQueue(new StorageData()
                {
                    Document = new BsonDocument().Set("url", args.Url).Set("province", curQCCProvinceCode).Set("cityName", cityNameStr),
                    Name = DataTableNameErrorUrl,
                    Type = StorageType.Insert
                });
                LogHelper.Info($"{ex.Message}\n{args.Url}");
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
            var message = string.Format("详细信息{0}获取成功剩余url{1}\r{2}", guid, UrlQueueCount(), args.Url);
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
                ShowMessageInfo(string.Format("当前对象:{0}剩余url{1}", oldName, UrlQueueCount()));
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
                var message = string.Format("详细信息{0}:{1}获取成功剩余url{2}\r", guid, oldName, UrlQueueCount());
                ShowMessageInfo(message);
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Insert });

                //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("status", "1"), Query = Query.EQ("name", enterpriseName), Name = DataTableNameList, Type = StorageType.Update });
            }
            else
            {
                ShowMessageInfo(string.Format("guid:{0}{1}已存在或者无法添加剩余url{2}\r", guid, oldName, UrlQueueCount()));

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
                ShowMessageInfo(string.Format("当前记录:{0}过大进行分支处理剩余url:{1}", firstRecordCount, UrlQueueCount()));
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
                ShowMessageInfo("找不到searchDiv对象" + args.Url + firstRecordCountSpan.InnerText + "剩余url" + UrlQueueCount());
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

                        SB.AppendFormat("获得对象{0}:{1}\r", guid, enterpriseName, UrlQueueCount());
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
            ShowMessageInfo(string.Format("剩余url{4}当前：{5}添加:{0} 已存在:{1}当前url:{3} 详细:{2}", addCount, existCount, SB.ToString(), HttpUtility.UrlDecode(queryStr), UrlQueueCount(), firstRecordCount.ToString()));
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
            ShowMessageInfo(str + string.Format("addCount:{0} updateCount:{1} existCount:{2} 剩余url:{3} ", addCount, updateCount, existCount, UrlQueueCount()));


        }
        /// <summary>
        /// 用与查找城市
        /// </summary>
        /// <param name="args"></param>
        public void DataReceiveSearchGuidByCity_abort(DataReceivedEventArgs args)
        {
#pragma warning disable CS0219 // 变量“updateCount”已被赋值，但从未使用过它的值
#pragma warning disable CS0219 // 变量“addCount”已被赋值，但从未使用过它的值
#pragma warning disable CS0219 // 变量“existCount”已被赋值，但从未使用过它的值
            int addCount = 0, updateCount = 0, existCount = 0;
#pragma warning restore CS0219 // 变量“existCount”已被赋值，但从未使用过它的值
#pragma warning restore CS0219 // 变量“addCount”已被赋值，但从未使用过它的值
#pragma warning restore CS0219 // 变量“updateCount”已被赋值，但从未使用过它的值
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
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
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
            ShowMessageInfo(str + string.Format("addCount:{0} updateCount:{1} existCount:{2} 剩余url:{3} ", addCount, updateCount, existCount, UrlQueueCount()));


        }

        #endregion
        #region 数据处理
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
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

            }
            return false;
        }


        /// <summary>
        /// 添加修正当个数大于=5000的时候进行url笛卡儿增加url 减少url的访问个数
        /// V3版本有CountryCode  
        /// V2版本没有所在城市字段 行业url有所区分（效率更高，可能异常封号？
        /// 需要对比vscode1字段）尽量手动生成country字段 ，即url中没有cityCode字段按省爬取，需要手动生成countyCode字段
        /// </summary>
        /// <param name="args"></param>
        /// <param name="firstRecordCount"></param>
        /// <param name="htmlDoc"></param>
        /// <returns></returns>
        private bool IsKeyWordUrlSplited_APP(DataReceivedEventArgs args, int firstRecordCount, JToken htmlDoc)
        {
            

            //使用旧版本
            if (!IsQCCUrlNewVersion())
            {
                return IsKeyWordUrlSplited_APP_V2(args, firstRecordCount, htmlDoc);
            }
            if (firstRecordCount < 40) return false;
            //registcapiNew 注册资本//startdateNew 成立日期//statuscodeNew 企业状态 industrycodeNew 行业门类
            var registCapiBeginParam = GetUrlParam(args.Url, "registCapiBegin");
            var registCapiEndParam = GetUrlParam(args.Url, "registCapiEnd");
            var statusCodeParam = GetUrlParam(args.Url, "statusCode");
            var startDateBeginParam = GetUrlParam(args.Url, "startDateBegin");
            var startDateEndParam = GetUrlParam(args.Url, "startDateEnd");
            var industryCode = GetUrlParam(args.Url, "industryCode");
            var subIndustryCode = GetUrlParam(args.Url, "subIndustryCode");
            var countyCode = GetUrlParam(args.Url, "countyCode");//新版城市代码
            var province = GetUrlParam(args.Url, "province");//新版城市省份代码


            try
            {
                ///省份参数替换,只传入城市code未传入省份code
                if (IsEmptyProvinceUrlSplited_Manual(args, province, firstRecordCount))
                {
                    return true;
                }

                #region 行业门类大类 可能导致超出url 计算 需要contains去除 使用新的vip2 接口的时候instructorycode可能为空
                //var industryDataValueList = GetFilterDataValue_App(htmlDoc, "industrycode", 0);
                ///是否对url进行了人工产业细分
                if (IsIndustryUrlSplited_Manual(args, industryCode, subIndustryCode, firstRecordCount))
                {
                    return true;
                }
                #endregion

                #region 使用countyCode进行筛选，当countyCode不为空则获取对应下的区域
                //进行手动获取城市下的区县url切分
                if (IsCountyCodeUrlSplited_Manual(args, countyCode, province, firstRecordCount))
                {
                    return true;
                }

                #endregion

                #region 成立日期分支筛选

                if (IsDateDataUrlSplited_Manual(args, startDateBeginParam, startDateEndParam, htmlDoc, firstRecordCount))
                {
                    return true;
                }

                #endregion

                #region 注册资本分支筛选
                if (IsRegistCapiUrlSplited_Manual(args, registCapiBeginParam, registCapiEndParam, htmlDoc, firstRecordCount))
                {
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
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

            }
            //此处有个情况 当 2016-08-21中有46条数据，而次url为 -2016-08-01 2016-08-30 此处需要读取超级多次，2倍后10次内需要重复读取，而只要随意换算条件即可正确算出url
            //好的读取顺序可能会导致减少url的读取防止账号浪费
            //再次分解时间

            return IterationSplitModeActive(args, htmlDoc, firstRecordCount);
          
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
#pragma warning disable CS0162 // 检测到无法访问的代码
            ///如何防止重复循环获取
            if ((registCapiBeginParam == registCapiEndParam || (registCapiBeginParam == "" || registCapiEndParam == "" || registCapiEndParam == "50000")) && startDateBeginParam == startDateEndParam)
#pragma warning restore CS0162 // 检测到无法访问的代码
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

                    UrlSplitedEnqueue(args.urlInfo, sortAscUrl, firstRecordCount);
                    //AddUrlQueue(sortAscUrl);
                    //AddUrlQueue(registcapSortDescUrl);
                    //AddUrlQueue(startdateSortAscUrl); 
                    //AddUrlQueue(startdateSortDescUrl);
                    //AddUrlQueue(registcapSortAscUrl);
                    // curAddUrlList.Add(sortAscUrl);
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



        /// <summary>
        /// V2版本拆分url
        /// 添加修正当个数大于=5000的时候进行url笛卡儿增加url 减少url的访问个数
        ///  url = string.Format(this.QCCSearchUrl + "/base/advancedSearch?searchKey={0}&searchIndex=default&province=&cityCode=&pageIndex=1&isSortAsc=false&startDateBegin=&startDateEnd=&registCapiBegin=&registCapiEnd=&industryV3=&industryCode=&subIndustryCode=&searchType=&countyCode=", new object[] { curTypeName
        ///  url = string.Format(this.QCCSearchUrl + "/base/advancedSearch?pageIndex=1&searchKey={0}&province=&cityCode=&industryCode=&subIndustryCode=&registCapiBegin=&registCapiEnd=&startDateBegin=&startDateEnd=&hasPhone=&hasEmail=&isSortAsc=false", new object[] { curTypeName });
        /// </summary>
        /// <param name="args"></param>
        /// <param name="firstRecordCount"></param>
        /// <param name="htmlDoc"></param>
        /// <returns></returns>
        private bool IsKeyWordUrlSplited_APP_V2(DataReceivedEventArgs args, int firstRecordCount, JToken htmlDoc)
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
            var countyCode = GetUrlParam(args.Url, "countyCode");//新版城市代码
            var province = GetUrlParam(args.Url, "province");//新版城市省份代码
            var citycode = GetUrlParam(args.Url, "cityCode");//新版城市省份代码
            try
            {
                ///对url 附加province,此处需要考虑旧版本不应该进行数据更i性能
                if (IsProvinceUrlSplited_JsonData(args, province, citycode, htmlDoc, firstRecordCount))
                {
                    return true;
                }

                //2018.3.20增加更新的时候不进行行业门类过滤 大于5000才进行子分类
                if (IsIndustryUrlSplited_JsonData(args, industryCode, subIndustryCode, htmlDoc, firstRecordCount))
                {
                    return true;
                }
                #region 成立日期分支筛选
                if (IsDateDataUrlSplited_JsonData(args, startDateBeginParam, startDateEndParam, htmlDoc, firstRecordCount))
                {
                    return true;
                }

                #endregion

               

                #region 注册资本分支筛选
                if (IsRegistCapiUrlSplited_JsonData(args, registCapiBeginParam, registCapiEndParam, htmlDoc, firstRecordCount))
                {
                    return true;
                }

                #endregion



            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

            }
            ///此处有个情况 当 2016-08-21中有46条数据，而次url为 -2016-08-01 2016-08-30 此处需要读取超级多次，2倍后10次内需要重复读取，而只要随意换算条件即可正确算出url
            ///好的读取顺序可能会导致减少url的读取防止账号浪费
            ///再次分解时间
            #region 优化方法
            //var searchKey = GetUrlParam(args.Url, "searchKey");
            //var hitSearchKeyConditionObj = searchKeyConditonList.Where(c => c.searchKey == searchKey).FirstOrDefault();
            //if (hitSearchKeyConditionObj == null)
            //{
            //    hitSearchKeyConditionObj = new SearchKeyCondition() { recordCount = firstRecordCount, searchKey = searchKey, mode = urlSplitMode };
            //    searchKeyConditonList.Add(hitSearchKeyConditionObj);
            //}
            //else
            //{

            //    if (hitSearchKeyConditionObj.hitCount >= 10)
            //    {
            //        return false;
            //    }
            //    if (hitSearchKeyConditionObj.recordCount == firstRecordCount)
            //    {
            //        hitSearchKeyConditionObj.hitCount++;
            //    }
            //    else
            //    {
            //        hitSearchKeyConditionObj.recordCount = firstRecordCount;
            //    }
            //}
            //UrlSpliteMode _urlSplitMode = this.urlSplitMode;



            //DateTime _startDateBegin;
            //DateTime _startDateEnd;
            //TimeSpan span;
            //try
            //{
            //    if (string.IsNullOrEmpty(startDateBeginParam))
            //    {
            //        startDateBeginParam = "17700101";
            //    }
            //    if (string.IsNullOrEmpty(startDateEndParam))
            //    {
            //        startDateEndParam = DateTime.Now.ToString("yyyyMMdd");
            //    }
            //    _startDateBegin = DateTime.ParseExact(startDateBeginParam, "yyyyMMdd", Thread.CurrentThread.CurrentCulture);
            //    _startDateEnd = DateTime.ParseExact(startDateEndParam, "yyyyMMdd", Thread.CurrentThread.CurrentCulture);
            //    span = _startDateEnd - _startDateBegin;
            //}
            //catch (Exception ex)
            //{
            //    var dateStr = $"{startDateBeginParam}{startDateEndParam}";
            //    throw new Exception("SplitModeActiveBegin" + ex.Message + dateStr);
            //}

            //try
            //{
            //    if ((span.Days <= 30) || (hitSearchKeyConditionObj.hitCount >= 5))
            //    {
            //        if (registCapiBeginParam != registCapiEndParam)
            //        {
            //            _urlSplitMode = this.GetNextSplitMode(hitSearchKeyConditionObj.mode);
            //        }

            //    }
            //    bool splitStatus = this.SplitModeActive(_urlSplitMode, args.Url, startDateBeginParam, startDateEndParam, registCapiBeginParam, registCapiEndParam, subIndustryCode, htmlDoc, firstRecordCount);
            //    if (splitStatus)
            //    {
            //        return splitStatus;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception("SplitModeActiveEnd" + ex.Message );
            //}
            //return true;
            //迭代

            #endregion
            return IterationSplitModeActive(args, htmlDoc, firstRecordCount);

        }
        #region 服务器数据条件过滤
        /// <summary>
        /// 注册金额服务器条件过滤
        /// </summary>
        /// <param name="args"></param>
        /// <param name="registCapiBeginParam"></param>
        /// <param name="registCapiEndParam"></param>
        /// <param name="htmlDoc"></param>
        /// <param name="firstRecordCount"></param>
        /// <returns></returns>
        private bool IsRegistCapiUrlSplited_JsonData(DataReceivedEventArgs args, string registCapiBeginParam, string registCapiEndParam, JToken htmlDoc, int firstRecordCount)
        {
            var dataValueList = GetFilterDataValue_App(htmlDoc, "registcapi", firstRecordCount);
            if (dataValueList.Count() > 0 && (string.IsNullOrEmpty(registCapiBeginParam)|| registCapiBeginParam=="0") && string.IsNullOrEmpty(registCapiEndParam))
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

                        //curUrl = curUrl.Replace("&registCapiBegin=", string.Format("&registCapiBegin={0}", registCapiBegin));
                        curUrl = ReplaceUrlParamEx(curUrl, "registCapiBegin", registCapiBegin);
                        //curUrl = curUrl.Replace("&registCapiEnd=", string.Format("&registCapiEnd={0}", registCapiEnd));
                        curUrl = ReplaceUrlParamEx(curUrl, "registCapiEnd", registCapiEnd);
                        UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);
                        //if (!urlFilter.Contains(curUrl))//防止重复爬取
                        //{
                        //    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤

                        //    urlFilter.Add(curUrl);
                        //    curAddUrlList.Add(curUrl);
                        //}
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 通过服务器返回的日期数据进行过滤
        /// 2020优化对url进行优化 可能为 2018 1 2019 1 2020 3 2015
        /// 将少于40的合并成一个url
        /// </summary>
        /// <param name="args"></param>
        /// <param name="startDateBeginParam"></param>
        /// <param name="startDateEndParam"></param>
        /// <param name="htmlDoc"></param>
        /// <returns></returns>
        private bool IsDateDataUrlSplited_JsonData(DataReceivedEventArgs args, string startDateBeginParam, string startDateEndParam, JToken htmlDoc, int firstRecordCount)
        {
            var dateDataValueList = GetFilterDataValue_App(htmlDoc, "startdateyear", firstRecordCount);
            if (dateDataValueList.Count() > 0 && (string.IsNullOrEmpty(startDateBeginParam) || string.IsNullOrEmpty(startDateEndParam)))
            {
                if (!args.Url.Contains("startDateBegin"))
                {
                    args.Url += "&startDateBegin=";
                }
                if (!args.Url.Contains("startDateEnd"))
                {
                    args.Url += "&startDateEnd=";
                }
                var filterYearList= IsDateDataUrlSplited_Aggregate_JsonData(args, dateDataValueList, firstRecordCount);
                foreach (var dataValueDic in dateDataValueList.Where(c=>!filterYearList.Contains(c.Key)))//2016
                {
                    var dataValue = dataValueDic.Key;
                    var dataCount = dataValueDic.Value;
                     
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
                       // var curUrl = args.Url.Replace("&startDateBegin=", string.Format("&startDateBegin={0}", startDateBegin));
                        var curUrl = ReplaceUrlParamEx(args.Url, "startDateBegin", startDateBegin);
                        //curUrl = curUrl.Replace("&startDateEnd=", string.Format("&startDateEnd={0}", startDateEnd));
                        curUrl = ReplaceUrlParamEx(curUrl, "startDateEnd", startDateEnd);
                        UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);
                        //if (!urlFilter.Contains(curUrl))//防止重复爬取
                        //{

                        //    AddUrlQueue(args.urlInfo, curUrl);
                        //    urlFilter.Add(curUrl);
                        //    curAddUrlList.Add(curUrl);
                        //}
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
                                //var curUrl = args.Url.Replace("&startDateBegin=", string.Format("&startDateBegin={0}", startDateBegin));
                                var curUrl = ReplaceUrlParamEx(args.Url, "startDateBegin", startDateBegin);
                                //curUrl = curUrl.Replace("&startDateEnd=", string.Format("&startDateEnd={0}", startDateEnd));
                                curUrl = ReplaceUrlParamEx(curUrl, "startDateEnd", startDateEnd);
                                UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);
                                //if (!urlFilter.Contains(curUrl))//防止重复爬取
                                //{
                                //    //args.urlInfo.UrlString = curUrl;
                                //    //UrlQueue.Instance.EnQueue(args.urlInfo);//添加条件过滤
                                //    AddUrlQueue(args.urlInfo, curUrl);
                                //    urlFilter.Add(curUrl);
                                //    curAddUrlList.Add(curUrl);
                                //}

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
                               // var curUrl = args.Url.Replace("&startDateBegin=", string.Format("&startDateBegin={0}", startDateBegin));
                                var curUrl = ReplaceUrlParamEx(args.Url, "startDateBegin", startDateBegin);
                                //curUrl = curUrl.Replace("&startDateEnd=", string.Format("&startDateEnd={0}", startDateEnd));
                                curUrl = ReplaceUrlParamEx(curUrl, "startDateEnd", startDateEnd);
                                UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);
                                //if (!urlFilter.Contains(curUrl))//防止重复爬取
                                //{
                                //    //args.urlInfo.UrlString = curUrl;
                                //    //UrlQueue.Instance.EnQueue(args.urlInfo);//添加条件过滤
                                //    AddUrlQueue(args.urlInfo, curUrl);
                                //    urlFilter.Add(curUrl);
                                //    curAddUrlList.Add(curUrl);
                                //}

                                beginDate = beginDate.AddDays(2);
                                if (beginDate.Year.ToString() != dataValue)
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
            return false;
        }
        /// <summary>
        /// 2020优化对url进行优化 可能为 2018 1 2019 1 2020 3 2015
        /// 聚合前n个
        /// </summary>
        /// <param name="dataValueDic"></param>
        /// <returns></returns>
        private List<string> IsDateDataUrlSplited_Aggregate_JsonData(DataReceivedEventArgs args, Dictionary<string, int> dataValueDic,int firstRecordCount)
        {
            var sortDic = dataValueDic.OrderBy(c => c.Key);
            var maxCount = 40;
            var curUrlCount = 0;
#pragma warning disable CS0219 // 变量“hitIndex”已被赋值，但从未使用过它的值
            var hitIndex = 0;
#pragma warning restore CS0219 // 变量“hitIndex”已被赋值，但从未使用过它的值
            var beginYear =string.Empty;
            var endYear = string.Empty;
            var addYearList = new List<string>();
            foreach (var item in sortDic)
            {
                var curYear = item.Key;
                var curYearCount = item.Value;
                curUrlCount += curYearCount;
                if (curUrlCount > maxCount)//超出每条个数
                {
                    if (!string.IsNullOrEmpty(beginYear) && !string.IsNullOrEmpty(endYear)&& beginYear!= endYear)
                    {
                        //添加url;
                        var startDateBegin = string.Format("{0}0101", beginYear);
                        var startDateEnd = string.Format("{0}1231", endYear);
                        var curUrl = ReplaceUrlParamEx(args.Url, "startDateBegin", beginYear);
                        curUrl = ReplaceUrlParamEx(curUrl, "startDateEnd", endYear);
                        UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);
                        addYearList.Add(beginYear);
                        addYearList.Add(endYear);
                    }
                    beginYear = curYear;
                    endYear = curYear;
                    curUrlCount = curYearCount;
                    continue;
                }
                if (string.IsNullOrEmpty(beginYear))
                {
                    beginYear = curYear;
                }
                endYear = curYear;
                addYearList.Add(curYear);
            }
            if (curUrlCount<= maxCount&&!string.IsNullOrEmpty(beginYear) && !string.IsNullOrEmpty(endYear) && beginYear != endYear)
            {
                //添加url;
                var startDateBegin = string.Format("{0}0101", beginYear);
                var startDateEnd = string.Format("{0}1231", endYear);
                var curUrl = ReplaceUrlParamEx(args.Url, "startDateBegin", beginYear);
                curUrl = ReplaceUrlParamEx(curUrl, "startDateEnd", endYear);
                UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);
                addYearList.Add(beginYear);
                addYearList.Add(endYear);
            }
            addYearList = addYearList.Distinct().ToList();
            return addYearList;
        }
        /// <summary>
        /// 通过server返回json条件进行过滤
        /// </summary>
        /// <param name="args"></param>
        /// <param name="industryCode"></param>
        /// <param name="subIndustryCode"></param>
        /// <param name="htmlDoc"></param>
        /// <returns></returns>
        private bool IsIndustryUrlSplited_JsonData(DataReceivedEventArgs args, string industryCode, string subIndustryCode, JToken htmlDoc, int firstRecordCount)
        {
            if (string.IsNullOrEmpty(industryCode))
            {
                var industryDataValueList = GetFilterDataValue_App(htmlDoc, "industrycode");
                if (industryDataValueList.Count() > 0 && string.IsNullOrEmpty(industryCode))
                {
                    foreach (var dataValueDic in industryDataValueList)//2016
                    {

                        var dataValue = dataValueDic.Key;
                        if (string.IsNullOrEmpty(dataValue)) continue;
                        //var curUrl = this.ReplaceParam(args.Url, "industryCode", industryCode, dataValue);
                        var curUrl = ReplaceUrlParamEx(args.Url, "industryCode", dataValue);
                        UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);
                        //if (!urlFilter.Contains(curUrl))//防止重复爬取
                        //{
                        //    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                        //    urlFilter.Add(curUrl);
                        //    curAddUrlList.Add(curUrl);
                        //}
                    }
                    return true;
                }
            }
            //大于500使用子分类分解
            if (firstRecordCount >= 4000 && string.IsNullOrEmpty(subIndustryCode))
            {
                var subIndustryDataValueList = GetFilterDataValue_App(htmlDoc, "subindustrycode");
                if (subIndustryDataValueList.Count() > 0 && string.IsNullOrEmpty(subIndustryCode))
                {
                    foreach (var dataValueDic in subIndustryDataValueList)//2016
                    {

                        var dataValue = dataValueDic.Key;
                        if (string.IsNullOrEmpty(dataValue)) continue;
                        //var curUrl = this.ReplaceParam(args.Url, "subIndustryCode", subIndustryCode, dataValue);
                        var curUrl = this.ReplaceUrlParamEx(args.Url, "subIndustryCode", dataValue);
                        UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);
                        //if (!urlFilter.Contains(curUrl))//防止重复爬取
                        //{
                        //    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                        //    urlFilter.Add(curUrl);
                        //    curAddUrlList.Add(curUrl);
                        //}
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 通过修改url 匹配  没有省份匹配省份
        /// </summary>
        /// <param name="args"></param>
        /// <param name="province"></param>
        /// <param name="htmlDoc"></param>
        /// <returns></returns>
        private bool IsProvinceUrlSplited_JsonData(DataReceivedEventArgs args, string province, string citycode, JToken htmlDoc, int firstRecordCount)
        {
            if (string.IsNullOrEmpty(province))
            {
                var provinceDataValueList = GetFilterDataValue_App(htmlDoc, "province");
                if (onlySpecialCityChk.Checked)
                {
                    provinceDataValueList = provinceDataValueList.Where(c => specialProvince.Contains(c.Key)).ToDictionary(d => d.Key, c => c.Value);
                }
                if (provinceDataValueList.Count() > 0 && string.IsNullOrEmpty(province))
                {
                    foreach (var dataValueDic in provinceDataValueList)//2016
                    {

                        var dataValue = dataValueDic.Key;
                        if (string.IsNullOrEmpty(dataValue)) continue;
                        //var curInitUrl = this.ReplaceParam(args.Url, "province", province, dataValue);
                        var curInitUrl = this.ReplaceUrlParamEx(args.Url, "province",dataValue);
                        //是否需要快速模式，打开该模式不进行细分城市注意：该模式会导致所属城市可能获取不到
                        if (quickModeChk.Checked)
                        {
                            UrlSplitedEnqueue(args.urlInfo, curInitUrl, firstRecordCount);
                        }
                        else
                        {
                            //细分城市，旧版本需要该模式才能正确获取所属城市
                            var resultUrlList = GetCityCountyCodeUrlSplited_JsonData(curInitUrl, dataValue);//获取子城市
                            foreach (var curUrl in resultUrlList)
                            {
                                UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);
                            }
                        }
                    }
                    return true;

                }
            }

            if (string.IsNullOrEmpty(citycode))
            {
                var curCityList = oldCityList.Where(c => c.Text("provinceCode") == province).ToList();//当前省对象
                var countyCode = GetUrlParam(args.Url, "countyCode");//新版城市代码
                var cityDataValueList = GetFilterDataValue_App(htmlDoc, "citycode");
                if (cityDataValueList.Count() > 0 && string.IsNullOrEmpty(citycode))
                {
                    foreach (var dataValueDic in cityDataValueList)//2016
                    {
                        var dataValue = dataValueDic.Key;//对应城市下的cityCode
                        var countyCode_new = string.Empty;
                        ///获取旧的
                        var cityObj = curCityList.Where(c => c.Text("code") == dataValue).FirstOrDefault();
                        if (cityObj != null)
                        {
                            countyCode_new = GetProvinceCountryCode(province, cityObj.Text("name"), "1");
                        }
                        else
                        {
                            ShowMessageInfo("获取countryCode失败");
                        }
                        if (string.IsNullOrEmpty(dataValue)) continue;
                        //var curUrl = this.ReplaceParam(args.Url, "cityCode", citycode, dataValue);
                        var curUrl = this.ReplaceUrlParamEx(args.Url, "cityCode", dataValue);
                        //curUrl = this.ReplaceParam(curUrl, "countyCode", countyCode, countyCode_new);//增加countyCode
                        curUrl = this.ReplaceUrlParamEx(curUrl, "countyCode", countyCode_new);
                        UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);

                    }
                    return true;

                }
            }
            return false;
        }



        /// <summary>
        /// 进行手动获取城市下的城市url切分
        /// </summary>
        /// <param name="args"></param>
        /// <param name="countyCode"></param>
        /// <param name="province"></param>
        /// <returns></returns>
        private List<string> GetCityCountyCodeUrlSplited_JsonData(string url, string province)
        {
            var urlResult = new List<string>();
            if (!string.IsNullOrEmpty(province))
            {
                List<string> subCityCodeList = oldCityList.Where(c => c.Text("provinceCode") == province && c.Text("type") == "1").Select(c => c.Text("code")).ToList();
                if (specialProvince.Contains(province))
                {
                    subCityCodeList = new List<string>() { province };
                }
                if (subCityCodeList.Count > 0)
                {
                    foreach (string curCityCode in subCityCodeList)
                    {
                        string curUrl = this.ReplaceUrlParamEx(url, "cityCode", curCityCode);
                        if (!this.urlFilter.Contains(curUrl))
                        {
                            urlResult.Add(curUrl);
                        }
                    }

                }
            }
            return urlResult;
        }
        #endregion




        #region V3版本后因返回的url 不再出现筛选条件的url手工切分汇总

        /// <summary>
        /// 注册金额手工拆分
        /// </summary>
        /// <param name="args"></param>
        /// <param name="registCapiBeginParam"></param>
        /// <param name="registCapiEndParam"></param>
        /// <param name="htmlDoc"></param>
        /// <param name="firstRecordCount"></param>
        /// <returns></returns>
        private bool IsRegistCapiUrlSplited_Manual(DataReceivedEventArgs args, string registCapiBeginParam, string registCapiEndParam, JToken htmlDoc, int firstRecordCount)
        {
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

                        UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);

                    }
                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// 对url进行日期手动拆分
        /// </summary>
        /// <param name="args"></param>
        /// <param name="startDateBeginParam"></param>
        /// <param name="startDateEndParam"></param>
        /// <param name="htmlDoc"></param>
        /// <param name="firstRecordCount"></param>
        /// <returns></returns>
        private bool IsDateDataUrlSplited_Manual(DataReceivedEventArgs args, string startDateBeginParam, string startDateEndParam, JToken htmlDoc, int firstRecordCount)
        {
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

                        UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);

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

                                UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);
                                //if (!urlFilter.Contains(curUrl))//防止重复爬取
                                //{
                                //    //args.urlInfo.UrlString = curUrl;
                                //    //UrlQueue.Instance.EnQueue(args.urlInfo);//添加条件过滤
                                //    AddUrlQueue(args.urlInfo, curUrl);
                                //    urlFilter.Add(curUrl);
                                //    curAddUrlList.Add(curUrl);
                                //}

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

                                UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);
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
            return false;
        }

        /// <summary>
        /// 进行手动获取城市下的区县url切分
        /// </summary>
        /// <param name="args"></param>
        /// <param name="countyCode"></param>
        /// <param name="province"></param>
        /// <returns></returns>
        private bool IsCountyCodeUrlSplited_Manual(DataReceivedEventArgs args, string countyCode, string province, int firstRecordCount)
        {
            if (!string.IsNullOrEmpty(countyCode) || !string.IsNullOrEmpty(province))
            {
                List<string> subCountryList = this.GetSubCountryCodeList(province, countyCode, "2");
                if (subCountryList.Count > 0)
                {
                    int urlAddCount = 0;
                    foreach (string curCountyCode in subCountryList)
                    {
                        string curUrl = this.ReplaceParam(args.Url, "countyCode", countyCode, curCountyCode);

                        UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);
                        //if (!this.urlFilter.Contains(curUrl))
                        //{
                        //    urlAddCount = urlAddCount + 1;
                        //    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));
                        //    this.urlFilter.Add(curUrl);
                        //    this.curAddUrlList.Add(curUrl);
                        //}
                    }
                    if (urlAddCount > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 进行手动获取城市下的城市url切分
        /// </summary>
        /// <param name="args"></param>
        /// <param name="countyCode"></param>
        /// <param name="province"></param>
        /// <returns></returns>
        private List<string> GetCityCountyCodeUrlSplited_Manual(string url, string province)
        {
            var urlResult = new List<string>();
            if (!string.IsNullOrEmpty(province))
            {
                List<string> subCountryList = this.GetSubCountryCodeList(province, "", "1");
                if (subCountryList.Count > 0)
                {
                    foreach (string curCountyCode in subCountryList)
                    {
                        string curUrl = this.ReplaceUrlParamEx(url, "countyCode", curCountyCode);
                        if (!this.urlFilter.Contains(curUrl))
                        {

                            urlResult.Add(curUrl);
                        }
                    }

                }
            }
            return urlResult;
        }

        ///// <summary>
        ///// 当进行省份爬取时，并countryCode为null进行手工城市切分
        ///// </summary>
        ///// <param name="args"></param>
        ///// <param name="countyCode"></param>
        ///// <param name="province"></param>
        ///// <returns></returns>
        //private bool IsCityCountyCodeUrlSplited_Manual(DataReceivedEventArgs args, string province)
        //{
        //    if ( !string.IsNullOrEmpty(province))
        //    {
        //        List<string> subCountryList = this.GetSubCountryCodeList(province, "", "1");//获取城市列表
        //        if (subCountryList.Count > 0)
        //        {
        //            int urlAddCount = 0;
        //            foreach (string curCountyCode in subCountryList)
        //            {
        //                var oldCountryCode = GetUrlParam(args.Url, "countyCode");
        //                string curUrl = this.ReplaceParam(args.Url, "countyCode", oldCountryCode, curCountyCode);
        //                if (!this.urlFilter.Contains(curUrl))
        //                {
        //                    urlAddCount = urlAddCount + 1;
        //                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));
        //                    this.urlFilter.Add(curUrl);
        //                    this.curAddUrlList.Add(curUrl);
        //                }
        //            }
        //            if (urlAddCount > 0)
        //            {
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}
        /// <summary>
        /// 省份url切分，当传入的市城市而进行了全省爬取
        /// </summary>
        /// <param name="args"></param>
        /// <param name="province"></param>
        /// <returns></returns>
        private bool IsEmptyProvinceUrlSplited_Manual(DataReceivedEventArgs args, string province, int firstRecordCount)
        {
            if (string.IsNullOrEmpty(province))
            {
                var allProvince = allCountyCodeList.Where(c => c.Int("type") == 0).ToList();
                var provinceAdd = 0;
                foreach (var provinceObj in allProvince)
                {
                    string curInitUrl = this.ReplaceParam(args.Url, "province", "", provinceObj.Text("code"));
                    var resultUrl = GetCityCountyCodeUrlSplited_Manual(curInitUrl, province);//
                    foreach (var curUrl in resultUrl)
                    {

                        UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);
                        //if (!this.urlFilter.Contains(curUrl))
                        //{
                        //    provinceAdd++;
                        //    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));
                        //    this.urlFilter.Add(curUrl);
                        //    this.curAddUrlList.Add(curUrl);
                        //}
                    }
                }
                if (provinceAdd > 0)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 是否对url进行了人工产业细分
        /// </summary>
        /// <param name="industryCode"></param>
        /// <param name="subIndustryCode"></param>
        /// <returns></returns>
        private bool IsIndustryUrlSplited_Manual(DataReceivedEventArgs args, string industryCode, string subIndustryCode, int firstRecordCount)
        {
            var industryFilter = this.industryFilterChk.Checked;//是否进行产业细分
            if (industryFilter && allFactoryList.Count() > 0 && string.IsNullOrEmpty(subIndustryCode))
            {
                var factoryHasAdd = false;
                var hitSubFactoryList = allSubFactoryList.Where(c => c.Text("parentCode") == industryCode).ToList();
                foreach (var dataValueDic in hitSubFactoryList)//2016
                {

                    var dataValue = dataValueDic.Text("code");
                    var parentCode = dataValueDic.Text("parentCode");
                    if (string.IsNullOrEmpty(parentCode))
                    {
                        parentCode = dataValue;
                    }
                    if (string.IsNullOrEmpty(dataValue)) continue;
                    //var curUrl = args.Url.Replace("&industryV3=&industryCode=", string.Format("&industryV3={0}&industryCode={1}", dataValue, parentCode));
                    var curUrl = ReplaceUrlParamEx(args.Url, "industryV3", dataValue);
                    curUrl = ReplaceUrlParamEx(curUrl, "industryCode", parentCode);
                    UrlSplitedEnqueue(args.urlInfo, curUrl, firstRecordCount);
                    //if (!urlFilter.Contains(curUrl))//防止重复爬取
                    //{
                    //    factoryHasAdd = true;
                    //    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                    //    urlFilter.Add(curUrl);
                    //    curAddUrlList.Add(curUrl);
                    //}
                }
                //有添加才返回
                if (factoryHasAdd)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion


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
        private bool SplitModeActive(UrlSpliteMode _urlSplitMode, string argUrl, string startDateBeginParam, string startDateEndParam, string registCapiBeginParam, string registCapiEndParam, string subIndustryCode, JToken htmlDoc, int firstRecordCount)
        {
            bool dateSplitStatus;
            bool capiSplitStatus;
            bool subFactorySplitStatus;
            //考虑每个关键字有一定的分解模式，当某个关键字模式连续3次个数一样，则切换其他分解模式
            switch (_urlSplitMode)
            {
                case UrlSpliteMode.DateFirst://日期优先
                    dateSplitStatus = SplitUrlByDateAgain(argUrl, startDateBeginParam, startDateEndParam, firstRecordCount);
                    if (dateSplitStatus == true) return dateSplitStatus;

                    ///再次分解注册资金
                    capiSplitStatus = SplitUrlByRegCapiAgain(argUrl, registCapiBeginParam, registCapiEndParam, firstRecordCount);
                    if (capiSplitStatus == true) return capiSplitStatus;

                    ///再次分解子类
                    subFactorySplitStatus = SplitUrlBySubFactory(argUrl, subIndustryCode, htmlDoc, firstRecordCount);
                    if (subFactorySplitStatus == true) return subFactorySplitStatus;
                    break;
                case UrlSpliteMode.RecpiFirst://注册资金优先
                                              ///再次分解注册资金
                    capiSplitStatus = SplitUrlByRegCapiAgain(argUrl, registCapiBeginParam, registCapiEndParam, firstRecordCount);
                    if (capiSplitStatus == true) return capiSplitStatus;
                    ///再次分解子类
                    subFactorySplitStatus = SplitUrlBySubFactory(argUrl, subIndustryCode, htmlDoc, firstRecordCount);
                    if (subFactorySplitStatus == true) return subFactorySplitStatus;
                    dateSplitStatus = SplitUrlByDateAgain(argUrl, startDateBeginParam, startDateEndParam, firstRecordCount);
                    if (dateSplitStatus == true) return dateSplitStatus;

                    break;
                case UrlSpliteMode.SubFacortyFist://注册资金优先  ///再次分解注册资金

                    subFactorySplitStatus = SplitUrlBySubFactory(argUrl, subIndustryCode, htmlDoc, firstRecordCount);
                    if (subFactorySplitStatus == true) return subFactorySplitStatus;

                    ///再次分解子类
                    dateSplitStatus = SplitUrlByDateAgain(argUrl, startDateBeginParam, startDateEndParam, firstRecordCount);
                    if (dateSplitStatus == true) return dateSplitStatus;
                    capiSplitStatus = SplitUrlByRegCapiAgain(argUrl, registCapiBeginParam, registCapiEndParam, firstRecordCount);
                    if (capiSplitStatus == true) return capiSplitStatus;

                    break;
            }
            return false;
        }

        #endregion
        /// <summary>
        /// 迭代分割
        /// </summary>
        /// <param name="args"></param>
        /// <param name="htmlDoc"></param>
        /// <param name="firstRecordCount"></param>
        /// <returns></returns>
        private bool IterationSplitModeActive(DataReceivedEventArgs args, JToken htmlDoc, int firstRecordCount)
        {

            //registcapiNew 注册资本//startdateNew 成立日期//statuscodeNew 企业状态 industrycodeNew 行业门类
            var registCapiBeginParam = GetUrlParam(args.Url, "registCapiBegin");
            var registCapiEndParam = GetUrlParam(args.Url, "registCapiEnd");
            var statusCodeParam = GetUrlParam(args.Url, "statusCode");
            var startDateBeginParam = GetUrlParam(args.Url, "startDateBegin");
            var startDateEndParam = GetUrlParam(args.Url, "startDateEnd");
            var industryCode = GetUrlParam(args.Url, "industryCode");
            var subIndustryCode = GetUrlParam(args.Url, "subIndustryCode");
            var countyCode = GetUrlParam(args.Url, "countyCode");//新版城市代码
            var province = GetUrlParam(args.Url, "province");//新版城市省份代码
            var citycode = GetUrlParam(args.Url, "cityCode");//新版城市省份代码
            var searchKey = GetUrlParam(args.Url, "searchKey");
            var hitSearchKeyConditionObj = searchKeyConditonList.Where(c => c.searchKey == searchKey).FirstOrDefault();
            if (hitSearchKeyConditionObj == null)
            {
                hitSearchKeyConditionObj = new SearchKeyCondition() { recordCount = firstRecordCount, searchKey = searchKey, mode = urlSplitMode };
                searchKeyConditonList.Add(hitSearchKeyConditionObj);
            }
            else
            {

                if (hitSearchKeyConditionObj.hitCount >= 100)
                {
                    return false;
                }
                if (hitSearchKeyConditionObj.recordCount == firstRecordCount)
                {
                    hitSearchKeyConditionObj.hitCount++;
                }
                else
                {
                    hitSearchKeyConditionObj.recordCount = firstRecordCount;
                    hitSearchKeyConditionObj.hitCount = 0;
                }
            }
            UrlSpliteMode _urlSplitMode = this.urlSplitMode;



            DateTime _startDateBegin;
            DateTime _startDateEnd;
            TimeSpan span;
            try
            {
                if (string.IsNullOrEmpty(startDateBeginParam))
                {
                    startDateBeginParam = "18800101";
                }
                if (string.IsNullOrEmpty(startDateEndParam))
                {
                    startDateEndParam = DateTime.Now.ToString("yyyyMMdd");
                }
                if (startDateBeginParam.Length == 4)
                {
                    startDateBeginParam = $"{startDateBeginParam}0101";
                }
                if (startDateEndParam.Length == 4)
                {
                    startDateEndParam = $"{startDateEndParam}1231";
                }
                _startDateBegin = DateTime.ParseExact(startDateBeginParam, "yyyyMMdd", Thread.CurrentThread.CurrentCulture);
                _startDateEnd = DateTime.ParseExact(startDateEndParam, "yyyyMMdd", Thread.CurrentThread.CurrentCulture);
                span = _startDateEnd - _startDateBegin;
            }
            catch (Exception ex)
            {
                var dateStr = $"{startDateBeginParam}{startDateEndParam}";
                throw new Exception("SplitModeActiveBegin" + ex.Message + dateStr);
            }

            try
            {
                if ((span.Days <= 30) || (hitSearchKeyConditionObj.hitCount >= 5))
                {
                    if (registCapiBeginParam != registCapiEndParam)
                    {
                        _urlSplitMode = this.GetNextSplitMode(hitSearchKeyConditionObj.mode);
                    }

                }
                bool splitStatus = this.SplitModeActive(_urlSplitMode, args.Url, startDateBeginParam, startDateEndParam, registCapiBeginParam, registCapiEndParam, subIndustryCode, htmlDoc, firstRecordCount);
                if (splitStatus)
                {
                    return splitStatus;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("SplitModeActiveEnd" + ex.Message);
            }
            return true;
        }
        /// <summary>
        /// url再次分解时间
        /// </summary>
        /// <param name="argUrl"></param>
        /// <param name="startDateBeginParam"></param>
        /// <param name="startDateEndParam"></param>
        /// <returns></returns>
        private bool SplitUrlByDateAgain(string argUrl, string startDateBeginParam, string startDateEndParam, int firstRecordCount)
        {
            try
            {
                #region 此处再次分解时间
                var oldStartDateBeginParam = GetUrlParam(argUrl, "startDateBegin");
                var oldStartDateEndParam = GetUrlParam(argUrl, "startDateEnd");
               
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
                               // _BegincurUrl = ReplaceUrlParam(_BegincurUrl, "startDateEnd", oldStartDateEndParam, startDateBeginParam);
                                _BegincurUrl = ReplaceUrlParamEx(_BegincurUrl, "startDateEnd", startDateBeginParam);
                                UrlSplitedEnqueue(new UrlInfo(argUrl), _BegincurUrl, firstRecordCount);
                                //AddUrlQueue(_BegincurUrl);
                                //curAddUrlList.Add(_BegincurUrl);
                                var _endCurUrl = argUrl;
                               // _endCurUrl = ReplaceUrlParam(_endCurUrl, "startDateBegin", oldStartDateBeginParam, startDateEndParam);
                                _endCurUrl = ReplaceUrlParamEx(_endCurUrl, "startDateBegin", startDateEndParam);
                                UrlSplitedEnqueue(new UrlInfo(argUrl), _endCurUrl, firstRecordCount);
                                //AddUrlQueue(_endCurUrl);
                                //curAddUrlList.Add(_endCurUrl);
                                hasAdd = true;
                            }
                            else
                            {
                                ///时间只能坐落在具体日期上
                                for (var tempDate = _startDateBegin; tempDate <= _startDateEnd; tempDate = tempDate.AddDays(1))
                                {

                                    var _curUrl = argUrl;
                                   // _curUrl = _curUrl.Replace("&startDateBegin=" + oldStartDateBeginParam, string.Format("&startDateBegin={0}", tempDate.ToString("yyyyMMdd")));
                                    _curUrl = ReplaceUrlParamEx(_curUrl, "startDateBegin", tempDate.ToString("yyyyMMdd"));
                                  //  _curUrl = _curUrl.Replace("&startDateEnd=" + oldStartDateEndParam, string.Format("&startDateEnd={0}", tempDate.ToString("yyyyMMdd")));
                                    _curUrl = ReplaceUrlParamEx(_curUrl, "startDateEnd", tempDate.ToString("yyyyMMdd"));
                                    if (UrlSplitedEnqueue(new UrlInfo(argUrl), _curUrl, firstRecordCount))
                                    {
                                        if (!hasAdd)
                                        {
                                            hasAdd = true;
                                        }
                                    }
                                    //if (!urlFilter.Contains(_curUrl))//防止重复爬取
                                    //{

                                    //    UrlQueue.Instance.EnQueue(new UrlInfo(_curUrl));//添加条件过滤
                                    //    urlFilter.Add(_curUrl);
                                    //    curAddUrlList.Add(_curUrl);
                                    //    if (!hasAdd)
                                    //    {
                                    //        hasAdd = true;
                                    //    }
                                    //}
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
                              //  _curUrl = _curUrl.Replace("&startDateBegin=" + oldStartDateBeginParam, string.Format("&startDateBegin={0}", _startDateBegin.ToString("yyyyMMdd")));
                                _curUrl = ReplaceUrlParamEx(_curUrl, "startDateBegin", _startDateBegin.ToString("yyyyMMdd"));

                                //_curUrl = _curUrl.Replace("&startDateEnd=" + oldStartDateEndParam, string.Format("&startDateEnd={0}", tempCapEnd.ToString("yyyyMMdd")));
                                _curUrl = ReplaceUrlParamEx(_curUrl, "startDateEnd", tempCapEnd.ToString("yyyyMMdd"));

                                if (UrlSplitedEnqueue(new UrlInfo(argUrl), _curUrl, firstRecordCount))
                                {
                                    if (!hasAdd)
                                    {
                                        hasAdd = true;
                                    }
                                }
                                //if (!urlFilter.Contains(_curUrl))//防止重复爬取
                                //{
                                //    UrlQueue.Instance.EnQueue(new UrlInfo(_curUrl));//添加条件过滤
                                //    urlFilter.Add(_curUrl);
                                //    curAddUrlList.Add(_curUrl);
                                //    if (!hasAdd)
                                //    {
                                //        hasAdd = true;
                                //    }
                                //}
                                _startDateBegin = tempCapEnd.AddDays(1);

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
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
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
        private bool SplitUrlByRegCapiAgain(string argUrl, string registCapiBeginParam, string registCapiEndParam, int firstRecordCount)
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
                            //_BegincurUrl = ReplaceUrlParam(_BegincurUrl, "registCapiEnd", registCapiEndParam, registCapiBeginParam);
                            _BegincurUrl = ReplaceUrlParamEx(_BegincurUrl, "registCapiEnd", registCapiBeginParam);
                            UrlSplitedEnqueue(new UrlInfo(argUrl), _BegincurUrl, firstRecordCount);
                            //AddUrlQueue(_BegincurUrl);
                            //curAddUrlList.Add(_BegincurUrl);
                            hasAdd = true;
                        }

                        if (registCapiEndParam != "")
                        {
                            var _endCurUrl = argUrl;
                            //_endCurUrl = ReplaceUrlParam(_endCurUrl, "registCapiBegin", registCapiBeginParam, registCapiEndParam);
                            _endCurUrl = ReplaceUrlParamEx(_endCurUrl, "registCapiBegin", registCapiEndParam);
                            UrlSplitedEnqueue(new UrlInfo(argUrl), _endCurUrl, firstRecordCount);
                            //AddUrlQueue(_endCurUrl);
                            //curAddUrlList.Add(_endCurUrl);
                            hasAdd = true;
                        }
                    }
                    else
                    {
                        //6-8= 6-7 7-8  需要 6,7,8 需要进行验证
                        for (var tempCapi = _registCapiBegin; tempCapi <= _registCapiEnd; tempCapi++)
                        {


                            var _curUrl = argUrl;
                            //_curUrl = _curUrl.Replace("&registCapiBegin=" + registCapiBeginParam, string.Format("&registCapiBegin={0}", tempCapi));
                            _curUrl = ReplaceUrlParamEx(_curUrl, "registCapiBegin", tempCapi.ToString());
                           // _curUrl = _curUrl.Replace("&registCapiEnd=" + registCapiEndParam, string.Format("&registCapiEnd={0}", tempCapi));
                            _curUrl = ReplaceUrlParamEx(_curUrl, "registCapiEnd", tempCapi.ToString());
                            if (UrlSplitedEnqueue(new UrlInfo(argUrl), _curUrl, firstRecordCount))
                            {
                                if (!hasAdd)
                                {
                                    hasAdd = true;
                                }
                            }
                            //if (!urlFilter.Contains(_curUrl))//防止重复爬取
                            //{
                            //    UrlQueue.Instance.EnQueue(new UrlInfo(_curUrl));//添加条件过滤
                            //    urlFilter.Add(_curUrl);
                            //    curAddUrlList.Add(_curUrl);
                            //    if (!hasAdd)
                            //    {
                            //        hasAdd = true;
                            //    }
                            //}
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
                        //_curUrl = _curUrl.Replace("&registCapiBegin=" + registCapiBeginParam, string.Format("&registCapiBegin={0}", _registCapiBegin));
                        _curUrl = ReplaceUrlParamEx(_curUrl, "registCapiBegin", _registCapiBegin.ToString());
                        //_curUrl = _curUrl.Replace("&registCapiEnd=" + registCapiEndParam, string.Format("&registCapiEnd={0}", tempCapEnd));
                        _curUrl = ReplaceUrlParamEx(_curUrl, "registCapiEnd", tempCapEnd.ToString());
                        if (UrlSplitedEnqueue(new UrlInfo(argUrl), _curUrl, firstRecordCount))
                        {
                            if (!hasAdd)
                            {
                                hasAdd = true;
                            }
                        }
                        //    if (!urlFilter.Contains(_curUrl))//防止重复爬取
                        //{
                        //    UrlQueue.Instance.EnQueue(new UrlInfo(_curUrl));//添加条件过滤
                        //    urlFilter.Add(_curUrl);
                        //    curAddUrlList.Add(_curUrl);
                        //    if (!hasAdd)
                        //    {
                        //        hasAdd = true;
                        //    }
                        //}
                        _registCapiBegin = tempCapEnd + 1;

                    }
                }
                #endregion

                if (hasAdd)//此处不想等时候扔不等于已经有url添加了此处需要跳出
                {
                    return true;
                }

            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
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
        private bool SplitUrlBySubFactory(string argUrl, string subIndustryCode, JToken htmlDoc, int firstRecordCount)
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
                    //var curUrl = argUrl.Replace("&subIndustryCode=", string.Format("&subIndustryCode={0}", dataValue));
                    var curUrl = ReplaceUrlParamEx(argUrl, "subIndustryCode", dataValue);
                    UrlSplitedEnqueue(new UrlInfo(argUrl), curUrl, firstRecordCount);
                    //if (!urlFilter.Contains(curUrl))//防止重复爬取
                    //{
                    //    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));//添加条件过滤
                    //    urlFilter.Add(curUrl);
                    //    curAddUrlList.Add(curUrl);
                    //}

                }
                return true;

            }
            return false;
            #endregion
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

            ///优先查看数据源中是否已经存在数据
            Dictionary<string, int> result = new Dictionary<string, int>();
            var hitGourp = groupItems.Where(c => c["Key"].ToString() == name).FirstOrDefault();
            if (hitGourp != null)
            {
                foreach (var item in hitGourp["Items"])
                {
                    if (result.ContainsKey(item["Value"].ToString())) continue;//防止重复添加

                    var curCount = 0;
                    if (int.TryParse(item["Count"].ToString(), out curCount))
                    {
                        if (item["Count"].ToString() != "0")
                        {
                            result.Add(item["Value"].ToString(), curCount);

                        }
                    }
                }
                return result;
            }
            // List<string> result = new List<string>();

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
                                result.Add("2-5", 1);
                                result.Add("5-10", 1);
                                result.Add("10-20", 1);
                                result.Add("20-30", 1);
                                result.Add("30-40", 1);
                                result.Add("40-50", 1);
                                result.Add("50-100", 1);
                                result.Add("100-200", 1);
                                result.Add("200-300", 1);
                                result.Add("300-400", 1);
                                result.Add("400-500", 1);
                                result.Add("500-600", 1);
                                result.Add("600-700", 1);
                                result.Add("700-800", 1);
                                result.Add("800-900", 1);
                                result.Add("900-1000", 1);
                                result.Add("1000-2000", 1);
                                result.Add("2000-3000", 1);
                                result.Add("3000-4000", 1);
                                result.Add("4000-5000", 1);
                                result.Add("5000-10000", 1);
                                result.Add("10000-0", 1);
                            }
                            else
                            {
                                result.Add("0-1", 1);
                                result.Add("1-2", 1);
                                result.Add("2-10", 1);
                                result.Add("10-100", 1);
                                result.Add("100-500", 1);
                                result.Add("500-1000", 1);
                                result.Add("1000-5000", 1);
                                result.Add("5000-0", 1);
                                result.Add("5000-10000", 1);
                                result.Add("10000-0", 1);
                            }
                            #endregion
                            break;
                        case "startdateyear":

                            var avgCount = DateTime.Now.Year - 1990;
                            for (var year = 1990; year <= DateTime.Now.Year; year++)
                            {
                                result.Add(year.ToString(), firstRecordCount / avgCount);
                            }

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

                            hitGourp = groupItems.Where(c => c["Key"].ToString() == name).FirstOrDefault();
                            if (hitGourp != null)
                            {
                                foreach (var item in hitGourp["Items"])
                                {
                                    if (result.ContainsKey(item["Value"].ToString())) continue;//防止重复添加
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
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                {
                    ShowMessageInfo("12");
                }
                return result;
            }
        }

        #endregion

        #endregion
        #region HTTP模拟请求
        /// <summary>
        /// 获取QCCpost 搜索关键字
        /// </summary>
        /// <param name="keyWord"></param>
        /// <returns></returns>
        public HttpResult GetPostData(UrlInfo curUrlObj, string refer = "", bool useProxy = true)
        {
            http = new SimpleCrawler.HttpHelper();
            if (IsAPP(searchType))
            {
                return GetPostDataAPP(curUrlObj, false);
            }
            if (searchType == SearchType.EnterpriseGuidByKeyWordEnhence)
            {
                return GetPostDataKeyWordEnhence(curUrlObj, "", true);
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
        /// enhence获取关键字guid
        /// </summary>
        /// <param name="curUrlObj"></param>
        /// <param name="refer"></param>
        /// <param name="useProxy"></param>
        /// <returns></returns>
        public HttpResult GetPostDataKeyWordEnhence(UrlInfo curUrlObj, string refer = "", bool useProxy = true)
        {
            //创建Httphelper参数对象

            //curUrlObj.PostData = string.Format("key=安徽省合肥市荣事达大道568号511室 程华&type=undefined");
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
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3325.146 Safari/537.36",//用户的浏览器类型，版本，操作系统     可选项有默认值
                Referer = "http://www.qichacha.com/",//来源URL     可选项
                Postdata = curUrlObj.PostData,
                Allowautoredirect = true,
                Cookie = Settings.SimulateCookies,
                KeepAlive = true,

            };
            //item.WebProxy = GetWebProxy();
            item.PostEncoding = System.Text.Encoding.GetEncoding("utf-8");
            var result = http.GetHtml(item);
            if (string.IsNullOrEmpty(result.Html))
            {

            }
            return result;
        }


        public HttpResult GetHttpHtmlAPP(UrlInfo curUrlObj, bool tryAgain = false, bool forceAccesstoken = false)
        {
            try
            {
                //刚切换账号前N个url 加入备用队列
                if (IsMoreDetailInfo)
                {
                    QCCEnterpriseHelper qccHelper = new QCCEnterpriseHelper();
                    DeviceInfo info = new DeviceInfo
                    {
                        accessToken = Settings.AccessToken,
                        appId = Settings.AppId,
                        deviceId = Settings.DeviceId,
                        refreshToken = Settings.RefleshToken,
                        sign = Settings.sign,
                        timestamp = Settings.timestamp
                    };
                    qccHelper.curDeviceInfo = info;
                    qccHelper.webProxy = this.GetWebProxy();
                    return qccHelper.GetEnterpriseBackDetailInfo(curUrlObj.UniqueKey);
                }

                http = new SimpleCrawler.HttpHelper();
                var url = FixUrlSignStr(curUrlObj);
                var item = new SimpleCrawler.HttpItem()
                {
                    URL = url,
                    Method = "get",//URL     可选项 默认为Get   
                                   // ContentType = "text/html",//返回类型    可选项有默认值 
                    UserAgent = "okhttp/3.6.0",
                    ContentType = "application/x-www-form-urlencoded",
                };

                // item.Header.Add("Content-Type", "application/x-www-form-urlencoded");
                // hi.HeaderSet("Content-Length","154");
                // hi.HeaderSet("Connection","Keep-Alive");

                item.Header.Add("Accept-Encoding", "gzip");
                if (forceAccesstoken || string.IsNullOrEmpty(Settings.AccessToken))
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
                        if (IsNeedLogin(searchType))
                        {
                            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isInvalid", "1"), Query = Query.EQ("name", Settings.LoginAccount), Name = DataTableAccount, Type = StorageType.Update });
                        }
                         StartDBChangeProcessQuick();
                        if (autoChangeAccountCHK.Checked)
                        {
                            //ChangeIp();//更换IP后进行
                            AutoChangeAccount();
                        }
                        else
                        {
                            timerStop();
                            // this.checkBoxGuard.Checked = false;
                            //guardTimer.Stop();
                        }

                    }
                    else
                    if (result.Html.Contains("失效") || result.Html.Contains("<html><script>") || result.Html.Contains("签名失败") || result.Html.Contains("非法"))
                    {

                        if (result.Html.Contains("失效"))
                        {
                            //GetAccessToken();
                            return GetHttpHtmlAPP(curUrlObj, forceAccesstoken: true);
                        }
                        if (IsSameUrlSign(url) == false)//只尝试一次
                        {
                            ShowMessageInfo("当前url与settings.sign不一致 进行重试");
                            return GetHttpHtmlAPP(curUrlObj);
                        }
                        timerStop();
                        //var tempResult = RefreshToken();
                        if (ContinueMethodByBusyGear("HttpRequestChangeAccount", 3))
                        {
                            QuickChangeDeviceAccount();
                            // AutoChangeAccount();
                            timerStart();
                        }
                    }
                    else if (result.Html.Contains("<html><script>"))
                    {
                        timerStop();
                       
                    }
                }
                return result;
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

            }
            //return new HttpResult() { StatusCode = HttpStatusCode.OK, Html = result.Html };
            return new HttpResult();
            //return GetPostDataFix(curUrlObj);

        }

        public HttpResult GetPostDataLogin(UrlInfo curUrlObj, string refer = "", bool useProxy = true)
        {
            if (IsAPP(searchType))
            {
                return GetPostDataAPP(curUrlObj, false);
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
        /// 2020.3.20 修改详细信息的爬取需要账号切换
        /// </summary>
        /// <param name="searchType"></param>
        /// <returns></returns>
        private bool IsAPP(SearchType searchType)
        {
            return searchType == SearchType.EnterpriseGuidByKeyWord_APP || searchType == SearchType.UpdateEnterpriseInfoAPP || searchType == SearchType.ADDEnterpriseAPP || searchType == SearchType.EnterprisePositionDetailByGuid_APP|| searchType == SearchType.EnterpriseBackGroundDetailByGuid_APP;
           // return searchType == SearchType.EnterpriseGuidByKeyWord_APP ||  searchType == SearchType.ADDEnterpriseAPP;
        }

        //是否需要登陆
        private bool IsNeedLogin(SearchType searchType)
        {
             // return  searchType == SearchType.UpdateEnterpriseInfoAPP || searchType == SearchType.ADDEnterpriseAPP;
             return searchType == SearchType.ADDEnterpriseAPP;
        }

       /// <summary>
            /// 返回请求数据
            /// </summary>
            /// <param name="curUrlObj"></param>
            /// <returns></returns>
       public HttpResult GetHttpHtml(UrlInfo curUrlObj, string refer = "")
        {
            http = new SimpleCrawler.HttpHelper();
            if (IsAPP(searchType))
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
                var result = GetHtmlPostDataFix(curUrlObj);
               // var result = GetPostDataFix(curUrlObj);//

                return result;
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
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
            return GetHtmlPostDataFix(curUrlObj);
            //string.Format("refreshToken=f128619e442a6efe44c1544b4c926824&timestamp=1473757386869&appId=80c9ef0fb86369cd25f90af27ef53a9e&sign=a5ae576bcddcba5df634f041995e45cd54b255e6");
            hi.Url = curUrlObj.UrlString.ToString();
            //hi.Refer = $"{domainUrl}";
            hi.PostData = curUrlObj.PostData;
            hi.UserAgent = "okhttp/3.6.0";
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
            if (ho.IsOK&& CheckHtmlPostResult(ho.TxtData))
            {
                 
                return new HttpResult() { StatusCode = HttpStatusCode.OK, Html = ho.TxtData };
            }
            else
            {
                return new HttpResult() { StatusCode = HttpStatusCode.Forbidden };
            }

        }
        /// <summary>
        /// 判断返回的数据
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private bool CheckHtmlPostResult(string html)
        {
            if (!html.Contains("成功"))
            {
                if (html.Contains("异常"))
                {
                    timerStop();
                    ShowMessageInfo(html, true);
                    //10秒内不能重复执行
                    if (ContinueMethodByBusyGear("DeviceIsInvalidSetting", 5))
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
                        // this.checkBoxGuard.Checked = false;
                        //guardTimer.Stop();
                    }

                }
                else
                if (html.Contains("失效") || html.Contains("签名失败") || html.Contains("非法请求"))
                {
                    timerStop();
                    if (ContinueMethodByBusyGear("DeviceRefreshToken", 5))//防止重复率刷新请求
                    {
                        var result = RefreshToken();
                        if (SearchType.ADDEnterpriseAPP == searchType)
                        {
                            if (ContinueMethodByBusyGear(" AppAutoLoin", 30))
                            {
                                AppAutoLoin();
                            }
                        }
                        if (result.Contains("成功"))
                        {
                            timerStart();
                        }
                    }
                }
                return false;
            }
            return true;
              
        }
        public HttpResult GetHtmlPostDataFix(UrlInfo curUrlObj)
        {

            http = new SimpleCrawler.HttpHelper();
            var url = curUrlObj.UrlString;
            var item = new SimpleCrawler.HttpItem()
            {
                URL = url,
                Method = "get",//URL     可选项 默认为Get   
                               // ContentType = "text/html",//返回类型    可选项有默认值 
                UserAgent = "okhttp/3.6.0",
                ContentType = "application/x-www-form-urlencoded",
            };
            if (!string.IsNullOrEmpty(curUrlObj.PostData))
            {
                // item.PostEncoding = System.Text.Encoding.GetEncoding("utf-8");
                item.Method = "post";
                item.Postdata = curUrlObj.PostData;
            }
             
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
            CheckHtmlPostResult(result.Html);
            return result;
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
                if (true||string.IsNullOrEmpty(Settings.RefleshToken))
                {

                    return GetAccessToken();
                }
                ShowMessageInfo("开始进行RefreshToken", true);

                hi.Url = QCCRefreshTokenUrl;
                //hi.Refer = $"{domainUrl}";
                hi.PostData = string.Format("refreshToken={0}&timestamp={1}&appId={2}&sign={3}", Settings.RefleshToken, Settings.timestamp, Settings.AppId, Settings.sign); ;
                hi.UserAgent = "okhttp/3.6.0";
                hi.HeaderSet("Content-Type", "application/x-www-form-urlencoded");
                if (USEWEBPROXY)
                {
                    hi.CurlObject.SetOpt(LibCurlNet.CURLoption.CURLOPT_PROXY, GetWebProxyString());
                }
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
            return GetAccessToken_abort();
#pragma warning disable CS0162 // 检测到无法访问的代码
            hi.Url = QCCAccessTokenUrl;
#pragma warning restore CS0162 // 检测到无法访问的代码
            //hi.Refer = $"{domainUrl}";
            hi.PostData = string.Format("appId={0}&deviceId={1}&version=10.0.4&deviceType=android&os=&timestamp={2}&sign={3}", Settings.AppId, Settings.DeviceId, Settings.timestamp, Settings.sign);

            hi.UserAgent = "okhttp/3.6.0";
            hi.HeaderSet("Content-Type", "application/x-www-form-urlencoded");
            // hi.HeaderSet("Content-Length","154");
            // hi.HeaderSet("Connection","Keep-Alive");
            hi.HeaderSet("Accept-Encoding", "gzip");
            //hi.HeaderSet("Authorization", string.Format("Bearer {0}", Settings.AccessToken));
            //if (USEWEBPROXY)
            //{
            //    hi.CurlObject.SetOpt(LibCurlNet.CURLoption.CURLOPT_PROXY, GetWebProxyString());
            //}
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
                //this.checkBoxGuard.Checked = false;
                // guardTimer.Stop();
                GetAccessRetryTimes = 10;
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
            var url = QCCAccessTokenUrl;
            var postData = string.Format("appId={0}&deviceId={1}&version=10.0.4&deviceType=android&os=&timestamp={2}&sign={3}", Settings.AppId, Settings.DeviceId, Settings.timestamp, Settings.sign);

            //var result = GetPostData(new UrlInfo(url) { PostData = postData });
            SimpleCrawler.HttpItem item = new SimpleCrawler.HttpItem()
            {
                URL = url,//URL     必需项    

                ContentType = "application/x-www-form-urlencoded",//返回类型    可选项有默认值 

                Timeout = 1500,
                //Accept = "*/*",
                // Encoding = null,//编码格式（utf-8,gb2312,gbk）     可选项 默认类会自动识别
                //Encoding = Encoding.Default,
                Method = "POST",//URL     可选项 默认为Get
                                //Timeout = 100000,//连接超时时间     可选项默认为100000
                                //ReadWriteTimeout = 30000,//写入Post数据超时时间     可选项默认为30000
                                //IsToLower = false,//得到的HTML代码是否转成小写     可选项默认转小写
                                //Cookie = "",//字符串Cookie     可选项
                UserAgent = "okhttp/3.6.0",//用户的浏览器类型，版本，操作系统     可选项有默认值
                                           //Referer = "app.",//来源URL     可选项
                Postdata = postData,
                // Allowautoredirect = true,
                // Cookie = Settings.SimulateCookies
            };


            item.Header.Add("Accept-Encoding", "gzip");
            // item.Header.Add("Host", "app.");
            if (!string.IsNullOrEmpty(Settings.AccessToken))
            {
                item.Header.Add("Authorization", Settings.AccessToken);
            }

            //item.Header.Add("Accept-Language", "zh-CN");
            // item.Header.Add("charset", "UTF-8");
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
            //this.checkBoxGuard.Checked = false;
            //guardTimer.Stop();
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

                var url = QCCRefreshTokenUrl;
                //hi.Refer = $"{domainUrl}";
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
                    UserAgent = "okhttp/3.6.0",//用户的浏览器类型，版本，操作系统     可选项有默认值
                                               //Referer = "app.q",//来源URL     可选项
                    Postdata = postData,
                    // Allowautoredirect = true,
                    // Cookie = Settings.SimulateCookies
                };


                item.Header.Add("Accept-Encoding", "gzip");
                // item.Header.Add("Host", "app.et");
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
        #region 基础方法 
        /// <summary>
        /// 是否新的版本
        /// </summary>
        /// <returns></returns>
        private bool IsQCCUrlNewVersion()
        {
            if (QCCSearchUrl.Contains("appv1"))
            {
                return false;
            }
            if (QCCSearchUrl.Contains("appv2") && (QCCSearchUrl.Contains("app/v2") || QCCSearchUrl.Contains("app/v1")))
            {
                return false;
            }
            return true;

        }
        public string FixDocStr(string str)
        {
            str = str.Replace("<em>", "").Replace("</em>", "");
            return str;
        }
        public string FixRegionStr(string str)
        {
            var tempStr = str.Replace("市", "").Replace("区", "").Replace("县", "").Replace("自治区", "");
            if (!string.IsNullOrEmpty(tempStr))
            {
                return tempStr;
            }
            return str;
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
        private bool AddUrlQueue(UrlInfo urlInfo)
        {

            UrlQueue.Instance.EnQueue(urlInfo);//添加条件过滤
            return true;
        }

        private bool AddUrlQueue(UrlInfo urlInfo, string url)
        {
            var newUrlInfo = new UrlInfo(url)
            {
                Authorization = urlInfo.Authorization,
                Depth = urlInfo.Depth,
                operation = urlInfo.operation,
                PostData = urlInfo.PostData,
                UrlSplitTimes = urlInfo.UrlSplitTimes,
                script = urlInfo.script,
                Status = urlInfo.Status,
                UniqueKey = urlInfo.UniqueKey,
                UrlString = url
            };
            UrlQueue.Instance.EnQueue(newUrlInfo);//添加条件过滤
            return true;
        }
        /// <summary>
        /// 增加到重试队列
        /// </summary>
        /// <param name="urlInfo"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        private bool AddRetryUrlQueue(UrlInfo urlInfo, string url)
        {
            urlInfo.Depth = urlInfo.Depth + Settings.MaxReTryTimes / 3;
            UrlRetryQueue.Instance.EnQueue(urlInfo);//重试
            return true;
        }


        /// <summary>
        /// 拆分的url是否正常插入到待请求队列
        /// </summary>
        /// <param name="url"></param>
        /// <param name="curInitUrl"></param>
        /// <returns></returns>
        private bool UrlSplitedEnqueue(UrlInfo urlInfo, string newUrl, int recordCount = 0)
        {
            if (!urlFilter.Contains(newUrl))//防止重复爬取
            {
                AddUrlQueue(urlInfo, newUrl);
                urlFilter.Add(newUrl);
                AddUrlSplitTree(urlInfo.UrlString, newUrl, recordCount);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 添加到跟踪树中 但关键字才生效
        /// </summary>
        /// <param name="parentUrl"></param>
        /// <param name="childUrl"></param>
        /// <param name="recordCount"></param>
        private void AddUrlSplitTree(string parentUrl, string childUrl, int recordCount = 0)
        {
            if (!singalKeyWordCHK.Checked) return;
            try
            {
                parentUrl = Toolslib.Str.Sub(GetQueryString(parentUrl), "&isSortAsc=false&", "");
                childUrl = Toolslib.Str.Sub(GetQueryString(childUrl), "&isSortAsc=false&", "");
                var hitUrlNode = globalUrlSplitTreeNode.FindSimpleTreeNode(c => c.Data.Text("name") == parentUrl);
                if (hitUrlNode == null)
                {
                    hitUrlNode = globalUrlSplitTreeNode;
                }
                hitUrlNode.AddChild(new BsonDocument().Add("name", childUrl).Add("parentRecordCount", recordCount));
            }
            catch (Exception ex)
            {
                ShowMessageInfo(ex.Message);
            }
        }

        public int UrlQueueCount()
        {
            return UrlQueue.Instance.Count + UrlRetryQueue.Instance.Count;
        }

        /// <summary>
        /// 是否需要自动跳过下一个关键字
        /// </summary>
        /// <returns></returns>
        private void ProcessCanAutoPassKeyWord(Tuple<int, int> curKeyWordCount, string keyWord)
        {
            try
            {
                var tempStr = curKeyWordCount.ToString();
                //var addCount = int.Parse(Regex.Replace(tempStr, @"(\d+)(\.\d+)?", "$1")); //输出：100 10000 23
                //var existCountStr = Regex.Replace(tempStr, @"(\d+)(\.\d+)?", "$2").Replace(".", "");
                //var existCount = 0;
                //if (!string.IsNullOrEmpty(existCountStr))
                //{
                //    int.TryParse(existCountStr, out existCount); //输出：100 10000 23
                //}
                var addCount = curKeyWordCount.Item1;
                var existCount = curKeyWordCount.Item2;
                if (CanAutoPassKeyWord(addCount, existCount, UrlQueueCount(), StringQueue.Instance.Count))
                {
                    needPassKeyWord = true;
                    if (autoPassKeyWordChk.Checked == true)
                    {
                        autoPassKeyWordChk.BeginInvoke(new Action(() =>
                        {

                            PassKeyWord();
                        }));
                    }
                }
                else
                {
                    needPassKeyWord = false;
                }
            }
            catch (Exception ex)
            {
                ShowMessageInfo(ex.Message);
            }
        }
        /// <summary>
        /// 是否需要自动跳过下一个关键字
        /// 当关键字小于700 并且 增加个数
        /// 使用机器学习应用 下一个关键字如何进行使用
        /// 计入 每个关键字运行10秒后开始进行判断防止刚开始数量较少
        /// 2020.1.14增加长时间未新增情况下需要跳过关键字
        /// 如：连续100次请求没有新增任何数据
        /// 10秒执行一次 5分钟内addCount未有任何改变
        /// </summary>
        /// <param name="addCount"></param>
        /// <param name="existCount"></param>
        /// <param name="instanceCount"></param>
        /// <param name="curKeyWordCount"></param>
        /// <returns></returns>
        private bool CanAutoPassKeyWord(int addCount, int existCount, int instanceCount, int curKeyWordCount)
        {
            //数据正在请求并且没有任何请求数据10秒执行一次
            if (aTimer.Enabled)
            {
                ///每个关键字的最大爬取个数
                if (SearchKeyWordTakeCountLimit > 0)
                {
                    if (Settings.KeyWordAddCount >= SearchKeyWordTakeCountLimit)
                    {
                        Settings.KeyWordAddCount = 0;
                        return true;
                    }
                }
                var minIncreaseCount = 50;//最少增加量
                var maxNoIncreaseSeconds = 60 * 10;// 10分钟未增加50条
                if (Math.Abs(keyWordNoAddSeconds.Item1 - addCount) > minIncreaseCount)
                {
                    keyWordNoAddSeconds = new Tuple<int, int>(addCount, 0);
                }
                else
                {
                    //代表n次没有新增数据 5分钟相当于30次没有更新新数据了

                    if ((keyWordNoAddSeconds.Item2) * 10 >= maxNoIncreaseSeconds)
                    {
                        keyWordNoAddSeconds = new Tuple<int, int>(addCount, 0);
                        ShowMessageInfo($"超过{maxNoIncreaseSeconds}分钟没有新增数据开始进行pass操作", false);
                        timerStop();
                        return true;
                    }
                    else
                    {
                        keyWordNoAddSeconds = new Tuple<int, int>(addCount, keyWordNoAddSeconds.Item2 + 1);//10秒增加一次
                    }
                }
            }
            if (curKeyWordCount >= 700) return false;//保证前面的能完全过滤
            if (instanceCount < 500) return false;//数量少可以进行pass ，尽量走完不pass


            var hitFilter = PassKeyWordFilterCondition.Where(c => addCount <= c.Int("addCount")).FirstOrDefault();
            if (hitFilter == null) return false;
            var maxExistCount = hitFilter.Int("existCount");
            if (existCount >= maxExistCount)
            {
                return true;
            }
            return false;

        }

        private string ReplaceParam(string url, string paramName, string oldValue, string newValue)
        {
            if (url.Contains(paramName))
            {
                return url.Replace(string.Format("&{0}={1}", paramName, oldValue), string.Format("&{0}={1}", paramName, newValue));
            }
            return (url + string.Format("&{0}={1}", paramName, newValue));

        }

        private string ReplaceUrlParam(string _curUrl, string parameName, string oldValue, string newValue)
        {
            return ReplaceUrlParamEx(_curUrl, parameName,  newValue);
        }

        private string ReplaceParamEx(string url, string joinStr, string paramName, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(joinStr))
            {
                joinStr = "&";
            }
            if (url.Contains(paramName))
            {
                return url.Replace(string.Format(joinStr + "{0}={1}", paramName, oldValue), string.Format(joinStr + "{0}={1}", paramName, newValue));
            }
            return (url + string.Format(joinStr + "{0}={1}", paramName, newValue));
        }

        public string ReplaceUrlParamEx(string _curUrl, string parameName, string newValue, string joinStr = "&")
        {
            var oldValue = HttpUtility.UrlEncode(GetUrlParam(_curUrl, parameName));
            return this.ReplaceParamEx(_curUrl, joinStr, parameName, oldValue, newValue);
        }

        /// <summary>
        /// 保存匹配值到数据库
        /// 数据超过4w的时候保存出错。目前通过删除旧数据可以重新进行数据保存操作，切未能捕获异常，目前原因未知
        /// 全国类的数据
        /// </summary>
        private void SaveKeyWordHitCount(bool onlyCurKeyWord = false)
        {
            if (isUpCrawDetailCHK.Checked == false)
            {
                return;
            }
            var curCityName = this.cityNameStr;
            if (string.IsNullOrEmpty(curCityName))
            {
                curCityName = "全国";
            }
            else
            {
                if (IsProvince)
                {
                    curCityName = curQCCProvinceCode;
                }
            }

            var filterKeyWord = string.Empty;
            var hitKeyWordCountDic = keyWordCountDic;

            try
            {
                foreach (var keyWordDic in keyWordCountDic)
                {
                    //if (onlyCurKeyWord && singalKeyWordCHK.Checked)//单个关键字中只保存当前的关键字
                    //{
                    //    if (keyWordDic.Key != Settings.SearchKeyWord)
                    //    {
                    //        continue;
                    //    }
                    //}
                    if (keyWordDic.Key != Settings.SearchKeyWord)
                    {
                        continue;
                    }
                    var existObj = keyWordHitCountList.Where(c => c.Text("keyWord") == keyWordDic.Key).FirstOrDefault();

                    var addCount = 0;
                    var updateCount = 0;
                    try
                    {
                        addCount = keyWordDic.Value.Item1;
                        updateCount = keyWordDic.Value.Item2;

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
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("addCount", addCount).Add("updateCount", updateCount), Query = Query.EQ("_id", ObjectId.Parse(existObj.Text("_id"))), Name = DataTableEnterriseKeyWordCount, Type = StorageType.Update });
                    }
                    else
                    {
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("cityName", curCityName).Add("keyWord", keyWordDic.Key).Add("addCount", addCount).Add("updateCount", updateCount), Name = DataTableEnterriseKeyWordCount, Type = StorageType.Insert });
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
            if (string.IsNullOrEmpty(cityNameStr))
            {
                cityNameStr = "全国";
            }
            else
            {
                if (IsProvince)
                {
                    cityNameStr = curQCCProvinceCode;
                }
            }
            #region 关键字匹配
            if (!string.IsNullOrEmpty(cityNameStr))
            {
                keyWordHitCountList = dataop.FindAllByQuery(DataTableEnterriseKeyWordCount, Query.EQ("cityName", cityNameStr)).ToList();
                foreach (var _keyWordDoc in keyWordHitCountList)
                {
                    if (keyWordCountDic.ContainsKey(_keyWordDoc.Text("keyWord")))
                    {
                        keyWordCountDic[_keyWordDoc.Text("keyWord")] = new Tuple<int, int>(_keyWordDoc.Int("addCount"), _keyWordDoc.Int("updateCount"));
                    }
                    else
                    {
                        keyWordCountDic.TryAdd(_keyWordDoc.Text("keyWord"), new Tuple<int, int>(_keyWordDoc.Int("addCount"), _keyWordDoc.Int("updateCount")));
                    }
                }
            }
            #endregion
        }
        /// <summary>
        /// 匹配关键字匹配增加的记录个数 为12345.44312 标示增加12345 更新44312
        /// </summary>
        private Tuple<int, int> SetKeyWordHitCount(string keyWord, int addCount = 0, int updateCount = 0)
        {
            if (string.IsNullOrEmpty(keyWord))
            {
                return new Tuple<int, int>(0, 0);
            }

            try
            {
                if (keyWordCountDic.ContainsKey(keyWord))
                {
                    keyWordCountDic[keyWord] = new Tuple<int, int>(keyWordCountDic[keyWord].Item1 + addCount, keyWordCountDic[keyWord].Item2 + updateCount);
                    return keyWordCountDic[keyWord];
                }
                else
                {
                    keyWordCountDic.TryAdd(keyWord, new Tuple<int, int>(addCount, updateCount));
                    return keyWordCountDic[keyWord];
                }

            }
            catch (Exception ex)
            {
                return new Tuple<int, int>(addCount, updateCount); ;
#pragma warning disable CS0162 // 检测到无法访问的代码
                ShowMessageInfo("SetKeyWordHitCount" + ex.Message);
#pragma warning restore CS0162 // 检测到无法访问的代码
            }
        }

        /// <summary>
        /// 设置企业连接字符串
        /// </summary>
        /// <param name="_enterpriseIp"></param>
        private void SetEnterpriseDataOP(string _enterpriseIp,string _enterprisePort="")
        {
            enterpriseIp = _enterpriseIp;
            if (string.IsNullOrEmpty(_enterprisePort))
            {
                _enterprisePort = enterprisePort.ToString();
            }

            if (globalDBHasPassWord)
            {
                enterpriseConnStr = string.Format("mongodb://MZsa:MZdba@{0}:{1}/{2}", _enterpriseIp, _enterprisePort, globalDBName);
            }
            else
            {
                enterpriseConnStr = string.Format("mongodb://{0}:{1}/{2}", _enterpriseIp, _enterprisePort, globalDBName);
            }
          
            enterpriseDataop = new DataOperation(new MongoOperation(enterpriseConnStr));//主数据库
            _enterpriseMongoDBOp = new MongoOperation(enterpriseConnStr);
            ShowMessageInfo(string.Format("当前 ip:{0}", _enterpriseIp));
        }
        private bool ExistGuid(string guid)
        {
            return enterpriseDataop.FindCount(DataTableName, Query.EQ("guid", guid)) > 0;
        }

        private bool ExistKeyWordMapEntGuid(string guid)
        {
            return enterpriseDataop.FindCount(DataTableName_KeyWordEntRelation, Query.EQ("guid", guid)) > 0;
        }

        private bool ExistGuid(string tableName, string guid)
        {
            return (enterpriseDataop.FindCount(tableName, Query.EQ("guid", guid)) > 0);
        }
        private bool ExistKeyWordSearchGuid(string guid)
        {
            return enterpriseDataop.FindCount(DataTableKeyWordSearch, Query.EQ("guid", guid)) > 0;
        }


        private bool ExistUrl(string url, bool forceQuery = false)
        {
            if (!NEEDRECORDURL && forceQuery == false)
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
        public  void ReloadLoginAccount(bool forceLoad=false)
        {
            if (forceLoad==false&&!ContinueMethodByBusyGear("ReloadLoginAccount", 20))
            {
                return;
            }

            this.comboBox1.Items.Clear();
            var tempAccountList = new List<BsonDocument>();
            var takeCount = 100;
            var takeCount_device = 1000;
            //,Query.EQ("deviceId", "zv5RzAxD9JlrlLCgjAbuevrh"),最后一个可用
             var query = Query.And(Query.NE("isInvalid", "1"));//,Query.NE("isInvalid", "1") Query.NE("isBusy", "1"), Query.NE("status", "1")
            //var query = Query.And(Query.NE("isInvalid", "1"));
            var totalCount = dataop.FindCount(DataTableAccount );
            var rand = new Random();
            var count = rand.Next(0, totalCount);
            if (count <= 100)
            {
                count = 0;
            }
            else {
                count = rand.Next(0, 1000);
            }
            if (allAccountList.Count <= 0)
            {
               // allAccountList = dataop.FindLimitByQuery(DataTableAccount, query, new SortByDocument() { { "_id",1} }, count, takeCount).OrderBy(c => c.Int("isUsed") + c.Int("isBusy")).ThenBy(c => c.Int("UpdateEnterpriseInfo") + c.Int("EnterpriseGuidByKeyWord")).ThenBy(c => c.Date("updateDate")).ToList();
                allAccountList = dataop.FindLimitByQuery(DataTableAccount, query, new SortByDocument() { { "updateDate", 1 } }, count, takeCount).OrderBy(c=>c.Text("updateDate")).ThenBy(c => c.Int("EnterpriseGuidByKeyWord_APP") + c.Int("UpdateEnterpriseInfoAPP")).ToList();
            }
            else
            {
                allAccountList = dataop.FindLimitByQuery(DataTableAccount, query, new SortByDocument() { { "updateDate", 1 } }, count, takeCount).OrderBy(c => c.Text("updateDate")).ThenBy(c => c.Int("EnterpriseGuidByKeyWord_APP") + c.Int("UpdateEnterpriseInfoAPP")).ToList();

            }
            tempAccountList = allAccountList;
            foreach (var account in tempAccountList)
            {

                //var accountName = string.Empty;
                //if (account.Int("isUsed") == 1)
                //{
                //    accountName = string.Format("{0}_占用", account.Text("name"));
                //}
                //else
                //{
                //    accountName = string.Format("{0}", account.Text("name"));
                //}
                //if (account.Int("isBusy") == 1)
                //{
                //    accountName = string.Format("{0}_频繁", accountName);
                //}
                this.comboBox1.Items.Add(account.Text("name"));
            }


            var deviceTotalCount = dataop.FindCount(QCCDeviceAccount, query);

            rand = new Random();
            count = rand.Next(0, deviceTotalCount);
            if (count <= 100)
            {
                count = 0;
            }
            ///获取设备列表
            // allDeviceAccountList = dataop.FindAllByQuery(QCCDeviceAccount, Query.And(Query.NE("isValid", "1"), Query.NE("status", "1"), Query.NE("isUse", "1"), Query.NE("isBusy", "1"))).Where(c => c.Int("EnterpriseGuidByKeyWord_APP") <= Settings.MaxAccountCrawlerCount / 2).OrderBy(c => c.Int("EnterpriseGuidByKeyWordApp")).ThenBy(c => c.Date("updateDate")).ToList();
           // allDeviceAccountList = dataop.FindLimitByQuery(QCCDeviceAccount, query, new SortByDocument() { { "EnterpriseGuidByKeyWord_APP", 1 }, { "createDate", -1 } }, 0, takeCount_device).OrderBy(c => c.Int("EnterpriseGuidByKeyWord_APP")).ThenBy(c => c.Date("updateDate")).ToList();
            allDeviceAccountList = dataop.FindLimitByQuery(QCCDeviceAccount, query, new SortByDocument() { { "updateDate", 1 } }, 0, takeCount_device).OrderBy(c=>c.Text("updateDate")).ToList();
        }
        bool hasLoadInvalidAccount = false;
        public void LoadInvalidAccount()
        {
            hasLoadInvalidAccount = true;
            var takeCount = 100;
            var query = Query.And(Query.EQ("isInvalid", "1"),Query.NE("exceptionAgain",1));
            var totalCount = dataop.FindCount(DataTableAccount);
            var rand = new Random();
            var count = rand.Next(0, totalCount);
            if (count <= 100)
            {
                count = 0;
            }
            else
            {
                count = rand.Next(0, 1000);
            }
            if (allAccountList.Count <= 0)
            {
                // allAccountList = dataop.FindLimitByQuery(DataTableAccount, query, new SortByDocument() { { "_id",1} }, count, takeCount).OrderBy(c => c.Int("isUsed") + c.Int("isBusy")).ThenBy(c => c.Int("UpdateEnterpriseInfo") + c.Int("EnterpriseGuidByKeyWord")).ThenBy(c => c.Date("updateDate")).ToList();
                allAccountList = dataop.FindLimitByQuery(DataTableAccount, query, new SortByDocument() { { "updateDate", 1 } }, count, takeCount).OrderBy(c => c.Text("updateDate")).ThenBy(c => c.Int("EnterpriseGuidByKeyWord_APP") + c.Int("UpdateEnterpriseInfoAPP")).ToList();
            }
            else
            {
                allAccountList = dataop.FindLimitByQuery(DataTableAccount, query, new SortByDocument() { { "updateDate", 1 } }, count, takeCount).OrderBy(c => c.Text("updateDate")).ThenBy(c => c.Int("EnterpriseGuidByKeyWord_APP") + c.Int("UpdateEnterpriseInfoAPP")).ToList();

            }
        }
        /// <summary>
        /// 重新计算所有的accessToken
        /// </summary>
        public void ReloadCaculateAccessToken()
        {

            accountInfoTxt.BeginInvoke(new Action(() =>
            {
                try
                {
                    var query = Query.And(Query.NE("isInvalid", "1"));//,Query.NE("isInvalid", "1") Query.NE("isBusy", "1"), Query.NE("status", "1")
                    var hitDeviceAccountList = dataop.FindAllByQuery(QCCDeviceAccount, query).ToList();
                    var allDeviceCount = hitDeviceAccountList.Count();
                    foreach (var deviceInfo in hitDeviceAccountList)
                    {
                        SetSetting(deviceInfo);
                        allDeviceCount -= 1;
                        ShowMessageInfo($"正在初始化AccessToken剩余{allDeviceCount}");
                    }
                    //重新加载
                    allDeviceAccountList = hitDeviceAccountList.OrderBy(c => c.Int("EnterpriseGuidByKeyWord_APP")).ThenBy(c => c.Date("updateDate")).ToList();
                }
                catch (Exception ex)
                {
                    ShowMessageInfo(ex.Message);
                }
            }));

        }

        private void ShowAccountInfo(BsonDocument curLoginAccountObj)
        {
            var sb = new StringBuilder();
            try
            {
                Type searchTypeEnum = typeof(SearchType);

                // foreach (string _searchType in Enum.GetNames(searchTypeEnum))

                var _searchType = searchType.ToString();
                //Console.WriteLine("{0,-11}= {1}", s, Enum.Format(searchTypeEnum, Enum.Parse(searchTypeEnum, s), "s"));
                var addColumnName = string.Format("{0}_add", _searchType);
                var addTotalColumnName = string.Format("{0}_total", _searchType);
                var curAddional = curLoginAccountObj.Int(addColumnName);
                var initial = curLoginAccountObj.Int(_searchType);
                var all = curLoginAccountObj.Int(addTotalColumnName);
                if (curAddional != 0 || initial != 0 || all != 0)
                {
                    if (IsAPP(searchType))
                    {
                        sb.AppendFormat("{0}:{1}【add:{2}_{3}】 ", "app", initial, curAddional, all);
                    }
                    else
                    {
                        sb.AppendFormat("{0}:{1}【add:{2}_{3}】 ", _searchType, initial, curAddional, all);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessageInfo(ex.Message + "ShowAccountInfo");
            }
            accountInfoTxt.BeginInvoke(new Action(() =>
            {
                try
                {
                    this.accountInfoTxt.Text = sb.ToString();
                }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                {

                }
            }));
            //if (AccountMaxAddional!=0&&curAddional >= AccountMaxAddional)
            //{
            //      ShowMessageInfo("达到最大账号可爬取数量正在自动切换账号");
            //      AutoChangeAccount();
            //}
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

            if (html.StartsWith("<script>") || html.StartsWith("<html>"))
            {
                return true;
                //todo:AccountBusy
                //AccountBusy
            }
            if (IsAPP(searchType))
            {
                var status = Toolslib.Str.Sub(html, "message\":\"", "\"");
                if (status.Contains("成功") || html.Contains("该企业主体信息获取失败") || html.Contains("没找到数据"))
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
            if (searchType == SearchType.UpdateEnterpriseInfoAPP && !html.Contains("基本信息") && !url.Contains("more_findmuhou"))
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
                    if (args.Html.StartsWith("<html>"))
                    {
                        ShowMessageInfo(args.Html);
                        //timerStop();
                        if (AutoChangeIp.Checked == true)
                        {
                            ChangeIp();
                        }
                        // timerStart();
                        return true;

                    }
                    //出错次数过多肯呢个到职被异常
                    if (!IsAPP(searchType))
                    {
                        if (string.IsNullOrEmpty(args.Html)) return true;//等待期间内的url 不限制
                        if (searchType == SearchType.UpdateEnterpriseInfoAPP)
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
                            //this.webBrowser.Navigate(addCredsToUri(curUri));


                            //this.richTextBoxInfo.Document.Blocks.Clear();
                            //this.richTextBoxInfo.AppendText(string.Format("当前url:{0}剩余url:{1}", this.curUri.ToString(), UrlQueue.Instance.Count));
                            //this.richTextBoxInfo.AppendText("正在检测是否自动验证!");
                            if (Settings.neeedChangeAccount)
                            {
                                ShowMessageInfo(string.Format("账号爬取达到上限{0},请切换账号", Settings.MaxAccountCrawlerCount));
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
                                    string preText = string.Format("{0}_{1}", this.proxyIpDetail, enterpriseIp);
                                    ShowMessageInfo($"{preText}正在刷新浏览器，请点击重新爬取", true);
                                    waitBrowerMouseUpResponse = true;
                                    ///是否获得焦点
                                    if (this.checkBox.Checked)
                                    {
                                        //this.Activate();
                                    }
                                }

                                ///重载uri
                                if (curUri != null)
                                {
                                    var cookies = FullWebBrowserCookie.GetCookieInternal(curUri, false);
                                    Settings.SimulateCookies = cookies;
                                }
                            }
                        }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                        catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                        {
                            this.richTextBoxInfo.Text += "刷新浏览器失败";
                            // ShowMessageInfo("刷新浏览器失败", true);
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

            try
            {
                if (Settings.neeedChangeAccount)
                {
                    //this.richTextBoxInfo.Text +=;
                    ShowMessageInfo(string.Format("账号爬取达到上限{0},请切换账号", Settings.MaxAccountCrawlerCount));
                    AutoChangeAccount();
                }
                Settings.LastAvaiableTime = DateTime.Now;
                //ShowMessageInfo("无效内容"+args.Url, true);
            }
            catch (Exception ex)
            {
                ShowMessageInfo(ex.Message);
            }
            return false;
        }

        public void ShowMessageInfo(string str, bool isAppend = false)
        {
            try
            {
                richTextBoxInfo.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (isAppend == false)
                        {
                            this.richTextBoxInfo.Clear();
                        }
                        string preText = string.Format("{0}_{1}", this.proxyIpDetail, enterpriseIp);
                        if (IsNeedLogin(searchType))
                        {
                            preText += $"账号:{Settings.LoginAccount} {Settings.LoginPassword}";
                            if (str.Contains("异常"))
                            {
                                var curAccount = new BsonDocument();
                                curAccount.Set("account", Settings.LoginAccount);
                                curAccount.Set("password", Settings.LoginPassword);
                                invalidAccountList.Add(curAccount);
                            }
                        }
                        var messageInfo = string.Format("{0} 剩余更新：{1} 剩余关键字: {2}", preText, DBChangeQueue.Instance.Count, StringQueue.Instance.Count);
                        this.richTextBoxInfo.AppendText(messageInfo);
                        this.richTextBoxInfo.AppendText(str);
                    }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                    catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                    {

                    }
                })
               );
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

            }
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
        private void StartDBChangeProcessQuick(MongoOperation curMongoDBOP = null)
        {
            try
            {
                var limitCount = Settings.DBSaveCountLimit;
                if (limitCount <= 0)
                {
                    limitCount = 20;
                }
                if (UrlQueueCount() >= 10 && DBChangeQueue.Instance.Count < limitCount) return;
                var curDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var result = new InvokeResult();
                List<StorageData> updateList = new List<StorageData>();

                {

                    var temp = DBChangeQueue.Instance.DeQueue();
                    if (temp != null)
                    {
                        var insertDoc = temp.Document;
                        if (curMongoDBOP == null)
                        {
                            curMongoDBOP = _mongoDBOp;
                        }
                        if (temp.Name == DataTableName)
                        {
                            curMongoDBOP = _enterpriseMongoDBOp;
                        }
                        try
                        {
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
                                    result = curMongoDBOP.UpdateOrInsert(temp.Name, temp.Query, insertDoc);
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
                        catch (Exception ex)
                        {
                            ShowMessageInfo("StartDBChangeProcessQuick" + ex.Message);
                        }

                    }

                }

                if (DBChangeQueue.Instance.Count > 0)
                {
                    StartDBChangeProcessQuick(curMongoDBOP);
                }
                if (DBChangeQueue.Instance.Count >= 100)
                {
                    ShowMessageInfo(DBChangeQueue.Instance.Count.ToString(), true);
                }
            }
            catch (Exception ex)
            {
                ShowMessageInfo(ex.Message);
            }
        }

        /// <summary>
        /// 获取当前timer interval
        /// </summary>
        /// <returns></returns>
        private double GetCurInterval()
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
            return curInterVal;
        }
        private void timerStart()
        {

            if (aTimer.Enabled == false)
            {

                var curInterVal = GetCurInterval();
                aTimer.Interval = curInterVal;
                aTimer.Enabled = true;
                aTimer.Start();
                waitBrowerMouseUpResponse = false;

                ShowMessageInfo("计时器开始");

                // GetSimulateCookies();

            }

        }

        private void GetSimulateCookies()
        {
            webBrowser.BeginInvoke(new Action(() =>
            {
                try
                {
                    ///重载uri
                    if (curUri != null)
                    {
                        var cookies = FullWebBrowserCookie.GetCookieInternal(curUri, false);
                        Settings.SimulateCookies = cookies;
                    }
                }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                {

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
        private bool timerHasSlow = false;
        /// <summary>
        /// 减慢timer速度
        /// </summary>
        private void TimerSlow()
        {

            // ShowMessageInfo("减慢timer速度");
            aTimer.Interval = 1000;
            timerHasSlow = true;

        }
        /// <summary>
        /// 回复timer速度
        /// </summary>
        private void TimerReset()
        {
            if (timerHasSlow)
            {
                aTimer.Interval = GetCurInterval();
                timerHasSlow = false;
                if (aTimer.Enabled == false)
                {
                    aTimer.Start();
                }
            }
        }

        /// <summary>
        /// 跳过下一个关键字
        /// </summary>
        private void PassKeyWord()
        {
            if (!ContinueMethodByBusyGear("ProcessCanAutoPassKeyWord", 5))
            {
                return;
            }
            try
            {
                timerStop();
                var setUrlCount = 0;
                Settings.KeyWordAddCount = 0;
                if (int.TryParse(textBox3.Text.Trim(), out setUrlCount))
                {

                }
                if (UrlRetryQueue.Instance.Count > 0)
                {
                    while (UrlRetryQueue.Instance.Count > setUrlCount)
                    {
                        UrlRetryQueue.Instance.DeQueue();
                    }
                }
                else
                {
                    while (UrlQueue.Instance.Count > setUrlCount)
                    {
                        UrlQueue.Instance.DeQueue();
                    }
                }
                if (StringQueue.Instance.Count > 0)
                {
                    ShowMessageInfo("正在尝试提取关键字");
                    var leftCount = 1;
                    if (singalKeyWordCHK.Checked == true)
                    {
                        leftCount = StringQueue.Instance.Count - 1;
                    }
                    else
                    {
                        leftCount = 0;
                    }

                    while (StringQueue.Instance.Count > leftCount)
                    {
                        var curKeyWord = StringQueue.Instance.DeQueue();
                        Settings.SearchKeyWord = curKeyWord;
                        if (string.IsNullOrEmpty(curKeyWord)) continue;
                        var urlList = InitalQCCAppUrlByKeyWord(curKeyWord);
                        foreach (var url in urlList)
                        {
                            if (!urlFilter.Contains(url))
                            {
                                UrlQueue.Instance.EnQueue(new UrlInfo(url));
                                urlFilter.Add(url);
                            }
                        }
                    }
                    timerStart();
                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

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
            if (Settings.neeedChangeAccount && IsAPP(searchType))
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
                        case SearchType.UpdateEnterpriseInfoAPP:
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
            return AppAutoLoin();
#pragma warning disable CS0162 // 检测到无法访问的代码
            try
#pragma warning restore CS0162 // 检测到无法访问的代码
            {
                return AliyunAutoLoin();
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
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
                    //this.checkBoxGuard.Checked = false;
                    timerStop();
                    checkBox1.Checked = false;
                    return false;
                    // MessageBox.Show("登陆失败太多");


                    //Application.Exit();
                }
            }
            ///app登陆模式
            if (IsAPP(searchType))
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
#pragma warning disable CS0219 // 变量“validUrl”已被赋值，但从未使用过它的值
            var validUrl = "";
#pragma warning restore CS0219 // 变量“validUrl”已被赋值，但从未使用过它的值
#pragma warning disable CS0219 // 变量“postFormat”已被赋值，但从未使用过它的值
            var postFormat = "";
#pragma warning restore CS0219 // 变量“postFormat”已被赋值，但从未使用过它的值
#pragma warning disable CS0219 // 变量“result”已被赋值，但从未使用过它的值
            bool result = false;
#pragma warning restore CS0219 // 变量“result”已被赋值，但从未使用过它的值
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
        /// app模拟登陆,切换账号
        /// </summary>
        /// <returns></returns>
        private bool AppAutoLoin()
        {

            if (string.IsNullOrEmpty(Settings.LoginAccount))
            {
                Settings.LoginAccount = this.textBox1.Text;
                Settings.LoginPassword = this.textBox2.Text;
            }
            else
            {
                this.textBox1.Text = Settings.LoginAccount;
                this.textBox2.Text = Settings.LoginPassword;
            }
            return AppAutoLoin(Settings.LoginAccount, Settings.LoginPassword);
        }

        /// <summary>
        /// app模拟登陆
        /// </summary>
        /// <returns></returns>
        private bool AppAutoLoin(string phoneNum,string pwdNormal)
        {
            RefreshToken();
            var hashPwd = string.Empty;//加密后的hash
            var hitHashObj = allAccountHashMapList.Where(c => c.Text("password") == pwdNormal).FirstOrDefault();
            if (hitHashObj == null)
            {
                hitHashObj = new BsonDocument().Set("hash", "b412a4532991798fcddf698e31125c03");
            }
            if (hitHashObj != null)
            {
                hashPwd = hitHashObj.Text("hash");
           
                var _url = new UrlInfo($"{ConstParam.LoginUrlV2}");
               // _url.PostData = string.Format("loginType=2&accountType=1&account={0}&password={1}&identifyCode=&key=&token=&timestamp={2}&sign={3}", phoneNum, hashPwd, Settings.timestamp, Settings.sign);
                //
                // _url.PostData = $"loginType=2&accountType=1&account={phoneNum}&password={hashPwd}&identifyCode=&key=&token=&deviceToken=cd572cc4f87f455d9dbb6f1d1304f561&internationalCode=%2B86&timestamp={Settings.timestamp}&sign={Settings.sign}";
                _url.PostData = $"loginType=2&accountType=1&account={phoneNum}&password={hashPwd}&identifyCode=&key=&token=&&timestamp={Settings.timestamp}&sign={Settings.sign}";
                var result = GetPostDataAPP(_url);
                if (result.StatusCode == HttpStatusCode.OK && result.Html.Contains("成功"))
                {
                    var token = Toolslib.Str.Sub(result.Html, "access_token\":\"", "\"");
                    if (!string.IsNullOrEmpty(token))
                        Settings.AccessToken = token;
                    var htmlResult = $"{phoneNum}{pwdNormal}登陆状态：";
                    ShowMessageInfo($"{htmlResult}{result.Html}", true);
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
            var result = GetHttpHtml(new UrlInfo(proxyHost + "/switch-ip"));
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
            try
            {
                ///账号爬取统计，防止被封，查看爬去个数
                var curLoginAccountObj = allDeviceAccountList.Where(c => c.Text("name") == loginAccount).FirstOrDefault();
                if (curLoginAccountObj != null)
                {


                    //Console.WriteLine("{0,-11}= {1}", s, Enum.Format(searchTypeEnum, Enum.Parse(searchTypeEnum, s), "s"));
                    var columnName = string.Format("{0}_add", searchType.ToString());
                    var curAddional = curLoginAccountObj.Int(columnName);
                    var finalResult = 0;
                    if (curAddional != 0)
                    {
                        finalResult = curLoginAccountObj.Int(searchType.ToString()) + curAddional;
                        updateBson.Set(searchType.ToString(), finalResult.ToString());//增加值
                        curLoginAccountObj.Set(columnName, "0");
                        curLoginAccountObj.Set(searchType.ToString(), finalResult.ToString());// 当前值
                    }

                    ///设置为已完成当前任务
                    if (Settings.MaxAccountCrawlerCount > 0 && finalResult > Settings.MaxAccountCrawlerCount)
                    {
                        updateBson.Add("status", "1");
                        updateBson.Add("maxOrverLoad", "1");
                    }

                }
            }
            catch (Exception ex)
            {
                ShowMessageInfo(ex.Message + "DeviceAccountRelease");
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
            if (USEWEBPROXY)
            {
                ChangeIp();
            }
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
                        if (maxLength > 20)
                        {
                            maxLength = 20;
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
                                var curAccount = allAccountList.Where(c => c.Text("name") == Settings.LoginAccount).FirstOrDefault();
                               
                                // ForceChangeProxy();//手动切换ip
                                ReloadLoginAccount();//放在后面进行账号读取，防止账号获得数据但是没有进行其获取 的数据被重置 2020改为放置到前面
                                allDeviceAccountList.Add(nextAccount);//防止新的nextaccount不存在
                                if (curAccount != null)
                                {
                                    allAccountList.Add(curAccount);
                                }
                                //GeetestChartAutoLoin();
                                if (checkBoxGuard.Checked = true) {
                                    timerStart();
                                }
                                
                                ShowMessageInfo("启用新账号");
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
        /// 每次请求更换accesstoken
        /// </summary>
        public void QuickChangeDeviceAccount()
        {
            //if (USEWEBPROXY)
            //{
            //    ChangeIp();
            //}
            if (autoChangeAccountCHK.Checked == true)
            {

                if (allDeviceAccountList.Count() > 0)
                {
                    var oldDeviceId = Settings.DeviceId;
                    var hitAccountList = allDeviceAccountList.Where(c => c.Text("deviceId") != Settings.DeviceId && c.Int("EnterpriseGuidByKeyWord_APP") <= 0).ToList();
                    if (hitAccountList.Count <= 0)
                    {
                        hitAccountList = allDeviceAccountList.Where(c => c.Text("deviceId") != Settings.DeviceId).ToList();
                    }
                    #region 随机获取数据
                    var rnd = new Random();
                    var maxLength = (hitAccountList.Count() - 1);
                    if (hitAccountList.Count() < 10)
                    {
                        maxLength = hitAccountList.Count() - 1;
                    }
                    //if (maxLength > 500)
                    //{
                    //    maxLength = 500;
                    //}
                    if (maxLength <= 0)
                    {
                        maxLength = 1;
                    }
                    #endregion
                    var skipCount = rnd.Next(0, maxLength);
                    var nextAccount = hitAccountList.Skip(skipCount).FirstOrDefault();
                    if (nextAccount != null)
                    {

                        if (!string.IsNullOrEmpty(nextAccount.Text("deviceId")))
                        {
                            Settings.DeviceId = nextAccount.Text("deviceId");
                        }
                        if (!string.IsNullOrEmpty(nextAccount.Text("timestamp")))
                        {
                            Settings.timestamp = nextAccount.Text("timestamp").Trim();
                        }
                        if (!string.IsNullOrEmpty(nextAccount.Text("sign")))
                        {
                            Settings.sign = nextAccount.Text("sign").Trim();
                        }

                        Settings.RefleshToken = nextAccount.Text("refleshToken").Trim();
                        Settings.AccessToken = nextAccount.Text("accessToken").Trim();
                        Settings.AccounInfo = nextAccount.Text("updateDate");

                    }
                }
                else
                {
                    autoChangeAccountCHK.Checked = false;//取消自动切换账户
                    checkBoxGuard.Checked = false;
                    ShowMessageInfo("无可用账户");
                }
            }
        }




        /// <summary>
        /// 自动切换账号 并登陆,设定自动切换账号间隔，与使用前先进行刷新ip操作
        /// </summary>
        public void AutoChangeAccount()
        {
            //lock (lockChangeAccount)//2017.1.11可能导致死锁，或者同时进去
            {
                if (!ContinueMethodByBusyGear("AutoChangeAccount", 10))
                {
                    return;
                }
                timerStop();
                ///切换app设备账号，后续需要登陆在使用账号登陆
                if (IsAPP(searchType))
                {
                    AutoChangeDeviceAccount();
                    //return;
                }
                ///是否需要登陆用户
                //if (IsNeedLogin(searchType))
                //{
                //    ChangeRandomLoginAccount();
                //    //#region 旧操作
                //    //richTextBoxInfo.Invoke(new Action(() =>
                //    //{

                //    //    if (autoChangeAccountCHK.Checked == true)
                //    //    {

                //    //        if (allAccountList.Count() > 0)
                //    //        {
                //    //            // ForceChangeProxy();//手动切换ip
                //    //            ReloadLoginAccount();//放在后面进行账号读取，防止账号获得数据但是没有进行其获取 的数据被重置
                //    //           // AppAutoLoin();//模拟自动登陆,登陆失败
                //    //            this.textBox1.Text = Settings.LoginAccount;
                //    //            this.textBox2.Text = Settings.LoginPassword;
                //    //        }
                //    //        else
                //    //        {
                //    //            autoChangeAccountCHK.Checked = false;//取消自动切换账户
                //    //            checkBoxGuard.Checked = false;
                //    //            ShowMessageInfo("无可用账户");
                //    //        }
                //    //    }

                //    //}));
                //    //#endregion
                //}



            }
        }
        /// <summary>
        /// 随机获取登陆用户
        /// </summary>
        /// <returns></returns>
        public string ChangeRandomLoginAccount()
        {
            if (!string.IsNullOrEmpty(Settings.LoginAccount)&&UrlQueueCount()>0)
            {
                AccountRelease(Settings.LoginAccount);//当前设备注销
            }
            if (allAccountList.Count() > 0)
            {

                var hitAccountList = allAccountList.Where(c =>  c.Text("name") != Settings.LoginAccount&&c.Int("isInvalid") !=1).ToList();
                #region 随机获取数据
                var rnd = new Random();
                var maxLength = hitAccountList.Count();  
                if (hitAccountList.Count() < 10)
                {
                    maxLength = hitAccountList.Count() - 1;
                }
                if (maxLength > 20)//前50里面变换
                {
                    maxLength = 20;
                }
                #endregion
                var skipCount = 0;
                if (maxLength > 1)
                {
                    skipCount = rnd.Next(0, maxLength);
                }
                var nextAccount = hitAccountList.Skip(skipCount).FirstOrDefault();
                if (nextAccount != null)
                {
                    Settings.LoginAccount = nextAccount.Text("name");//当前账号
                    Settings.LoginPassword = nextAccount.Text("password");//当前账号密码
                    Settings.AccounInfo =$"{Settings.AccounInfo}_{nextAccount.Text("updateDate")}";
                    //ReloadLoginAccount();//放在后面进行账号读取，防止账号获得数据但是没有进行其获取 的数据被重置
                    if (AppAutoLoin())//模拟自动登陆,登陆失败
                    {
                        //allAccountList.Remove(nextAccount);
                    }
                     
                }
            }
            return Settings.LoginAccount;
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
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                catch (FormatException ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                {
                    return Regex.Unescape(str);
                }
            }
            return strResult.ToString();
        }



        /// <summary>
        /// 调度器列表
        /// </summary>
        private ConcurrentDictionary<string, DateTime> MehodBusyGearList = new ConcurrentDictionary<string, DateTime>();
        /// <summary>
        /// 防止因为异步访问某个时间太频繁添加的调度器，及需要某个时间段内才能第二次访问
        /// </summary>
        /// <param name="key"></param>
        /// <param name="secondsSpan"></param>
        /// <returns></returns>
        private bool ContinueMethodByBusyGear(string key, int secondsSpan = 5)
        {
            //static SemaphoreSlim _sem = new SemaphoreSlim(3); 进行重写
            //Console.WriteLine(id + " 开始排队...");
            //_sem.Wait();
            //Console.WriteLine(id + " 开始执行！");
            //Thread.Sleep(1000 * (int)id);
            //Console.WriteLine(id + " 执行完毕，离开！");
            //_sem.Release();
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
                        //ShowMessageInfo(string.Format("{0}s内无法重复进行{1}操作", secondsSpan, key));
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

        private DateTime GetContinueMethodByBusyGear(string key)
        {
            if (MehodBusyGearList.ContainsKey(key))
            {
                var DateTime = MehodBusyGearList[key];
                return DateTime;
            }
            return DateTime.Now;
        }


        /// <summary>
        /// 修复Url,此处因为其curl的timestamp 与sign值 每个账号需要的只不一样，因此需要进行替换,才会不限制
        /// </summary>
        /// <param name="curUrlObj"></param>
        /// <returns></returns>
        public string FixUrlSignStr(UrlInfo curUrlObj)
        {
            ///每次请求快速切换设备key 跟ip
            if (quickChangeAccountChk.Checked)
            {
                if (ContinueMethodByBusyGear("QuickChangeDeviceAccount", 8)) { QuickChangeDeviceAccount(); }
            }
            var url = curUrlObj.UrlString;
            var symbol = !url.Contains("?")?"?":"&";
            
            //补齐参数
            if (IsAPP(searchType))
            {
                var _timestamp = GetUrlParam(curUrlObj.UrlString, "timestamp");
                var _sign = GetUrlParam(curUrlObj.UrlString, "sign");
                var _token = GetUrlParam(curUrlObj.UrlString, "token");
                if (url.Contains($"{symbol}timestamp=") && _timestamp != Settings.timestamp)
                {
                    url = url.Replace($"{symbol}timestamp=" + _timestamp, "&timestamp=" + Settings.timestamp);
                }
                else
                {
                    if (!url.Contains($"{symbol}timestamp="))
                    {
                        url = url + $"{symbol}timestamp=" + Settings.timestamp;
                    }
                  
                }

                if (url.Contains($"{symbol}sign=") && _sign != Settings.sign)
                {

                    url = url.Replace($"{symbol}sign=" + _sign, "&sign=" + Settings.sign);
                }
                else
                {
                    if (!url.Contains($"{symbol}sign="))
                    {
                        url = url + $"{symbol}sign=" + Settings.sign;
                    }
                }
                if (url.Contains("token"))
                {
                    url = ReplaceUrlParamEx(url, "token",HttpUtility.UrlEncode(Settings.AccessToken));
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
            if (IsAPP(searchType))
            {
                var _timestamp = GetUrlParam(curUrlObj.UrlString, "timestamp");
                var _sign = GetUrlParam(curUrlObj.UrlString, "sign");
                var _token = GetUrlParam(curUrlObj.UrlString, "token");
                if (!string.IsNullOrEmpty(_timestamp))
                {
                    url = url.Replace("&timestamp=" + _timestamp, "");
                }
                if (!string.IsNullOrEmpty(_sign))
                {
                    url = url.Replace("&sign=" + _sign, "");
                }
                if (!string.IsNullOrEmpty(_token))
                {
                    url = url.Replace("&token=" + _token, "");
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

        public void GetInventRelation(BsonDocument doc, string rootGuid = "", string parentGuid = "")
        {
            string tableName = "QCCEnterpriseKeyInventInfoRelation";
            string name = doc.Text("name");
            string guid = doc.Text("KeyNo");
            string Level = doc.Text("Level");
            string Category = doc.Text("Category");
            string FundedAmount = doc.Text("FundedAmount");
            string FundedRate = doc.Text("FundedRate");
            string ShortName = doc.Text("ShortName");
            string Count = doc.Text("Count");
            if (name != "股东")
            {
                if (string.IsNullOrEmpty(rootGuid) || (rootGuid == "BsonNull"))
                {
                    rootGuid = guid;
                }
                if (string.IsNullOrEmpty(parentGuid) || (parentGuid == "BsonNull"))
                {
                    parentGuid = rootGuid;
                }
                BsonDocument updateDoc = new BsonDocument {
                    {
                        "guid",
                        guid
                    },
                    {
                        "level",
                        Level
                    },
                    {
                        "name",
                        name
                    },
                    {
                        "ShortName",
                        ShortName
                    },
                    {
                        "Category",
                        Category
                    }
                };
                if (FundedAmount != "BsonNull")
                {
                    updateDoc.Add("FundedAmount", FundedAmount);
                }
                if (FundedRate != "BsonNull")
                {
                    updateDoc.Add("FundedRate", FundedRate);
                }
                updateDoc.Add("Count", Count);
                updateDoc.Add("nodePid", parentGuid);
                updateDoc.Add("primaryGuid", rootGuid);
                if (!string.IsNullOrEmpty(guid) && (guid != "BsonNull"))
                {
                    StorageData target = new StorageData
                    {
                        Name = tableName,
                        Document = updateDoc,
                        Type = StorageType.Insert
                    };
                    DBChangeQueue.Instance.EnQueue(target);
                }
                if (doc["children"] != null)
                {
                    List<BsonDocument> children = doc.GetBsonDocumentList("children");
                    if (children != null)
                    {
                        foreach (BsonDocument node in children)
                        {
                            this.GetInventRelation(node, rootGuid, guid);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 判断ip区域是否有效
        /// </summary>
        /// <returns></returns>
        private bool IpValid(string html)
        {
            var validWordStr = "山西,陕西,湖北,湖南,揭阳,舟山,辽宁，鞍山，铜陵，衢州，滨州，,台州，荆州，南通，黄冈，宿迁";
            var validWords = validWordStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            return validWords.Any(d => html.Contains(d));
        }

        private void ChangeIp()
        {
            if (ContinueMethodByBusyGear("HttpRequestChangeIP", 1))
            {
                //一直切换直到可用;
               var curIpDetail= QuickProxyPoolHelper.Instance(GetWebProxy()).ChangeIPUnitlAvaiabl_QCC(
                    begin:()=> { timerStop(); ShowMessageInfo("开始切换Ip"); },
                    after:()=> { timerStart(); },
                    sleepTime:3000);

                if (!string.IsNullOrEmpty(curIpDetail))
                {
                    proxyIpDetail = curIpDetail;
                }

                // var result = GetHttpHtml(new UrlInfo(proxyHost + "/switch-ip"));
                //正常切换
                //var result = ExecChangeIp();
                //if (!string.IsNullOrEmpty(result))
                //{
                //    proxyIpDetail = result.Trim();
                //}
            }
            //while(!IpValid(result))
            //{
            //    Thread.Sleep(500);
            //    ChangeIp();
            //}

        }

        private string ExecChangeIp()
        {
            //proxyIpDetail = string.Empty;
            // while (true)
            {
                //  Thread.Sleep(500);
                SimpleCrawler.HttpResult result = new SimpleCrawler.HttpResult();
                try
                {
                    var item = new SimpleCrawler.HttpItem()
                    {
                        URL = proxyHost + "/switch-ip",
                        Method = "get",//URL     可选项 默认为Get   
                                       // ContentType = "text/html",//返回类型    可选项有默认值 
                        UserAgent = "okhttp/3.6.0",
                        ContentType = "application/x-www-form-urlencoded",
                    };

                    // item.Header.Add("Content-Type", "application/x-www-form-urlencoded");
                    // hi.HeaderSet("Content-Length","154");
                    // hi.HeaderSet("Connection","Keep-Alive");
                    item.Header.Add("Proxy-Switch-Ip", "yes");
                    item.WebProxy = GetWebProxy();
                    result = http.GetHtml(item);

                    if (!string.IsNullOrEmpty(result.Html))
                    {
                        proxyIpDetail = result.Html.Trim();
                        // ShowMessageInfo(result.Html);
                        //if (!ipLimitFilter.Contains(proxyIpDetail))
                        {
                            return result.Html;
                        }

                    }

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
            }
            return string.Empty;
        }

        /// <summary>
        /// 建立关键字与企业关联
        /// </summary>
        /// <returns></returns>
        private  void BuildKeyWordEntRelate(string keyWordGuid,string entGuid,BsonDocument doc)
        {
            try
            {
                if (string.IsNullOrEmpty(keyWordGuid)) { return; }
                var guid = MD5EncodeHelper.Encode($"{keyWordGuid}{entGuid}").ToLower();
                if (!keyWordEntRelateGuidLimitFilter.Contains(guid)&&!ExistKeyWordMapEntGuid(guid))
                {
                    var addDoc = new BsonDocument();
                    addDoc.Set("guid", guid);
                    addDoc.Set("keyWordGuid", keyWordGuid);
                    addDoc.Set("entGuid", entGuid);
                    if (doc.Double("x") > 0 && doc.Double("y") > 0)
                    {
                        var loc = new BsonArray();
                        loc.Add(doc.Double("x"));
                        loc.Add(doc.Double("y"));
                        addDoc.Set("loc", loc);
                    }
                    keyWordEntRelateGuidLimitFilter.Add(guid);
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = addDoc, Name = DataTableName_KeyWordEntRelation, Type = StorageType.Insert });
                }
            }
            catch (Exception ex)
            {
                ShowMessageInfo(ex.Message + "BuildKeyWordEntRelate");
            }
        }
        
        #endregion
        #region 系统初始化
        List<BsonDocument> proxyList = new List<BsonDocument>();
        private void InitialProxyList()
        {

            //proxyList.Add(new BsonDocument().Add("name", "经典版")
            //    .Add("proxyUser", "H283EZ4CP1YFQCRC").Add("proxyPass", "2BAB4571505B4807")
            //    .Add("proxyHost", "http://http-cla.abuyun.com").Add("proxyPort", "9030"));

            proxyList.Add(new BsonDocument().Add("name", "专业版本")
              .Add("proxyUser", ConstParam.proxyUser).Add("proxyPass", ConstParam.proxyPass)
              .Add("proxyHost", "http://http-pro.abuyun.com").Add("proxyPort", ConstParam.proxyPort));

            proxyList.Add(new BsonDocument().Add("name", "专业版本")
             .Add("proxyUser", "HY0N2Z8VXA235EHP").Add("proxyPass", "8CF42B3A5C35E1CC")
             .Add("proxyHost", "http://http-pro.abuyun.com").Add("proxyPort", ConstParam.proxyPort));

            proxyList.Add(new BsonDocument().Add("name", "专业版本")
             .Add("proxyUser", "HL84346425F5JWDP").Add("proxyPass", "8090C1A440811D4B")
             .Add("proxyHost", "http://http-pro.abuyun.com").Add("proxyPort", ConstParam.proxyPort));

            proxyList.Add(new BsonDocument().Add("name", "专业版本")
            .Add("proxyUser", "HY0N2Z8VXA235EHP").Add("proxyPass", "8CF42B3A5C35E1CC")
            .Add("proxyHost", "http://http-pro.abuyun.com").Add("proxyPort", ConstParam.proxyPort));


            this.proxyListCB.Items.Add("请选择");
            foreach (var proxyItem in proxyList)
            {
                this.proxyListCB.Items.Add(proxyItem.Text("proxyUser"));//0

            }
            //this.proxyListCB.SelectedIndex = proxyList.Count() - 1;
        }
        private void InitialCountyCodeList()
        {
            allCountyCodeList = dataop.FindAll(DataTableCountyCode).ToList();
        }
        private void InitInitLimitIpList()
        {
            var LimitIpPoorList = dataop.FindAll(LimitIpPoor).ToList();
            foreach (var ipObj in LimitIpPoorList)
            {
                if (!ipLimitFilter.Contains(ipObj.Text("ip").Trim()))
                    ipLimitFilter.Add(ipObj.Text("ip").Trim());
            }

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

            if (!String.IsNullOrEmpty(PublicSettings.Default.PublicDBName))
                globalDBName = PublicSettings.Default.PublicDBName;
            
                globalDBHasPassWord = PublicSettings.Default.PublicHasDBPassword;
          

            //globalDBHasPassWord = PublicSettings.Default.PublicHasDBPassword;
            if (PublicSettings.Default.PublicPort != 0) {
                enterprisePort = PublicSettings.Default.PublicPort;
            }
            if (!String.IsNullOrEmpty(PublicSettings.Default.PublicRegBegin))
                GRegistCapiBegin = PublicSettings.Default.PublicRegBegin;

            if (!String.IsNullOrEmpty(PublicSettings.Default.PublicRegEnd))
                GRegistCapiEnd = PublicSettings.Default.PublicRegEnd;


            if (!String.IsNullOrEmpty(PublicSettings.Default.PublicDataTable))
                this.EnterpriseKeySuffixTxt.Text = PublicSettings.Default.PublicDataTable;

            if (!String.IsNullOrEmpty(PublicSettings.Default.PublicDeviceAccountDataTable))
                qccDeviceAccountName = PublicSettings.Default.PublicDeviceAccountDataTable;

            
            allAccountHashMapList = dataop.FindAll(DataTableAccountHashMap).ToList();
            oldCityList = dataop.FindAll(DataTableCity).ToList();
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
            this.comboBox.Items.Add("投资关系");//7
            this.comboBox.Items.Add("新增企业（Vip登陆）");//8
            this.comboBox.Items.Add("新增企业岗位");//9
            this.comboBox.Items.Add("背后关系");//10

            //var cityNameStr = "地块企业,佛山,北京,西安,烟台,上海,深圳,成都,福州,广州,杭州,黄山,济南,龙岩,南昌,南京,宁波,泉州,苏州,武汉,厦门,大连,长沙,合肥,镇江,宁波,中山,郑州,昆明,江苏,重庆";
            var cityNameStr = "自贡,株洲,漳州,西安,唐山,上饶,厦门,泉州,三亚,梅州,惠州,济南,合肥,温岭,南充,绵阳,张家口,仙桃,宁波,扬州,泸州,江阴,马鞍山,太原,兰州,长春,海口,北海,南宁,保定,南京,苏州,常州,无锡,南通,西安,烟台,佛山,泉州,北京,上海,广州,深圳,成都,昆明,大连,青岛,哈尔滨,沈阳,日照,南宁,武汉,长沙,合肥,济南,郑州,南昌,天津,杭州,兰州,长春,海口,西宁,石家庄,贵阳,西宁,乌鲁木齐,呼和浩特,银川,拉萨,福州,厦门,漳州,莆田,三明,南平,龙岩,宁德市,宁德地区,东莞,重庆,嘉兴,惠州,珠海,汕头,中山,湛江,泉州,龙岩,南通,常州,镇江,连云港,舟山,黄山,烟台,";
            // var cityNameStr = "广州,韶关,深圳,珠海,汕头,佛山,江门,湛江,茂名,肇庆,惠州,梅州,汕尾,河源,阳江,清远,东莞,中山,潮州,揭阳,云浮";
            var provinceCityList = new List<string>();
            var provinceCode = textBox.Text;
            provinceCityList = dataop.FindAllByQuery(DataTableCity,
                Query.And(Query.EQ("provinceCode", provinceCode), Query.EQ("type", "1"))).Select(c => c.Text("name")).ToList();
            var cityNames = new List<string>();
            if (provinceCityList.Count > 0)
            {
                cityNames = provinceCityList;
            }
            else
            {
                cityNames = cityNameStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            foreach (var cityName in cityNames)
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
            EnterpriseInfoMapDic_App.Add("X", "x");
            EnterpriseInfoMapDic_App.Add("Y", "y");
            EnterpriseInfoMapDic_App.Add("TaxNo", "taxNo");
            EnterpriseInfoMapDic_App.Add("Industry", "industry");
            ///更新时间与数据库中的更新时间不同
            EnterpriseInfoMapDic_App.Add("UpdatedDate", "companyUpdatedDate");
            EnterpriseInfoMapDic_App.Add("HitReason", "HitReason");
            #endregion

            Settings.MaxReTryTimes = 100;//尝试最大个数
            //加载账号
            ReloadLoginAccount();
            ///用于判断是否过滤关键字
            PassKeyWordFilterCondition.Add(new BsonDocument().Add("addCount", 10).Add("existCount", 2000));
            PassKeyWordFilterCondition.Add(new BsonDocument().Add("addCount", 100).Add("existCount", 4000));
            PassKeyWordFilterCondition.Add(new BsonDocument().Add("addCount", 200).Add("existCount", 10000));
            PassKeyWordFilterCondition.Add(new BsonDocument().Add("addCount", 300).Add("existCount", 15000));
            PassKeyWordFilterCondition.Add(new BsonDocument().Add("addCount", 400).Add("existCount", 20000));
            PassKeyWordFilterCondition.Add(new BsonDocument().Add("addCount", 500).Add("existCount", 25000));
            PassKeyWordFilterCondition.Add(new BsonDocument().Add("addCount", 1000).Add("existCount", 30000));
            PassKeyWordFilterCondition.Add(new BsonDocument().Add("addCount", 1500).Add("existCount", 35000));
            PassKeyWordFilterCondition.Add(new BsonDocument().Add("addCount", 2000).Add("existCount", 40000));
            PassKeyWordFilterCondition.Add(new BsonDocument().Add("addCount", 2500).Add("existCount", 50000));
            PassKeyWordFilterCondition.Add(new BsonDocument().Add("addCount", 3000).Add("existCount", 60000));
            PassKeyWordtimer.Enabled = true;
            PassKeyWordtimer.Start();
            InitInitLimitIpList();
            InitialCountyCodeList();
            InitialProxyList();
            InitialMQ();
        }

        private void InitialMQ(string virtualHost= "mz.core.enterprise_info")
        {
            
            MQHelper.Instance().Init(virtualHost);
        }
        /// <summary>
        /// 推送消息到队列
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private async Task<bool> PushMessageAsync(BsonDocument doc)
        {
            var result = await MQHelper.Instance().PublishAsync<String>(doc.ToJson());
            return result;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            UrlInfo curGlobalUrlInfo = null;
            try
            {
                if (autoChangeAccountCHK.Checked)//自动切换开启中
                {
                    if (DateTime.Now.Hour >= 123)//11点进行关闭
                    {
                        Application.Exit();
                    }
                }
                if (UrlQueueCount() > 0)
                {
                    var maxRetryTimes = Settings.MaxReTryTimes != 0 ? Settings.MaxReTryTimes : 3;
                    //优先错误尝试列表中的数据
                    UrlInfo curUrlObj = UrlRetryQueue.Instance.Count > 0 ? UrlRetryQueue.Instance.DeQueue() : UrlQueue.Instance.DeQueue();
                    if (curUrlObj != null && !string.IsNullOrEmpty(curUrlObj.UrlString))
                    {
                        var startDateBeginStr = GetUrlParam(curUrlObj.UrlString, "startDateBegin");
                        var startDateEndStr= GetUrlParam(curUrlObj.UrlString, "startDateEnd");
                        //if (startDateBeginStr.Length == 4&& startDateEndStr.Length == 4)
                        //{
                        //    var startDateBegin = string.Format("{0}0101", startDateBeginStr);
                        //    var startDateEnd = string.Format("{0}1231", startDateEndStr);
                        //    curUrlObj.UrlString = ReplaceUrlParamEx(curUrlObj.UrlString, "startDateBegin", startDateBegin);
                        //    curUrlObj.UrlString = ReplaceUrlParamEx(curUrlObj.UrlString, "startDateEnd", startDateEnd);
                        //}
                         
                      

                        curGlobalUrlInfo = curUrlObj;
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
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                        catch (Exception ex)//异重试常超时
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                        {
                            timerStop();
                            curUrlObj.Depth = curUrlObj.Depth + Settings.MaxReTryTimes / 2;
                            AddUrlQueue(curUrlObj);

                            return;
                        }

                        if (curUrlObj.Depth >= maxRetryTimes)//尝试超过三次,用于企业信息json查找
                        {
                            ///添加错误的url列表
                            if (UrlContentLimit(result.Html) && searchType != SearchType.UpdateEnterpriseInfoAPP)
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
                        var curUrl = ClearUrlSignStr(curUrlObj);
                        var args = new DataReceivedEventArgs() { Depth = curUrlObj.Depth, Html = result.Html, IpProx = null, Url = curUrl, urlInfo = curUrlObj };

                        if (result != null && result.StatusCode == HttpStatusCode.OK && !result.Html.Contains("Service Unavailable") && !IPLimitProcess(args))
                        {
                            TimerReset();
                            DataReceive(args);
                            StartDBChangeProcessQuick();
                            //AutoChangeAccount();
                            //StartDBChangeProcess();
                            //StartDBChangeProcessQuick();
                        }
                        else
                        {
                            
                            //请求次数太多
                            ///result.Html
                            if (result.Html.Contains("Too many requests!!!"))
                            {
                                timerStop();
                                UrlRetryQueue.Instance.EnQueue(curUrlObj);//重试
                            }
                            else
                            {
                                if (result.Html.Contains("异常"))
                                {
                                    TimerSlow();
                                }
                                curUrlObj.Depth = curUrlObj.Depth + 1;
                                UrlRetryQueue.Instance.EnQueue(curUrlObj);//重试
                            }
                           
                            AutoChangeAccount();
                        }
                    }

                }
                else
                {

                    timerStop();
                    ShowMessageInfo($"{Settings.SearchKeyWord}正在尝试提取关键字");
                    if (StringQueue.Instance.Count > 0 && (UrlQueueCount() <= 0))
                    {
                        ///5秒内只能执行一次防止重复太多
                        if (ContinueMethodByBusyGear("InitalQCCAppUrlByKeyWord", 2))
                        {
                            timerStop();
                            var curKeyWord = StringQueue.Instance.DeQueue();
                            if (!string.IsNullOrEmpty(curKeyWord))
                            {
                                var urlList = InitalQCCAppUrlByKeyWord(curKeyWord);
                                foreach (var url in urlList)
                                {
                                    if (!urlFilter.Contains(url))
                                    {
                                        AddUrlQueue(url);
                                        ShowMessageInfo("成功提取关键字");
                                        timerStart();
                                    }

                                }
                            }
                        }

                    }
                    else
                    {

                        waitBrowerMouseUpResponse = false;
                        ShowMessageInfo(string.Format("当前数据更新结束，请单击crawler重新当前操作已获取{0}", AllAddCount.ToString()));
                        if (checkBoxGuard.Checked && searchType == SearchType.UpdateEnterpriseInfoAPP)
                        {
                            //每次获取新的数据
                             startCrawlerBtn.Invoke(new Action(() => { startCrawlerBtn_Click(source, e); }));

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //UrlQueue.Instance.EnQueue()
                //MessageBox.Show(ex.Message);
                ShowMessageInfo(ex.Message);
                if (curGlobalUrlInfo != null)
                {
                    AddRetryUrlQueue(curGlobalUrlInfo, curGlobalUrlInfo.UrlString);
                }
                var exUrl = curGlobalUrlInfo != null ? curGlobalUrlInfo.UrlString : "";
                LogHelper.Info($"{ex.Message}\n{exUrl}");
                timerStop();
            }
        }
        /// <summary>
        /// 初始化代理
        /// </summary>

        #region 不常用
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
            if (string.IsNullOrEmpty(cookies))
            {
                cookies = "";
            }
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
            var aLinks = documentText.GetElementsByTagName("button");
            foreach (HtmlElement alink in aLinks)
            {

                if (alink.InnerText != null && alink.InnerText.Contains("立即登录"))
                {
                    alink.Click += (sender1,e1)=> {
                        Task.Delay(int.Parse(textBox5.Text)).ContinueWith((t)=> {
                            JumpSearchUrl();
                        });
                            
                     };
                    break;
                }
            }

            // documentText.MouseUp += new HtmlElementEventHandler(webBrowser_MouseUP);

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("是否确定关闭", "提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                try
                {
                    timerStop();
                    SaveKeyWordHitCount();
                    AccountRelease(Settings.LoginAccount);

                    PublicSettings.Default.PubilcUserName = this.textBox1.Text.Trim();
                    PublicSettings.Default.PubilcPassword = this.textBox2.Text.Trim();
                    PublicSettings.Default.PubilcTakeCount = this.textBox3.Text.Trim();
                    PublicSettings.Default.PubilcTimerElapse = this.textBox5.Text.Trim();
                    PublicSettings.Default.PublicProxyUid = this.ipProxyTxt.Text.Trim();
                    PublicSettings.Default.PublicProxyPwd = this.ipProxyTxt2.Text.Trim();
                    PublicSettings.Default.PublicDBName = globalDBName;
                    PublicSettings.Default.PublicHasDBPassword = globalDBHasPassWord;
                    PublicSettings.Default.PublicPort = enterprisePort;
                    PublicSettings.Default.PublicRegBegin = GRegistCapiBegin;
                    PublicSettings.Default.PublicRegEnd = GRegistCapiEnd;
                    PublicSettings.Default.PublicDataTable = this.EnterpriseKeySuffixTxt.Text;
                    PublicSettings.Default.PublicDeviceAccountDataTable = qccDeviceAccountName; ;
                    PublicSettings.Default.Save();
                    MQHelper.Instance().Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "Form1_FormClosing");
                }
                
               // e.Cancel = false;
                //  Application.Exit();
            }
            else
            {
                e.Cancel = true;
            }
        }

        //private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        //{
        //    //执行父父目录的自动更新程序
        //    //var curDir = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        //    //if (curDir != null && curDir.Parent != null)
        //    //{
        //    //    System.IO.FileInfo hitWEBSiteUpdate = curDir.Parent.GetFiles().Where(c => c.Name == "WEBSiteUpdate.exe").FirstOrDefault();
        //    //    var parent = curDir.Parent;
        //    //    var maxLevel = 4;
        //    //    while (hitWEBSiteUpdate == null && parent != null && maxLevel >= 0)
        //    //    {
        //    //        hitWEBSiteUpdate = parent.GetFiles().Where(c => c.Name == "WEBSiteUpdate.exe").FirstOrDefault();
        //    //        parent = parent.Parent;
        //    //        maxLevel--;
        //    //    }
        //    //    if (hitWEBSiteUpdate != null)
        //    //    {
        //    //        var thread = new Thread(delegate ()
        //    //        {
        //    //            ExecProcess(hitWEBSiteUpdate.FullName);
        //    //        });
        //    //        thread.Start();
        //    //    }
        //    //}
        //    //if (hi != null)
        //    //{
        //    //    hi.Dispose();
        //    //}
        //}
        /// <summary>
        /// 定时timer时间
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>

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
        #region 其他
        private void webBrowser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            if (e != null)
            {
                curUri = e.Url;
                if (curUri != null)
                {
                    var cookies = FullWebBrowserCookie.GetCookieInternal(curUri, false);
                    Settings.SimulateCookies = cookies;
                }
            }

        }

        /// <summary>
        /// 拉动验证码后事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void webBrowser_MouseUP(object sender, EventArgs e)
        {
            try
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
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
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
            curEnterpriseKeySuffixTxt = EnterpriseKeySuffixTxt.Text;
            this.requestNewVersionCHk.Enabled = false;
            if (!string.IsNullOrEmpty(this.MaxAccountCrawlerCountTxt.Text))
            {
                int crawlerCount = 0;
                if (int.TryParse(this.MaxAccountCrawlerCountTxt.Text, out crawlerCount))
                {
                    var random = new Random();
                    var min = (int)(crawlerCount * 0.9);
                    var max = (int)(crawlerCount * 1.1);
                    var curCrawlerCount = random.Next(min, max);
                    //最大化利用账号
                    Settings.MaxAccountCrawlerCount = curCrawlerCount;
                }
            }
            ///重载uri
            if (curUri != null)
            {
                var cookies = FullWebBrowserCookie.GetCookieInternal(curUri, false);
                Settings.SimulateCookies = cookies;
            }
            if (string.IsNullOrEmpty(Settings.LoginAccount))
            {
                Settings.LoginAccount = this.textBox1.Text;
            }
           
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
                case (int)SearchType.ADDEnterpriseAPP:
                    searchType = SearchType.ADDEnterpriseAPP;
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    //Settings.MaxAccountCrawlerCount = 0;
                    break;
                case (int)SearchType.EnterprisePositionDetailByGuid_APP:
                    searchType = SearchType.EnterprisePositionDetailByGuid_APP;
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    //Settings.MaxAccountCrawlerCount = 0;
                    break;
                case (int)SearchType.EnterpriseBackGroundDetailByGuid_APP:
                    searchType = SearchType.EnterpriseBackGroundDetailByGuid_APP;
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    //Settings.MaxAccountCrawlerCount = 0;
                    break;
                case (int)SearchType.EnterpriseInvent:
                    searchType = SearchType.EnterpriseInvent;
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    //Settings.MaxAccountCrawlerCount = 0;
                    break;
                case (int)SearchType.UpdateEnterpriseInfoAPP:
                default:
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    searchType = SearchType.UpdateEnterpriseInfoAPP;
                    break;

            }
            siteIndexUrl = validUrl;
            curUri = new Uri(siteIndexUrl);
            if (UrlQueueCount() <= 0)
            {
               // this.webBrowser.Navigate(addCredsToUri(validUrl));

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
            Logout(false);
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
                    try
                    {
                        ///重载uri
                        if (curUri != null)
                        {
                            var cookies = FullWebBrowserCookie.GetCookieInternal(curUri, false);
                            this.richTextBox.Clear();
                            this.richTextBox.AppendText("重载" + cookies);
                            Settings.SimulateCookies = cookies;
                        }
                    }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                    catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                    {

                    }
                }));
                if (UserProxyChk.Checked)
                {
                    ShowMessageInfo("开始切换ip");
                    var result = GetHttpHtml(new UrlInfo(proxyHost + "/switch-ip"));
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
                if (GetAccessRetryTimes <= 0)
                {
                    //发送错误通知邮件
                    SendErrorMessage("getAccessToken 出错");
                }
            }
        }
        /// <summary>
        /// 发送错误通知邮件
        /// </summary>
        /// <param name="message"></param>
        private void SendErrorMessage(string message)
        {
            try
            {
                var msgHelper = new MessagePushQueueHelper();
                msgHelper.PushMessage(new MessagePushEntity() { arrivedUserIds = "1", title = "企查查爬虫", content = message, type = "0", sendDate = DateTime.Now.AddMinutes(5).ToString("yyyy-MM-dd mm:ss"), sendType = "0" });
            }
            catch (Exception ex)
            {
                ShowMessageInfo(ex.Message);
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            // AppAutoLoin("15959266823","qwer1234");

            // var rHtml = url.UrlGetHtml();
            var url = "";//test
            var result = GetHttpHtmlAPP(new UrlInfo(url));


            ShowMessageInfo(result.Html);

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
            switch (accountRegisterType)
            {
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
            switch (accountRegisterType)
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
                var _url = new UrlInfo(ConstParam.SendvalidateUrlV2);
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
#pragma warning disable CS0436 // “G:\MN测试Project\SimpleCrawler-master\SimpleCrawler-master\QCCWebBrowser\DESCryptDecodeHelper.cs”中的类型“DESCryptDecodeHelper”与“Helper, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null”中的导入类型“DESCryptDecodeHelper”冲突。请使用“G:\MN测试Project\SimpleCrawler-master\SimpleCrawler-master\QCCWebBrowser\DESCryptDecodeHelper.cs”中定义的类型。
                var encodePhone = HttpUtility.UrlEncode(DESCryptDecodeHelper.Decode(phoneNum, "soufunss"));
#pragma warning restore CS0436 // “G:\MN测试Project\SimpleCrawler-master\SimpleCrawler-master\QCCWebBrowser\DESCryptDecodeHelper.cs”中的类型“DESCryptDecodeHelper”与“Helper, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null”中的导入类型“DESCryptDecodeHelper”冲突。请使用“G:\MN测试Project\SimpleCrawler-master\SimpleCrawler-master\QCCWebBrowser\DESCryptDecodeHelper.cs”中定义的类型。
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

                var _url = new UrlInfo(ConstParam.RegisterUrlV2);
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
#pragma warning disable CS0436 // “G:\MN测试Project\SimpleCrawler-master\SimpleCrawler-master\QCCWebBrowser\DESCryptDecodeHelper.cs”中的类型“DESCryptDecodeHelper”与“Helper, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null”中的导入类型“DESCryptDecodeHelper”冲突。请使用“G:\MN测试Project\SimpleCrawler-master\SimpleCrawler-master\QCCWebBrowser\DESCryptDecodeHelper.cs”中定义的类型。
            var encodePhone = HttpUtility.UrlEncode(DESCryptDecodeHelper.Decode(phone, "soufunss"));
#pragma warning restore CS0436 // “G:\MN测试Project\SimpleCrawler-master\SimpleCrawler-master\QCCWebBrowser\DESCryptDecodeHelper.cs”中的类型“DESCryptDecodeHelper”与“Helper, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null”中的导入类型“DESCryptDecodeHelper”冲突。请使用“G:\MN测试Project\SimpleCrawler-master\SimpleCrawler-master\QCCWebBrowser\DESCryptDecodeHelper.cs”中定义的类型。
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
                var userName = Toolslib.Str.Sub(ho.TxtData, "username\":\"", "\"");//用户名
                var issuccess = Toolslib.Str.Sub(ho.TxtData, "issuccess\":\"", "\"");//成功代码
                return true;
            }
            return false;
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
                case (int)SearchType.EnterpriseInvent:
                    searchType = SearchType.EnterpriseInvent;
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    break;
                case (int)SearchType.ADDEnterpriseAPP:
                    searchType = SearchType.ADDEnterpriseAPP;
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    break;
                    //岗位信息
                case (int)SearchType.EnterprisePositionDetailByGuid_APP:
                    searchType = SearchType.EnterprisePositionDetailByGuid_APP;
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    break;
                case (int)SearchType.EnterpriseBackGroundDetailByGuid_APP:
                    searchType = SearchType.EnterpriseBackGroundDetailByGuid_APP;
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    break;
                
                case (int)SearchType.UpdateEnterpriseInfoAPP:
                default:
                    validUrl = "http://www.qichacha.com/firm_ZJ_c29fb59a50a8d6f0cab90a2dac54cbf8.shtml";
                    searchType = SearchType.UpdateEnterpriseInfoAPP;

                    break;

            }
            if (searchType == SearchType.UpdateEnterpriseInfoAPP|| searchType == SearchType.EnterpriseBackGroundDetailByGuid_APP|| searchType == SearchType.EnterpriseInvent)
            {
                mqPushChk.Checked = true;
                timerStop();
                if (searchType == SearchType.EnterpriseBackGroundDetailByGuid_APP)
                {
                    InitialMQ("mz.core.enterprise_backgroundInfo");
                }
                else {
                    if (searchType == SearchType.EnterpriseInvent)
                    {
                       
                        InitialMQ("mz.core.enterprise_inventInfo");
                    }
                    else { InitialMQ(); }
                  
                }
               
            }
            else
            {
                mqPushChk.Checked = false;
            }
           

        }

        private void button7_Click(object sender, EventArgs e)
        {
            List<string> QueueList = new List<string>();
            if (UrlQueueCount() > 0)
            {
                while (UrlQueueCount() > 0)
                {
                    var urlObj = UrlQueue.Instance.DeQueue();
                    if (urlObj == null) {
                        QueueList.Add(urlObj.UrlString);
                    }
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

            MessageBox.Show(string.Format("url:{0},KeyWord:{1}", QueueList.Count.ToString(), keyWordQueueList.Count()));


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
            MessageBox.Show("url:{0}keyWord:{1}", UrlQueueCount().ToString());
        }

        private void ipChangeTimer_Tick(object sender, EventArgs e)
        {
            if (UserProxyChk.Checked)
            {
                ShowMessageInfo("开始切换ip" + GetWebProxyString());
                ChangeIp();

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
            var provinceCode = textBox.Text;
            var provinceCityList = dataop.FindAllByQuery(DataTableCity,
                Query.And(Query.EQ("provinceCode", provinceCode), Query.EQ("type", "1"))).Select(c => c.Text("name")).ToList();
            var cityNames = new List<string>();
            if (provinceCityList.Count > 0)
            {
                cityNames = provinceCityList;
            }
            else
            {
                cityNames = cityNameStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            this.comboBox2.Items.Clear();
            foreach (var cityName in cityNames)
            {
                this.comboBox2.Items.Add(cityName);
            }
            // LandFangSendPhoneCode("17080371218");
            return;
#pragma warning disable CS0162 // 检测到无法访问的代码
            accountRegisterType = AccountRegisterType.LandFang;
#pragma warning restore CS0162 // 检测到无法访问的代码
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

            PassKeyWord();
        }

        /// <summary>
        /// 定时器监控查看是否可以跳过当前关键字
        /// 目前规则根据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PassKeyWordtimer_Tick(object sender, EventArgs e)
        {
            var curKeyWordCount = SetKeyWordHitCount(curKeyWordStr, 0);//设置更新个数
            if (StringQueue.Instance.Count > 0)
            {
                ProcessCanAutoPassKeyWord(curKeyWordCount, curKeyWordStr);
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(proxyIpDetail))
            {
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("ip", proxyIpDetail), Name = LimitIpPoor, Type = StorageType.Insert });
                if (!ipLimitFilter.Contains(proxyIpDetail) && !string.IsNullOrEmpty(proxyIpDetail))
                {
                    ipLimitFilter.Add(proxyIpDetail);
                }
                StartDBChangeProcessQuick();
            }
        }

        private void proxyListCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            string text = (sender as ComboBox).SelectedItem as string;
            if (text == null) return;
            var hitProxy = proxyList.Where(c => c.Text("proxyUser") == text).FirstOrDefault();
            if (hitProxy != null)
            {
                proxyHost = hitProxy.Text("proxyHost");
                proxyPort = hitProxy.Text("proxyPort");
                proxyUser = hitProxy.Text("proxyUser");
                proxyPass = hitProxy.Text("proxyPass");
                ShowMessageInfo("切换代理成功");
            }

        }
        #endregion

        #endregion

        private void button14_Click(object sender, EventArgs e)
        {
            SetEnterpriseDataOP(enterpriseIp);
            var finalObj = enterpriseDataop.FindLimitByQuery(DataTableName, null, new SortByDocument() { { "_id", -1 } }, 0, 1).FirstOrDefault();
            if (finalObj != null)
            {
                this.updateDateTxt.Text = finalObj.Date("createDate").ToString("yyyyMMdd");
            }
        }

        private void requestNewVersionCHk_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void mqPushChk_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button15_Click(object sender, EventArgs e)
        {
            var url = "";//测试url搜索请求
            var testDic = new Dictionary<string, int>();
            testDic.Add("1989", 100);
            testDic.Add("1991", 10);
            testDic.Add("1990", 20);
            testDic.Add("1993", 50);
            testDic.Add("1995", 20);
            testDic.Add("2000", 20);
            testDic.Add("2002", 50);
            testDic.Add("2004", 80);
            testDic.Add("2010", 10);
            testDic.Add("2015", 15);
            testDic.Add("2018", 10);
            testDic.Add("2019", 1);
            testDic.Add("2020", 4);
            IsDateDataUrlSplited_Aggregate_JsonData(new DataReceivedEventArgs() { urlInfo = new UrlInfo(url), Url = url }, testDic, 5000);
            //startDateBegin=17700101&startDateEnd=18950101&registCapiBegin=0&registCapiEnd=&industryV3=&industryCode=A&subIndustryCode=02&searchType=&countyCode=&cityCode=1
             return;
#pragma warning disable CS0162 // 检测到无法访问的代码
            UrlSplitedEnqueue(new UrlInfo(url), url, 5000);
#pragma warning restore CS0162 // 检测到无法访问的代码
            while (UrlQueue.Instance.Count> 0)
            {
                var curUrlInfo = UrlQueue.Instance.DeQueue();
                IterationSplitModeActive(new DataReceivedEventArgs() {  urlInfo= curUrlInfo ,Url= curUrlInfo.UrlString},null,5000);
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {

            JumpSearchUrl();

        }

        private void button17_Click(object sender, EventArgs e)
        {
            if (hasLoadInvalidAccount)
            {
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("exceptionAgain", 1), Query = Query.EQ("name", this.textBox1.Text), Name = DataTableAccount, Type = StorageType.Update });
                StartDBChangeProcessQuick();
            }
            Logout();
            searchBtn_Click(sender, e);
           
        }
        private void JumpSearchUrl()
        {
            var url = "https://www.qcc.com/search?key=%E8%AE%A1%E7%AE%97%E6%9C%BA";
            this.webBrowser.Navigate(addCredsToUri(url));
        }
        private void Logout(bool loadInvalidAccount=true)
        {
            var aLinks = documentText.GetElementsByTagName("a");
            if (aLinks != null && aLinks.Count >= 0)
            {
                foreach (HtmlElement alink in aLinks)
                {

                    if (alink.InnerText != null && alink.InnerText.Contains("退出登录"))
                    {
                        alink.InvokeMember("click");
                        continue;
                    }
                }
            }

            //var url = " http://www.qichacha.com/user_login";
            //this.webBrowser.Navigate(addCredsToUri(url));
            if (loadInvalidAccount)
            {
                LoadInvalidAccount();
                this.comboBox1.Items.Clear();
                foreach (var account in allAccountList.OrderBy(c => Guid.NewGuid()))
                {
                    this.comboBox1.Items.Add(account.Text("name"));
                }
                if (allAccountList.Count > 0)
                {
                    this.comboBox1.SelectedIndex = 0;
                }
            }
            
        }

        private void changeValidBtn_Click(object sender, EventArgs e)
        {
            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isInvalid", ""), Query = Query.EQ("name", this.textBox1.Text), Name = DataTableAccount, Type = StorageType.Update });
            StartDBChangeProcessQuick();
            Logout();
            searchBtn_Click(sender, e);
        }
    }
}
