using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    public class Utilities
    {
        // variables used for dirt height generation
        static int maxDirtHeight = 100;
        static float dirtSmooth = 0.01f;
        static int dirtOctaves = 4;
        static float dirtPersistence = 0.5f;
        // varibles used for stone height generation
        static int maxStoneHeight = 95;
        static float stoneSmooth = 0.02f;
        static int stoneOctaves = 5;
        static float stonePersistence = 0.5f;
        public static int GenerateStoneHeight(float x, float z) // generates the stone height map using fractal brownian motion
        {
            float height = Map(0, maxStoneHeight, 0, 1, fBM(x * stoneSmooth, z * stoneSmooth, stoneOctaves, stonePersistence));
            return (int)height;
        }
        public static int GenerateDirtHeight(float x, float z) // generates the dirt height map using fractal brownian motion
        {
            float height = Map(0, maxDirtHeight, 0, 1, fBM(x * dirtSmooth, z * dirtSmooth, dirtOctaves, dirtPersistence));
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
                amplitude *= dirtPersistence;
                frequency *= 2;
            }
            return total / maxValue;
        }
    }
}
