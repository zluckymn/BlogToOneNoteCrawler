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
using Helper;

namespace SimpleCrawler.Demo
{

    /// <summary>
    /// 门派url
    /// https://www.xiamh.com/comic/16745.html
    ///
    /// </summary>
    public class MHListCrawler_XiaManHua : SimpleCrawlerBase
    {


#pragma warning disable CS0414 // 字段“PositionListCrawler_LiePin.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“PositionListCrawler_LiePin.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 48;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public MHListCrawler_XiaManHua(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "ManHua_XiaManHua";//房间
            DataTableCategoryName = "ManHua_Category_XiaManHua";//房间
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";

        }
        List<BsonDocument> allmanHuaList = new List<BsonDocument>();
        public void initialUrl()
        {

            var url = "https://www.xiamh.com/comic/16745.html";
            //获取目录
            var html = url.UrlGetHtml();
            var htmlObj = html.HtmlLoad();
            var categoryNode = htmlObj.GetElementbyId("mh-chapter-list-ol-0");
            if (categoryNode == null) return;
            foreach (var liNode in categoryNode.ChildNodes.Where(c => c.Name == "li"))
            {
                var aNode = liNode.SelectSingleNode("./a");
                if (aNode == null) continue;
                var link = GetNodeAttribute(aNode, "href");
                var topicName = aNode.InnerText.Trim();
                var comicUrl = $"https://m.xiamh.com{link}";
                var comicId = GetGuidFromUrl(comicUrl, "/", ".");
                if (!filter.Contains(comicUrl))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(comicUrl) { UniqueKey = comicId, extraData = topicName });
                    filter.Add(comicUrl);// 防止执行2次
                }
            }




        }

        override
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //种子地址需要加布隆过滤
            //Settings.Depth = 4;
            //代理ip模式
            //种子地址需要加布隆过滤
            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.ThreadCount = 1;
            Settings.MaxReTryTimes = 10;
            // Settings.AutoSpeedLimit = true;
            //Settings.AutoSpeedLimitMinMSecond = 3000;
            //Settings.AutoSpeedLimitMinMSecond = 10000;
            //Settings.ContentType = "application/json; charset=UTF-8";
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.75 Safari/537.36";
            Settings.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "gzip, deflate, br");
            //Settings.HeadSetDic.Add(":scheme", "https");
            //Settings.HeadSetDic.Add(":path", "/Argeon_Highmayne");
            //Settings.HeadSetDic.Add(":method", "GET");
            //Settings.HeadSetDic.Add(":authority", "duelyst.gamepedia.com");
            //Settings.SimulateCookies = "Geo={%22region%22:%22FJ%22%2C%22country%22:%22CN%22%2C%22continent%22:%22AS%22}; _ga=GA1.2.987882347.1594083335; __qca=P0-1227115721-1594083385201; __gads=ID=b05f8069fced27fa:T=1594083398:S=ALNI_MaHAGtfVP3fAbyrBDkvfMj4w7Oz-w; vector-nav-p-Factions=true; crfgL0cSt0r=true; _gid=GA1.2.1608004051.1594206130; wikia_beacon_id=3Dmw2iT6gy; tracking_session_id=nwxlyfoG9c; ___rl__test__cookies=1594206451799; OUTFOX_SEARCH_USER_ID_NCOO=76744037.13684113; _gat_tracker0=1; _gat_tracker1=1; mnet_session_depth=1%7C1594207140419; pv_number=9; pv_number_global=9; _sg_b_p=%2FLyonar_Kingdoms%2C%2FArgeon_Highmayne%2C%2FSonghai_Empire%2C%2FVetruvian_Imperium%2C%2FAbyssian_Host%2C%2FMagmar_Aspects%2C%2FVanar_Kindred%2C%2FNeutral%2C%2FSonghai_Empire%2C%2FArgeon_Highmayne; _sg_b_v=2%3B1542%3B1594206133";
            Settings.Referer = "www.xiamh.com";
            Settings.SimulateCookies = "UM_distinctid=173c365e3a15f4-067e1ab688f244-7a1b34-13c680-173c365e3a2b5c; CNZZDATA1277712665=1198216053-1596709552-https%253A%252F%252Fwww.baidu.com%252F%7C1596709552; mac_history=%7Bvideo%3A%5B%7B%22name%22%3A%22%u6211%u72EC%u81EA%u5347%u7EA7%22%2C%22link%22%3A%22/comic/16745.html%22%2C%22pic%22%3A%22https%3A//img.xpelly.com/upload/pic/2019-11-16/157389388416.jpg%22%2C%22comicname%22%3A%22%u5E8F%u7AE0%20%u9876%u7EA7%u730E%u4EBA%u7684%u4FEE%u7F57%u4E4B%u9053%22%2C%22comiclink%22%3A%22/comic/16745/898457.html%22%2C%22comicp%22%3A%221%22%7D%5D%7D";
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            initialUrl();
            base.SettingInit();



        }
#pragma warning disable CS0414 // 字段“PositionListCrawler_LiePin.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;

#pragma warning restore CS0414 // 字段“PositionListCrawler_LiePin.noCountTimes”已被赋值，但从未使用过它的值
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            var comicId = args.urlInfo.UniqueKey;
            var title = args.urlInfo.extraData;
            var htmlObj = args.Html.HtmlLoad();
            var jsonStr =  args.Html.ToolsSubStr("z_img='", "';").Replace("\\","").Replace("\"","").Replace("[","").Replace("]","") ;
            var jsonObj = jsonStr.Split(new string[] { ","},StringSplitOptions.RemoveEmptyEntries);
            if (jsonObj != null)
            {
                var index = 0;
                foreach (var imgUrl in jsonObj) {
                    index++;
                    var fileName = $"{comicId}_{index}.jpg";
                    DownLoadFile("https://img.xpelly.com/"+imgUrl.ToString(), fileName, $"xiaomh/{title}");
                }
               
            }
            
            
            ShowStatus();
        }

        /// <summary>
        /// IP限定处理，ip被限制 账号被限制跳转处理
        /// </summary>
        /// <param name="args"></param>
        override
        public bool IPLimitProcess(DataReceivedEventArgs args)
        {
            try
            { 

                if (args.Html.Contains("k_total"))//需要编写被限定IP的处理
                {
                    return false;
                }
                else
                {
                    Console.WriteLine(args.Url);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return true;
            }
        }


    }

}
