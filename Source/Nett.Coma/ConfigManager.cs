using System;
using System.Collections.Generic;
using Castle.DynamicProxy;

namespace Nett.Coma
{
    public static class ConfigManager
    {
        private static readonly Dictionary<Type, ManagedConfig> configs = new Dictionary<Type, ManagedConfig>();

        private static readonly ProxyGenerator proxyGenerator = new ProxyGenerator();

        public static T Setup<T>(ConfigScope rootScope) where T : class, new()
        {
            var interceptor = new Interceptor();
            var proxy = proxyGenerator.CreateClassProxy<T>(interceptor);
            configs[typeof(T)] = new ManagedConfig(rootScope);
            return proxy;
        }

        private class Interceptor : IInterceptor
        {
            private bool autoSaveLoadDisabled = false;

            void IInterceptor.Intercept(IInvocation invocation)
            {
                if (invocation.Method.Name.StartsWith("set_"))
                {
                    invocation.Proceed();
                    if (!this.autoSaveLoadDisabled)
                    {
                        this.Save(invocation);
                    }

                }
                else if (invocation.Method.Name.StartsWith("get_"))
                {
                    if (!this.autoSaveLoadDisabled)
                    {
                        this.Load(invocation);
                    }

                    invocation.Proceed();
                }
            }

            private void Save(IInvocation invocation)
            {
                using (new DisableAutoSaveLoadContext(this))
                {
                    var mc = configs[invocation.TargetType];
                    Toml.WriteFile(invocation.InvocationTarget, mc.RootScope.ConfigFilePath);
                }
            }

            private void Load(IInvocation invocation)
            {
                using (new DisableAutoSaveLoadContext(this))
                {
                    var mc = configs[invocation.TargetType];
                    var table = Toml.ReadFile(mc.RootScope.ConfigFilePath);
                    var prpName = invocation.Method.Name.Substring("get_".Length);
                    var prop = invocation.InvocationTarget.GetType().GetProperty(prpName);
                    prop.SetValue(invocation.InvocationTarget, table.Get(prpName).Get(prop.PropertyType), null);
                }
            }

            private class DisableAutoSaveLoadContext : IDisposable
            {
                private readonly Interceptor interceptor;

                public DisableAutoSaveLoadContext(Interceptor interceptor)
                {
                    this.interceptor = interceptor;
                    this.interceptor.autoSaveLoadDisabled = true;
                }

                public void Dispose()
                {
                    this.interceptor.autoSaveLoadDisabled = false;
                }
            }
        }

        private class ManagedConfig
        {
            public ConfigScope RootScope { get; }

            public ManagedConfig(ConfigScope rootScope)
            {
                this.RootScope = rootScope;
            }
        }
    }
}
