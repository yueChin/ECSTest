// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2022 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace ECS
{
    /// <summary>
    /// Entity descriptor.
    /// </summary>
    public struct EcsEntity : IEquatable<EcsEntity>
    {
        internal int Id;
        internal ushort Gen;
        internal EcsWorld OwnerWorld;
#if DEBUG
        // For using in IDE debugger.
        internal object[] components
        {
            get
            {
                object[] list = null;
                if (this.IsAlive())
                {
                    this.GetComponentValues(ref list);
                }

                return list;
            }
        }
#endif

        public static readonly EcsEntity Null = new EcsEntity();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in EcsEntity lhs, in EcsEntity rhs)
        {
            return lhs.Id == rhs.Id && lhs.Gen == rhs.Gen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in EcsEntity lhs, in EcsEntity rhs)
        {
            return lhs.Id != rhs.Id || lhs.Gen != rhs.Gen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyMemberInGetHashCode
                int hashCode = (Id * 397) ^ Gen.GetHashCode();
                hashCode = (hashCode * 397) ^ (OwnerWorld != null ? OwnerWorld.GetHashCode() : 0);
                // ReSharper restore NonReadonlyMemberInGetHashCode
                return hashCode;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            return other is EcsEntity otherEntity && Equals(otherEntity);
        }

#if DEBUG
        public override string ToString()
        {
            if (this.IsNull())
            {
                return "Entity-Null";
            }

            if (!this.IsAlive())
            {
                return "Entity-NonAlive";
            }

            Type[] types = null;
            this.GetComponentTypes(ref types);
            StringBuilder sb = new System.Text.StringBuilder(512);
            foreach (Type type in types)
            {
                if (sb.Length > 0)
                {
                    sb.Append(",");
                }

                sb.Append(type.Name);
            }

            return $"Entity-{Id}:{Gen} [{sb}]";
        }
#endif
        public bool Equals(EcsEntity other)
        {
            return Id == other.Id && Gen == other.Gen && OwnerWorld == other.OwnerWorld;
        }
    }

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    
   
}