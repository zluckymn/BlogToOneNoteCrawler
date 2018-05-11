// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UrlRetryQueue.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The url queue.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleCrawler
{
    /// <summary>
    /// The url queue.
    /// </summary>
    public class UrlRetryQueue : SecurityQueue<UrlInfo>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Prevents a default instance of the <see cref="UrlRetryQueue"/> class from being created.
        /// </summary>
        private UrlRetryQueue()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static UrlRetryQueue Instance
        {
            get
            {
                return Nested.Inner;
            }
        }

        #endregion

        /// <summary>
        /// The nested.
        /// </summary>
        private static class Nested
        {
            #region Static Fields

            /// <summary>
            /// The inner.
            /// </summary>
            internal static readonly UrlRetryQueue Inner = new UrlRetryQueue();

            #endregion
        }
    }
}