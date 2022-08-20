using System;
using System.Collections.Generic;
using System.Reflection;

namespace ECS
{
    public static class InjectHelper
    {
        private static readonly HashSet<Type> s_AllType = new HashSet<Type>();
        private static readonly HashSetDict<Type, Type> s_AttributeTypeDict = new HashSetDict<Type,Type>();
        
        public static void InjectSystemToBaseSystem(this EcsWorldSystems system,bool isFixed = false)
        {
            AddAssemblyType("BP");
            s_AttributeTypeDict.Clear();
            foreach (Type type in s_AllType)
            {
                object[] objects = type.GetCustomAttributes(typeof(BaseAttribute), true);

                if (objects.Length == 0)
                {
                    continue;
                }

                BaseAttribute baseAttribute = (BaseAttribute)objects[0];
                s_AttributeTypeDict.Add(baseAttribute.AttributeType, type);
            }

            Type objSystem = isFixed ? typeof(FixedUpdateSystemAttribute): typeof(UpdateSystemAttribute);
            HashSet<Type> typeSet = !s_AttributeTypeDict.ContainsKey(objSystem) ? new HashSet<Type>() : s_AttributeTypeDict[objSystem];
            foreach (Type type in typeSet)
            {
                object obj = Activator.CreateInstance(type);
                switch (obj)
                {
                    case IEcsAwakeSystem objectSystem:
                        system.Add(objectSystem);
                        break;
                    case IEcsStartSystem objectSystem:
                        system.Add(objectSystem);
                        break;
                    case IEcsRunSystem objectSystem:
                        system.Add(objectSystem);
                        break;
                    case IEcsDestroySystem objectSystem:
                        system.Add(objectSystem);
                        break;
                    case IEcsPostDestroySystem objectSystem:
                        system.Add(objectSystem);
                        break;
                }
            }
            system.SortSystem();
        }
        
        public static void AddAssemblyType(string dllName)
        {
            Assembly assembly = Assembly.Load(dllName);

            if (assembly != null)
            {
                IEnumerable<Type> typeList = assembly.GetTypes();
                foreach (Type item in typeList)
                {
                    if(item.IsAbstract && item.IsSealed)
                    {
                        continue;
                    }
                    s_AllType.Add(item);
                      
                }
            }
        }
        
        /// <summary>
        /// Injects custom data to fields of ISystem instance.
        /// </summary>
        /// <param name="system">ISystem instance.</param>
        /// <param name="world">EcsWorld instance.</param>
        /// <param name="injections">Additional instances for injection.</param>
        public static void InjectDataToSystem(this IEcsSystem system, EcsWorld world, Dictionary<Type, object> injections)
        {
            Type systemType = system.GetType();
            Type worldType = world.GetType();
            Type filterType = typeof(EcsFilter);
            Type ignoreType = typeof(EcsIgnoreInjectAttribute);

            foreach (FieldInfo f in systemType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                // skip statics or fields with [EcsIgnoreInject] attribute.
                if (f.IsStatic || Attribute.IsDefined(f, ignoreType))
                {
                    continue;
                }

                // EcsWorld
                if (f.FieldType.IsAssignableFrom(worldType))
                {
                    f.SetValue(system, world);
                    continue;
                }
                // EcsFilter
#if DEBUG
                if (f.FieldType == filterType)
                {
                    throw new Exception($"Cant use EcsFilter type at \"{system}\" system for dependency injection, use generic version instead");
                }
#endif
                if (f.FieldType.IsSubclassOf(filterType))
                {
                    f.SetValue(system, world.GetFilter(f.FieldType));
                    continue;
                }

                // Other injections.
                foreach (KeyValuePair<Type, object> pair in injections)
                {
                    if (f.FieldType.IsAssignableFrom(pair.Key))
                    {
                        f.SetValue(system, pair.Value);
                        break;
                    }
                }
            }
        }
    }
}