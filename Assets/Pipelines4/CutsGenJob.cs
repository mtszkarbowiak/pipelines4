using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

namespace Pipelines4
{
    [Unity.Burst.BurstCompile]
    public struct CutsGenJob : IJobFor
    {
        private const float MIN_NODES_SEPARATION = 0.0001f;
        private const float MIN_NODES_SEPARATION_SQUARED = 
            MIN_NODES_SEPARATION * MIN_NODES_SEPARATION;
        private const float MAX_GLOBAL_UP_MAGNITUDE_DEVIATION = 0.0001f;
        private const float MAX_GLOBAL_UP_MAGNITUDE_DEVIATION_SQUARED = 
            MAX_GLOBAL_UP_MAGNITUDE_DEVIATION * MAX_GLOBAL_UP_MAGNITUDE_DEVIATION;
        private const float MIN_TOLERABLE_BEND_RADIUS = 0.001f;
        
        
        [ReadOnly] public float3 GlobalUp;
        [ReadOnly] public float CutMaxAngle, MinimalBendAngle;
        [ReadOnly] public NativeList<float4> Nodes; // Node is built of position XYZ and specific radius W
        [WriteOnly] public NativeList<Cut> Cuts;

        private float3 _cachedArm;
        private float _cachedArmLength;
        private Cut _lastCut;
        private float _totalSplineLenght;
        private float3 _lastSplineLenghtPoint;
        
        
        public bool ValidateBeforeExecution()
        {
            // Check if GlobalUp is normalized.
            if (math.abs(math.lengthsq(GlobalUp) - 1.0f) > MIN_NODES_SEPARATION_SQUARED)
                return false;
            
            for (var i = 1; i < Nodes.Length; i++)
            {
                // Check if Nodes do not overlap.
                var armLenSq = math.lengthsq(Nodes[i] - Nodes[i - 1]);
                if (armLenSq < MAX_GLOBAL_UP_MAGNITUDE_DEVIATION_SQUARED)
                    return false;
                
                // Check if BendRadius is not too small.
                if (Nodes[i].w < MIN_TOLERABLE_BEND_RADIUS)
                    return false;
            }

            
            // Check if Nodes is populated and Cuts empty.
            return Nodes.Length >= 2 && Cuts.IsEmpty;
        }
        
        
        
        public void Execute(int index)
        {
            // Check if by index caps are to be built...
            if (index == 0)
            {
                AddStartCap();
                return;
            }
            
            if (index == Nodes.Length - 1)
            {
                AddEndCap();
                return;
            }
            
            // ... or normal bend.
            AddBend(index);
        }
        
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddStartCap()
        {
            // Check direction of first 'Forward' vector.
            var arm = Nodes[1].xyz - Nodes[0].xyz;
            var armLength = math.length(arm);
            var forward = arm / armLength;

            // Obtain first 'Right' by crossing first 'Forward' with 'GlobalUp'.
            var right = math.normalize(math.cross(forward, GlobalUp));
            
            // Analogically obtain next 'Up'.
            var up = math.normalize(math.cross(right, forward));
            
            // Build cut and assign cached values.
            var result = new Cut()
            {
                Lenght = 0.0f,
                Origin = Nodes[0].xyz,
                Matrix = new float3x3(right, up, forward),
            };
            
            // Add result.
            Cuts.Add(in result);
            
            // Cache useful items for next (first) iteration.
            _lastCut = result;
            _cachedArm = arm;
            _cachedArmLength = armLength;
            _lastSplineLenghtPoint = Nodes[0].xyz;
        }
        
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddBend(int nodeIndex)
        {
            // Obtain 
            var nextArm = Nodes[nodeIndex+1].xyz - Nodes[nodeIndex].xyz;
            var nextArmLength = math.length(nextArm);
            var forward = nextArm / nextArmLength;
            
            // Obtain first 'Right' by crossing first 'Forward' with 'GlobalUp'.
            var right = math.normalize(math.cross(forward, _lastCut.Matrix.c2));
            
            // Analogically obtain next 'Up'.
            var up = math.normalize(math.cross(right, forward));
            
            // Angle calculation.
            var armsDotNumerator = math.dot(_cachedArm, nextArm);
            var armsLenDenominator = _cachedArmLength * nextArmLength;
            var armsAngle = math.abs(math.acos( armsDotNumerator / armsLenDenominator));
            
            //TODO Add first cut without quaternion.
            
            // Check if angle isn't too small.
            if(armsAngle < MinimalBendAngle) return;
            
            // Calculate distance between Node and points of tangency.
            var tangentDist = Nodes[nodeIndex].w * math.abs( math.tan(armsAngle / 2.0f) );
            
            // Calculate point of tangency (before the node itself).
            // From Node itself, we back one (previous) forward (z) step of distance of tangency.
            var tangentPoint = Nodes[nodeIndex].xyz - _lastCut.Matrix.c2 * tangentDist;
            
            // Obtain vector perpendicular to bend plane.
            var bendPlaneNormal = math.normalize(math.cross(_lastCut.Matrix.c2, forward));
            
            // Obtain vector perpendicular to plane normal and (previous) forward.
            var tangentToCenterVector = 
                Nodes[nodeIndex].w * math.normalize(math.cross(bendPlaneNormal, _lastCut.Matrix.c2));

            // Bend center.
            var center = tangentPoint + tangentToCenterVector;

            _totalSplineLenght += math.distance(tangentPoint, _lastSplineLenghtPoint);
            
            // Total cuts number calculation.
            var cutsCount = (int)(armsAngle / CutMaxAngle + 1.5f);
            
            // Let's make sure there's no division by zero while iterating.
            if (cutsCount <= 1) cutsCount = 2;
            
            for (var i = 0; i < cutsCount; i++)
            {
                // Create 
                var cutAngle = i / (float)(cutsCount - 1) * armsAngle;
                var rotator = quaternion.AxisAngle(math.normalize(bendPlaneNormal), cutAngle);
                
                var cutRight = math.rotate(rotator,_lastCut.Matrix.c0);
                var cutUp = math.rotate(rotator,_lastCut.Matrix.c1);
                var cutForward = math.rotate(rotator,_lastCut.Matrix.c2);
                
                var centerToOrigin = math.rotate(rotator, tangentToCenterVector);
                var origin = center - centerToOrigin;
                
                var cut = new Cut()
                {
                    Origin = origin,
                    Matrix = new float3x3(cutRight, cutUp, cutForward),
                    Lenght = _totalSplineLenght + cutAngle * Nodes[nodeIndex].w,
                };
                Cuts.Add(in cut);

                // Cache useful items from last iteration.
                if (i != cutsCount - 1) continue;
                
                _lastCut = cut;
                _lastSplineLenghtPoint = origin;
            }
            
            // Cache useful items for next iteration.
            _totalSplineLenght += armsAngle * Nodes[nodeIndex].w;
            _cachedArm = nextArm;
            _cachedArmLength = nextArmLength;
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddEndCap()
        {
            _totalSplineLenght += math.distance(Nodes[Nodes.Length - 1].xyz, _lastSplineLenghtPoint);
            var origin = Nodes[Nodes.Length - 1].xyz;

            var endingCut = new Cut
            {
                Lenght = _totalSplineLenght,
                Matrix = _lastCut.Matrix,
                Origin = origin
            };
            
            Cuts.Add(in endingCut);
        }
    }
}