using System;

namespace WebSocket.Protocol
{
    /// <summary>
    /// Represents a socket frame message used to transmit data over WebSockets
    /// </summary>
    /// <see cref="https://tools.ietf.org/html/rfc6455#section-5.2"/>
    public class WebSocketFrame
    {
        /// <summary>
        /// Represents header of the websocket frame
        /// </summary>
        private byte[] _header;

        private WebSocketFrame(bool masked)
        {
            _header = new byte[masked ? 21 : 17];
            Mask = masked;
        }

        public WebSocketFrame(bool eom, bool masked, OpCodeEnum opCode, byte[] payload) : this(masked)
        {
            OpCode = opCode;
            Fin = eom;
            ExtendedPayloadLength = payload.Length;
            Frame = new byte[_header.Length + payload.Length];
            Payload = payload;
            Buffer.BlockCopy(_header, 0, Frame, 0, _header.Length);
            Buffer.BlockCopy(payload, 0, Frame, _header.Length, payload.Length);
        }

        public WebSocketFrame(byte[] frame)
        {
            if (frame == null || frame.Length < 17) throw new WebSocketFrameException();

            var masked = BitConverter.ToBoolean(frame, 8);
            _header = new byte[masked ? 21 : 17];
            Buffer.BlockCopy(frame, 0, _header, 0, _header.Length);
            Payload = new byte[frame.Length - _header.Length];
            Buffer.BlockCopy(frame, _header.Length, Payload, 0, Payload.Length);
        }

        /// <summary>
        /// Indicates that this is the final fragment in a message. The first fragment MAY also be the final fragment
        /// </summary>
        public bool Fin { get { return BitConverter.ToBoolean(_header, 0); } set { _header[0] = (byte)(value ? 1 : 0); } }
        /// <summary>
        /// MUST be 0 unless an extension is negotiated that defines meanings for non-zero values.
        /// </summary>
        /// <remarks>
        /// If a nonzero value is received and none of the negotiated extensions defines the meaning of such a nonzero value, 
        /// the receiving endpoint MUST _Fail the WebSocket Connection_.
        /// </remarks>
        public bool Rsv1 { get { return BitConverter.ToBoolean(_header, 1); } set { _header[1] = (byte)(value ? 1 : 0); } }
        /// <summary>
        /// MUST be 0 unless an extension is negotiated that defines meanings for non-zero values.
        /// </summary>
        /// <remarks>
        /// If a nonzero value is received and none of the negotiated extensions defines the meaning of such a nonzero value, 
        /// the receiving endpoint MUST _Fail the WebSocket Connection_.
        /// </remarks> 
        public bool Rsv2 { get { return BitConverter.ToBoolean(_header, 2); } set { _header[2] = (byte)(value ? 1 : 0); } }
        /// <summary>
        /// MUST be 0 unless an extension is negotiated that defines meanings for non-zero values.
        /// </summary>
        /// <remarks>
        /// If a nonzero value is received and none of the negotiated extensions defines the meaning of such a nonzero value, 
        /// the receiving endpoint MUST _Fail the WebSocket Connection_.
        /// </remarks>
        public bool Rsv3 { get { return BitConverter.ToBoolean(_header, 3); } set { _header[3] = (byte)(value ? 1 : 0); } }

        /// <summary>
        /// Defines the interpretation of the "Payload data".
        /// </summary>
        /// <remarks>
        /// If an unknown opcode is received, the receiving endpoint MUST _Fail the WebSocket Connection_.
        /// </remarks>
        public OpCodeEnum OpCode
        {
            get
            {
                var opCode = new byte[4];
                Buffer.BlockCopy(_header, 4, opCode, 0, 4);
                return (OpCodeEnum)BitConverter.ToInt32(opCode, 0);
            }
            set
            {
                var byteValue = BitConverter.GetBytes((int)value);
                Buffer.BlockCopy(byteValue, 0, _header, 4, 4);
            }
        }

        /// <summary>
        /// Defines whether the "Payload data" is masked.
        /// </summary>
        /// <remarks>
        /// If set to 1, a masking key is present in masking-key, and this is used to unmask client to server have this bit set to 1.
        /// </remarks>
        public bool Mask { get { return BitConverter.ToBoolean(_header, 8); } set { _header[8] = (byte)(value ? 1 : 0); } }

        /// <summary>
        /// The length of the "Payload data", in bytes
        /// </summary>
        /// <remarks>
        /// If 0-125, that is the payload length. 
        /// If 126, the following 2 bytes interpreted as a 16-bit unsigned integer are the payload length.
        /// If 127, the following 8 bytes interpreted as 64-bit unsigned integer (the most significant bit MUST be 0) are the payload length.
        /// </remarks>
        public int PayloadLength
        {
            get
            {
                return _header[9] & 0x7F;
            }
            private set
            {
                _header[9] |= (byte)(value & 0x7F);
            }
        }

        /// <summary>
        /// Extended payload length (if <see cref="PayloadLength"/> == 126/127)
        /// </summary>
        public long ExtendedPayloadLength
        {
            get
            {
                if (PayloadLength <= 125) return PayloadLength;
                else if (PayloadLength == 126) return (_header[11] << 8) + _header[10];
                else return (_header[17] << 56) + (_header[16] << 48) + 
                        (_header[15] << 40) + (_header[14] << 32) + 
                        (_header[13] << 24) + (_header[12] << 16) + 
                        (_header[11] << 8) + _header[10];
            }
            set
            {
                if (value <= 125)
                    PayloadLength = (int)value;
                else if (125 < value && value <= 0xFFFF)
                {
                    PayloadLength = 126;

                    _header[10] = (byte)value;
                    _header[11] = (byte)(value >> 8);
                }
                else
                {
                    PayloadLength = 127;

                    _header[10] = (byte)value;
                    _header[11] = (byte)(value >> 8);
                    _header[12] = (byte)(value >> 16);
                    _header[13] = (byte)(value >> 24);
                    _header[14] = (byte)(value >> 32);
                    _header[15] = (byte)(value >> 40);
                    _header[16] = (byte)(value >> 48);
                    _header[17] = (byte)(value >> 56);
                }
            }
        }
        /// <summary>
        /// All frames sent from the client to the server are masked by a 32-bit value that is contained within the frame
        /// </summary>
        /// <remarks>
        /// This field is present if the mask bit is set to 1 and is absent if the mask bit is set to 0
        /// </remarks>
        public int MaskKey
        {
            get
            {
                if (!Mask) return 0;
                var offset = MaskOffset;
                return (_header[offset + 3] << 24) + (_header[offset + 2] << 16) + 
                    (_header[offset + 1] << 8) + _header[offset];
            }
            set
            {
                var offset = MaskOffset;

                _header[offset] = (byte)value;
                _header[offset + 1] = (byte)(value >> 8);
                _header[offset + 2] = (byte)(value >> 16);
                _header[offset + 3] = (byte)(value >> 24);
            }
        }

        /// <summary>
        /// Payload data
        /// </summary>
        public byte[] Payload { get; private set; }

        /// <summary>
        /// Represents entire websocket frame serialized into byte array (header + payload data)
        /// </summary>
        public byte[] Frame { get; private set; }

        private int MaskOffset { get { return PayloadLength <= 125 ? 10 : PayloadLength == 126 ? 12 : 18; } }
    }
}
