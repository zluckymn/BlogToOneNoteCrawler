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
using System.Web.UI.WebControls;
using Helper;

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// https://esapi.org.hc360.com/interface/getinfos.html?pnum=1&psize=10&sys=tencent&kwd=%E9%98%B2%E7%81%AB%E9%97%A8&z=%E4%B8%AD%E5%9B%BD&index=basebusininfo&collapsef=providerid
    /// 
    ///  </summary>
    public class HuiCongMaterialDetailAPPCrawler_WeiXin : SimpleCrawlerBase
    {
         
        private Dictionary<string, string> columnMapDic = new Dictionary<string, string>();
      
        private Hashtable  userCrawlerCountHashTable = new Hashtable();
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
       
        private BloomFilter<string> guidFilter;
      
       
 
        /// <summary>
        ///  构造函数
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public HuiCongMaterialDetailAPPCrawler_WeiXin(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop):base(_Settings, _filter, _dataop)
        {
            DataTableName = "Material_HuiCong_WLM";//房间
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";
            guidFilter = new BloomFilter<string>(9000000);
        }
        public bool isSpecialUrlMode = false;
      

        int pageSize = 200;//24
        int pageBeginNum = 1;
        // string materialUrl = "http://z.hc360.com/getmmtlast.cgi?dt=1&w={0}&v=59&e={1}&c=供应信息&n={2}&m=2&H=1&bt=0";
        //string materialUrl = "http://z.hc360.com/getmmtlast.cgi?dt=1&e={1}&c=供应信息&a=13&n={2}&m=2&H=1&fc=0&bt=0&w={0}&v=60&t=1";
        string materialUrl = "https://wsmobile.hc360.com/busininfo?bcid={0}";
        //将z.hc360改成 s.hc360 可用

        public void initialUrl()
        {

            var hitMatList = FindDataForUpdateLimit(Query.Exists("isUpdate", false), fields: new string[] { "bcid" }, DataTableName,limit:10000);

            foreach (var matObj in hitMatList)
            {

                var curUrl = string.Format(materialUrl, matObj.Text("bcid"));

                if (!filter.ContainsAdd(curUrl))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { Depth = 1, UniqueKey = matObj.Text("bcid") });
                }

            }



        }

        override
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
            Settings.ThreadCount = 2;
            Settings.DBSaveCountLimit = 1;
            Settings.AutoSpeedLimit = true;
            Settings.AutoSpeedLimitMaxMSecond =1000;
            //Settings.CurWebProxy = GetWebProxy();
            Settings.ContentType = "application/x-www-form-urlencoded;charset=utf8;";
            this.Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/7.0.12(0x17000c2d) NetType/WIFI";

            
            Settings.Referer = "esapi.org.hc360.com";


            var headSetDic = new Dictionary<string, string>();
            headSetDic.Add("Accept-Encoding", "br, gzip, deflate");
            //headSetDic.Add("Host", "esapi.org.hc360.com");
            
            Settings.HeadSetDic = headSetDic;
            //date=&end_date=&title=&content=&key=%E5%85%AC%E5%8F%B8&database=saic&search_field=all&search_type=yes&page=2
          
            Console.WriteLine("请输入关键字以逗号隔开,输入1代表从数据库读取");



            initialUrl();



            Console.WriteLine("正在加载账号数据");

 
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
         
        override
        /// <summary>
        /// http://z.hc360.com/getmmtlast.cgi?dt=1&w=单开&v=59&e=1&c=供应信息&n=2&m=2&H=1&bt=0
        ///  </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            if (CanLoadNewData())
            {
                initialUrl();
            }
            JObject jsonObj = JObject.Parse(args.Html);
            var guid = args.urlInfo.UniqueKey;
            var bsonDoc = args.Html.GetBsonDocFromJson();
            bsonDoc.Set("guid", guid);
            bsonDoc.Set("isUpdate", 1);
            //AddData(bsonDoc);
            UpdateData(bsonDoc);
            ShowStatus();
            Console.WriteLine(HttpUtility.UrlDecode(args.Url));
            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="allRecordCount"></param>
        /// <param name="curNum"></param>
        /// <returns></returns>
        private void InitNextUrl(UrlInfo urlInfo,string keyWord,int curRecordIndex, int endPageIndex,  int pageSize)
        {
            
            while (curRecordIndex < endPageIndex)
           {
                curRecordIndex = curRecordIndex + 1;
                var curUrl = ReplaceUrlParam(urlInfo.UrlString, "pnum", curRecordIndex.ToString());
                urlInfo.UrlString = curUrl;
                if (!filter.ContainsAdd(curUrl)) {
                    UrlQueue.Instance.EnQueue(urlInfo);
                }
                
                //return;
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


                if (args.Html.Contains("busininfo"))//需要编写被限定IP的处理
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
