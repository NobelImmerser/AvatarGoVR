using Nuro.VRWeb.Core.Avatar;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AvatarGoVR
{
    using IkInfo = IAvatarIK.IkInfo;

    public class TestIAvatarIK : MonoBehaviour
    {
        public Transform Head;
        public Transform Hips;
        public Transform LeftHand;
        public Transform RightHand;
        public Transform LeftFoot;
        public Transform RightFoot;

        private IAvatarIK m_AvatarIK;

        private void Awake()
        {
            m_AvatarIK = GetComponent<IAvatarIK>();
        }

        private void OnEnable()
        {
            m_AvatarIK.BindAvatarToIK(gameObject, GetComponent<Animator>());
        }

        private void OnDisable()
        {
            m_AvatarIK.UnbindAvatarFromIK();
        }

        [ContextMenu("Calibrate()")]
        public void Calibrate()
        {
            IkInfo info = new()
            {
                HeadPosition = Head != null ? Head.position : Vector3.zero,
                HeadRotation = Head != null ? Head.rotation : Quaternion.identity,
                HipsPosition = Hips != null ? Hips.position : Vector3.zero,
                HipsRotation = Hips != null ? Hips.rotation : Quaternion.identity,
                LeftHandPosition = LeftHand != null ? LeftHand.position : Vector3.zero,
                LeftHandRotation = LeftHand != null ? LeftHand.rotation : Quaternion.identity,
                RightHandPosition = RightHand != null ? RightHand.position : Vector3.zero,
                RightHandRotation = RightHand != null ? RightHand.rotation : Quaternion.identity,
                LeftFootPosition = LeftFoot != null ? LeftFoot.position : Vector3.zero,
                LeftFootRotation = LeftFoot != null ? LeftFoot.rotation : Quaternion.identity,
                RightFootPosition = RightFoot != null ? RightFoot.position : Vector3.zero,
                RightFootRotation = RightFoot != null ? RightFoot.rotation : Quaternion.identity
            };

            m_AvatarIK.Calibrate(info);
        }

        private void Update()
        {
            if (m_AvatarIK == null)
                return;

            IkInfo info = new()
            {
                HeadPosition = Head != null ? Head.position : Vector3.zero,
                HeadRotation = Head != null ? Head.rotation : Quaternion.identity,
                HipsPosition = Hips != null ? Hips.position : Vector3.zero,
                HipsRotation = Hips != null ? Hips.rotation : Quaternion.identity,
                LeftHandPosition = LeftHand != null ? LeftHand.position : Vector3.zero,
                LeftHandRotation = LeftHand != null ? LeftHand.rotation : Quaternion.identity,
                RightHandPosition = RightHand != null ? RightHand.position : Vector3.zero,
                RightHandRotation = RightHand != null ? RightHand.rotation : Quaternion.identity,
                LeftFootPosition = LeftFoot != null ? LeftFoot.position : Vector3.zero,
                LeftFootRotation = LeftFoot != null ? LeftFoot.rotation : Quaternion.identity,
                RightFootPosition = RightFoot != null ? RightFoot.position : Vector3.zero,
                RightFootRotation = RightFoot != null ? RightFoot.rotation : Quaternion.identity
            };

            m_AvatarIK.OnUpdateAvatarIK(transform, info);
        }
    }
}