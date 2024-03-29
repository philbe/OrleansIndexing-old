using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Orleans.CodeGeneration;
using Orleans.Providers;

namespace Orleans.Runtime
{
    internal class SiloAssemblyLoader
    {
        private readonly LoggerImpl logger = LogManager.GetLogger("AssemblyLoader.Silo");
        private List<string> discoveredAssemblyLocations;
        private Dictionary<string, SearchOption> directories;

        public SiloAssemblyLoader(IDictionary<string, SearchOption> additionalDirectories)
        {
            var exeRoot = Path.GetDirectoryName(typeof(SiloAssemblyLoader).GetTypeInfo().Assembly.Location);
            var appRoot = Path.Combine(exeRoot, "Applications");
            var cwd = Directory.GetCurrentDirectory();

            directories = new Dictionary<string, SearchOption>
                    {
                        { exeRoot, SearchOption.TopDirectoryOnly },
                        { appRoot, SearchOption.AllDirectories }
                    };

            foreach (var kvp in additionalDirectories)
            {
                // Make sure the path is clean (get rid of ..\'s)
                directories[new DirectoryInfo(kvp.Key).FullName] = kvp.Value;
            }


            if (!directories.ContainsKey(cwd))
            {
                directories.Add(cwd, SearchOption.TopDirectoryOnly);
            }

            LoadApplicationAssemblies();
        }

        private void LoadApplicationAssemblies()
        {
            AssemblyLoaderPathNameCriterion[] excludeCriteria =
                {
                    AssemblyLoaderCriteria.ExcludeResourceAssemblies,
                    AssemblyLoaderCriteria.ExcludeSystemBinaries()
                };
            AssemblyLoaderReflectionCriterion[] loadCriteria =
                {
                    AssemblyLoaderReflectionCriterion.NewCriterion(
                        TypeUtils.IsConcreteGrainClass,
                        "Assembly does not contain any acceptable grain types."),
                    AssemblyLoaderCriteria.LoadTypesAssignableFrom(
                        typeof(IProvider))
                };

            discoveredAssemblyLocations = AssemblyLoader.LoadAssemblies(directories, excludeCriteria, loadCriteria, logger);
        }

        public IDictionary<string, GrainTypeData> GetGrainClassTypes(bool strict)
        {
            var result = new Dictionary<string, GrainTypeData>();
            Type[] grainTypes = strict
                ? TypeUtils.GetTypes(TypeUtils.IsConcreteGrainClass, logger).ToArray()
                : TypeUtils.GetTypes(discoveredAssemblyLocations, TypeUtils.IsConcreteGrainClass, logger).ToArray();

            foreach (var grainType in grainTypes)
            {
                var className = TypeUtils.GetFullName(grainType);
                if (result.ContainsKey(className))
                    throw new InvalidOperationException(
                        string.Format("Precondition violated: GetLoadedGrainTypes should not return a duplicate type ({0})", className));

                Type grainStateType = null;

                // check if grainType derives from Grai<nT> where T is a concrete class

                var parentType = grainType.GetTypeInfo().BaseType;
                while (parentType != typeof(Grain) && parentType != typeof(object))
                {
                    TypeInfo parentTypeInfo = parentType.GetTypeInfo();
                    if (parentTypeInfo.IsGenericType)
                    {
                        var definition = parentTypeInfo.GetGenericTypeDefinition();
                        if (definition == typeof(Grain<>))
                        {
                            var stateArg = parentType.GetGenericArguments()[0];
                            if (stateArg.GetTypeInfo().IsClass)
                            {
                                grainStateType = stateArg;
                                break;
                            }
                        }
                    }

                    parentType = parentTypeInfo.BaseType;
                }

                GrainTypeData typeData = GetTypeData(grainType, grainStateType);
                result.Add(className, typeData);
            }

            LogGrainTypesFound(logger, result);
            return result;
        }

        public static string OrleansIndexingAssembly = "OrleansIndexing";
        public static string AssemblySeparator = ", ";

        //private static Type iIndexableGrainType = Type.GetType("Orleans.Indexing.IIndexableGrain, OrleansIndexing");
        private static Type genericIIndexableGrainType = Type.GetType("Orleans.Indexing.IIndexableGrain`1" + AssemblySeparator + OrleansIndexingAssembly);
        private static Type genericFaultTolerantIndexableGrainType = Type.GetType("Orleans.Indexing.IndexableGrain`1" + AssemblySeparator + OrleansIndexingAssembly);
        private static Type indexAttributeType = Type.GetType("Orleans.Indexing.IndexAttribute" + AssemblySeparator + OrleansIndexingAssembly);
        private static PropertyInfo indexTypeProperty = indexAttributeType.GetProperty("IndexType");
        private static Type indexFactoryType = Type.GetType("Orleans.Indexing.IndexFactory" + AssemblySeparator + OrleansIndexingAssembly);
        private static Func<IGrainFactory, Type, string, bool, bool, PropertyInfo, Tuple<object, object, object>> createIndexMethod = (Func<IGrainFactory, Type, string, bool, bool, PropertyInfo, Tuple<object, object, object>>)Delegate.CreateDelegate(
                                typeof(Func<IGrainFactory, Type, string, bool, bool, PropertyInfo, Tuple<object, object, object>>),
                                indexFactoryType.GetMethod("CreateIndex", BindingFlags.Static | BindingFlags.NonPublic));
        private static Action<Type, Type> registerIndexWorkflowQueuesMethod = (Action<Type, Type>)Delegate.CreateDelegate(
                                typeof(Action<Type, Type>),
                                indexFactoryType.GetMethod("RegisterIndexWorkflowQueues", BindingFlags.Static | BindingFlags.NonPublic));
        private static PropertyInfo isEagerProperty = indexAttributeType.GetProperty("IsEager");
        private static PropertyInfo isUniqueProperty = indexAttributeType.GetProperty("IsUnique");
        private static Type initializedIndexType = Type.GetType("Orleans.Indexing.InitializedIndex" + AssemblySeparator + OrleansIndexingAssembly);

        /// <summary>
        /// This method crawls the assemblies and looks for the index
        /// definitions (determined by extending IIndexable{TProperties}
        /// interface and adding annotations to properties in TProperties).
        /// 
        /// In order to avoid having any dependency on OrleansIndexing
        /// project, all the required types are loaded via reflection.
        /// </summary>
        /// <param name="strict">determines the lookup strategy for
        /// looking into the assemblies</param>
        /// <param name="gfactory">the instance of grain factory</param>
        /// <returns></returns>
        public IDictionary<Type, IDictionary<string, Tuple<object, object, object>>> GetGrainClassIndexes(bool strict, IGrainFactory gfactory)
        {
            var result = new Dictionary<Type, IDictionary<string, Tuple<object, object, object>>>();
            Type[] grainTypes = strict
                ? TypeUtils.GetTypes(TypeUtils.IsConcreteGrainClass, logger).ToArray()
                : TypeUtils.GetTypes(discoveredAssemblyLocations, TypeUtils.IsConcreteGrainClass, logger).ToArray();

            //for all discovered grain types
            foreach (var grainType in grainTypes)
            {
                if (result.ContainsKey(grainType))
                    throw new InvalidOperationException(
                        string.Format("Precondition violated: GetLoadedGrainTypes should not return a duplicate type ({0})", TypeUtils.GetFullName(grainType)));
                GetIndexesForASingleGrainType(gfactory, result, grainType);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GetIndexesForASingleGrainType(IGrainFactory gfactory, Dictionary<Type, IDictionary<string, Tuple<object, object, object>>> result, Type grainType)
        {
            Type[] interfaces = grainType.GetInterfaces();
            int numInterfaces = interfaces.Length;

            //iterate over the interfaces of the grain type
            for (int i = 0; i < numInterfaces; ++i)
            {
                Type iIndexableGrain = interfaces[i];

                //if the interface extends IIndexable<TProperties> interface
                if (iIndexableGrain.IsGenericType && iIndexableGrain.GetGenericTypeDefinition() == genericIIndexableGrainType)
                {
                    Type propertiesArg = iIndexableGrain.GetGenericArguments()[0];
                    //and if TProperties is a class
                    if (propertiesArg.GetTypeInfo().IsClass)
                    {
                        //then, the indexes are added to all the descendant
                        //interfaces of IIndexable<TProperties>, which are
                        //defined by end-users
                        for (int j = 0; j < numInterfaces; ++j)
                        {
                            Type userDefinedIGrain = interfaces[j];
                            CreateIndexesForASingleInterfaceOfAGrainType(gfactory, result, iIndexableGrain, propertiesArg, userDefinedIGrain, grainType);
                        }
                    }
                    break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CreateIndexesForASingleInterfaceOfAGrainType(IGrainFactory gfactory, Dictionary<Type, IDictionary<string, Tuple<object, object, object>>> result, Type iIndexableGrain, Type propertiesArg, Type userDefinedIGrain, Type userDefinedGrainImpl)
        {
            //checks whether the given interface is a user-defined
            //interface extending IIndexable<TProperties>
            if (iIndexableGrain != userDefinedIGrain && iIndexableGrain.IsAssignableFrom(userDefinedIGrain) && !result.ContainsKey(userDefinedIGrain))
            {
                //check either all indexes are defined as lazy
                //or all indexes are defined as lazy and none of them
                //are I-Index, because I-Indexes cannot be lazy
                CheckAllIndexesAreEitherLazyOrEager(propertiesArg, userDefinedIGrain, userDefinedGrainImpl);

                IDictionary<string, Tuple<object, object, object>> indexesOnGrain = new Dictionary<string, Tuple<object, object, object>>();
                //all the properties in TProperties are scanned for Index
                //annotation and the index is created using the information
                //provided in the annotation
                bool isEagerlyUpdated = true;
                foreach (PropertyInfo p in propertiesArg.GetProperties())
                {
                    var indexAttrs = p.GetCustomAttributes(indexAttributeType, false);
                    foreach (var indexAttr in indexAttrs)
                    {
                        string indexName = "__" + p.Name;
                        Type indexType = (Type)indexTypeProperty.GetValue(indexAttr);
                        if (indexType.IsGenericTypeDefinition)
                        {
                            indexType = indexType.MakeGenericType(p.PropertyType, userDefinedIGrain);
                        }

                        //if it's not eager, then it's configured to be lazily updated
                        isEagerlyUpdated = (bool)isEagerProperty.GetValue(indexAttr);
                        bool isUnique = (bool)isUniqueProperty.GetValue(indexAttr);
                        indexesOnGrain.Add(indexName, createIndexMethod(gfactory, indexType, indexName, isUnique, isEagerlyUpdated, p));

                    }
                }
                result.Add(userDefinedIGrain, indexesOnGrain);
                if (!isEagerlyUpdated)
                {
                    registerIndexWorkflowQueuesMethod(userDefinedIGrain, userDefinedGrainImpl);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckAllIndexesAreEitherLazyOrEager(Type propertiesArg, Type userDefinedIGrain, Type userDefinedGrainImpl)
        {
            bool isFaultTolerant = TypeUtils.IsSubclassOfRawGenericType(genericFaultTolerantIndexableGrainType, userDefinedGrainImpl);
            foreach (PropertyInfo p in propertiesArg.GetProperties())
            {
                var indexAttrs = p.GetCustomAttributes(indexAttributeType, false);
                bool isFirstIndexEager = false;
                if (indexAttrs.Count() > 0)
                {
                    isFirstIndexEager = (bool)isEagerProperty.GetValue(indexAttrs[0]);
                }
                foreach (var indexAttr in indexAttrs)
                {
                    bool isEager = (bool)isEagerProperty.GetValue(indexAttr);
                    Type indexType = (Type)indexTypeProperty.GetValue(indexAttr);
                    bool isIIndex = initializedIndexType.IsAssignableFrom(indexType);

                    //I-Index cannot be configured as being lazy
                    if (isIIndex && isEager)
                    {
                        throw new InvalidOperationException(string.Format("An I-Index cannot be configured to be updated eagerly. The only option for updating an I-Index is lazy updating. I-Index of type {0} is defined to be updated eagerly on property {1} of class {2} on {3} grain interface.", TypeUtils.GetFullName(indexType), p.Name, TypeUtils.GetFullName(propertiesArg), TypeUtils.GetFullName(userDefinedIGrain)));
                    }
                    else if(isFaultTolerant && isEager)
                    {
                        throw new InvalidOperationException(string.Format("A fault-tolerant grain implementation cannot be configured to eagerly update its indexes. The only option for updating the indexes of a fault-tolerant indexable grain is lazy updating. The index of type {0} is defined to be updated eagerly on property {1} of class {2} on {3} grain implementation class.", TypeUtils.GetFullName(indexType), p.Name, TypeUtils.GetFullName(propertiesArg), TypeUtils.GetFullName(userDefinedGrainImpl)));
                    }
                    else if (isEager != isFirstIndexEager)
                    {
                        throw new InvalidOperationException(string.Format("Some indexes on property class {0} of {1} grain interface are defined to be updated eagerly while others are configured as lazy updating. You should fix this by configuring all indexes to be updated lazily or eagerly. If you have at least one I-Index among your indexes, then all other indexes should be configured as lazy, too.", TypeUtils.GetFullName(propertiesArg), TypeUtils.GetFullName(userDefinedIGrain)));
                    }
                }
            }
        }

        public IEnumerable<KeyValuePair<int, Type>> GetGrainMethodInvokerTypes(bool strict)
        {
            var result = new Dictionary<int, Type>();
            Type[] types = strict
                ? TypeUtils.GetTypes(TypeUtils.IsGrainMethodInvokerType, logger).ToArray()
                : TypeUtils.GetTypes(discoveredAssemblyLocations, TypeUtils.IsGrainMethodInvokerType, logger).ToArray();

            foreach (var type in types)
            {
                var attrib = type.GetTypeInfo().GetCustomAttribute<MethodInvokerAttribute>(true);
                int ifaceId = attrib.InterfaceId;

                if (result.ContainsKey(ifaceId))
                    throw new InvalidOperationException(string.Format("Grain method invoker classes {0} and {1} use the same interface id {2}", result[ifaceId].FullName, type.FullName, ifaceId));

                result[ifaceId] = type;
            }
            return result;
        }

        /// <summary>
        /// Get type data for the given grain type
        /// </summary>
        private static GrainTypeData GetTypeData(Type grainType, Type stateObjectType)
        {
            return grainType.GetTypeInfo().IsGenericTypeDefinition ?
                new GenericGrainTypeData(grainType, stateObjectType) :
                new GrainTypeData(grainType, stateObjectType);
        }

        private static void LogGrainTypesFound(LoggerImpl logger, Dictionary<string, GrainTypeData> grainTypeData)
        {
            var sb = new StringBuilder();
            sb.AppendLine(String.Format("Loaded grain type summary for {0} types: ", grainTypeData.Count));

            foreach (var grainType in grainTypeData.Values.OrderBy(gtd => gtd.Type.FullName))
            {
                // Skip system targets and Orleans grains
                var assemblyName = grainType.Type.GetTypeInfo().Assembly.FullName.Split(',')[0];
                if (!typeof(ISystemTarget).IsAssignableFrom(grainType.Type))
                {
                    int grainClassTypeCode = CodeGeneration.GrainInterfaceUtils.GetGrainClassTypeCode(grainType.Type);
                    sb.AppendFormat("Grain class {0}.{1} [{2} (0x{3})] from {4}.dll implementing interfaces: ",
                        grainType.Type.Namespace,
                        TypeUtils.GetTemplatedName(grainType.Type),
                        grainClassTypeCode,
                        grainClassTypeCode.ToString("X"),
                        assemblyName);
                    bool first = true;

                    foreach (var iface in grainType.RemoteInterfaceTypes)
                    {
                        if (!first)
                            sb.Append(", ");

                        sb.Append(iface.Namespace).Append(".").Append(TypeUtils.GetTemplatedName(iface));

                        if (CodeGeneration.GrainInterfaceUtils.IsGrainType(iface))
                        {
                            int ifaceTypeCode = CodeGeneration.GrainInterfaceUtils.GetGrainInterfaceId(iface);
                            sb.AppendFormat(" [{0} (0x{1})]", ifaceTypeCode, ifaceTypeCode.ToString("X"));
                        }
                        first = false;
                    }
                    sb.AppendLine();
                }
            }
            var report = sb.ToString();
            logger.LogWithoutBulkingAndTruncating(Severity.Info, ErrorCode.Loader_GrainTypeFullList, report);
        }
    }
}
