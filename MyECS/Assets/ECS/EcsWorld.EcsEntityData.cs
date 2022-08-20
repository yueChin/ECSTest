using System.Runtime.InteropServices;

namespace ECS
{
    public partial class EcsWorld
    {
        /// <summary>
        /// Internal state of entity.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct EcsEntityData
        {
            public ushort Gen;
            public short ComponentsCountX2;
            public int[] Components;
        }
    }
}