using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Mapbox.VectorTile.Contants;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.Data
{
    public ref struct PbfReader
    {
        /// <summary>Tag at current position</summary>
        public int Tag;

        /// <summary>Value at current position</summary>
        public ulong Value;
        //public ulong Pos { get; private set; }
        /// <summary>Wire type at current position</summary>
        public WireTypes WireType;


        private ReadOnlySpan<byte> _buffer;
        private int _length;
        private int _pos;


        /// <summary>
        /// PbfReader constructor
        /// </summary>
        /// <param name="tileBuffer">Byte array containing the raw (already unzipped) tile data</param>
        public PbfReader(ReadOnlySpan<byte> tileBuffer)
        {
            _buffer = tileBuffer;
            _length = _buffer.Length;
            WireType = WireTypes.UNDEFINED;
            _pos = 0;
            Tag = 0;
            Value = 0;
        }


        /// <summary>
        /// <para>Gets Varint at current position, moves to position after Varint.</para>
        /// <para>Throws exception if Varint cannot be decoded</para>
        /// </summary>
        /// <returns>Decoded Varint</returns>
        public long Varint()
        {
            // convert to base 128 varint
            // https://developers.google.com/protocol-buffers/docs/encoding
            int shift = 0;
            long result = 0;
            while (shift < 64)
            {
                byte b = _buffer[_pos];
                result |= (long)(b & 0x7F) << shift;
                _pos++;
                if ((b & 0x80) == 0)
                {
                    return result;
                }
                shift += 7;
            }
            throw new System.ArgumentException("Invalid varint");

        }


        /// <summary>
        /// <para>Get a view into the buffer.</para>
        /// <para>TODO: refactor to return a DataView instead of a byte array</para>
        /// </summary>
        /// <returns>Byte array containing the view</returns>
        public Vector2Int View()
        {
            // return layer/feature subsections of the main stream
            if (Tag == 0)
            {
                throw new System.Exception("call next() before accessing field value");
            };
            if (WireType != WireTypes.BYTES)
            {
                throw new System.Exception("not of type string, bytes or message");
            }

            var skipBytes = Varint();
            SkipBytes((int)skipBytes);

            //byte[] buf = new byte[skipBytes];
            //System.Array.Copy(_buffer, (int)_pos - (int)skipBytes, buf, 0, (int)skipBytes);

            return  new Vector2Int((int)_pos - (int)skipBytes, (int)skipBytes);
        }


        /// <summary>
        /// Get repeated `uint`s a current position, move position
        /// </summary>
        /// <returns>List of decoded `uint`s</returns>
        public uint[] GetPackedUnit32()
        {
            List<uint> values = new List<uint>(32);
            var sizeInByte = Varint();
            var end = _pos + sizeInByte;
            while (_pos < end)
            {
                values.Add((uint)Varint());
            }
            return values.ToArray();
        }
		
        public int[] GetPackedInt()
        {
            List<int> values = new List<int>(32);
            var sizeInByte = Varint();
            var end = _pos + sizeInByte;
            while (_pos < end)
            {
                values.Add((int)Varint());
            }
            return values.ToArray();
        }


        public List<int> GetPackedSInt32()
        {
            List<int> values = new List<int>(200);
            var sizeInByte = Varint();
            var end = _pos + sizeInByte;
            while (_pos < end)
            {
                values.Add(decodeZigZag32((int)Varint()));
            }
            return values;
        }


        public List<long> GetPackedSInt64()
        {
            List<long> values = new List<long>(200);
            var sizeInByte = Varint();
            var end = _pos + sizeInByte;
            while (_pos < end)
            {
                values.Add(decodeZigZag64((long)Varint()));
            }
            return values;
        }


        private int decodeZigZag32(int value)
        {
            return (value >> 1) ^ -(value & 1);
        }


        private long decodeZigZag64(long value)
        {
            return (value >> 1) ^ -(value & 1);
        }


        /// <summary>
        /// Get double at current position, move to next position
        /// </summary>
        /// <returns>Decoded double</returns>
        public double GetDouble()
        {
            double dblVal = System.BitConverter.ToDouble(_buffer.Slice(_pos, 8));
            _pos += 8;
            return dblVal;
        }


        /// <summary>
        /// Get float a current position, move to next position
        /// </summary>
        /// <returns>Decoded float</returns>
        public float GetFloat()
        {
            float snglVal = System.BitConverter.ToSingle(_buffer.Slice(_pos, 4));
            _pos += 4;
            return snglVal;
        }


        /// <summary>
        /// Get bytes as string
        /// </summary>
        /// <param name="length">Number of bytes to read</param>
        /// <returns>Decoded string</returns>
        public string GetString(int length)
        {
            var str =  Encoding.UTF8.GetString(_buffer.Slice(_pos, length));
            _pos += length;
            return str;
        }


        /// <summary>
        /// Move to next byte and set wire type. Throws exeception if tag is out of range
        /// </summary>
        /// <returns>Returns false if at end of buffer</returns>
        public bool NextByte()
        {
            if (_pos >= _length)
            {
                return false;
            }
            // get and process the next byte in the buffer
            // return true until end of stream
            Value = (ulong)Varint();
            Tag = (int)Value >> 3;
            if (
                (Tag == 0 || Tag >= 19000)
                && (Tag > 19999 || Tag <= ((1 << 29) - 1))
            )
            {
                throw new System.Exception("tag out of range");
            }
            WireType = (WireTypes)(Value & 0x07);
            return true;
        }


        /// <summary>
        /// Skip over a Varint
        /// </summary>
        public void SkipVarint()
        {
            Varint();
            //while (0 == (_buffer[Pos] & 0x80))
            //{
            //    Pos++;
            //    if (Pos >= _length)
            //    {
            //        throw new Exception("Truncated message.");
            //    }
            //}

            //if (Pos > _length)
            //{
            //    throw new Exception("Truncated message.");
            //}
        }


        /// <summary>
        /// Skip bytes
        /// </summary>
        /// <param name="skip">Number of bytes to skip</param>
        public void SkipBytes(int skip)
        {
            if (_pos + skip > _length)
            {
                string msg = string.Format(NumberFormatInfo.InvariantInfo, "[SkipBytes()] skip:{0} pos:{1} len:{2}", skip, _pos, _length);
                throw new System.Exception(msg);
            }
            _pos += skip;
        }


        /// <summary>
        /// Automatically skip bytes based on wire type
        /// </summary>
        /// <returns>New position within the byte array</returns>
        public int Skip()
        {
            if (Tag == 0)
            {
                throw new System.Exception("call next() before calling skip()");
            }

            switch (WireType)
            {
                case WireTypes.VARINT:
                    SkipVarint();
                    break;
                case WireTypes.BYTES:
                    SkipBytes((int)Varint());
                    break;
                case WireTypes.FIXED32:
                    SkipBytes(4);
                    break;
                case WireTypes.FIXED64:
                    SkipBytes(8);
                    break;
                case WireTypes.UNDEFINED:
                    throw new System.Exception("undefined wire type");
                default:
                    throw new System.Exception("unknown wire type");
            }

            return _pos;
        }
		
    }
}