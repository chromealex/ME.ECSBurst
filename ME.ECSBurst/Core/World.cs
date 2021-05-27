
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

            internal delegate void SystemExecute();

            public struct Job {

                public FunctionPointer<FunctionPointerDelegate> func;
                [NativeDisableUnsafePtrRestriction]
                public void* system;

                public void Execute() {
                    
                    this.func.Invoke(ref this.system);
                    
                }

            }

            internal struct SystemJobData {

                [NativeDisableUnsafePtrRestriction]
                public void* system;
                [NativeDisableUnsafePtrRestriction]
                public Job* job;

            }

            internal struct SystemData {

                [NativeDisableUnsafePtrRestriction]
                public void* system;
                [NativeDisableUnsafePtrRestriction]
                public void* method;

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

            private ref SystemData Add<T>(T system, ref NativeArrayBurst<SystemData> arr) where T : struct {

                var size = UnsafeUtility.SizeOf<T>();
                var addr = UnsafeUtility.AddressOf(ref system);
                var buffer = pnew(ref system);
                UnsafeUtility.MemCpy(buffer, addr, size);

                var sysData = new SystemData();
                sysData.system = buffer;
                
                var idx = arr.Length;
                ArrayUtils.Resize(idx, ref arr);
                arr[idx] = sysData;
                
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

                    System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer<SystemExecute>((System.IntPtr)arr[i].method).Invoke();

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
                        free(ref data.job);

                    }

                    for (int i = 0, cnt = this.allSystems.Length; i < cnt; ++i) {

                        free(ref this.allSystems.GetRef(i).system);

                    }

                }

                this.allSystems.Dispose();
                this.disposable.Dispose();
                this.advanceTick.Dispose();
                this.updateVisual.Dispose();
                this.updateInput.Dispose();

            }
            
            public void* AddAdvanceTick<T>(T system) where T : struct, ISystem, IAdvanceTick {

                ref var mainData = ref this.Add(system, ref this.allSystems);
                
                ref var sysData = ref this.Add(mainData.system, ref this.advanceTick);
                var job = new Job() {
                    func = Burst<T>.cache,
                    system = sysData.system,
                };
                sysData.job = tnew(ref job);
                return mainData.system;

            }

            public void* AddVisual<T>(T system) where T : struct, ISystem, IUpdateVisual {

                ref var mainData = ref this.Add(system, ref this.allSystems);
                
                ref var sysData = ref this.Add(mainData.system, ref this.updateVisual);
                sysData.method = (void*)System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate((SystemExecute)system.UpdateVisual);
                return mainData.system;

            }

            public void* AddInput<T>(T system) where T : struct, ISystem, IUpdateInput {

                ref var mainData = ref this.Add(system, ref this.allSystems);
                
                ref var sysData = ref this.Add(mainData.system, ref this.updateVisual);
                sysData.method = (void*)System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate((SystemExecute)system.UpdateInput);
                return mainData.system;
                
            }

            public void Run(void* world, float dt) {

                if (this.updateInput.IsCreated == true) {

                    Worlds.time.Data.deltaTime = dt;
                    this.RunAllMethods(ref this.updateInput);
                    
                }
                
                if (this.advanceTick.IsCreated == true) {

                    Worlds.time.Data.deltaTime = 0.033f;
                    for (int i = 0, cnt = this.advanceTick.Length; i < cnt; ++i) {

                        var data = this.advanceTick[i];
                        data.job->Execute();

                    }

                }
                
                if (this.updateVisual.IsCreated == true) {

                    Worlds.time.Data.deltaTime = dt;
                    this.RunAllMethods(ref this.updateVisual);

                }

            }
            #endregion

        }

        // Current world link
        [NativeDisableUnsafePtrRestriction]
        internal World* buffer;

        [NativeDisableUnsafePtrRestriction]
        public State* resetState;
        [NativeDisableUnsafePtrRestriction]
        public State* currentState;
        
        public Info info;
        internal Systems* systems;
        
        public World(string name, int entitiesCapacity = 100) {

            this.resetState = State.Create(entitiesCapacity);
            this.currentState = State.Create(entitiesCapacity);
            
            this.info = new Info() { name = name };
            this.systems = tnew<Systems>();
            this.systems->Initialize();

            this.buffer = null;
            tnew(ref this.buffer, ref this);
            
            Worlds.currentInternalWorld.Data = (System.IntPtr)this.buffer;

        }
        
        private void Create<T>(T source, void* sysPtr) where T : struct, ISystem {
            
            if (source is IOnCreate onCreate) {

                onCreate.OnCreate();
                var s = (T)onCreate;
                UnsafeUtility.CopyStructureToPtr(ref s, sysPtr);

            }

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

        public void AddSystem<T>(T system) where T : struct, ISystem, IOnCreate {

            system.OnCreate();
            
        }

        public void AddSystemAdvanceTick<T>(T system) where T : struct, ISystem, IAdvanceTick {

            Burst<T>.Prewarm();

            var sysPtr = this.systems->AddAdvanceTick(system);
            this.Create(system, sysPtr);
            
        }
        
        public void AddSystemVisual<T>(T system) where T : struct, ISystem, IUpdateVisual {

            var sysPtr = this.systems->AddVisual(system);
            this.Create(system, sysPtr);

        }
        
        public void AddSystemInput<T>(T system) where T : struct, ISystem, IUpdateInput {

            var sysPtr = this.systems->AddInput(system);
            this.Create(system, sysPtr);

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

    public interface IUpdateInput : ISystem {

        void UpdateInput();

    }

    public interface IUpdateVisual : ISystem {

        void UpdateVisual();

    }

}
