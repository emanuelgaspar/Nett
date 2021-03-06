﻿using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Nett.UnitTests
{
    public class WriteTomlTypesTests
    {
        public static IEnumerable<object[]> WriteTomlStringData
        {
            get
            {
                yield return new object[] { new StringType { S = "C:\\dir1" }, "S = \"C:\\\\dir1\"" };
                yield return new object[] { new StringType { S = "C:\\\r\nX" }, "S = \"C:\\\\\\r\\nX\"" };
                yield return new object[] { new StringType { S = @"C:\Windows\System32\BestPractices\v1.0\Models\Microsoft\Windows\Hyper-V\en-US\test.txt" }, @"S = ""C:\\Windows\\System32\\BestPractices\\v1.0\\Models\\Microsoft\\Windows\\Hyper-V\\en-US\\test.txt""" };
            }
        }

        [Theory]
        [MemberData(nameof(WriteTomlStringData))]
        public void WriteTomlStrings(StringType s, string expected)
        {
            // Act
            var written = Toml.WriteString(s);

            // Assert
            Assert.Equal(expected, written.Trim());
        }

        [Fact(DisplayName = "Type with float is always written with at least one decimal point")]
        public void WriteFloatType_WritesAtLeastOneDecimalPoint()
        {
            // Arrange
            var ft = new FloatType() { F = 123 };

            // Act
            var s = Toml.WriteString(ft);

            // Assert
            s.Trim().Should().Be("F = 123.0");
        }

        [Fact(DisplayName = "Type with double is always written with at least one decimal point")]
        public void WriteDoubleType_WritesAtLeastOneDecimalPoint()
        {
            // Arrange
            var ft = new DoubleType() { D = 123 };

            // Act
            var s = Toml.WriteString(ft);

            // Assert
            s.Trim().Should().Be("D = 123.0");
        }

        [Theory]
        [InlineData("02:01", "02:01:00")]
        [InlineData("03:02:01", "03:02:01")]
        [InlineData("4.03:02:01", "4.03:02:01")]
        [InlineData("4.03:02:01.001", "4.03:02:01.0010000")]
        public void WriteTimespan_WritesTheTimepspansInCultureInvariantFormat(string span, string expected)
        {
            var t = new TimespanType() { Ts = TimeSpan.Parse(span) };

            var written = Toml.WriteString(t);

            written.Should().Be($"Ts = {expected}\r\n");
        }

        [Fact(DisplayName = "Test that DateTime gets written in RFC3339 spec", Skip = "Testing this parse / tostring equivalence is somewhat hard.... :-(")]
        public void WriteDateTime_WritesDatetimeInRFC339Spec()
        {
            // Arrange
            var o = new DateTimeType();

            // Act
            var written = Toml.WriteString(o);

            // Assert
            written.Should().Be($"DT = {DateTimeType.Default}\r\n");
        }

        public class TimespanType
        {
            public TimeSpan Ts { get; set; }
        }

        public class StringType
        {
            public string S { get; set; }
        }

        public class DateTimeType
        {
            public const string Default = "1979-05-27T00:32:00.999999-07:00";
            public DateTime DT { get; set; } = DateTime.Parse(Default);
        }

        public class FloatType
        {
            public float F { get; set; }
        }

        public class DoubleType
        {
            public double D { get; set; }
        }
    }
}
