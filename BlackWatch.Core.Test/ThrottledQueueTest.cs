using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BlackWatch.Core.Test.Util;
using BlackWatch.Core.Util;
using Xunit;
using Xunit.Abstractions;

namespace BlackWatch.Core.Test
{
    public class ThrottledQueueTest
    {
        private readonly ITestOutputHelper _out;

        public ThrottledQueueTest(ITestOutputHelper @out)
        {
            _out = @out;
        }

        [Fact]
        public async Task TestOnePerSecond()
        {
            using var _ = ConsoleOutput.Redirect(_out);
            const int timeFrameMillis = 1000;

            var queue = new ThrottledQueue<int>(TimeSpan.FromMilliseconds(timeFrameMillis), 1);
            for (var i = 0; i < 5; i++)
            {
                queue.TryEnqueue(i);
            }
            queue.Complete();

            int? lastTicks = null;

            await foreach (var i in queue.DequeueAllAsync())
            {
                int? elapsed = null;
                var ticks = Environment.TickCount;

                if (lastTicks != null)
                {
                    elapsed = ticks - lastTicks;
                    Assert.True(elapsed >= timeFrameMillis, $"{elapsed} millis elapsed, minimum {timeFrameMillis}");
                }
 
                _out.WriteLine($"[{elapsed}] dequeued {i}");
                lastTicks = ticks;
            }
        }
        
        [Fact]
        public async Task TestTwoPerSecond()
        {
            using var _ = ConsoleOutput.Redirect(_out);
            const int timeFrameMillis = 1000;

            var queue = new ThrottledQueue<int>(TimeSpan.FromMilliseconds(timeFrameMillis), 2);
            for (var i = 0; i < 10; i++)
            {
                queue.TryEnqueue(i);
            }
            queue.Complete();

            int? lastTicks = null;

            await foreach (var i in queue.DequeueAllAsync())
            {
                int? elapsed = null;
                var ticks = Environment.TickCount;

                if (lastTicks != null)
                {
                    elapsed = ticks - lastTicks;
                    //Assert.True(elapsed >= timeFrameMillis, $"{elapsed} millis elapsed, minimum {timeFrameMillis}");
                }
 
                _out.WriteLine($"[{elapsed}] dequeued {i}");
                lastTicks = ticks;
            }
        }
    }
}