using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Nett
{
    public sealed partial class TomlConfig
    {
        public IConfigureTypeStart<T> ConfigureType<T>() => new TypeConfigurator<T>(this);

        public interface IConfigureTypeStart<T>
        {
            IConfigureType<T> As { get; }
        }

        public interface IConfigureTypeCombiner<T>
        {
            IConfigureType<T> And { get; }

            TomlConfig Apply();
        }

        public interface IConfigureType<T>
        {
            IConfigureTypeCombiner<T> CreateWith(Func<T> activator);

            IConfigureCast<T, TFrom, T> ConvertFrom<TFrom>() where TFrom : TomlObject;
            IConfigureCast<T, T, TTo> ConvertTo<TTo>() where TTo : TomlObject;

            IConfigureTypeCombiner<T> TreatAsInlineTable();
        }

        public interface IConfigureCast<T, TFrom, TTo>
        {
            IConfigureTypeCombiner<T> As(Func<TFrom, TTo> cast);
        }

        public class TypeConfigurator<T> : IConfigureType<T>, IConfigureTypeCombiner<T>, IConfigureTypeStart<T>
        {
            private readonly TomlConfig config;

            public TypeConfigurator(TomlConfig config)
            {
                Debug.Assert(config != null);

                this.config = config;
            }

            public IConfigureType<T> And => this;

            public IConfigureType<T> As => this;

            public IConfigureTypeCombiner<T> CreateWith(Func<T> activator)
            {
                if (activator == null) { throw new ArgumentNullException("activator"); }
                this.config.activators.Add(typeof(T), () => activator());
                return this;
            }

            public IConfigureCast<T, TFrom, T> ConvertFrom<TFrom>() where TFrom : TomlObject => new FromTomlCastConfigurator<T, TFrom, T>(this.config, this);

            public IConfigureCast<T, T, TTo> ConvertTo<TTo>() where TTo : TomlObject => new ToTomlCastConfigurator<T, T, TTo>(this.config, this);

            public IConfigureTypeCombiner<T> TreatAsInlineTable()
            {
                config.inlineTableTypes.Add(typeof(T));
                return this;
            }

            public TomlConfig Apply() => this.config;
        }

        internal IEnumerable<IToTomlConverter> GetAllToTomlConverters(Type from) =>
            this.toTomlConverters.Where(c => c.FromType == from);

        private abstract class CastConfigurator<T>
        {
            protected readonly TomlConfig config;
            protected readonly TypeConfigurator<T> typeConfig;

            public CastConfigurator(TomlConfig config, TypeConfigurator<T> typeConfig)
            {
                Debug.Assert(config != null);
                Debug.Assert(typeConfig != null);

                this.config = config;
                this.typeConfig = typeConfig;
            }
        }

        private class ToTomlCastConfigurator<T, TFrom, TTo> : CastConfigurator<T>, IConfigureCast<T, TFrom, TTo> where TTo : TomlObject
        {
            public ToTomlCastConfigurator(TomlConfig config, TypeConfigurator<T> typeConfig) :
                base(config, typeConfig)
            {
            }

            public IConfigureTypeCombiner<T> As(Func<TFrom, TTo> cast)
            {
                var conv = new ToTomlConverter<TFrom, TTo>(cast);
                config.toTomlConverters.Add(conv);
                return this.typeConfig;
            }
        }

        private class FromTomlCastConfigurator<T, TFrom, TTo> : CastConfigurator<T>, IConfigureCast<T, TFrom, TTo> where TFrom : TomlObject
        {
            public FromTomlCastConfigurator(TomlConfig config, TypeConfigurator<T> typeConfig)
                : base(config, typeConfig)
            {
            }

            public IConfigureTypeCombiner<T> As(Func<TFrom, TTo> cast)
            {
                var conv = new FromTomlConverter<TFrom, TTo>(cast);
                config.fromTomlConverters.Add(conv);
                return this.typeConfig;
            }
        }
    }
}
