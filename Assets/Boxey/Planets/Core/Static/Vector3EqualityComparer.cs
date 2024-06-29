using System.Collections.Generic;
using UnityEngine;

namespace Boxey.Planets.Core.Static {
    public class Vector3EqualityComparer : IEqualityComparer<Vector3> {
        private readonly float _threshold;
        public Vector3EqualityComparer(float threshold) {
            _threshold = threshold;
        }
        public bool Equals(Vector3 v1, Vector3 v2) {
            return Vector3.SqrMagnitude(v1 - v2) < _threshold * _threshold;
        }
        public int GetHashCode(Vector3 v) {
            return v.GetHashCode();
        }
    }
}