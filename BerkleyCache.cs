using System;
using System.Collections.Generic;
using System.Linq;

namespace TraceDrivenSimulation
{
    /// <summary>
    /// Berkleyキャッシュメモリ
    /// </summary>
    class BerkleyCache : Cache
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="size">キャッシュサイズ</param>
        public BerkleyCache(int size) : base(size) { }

        /// <summary>
        /// キャッシュから対象ブロックを読み込む
        ///     今回の課題では実際にはデータを返さない
        /// </summary>
        protected override ReadContent Read(Line targetLine, LineSetLRU targetSet)
        {
            if (targetLine == null || // 対象のインデックスのセットにデータが入っていない
                targetLine.CacheTag.State == (int)BerkleyProtocol.State.I)
            {
                return new ReadContent { BusMessage = CPU.BusMessage.ReadMiss, WriteBacked = false };
            }
            else if (targetLine.CacheTag.State == (int)BerkleyProtocol.State.EO ||
                        targetLine.CacheTag.State == (int)BerkleyProtocol.State.SN ||
                        targetLine.CacheTag.State == (int)BerkleyProtocol.State.SO)
            {
                _hitCount++;
                // NOTE: キャッシュがI以外のとき, 絶対に追い出しは起こらない
                return new ReadContent { BusMessage = CPU.BusMessage.None, WriteBacked = false };
            }
            else
                throw new NotSupportedException();
        }

        /// <summary>
        /// キャッシュへ対象ブロックを書き込む
        /// </summary>
        protected override CPU.BusMessage Write(object data, Line targetLine, LineSetLRU targetSet)
        {
            if (targetLine == null ||
                targetLine.CacheTag.State == (int)BerkleyProtocol.State.I)
            {
                return CPU.BusMessage.WriteMiss;
            }
            else if (targetLine.CacheTag.State == (int)BerkleyProtocol.State.SN ||
                    targetLine.CacheTag.State == (int)BerkleyProtocol.State.SO)
            {
                _hitCount++;
                targetLine.CacheTag.State = (int)BerkleyProtocol.State.EO;
                targetLine.Data = data;
                return CPU.BusMessage.Invalidation;
            }
            else if (targetLine.CacheTag.State == (int)BerkleyProtocol.State.EO)
            {
                _hitCount++;
                targetLine.Data = data;
                return CPU.BusMessage.None;
            }
            else
                throw new NotSupportedException();
        }

        /// <summary>
        /// メモリ/キャッシュからの転送データを書き込む
        /// </summary>
        /// <returns>Write-backがあるかどうか</returns>
        public override bool Transfer(object data, string tag, int index)
        {
            var transLine = new Line { CacheTag = new CacheTag { Tag = tag }, Data = data }; // 本当は外で作る
            var evictionState = _lineDatas[index].Push(transLine);

            if (evictionState == (int)BerkleyProtocol.State.SO ||
                evictionState == (int)BerkleyProtocol.State.EO)
            {
                // NOTE: 本当は呼び出し元でメモリにライトバック
                return true;
            }
            
            return false;
        }
    }
}