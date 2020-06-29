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
    public class DynamicQueue<T> : SecurityQueue<T> where T:  class
    {
        #region Constructors and Destructors

        /// <summary>
        /// Prevents a default instance of the <see cref="UrlRetryQueue"/> class from being created.
        /// </summary>
        private DynamicQueue()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static DynamicQueue<T> Instance
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
            internal static readonly DynamicQueue<T> Inner = new DynamicQueue<T>();

            #endregion
        }
    }
}