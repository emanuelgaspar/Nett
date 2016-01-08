using FluentAssertions;
using Xunit;

namespace Nett.Coma.UnitTests
{
    public sealed class AutoSaveAndLoadTests
    {
        private const string LocalFile = "config.toml";
        private readonly ConfigScope local = new ConfigScope(LocalFile);

        [Fact(DisplayName = "When config object property is set, the config gets saved automatically")]
        public void SettingPropertyTriggersSave()
        {
            // Arrange
            const int expected = 1;
            var cfg = ConfigManager.Setup<Config>(local);

            // Act
            cfg.TestProperty = expected;

            // Assert
            var read = Toml.ReadFile<Config>(LocalFile);
            read.TestProperty.Should().Be(expected);
        }

        [Fact(DisplayName = "When config object property is read, the config get read prior to the getter invocation.")]
        public void GetingProperty_ExecutesLoadBeforeTheGetter()
        {
            // Arrange
            const int expected = 1;
            var writeToDisk = new Config() { TestProperty = expected };
            Toml.WriteFile(writeToDisk, LocalFile);
            var cfg = ConfigManager.Setup<Config>(local);

            // Act
            var value = cfg.TestProperty;

            // Assert
            value.Should().Be(expected);
        }

        public class Config
        {
            public virtual int TestProperty { get; set; }
        }
    }
}
