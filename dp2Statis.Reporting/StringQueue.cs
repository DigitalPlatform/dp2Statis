using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dp2Statis.Reporting
{
    public class StringQueue
    {
        Queue<string> _queue = new Queue<string>();

        public void Enqueue(string text)
        {
            _queue.Enqueue(text);
        }

        public async Task<string?> DequeueAsync(
            TimeSpan timeout,
            CancellationToken token = default)
        {
            if (_queue == null)
                throw new ArgumentException("_queue 为 null");
            
            DateTime start = DateTime.Now;
            if (_queue.Count == 0)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(1000);
                if (DateTime.Now - start > timeout)
                    return null;
            }
            return _queue.Dequeue();
        }
    }
}
