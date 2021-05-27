namespace ME.ECSBurst {
    
    using Unity.Mathematics;

    public struct Name : IComponentBase {

        public Unity.Collections.FixedString32 value;

    }

    public struct Position : IComponentBase {

        public float3 value;

    }

    public struct Rotation : IComponentBase {

        public quaternion value;

    }

    public struct Scale : IComponentBase {

        public float3 value;

    }

}