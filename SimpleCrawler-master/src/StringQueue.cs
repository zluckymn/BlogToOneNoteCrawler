// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UrlQueue.cs" company="pzcast">
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
    public class StringQueue : SecurityQueue<string>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Prevents a default instance of the <see cref="UrlQueue"/> class from being created.
        /// </summary>
        private StringQueue()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static StringQueue Instance
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
            internal static readonly StringQueue Inner = new StringQueue();

            #endregion
        }
    }
}