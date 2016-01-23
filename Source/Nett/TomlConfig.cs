using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nett
{
    enum TomlCommentLocation
    {
        Prepend,
        Append,
    }

    public sealed partial class TomlConfig
    {
        internal static readonly TomlConfig DefaultInstance = Create();

        private readonly List<IToTomlConverter> toTomlConverters = new List<IToTomlConverter>();
        private readonly List<IFromTomlConverter> fromTomlConverters = new List<IFromTomlConverter>();
        private readonly HashSet<Type> inlineTableTypes = new HashSet<Type>();
        private readonly Dictionary<Type, Func<object>> activators = new Dictionary<Type, Func<object>>();

        private TomlCommentLocation DefaultCommentLocation = TomlCommentLocation.Prepend;
        private TomlConfig()
        {
            this.AddStandardTomlConverters();
        }

        private void AddStandardTomlConverters()
        {
            // Do not change the order of operations here, registrations that happen first are considered the better conversion
            // There are code issues that cannot specify both types, in such cases the choose the best one by taking the first found in the list.
            // Generic converters (needed by the from methods that generically transform CLR objects to the best equivalent TOML object)
            this.AddToTomlConverter<int, TomlValue>(i => new TomlInt(i));
            this.AddToTomlConverter<long, TomlValue>(l => new TomlInt(l));
            this.AddToTomlConverter<float, TomlValue>(f => new TomlFloat(f));
            this.AddToTomlConverter<double, TomlValue>(d => new TomlFloat(d));
            this.AddToTomlConverter<string, TomlValue>(s => new TomlString(s));
            this.AddToTomlConverter<DateTime, TomlValue>(dt => new TomlDateTime(dt));
            this.AddToTomlConverter<TimeSpan, TomlValue>(ts => new TomlTimeSpan(ts));

            // TomlInt
            this.AddBidirectionalConverter<TomlInt, int>(t => (int)t.Value, v => new TomlInt(v));
            this.AddBidirectionalConverter<TomlInt, long>(t => t.Value, v => new TomlInt(v));
            this.AddBidirectionalConverter<TomlInt, char>(t => (char)t.Value, v => new TomlInt(v));
            this.AddBidirectionalConverter<TomlInt, byte>(t => (byte)t.Value, v => new TomlInt(v));
            this.AddBidirectionalConverter<TomlInt, short>(t => (short)t.Value, v => new TomlInt(v));

            // TomlFloat
            this.AddBidirectionalConverter<TomlFloat, double>(t => t.Value, v => new TomlFloat(v));
            this.AddBidirectionalConverter<TomlFloat, float>(t => (float)t.Value, v => new TomlFloat(v));

            // TomlString
            this.AddBidirectionalConverter<TomlString, string>(t => t.Value, v => new TomlString(v));

            // TomlDateTime
            this.AddBidirectionalConverter<TomlDateTime, DateTime>(t => t.Value.UtcDateTime, v => new TomlDateTime(v));
            this.AddBidirectionalConverter<TomlDateTime, DateTimeOffset>(t => t.Value, v => new TomlDateTime(v));

            // TomlTimeSpan
            this.AddBidirectionalConverter<TomlTimeSpan, TimeSpan>(t => t.Value, v => new TomlTimeSpan(v));

            // TomlBool
            this.AddBidirectionalConverter<TomlBool, bool>(t => t.Value, v => new TomlBool(v));
        }

        private void AddToTomlConverter<TFrom, TTo>(Func<TFrom, TTo> toToml) where TTo : TomlValue =>
            this.toTomlConverters.Add(new ToTomlConverter<TFrom, TTo>(toToml));

        private void AddBidirectionalConverter<TA, TB>(Func<TA, TB> fromToml, Func<TB, TA> toToml) where TA : TomlValue
        {
            this.fromTomlConverters.Add(new FromTomlConverter<TA, TB>(fromToml));
            this.toTomlConverters.Add(new ToTomlConverter<TB, TA>(toToml));
        }

        public static TomlConfig Create() => new TomlConfig();

        internal IEnumerable<IToTomlConverter> GetConvertersFromTypeToToml(Type from)
            => this.toTomlConverters.Where(c => c.FromType == from);

        internal IFromTomlConverter GetFromTomlConverter(Type toType, Type fromType)
            => this.fromTomlConverters.FirstOrDefault(c => c.ToType == toType && c.FromType == fromType);

        internal IToTomlConverter GetToTomlConverer(Type toType, Type fromType)
            => this.toTomlConverters.FirstOrDefault(c => c.ToType == toType && c.FromType == fromType);

        internal object GetActivatedInstance(Type t)
        {
            Func<object> a;
            if (this.activators.TryGetValue(t, out a))
            {
                return a();
            }
            else
            {
                try
                {
                    return Activator.CreateInstance(t);
                }
                catch (MissingMethodException exc)
                {
                    throw new InvalidOperationException(string.Format("Failed to create type '{1}'. Only types with a " +
                        "parameterless constructor or an specialized creator can be created. Make sure the type has " +
                        "a parameterless constructor or a configuration with an corresponding creator is provided.", exc.Message, t.FullName));
                }
            }
        }

        internal TomlTable.TableTypes GetTableType(PropertyInfo pi) =>
            this.inlineTableTypes.Contains(pi.PropertyType) ||
            pi.GetCustomAttributes(false).Any((a) => a.GetType() == typeof(TomlInlineTableAttribute)) ? TomlTable.TableTypes.Inline : TomlTable.TableTypes.Default;

        internal TomlCommentLocation GetCommentLocation(TomlComment c)
        {
            switch (c.Location)
            {
                case CommentLocation.Append: return TomlCommentLocation.Append;
                case CommentLocation.Prepend: return TomlCommentLocation.Prepend;
                default: return DefaultCommentLocation;
            }
        }
    }
}
