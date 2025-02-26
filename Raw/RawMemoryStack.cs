//using System;
//using System.Runtime.CompilerServices;
//using Unity.Collections;
//using Unity.Collections.LowLevel.Unsafe;

//namespace Ces.Collections
//{
//    public unsafe struct RawMemoryStack : IDisposable
//    {
//        [NativeDisableUnsafePtrRestriction]
//        byte* _start;

//        readonly Allocator _allocator;
//        readonly int _capacity;
//        int _current;

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public readonly bool IsCreated() => _start != null;

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public RawMemoryStack(Allocator allocator, int capacity)
//        {
//            if (capacity <= 0)
//                throw new Exception($"RawMemoryStack :: Capacity ({capacity}) must be higher than 0!");

//            _allocator = allocator;
//            _capacity = capacity;

//            _start = CesMemoryUtility.Allocate<byte>(capacity, allocator);
//            _current = 0;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public void Dispose()
//        {
//            if (!IsCreated())
//                throw new Exception("RawMemoryStack :: Dispose :: Is not created!");

//            CesMemoryUtility.FreeAndNullify(ref _start, _allocator);
//            _current = 0;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public RawSpan<T> Allocate<T>(int capacity) where T : unmanaged
//        {
//            int sizeOfT = UnsafeUtility.SizeOf<T>();
//            int sizeT = CesMemoryUtility.GetSafeSize(sizeOfT, capacity);

//#if CES_COLLECTIONS_CHECK
//            if (_current + sizeT > _capacity)
//                throw new Exception("RawMemoryStack :: Allocate :: Out of memory!");
//#endif

//            var span = new RawSpan<T>((T*)(_start + _current), capacity);

//            _current += sizeT;

//            return span;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public void Reset()
//        {
//            _current = 0;
//        }
//    }
//}