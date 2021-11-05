using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BlackWatch.Core.Util
{
    public class ThrottledQueue<T>
    {
        private readonly Channel<T> _channel;
        private DateTime? _lastYieldTime;
        private int _yieldCount;

        public ThrottledQueue(TimeSpan timeFrame, int maxItemsPerTimeFrame, int? limit = null)
        {
            if (timeFrame <= TimeSpan.Zero)
            {
                throw new ArgumentException("time frame must be greater than zero", nameof(timeFrame));
            }

            if (maxItemsPerTimeFrame < 1)
            {
                throw new ArgumentException("max number of items per time frame must be >= 1", nameof(maxItemsPerTimeFrame));
            }

            _channel = limit == null
                ? Channel.CreateUnbounded<T>()
                : Channel.CreateBounded<T>(limit.Value);

            TimeFrame = timeFrame;
            MaxItemsPerTimeFrame = maxItemsPerTimeFrame;
        }

        public TimeSpan TimeFrame { get; }
        public int MaxItemsPerTimeFrame { get; }

        public bool TryEnqueue(T item)
        {
            return _channel.Writer.TryWrite(item);
        }

        public void Complete()
        {
            _channel.Writer.Complete();
        }

        public async IAsyncEnumerable<T> DequeueAllAsync([EnumeratorCancellation] CancellationToken ct = default)
        {
            while (ct.IsCancellationRequested == false && _channel.Reader.Completion.IsCompleted == false)
            {
                T item;
                try
                {
                    item = await _channel.Reader.ReadAsync(ct);
                }
                catch (ChannelClosedException)
                {
                    break;
                }

                await Throttle(ct);

                yield return item;
            }
        }

        private async Task Throttle(CancellationToken ct)
        {
            var now = DateTime.Now;

            if (_lastYieldTime == null)
            {
                Console.WriteLine("A");
                _lastYieldTime = now;
                _yieldCount = 1;
                return;
            }

            if (_yieldCount < MaxItemsPerTimeFrame)
            {
                Console.WriteLine("B");
                _yieldCount++;
                return;
            }

            var elapsed = now - _lastYieldTime.Value;
            if (elapsed < TimeFrame)
            {
                Console.WriteLine("C");
                await Task.Delay(TimeFrame.Subtract(elapsed), ct);
                _lastYieldTime = DateTime.Now;
                _yieldCount = 1;
            }
        }
    }
}
