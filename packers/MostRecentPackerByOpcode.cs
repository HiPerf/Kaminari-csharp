﻿using System;
using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
	public class MostRecentPackerByOpcode : Packer<PacketWithOpcode, IData>
	{

		private uint opcode;
		private Dictionary<ushort, PendingData<PacketWithOpcode>> opcodeMap = new Dictionary<ushort, PendingData<PacketWithOpcode>>();

		public override void onAck(List<PendingData<PacketWithOpcode>> toBeRemoved)
		{
			foreach (PendingData<PacketWithOpcode> pending in toBeRemoved)
			{
				opcodeMap.Remove(pending.data.opcode);
			}
		}

		public override void onClear()
		{
			opcodeMap.Clear();
		}

		public override void add(Packet packet)
		{
			add(packet, packet.getOpcode());
		}

		public override void add(IMarshal marshal, ushort opcode, IData data, Action callback)
		{
			Packet packet = Packet.make(opcode, callback);
			data.pack(marshal, packet);
			add(packet, opcode);
		}

		private void add(Packet packet, ushort opcode)
		{
			if (opcodeMap.ContainsKey(opcode))
			{
				opcodeMap[opcode].data.packet = packet;
				opcodeMap[opcode].blocks.Clear();
			}
			else
			{
				PendingData<PacketWithOpcode> pendingData = new PendingData<PacketWithOpcode>(new PacketWithOpcode(packet, opcode));
				pending.Add(pendingData);
			}
		}

		public override void process(IMarshal marshal, ushort blockId, ref ushort remaining, SortedDictionary<uint, List<Packet>> byBlock)
		{
			foreach (PendingData<PacketWithOpcode> pnd in pending)
			{
				if (!isPending(pnd.blocks, blockId, false))
				{
					continue;
				}
				
				uint actualBlock = getActualBlock(pnd.blocks, blockId);
				ushort size = (ushort)pnd.data.packet.getSize();

				if (byBlock.ContainsKey(actualBlock))
				{
					if (size > remaining)
					{
						break;
					}

					byBlock[actualBlock].Add(pnd.data.packet);
				}
				else
				{
					size = (ushort)(size + 4);
					if (size > remaining)
					{
						break;
					}

					byBlock.Add(actualBlock, new List<Packet>());
					byBlock[actualBlock].Add(pnd.data.packet);
				}

				pnd.blocks.Add(blockId);
				remaining = (ushort)(remaining - size);
			}
		}
	}
}
