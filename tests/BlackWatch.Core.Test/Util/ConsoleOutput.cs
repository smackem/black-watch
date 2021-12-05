using System;
using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace BlackWatch.Core.Test.Util
{
    public static class ConsoleOutput
    {
        public static IDisposable Redirect(ITestOutputHelper @out)
        {
            return new Redirection(@out);
        }

        private class Writer : TextWriter
        {
            private readonly ITestOutputHelper _output;

            public Writer(ITestOutputHelper output)
            {
                _output = output;
            }

            public override Encoding Encoding => Encoding.UTF8;

            public override void WriteLine(string? message)
            {
                _output.WriteLine(message);
            }

            public override void WriteLine(string format, params object?[] args)
            {
                _output.WriteLine(format, args);
            }

            public override void Write(char value)
            {
                throw new NotSupportedException("This text writer only supports WriteLine(string) and WriteLine(string, params object[]).");
            }
        }

        private class Redirection : IDisposable
        {
            private readonly TextWriter _oldOut;

            public Redirection(ITestOutputHelper @out)
            {
                _oldOut = Console.Out;
                Console.SetOut(new Writer(@out));
            }

            public void Dispose()
            {
                Console.SetOut(_oldOut);
            }
        }
    }
}