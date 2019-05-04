using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace TraceDrivenSimulation
{
    /// <summary>
    /// メモリ/キャッシュメモリのプロトコル  制御クラス
    ///     簡易のためハードコーディングあり
    /// </summary>
    abstract class Protocol
    {
        /// <summary>
        /// 命令対象のプロセッサID
        ///     内部のRead/Write関数に渡すのを省くためだけにとりあえず置いてます
        /// </summary>
        protected int _targetID;

        /// <summary>
        /// プロセッサ群(の参照)
        /// </summary>
        protected List<Processor> _processors;

        /// <summary>
        /// 命令対象以外のプロセッサ群
        ///     内部のRead/Write関数に渡すのを省くためだけにとりあえず置いてます
        /// </summary>
        protected List<Processor> _otherProcessors;

        /// <summary>
        /// ライトバックの回数
        ///     ライトスルーの考慮は今の所なし
        /// </summary>
        public int WriteBackCount { get; protected set; } = 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Protocol(List<Processor> processors) { _processors = processors; }

        /// <summary>
        /// 読み込み処理
        /// </summary>
        public void Read(string address, int PU)
        {
            _targetID = PU;
            _otherProcessors = _processors.Where((_, i) => i != _targetID).ToList();

            var datas = this.SplitAddressToBits(address);
            this.Read(datas[0], Convert.ToInt32(datas[1], 2), Convert.ToInt32(datas[2], 2));
        }

        /// <summary>
        /// 書き込み処理
        /// </summary>
        public void Write(string address, int PU)
        {
            _targetID = PU;
            _otherProcessors = _processors.Where((_, i) => i != _targetID).ToList();

            var datas = this.SplitAddressToBits(address);
            this.Write(datas[0], Convert.ToInt32(datas[1], 2), Convert.ToInt32(datas[2], 2));
        }

        /// <summary>
        /// 読み込み処理 abstract
        /// </summary>
        abstract protected void Read(string tag, int index, int offset);

        /// <summary>
        /// 書き込み処理 abstract
        /// </summary>
        abstract protected void Write(string tag, int index, int offset);

        /// <summary>
        /// アドレスをtag, index, offsetのビット列へ分割し取得
        /// </summary>
        /// <param name="address">2進アドレス列</param>
        protected List<string> SplitAddressToBits(string address)
        {
            // 下位bitから
            var tag = address.Substring(0, 19); // 19bit
            var index = address.Substring(19, 8); // 8bit
            var offset = address.Substring(27, 5); // 5bit(バイトオフセット2bit+ワードオフセット3bit)
        
            return new List<string> { tag, index, offset };
        }
    }
}