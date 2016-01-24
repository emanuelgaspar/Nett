using System;
using FluentAssertions;
using Xunit;

namespace Nett.Coma.UnitTests
{
    public sealed class MangedConfigInitTests
    {
        private static string GenFileName() => $"{Guid.NewGuid()}.toml";

        public MangedConfigInitTests()
        {
        }

        [Fact(DisplayName = "When no file exists, the config returns the values created by the default instance creator")]
        public void CreatingManagedConfig_EnsuresProxyHasOriginalDefaultValues()
        {
            // Act
            var f = GenFileName();
            var cfg = ConfigManager.Setup(f, () => new Config());

            // Assert
            cfg.TestInt.Should().Be(Config.DefaultInt);
        }

        [Fact(DisplayName = "When there is a file on disk the managed config loads that and will return its values")]
        public void CreatManagedConfig_WhenConfigOnDisk_LoadsThatConfig()
        {
            // Arrange
            var f = GenFileName();
            const int expected = 6;
            var c = new Config() { TestInt = expected };
            Toml.WriteFile(c, f);
            var cfg = ConfigManager.Setup(f, () => new Config());

            // Act
            var r = cfg.TestInt;

            // Assert
            r.Should().Be(expected);
        }

        public class Config
        {
            public const int DefaultInt = 1;

            public virtual int TestInt { get; set; }

            public Config()
            {
                this.TestInt = DefaultInt;
            }
        }
    }
}
