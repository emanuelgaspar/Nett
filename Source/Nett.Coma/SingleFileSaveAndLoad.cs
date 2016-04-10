using System.IO;
using static System.Diagnostics.Debug;

namespace Nett.Coma
{
    internal sealed class SingleFileSaveAndLoad<T> : ISaveLoad<T>
    {
        private readonly string filePath;
        private readonly TomlConfig config;

        public SingleFileSaveAndLoad(string filePath, TomlConfig config)
        {
            Assert(!string.IsNullOrWhiteSpace(filePath));
            Assert(config != null);

            this.config = config;
            this.filePath = filePath;
        }

        T ISaveLoad<T>.Load() => Toml.ReadFile<T>(this.filePath, this.config);
        void ISaveLoad<T>.Save(T toSave) => Toml.WriteFile(toSave, this.filePath, this.config);

        bool ISaveLoad<T>.CanLoad() => File.Exists(filePath);
    }
}
