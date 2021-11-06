using System;
using System.Threading.Tasks;
using BlackWatch.Core.Test.Util;
using BlackWatch.Core.Util;
using Xunit;
using Xunit.Abstractions;

namespace BlackWatch.Core.Test
{
    public class ThrottlingQueueTest
    {
        private readonly ITestOutputHelper _out;

        public ThrottlingQueueTest(ITestOutputHelper @out)
        {
            _out = @out;
        }

        [Fact]
        public async Task OnePerSecond()
        {
            using var _ = ConsoleOutput.Redirect(_out);
            const int timeFrameMillis = 1000;
            var queue = await CreateQueueAsync(5, timeFrameMillis, 1);
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
        public async Task FivePerSecond()
        {
            using var _ = ConsoleOutput.Redirect(_out);
            const int timeFrameMillis = 1000;
            var queue = await CreateQueueAsync(20, timeFrameMillis, 5);

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

        [Fact]
        public async Task ConsumeSlowerThanPossible()
        {
            using var _ = ConsoleOutput.Redirect(_out);
            const int timeFrameMillis = 1000;
            var queue = await CreateQueueAsync(4, timeFrameMillis, 2);

            int? lastTicks = null;

            await foreach (var i in queue.DequeueAllAsync())
            {
                int? elapsed = null;
                var ticks = Environment.TickCount;

                if (lastTicks != null)
                {
                    elapsed = ticks - lastTicks;
                }
 
                _out.WriteLine($"[{elapsed}] dequeued {i}");
                lastTicks = ticks;
                
                // two per second allowed, but only consume one per second
                await Task.Delay(1000);
            }
        }

        [Fact]
        public async Task TestHundredPerSecond()
        {
            using var _ = ConsoleOutput.Redirect(_out);
            const int timeFrameMillis = 1000;
            var queue = await CreateQueueAsync(50, timeFrameMillis, 100);

            int? lastTicks = null;

            await foreach (var i in queue.DequeueAllAsync())
            {
                int? elapsed = null;
                var ticks = Environment.TickCount;

                if (lastTicks != null)
                {
                    elapsed = ticks - lastTicks;
                }
 
                _out.WriteLine($"[{elapsed}] dequeued {i}");
                lastTicks = ticks;
            }
        }

        private static async Task<ThrottlingQueue<int>> CreateQueueAsync(int count, int timeFrameMillis, int itemsPerTimeFrame)
        {
            var queue = new ThrottlingQueue<int>(TimeSpan.FromMilliseconds(timeFrameMillis), itemsPerTimeFrame);
            for (var i = 0; i < count; i++)
            {
                await queue.EnqueueAsync(i).Linger();
            }
            queue.Complete();
            return queue;
        }
    }
}