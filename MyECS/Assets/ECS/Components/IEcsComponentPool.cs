using System;

namespace ECS
{
    public interface IEcsComponentPool
    {
        Type itemType { get; }
        object GetItem(int idx);
        void Recycle(int idx);
        int New();
        void CopyData(int srcIdx, int dstIdx);
    }
}