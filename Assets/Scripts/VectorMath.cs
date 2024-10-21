using UnityEngine;
using System.Collections.Generic;

public static class VectorMath {

    public static void Mean(List<float> list, out float mean){
        float sum = 0;
        for(int i = 0; i < list.Count; i++){
            sum += list[i];
        }
        mean = sum / list.Count;
    }

    public static void Mean(List<float> list, out float mean, out float std){
        float sum = 0;
        for(int i = 0; i < list.Count; i++){
            sum += list[i];
        }
        mean = sum / list.Count;

        float sumOfSquaresOfDifferences = 0;
        for(int i = 0; i < list.Count; i++){
            sumOfSquaresOfDifferences += (list[i] - mean) * (list[i] - mean);
        }
        std = Mathf.Sqrt(sumOfSquaresOfDifferences / list.Count);
    }

    public static void Mean(List<int> list, out float mean){
        int sum = 0;
        for(int i = 0; i < list.Count; i++){
            sum += list[i];
        }
        mean = sum / list.Count;
    }

    public static void Mean(List<int> list, out float mean, out float std){
        Mean(list, out mean);

        float sumOfSquaresOfDifferences = 0;
        for(int i = 0; i < list.Count; i++){
            sumOfSquaresOfDifferences += (list[i] - mean) * (list[i] - mean);
        }
        std = Mathf.Sqrt(sumOfSquaresOfDifferences / list.Count);
    }
}

public static class Vector2Math {

    public static Vector2 Rotate(Vector2 v, float degrees){
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        float tx = v.x;
        float ty = v.y;
        return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
    }

    public static Vector2 Mean(List<Vector2> list){
        Vector2 sum = Vector2.zero;
        for(int i = 0; i < list.Count; i++){
            sum += list[i];
        }
        return sum / list.Count;
    }

    public static void Mean(float[,] array, out float mean){
        float sum = 0;
        for(int i = 0; i < array.GetLength(0); i++){
            for(int j = 0; j < array.GetLength(1); j++){
                sum += array[i, j];
            }
        }
        mean = sum / (array.GetLength(0) * array.GetLength(1));
    }

    public static void Mean(float[,] array, out float mean, out float std){
        Mean(array, out mean);

        float sumOfSquaresOfDifferences = 0;
        for(int i = 0; i < array.GetLength(0); i++){
            for(int j = 0; j < array.GetLength(1); j++){
                sumOfSquaresOfDifferences += (array[i, j] - mean) * (array[i, j] - mean);
            }
        }
        std = Mathf.Sqrt(sumOfSquaresOfDifferences / (array.GetLength(0) * array.GetLength(1)));
    }

    public static float Mean(float[,] array){
        float mean;
        Mean(array, out mean);
        return mean;
    }

    public static float Max(float[,] array){
        float max = float.MinValue;
        for(int i = 0; i < array.GetLength(0); i++){
            for(int j = 0; j < array.GetLength(1); j++){
                if(array[i, j] > max){
                    max = array[i, j];
                }
            }
        }
        return max;
    }

    public static float Min(float[,] array){
        float min = float.MaxValue;
        for(int i = 0; i < array.GetLength(0); i++){
            for(int j = 0; j < array.GetLength(1); j++){
                if(array[i, j] < min){
                    min = array[i, j];
                }
            }
        }
        return min;
    }

    public static void MinAndMax(float[,] array, out float _min, out float _max){
        float min = float.MaxValue;
        float max = float.MinValue;
        for(int i = 0; i < array.GetLength(0); i++){
            for(int j = 0; j < array.GetLength(1); j++){
                if(array[i, j] < min){
                    min = array[i, j];
                }
                if(array[i, j] > max){
                    max = array[i, j];
                }
            }
        }
        _min = min;
        _max = max;
    }
}