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

            // 3-state protocol
            Console.WriteLine("3-State-Base Protocol");
            cpu0.Simulate(filepath);

            // プロトコル変更(途中で変更できるものと仮定)
            //cpu.ChangeProtocol(Cache.Protocol.Berkley);

            // Berkley protocol
            Console.WriteLine("Berkley Protocol");
            cpu1.Simulate(filepath);  
        }
    }
}