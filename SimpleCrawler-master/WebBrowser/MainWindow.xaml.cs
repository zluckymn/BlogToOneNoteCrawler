using HtmlAgilityPack;
using LibCurlNet;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using mshtml;
using Newtonsoft.Json.Linq;
using SimpleCrawler;
using SimpleCrawler.Demo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace WebBrowser
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 变量
        private static string connStr = "mongodb://MZsa:MZdba@59.61.72.34:37088/SimpleCrawler";
        // private static string connStr = "mongodb://MZsa:MZdba@59.61.72.34:37088/Shared";
        static DataOperation dataop = new DataOperation(new MongoOperation(connStr));
        static MongoOperation _mongoDBOp = new MongoOperation(connStr);
        private const string _DataTableName = "QiXinEnterpriseKey";//存储的数据库表明
        private static CrawlSettings Settings = new CrawlSettings();
        Dictionary<string, string> cityNameDic = new Dictionary<string, string>();
        private static bool canNextUrl = true;
        Uri curUri = null;

        string validUrl = "http://www.qixin.com/company/network/e6c8b0b6-a2b7-4ab3-8403-3ec6215d683b?name=%E6%B5%99%E6%B1%9F%E6%B7%98%E5%AE%9D%E5%A4%A7%E5%AD%A6%E6%9C%89%E9%99%90%E5%85%AC%E5%8F%B8";
        string curTimerElapse = string.Empty;
        List<BsonDocument> allAccountList = new List<BsonDocument>();
        Dictionary<string, string> EnterpriseInfoMapDic = new Dictionary<string, string>();
        PassGeetestHelper geetestHelper = new PassGeetestHelper();
        public int PassInValidTimes = 5;
        public int PassSuccessTimes = 20;
        private string cityNameStr = "北京";
        List<string> existGuidList = new List<string>();
        //string[] enterpriseInfoUrlType = new string[] {  "getRiskInfo", "getAbilityInfo", "getInvestedCompaniesById", "getAnnualReport", "getOperationInfo" };
        string[] enterpriseInfoUrlType = new string[] { "getInvestedCompaniesById" };
        System.Timers.Timer aTimer = new System.Timers.Timer();
        System.Timers.Timer autoRestartTimer = new System.Timers.Timer();
        HttpInput hi = new HttpInput();
        SimpleCrawler.HttpHelper http = new SimpleCrawler.HttpHelper();
        bool waitBrowerMouseUpResponse = false;
        SearchType searchType = SearchType.UpdateEnterpriseInfo;
        private mshtml.HTMLDocumentEvents2_Event documentEvents;
        private mshtml.IHTMLDocument2 documentText;
        static string curHtml = string.Empty;
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
            /// 更新企业信息
            /// </summary>
            [EnumDescription("UpdateEnterpriseCompnayInfo")]
            UpdateEnterpriseCompnayInfo = 4,
            /// <summary>
            /// 通过地区分类关键字搜索企业Guid
            /// </summary>
            [EnumDescription("EnterpriseGuidByKeyWord")]
            EnterpriseGuidByKeyWord = 5,


        }
        /// <summary>
        /// 返回
        /// </summary>
        public static string DataTableName
        {
            get { return _DataTableName; }

        }

        /// <summary>
        /// 返回
        /// </summary>
        public static string DataTableNameURL
        {
            get { return _DataTableName + "URL"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public static string DataTableNameList
        {
            get { return "QiXinEnterprise"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public static string DataTableAccount
        {
            get { return "QiXinAccount"; }

        }

        /// <summary>
        /// 返回
        /// </summary>
        public static string DataTableHolder
        {
            get { return "QiXinEnterpriseHolder"; }

        }
        /// <summary>
        /// 股东
        /// </summary>
        public static string DataTableShareHolder
        {
            get { return "QiXinEnterpriseShareHolder"; }

        }


        /// <summary>
        /// 返回
        /// </summary>
        public static string DataTableRelation
        {
            get { return "QiXinEnterpriseRelation"; }

        }
        /// <summary>
        /// html地址
        /// </summary>
        public static string DataTableHtml
        {
            get { return "QiXinEnterpriseHtml"; }

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
            get { return "QiXinEnterpriseCityKeyWord"; }

        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }


        #region 初始化待处理的数据
        /// <summary>
        /// 中转地址
        /// </summary>
        private void InitialEnterpriseData()
        {

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

                case SearchType.UpdateEnterpriseCompnayInfo:
                    InitialEnterpriseCompanyInfo();
                    break;
                case SearchType.UpdateEnterpriseInfo:
                default:
                    InitialEnterpriseInfo();
                    break;
            }

        }

        /// <summary>
        /// 初始化待转化的企业名称
        /// http://www.qixin.com/service/getRiskInfo?eid=aacadab2-4e8c-416d-b1d9-8dc68d65c6e7&_=1469612689725 风险信息
        /// http://www.qixin.com/service/getAbilityInfo?eid=aacadab2-4e8c-416d-b1d9-8dc68d65c6e7&_=1469612689726 知识产权
        /// http://www.qixin.com/service/getInvestedCompaniesById?eid=aacadab2-4e8c-416d-b1d9-8dc68d65c6e7&_=1469612689727 对外投资
        /// http://www.qixin.com/service/getAnnualReport?eid=aacadab2-4e8c-416d-b1d9-8dc68d65c6e7&_=1469612689728 企业年报
        /// http://www.qixin.com/service/getOperationInfo?eid=aacadab2-4e8c-416d-b1d9-8dc68d65c6e7&_=1469612689729 经营信息
        /// http://www.qixin.com/service/getRootNodeInfoByEnterpriseId?enterpriseId={0}&_={1}//企业脉络图
        /// 
        /// </summary>
        private void InitialEnterpriseInfo()
        {
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
            var cityName = string.Empty;
            if (comboBox2.SelectedIndex != -1)
            {
                cityName = comboBox2.SelectedItem.ToString();
               // allEnterpriseList = allEnterpriseList.Where(c => c.Text("cityName").Contains(cityName)).ToList();
            }
            var cityQuery = Query.EQ("cityName", cityName);
            var filterQuery = Query.And(Query.NE("status", "吊销"), Query.NE("status", "注销"));
            var allEnterpriseList = new List<BsonDocument>();
            if (!string.IsNullOrEmpty(cityName))
            {
                allEnterpriseList= dataop.FindAllByQuery(DataTableName, Query.And(Query.Exists("detailInfo", false), cityQuery, filterQuery)).SetFields("name", "guid", "cityName", "provinceName").ToList();
            }
            else {
                allEnterpriseList = dataop.FindAllByQuery(DataTableName, Query.And(Query.Exists("detailInfo", false), filterQuery)).SetFields("name", "guid", "cityName", "provinceName").ToList();
            }
            this.Title= string.Format("{0}剩余个数：{1}条数据", cityName, allEnterpriseList.Count());
          
            var rand = new Random();
            var count = rand.Next(0, allEnterpriseList.Count());
            if (count >= 200)
            {
                allEnterpriseList = allEnterpriseList.Skip(count).Take(takeCount).ToList();
            }
            else
            {
                allEnterpriseList = allEnterpriseList.Take(takeCount).ToList();
            }
            if (allEnterpriseList.Count() > 0)
            {
                foreach (var enterprise in allEnterpriseList)
                {
                    var guidUrl = string.Format("http://www.qixin.com/service/getRootNodeInfoByEnterpriseId?enterpriseId={0}&_={1}", enterprise.Text("guid"), GetTimeLikeJS());
                    UrlQueue.Instance.EnQueue(new UrlInfo(guidUrl) { Depth = 1 });
                    //foreach (var urlType in enterpriseInfoUrlType)
                    //{
                    //    var otherInfoUrl = string.Format("http://www.qixin.com/service/{0}?eid={1}&_={2}", urlType, enterprise.Text("guid"), GetTimeLikeJS());
                    //    UrlQueue.Instance.EnQueue(new UrlInfo(otherInfoUrl) { Depth = 1 });
                    //}
                }
            }
            else
            {
                MessageBox.Show("无数据");
            }

        }

        /// <summary>
        /// 初始化待转化的企业名称http://www.qixin.com/company/{0}
        /// </summary>
        private void InitialEnterpriseCompanyInfo()
        {
            // UrlQueue.Instance.EnQueue(new UrlInfo("http://www.qixin.com/company/00bc8987-6200-47a2-88fb-c0be54b43808") { Depth = 1 });
            //return;
            var takeCount = 0;
            if (!string.IsNullOrEmpty(this.textBox3.Text))
            {
                int.TryParse(this.textBox3.Text.Trim(), out takeCount);
            }
            if (takeCount <= 0)
            {
                takeCount = 100;
            }
            var allEnterpriseList = dataop.FindAllByQuery(DataTableName, Query.And(Query.Exists("detailInfo", false), Query.And(Query.NE("status", "吊销"), Query.NE("status", "注销")))).SetFields("name", "guid", "cityName", "provinceName").ToList();
            //获取key!=1的 或者没有hasPrblem字段的优先,这些是从enterprise地块中优先提取出来的
            var cityName = string.Empty;
            if (comboBox2.SelectedIndex != -1)
            {
                cityName = comboBox2.SelectedItem.ToString();
                allEnterpriseList = allEnterpriseList.Where(c => c.Text("cityName").Contains(cityName)).ToList();
            }
            this.Title = string.Format("{0}剩余个数：{1}条数据", cityName, allEnterpriseList.Count());
            var rand = new Random();
            var count = rand.Next(0, allEnterpriseList.Count());
            if (count >= 200)
            {
                allEnterpriseList = allEnterpriseList.Skip(count).Take(takeCount).ToList();
            }
            else
            {
                allEnterpriseList = allEnterpriseList.Take(takeCount).ToList();
            }
            if (allEnterpriseList.Count() > 0)
            {
                foreach (var enterprise in allEnterpriseList)
                {
                    var guidUrl = string.Format("http://www.qixin.com/company/{0}", enterprise.Text("guid"));
                    UrlQueue.Instance.EnQueue(new UrlInfo(guidUrl) { Depth = 1 });

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
            var allEnterpriseList = dataop.FindAll(DataTableName).SetFields("name", "guid", "oldName").ToList();

            var existNameList = allEnterpriseList.Select(c => c.Text("name")).ToList();
            var existOldNameList = allEnterpriseList.Select(c => c.Text("oldName")).ToList();
            existGuidList = allEnterpriseList.Select(c => c.Text("guid")).ToList();
          
            //var cityNameStr = "上海,北京,成都,福州,广州,杭州,黄山,济南,龙岩,南昌,南京,宁波,泉州,深圳,苏州,武汉,西安,厦门,大连,长沙,合肥,镇江,宁波,中山,郑州,昆明,江苏,重庆";
            var cityNameStr = "北京";
            // var cityNameStr = "上海,北京,成都,福州,广州,杭州,黄山,济南,龙岩,南昌,南京,宁波,泉州,深圳,苏州,武汉,西安,厦门,大连,长沙,合肥,镇江,宁波,中山,郑州,昆明,江苏,重庆";//北京,广州,上海
            var cityNameList = cityNameStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (comboBox2.SelectedIndex != -1)
            {
                cityNameList = new List<string>() { comboBox2.SelectedItem.ToString() };
            }
            var allNeedEnterpriseList = dataop.FindAllByQuery(DataTableNameList,
                Query.And(Query.In("城市", cityNameList.Select(c => (BsonValue)c)),
                Query.EQ("isFirst", "1"), Query.NE("status", "1"), Query.NE("isSearched", "1"))).SetFields("name").Take(200).Select(c => c.Text("name")).ToList();

            //过滤已存在的对象
            allNeedEnterpriseList = allNeedEnterpriseList.Where(c => !existNameList.Contains(c) && !existOldNameList.Contains(c)).ToList();



            Console.WriteLine("初始化数据");
            var updateSB = new StringBuilder();
            foreach (string enterpriseName in allNeedEnterpriseList.Where(c => c.Length > 3))
            {
                //if (allEnterpriseList.Where(c => c.Text("name") == enterpriseName.Trim()).Count() > 0) continue;
                //var enterPriseNameArray = enterpriseName.Split(new string[] { ",","、","，","和"},StringSplitOptions.RemoveEmptyEntries);
                //foreach(var name in enterPriseNameArray){
                enterpriseName.Replace("（）", "");
                var url = string.Format("http://www.qixin.com/search?key={0}&type=enterprise&source=&isGlobal=Y", HttpUtility.UrlEncode(enterpriseName));
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
        /// http://www.qixin.com/search?key=%E7%83%9F%E5%8F%B0+%E9%A3%9F%E5%93%81%E6%B7%BB%E5%8A%A0%E5%89%82&type=enterprise&source=&isGlobal=Y
        /// </summary>
        private void InitialEnterpriseGuidByKeyWord()
        {
            Console.WriteLine("初始化数据");
            var allEnterpriseList = dataop.FindAll(DataTableName).SetFields("name", "guid", "oldName").ToList();
           var existNameList = allEnterpriseList.Select(c => c.Text("name")).ToList();
            var existOldNameList = allEnterpriseList.Select(c => c.Text("oldName")).ToList();
            existGuidList = allEnterpriseList.Select(c => c.Text("guid")).ToList();
         
            //var cityNameStr = "上海,北京,成都,福州,广州,杭州,黄山,济南,龙岩,南昌,南京,宁波,泉州,深圳,苏州,武汉,西安,厦门,大连,长沙,合肥,镇江,宁波,中山,郑州,昆明,江苏,重庆";
            cityNameStr = "北京";
            // var cityNameStr = "上海,北京,成都,福州,广州,杭州,黄山,济南,龙岩,南昌,南京,宁波,泉州,深圳,苏州,武汉,西安,厦门,大连,长沙,合肥,镇江,宁波,中山,郑州,昆明,江苏,重庆";//北京,广州,上海
            var cityNameList = cityNameStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (comboBox2.SelectedIndex != -1)
            {
                cityNameStr= comboBox2.SelectedItem.ToString() ;
            }
          //获取该城市已查询过的城市列表
            var existCityKeyWordList = dataop.FindAllByQuery(DataTableCityKeyWord,Query.EQ("cityName", cityNameStr)).SetFields("keyWord","pageCount", "execPage").Where(c=>c.Text("pageCount")==c.Text("execPage")).Select(c=>(BsonValue)c.Text("keyWord")).ToList();
            //获取关键字列表
            var allKeyWordList = dataop.FindAllByQuery(DataTableKeyWord,Query.NotIn("keyWord", existCityKeyWordList)).SetFields("keyWord").Select(c=>c.Text("keyWord")).ToList();
       
            var updateSB = new StringBuilder();
            foreach (string keyWord in allKeyWordList)
            {
                var url = string.Format("http://www.qixin.com/search?key={0} {1}&type=enterprise&source=&isGlobal=Y", HttpUtility.UrlEncode(cityNameStr), HttpUtility.UrlEncode(keyWord));
                UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });
            }
            //var url = string.Format("http://www.qixin.com/search?key=%E7%83%9F%E5%8F%B0+%E8%AE%A1%E7%AE%97%E6%9C%BA&type=enterprise&source=&isGlobal=Y");
            //UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });
      
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
            var allEnterpriseList = dataop.FindAll(DataTableName).SetFields("name", "guid", "oldName").ToList();
            existGuidList = allEnterpriseList.Select(c => c.Text("guid")).ToList();

            if (!validUrl.ToString().Contains("domain")) return;
            var typeName = HttpUtility.UrlDecode(GetQuerySearchTypeString(validUrl.ToString()));
            //获取当前页面的分页信息
            var item = new HttpItem()
            {
                URL = validUrl,//URL     必需项    
                Method = "get",//URL     可选项 默认为Get   
                ContentType = "text/html",//返回类型    可选项有默认值 
                Timeout = Settings.Timeout,
                Cookie = Settings.SimulateCookies
            };
            HttpResult result = http.GetHtml(item);
            var args = new DataReceivedEventArgs() { Depth = 1, Html = result.Html, IpProx = null, Url = validUrl };
            var typeNameList = new List<string>();
            var pageCount = 20;
            if (!IPLimitProcess(args) && result.StatusCode == HttpStatusCode.OK)
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(args.Html);
                var curUpdateBson = new BsonDocument();

                var searchResult = htmlDoc.DocumentNode.SelectNodes("//div[@class='city-filter-section']/div[@class='prov-filter-right']/a");
                foreach (var aNode in searchResult)
                {
                    var url = aNode.Attributes["href"] != null ? aNode.Attributes["href"].Value : string.Empty;
                    typeNameList.Add(string.Format("http://www.qixin.com{0}", url));
                }
                //var pageDiv = htmlDoc.DocumentNode.SelectNodes("//div[@class='oni-pager search-pager']/a");
                //if (pageDiv != null) {
                //    var pageANode=pageDiv.Where(c => c.Attributes["class"] != null && c.Attributes["class"].Value == "oni-pager-item").LastOrDefault();
                //    if (pageANode != null) {
                //        var curPageCountStr = pageANode.InnerText.Trim();
                //        if (int.TryParse(curPageCountStr, out pageCount))
                //        {

                //        }

                //    }
                //}
            }


            Console.WriteLine("初始化数据");
            var updateSB = new StringBuilder();
            foreach (var typeNameStr in typeNameList)
            {
                for (var i = 1; i <= pageCount; i++)//当前行业大类分页
                {

                    //var url = string.Format("http://www.qixin.com/search/domain/{0}?page={1}", HttpUtility.UrlEncode(typeName), i);
                    var url = string.Format("{0}?page={1}", typeNameStr, i);
                    UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });

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
                cityNameDic.Add("beijing", "北京");
                cityNameDic.Add("shanghai", "上海");
                cityNameDic.Add("guangzhou", "广州");
                cityNameDic.Add("shenzhen", "深圳");
                cityNameDic.Add("tianjin", "天津");
                cityNameDic.Add("chongqing", "重庆");
                cityNameDic.Add("hangzhou", "杭州");
                cityNameDic.Add("chengdu", "成都");
                cityNameDic.Add("nanjing", "南京");
                cityNameDic.Add("suzhou", "苏州");
                cityNameDic.Add("dalian", "大连");
                cityNameDic.Add("wuhan", "武汉");
                cityNameDic.Add("xian", "西安");
                cityNameDic.Add("wuxi", "无锡");
                cityNameDic.Add("xiamen", "厦门");
                cityNameDic.Add("hefei", "合肥");
                cityNameDic.Add("fuzhou", "福州");
                cityNameDic.Add("jinan", "济南");
                cityNameDic.Add("nanning", "南宁");
                cityNameDic.Add("sanya", "三亚");
                cityNameDic.Add("haikou", "海口");
                cityNameDic.Add("zhengzhou", "郑州");
                cityNameDic.Add("shijiazhuang", "石家庄");
                cityNameDic.Add("changsha", "长沙");
                cityNameDic.Add("nanchang", "南昌");
                cityNameDic.Add("taiyuan", "太原");
                cityNameDic.Add("kunming", "昆明");
                cityNameDic.Add("haerbin", "哈尔滨");
                cityNameDic.Add("changchun", "长春");
                cityNameDic.Add("guiyang", "贵阳");
                cityNameDic.Add("huhehaote", "呼和浩特");
                cityNameDic.Add("lanzhou", "兰州");
                cityNameDic.Add("wulumuqi", "乌鲁木齐");
                cityNameDic.Add("yinchuan", "银川");
                cityNameDic.Add("xining", "西宁");
            }
              
            var existGuidList = dataop.FindAll(DataTableName).SetFields("guid").Select(c => c.Text("guid")).ToList();

            if (!validUrl.ToString().Contains("qyml")) return;
            var typeName = HttpUtility.UrlDecode(GetQuerySearchTypeString(validUrl.ToString()));
            //获取当前页面的分页信息
            var item = new HttpItem()
            {
                URL = validUrl,//URL     必需项    
                Method = "get",//URL     可选项 默认为Get   
                ContentType = "text/html",//返回类型    可选项有默认值 
                Timeout = Settings.Timeout,
                Cookie = Settings.SimulateCookies
            };
            HttpResult result = http.GetHtml(item);
            var args = new DataReceivedEventArgs() { Depth = 1, Html = result.Html, IpProx = null, Url = validUrl };
            var typeNameList = new List<string>();

            if (!IPLimitProcess(args) && result.StatusCode == HttpStatusCode.OK)
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(args.Html);
                var curUpdateBson = new BsonDocument();

                var searchResult = htmlDoc.DocumentNode.SelectNodes("//div[@class='all-domains']/ul/li");
                foreach (var liNode in searchResult)
                {
                    var aNode = liNode.ChildNodes.Where(c => c.Name == "a").FirstOrDefault();
                    if (aNode == null) continue;
                    var url = aNode.Attributes["href"] != null ? aNode.Attributes["href"].Value : string.Empty;
                    typeNameList.Add(url);
                }

            }

            var pageCount = 1;
            Console.WriteLine("初始化数据");
            var updateSB = new StringBuilder();
            foreach (var typeNameStr in typeNameList)
            {
                for (var i = 1; i <= pageCount; i++)//当前行业大类分页
                {

                    //var url = string.Format("http://www.qixin.com/search/domain/{0}?page={1}", HttpUtility.UrlEncode(typeName), i);
                    var url = typeNameStr;
                    UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });

                }
            }


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
            ///重载uri

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
            {
                ///重载uri
                if (curUri != null)
                {
                    var cookies = FullWebBrowserCookie.GetCookieInternal(curUri, false);
                    Settings.SimulateCookies = cookies;
                }
            })
           );
            ///账号爬取统计，防止被封，查看爬去个数
            var curLoginAccountObj = allAccountList.Where(c => c.Text("name") == Settings.LoginAccount).FirstOrDefault();
            if (curLoginAccountObj != null)
            {
                var columnName = string.Format("{0}_add", searchType);
                var curAddional = curLoginAccountObj.Int(columnName);
                curLoginAccountObj.Set(columnName, curAddional + 1);
                ShowAccountInfo();
            }
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
                case SearchType.UpdateEnterpriseCompnayInfo:
                    DataReceiveEnterpriseCompnayInfo(args);
                    break;
                case SearchType.UpdateEnterpriseInfo:
                default:
                    DataReceiveEnterpriseInfo(args);
                    break;
            }


        }
        public void DataReceiveEnterpriseInfo(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;

            var curUpdateBson = new BsonDocument();
            var guid = string.Empty;
            var queryStr = GetQueryString(args.Url);
            var infoType = "getRootNodeInfoByEnterpriseId";//获取脉络图方式
            if (!string.IsNullOrEmpty(queryStr))
            {
                var dic = HttpUtility.ParseQueryString(queryStr);
                guid = dic["enterpriseId"] != null ? dic["enterpriseId"].ToString() : string.Empty;
                if (string.IsNullOrEmpty(guid))
                {
                    guid = dic["eid"] != null ? dic["eid"].ToString() : string.Empty;
                    if (string.IsNullOrEmpty(guid))
                    {
                        ShowMessageInfo("获取不到guid");
                        return;
                    }
                }
                var startIndex = args.Url.LastIndexOf("/");
                if (startIndex == -1) return;
                var endIndex = args.Url.IndexOf("?");
                if (endIndex == -1) return;
                infoType = args.Url.Substring(startIndex + 1, endIndex- startIndex - 1);

            }
            

            //获取企业信息http://www.qixin.com/service/getRootNodeInfoByEnterpriseId?enterpriseId=1b9df7af-e7b3-4d45-93ce-8acf02534adb&_=1466587526737
            if (!string.IsNullOrEmpty(guid) && hmtl.Contains("status"))
            {
                //Console.WriteLine("详细信息获取成功");

                var message = string.Format("详细信息{0}获取成功剩余url{1}\r{2}", guid, UrlQueue.Instance.Count, hmtl);
                ShowMessageInfo(message);
                if (enterpriseInfoUrlType.Contains(infoType))
                {
                    curUpdateBson.Set(infoType, hmtl);
                }
                else { 
                curUpdateBson.Set("detailInfo", hmtl);
                }
                foreach (var urlType in enterpriseInfoUrlType)
                {
                    var otherInfoUrl = string.Format("http://www.qixin.com/service/{0}?eid={1}&_={2}", urlType, guid, GetTimeLikeJS());
                    var item = new HttpItem()
                    {
                        URL = otherInfoUrl,//URL     必需项    
                        Method = "get",//URL     可选项 默认为Get   
                        ContentType = "text/html",//返回类型    可选项有默认值 
                        Timeout = Settings.Timeout,
                        Cookie = Settings.SimulateCookies
                    };
                    HttpResult result = http.GetHtml(item);
                    if (result.Html.Contains("\"status\":0"))
                    {
                        curUpdateBson.Set(urlType, hmtl);
                    }
                    //UrlQueue.Instance.EnQueue(new UrlInfo(otherInfoUrl) { Depth = 1 });
                }

                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Update, Query = Query.EQ("guid", guid) });
            }

        }

        /// <summary>
        /// 封号几率大 且信息顺序调换
        /// </summary>
        /// <param name="args"></param>
        public void DataReceiveEnterpriseCompnayInfo(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var curUpdateBson = new BsonDocument();
            var oldName = string.Empty;
            //var queryStr = GetQueryString(args.Url);
            var oldBsonDocument = new BsonDocument();
            var startIndex = args.Url.LastIndexOf("/");
            if (startIndex == -1) return;
            var guid = args.Url.Substring(startIndex + 1, args.Url.Length - startIndex - 1);
            //
            var companyInfo = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='company-card']");
            if (companyInfo == null) return;
            var baseInfoDivList = companyInfo.SelectNodes("./div[@class='company-info-item clearfix']");
            if (baseInfoDivList == null) return;
            var statusDiv = baseInfoDivList.Where(c => c.InnerText.Contains("状态")).FirstOrDefault();
            var telDiv = baseInfoDivList.Where(c => c.InnerText.Contains("电话")).FirstOrDefault();
            var addressDiv = baseInfoDivList.Where(c => c.InnerText.Contains("地址")).FirstOrDefault();
            var websiteDiv = baseInfoDivList.Where(c => c.InnerText.Contains("官网")).FirstOrDefault();
            if (statusDiv != null)
            {
                curUpdateBson.Add("status", statusDiv.InnerText.Replace("状态：", ""));
            }
            if (telDiv != null)
            {
                curUpdateBson.Add("telphone", telDiv.InnerText.Replace("电话：", ""));
            }
            if (addressDiv != null)
            {
                curUpdateBson.Add("address", addressDiv.InnerText.Replace("地址：", "").Replace("查看地图", ""));
            }
            if (websiteDiv != null)
            {
                var urlDiv = websiteDiv.SelectSingleNode("./div[last()]/a");
                if (urlDiv != null)
                {
                    var url = urlDiv.Attributes["href"] != null ? urlDiv.Attributes["href"].Value : string.Empty;
                    oldBsonDocument.Add("website", url);
                }
            }
            var info = htmlDoc.GetElementbyId("info");//基本信息
            var risk = htmlDoc.GetElementbyId("risk");//风险信息
            var ability = htmlDoc.GetElementbyId("ability");//只是产权
            var investment = htmlDoc.GetElementbyId("investment");//对外投资
            var report = htmlDoc.GetElementbyId("report");//企业年报
            var operation = htmlDoc.GetElementbyId("operation");//经营信息

            DealInfo(info, ref curUpdateBson);
            if(curUpdateBson.Text("status")!= "吊销") { 
            foreach (var urlType in enterpriseInfoUrlType)
            {
                var otherInfoUrl = string.Format("http://www.qixin.com/service/{0}?eid={1}&_={2}", urlType, guid, GetTimeLikeJS());
                var item = new HttpItem()
                {
                    URL = otherInfoUrl,//URL     必需项    
                    Method = "get",//URL     可选项 默认为Get   
                    ContentType = "text/html",//返回类型    可选项有默认值 
                    Timeout = Settings.Timeout,
                    Cookie = Settings.SimulateCookies
                };
                HttpResult result = http.GetHtml(item);
                if (result.Html.Contains("\"status\":0"))
                {
                    curUpdateBson.Set(urlType, hmtl);
                }
                    Thread.Sleep(500);
                //UrlQueue.Instance.EnQueue(new UrlInfo(otherInfoUrl) { Depth = 1 });
            }
            }
            if (!string.IsNullOrEmpty(guid))
            {
                var message = string.Format("详细信息{0}获取成功剩余url{1}\r", guid, UrlQueue.Instance.Count);
                ShowMessageInfo(message);
                curUpdateBson.Set("isNoDetailInfoUpdate", "1");
                curUpdateBson.Set("detailInfo", "");
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Update, Query = Query.EQ("guid", guid) });

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
                            columnValue=columnValue.Replace("法人对外投资", "").Replace("&nbsp;","").Trim();
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
                        if (nameSpan != null) {
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
                if (itemCountAttr != null) { 
                if(int.TryParse(itemCountAttr.Value, out itemCount))
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
        private void DealOtherInfo(HtmlNode info, ref BsonDocument updateBson,string name)
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
            HtmlDocument htmlDoc = new HtmlDocument();
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
            curUpdateBson.Add("url", string.Format("http://www.qixin.com{0}", url));
            ///company/fc0de68c-acff-4e5e-9444-7ed41761c2f5
            var startIndex = url.LastIndexOf("/");
            if (startIndex == -1) return;
            var guid = url.Substring(startIndex + 1, url.Length - startIndex - 1);
            curUpdateBson.Add("guid", guid);
            //获取企业信息http://www.qixin.com/service/getRootNodeInfoByEnterpriseId?enterpriseId=1b9df7af-e7b3-4d45-93ce-8acf02534adb&_=1466587526737
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
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var searchDiv = htmlDoc.DocumentNode.SelectNodes("//div[@class='search-list-bg']/div[@class='search-ent-row clearfix']");
            if (searchDiv == null) return;
         
            var keyWord = string.Empty;
            var queryStr = GetQueryString(args.Url);
            var oldBsonDocument = new BsonDocument();
            var page = string.Empty;
            if (!string.IsNullOrEmpty(queryStr))
            {
                var dic = HttpUtility.ParseQueryString(queryStr);
                var serchKey = dic["key"] != null ? dic["key"].ToString() : string.Empty;
                keyWord = HttpUtility.UrlDecode(serchKey).Replace(cityName,"").Trim();
                //页数关键字
                page = dic["page"] != null ? dic["page"].ToString() : string.Empty;
                if (string.IsNullOrEmpty(page) || page == "1")//首页
                {    //获取分页
                    long pageCount = 1; ;
                     //获取分页
                        var pageDiv = htmlDoc.GetElementbyId("search_pager");
                        if (pageDiv != null)
                        {
                            var itemCountAttr = pageDiv.Attributes["data-pager-total-items"];
                            var itemCount = 0;
                            if (itemCountAttr != null)
                            {
                                if (int.TryParse(itemCountAttr.Value, out itemCount))
                                {
                                    if (itemCount % 10 == 0)
                                        pageCount = itemCount / 10;
                                    else
                                        pageCount = itemCount / 10 + 1;
                                    //添加到待处理列表
                                    for (var i = 2; i <= pageCount; i++)
                                    {
                                        var url = string.Format("http://www.qixin.com/search?key={0} {1}&type=enterprise&source=&isGlobal=Y&page={2}", HttpUtility.UrlEncode(cityName), HttpUtility.UrlEncode(keyWord), i);
                                        UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });
                                    }
                                }

                            }
                        }
                  
                    DBChangeQueue.Instance.EnQueue(new StorageData()
                    {
                        Document = new BsonDocument().Add("keyWord", keyWord).Add("cityName", cityName).Add("pageCount", pageCount),
                        Name = DataTableCityKeyWord,
                        Type = StorageType.Insert
                    });

                }
                else {//分页
                    DBChangeQueue.Instance.EnQueue(new StorageData()
                    {
                        Document = new BsonDocument().Add("execPage", page),
                        Name = DataTableCityKeyWord, Query = Query.And(Query.EQ("cityName", cityName), Query.EQ("keyWord", keyWord)),
                        Type = StorageType.Update
                    });
                }
               
            }
            var SB = new StringBuilder();
            var existCount = 0;
            var addCount = 0;
           var searchDivResult = searchDiv.Where(c => !c.InnerHtml.Contains("el.name")).ToList();
            foreach (var hitDiv in searchDivResult) {
                var curUpdateBson = new BsonDocument();
                var companyInfo= hitDiv.SelectSingleNode("./div/div/div[@class='search-result-company-info']");
                if (companyInfo != null)
                {
                    curUpdateBson.Add("companyInfo", companyInfo.InnerText);
                }
                var companyAddress = hitDiv.SelectSingleNode("./div/div/div[@class='search-result-company-history']");
                if (companyAddress != null)
                {
                    curUpdateBson.Add("address", companyAddress.InnerText.Replace("企业地址：", ""));
                }
                var companyStatus = hitDiv.SelectSingleNode("./div/ul/li/div[@class='company-state-type']");
                if (companyStatus != null)
                {
                    curUpdateBson.Add("status", companyStatus.InnerText.Trim());
                }
                var searchResult = hitDiv.SelectSingleNode("./div/div/a[@class='search-result-company-name']");
                if (searchResult == null) continue;
                var enterpriseName = searchResult.InnerText;
                var url = searchResult.Attributes["href"] != null ? searchResult.Attributes["href"].Value : string.Empty;
                if (string.IsNullOrEmpty(url)) continue;
                curUpdateBson.Add("name", enterpriseName);
                curUpdateBson.Add("url", string.Format("http://www.qixin.com{0}", url));
                ///company/fc0de68c-acff-4e5e-9444-7ed41761c2f5
                var startIndex = url.LastIndexOf("/");
                if (startIndex == -1) continue;
                var guid = url.Substring(startIndex + 1, url.Length - startIndex - 1);
                curUpdateBson.Add("guid", guid);
                //获取企业信息http://www.qixin.com/service/getRootNodeInfoByEnterpriseId?enterpriseId=1b9df7af-e7b3-4d45-93ce-8acf02534adb&_=1466587526737
                if (!string.IsNullOrEmpty(guid) && !existGuidList.Contains(guid))
                {
                    existGuidList.Add(guid);
                    SB.AppendFormat("获得对象{0}:{1}\r", guid, enterpriseName, UrlQueue.Instance.Count);
                    addCount++;
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Insert });
                   //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("status", "1"), Query = Query.EQ("name", enterpriseName), Name = DataTableNameList, Type = StorageType.Update });
                }
                else
                {
                    //ShowMessageInfo(string.Format("guid:{0}{1}已存在或者无法添加剩余url{2}\r", guid, oldName, UrlQueue.Instance.Count));
                    existCount++;
                }
                
            }
            ShowMessageInfo(string.Format("添加:{0} 已存在:{1} 详细:{2}当前url:{3}剩余url{4}", addCount, existCount, SB.ToString(), HttpUtility.UrlDecode(queryStr), UrlQueue.Instance.Count));
   

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
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);

            var oldName = string.Empty;
            var queryStr = GetQueryString(args.Url);


            var searchResult = htmlDoc.DocumentNode.SelectNodes("//div[@class='search-ent-row']");
            if (searchResult == null) return;

            foreach (var enterpriseNode in searchResult)
            {

                var nameNode = enterpriseNode.SelectSingleNode("./div/div/a[@class='search-result-title']");
                var curUpdateBson = new BsonDocument();
                var oldBsonDocument = new BsonDocument();
                var enterpriseName = nameNode.InnerText;
                var url = nameNode.Attributes["href"] != null ? nameNode.Attributes["href"].Value : string.Empty;
                if (string.IsNullOrEmpty(url)) continue;
                curUpdateBson.Add("name", enterpriseName);
                curUpdateBson.Add("url", string.Format("http://www.qixin.com{0}", url));
                ///company/fc0de68c-acff-4e5e-9444-7ed41761c2f5
                var startIndex = url.LastIndexOf("/");
                if (startIndex == -1) continue;
                var guid = url.Substring(startIndex + 1, url.Length - startIndex - 1);
                curUpdateBson.Add("guid", guid);
                var addressNode = enterpriseNode.SelectSingleNode("./div/div/div[@class='search-result-correlation']/span");
                if (addressNode != null)
                    curUpdateBson.Set("address", addressNode.InnerText.Replace("企业地址：", ""));
                //获取企业信息http://www.qixin.com/service/getRootNodeInfoByEnterpriseId?enterpriseId=1b9df7af-e7b3-4d45-93ce-8acf02534adb&_=1466587526737
                if (!string.IsNullOrEmpty(guid) && !existGuidList.Contains(guid))
                {
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
                if (!string.IsNullOrEmpty(guid))
                {

                    oldBsonDocument.Add("guid", guid);
                    oldBsonDocument.Add("searchName", enterpriseName);
                    oldBsonDocument.Set("status", "1");
                    existCount++;

                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = oldBsonDocument, Query = Query.Or(Query.EQ("name", oldName), Query.EQ("name", enterpriseName)), Name = DataTableNameList, Type = StorageType.Update });
                }
            }
            var str = "解析" + HttpUtility.UrlDecode(args.Url) + "\r";
            ShowMessageInfo(str + string.Format("addCount:{0} updateCount:{1} existCount:{2} 剩余url:{3} ", addCount, updateCount, existCount, UrlQueue.Instance.Count));


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
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);

            var oldName = string.Empty;
            var queryStr = GetQueryString(args.Url);

            var cityName = string.Empty;
            var hitCityName = cityNameDic.Where(c => args.Url.Contains(c.Key)).Select(c => c.Value).FirstOrDefault();
            if (hitCityName != null)
            {
                cityName = hitCityName;
            }
            var searchResult = htmlDoc.DocumentNode.SelectNodes("//div[@class='search-result-data']/div");
            if (searchResult == null) return;

            foreach (var enterpriseNode in searchResult)
            {

                var nameNode = enterpriseNode.SelectSingleNode("./a");
                var curUpdateBson = new BsonDocument();
                var oldBsonDocument = new BsonDocument();
                var enterpriseName = nameNode.InnerText;
                var url = nameNode.Attributes["href"] != null ? nameNode.Attributes["href"].Value : string.Empty;
                if (string.IsNullOrEmpty(url)) continue;
                curUpdateBson.Add("name", enterpriseName);
                curUpdateBson.Add("url", url);
                ///company/fc0de68c-acff-4e5e-9444-7ed41761c2f5
                var startIndex = url.LastIndexOf("/");
                if (startIndex == -1) continue;
                var guid = url.Substring(startIndex + 1, url.Length - startIndex - 1);
                curUpdateBson.Add("guid", guid);
                if (!string.IsNullOrEmpty(cityName))
                {
                    curUpdateBson.Add("cityName", cityName);
                }
                //获取企业信息http://www.qixin.com/service/getRootNodeInfoByEnterpriseId?enterpriseId=1b9df7af-e7b3-4d45-93ce-8acf02534adb&_=1466587526737
                if (!string.IsNullOrEmpty(guid) && !existGuidList.Contains(guid))
                {
                    existGuidList.Add(guid);
                    // var message = string.Format("详细信息{0}:{1}获取成功剩余url{2}\r", guid, oldName, UrlQueue.Instance.Count);

                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Insert });
                    addCount++;
                    //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("status", "1"), Query = Query.EQ("name", enterpriseName), Name = DataTableNameList, Type = StorageType.Update });
                }
                else
                {
                    var updateBson = new BsonDocument().Add("cityName", cityName);
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBson, Query = Query.EQ("guid", guid), Name = DataTableName, Type = StorageType.Update });
                    ////ShowMessageInfo(string.Format("guid:{0}{1}已存在或者无法添加剩余url{2}\r", guid, oldName, UrlQueue.Instance.Count));
                    updateCount++;
                }
                //if (!string.IsNullOrEmpty(guid))
                //{

                //    oldBsonDocument.Add("guid", guid);
                //    oldBsonDocument.Add("searchName", enterpriseName);
                //    oldBsonDocument.Set("status", "1");
                //    existCount++;

                //    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = oldBsonDocument, Query =Query.Or(Query.EQ("name", oldName), Query.EQ("name", enterpriseName)), Name = DataTableNameList, Type = StorageType.Update });
                //}
            }
            var str = "解析" + HttpUtility.UrlDecode(args.Url) + "\r";
            ShowMessageInfo(str + string.Format("addCount:{0} updateCount:{1} existCount:{2} 剩余url:{3} 更新队列:{4} ", addCount, updateCount, existCount, UrlQueue.Instance.Count, DBChangeQueue.Instance.Count));


        }



        #endregion
        #region 基础方法 
        /// 刷新账号
        /// </summary>
        private void ReloadLoginAccount()
        {
            this.comboBox1.Items.Clear();
            var tempAccountList = new List<BsonDocument>();
            if (allAccountList.Count <= 0)
            {
                allAccountList = dataop.FindAllByQuery(DataTableAccount, Query.NE("isInvalid", "1")).OrderBy(c => c.Int("isUsed")).ThenBy(c => c.Date("updateDate")).ToList();
                tempAccountList = allAccountList;
            }
            else
            {
                tempAccountList = dataop.FindAllByQuery(DataTableAccount, Query.NE("isInvalid", "1")).OrderBy(c => c.Int("isUsed")).ThenBy(c => c.Date("updateDate")).ToList();
            }

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
        }
        private void ShowAccountInfo()
        {
            var curLoginAccountObj = allAccountList.Where(c => c.Text("name") == Settings.LoginAccount).FirstOrDefault();
            if (curLoginAccountObj != null)
            {

                Type searchTypeEnum = typeof(SearchType);
                var sb = new StringBuilder();
                foreach (string _searchType in Enum.GetNames(searchTypeEnum))
                {
                    //Console.WriteLine("{0,-11}= {1}", s, Enum.Format(searchTypeEnum, Enum.Parse(searchTypeEnum, s), "s"));
                    var addColumnName = string.Format("{0}_add", _searchType);
                    var curAddional = curLoginAccountObj.Int(addColumnName);
                    var initial = curLoginAccountObj.Int(_searchType);
                    if (curAddional != 0 || initial != 0)
                    {
                        sb.AppendFormat("{0}:{1}【add:{2}】 ", _searchType, initial, curAddional);
                    }
                }
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                {
                    this.accountInfoTxt.Content = sb.ToString();
                }));

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

        public bool UrlContentLimit(string html)
        {
            if (searchType == SearchType.EnterpriseGuidByKeyWord || searchType == SearchType.EnterpriseGuid)
            {
                if (!html.Contains("search-ent-row clearfix"))
                {
                    return true;
                }
            }
            if (html.Length <= 10 || html.Contains("您使用验证码过于频繁") || html.Contains("请求的网址（URL）无法获取") || html.Contains("上限"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// IP限定处理，ip被限制 账号被限制跳转处理
        /// </summary>
        /// <param name="args"></param>
        public bool IPLimitProcess(DataReceivedEventArgs args)
        {
           
            if (UrlContentLimit(args.Html))
            {
                timerStop();
            
                //this.webBrowser.Refresh();
                //自动过验证码
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                {
                    if (this.aTimer.Enabled == false) { 
                        // this.webBrowser.Refresh();
                        this.webBrowser.Navigate(curUri);
                    }
                    //this.richTextBoxInfo.Document.Blocks.Clear();
                    //this.richTextBoxInfo.AppendText(string.Format("当前url:{0}剩余url:{1}", this.curUri.ToString(), UrlQueue.Instance.Count));
                    //this.richTextBoxInfo.AppendText("正在检测是否自动验证!");
                    if (PassEnterpriseInfoGeetestChart())
                        {
                            //waitBrowerMouseUpResponse = true;
                            // Thread.Sleep(1000);
                            timerStart();
                        }
                        else
                        {

                            this.richTextBoxInfo.AppendText("正在刷新浏览器，请点击重新爬取");
                            waitBrowerMouseUpResponse = true;
                            ///是否获得焦点
                            if (this.checkBox.IsChecked.HasValue && this.checkBox.IsChecked.Value)
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

                }));
                return true;
            }


            return false;
        }

        public void ShowMessageInfo(string str, bool isAppend = false)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
            {
                if (isAppend == false)
                {
                    this.richTextBoxInfo.Document.Blocks.Clear();
                }
                this.richTextBoxInfo.AppendText(str);
            })
           );
        }


        /// <summary>
        /// 对需要更新的队列数据更新操作进行批量处理,可考虑异步执行
        /// </summary>
        private static void StartDBChangeProcess()
        {

            List<StorageData> updateList = new List<StorageData>();
            while (DBChangeQueue.Instance.Count > 0 && updateList.Count() <= 50)
            {
                var curStorage = DBChangeQueue.Instance.DeQueue();
                if (curStorage != null)
                {
                    updateList.Add(curStorage);
                }
            }
            if (updateList.Count() > 0)
            {
                // Task.Run(() => { 
                var result = dataop.BatchSaveStorageData(updateList);
                if (result.Status != Status.Successful)//出错进行重新添加处理
                {
                    if (!result.Message.Contains("memory"))
                    {
                        foreach (var storageData in updateList)
                        {
                            DBChangeQueue.Instance.EnQueue(storageData);
                        }
                    }
                }
                //  }
                // );
            }
            if (DBChangeQueue.Instance.Count > 0)
            {
                StartDBChangeProcess();
            }
        }

        /// <summary>
        /// 对需要更新的队列数据更新操作进行批量处理,可考虑异步执行
        /// </summary>
        private void StartDBChangeProcessQuick()
        {
            var result = new InvokeResult();
            List<StorageData> updateList = new List<StorageData>();
            while (DBChangeQueue.Instance.Count > 0 && updateList.Count() <= 50)
            {

                var temp = DBChangeQueue.Instance.DeQueue();
                if (temp != null)
                {
                    var insertDoc = temp.Document;

                    switch (temp.Type)
                    {
                        case StorageType.Insert:
                            if (insertDoc.Contains("createDate") == false) insertDoc.Add("createDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));      //添加时,默认增加创建时间
                            if (insertDoc.Contains("createUserId") == false) insertDoc.Add("createUserId", "1");
                            //更新用户
                            //if (insertDoc.Contains("underTable") == false) insertDoc.Add("underTable", temp.Name);
                            //insertDoc.Set("updateDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));      //更新时间
                            //                                                                                // insertDoc.Set("updateUserId", "1");
                            result = _mongoDBOp.Save(temp.Name, insertDoc); ;
                            break;
                        case StorageType.Update:
                            // insertDoc.Set("updateDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));      //更新时间
                            // insertDoc.Set("updateUserId", "1");
                            result = _mongoDBOp.Save(temp.Name, temp.Query, insertDoc);
                            break;
                        case StorageType.Delete:
                            result = _mongoDBOp.Delete(temp.Name, temp.Query);
                            break;
                    }
                    //logInfo1.Info("");
                    if (result.Status == Status.Failed) throw new Exception(result.Message);

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
              

                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
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

        }
        private void timerStop()
        {
            if (aTimer.Enabled == true)
            {
              
                aTimer.Stop();
                aTimer.Enabled = false;
                waitBrowerMouseUpResponse = true;
                ShowMessageInfo("计时器结束");

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

        #region helper

       
        /// <summary>
        /// 过企业信息chart验证码
        /// </summary>
        /// <returns></returns>
        private bool PassEnterpriseInfoGeetestChart()
        {

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
            {
                ///重载uri
                if (curUri != null)
                {
                    var cookies = FullWebBrowserCookie.GetCookieInternal(curUri, false);
                    Settings.SimulateCookies = cookies;
                }
            })
           );
            var validUrl = "http://www.qixin.com/service/gt-validate-for-chart";
            var postFormat = "geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}%7Cjordan";
            bool result = false;

            switch (searchType)
            {
                case SearchType.UpdateEnterpriseInfo: break;
                case SearchType.UpdateEnterpriseCompnayInfo:
                    validUrl = "http://www.qixin.com/service/gtvalidate";
                    postFormat += "&requestType=company_detail";
                    break;
                default:
                    return false;
            }

            if (this.checkBox1.IsChecked.HasValue && this.checkBox1.IsChecked.Value)
            {
                this.richTextBoxInfo.AppendText("等待过验证码");
                // var postFormat = "geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}%7Cjordan&requestType=search_enterprise";
                var passResult = geetestHelper.PassGeetest(hi, postFormat, validUrl, Settings.SimulateCookies);
                result = passResult.Status;
                // this.richTextBoxInfo.Document.Blocks.Clear();
                this.richTextBoxInfo.AppendText("已自动过验证码");
                this.textBox4.Text = passResult.LastPoint;
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
            //在查看一遍防止无线过点
            var item = new HttpItem()
            {
                URL = this.curUri.ToString(),//URL     必需项    
                Method = "get",//URL     可选项 默认为Get   
                ContentType = "text/html",//返回类型    可选项有默认值 
                Timeout = Settings.Timeout,
                Cookie = Settings.SimulateCookies
            };
            HttpResult curResult = http.GetHtml(item);
            if (UrlContentLimit(curResult.Html))
            {
                checkBox1.IsChecked = false;
                return false;
            }
            if (PassInValidTimes <= 0 || PassSuccessTimes <= 0)//失败20次后进行屏蔽
            {
                checkBox1.IsChecked = false;
            }
            return result;
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
                 
                Type searchTypeEnum = typeof(SearchType);
                foreach (string _searchType in Enum.GetNames(searchTypeEnum)) {
                        //Console.WriteLine("{0,-11}= {1}", s, Enum.Format(searchTypeEnum, Enum.Parse(searchTypeEnum, s), "s"));
                        var columnName = string.Format("{0}_add", _searchType);
                        var curAddional = curLoginAccountObj.Int(columnName);
                        if (curAddional != 0) {
                        var finalResult = curLoginAccountObj.Int(_searchType) + curAddional;
                        updateBson.Set(_searchType, finalResult.ToString());//增加值
                        curLoginAccountObj.Set(columnName, "0");
                        curLoginAccountObj.Set(_searchType, finalResult.ToString());// 当前值
                    }
                }
              }
            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBson, Query = Query.EQ("name", loginAccount), Name = DataTableAccount, Type = StorageType.Update });
            StartDBChangeProcessQuick();
        }
        #endregion

        #region 产业相关处理
      
        /// <summary>
        /// 清洗curCompanyShortIntro
        /// 民营公司                    ;;|;;1000-5000人                              ;;|;;电气/电力/水利 电子技术/半导体/集成电路
        /// </summary>
        /// <param name="curCompanyShortIntro"></param>
        /// <param name="updateBosn"></param>
        /// <returns></returns>
        public void DealCompanyShortIntro(string curCompanyShortIntro, ref BsonDocument updateBosn)
        {
            var strArray = curCompanyShortIntro.Split(new string[] { ";;|;;" }, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length == 3)
            {
                updateBosn.Set("companyType", strArray[0].Trim());//类型
                updateBosn.Set("companyScale", strArray[1].Trim());//规模
                updateBosn.Set("companyDomain", strArray[2].Trim());//公司运营范围
            }
            else
            {
                updateBosn.Set("needUpdate", "1");
            }

        }

        /// <summary>
        /// curCompanyAddress
        ///公司地址：三水区乐平镇                    (邮编：528137)
        /// </summary>
        /// <param name="curCompanyShortIntro"></param>
        /// <param name="updateBosn"></param>
        /// <returns></returns>
        public void DealCompanyAddress(string curCompanyAddress, ref BsonDocument updateBosn)
        {
            var fixCompanyAddress = curCompanyAddress.Replace("公司地址：", "").Replace(" ", "").Trim().TrimEnd(')');
            var strArray = fixCompanyAddress.Split(new string[] { "(邮编：" }, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length == 2)
            {
                updateBosn.Set("address", strArray[0]);//地址
                updateBosn.Set("zipCode", strArray[1]);//规模

            }
            else
            {
                updateBosn.Set("needUpdate", "1");
            }

        }
        /// <summary>
        /// curPostNum
        /// 2年 | 中专 | 招聘2人    招聘1人   大专 | 招聘2人
        /// </summary>
        /// <param name="curCompanyShortIntro"></param>
        /// <param name="updateBosn"></param>
        /// <returns></returns>
        public void DealCompanyPostNum(string curPostNum, ref BsonDocument updateBosn)
        {

            var strArray = curPostNum.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var str in strArray)
            {
                if (str.Contains("年"))
                {
                    updateBosn.Set("postExp", str);
                }
                else if (str.Contains("人") && str.Contains("招聘"))//招聘xx人
                {
                    updateBosn.Set("postUserNum", str);
                }
                else
                {
                    updateBosn.Set("postDegree", str);//学历
                }
            }


        }

        
        /// <summary>
        /// 获取企业领域
        /// </summary>
        public void GetEnterpriseDomain()
        {
            var allEnterpriseList = dataop.FindAll("_51JobEnterprise").SetFields("companyDomain").Select(c => c.Text("companyDomain").Trim()).Distinct().ToList();
            var sb = new StringBuilder();
            var strList = new List<string>();
            foreach (var domain in allEnterpriseList)
            {
                var strArr = domain.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var str in strArr)
                {
                    strList.Add(str);
                }

            }
            this.richTextBox.AppendText(string.Join("\r", strList.Distinct().ToArray()));
            MessageBox.Show("1");
        }
        /// <summary>
        /// 智联招聘企业
        /// </summary>
        public void ZHIlianJob()
        {
            string connStr1 = "mongodb://sa:dba@192.168.1.43/SimpleCrawler";

            DataOperation dataop1 = new DataOperation(new MongoOperation(connStr1));
            //var allPostList = dataop.FindAllByQuery("_51Job", Query.NE("isExtract", "1")).SetFields("_id", "companyName", "companyMessage", "cityName", "companyType", "companyScale", "companyDomain", "address", "zipCode").ToList();
            var allEnterpriseList = dataop.FindAll("_51JobEnterprise").SetFields("companyName", "cityName", "guid").ToList();
            var allEnterpriseNameList = allEnterpriseList.Select(c => c.Text("companyName")).ToList();
            var allPostList = dataop1.FindAll("ZhiLianEnterprise").SetFields("companyName", "cityName", "companyType", "companyAddress", "companyDomain", "companyScale", "lo", "la").ToList();
            // allPostList = allPostList.Where(c => !allEnterpriseNameList.Contains(c.Text("companyName"))).ToList();
            var existCompantyNameList = new Dictionary<string, BsonDocument>();
            existCompantyNameList = allEnterpriseList.ToDictionary(c => c.Text("companyName"), d => d);
            var newIndex = 1;
            foreach (var postObj in allPostList)
            {
                var updateBson = new BsonDocument();
                var postUpdateBson = new BsonDocument();
                if (!existCompantyNameList.ContainsKey(postObj.Text("companyName")))
                {
                    var curEnterpriseGuid = Guid.NewGuid().ToString();

                    updateBson.Add("companyName", postObj.Text("companyName"));
                    // updateBson.Add("companyMessage", postObj.Text("companyMessage"));
                    updateBson.Add("cityName", postObj.Text("cityName"));
                    updateBson.Add("companyType", postObj.Text("companyType"));
                    updateBson.Add("companyScale", postObj.Text("companyScale"));
                    updateBson.Add("companyDomain", postObj.Text("companyDomain"));
                    updateBson.Add("address", postObj.Text("companyAddress"));
                    updateBson.Add("guid", curEnterpriseGuid);
                    updateBson.Set("lo", postObj.Text("lo"));
                    updateBson.Set("la", postObj.Text("la"));
                    existCompantyNameList.Add(postObj.Text("companyName"), updateBson);
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Name = "_51JobEnterprise", Document = updateBson, Type = StorageType.Insert });
                }
                else
                {
                    var curObj = existCompantyNameList[postObj.Text("companyName")];
                    if (!string.IsNullOrEmpty(postObj.Text("lo")) && string.IsNullOrEmpty(curObj.Text("lo")))
                    {

                        updateBson.Set("lo", postObj.Text("lo"));
                        updateBson.Set("la", postObj.Text("la"));
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Name = "_51JobEnterprise", Document = updateBson, Query = Query.EQ("guid", curObj.Text("guid")), Type = StorageType.Update });
                    }
                }
                if (newIndex++ % 10000 == 0)
                {
                    Task.Run(new Action(StartDBChangeProcessQuick));
                }

            }

            StartDBChangeProcessQuick();
            MessageBox.Show("1");
        }
        public void Clear51Job()
        {

            //var allPostList = dataop.FindAllByQuery("_51Job", Query.NE("isExtract", "1")).SetFields("_id", "companyName", "companyMessage", "cityName", "companyType", "companyScale", "companyDomain", "address", "zipCode").ToList();
            var allEnterpriseList = dataop.FindAll("_51JobEnterprise").SetFields("companyName", "cityName", "guid").ToList();
            var allEnterpriseNameList = allEnterpriseList.Select(c => c.Text("companyName")).ToList();
            var allPostList = dataop.FindAll("_51Job").SetFields("_id", "companyName", "cityName", "companyType", "companyScale", "companyDomain", "address", "zipCode").ToList();
            allPostList = allPostList.Where(c => !allEnterpriseNameList.Contains(c.Text("companyName"))).ToList();
            var existCompantyNameList = new Dictionary<string, BsonDocument>();
            existCompantyNameList = allEnterpriseList.ToDictionary(c => c.Text("companyName"), d => d);
            var newIndex = 1;
            foreach (var postObj in allPostList)
            {
                var postUpdateBson = new BsonDocument();
                if (!existCompantyNameList.ContainsKey(postObj.Text("companyName")))
                {
                    var curEnterpriseGuid = Guid.NewGuid().ToString();
                    var updateBson = new BsonDocument();
                    updateBson.Add("companyName", postObj.Text("companyName"));
                    // updateBson.Add("companyMessage", postObj.Text("companyMessage"));
                    updateBson.Add("cityName", postObj.Text("cityName"));
                    updateBson.Add("companyType", postObj.Text("companyType"));
                    updateBson.Add("companyScale", postObj.Text("companyScale"));
                    updateBson.Add("companyDomain", postObj.Text("companyDomain"));
                    updateBson.Add("address", postObj.Text("address"));
                    updateBson.Add("zipCode", postObj.Text("zipCode"));
                    updateBson.Add("guid", curEnterpriseGuid);
                    existCompantyNameList.Add(postObj.Text("companyName"), updateBson);
                    postUpdateBson.Set("guid", curEnterpriseGuid);
                }
                else
                {
                    var curObj = existCompantyNameList[postObj.Text("companyName")];
                    if (curObj.Text("cityName") != postObj.Text("cityName"))
                    {
                        curObj.Set("otherCityName", string.Format("{0},{1}", curObj.Text("otherCityName"), postObj.Text("cityName")));
                        curObj.Set("isOtherCity", "1");
                    }
                    postUpdateBson.Set("guid", curObj.Text("guid"));
                }
                postUpdateBson.Set("isExtract", "1");
                DBChangeQueue.Instance.EnQueue(new StorageData() { Name = "_51Job", Document = postUpdateBson, Query = Query.EQ("_id", ObjectId.Parse(postObj.Text("_id"))), Type = StorageType.Update });
                if (newIndex++ % 10000 == 0)
                {
                    Task.Run(new Action(StartDBChangeProcessQuick));
                }
            }
            var index = 1;
            foreach (var dic in existCompantyNameList)
            {
                var updateBson = dic.Value;
                DBChangeQueue.Instance.EnQueue(new StorageData() { Name = "_51JobEnterprise", Document = updateBson, Type = StorageType.Insert });
                if (index++ % 10000 == 0)
                {
                    Task.Run(new Action(StartDBChangeProcessQuick));
                }
            }
            StartDBChangeProcessQuick();
            MessageBox.Show("1");
        }
        #endregion

        #endregion
        #region 事件
        /// <summary>
        #region 控件事件
        private void checkBox1_Checked(object sender, RoutedEventArgs e)
        {
            //HttpInput hi = new HttpInput();
            //HttpManager.Instance.InitWebClient(hi, true, 30, 30);
            //Random rand = new Random(Environment.TickCount);
            var passResult = geetestHelper.GetLastPoint(hi);
            this.textBox4.Text = passResult;
            if (this.checkBox1.IsChecked.HasValue && this.checkBox1.IsChecked.Value)
            {
                this.textBox5.Text = "2000";
            }
            MessageBox.Show(string.Format("请注意，目前剩余{0}点，该功能只支持自动获取详细信息，使用该功能会进行扣费10条数据扣点,成功1000次或者失败20次会自动停止改功能", passResult));
        }

        private void webBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            ///重载uri
            if (curUri != null)
            {
                var cookies = FullWebBrowserCookie.GetCookieInternal(curUri, false);
                Settings.SimulateCookies = cookies;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AccountRelease(Settings.LoginAccount);
        }

        private void Window_Closed(object sender, EventArgs e)
        {

            //执行父父目录的自动更新程序
            var curDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            if (curDir != null && curDir.Parent != null)
            {
                FileInfo hitWEBSiteUpdate = curDir.Parent.GetFiles().Where(c => c.Name == "WEBSiteUpdate.exe").FirstOrDefault();
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
        }
        /// <summary>
        /// 定时timer时间
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {

            if (UrlQueue.Instance.Count > 0)
            {

                var curUrlObj = UrlQueue.Instance.DeQueue();
                if (curUrlObj != null)
                {
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

                        result = http.GetHtml(item);
                    }
                    catch (WebException ex)
                    {
                        if (ex.Message == "操作超时" && curUrlObj.Depth >= 2)
                        {
                            var message = "{ \"status\": 1, \"data\": { } }";
                            result.Html = message;
                            result.StatusCode = HttpStatusCode.OK;
                        }
                    }
                    catch (TimeoutException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                    if (curUrlObj.Depth >= 3 && result.Html == "{}")//尝试超过三次,用于企业信息json查找
                    {
                        var message = "{ \"status\": 1, \"data\": { } }";
                        result.Html = message;
                        result.StatusCode = HttpStatusCode.OK;
                    }
                    var args = new DataReceivedEventArgs() { Depth = 1, Html = result.Html, IpProx = null, Url = curUrlObj.UrlString };
                    if (!IPLimitProcess(args) && result.StatusCode == HttpStatusCode.OK)
                    {
                        DataReceive(args);
                        StartDBChangeProcessQuick();
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
                timerStop();
                waitBrowerMouseUpResponse = false;
                ShowMessageInfo("当前数据更新结束，请单击crawler重新获取");
            }
        }
        /// <summary>
        /// 监听自动重现开始事件
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnAutoStartTimedEvent(object source, ElapsedEventArgs e)
        {
            if (waitBrowerMouseUpResponse)
            {
                var captCha = (mshtml.IHTMLElement)documentText.all.item("captcha - box", 0);
                if (captCha == null)
                {
                    ShowMessageInfo("自动开始执行");
                    timerStart();
                    autoRestartTimer.Stop();
                }

            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.webBrowser.Navigate(this.textBox.Text);
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 1000;    // 1秒 = 1000毫秒,修改成可随机
            aTimer.Enabled = false;

            autoRestartTimer.Elapsed += new ElapsedEventHandler(OnAutoStartTimedEvent);
            autoRestartTimer.Interval = 3000;    // 1秒 = 1000毫秒
            autoRestartTimer.Enabled = false;

            //守护timer 模拟单击事件


            //InitialEnterpriseData();
            //aTimer.Enabled = true;
            this.comboBox.Items.Add("企业Info");//0
            this.comboBox.Items.Add("企业Guid");//1
            this.comboBox.Items.Add("企业分类Guid");//2
            this.comboBox.Items.Add("企业城市分类Guid");//3
            this.comboBox.Items.Add("企业CompanyInfo");////4http://www.qixin.com/company/00bc8987-6200-47a2-88fb-c0be54b43808
            this.comboBox.Items.Add("企业城市分类关键字Guid");////4http://www.qixin.com/search?key=%E5%8C%97%E4%BA%AC++%E9%A3%9F%E5%93%81%E6%B7%BB%E5%8A%A0%E5%89%82&type=enterprise&source=&isGlobal=Y
            ReloadLoginAccount();
            this.comboBox1.SelectionChanged += new SelectionChangedEventHandler(ComboBoxEditTextChange);

            var cityNameStr = "西安,烟台,上海,北京,成都,福州,广州,杭州,黄山,济南,龙岩,南昌,南京,宁波,泉州,深圳,苏州,武汉,厦门,大连,长沙,合肥,镇江,宁波,中山,郑州,昆明,江苏,重庆";
            foreach (var cityName in cityNameStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries))
            {
                this.comboBox2.Items.Add(cityName);
            }
            HttpManager.Instance.InitWebClient(hi, true, 30, 30);
            Random rand = new Random(Environment.TickCount);
            EnterpriseInfoMapDic.Add("统一社会信用代码", "credit_no");
            EnterpriseInfoMapDic.Add("组织机构代码", "org_no");
            EnterpriseInfoMapDic.Add("注册号", "reg_no");
            EnterpriseInfoMapDic.Add("经营状态", "status");
            EnterpriseInfoMapDic.Add("公司类型", "type");
            EnterpriseInfoMapDic.Add("成立日期", "date");
            EnterpriseInfoMapDic.Add("法定代表人", "oper_name");
            EnterpriseInfoMapDic.Add("营业期限", "limitDate");
            EnterpriseInfoMapDic.Add("注册资本", "reg_capi_desc");

            EnterpriseInfoMapDic.Add("发照日期", "issueDate");
            EnterpriseInfoMapDic.Add("登记机关", "registrar");
            EnterpriseInfoMapDic.Add("企业地址", "address");
            EnterpriseInfoMapDic.Add("经营范围", "operationDomain");


        }
        /// <summary>
        /// text更换时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBoxEditTextChange(object sender, SelectionChangedEventArgs e)
        {
            string text = (sender as ComboBox).SelectedItem as string;
            if (text == null) return;
            if (!text.Contains("占用"))
            {
                AccountRelease(Settings.LoginAccount);//释放原有账号
            }
            else
            {
                MessageBox.Show("该账号可能被占用请慎用，强制登陆会其他用户会退出登录");
            }

            text = text.Replace("_占用", "").Replace("_频繁", "");
            this.textBox1.Text = text;
            Settings.LoginAccount = text;
            AccountApply(Settings.LoginAccount);
            var hitAccountObj = allAccountList.Where(c => c.Text("name") == text.Trim()).FirstOrDefault();
            if (hitAccountObj != null)
            {
                this.textBox2.Text = hitAccountObj.Text("password");
            }

            ShowAccountInfo();
            //ReloadLoginAccount();
        }
        private void webBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            var cookies = FullWebBrowserCookie.GetCookieInternal(e.Uri, false);
            this.richTextBox.Document.Blocks.Clear();
            this.richTextBox.AppendText(cookies);
            Settings.SimulateCookies = cookies;
            curUri = e.Uri;
            // timerStart();
            //获取cookie
            if (e.Uri.AbsoluteUri.Contains("login"))
            {
                //填写表单
                mshtml.IHTMLDocument2 doc2 = (mshtml.IHTMLDocument2)webBrowser.Document;
                mshtml.IHTMLElement loginname = (mshtml.IHTMLElement)doc2.all.item("account", 0);
                mshtml.IHTMLElement loginPW = (mshtml.IHTMLElement)doc2.all.item("password", 0);
                var userNameTxt = this.textBox1.Text;
                var passwordTxt = this.textBox2.Text;
                if (loginname != null)

                    loginname.setAttribute("value", userNameTxt);
                if (loginPW != null)
                    loginPW.setAttribute("value", passwordTxt);
            }

            documentText = (IHTMLDocument2)webBrowser.Document; //this will access the document properties as needed
            documentEvents = (HTMLDocumentEvents2_Event)webBrowser.Document; // this will access the events properties as needed
            documentEvents.onmouseup += webBrowser_MouseUP;

            //mshtml.IHTMLWindow2 win = (mshtml.IHTMLWindow2)doc2.parentWindow;
            //win.execScript("changeRegImg()", "javascript");//使用JS
        }
        private void webBrowser_MouseUP(IHTMLEventObj pEvtObj)
        {
            pEvtObj.returnValue = false; // Stops key down
            pEvtObj.returnValue = true; // Return value as pressed to be true;
            if (waitBrowerMouseUpResponse)
            {

                // var captCha   = (mshtml.IHTMLElement)documentText.all.item("captcha - box", 0);
                // mshtml.IHTMLDocument2 doc2 = (mshtml.IHTMLDocument2)webBrowser.Document;
                if (documentText != null && documentText.body.innerHTML != null)
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
                this.richTextBox.Document.Blocks.Clear();
                this.richTextBox.AppendText("重载" + cookies);
                Settings.SimulateCookies = cookies;
            }
        }
        private void webBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            curUri = e.Uri;
        }
        #endregion
        #region  按钮事件
        /// <summary>
        /// 首次加载按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Settings.LoginAccount = this.textBox1.Text;
            curTimerElapse = this.textBox5.Text.Trim();
            ReloadLoginAccount();
            switch (this.comboBox.SelectedIndex)
            {
                //通过城市更新企业信息
                case (int)SearchType.EnterpriseGuidByCity:
                    searchType = SearchType.EnterpriseGuidByCity;
                    if (this.curUri != null && this.curUri.ToString().Contains("qyml"))
                    {
                        validUrl = this.curUri.ToString();
                    }
                    else
                    {
                        validUrl = "http://gs.qixin.com/qyml/beijing";
                    }
                    break;

                //更新企业信息
                case (int)SearchType.EnterpriseGuidByType:
                    searchType = SearchType.EnterpriseGuidByType;
                    if (this.curUri != null && this.curUri.ToString().Contains("search/domain"))
                    {
                        validUrl = this.curUri.ToString();
                    }
                    else
                    {
                        validUrl = "http://www.qixin.com/search/domain/%E5%88%B6%E9%80%A0%E4%B8%9A_%E5%86%9C%E5%89%AF%E9%A3%9F%E5%93%81%E5%8A%A0%E5%B7%A5%E4%B8%9A";
                    }
                    break;
                case (int)SearchType.EnterpriseGuid:
                    searchType = SearchType.EnterpriseGuid;
                    validUrl = "http://www.qixin.com/search?key=%E5%B7%A7%E5%90%88&type=enterprise&method=all";
                    break;
                case (int)SearchType.EnterpriseGuidByKeyWord:
                    searchType = SearchType.EnterpriseGuidByKeyWord;
                    validUrl = "http://www.qixin.com/search?key=%E5%B7%A7%E5%90%88&type=enterprise&method=all&1=2";
                    break;
                case (int)SearchType.UpdateEnterpriseCompnayInfo:
                    searchType = SearchType.UpdateEnterpriseCompnayInfo;
                    validUrl = "http://www.qixin.com/company/17bf585d-5240-4ce7-a739-b50a217cc9f7";
                    break;

                case (int)SearchType.UpdateEnterpriseInfo:
                default:
                    validUrl = "http://www.qixin.com/company/network/e6c8b0b6-a2b7-4ab3-8403-3ec6215d683b?name=%E6%B5%99%E6%B1%9F%E6%B7%98%E5%AE%9D%E5%A4%A7%E5%AD%A6%E6%9C%89%E9%99%90%E5%85%AC%E5%8F%B8";
                    searchType = SearchType.UpdateEnterpriseInfo;
                    break;

            }

            if (!waitBrowerMouseUpResponse)
            {
                this.webBrowser.Navigate(validUrl);
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
        /// <summary>
        /// 搜索url按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_Click(object sender, RoutedEventArgs e)
        {
            this.webBrowser.Navigate(this.textBox.Text);
            waitBrowerMouseUpResponse = false;
        }
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("请先取出重复数据");
            //return ;
            var enterpriseField = new string[] { "id", "reg_no", "credit_no", "org_no", "name", "oper_name", "status", "short_name", "address", "telephone", "reg_capi", "capi_unit", "reg_capi_desc", "domain", "province", "has_problem" };

            var allEnterpriseGuidList = dataop.FindAll(DataTableName).SetFields("guid").Select(c => c.Text("guid")).ToList();
            var allNeedEnterpriseList = dataop.FindAllByQuery(DataTableName, Query.And(Query.NE("isDealed", "1"), Query.Exists("detailInfo", true))).SetFields("detailInfo","guid").ToList();
            //var allEnterpriseGuidRelationList = dataop.FindAll(DataTableRelation).ToList();//企业关系
            var allEnterpriseGuidRelationList = new List<BsonDocument>() ;//企业关系
            var userRelationList = new List<BsonDocument>();
            var enterpriseRelationList = new List<BsonDocument>();//企业关系
            var enterpriseList = new List<BsonDocument>();//新增企业名称
            var updateStorageDataList = new List<StorageData>();
            var faildDealGuid = new List<string>();
            foreach (var info in allNeedEnterpriseList.Where(c=>c.Text("detailInfo")!=""))
            {
                try
                {
                    JObject jsonObj = JObject.Parse(info.Text("detailInfo"));
                    var dataInfo = jsonObj["data"]["data"];
                    //MessageBox.Show(dataInfo["info"]["id"].ToString());
                    var enterpriseInfo = dataInfo["info"];
                    var enterpriseBson = new BsonDocument();
                    var curEnterpriseGuid = GetString(enterpriseInfo, "id");
                    if (string.IsNullOrEmpty(curEnterpriseGuid)) continue;
                    ///初始化enterprise字段
                    foreach (var infoField in enterpriseField)
                    {
                        if (enterpriseInfo[infoField] != null)
                        {
                            enterpriseBson.Set(infoField, enterpriseInfo[infoField].ToString());
                        }
                    }
                    var nodeInfo = dataInfo["node_info"];

                    if (nodeInfo != null)
                    {

                        //股东nodeInfo["1"]
                        if (nodeInfo["1"] != null)
                        {
                            foreach (var userInfo in nodeInfo["1"].ToList())
                            {

                                var userBson = new BsonDocument().Add("guid", curEnterpriseGuid);
                                userBson.Set("holderGuid", GetString(userInfo, "id"));
                                userBson.Set("name", GetString(userInfo, "name"));
                                userBson.Set("short_name", GetString(userInfo, "short_name"));
                                userBson.Set("title", GetString(userInfo, "title"));
                                userBson.Set("has_problem", GetString(userInfo, "has_problem"));
                                userBson.Set("reg_capi", GetString(userInfo, "reg_capi"));
                                userBson.Set("real_capi", GetString(userInfo, "real_capi"));
                                userBson.Set("shareholding_ratio", GetString(userInfo, "shareholding_ratio"));
                                userBson.Add("type", "1");
                                //userRelationList.Add(userBson);
                                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = userBson, Name = DataTableShareHolder, Type = StorageType.Insert });
                            }
                        }
                        //高管nodeInfo["2"]
                        if (nodeInfo["2"] != null)
                        {
                            foreach (var userInfo in nodeInfo["2"].ToList())
                            {

                                var userBson = new BsonDocument().Add("guid", curEnterpriseGuid);
                                userBson.Set("holderGuid", GetString(userInfo, "id"));
                                userBson.Set("name", GetString(userInfo, "name"));
                                userBson.Set("short_name", GetString(userInfo, "short_name"));
                                userBson.Set("title", GetString(userInfo, "title"));//职称
                                userBson.Set("has_problem", GetString(userInfo, "has_problem"));
                                userBson.Set("type", GetString(userInfo, "type"));
                                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = userBson, Name = DataTableHolder, Type = StorageType.Insert });
                            }
                        }
                        //历史股东
                        if (nodeInfo["6"] != null)
                        {
                            foreach (var userInfo in nodeInfo["6"].ToList())
                            {

                                var userBson = new BsonDocument().Add("guid", curEnterpriseGuid);
                                userBson.Set("userId", GetString(userInfo, "id"));
                                userBson.Set("name", GetString(userInfo, "name"));
                                userBson.Set("short_name", GetString(userInfo, "short_name"));
                                userBson.Set("title", GetString(userInfo, "title"));//职称
                                userBson.Set("has_problem", GetString(userInfo, "has_problem"));
                                userBson.Set("type", "6");
                                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = userBson, Name = DataTableShareHolder, Type = StorageType.Insert });
                            }
                        }

                        //对外投资nodeInfo["3"]
                        for (var index = 3; index <= 7; index++)
                        {
                            if (index == 6) continue;//疑似股东
                            var i = index.ToString();
                            if (nodeInfo[i] != null)//通过guid匹配是否存在存在更新字段，不存在则添加，关系也需要进行添加
                            {
                                foreach (var relationInfo in nodeInfo[i].ToList())
                                {
                                    var targetGuid = GetString(relationInfo, "id");

                                    if (index == 7 && GetString(relationInfo, "short_name") == "?") continue;
                                    var enterpriseRelationBson = new BsonDocument();
                                    enterpriseRelationBson.Set("name", GetString(relationInfo, "name"));
                                    enterpriseRelationBson.Set("short_name", GetString(relationInfo, "short_name"));
                                    enterpriseRelationBson.Set("status", GetString(relationInfo, "status"));
                                    enterpriseRelationBson.Set("title", GetString(relationInfo, "title"));
                                    enterpriseRelationBson.Set("has_problem", GetString(relationInfo, "has_problem"));
                                    enterpriseRelationBson.Set("investment_time", GetString(relationInfo, "investment_time"));
                                    enterpriseRelationBson.Set("domain", GetString(relationInfo, "domain"));
                                    enterpriseRelationBson.Set("reg_capi", GetString(relationInfo, "reg_capi"));
                                    enterpriseRelationBson.Set("related_by", GetString(relationInfo, "related_by"));
                                    // enterpriseRelationBson.Set("investment_enterpriseGuid", GetString(relationInfo, "id"));
                                    if (!allEnterpriseGuidList.Contains(targetGuid))
                                    { //不存在 添加企业
                                        enterpriseRelationBson.Set("guid", targetGuid);
                                        enterpriseRelationBson.Set("isRelationAdd", targetGuid);
                                        enterpriseRelationBson.Set("url", string.Format("http://www.qixin.com/company/{0}", targetGuid));
                                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = enterpriseRelationBson, Name = DataTableName, Type = StorageType.Insert });
                                        allEnterpriseGuidList.Add(targetGuid);//防止重复添加
                                    }
                                    else
                                    {
                                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = enterpriseRelationBson, Name = DataTableName, Query = Query.EQ("guid", targetGuid), Type = StorageType.Update });
                                    }

                                    //添加关系DataTableRelation
                                    //查看是否存在关系

                                    var existRelationCount = allEnterpriseGuidRelationList.Where(c => c.Text("type") == i && ((c.Text("curGuid") == curEnterpriseGuid && c.Text("targetGuid") == targetGuid))).Count();
                                    if (existRelationCount <= 0)
                                    {
                                        var newRelation = new BsonDocument();
                                        newRelation.Set("type", i);
                                        newRelation.Set("curGuid", curEnterpriseGuid);
                                        newRelation.Set("targetGuid", targetGuid);
                                        newRelation.Set("investment_time", enterpriseRelationBson.Text("investment_time"));//投资时间
                                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = newRelation, Name = DataTableRelation, Type = StorageType.Insert });
                                    }

                                    //enterpriseRelationList.Add(enterpriseRelationBson);

                                }
                            }
                        }

                    }


                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = enterpriseBson.Add("isDealed", "1"), Name = DataTableName, Query = Query.EQ("guid", curEnterpriseGuid), Type = StorageType.Update });
                }
                catch (InvalidOperationException ex)
                {
                    faildDealGuid.Add(info.Text("guid"));
                }
                catch (Exception ex)
                {
                    faildDealGuid.Add(info.Text("guid"));
                }
                if (faildDealGuid.Count() > 0)
                {
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isDealed", "1").Add("errorInfo",""), Name = DataTableName, Query = Query.In ("guid", faildDealGuid.Select(c=>(BsonValue)c)), Type = StorageType.Update });
                }
            }

            StartDBChangeProcessQuick();

            MessageBox.Show("succeed");
        }
        private void button3_Click(object sender, RoutedEventArgs e)
        {
            
            var allEnterpriseGuidList = dataop.FindAll(DataTableName).SetFields("guid", "_id").ToList();

            var hitResult = from c in allEnterpriseGuidList
                            group c by c.Text("guid") into g
                            where g.Count() >= 2
                            select new { Key = g.Key, count = g.Count() };
            var curResult = hitResult.ToList();
            foreach (var g in curResult)
            {
                var curGuid = g.Key;
                var hitGuidList = allEnterpriseGuidList.Where(c => c.Text("guid") == curGuid).ToList();
                if (hitGuidList.Count() > 0)
                {
                    var last = hitGuidList.LastOrDefault();
                    hitGuidList.Remove(last);
                    foreach (var hitGuid in hitGuidList)
                    {
                        _mongoDBOp.Delete(DataTableName, Query.EQ("_id", ObjectId.Parse(hitGuid.Text("_id"))));
                        //DBChangeQueue.Instance.EnQueue(new StorageData() {  Name = DataTableName, Query = Query.EQ("_id", ObjectId.Parse(hitGuid.Text("_id"))), Type = StorageType.Delete });
                    }
                }
                StartDBChangeProcessQuick();
            }
        }
        private void button4_Click(object sender, RoutedEventArgs e)
        {
            var allEnterpriseList = dataop.FindAll(DataTableName).SetFields("name", "guid", "oldName").ToList();
            var existNameList = allEnterpriseList.Select(c => c.Text("name")).ToList();
            var existOldNameList = allEnterpriseList.Where(c => c.Text("oldName") != "").Select(c => c.Text("oldName")).ToList();


            var allNeedEnterpriseList = dataop.FindAllByQuery(DataTableNameList,
                Query.And(Query.Or(Query.NE("isSearched", "1"), Query.NE("status", "1")))).SetFields("name").Select(c => c.Text("name")).ToList();
            allNeedEnterpriseList = allNeedEnterpriseList.Where(c => existNameList.Contains(c) || existOldNameList.Contains(c)).ToList();
            foreach (var enterprise in allNeedEnterpriseList)
            {
                if (string.IsNullOrEmpty(enterprise)) continue;
                var hitObj = allEnterpriseList.Where(c => c.Text("name") == enterprise || c.Text("oldName") == enterprise).FirstOrDefault();
                if (hitObj == null) continue;
                if (string.IsNullOrEmpty(hitObj.Text("name"))) continue;
                var updateBson = new BsonDocument();
                updateBson.Add("isSearched", "1");
                updateBson.Add("status", "1");
                updateBson.Add("searchName", hitObj.Text("name"));
                updateBson.Add("guid", hitObj.Text("guid"));
                DBChangeQueue.Instance.EnQueue(new StorageData() { Name = DataTableNameList, Document = updateBson, Query = Query.EQ("name", enterprise), Type = StorageType.Update });
            }
            StartDBChangeProcessQuick();
        }
        private void button7_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox1.SelectedIndex != -1)
            {
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isInvalid", "1"), Query = Query.EQ("name", comboBox1.SelectedItem.ToString()), Name = DataTableAccount, Type = StorageType.Update });
                StartDBChangeProcessQuick();
                MessageBox.Show("成功设为无效账号");
            }
        }
        private void button8_Click(object sender, RoutedEventArgs e)
        {
            var allEnterpriseGuidList = dataop.FindAll(DataTableNameList).SetFields("name", "_id", "createDate").ToList();

            var hitResult = from c in allEnterpriseGuidList
                            group c by c.Text("name") into g
                            where g.Count() >= 2
                            select new { Key = g.Key, count = g.Count() };
            var curResult = hitResult.ToList();
            foreach (var g in curResult)
            {
                var curGuid = g.Key;
                var hitGuidList = allEnterpriseGuidList.Where(c => c.Text("name") == curGuid).ToList();
                if (hitGuidList.Count() > 0)
                {
                    var last = hitGuidList.OrderBy(c => c.Date("createDate")).FirstOrDefault();
                    hitGuidList.Remove(last);
                    foreach (var hitGuid in hitGuidList)
                    {
                        //_mongoDBOp.Delete(DataTableName, Query.EQ("_id", ObjectId.Parse(hitGuid.Text("_id"))));
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Name = DataTableNameList, Query = Query.EQ("_id", ObjectId.Parse(hitGuid.Text("_id"))), Type = StorageType.Delete });
                    }
                }

            }
            StartDBChangeProcessQuick();

        }
        private void button11_Click(object sender, RoutedEventArgs e)
        {
            //http://www.qixin.com/search/domain/%E5%88%B6%E9%80%A0%E4%B8%9A_%E5%86%9C%E5%89%AF%E9%A3%9F%E5%93%81%E5%8A%A0%E5%B7%A5%E4%B8%9A
        }
        private void button10_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Settings.LoginAccount))
            {
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("status", "1").Add("isBusy", "1"), Query = Query.EQ("name", Settings.LoginAccount), Name = DataTableAccount, Type = StorageType.Update });
                StartDBChangeProcessQuick();
                MessageBox.Show("成功设为频繁账号");
            }
        }
        private void button6_Click(object sender, RoutedEventArgs e)
        {
            //var allPostList = dataop.FindAllByQuery("_51Job", Query.NE("isExtract", "1")).SetFields("_id", "companyName", "companyMessage", "cityName", "companyType", "companyScale", "companyDomain", "address", "zipCode").ToList();
            ZHIlianJob();
        }
        private void button5_Click(object sender, RoutedEventArgs e)
        {
            var allPostList = dataop.FindAllByQuery("_51Job", Query.NE("isUpdate", "1")).SetFields("_id", "companyShortIntro", "companyAddress", "postNum", "postDate").ToList();
            var index = 1;
            foreach (var postObj in allPostList)
            {
                var updateBson = new BsonDocument();
                var curCompanyShortIntro = postObj.Text("companyShortIntro");
                var curCompanyAddress = postObj.Text("companyAddress");
                var curPostNum = postObj.Text("postNum");
                //06-24
                var curPostDate = postObj.Text("postDate");
                DealCompanyShortIntro(curCompanyShortIntro, ref updateBson);
                DealCompanyAddress(curCompanyAddress, ref updateBson);
                DealCompanyPostNum(curPostNum, ref updateBson);
                DateTime curDate;
                if (DateTime.TryParse(curPostDate, out curDate))
                {
                    updateBson.Set("date", curDate.ToString("yyyy-MM-dd"));
                }
                else
                {
                    updateBson.Set("needUpdate", "1");
                }
                updateBson.Set("isUpdate", "1");
                DBChangeQueue.Instance.EnQueue(new StorageData() { Name = "_51Job", Document = updateBson, Query = Query.EQ("_id", ObjectId.Parse(postObj.Text("_id"))), Type = StorageType.Update });
                if (index++ % 2000 == 0)
                {
                    Task.Run(new Action(StartDBChangeProcessQuick));
                }
            }
            StartDBChangeProcessQuick();
            MessageBox.Show("1");
        }
        #endregion
       
        #endregion
    }
}
