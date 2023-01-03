using System.Collections;
using Unity.Entities.LowLevel.Unsafe;
using UnityEngine;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.Serialization;
using Unity.Mathematics;

namespace Unity.Entities.Exposed
{
    public static class BlobAssetReferenceExtensions
    {
        //due to we can not get blobasset header directly(it is a internal class), so add some hard code here
        #region hard code
        //hard core
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        internal unsafe struct GBlobAssetHeader
        {
            [FieldOffset(0)] public void* ValidationPtr;
            [FieldOffset(8)] public int Length;
            [FieldOffset(12)] public Allocator Allocator;
            [FieldOffset(16)] public ulong Hash;
            [FieldOffset(24)] private ulong Padding;

            internal static GBlobAssetHeader CreateForSerialize(int length, ulong hash)
            {
                return new GBlobAssetHeader
                {
                    ValidationPtr = null,
                    Length = length,
                    Allocator = Allocator.None,
                    Hash = hash,
                    Padding = 0
                };
            }

            public void Invalidate()
            {
                ValidationPtr = (void*)0xdddddddddddddddd;
            }
        }
        #endregion

        //todo Randall [priority 5] fix it
        public static unsafe int GetLength<T>(this in BlobAssetReference<T> blobReference) where T : unmanaged
        {
            /*
             *  // source code in blobs.cs. plz make it the same as unity entities
             *  [NativeDisableUnsafePtrRestriction]
             *  [FieldOffset(0)]
             *  public byte* m_Ptr;
             *
             *  /// <summary>
             *  /// This field overlaps m_Ptr similar to a C union.
             *  /// It is an internal (so we can initialize the struct) field which
             *  /// is here to force the alignment of BlobAssetReferenceData to be 8-bytes.
             *  /// </summary>
             *  [FieldOffset(0)]
             *  internal long m_Align8Union;
             *
             *  internal BlobAssetHeader* Header
             *  {
             *      get { return ((BlobAssetHeader*) m_Ptr) - 1; }
             *  }
             */

            void* ptr = blobReference.GetUnsafePtr();
            if (ptr == null)
                return 0;

            byte* realPtr = (byte*)ptr;
            GBlobAssetHeader* header = ((GBlobAssetHeader*)realPtr) - 1;
            return header->Length;
        }
    }
}

