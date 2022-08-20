namespace ECS
{
    /// <summary>
    /// Base interface for all systems.
    /// </summary>
    public interface IEcsSystem
    {
        public int SystemPriority();
    }

    /// <summary>
    /// Interface for PreInit systems. PreInit() will be called before Init().
    /// </summary>
    public interface IEcsAwakeSystem : IEcsSystem
    {
        void Awake();
    }

    /// <summary>
    /// Interface for Init systems. Init() will be called before Run().
    /// </summary>
    public interface IEcsStartSystem : IEcsSystem
    {
        void Start();
    }
    
    /// <summary>
    /// Interface for Destroy systems. Destroy() will be called last in system lifetime cycle.
    /// </summary>
    public interface IEcsDestroySystem : IEcsSystem
    {
        void Destroy();
    }

    /// <summary>
    /// Interface for PostDestroy systems. PostDestroy() will be called after Destroy().
    /// </summary>
    public interface IEcsPostDestroySystem : IEcsSystem
    {
        void PostDestroy();
    }

    
    /// <summary>
    /// Interface for Run systems.
    /// </summary>
    public interface IEcsRunSystem : IEcsSystem
    {
        void Run();
    }
    
#if DEBUG
    /// <summary>
    /// Debug interface for systems events processing.
    /// </summary>
    public interface IEcsSystemsDebugListener
    {
        void OnSystemsDestroyed(EcsWorldSystems systems);
    }
#endif
}