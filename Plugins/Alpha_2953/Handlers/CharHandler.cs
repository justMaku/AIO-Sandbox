﻿using Common.Interfaces.Handlers;
using System;
using Common.Interfaces;
using Common.Commands;
using Common.Structs;
using Common.Constants;
using Common.Extensions;
using System.Linq;

namespace Alpha_2953.Handlers
{
	public class CharHandler : ICharHandler
	{
		public void HandleCharCreate(ref IPacketReader packet, ref IWorldManager manager)
		{
			string name = packet.ReadString();

			Character cha = new Character()
			{
				Name = name.ToUpperFirst(),
				Race = packet.ReadByte(),
				Class = packet.ReadByte(),
				Gender = packet.ReadByte(),
				Skin = packet.ReadByte(),
				Face = packet.ReadByte(),
				HairStyle = packet.ReadByte(),
				HairColor = packet.ReadByte(),
				FacialHair = packet.ReadByte()
			};

			var result = manager.Account.Characters.Where(x => x.Build == Sandbox.Instance.Build);
			PacketWriter writer = new PacketWriter(Sandbox.Instance.Opcodes[global::Opcodes.SMSG_CHAR_CREATE], "SMSG_CHAR_CREATE");

			if (result.Any(x => x.Name.Equals(cha.Name, StringComparison.CurrentCultureIgnoreCase)))
			{
				writer.WriteUInt8(0x2B); //Duplicate name
				manager.Send(writer);
				return;
			}

			cha.Guid = (ulong)(manager.Account.Characters.Count + 1);
			cha.Location = new Location(-8949.95f, -132.493f, 83.5312f, 0, 0);
			cha.SetDefaultValues(true);
			cha.Level = 110;

			manager.Account.Characters.Add(cha);
			manager.Account.Save();

			//Success
			writer.WriteUInt8(0x28);
			manager.Send(writer);
		}

		public void HandleCharDelete(ref IPacketReader packet, ref IWorldManager manager)
		{
			ulong guid = packet.ReadUInt64();
			var character = manager.Account.GetCharacter(guid,Sandbox.Instance.Build);

			PacketWriter writer = new PacketWriter(Sandbox.Instance.Opcodes[global::Opcodes.SMSG_CHAR_DELETE], "SMSG_CHAR_DELETE");
			writer.WriteUInt8(0x2C);
			manager.Send(writer);

			if (character != null)
			{
				manager.Account.Characters.Remove(character);
				manager.Account.Save();
			}
		}

		public void HandleCharEnum(ref IPacketReader packet, ref IWorldManager manager)
		{
			var account = manager.Account;
			var result = account.Characters.Where(x => x.Build == Sandbox.Instance.Build);

			PacketWriter writer = new PacketWriter(Sandbox.Instance.Opcodes[global::Opcodes.SMSG_CHAR_ENUM], "SMSG_CHAR_ENUM");
			writer.WriteUInt8((byte)result.Count());

			foreach (Character c in result)
			{
				writer.WriteUInt64(c.Guid);
				writer.WriteString(c.Name);

				writer.WriteUInt8(c.Race);
				writer.WriteUInt8(c.Class);
				writer.WriteUInt8(c.Gender);
				writer.WriteUInt8(c.Skin);
				writer.WriteUInt8(c.Face);
				writer.WriteUInt8(c.HairStyle);
				writer.WriteUInt8(c.HairColor);
				writer.WriteUInt8(c.FacialHair);
				writer.WriteUInt8((byte)c.Level);

				writer.WriteUInt32(c.Zone);
				writer.WriteUInt32(c.Location.Map);

				writer.WriteFloat(c.Location.X);
				writer.WriteFloat(c.Location.Y);
				writer.WriteFloat(c.Location.Z);

				writer.WriteUInt32(0);
				writer.WriteUInt32(0);
				writer.WriteUInt32(0);
				writer.WriteUInt32(0);

				//Items
				for (int j = 0; j < 0x14; j++)
				{
					writer.WriteUInt32(0);    //DisplayId
					writer.WriteUInt8(0);     //InventoryType
				}
			}

			manager.Send(writer);
		}

		public void HandleMessageChat(ref IPacketReader packet, ref IWorldManager manager)
		{
			PacketWriter writer = new PacketWriter(Sandbox.Instance.Opcodes[global::Opcodes.SMSG_MESSAGECHAT], "SMSG_MESSAGECHAT");
			writer.WriteUInt8((byte)packet.ReadInt32()); //System Message
			packet.ReadUInt32();
			writer.WriteUInt32(0); //Language: General
			writer.WriteUInt64(manager.Account.ActiveCharacter.Guid);

			string message = packet.ReadString();
			writer.WriteString(message);
			writer.WriteUInt8(0);

			if (!CommandManager.InvokeHandler(message, manager))
				manager.Send(writer);
		}

		public void HandleMovementStatus(ref IPacketReader packet, ref IWorldManager manager)
		{
			if (manager.Account.ActiveCharacter.IsTeleporting)
				return;

			ulong TransportGuid = packet.ReadUInt64();
			float TransportX = packet.ReadFloat();
			float TransportY = packet.ReadFloat();
			float TransportZ = packet.ReadFloat();
			float TransportO = packet.ReadFloat();

			manager.Account.ActiveCharacter.Location.Update(packet, true);
		}

		public void HandleNameCache(ref IPacketReader packet, ref IWorldManager manager)
		{
			ulong guid = packet.ReadUInt64();
			Character character = (Character)manager.Account.ActiveCharacter;

			PacketWriter nameCache = new PacketWriter(Sandbox.Instance.Opcodes[global::Opcodes.SMSG_NAME_QUERY_RESPONSE], "SMSG_NAME_QUERY_RESPONSE");
			nameCache.WriteUInt64(guid);
			nameCache.WriteString(character.Name);
			nameCache.WriteUInt32(character.Race);
			nameCache.WriteUInt32(character.Gender);
			nameCache.WriteUInt32(character.Class);
			nameCache.WriteUInt8(0);
			manager.Send(nameCache);
		}

		public void HandleStandState(ref IPacketReader packet, ref IWorldManager manager)
		{
			manager.Account.ActiveCharacter.StandState = (StandState)packet.ReadUInt32();
			manager.Send(manager.Account.ActiveCharacter.BuildUpdate());
		}

		public void HandleTextEmote(ref IPacketReader packet, ref IWorldManager manager)
		{
			uint emote = packet.ReadUInt32();
			ulong guid = packet.ReadUInt64();
			uint emoteId = Emotes.Get((TextEmotes)emote);
			Character character = (Character)manager.Account.ActiveCharacter;

			PacketWriter pw = new PacketWriter(Sandbox.Instance.Opcodes[global::Opcodes.SMSG_TEXT_EMOTE], "SMSG_TEXT_EMOTE");
			pw.Write(character.Guid);
			pw.Write(emote);

			if (guid == character.Guid)
				pw.WriteString(character.Name);
			else
				pw.WriteUInt8(0);

			manager.Send(pw);

			switch ((TextEmotes)emote)
			{
				case TextEmotes.EMOTE_SIT:
					character.StandState = StandState.SITTING;
					manager.Send(character.BuildUpdate());
					return;
				case TextEmotes.EMOTE_STAND:
					character.StandState = StandState.STANDING;
					manager.Send(character.BuildUpdate());
					return;
				case TextEmotes.EMOTE_SLEEP:
					character.StandState = StandState.SLEEPING;
					manager.Send(character.BuildUpdate());
					return;
				case TextEmotes.EMOTE_KNEEL:
					character.StandState = StandState.KNEEL;
					manager.Send(character.BuildUpdate());
					return;
			}

			if (emoteId > 0)
			{
				pw = new PacketWriter(Sandbox.Instance.Opcodes[global::Opcodes.SMSG_EMOTE], "SMSG_EMOTE");
				pw.WriteUInt32(emoteId);
				pw.WriteUInt64(character.Guid);
				manager.Send(pw);
			}
		}
	}
}
