
using Unity.Jobs;

namespace ME.ECSBurst {
    
    using Collections;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Burst;
    using static MemUtilsCuts;

    public abstract unsafe class Worlds {

        public static ref World currentWorld => ref mref<World>((void*)Worlds.currentInternalWorld.Data);

        internal static readonly Unity.Burst.SharedStatic<System.IntPtr> currentInternalWorld = Unity.Burst.SharedStatic<System.IntPtr>.GetOrCreate<Worlds, WorldsKey>();

        public class WorldsKey {}

        public static readonly Unity.Burst.SharedStatic<TimeData> time = Unity.Burst.SharedStatic<TimeData>.GetOrCreate<Worlds, TimeDataKey>();

        public class TimeDataKey {}

    }

    public static class WorldsCache {

        public static System.Collections.Generic.Dictionary<int, World.Systems.SystemExecute> advanceTickDelegates = new System.Collections.Generic.Dictionary<int, World.Systems.SystemExecute>();

    }

    public struct TimeData {

        public float deltaTime;

    }

    public unsafe partial struct World {

        /*public void Validate<T>(int entityId) where T : struct, IComponentBase {

            this.currentState->Validate<T>(entityId);

        }*/

        public void Validate<T>() where T : struct, IComponentBase {

            this.currentState->Validate<T>();

        }

        public void Set<T>(in Entity entity, T component) where T : struct, IComponentBase {

            this.currentState->Set(in entity, component);

        }

        public bool Remove<T>(in Entity entity) where T : struct, IComponentBase {

            return this.currentState->Remove<T>(in entity);

        }

        public bool Has<T>(in Entity entity) where T : struct, IComponentBase {

            return this.currentState->Has<T>(in entity);

        }
        
        public ref T Get<T>(in Entity entity) where T : struct, IComponentBase {

            return ref this.currentState->Get<T>(in entity);

        }

        public ref readonly T Read<T>(in Entity entity) where T : struct, IComponentBase {

            return ref this.currentState->Read<T>(in entity);

        }

        public ref readonly Entity AddEntity() {

            return ref this.currentState->AddEntity();

        }

        public bool RemoveEntity(in Entity entity) {

            return this.currentState->RemoveEntity(in entity);

        }

        public bool IsAlive(in Entity entity) {

            return this.currentState->IsAlive(in entity);

        }

    }

    public unsafe partial struct World {

        public struct Info {

            public Unity.Collections.FixedString32 name;

        }

        public struct Systems {

            public delegate void SystemExecute();

            public struct Job {

                [NativeDisableUnsafePtrRestriction]
                public Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobScheduleParameters job;

                public void Execute() {

                    Unity.Jobs.LowLevel.Unsafe.JobsUtility.Schedule(ref this.job).Complete();

                }

            }

            internal struct SystemJobData {

                public int id;
                [NativeDisableUnsafePtrRestriction]
                public void* system;
                [NativeDisableUnsafePtrRestriction]
                public Job* job;
                [NativeDisableUnsafePtrRestriction]
                public void* method;
                public System.Runtime.InteropServices.GCHandle methodHandle;
                [NativeDisableUnsafePtrRestriction]
                public FunctionPointer<FunctionPointerDelegate> burstMethod;
                public byte isJob;
                public byte isBurst;

            }

            internal struct SystemData {

                public int id;
                [NativeDisableUnsafePtrRestriction]
                public void* system;
                [NativeDisableUnsafePtrRestriction]
                public void* method;
                public System.Runtime.InteropServices.GCHandle methodHandle;

            }

            internal NativeArrayBurst<SystemData> allSystems;
            internal NativeArrayBurst<SystemData> disposable;
            internal NativeArrayBurst<SystemJobData> advanceTick;
            internal NativeArrayBurst<SystemData> updateInput;
            internal NativeArrayBurst<SystemData> updateVisual;

            public void Initialize() {
                
                this.allSystems = new NativeArrayBurst<SystemData>(0, Allocator.Persistent);
                this.disposable = new NativeArrayBurst<SystemData>(0, Allocator.Persistent);
                this.advanceTick = new NativeArrayBurst<SystemJobData>(0, Allocator.Persistent);
                this.updateInput = new NativeArrayBurst<SystemData>(0, Allocator.Persistent);
                this.updateVisual = new NativeArrayBurst<SystemData>(0, Allocator.Persistent);
                
            }

            internal ref SystemData Add<T>(ref T system, ref NativeArrayBurst<SystemData> arr) where T : struct, ISystem {

                var id = WorldUtilities.GetAllSystemTypeId<T>();
                
                for (int i = 0; i < arr.Length; ++i) {

                    if (arr[i].id == id) {

                        return ref arr[i];

                    } 
                    
                }
                
                var size = UnsafeUtility.SizeOf<T>();
                var addr = UnsafeUtility.AddressOf(ref system);
                var buffer = pnew(ref system);
                UnsafeUtility.MemCpy(buffer, addr, size);

                var sysData = new SystemData();
                sysData.system = buffer;
                sysData.id = id;
                
                var idx = arr.Length;
                ArrayUtils.Resize(idx, ref arr);
                arr[idx] = sysData;
                
                system = mref<T>(sysData.system);
                
                return ref arr.GetRef(idx);

            }

            private ref SystemJobData Add(void* system, ref NativeArrayBurst<SystemJobData> arr) {

                var sysData = new SystemJobData();
                sysData.system = system;
                
                var idx = arr.Length;
                ArrayUtils.Resize(idx, ref arr);
                arr[idx] = sysData;
                
                return ref arr.GetRef(idx);

            }

            private ref SystemData Add(void* system, ref NativeArrayBurst<SystemData> arr) {

                var sysData = new SystemData();
                sysData.system = system;
                
                var idx = arr.Length;
                ArrayUtils.Resize(idx, ref arr);
                arr[idx] = sysData;
                
                return ref arr.GetRef(idx);

            }

            private void RunAllMethods(ref NativeArrayBurst<SystemData> arr) {
                
                for (int i = 0, cnt = arr.Length; i < cnt; ++i) {

                    var pinAddr = arr[i].method;
                    System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer<SystemExecute>((System.IntPtr)pinAddr).Invoke();

                }

            }

            private static class ExecJob<T> where T : struct, IAdvanceTickJob {

                public static System.IntPtr CreateReflectionData() {

                    return Unity.Jobs.LowLevel.Unsafe.JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobDelegate)ExecJob<T>.ExecuteJob);

                }

                private delegate void ExecuteJobDelegate(ref T jobData, System.IntPtr additionalData, System.IntPtr bufferRangePatchData,
                                                          ref Unity.Jobs.LowLevel.Unsafe.JobRanges ranges, int jobIndex);

                private static void ExecuteJob(ref T jobData, System.IntPtr additionalData,
                                               System.IntPtr bufferRangePatchData, ref Unity.Jobs.LowLevel.Unsafe.JobRanges ranges, int jobIndex) {

                    jobData.AdvanceTick();

                }

            }

            #region Public API
            public void Dispose() {

                if (this.disposable.IsCreated == true) {

                    this.RunAllMethods(ref this.disposable);

                }
                
                if (this.allSystems.IsCreated == true) {

                    for (int i = 0, cnt = this.advanceTick.Length; i < cnt; ++i) {

                        ref var data = ref this.advanceTick.GetRef(i);
                        if (data.methodHandle.IsAllocated == true) data.methodHandle.Free();
                        free(ref data.job);

                    }

                    for (int i = 0, cnt = this.allSystems.Length; i < cnt; ++i) {

                        ref var data = ref this.allSystems.GetRef(i);
                        if (data.methodHandle.IsAllocated == true) data.methodHandle.Free();
                        free(ref data.system);

                    }

                }

                this.allSystems.Dispose();
                this.disposable.Dispose();
                this.advanceTick.Dispose();
                this.updateVisual.Dispose();
                this.updateInput.Dispose();

            }

            private void* AddSystemMethod_INTERNAL<T>(in T system, string method) where T : struct, ISystem {

                var systemRef = system;
                ref var mainData = ref this.Add(ref systemRef, ref this.allSystems);
                ref var sysData = ref this.Add(mainData.system, ref this.updateVisual);
                ref var sys = ref mref<T>(mainData.system);
                
                var methodInfo = typeof(T).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                var del = (SystemExecute)SystemExecute.CreateDelegate(typeof(SystemExecute), sys, methodInfo);

                var handle = System.Runtime.InteropServices.GCHandle.Alloc(del);
                sysData.methodHandle = handle;
                sysData.method = (void*)System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(del);
                return mainData.system;

            }

            public void* AddAdvanceTick<T>(T system) where T : struct, ISystem, IAdvanceTick {

                ref var mainData = ref this.Add(ref system, ref this.allSystems);
                ref var sysData = ref this.Add(mainData.system, ref this.advanceTick);
                sysData.id = mainData.id;
                sysData.isJob = 0;

                var burstCompile = typeof(T).GetCustomAttributes(typeof(BurstCompileAttribute), false);
                var isBurstSystem = (burstCompile.Length > 0);

                if (isBurstSystem == true) {

                    sysData.isBurst = 1;
                    
                    Burst<T>.Prewarm();
                    sysData.burstMethod = Burst<T>.cache;
                    Burst<T>.cache = default;
                    Burst<T>.cacheDelegate = default;

                } else {

                    sysData.isBurst = 0;
                    
                    system = mref<T>(sysData.system);

                    var methodInfo = typeof(T).GetMethod("AdvanceTick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var del = (SystemExecute)SystemExecute.CreateDelegate(typeof(SystemExecute), system, methodInfo);

                    var handle = System.Runtime.InteropServices.GCHandle.Alloc(del);
                    sysData.methodHandle = handle;
                    sysData.method = (void*)System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(del);

                    var ptr = (System.IntPtr)sysData.method;
                    var m = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer<SystemExecute>(ptr);
                    WorldsCache.advanceTickDelegates.Add(mainData.id, m);

                }

                return mainData.system;

            }

            public void* AddAdvanceTickJob<T>(T system) where T : struct, ISystem, IAdvanceTickJob {

                ref var mainData = ref this.Add(ref system, ref this.allSystems);
                
                var burstCompile = typeof(T).GetCustomAttributes(typeof(BurstCompileAttribute), false);
                var isBurstSystem = (burstCompile.Length > 0);

                ref var sysData = ref this.Add(mainData.system, ref this.advanceTick);
                sysData.id = mainData.id;
                if (isBurstSystem == true) {

                    var jobData = ExecJob<T>.CreateReflectionData();
                    var p = new Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobScheduleParameters(mainData.system, jobData, default, Unity.Jobs.LowLevel.Unsafe.ScheduleMode.Run);
                    var job = new Job() {
                        job = p,
                    };
                    sysData.job = tnew(ref job);
                    sysData.isJob = 1;
                    sysData.isBurst = 1;

                } else {

                    system = mref<T>(sysData.system);
                    
                    var methodInfo = typeof(T).GetMethod("AdvanceTick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var del = (SystemExecute)SystemExecute.CreateDelegate(typeof(SystemExecute), system, methodInfo);

                    var handle = System.Runtime.InteropServices.GCHandle.Alloc(del);
                    sysData.methodHandle = handle;
                    
                    sysData.method = (void*)System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(del);
                    sysData.isJob = 1;
                    sysData.isBurst = 0;
                    
                    var ptr = (System.IntPtr)sysData.method;
                    var m = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer<SystemExecute>(ptr);
                    WorldsCache.advanceTickDelegates.Add(mainData.id, m);

                }

                return mainData.system;

            }

            public void* AddDispose<T>(in T system) where T : struct, ISystem {

                return this.AddSystemMethod_INTERNAL(in system, "OnDispose");
                
            }

            public void* AddVisual<T>(in T system) where T : struct, ISystem {

                return this.AddSystemMethod_INTERNAL(in system, "UpdateVisual");

            }

            public void* AddInput<T>(in T system) where T : struct, ISystem {

                return this.AddSystemMethod_INTERNAL(in system, "UpdateInput");

            }

            public void Run(World* world, float dt) {

                if (this.updateInput.IsCreated == true) {

                    Worlds.time.Data.deltaTime = dt;
                    this.RunAllMethods(ref this.updateInput);
                    
                }
                
                //UnityEngine.Debug.Log($"World: {(System.IntPtr)world}");
                if (this.advanceTick.IsCreated == true) {

                    Worlds.time.Data.deltaTime = 0.033f;
                    for (int i = 0, cnt = this.advanceTick.Length; i < cnt; ++i) {

                        var task = this.advanceTick[i];
                        if (task.isBurst == 0) {
                            
                            if (WorldsCache.advanceTickDelegates.TryGetValue(this.advanceTick[i].id, out var del) == true) {

                                del.Invoke();

                            }

                            continue;
                            
                        }

                        if (task.isJob == 1) {

                            task.job->Execute();
    
                        } else {
                            
                            task.burstMethod.Invoke(ref task.system);

                        }

                    }

                }
                
                if (this.updateVisual.IsCreated == true) {

                    Worlds.time.Data.deltaTime = dt;
                    this.RunAllMethods(ref this.updateVisual);

                }

            }
            #endregion

        }

        public struct ManagedData {

            public System.Collections.Generic.List<Feature> features;

        }
        
        // Current world link
        [NativeDisableUnsafePtrRestriction]
        internal World* buffer;

        [NativeDisableUnsafePtrRestriction]
        public State* resetState;
        [NativeDisableUnsafePtrRestriction]
        public State* currentState;
        
        public Info info;
        [NativeDisableUnsafePtrRestriction]
        internal Systems* systems;

        [NativeDisableUnsafePtrRestriction]
        internal void* managedData;

        public World(string name, int entitiesCapacity = 100) {

            var mData = new ManagedData();
            mData.features = new System.Collections.Generic.List<Feature>();
            this.managedData = pnew(ref mData);

            this.resetState = State.Create(entitiesCapacity);
            this.currentState = State.Create(entitiesCapacity);
            
            this.info = new Info() { name = name };
            this.systems = tnew<Systems>();
            this.systems->Initialize();

            this.buffer = null;
            tnew(ref this.buffer, ref this);
            
            Worlds.currentInternalWorld.Data = (System.IntPtr)this.buffer;

        }
        
        #region Public API
        public void Dispose() {

            this.resetState->Dispose();
            this.currentState->Dispose();
            this.systems->Dispose();
            free(ref this.resetState);
            free(ref this.currentState);
            free(ref this.systems);
            free(ref this.buffer);

        }
        
        public void Update(float deltaTime) {

            Worlds.currentInternalWorld.Data = (System.IntPtr)this.buffer;
            this.systems->Run(this.buffer, deltaTime);

        }

        public abstract class Base<T> {

            public abstract void* Call(Systems* systems, ref T system);

        }

        public sealed class CallAdvanceTick<T> : Base<T> where T : struct, IAdvanceTick {

            public override void* Call(Systems* systems, ref T system) {

                var ptr = systems->AddAdvanceTick(system);
                system = mref<T>(ptr);
                return ptr;

            }

        }

        public sealed class CallAdvanceTickJob<T> : Base<T> where T : struct, IAdvanceTickJob {

            public override void* Call(Systems* systems, ref T system) {

                var ptr = systems->AddAdvanceTickJob(system);
                system = mref<T>(ptr);
                return ptr;

            }

        }

        public T AddSystem<T>(T system) where T : struct, ISystem {

            void* ptr = null;
            if (system is IOnCreate sysCreate) {

                var mainData = this.systems->Add(ref system, ref this.systems->allSystems);
                ptr = mainData.system;
                sysCreate.OnCreate();
                var sys = (T)sysCreate;
                UnsafeUtility.CopyStructureToPtr(ref sys, ptr);
                
            }
            if (system is IOnDispose) {
                ptr = this.systems->AddDispose(in system);
                system = mref<T>(ptr);
            }
            if (system is IAdvanceTick) {
                var adder = (Base<T>)System.Activator.CreateInstance(typeof(CallAdvanceTick<>).MakeGenericType(typeof(T)));
                ptr = adder.Call(this.systems, ref system);
            }
            if (system is IAdvanceTickJob) {
                var adder = (Base<T>)System.Activator.CreateInstance(typeof(CallAdvanceTickJob<>).MakeGenericType(typeof(T)));
                ptr = adder.Call(this.systems, ref system);
            }
            if (system is IUpdateVisual) {
                ptr = this.systems->AddVisual(in system);
                system = mref<T>(ptr);
            }
            if (system is IUpdateInput) {
                ptr = this.systems->AddInput(in system);
                system = mref<T>(ptr);
            }

            return ptr != null ? mref<T>(ptr) : system;

        }

        public void AddFeature<T>(T feature) where T : Feature {

            ref var mData = ref mref<ManagedData>(this.managedData);
            mData.features.Add(feature);

        }

        public T GetFeature<T>(out T feature) where T : Feature {

            feature = this.GetFeature<T>();
            return feature;

        }

        public T GetFeature<T>() where T : Feature {

            ref var mData = ref mref<ManagedData>(this.managedData);
            foreach (var feature in mData.features) {

                if (feature is T) {

                    return (T)feature;

                }
                
            }

            return null;

        }
        #endregion

    }

    public interface ISystem {

        

    }

    public interface IOnCreate : ISystem {

        void OnCreate();

    }
    
    public interface IOnDispose : ISystem {

        void OnDispose();

    }
    
    public interface IAdvanceTick : ISystem {

        void AdvanceTick();

    }

    public interface IAdvanceTickJob : ISystem {

        void AdvanceTick();

    }

    public interface IUpdateInput : ISystem {

        void UpdateInput();

    }

    public interface IUpdateVisual : ISystem {

        void UpdateVisual();

    }

}
