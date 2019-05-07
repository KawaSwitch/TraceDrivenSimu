using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace TraceDrivenSimulation
{
    /// <summary>
    /// 3-Stateプロトコル 制御クラス
    /// </summary>
    class ThreeStateBasicProtocol : Protocol
    {
        /// <summary>
        /// キャッシュの状態
        /// </summary>
        [Flags]
        public enum State
        {
            I = 1,
            C = 1 << 1,
            D = 1 << 2,
        };

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ThreeStateBasicProtocol(List<Processor> processors) : base(processors) { }

        /// <summary>
        /// 読み込み処理 バスの処理
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
                var dirtyCache = _otherProcessors
                    .Where(p => p.Cache.GetState(tag, index) == (int)State.D)
                    .Select(p => p.Cache)
                    .FirstOrDefault();
                
                if (dirtyCache == null) // Dirtyなキャッシュが存在しない
                {
                    // NOTE: 本当はここでメモリから読むこむ処理
                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer("", tag, index))
                        this.WriteBackCount++;

                    _processors[_targetID].Cache.SetState(tag, index, (int)State.C);
                }
                else
                {
                    this.WriteBackCount++; // 無条件にメモリへライトバック
                    dirtyCache.SetState(tag, index, (int)State.C);

                    // NOTE: 本当はここでメモリから読むこむ処理
                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer("", tag, index))
                        this.WriteBackCount++;
                    _processors[_targetID].Cache.SetState(tag, index, (int)State.C);
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
                    .Where(p => p.Cache.GetState(tag, index) == (int)State.C)
                    .ForEach(p => p.Cache.SetState(tag, index, (int)State.I));
            }
            else if (message == CPU.BusMessage.WriteMiss)
            {
                // 他キャッシュのClearはInvalidateしておく
                _otherProcessors
                    .Where(p => p.Cache.GetState(tag, index) == (int)State.C)
                    .ForEach(p => p.Cache.SetState(tag, index, (int)State.I));

                var dirtyCache = _otherProcessors
                    .Where(p => p.Cache.GetState(tag, index) == (int)State.D)
                    .Select(p => p.Cache)
                    .FirstOrDefault();    

                if (dirtyCache == null) // Dirtyなキャッシュが存在しない
                {
                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer("", tag, index))
                        this.WriteBackCount++;

                    _processors[_targetID].Cache.SetState(tag, index, (int)State.D);
                }
                else
                {
                    dirtyCache.SetState(tag, index, (int)State.I);
                    // NOTE: メモリにライトバック
                    this.WriteBackCount++;

                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer("", tag, index))
                        this.WriteBackCount++;

                    _processors[_targetID].Cache.SetState(tag, index, (int)State.C);
                    // NOTE: Write
                    _processors[_targetID].Cache.SetState(tag, index, (int)State.D);
                }
            }
            else
                throw new NotSupportedException();
        }
    }
}