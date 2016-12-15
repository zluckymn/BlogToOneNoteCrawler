// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DBChangeQueue.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The url queue.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler
{
    /// <summary>
    /// The url queue.
    /// </summary>
    public class DBChangeQueue : SecurityQueue<StorageData>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Prevents a default instance of the <see cref="DBChangeQueue"/> class from being created.
        /// </summary>
        private DBChangeQueue()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static DBChangeQueue Instance
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
            internal static readonly DBChangeQueue Inner = new DBChangeQueue();

            #endregion
        }
    }
}