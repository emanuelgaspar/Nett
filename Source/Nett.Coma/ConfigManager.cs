﻿using System;
using System.Linq;
using Castle.DynamicProxy;

namespace Nett.Coma
{
    public static partial class ConfigManager
    {
        private static readonly ProxyGenerator proxyGenerator = new ProxyGenerator();

        public static T Setup<T>(string path, Func<T> createInitial) where T : class, new()
            => Setup<T>(path, createInitial, TomlConfig.DefaultInstance);

        public static T Setup<T>(string path, Func<T> createInitial, TomlConfig config) where T : class, new()
        {
            var mc = new ManagedConfig(path, config);
            var interceptor = new RootInterceptor<T>(mc, createInitial);
            interceptor.Init();

            var proxy = proxyGenerator.CreateClassProxy<T>(interceptor);
            GenerateSubProxies(proxy, interceptor, interceptor, mc);
            return proxy;
        }

        private static void GenerateSubProxies<T>(object parentProxy, RootInterceptor<T> root, InterceptorBase parent, ManagedConfig managedConfig) where T : class, new()
        {
            foreach (var p in parentProxy.GetType().GetProperties())
            {
                if (managedConfig.TomlConfig.GetAllToTomlConverters(p.PropertyType).FirstOrDefault() == null)
                {
                    var interceptor = new RootInterceptor<T>.Interceptor(root, parent, p.Name);
                    var subProxy = proxyGenerator.CreateClassProxy(p.PropertyType, new RootInterceptor<T>.Interceptor(root, parent, p.Name));
                    GenerateSubProxies(subProxy, root, interceptor, managedConfig);

                    p.SetValue(parentProxy, subProxy, null);
                }
            }
        }

        private class ManagedConfig
        {
            public string FilePath { get; }
            public TomlConfig TomlConfig { get; }
            public object ConfigObjectRoot { get; set; }

            public ManagedConfig(string filePath, TomlConfig tomlConfig)
            {
                this.FilePath = filePath;
                this.TomlConfig = tomlConfig;
            }
        }
    }
}
