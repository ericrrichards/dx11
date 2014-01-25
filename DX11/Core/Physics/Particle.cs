using System.Diagnostics;
using SlimDX;

namespace Core.Physics {
    public static class Constants {
        public static readonly Vector3 Gravity = new Vector3(0, -9.81f, 0);
    }


    public class Particle {

        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 Acceleration { get; set; }

        // fake tweakable "drag" force
        public float Damping { get; set; }

        // using inverse-mass rather than mass avoids 0-mass particles, and allows infinite mass objects
        public float InverseMass { get; set; }
        public float Mass {
            get {
                if (InverseMass <= 0) {
                    return float.MaxValue;
                }
                return 1.0f / InverseMass;
            }
            set {
                Debug.Assert(value > 0);
                InverseMass = 1.0f / value;
            }
        }
        public bool HasFiniteMass { get { return InverseMass >= 0; } }

        // store forces applied during a frame
        protected Vector3 ForceAccum;


        public Particle(Vector3 position, Vector3 initVelocity, Vector3 initAcceleration, float mass) {
            Position = position;
            Velocity = initVelocity;
            Acceleration = initAcceleration;
            Mass = mass;
        }

        protected Particle() {

        }

        public void ClearAccumulator() {
            ForceAccum = new Vector3();
        }
        public void AddForce(Vector3 force) {
            ForceAccum += force;
        }

        public void Integrate(float dt) {
            Debug.Assert(dt > 0);

            // ignore immovable, infinitely massive objects
            if (InverseMass <= 0.0f) {
                return;
            }
            // update position
            Position += Velocity * dt;

            // calculate the acceleration
            var resultingAcc = Acceleration;
            resultingAcc += ForceAccum * InverseMass;

            // update velocity
            Velocity += resultingAcc * dt;
            // apply damping, accounting for frame-time
            Velocity *= MathF.Pow(Damping, dt);

            ClearAccumulator();
        }

    }
}
