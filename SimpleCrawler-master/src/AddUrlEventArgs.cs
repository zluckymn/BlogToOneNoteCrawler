// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AddUrlEventArgs.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The add url event args.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleCrawler
{
    using System;

    /// <summary>
    /// The add url event handler.
    /// </summary>
    /// <param name="args">
    /// The args.
    /// </param>
    /// <returns>
    /// The <see cref="bool"/>.
    /// </returns>
    public delegate bool AddUrlEventHandler(AddUrlEventArgs args);

    /// <summary>
    /// The add url event args.
    /// </summary>
    public class AddUrlEventArgs : EventArgs
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the depth.
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the url.
        /// </summary>
        public string Url { get; set; }

        #endregion
    }
}