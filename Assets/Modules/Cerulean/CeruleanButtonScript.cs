using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using BlueButtonLib;

public class CeruleanButtonScript : MonoBehaviour
{
    private int _id = ++_idc;
    private static int _idc;

    public void Start()
    {
        int seed = UnityEngine.Random.Range(0, int.MaxValue);
        Debug.LogFormat("[The Cerulean Button #{0}] Using seed {1}.", _id, seed);
        CeruleanButtonPuzzle c = CeruleanButtonPuzzle.GeneratePuzzle(seed,
            s =>
            {
                Debug.LogFormat("[The Cerulean Button #{0}] {1}", _id, s);
            },
            s =>
            {
                Debug.LogFormat("<The Cerulean Button #{0}> {1}", _id, s);
            });
        Debug.Log(0);
    }
}
