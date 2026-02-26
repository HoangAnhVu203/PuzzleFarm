using System.Collections;
using UnityEngine;

public class DailyOnlineTimer : MonoBehaviour
{
    Coroutine cr;

    public void StartCount()
    {
        if (cr != null) return;
        cr = StartCoroutine(Loop());
    }

    public void StopCount()
    {
        if (cr == null) return;
        StopCoroutine(cr);
        cr = null;
    }

    IEnumerator Loop()
    {
        while (true)
        {
            yield return new WaitForSeconds(60f);
            DailyMissionSystem.Instance?.AddProgressById(DailyMissionId.Online30Min, 1);
        }
    }
}