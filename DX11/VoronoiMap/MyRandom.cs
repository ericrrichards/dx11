using System;

namespace VoronoiMap {
    /// <summary>
    ///  This is soley for the purpose of testing the javascript random generator I added to the original javascript implementation
    /// 
    /// </summary>
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
    class MyRandom {



        private const int MBIG = Int32.MaxValue;

        private const int MSEED = 161803398;

        private const int MZ = 0;



        //
        // Member Variables 
        // 
        private int inext;
        private int inextp;
        private readonly int[] SeedArray = new int[56];

        public MyRandom(int Seed) {
            //Initialize our Seed array.
            //This algorithm comes from Numerical Recipes in C (2nd Ed.) 
            var subtraction = (Seed == Int32.MinValue) ? Int32.MaxValue : Math.Abs(Seed);
            var mj = MSEED - subtraction;
            SeedArray[55] = mj;
            var mk = 1;
            for (var i = 1; i < 55; i++) {  //Apparently the range [1..55] is special (Knuth) and so we're wasting the 0'th position.
                var ii = (21 * i) % 55;
                SeedArray[ii] = mk;
                mk = mj - mk;
                if (mk < 0) mk += MBIG;
                mj = SeedArray[ii];
            }
            for (var k = 1; k < 5; k++) {
                for (var i = 1; i < 56; i++) {
                    SeedArray[i] -= SeedArray[1 + (i + 30) % 55];
                    if (SeedArray[i] < 0) SeedArray[i] += MBIG;
                }
            }
            inext = 0;
            inextp = 21;
        }
        protected virtual double Sample() {
            //Including this division at the end gives us significantly improved 
            //random number distribution.
            return (InternalSample() * (1.0 / MBIG));
        }

        private int InternalSample() {
            var locINext = inext;
            var locINextp = inextp;

            if (++locINext >= 56) locINext = 1;
            if (++locINextp >= 56) locINextp = 1;

            var retVal = SeedArray[locINext] - SeedArray[locINextp];

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
    // ReSharper restore InconsistentNaming
    // ReSharper restore UnusedMember.Local
    // ReSharper restore UnusedMember.Global
}