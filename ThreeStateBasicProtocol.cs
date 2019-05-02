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
        public enum State
        {
            I,
            C,
            D,
        };

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
                var dirtyProcessor = _processors
                    .Where((_, i) => i != _targetID)
                    .Where(p => p.Cache.GetState(tag, index) == (int)ThreeStateBasicProtocol.State.D);
                
                if (dirtyProcessor.Count() == 0)
                {
                    // NOTE: 本当はここでメモリから読むこむ処理
                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer("", tag, index))
                        this.WriteBackCount++;

                    _processors[_targetID].Cache.SetState(tag, index, (int)ThreeStateBasicProtocol.State.C);
                }
                else if (dirtyProcessor.Count() == 1)
                {
                    this.WriteBackCount++; // 無条件にメモリへライトバック
                    dirtyProcessor.First().Cache.SetState(tag, index, (int)ThreeStateBasicProtocol.State.C);

                    // NOTE: 本当はここでメモリから読むこむ処理
                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer("", tag, index))
                        this.WriteBackCount++;
                    _processors[_targetID].Cache.SetState(tag, index, (int)ThreeStateBasicProtocol.State.C);
                }
                else
                    throw new Exception();
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
                _processors
                    .Where((_, i) => i != _targetID)
                    .Where(p => p.Cache.GetState(tag, index) == (int)ThreeStateBasicProtocol.State.C)
                    .ForEach(p => p.Cache.SetState(tag, index, (int)ThreeStateBasicProtocol.State.I));
            }
            else if (message == CPU.BusMessage.WriteMiss)
            {
                _processors
                    .Where((_, i) => i != _targetID)
                    .Where(p => p.Cache.GetState(tag, index) == (int)ThreeStateBasicProtocol.State.C)
                    .ForEach(p => p.Cache.SetState(tag, index, (int)ThreeStateBasicProtocol.State.I));

                if (_processors.Any(p => p.Cache.GetState(tag, index) == (int)ThreeStateBasicProtocol.State.D))
                {
                    var dirtyProcessor = _processors
                        .Where((_, i) => i != _targetID)
                        .Where(p => p.Cache.GetState(tag, index) == (int)ThreeStateBasicProtocol.State.D)
                        .FirstOrDefault();

                    dirtyProcessor.Cache.SetState(tag, index, (int)ThreeStateBasicProtocol.State.I);
                    // NOTE: メモリにライトバック
                    this.WriteBackCount++;

                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer("", tag, index))
                        this.WriteBackCount++;

                    _processors[_targetID].Cache.SetState(tag, index, (int)ThreeStateBasicProtocol.State.C);
                    // NOTE: Write
                    _processors[_targetID].Cache.SetState(tag, index, (int)ThreeStateBasicProtocol.State.D);
                }
                else
                {
                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer("", tag, index))
                        this.WriteBackCount++;

                    _processors[_targetID].Cache.SetState(tag, index, (int)ThreeStateBasicProtocol.State.D);
                }
            }
            else
                throw new NotSupportedException();
        }
    }
}