using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace BlackWatch.Core.Test
{
    public class Tryout
    {
        private readonly ITestOutputHelper _out;

        public Tryout(ITestOutputHelper @out)
        {
            _out = @out;
        }

        [Fact]
        public unsafe void FunctionPointers()
        {
            static void Write(ITestOutputHelper @out, string s)
            {
                @out.WriteLine(s);
            }

            delegate*<ITestOutputHelper, string, void> print = &Write;
            print(_out, "hello");

            var arr = Enumerable.Range(1, 50).Select(n => n * n).ToArray();
            var span = arr.AsSpan(3 .. 10);
            fixed (int* arrayPtr = span)
            {
                var ptr = arrayPtr;
                for (var i = 0; i < span.Length; i++, ptr++)
                {
                    print(_out, $"{i}: {*ptr}");
                }
            }
        }

        [Fact]
        public void CompilerDirectives()
        {
#if NET
            _out.WriteLine("NET");
#endif
#if NETCOREAPP
            _out.WriteLine("NETCOREAPP");
#endif
#if NET5_0
            _out.WriteLine("NET5_0");
#endif
#if NET5_0_OR_GREATER
            _out.WriteLine("NET5_0_OR_GREATER");
#endif
#if NETCOREAPP1_0_OR_GREATER
            _out.WriteLine("NETCOREAPP1_0_OR_GREATER");
#endif
        }
    }
}