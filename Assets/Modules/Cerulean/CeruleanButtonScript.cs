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
#if UNITY_EDITOR
            s =>
            {
                //Debug.LogFormat("<The Cerulean Button #{0}> {1}", _id, s);
            }
#else
            s => { }
#endif
        );
        Debug.LogFormat("[The Cerulean Button #{0}] Answer: {1}, Constraints: {2}, Left Cube: {3}, Right Cube: {4}", _id, c.Answer, c.Constraints.Select(evc => evc.Direction.ToString() + evc.Index + (char)(evc.Letter + 'A' - 1)).Join(" "), c.LeftCube, c.RightCube);
    }
}
