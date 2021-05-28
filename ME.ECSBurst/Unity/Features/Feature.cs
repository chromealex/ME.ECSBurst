namespace ME.ECSBurst {

    public abstract class Feature : UnityEngine.ScriptableObject {

        public void AddSystem<T>() where T : struct, ISystem, IOnCreate {
            
            Worlds.currentWorld.AddSystem(new T());
            
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