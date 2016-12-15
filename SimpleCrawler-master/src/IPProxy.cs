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
    public class IPProxy
    {
        #region Fields

       
        
        #endregion

        public string IP { get; set; }
        public string Port { get; set; }
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public bool Unavaiable { get; set; }// 是否可用
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlInfo"/> class.
        /// </summary>
        /// <param name="urlString">
        /// The url string.
        /// </param>

        public IPProxy(string ip)
        {
            IP = ip;
        }
        public IPProxy(string ip,string port)
        {
            IP = ip; Port = port;
        }
        public IPProxy(string ip, string port,string userName,string passWord)
        {
            IP = ip; Port = port; UserName = userName; PassWord = passWord;
        }

        #endregion


    }
}