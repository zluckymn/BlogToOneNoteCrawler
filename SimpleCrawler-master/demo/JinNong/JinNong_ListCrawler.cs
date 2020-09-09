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
using System.Web.UI.WebControls;

namespace SimpleCrawler.Demo
{

    /// <summary>
    /// 金农网
    ///  
    ///
    /// </summary>
    public class JinNong_ListCrawler : SimpleCrawlerBase
    {

        
#pragma warning disable CS0414 // 字段“PositionListCrawler_LiePin.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“PositionListCrawler_LiePin.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 6;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public JinNong_ListCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "JinNong_Enterprise";//房间
            DataTableCategoryName = "JinNong_Enterprise_Category";//房间
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";
            
        }
       
        List<BsonDocument> allmanHuaList = new List<BsonDocument>();
        string url = "http://m.jinnong.cn/jnwapcompanylistlist_ajax.htm";
        public void initialUrl()
        {
             var industryList = "畜牧,果蔬,水产,农业,农药,化肥,种子,农机,粮油,食品,肉制品,饲料,兽药,园林,苗木,花卉".Split(new string[] { "," },StringSplitOptions.RemoveEmptyEntries);
            //var industryList = "中药材".Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var industry in industryList)
            { 
                var postData = $"pageno=1&companylistAllCount=0&companyname=&jnindustry={HttpUtility.UrlEncode(industry)}&jnindustry_E=&pcity=&citycity=&quxian=&status=&busitype=&busitype_E=&tel=&email=&url=&msgconfirm=&companytagsid=";
                if (!filter.Contains(postData)) {
                    UrlQueue.Instance.EnQueue(new UrlInfo(url) { UniqueKey = industry, PostData= postData });
                    filter.Add(postData);// 防止执行2次
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
            Settings.ThreadCount =5;
            Settings.MaxReTryTimes = 10;
            // Settings.AutoSpeedLimit = true;
            //Settings.AutoSpeedLimitMinMSecond = 3000;
            //Settings.AutoSpeedLimitMinMSecond = 10000;
            //Settings.ContentType = "application/json; charset=UTF-8";
            Settings.UserAgent = "Mozilla/5.0 (Linux; Android 6.0.1; MuMu Build/V417IR; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/52.0.2743.100 Mobile Safari/537.36";
            Settings.Accept = "*/*";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "gzip, deflate");
            Settings.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            //Settings.HeadSetDic.Add(":scheme", "https");
            //Settings.HeadSetDic.Add(":path", "/Argeon_Highmayne");
            //Settings.HeadSetDic.Add(":method", "GET");
            //Settings.HeadSetDic.Add(":authority", "duelyst.gamepedia.com");
            //Settings.SimulateCookies = "Geo={%22region%22:%22FJ%22%2C%22country%22:%22CN%22%2C%22continent%22:%22AS%22}; _ga=GA1.2.987882347.1594083335; __qca=P0-1227115721-1594083385201; __gads=ID=b05f8069fced27fa:T=1594083398:S=ALNI_MaHAGtfVP3fAbyrBDkvfMj4w7Oz-w; vector-nav-p-Factions=true; crfgL0cSt0r=true; _gid=GA1.2.1608004051.1594206130; wikia_beacon_id=3Dmw2iT6gy; tracking_session_id=nwxlyfoG9c; ___rl__test__cookies=1594206451799; OUTFOX_SEARCH_USER_ID_NCOO=76744037.13684113; _gat_tracker0=1; _gat_tracker1=1; mnet_session_depth=1%7C1594207140419; pv_number=9; pv_number_global=9; _sg_b_p=%2FLyonar_Kingdoms%2C%2FArgeon_Highmayne%2C%2FSonghai_Empire%2C%2FVetruvian_Imperium%2C%2FAbyssian_Host%2C%2FMagmar_Aspects%2C%2FVanar_Kindred%2C%2FNeutral%2C%2FSonghai_Empire%2C%2FArgeon_Highmayne; _sg_b_v=2%3B1542%3B1594206133";
            Settings.Referer = "m.jinnong.cn";
            Settings.SimulateCookies = "jeesite.session.id=65a7f9816bd54f3ab63a7b8c43cc393c; JSESSIONID=39B34644EC93DE8FD87419A4AA4D86C1";
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            //var areas = new string[] { "040010180"};//, "040010180", "040010100", "040010130" "040010220"
            //foreach (var area in areas)
            //{
            //    initialUrl(area,0 );
            //}
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
            var tagValue = args.urlInfo.UniqueKey;
            var tagName = args.urlInfo.extraData;
            var hmtl = args.Html;
            var root = hmtl.GetBsonDocFromJson();
            var type = args.urlInfo.UniqueKey;
            if (root == null) return;
            var total = root.Int("count");//总页数
            var pageno = root.Int("pageno");//总个数
        
            var data = root.GetBsonDocumentList("data");
             
            if (data != null)
            {
               
                 foreach (var ent in data)
                {
                    ent.Set("guid", ent.Text("id"));
                    ent.Set("reg_capi", ent.Text("zczb").ToMoney());
                    ent.Set(type, 1);// 当前分类可能多个分类
                    PushData(ent);
                }
                var updateDoc = new BsonDocument();
                updateDoc.Set("name", tagValue);
                updateDoc.Set("guid", tagValue.EncodeMD5().ToLower().ToString());
                updateDoc.Set("pageno", pageno);
                UpdateData(updateDoc,dataTable: DataTableCategoryName);
            }
            if (total >takeCount&& pageno<=1)
            {
                var pageCount = root.Int("pagecount");//总个数
                var url = args.urlInfo.UrlString;
                var postdata = args.urlInfo.PostData;
                for (var index = pageno+1; index <= pageCount; index++)
                {
                    var  newPostData = ReplaceUrlParam(postdata, "pageno", index.ToString(), "",true);
                    newPostData = ReplaceUrlParam(newPostData, "companylistAllCount", total.ToString(), "&");
                    if (!filter.Contains(newPostData))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(url) { UniqueKey=args.urlInfo.UniqueKey, PostData = newPostData });
                        filter.Add(newPostData);// 防止执行2次
                    }
                    
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

                if (args.Html.Contains("data"))//需要编写被限定IP的处理
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
