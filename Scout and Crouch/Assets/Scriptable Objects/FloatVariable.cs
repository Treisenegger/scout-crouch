using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FloatVariable", menuName = "FloatVariable", order = 0)]
public class FloatVariable : ScriptableObject {
    [SerializeField] float value;

    public float Value { 
        get { return value; }
    }
}
