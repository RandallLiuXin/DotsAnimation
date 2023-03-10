using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Unity.Entities;

namespace Tinder
{
    /// <summary>
    /// Add this attribute to a system to prevent the system from being injected into the default group.
    /// Only works when using an injection method in BootstrapTools.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class NoGroupInjectionAttribute : Attribute
    {
    }

    public static class BootstrapTools
    {
        /// <summary>
        /// Injects all systems in which contain 'namespaceSubstring within their namespaces.
        /// Automatically creates parent ComponentSystemGroups if necessary.
        /// Use this for libraries which use traditional injection to inject into the default world.
        /// </summary>
        /// <param name="systems">List of systems containing the namespaced systems to inject using world.GetOrCreateSystem</param>
        /// <param name="namespaceSubstring">The namespace substring to query the systems' namespace against</param>
        /// <param name="world">The world to inject the systems into</param>
        /// <param name="defaultGroup">If no UpdateInGroupAttributes exist on the type and this value is not null, the system is injected in this group</param>
        public static void InjectSystemsFromNamespace(List<Type> systems, string namespaceSubstring, World world, ComponentSystemGroup defaultGroup = null)
        {
            foreach (var type in systems)
            {
                if (type.Namespace == null)
                {
                    Debug.LogWarning("No namespace for " + type.ToString());
                    continue;
                }
                else if (!type.Namespace.Contains(namespaceSubstring))
                    continue;

                InjectSystem(type, world, defaultGroup);
            }
        }

        /// <summary>
        /// Injects all systems made by Unity (or systems that use "Unity" in their namespace or assembly).
        /// Automatically creates parent ComponentSystemGroups if necessary.
        /// Use this instead of InjectSystemsFromNamespace because Unity sometimes forgets to put namespaces on things.
        /// </summary>
        /// <param name="systems"></param>
        /// <param name="world"></param>
        /// <param name="defaultGroup"></param>
        public static void InjectUnitySystems(List<Type> systems, World world, ComponentSystemGroup defaultGroup = null, bool silenceWarnings = true)
        {
            var sysList = new List<Type>();
            foreach (var type in systems)
            {
                if (type.Namespace == null)
                {
                    if (type.Assembly.FullName.Contains("Unity") && !silenceWarnings)
                    {
                        Debug.LogWarning("Hey Unity Devs! You forget a namespace for " + type.ToString());
                    }
                    else
                        continue;
                }
                else if (!type.Namespace.Contains("Unity"))
                    continue;

                sysList.Add(type);
                InjectSystem(type, world, defaultGroup);
            }
        }

        public struct ComponentSystemBaseSystemHandleUntypedUnion
        {
            public ComponentSystemBase systemManaged;
            public SystemHandleUntyped systemHandle;

            public static implicit operator ComponentSystemBase(ComponentSystemBaseSystemHandleUntypedUnion me) => me.systemManaged;
            public static implicit operator SystemHandleUntyped(ComponentSystemBaseSystemHandleUntypedUnion me) => me.systemHandle;
        }

        //Copied and pasted from Entities package and then modified as needed.
        /// <summary>
        /// Injects the system into the world. Automatically creates parent ComponentSystemGroups if necessary.
        /// </summary>
        /// <remarks>This function does nothing for unmanaged systems.</remarks>
        /// <param name="type">The type to inject. Uses world.GetOrCreateSystem</param>
        /// <param name="world">The world to inject the system into</param>
        /// <param name="defaultGroup">If no UpdateInGroupAttributes exist on the type and this value is not null, the system is injected in this group</param>
        /// <param name="groupRemap">If a type in an UpdateInGroupAttribute matches a key in this dictionary, it will be swapped with the value</param>
        public static ComponentSystemBaseSystemHandleUntypedUnion InjectSystem(Type type,
                                                                               World world,
                                                                               ComponentSystemGroup defaultGroup          = null,
                                                                               IReadOnlyDictionary<Type, Type> groupRemap = null)
        {
            bool isManaged = false;
            if (typeof(ComponentSystemBase).IsAssignableFrom(type))
            {
                isManaged = true;
            }
            else if (!typeof(ISystem).IsAssignableFrom(type))
            {
                return default;
            }

            var groups = TypeManager.GetSystemAttributes(type, typeof(UpdateInGroupAttribute));

            ComponentSystemBaseSystemHandleUntypedUnion newSystem = default;
            if (isManaged)
            {
                newSystem.systemManaged = world.GetOrCreateSystem(type);
                newSystem.systemHandle  = newSystem.systemManaged.SystemHandleUntyped;
            }
            else
            {
                newSystem.systemHandle = world.GetOrCreateUnmanagedSystem(type);
            }
            if (groups.Length == 0 && defaultGroup != null)
            {
                if (isManaged)
                    defaultGroup.AddSystemToUpdateList(newSystem);
                else
                    defaultGroup.AddUnmanagedSystemToUpdateList(newSystem);
            }
            foreach (var g in groups)
            {
                if (TypeManager.GetSystemAttributes(newSystem.GetType(), typeof(NoGroupInjectionAttribute)).Length > 0)
                    break;

                var group = FindOrCreateGroup(world, type, g, defaultGroup, groupRemap);
                if (group != null)
                {
                    if (isManaged)
                        group.AddSystemToUpdateList(newSystem);
                    else
                        group.AddUnmanagedSystemToUpdateList(newSystem);
                }
            }
            return newSystem;
        }

        private static ComponentSystemGroup FindOrCreateGroup(World world, Type systemType, Attribute attr, ComponentSystemGroup defaultGroup, IReadOnlyDictionary<Type, Type> remap)
        {
            var uga = attr as UpdateInGroupAttribute;

            if (uga == null)
                return null;

            var groupType = uga.GroupType;
            if (remap != null && remap.TryGetValue(uga.GroupType, out var remapType))
                groupType = remapType;
            if (groupType == null)
                return null;

            if (!TypeManager.IsSystemAGroup(groupType))
            {
                throw new InvalidOperationException($"Invalid [UpdateInGroup] attribute for {systemType}: {uga.GroupType} must be derived from ComponentSystemGroup.");
            }
            if (uga.OrderFirst && uga.OrderLast)
            {
                throw new InvalidOperationException($"The system {systemType} can not specify both OrderFirst=true and OrderLast=true in its [UpdateInGroup] attribute.");
            }

            var groupSys = world.GetExistingSystem(groupType);
            if (groupSys == null)
            {
                groupSys = InjectSystem(groupType, world, defaultGroup, remap);
            }

            return groupSys as ComponentSystemGroup;
        }

        /// <summary>
        /// Builds a ComponentSystemGroup and auto-injects children systems in the list.
        /// Systems without an UpdateInGroupAttribute are not added.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="systems">Systems that are allowed to be added to the Group hierarchy</param>
        /// <param name="world">The world in which the systems are built</param>
        /// <returns></returns>
        public static T BuildSystemGroup<T>(List<Type> systems, World world) where T : ComponentSystemGroup
        {
            var groupsToCreate = new HashSet<Type>();
            var allGroups      = new List<Type>();
            foreach (var system in systems)
            {
                if (IsGroup(system))
                    allGroups.Add(system);
            }
            AddChildrenGroupsToHashsetRecursively(typeof(T), allGroups, groupsToCreate);

            var groupList = new List<(Type, ComponentSystemGroup)>();
            foreach (var system in groupsToCreate)
            {
                groupList.Add((system, world.CreateSystem(system) as ComponentSystemGroup));
            }

            foreach (var system in groupList)
            {
                foreach (var targetGroup in groupList)
                {
                    if (IsInGroup(system.Item1, targetGroup.Item1))
                        targetGroup.Item2.AddSystemToUpdateList(system.Item2);
                }
            }

            foreach (var system in systems)
            {
                if (IsGroup(system))
                    continue;
                if (!typeof(ComponentSystemBase).IsAssignableFrom(system))
                    continue;
                foreach (var targetGroup in groupList)
                {
                    if (IsInGroup(system, targetGroup.Item1))
                        targetGroup.Item2.AddSystemToUpdateList(world.GetOrCreateSystem(system));
                }
            }

            foreach (var group in groupList)
            {
                if (group.Item1 == typeof(T))
                {
                    group.Item2.SortSystems();
                    return group.Item2 as T;
                }
            }

            return null;
        }

        /// <summary>
        /// Is the system a type of ComponentSystemGroup?
        /// </summary>
        public static bool IsGroup(Type systemType)
        {
            return typeof(ComponentSystemGroup).IsAssignableFrom(systemType);
        }

        /// <summary>
        /// Does the system want to be injected in the group?
        /// </summary>
        /// <param name="systemType">The type of system to be injected</param>
        /// <param name="groupType">The type of group that would be specified in the UpdateInGroupAttribute</param>
        /// <returns></returns>
        public static bool IsInGroup(Type systemType, Type groupType)
        {
            var atts = systemType.GetCustomAttributes(typeof(UpdateInGroupAttribute), true);
            foreach (var att in atts)
            {
                if (!(att is UpdateInGroupAttribute uig))
                    continue;
                if (uig.GroupType.IsAssignableFrom(groupType))
                {
                    return true;
                }
            }
            return false;
        }

        private static void AddChildrenGroupsToHashsetRecursively(Type startType, List<Type> componentSystemGroups, HashSet<Type> foundGroups)
        {
            if (!foundGroups.Contains(startType))
            {
                foundGroups.Add(startType);
            }
            foreach (var system in componentSystemGroups)
            {
                if (!foundGroups.Contains(system) && IsInGroup(system, startType))
                    AddChildrenGroupsToHashsetRecursively(system, componentSystemGroups, foundGroups);
            }
        }
    }
}

