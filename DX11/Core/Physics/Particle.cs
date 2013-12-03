using System;
using System.Diagnostics;
using SlimDX;

namespace Core.Physics {
    public class Particle {

        protected float inverseMass;
        protected float damping;
        protected Vector3 position;
        protected Vector3 velocity;
        protected Vector3 forceAccum;
        protected Vector3 acceleration;

        public void Integrate(float duration) {
            if (inverseMass <= 0.0f) {
                return;
            }
            Debug.Assert(duration > 0.0f);
            position += velocity*duration;
            var resultingAcc = acceleration;
            resultingAcc += forceAccum*inverseMass;

            velocity += resultingAcc*duration;

            velocity *= MathF.Pow(damping, duration);
            
            ClearAccumulator();
        }
        public float Mass {
            get {
                if (Math.Abs(inverseMass - 0) < float.Epsilon) {
                    return float.MaxValue;
                }
                return 1.0f / inverseMass;
            }
            set {
                Debug.Assert(Math.Abs(value - 0) > float.Epsilon);
                inverseMass = 1.0f/value;
            }
        }
        public float InverseMass {
            get { return inverseMass; }
            set { inverseMass = value; }
        }
        public bool HasFiniteMass { get { return inverseMass >= 0; } }
        public float Damping { get { return damping; } set { damping = value; } }
        public Vector3 Position { get { return position; } set { position = value; } }
        public Vector3 Velocity { get { return velocity; } set { velocity = value; } }
        public Vector3 Acceleration { get { return acceleration; } set { acceleration = value; } }

        public void ClearAccumulator() {
            forceAccum = new Vector3();
        }
        public void AddForce(Vector3 force) {
            forceAccum += force;
        }
    }
}
