using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncAndAsync
{
    public static class SingleOperation
    {
        static int x = 1;
        static Func<int, string> f = n => n.ToString();
        static Func<int, Task<string>> fAsync = n => Task.FromResult(n.ToString());

        public static void Sync()
        {
            var y = f(x);
        }

        public static async Task Async()
        {
            var y = await fAsync(x);
        }
    }
}
