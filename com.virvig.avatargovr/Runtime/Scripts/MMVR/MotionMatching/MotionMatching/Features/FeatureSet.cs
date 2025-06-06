using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;

namespace MotionMatching
{
    using Joint = Skeleton.Joint;

    /// <summary>
    /// Stores all features vectors of all poses for Motion Matching
    /// </summary>
    public class FeatureSet
    {
        public int NumberFeatureVectors { get; private set; } // Total number of feature vectors
        public int NumberTrajectoryFeatures { get; private set; } // Number of different trajectory features (e.g. 2 = position and direction)
        private readonly int[] NumberPredictionsTrajectory; // Size: NumberTrajectoryFeatures. Number of predictions per trajectory feature (e.g. {3, 4}, means 3 predictions for position and 4 for direction)
        private readonly int[] NumberFloatsTrajectory; // Size: NumberTrajectoryFeatures. Number of floats per trajectory feature (e.g. {2, 3}, means 2 floats for position (float2) and 3 for direction (float3))
        private readonly int[] TrajectoryOffset; // Size: NumberTrajectoryFeatures. Offset of the trajectory feature in the feature vector (e.g. {0, 6}, means the position feature is at the beginning of the feature vector, and the direction feature starts at the sixth float)
        public int NumberPoseFeatures { get; private set; } // Number of different pose features (e.g. 3 = leftFootPosition, leftFootVelocity, hipsVelocity)
        private const int NumberFloatsPose = 3; // Number of floats per pose feature (e.g. 3 = float3)
        public int PoseOffset { get; private set; } // Offset of the pose feature in the feature vector (e.g. 3 = leftFootPosition is at the third float)
        public int FeatureSize { get; private set; } // Total size of a feature vector

        private NativeArray<bool> Valid; // TODO: Refactor to avoid needing this
        private NativeArray<float> Features; // Each feature: Trajectory + Pose
        private float[] Mean;
        private float[] StandardDeviation;

        public FeatureSet(MotionMatchingData mmData, int numberFeatureVectors)
        {
            NumberFeatureVectors = numberFeatureVectors;

            NumberTrajectoryFeatures = mmData.TrajectoryFeatures.Count;
            NumberPredictionsTrajectory = new int[NumberTrajectoryFeatures];
            NumberFloatsTrajectory = new int[NumberTrajectoryFeatures];
            TrajectoryOffset = new int[NumberTrajectoryFeatures];
            int offset = 0;
            for (int i = 0; i < NumberTrajectoryFeatures; i++)
            {
                TrajectoryOffset[i] = offset;
                NumberPredictionsTrajectory[i] = mmData.TrajectoryFeatures[i].FramesPrediction.Length;
                NumberFloatsTrajectory[i] = mmData.TrajectoryFeatures[i].Project ? 2 : 3;
                offset += NumberPredictionsTrajectory[i] * NumberFloatsTrajectory[i];
            }
            PoseOffset = offset;
            NumberPoseFeatures = mmData.PoseFeatures.Count;
            FeatureSize = offset; // Trajectory
            FeatureSize += NumberPoseFeatures * NumberFloatsPose; // + Pose
        }

        public bool IsValidFeature(int featureIndex)
        {
            return Valid[featureIndex];
        }

        public void GetFeature(ref NativeArray<float> feature, int featureIndex)
        {
            Debug.Assert(feature.Length == FeatureSize, "Feature vector has wrong size");
            for (int i = 0; i < FeatureSize; i++)
            {
                feature[i] = Features[featureIndex * FeatureSize + i];
            }
        }

        public float2 GetProjectedTrajectoryFeature(int featureIndex, int trajectoryFeatureIndex, int predictionIndex, bool denormalize = false)
        {
            int featureOffset = TrajectoryOffset[trajectoryFeatureIndex] + predictionIndex * NumberFloatsTrajectory[trajectoryFeatureIndex];
            int startIndex = featureIndex * FeatureSize + featureOffset;
            float x = Features[startIndex];
            float y = Features[startIndex + 1];
            if (denormalize)
            {
                x = x * StandardDeviation[featureOffset] + Mean[featureOffset];
                y = y * StandardDeviation[featureOffset + 1] + Mean[featureOffset + 1];
            }
            return new float2(x, y);
        }
        public float3 GetTrajectoryFeature(int featureIndex, int trajectoryFeatureIndex, int predictionIndex, bool denormalize = false)
        {
            int featureOffset = TrajectoryOffset[trajectoryFeatureIndex] + predictionIndex * NumberFloatsTrajectory[trajectoryFeatureIndex];
            int startIndex = featureIndex * FeatureSize + featureOffset;
            float x = Features[startIndex];
            float y = Features[startIndex + 1];
            float z = Features[startIndex + 2];
            if (denormalize)
            {
                x = x * StandardDeviation[featureOffset] + Mean[featureOffset];
                y = y * StandardDeviation[featureOffset + 1] + Mean[featureOffset + 1];
                z = z * StandardDeviation[featureOffset + 2] + Mean[featureOffset + 2];
            }
            return new float3(x, y, z);
        }
        public float3 GetPoseFeature(int featureIndex, int poseFeatureIndex, bool denormalize = false)
        {
            int featureOffset = PoseOffset + poseFeatureIndex * NumberFloatsPose;
            int startIndex = featureIndex * FeatureSize + featureOffset;
            float x = Features[startIndex];
            float y = Features[startIndex + 1];
            float z = Features[startIndex + 2];
            if (denormalize)
            {
                x = x * StandardDeviation[featureOffset] + Mean[featureOffset];
                y = y * StandardDeviation[featureOffset + 1] + Mean[featureOffset + 1];
                z = z * StandardDeviation[featureOffset + 2] + Mean[featureOffset + 2];
            }
            return new float3(x, y, z);
        }

        public NativeArray<bool> GetValid()
        {
            return Valid;
        }
        public NativeArray<float> GetFeatures()
        {
            return Features;
        }

        public float GetMean(int dimension)
        {
            return Mean[dimension];
        }
        public float GetStandardDeviation(int dimension)
        {
            return StandardDeviation[dimension];
        }

        // Deserialize ---------------------------------------
        public void SetValid(NativeArray<bool> valid)
        {
            Debug.Assert(valid.Length == NumberFeatureVectors, "Valid array has wrong size");

            if (Valid != null && Valid.IsCreated)
            {
                Valid.Dispose();
            }

            Valid = valid;
        }
        public void SetFeatures(NativeArray<float> features)
        {
            Debug.Assert(features.Length == NumberFeatureVectors * FeatureSize, "Feature vector has wrong size");

            if (Features != null && Features.IsCreated)
            {
                Features.Dispose();
            }

            Features = features;
        }
        public void SetMean(float[] mean)
        {
            Debug.Assert(mean.Length == FeatureSize, "Mean array has wrong size");
            Mean = mean;
        }
        public void SetStandardDeviation(float[] standardDeviation)
        {
            Debug.Assert(standardDeviation.Length == FeatureSize, "Standard deviation array has wrong size");
            StandardDeviation = standardDeviation;
        }
        // --------------------------------------------------

        /// <summary>
        /// Normalizes the trajectory features (pose features remaing untouched)
        /// </summary>
        public void NormalizeTrajectory(ref NativeArray<float> featureVector)
        {
            Debug.Assert(Mean != null, "Mean is not initialized");
            Debug.Assert(StandardDeviation != null, "StandardDeviation is not initialized");
            Debug.Assert(featureVector.Length == FeatureSize, "Feature vector size does not match");

            for (int i = 0; i < PoseOffset; i++)
            {
                featureVector[i] = (featureVector[i] - Mean[i]) / StandardDeviation[i];
            }
        }

        /// <summary>
        /// Normalizes all features (trajectory + pose)
        /// </summary>
        public void NormalizeFeatureVector(ref NativeArray<float> featureVector)
        {
            Debug.Assert(Mean != null, "Mean is not initialized");
            Debug.Assert(StandardDeviation != null, "StandardDeviation is not initialized");
            Debug.Assert(featureVector.Length == FeatureSize, "Feature vector size does not match");

            for (int i = 0; i < featureVector.Length; i++)
            {
                featureVector[i] = (featureVector[i] - Mean[i]) / StandardDeviation[i];
            }
        }

        /// <summary>
        /// Returns a copy of the feature vector with the features before normalization
        /// </summary>
        public void DenormalizeFeatureVector(NativeArray<float> featureVector)
        {
            Debug.Assert(Mean != null, "Mean is not initialized");
            Debug.Assert(StandardDeviation != null, "StandardDeviation is not initialized");
            Debug.Assert(featureVector.Length == FeatureSize, "Feature vector size does not match");

            for (int i = 0; i < featureVector.Length; i++)
            {
                featureVector[i] = featureVector[i] * StandardDeviation[i] + Mean[i];
            }
        }

        /// <summary>
        /// Normalizes the features by subtracting mean and dividing by the standard deviation
        /// </summary>
        public void NormalizeFeatures()
        {
            // Compute Mean and Standard Deviation
            ComputeMeanAndStandardDeviation();

            // Normalize all feature vectors
            for (int i = 0; i < NumberFeatureVectors; i++)
            {
                int featureIndex = i * FeatureSize;
                if (Valid[i])
                {
                    for (int j = 0; j < FeatureSize; j++)
                    {
                        Features[featureIndex + j] = (Features[featureIndex + j] - Mean[j]) / StandardDeviation[j];
                    }
                }
            }
        }

        private void ComputeMeanAndStandardDeviation()
        {
            int nTotalDimensions = FeatureSize;
            // Mean for each dimension
            Mean = new float[nTotalDimensions];
            // Variance for each dimension
            Span<float> variance = stackalloc float[nTotalDimensions];
            // Standard Deviation for each dimension
            StandardDeviation = new float[nTotalDimensions];

            // Compute Means for each dimension of each feature
            int count = 0;
            for (int i = 0; i < NumberFeatureVectors; i++)
            {
                if (Valid[i])
                {
                    int featureIndex = i * FeatureSize;
                    for (int j = 0; j < nTotalDimensions; j++)
                    {
                        Mean[j] += Features[featureIndex + j];
                    }
                    count += 1;
                }
            }
            for (int i = 0; i < nTotalDimensions; i++)
            {
                Mean[i] /= count;
            }
            // Compute Variance for each dimension of each feature - variance = (x - mean)^2 / n
            for (int i = 0; i < NumberFeatureVectors; i++)
            {
                int featureIndex = i * FeatureSize;
                if (Valid[i])
                {
                    for (int j = 0; j < nTotalDimensions; j++)
                    {
                        float diff = Features[featureIndex + j] - Mean[j];
                        variance[j] += diff * diff;
                    }
                }
            }
            for (int i = 0; i < nTotalDimensions; i++)
            {
                variance[i] /= count;
            }

            // Compute Standard Deviations of a feature as the average std across all dimensions - std = sqrt(variance)
            //for (int d = 0; d < NumberTrajectoryFeatures; d++)
            //{
            //    int offset = TrajectoryOffset[d];
            //    int nDimensions = NumberPredictionsTrajectory[d] * NumberFloatsTrajectory[d];
            //    float std = 0;
            //    for (int j = 0; j < nDimensions; j++)
            //    {
            //        std += math.sqrt(variance[offset + j]);
            //    }
            //    std /= nDimensions;
            //    Debug.Assert(std > 0, "Standard deviation is zero, feature with no variation is probably a bug");
            //    for (int j = 0; j < nDimensions; j++)
            //    {
            //        StandardDeviation[offset + j] = std;
            //    }
            //}
            //for (int d = 0; d < NumberPoseFeatures; d++)
            //{
            //    int offset = PoseOffset + d * NumberFloatsPose;
            //    float std = 0;
            //    for (int j = 0; j < NumberFloatsPose; j++)
            //    {
            //        std += math.sqrt(variance[offset + j]);
            //    }
            //    std /= NumberFloatsPose;
            //    Debug.Assert(std > 0, "Standard deviation is zero, feature with no variation is probably a bug");
            //    for (int j = 0; j < NumberFloatsPose; j++)
            //    {
            //        StandardDeviation[offset + j] = std;
            //    }
            //}

            // Compute Standard Deviations of a feature as the sqrt(variance) for each dimension
            for (int d = 0; d < NumberTrajectoryFeatures; d++)
            {
                int offset = TrajectoryOffset[d];
                int nDimensions = NumberPredictionsTrajectory[d] * NumberFloatsTrajectory[d];
                for (int j = 0; j < nDimensions; j++)
                {
                    StandardDeviation[offset + j] = math.sqrt(variance[offset + j]);
                }
            }
            for (int d = 0; d < NumberPoseFeatures; d++)
            {
                int offset = PoseOffset + d * NumberFloatsPose;
                for (int j = 0; j < NumberFloatsPose; j++)
                {
                    StandardDeviation[offset + j] = math.sqrt(variance[offset + j]);
                }
            }
        }

        /// <summary>
        /// Extract the feature vectors from poseSet
        /// </summary>
        public void Extract(PoseSet poseSet, MotionMatchingData mmData)
        {
            // Init
            int nPoses = poseSet.NumberPoses;
            Valid = new NativeArray<bool>(nPoses, Allocator.Persistent);
            Features = new NativeArray<float>(nPoses * FeatureSize, Allocator.Persistent);
            // Check skeleton has all needed joints
            bool[] simulationBone = new bool[NumberTrajectoryFeatures];
            Joint[] jointsTrajectory = new Joint[NumberTrajectoryFeatures];
            int i = 0;
            foreach (var trajectoryFeature in mmData.TrajectoryFeatures)
            {
                if (trajectoryFeature.SimulationBone) simulationBone[i] = true;
                else if (!poseSet.Skeleton.Find(trajectoryFeature.Bone, out jointsTrajectory[i])) Debug.Assert(false, "The skeleton does not contain any joint of type " + trajectoryFeature.Bone);
                i += 1;
            }
            Joint[] jointsPose = new Joint[NumberPoseFeatures];
            i = 0;
            foreach (var poseFeature in mmData.PoseFeatures)
            {
                if (!poseSet.Skeleton.Find(poseFeature.Bone, out jointsPose[i])) Debug.Assert(false, "The skeleton does not contain any joint of type " + poseFeature.Bone);
                i += 1;
            }
            // Extract Features
            for (int poseIndex = 0; poseIndex < nPoses; ++poseIndex)
            {
                if (poseSet.IsPoseValidForPrediction(poseIndex))
                {
                    Valid[poseIndex] = true;
                    ExtractFeature(poseSet, poseIndex, simulationBone, jointsTrajectory, jointsPose, mmData);
                }
                else Valid[poseIndex] = false;
            }
        }

        /// <summary>
        /// Extract the feature vectors from poseSet
        /// </summary>
        private void ExtractFeature(PoseSet poseSet, int poseIndex, bool[] simulationBone, Joint[] jointsTrajectory, Joint[] jointsPose, MotionMatchingData mmData)
        {
            int featureIndex = poseIndex * FeatureSize;
            poseSet.GetPose(poseIndex, out PoseVector pose);
            poseSet.GetPose(poseIndex + 1, out PoseVector poseNext);
            // Compute local features based on the Simulation Bone
            // so hips and feet are local to a stable position with respect to the character
            GetWorldOriginCharacter(pose, out float3 characterOrigin, out float3 characterForward);
            // Trajectory
            for (int i = 0; i < NumberTrajectoryFeatures; i++)
            {
                MotionMatchingData.TrajectoryFeature trajectoryFeature = mmData.TrajectoryFeatures[i];
                int featureOffset = featureIndex + TrajectoryOffset[i];
                int featureSize = NumberPredictionsTrajectory[i] * NumberFloatsTrajectory[i];
                for (int p = 0; p < trajectoryFeature.FramesPrediction.Length; ++p)
                {
                    if (poseIndex + trajectoryFeature.FramesPrediction[p] >= poseSet.NumberPoses)
                    {
                        Debug.LogWarning("Out-of-bounds pose access in trajectory feature.");
                        continue;
                    }
                    int predictionOffset = featureOffset + p * NumberFloatsTrajectory[i];
                    poseSet.GetPose(poseIndex + trajectoryFeature.FramesPrediction[p], out PoseVector futurePose);
                    float3 value = new float3();
                    float2 projectedValue = new float2();
                    switch (trajectoryFeature.FeatureType)
                    {
                        case MotionMatchingData.TrajectoryFeature.Type.Position:
                            GetTrajectoryPosition(futurePose, poseSet.Skeleton, simulationBone[i], jointsTrajectory[i], characterOrigin, characterForward,
                                                  out value);
                            if (trajectoryFeature.Project)
                            {
                                projectedValue = new float2(value.x, value.z);
                            }
                            break;
                        case MotionMatchingData.TrajectoryFeature.Type.Direction:
                            GetTrajectoryDirection(futurePose, poseSet.Skeleton, simulationBone[i], jointsTrajectory[i], characterForward, mmData,
                                                   out value);
                            if (trajectoryFeature.Project)
                            {
                                projectedValue = math.normalize(new float2(value.x, value.z));
                            }
                            break;
                        default:
                            Debug.Assert(false, "Unsupported TrajectoryFeature.Type: " + trajectoryFeature.FeatureType);
                            break;
                    }
                    if (trajectoryFeature.Project)
                    {
                        Features[predictionOffset] = projectedValue.x;
                        Features[predictionOffset + 1] = projectedValue.y;
                    }
                    else
                    {
                        Features[predictionOffset] = value.x;
                        Features[predictionOffset + 1] = value.y;
                        Features[predictionOffset + 2] = value.z;
                    }
                }
            }

            // Pose
            for (int i = 0; i < NumberPoseFeatures; i++)
            {
                MotionMatchingData.PoseFeature poseFeature = mmData.PoseFeatures[i];
                int featureOffset = featureIndex + PoseOffset + i * NumberFloatsPose;
                float3 feature = new float3();
                switch (poseFeature.FeatureType)
                {
                    case MotionMatchingData.PoseFeature.Type.Position:
                        GetJointPosition(pose, poseSet.Skeleton, jointsPose[i], characterOrigin, characterForward,
                                         out feature);
                        break;
                    case MotionMatchingData.PoseFeature.Type.Velocity:
                        GetJointVelocity(pose, poseNext, poseSet.Skeleton, jointsPose[i], characterOrigin, characterForward, poseSet.FrameTime,
                                         out feature);
                        break;
                    default:
                        Debug.Assert(false, "Unknown PoseFeature.Type: " + poseFeature.FeatureType);
                        break;
                }
                Features[featureOffset + 0] = feature.x;
                Features[featureOffset + 1] = feature.y;
                Features[featureOffset + 2] = feature.z;
            }
        }

        private static void GetTrajectoryPosition(PoseVector pose, Skeleton skeleton, bool simulationBone, Joint joint, float3 characterOrigin, float3 characterForward,
                                                  out float3 futureLocalPosition)
        {
            float3 worldPosition;
            if (simulationBone)
            {
                worldPosition = pose.JointLocalPositions[0];
            }
            else
            {
                worldPosition = GetWorldPosition(skeleton, pose, joint);
            }
            futureLocalPosition = GetLocalPositionFromCharacter(worldPosition, characterOrigin, characterForward);
        }
        private static void GetTrajectoryDirection(PoseVector pose, Skeleton skeleton, bool simulationBone, Joint joint, float3 characterForward, MotionMatchingData mmData,
                                                   out float3 futureLocalDirection)
        {
            quaternion worldRotation;
            float3 localForward;
            if (simulationBone)
            {
                worldRotation = pose.JointLocalRotations[0];
                localForward = math.forward();
            }
            else
            {
                worldRotation = GetWorldRotation(skeleton, pose, joint);
                localForward = mmData.GetLocalForward(joint.Index);
            }
            float3 worldDirection = math.mul(worldRotation, localForward);
            futureLocalDirection = GetLocalDirectionFromCharacter(worldDirection, characterForward);
        }

        private static void GetJointPosition(PoseVector pose, Skeleton skeleton, Joint joint, float3 characterOrigin, float3 characterForward,
                                             out float3 localPosition)
        {
            float3 worldPosition = GetWorldPosition(skeleton, pose, joint);
            localPosition = GetLocalPositionFromCharacter(worldPosition, characterOrigin, characterForward);
        }
        private static void GetJointVelocity(PoseVector pose, PoseVector poseNext, Skeleton skeleton, Joint joint, float3 characterOrigin, float3 characterForward, float frameTime,
                                             out float3 localVelocity)
        {
            float3 worldPosition = GetWorldPosition(skeleton, pose, joint);
            float3 worldPositionNext = GetWorldPosition(skeleton, poseNext, joint);
            float3 localPosition = GetLocalPositionFromCharacter(worldPosition, characterOrigin, characterForward);
            localVelocity = (GetLocalPositionFromCharacter(worldPositionNext, characterOrigin, characterForward) - localPosition) / frameTime;
        }

        /// <summary>
        /// Returns the position of the joint in world space after applying FK using the pose
        /// </summary>
        public static float3 GetWorldPosition(Skeleton skeleton, PoseVector pose, Joint joint)
        {
            Matrix4x4 localToWorld = Matrix4x4.identity;
            while (joint.Index != 0) // while not root
            {
                localToWorld = Matrix4x4.TRS(pose.JointLocalPositions[joint.Index], pose.JointLocalRotations[joint.Index], new float3(1.0f, 1.0f, 1.0f)) * localToWorld;
                joint = skeleton.GetParent(joint);
            }
            localToWorld = Matrix4x4.TRS(pose.JointLocalPositions[0], pose.JointLocalRotations[0], new float3(1.0f, 1.0f, 1.0f)) * localToWorld; // root
            return localToWorld.MultiplyPoint3x4(Vector3.zero);
        }

        /// <summary>
        /// Returns the rotation of the joint in world space after applying FK using the pose
        /// </summary>
        public static quaternion GetWorldRotation(Skeleton skeleton, PoseVector pose, Joint joint)
        {
            quaternion worldRot = quaternion.identity;
            while (joint.Index != 0) // while not root
            {
                worldRot = math.mul(pose.JointLocalRotations[joint.Index], worldRot);
                joint = skeleton.GetParent(joint);
            }
            worldRot = math.mul(pose.JointLocalRotations[0], worldRot); // root
            return worldRot;
        }

        /// <summary>
        /// Returns the position and forward vector of the character in world space using the pose vector simulation bone
        /// </summary>
        /// <param name="hipsForwardLocalVector">forward vector of the hips in world space when in bind pose</param>
        public static void GetWorldOriginCharacter(PoseVector poseVector, out float3 center, out float3 forward)
        {
            center = poseVector.JointLocalPositions[0]; // Simulation Bone World Position
            forward = math.mul(poseVector.JointLocalRotations[0], math.forward()); // Simulation Bone World Rotation
        }

        public static float3 GetLocalPositionFromCharacter(float3 worldPos, float3 centerCharacter, float3 forwardCharacter)
        {
            return math.mul(math.inverse(quaternion.LookRotation(forwardCharacter, math.up())), worldPos - centerCharacter);
        }

        public static float3 GetLocalDirectionFromCharacter(float3 worldDir, float3 forwardCharacter)
        {
            float3 localDir = math.mul(math.inverse(quaternion.LookRotation(forwardCharacter, math.up())), worldDir);
            return localDir;
        }

        public void Dispose()
        {
            if (Valid != null && Valid.IsCreated) Valid.Dispose();
            if (Features != null && Features.IsCreated) Features.Dispose();
        }
    }
}