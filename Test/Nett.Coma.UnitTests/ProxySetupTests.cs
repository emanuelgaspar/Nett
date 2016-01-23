using System;
using FluentAssertions;
using Xunit;

namespace Nett.Coma.UnitTests
{
    public sealed class ProxySetupTests
    {
        private readonly string LocalFile;

        public ProxySetupTests()
        {
            this.LocalFile = $"{Guid.NewGuid()}.toml";
        }

        [Fact]
        public void CreatingConfigProxy_EnsuresProxyHasOriginalDefaultValues()
        {
            // Act
            var cfg = ConfigManager.Setup<Config>(LocalFile);

            // Assert
            cfg.TestInt.Should().Be(Config.DefaultInt);
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
