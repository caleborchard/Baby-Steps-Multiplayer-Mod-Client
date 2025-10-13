using BabyStepsMultiplayerClient.Player;
using MelonLoader;
using UnityEngine;

namespace BabyStepsMultiplayerClient.Networking
{
    public class BoneSnapshot
    {
        public double time;
        public TransformNet[] transformNets;

        public BoneSnapshot(int boneCount) { transformNets = new TransformNet[boneCount]; }
    }

    public class TransformNet
    {
        private const float loopWidth = 512f;

        public System.Numerics.Vector3 position;
        public System.Numerics.Quaternion rotation;
        public int heirarchyIndex;

        public TransformNet(int index, System.Numerics.Vector3 Position, System.Numerics.Quaternion Rotation)
        {
            heirarchyIndex = index;
            position = Position;
            rotation = Rotation;
        }

        public static TransformNet[] ToNet(List<Transform> bones)
        {
            int boneCount = bones.Count;
            TransformNet[] final = new TransformNet[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                Transform b = bones[i];
                if (b == null) continue;

                final[i] = new TransformNet(
                    i,
                    new System.Numerics.Vector3(b.position.x, b.position.y, b.position.z),
                    new System.Numerics.Quaternion(b.rotation.x, b.rotation.y, b.rotation.z, b.rotation.w)
                );
            }
            return final;
        }

        public static byte[] Serialize(TransformNet[] bones)
        {
            List<byte> data = new();

            sbyte loop = (sbyte)Mathf.FloorToInt((bones[0].position.X + loopWidth / 2f) / loopWidth);
            data.Add((byte)loop);

            for (int i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];

                // Wrap position inside a single loop [0, loopWidth)
                float wrapped = (bone.position.X + loopWidth / 2f) % loopWidth;

                // Normalize to [0, 1] for 24-bit packing
                float normalized = wrapped / loopWidth;

                // Pack into 3 bytes
                int packed = (int)(normalized * 16777215f); // 2^24 - 1
                byte b1 = (byte)(packed & 0xFF);
                byte b2 = (byte)((packed >> 8) & 0xFF);
                byte b3 = (byte)((packed >> 16) & 0xFF);

                data.Add(b1);
                data.Add(b2);
                data.Add(b3);

                data.AddRange(BitConverter.GetBytes((short)(bone.position.Y * 1000f)));
                data.AddRange(BitConverter.GetBytes(bone.position.Z));

                System.Numerics.Quaternion q = bone.rotation;
                if (q.W < 0f) q = new System.Numerics.Quaternion(-q.X, -q.Y, -q.Z, -q.W);

                short x = (short)Mathf.RoundToInt(q.X * 32767f);
                short y = (short)Mathf.RoundToInt(q.Y * 32767f);
                short z = (short)Mathf.RoundToInt(q.Z * 32767f);

                data.AddRange(BitConverter.GetBytes(x));
                data.AddRange(BitConverter.GetBytes(y));
                data.AddRange(BitConverter.GetBytes(z));
            }

            return data.ToArray();
        }

        public static TransformNet[] Deserialize(byte[] data, float loopWidth = 512f)
        {
            int boneSize = 15;
            int count = data.Length / boneSize;
            TransformNet[] bones = new TransformNet[count];

            sbyte loop = (sbyte)data[0];

            for (int i = 0; i < count; i++)
            {
                int offset = i * boneSize + 1;

                // --- Position X ---
                byte b1 = data[offset];
                byte b2 = data[offset + 1];
                byte b3 = data[offset + 2];

                int unpacked = b1 | (b2 << 8) | (b3 << 16);

                // Back to normalized [0,1]
                float normalizedBack = unpacked / 16777215f;

                // Scale back to loop units and shift to original position
                float xDecoded = normalizedBack * loopWidth - loopWidth / 2f + loop * loopWidth;

                float posX = xDecoded;

                // Fix posX around LocalPlayer for looping solution
                float localX = LocalPlayer.Instance.rootBone.position.x;
                float centeredOffset = localX - (loopWidth * 0.5f);
                float relativeX = posX - centeredOffset;
                relativeX = ((relativeX % loopWidth) + loopWidth) % loopWidth;
                posX = relativeX + centeredOffset;

                // --- Position Y ---
                short posYshort = BitConverter.ToInt16(data, offset + 3);
                float posY = posYshort / 1000f;

                // --- Position Z ---
                float posZ = BitConverter.ToSingle(data, offset + 5);

                // --- Rotation ---
                short rotXshort = BitConverter.ToInt16(data, offset + 9);
                short rotYshort = BitConverter.ToInt16(data, offset + 11);
                short rotZshort = BitConverter.ToInt16(data, offset + 13);

                float rotX = rotXshort / 32767f;
                float rotY = rotYshort / 32767f;
                float rotZ = rotZshort / 32767f;

                float wSquared = 1f - (rotX * rotX + rotY * rotY + rotZ * rotZ);
                float rotW = wSquared > 0f ? MathF.Sqrt(wSquared) : 0f;

                bones[i] = new TransformNet(
                    i,
                    new System.Numerics.Vector3(posX, posY, posZ),
                    new System.Numerics.Quaternion(rotX, rotY, rotZ, rotW)
                );
            }

            return bones;
        }
    }
}