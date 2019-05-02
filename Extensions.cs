using System;
using System.Collections.Generic;
using System.Text;

namespace TraceDrivenSimulation
{
    /// <summary>
    /// <see cref="string">型の拡張メソッド群
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// 16進数文字列を2進数文字列に変換する
        /// </summary>
        /// <param name="s">16進数文字列</param>
        /// <param name="min">最小の文字数(足りなかったら0埋め)</param>
        /// <returns>2進数文字列</returns>
        public static string HexToBinary(this string s, int min)
        {
            var temp = Convert.ToInt32(s, 16);
            var binaryStr = Convert.ToString(temp, 2);
            return new string('0', min - binaryStr.Length) + binaryStr;
        }
    }

    /// <summary>
    /// <see cref="IEnumerable<T>">型の拡張メソッド群
    /// </summary>
    public static class IEnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (T item in sequence)
                action(item);
        }
    }
}
