using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace FieldDay {
    static public class OGDLogUtils {
        #region Consts

        static private byte[] EscapePostData_Space = Encoding.ASCII.GetBytes("%20");
        static private byte[] EscapePostData_Forbidden = Encoding.ASCII.GetBytes("@&;:<>=?\"'/\\!#%+$,{}|^[]`");
        static private byte[] EscapePostData_HexSequence = Encoding.ASCII.GetBytes("0123456789ABCDEF");
        private const byte EscapePostData_EscapeChar = (byte) '%';

        #endregion // Consts

        /// <summary>
        /// Returns a 17-digit unique identifier using the current datetime.
        /// </summary>
        static public long UUIDint() {
            long uuid = 0;

            DateTime now = DateTime.Now;

            uuid = UUIDAccumulate(uuid, now.Year, 100);
            uuid = UUIDAccumulate(uuid, now.Month, 100);
            uuid = UUIDAccumulate(uuid, now.Day, 100);
            uuid = UUIDAccumulate(uuid, now.Hour, 100);
            uuid = UUIDAccumulate(uuid, now.Minute, 100);
            uuid = UUIDAccumulate(uuid, now.Second, 100);
            uuid = UUIDAccumulate(uuid, UnityEngine.Random.Range(0, 10), 10);
            uuid = UUIDAccumulate(uuid, UnityEngine.Random.Range(0, 10), 10);
            uuid = UUIDAccumulate(uuid, UnityEngine.Random.Range(0, 10), 10);
            uuid = UUIDAccumulate(uuid, UnityEngine.Random.Range(0, 10), 10);
            uuid = UUIDAccumulate(uuid, UnityEngine.Random.Range(0, 10), 10);

            return uuid;
        }

        // Accumulates by multiplying by the given factor and adding.
        [MethodImpl(256)]
        static private long UUIDAccumulate(long uuid, int input, int multiply) {
            return (uuid * multiply) + (input % multiply);
        }

        /// <summary>
        /// Stringifies the given object to JSON.
        /// Note that Unity must consider this object to be [Serializable]
        /// in order for this function to return a valid JSON string.
        /// </summary>
        static public string Stringify(object obj) {
            return JsonUtility.ToJson(obj);
        }

        #region Escape

        /// <summary>
        /// Escapes the given string to JSON.
        /// </summary>
        static internal StringBuilder EscapeJSON(this StringBuilder builder, string text) {
            if (text == null || text.Length == 0) {
                return builder;
            }

            unsafe {
                fixed(char* textPin = text) {
                    char* ptr = textPin;
                    char* end = textPin + text.Length;
                    char c;
                    while(ptr != end) {
                        switch((c = *ptr++)) {
                            case '\\': {
                                builder.Append("\\\\");
                                break;
                            }
                            case '\"': {
                                builder.Append("\\\"");
                                break;
                            }
                            case '\n': {
                                builder.Append("\\n");
                                break;
                            }
                            case '\r': {
                                builder.Append("\\r");
                                break;
                            }
                            case '\t': {
                                builder.Append("\\t");
                                break;
                            }
                            case '\b': {
                                builder.Append("\\b");
                                break;
                            }
                            case '\f': {
                                builder.Append("\\f");
                                break;
                            }
                            default: {
                                builder.Append(c);
                                break;
                            }
                        }
                    }
                }
            }

            return builder;
        }

        /// <summary>
        /// Escapes the given string to JSON.
        /// </summary>
        static internal StringBuilder EscapeJSON(this StringBuilder builder, StringBuilder text) {
            if (text == null || text.Length == 0) {
                return builder;
            }

            char c;
            int i = 0;
            int end = text.Length;
            while(i != end) {
                switch((c = text[i++])) {
                    case '\\': {
                        builder.Append("\\\\");
                        break;
                    }
                    case '\"': {
                        builder.Append("\\\"");
                        break;
                    }
                    case '\n': {
                        builder.Append("\\n");
                        break;
                    }
                    case '\r': {
                        builder.Append("\\r");
                        break;
                    }
                    case '\t': {
                        builder.Append("\\t");
                        break;
                    }
                    case '\b': {
                        builder.Append("\\b");
                        break;
                    }
                    case '\f': {
                        builder.Append("\\f");
                        break;
                    }
                    default: {
                        builder.Append(c);
                        break;
                    }
                }
            }
            
            return builder;
        }

        /// <summary>
        /// Escapes the given string to JSON.
        /// </summary>
        static internal unsafe StringBuilder EscapeJSON(this StringBuilder builder, char* textStart, int textLength) {
            if (textStart == null || textLength == 0) {
                return builder;
            }

            char* ptr = textStart;
            char* end = textStart + textLength;
            char c;
            while(ptr != end) {
                switch((c = *ptr++)) {
                    case '\\': {
                        builder.Append("\\\\");
                        break;
                    }
                    case '\"': {
                        builder.Append("\\\"");
                        break;
                    }
                    case '\n': {
                        builder.Append("\\n");
                        break;
                    }
                    case '\r': {
                        builder.Append("\\r");
                        break;
                    }
                    case '\t': {
                        builder.Append("\\t");
                        break;
                    }
                    case '\b': {
                        builder.Append("\\b");
                        break;
                    }
                    case '\f': {
                        builder.Append("\\f");
                        break;
                    }
                    default: {
                        builder.Append(c);
                        break;
                    }
                }
            }

            return builder;
        }

        /// <summary>
        /// Escapes the given string to JSON.
        /// </summary>
        static internal unsafe StringBuilder EscapeJSON(this StringBuilder builder, ref FixedCharBuffer buffer) {
            return EscapeJSON(builder, buffer.Base, buffer.Length);
        }

        /// <summary>
        /// Escapes the given string to JSON.
        /// </summary>
        static internal void EscapeJSON(ref FixedCharBuffer buffer, string text) {
            if (text == null || text.Length == 0) {
                return;
            }

            unsafe {
                fixed(char* textPin = text) {
                    char* ptr = textPin;
                    char* end = textPin + text.Length;
                    char c;
                    while(ptr != end) {
                        switch((c = *ptr++)) {
                            case '\\': {
                                buffer.Write("\\\\");
                                break;
                            }
                            case '\"': {
                                buffer.Write("\\\"");
                                break;
                            }
                            case '\n': {
                                buffer.Write("\\n");
                                break;
                            }
                            case '\r': {
                                buffer.Write("\\r");
                                break;
                            }
                            case '\t': {
                                buffer.Write("\\t");
                                break;
                            }
                            case '\b': {
                                buffer.Write("\\b");
                                break;
                            }
                            case '\f': {
                                buffer.Write("\\f");
                                break;
                            }
                            default: {
                                buffer.Write(c);
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Escapes the given string to JSON.
        /// </summary>
        static internal void EscapeJSON(ref FixedCharBuffer buffer, StringBuilder text) {
            if (text == null || text.Length == 0) {
                return;
            }

            char c;
            int i = 0;
            int end = text.Length;
            while(i != end) {
                switch((c = text[i++])) {
                    case '\\': {
                        buffer.Write("\\\\");
                        break;
                    }
                    case '\"': {
                        buffer.Write("\\\"");
                        break;
                    }
                    case '\n': {
                        buffer.Write("\\n");
                        break;
                    }
                    case '\r': {
                        buffer.Write("\\r");
                        break;
                    }
                    case '\t': {
                        buffer.Write("\\t");
                        break;
                    }
                    case '\b': {
                        buffer.Write("\\b");
                        break;
                    }
                    case '\f': {
                        buffer.Write("\\f");
                        break;
                    }
                    default: {
                        buffer.Write(c);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Escapes the given string to JSON.
        /// </summary>
        static internal void EscapeJSON(ref FixedCharBuffer buffer, FixedCharBuffer src) {
            unsafe {
                if (src.Base == null || src.Length == 0) {
                    return;
                }

                char c;
                int i = 0;
                int end = src.Length;
                while(i != end) {
                    switch((c = *(src.Base + i++))) {
                        case '\\': {
                            buffer.Write("\\\\");
                            break;
                        }
                        case '\"': {
                            buffer.Write("\\\"");
                            break;
                        }
                        case '\n': {
                            buffer.Write("\\n");
                            break;
                        }
                        case '\r': {
                            buffer.Write("\\r");
                            break;
                        }
                        case '\t': {
                            buffer.Write("\\t");
                            break;
                        }
                        case '\b': {
                            buffer.Write("\\b");
                            break;
                        }
                        case '\f': {
                            buffer.Write("\\f");
                            break;
                        }
                        default: {
                            buffer.Write(c);
                            break;
                        }
                    }
                }
            }
        }
    
        /// <summary>
        /// Escapes the given buffer to JSON over itself.
        /// </summary>
        static internal void EscapeJSONInline(ref FixedCharBuffer buffer) {
            unsafe {
                int length = buffer.Length;
                char* copy = stackalloc char[length];
                FixedCharBuffer temp = new FixedCharBuffer("temp", copy, length);
                temp.Write(buffer.Base, buffer.Length);
                buffer.Clear();
                EscapeJSON(ref buffer, temp);
            }
        }

        /// <summary>
        /// Escapes the given post data to ensure it can be passed in the request body.
        /// </summary>
        static internal unsafe int EscapePostData(byte[] src, int srcOffset, int srcLength, byte[] dest, int destOffset) {
            fixed(byte* destPtr = dest) {
                byte* writeHead = destPtr + destOffset;
                int writtenSize = 0;
                for(int i = srcOffset, end = srcOffset + srcLength; i < end; i++) {
                    byte b = src[i];
                    if (b == 32) { // space
                        WriteToDest(&writeHead, &writtenSize, EscapePostData_Space);
                    } else if (b < 32 || b > 126 || Array.IndexOf(EscapePostData_Forbidden, b) >= 0) {
                        WriteToDest(&writeHead, &writtenSize, EscapePostData_EscapeChar);
                        WriteToDest(&writeHead, &writtenSize, EscapePostData_HexSequence[b >> 4]);
                        WriteToDest(&writeHead, &writtenSize, EscapePostData_HexSequence[b & 0xF]);
                    } else {
                        WriteToDest(&writeHead, &writtenSize, b);
                    }
                }
                return writtenSize;
            }
        }

        static private unsafe void WriteToDest(byte** dest, int* size, byte[] data) {
            for(int i = 0; i < data.Length; i++) {
                **dest = data[i];
                (*dest)++;
                (*size)++;
            }
        }

        static private unsafe void WriteToDest(byte** dest, int* size, byte data) {
            **dest = data;
            (*dest)++;
            (*size)++;
        }

        #endregion // Escape

        #region Numbers

        /// <summary>
        /// Writes an integer to a StringBuilder without allocating GC memory.
        /// </summary>
        static internal unsafe StringBuilder AppendInteger(this StringBuilder builder, long integer, int padLeft) {
            if (padLeft > 20) {
                padLeft = 20;
            }

            char* buffer = stackalloc char[32];
            int idx = 31;
            int minIdx = idx - padLeft;
            bool negative = integer < 0;
            if (negative)
            {
                integer = -integer;
            }
            do
            {
                buffer[idx--] = (char) ('0' + (integer % 10));
                integer /= 10;
            }
            while(integer != 0 || idx > minIdx);

            if (negative)
            {
                buffer[idx--] = '-';
            }

            return builder.Append(buffer + idx + 1, 31 - idx);
        }

        /// <summary>
        /// Writes an integer to a FixedCharBuffer without allocating GC memory.
        /// </summary>
        static internal unsafe void WriteInteger(ref FixedCharBuffer builder, long integer, int padLeft) {
            if (padLeft > 20) {
                padLeft = 20;
            }

            char* buffer = stackalloc char[32];
            int idx = 31;
            int minIdx = idx - padLeft;
            bool negative = integer < 0;
            if (negative)
            {
                integer = -integer;
            }
            do
            {
                buffer[idx--] = (char) ('0' + (integer % 10));
                integer /= 10;
            }
            while(integer != 0 || idx > minIdx);

            if (negative)
            {
                buffer[idx--] = '-';
            }

            builder.Write(buffer + idx + 1, 31 - idx);
        }

        /// <summary>
        /// Writes a floating point value to a StringBuilder without allocating GC memory.
        /// </summary>
        static internal unsafe StringBuilder AppendNumber(StringBuilder builder, double value, int padLeft, int precision)
        {
            if (double.IsNaN(value))
            {
                return builder.Append("NaN");
            }
            else if (double.IsPositiveInfinity(value))
            {
                return builder.Append("Infinity");
            }
            else if (double.IsNegativeInfinity(value))
            {
                return builder.Append("-Infinity");
            }

            if (value < 0)
            {
                builder.Append('-');
                value = -value;
            }

            if (precision > 20)
            {
                precision = 20;
            }

            if (precision >= 0)
            {
                value += s_RoundOffsets[precision];
            }

            long integerValue = (long) value;
            AppendInteger(builder, integerValue, padLeft);

            if (precision == 0)
            {
                return builder;
            }

            value -= integerValue;

            if (precision < 0 && value == 0)
            {
                return builder;
            }

            int minLength = precision;
            int maxLength = precision < 0 ? 4 : precision;

            builder.Append('.');

            int charsWritten = 0;
            int digit;
            do
            {
                value = value * 10;
                digit = ((int) value % 10);
                builder.Append((char) ('0' + digit));
                value -= digit;
                charsWritten++;
            }
            while(charsWritten < minLength || (charsWritten < maxLength && (value != 0)));

            return builder;
        }

        /// <summary>
        /// Writes a floating point value to a FixedCharBuffer without allocating GC memory.
        /// </summary>
        static internal unsafe void WriteNumber(ref FixedCharBuffer builder, double value, int padLeft, int precision)
        {
            if (double.IsNaN(value))
            {
                builder.Write("NaN");
                return;
            }
            else if (double.IsPositiveInfinity(value))
            {
                builder.Write("Infinity");
                return;
            }
            else if (double.IsNegativeInfinity(value))
            {
                builder.Write("-Infinity");
                return;
            }

            if (value < 0)
            {
                builder.Write('-');
                value = -value;
            }

            if (precision > 20)
            {
                precision = 20;
            }

            if (precision >= 0)
            {
                value += s_RoundOffsets[precision];
            }

            long integerValue = (long) value;
            WriteInteger(ref builder, integerValue, padLeft);

            if (precision == 0)
            {
                return;
            }

            value -= integerValue;

            if (precision < 0 && value == 0)
            {
                return;
            }

            int minLength = precision;
            int maxLength = precision < 0 ? 4 : precision;

            builder.Write('.');

            int charsWritten = 0;
            int digit;
            do
            {
                value = value * 10;
                digit = ((int) value % 10);
                builder.Write((char) ('0' + digit));
                value -= digit;
                charsWritten++;
            }
            while(charsWritten < minLength || (charsWritten < maxLength && (value != 0)));
        }

        static private readonly double[] s_RoundOffsets = new double[] { 0.5, 0.05, 0.005, 0.0005, 0.00005, 0.000005, 0.0000005, 0.00000005, 0.000000005, 0.0000000005, 0.00000000005 };

        #endregion // Numbers

        /// <summary>
        /// Aligns the given value.
        /// </summary>
        [MethodImpl(256)]
        static internal uint AlignUp(uint val, uint align) {
            return (val + align - 1) & ~(align - 1);
        }

        /// <summary>
        /// Trims characters from the end of the StringBuilder.
        /// </summary>
        static internal void TrimEnd(StringBuilder builder, char endChar) {
            int length = builder.Length;
            while(length > 0 && builder[length - 1] == endChar) {
                length--;
            }
            builder.Length = length;
        }

        /// <summary>
        /// Appends the contents of a FixedCharBuffer to a StringBuilder.
        /// </summary>
        static internal unsafe StringBuilder AppendBuffer(this StringBuilder builder, ref FixedCharBuffer buffer) {
            return builder.Append(buffer.Base, buffer.Length);
        }

        /// <summary>
        /// Copies buffer data from one buffer to another.
        /// </summary>
        static internal unsafe void CopyArray<T>(T[] src, int srcOffset, int srcLength, T[] dest, int destOffset) where T : unmanaged {
            int size = sizeof(T);
            Buffer.BlockCopy(src, srcOffset * size, dest, destOffset * size, srcLength * size);
        }

        /// <summary>
        /// Copies buffer data from one buffer to another.
        /// </summary>
        static internal unsafe void CopyArray<T>(T[] src, int srcOffset, int srcLength, T* dest, int destOffset, int destLength) where T : unmanaged {
            int size = sizeof(T);
            fixed(T* srcPtr = src) {
                Buffer.MemoryCopy(srcPtr + srcOffset, dest + destOffset, (destLength - destOffset) * size, srcLength * size);
            }
        }
    }

    /// <summary>
    /// Fixed character buffer.
    /// </summary>
    internal unsafe struct FixedCharBuffer {
        public readonly char* Base;
        public readonly int Capacity;
        public readonly string Name;

        public char* Tail {
            get { return Base + Capacity; }
        }

        private char* m_WriteHead;
        private int m_Remaining;

        internal FixedCharBuffer(string name, char* buffer, int capacity) {
            Base = buffer;
            Capacity = capacity;
            m_WriteHead = buffer;
            m_Remaining = capacity;
            Name = name;
        }

        public void Write(string data) {
            if (string.IsNullOrEmpty(data)) {
                return;
            }

            int length = data.Length;
            if (length > m_Remaining) {
                throw GetWriteException(length);
            }

            fixed(char* dataPtr = data) {
                Buffer.MemoryCopy(dataPtr, m_WriteHead, m_Remaining * sizeof(char), length * sizeof(char));
            }

            m_WriteHead += length;
            m_Remaining -= length;
        }

        public void Write(char data) {
            if (m_Remaining <= 0) {
                throw GetWriteException(1);
            }

            *m_WriteHead++ = data;
            m_Remaining -= 1;
        }

        public void Write(char* buffer, int length) {
            if (length <= 0) {
                return;
            }

            if (length > m_Remaining) {
                throw GetWriteException(length);
            }

            Buffer.MemoryCopy(buffer, m_WriteHead, m_Remaining * sizeof(char), length * sizeof(char));

            m_WriteHead += length;
            m_Remaining -= length;
        }
    
        public void Write(long data) {
            OGDLogUtils.WriteInteger(ref this, data, 0);
        }

        public void Write(double data, int precision) {
            OGDLogUtils.WriteNumber(ref this, data, 0, precision);
        }

        public void Write(bool data) {
            Write(data ? "true" : "false");
        }

        public void Backspace(int count) {
            if (count < 0) {
                return;
            }

            if (count > Length) {
                throw new ArgumentOutOfRangeException("count");
            }

            m_WriteHead -= count;
            m_Remaining += count;
        }

        public void NullTerminate() {
            Write('\0');
        }

        public void TrimEnd(char endChar) {
            while(m_WriteHead > Base && *(m_WriteHead - 1) == endChar) {
                m_WriteHead--;
                m_Remaining++;
            }
        }

        public void Clear() {
            m_WriteHead = Base;
            m_Remaining = Capacity;
        }

        public int Length {
            get { return Capacity - m_Remaining; }
        }

        public override string ToString() {
            if (Base != null) {
                return new string(Base, 0, Length);
            } else {
                return null;
            }
        }
    
        private Exception GetWriteException(int writeLength) {
            return new OutOfMemoryException(string.Format("Cannot write {0} bytes to buffer '{1}' - only {2} bytes available of {3}", writeLength, Name, m_Remaining, Capacity));
        }
    }
}