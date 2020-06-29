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
using System.Threading.Tasks;
using System.Web;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// 注销吊销专题，https://www.mingluji.com/zhuxiaodiaoxiao/update-list?page=0 地方人物库，后续每天定期爬取
    /// 北京 天津 河北 山西 内蒙古 辽宁 吉林 黑龙江 上海 江苏 浙江 安徽 福建 江西 山东 河南 湖北 湖南 广东 广西 海南 重庆 四川 贵州 云南 西藏 陕西 甘肃 青海 宁夏 新疆 香港 澳门
    /// </summary>
    public class CancellationEnterpriseList : SimpleCrawlerBase
    {
 
        List<BsonDocument> cityUrlList = new List<BsonDocument>();
        List<string> cityNameList = new List<string>();
#pragma warning disable CS0414 // 字段“CancellationEnterpriseList.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“CancellationEnterpriseList.isUpdate”已被赋值，但从未使用过它的值
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public CancellationEnterpriseList(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop):base(_Settings, _filter, _dataop)
        {
            DataTableName = "QCCEnterpriseKey_Cancellation";//注销企业
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
            Console.WriteLine("正在获取已存在的url数据");
          
            cityUrlList = dataop.FindAll(DataTableNameCity).ToList();//城市url
            cityNameList = cityUrlList.Select(c => c.Text("name")).ToList();


            Console.WriteLine("初始化布隆过滤器");
            //初始化布隆过滤器
            for (var index=0; index<1; index++)
            {
                var curUrl = $"https://www.mingluji.com/zhuxiaodiaoxiao/update-list?page={index}";
                if (!filter.Contains(curUrl))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { });
                    filter.Add(curUrl);// 防止执行2次
                }
            }

            base.SettingInit();
             
            

        }
        int noCountTimes = 3;
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var indexStr = GetUrlParam(args.Url, "page");
            var root = htmlDoc.GetElementbyId("block-system-main");
            var companyNodeList = root.SelectNodes("//span[@class='views-field views-field-company']");
            var add = 0;
            var update = 0;
            if (companyNodeList != null && companyNodeList.Count() > 0)
            {
                foreach (var companyNode in companyNodeList)
                {
                    var companyName = GetInnerText(companyNode);
                    if (string.IsNullOrEmpty(companyName)) continue;
                    var guid = (DataTableName + companyName).GetHashCode().ToString();
                    if (!hasExistObj(guid))
                    {
                        var cityName = GetCityName(companyName);
                        var addBosn = new BsonDocument();
                        addBosn.Add("name", companyName);
                        addBosn.Add("guid", guid);
                        if (!string.IsNullOrEmpty(cityName))
                        {
                            addBosn.Add("cityName", cityName);
                        }
                        DBChangeQueue.Instance.EnQueue(new StorageData()
                        {
                            Name = DataTableName,
                            Document = addBosn,
                            Type = StorageType.Insert,
                        });
                        add++;
                        Console.WriteLine($"新增：{companyName}所在城市解析：{cityName} 当前页码:{indexStr}");
                    }
                    else
                    {
                        update++;
                    }
                }
               
            }

            Console.WriteLine($"新增{add} 更新{update}当前页码:{indexStr}");
         
            int.TryParse(indexStr, out int index);
            if (add > 0)
            {
                noCountTimes = 3;//重置
            }
            else {
                noCountTimes -= 1;
                Console.WriteLine($"当前未取到新增数据,index:{index}剩余次数：{noCountTimes}");
            }

            if (noCountTimes>=0)//连续三次没取到数据才不进行
            {
                if (index < 1000)
                {
                    index++;

                    var curUrl = $"https://www.mingluji.com/zhuxiaodiaoxiao/update-list?page={index}";
                    if (!filter.Contains(curUrl))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { });
                        filter.Add(curUrl);// 防止执行2次
                    }
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
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var indexStr = GetUrlParam(args.Url, "page");
            var root = htmlDoc.GetElementbyId("block-system-main");
            if (root==null)//需要编写被限定IP的处理
            {
                return true;
            }
            return false;
        }
        int maxCityNameLen = 0;
        private string GetCityName(string  companyName)
        {
            
            var provinceStr = Toolslib.Str.Sub(companyName, "", "省");
            var cityNameStr = Toolslib.Str.Sub(companyName, "", "市");
            var regionStr = Toolslib.Str.Sub(companyName, "", "县");
            var distinctStr = Toolslib.Str.Sub(companyName, "", "区");
            if (!string.IsNullOrEmpty(regionStr))
            {
                var hitRegion = cityNameList.Where(c => regionStr.Contains(FixRegion(c))).FirstOrDefault();
                if (hitRegion != null)
                {
                    return hitRegion;
                }
            }
            if (!string.IsNullOrEmpty(distinctStr))
            {
                var hitRegion = cityNameList.Where(c => distinctStr.Contains(FixRegion(c))).FirstOrDefault();
                if (hitRegion != null)
                {
                    return hitRegion;
                }
            }
            if (!string.IsNullOrEmpty(cityNameStr))
            {
                var hitCity = cityNameList.Where(c => cityNameStr.Contains(FixCity(c))).FirstOrDefault();
                if (hitCity != null)
                {
                    return hitCity;
                }
            }

            if (!string.IsNullOrEmpty(provinceStr))
            {
                var hitCity = cityNameList.Where(c => provinceStr.Contains(FixProvince(c))).FirstOrDefault();
                if (hitCity != null)
                {
                    return hitCity;
                }
            }
           
            return  GetCutCityName(companyName);
            
            
        }

        private string GetCutCityName(string str)
        {
            if (maxCityNameLen <= 0)
            {
                maxCityNameLen = cityUrlList.Max(c => c.Text("name").Length);
            }
            var endIndex = str.Length < maxCityNameLen ? str.Length - 1 : maxCityNameLen - 1;
            var subStr = str.Substring(0, endIndex);
            if (!string.IsNullOrEmpty(subStr))
            {
                var hitCity = cityNameList.Where(c => subStr.Contains(FixProvince(c))).FirstOrDefault();
                if (hitCity != null)
                {
                    return hitCity;
                }
            }
            return string.Empty;
        }

        private string FixRegion(string regionName)
        {
           return  regionName.Replace("本级", "").Replace("区", "").Replace("县", "");
        }

        private string FixCity(string cityName)
        {
            return cityName.Replace("市", "");
        }

        private string FixProvince(string provinceName)
        {
            return provinceName.Replace("省","").Replace("市", "").Replace("本级", "").Replace("区", "").Replace("县", "");
        }
   }

}
