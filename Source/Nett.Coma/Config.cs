﻿namespace Nett.Coma
{
    using System;
    using System.Linq;
    using Extensions;

    public class Config
    {
        private IPersistableConfig persistable;

        internal Config(IPersistableConfig persistable)
        {
            this.persistable = persistable.CheckNotNull(nameof(persistable));

            this.persistable = persistable;
        }

        internal delegate void SetAction(ref TomlTable table);

        public static Config<T> Create<T>(Func<T> createDefault, string filePath)
            where T : class
            => Create(createDefault, ConfigSource.CreateFileSource(filePath));

        public static Config<T> Create<T>(Func<T> createDefault, params string[] filePaths)
            where T : class
        {
            var sources = filePaths.Select(fp => ConfigSource.CreateFileSource(fp)).ToArray();
            var merged = ConfigSource.Merged(sources);
            return Create(createDefault, merged);
        }

        public static Config<T> Create<T>(Func<T> createDefault, IConfigSource source)
            where T : class
        {
            createDefault.CheckNotNull(nameof(createDefault));
            source.CheckNotNull(nameof(source));

            var cfg = createDefault();

            var persisted = ((ISourceFactory)source).CreatePersistable();
            persisted.EnsureExists(Toml.Create(cfg));

            return new Config<T>(persisted);
        }

        public TRet Get<TRet>(Func<TomlTable, TRet> getter)
        {
            getter.CheckNotNull(nameof(getter));

            var cfg = this.persistable.Load();
            return getter(cfg);
        }

        public void Set(Action<TomlTable> setter)
        {
            setter.CheckNotNull(nameof(setter));

            this.SetInternal((ref TomlTable tbl) => setter(tbl));
        }

        public IDisposable StartTransaction()
        {
            var transaction = Transaction.Start(this.persistable, onCloseTransactionCallback: original => this.persistable = original);
            this.persistable = transaction;
            return transaction;
        }

        public TomlTable Unmanaged() => this.persistable.Load();

        internal void SetInternal(SetAction setter)
        {
            var cfg = this.persistable.Load();
            setter(ref cfg);
            this.persistable.Save(cfg);
        }
    }
}
