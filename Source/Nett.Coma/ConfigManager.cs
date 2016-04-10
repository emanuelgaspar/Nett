using System;
using System.Linq;
using Castle.DynamicProxy;

namespace Nett.Coma
{
    public static partial class ConfigManager
    {
        private static readonly ProxyGenerator proxyGenerator = new ProxyGenerator();

        public static T Setup<T>(string path, Func<T> createInitial) where T : class
            => Setup<T>(path, createInitial, TomlConfig.DefaultInstance);

        public static T Setup<T>(string path, Func<T> createInitial, TomlConfig config) where T : class
        {
            var mc = new ManagedConfig<T>(path, config);
            var interceptor = new RootInterceptor<T>(mc, createInitial);

            var proxy = proxyGenerator.CreateClassProxy<T>(interceptor);
            GenerateSubProxies(proxy, interceptor, interceptor, mc);
            interceptor.Activate();
            return proxy;
        }

        private static void GenerateSubProxies<T>(object parentProxy, RootInterceptor<T> root, Interceptor parent, ManagedConfig<T> managedConfig) where T : class
        {
            foreach (var p in parentProxy.GetType().GetProperties())
            {
                if (managedConfig.TomlConfig.GetAllToTomlConverters(p.PropertyType).FirstOrDefault() == null)
                {
                    var interceptor = new RootInterceptor<T>.SubInterceptor(root, parent, p.Name);
                    var subProxy = proxyGenerator.CreateClassProxy(p.PropertyType, interceptor);
                    GenerateSubProxies(subProxy, root, interceptor, managedConfig);
                    p.SetValue(parentProxy, subProxy, null);
                    interceptor.Activate();
                }
            }
        }

        private class ManagedConfig<T>
        {
            private readonly ISaveLoad<T> saveAndLoad;

            public TomlConfig TomlConfig { get; }
            //public object ConfigObjectRoot { get; set; }

            public ManagedConfig(string filePath, TomlConfig tomlConfig)
            {
                this.TomlConfig = tomlConfig;
                this.saveAndLoad = new SingleFileSaveAndLoad<T>(filePath, tomlConfig);
            }

            public bool CanLoad() => this.saveAndLoad.CanLoad();

            public void Save(T toSave) => this.saveAndLoad.Save(toSave);

            public T Load() => this.saveAndLoad.Load();
        }
    }
}
