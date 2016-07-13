using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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


            //Type iIndexableGrainType = Type.GetType("Orleans.Indexing.IIndexableGrain, OrleansIndexing");
            Type genericIIndexableGrainType = Type.GetType("Orleans.Indexing.IIndexableGrain`1" + AssemblySeparator + OrleansIndexingAssembly);
            Type indexAttributeType = Type.GetType("Orleans.Indexing.IndexAttribute" + AssemblySeparator + OrleansIndexingAssembly);
            Type indexFactoryType = Type.GetType("Orleans.Indexing.IndexFactory" + AssemblySeparator + OrleansIndexingAssembly);
            var createIndexMethod = (Func<IGrainFactory, Type, string, PropertyInfo, Tuple <object,object,object>>) Delegate.CreateDelegate(
                                    typeof(Func<IGrainFactory, Type, string, PropertyInfo, Tuple<object, object, object>>), 
                                    indexFactoryType.GetMethod("CreateIndex", BindingFlags.Static | BindingFlags.NonPublic));
            Type genericDefaultIndexType = Type.GetType("Orleans.Indexing.IHashIndexSingleBucket`2" + AssemblySeparator + OrleansIndexingAssembly);

            //for all discovered grain types
            foreach (var grainType in grainTypes)
            {
                if (result.ContainsKey(grainType))
                    throw new InvalidOperationException(
                        string.Format("Precondition violated: GetLoadedGrainTypes should not return a duplicate type ({0})", TypeUtils.GetFullName(grainType)));

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
                                //checks whether the given interface is a user-defined
                                //interface extending IIndexable<TProperties>
                                if (iIndexableGrain != userDefinedIGrain && iIndexableGrain.IsAssignableFrom(userDefinedIGrain) && !result.ContainsKey(userDefinedIGrain))
                                {
                                    IDictionary<string, Tuple<object, object, object>> indexesOnGrain = new Dictionary<string, Tuple<object, object, object>>();
                                    //all the properties in TProperties are scanned for Index
                                    //annotation and the index is created using the information
                                    //provided in the annotation
                                    foreach (PropertyInfo p in propertiesArg.GetProperties())
                                    {
                                        var indexAttrs = p.GetCustomAttributes(indexAttributeType, false);
                                        if (indexAttrs.Length > 0)
                                        {
                                            string indexName = "__" + p.Name;
                                            Type indexType = (Type)indexAttributeType.GetProperty("IndexType").GetValue(indexAttrs[0]);
                                            if (indexType.IsGenericTypeDefinition)
                                            {
                                                indexType = indexType.MakeGenericType(p.PropertyType, userDefinedIGrain);
                                            }
                                            indexesOnGrain.Add(indexName, createIndexMethod(gfactory, indexType, indexName, p));
                                        }
                                    }
                                    result.Add(userDefinedIGrain, indexesOnGrain);
                                }
                            }
                        }
                        break;
                    }
                }
            }
            return result;
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
                    var first = true;

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
