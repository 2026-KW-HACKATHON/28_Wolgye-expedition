using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mapbox.BaseModule.Utilities
{
    public static class IEnumerableExtensions
    {
        public static IEnumerator WaitForAll(this IEnumerable<IEnumerator> coroutines)
        {
            int tally = 0;

            foreach(IEnumerator c in coroutines)
            {
                Runnable.Instance.StartCoroutine(RunCoroutine(c));
            }

            while (tally > 0)
            {
                yield return null;
            }

            IEnumerator RunCoroutine(IEnumerator c)
            {
                tally++;
                yield return Runnable.Instance.StartCoroutine(c);
                tally--;
            }
        }
        public static IEnumerator RunSequential(this IEnumerable<IEnumerator> coroutines)
        {
            foreach(IEnumerator c in coroutines)
            {
                yield return c;
            }
        }
    }
}