// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleCrawler.Demo
{
    using Microsoft.Office.Interop.OneNote;
    using System;
    using System.Xml.Linq;
    using System.Linq;
    using System.Xml;
    using System.IO;
    using System.Text;
    using HtmlAgilityPack;
    using System.Text.RegularExpressions;
    using System.Collections.Generic;

    /// <summary>
    /// The program.
    /// </summary>
    internal class Program
    {
        #region Static Fields

        /// <summary>
        /// The settings.
        /// </summary>
        private static readonly CrawlSettings Settings = new CrawlSettings();

        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private static BloomFilter<string> filter;
        private static List<string> urlFilterKeyWord = new List<string>();
        #endregion

        #region Methods

        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Main(string[] args)
        {
             filter = new BloomFilter<string>(200000);
           
            const string CityName = "beijing";
         
         
                // 设置种子地址
                 //Settings.SeedsAddress.Add(string.Format("http://jobs.zhaopin.com/{0}", CityName));
                 // Settings.SeedsAddress.Add(string.Format("http://www.fzhouse.com.cn:7002/result_new.asp"));
          

            // 设置 URL 关键字
            //Settings.HrefKeywords.Add(string.Format("/{0}/bj", CityName));
            //Settings.HrefKeywords.Add(string.Format("/{0}/sj", CityName));

           

            //Settings.HrefKeywords.Add(string.Format("building.asp?ProjectID="));
            //Settings.HrefKeywords.Add(string.Format("result_new"));
            // 设置爬取线程个数
            Settings.ThreadCount = 5;
            // Settings.ThreadCount = 1;
            // 设置爬取深度
            Settings.Depth = 27;

            // 设置爬取时忽略的 Link，通过后缀名的方式，可以添加多个
            Settings.EscapeLinks.Add(".jpg");

            // 设置自动限速，1~5 秒随机间隔的自动限速
            Settings.AutoSpeedLimit = false;

            // 设置都是锁定域名,去除二级域名后，判断域名是否相等，相等则认为是同一个站点
            // 例如：mail.pzcast.com 和 www.pzcast.com
            Settings.LockHost = false;

            // 设置请求的 User-Agent HTTP 标头的值
            // settings.UserAgent 已提供默认值，如有特殊需求则自行设置

            // 设置请求页面的超时时间，默认值 15000 毫秒
            // settings.Timeout 按照自己的要求确定超时时间

            // 设置用于过滤的正则表达式
            // settings.RegularFilterExpressions.Add("");

            //云风Bloginit初始化
            //YunFengBlogInit();
            JGZFBlogInit();
            var master = new CrawlMaster(Settings);
            master.AddUrlEvent += MasterAddUrlEvent;
            master.DataReceivedEvent += MasterDataReceivedEvent;
            master.Crawl();
            //Console.WriteLine("遍历结束");
            Console.ReadKey();
        }

        /// <summary>
        /// The master add url event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private static bool MasterAddUrlEvent(AddUrlEventArgs args)
        {
            if (urlFilterKeyWord.Any(c => args.Url.Contains(c))) return false;//url过滤
            if (!filter.Contains(args.Url))
            {
                filter.Add(args.Url);
                Console.WriteLine(args.Url);
                return true;
            }

            return false; // 返回 false 代表：不添加到队列中
        }

        /// <summary>
        /// The master data received event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void MasterDataReceivedEvent(DataReceivedEventArgs args)
        {


            // 在此处解析页面，可以用类似于 HtmlAgilityPack（页面解析组件）的东东、也可以用正则表达式、还可以自己进行字符串分析

            //云风blog
            var parseHtml = JGZFBlogProcess(args);
            if(!string.IsNullOrEmpty(parseHtml))
            {
                SendOneNote(parseHtml, args.Url, "blog");
            }
           
        }


        #region 结构之法
        /// <summary>
        /// blog初始化设置
        /// </summary>
        private static void JGZFBlogInit()
        {
            Settings.SeedsAddress.Add(string.Format("http://blog.csdn.net/v_JULY_v/article/list/7"));
            
            var title = string.Format("结构之法blog");
            SendOneNote(title, "http://blog.csdn.net/v_JULY_v/article/list/7", "blog");
            Settings.HrefKeywords.Add(string.Format("/v_july_v/article/list"));
            Settings.HrefKeywords.Add(string.Format("/v_JULY_v/article/list"));
            Settings.HrefKeywords.Add(string.Format("v_july_v/article/details"));
            urlFilterKeyWord.Add("#comments");//过滤url关键字
            urlFilterKeyWord.Add("#trackback");
        }

        /// <summary>
        /// 云风blog爬取
        /// </summary>
        /// <param name="args"></param>
        private static string JGZFBlogProcess(DataReceivedEventArgs args)
        {
            if (!args.Url.Contains("details")) return string.Empty;
            // 在此处解析页面，可以用类似于 HtmlAgilityPack（页面解析组件）的东东、也可以用正则表达式、还可以自己进行字符串分析
         
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);

            //var parseHtml = htmlDoc.DocumentNode.InnerText;
            var parseHtml = string.Empty;
            var content = htmlDoc.GetElementbyId("article_content");
             if(content!=null)
            { parseHtml= content.InnerText; }

            parseHtml = parseHtml.Replace("<", "").Replace(">", "");
            //var url = args.Url;
            return parseHtml;

        }
        #endregion

        #region 云风Blog爬取初始化
        /// <summary>
        /// blog初始化设置
        /// </summary>
        private static void YunFengBlogInit()
        {
            Settings.SeedsAddress.Add(string.Format("http://blog.codingnow.com/"));
            for (var startYear = 2016; startYear <= 2016; startYear++)
            {
                var title = string.Format("云风blog{0}", startYear);
                SendOneNote(title, string.Format("blog.codingnow.com/{0}", startYear),"blog");
                Settings.HrefKeywords.Add(string.Format("blog.codingnow.com/{0}", startYear));
            }
            urlFilterKeyWord.Add("#comments");//过滤url关键字
            urlFilterKeyWord.Add("#trackback");
        }

        /// <summary>
        /// 云风blog爬取
        /// </summary>
        /// <param name="args"></param>
        private static string YunFengBlogProcess(DataReceivedEventArgs args)
        {
            if ("http://blog.codingnow.com/" == args.Url) return string.Empty ;
            // 在此处解析页面，可以用类似于 HtmlAgilityPack（页面解析组件）的东东、也可以用正则表达式、还可以自己进行字符串分析
            // if ("http://blog.codingnow.com/2005/12/aiooossaeeeoeoee.html#comments" != args.Url) return;
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);

            var parseHtml = htmlDoc.DocumentNode.InnerText;
            parseHtml = Regex.Replace(parseHtml, @"<!--[\s\S]*?-->", "");
            parseHtml = Regex.Replace(parseHtml, @"</form>[\s\S]*?</form>", "");
            parseHtml = parseHtml.Replace("</form>", "").Replace("云风的 BLOG:", "");
            
            //var url = args.Url;
            return parseHtml;

        }
        #endregion
        /// <summary>
        /// 发送至我的oneNote
        /// </summary>
        /// <param name="html"></param>
        private static void SendOneNote(string html,string url,string pageName)
        {
               
                var ls = new Application();
                string ls_return = "";
                var pageId = string.Empty;

                string notebookXml;
                ls.GetHierarchy(null, HierarchyScope.hsPages, out notebookXml);
                string existingPageId = string.Empty;
                var doc = XDocument.Parse(notebookXml);
                var ns = doc.Root.Name.Namespace;
                var session = doc.Descendants(ns + "Section").Where(n => n.Attribute("name").Value == pageName).FirstOrDefault();
                if (session != null)
                {
                    existingPageId = session.Attribute("ID").Value;
                    ls.CreateNewPage(existingPageId, out pageId, NewPageStyle.npsDefault);
                    var page = new XDocument(new XElement(ns + "Page",
                                              new XElement(ns + "Outline",
                                                new XElement(ns + "OEChildren",
                                                  new XElement(ns + "OE",
                                                    new XElement(ns + "T",
                                                      new XCData(html + url)))))));
                    page.Root.SetAttributeValue("ID", pageId);
                    try
                    {
                        ls.UpdatePageContent(page.ToString(), DateTime.MinValue);
                         

                    }
                    catch (Exception ex)
                    {
                       ls.DeleteHierarchy(pageId, DateTime.MinValue);
                        Console.WriteLine(url+ex.Message);
                    }
                }
            

        }
        #endregion
    }
}