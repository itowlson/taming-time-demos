using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxTranslatable
{
    class Program
    {
        static void Main(string[] args)
        {
            RunTest();

            Console.ReadKey();
        }

        public static void RunTest()
        {
            var progname = "mspaint.exe";

            new Processes()
                //.Where(pi => pi.Name == progname)
                //.Select(pi => pi.Name)
                .Subscribe(
                    n => Console.WriteLine("PROCESS CREATED: " + n.Name)
                );
        }
    }
}
