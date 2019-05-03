using System;
using System.Collections.Generic;
using System.Linq;

namespace TraceDrivenSimulation
{
    /// <summary>
    /// キャッシュデータのタグ
    /// </summary>
    class CacheTag
    {
        /// <summary>
        /// タグ(アドレス上位ビット)
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// 状態
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// LRU用カウンタ
        /// </summary>
        public int Counter { get; set; }
    }

    /// <summary>
    /// キャッシュライン
    /// </summary>
    /// <remarks>キャッシュするデータにタグをつけたもの</remarks>
    class Line
    {
        /// <summary>
        /// データ(今回は不使用)
        /// 32Byte
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// キャッシュのタグ
        /// </summary>
        public CacheTag CacheTag { get; set; }
    }

    /// <summary>
    /// ラインのLRUセット
    /// </summary>
    /// <remarks>LRUは簡易実装</remarks>
    class LineSetLRU
    {
        private int _setSize;
        private List<Line> _lineSet;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="setSize">セットに含まれるラインの数</param>
        public LineSetLRU(int setSize)
        {
            _setSize = setSize;
            _lineSet = new List<Line>(setSize);
        }

        /// <summary>
        /// インデクサ
        /// </summary>
        public Line this[int index]
        {
            get { return _lineSet[index]; }
        }

        /// <summary>
        /// タグが等しいラインを取得
        /// </summary>
        public Line GetLineByTag(string tag)
        {
            var lines = _lineSet.Where(l => l.CacheTag.Tag == tag);
            return (lines.Count() == 0) ? null : lines.First();
        }

        /// <summary>
        /// セットへ追加(LRU Policy)
        /// </summary>
        /// <returns>追い出しがあったら状態, なかったら-1</returns>
        public int Push(Line line)
        {
            Func<Line, bool> tagEqual = (l => l.CacheTag.Tag == line.CacheTag.Tag);

            if (_lineSet.Count == 0)
                _lineSet.Add(line);
            else if (_lineSet.Any(tagEqual))
            {
                var targetLine = _lineSet.Where(tagEqual).FirstOrDefault();
                var restLines = _lineSet.Where(l => l.CacheTag.Tag != line.CacheTag.Tag);
                
                targetLine.CacheTag.Counter = 0;
                restLines.ForEach(l => l.CacheTag.Counter++);
                _lineSet = _lineSet.OrderByDescending(l => l.CacheTag.Counter).ToList();
            }
            else
            {
                if (_lineSet.Count < _setSize)
                {
                    foreach (var l in _lineSet)
                        l.CacheTag.Counter++;

                    line.CacheTag.Counter = 0;
                    _lineSet.Add(line);
                }
                else
                {
                    var envictState = _lineSet[0].CacheTag.State;
                    _lineSet.RemoveAt(0); // 先頭要素を削除

                    foreach (var l in _lineSet)
                        l.CacheTag.Counter++;

                    line.CacheTag.Counter = 0;
                    _lineSet.Add(line);

                    return envictState; // 追い出したラインの状態を返す
                }
            }

            return -1;
        }
    }

    /// <summary>
    /// 読み込み後のメッセージ
    /// </summary>
    class ReadContent
    {
        /// <summary>
        /// バスへのメッセージ
        /// </summary>
        public CPU.BusMessage BusMessage { get; set; }

        /// <summary>
        /// キャッシュへの書き込みでWriteBackが生じたか
        /// </summary>
        public bool WriteBacked { get; set; }
    }

    /// <summary>
    /// キャッシュメモリ
    ///     Mappingは4-wayのSetAssociative固定
    /// </summary>
    abstract class Cache
    {
        /// <summary>
        /// キャッシュメモリの動作方式
        /// </summary>
        public enum Protocol
        {
            /// <summary>
            /// 3-State Basic プロトコル
            /// </summary>
            ThreeStateBasic,

            /// <summary>
            /// Berkley プロトコル
            /// </summary>
            Berkley,
        }

        /// <summary>
        /// キャッシュするブロック
        /// </summary>
        protected List<LineSetLRU> _lineDatas;

        /// <summary>
        /// Way数 N-Way Set Associative
        /// </summary>
        public int Way { get; } = 4;

        /// <summary>
        /// キャッシュメモリサイズ
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// ブロックサイズ (とりあえず32Byte固定)
        /// </summary>
        public int BlockSize { get; } = (int)Math.Pow(2,5);

        private int _accessCount = 0;
        protected int _hitCount = 0;

        /// <summary>
        /// キャッシュのヒット率
        /// </summary>
        public double HitRate { get { return (double)_hitCount / _accessCount; } }
        /// <summary>
        /// キャッシュのミス率
        /// </summary>
        public double MissRate { get { return 1 - this.HitRate; } }


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="size">キャッシュサイズ</param>
        public Cache(int size)
        {
            this.Size = size;

            // キャッシュの行サイズ
            int index_num = this.Size / this.BlockSize / this.Way;
            _lineDatas = new List<LineSetLRU>(index_num);

            for (int i = 0; i < index_num; ++i)
                _lineDatas.Add(new LineSetLRU(this.Way)); 
        }

        /// <summary>
        /// 対象のラインの状態を取得する
        /// </summary>
        /// <returns>-1:対象ラインは存在しない</returns>
        public int GetState(string tag, int index)
        {
            var line = _lineDatas[index].GetLineByTag(tag);
            return (line != null) ? line.CacheTag.State : -1;
        }

        /// <summary>
        /// 対象のラインの状態を書き換える
        /// </summary>
        public void SetState(string tag, int index, int state)
        {
            var line = _lineDatas[index].GetLineByTag(tag);

            if (line != null)
                line.CacheTag.State = state;
        }

        /// <summary>
        /// ラインを持っているか
        /// </summary>
        public bool HasLine(string tag, int index)
        {
            return (_lineDatas[index].GetLineByTag(tag) == null) ? false : true;
        }

        /// <summary>
        /// ラインのデータを取得
        /// </summary>
        public object GetLineData(string tag, int index)
        {
            var line = _lineDatas[index].GetLineByTag(tag);
            return (line != null) ? line.Data : null;
        }

        /// <summary>
        /// キャッシュから対象ブロックを読み込む
        ///     今回の課題では実際にはデータを返さない
        /// </summary>
        public ReadContent Read(string tag, int index, int offset)
        {
            _accessCount++;

            var targetLine = _lineDatas[index].GetLineByTag(tag);
            return this.Read(targetLine, _lineDatas[index]);
        }

        /// <summary>
        /// キャッシュへ対象ブロックを書き込む
        /// </summary>
        public CPU.BusMessage Write(object data, string tag, int index, int offset)
        {
            _accessCount++;

            var targetLine = _lineDatas[index].GetLineByTag(tag);
            return this.Write(data, targetLine, _lineDatas[index]);
        }

        /// <summary>
        /// キャッシュから対象ブロックを読み込む abstract
        /// </summary>
        abstract protected ReadContent Read(Line targetLine, LineSetLRU targetSet);

        /// <summary>
        /// キャッシュへ対象ブロックを書き込む abstract
        /// </summary>
        abstract protected CPU.BusMessage Write(object data, Line targetLine, LineSetLRU targetSet);

        /// <summary>
        /// メモリ/キャッシュからの転送データを書き込む abstract
        /// </summary>
        abstract public bool Transfer(object data, string tag, int index);
    }
}