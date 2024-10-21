using UnityEngine;

public static class Noise {

    public static float[,] GenerateNoiseMap(int mapWidth, int mapLength, float scale, float persistance, 
                                            float lacunarity, int seed, Vector2 offset, AnimationCurve heightCurve){

        AnimationCurve heightCurveCopy = new AnimationCurve(heightCurve.keys);
        System.Random prng = new System.Random(seed);
        float amplitude = 1;
        float frequency = 1;
        float maxPossibleHeight = amplitude;

        float offsetX = prng.Next(-100000, 100000) + offset.x;
        float offsetY = prng.Next(-100000, 100000) + offset.y;
        Vector2 octiveOffset = new Vector2(offsetX, offsetY);
        float[,] noiseMap = new float[mapWidth, mapLength];
        if(scale <= 0){
            scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        for(int y = 0; y < mapLength; y++){
            for(int x = 0; x < mapWidth; x++){
                amplitude = 1;
                frequency = 1;

                float sampleX = x / scale * frequency + octiveOffset.x;
                float sampleY = y / scale * frequency + octiveOffset.y;
                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                float noiseHeight = perlinValue * amplitude;
                frequency *= lacunarity;

                // normalize
                if (noiseHeight > maxNoiseHeight){
                    maxNoiseHeight = noiseHeight;
                }else if(noiseHeight < minNoiseHeight){
                    minNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapLength; y++){
            for (int x = 0; x < mapWidth; x++){
                float normalizedHeight = noiseMap[x, y] / maxPossibleHeight;
                //normalizedHeight = Truncate(normalizedHeight, 2);
                noiseMap[x, y] = heightCurve.Evaluate(normalizedHeight);
            }
        }

        return noiseMap;
    }

    public static float[,] TransformNoiseMap(float[,] noiseMap){
        int mapWidth = noiseMap.GetLength(0);
        int mapLength = noiseMap.GetLength(1);
        float[,] transformedNoiseMap = new float[mapLength, mapWidth];
        for (int x = 0; x < mapLength; x++){
            for (int y = 0; y < mapWidth; y++){
                transformedNoiseMap[x, y] = noiseMap[y, x];
            }
        }
        return transformedNoiseMap;
    }

    private static float Truncate(float value, int digits){
        double mult = System.Math.Pow(10.0, digits);
        double result = System.Math.Truncate(mult * value) / mult;
        return (float)result;
    }

    public static float[,] ReverseNoiseMap(float[,] noiseMap){
        int mapWidth = noiseMap.GetLength(0);
        int mapLength = noiseMap.GetLength(1);
        float[,] reversedNoiseMap = new float[mapWidth, mapLength];
        for (int x = 0; x < mapLength; x++){
            for (int y = 0; y < mapWidth; y++){
                reversedNoiseMap[x, y] = noiseMap[mapWidth - x - 1, mapLength - y - 1];
            }
        }
        return reversedNoiseMap;
    }

    public static float[,] HeightMapToRenderMap(float[,] noiseMap){
        return ReverseNoiseMap(TransformNoiseMap(noiseMap));
    }

    
}