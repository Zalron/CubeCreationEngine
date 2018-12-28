using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeCreationEngine.Core
{
    public class Utilities
    {
        // variables used for dirt height generation
        public static int maxDirtSpawnHeight = 500;
        public static float dirtSmooth = 0.01f;
        public static int dirtOctaves = 4;
        public static float dirtPersistence = 0.5f;
        // varibles used for stone height generation
        public static int maxStoneSpawnHeight = 400;
        public static float stoneSmooth = 0.02f;
        public static int stoneOctaves = 5;
        public static float stonePersistence = 0.5f;
        // varibles used for cave generation
        public static float caveSmooth = 0.07f;
        public static int caveOctaves = 3;
        // variables used for Diamond generation
        public static int maxDiamondSpawnHeight = 20;
        public static float DiamondChance = 0.41f;
        public static int DiamondOctaves = 2;
        public static int GenerateStoneHeight(float x, float z) // generates the stone height map using fractal brownian motion
        {
            float height = Map(0, maxStoneSpawnHeight, 0, 1, fBM(x * stoneSmooth, z * stoneSmooth, stoneOctaves, stonePersistence));
            return (int)height;
        }
        public static int GenerateDirtHeight(float x, float z) // generates the dirt height map using fractal brownian motion
        {
            float height = Map(0, maxDirtSpawnHeight, 0, 1, fBM(x * dirtSmooth, z * dirtSmooth, dirtOctaves, dirtPersistence));
            return (int)height;
        }
        static float Map(float newmin, float newmax, float originalmin, float originalmax, float value) //generates a map for the fractal brownian motion to map on to
        {
            return Mathf.Lerp(newmin, newmax, Mathf.InverseLerp(originalmin, originalmax, value));
        }
        public static float fBM3D(float x, float y, float z, float caveSmooth, int caveOctaves) // creates a 3D version of a fractal brownian motion with PerlinNoise
        {
            float XY = fBM(x * caveSmooth, y * caveSmooth, caveOctaves, 0.5f);
            float YZ = fBM(y * caveSmooth, z * caveSmooth, caveOctaves, 0.5f);
            float XZ = fBM(x * caveSmooth, z * caveSmooth, caveOctaves, 0.5f);
            float YX = fBM(y * caveSmooth, x * caveSmooth, caveOctaves, 0.5f);
            float ZY = fBM(z * caveSmooth, y * caveSmooth, caveOctaves, 0.5f);
            float ZX = fBM(z * caveSmooth, x * caveSmooth, caveOctaves, 0.5f);
            return (XY + YZ + XZ + YX + ZY + ZX) / 6.0f;
        }
        static float fBM(float x, float z, int octaves, float pers) // creates fractal brownian motion with PerlinNoise
        {
            float total = 0;
            float frequency = 1;
            float amplitude = 1;
            float maxValue = 0;
            float offset = 32000f;
            for (int i = 0; i < octaves; i++)
            {
                total += Mathf.PerlinNoise((x+offset) * frequency, (z + offset) * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= dirtPersistence;
                frequency *= 2;
            }
            return total / maxValue;
        }
    }
}
