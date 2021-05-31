
namespace ME.ECSBurst {
    
    using Collections;
    using Unity.Collections.LowLevel.Unsafe;

    #if ECS_COMPILE_IL2CPP_OPTIONS
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.NullChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
    #endif
    public struct Filters {

        public NativeListBurst<Filter> filters;

        public void Initialize() {

            this.filters = new NativeListBurst<Filter>(DefaultConfig.FILTERS_CAPACITY, Unity.Collections.Allocator.Persistent);
            
        }
        
        public void Dispose() {

            this.filters.Dispose();

        }
        
        public unsafe Filter Add(ref Filter filter) {

            for (int i = 0; i < this.filters.Length; ++i) {

                if (this.filters[i].IsEquals(in filter) == true) {
                    
                    return this.filters[i];
                    
                }
                
            }

            filter.id = this.filters.Length;
            filter.entities = PoolArrayNative<byte>.Spawn(10);
            
            // For each entity in world - create
            var entities = filter.storage->GetAlive();
            filter.Validate(filter.storage->GetMaxId());
            for (int i = 0, cnt = entities.Length; i < cnt; ++i) {

                var ent = filter.storage->cache[entities[i]];
                filter.UpdateEntity(in ent);

            }
            
            this.filters.Add(filter);

            return filter;

        }

        public void OnBeforeAddComponent<T>(in Entity entity) {
            
            for (int i = 0; i < this.filters.Length; ++i) {
                
                this.filters[i].AddEntityCheckComponent<T>(in entity);
                
            }

        }

        public void OnBeforeRemoveComponent<T>(in Entity entity) {
            
            for (int i = 0; i < this.filters.Length; ++i) {
                
                this.filters[i].RemoveEntityCheckComponent<T>(in entity);
                
            }

        }

        public void OnAfterEntityCreate(in Entity entity) {

            for (int i = 0; i < this.filters.Length; ++i) {
                
                this.filters.GetRef(i).OnEntityCreate(in entity);
                
            }

        }

        public void OnBeforeEntityDestroy(in Entity entity) {

            for (int i = 0; i < this.filters.Length; ++i) {
                
                this.filters[i].RemoveEntity(in entity);
                
            }

        }

    }

    #if ECS_COMPILE_IL2CPP_OPTIONS
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.NullChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
    #endif
    public unsafe struct Filter {

        public struct Enumerator : System.Collections.Generic.IEnumerator<Entity> {

            private Filter filter;
            private int index;
            private NativeBufferArray<Entity> cache;

            public Enumerator(Filter filter) {
                
                this.filter = filter;
                this.index = -1;
                this.cache = filter.storage->cache;

            }

            public bool MoveNext() {

                ++this.index;
                while (this.index < this.filter.entities.Length) {

                    var state = this.filter.entities[this.index];
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
                this.filter = default;
                this.cache = default;

            }

        }

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
        
        internal Filter Set(FilterEntry entry) {

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

            Filter.current = FilterEntry.Empty;
            this.storage = world.currentState->storage;
            variable = world.currentState->AddFilter(ref this);
            var ptr = variable.entities.arr.GetUnsafePtr();
            UnityEngine.Debug.Log(string.Format("Create filter: {0}", (System.IntPtr)ptr));
            return variable;

        }
        
        internal bool IsEquals(in Filter filter) {

            return this.contains == filter.contains &&
                   this.notContains == filter.notContains;
            
        }
        #endregion
        
        #region Public API
        public Enumerator GetEnumerator() {
            
            return new Enumerator(this);
            
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

                return new Filter().Set(this).Push();

            }

            public Filter Push(ref Filter filter) {

                return new Filter().Set(this).Push(ref filter);

            }

        }

        private static FilterEntry current;
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

}
