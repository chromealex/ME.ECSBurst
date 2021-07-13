
namespace ME.ECSBurst {
    
    using Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using static MemUtilsCuts;

    #if ECS_COMPILE_IL2CPP_OPTIONS
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.NullChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
    #endif
    public unsafe struct Filters {

        public NativeListBurst<System.IntPtr> filters;

        public void Initialize() {

            this.filters = new NativeListBurst<System.IntPtr>(DefaultConfig.FILTERS_CAPACITY, Unity.Collections.Allocator.Persistent);
            
        }
        
        public void Dispose() {

            this.filters.Dispose();

        }
        
        public FilterData* Add(ref FilterData filterData) {

            for (int i = 0; i < this.filters.Length; ++i) {

                if (((FilterData*)this.filters[i])->IsEquals(in filterData) == true) {
                    
                    UnityEngine.Debug.Log("Found equals: " + filterData.id);
                    return (FilterData*)this.filters[i];
                    
                }
                
            }

            filterData.id = this.filters.Length;
            filterData.entities = PoolArrayNative<byte>.Spawn(10);
            
            // For each entity in world - create
            var entities = filterData.storage->GetAlive();
            filterData.Validate(filterData.storage->GetMaxId());
            for (int i = 0, cnt = entities.Length; i < cnt; ++i) {

                var ent = filterData.storage->cache[entities[i]];
                filterData.UpdateEntity(in ent);

            }

            var ptr = tnew(ref filterData);
            this.filters.Add((System.IntPtr)ptr);

            return ptr;

        }

        private FilterData* GetPtr(int index) {

            return (FilterData*)this.filters[index];

        }

        public void OnBeforeAddComponent<T>(in Entity entity) {
            
            for (int i = 0; i < this.filters.Length; ++i) {
                
                this.GetPtr(i)->AddEntityCheckComponent<T>(in entity);
                
            }

        }

        public void OnBeforeRemoveComponent<T>(in Entity entity) {
            
            for (int i = 0; i < this.filters.Length; ++i) {
                
                this.GetPtr(i)->RemoveEntityCheckComponent<T>(in entity);
                
            }

        }

        public void OnAfterEntityCreate(in Entity entity) {

            for (int i = 0; i < this.filters.Length; ++i) {
                
                this.GetPtr(i)->OnEntityCreate(in entity);
                
            }

        }

        public void OnBeforeEntityDestroy(in Entity entity) {

            for (int i = 0; i < this.filters.Length; ++i) {
                
                this.GetPtr(i)->RemoveEntity(in entity);
                
            }

        }

    }

    public unsafe struct Filter {

        public struct Enumerator : System.Collections.Generic.IEnumerator<Entity> {

            private FilterData* filterData;
            private int index;
            private NativeBufferArray<Entity> cache;

            public Enumerator(FilterData* filterData) {
                
                this.filterData = filterData;
                this.index = -1;
                this.cache = filterData->storage->cache;

            }

            public bool MoveNext() {

                ++this.index;
                while (this.index < this.filterData->entities.Length) {

                    var state = this.filterData->entities[this.index];
                    if (state == 0) {
                        ++this.index;
                        continue;
                    }
                    return true;
                    
                }
                
                return false;
                
            }

            object System.Collections.IEnumerator.Current => throw new AllocationException();

            Entity System.Collections.Generic.IEnumerator<Entity>.Current => throw new AllocationException();

            public ref Entity Current => ref this.cache[this.index];

            public void Reset() {
                
                this.index = -1;
                
            }

            public void Dispose() {

                this.index = default;
                this.filterData = default;
                this.cache = default;

            }

        }

        [NativeDisableUnsafePtrRestriction]
        public FilterData* ptr;

        #region Public API
        public Enumerator GetEnumerator() {
            
            return new Enumerator(this.ptr);
            
        }

        public struct FilterEntry {
            
            public static FilterEntry Empty = new FilterEntry() { isCreated = false };
            public static FilterEntry New = new FilterEntry() { isCreated = true };

            public bool isCreated;
            public Archetype contains;
            public Archetype notContains;

            public FilterEntry With<T>() {
                
                WorldUtilities.UpdateComponentTypeId<T>();
                Filter.current.contains.Add<T>();
                return this;

            }

            public FilterEntry Without<T>() {
                
                WorldUtilities.UpdateComponentTypeId<T>();
                Filter.current.notContains.Add<T>();
                return this;

            }

            public Filter Push() {

                return new FilterData().Set(this).Push();

            }

            public Filter Push(ref Filter filterData) {

                return new FilterData().Set(this).Push(ref filterData);

            }

        }

        internal static FilterEntry current;
        public static FilterEntry With<T>() {
            
            if (Filter.current.isCreated == false) Filter.current = FilterEntry.New;
            return Filter.current.With<T>();
            
        }

        public static FilterEntry Without<T>() {
            
            if (Filter.current.isCreated == false) Filter.current = FilterEntry.New;
            return Filter.current.Without<T>();
            
        }
        #endregion

    }

    #if ECS_COMPILE_IL2CPP_OPTIONS
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.NullChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
    #endif
    public unsafe struct FilterData {

        public int id;
        [NativeDisableUnsafePtrRestriction]
        public Storage* storage;
        public NativeBufferArray<byte> entities; // 0 - not contains, 1 - contains
        public Archetype contains;
        public Archetype notContains;

        public int Capacity => this.entities.Length;
        
        #region Internal API
        internal void AddEntityCheckComponent<T>(in Entity entity) {

            var arch = this.storage->archetypes.Get(in entity);
            if (arch.Has(this.contains) == false ||
                arch.HasNot(this.notContains) == false) {

                arch.Add<T>();
                
                if (arch.Has(this.contains) == true &&
                    arch.HasNot(this.notContains) == true) {

                    this.entities[entity.id] = 1;

                }

            }

        }

        internal void RemoveEntityCheckComponent<T>(in Entity entity) {
            
            var arch = this.storage->archetypes.Get(in entity);
            if (arch.Has(this.contains) == true &&
                arch.HasNot(this.notContains) == true) {

                arch.Subtract<T>();
                
                if (arch.Has(this.contains) == false ||
                    arch.HasNot(this.notContains) == false) {

                    this.entities[entity.id] = 0;

                }

            }

        }

        internal void OnEntityCreate(in Entity entity) {

            ArrayUtils.Resize(entity.id, ref this.entities);

        }

        internal void Validate(int entityId) {

            ArrayUtils.Resize(entityId, ref this.entities);

        }

        internal void UpdateEntity(in Entity entity) {
            
            var arch = this.storage->archetypes.Get(in entity);
            if (arch.Has(this.contains) == true &&
                arch.HasNot(this.notContains) == true) {

                this.entities[entity.id] = 1;

            }

        }

        internal void RemoveEntity(in Entity entity) {
            
            this.entities[entity.id] = 0;
            
        }
        
        internal FilterData Set(Filter.FilterEntry entry) {

            this.contains = entry.contains;
            this.notContains = entry.notContains;
            return this;

        }
        
        internal Filter Push() {

            Filter _ = default;
            return this.Push(ref Worlds.currentWorld, ref _);
            
        }

        internal Filter Push(ref World world) {

            Filter _ = default;
            return this.Push(ref world, ref _);
            
        }

        internal Filter Push(ref Filter variable) {
            
            return this.Push(ref Worlds.currentWorld, ref variable);
            
        }

        internal Filter Push(ref World world, ref Filter variable) {

            Filter.current = Filter.FilterEntry.Empty;
            this.storage = world.currentState->storage;
            var filterDataPtr = world.currentState->AddFilter(ref this);
            variable = new Filter() {
                ptr = filterDataPtr,
            };
            UnityEngine.Debug.Log($"Create filter #{variable.ptr->id}, ptr: {(System.IntPtr)variable.ptr} + {(System.IntPtr)variable.ptr->entities.arr.GetUnsafeReadOnlyPtr()}");
            return variable;

        }
        
        internal bool IsEquals(in FilterData filterData) {

            return this.contains == filterData.contains &&
                   this.notContains == filterData.notContains;
            
        }
        #endregion
        
    }

}
