using UnityEngine;

namespace BabyStepsMultiplayerClient.Extensions
{
    public static class TransformExtensions
    {
        public static Transform FindChildByKeyword(this Transform root, string keyword)
        {
            var queue = new Queue<Transform>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current.name.Contains(keyword)) 
                    return current;

                for (int i = 0; i < current.childCount; i++) 
                    queue.Enqueue(current.GetChild(i));
            }

            return null;
        }

        public static List<Transform> FindMatchingChildren(this Transform puppetMaster, List<Transform> bones)
        {
            var matches = new List<Transform>();
            var queue = new Queue<Transform>();
            var boneNames = new HashSet<string>();

            foreach (var bone in bones)
            {
                if (bone != null) boneNames.Add(bone.name);
            }

            queue.Enqueue(puppetMaster);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (boneNames.Contains(current.name)) matches.Add(current);

                for (int i = 0; i < current.childCount; i++)
                {
                    queue.Enqueue(current.GetChild(i));
                }
            }

            return matches;
        }
    }
}
