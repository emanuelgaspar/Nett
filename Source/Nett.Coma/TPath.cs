﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Nett.Extensions;

namespace Nett.Coma
{
    internal interface ITPathSegment : IEquatable<ITPathSegment>
    {
        TomlObject Apply(TomlObject obj);
    }

    [DebuggerDisplay("{DebuggerDisplay}")]
    internal class TPath : IEnumerable<ITPathSegment>, IEquatable<TPath>
    {
        public const char PathSeperatorChar = '/';

        private readonly TPath prefixPath;
        private readonly ITPathSegment segment;

        private bool IsRootPrefixPath => this.segment == null;

        public TPath()
        {
            this.prefixPath = null;
            this.segment = null;
        }

        private TPath(TPath prefixPath, ITPathSegment segment)
        {
            this.prefixPath = prefixPath;
            this.segment = segment;
        }

        public TomlObject Apply(TomlObject obj)
        {
            if (this.IsRootPrefixPath)
            {
                return obj;
            }

            var prefixObj = this.prefixPath.Apply(obj);
            return this.segment.Apply(prefixObj);
        }

        public bool Equals(TPath other)
        {
            if (other == null) { return false; }
            if (other.segment != this.segment) { return false; }

            return this.prefixPath == other.prefixPath;
        }

        public TPath WithKeyAdded(string key) => new TPath(this, new KeySegment(key));

        public TPath WithIndexAdded(int index) => new TPath(this, new IndexSegment(index));

        public bool ClearFrom(TomlTable from)
        {
            from.CheckNotNull(nameof(from));

            var keySegment = this.segment as KeySegment;
            if (keySegment == null)
            {
                throw new InvalidOperationException("Final segment of path needs to be a key");
            }

            var targetTable = (TomlTable)this.prefixPath.Apply(from);
            return keySegment.ClearFrom(targetTable);
        }

        public static TPath Parse(string src)
        {
            var path = new TPath();

            for (int i = 0; i < src.Length; i++)
            {
                if (src[i] == '/')
                {
                    i++;
                    var key = ParseKey(ref i, src);
                    path = path.WithKeyAdded(key);
                }
                else if (src[i] == '[')
                {
                    i++;
                    var index = ParseIndex(ref i, src);
                    path = path.WithIndexAdded(index);
                }
                else
                {
                    throw new Exception("Input is no valid TPath");
                }
            }

            return path;
        }

        private static string ParseKey(ref int parseIndex, string input) => ParseSegment(ref parseIndex, input, 32);

        private static int ParseIndex(ref int parseIndex, string input) => int.Parse(ParseSegment(ref parseIndex, input, 8));

        private static string ParseSegment(ref int parseIndex, string input, int capacity)
        {
            var sb = new StringBuilder(capacity);
            for (; parseIndex < input.Length; parseIndex++)
            {
                var ic = input[parseIndex];

                if (ic == '/' || ic == ']' || ic == '[')
                {
                    if (ic != ']') { parseIndex--; } // No closing tag -> got back one char to compensate the final parseIndex++
                    break;
                }
                else
                {
                    sb.Append(ic);
                }
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            if (this.IsRootPrefixPath) { return string.Empty; }

            return this.prefixPath.ToString() + this.segment.ToString();
        }

        internal TPath WithSegmentAdded(ITPathSegment segment) => new TPath(this, segment);

        private static string GetKey(string segment)
        {
            var endIndex = segment.IndexOf("[");
            endIndex = endIndex > 0 ? endIndex : segment.Length;
            return segment.Substring(0, endIndex);
        }

        private static int? TryGetIndex(string segment)
        {
            int index = segment.IndexOf("[");
            if (index >= 0)
            {
                var indexString = segment.Substring(index + 1);
                indexString = indexString.Substring(0, indexString.Length - 1);
                indexString.Replace("]", string.Empty);
                return int.Parse(indexString);
            }

            return null;
        }

        public IEnumerator<ITPathSegment> GetEnumerator() => this.Segments().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private string DebuggerDisplay => this.ToString();

        private IEnumerable<ITPathSegment> Segments()
        {
            if (this.IsRootPrefixPath)
            {
                Debug.WriteLine("emtpy");
                yield break;
            }

            foreach (var seg in this.prefixPath)
            {
                Debug.WriteLine($"Seg: {seg}");
                yield return seg;
            }

            Debug.WriteLine($"Seg: {this.segment}");
            yield return this.segment;
        }

        private sealed class KeySegment : ITPathSegment
        {
            private readonly string key;

            public KeySegment(string key)
            {
                this.key = key;
            }

            public TomlObject Apply(TomlObject obj)
            {
                var table = obj as TomlTable;
                if (table != null) { return table[this.key]; }

                throw new InvalidOperationException(
                    $"Cannot apply key path segment '{this.key}' on TOML object of type '{obj.ReadableTypeName}.");
            }

            public int CompareTo(ITPathSegment other)
            {
                throw new NotImplementedException();
            }

            public bool Equals(ITPathSegment other)
            {
                var otherKeySegment = other as KeySegment;

                if (other == null) { return false; }

                return this.key == otherKeySegment.key;
            }

            public bool ClearFrom(TomlTable table) => table.Remove(this.key);

            public override bool Equals(object obj) => this.Equals(obj as ITPathSegment);

            public override string ToString() => $"/{this.key}";
        }

        private sealed class IndexSegment : ITPathSegment
        {
            private readonly int index;

            public IndexSegment(int index)
            {
                this.index = index;
            }

            public TomlObject Apply(TomlObject obj)
            {
                var ta = obj as TomlArray;
                if (ta != null) { return ta[this.index]; }

                var tta = obj as TomlTableArray;
                if (tta != null) { return tta[this.index]; }

                throw new InvalidOperationException(
                    $"Cannot apply index path segment '{this.index}' on TOML object of type '{obj.ReadableTypeName}'.");
            }

            public bool Equals(ITPathSegment other)
            {
                var otherIndexSeg = other as IndexSegment;
                return otherIndexSeg != null && this.index == otherIndexSeg.index;
            }

            public override bool Equals(object obj) => this.Equals(obj as ITPathSegment);

            public override string ToString() => this.index.ToString();
        }
    }
}
