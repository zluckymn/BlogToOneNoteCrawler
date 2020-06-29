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
using System.Threading;
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
    /// </summary>
    public class CancellationEnterpriseList_Beijing : SimpleCrawlerBase
    {
 
        List<BsonDocument> cityUrlList = new List<BsonDocument>();
        List<string> cityNameList = new List<string>();
#pragma warning disable CS0414 // 字段“CancellationEnterpriseList_Beijing.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“CancellationEnterpriseList_Beijing.isUpdate”已被赋值，但从未使用过它的值
        int limit = 1000;
        string curUrl = "http://scjgj.beijing.gov.cn/djgg/release/getMoreKindNoticeList.do";
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public CancellationEnterpriseList_Beijing(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop):base(_Settings, _filter, _dataop)
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
            Settings.ThreadCount = 1;
            Settings.MaxReTryTimes = 10;
            Settings.noCountTimes = 5;//5次没有数据则退出
            Settings.curNoCountTimes = 5;//5次没有数据则退出
            Console.WriteLine("正在获取已存在的url数据");
            var headerDic = new Dictionary<string, string>();
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.75 Safari/537.36";
            Settings.Accept = "application/json, text/javascript, */*; q=0.01";
            Settings.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            headerDic.Add("Origin", "http://scjgj.beijing.gov.cn");
            headerDic.Add("Accept-Encoding", "gzip, deflate");
            Settings.HeadSetDic= headerDic;

            var postData = "{\"data\":[{\"vtype\":\"pagination\",\"name\":\"pagerows\",\"data\":"+ limit + "}]}";
           // var postData = "{\"data\":[{\"vtype\":\"pagination\",\"name\":\"pagerows\",\"data\":" + limit + "},{\"vtype\":\"pagination\",\"name\":\"totalrows\",\"data\":0},{\"vtype\":\"pagination\",\"name\":\"page\",\"data\":1},{\"vtype\":\"pagination\",\"name\":\"sortName\",\"data\":\"\"},{\"vtype\":\"pagination\",\"name\":\"sortFlag\",\"data\":\"\"},{\"vtype\":\"attr\",\"name\":\"noticeType\",\"data\":\"1\"},{\"vtype\":\"attr\",\"name\":\"state\",\"data\":\"processing\"}]}";

            Console.WriteLine("初始化布隆过滤器");
            //初始化布隆过滤器
            for (var index=0; index<1; index++)
            {
                 if (!filter.Contains(curUrl))
                {

                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { PostData= "postData="+HttpUtility.UrlEncode( postData) });
                    filter.Add(curUrl);// 防止执行2次
                }
            }

            base.SettingInit();
             
            

        }
       
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            JObject jsonObj = GetJsonObject(hmtl);
            var add = 0;
            var update = 0;
            var data = jsonObj["data"][0]["data"];
            var curPageStr = args.urlInfo.UniqueKey;
            var pageRows= (int)data["pagerows"];
            if (pageRows != limit)
            {
                limit = pageRows;
            }
            var totalCount = (int)data["totalrows"];//总行数
            var dataList = data["rows"];
            if (dataList != null)
            {
                foreach (var dataItem in dataList)
                {

                    var entName = GetJsonValueString(dataItem,"entName");
                    var linkMan = GetJsonValueString(dataItem, "linkMan");
                    var noticeId= GetJsonValueString(dataItem, "noticeId") ; 
                    var noticeType= GetJsonValueString(dataItem, "noticeType");
                    var publishdate = GetJsonValueString(dataItem, "publishdate");
                    var regNo = GetJsonValueString(dataItem, "regNo"); 
                    var state = GetJsonValueString(dataItem, "state"); 
                    var guid = noticeId;
                    if (!hasExistObj(guid))
                    {
                       
                        var addBosn = new BsonDocument();
                        addBosn.Add("name", entName);
                        addBosn.Add("guid", guid);
                        if (!string.IsNullOrEmpty(linkMan))
                        {
                            addBosn.Add("oper_name", linkMan);
                        }
                        if (!string.IsNullOrEmpty(publishdate))
                        {
                            addBosn.Add("publishDate", publishdate);
                        }
                        if (!string.IsNullOrEmpty(regNo))
                        {
                            addBosn.Add("regNo", regNo);
                        }
                        if (!string.IsNullOrEmpty(noticeType))
                        {
                            addBosn.Add("noticeType", noticeType);
                        }
                        if (!string.IsNullOrEmpty(state))
                        {
                            addBosn.Add("state", state);
                        }
                        DBChangeQueue.Instance.EnQueue(new StorageData()
                        {
                            Name = DataTableName,
                            Document = addBosn,
                            Type = StorageType.Insert,
                        });
                        if (DBChangeQueue.Instance.Count >= 10000)
                        {
                            StartDBChangeProcess();
                        }
                        add++;
                        Console.WriteLine($"新增_{add}：{entName}当前页码:{curPageStr}");
                    }
                    else
                    {
                        update++;
                    }
                }
               
            }
            if (OnGetNothing(add))
            {
                Console.WriteLine($"超过无获取新数据次数,即将退出");
                Thread.Sleep(5000);
                Environment.Exit(0);
            }
            Console.WriteLine($"新增{add} 更新{update}当前页码:{curPageStr} 总记录数{totalCount}");
             
            if (string.IsNullOrEmpty(curPageStr) &&totalCount >limit)//连续三次没取到数据才不进行
            {
                int.TryParse(curPageStr, out int curPage);
                var needPageCount = (totalCount) / limit + 1;
                
                for(var index=2;index<= needPageCount; index++)
                {
                    //  var postData ="{ \"data\":[{\"vtype\":\"pagination\",\"name\":\"pagerows\",\"data\":"+limit+"},{\"vtype\":\"pagination\",\"name\":\"totalrows\",\"data\":"+totalCount+"},{\"vtype\":\"pagination\",\"name\":\"page\",\"data\":"+ index + "},{\"vtype\":\"pagination\",\"name\":\"sortName\",\"data\":\"\"},{\"vtype\":\"pagination\",\"name\":\"sortFlag\",\"data\":\"\"},{\"vtype\":\"attr\",\"name\":\"noticeType\",\"data\":\"1\"},{\"vtype\":\"attr\",\"name\":\"state\",\"data\":\"processing\"}]}";
                    var postData = "{ \"data\":[{\"vtype\":\"pagination\",\"name\":\"pagerows\",\"data\":" + limit + "},{\"vtype\":\"pagination\",\"name\":\"totalrows\",\"data\":" + totalCount + "},{\"vtype\":\"pagination\",\"name\":\"page\",\"data\":" + index + "},{\"vtype\":\"pagination\",\"name\":\"sortName\",\"data\":\"\"},{\"vtype\":\"pagination\",\"name\":\"sortFlag\",\"data\":\"\"}]}";
                    var urlGuid = postData.GetHashCode().ToString();
                    if (!filter.Contains(urlGuid))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) {
                            PostData = "postData=" + HttpUtility.UrlEncode(postData),
                            UniqueKey = index.ToString()
                        });
                        filter.Add(urlGuid);// 防止执行2次
                    }
                }
                Console.WriteLine($"新增Url当前页码:{needPageCount}");
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
