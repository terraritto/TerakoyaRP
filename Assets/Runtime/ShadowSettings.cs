using UnityEngine;

[System.Serializable]
public class ShadowSettings
{
    // ShadowMapのサイズ(2の累乗)
    public enum MapSize
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
        _8192 = 8192,
    }

    // 影の描画をする最大距離
    [Min(0f)]
    public float maxDistance = 100f;

    // Shadow用DirectionalLight
    [System.Serializable]
    public struct Directional
    {
        public MapSize atlasSize;
    }
    
    // デフォルト値
    public Directional directional =
        new Directional 
        { 
            atlasSize = MapSize._1024 
        };
}
