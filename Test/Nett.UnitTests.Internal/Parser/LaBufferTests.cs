﻿using System.IO;
using System.Text;
using FluentAssertions;
using Nett.Parser;
using Nett.UnitTests.Util;
using Xunit;

namespace Nett.UnitTests.Parser
{
    public class LaBufferTests
    {
        [Fact]
        public void CanReadCompleContent()
        {
            // Arrange
            var content = "This is";
            var sb = new StringBuilder();
            var sr = new StreamReader(content.ToStream());
            var lab = new CharBuffer(() =>
            {
                int read = sr.Read();
                return read != -1 ? new char?((char)read) : new char?();
            }, 3);

            // Act

            while (lab.HasNext())
            {
                sb.Append(lab.PeekAt(0));
                lab.Consume();
            }

            sb.Append(lab.PeekAt(0));

            // Assert
            sb.ToString().Should().Be(content);
        }
    }
}
