using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ECS
{
    public sealed class EcsComponentPool
    {
        /// <summary>
        /// Global component type counter.
        /// First component will be "1" for correct filters updating (add component on positive and remove on negative).
        /// </summary>
        internal static int ComponentTypesCount;
    }
    
    public sealed class EcsComponentPool<T> : IEcsComponentPool where T : struct
    {
        private delegate void AutoResetHandler(ref T component);

        public Type itemType { get; }
        public T[] Items = new T[128];
        private int[] m_ReservedItems = new int[128];
        private int m_ItemsCount;
        private int m_ReservedItemsCount;
        private readonly AutoResetHandler m_AutoReset;
#if ENABLE_IL2CPP && !UNITY_EDITOR
        T _autoresetFakeInstance;
#endif
        private IEcsComponentPoolResizeListener[] m_ResizeListeners;
        private int m_ResizeListenersCount;

        internal EcsComponentPool()
        {
            itemType = typeof(T);
            if (EcsComponentType<T>.IsAutoReset)
            {
                MethodInfo autoResetMethod = typeof(T).GetMethod(nameof(IEcsAutoReset<T>.AutoReset));
#if DEBUG

                if (autoResetMethod == null)
                {
                    throw new Exception(
                        $"IEcsAutoReset<{typeof(T).Name}> explicit implementation not supported, use implicit instead.");
                }
#endif
                m_AutoReset = (AutoResetHandler)Delegate.CreateDelegate(
                    typeof(AutoResetHandler),
#if ENABLE_IL2CPP && !UNITY_EDITOR
                    _autoresetFakeInstance,
#else
                    null,
#endif
                    autoResetMethod);
            }

            m_ResizeListeners = new IEcsComponentPoolResizeListener[128];
            m_ReservedItemsCount = 0;
        }

        private void RaiseOnResizeEvent()
        {
            for (int i = 0, iMax = m_ResizeListenersCount; i < iMax; i++)
            {
                m_ResizeListeners[i].OnComponentPoolResize();
            }
        }

        public void AddResizeListener(IEcsComponentPoolResizeListener listener)
        {
#if DEBUG
            if (listener == null)
            {
                throw new Exception("Listener is null.");
            }
#endif
            if (m_ResizeListeners.Length == m_ResizeListenersCount)
            {
                Array.Resize(ref m_ResizeListeners, m_ResizeListenersCount << 1);
            }

            m_ResizeListeners[m_ResizeListenersCount++] = listener;
        }

        public void RemoveResizeListener(IEcsComponentPoolResizeListener listener)
        {
#if DEBUG
            if (listener == null)
            {
                throw new Exception("Listener is null.");
            }
#endif
            for (int i = 0, iMax = m_ResizeListenersCount; i < iMax; i++)
            {
                if (m_ResizeListeners[i] == listener)
                {
                    m_ResizeListenersCount--;
                    if (i < m_ResizeListenersCount)
                    {
                        m_ResizeListeners[i] = m_ResizeListeners[m_ResizeListenersCount];
                    }

                    m_ResizeListeners[m_ResizeListenersCount] = null;
                    break;
                }
            }
        }

        /// <summary>
        /// Sets new capacity (if more than current amount).
        /// </summary>
        /// <param name="capacity">New value.</param>
        public void SetCapacity(int capacity)
        {
            if (capacity > Items.Length)
            {
                Array.Resize(ref Items, capacity);
                RaiseOnResizeEvent();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int New()
        {
            int id;
            if (m_ReservedItemsCount > 0)
            {
                id = m_ReservedItems[--m_ReservedItemsCount];
            }
            else
            {
                id = m_ItemsCount;
                if (m_ItemsCount == Items.Length)
                {
                    Array.Resize(ref Items, m_ItemsCount << 1);
                    RaiseOnResizeEvent();
                }

                // reset brand new instance if custom AutoReset was registered.
                m_AutoReset?.Invoke(ref Items[m_ItemsCount]);
                m_ItemsCount++;
            }

            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetItem(int idx)
        {
            return ref Items[idx];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Recycle(int idx)
        {
            if (m_AutoReset != null)
            {
                m_AutoReset(ref Items[idx]);
            }
            else
            {
                Items[idx] = default;
            }

            if (m_ReservedItemsCount == m_ReservedItems.Length)
            {
                Array.Resize(ref m_ReservedItems, m_ReservedItemsCount << 1);
            }

            m_ReservedItems[m_ReservedItemsCount++] = idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyData(int srcIdx, int dstIdx)
        {
            Items[dstIdx] = Items[srcIdx];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsComponentRef<T> Ref(int idx)
        {
            EcsComponentRef<T> componentRef;
            componentRef.Pool = this;
            componentRef.Idx = idx;
            return componentRef;
        }

        object IEcsComponentPool.GetItem(int idx)
        {
            return Items[idx];
        }
    }
}