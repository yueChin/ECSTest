using System;
using System.Collections.Generic;
using System.Reflection;

namespace ECS
{
    /// <summary>
    /// Logical group of systems.
    /// </summary>
#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsWorldSystems : IEcsStartSystem, IEcsDestroySystem, IEcsRunSystem
    {
        public readonly string Name;
        public readonly EcsWorld World;
        private readonly EcsGrowList<IEcsAwakeSystem> m_AwakeSystemGrowList = new EcsGrowList<IEcsAwakeSystem>(64);
        private readonly EcsGrowList<IEcsStartSystem> m_StartSystemGrowList = new EcsGrowList<IEcsStartSystem>(64);
        private readonly EcsGrowList<IEcsDestroySystem> m_DestroySystemGrowList = new EcsGrowList<IEcsDestroySystem>(64);
        private readonly EcsGrowList<IEcsPostDestroySystem> m_PostDestroySystemGrowList = new EcsGrowList<IEcsPostDestroySystem>(64);
        private readonly EcsGrowList<IEcsRunSystem> m_RunSystemGrowList = new EcsGrowList<IEcsRunSystem>(64);
        
        private readonly Dictionary<int, int> m_NamedRunSystemDict = new Dictionary<int, int>(64);
        private readonly Dictionary<Type, object> m_InjectionDict = new Dictionary<Type, object>(32);
        private bool m_Injected;
#if DEBUG
        private bool m_Initialized;
        private bool m_Destroyed;
        private readonly List<IEcsSystemsDebugListener> m_DebugListeners = new List<IEcsSystemsDebugListener>(4);

        /// <summary>
        /// Adds external event listener.
        /// </summary>
        /// <param name="listener">Event listener.</param>
        public void AddDebugListener(IEcsSystemsDebugListener listener)
        {
            if (listener == null)
            {
                throw new Exception("listener is null");
            }

            m_DebugListeners.Add(listener);
        }

        /// <summary>
        /// Removes external event listener.
        /// </summary>
        /// <param name="listener">Event listener.</param>
        public void RemoveDebugListener(IEcsSystemsDebugListener listener)
        {
            if (listener == null)
            {
                throw new Exception("listener is null");
            }

            m_DebugListeners.Remove(listener);
        }
#endif

        /// <summary>
        /// Creates new instance of EcsSystems group.
        /// </summary>
        /// <param name="world">EcsWorld instance.</param>
        /// <param name="name">Custom name for this group.</param>
        public EcsWorldSystems(EcsWorld world, string name = null)
        {
            World = world;
            Name = name;
        }

        /// <summary>
        /// Adds new system to processing.
        /// </summary>
        /// <param name="system">System instance.</param>
        /// <param name="namedRunSystem">Optional name of system.</param>
        public EcsWorldSystems Add(IEcsSystem system, string namedRunSystem = null)
        {
#if DEBUG
            if (system == null)
            {
                throw new Exception("System is null.");
            }

            if (m_Initialized)
            {
                throw new Exception("Cant add system after initialization.");
            }

            if (m_Destroyed)
            {
                throw new Exception("Cant touch after destroy.");
            }

            if (!string.IsNullOrEmpty(namedRunSystem) && !(system is IEcsRunSystem))
            {
                throw new Exception("Cant name non-IEcsRunSystem.");
            }
#endif
            switch (system)
            {
                case IEcsAwakeSystem ecsAwakeSystem :
                    m_AwakeSystemGrowList.Add(ecsAwakeSystem);
                    break;
                case IEcsStartSystem ecsStartSystem:
                    m_StartSystemGrowList.Add(ecsStartSystem);
                    break;
                case IEcsRunSystem runSystem:
                {
                    if (namedRunSystem == null && runSystem is EcsWorldSystems ecsSystems)
                    {
                        namedRunSystem = ecsSystems.Name;
                    }

                    if (namedRunSystem != null)
                    {
#if DEBUG
                        if (m_NamedRunSystemDict.ContainsKey(namedRunSystem.GetHashCode()))
                        {
                            throw new Exception($"Cant add named system - \"{namedRunSystem}\" name already exists.");
                        }
#endif
                        m_NamedRunSystemDict[namedRunSystem.GetHashCode()] = m_RunSystemGrowList.Count;
                    }
                    m_RunSystemGrowList.Add(runSystem);
                    break;
                }
                case IEcsDestroySystem ecsDestroySystem:
                    m_DestroySystemGrowList.Add(ecsDestroySystem);
                    break;
                case IEcsPostDestroySystem ecsPostDestroySystem:
                    m_PostDestroySystemGrowList.Add(ecsPostDestroySystem);
                    break;
            }

            return this;
        }

        public int GetNamedRunSystem(string name)
        {
            return m_NamedRunSystemDict.TryGetValue(name.GetHashCode(), out int idx) ? idx : -1;
        }

        /// <summary>
        /// Gets all run systems. Important: Don't change collection!
        /// </summary>
        public EcsGrowList<IEcsRunSystem> GetRunSystems()
        {
            return m_RunSystemGrowList;
        }

        /// <summary>
        /// Injects instance of object type to all compatible fields of added systems.
        /// </summary>
        /// <param name="obj">Instance.</param>
        /// <param name="overridenType">Overriden type, if null - typeof(obj) will be used.</param>
        public EcsWorldSystems Inject(object obj, Type overridenType = null)
        {
#if DEBUG
            if (m_Initialized)
            {
                throw new Exception("Cant inject after initialization.");
            }

            if (obj == null)
            {
                throw new Exception("Cant inject null instance.");
            }

            if (overridenType != null && !overridenType.IsInstanceOfType(obj))
            {
                throw new Exception("Invalid overriden type.");
            }
#endif
            if (overridenType == null)
            {
                overridenType = obj.GetType();
            }

            m_InjectionDict[overridenType] = obj;
            return this;
        }

        /// <summary>
        /// Processes injections immediately.
        /// Can be used to DI before Init() call.
        /// </summary>
        public EcsWorldSystems ProcessInjects()
        {
#if DEBUG
            if (m_Initialized)
            {
                throw new Exception("Cant inject after initialization.");
            }

            if (m_Destroyed)
            {
                throw new Exception("Cant touch after destroy.");
            }
#endif
            // if (!m_Injected)
            // {
            //     m_Injected = true;
            //     for (int i = 0, iMax = m_AllSystemGrowList.Count; i < iMax; i++)
            //     {
            //         if (m_AllSystemGrowList[i] is EcsWorldSystems nestedSystems)
            //         {
            //             foreach (KeyValuePair<Type, object> pair in m_InjectionDict)
            //             {
            //                 nestedSystems.m_InjectionDict[pair.Key] = pair.Value;
            //             }
            //
            //             nestedSystems.ProcessInjects();
            //         }
            //         else
            //         {
            //             m_AllSystemGrowList[i].InjectDataToSystem( World, m_InjectionDict);
            //         }
            //     }
            // }

            return this;
        }

        /// <summary>
        /// Closes registration for new systems, initialize all registered.
        /// </summary>
        public void Start()
        {
#if DEBUG
            if (m_Initialized)
            {
                throw new Exception("Already initialized.");
            }

            if (m_Destroyed)
            {
                throw new Exception("Cant touch after destroy.");
            }
#endif
            ProcessInjects();
            // IEcsPreInitSystem processing.
            for (int i = 0, iMax = m_AwakeSystemGrowList.Count; i < iMax; i++)
            {
                IEcsSystem system = m_AwakeSystemGrowList[i];
                if (system is IEcsAwakeSystem awakeSystem)
                {
                    awakeSystem.Awake();
#if DEBUG
                    World.CheckForLeakedEntities($"{awakeSystem.GetType().Name}.PreInit()");
#endif
                }
            }

            // IEcsInitSystem processing.
            for (int i = 0, iMax = m_StartSystemGrowList.Count; i < iMax; i++)
            {
                IEcsSystem system = m_StartSystemGrowList[i];
                if (system is IEcsStartSystem initSystem)
                {
                    initSystem.Start();
#if DEBUG
                    World.CheckForLeakedEntities($"{initSystem.GetType().Name}.Init()");
#endif
                }
            }
#if DEBUG
            m_Initialized = true;
#endif
        }

        /// <summary>
        /// Processes all IEcsRunSystem systems.
        /// </summary>
        public void Run()
        {
#if DEBUG
            if (!m_Initialized)
            {
                throw new Exception($"[{Name ?? "NONAME"}] EcsSystems should be initialized before.");
            }

            if (m_Destroyed)
            {
                throw new Exception("Cant touch after destroy.");
            }
#endif
            for (int i = 0, iMax = m_RunSystemGrowList.Count; i < iMax; i++)
            {
                IEcsRunSystem runItem = m_RunSystemGrowList.Items[i];
                runItem.Run();
#if DEBUG
                if (World.CheckForLeakedEntities(null))
                {
                    throw new Exception($"Empty entity detected, possible memory leak in {m_RunSystemGrowList.Items[i].GetType().Name}.Run ()");
                }
#endif
            }
        }

        /// <summary>
        /// Destroys registered data.
        /// </summary>
        public void Destroy()
        {
#if DEBUG
            if (m_Destroyed)
            {
                throw new Exception("Already destroyed.");
            }

            m_Destroyed = true;
#endif
            // IEcsDestroySystem processing.
            for (int i = m_DestroySystemGrowList.Count - 1; i >= 0; i--)
            {
                IEcsSystem system = m_DestroySystemGrowList[i];
                if (system is IEcsDestroySystem destroySystem)
                {
                    destroySystem.Destroy();
#if DEBUG
                    World.CheckForLeakedEntities($"{destroySystem.GetType().Name}.Destroy ()");
#endif
                }
            }

            // IEcsPostDestroySystem processing.
            for (int i = m_PostDestroySystemGrowList.Count - 1; i >= 0; i--)
            {
                IEcsSystem system = m_PostDestroySystemGrowList.Items[i];
                if (system is IEcsPostDestroySystem postDestroySystem)
                {
                    postDestroySystem.PostDestroy();
#if DEBUG
                    World.CheckForLeakedEntities($"{postDestroySystem.GetType().Name}.PostDestroy ()");
#endif
                }
            }
#if DEBUG
            for (int i = 0, iMax = m_DebugListeners.Count; i < iMax; i++)
            {
                m_DebugListeners[i].OnSystemsDestroyed(this);
            }
#endif
        }

        public int SystemPriority()
        {
            return 0;
        }

        public void SortSystem()
        {
            
        }
    }
}