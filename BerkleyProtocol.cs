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
        public enum State
        {
            I,
            SN,
            SO,
            EO,
        };

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
                var ownerProcessor = _processors
                    .Where((_, i) => i != _targetID)
                    .Where(p => p.Cache.GetState(tag, index) == (int)BerkleyProtocol.State.SO ||
                                p.Cache.GetState(tag, index) == (int)BerkleyProtocol.State.EO);
                
                if (ownerProcessor.Count() == 0)
                {
                    // NOTE: 本当はここでメモリから読むこむ処理
                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer("", tag, index))
                        this.WriteBackCount++;

                    _processors[_targetID].Cache.SetState(tag, index, (int)BerkleyProtocol.State.SN);
                }
                else if (ownerProcessor.Count() == 1)
                {
                    if (ownerProcessor.First().Cache.GetState(tag, index) == (int)BerkleyProtocol.State.EO)
                        ownerProcessor.First().Cache.SetState(tag, index, (int)BerkleyProtocol.State.SO);

                    // NOTE: 本当はここでメモリから読むこむ処理
                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer("", tag, index))
                        this.WriteBackCount++;
                    _processors[_targetID].Cache.SetState(tag, index, (int)BerkleyProtocol.State.SN);
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
                    .Where(p => p.Cache.GetState(tag, index) == (int)BerkleyProtocol.State.SN ||
                                p.Cache.GetState(tag, index) == (int)BerkleyProtocol.State.SO)
                    .ForEach(p => p.Cache.SetState(tag, index, (int)BerkleyProtocol.State.I));
            }
            else if (message == CPU.BusMessage.WriteMiss)
            {
                _processors
                    .Where((_, i) => i != _targetID)
                    .Where(p => p.Cache.GetState(tag, index) == (int)BerkleyProtocol.State.EO ||
                                p.Cache.GetState(tag, index) == (int)BerkleyProtocol.State.SN || // 結局最後にSNとSOもInvalidateされる
                                p.Cache.GetState(tag, index) == (int)BerkleyProtocol.State.SO)
                    .ForEach(p => p.Cache.SetState(tag, index, (int)BerkleyProtocol.State.I));

                if (_processors.Any(p => p.Cache.GetState(tag, index) == (int)BerkleyProtocol.State.SO ||
                                         p.Cache.GetState(tag, index) == (int)BerkleyProtocol.State.EO))
                {
                    var ownerProcessor = _processors
                        .Where((_, i) => i != _targetID)
                        .Where(p => p.Cache.GetState(tag, index) == (int)BerkleyProtocol.State.SO ||
                                    p.Cache.GetState(tag, index) == (int)BerkleyProtocol.State.EO);

                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer(ownerProcessor.First().Cache.GetLineData(tag, index), tag, index))
                        this.WriteBackCount++;

                    _processors[_targetID].Cache.SetState(tag, index, (int)BerkleyProtocol.State.SN);

                    // NOTE: Write
                    _processors[_targetID].Cache.SetState(tag, index, (int)BerkleyProtocol.State.EO);
                }
                else
                {
                    // Line Transfer
                    if (_processors[_targetID].Cache.Transfer("", tag, index)) // メモリがowner!
                        this.WriteBackCount++;

                    _processors[_targetID].Cache.SetState(tag, index, (int)BerkleyProtocol.State.EO);
                }
            }
            else
                throw new NotSupportedException();
        }
    }
}