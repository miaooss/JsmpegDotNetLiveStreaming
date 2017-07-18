using System;

namespace LiveStreamingWebRTC.Utils
{
    public static class CollectionExtensions
    {
        public static T[] ResizeArray<T>(this T[] sourceArray, int startIndex, int readlength)
        {
            if (sourceArray == null) throw new ArgumentNullException("sourceArray");
            if (startIndex < 0) throw new ArgumentException("startIndex should be >= 0");
            if (readlength < 0 || readlength > sourceArray.Length) throw new ArgumentException("readlength should be >= 0 and readlength < sourceArray.Length");
            if (startIndex == 0 && sourceArray.Length == readlength) return sourceArray;

            var resultLength = readlength - startIndex;
            var resultArray = new T[resultLength];
            Buffer.BlockCopy(sourceArray, startIndex, resultArray, 0, resultLength);
            return resultArray;
        }
    }
}
