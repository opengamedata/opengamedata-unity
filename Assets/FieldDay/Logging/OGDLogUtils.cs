using System;
using System.Runtime.CompilerServices;
using System.Text;

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

        #region Escape

        /// <summary>
        /// Escapes the given string to JSON.
        /// </summary>
        static internal void EscapeJSON(StringBuilder builder, string text) {
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
        }

        /// <summary>
        /// Escapes the given string to JSON.
        /// </summary>
        static internal unsafe void EscapeJSON(StringBuilder builder, char* textStart, int textLength) {
            if (textStart == null || textLength == 0) {
                return;
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
        }

        /// <summary>
        /// Escapes the given string to JSON.
        /// </summary>
        static internal unsafe void EscapeJSON(StringBuilder builder, ref FixedCharBuffer buffer) {
            EscapeJSON(builder, buffer.Base, buffer.Length);
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
        /// Copies buffer data from one buffer to another.
        /// </summary>
        static internal unsafe void CopyArray<T>(T[] src, int srcOffset, int srcLength, T[] dest, int destOffset) where T : unmanaged {
            int size = sizeof(T);
            Buffer.BlockCopy(src, srcOffset * size, dest, destOffset * size, srcLength * size);
        }

        /// <summary>
        /// Aligns the given value.
        /// </summary>
        [MethodImpl(256)]
        static internal uint AlignUp(uint val, uint align) {
            return (val + align - 1) & ~(align - 1);
        }
    }

    /// <summary>
    /// Fixed character buffer.
    /// </summary>
    internal unsafe struct FixedCharBuffer {
        public readonly char* Base;
        public readonly int Capacity;

        private char* m_WriteHead;
        private int m_Remaining;

        internal FixedCharBuffer(char* buffer, int capacity) {
            Base = buffer;
            Capacity = capacity;
            m_WriteHead = buffer;
            m_Remaining = capacity;
        }

        public void Write(string data) {
            if (string.IsNullOrEmpty(data)) {
                return;
            }

            int length = data.Length;
            if (length > m_Remaining) {
                throw new OutOfMemoryException(string.Format("Cannot write data - fixed buffer would be overrun"));
            }

            fixed(char* dataPtr = data) {
                Buffer.MemoryCopy(dataPtr, m_WriteHead, m_Remaining * sizeof(char), length * sizeof(char));
            }

            m_WriteHead += length;
            m_Remaining -= length;
        }

        public void Write(char data) {
            if (m_Remaining <= 0) {
                throw new OutOfMemoryException(string.Format("Cannot write data - fixed buffer would be overrun"));
            }

            *m_WriteHead++ = data;
            m_Remaining -= 1;
        }
    
        public void Write(long data) {
            // TODO: Make this more efficient
            Write(data.ToString());
        }

        public void Write(float data) {
            // TODO: Make this more efficient
            Write(data.ToString());
        }

        public void Write(bool data) {
            // TODO: Make this more efficient
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
    }
}