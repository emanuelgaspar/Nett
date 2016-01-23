using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Nett.Coma.UnitTests
{
    //var cfg = ConfigManager.
    //Config<Root>()
    //	.Sub()
    //		.Config<SCA>((r) => r.sca)
    //			.Sub().
    //				Config<SCB>((sca) => r.ssc)
    //			.EndSub()
    //		.Config((r) => )
    //	.EndSub()
    //.Setup();

    public sealed class AutoSaveAndLoadTests : IDisposable
    {
        private readonly string LocalFile;

        public AutoSaveAndLoadTests()
        {
            this.LocalFile = $"{Guid.NewGuid()}.toml";
        }

        public void Dispose()
        {
            var files = Directory.GetFiles(".", "*.toml", SearchOption.TopDirectoryOnly);
            foreach (var f in files)
            {
                File.Delete(f);
            }
        }

        [Fact(DisplayName = "When config object property is set, the config gets saved automatically")]
        public void SettingPropertyTriggersSave()
        {
            // Arrange
            const int expected = 1;
            var cfg = ConfigManager.Setup<Config>(LocalFile);

            // Act
            cfg.TestInt = expected;

            // Assert
            var read = Toml.ReadFile<Config>(LocalFile);
            read.TestInt.Should().Be(expected);
        }

        [Fact(DisplayName = "When config object property is read, the config get read prior to the getter invocation.")]
        public void GetingProperty_ExecutesLoadBeforeTheGetter()
        {
            // Arrange
            const int expected = 1;
            var writeToDisk = new Config() { TestInt = expected };
            Toml.WriteFile(writeToDisk, LocalFile);
            var cfg = ConfigManager.Setup<Config>(LocalFile);

            // Act
            var value = cfg.TestInt;

            // Assert
            value.Should().Be(expected);
        }

        [Fact(DisplayName = "When property in sub config gets set, the config gets saved automatically")]
        public void SettingSubProeprty_SavesConfigAutoamtically()
        {
            // Arrange
            const int expected = 1;
            var cfg = ConfigManager.Setup<ParentConfig>(this.LocalFile);

            // Act
            cfg.SubConfig.TestInt = expected;

            // Assert
            var read = Toml.ReadFile<ParentConfig>(LocalFile);
            read.SubConfig.TestInt.Should().Be(expected);
        }

        public class Config
        {
            public virtual int TestInt { get; set; }
        }

        public class ParentConfig
        {
            public virtual string TestString { get; set; }

            public Config SubConfig { get; set; }

            public ParentConfig()
            {
                this.SubConfig = new Config();
            }
        }
    }
}
