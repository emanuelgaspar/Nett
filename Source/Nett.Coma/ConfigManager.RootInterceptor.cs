using System;
using System.IO;
using Castle.DynamicProxy;
using static System.Diagnostics.Debug;

namespace Nett.Coma
{
    public static partial class ConfigManager
    {
        private abstract class InterceptorBase : IInterceptor
        {
            public abstract object ConfigObject { get; }
            protected bool autoSaveLoadDisabled = false;

            public void Intercept(IInvocation invocation)
            {
                if (invocation.Method.Name.StartsWith("set_"))
                {
                    var prop = this.ConfigObject.GetType().GetProperty(invocation.Method.Name.Substring("set_".Length));
                    prop.SetValue(this.ConfigObject, invocation.GetArgumentValue(0), null);

                    if (!this.autoSaveLoadDisabled)
                    {
                        this.Save();
                    }

                }
                else if (invocation.Method.Name.StartsWith("get_"))
                {
                    if (!this.autoSaveLoadDisabled)
                    {
                        this.Load();
                    }

                    var prop = this.ConfigObject.GetType().GetProperty(invocation.Method.Name.Substring("get_".Length));
                    var value = prop.GetValue(this.ConfigObject, null);
                    invocation.ReturnValue = value;
                }
            }

            public object GetConfigObjectValue(string propertyName) =>
                this.ConfigObject.GetType().GetProperty(propertyName).GetValue(this.ConfigObject, null);

            public abstract void Save();
            public abstract void Load();
        }


        private class RootInterceptor<T> : InterceptorBase
        {
            private readonly ManagedConfig managedConfig;
            private T configObject;
            public override object ConfigObject => this.configObject;

            public RootInterceptor(ManagedConfig managedConfig, Func<T> createConfigObject)
            {
                Assert(managedConfig != null);
                Assert(createConfigObject != null);

                this.managedConfig = managedConfig;
                this.configObject = createConfigObject();
            }

            public void Init() => this.Load();

            public override void Save()
            {
                using (new DisableAutoSaveLoadContext(this))
                {
                    Toml.WriteFile(this.configObject, this.managedConfig.FilePath, this.managedConfig.TomlConfig);
                }
            }

            public override void Load()
            {
                if (File.Exists(this.managedConfig.FilePath))
                {
                    using (new DisableAutoSaveLoadContext(this))
                    {
                        this.configObject = Toml.ReadFile<T>(this.managedConfig.FilePath, this.managedConfig.TomlConfig);
                    }
                }
            }

            public class Interceptor : InterceptorBase
            {
                private readonly RootInterceptor<T> root;
                private readonly InterceptorBase parent;
                private readonly string configObjectPropertyName;

                public override object ConfigObject => this.parent.ConfigObject.GetType().GetProperty(this.configObjectPropertyName).GetValue(this.parent.ConfigObject, null);

                public Interceptor(RootInterceptor<T> root, InterceptorBase parent, string propertyName)
                {
                    Assert(root != null);
                    Assert(parent != null);
                    Assert(!string.IsNullOrWhiteSpace(propertyName));

                    this.root = root;
                    this.parent = parent;
                    this.configObjectPropertyName = propertyName;
                }

                public override void Load() => this.root.Load();

                public override void Save() => this.root.Save();
            }

            private class DisableAutoSaveLoadContext : IDisposable
            {
                private readonly RootInterceptor<T> interceptor;

                public DisableAutoSaveLoadContext(RootInterceptor<T> interceptor)
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
    }
}
