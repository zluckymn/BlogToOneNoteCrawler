// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CrawlErrorEventArgs.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The crawl error event args.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleCrawler
{
    using System;

    /// <summary>
    /// The crawl error event handler.
    /// </summary>
    /// <param name="args">
    /// The args.
    /// </param>
    public delegate void CrawlErrorEventHandler(CrawlErrorEventArgs args);

    /// <summary>
    /// The crawl error event args.
    /// </summary>
    public class CrawlErrorEventArgs : EventArgs
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Gets or sets the url.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the Depth.
        /// </summary>
        public int Depth { get; set; }
        /// <summary>
        /// Gets or sets the IpProx.
        /// </summary>
        public IPProxy IpProx { get; set; }
        /// <summary>
        /// Gets or sets the IpProx.
        /// </summary>
        public bool needTryAgain { get; set; }


        /// <summary>
        /// Gets or sets the needChangeIp
        /// </summary>
        public bool needChangeIp { get; set; }

        public UrlInfo urlInfo { get; set; }
        #endregion
    }
}