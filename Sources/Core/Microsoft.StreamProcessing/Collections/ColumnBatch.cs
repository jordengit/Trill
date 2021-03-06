﻿// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.StreamProcessing.Internal.Collections;

namespace Microsoft.StreamProcessing.Internal
{
    /// <summary>
    /// Currently for internal use only - do not use directly.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ColumnBatch<T>
    {
        /// <summary>
        /// Used to make sure this class is thread-safe when it makes decisions
        /// about the reference count. (See <see cref="MakeWritable"/>.
        /// </summary>
        private readonly object columnBatchLock = new object();

        /// <summary>
        /// Currently for internal use only - do not use directly.
        /// </summary>
        [DataMember]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public T[] col;

        /// <summary>
        /// Currently for internal use only - do not use directly.
        /// </summary>
        [DataMember]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int UsedLength;

        /// <summary>
        /// Currently for internal use only - do not use directly.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ColumnPool<T> pool;
        internal int RefCount;

        /// <summary>
        /// Currently for internal use only - do not use directly.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ColumnBatch() => this.RefCount = 1;

        /// <summary>
        /// Currently for internal use only - do not use directly.
        /// </summary>
        /// <param name="size"></param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ColumnBatch(int size)
        {
            this.pool = null;
            this.col = new T[size];
            this.UsedLength = 0;
            this.RefCount = 1;
        }

        /// <summary>
        /// Currently for internal use only - do not use directly.
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="size"></param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ColumnBatch(ColumnPool<T> pool, int size)
        {
            this.pool = pool;
            this.col = new T[size];
            this.RefCount = 1;
        }

        /// <summary>
        /// Currently for internal use only - do not use directly.
        /// </summary>
        /// <param name="cnt"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void IncrementRefCount(int cnt)
        {
            lock (this.columnBatchLock)
            {
                Interlocked.Add(ref this.RefCount, cnt);
            }
        }

        /// <summary>
        /// Currently for internal use only - do not use directly.
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ColumnBatch<T> MakeWritable(ColumnPool<T> pool)
        {
            lock (this.columnBatchLock)
            {
                if (this.RefCount == 1)
                {
                    return this;
                }
                else
                {
                    pool.Get(out ColumnBatch<T> result);
                    System.Array.Copy(this.col, result.col, this.col.Length);
                    result.UsedLength = this.UsedLength;
                    Interlocked.Decrement(ref this.RefCount);
                    return result;
                }
            }
        }

        /// <summary>
        /// Currently for internal use only - do not use directly.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Return()
        {
            lock (this.columnBatchLock)
            {
                int localRefCount = Interlocked.Decrement(ref this.RefCount);

                if (localRefCount == 0)
                {
                    if (Config.ClearColumnsOnReturn)
                        System.Array.Clear(this.col, 0, this.col.Length);
                    if ((this.pool != null) && (!Config.DisableMemoryPooling))
                    {
                        this.UsedLength = 0;
                        this.pool.Return(this);
                    }
                }
            }
        }

        /// <summary>
        /// Currently for internal use only - do not use directly.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ReturnClear()
        {
            lock (this.columnBatchLock)
            {
                int localRefCount = Interlocked.Decrement(ref this.RefCount);

                if (localRefCount == 0)
                {
                    System.Array.Clear(this.col, 0, this.col.Length);
                    if ((this.pool != null) && (!Config.DisableMemoryPooling))
                    {
                        this.UsedLength = 0;
                        this.pool.Return(this);
                    }
                }
            }
        }

    }
}