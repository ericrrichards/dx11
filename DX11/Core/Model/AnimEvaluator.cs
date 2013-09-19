namespace Core.Model {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Assimp;

    using SlimDX;

    public class AnimEvaluator {
        public string Name { get; set; }
        public List<AnimationChannel> Channels { get; set; }
        public bool PlayAnimationForward { get; set; }
        public float LastTime { get; set; }
        public float TicksPerSecond { get; set; }
        public float Duration { get; set; }
        public List<Tuple<int, int, int>> LastPositions { get; set; }
        public List<List<Matrix>> Transforms { get; set; }

        public AnimEvaluator() {
            LastTime = 0.0f;
            TicksPerSecond = 0.0f;
            Duration = 0.0f;
            PlayAnimationForward = true;
            Transforms = new List<List<Matrix>>();
        }
        public AnimEvaluator(Animation anim, SceneAnimator scene) {
            LastTime = 0.0f;
            TicksPerSecond = anim.TicksPerSecond != 0.0f ? (float)anim.TicksPerSecond : 100.0f;
            Duration = (float)anim.DurationInTicks;
            Name = anim.Name;
            Channels = new List<AnimationChannel>();
            foreach (var channel in anim.NodeAnimationChannels) {
                var c = new AnimationChannel();
                c.Name = channel.NodeName;
                c.PositionKeys = channel.PositionKeys.ToList();
                c.RotationKeys = channel.RotationKeys.ToList();
                c.ScalingKeys = channel.ScalingKeys.ToList();
                Channels.Add(c);
            }
            LastPositions = Enumerable.Repeat(new Tuple<int, int, int>(0, 0, 0), anim.NodeAnimationChannelCount).ToList();
            Transforms = new List<List<Matrix>>();
            PlayAnimationForward = true;
        }

        public void Evaluate(float dt, Dictionary<string, Bone> bones) {
            dt *= TicksPerSecond;
            var time = 0.0f;
            if (Duration > 0.0f) {
                time = dt % Duration;
            }
            for (int i = 0; i < Channels.Count; i++) {
                var channel = Channels[i];
                if (!bones.ContainsKey(channel.Name)) {
                    Console.WriteLine("Did not find the bone node " + channel.Name);
                    continue;
                }
                var pPosition = new Vector3D();
                if (channel.PositionKeys.Count > 0) {
                    var frame = (time >= LastTime) ? LastPositions[i].Item1 : 0;
                    while (frame < channel.PositionKeys.Count - 1) {
                        if (time < channel.PositionKeys[frame + 1].Time) {
                            break;
                        }
                        frame++;
                    }
                    if (frame >=channel.PositionKeys.Count) {
                        frame = 0;
                    }
                    
                    var nextFrame = (frame + 1) % channel.PositionKeys.Count;

                    var key = channel.PositionKeys[frame];
                    var nextKey = channel.PositionKeys[nextFrame];
                    var diffTime = nextKey.Time - key.Time;
                    if (diffTime < 0.0) {
                        diffTime += Duration;
                    }
                    if (diffTime > 0.0) {
                        var factor = (float)((time - key.Time) / diffTime);
                        pPosition = key.Value + (nextKey.Value - key.Value)*factor;
                    } else {
                        pPosition = key.Value;
                    }
                    LastPositions[i].Item1 = frame;

                }
                var pRot = new Assimp.Quaternion(1, 0, 0, 0);
                if (channel.RotationKeys.Count > 0) {
                    var frame = (time >= LastTime) ? LastPositions[i].Item2 : 0;
                    while (frame < channel.RotationKeys.Count - 1) {
                        if (time < channel.RotationKeys[frame + 1].Time) {
                            break;
                        }
                        frame++;
                    }
                    if (frame >= channel.RotationKeys.Count) {
                        frame = 0;
                    }
                    var nextFrame = (frame + 1) % channel.RotationKeys.Count;

                    var key = channel.RotationKeys[frame];
                    var nextKey = channel.RotationKeys[nextFrame];
                    key.Value.Normalize();
                    nextKey.Value.Normalize();
                    var diffTime = nextKey.Time - key.Time;
                    if (diffTime < 0.0) {
                        diffTime += Duration;
                    }
                    if (diffTime > 0) {
                        var factor = (float)((time - key.Time) / diffTime);
                        pRot = Assimp.Quaternion.Slerp(key.Value, nextKey.Value, factor);
                    } else {
                        pRot = key.Value;
                    }
                    LastPositions[i].Item2 = frame;

                }
                var pscale = new Vector3D(1);
                if (channel.ScalingKeys.Count > 0) {
                    var frame = (time >= LastTime) ? LastPositions[i].Item3 : 0;
                    while (frame < channel.ScalingKeys.Count - 1) {
                        if (time < channel.ScalingKeys[frame + 1].Time) {
                            break;
                        }
                        frame++;
                    }
                    if (frame >= channel.ScalingKeys.Count) {
                        frame = 0;
                    }
                    var nextFrame = (frame + 1) % channel.ScalingKeys.Count;
                    var key = channel.ScalingKeys[frame];
                    var nextKey = channel.ScalingKeys[nextFrame];
                    var diffTime = nextKey.Time - key.Time;
                    if (diffTime < 0.0) {
                        diffTime += Duration;
                    }
                    pscale = key.Value;
                    /*
                    if (diffTime > 0.0) {
                        var factor = (float) ((time - key.Time)/diffTime);
                        pscale = Vector3.Lerp(key.Value.ToVector3(), nextKey.Value.ToVector3(), factor);
                    } else {

                        pscale = channel.ScalingKeys[frame].Value.ToVector3();
                    }*/
                    LastPositions[i].Item3 = frame;
                }
                var mat = new Assimp.Matrix4x4(pRot.GetMatrix());
                mat.A1 *= pscale.X;mat.B1 *= pscale.X;mat.C1 *= pscale.X;
                mat.A2 *= pscale.Y; mat.B2 *= pscale.Y; mat.C2 *= pscale.Y;
                mat.A3 *= pscale.Z; mat.B3 *= pscale.Z; mat.C3 *= pscale.Z;
                mat.A4 = pPosition.X;mat.B4 = pPosition.Y;mat.C4 = pPosition.Z;

                mat.Transpose();
                bones[channel.Name].LocalTransform = mat.ToMatrix();
            }
            LastTime = time;
        }
        public List<Matrix> GetTransforms(float dt) {
            return Transforms[GetFrameIndexAt(dt)];
        }

        public int GetFrameIndexAt(float dt) {
            dt *= TicksPerSecond;
            var time = 0.0f;
            if (Duration > 0.0f) {
                time = dt % Duration;
            }
            var percent = time / Duration;
            if (!PlayAnimationForward) {
                percent = (percent - 1.0f) * -1.0f;
            }
            var frameIndexAt = (int) (Transforms.Count*percent);
            return frameIndexAt;
        }
    }
    
    public class AnimationChannel {
        public string Name { get; set; }
        public List<VectorKey> PositionKeys { get; set; }
        public List<QuaternionKey> RotationKeys { get; set; }
        public List<VectorKey> ScalingKeys { get; set; }
    }

    public class Tuple<T1, T2, T3> {
        public Tuple(T1 i, T2 i1, T3 i2) {
            Item1 = i;
            Item2 = i1;
            Item3 = i2;
        }

        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
    }
}