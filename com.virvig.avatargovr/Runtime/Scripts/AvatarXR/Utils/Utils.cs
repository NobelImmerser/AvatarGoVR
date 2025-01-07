using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AvatarGoVR
{

    public static class Utils
    {
        public static void SetPlayerPrefsPose(string name, Vector3 pos, Quaternion rot)
        {
            PlayerPrefs.SetFloat(name + "_pos_x", pos.x);
            PlayerPrefs.SetFloat(name + "_pos_y", pos.y);
            PlayerPrefs.SetFloat(name + "_pos_z", pos.z);
            PlayerPrefs.SetFloat(name + "_rot_x", rot.x);
            PlayerPrefs.SetFloat(name + "_rot_y", rot.y);
            PlayerPrefs.SetFloat(name + "_rot_z", rot.z);
            PlayerPrefs.SetFloat(name + "_rot_w", rot.w);
        }

        public static (Vector3, Quaternion) GetPlayerPrefsPose(string name)
        {
            Vector3 pos = new Vector3(
                PlayerPrefs.GetFloat(name + "_pos_x"),
                PlayerPrefs.GetFloat(name + "_pos_y"),
                PlayerPrefs.GetFloat(name + "_pos_z"));
            Quaternion rot = new Quaternion(
                PlayerPrefs.GetFloat(name + "_rot_x"),
                PlayerPrefs.GetFloat(name + "_rot_y"),
                PlayerPrefs.GetFloat(name + "_rot_z"),
                PlayerPrefs.GetFloat(name + "_rot_w"));
            return (pos, rot);
        }
    }
}