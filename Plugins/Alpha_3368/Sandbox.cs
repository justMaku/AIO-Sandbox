﻿using Common.Interfaces;
using System;
using Common.Interfaces.Handlers;
using Alpha_3368.Handlers;
using Common.Structs;

namespace Alpha_3368
{
    public class Sandbox : ISandbox
    {
        public static Sandbox Instance => _instance;
        static readonly Sandbox _instance = new Sandbox();

        public string RealmName { get; set; } = "|cFF00FFFFAlpha (0.5.3) Sandbox";
        public int Build { get; set; } = 3368;
        public int RealmPort { get; set; } = 9100;
        public int RedirectPort { get; set; } = 9090;
        public int WorldPort { get; set; } = 8100;

        public IOpcodes Opcodes { get; set; } = new Opcodes();

        public IAuthHandler AuthHandler { get; set; } = new AuthHandler();
        public ICharHandler CharHandler { get; set; } = new CharHandler();
        public IWorldHandler WorldHandler { get; set; } = new WorldHandler();

        public IPacketReader ReadPacket(byte[] data, bool parse = true) => new PacketReader(data, parse);
        public IPacketWriter WritePacket() => new PacketWriter();
    }
}
