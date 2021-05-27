
namespace ME.ECSBurst {
    
    using Collections;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;
    using static MemUtilsCuts;

    public interface IComponentBase {}
    
    [StructLayout(LayoutKind.Sequential)]
    public struct StructComponentsItem<T> where T : struct {

        private NativeBufferArray<bool> dataExists;
        private NativeBufferArray<T> data;

        public bool Has(int entityId) {

            return this.dataExists[entityId];

        }

        public ref T Get(int entityId) {

            this.dataExists[entityId] = true;
            return ref this.data[entityId];

        }

        public void Set(int entityId, T data) {
            
            this.data[entityId] = data;
            this.dataExists[entityId] = true;

        }

        public bool Remove(int entityId) {
            
            ref var state = ref this.dataExists[entityId];
            var prevState = state;
            this.data[entityId] = default;
            state = false;
            return prevState;

        }

        public void Validate(int entityId) {

            ArrayUtils.Resize(entityId, ref this.data);
            ArrayUtils.Resize(entityId, ref this.dataExists);
            
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructComponentsItemUnknown {

        public NativeBufferArray<bool> dataExists;
        public NativeBufferArray<byte> data;
        
        public void Dispose() {

            if (this.data.isCreated == true) this.data.Dispose();
            if (this.dataExists.isCreated == true) this.dataExists.Dispose();

        }

        public void Validate(int entityId) {

            ArrayUtils.Resize(entityId, ref this.dataExists);
            ArrayUtils.Resize(entityId, ref this.data);
            
        }

    }

    public unsafe struct StructComponents {

        public NativeBufferArray<System.IntPtr> list;

        public void Initialize() {
            
        }

        public void Dispose() {

            for (int i = 0; i < this.list.Length; ++i) {
                
                ref var item = ref mref<StructComponentsItemUnknown>((void*)this.list[i]);
                item.Dispose();

            }
            if (this.list.isCreated == true) this.list.Dispose();

        }

        public void RemoveAll(int entityId) {

            for (int i = 0; i < this.list.Length; ++i) {
                
                ref var item = ref mref<StructComponentsItemUnknown>((void*)this.list[i]);
                item.dataExists[entityId] = false;

            }
            
        }

        public void Validate(int entityId) {
            
            ArrayUtils.Resize(AllComponentTypesCounter.counter.Data, ref this.list);
            
            for (int i = 0; i < this.list.Length; ++i) {
                
                if (this.list[i] == System.IntPtr.Zero) {
    
                    UnityEngine.Debug.LogError(string.Format("Validation failed on index {0}. Do you forget to call Validate<T>?", i));
                    
                }
                
                UnityEngine.Debug.Log("Validate " + entityId + " :: " + i);
                ref var item = ref mref<StructComponentsItemUnknown>((void*)this.list[i]);
                item.Validate(entityId);
                //UnsafeUtility.CopyStructureToPtr(ref item, (void*)this.list[i]);

            }

        }
        
        public void Validate<T>(int entityId) where T : struct {

            var id = WorldUtilities.GetAllComponentTypeId<T>();
            ArrayUtils.Resize(id, ref this.list);

            for (int i = 0; i < this.list.Length; ++i) {

                ref var item = ref mref<StructComponentsItem<T>>((void*)this.list[i]);
                item.Validate(entityId);

            }

        }

        public void Validate<T>() where T : struct {

            var id = WorldUtilities.GetAllComponentTypeId<T>();
            ArrayUtils.Resize(id, ref this.list);

            if (this.list[id] == System.IntPtr.Zero) {

                this.list[id] = (System.IntPtr)pnew<StructComponentsItem<T>>();
                
            }
            
        }

        public bool Remove<T>(int entityId) where T : struct {

            var id = WorldUtilities.GetAllComponentTypeId<T>();
            this.Validate<T>(entityId);
            var ptr = this.list[id];
            ref var item = ref mref<StructComponentsItem<T>>((void*)ptr);
            return item.Remove(entityId);

        }

        public bool Has<T>(int entityId) where T : struct {

            var id = WorldUtilities.GetAllComponentTypeId<T>();
            this.Validate<T>(entityId);
            var ptr = this.list[id];
            ref var item = ref mref<StructComponentsItem<T>>((void*)ptr);
            return item.Has(entityId);

        }

        public void Set<T>(int entityId, T data) where T : struct {

            var id = WorldUtilities.GetAllComponentTypeId<T>();
            this.Validate<T>(entityId);
            var ptr = this.list[id];
            ref var item = ref mref<StructComponentsItem<T>>((void*)ptr);
            item.Set(entityId, data);

        }

        public ref T Get<T>(int entityId) where T : struct {
            
            var id = WorldUtilities.GetAllComponentTypeId<T>();
            this.Validate<T>(entityId);
            var ptr = this.list[id];
            ref var item = ref mref<StructComponentsItem<T>>((void*)ptr);
            return ref item.Get(entityId);

        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct State {

        [NativeDisableUnsafePtrRestriction]
        internal StructComponents* components;
        [NativeDisableUnsafePtrRestriction]
        internal Storage* storage;
        [NativeDisableUnsafePtrRestriction]
        internal Filters* filters;

        public static State* Create(int entitiesCapacity) {

            var state = new State();
            state.Initialize(entitiesCapacity);
            return tnew(ref state);

        }

        public void Initialize(int capacity) {

            this.components = tnew<StructComponents>();
            this.storage = tnew<Storage>();
            this.filters = tnew<Filters>();
            
            this.components->Initialize();
            this.storage->Initialize(capacity);
            this.filters->Initialize();

        }

        public void Dispose() {

            this.components->Dispose();
            this.storage->Dispose();
            this.filters->Dispose();
            
            free(ref this.components);
            free(ref this.storage);
            free(ref this.filters);

        }
        
        /*public void Validate<T>(int entityId) where T : struct, IComponentBase {

            this.components->Validate<T>(entityId);

        }*/

        public void Validate<T>() where T : struct, IComponentBase {

            this.components->Validate<T>();

        }

        public Filter AddFilter(ref Filter filter) {
            
            return this.filters->Add(ref filter);

        }

        [Unity.Collections.NotBurstCompatibleAttribute]
        public ref readonly Entity AddEntity() {

            var willNew = this.storage->WillNew();
            ref var entity = ref this.storage->Alloc();
            if (willNew == true) {

                this.storage->archetypes.Validate(in entity);
                //this.components->Validate<Name>(entity.id);
                //this.components->Validate(entity.id);
                this.filters->OnAfterEntityCreate(in entity);

            }
            
            return ref entity;

        }

        [Unity.Collections.NotBurstCompatibleAttribute]
        public bool RemoveEntity(in Entity entity) {

            if (this.storage->Dealloc(in entity) == true) {

                this.components->RemoveAll(entity.id);
                this.filters->OnBeforeEntityDestroy(in entity);
                this.storage->IncrementGeneration(in entity);
                return true;

            }
            
            return false;

        }

        public bool IsAlive(in Entity entity) {

            return this.storage->IsAlive(entity.id, entity.generation);

        }

        public void Set<T>(in Entity entity, T data) where T : struct, IComponentBase {
            
            this.components->Set(entity.id, data);
            this.filters->OnBeforeAddComponent<T>(in entity);
            this.storage->archetypes.Set<T>(in entity);
            this.storage->versions.Increment(entity.id);
            
        }

        public bool Has<T>(in Entity entity) where T : struct, IComponentBase {
            
            return this.components->Has<T>(entity.id);
            
        }

        public bool Remove<T>(in Entity entity) where T : struct, IComponentBase {

            if (this.components->Remove<T>(entity.id) == true) {
                
                this.filters->OnBeforeRemoveComponent<T>(in entity);
                this.storage->archetypes.Remove<T>(in entity);
                this.storage->versions.Increment(entity.id);
                
                return true;

            }
            
            return false;

        }

        public ref T Get<T>(in Entity entity) where T : struct, IComponentBase {

            if (this.components->Has<T>(entity.id) == false) {

                this.filters->OnBeforeAddComponent<T>(in entity);
                this.components->Set<T>(entity.id, default);
                this.storage->versions.Increment(entity.id);

            }
            
            return ref this.components->Get<T>(entity.id);

        }

        public ref readonly T Read<T>(in Entity entity) where T : struct, IComponentBase {
            
            return ref this.components->Get<T>(entity.id);

        }

    }

}
