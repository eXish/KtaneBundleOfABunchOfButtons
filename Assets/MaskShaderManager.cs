using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class MaskShaderManager : MonoBehaviour
{
    public Shader[] MaskShaders;
    public Shader[] DiffuseTintShaders;
    public Shader[] TextShaders;
    public Shader[] DiffuseTextShaders;

    public static HashSet<int> UsedMaskLayers = new HashSet<int>();

    public void Clear()
    {
        UsedMaskLayers.Clear();
    }

    public MaskMaterials MakeMaterials()
    {
        if (UsedMaskLayers.Count == 255)
            UsedMaskLayers.Clear();

        int layer = 0;
        while (UsedMaskLayers.Contains(layer))
            layer++;

        //if (UsedMaskLayers.Count < 128)
        //{
        //    do
        //        layer = Rnd.Range(1, 256);
        //    while (UsedMaskLayers.Contains(layer));
        //}
        //else
        //{
        //    var available = Enumerable.Range(1, 256).Where(i => !UsedMaskLayers.Contains(i)).ToArray();
        //    layer = available[Rnd.Range(0, available.Length)];
        //}

        UsedMaskLayers.Add(layer);
        return new MaskMaterials
        {
            Mask = new Material(MaskShaders[layer]),
            DiffuseTint = new Material(DiffuseTintShaders[((layer + 1) | 1) - 1]),
            DiffuseText = new Material(DiffuseTextShaders[layer]),
            Text = new Material(TextShaders[layer])
        };
    }
}
