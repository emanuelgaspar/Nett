using System;

namespace Nett
{
    public sealed class ToTomlConverter<TFrom, TTo> : IToTomlConverter where TTo : TomlObject
    {
        public static readonly Type StaticFromType = typeof(TFrom);
        public static readonly Type StaticToType = typeof(TTo);

        private readonly Func<TFrom, TTo> convert;

        public Type FromType => StaticFromType;
        public Type ToType => StaticToType;

        TomlObject IToTomlConverter.Convert(object from) => this.convert((TFrom)from);

        public ToTomlConverter(Func<TFrom, TTo> convert)
        {
            this.convert = convert;
        }
    }

    public sealed class FromTomlConverter<TFrom, TTo> : IFromTomlConverter where TFrom : TomlObject
    {
        public static readonly Type StaticFromType = typeof(TFrom);
        public static readonly Type StaticToType = typeof(TTo);

        private readonly Func<TFrom, TTo> convert;

        public Type FromType => StaticFromType;
        public Type ToType => StaticToType;

        object IFromTomlConverter.Convert(TomlObject from) => this.convert((TFrom)from);

        public FromTomlConverter(Func<TFrom, TTo> convert)
        {
            this.convert = convert;
        }
    }
}
