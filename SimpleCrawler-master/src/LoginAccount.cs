// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UrlInfo.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The url info.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace SimpleCrawler
{
    /// <summary>
    /// The url info.
    /// </summary>
    public class LoginAccount
    {
        #region Fields


        /// <summary>
        /// 用户爬取信息统计
        /// </summary>
        /// <summary>
        /// 当前初始值
        /// </summary>
        public Dictionary<string, int> initCountDic { get; set; }
        /// <summary>
        /// 操作后的增加值
        /// </summary>
        public Dictionary<string, int> addionalCountDic { get; set; }
       
       
        #endregion

        public string userName { get; set; }
        public string postData { get; set; }
        public string status { get; set; }
        public string passWord { get; set; }
       
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlInfo"/> class.
        /// </summary>
        /// <param name="urlString">
        /// The url string.
        /// </param>

        
        public LoginAccount(string _userName, string _passWord, string _postData, string _status)
        {
            userName = _userName; postData = _postData; status = _status; passWord = _passWord;
        }
        public LoginAccount(string _userName )
        {
            userName = _userName;
        }
        #endregion


    }
}