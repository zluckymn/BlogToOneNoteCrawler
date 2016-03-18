// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BloomFilter.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The bloom filter.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleCrawler
{
    using System;
    using System.Collections;

    /// <summary>
    /// The bloom filter.
    /// </summary>
    /// <typeparam name="T">
    /// The generic type.
    /// </typeparam>
    public class BloomFilter<T>
    {
        #region Fields

        /// <summary>
        /// The get hash secondary.
        /// </summary>
        private readonly HashFunction getHashSecondary;

        /// <summary>
        /// The hash bits.
        /// </summary>
        private readonly BitArray hashBits;

        /// <summary>
        /// The hash function count.
        /// </summary>
        private readonly int hashFunctionCount;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BloomFilter{T}"/> class.
        /// </summary>
        /// <param name="capacity">
        /// The capacity.
        /// </param>
        public BloomFilter(int capacity)
            : this(capacity, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BloomFilter{T}"/> class.
        /// </summary>
        /// <param name="capacity">
        /// The capacity.
        /// </param>
        /// <param name="errorRate">
        /// The error rate.
        /// </param>
        public BloomFilter(int capacity, int errorRate)
            : this(capacity, errorRate, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BloomFilter{T}"/> class.
        /// </summary>
        /// <param name="capacity">
        /// The capacity.
        /// </param>
        /// <param name="hashFunction">
        /// The hash function.
        /// </param>
        public BloomFilter(int capacity, HashFunction hashFunction)
            : this(capacity, BestErrorRate(capacity), hashFunction)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BloomFilter{T}"/> class.
        /// </summary>
        /// <param name="capacity">
        /// The capacity.
        /// </param>
        /// <param name="errorRate">
        /// The error rate.
        /// </param>
        /// <param name="hashFunction">
        /// The hash function.
        /// </param>
        public BloomFilter(int capacity, float errorRate, HashFunction hashFunction)
            : this(capacity, errorRate, hashFunction, BestM(capacity, errorRate), BestK(capacity, errorRate))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BloomFilter{T}"/> class.
        /// </summary>
        /// <param name="capacity">
        /// The capacity.
        /// </param>
        /// <param name="errorRate">
        /// The error rate.
        /// </param>
        /// <param name="hashFunction">
        /// The hash function.
        /// </param>
        /// <param name="m">
        /// The m.
        /// </param>
        /// <param name="k">
        /// The k.
        /// </param>
        public BloomFilter(int capacity, float errorRate, HashFunction hashFunction, int m, int k)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException("capacity", capacity, "capacity must be > 0");
            }

            if (errorRate >= 1 || errorRate <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "errorRate", 
                    errorRate, 
                    string.Format("errorRate must be between 0 and 1, exclusive. Was {0}", errorRate));
            }

            if (m < 1)
            {
                throw new ArgumentOutOfRangeException(
                    string.Format(
                        "The provided capacity and errorRate values would result in an array of length > int.MaxValue. Please reduce either of these values. Capacity: {0}, Error rate: {1}", 
                        capacity, 
                        errorRate));
            }

            if (hashFunction == null)
            {
                if (typeof(T) == typeof(string))
                {
                    this.getHashSecondary = HashString;
                }
                else if (typeof(T) == typeof(int))
                {
                    this.getHashSecondary = HashInt32;
                }
                else
                {
                    throw new ArgumentNullException(
                        "hashFunction", 
                        "Please provide a hash function for your type T, when T is not a string or int.");
                }
            }
            else
            {
                this.getHashSecondary = hashFunction;
            }

            this.hashFunctionCount = k;
            this.hashBits = new BitArray(m);
        }

        #endregion

        #region Delegates

        /// <summary>
        /// The hash function.
        /// </summary>
        /// <param name="input">
        /// The input.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public delegate int HashFunction(T input);

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the truthiness.
        /// </summary>
        public double Truthiness
        {
            get
            {
                return (double)this.TrueBits() / this.hashBits.Count;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The add.
        /// </summary>
        /// <param name="item">
        /// The item.
        /// </param>
        public void Add(T item)
        {
            int primaryHash = item.GetHashCode();
            int secondaryHash = this.getHashSecondary(item);

            for (int i = 0; i < this.hashFunctionCount; i++)
            {
                int hash = this.ComputeHash(primaryHash, secondaryHash, i);
                this.hashBits[hash] = true;
            }
        }

        /// <summary>
        /// The contains.
        /// </summary>
        /// <param name="item">
        /// The item.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool Contains(T item)
        {
            int primaryHash = item.GetHashCode();
            int secondaryHash = this.getHashSecondary(item);

            for (int i = 0; i < this.hashFunctionCount; i++)
            {
                int hash = this.ComputeHash(primaryHash, secondaryHash, i);
                if (this.hashBits[hash] == false)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// The best error rate.
        /// </summary>
        /// <param name="capacity">
        /// The capacity.
        /// </param>
        /// <returns>
        /// The <see cref="float"/>.
        /// </returns>
        private static float BestErrorRate(int capacity)
        {
            var c = (float)(1.0 / capacity);
            if (Math.Abs(c) > 0)
            {
                return c;
            }

            double y = int.MaxValue / (double)capacity;
            return (float)Math.Pow(0.6185, y);
        }

        /// <summary>
        /// The best k.
        /// </summary>
        /// <param name="capacity">
        /// The capacity.
        /// </param>
        /// <param name="errorRate">
        /// The error rate.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        private static int BestK(int capacity, float errorRate)
        {
            return (int)Math.Round(Math.Log(2.0) * BestM(capacity, errorRate) / capacity);
        }

        /// <summary>
        /// The best m.
        /// </summary>
        /// <param name="capacity">
        /// The capacity.
        /// </param>
        /// <param name="errorRate">
        /// The error rate.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        private static int BestM(int capacity, float errorRate)
        {
            return (int)Math.Ceiling(capacity * Math.Log(errorRate, 1.0 / Math.Pow(2, Math.Log(2.0))));
        }

        /// <summary>
        /// The hash int 32.
        /// </summary>
        /// <param name="input">
        /// The input.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        private static int HashInt32(T input)
        {
            var x = input as uint?;
            unchecked
            {
                x = ~x + (x << 15);
                x = x ^ (x >> 12);
                x = x + (x << 2);
                x = x ^ (x >> 4);
                x = x * 2057;
                x = x ^ (x >> 16);

                return (int)x;
            }
        }

        /// <summary>
        /// The hash string.
        /// </summary>
        /// <param name="input">
        /// The input.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        private static int HashString(T input)
        {
            var str = input as string;
            int hash = 0;

            if (str != null)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    hash += str[i];
                    hash += hash << 10;
                    hash ^= hash >> 6;
                }

                hash += hash << 3;
                hash ^= hash >> 11;
                hash += hash << 15;
            }

            return hash;
        }

        /// <summary>
        /// The compute hash.
        /// </summary>
        /// <param name="primaryHash">
        /// The primary hash.
        /// </param>
        /// <param name="secondaryHash">
        /// The secondary hash.
        /// </param>
        /// <param name="i">
        /// The i.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        private int ComputeHash(int primaryHash, int secondaryHash, int i)
        {
            int resultingHash = (primaryHash + (i * secondaryHash)) % this.hashBits.Count;
            return Math.Abs(resultingHash);
        }

        /// <summary>
        /// The true bits.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        private int TrueBits()
        {
            int output = 0;

            foreach (bool bit in this.hashBits)
            {
                if (bit)
                {
                    output++;
                }
            }

            return output;
        }

        #endregion
    }
}