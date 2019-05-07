using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace TraceDrivenSimulation
{
    /// <summary>
    /// Berkleyプロトコル 制御クラス
    /// </summary>
    class BerkleyProtocol : Protocol
    {
        /// <summary>
        /// キャッシュの状態
        /// </summary>
        [Flags]
        public enum State
        {
            I = 1,
            SN = 1 << 1,
            SO = 1 << 2,
            EO = 1 << 3,
        };

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public BerkleyProtocol(List<Processor> processors) : base(processors) { }

        /// <summary>
        /// 読み込み処理
        /// </summary>
        protected override void Read(string tag, int index, int offset)
        {
            var readResult = _processors[_targetID].Cache.Read(tag, index, offset);
            var message = readResult.BusMessage;

            if (message == CPU.BusMessage.None)
            {
                // do nothing... あとで消すかも
            }
            else if (message == CPU.BusMessage.ReadMiss)
            {
                var ownerCache = _otherProcessors
                    .Where(p => p.Cache.AnyState(tag, index, (int)(State.SO | State.EO)))
                    .Select(p => p.Cache)
                    .FirstOrDefault();
                
                if (ownerCache == null) // メモリがOwner
                {
                    // NOTE: 本当はここでメモリから読むこむ処理
                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer("", tag, index))
                        this.WriteBackCount++;

                    _processors[_targetID].Cache.SetState(tag, index, (int)State.SN);
                }
                else // 他キャッシュがOwner
                {
                    if (ownerCache.GetState(tag, index) == (int)State.EO)
                        ownerCache.SetState(tag, index, (int)State.SO);

                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer(ownerCache.GetLineData(tag, index), tag, index))
                        this.WriteBackCount++;
                    _processors[_targetID].Cache.SetState(tag, index, (int)State.SN);
                }
            }
            else
                throw new NotSupportedException();
        }

        /// <summary>
        /// 書き込み処理
        /// </summary>
        protected override void Write(string tag, int index, int offset)
        {
            var message = _processors[_targetID].Cache.Write("", tag, index, offset);

            if (message == CPU.BusMessage.None)
            {
                // do nothing... あとで消すかも
            }
            else if (message == CPU.BusMessage.Invalidation)
            {
                _otherProcessors
                    .Where(p => p.Cache.AnyState(tag, index, (int)(State.SN | State.SO)))
                    .ForEach(p => p.Cache.SetState(tag, index, (int)State.I));
            }
            else if (message == CPU.BusMessage.WriteMiss)
            {
                var ownerCache = _otherProcessors
                    .Where(p => p.Cache.AnyState(tag, index, (int)(State.SO | State.EO)))
                    .Select(p => p.Cache)
                    .FirstOrDefault();

                if (ownerCache == null) // メモリがOwner
                {
                    // NOTE: 本当はここでメモリから読むこむ処理
                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer("", tag, index))
                        this.WriteBackCount++;

                    _processors[_targetID].Cache.SetState(tag, index, (int)State.EO);
                }
                else // 多キャッシュがOwner
                {
                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer(ownerCache.GetLineData(tag, index), tag, index))
                        this.WriteBackCount++;

                    _processors[_targetID].Cache.SetState(tag, index, (int)State.SN);

                    // NOTE: Write
                    _processors[_targetID].Cache.SetState(tag, index, (int)State.EO);
                }

                // 最終的に対象キャッシュ以外はすべてInvalidate
                _otherProcessors
                    .Where(p => p.Cache.AnyState(tag, index, (int)(State.SN | State.SO | State.EO)))
                    .ForEach(p => p.Cache.SetState(tag, index, (int)State.I));
            }
            else
                throw new NotSupportedException();
        }
    }
}