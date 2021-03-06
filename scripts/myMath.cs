using System;
using Godot;

namespace MyMath{
    public static class myMath{
        public static float array2dMean(float[,] array, int firstD){
            float total = 0;
            for (int i = 0; i < array.GetLength(1); i++){
                total += (float)array[firstD, i];
            }
            if (float.IsNaN(total)) return 0;
            return ((float)total / array.GetLength(1));
        }

        public static float arrayMax(float[] array){
            int targ = 0;
            float maxval = 0;
            for (int i = 0; i < array.Length; i++){
                if (Math.Abs(array[i]) > maxval == true){
                    maxval = Math.Abs(array[i]);
                    targ = i;
                }
            }
            return array[targ];
        }

        public static float findDegreeDistance(float from, float to){
            float max_angle = 6.28F; //appox PI * 2
            float difference = (to - from % max_angle);
            return Math.Abs((2 * difference % max_angle) - difference);
        }

        public static float roundTo(float n, int r){ //rounds n to nearest r
	        return (Mathf.Round(n * r) / r);
        }
    }
}