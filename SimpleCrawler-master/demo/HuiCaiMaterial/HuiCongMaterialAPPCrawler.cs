using DotNet.Utilities;
using HtmlAgilityPack;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Yinhe.ProcessingCenter;

using Yinhe.ProcessingCenter.DataRule;
using System.Collections;
using Newtonsoft.Json.Linq;
using LibCurlNet;

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// 慧聪网络App 数据采集
    /// 类别 var url = "http://z.hc360.com/getmmtlast.cgi?dt=1&w=单开&v=59&e=1&c=供应信息&n=2&m=2&H=1&bt=0";
    ///  authoration D1A4976615B875529F63090417A286C9";
    /// http://openapi.m.hc360.com/openapi/v1/productDetail/getSameProduct/621618969?page=1&pagesize=9相似产品
    /// http://openapi.m.hc360.com/openapi/v1/company/getInfo/wbz8 供应商信息 联系人电话所在城市
    /// 
    ///  </summary>
    public class HuiCongMaterialAPPCrawler : ISimpleCrawler
    {

       // private   string connStr = "mongodb://MZsa:MZdba@59.61.72.34:37088/SimpleCrawler";
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
         
        private Dictionary<string, string> columnMapDic = new Dictionary<string, string>();
      
        private Hashtable  userCrawlerCountHashTable = new Hashtable();
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private BloomFilter<string> filter;
        private BloomFilter<string> guidFilter;
        private   string _DataTableName = "Material_HuiCong_WLM";//存储的数据库表明
        private string _GuidDataTableName = "Material_HuiCong_Guid";//存储的数据库表明

        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableName
        {
            get { return _DataTableName; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableNameURL
        {
            get { return _DataTableName + "URL"; }

        }
        public string DataTableNameSpecialURL
        {
            get { return _DataTableName + "SpecialURL"; }

        }
        /// <summary>
        /// 城市信息
        /// </summary>
        public string DataTableNameCity
        {
            get { return "CityInfo_MT"; }

        }
        /// <summary>
        /// 城市信息
        /// </summary>
        public string DataTableNameCityCategory
        {
            get { return "CityCategoryInfo_MT"; }

        }
        /// <summary>
        /// 模拟登陆账号
        /// </summary>
        public string DataTableNameAccount
        {
            get { return _DataTableName + "Account"; }

        }
        ///// <summary>
        /////  分类信息
        ///// </summary>
        //public string DataTableNameCategory
        //{
        //    get { return "CategoryInfo_MT"; }

        //}

        /// <summary>
        ///  构造函数
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public HuiCongMaterialAPPCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
            guidFilter = new BloomFilter<string>(9000000);
        }
        public bool isSpecialUrlMode = false;
      

        int pageSize = 100;//24
        int pageBeginNum = 1;
       // string materialUrl = "http://z.hc360.com/getmmtlast.cgi?dt=1&w={0}&v=59&e={1}&c=供应信息&n={2}&m=2&H=1&bt=0";
      //  string materialUrl = "http://z.hc360.com/getmmtlast.cgi?dt=1&e={1}&c=供应信息&a=13&n={2}&m=2&H=1&fc=0&bt=0&w={0}&v=60&t=1";
        string materialUrl = "http://s.hc360.com/getmmtlast.cgi?sys=yidonghulian&bus=phone_ios&m=2&c=%E4%BE%9B%E5%BA%94%E4%BF%A1%E6%81%AF&bt=0&dt=1&w={0}&e={1}&v=59&n={2}&H=1";
        //将z.hc360改成 s.hc360 可用
        HuiCongAppHelper appHelper = new HuiCongAppHelper();
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {

            //种子地址需要加布隆过滤

            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            //var ipProxyList = dataop.FindAllByQuery("IPProxy", Query.NE("status", "1")).ToList();
            // Settings.IPProxyList.AddRange(ipProxyList.Select(c => new IPProxy(c.Text("ip"))).Distinct());
            // Settings.IPProxyList.Add(new IPProxy("1.209.188.180:8080"));
            Settings.IgnoreSucceedUrlToDB = true;
            Settings.ThreadCount = 1;
            Settings.DBSaveCountLimit = 1;
           
            Settings.MaxReTryTimes = 20;
            Settings.IgnoreFailUrl = true;
            Settings.AutoSpeedLimit = true;
            Settings.AutoSpeedLimitMaxMSecond = 2000;
            Settings.CrawlerClassName = "HuiCongMaterial";//需要进行token替换
            //Settings.CurWebProxy = GetWebProxy();
            Settings.ContentType = "application/x-www-form-urlencoded";
            this.Settings.UserAgent = "AiMeiTuan /samsung-4.4.2-GT-I9300-900x1440-320-5.5.4-254-864394010401414-qqcpd";
            Settings.UseSuperWebClient = true;
            Settings.hi = new HttpInput();
            HttpManager.Instance.InitWebClient(Settings.hi, true, 30, 30);
            //if (!string.IsNullOrEmpty(Settings.CurWebProxyString))
            //{
            //    Settings.hi.CurlObject.SetOpt(LibCurlNet.CURLoption.CURLOPT_PROXY, Settings.CurWebProxyString);
            //}
            var headSetDic = new Dictionary<string, string>();
            // Settings.hi.HeaderSet("Authorization", authorizationCode);
            headSetDic.Add("If-Modified-Since", "0");
            headSetDic.Add("User-Agent", "56");
            headSetDic.Add("Host", "z.hc360.com");
            headSetDic.Add("Content-Type", "text/html;charset=gb2312");
            Settings.HeadSetDic = headSetDic;
            //date=&end_date=&title=&content=&key=%E5%85%AC%E5%8F%B8&database=saic&search_field=all&search_type=yes&page=2
          
            Console.WriteLine("请输入关键字以逗号隔开");

           
            var keyWordStr = Console.ReadLine();
            //防水涂料50000中断开始
            //var keyWordStr =   "钢木入户门,木质入户门,木质防火门,钢质防火门,弹性面漆,非弹性面漆,防水涂料,外墙隔热面漆,外墙弹性中层涂料,外墙浮雕中层,水性外墙底漆,水性外墙底漆（弹性专用）,外墙柔性腻子,真石漆,质感涂料,多彩漆,罩面清漆,外墙面砖,铝包木复合门窗,断桥铝门窗,铁艺栏杆,玻璃栏杆,百叶,耐力板组装式雨棚,铝合金雨棚,PC板材(阳光板、耐力板)雨棚,全钢结构雨棚,玻璃钢结构雨棚,EPS构件,GRC构件,挤塑板,模压型聚苯乙烯泡沫塑料,泡沫玻璃,泡沫混凝土,聚苯颗粒保温砂浆,无机保温砂浆,硬泡聚氨酯保温板" ;
           // var keyWordStr = "玻化砖,微晶石,釉面砖,大理石,人造石,花岗岩,玄关柜";//,
                                   //水性外墙底漆,百叶,,,,,,,,,,,GRC构件,挤塑板,模压型聚苯乙烯泡沫塑料,泡沫玻璃,泡沫混凝土,聚苯颗粒保温砂浆,无机保温砂浆,硬泡聚氨酯保温板
                                   //,衣柜,橱柜,浴室柜,踢脚线,洗手盆,洗衣盆,浴缸,坐便器,小便斗,拖把池,厨卫龙头,台盆龙头,洗衣机龙头,浴缸龙头,拖把池龙头,厨盆,花洒,淋浴柱,地漏,毛巾架,置物架,淋浴屏,PVC墙纸,软包,无纺布,纯纸,墙布,排气扇,吸顶灯,射灯,筒灯,水晶吊灯,灯管,条形铝扣板,方形铝扣板,实木复合木地板,实木地板,金刚板,抽油烟机,燃气灶,消毒柜,热水器,微波炉,空调,浴霸,正压式新风系统,负压式新风系统,可视对讲,室内涂料,光纤入户箱,户内配电箱,开关插座,内墙涂料,功能内墙涂料,内墙弹性涂料,内墙底漆,内墙耐水腻子
            var keyWordList = keyWordStr.Split(new string[] { ",", "、" }, StringSplitOptions.RemoveEmptyEntries);
           
            foreach (var keyWord in keyWordList)
            {
                var curUrl = string.Format(materialUrl, keyWord, pageSize, pageBeginNum);
                var authorization = appHelper.GetHuiCongAuthorizationCode(curUrl);
                UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { Depth = 1, Authorization= authorization });

                //if (keyWord.Contains("景观"))
                //{
                //   var TkeyWord=keyWord.Replace("景观", "小区");
                //    curUrl = string.Format(materialUrl, TkeyWord, pageSize, pageBeginNum);
                //    authorization = appHelper.GetHuiCongAuthorizationCode(curUrl);
                //    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { Depth = 1, Authorization = authorization });

                //}
            }
            
           

            //var testUrl = "http://z.hc360.com/getmmtlast.cgi?dt=1&w=外墙面砖&v=59&e=100&c=供应信息&n=3101&m=2&H=1&bt=0";
            //var testAuthorization = appHelper.GetHuiCongAuthorizationCode(testUrl);
            //UrlQueue.Instance.EnQueue(new UrlInfo(testUrl) { Depth = 1, Authorization = testAuthorization });
            Console.WriteLine("正在加载账号数据");


            //Settings.HrefKeywords.Add(string.Format("/market/"));//先不加其他的

            //Settings.HrefKeywords.Add(string.Format("data/land/_________0_"));//先不加其他的
            ////是否guid
            ///不进行地址爬取
            Settings.RegularFilterExpressions.Add(@"luckymnXXXXXXXXXXXXXXXXXX");

            if (SimulateLogin())
            {
                //  Console.WriteLine("zluckymn模拟登陆成功");
            }
            else
            {
                Console.WriteLine("模拟登陆失败");
            }

        }

        private BsonDocument GetObj(string guid)
        {
            return dataop.FindOneByQuery(DataTableName, Query.EQ("guid", guid));
        }
        private bool hasExistObj(string guid)
        {
            // return  dataop.FindCount(_GuidDataTableName, Query.EQ("guid", guid)) > 0;
            return dataop.FindCount(DataTableName, Query.EQ("guid", guid)) > 0;
        }
        private string TrimStr(string str)
        {
            return str.Replace(" ", "").Replace("\"", "").Trim();
        }

        /// <summary>
        /// http://z.hc360.com/getmmtlast.cgi?dt=1&w=单开&v=59&e=1&c=供应信息&n=2&m=2&H=1&bt=0
        ///  </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            JObject jsonObj = JObject.Parse(args.Html);
            var curPageSize = GetUrlParam(args.Url, "e");//每页个数默认24
            var catName=HttpUtility.UrlDecode(GetUrlParam(args.Url, "w"));//每页个数默认24
            var curRecordIndexStr = GetUrlParam(args.Url, "n");//当前序号
            var curRecordIndex = 0;
            var searchKeyWordMd5= jsonObj["searchKeyWordMd5"];
            var searchResultfoAllNum= jsonObj["searchResultfoAllNum"];//总记录个数
            var allRecordCount = 0;
            var searchResultfoNum = jsonObj["searchResultfoNum"];//当前记录个数
            if (searchResultfoNum.ToString() != curPageSize)
            {
                Console.Write("当前记录个数:{0} url个数:{1}", searchResultfoNum, curPageSize);
                 
            }
            if(int.TryParse(searchResultfoAllNum.ToString(),out allRecordCount)==false)
            {
                Console.Write("总数无法转换{0}为整形", searchResultfoAllNum);
                return;
            }
            if (int.TryParse(curRecordIndexStr, out curRecordIndex) == false)
            {
                Console.Write("当前序号无法转换{0}为整形", curRecordIndexStr);
                return;
            }
            var data = jsonObj["searchResultInfo"];
            var insert = 0;
            var update = 0;
            if (data != null)
            {
                Console.WriteLine("获得数据:{0}",data.ToList().Count);
                foreach (var entInfo in data.ToList())
                {

                    BsonDocument document  = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(entInfo.ToString());
                   
                    var guid = document.Text("searchResultfoId");
                    document.Set("catName", catName);//目录名
                
                    if (!guidFilter.Contains(guid) && !hasExistObj(guid))
                    {
                        document.Set("guid", guid);
                       
                        insert++;
                        guidFilter.Add(guid);
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = document, Name = DataTableName, Type = StorageType.Insert });
                        ///添加到guid表
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("guid", guid).Add("type", DataTableName), Name = _GuidDataTableName, Type = StorageType.Insert });
                    }
                    else
                    {
                        //var curObj = GetObj(guid);
                        //if (curObj != null)
                        //{
                        //    if (!curObj.Text("catName").Contains(catName))
                        //    {
                        //        var updateBson = new BsonDocument();
                        //        updateBson.Add("catName", curObj.Text("catName") + "," + catName);
                        //        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBson, Name = DataTableName, Type = StorageType.Update,Query=Query.EQ("guid",guid) });
                        //    }
                        //}
                        update++;
                    }
                }
                Console.WriteLine("获得数据{3},当前{4}{5}添加：{0} 更新{1}剩余url:{2}", insert, update,UrlQueue.Instance.Count, allRecordCount, curRecordIndex,catName);
            }
            if (curRecordIndex==1&& curRecordIndex < allRecordCount)
            {
                InitNextUrl(catName, allRecordCount, curRecordIndex, pageSize);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="allRecordCount"></param>
        /// <param name="curNum"></param>
        /// <returns></returns>
        private void InitNextUrl(string keyWord,int allRecordCount,int curRecordIndex, int pageSize)
        {
            /// http://z.hc360.com/getmmtlast.cgi?dt=1&w=单开&v=59&e=24&c=供应信息&n=1&m=2&H=1&bt=0
            while (curRecordIndex < allRecordCount)
           {
                curRecordIndex = curRecordIndex + pageSize;
                var curUrl = string.Format(materialUrl, keyWord, pageSize, curRecordIndex);
                var authorization = appHelper.GetHuiCongAuthorizationCode(curUrl);
                UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { Depth = 1, Authorization = authorization });
                //return;
            }
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


        public string GetXYValue(int startIndex, int allLength, string html)
        {
            var hitResult = new StringBuilder();
            if (startIndex >= allLength) return string.Empty;
            var curChart = html[++startIndex];
            while (curChart != '"')
            {
                hitResult.AppendFormat(curChart.ToString());
                if (++startIndex < allLength)
                {
                    curChart = html[startIndex];
                }
                else
                {
                    break;
                }
            }
            return hitResult.ToString();
        }
        public string ValeFix(string str)
        {
            return str.Replace("\n", "").Replace("\r", "").Trim();
        }

        public string GetInnerText(HtmlNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.InnerText)) { throw new NullReferenceException(); }
            return node.InnerText;
        }

        public string[] GetStrSplited(string str)
        {
            var strArr = str.Split(new string[] { ":", "：" }, StringSplitOptions.RemoveEmptyEntries);
            return strArr;
        }
        /// <summary>
        /// IP限定处理，ip被限制 账号被限制跳转处理
        /// </summary>
        /// <param name="args"></param>
        public bool IPLimitProcess(DataReceivedEventArgs args)
        {
            var html = args.Html;
            if (string.IsNullOrEmpty(html))
            {
                return true;
            }
            if (html.Contains("Object moved")|| html.Contains("Service Unavailable") )//需要编写被限定IP的处理
            {
                return true;
            }

            if (!html.Contains("It is not legal"))
            {
                 return false;
              
            }
            
            return true;
        }
        /// <summary>
        /// url处理,是否可添加
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool CanAddUrl(AddUrlEventArgs args)
        {

            return true;
        }

        /// <summary>
        /// void错误处理
        /// </summary>
        /// <param name="args"></param>
        public void ErrorReceive(CrawlErrorEventArgs args)
        {


        }

        /// <summary>
        /// 模拟登陆，ip代理可能需要用到
        /// </summary>
        /// <returns></returns>
        public bool SimulateLogin()
        {
            return true;
            if (Settings.LandFangIUserId == 0)
            {
                var hitAccount = dataop.FindOneByQuery(DataTableNameAccount, Query.EQ("userName", "savegod523"));
                if (hitAccount != null)
                {
                    Settings.LandFangIUserId = hitAccount.Int("LandFangIUserId");
                }
                if (Settings.LandFangIUserId == 0)
                {
                    Settings.LandFangIUserId = 42638;//初始化
                }
            }
            // Settings.LandFangIUserId = Settings.LandFangIUserId + 1;
            Settings.LandFangIUserId = new Random().Next(3333, 143630);
            Settings.MaxAccountCrawlerCount = new Random().Next(50,200);
            DBChangeQueue.Instance.EnQueue(new StorageData()
            {
                Name = DataTableNameAccount,
                Document = new BsonDocument().Add("LandFangIUserId", Settings.LandFangIUserId.ToString()),
                Query = Query.EQ("userName", "savegod523"), Type=StorageType.Update
            });
            StartDBChangeProcess();
            return true;
             
        }

        /// <summary>
        /// ip无效处理
        /// </summary>
        private void IPInvalidProcess(IPProxy ipproxy)
        {
           
        }

        /// <summary>
        /// 对需要更新的队列数据更新操作进行批量处理,可考虑异步执行
        /// </summary>
        private void StartDBChangeProcess()
        {

            List<StorageData> updateList = new List<StorageData>();
            while (DBChangeQueue.Instance.Count > 0)
            {
                var curStorage = DBChangeQueue.Instance.DeQueue();
                if (curStorage != null)
                {
                    updateList.Add(curStorage);
                }
            }
            if (updateList.Count() > 0)
            {
                var result = dataop.BatchSaveStorageData(updateList);
                if (result.Status != Status.Successful)//出错进行重新添加处理
                {
                    foreach (var storageData in updateList)
                    {
                        DBChangeQueue.Instance.EnQueue(storageData);
                    }
                }
            }



        }
    }

}
