﻿namespace Nett
{
    using System;

    internal abstract class TomlConverterBase<TFrom, TTo> : ITomlConverter<TFrom, TTo>
    {
        public static readonly Type StaticFromType = typeof(TFrom);
        public static readonly Type StaticToType = typeof(TTo);
        private static readonly bool CanConvertToTomlType = Types.TomlObjectType.IsAssignableFrom(typeof(TTo));

        public Type FromType => StaticFromType;

        public bool CanConvertFrom(Type t) => t == StaticFromType;

        public bool CanConvertTo(Type t) => t == StaticToType || t.IsAssignableFrom(StaticToType);

        public bool CanConvertToToml() => CanConvertToTomlType;

        public object Convert(IMetaDataStore metaData, object o, Type targetType) => this.Convert(metaData, (TFrom)o, targetType);

        public abstract TTo Convert(IMetaDataStore metaData, TFrom from, Type targetType);
    }
}
