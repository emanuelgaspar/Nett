﻿using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Nett.UnitTests
{
    public class ConversionTests
    {
        [Fact]
        public void ReadToml_WhenConfigHasConverter_ConverterGetsUsed()
        {
            // Arrange
            var config = TomlConfig.Create(cfg => cfg
                .ConfigureType<TestStruct>(ct => ct
                    .WithConversionFor<TomlInt>(conv => conv
                        .FromToml(ti => new TestStruct() { Value = (int)ti.Value })
                        .ToToml(ts => new TomlInt(ts.Value))
                    )
                )
            );

            string toml = @"S = 10";

            // Act
            var co = Toml.ReadString<ConfigObject>(toml, config);

            // Assert
            Assert.Equal(10, co.S.Value);
        }

        [Fact]
        public void WriteToml_WhenConfigHasConverter_ConverterGetsUsed()
        {
            // Arrange
            var config = TomlConfig.Create(cfg => cfg
                .ConfigureType<TestStruct>(ct => ct
                    .WithConversionFor<TomlInt>(conv => conv
                        .FromToml(ti => new TestStruct() { Value = (int)ti.Value })
                        .ToToml(ts => new TomlInt(ts.Value))
                    )
                    .CreateInstance(() => new TestStruct())
                    .TreatAsInlineTable()
                )
            );
            var obj = new ConfigObject() { S = new TestStruct() { Value = 222 } };

            // Act
            var ser = Toml.WriteString(obj, config);

            // Assert
            Assert.Equal("S = 222\r\n", ser);
        }

        [Fact]
        public void RadToml_WithGenricConverters_CanFindCorrectConverter()
        {
            // Arrange
            var config = TomlConfig.Create(cfg => cfg
                .ConfigureType<IGeneric<string>>(ct => ct
                    .WithConversionFor<TomlString>(conv => conv
                        .FromToml((ts) => new GenericImpl<string>(ts.Value))
                    )
                )
                .ConfigureType<IGeneric<int>>(ct => ct
                    .WithConversionFor<TomlString>(conv => conv
                        .FromToml((ts) => new GenericImpl<int>(int.Parse(ts.Value)))
                    )
                )
            );

            string toml = @"
Foo = ""Hello""
Foo2 = ""10""
Foo3 = [""A""]";

            // Act
            var co = Toml.ReadString<GenericHost>(toml, config);

            // Assert
            Assert.NotNull(co);
            Assert.NotNull(co.Foo);
            Assert.Equal("Hello", co.Foo.Value);
            Assert.NotNull(co.Foo2);
            Assert.Equal(10, co.Foo2.Value);
        }

        [Fact]
        public void WriteToml_ConverterIsUsedAndConvertedPropertiesAreNotEvaluated()
        {
            // Arrange
            var config = TomlConfig.Create(cfg => cfg
                .ConfigureType<ClassWithTrowingProp>(ct => ct
                    .WithConversionFor<TomlValue>(conv => conv
                        .ToToml((_) => new TomlString("Yeah converter was used, and property not accessed"))
                    )
                )
            );

            var toWrite = new Foo();

            // Act
            var written = Toml.WriteString(toWrite, config);

            // Assert
            Assert.Equal(@"Prop = ""Yeah converter was used, and property not accessed""", written.Trim());
        }

        [Fact]
        public void WriteToml_WithListItemConverter_UsesConverter()
        {
            // Arrange
            var config = TomlConfig.Create(cfg => cfg
                .ConfigureType<GenProp<GenType>>(ct => ct
                    .WithConversionFor<TomlValue>(conv => conv
                        .ToToml((_) => new TomlString("Yeah converter was used."))
                    )
                )
            );
            var toWrite = new GenHost();

            // Act
            var written = Toml.WriteString(toWrite, config);

            // Assert
            Assert.Equal(@"Props = [""Yeah converter was used."", ""Yeah converter was used.""]", written.Trim());
        }

        [Fact]
        public void WriteToml_WithListItemConverterAndPropertyUsesInterface_UsesConverter()
        {
            // Arrange
            var config = TomlConfig.Create(cfg => cfg
                .ConfigureType<IGenProp<GenType>>(ct => ct
                    .WithConversionFor<TomlValue>(conv => conv
                        .ToToml((_) => new TomlString("Yeah converter was used."))
                    )
                )
            );
            var toWrite = new GenInterfaceHost();

            // Act
            var written = Toml.WriteString(toWrite, config);

            // Assert
            Assert.Equal(@"Props = [""Yeah converter was used."", ""Yeah converter was used.""]", written.Trim());
        }



        private static TomlTable SetupConversionSetTest(TomlConfig.ConversionLevel set, string tomlInput)
        {
            var config = TomlConfig.Create(cfg => cfg
                .AllowImplicitConversions(set)
            );

            TomlTable table = Toml.ReadString(tomlInput, config);
            return table;
        }

        public static IEnumerable<object[]> EquivalentConversionTestData
        {
            get
            {
                // TomlFloat
                // +
                yield return new object[] { "v=1.1", new Func<TomlTable, object>(t => t.Get<double>("v")), true };
                yield return new object[] { "v=1.1", new Func<TomlTable, object>(t => t.Get<TomlFloat>("v")), true };
                // -
                yield return new object[] { "v=1.1", new Func<TomlTable, object>(t => t.Get<float>("v")), false };
                yield return new object[] { "v=1.1", new Func<TomlTable, object>(t => t.Get<int>("v")), false };
                yield return new object[] { "v=1.1", new Func<TomlTable, object>(t => t.Get<string>("v")), false };
                yield return new object[] { "v=1.1", new Func<TomlTable, object>(t => t.Get<TomlString>("v")), false };

                // TomlInt
                // +
                yield return new object[] { "v=1", new Func<TomlTable, object>(t => t.Get<long>("v")), true };
                yield return new object[] { "v=1", new Func<TomlTable, object>(t => t.Get<TomlInt>("v")), true };
                // -
                yield return new object[] { "v=1", new Func<TomlTable, object>(t => t.Get<int>("v")), false };
                yield return new object[] { "v=1", new Func<TomlTable, object>(t => t.Get<short>("v")), false };
                yield return new object[] { "v=1", new Func<TomlTable, object>(t => t.Get<char>("v")), false };
                yield return new object[] { "v=1", new Func<TomlTable, object>(t => t.Get<TomlString>("v")), false };
                yield return new object[] { "v=1", new Func<TomlTable, object>(t => t.Get<bool>("v")), false };
                yield return new object[] { "v=1", new Func<TomlTable, object>(t => t.Get<TomlBool>("v")), false };
                yield return new object[] { "v=1", new Func<TomlTable, object>(t => t.Get<float>("v")), false };
                yield return new object[] { "v=1", new Func<TomlTable, object>(t => t.Get<TomlFloat>("v")), false };
            }
        }

        [Theory(DisplayName = "Getting value when equivalent implicit conversions are activated only exact conversions will work others will fail")]
        [MemberData(nameof(EquivalentConversionTestData))]
        public void ReadToml_Equivalent_AllowsConversionFromTomlIntToFloat(string s, Func<TomlTable, object> read, bool shouldWork)
        {
            // Arrange
            var tbl = SetupConversionSetTest(TomlConfig.ConversionLevel.Strict, s);

            Action a = () => read(tbl);

            // Assert
            if (shouldWork)
            {
                a.ShouldNotThrow();
            }
            else
            {
                a.ShouldThrow<Exception>();
            }
        }

        public static IEnumerable<object[]> SameNumericCategoryImplicitConversionTestData
        {
            get
            {
                // TomlFloat
                // +
                yield return new object[] { "a0", "v=1.1", new Func<TomlTable, object>(t => t.Get<double>("v")), true };
                yield return new object[] { "a1", "v=1.1", new Func<TomlTable, object>(t => t.Get<TomlFloat>("v")), true };
                yield return new object[] { "b1", "v=1.1", new Func<TomlTable, object>(t => t.Get<float>("v")), true };
                // -

                yield return new object[] { "b2", "v=1.1", new Func<TomlTable, object>(t => t.Get<int>("v")), false };
                yield return new object[] { "b3", "v=1.1", new Func<TomlTable, object>(t => t.Get<string>("v")), false };
                yield return new object[] { "b4", "v=1.1", new Func<TomlTable, object>(t => t.Get<TomlString>("v")), false };

                // TomlInt
                yield return new object[] { "c1", "v=1", new Func<TomlTable, object>(t => t.Get<long>("v")), true };
                yield return new object[] { "c2", "v=1", new Func<TomlTable, object>(t => t.Get<TomlInt>("v")), true };
                yield return new object[] { "c3", "v=1", new Func<TomlTable, object>(t => t.Get<int>("v")), true };
                yield return new object[] { "c4", "v=1", new Func<TomlTable, object>(t => t.Get<short>("v")), true };
                yield return new object[] { "c5", "v=1", new Func<TomlTable, object>(t => t.Get<char>("v")), true };

                // -
                yield return new object[] { "c6", "v=1", new Func<TomlTable, object>(t => t.Get<TomlString>("v")), false };
                yield return new object[] { "d0", "v=1", new Func<TomlTable, object>(t => t.Get<bool>("v")), false };
                yield return new object[] { "d1", "v=1", new Func<TomlTable, object>(t => t.Get<TomlBool>("v")), false };
                yield return new object[] { "d2", "v=1", new Func<TomlTable, object>(t => t.Get<float>("v")), false };
                yield return new object[] { "d3", "v=1", new Func<TomlTable, object>(t => t.Get<TomlFloat>("v")), false };
            }
        }

        [Theory(DisplayName = "Getting value when same numeric category implicit conversions are activated only that conversions will work others will fail")]
        [MemberData(nameof(SameNumericCategoryImplicitConversionTestData))]
        public void ReadToml_Exact_AllowsConversionFromTomlIntToFloat(string id, string s, Func<TomlTable, object> read, bool shouldWork)
        {
            // Arrange
            var tbl = SetupConversionSetTest(TomlConfig.ConversionLevel.SameNumericCategory, s);

            // Act
            Action a = () => read(tbl);

            // Assert
            if (shouldWork)
            {
                a.ShouldNotThrow();
            }
            else
            {
                a.ShouldThrow<Exception>();
            }
        }

        public static IEnumerable<object[]> DotNetImplicitImplicitConversionsTestData
        {
            get
            {
                // TomlFloat
                // +
                yield return new object[] { "a0", "v=1.1", new Func<TomlTable, object>(t => t.Get<double>("v")), true };
                yield return new object[] { "a1", "v=1.1", new Func<TomlTable, object>(t => t.Get<TomlFloat>("v")), true };
                yield return new object[] { "a2", "v=1.1", new Func<TomlTable, object>(t => t.Get<float>("v")), true };
                // -
                yield return new object[] { "b0", "v=1.1", new Func<TomlTable, object>(t => t.Get<int>("v")), false };
                yield return new object[] { "b1", "v=1.1", new Func<TomlTable, object>(t => t.Get<string>("v")), false };
                yield return new object[] { "b2", "v=1.1", new Func<TomlTable, object>(t => t.Get<TomlString>("v")), false };

                // TomlInt
                yield return new object[] { "c1", "v=1", new Func<TomlTable, object>(t => t.Get<long>("v")), true };
                yield return new object[] { "c2", "v=1", new Func<TomlTable, object>(t => t.Get<TomlInt>("v")), true };
                yield return new object[] { "c3", "v=1", new Func<TomlTable, object>(t => t.Get<int>("v")), true };
                yield return new object[] { "c4", "v=1", new Func<TomlTable, object>(t => t.Get<short>("v")), true };
                yield return new object[] { "c5", "v=1", new Func<TomlTable, object>(t => t.Get<char>("v")), true };
                yield return new object[] { "c6", "v=1", new Func<TomlTable, object>(t => t.Get<float>("v")), true };
                yield return new object[] { "c7", "v=1", new Func<TomlTable, object>(t => t.Get<double>("v")), true };
                yield return new object[] { "c8", "v=1", new Func<TomlTable, object>(t => t.Get<TomlFloat>("v")), true };
                // -
                yield return new object[] { "d0", "v=1", new Func<TomlTable, object>(t => t.Get<TomlString>("v")), false };
                yield return new object[] { "d1", "v=1", new Func<TomlTable, object>(t => t.Get<bool>("v")), false };
                yield return new object[] { "d2", "v=1", new Func<TomlTable, object>(t => t.Get<TomlBool>("v")), false };
            }
        }

        [Theory(DisplayName = "Getting value when precise implicit conversions are activated only that conversions will work others will fail")]
        [MemberData(nameof(DotNetImplicitImplicitConversionsTestData))]
        public void ReadToml_Precise_AllowsConversionFromTomlIntToFloat(string id, string s, Func<TomlTable, object> read, bool shouldWork)
        {
            // Arrange
            var tbl = SetupConversionSetTest(TomlConfig.ConversionLevel.DotNetImplicit, s);

            // Act
            Action a = () => read(tbl);

            // Assert
            if (shouldWork)
            {
                a.ShouldNotThrow();
            }
            else
            {
                a.ShouldThrow<Exception>();
            }
        }

        public static IEnumerable<object[]> DotNetExplicitImplicitConversionsTestData
        {
            get
            {
                // TomlFloat
                // +
                yield return new object[] { "a0", "v=1.1", new Func<TomlTable, object>(t => t.Get<double>("v")), true };
                yield return new object[] { "a1", "v=1.1", new Func<TomlTable, object>(t => t.Get<TomlFloat>("v")), true };
                yield return new object[] { "a2", "v=1.1", new Func<TomlTable, object>(t => t.Get<float>("v")), true };
                yield return new object[] { "a3", "v=1.1", new Func<TomlTable, object>(t => t.Get<int>("v")), true };
                yield return new object[] { "a4", "v=1.1", new Func<TomlTable, object>(t => t.Get<long>("v")), true };
                yield return new object[] { "a5", "v=1.1", new Func<TomlTable, object>(t => t.Get<char>("v")), true };

                yield return new object[] { "b0", "v=1.1", new Func<TomlTable, object>(t => t.Get<short>("v")), true };
                yield return new object[] { "b1", "v=1.1", new Func<TomlTable, object>(t => t.Get<TomlInt>("v")), true };

                // -
                yield return new object[] { "e1", "v=1.1", new Func<TomlTable, object>(t => t.Get<string>("v")), false };
                yield return new object[] { "e2", "v=1.1", new Func<TomlTable, object>(t => t.Get<TomlString>("v")), false };
                yield return new object[] { "e3", "v=1.1", new Func<TomlTable, object>(t => t.Get<TomlBool>("v")), false };
                yield return new object[] { "e4", "v=1.1", new Func<TomlTable, object>(t => t.Get<bool>("v")), false };

                // TomlInt
                yield return new object[] { "c1", "v=1", new Func<TomlTable, object>(t => t.Get<long>("v")), true };
                yield return new object[] { "c2", "v=1", new Func<TomlTable, object>(t => t.Get<TomlInt>("v")), true };
                yield return new object[] { "c3", "v=1", new Func<TomlTable, object>(t => t.Get<int>("v")), true };
                yield return new object[] { "c4", "v=1", new Func<TomlTable, object>(t => t.Get<short>("v")), true };
                yield return new object[] { "c5", "v=1", new Func<TomlTable, object>(t => t.Get<char>("v")), true };
                yield return new object[] { "c6", "v=1", new Func<TomlTable, object>(t => t.Get<float>("v")), true };
                yield return new object[] { "c7", "v=1", new Func<TomlTable, object>(t => t.Get<double>("v")), true };
                yield return new object[] { "c8", "v=1", new Func<TomlTable, object>(t => t.Get<TomlFloat>("v")), true };

                // -
                yield return new object[] { "d0", "v=1", new Func<TomlTable, object>(t => t.Get<TomlString>("v")), false };
                yield return new object[] { "d2", "v=1", new Func<TomlTable, object>(t => t.Get<TomlBool>("v")), false };
                yield return new object[] { "d1", "v=1", new Func<TomlTable, object>(t => t.Get<bool>("v")), false };
            }
        }

        [Theory(DisplayName = "Getting value when explicit .Net implicit conversions are activated only that conversions will work others will fail")]
        [MemberData(nameof(DotNetExplicitImplicitConversionsTestData))]
        public void ReadToml_ExplicitDotNetImplicit_AllowsConversionFromTomlIntToFloat(string id, string s, Func<TomlTable, object> read, bool shouldWork)
        {
            // Arrange
            var tbl = SetupConversionSetTest(TomlConfig.ConversionLevel.DotNetExplicit, s);

            // Act
            Action a = () => read(tbl);

            // Assert
            if (shouldWork)
            {
                a.ShouldNotThrow();
            }
            else
            {
                a.ShouldThrow<Exception>();
            }
        }

        public class Testy
        {
            public float One { get; set; }
        }
        [Fact]
        public void ReadToml_WithAllConversionEnabled_AllowsConversionFromTomlIntToTomlFloat()
        {
            // Arrange
            var config = TomlConfig.Create(cfg => cfg
                .AllowImplicitConversions(TomlConfig.ConversionLevel.DotNetImplicit)
            );

            string abc = "SomeFloat = 1" + Environment.NewLine;
            TomlTable table = Toml.ReadString(abc, config);

            // Act
            var val = table.Get<TomlFloat>("SomeFloat", config);

            // Assert
            val.Value.Should().Be(1.0f);
        }

        [Fact]
        public void ReadToml_WithAllConversionEnabled_AllowsConversionFromTomlIntToDouble()
        {
            // Arrange
            var config = TomlConfig.Create(cfg => cfg
                .AllowImplicitConversions(TomlConfig.ConversionLevel.DotNetImplicit)
            );

            string abc = "SomeFloat = 1" + Environment.NewLine;
            TomlTable table = Toml.ReadString(abc, config);

            // Act
            var val = table.Get<double>("SomeFloat", config);

            // Assert
            val.Should().Be(1.0);
        }

        private class ConfigObject
        {
            public TestStruct S { get; set; }
        }

        private struct TestStruct
        {
            public int Value;
        }

        private class GenericHost
        {
            public IGeneric<string> Foo { get; set; }
            public IGeneric<int> Foo2 { get; set; }

            public List<IGeneric<string>> Foo3 { get; set; }
        }

        private interface IGeneric<T>
        {
            T Value { get; set; }
        }

        private class GenericImpl<T> : IGeneric<T>
        {
            public T Value { get; set; }

            public GenericImpl(T val)
            {
                this.Value = val;
            }
        }

        private class Foo
        {
            public IClassWithTrowingProp Prop { get; set; }

            public Foo()
            {
                this.Prop = new ClassWithTrowingProp();
            }
        }

        private interface IClassWithTrowingProp
        {
            object Value { get; }
        }

        private class ClassWithTrowingProp : IClassWithTrowingProp
        {
            public object Value { get { throw new NotImplementedException(); } }
        }

        private class GenHost
        {
            public List<GenProp<GenType>> Props { get; set; }

            public GenHost()
            {

                this.Props = new List<GenProp<GenType>>()
                {
                    new GenProp<GenType>() { Value = new GenType() },
                    new GenProp<GenType>() { Value = new GenType() },
                };
            }
        }

        private class GenInterfaceHost
        {
            public List<IGenProp<GenType>> Props { get; set; }

            public GenInterfaceHost()
            {

                this.Props = new List<IGenProp<GenType>>()
                {
                    new GenProp<GenType>() { Value = new GenType() },
                    new GenProp<GenType>() { Value = new GenType() },
                };
            }
        }

        private interface IGenProp<T>
        {
            T Value { get; set; }
        }

        private class GenProp<T> : IGenProp<T>
        {
            public T Value { get; set; }
        }

        private class GenType
        {
            public string Value { get { throw new NotImplementedException(); } }
        }
    }
}