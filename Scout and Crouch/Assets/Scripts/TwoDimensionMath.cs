using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TwoDimensionMath {

    public static Vector3 V3AtHeight(Vector3 _v, float _h) {
        return new Vector3(_v.x, _h, _v.y);
    }

    public static Vector3 V3AtZero(Vector3 _v) {
        return V3AtHeight(_v, 0f);
    }

    public static float V3ToV3Distance(Vector3 _v1, Vector3 _v2) {
        return Vector3.Distance(V3AtZero(_v1), V3AtZero(_v2));
    }

    public static Vector3 V3ToV3Direction(Vector3 _v1, Vector3 _v2) {
        return (V3AtZero(_v1) - V3AtZero(_v2)).normalized;
    }

    public static Vector2 V3ToV2(Vector3 _v) {
        return new Vector2(_v.x, _v.z);
    }

    public static Vector3 V2ToV3AtHeight(Vector2 _v, float _height) {
        return new Vector3(_v.x, _height, _v.y);
    }

    public static Vector3 V2ToV3AtZero(Vector2 _v) {
        return V2ToV3AtHeight(_v, 0f);
    }
}
