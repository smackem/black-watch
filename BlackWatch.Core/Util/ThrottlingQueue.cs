using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BlackWatch.Core.Util
{
    public class ThrottlingQueue<T>
    {
        private readonly Channel<T> _channel;
        private DateTime? _lastYieldTime;
        private int _yieldCount;

        public ThrottlingQueue(TimeSpan timeFrame, int maxItemsPerTimeFrame, int? limit = null)
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

        public ValueTask<bool> EnqueueAsync(T item, CancellationToken ct = default)
        {
            async ValueTask<bool> AsyncPath()
            {
                try
                {
                    await _channel.Writer.WriteAsync(item, ct).Linger();
                }
                catch (ChannelClosedException)
                {
                    return false;
                }
                return true;
            }

            return _channel.Writer.TryWrite(item)
                ? ValueTask.FromResult(true)
                : AsyncPath();
        }

        public void Complete()
        {
            _channel.Writer.TryComplete(); // only returns false if channel is already complete => ignore result
        }

        public async IAsyncEnumerable<T> DequeueAllAsync([EnumeratorCancellation] CancellationToken ct = default)
        {
            while (ct.IsCancellationRequested == false && _channel.Reader.Completion.IsCompleted == false)
            {
                T item;
                try
                {
                    item = await _channel.Reader.ReadAsync(ct).Linger();
                }
                catch (ChannelClosedException)
                {
                    break;
                }

                await Throttle(ct).Linger();

                yield return item;
            }
        }

        private async ValueTask Throttle(CancellationToken ct)
        {
            var now = DateTime.Now;

            if (_lastYieldTime == null)
            {
                _lastYieldTime = now;
                _yieldCount = 1;
                return;
            }

            if (_yieldCount < MaxItemsPerTimeFrame)
            {
                _yieldCount++;
                return;
            }

            var elapsed = now - _lastYieldTime.Value;
            if (elapsed < TimeFrame)
            {
                await Task.Delay(TimeFrame - elapsed, ct).Linger();
                _lastYieldTime = DateTime.Now;
                _yieldCount = 1;
            }
        }
    }
}
