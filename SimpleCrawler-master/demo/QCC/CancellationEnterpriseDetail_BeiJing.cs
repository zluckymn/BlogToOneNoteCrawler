using DotNet.Utilities;
using HtmlAgilityPack;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
    ///http://scjgj.beijing.gov.cn/djgg/page/notice/release/moreNotice.html?listSort=1&tdsourcetag=s_pctim_aiomsg
    /// noticeType
    /// {"data":[{"data":[{"text":"企业注销","value":"1"},
    /// {"text":"企业减资","value":"2"},
    /// {"text":"吸收合并","value":"3"},
    /// {"text":"新设合并","value":"4"},
    /// {"text":"个体转企业","value":"6"},
    /// {"text":"存续分立","value":"8"},
    /// {"text":"解散分立","value":"9"},
    /// {"text":"撤销公告","value":"s"},
    /// {"text":"企业变更","value":"10"},
    /// {"text":"企业注销","value":"11"},
    /// {"text":"不适宜名称","value":"12"},
    /// {"text":"清算组备案公告","value":"13"},
    /// {"text":"债权人公告","value":"14"}
    /// ],"vtype":"codeset"}]}
    public class CancellationEnterpriseDetail_BeiJing : SimpleCrawlerBase
    {
 
        List<BsonDocument> cityUrlList = new List<BsonDocument>();
        List<string> cityNameList = new List<string>();
#pragma warning disable CS0414 // 字段“CancellationEnterpriseDetail_BeiJing.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“CancellationEnterpriseDetail_BeiJing.isUpdate”已被赋值，但从未使用过它的值
#pragma warning disable CS0414 // 字段“CancellationEnterpriseDetail_BeiJing.limit”已被赋值，但从未使用过它的值
        int limit = 1000;
#pragma warning restore CS0414 // 字段“CancellationEnterpriseDetail_BeiJing.limit”已被赋值，但从未使用过它的值
        string curUrl = "http://scjgj.beijing.gov.cn/djgg/release/getNoticeByNoticeId.do";
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public CancellationEnterpriseDetail_BeiJing(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop):base(_Settings, _filter, _dataop)
        {
            DataTableName = "QCCEnterpriseKey_Cancellation_BeiJing";//注销企业
        }
        override
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //种子地址需要加布隆过滤
            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.ThreadCount = 5;
            Settings.MaxReTryTimes = 10;
            Console.WriteLine("正在获取已存在的url数据");
            var headerDic = new Dictionary<string, string>();
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.75 Safari/537.36";
            Settings.Accept = "application/json, text/javascript, */*; q=0.01";
            Settings.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            headerDic.Add("Origin", "http://scjgj.beijing.gov.cn");
            headerDic.Add("Accept-Encoding", "gzip, deflate");
            Settings.HeadSetDic= headerDic;
            var hitObjList = dataop.FindAllByQuery(DataTableName, Query.NE("isUpdated",1)).SetFields("guid").ToList();
          
           // var postData = "{\"data\":[{\"vtype\":\"pagination\",\"name\":\"pagerows\",\"data\":" + limit + "},{\"vtype\":\"pagination\",\"name\":\"totalrows\",\"data\":0},{\"vtype\":\"pagination\",\"name\":\"page\",\"data\":1},{\"vtype\":\"pagination\",\"name\":\"sortName\",\"data\":\"\"},{\"vtype\":\"pagination\",\"name\":\"sortFlag\",\"data\":\"\"},{\"vtype\":\"attr\",\"name\":\"noticeType\",\"data\":\"1\"},{\"vtype\":\"attr\",\"name\":\"state\",\"data\":\"processing\"}]}";
           Console.WriteLine("初始化布隆过滤器");
            //初始化布隆过滤器
            foreach (var item in hitObjList)
            {
                var postData = $"noticeId={item.Text("guid")}";
               
                if (!filter.Contains(postData))
                {  
                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { PostData= postData,UniqueKey= item.Text("guid") });
                    filter.Add(postData);// 防止执行2次
                }
            }

            base.SettingInit();
             
            

        }
#pragma warning disable CS0414 // 字段“CancellationEnterpriseDetail_BeiJing.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“CancellationEnterpriseDetail_BeiJing.noCountTimes”已被赋值，但从未使用过它的值
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            JObject jsonObj = GetJsonObject(hmtl);
            var data = jsonObj["data"][0]["data"];
            var guid = args.urlInfo.UniqueKey;
            var countDays = GetJsonValueString(data, "countDays");
            var entState = GetJsonValueString(data, "entState");
            var noticeObj = data["notice"];
            if (noticeObj != null)
            {
                var noticeJson = noticeObj.ToString();
                var updateBson = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(noticeJson);
                if (hasExistObj(guid))
                {
                    updateBson.Set("isUpdated", 1);
                    DBChangeQueue.Instance.EnQueue(new StorageData()
                    {
                        Name = DataTableName,
                        Document = updateBson,
                        Type = StorageType.Update,
                        Query = Query.EQ("guid", guid)
                    });
                    Console.WriteLine($"更新{guid}_{updateBson.Text("entName")}_{updateBson.Text("contactPhone")}");
                }

            }

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
                JObject jsonObj = GetJsonObject(args.Html);
                if (jsonObj == null)//需要编写被限定IP的处理
                {
                    return true;
                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return true;
            }
            return false;
        }
        
 

       
   }

}
