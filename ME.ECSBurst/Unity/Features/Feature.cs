namespace ME.ECSBurst {

    public abstract class Feature : UnityEngine.ScriptableObject {

        public void AddSystem<T>() where T : struct, ISystem, IOnCreate {
            
            Worlds.currentWorld.AddSystem(new T());
            
        }

        public void AddSystemAdvanceTick<T>() where T : struct, ISystem, IAdvanceTick {
            
            Worlds.currentWorld.AddSystemAdvanceTick(new T());
            
        }

        public void AddSystemVisual<T>() where T : struct, ISystem, IUpdateVisual {
            
            Worlds.currentWorld.AddSystemVisual(new T());
            
        }

        public void AddSystemInput<T>() where T : struct, ISystem, IUpdateInput {
            
            Worlds.currentWorld.AddSystemInput(new T());
            
        }

        public abstract void Initialize();

        public abstract void DeInitialize();

    }

    [System.Serializable]
    public class Features {

        public Feature[] list;

        public void Initialize() {

            for (int i = 0, count = this.list.Length; i < count; ++i) {
                
                this.list[i].Initialize();
                
            }
            
        }

        public void DeInitialize() {

            for (int i = 0, count = this.list.Length; i < count; ++i) {
                
                this.list[i].DeInitialize();
                
            }
            
        }

    }

}