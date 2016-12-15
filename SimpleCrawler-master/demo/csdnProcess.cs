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
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler.Demo
{
   internal partial class Program
    {
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
            if (content != null)
            { parseHtml = content.InnerText; }

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
                SendOneNote(title, string.Format("blog.codingnow.com/{0}", startYear), "blog");
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
            
            if ("http://blog.codingnow.com/" == args.Url) return string.Empty;
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
        private static void YunFengBlogReceive(DataReceivedEventArgs args)
        {
            //云风blog
            var parseHtml = JGZFBlogProcess(args);
            if (!string.IsNullOrEmpty(parseHtml))
            {
                SendOneNote(parseHtml, args.Url, "blog");
            }
        }


        #endregion
    }
}
