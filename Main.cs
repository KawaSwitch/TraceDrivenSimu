using System;
using System.IO;

namespace TraceDrivenSimulation
{
    class MainConsole
    {
        static void Main(string[] args)
        {
            var cpu0 = new CPU(8, Cache.Protocol.ThreeStateBasic);
            var cpu1 = new CPU(8, Cache.Protocol.Berkley);


            string filepath;
            {
                if (args.Length == 1)
                    filepath = args[0]; // コマンドライン引数で対象ファイルパスを受けとる
                else
                {
                    Console.Write("Input simulation file path: ");
                    filepath = Console.ReadLine();
                }
            }

            System.Threading.Tasks.Parallel.Invoke
            (
                () => { cpu0.Simulate(filepath, "3-State-Base Protocol"); },
                () => 
                { 
                    System.Threading.Thread.Sleep(100); // 短いファイルのとき適当に回避
                    cpu1.Simulate(filepath, "Berkley Protocol");
                }
            );
        }
    }
}