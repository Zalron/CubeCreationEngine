using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    public class Utilities
    {
        static int maxHeight = 100;
        static float smooth = 0.01f;
        static int octaves = 4;
        static float persistence = 0.5f;
        public static int GenerateHeight(float x, float z) // generates the height map using fractal brownian motion
        {
            float height = Map(0, maxHeight, 0, 1, fBM(x * smooth, z * smooth, octaves, persistence));
            return (int)height;
        }
        static float Map(float newmin, float newmax, float originalmin, float originalmax, float value) //generates a map for the fractal brownian motion to map on to
        {
            return Mathf.Lerp(newmin, newmax, Mathf.InverseLerp(originalmin, originalmax, value));
        }
        static float fBM(float x, float z, int octaves, float pers) // creates fractal brownian motion with PerlinNoise
        {
            float total = 0;
            float frequency = 1;
            float amplitude = 1;
            float maxValue = 0;
            for (int i = 0; i < octaves; i++)
            {
                total += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2;
            }
            return total / maxValue;
        }
    }
}
