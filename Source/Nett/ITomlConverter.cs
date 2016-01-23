using System;

namespace Nett
{
    public interface ITomlConverter
    {
        Type FromType { get; }
        Type ToType { get; }
    }

    public interface IToTomlConverter : ITomlConverter
    {
        TomlObject Convert(object from);
    }

    public interface IFromTomlConverter : ITomlConverter
    {
        object Convert(TomlObject from);
    }
}
