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
        if (UsedMaskLayers.Count >= 127)
            UsedMaskLayers.Clear();

        int layerIx;
        if (UsedMaskLayers.Count < 64)
        {
            do
                layerIx = Rnd.Range(0, 127);
            while (UsedMaskLayers.Contains(layerIx));
        }
        else
        {
            var available = Enumerable.Range(0, 127).Where(i => !UsedMaskLayers.Contains(i)).ToArray();
            layerIx = available[Rnd.Range(0, available.Length)];
        }

        UsedMaskLayers.Add(layerIx);
        var maskMat = new Material(MaskShaders[layerIx]);
        maskMat.renderQueue = 1000;
        return new MaskMaterials
        {
            Mask = maskMat,
            DiffuseTint = new Material(DiffuseTintShaders[layerIx]),
            DiffuseText = new Material(DiffuseTextShaders[layerIx]),
            Text = new Material(TextShaders[layerIx])
        };
    }
}
