using System;
using System.Threading.Tasks;
using System.Linq;

namespace WebSocket.Protocol
{
    public static class WebSocketMessage
    {
        private const int FrameSize = 4071;

        public static WebSocketFrame[] Packetize(byte[] payload)
        {
            if (payload == null || payload.Length == 0) return new WebSocketFrame[0];

            if (payload.Length <= FrameSize)
            {
                return new[] { new WebSocketFrame(true, false, OpCodeEnum.BinaryFrame, payload) };
            }

            var packagesLength = payload.Length / FrameSize;
            var lastFrameSize = (payload.Length % FrameSize);
            if (lastFrameSize > 0)
                packagesLength++;

            var frames = new WebSocketFrame[packagesLength];

            for (int index = 0, frameId = 0; index <= payload.Length; index += FrameSize, frameId++)
            {
                var remainingBits = payload.Length - index;
                var isFinal = remainingBits < FrameSize || remainingBits == 0;
                var bufferLength = isFinal ? remainingBits : FrameSize;
                var buffer = new byte[bufferLength];
                Buffer.BlockCopy(payload, index, buffer, 0, bufferLength);
                frames[frameId] = new WebSocketFrame(isFinal, false, frameId == 0 ? OpCodeEnum.BinaryFrame : OpCodeEnum.ContinuationFrame, buffer);
            } 
            return frames;
        }

        public static byte[] Unpacketize(WebSocketFrame[] frames)
        {
            if (frames == null || frames.Length == 0) return new byte[0];

            var messageSize = frames.Sum(frame => frame.ExtendedPayloadLength);
            var payload = new byte[messageSize];

            for (int index = 0, offset = 0; index < frames.Length; index++)
            {
                var frame = frames[index];
                Buffer.BlockCopy(frame.Payload, 0, payload, offset, (int)frame.ExtendedPayloadLength);
                offset += (int)frame.ExtendedPayloadLength;
            }

            return payload;
        }
        
    }
}
