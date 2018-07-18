using DotNet.Utilities;
using HtmlAgilityPack;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// 用于城市与区域代码初始化
    /// </summary>
    public class FangProjectDetailCrawler_WenZhou : ISimpleCrawler
    {

         
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：
        ///  www.hhcool.com/cool62061/1.html?s=10&d=0
        /// 结束http://www.hhcool.com/cool62061/253.html?s=10&d=0
        /// </summary>
        private BloomFilter<string> filter;
        private BloomFilter<string> idFilter = new BloomFilter<string>(8000000);
        private const string _DataTableName = "FangCrawler.WenZhou";//存储的数据库表名

        /// <summary>
        /// 项目名
        /// </summary>
        public string DataTableName
        {
            get { return _DataTableName + "_Project"; }

        }
        /// <summary>
        /// 房屋名
        /// </summary>
        public string DataTableNameBuilding
        {
            get { return _DataTableName + "_Building"; }

        }
        /// <summary>
        /// 房屋名
        /// </summary>
        public string DataTableNameHouse
        {
            get { return _DataTableName + "_House"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableNameURL
        {
            get { return _DataTableName + "URL"; }

        }

        /// <summary>
        /// 区域
        /// </summary>
        public string DataTableNameRegion
        {
            get { return _DataTableName + "_Region"; }

        }
        /// <summary>
        /// 五类类型
        /// </summary>
        public string DataTableNameType
        {
            get { return _DataTableName + "_Type"; }

        }



        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public FangProjectDetailCrawler_WenZhou(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
        }


        List<BsonDocument> projectList = new List<BsonDocument>();
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
            //种子地址需要加布隆过滤

            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.MaxReTryTimes = 20;
            Settings.ThreadCount =10;
            Settings.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            Settings.ContentType = "application/x-www-form-urlencoded";
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.181 Safari/537.36";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "gzip, deflate");
            Settings.SimulateCookies = "__51cke__=; __tins__18980026=%7B%22sid%22%3A%201530601694251%2C%20%22vd%22%3A%208%2C%20%22expires%22%3A%201530604031753%7D; __51laig__=8";
            Settings.Referer = "www.nnfcxx.com";
            Console.WriteLine("正在获取已存在的url数据");
            //布隆url初始化,防止重复读取url
            Console.WriteLine("正在初始化选择url队列");
          
            projectList = dataop.FindAllByQuery(DataTableName, Query.NE("isUpdate", 2)).ToList();
            //iCPH
            foreach (var proj in projectList)
            {
                var mhUrl =string.Format("http://www.wzfg.com/realweb/stat/{0}", proj.Text("url")) ;
                if (!filter.Contains(mhUrl))//具体页面
                {
                    filter.Add(mhUrl);
                    UrlQueue.Instance.EnQueue(new UrlInfo(mhUrl) { Depth = 1, UniqueKey=proj.Text("projId") });
                }
            }
            // UrlQueue.Instance.EnQueue(new UrlInfo("http://www.hhcool.com/cool286073/1.html?s=11&d=0"+"&checkPageCount=1") { Depth = 1 });
            //Settings.SeedsAddress.Add(string.Format("http://fdc.fang.com/data/land/CitySelect.aspx"));
            Settings.RegularFilterExpressions.Add("XXX");//不添加其他
            if (SimulateLogin())
            {
                //  Console.WriteLine("zluckymn模拟登陆成功");
            }
            else
            {
                Console.WriteLine("初始化失败");
            }

        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Console.WriteLine("下载成功");
        }

        public static object lockThis = new object();
        public static List<Task> allTask = new List<Task>();
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// cool62061/1.html?s=10&d=0
        /// 
        /// </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var root = htmlDoc.DocumentNode;
            var saleInfo = htmlDoc.GetElementbyId("saleInfo");
            var withdrawInfo = htmlDoc.GetElementbyId("withdrawInfo");
            if (saleInfo == null)
            {
                return;
            }
           

            var projId = args.urlInfo.UniqueKey;
            var page = GetUrlParam(args.Url, "page");
            var updateDoc = new BsonDocument();
            var contentTableNode = saleInfo.SelectSingleNode("./table");
            //获取房间信息
            if (contentTableNode != null)
            {
                var hitTableTxt = contentTableNode.InnerText;
                if (!string.IsNullOrEmpty(hitTableTxt))
                {
                    var releaseDate = Toolslib.Str.Sub(hitTableTxt, "发证日期：", "所在地区").Trim();
                    var sampleArea = Toolslib.Str.Sub(hitTableTxt, "样本区域：", "项目测算面积").Trim();
                    var projArea = Toolslib.Str.Sub(hitTableTxt, "项目测算面积：", "项目名称").Trim();

                    updateDoc.Add("releaseDate", releaseDate);
                    updateDoc.Add("sampleArea", sampleArea);
                    updateDoc.Add("projArea", projArea.Replace("平方米",""));
                    updateDoc.Add("isUpdate", 2);
                }
           }
            var saleInfoNodes = withdrawInfo.SelectNodes("./table/tr/td/table/tr[@ title]");
            //获取房间信息
            if (saleInfoNodes != null)
            {
                
                foreach (var trNode in saleInfoNodes)
                {
                  
                    var trInfo = trNode.InnerText;
                    var splitArray = trInfo.Split(new string[] { "\r","\n","\t"," " }, StringSplitOptions.RemoveEmptyEntries);
                    if (splitArray.Length >= 3)
                    {
                        updateDoc.Add(splitArray[0].Trim(), new BsonDocument().Add("套数", splitArray[1].Replace("套","").Trim()).Add("建筑面积", splitArray[2].Replace("㎡","").Trim()));
                    }
                }
            }
            if (updateDoc.ElementCount > 0)
            {
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateDoc, Query = Query.EQ("projId", projId), Name = DataTableName, Type = StorageType.Update });
            }
            //获取楼栋信息
            var tdBldList = htmlDoc.GetElementbyId("tdBldList");
            if (tdBldList != null)
            {
                var aNodes = tdBldList.SelectNodes("./a");
                var add = 0;
                var update = 0;
                foreach (HtmlNode aNode in aNodes)
                {
                   
                    if (aNode != null)
                    {

                        var title = aNode.Attributes["title"].Value.ToString();
                        if (string.IsNullOrEmpty(title))
                        {
                            Console.WriteLine("无法获取title");
                            continue;
                        }
                        var idStr = aNode.Attributes["id"].Value.ToString();
                        var idArray = idStr.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                        if (idArray.Length <= 0) continue;
                        var id = idArray[0].Trim();
                        var name = Toolslib.Str.Sub(title, "幢名：", "·地址").Trim();
                        var address= Toolslib.Str.Sub(title, "地址：", "·总层数").Trim();
                        var floorCount = Toolslib.Str.Sub(title, "总层数：", "·户室数").Trim();
                        var houseCount = Toolslib.Str.Sub(title, "户室数：", "·总建筑面积").Trim();
                        var buildingArea = Toolslib.Str.Sub(title, "总建筑面积：", "·建筑结构").Trim().Replace("㎡","");
                        var structure = Toolslib.Str.Sub(title, "建筑结构：", "·项目测算面积").Trim();
                        var projArea = Toolslib.Str.Sub(title, "项目测算面积：", "").Trim().Replace("㎡", "");

                        var curDoc = new BsonDocument();
                        curDoc.Add("projId", projId);
                        curDoc.Add("buildNO", id.Replace("Bt", "").Replace("Bd", ""));
                        curDoc.Add("name", name);
                        curDoc.Add("address", address);
                        curDoc.Add("floorCount", floorCount);
                        curDoc.Add("houseCount", houseCount);
                        curDoc.Add("buildingArea", buildingArea);
                        curDoc.Add("structure", structure);
                        curDoc.Add("projArea", projArea);
                         
                        ///cool286073/1.html?s=11
                        //提取数字

                        if (!idFilter.Contains(id) && !hasExistObj(id))
                        {
                            add++;
                            this.idFilter.Add(id);
                            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curDoc, Name = DataTableNameBuilding, Type = StorageType.Insert });
                        }
                        else
                        {
                             
                            update++;
                            //Console.WriteLine("已存在");
                        }
                    }
                }
                Console.WriteLine("增加:{0}更新:{1}", add, update);
 
            }
            //获取房间信息
            var tdRooms = htmlDoc.GetElementbyId("tdRooms");
            if (tdRooms != null)
            {
                var roomNodes = tdRooms.SelectNodes("./div/table[@id]");
                var add = 0;
                var update = 0;
                foreach (HtmlNode roomNode in roomNodes)
                {
                      
                        var buildingInfo= roomNode.Attributes["id"].Value.ToString();
                        var buildingInfoArray = buildingInfo.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                        var buildNO = buildingInfoArray[0].Replace("Bt","").Replace("Bd","");
                        var aNodes = roomNode.SelectNodes("./tr/td/a[@id]");
                        foreach (var aNode  in aNodes) {

                        var id = aNode.Attributes["id"].Value.ToString().Replace("R", "");
                        var saleArea=Toolslib.Str.Sub(aNode.Attributes["title"].Value.ToString(),"", "㎡");
                        var price= Toolslib.Str.Sub(aNode.Attributes["title"].Value.ToString(), "一房一价：", "元");
                        var className = aNode.Attributes["class"].Value.ToString();
                        var href = aNode.Attributes["href"].Value.ToString();
                        var roomNO = aNode.InnerText.Replace("◆", "");
                        var roomDoc = new BsonDocument();
                        roomDoc.Add("projId", projId);
                        roomDoc.Add("buildNO", buildNO);
                        roomDoc.Add("roomId", id);
                        roomDoc.Add("roomNO", roomNO);
                        roomDoc.Add("saleArea", saleArea);
                        
                        roomDoc.Add("url", string.Format("http://www.wzfg.com/realweb/stat/Frame.jsp?URL=HouseInfoUser5.jsp&houseID={0}", id));

                        var saleStatus = string.Empty;
                        switch (className)
                        {
                            case "G1":
                            case "B1":
                                saleStatus = "正常发售";
                                roomDoc.Set("saleStatus", "0");
                                break;
                            case "G2":
                            case "B2":
                                saleStatus = "安置房";
                                roomDoc.Set("saleStatus", "0");
                                break;
                            case "G3":
                            case "B3":
                                saleStatus = "自留房";
                                roomDoc.Set("saleStatus", "0");
                                break;
                            case "G4":
                            case "B4":
                                saleStatus = "非出售";
                                roomDoc.Set("saleStatus", "0");
                                break;
                            case "G10":
                            case "B10":
                                saleStatus = "已认购";
                                roomDoc.Set("saleStatus", "1");
                                break;
                            case "G5":
                            case "B5":
                                saleStatus = "已签预定协议";
                                roomDoc.Set("saleStatus", "1");
                                break;
                            case "G6":
                            case "B6":
                                saleStatus = "已签合同";
                                roomDoc.Set("saleStatus", "1");
                                break;
                            case "G7":
                            case "B7":
                                saleStatus = "合同已登记";
                                roomDoc.Set("saleStatus", "1");
                                break;
                            case "G8":
                            case "B8":
                                saleStatus = "已认购";
                                roomDoc.Set("saleStatus", "1");
                                break;
                           case "B":
                                saleStatus = "不在任何项目内";
                                break;
                        }
                        roomDoc.Set("saleStatusName", saleStatus);
                        ///cool286073/1.html?s=11
                        //提取数字
                        if (!string.IsNullOrEmpty(price))
                        {
                            roomDoc.Set("price", price);
                        }
                        if (!idFilter.Contains(id) && !hasExistObj(DataTableNameHouse, "roomId", id))
                        {
                            add++;
                            this.idFilter.Add(id);
                            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = roomDoc, Name = DataTableNameHouse, Type = StorageType.Insert });
                        }
                        else
                        {

                            update++;
                            //Console.WriteLine("已存在");
                        }
                    }
                   
                }
                Console.WriteLine("增加:{0}更新:{1}", add, update);

            }
        }


        private bool hasExistObj(string guid)
        {
            return (this.dataop.FindCount(this.DataTableNameBuilding, Query.EQ("buildNO", guid)) > 0);
        }
        private bool hasExistObj(string tableName,string keyName,string guid)
        {
            return (this.dataop.FindCount(tableName, Query.EQ(keyName, guid)) > 0);
        }
        private static string GetGuidFromUrl(string url)
        {
            int num = url.LastIndexOf("=");
            int num2 = url.Length;
            if ((num != -1) && (num2 != -1))
            {
                if (num > num2)
                {
                    int num3 = num;
                    num = num2;
                    num2 = num3;
                }
                return url.Substring(num + 1, (num2 - num) - 1);
            }
            return string.Empty;
        }

        public string GetInnerText(HtmlNode node)
        {
            if ((node == null) || string.IsNullOrEmpty(node.InnerText))
            {
                throw new NullReferenceException();
            }
            return node.InnerText;
        }

        private static string GetQueryString(string url)
        {
            int index = url.IndexOf("?");
            if (index != -1)
            {
                return url.Substring(index + 1, (url.Length - index) - 1);
            }
            return string.Empty;
        }

        public string[] GetStrSplited(string str)
        {
            string[] separator = new string[] { ":", "：" };
            return str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string GetUrlParam(string url, string name)
        {
            NameValueCollection values = HttpUtility.ParseQueryString(GetQueryString(url));
            return ((values[name] != null) ? values[name].ToString() : string.Empty);
        }



        #region method
 
        /// <summary>
        /// IP限定处理，ip被限制 账号被限制跳转处理
        /// </summary>
        /// <param name="args"></param>
        public bool IPLimitProcess(DataReceivedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.Html) || args.Html.Contains("503 Service Unavailable"))//需要编写被限定IP的处理
            {
                return true;
            }
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var root = htmlDoc.DocumentNode;
            var saleInfo = htmlDoc.GetElementbyId("saleInfo");
            if (saleInfo == null)
            {
                
                return true;
            }
             return false;
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

        }



        /// <summary>
        /// ip无效处理
        /// </summary>
        private void IPInvalidProcess(IPProxy ipproxy)
        {
            Settings.SetUnviableIP(ipproxy);//设置为无效代理
            if (ipproxy != null)
            {
                DBChangeQueue.Instance.EnQueue(new StorageData()
                {
                    Name = "IPProxy",
                    Document = new BsonDocument().Add("status", "1"),
                    Query = Query.EQ("ip", ipproxy.IP)
                });
                StartDBChangeProcess();
            }

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
        #endregion
    }

}
