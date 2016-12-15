// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AccountQueue.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The url queue.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using MongoDB.Bson;
using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler
{
    /// <summary>
    /// The url queue.
    /// </summary>
    public class AccountQueue : SecurityQueue<BsonDocument>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Prevents a default instance of the <see cref="AccountQueue"/> class from being created.
        /// </summary>
        private AccountQueue()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static AccountQueue Instance
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
            internal static readonly AccountQueue Inner = new AccountQueue();

            #endregion
        }
    }
}