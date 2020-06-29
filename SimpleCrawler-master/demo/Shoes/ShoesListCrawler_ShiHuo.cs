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
    ///  https://www.shihuo.cn/app_swoole_goods/list?page=1&range=%E7%AF%AE%E7%90%83%E9%9E%8B%2C%E8%B7%91%E9%9E%8B%2C%E8%AE%AD%E7%BB%83%E9%9E%8B%2C%E8%B6%B3%E7%90%83%E9%9E%8B%2C%E4%BC%91%E9%97%B2%E9%9E%8B%2C%E6%BB%91%E6%9D%BF%E9%9E%8B%2C%E6%8B%96%E9%9E%8B%2C%E7%9A%AE%E9%9E%8B%2C%E9%9D%B4%E5%AD%90%2C%E5%87%89%E9%9E%8B&search_rang=&c=&sort=hot&brand=&price_from=&price_to=&keywords=&tag_type=&size=&color=&show_type=&child_brand=Air%20Max&v=5.8.0&token=b9a8efd14c91aeee9e5a1c1a30e9a58c
    ///  
    /// </summary>
    public class ShoesListCrawler_ShiHuo : SimpleCrawlerBase
    {

        

#pragma warning disable CS0414 // 字段“ShoesListCrawler_ShiHuo.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“ShoesListCrawler_ShiHuo.isUpdate”已被赋值，但从未使用过它的值
#pragma warning disable CS0414 // 字段“ShoesListCrawler_ShiHuo.canNextPage”已被赋值，但从未使用过它的值
        bool canNextPage = false;//是否可以跳转下一页
#pragma warning restore CS0414 // 字段“ShoesListCrawler_ShiHuo.canNextPage”已被赋值，但从未使用过它的值
        const int takeCount = 20;
        

        /// <summary>
        /// 热门品牌系列
        /// </summary>
        public string DataTableHotBrands
        {
            get { return "Brands_Hot_ShiHuo"; }
        }
        /// <summary>
        ///品牌
        /// </summary>
        public string DataTableBrands
        {
            get { return "Brands_ShiHuo"; }
        }
        List<BsonDocument> allCategoryList = null;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public ShoesListCrawler_ShiHuo(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "Goods_ShiHuo";//商品
            DataTableCategoryName = "Category_ShiHuo";//分类
            uniqueKeyField = "goods_id";
        }
        public void initialUrl(BsonDocument seriesDoc, int nextIndex)
        {
            
            try
            {
                
                var seriesName = HttpUtility.UrlEncode(seriesDoc.Text("name"));
                //初始化布隆过滤器
                for (var index = nextIndex; index <= nextIndex + 1; index++)
                {
                    //类型
                    foreach (var cateObj in allCategoryList) {

                        var catName =HttpUtility.UrlEncode(cateObj.Text("name"));
                        seriesDoc.Set("catName", cateObj.Text("name"));
                        var postData = $"page={index}&range={catName}&search_rang=&c=&sort=hot&brand=&price_from=&price_to=&keywords=&tag_type=&size=&color=&show_type=&child_brand={seriesName}&v=5.8.0";
                        // var postData = "page=1&range=%E7%AF%AE%E7%90%83%E9%9E%8B%2C%E8%B7%91%E9%9E%8B%2C%E8%AE%AD%E7%BB%83%E9%9E%8B%2C%E8%B6%B3%E7%90%83%E9%9E%8B%2C%E4%BC%91%E9%97%B2%E9%9E%8B%2C%E6%BB%91%E6%9D%BF%E9%9E%8B%2C%E6%8B%96%E9%9E%8B%2C%E7%9A%AE%E9%9E%8B%2C%E9%9D%B4%E5%AD%90%2C%E5%87%89%E9%9E%8B&search_rang=&c=&sort=hot&brand=&price_from=&price_to=&keywords=&tag_type=&size=&color=&show_type=&child_brand=Air%20Max&v=5.8.0&token=b9a8efd14c91aeee9e5a1c1a30e9a58c";
                        var curUrl = $"https://www.shihuo.cn/app_swoole_goods/list";
                        var token = TokenGenerateHelper.TokenGenerater_ShiHuo(curUrl, postData);
                        postData = $"{postData}&token={token}";
                        if (!filter.Contains(token))
                        {
                            UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { UniqueKey = seriesDoc.ToJson(), PostData = postData });
                            filter.Add(curUrl);// 防止执行2次
                        }
                    }
                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

            }
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
            Settings.MaxReTryTimes = 5;
            Settings.ContentType = "application/x-www-form-urlencoded";
            Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/7.0.5(0x17000523) NetType/WIFI Language/zh_CN";
            Settings.Accept = "*/*";
            Settings.HeadSetDic = new Dictionary<string, string>();
           // Settings.HeadSetDic.Add("User-Device", "NTE3MjVhMDk4MjUyNDM5ZGZjN2QxYTg1MzA2MDUxMTF8djYuMC4y");
            //Settings.HeadSetDic.Add("aid", "android1");
            //Settings.HeadSetDic.Add("time", GetTimeStamp());
           // Settings.HeadSetDic.Add("auth", "a1ca3d173b24ebe94d53ed69309c8e06");
            Settings.HeadSetDic.Add("Accept-Encoding", "br, gzip, deflate");
             
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            var allRoomCat = FindDataForUpdate(dataTableName: DataTableHotBrands, query:Query.NE("name", ""), fields:new string[] { "guid", "name" });//获取待更新的目录列表
            allCategoryList = dataop.FindAll(DataTableCategoryName).ToList();
            // DataQueueInit(allRoomCat);//加到待爬取列表 ，一个一个爬取
            foreach (var cat in allRoomCat)
            {
                initialUrl(cat, 1);
            }
            //while (DynamicQueue<BsonDocument>.Instance.Count > 0) { 
            // DataDeQueue((cat) => { initialUrl(cat, 1); });//从列表中取出
            //}
            base.SettingInit();



        }
        
        /// <summary>
        /// 需要一直循环直到
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            JObject jsonObj = GetJsonObject(hmtl);
            var result = jsonObj["data"];
            var filters = result["filters"];
            var brands= filters["brands"];
            var curBrandName = string.Empty;
            if (brands.Count() > 0)
            {
                curBrandName = brands[0]["name"].ToString();
            }
            var catInfo = GetBsonDocument(args.urlInfo.UniqueKey);
            var pageStr = GetUrlParam(args.urlInfo.PostData, "page");
            var items = result["lists"];
            if (items != null)
            {
                if (items.Count() <= 0)
                {
                    ShowMessage($"无数据{args.urlInfo.PostData}");
                    return;
                }
                foreach (var item in items)
                {
                    var bsonDoc = GetBsonDocument(item);
                    bsonDoc.Set("guid", bsonDoc.Text(uniqueKeyField));
                    bsonDoc.Set("child_brandId", catInfo.Text("guid"));
                    bsonDoc.Set("curBrandName", curBrandName);
                    bsonDoc.Set("curSeriesName", catInfo.Text("name"));
                    bsonDoc.Set("curCatName", catInfo.Text("catName"));
                    PushData(bsonDoc);
                }
            }

            if (items.Count() < takeCount || UrlQueue.Instance.Count == 0)
            {
                ShowMessage($"已到最后一行{items.Count()}");
                //取出第二个分类，并更新
                //initialUrl(catInfo, 1)
                return;
            }

            if (pageStr != "")
            {
              
                if (int.TryParse(pageStr, out int pageIndex))
                {
                    var postData = args.urlInfo.PostData;
                    var curUrl = args.Url;
                    postData = postData.Replace($"page={pageStr}&", $"page={pageIndex + 1}&");
                    var token = TokenGenerateHelper.TokenGenerater_ShiHuo(curUrl, postData);
                    postData=ReplaceUrlParam(postData, "token", token);
                    if (!filter.Contains(token))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { UniqueKey = catInfo.ToJson(), PostData = postData });
                        filter.Add(curUrl);// 防止执行2次
                    }

                    ShowMessage($"获取{pageIndex}页数据并初始化成功");
                }
                else
                {
                    ShowMessage($"转换{pageIndex}出错1");
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
                JObject jsonObj = GetJsonObject(args.Html);
                var result = jsonObj["msg"];
              
                if (result.ToString() == "ok")//需要编写被限定IP的处理
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
                Console.WriteLine(ex.Message+ args.Url);
                return true;
            }
        }


    }

}
