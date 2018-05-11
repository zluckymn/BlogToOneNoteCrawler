// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UrlInfo.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The url info.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleCrawler
{
    /// <summary>
    /// The url info.
    /// </summary>
    public class UrlInfo
    {
        #region Fields

        /// <summary>
        /// The url.
        /// </summary>
        private   string url;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlInfo"/> class.
        /// </summary>
        /// <param name="urlString">
        /// The url string.
        /// </param>
        public UrlInfo(string urlString)
        {
            this.url = urlString;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the depth.
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Gets the url string.
        /// </summary>
        public string UrlString
        {
            get
            {
                return this.url;
            }
            set {
                this.url = value;
            }
        }

        /// <summary>
        /// Gets the url string.
        /// </summary>
        public string PostData { get; set; }


        /// <summary>
        /// Gets the url string.
        /// </summary>
        public string Authorization { get; set; }

        /// <summary>
        /// Gets the url string.
        /// </summary>
        public string UniqueKey { get; set; }
        /// <summary>
        ///通过urlsplit次数 通过限制个数节约key的用量
        /// </summary>
        public int UrlSplitTimes { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public CrawlStatus Status { get; set; }


        /// <summary>
        /// Gets or sets the script..配合PhantomJs使用
        /// </summary>
        public SeleniumScript script { get; set; }

        /// <summary>
        /// Gets or sets the operation.配合PhantomJs使用
        /// </summary>
        public SeleniumOperation operation { get; set; }
        #endregion
    }
}