using System;

namespace DFLEngine.GameEngine
{
    public static class Rand
    {
        private static Random rand;

        static Rand()
        {
            rand = new Random();
        }

        public static int Next(int min, int max)
        {
            return rand.Next(min, max);
        }

        public static float NextFloat()
        {
            return (float)rand.NextDouble();
        }
    }
}