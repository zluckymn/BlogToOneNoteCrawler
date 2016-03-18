// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SecurityQueue.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The security queue.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleCrawler
{
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// The security queue.
    /// </summary>
    /// <typeparam name="T">
    /// Any type.
    /// </typeparam>
    public abstract class SecurityQueue<T>
        where T : class
    {
        #region Fields

        /// <summary>
        /// The inner queue.
        /// </summary>
        protected readonly Queue<T> InnerQueue = new Queue<T>();

        /// <summary>
        /// The sync object.
        /// </summary>
        protected readonly object SyncObject = new object();

        /// <summary>
        /// The auto reset event.
        /// </summary>
        private readonly AutoResetEvent autoResetEvent;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityQueue{T}"/> class.
        /// </summary>
        protected SecurityQueue()
        {
            this.autoResetEvent = new AutoResetEvent(false);
        }

        #endregion

        #region Delegates

        /// <summary>
        /// The before en queue event handler.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public delegate bool BeforeEnQueueEventHandler(T target);

        #endregion

        #region Public Events

        /// <summary>
        /// The before en queue event.
        /// </summary>
        public event BeforeEnQueueEventHandler BeforeEnQueueEvent;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the auto reset event.
        /// </summary>
        public AutoResetEvent AutoResetEvent
        {
            get
            {
                return this.autoResetEvent;
            }
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Count
        {
            get
            {
                lock (this.SyncObject)
                {
                    return this.InnerQueue.Count;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether has value.
        /// </summary>
        public bool HasValue
        {
            get
            {
                return this.Count != 0;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The de queue.
        /// </summary>
        /// <returns>
        /// The <see cref="T"/>.
        /// </returns>
        public T DeQueue()
        {
            lock (this.SyncObject)
            {
                if (this.InnerQueue.Count > 0)
                {
                    return this.InnerQueue.Dequeue();
                }

                return default(T);
            }
        }

        /// <summary>
        /// The en queue.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        public void EnQueue(T target)
        {
            lock (this.SyncObject)
            {
                if (this.BeforeEnQueueEvent != null)
                {
                    if (this.BeforeEnQueueEvent(target))
                    {
                        this.InnerQueue.Enqueue(target);
                    }
                }
                else
                {
                    this.InnerQueue.Enqueue(target);
                }

                this.AutoResetEvent.Set();
            }
        }

        #endregion
    }
}