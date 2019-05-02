using System;
using System.IO;
using System.Collections.Generic;

namespace TraceDrivenSimulation
{
    /// <summary>
    /// プロセッサ(コア)クラス
    /// </summary>
    class Processor
    {
        /// <summary>
        /// キャッシュ
        /// </summary>
        public Cache Cache { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Processor(Cache.Protocol protocol)
        {
            int cacheSize = (int)Math.Pow(2, 15); // キャッシュサイズは32KB

            switch (protocol)
            {
                case Cache.Protocol.ThreeStateBasic:
                    this.Cache = new ThreeStateBasicCache(cacheSize);
                    break;
                case Cache.Protocol.Berkley:
                    this.Cache = new BerkleyCache(cacheSize);
                    break;
                default:
                    throw new ArgumentException();
            }
        }
    }
}