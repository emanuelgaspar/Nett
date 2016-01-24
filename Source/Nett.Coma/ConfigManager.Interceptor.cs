using Castle.DynamicProxy;

namespace Nett.Coma
{
    public static partial class ConfigManager
    {
        private abstract class Interceptor : IInterceptor
        {
            private bool active = false;
            public abstract object ConfigObject { get; }
            protected bool autoSaveLoadDisabled = false;

            public virtual void Activate() => this.active = true;

            public void Intercept(IInvocation invocation)
            {
                if (!active)
                {
                    return;
                }

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
    }
}
