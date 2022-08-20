using System;

namespace ECS
{
    /// <summary>
    /// Marks field of IEcsSystem class to be ignored during dependency injection.
    /// </summary>
    public sealed class FixedUpdateSystemAttribute : BaseAttribute
    {
    }
}