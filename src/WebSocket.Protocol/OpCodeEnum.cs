﻿namespace WebSocket.Protocol
{
    public enum OpCodeEnum
    {
        ContinuationFrame = 0x0,
        TextFrame = 0x1,
        BinaryFrame = 0x2,
        CloseFrame = 0x8,
        PingFrame = 0x9,
        PongFrame = 0xA
    }
}
