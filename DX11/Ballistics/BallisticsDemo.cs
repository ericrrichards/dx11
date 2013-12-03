using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core;
using Core.Physics;

namespace Ballistics {
    class BallisticsDemo :D3DApp {
        enum ShotType {
            Unused,
            Pistol,
            Artillery,
            Fireball,
            Laser
        }
        struct AmmoRound {
            public Particle Particle;
            public ShotType ShotType;
            public int StartTime;

            public void Render() {
                var position = Particle.Position;


            }

        }


        protected BallisticsDemo(IntPtr hInstance) : base(hInstance) {}

        static void Main(string[] args) {
        }
    }
}
