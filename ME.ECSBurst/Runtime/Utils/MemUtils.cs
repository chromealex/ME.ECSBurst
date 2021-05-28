namespace ME.ECSBurst {

    using System.Runtime.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;

    #if ECS_COMPILE_IL2CPP_OPTIONS
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.NullChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
    #endif
    public static unsafe class MemUtilsCuts {

        //[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static T mcall<T>(void* methodPtr) {
            
            return System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer<T>((System.IntPtr)methodPtr);

        }
    
        //[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static ref T mref<T>(void* ptr) where T : struct => ref UnsafeUtility.AsRef<T>(ptr);

        //[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static void free(ref void* ptr, Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent) {
            
            UnsafeUtility.Free(ptr, allocator);
            ptr = null;
            
        }

        public static void free<T>(ref T* ptr, Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent) where T : unmanaged {
            
            UnsafeUtility.Free(ptr, allocator);
            ptr = null;
            
        }

        //[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static void* pnew<T>(ref T source, Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent) where T : struct {

            return MemUtils.CreateFromStruct(ref source, allocator);

        }

        public static void* pnew<T>(Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent) where T : struct {

            return MemUtils.CreateFromStruct<T>(allocator);

        }

        public static void* pnew<T>(ref void* ptr, ref T source, Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent) where T : struct {

            return MemUtils.CreateFromStruct(ref ptr, ref source, allocator);

        }

        //[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static T* tnew<T>(ref T* ptr, ref T source, Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent) where T : unmanaged {

            return MemUtils.Create(ref ptr, ref source, allocator);

        }

        public static T* tnew<T>(ref T source, Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent) where T : unmanaged {

            return MemUtils.Create(ref source, allocator);

        }

        public static T* tnew<T>(Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent) where T : unmanaged {

            return MemUtils.Create<T>(allocator);

        }

    }

    #if ECS_COMPILE_IL2CPP_OPTIONS
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.NullChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
    #endif
    public static unsafe class MemUtils {

        //[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static T* Create<T>(ref T* ptr, ref T source, Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent) where T : unmanaged {

            ptr = (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator);
            UnsafeUtility.CopyStructureToPtr(ref source, ptr);
            return ptr;

        }

        public static T* Create<T>(ref T source, Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent) where T : unmanaged {

            var ptr = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator);
            UnsafeUtility.CopyStructureToPtr(ref source, ptr);
            return (T*)ptr;

        }

        public static T* Create<T>(Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent) where T : unmanaged {

            var size = UnsafeUtility.SizeOf<T>();
            var ptr = UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<T>(), allocator);
            UnsafeUtility.MemClear(ptr, size);
            return (T*)ptr;

        }

        //[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static void* CreateFromStruct<T>(ref T source, Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent) where T : struct {

            var ptr = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator);
            UnsafeUtility.CopyStructureToPtr(ref source, ptr);
            return ptr;

        }
        public static void* CreateFromStruct<T>(Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent) where T : struct {

            var size = UnsafeUtility.SizeOf<T>();
            var ptr = UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<T>(), allocator);
            UnsafeUtility.MemClear(ptr, size);
            return ptr;

        }

        public static void* CreateFromStruct<T>(ref void* ptr, ref T source, Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Persistent) where T : struct {

            ptr = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator);
            UnsafeUtility.CopyStructureToPtr(ref source, ptr);
            return ptr;

        }

        //[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static ref T Ref<T>(void* ptr) where T : struct => ref UnsafeUtility.AsRef<T>(ptr);

    }
    
}