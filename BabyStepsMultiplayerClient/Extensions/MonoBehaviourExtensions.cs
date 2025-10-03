using BabyStepsMultiplayerClient.Components;
using System.Collections;
using UnityEngine;

namespace BabyStepsMultiplayerClient.Extensions
{
    public static class MonoBehaviourExtensions
    {
        public static Coroutine StartCoroutine<T>(this T behaviour, IEnumerator enumerator)
            where T : MonoBehaviour
            => behaviour.StartCoroutine(
                new Il2CppSystem.Collections.IEnumerator(
                new ManagedEnumerator(enumerator).Pointer));
    }
}
