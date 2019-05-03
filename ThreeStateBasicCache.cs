using System;
using System.Collections.Generic;
using System.Linq;

namespace TraceDrivenSimulation
{
    /// <summary>
    /// 3-State Basicキャッシュメモリ
    /// </summary>
    class ThreeStateBasicCache : Cache
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="size">キャッシュサイズ</param>
        public ThreeStateBasicCache(int size) : base(size) { }

        /// <summary>
        /// キャッシュから対象ブロックを読み込む
        ///     今回の課題では実際にはデータを返さない
        /// </summary>
        protected override ReadContent Read(Line targetLine, LineSetLRU targetSet)
        {
            if (targetLine == null || // 対象のインデックスのセットにデータが入っていない
                targetLine.CacheTag.State == (int)ThreeStateBasicProtocol.State.I)
            {
                return new ReadContent { BusMessage = CPU.BusMessage.ReadMiss, WriteBacked = false };
            }
            else if (targetLine.CacheTag.State == (int)ThreeStateBasicProtocol.State.C ||
                        targetLine.CacheTag.State == (int)ThreeStateBasicProtocol.State.D)
            {
                _hitCount++;
                targetSet.Push(targetLine);
                // NOTE: キャッシュがCorDのとき, 絶対に追い出しは起こらない
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
                targetLine.CacheTag.State == (int)ThreeStateBasicProtocol.State.I)
            {
                return CPU.BusMessage.WriteMiss;
            }
            else if (targetLine.CacheTag.State == (int)ThreeStateBasicProtocol.State.C)
            {
                _hitCount++;
                targetLine.CacheTag.State = (int)ThreeStateBasicProtocol.State.D;
                targetLine.Data = data;
                targetSet.Push(targetLine);
                return CPU.BusMessage.Invalidation;
            }
            else if (targetLine.CacheTag.State == (int)ThreeStateBasicProtocol.State.D)
            {
                _hitCount++;
                targetLine.Data = data;
                targetSet.Push(targetLine);
                return CPU.BusMessage.None;
            }
            else
                throw new NotSupportedException();
        }

        /// <summary>
        /// メモリからの転送データを書き込む
        /// </summary>
        /// <returns>Write-backがあるかどうか</returns>
        public override bool Transfer(object data, string tag, int index)
        {
            var transLine = new Line { CacheTag = new CacheTag { Tag = tag }, Data = data };
            var evictionState = _lineDatas[index].Push(transLine);

            if (evictionState == (int)ThreeStateBasicProtocol.State.D)
            {
                // NOTE: 本当は呼び出し元でメモリにライトバック
                return true;
            }
            
            return false;
        }
    }
}