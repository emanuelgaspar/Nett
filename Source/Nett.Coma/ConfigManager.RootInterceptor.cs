using System;
using static System.Diagnostics.Debug;

namespace Nett.Coma
{
    public static partial class ConfigManager
    {
        private class RootInterceptor<T> : Interceptor
        {
            private readonly ManagedConfig<T> managedConfig;
            private T configObject;
            public override object ConfigObject => this.configObject;

            public RootInterceptor(ManagedConfig<T> managedConfig, Func<T> createConfigObject)
            {
                Assert(managedConfig != null);
                Assert(createConfigObject != null);

                this.managedConfig = managedConfig;
                this.configObject = createConfigObject();
            }

            public override void Activate()
            {
                this.Load();
                base.Activate();
            }

            public override void Save()
            {
                using (new DisableAutoSaveLoadContext(this))
                {
                    this.managedConfig.Save(this.configObject);
                }
            }

            public override void Load()
            {
                if (this.managedConfig.CanLoad())
                {
                    using (new DisableAutoSaveLoadContext(this))
                    {
                        this.configObject = this.managedConfig.Load();
                    }
                }
            }

            public class SubInterceptor : Interceptor
            {
                private readonly RootInterceptor<T> root;
                private readonly Interceptor parent;
                private readonly string configObjectPropertyName;

                public override object ConfigObject => this.parent.ConfigObject.GetType().GetProperty(this.configObjectPropertyName).GetValue(this.parent.ConfigObject, null);

                public SubInterceptor(RootInterceptor<T> root, Interceptor parent, string propertyName)
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
