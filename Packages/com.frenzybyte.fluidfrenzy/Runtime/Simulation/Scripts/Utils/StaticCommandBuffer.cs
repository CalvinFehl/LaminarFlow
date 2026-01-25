using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	public class StaticCommandBuffer
	{
		public StaticCommandBuffer()
		{
			commandBuffers = new Dictionary<CommandBufferKey, CommandBuffer>(new CommandBufferKey.Comparer());
		}

		public void Clear() { commandBuffers?.Clear(); }

		public bool GetCommandBuffer(CommandBufferKey key, out CommandBuffer cmd)
		{
			if (commandBuffers.TryGetValue(key, out cmd))
				return true;

			cmd = new CommandBuffer();
			commandBuffers.Add(key, cmd);
			return false;
		}

		public bool TryGetCommandBuffer(CommandBufferKey key, out CommandBuffer cmd)
		{
			if (commandBuffers.TryGetValue(key, out cmd))
				return true;

			cmd = new CommandBuffer();
			return false;
		}

		public void AddCommandBuffer(CommandBufferKey key, CommandBuffer cmd)
		{
			commandBuffers.Add(key, cmd);
		}

		public Dictionary<CommandBufferKey, CommandBuffer> commandBuffers;
	}

	public struct CommandBufferKey
	{
		public class Comparer : IEqualityComparer<CommandBufferKey>
		{
			public int GetHashCode(CommandBufferKey key)
			{
				return key.GetHashCode();
			}

			public bool Equals(CommandBufferKey x, CommandBufferKey y)
			{
				return x.EqualsKeys(y);
			}
		}

		public CommandBufferKey(RenderTexture _k1, RenderTexture _k2, RenderTexture _k3)
		{
			key1 = _k1;
			key2 = _k2;
			key3 = _k3;
		}


		public bool EqualsKeys(CommandBufferKey y)
		{
			return key1 == y.key1 && key2 == y.key2 && key3 == y.key3;
		}

		private int CombineHashCode(int a, int b)
		{
			int hash = 17;
			hash = hash * 31 + a.GetHashCode();
			hash = hash * 31 + b.GetHashCode();
			return hash;
		}

		public override int GetHashCode()
		{
#if UNITY_2021_1_OR_NEWER
			int hash = HashCode.Combine(key1, key2, key3);
#else
			int hash = key1.GetHashCode();
			if(key2)
				hash = CombineHashCode(hash, key2.GetHashCode());

			if (key3)
				hash = CombineHashCode(hash, key3.GetHashCode());
#endif
			return hash;
		}

		public RenderTexture key1;
		public RenderTexture key2;
		public RenderTexture key3;
	}
}