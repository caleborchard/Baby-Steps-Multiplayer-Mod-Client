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
        public int heirarchyIndex;
        public System.Numerics.Vector3 position;
        public System.Numerics.Quaternion rotation;

        public TransformNet(int HeirarchyIndex, System.Numerics.Vector3 Position, System.Numerics.Quaternion Rotation)
        {
            heirarchyIndex = HeirarchyIndex;
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

        public static byte[] Serialize(TransformNet[] bones) //1index,4posx,4posy,4posz,4rotx,4roty,4rotz,4rotw
        {
            List<byte> data = new();

            foreach (TransformNet bone in bones)
            {
                data.Add((byte)bone.heirarchyIndex);
                data.AddRange(BitConverter.GetBytes(bone.position.X));
                data.AddRange(BitConverter.GetBytes(bone.position.Y));
                data.AddRange(BitConverter.GetBytes(bone.position.Z));
                data.AddRange(BitConverter.GetBytes(bone.rotation.X));
                data.AddRange(BitConverter.GetBytes(bone.rotation.Y));
                data.AddRange(BitConverter.GetBytes(bone.rotation.Z));
                data.AddRange(BitConverter.GetBytes(bone.rotation.W));
            }

            return data.ToArray();
        }

        public static TransformNet[] Deserialize(byte[] data)
        {
            int boneSize = 1 + 4 * 7; // byte index + 7 floats
            int count = data.Length / boneSize;
            TransformNet[] bones = new TransformNet[count];

            for (int i = 0; i < count; i++)
            {
                int offset = i * boneSize;

                byte index = data[offset];
                float posX = BitConverter.ToSingle(data, offset + 1);
                float posY = BitConverter.ToSingle(data, offset + 5);
                float posZ = BitConverter.ToSingle(data, offset + 9);
                float rotX = BitConverter.ToSingle(data, offset + 13);
                float rotY = BitConverter.ToSingle(data, offset + 17);
                float rotZ = BitConverter.ToSingle(data, offset + 21);
                float rotW = BitConverter.ToSingle(data, offset + 25);

                bones[i] = new TransformNet(
                    index,
                    new System.Numerics.Vector3(posX, posY, posZ),
                    new System.Numerics.Quaternion(rotX, rotY, rotZ, rotW)
                );
            }

            return bones;
        }
    }
}