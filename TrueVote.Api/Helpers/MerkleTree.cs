using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Utf8Json;

namespace TrueVote.Api.Helpers
{
    public static class MerkleTree
    {
        private static readonly SHA256 s_sha256 = SHA256.Create();
        private static readonly ArrayPool<byte> s_bytePool = ArrayPool<byte>.Shared;

        public static byte[] CalculateMerkleRoot<T>(List<T> data)
        {
            if (data == null || data.Count == 0)
            {
                return null;
            }

            if (data.Count == 1)
            {
                return GetHash(data[0]);
            }

            var leafNodes = data.Select(GetHash).ToList();

            while (leafNodes.Count > 1)
            {
                var parentNodes = new List<byte[]>();
                for (var i = 0; i < leafNodes.Count; i += 2)
                {
                    var left = leafNodes[i];
                    var right = (i + 1 < leafNodes.Count) ? leafNodes[i + 1] : left;

                    var parent = GetHash(left, right);
                    parentNodes.Add(parent);
                }
                leafNodes = parentNodes;
            }

            return leafNodes[0];
        }

        public static byte[] GetHash<T>(T value)
        {
            var serializer = JsonSerializer.Serialize(value);
            return s_sha256.ComputeHash(serializer);
        }

        public static byte[] GetHash(byte[] left, byte[] right)
        {
            var buffer = s_bytePool.Rent(left.Length + right.Length);
            try
            {
                left.AsSpan().CopyTo(buffer);
                right.AsSpan().CopyTo(buffer.AsSpan(left.Length));

                return s_sha256.ComputeHash(buffer.AsSpan(0, left.Length + right.Length).ToArray());
            }
            finally
            {
                s_bytePool.Return(buffer);
            }
        }
    }
}
