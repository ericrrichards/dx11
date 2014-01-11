using System;
using System.Diagnostics;
using SlimDX;

namespace Core.Physics {
    public class Particle {
        protected Vector3 ForceAccum;
        public float InverseMass { get; set; }
        public float Mass {
            get {
                if (Math.Abs(InverseMass - 0) < float.Epsilon) {
                    return float.MaxValue;
                }
                return 1.0f / InverseMass;
            }
            set {
                Debug.Assert(Math.Abs(value - 0) > float.Epsilon);
                InverseMass = 1.0f / value;
            }
        }
        public bool HasFiniteMass { get { return InverseMass >= 0; } }
        public float Damping { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 Acceleration { get; set; }

        public void Integrate(float dt) {
            if (InverseMass <= 0.0f) {
                return;
            }
            Debug.Assert(dt > 0.0f);

            Position += Velocity*dt;

            var resultingAcc = Acceleration;
            resultingAcc += ForceAccum*InverseMass;

            Velocity += resultingAcc*dt;

            Velocity *= MathF.Pow(Damping, dt);
            
            ClearAccumulator();
        }
        
        public void ClearAccumulator() {
            ForceAccum = new Vector3();
        }
        public void AddForce(Vector3 force) {
            ForceAccum += force;
        }
    }
}
