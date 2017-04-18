using System;
using System.Threading.Tasks;
using System.Linq;

namespace WebSocketServer.Core
{
    public class WebSocketMessage : IDisposable
    {
        private const int FrameSize = 4050;

        private WebSocketFrame[] _frames;

        public WebSocketMessage(WebSocketFrame[] frames)
        {
            _frames = frames;
        }

        public WebSocketMessage(byte[] payload)
        {
            Payload = payload;
        }

        public byte[] Payload { get; private set; }

        public void Dispose()
        {
            Payload = null;
            _frames = null;
        }

        public async Task<WebSocketFrame[]> PacketizeAsync()
        {
            return await Task.Run(() =>
            {
                if (Payload == null || Payload.Length == 0) return null;

                if (Payload.Length <= FrameSize)
                    return new[] { new WebSocketFrame(true, false, OpCodeEnum.BinaryFrame, new ArraySegment<byte>(Payload)) };

                var packagesLength = Payload.Length / FrameSize + (Payload.Length % FrameSize);

                var frames = new WebSocketFrame[packagesLength];

                var offset = 0;

                for (int index = 0; index < packagesLength; index++)
                {
                    var isFinal = (index == packagesLength - 1);
                    var bufferLength = isFinal ? Payload.Length - (offset * FrameSize) : FrameSize;
                    var buffer = new byte[bufferLength];
                    Buffer.BlockCopy(Payload, offset * FrameSize, buffer, 0, bufferLength);
                    frames[index] = new WebSocketFrame(isFinal, false, index == 0 ? OpCodeEnum.BinaryFrame : OpCodeEnum.ContinuationFrame, new ArraySegment<byte>(buffer));
                }
                _frames = frames;
                return frames;
            });
        }

        public async void UnpacketizeAsync()
        {
            if (_frames == null || _frames.Length == 0) return;
            await Task.Run(() =>
            {
                var messageSize = _frames.Sum(frame => frame.ExtendedPayloadLength);
                Payload = new byte[messageSize];

                for (int index = 0; index < _frames.Length; index++)
                {
                    Buffer.BlockCopy(_frames[index].Payload.Array, 0, Payload, (int)_frames[index].ExtendedPayloadLength * index, (int)_frames[index].ExtendedPayloadLength);
                }
            });
        }
    }
}
