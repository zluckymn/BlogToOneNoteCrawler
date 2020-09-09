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
    ///  
    /// https://menpai.member.fun/api/Activity/GetActivityList
    ///
    /// </summary>
    public class KuaiKan_CommentDetailCrawler : SimpleCrawlerBase
    {


#pragma warning disable CS0414 // 字段“PositionDetailCrawler_LiePin.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“PositionDetailCrawler_LiePin.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 20;

        public string DataTableUserName{ get { return "ManHua_User"; } }
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public KuaiKan_CommentDetailCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "ManHua_Comics_Comment_KuaiKan";//漫画评论
            DataTableCategoryName = "ManHua_Comics_KuaiKan";//漫画
            updatedValue = "3";//是否更新字段
            uniqueKeyField = "guid";
        }
        BloomFilter<string> _guidFilter = new BloomFilter<string>(900000);
        List<BsonDocument> allHitObjList;
        public void initialUrl()
        {
            allHitObjList = FindDataForUpdate(dataTableName: DataTableCategoryName, fields:new string[] { "guid","topic_id" },limit:10000,query:Query.NE(updatedField, updatedValue));
            //初始化布隆过滤器
            foreach (var hitObj in allHitObjList)
            {

                var guid = hitObj.Text("guid");
                
                var topic_id = hitObj.Text("topic_id");
                var url = $"https://api.kkmh.com/v2/comments/cruel/floor_list?target_type=comic&target_id={guid}&order=score&offset=0&limit={takeCount}&total=false&source=0";
                if (!filter.Contains(url))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(url) { UniqueKey = guid, extraData = topic_id, UrlSplitTimes = 0 }); ;
                    filter.Add(url);// 防止执行2次
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
            Settings.AutoSpeedLimitMinMSecond = 100;
            Settings.AutoSpeedLimitMinMSecond = 1000;
            //Settings.ContentType = "application/json; charset=UTF-8";
            Settings.UserAgent = "Kuaikan/5.36.0/536000(Android;6.0.1;MuMu;kuaikan17;WIFI;1120*700)";
            Settings.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "gzip");
            Settings.HeadSetDic.Add("muid", "832ff6e87dfb90374d88cb7671668fe0");
            Settings.HeadSetDic.Add("package-id", "com.kuaikan.comic");
            Settings.HeadSetDic.Add("lower-flow", "No");
            Settings.HeadSetDic.Add("app-info", "eyJhbmRyb2lkX2lkIjoiOThlMzFjMTA1ZTgxNGY3NCIsImFwcF9zZWNyZXRfc2lnbiI6Ijk1MTdhMzdlZTAzNTMzZWE2NTU5MDExMDJhOTJmZTk5IiwiYmQiOiJOZXRlYXNlIiwiY2EiOjAsImN0IjoyMCwiZGV2dCI6MSwiZHBpIjoyNDAsImhlaWdodCI6MTEyMCwiaW1laSI6IjUyMDAwMDAwMDEwMDg1MCIsImltc2kiOiIiLCJtYWMiOiI4OjA6Mjc6Mzc6Mzc6OGQiLCJtb2RlbCI6Ik11TXUiLCJvdiI6IjYuMC4xIiwicGhvbmVOdW1iZXIiOiIiLCJ3aWR0aCI6NzAwfQ==");
            Settings.HeadSetDic.Add("x-device", "A:98e31c105e814f74");
            //Settings.HeadSetDic.Add(":scheme", "https");
            //Settings.HeadSetDic.Add(":path", "/Argeon_Highmayne");
            //Settings.HeadSetDic.Add(":method", "GET");
            //Settings.HeadSetDic.Add(":authority", "duelyst.gamepedia.com");
            //Settings.SimulateCookies = "Geo={%22region%22:%22FJ%22%2C%22country%22:%22CN%22%2C%22continent%22:%22AS%22}; _ga=GA1.2.987882347.1594083335; __qca=P0-1227115721-1594083385201; __gads=ID=b05f8069fced27fa:T=1594083398:S=ALNI_MaHAGtfVP3fAbyrBDkvfMj4w7Oz-w; vector-nav-p-Factions=true; crfgL0cSt0r=true; _gid=GA1.2.1608004051.1594206130; wikia_beacon_id=3Dmw2iT6gy; tracking_session_id=nwxlyfoG9c; ___rl__test__cookies=1594206451799; OUTFOX_SEARCH_USER_ID_NCOO=76744037.13684113; _gat_tracker0=1; _gat_tracker1=1; mnet_session_depth=1%7C1594207140419; pv_number=9; pv_number_global=9; _sg_b_p=%2FLyonar_Kingdoms%2C%2FArgeon_Highmayne%2C%2FSonghai_Empire%2C%2FVetruvian_Imperium%2C%2FAbyssian_Host%2C%2FMagmar_Aspects%2C%2FVanar_Kindred%2C%2FNeutral%2C%2FSonghai_Empire%2C%2FArgeon_Highmayne; _sg_b_v=2%3B1542%3B1594206133";
            Settings.Referer = "Expires=Fri, 24-Jul-2020 12:10:04 GMT;Max-Age=86400;kk_s_t=1595507276467;Domain=.kkmh.com;Path=/";
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
#pragma warning disable CS0414 // 字段“PositionDetailCrawler_LiePin.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“PositionDetailCrawler_LiePin.noCountTimes”已被赋值，但从未使用过它的值
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            if (CanLoadNewData())
            {
                initialUrl();
            }
            var guid = args.urlInfo.UniqueKey;//章节
            var topic_id = args.urlInfo.extraData;//漫画Id
            var UrlSplitTimes= args.urlInfo.UrlSplitTimes;//分割次数
            var hmtl = args.Html;
            var root = hmtl.GetBsonDocFromJson();
         
            if (root == null) return;
          
            var data = root.GetBsonDocument("data");
            if (data != null)
            {
                var total = data.Int("total");
                var comment_floors = data.GetBsonDocumentList("comment_floors");

                foreach (var comment in comment_floors)
                {
                  
                    var commentInfo = comment.GetBsonDocument("root");

                    commentInfo.Set("guid", commentInfo.Text("id"));
                    commentInfo.Set("commic_Id", guid);
                    commentInfo.Set("topic_id", topic_id);
                   
                    var childrenComment = comment.GetBsonDocumentList("children_comments");
                    if (childrenComment.Count > 0) {
                       
                        commentInfo.Set("children_comments", BsonArray.Create(childrenComment));
                        commentInfo.Set("children_total", childrenComment.Count);
                    }
                  
                  
                    var user = commentInfo.GetBsonDocument("user");
                    user.Set("guid", user.Text("id"));

                    commentInfo.Set("userId", user.Text("guid"));
                    commentInfo.Set("userNickname", user.Text("nickname"));
                    commentInfo.Remove("user");
                    PushData(commentInfo);

                    PushData(user, dataTable: DataTableUserName);
                }
                //只收集2页评论
                //if (total > takeCount&& UrlSplitTimes==0)
                //{
                //    var url = args.urlInfo.UrlString;
                //    for (var count = takeCount; count <= takeCount*2; count += takeCount)
                //    {
                //        var hitUrl = ReplaceUrlParam(url, "offset", count.ToString());
                //        if (!filter.Contains(hitUrl))
                //        {
                //            UrlQueue.Instance.EnQueue(new UrlInfo(hitUrl) { UniqueKey = args.urlInfo.UniqueKey, extraData = args.urlInfo.extraData, UrlSplitTimes=1 });
                //            filter.Add(url);// 防止执行2次
                //        }

                //    }
                //}
            }

           
            ShowStatus();

            UpdateDataParentCategory(guid);

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
