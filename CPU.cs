using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace TraceDrivenSimulation
{
    /// <summary>
    /// CPUクラス
    /// </summary>
    class CPU
    {
        /// <summary>
        /// バスに流れるメッセージ
        /// </summary>
        public enum BusMessage
        {
            /// <summary>
            /// No Transaction
            /// </summary>
            None,

            /// <summary>
            /// Read ミス
            /// </summary>
            ReadMiss,

            /// <summary>
            /// Write ミス
            /// </summary>
            WriteMiss,

            /// <summary>
            /// Invalidate
            /// </summary>
            Invalidation,
        }

        List<Processor> _processors; // プロセッサ(コア)
        Protocol _protocol; // キャッシュ制御プロトコル
        

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="core_num">プロセッサ(コア)数</param>
        /// <param name="protocol">キャッシュのプロトコル</param>
        public CPU(int core_num, Cache.Protocol protocol)
        {
            _processors = new List<Processor>(core_num);
            for (int i = 0; i < core_num; ++i)
                _processors.Add(new Processor(protocol));

            switch (protocol)
            {
                case Cache.Protocol.ThreeStateBasic:
                    _protocol = new ThreeStateBasicProtocol();
                    break;
                case Cache.Protocol.Berkley:
                    _protocol = new BerkleyProtocol();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Trace-driven Simulationを行う
        /// PCA特論 第3回課題用
        /// </summary>
        /// <param name="fileName">simulate対象ファイル名</param>
        /// <param name="protocol">simulateするプロトコル</param>
        public void Simulate(string fileName)
        {
            string line;
            string[] elements;
            var separator = new[] { ' ' };
            int PU; // プロセッサ番号
            string rw, address;

            using (var sr = new StreamReader(fileName)) // NOTE: 簡単のためファイルの例外は考えない
            {
                while (!string.IsNullOrEmpty(line = sr.ReadLine()))
                {
                    elements = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                    if (elements.Length < 4)
                        continue;

                    PU = int.Parse(elements[1]);
                    rw = elements[2];
                    address = elements[3].HexToBinary(32);

                    if (rw == "r")
                        _protocol.Read(address, PU, _processors);
                    else
                        _protocol.Write(address, PU, _processors);
                }
            }      

            // 結果表示
            for (int i = 0; i < _processors.Count; ++i)
                System.Console.WriteLine("Processor[" + (i+1) + "] " + "Miss rate : " + _processors[i].Cache.MissRate * 100 + " %");

            System.Console.WriteLine("Write-back count : " + _protocol.WriteBackCount);
            System.Console.WriteLine("");
        }
    }
}
