// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UrlInfo.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The url info.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using OpenQA.Selenium;
using System;

namespace SimpleCrawler
{
    /// <summary>
    /// PhantomJs与selenium结合通信动作
    /// var operation = new Operation
    ////            {
    ////                Action = (x) => {
    ////                    //通过Selenium驱动点击页面的“酒店评论”
    ////                    x.FindElement(By.XPath("//*[@id='refresh_hot']")).Click();
    ////},
    ////                Condition = (x) => {
    ////                    //判断Ajax评论内容是否已经加载成功
    ////                    return x.FindElement(By.XPath("//*[@id='hot_data']")).Displayed ;
    ////                },
    ////                Timeout = 15000
    ////            };
    /// </summary>
    public class SeleniumOperation
    {
        public int Timeout { get; set; }

        public Action<IWebDriver> Action { get; set; }

        public Func<IWebDriver, bool> Condition { get; set; }

    }
}