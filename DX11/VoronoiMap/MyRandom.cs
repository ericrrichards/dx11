using System;

namespace VoronoiMap {
    /// <summary>
    ///  This is soley for the purpose of testing the javascript random generator I added to the original javascript implementation
    /// 
    /// </summary>
    class MyRandom {
        private const int MBIG = Int32.MaxValue;
        private const int MSEED = 161803398;
        private const int MZ = 0;


        //
        // Member Variables 
        // 
        private int inext;
        private int inextp;
        internal int[] SeedArray = new int[56];

        public MyRandom(int Seed) {
            int ii;
            int mj, mk;

            //Initialize our Seed array.
            //This algorithm comes from Numerical Recipes in C (2nd Ed.) 
            int subtraction = (Seed == Int32.MinValue) ? Int32.MaxValue : Math.Abs(Seed);
            mj = MSEED - subtraction;
            SeedArray[55] = mj;
            mk = 1;
            for (int i = 1; i < 55; i++) {  //Apparently the range [1..55] is special (Knuth) and so we're wasting the 0'th position.
                ii = (21 * i) % 55;
                SeedArray[ii] = mk;
                mk = mj - mk;
                if (mk < 0) mk += MBIG;
                mj = SeedArray[ii];
            }
            for (int k = 1; k < 5; k++) {
                for (int i = 1; i < 56; i++) {
                    SeedArray[i] -= SeedArray[1 + (i + 30) % 55];
                    if (SeedArray[i] < 0) SeedArray[i] += MBIG;
                }
            }
            inext = 0;
            inextp = 21;
            Seed = 1;
        }
        protected virtual double Sample() {
            //Including this division at the end gives us significantly improved 
            //random number distribution.
            return (InternalSample() * (1.0 / MBIG));
        }

        private int InternalSample() {
            int retVal;
            int locINext = inext;
            int locINextp = inextp;

            if (++locINext >= 56) locINext = 1;
            if (++locINextp >= 56) locINextp = 1;

            retVal = SeedArray[locINext] - SeedArray[locINextp];

            if (retVal == MBIG) retVal--;
            if (retVal < 0) retVal += MBIG;

            SeedArray[locINext] = retVal;

            inext = locINext;
            inextp = locINextp;

            return retVal;
        }
        public virtual int Next(int maxValue) {
            if (maxValue < 0) {
                throw new ArgumentOutOfRangeException("maxValue", "ArgumentOutOfRange_MustBePositive", "maxValue");
            }
            return (int)(Sample() * maxValue);
        }

    }
}