using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EnemyVision))]
public class EnemyVisionEditor : Editor {
    private void OnSceneGUI() {
        EnemyVision ev = (EnemyVision) target;
        Handles.color = ev.isAlerted ? Color.red : Color.white;
        Handles.DrawSolidArc(ev.transform.position, Vector3.up, ev.DirFromAngle(-ev.visionAngle/2, false), ev.visionAngle, ev.visionRange);
    }
}
