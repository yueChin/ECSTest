using System;
using System.Runtime.CompilerServices;
using System.Threading;

// ReSharper disable ClassNeverInstantiated.Global

namespace ECS
{
    /// <summary>
    /// Marks component type to be not auto-filled as GetX in filter.
    /// </summary>
    public interface IEcsIgnoreInFilter
    {
    }

    /// <summary>
    /// Marks component type for custom reset behaviour.
    /// </summary>
    /// <typeparam name="T">Type of component, should be the same as main component!</typeparam>
    public interface IEcsAutoReset<T> where T : struct
    {
        void AutoReset(ref T c);
    }

    /// <summary>
    /// Global descriptor of used component type.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    public static class EcsComponentType<T> where T : struct
    {
        // ReSharper disable StaticMemberInGenericType
        public static readonly int TypeIndex;
        public static readonly Type Type;
        public static readonly bool IsIgnoreInFilter;

        public static readonly bool IsAutoReset;
        // ReSharper restore StaticMemberInGenericType

        static EcsComponentType()
        {
            TypeIndex = Interlocked.Increment(ref EcsComponentPool.ComponentTypesCount);
            Type = typeof(T);
            IsIgnoreInFilter = typeof(IEcsIgnoreInFilter).IsAssignableFrom(Type);
            IsAutoReset = typeof(IEcsAutoReset<T>).IsAssignableFrom(Type);
#if DEBUG
            if (!IsAutoReset && Type.GetInterface("IEcsAutoReset`1") != null)
            {
                throw new Exception($"IEcsAutoReset should have <{typeof(T).Name}> constraint for component \"{typeof(T).Name}\".");
            }
#endif
        }
    }

    /// <summary>
    /// Helper for save reference to component. 
    /// </summary>
    /// <typeparam name="T">Type of component.</typeparam>
    public struct EcsComponentRef<T> where T : struct
    {
        internal EcsComponentPool<T> Pool;
        internal int Idx;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreEquals(in EcsComponentRef<T> lhs, in EcsComponentRef<T> rhs)
        {
            return lhs.Idx == rhs.Idx && lhs.Pool == rhs.Pool;
        }
    }

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    public static class EcsComponentRefExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Unref<T>(in this EcsComponentRef<T> wrapper) where T : struct
        {
            return ref wrapper.Pool.Items[wrapper.Idx];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNull<T>(in this EcsComponentRef<T> wrapper) where T : struct
        {
            return wrapper.Pool == null;
        }
    }

    public interface IEcsComponentPoolResizeListener
    {
        void OnComponentPoolResize();
    }

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif

}