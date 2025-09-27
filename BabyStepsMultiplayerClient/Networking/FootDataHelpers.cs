using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BabyStepsMultiplayerClient.Networking
{
    public class FootDataHelpers
    {
        public static FootData DeserializeFootData(byte[] data, RemotePlayer player)
        {
            int offset = 0;

            bool isFootRight = BitConverter.ToBoolean(data, offset);
            offset += sizeof(bool);

            FootData fd = player.feet[0];
            if (fd == null) return null;
            if (isFootRight) fd = player.feet[1];

            fd.achillesIsConstraining = BitConverter.ToBoolean(data, offset);
            offset += sizeof(bool);

            fd.appliedAccel = DeserializeVector3(data, ref offset);

            fd.curDropSpd = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            fd.curPlantedRotSpd = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            fd.currentSlipVector = DeserializeVector3(data, ref offset);

            fd.curTractionCoef = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            fd.fatigue = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            fd.fatiguePokeTimer = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            fd.freshPlantInput = BitConverter.ToBoolean(data, offset);
            offset += sizeof(bool);

            fd.heightOnTrigRelease = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            fd.hitLiftedConstraint = BitConverter.ToBoolean(data, offset);
            offset += sizeof(bool);

            fd.isLifting = BitConverter.ToBoolean(data, offset);
            offset += sizeof(bool);

            fd.lastAppliedTorque = DeserializeVector3(data, ref offset);

            fd.liftedTime = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            fd.maxExert = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            fd.nextPoke = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            fd.originalPlantSpot = DeserializeVector3(data, ref offset);

            fd.pivotPt = DeserializeVector3(data, ref offset);

            fd.planted = BitConverter.ToBoolean(data, offset);
            offset += sizeof(bool);

            fd.plantedSinceStoodUp = BitConverter.ToBoolean(data, offset);
            offset += sizeof(bool);

            fd.plantedTime = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            fd.plantNormal = DeserializeVector3(data, ref offset);

            fd.prevPlanted = BitConverter.ToBoolean(data, offset);
            offset += sizeof(bool);

            fd.prevPos = DeserializeVector3(data, ref offset);

            fd.prevSteppedOnLocalPos = DeserializeVector3(data, ref offset);

            fd.prevSteppedOnWorldPos = DeserializeVector3(data, ref offset);

            fd.pushedBackByCollision = BitConverter.ToBoolean(data, offset);
            offset += sizeof(bool);

            fd.side = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            fd.slipping = BitConverter.ToBoolean(data, offset);
            offset += sizeof(bool);

            fd.slipTimer = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            fd.slipVel = DeserializeVector3(data, ref offset);

            fd.steppedOnLocalPos = DeserializeVector3(data, ref offset);

            // steppedOnRotation (Quaternion)
            float w = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            float x = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            float y = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            float z = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            fd.steppedOnRotation = new Quaternion(x, y, z, w);

            fd.steppedOnWorldPos = DeserializeVector3(data, ref offset);

            fd.stickedMove = DeserializeVector3(data, ref offset);

            //fd.targetPos = DeserializeVector3(data, ref offset);

            fd.trigPressedTimer = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            fd.wantsToLift = BitConverter.ToBoolean(data, offset);
            offset += sizeof(bool);

            return fd;
        }

        public static Vector3 DeserializeVector3(byte[] data, ref int offset)
        {
            float x = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            float y = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            float z = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);

            return new Vector3(x, y, z);
        }

        public static byte[] SerializeFootData(FootData fd)
        {
            List<byte> raw = new();

            bool isFootRight = false;
            if (fd.bone.name == "foot.r") isFootRight = true;
            raw.AddRange(BitConverter.GetBytes(isFootRight));

            raw.AddRange(BitConverter.GetBytes(fd.achillesIsConstraining));
            raw.AddRange(SerializeVector3(fd.appliedAccel));
            raw.AddRange(BitConverter.GetBytes(fd.curDropSpd));
            raw.AddRange(BitConverter.GetBytes(fd.curPlantedRotSpd));
            raw.AddRange(SerializeVector3(fd.currentSlipVector));
            raw.AddRange(BitConverter.GetBytes(fd.curTractionCoef));
            raw.AddRange(BitConverter.GetBytes(fd.fatigue));
            raw.AddRange(BitConverter.GetBytes(fd.fatiguePokeTimer));
            raw.AddRange(BitConverter.GetBytes(fd.freshPlantInput));
            raw.AddRange(BitConverter.GetBytes(fd.heightOnTrigRelease));
            raw.AddRange(BitConverter.GetBytes(fd.hitLiftedConstraint));
            raw.AddRange(BitConverter.GetBytes(fd.isLifting));
            raw.AddRange(SerializeVector3(fd.lastAppliedTorque));
            raw.AddRange(BitConverter.GetBytes(fd.liftedTime));
            raw.AddRange(BitConverter.GetBytes(fd.maxExert));
            raw.AddRange(BitConverter.GetBytes(fd.nextPoke));
            raw.AddRange(SerializeVector3(fd.originalPlantSpot));
            raw.AddRange(SerializeVector3(fd.pivotPt));
            raw.AddRange(BitConverter.GetBytes(fd.planted));
            raw.AddRange(BitConverter.GetBytes(fd.plantedSinceStoodUp));
            raw.AddRange(BitConverter.GetBytes(fd.plantedTime));
            raw.AddRange(SerializeVector3(fd.plantNormal));
            raw.AddRange(BitConverter.GetBytes(fd.prevPlanted));
            raw.AddRange(SerializeVector3(fd.prevPos));
            raw.AddRange(SerializeVector3(fd.prevSteppedOnLocalPos));
            raw.AddRange(SerializeVector3(fd.prevSteppedOnWorldPos));
            raw.AddRange(BitConverter.GetBytes(fd.pushedBackByCollision));
            raw.AddRange(BitConverter.GetBytes(fd.side));
            raw.AddRange(BitConverter.GetBytes(fd.slipping));
            raw.AddRange(BitConverter.GetBytes(fd.slipTimer));
            raw.AddRange(SerializeVector3(fd.slipVel));
            raw.AddRange(SerializeVector3(fd.steppedOnLocalPos));

            raw.AddRange(BitConverter.GetBytes(fd.steppedOnRotation.w));
            raw.AddRange(BitConverter.GetBytes(fd.steppedOnRotation.x));
            raw.AddRange(BitConverter.GetBytes(fd.steppedOnRotation.y));
            raw.AddRange(BitConverter.GetBytes(fd.steppedOnRotation.z));

            raw.AddRange(SerializeVector3(fd.steppedOnWorldPos));
            raw.AddRange(SerializeVector3(fd.stickedMove));

            //raw.AddRange(SerializeVector3(fd.targetPos));
            raw.AddRange(SerializeVector3(Vector3.zero));

            raw.AddRange(BitConverter.GetBytes(fd.trigPressedTimer));
            raw.AddRange(BitConverter.GetBytes(fd.wantsToLift));

            return raw.ToArray();
        }

        private static byte[] SerializeVector3(Vector3 v)
        {
            List<byte> raw = new();

            raw.AddRange(BitConverter.GetBytes(v.x));
            raw.AddRange(BitConverter.GetBytes(v.y));
            raw.AddRange(BitConverter.GetBytes(v.z));

            return raw.ToArray();
        }
    }
}
