using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Planets.Core.Static {
    public static class Helpers {
        private static Camera _camera;
        public static Camera GetCamera {
            get {
                if (_camera == null) {
                    _camera = Camera.main;
                }
                return _camera;
            }
        }
        
        //Float Extensions
        public static float[] GenerateCurveArray(this AnimationCurve self, int samplePoints) {
            var returnArray = new float[samplePoints];
            for (var j = 0; j < samplePoints; j++) returnArray[j] = self.Evaluate(j / (float)samplePoints);
            return returnArray;
        }
        //Int Extensions
        public static int[] ToArray(this NativeList<int> input) {
            var array = new int[input.Length];
            for (var i = 0; i < input.Length; i++) array[i] = input[i];
            return array;
        }
        //Vector 2 Extensions
        public static Vector2[] ToArray(this NativeList<float2> input) {
            var array = new Vector2[input.Length];
            for (var i = 0; i < input.Length; i++) array[i] = new Vector2(input[i].x, input[i].y);
            return array;
        }
        //Vector3 Extensions
        public static Vector3[] ToArray(this NativeList<float3> input) {
            var array = new Vector3[input.Length];
            for (var i = 0; i < input.Length; i++) array[i] = new Vector3(input[i].x, input[i].y, input[i].z);
            return array;
        }
        public static float3[] ToArray(this Vector3[] input) {
            var array = new float3[input.Length];
            for (var i = 0; i < input.Length; i++) array[i] = new float3(input[i].x, input[i].y, input[i].z);
            return array;
        }
        //Compute Shader
        public static void ReleaseBuffers(params ComputeBuffer[] buffers) {
            foreach (var buffer in buffers) {
                buffer.Release();
            }
        }
    }
}

public class Singleton<T> : MonoBehaviour where T : Component {
    private static T Instance { get; set; }

    public void Awake() {
        if (Instance == null) {
            Instance = this as T;
        }else {
            Destroy(gameObject);
        }
    }
}

public class SingletonPersistent<T> : MonoBehaviour where T : Component {
    private static T Instance { get; set; }

    public void Awake() {
        if (Instance == null) {
            Instance = this as T;
            DontDestroyOnLoad(this);
        }else {
            Destroy(gameObject);
        }
    }
}