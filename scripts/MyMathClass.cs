using System;

namespace MyMath{
    public static class MyMathClass{
        public static float array2dMean(float[,] array, int firstD){
            float total = 0;
            for (int i = 0; i < array.Length; i++){
                total += array[firstD, i];
            }
            return (total / array.Length);
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
    }
}