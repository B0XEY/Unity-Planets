void AlignedVectorMatrix_float(float3 VecA, float3 VecB, out float3x3 RotationMatrix) {
	float c = dot(VecA, VecB);

	// Countermeasure to help prevent undefined errors when the two vectors are exactly opposite
	if (c <= -1) {
		float3x3 backupMatrix = {
			1.0, 0.0, 0.0,
			0.0, -1.0, 0.0,
			0.0, 0.0, -1.0
		};
		RotationMatrix = backupMatrix;
	}
	else {
		float3 v = cross(VecA, VecB);
		float s = length(v);

		float3x3 vMatrix = {
			0, -v.z, v.y,
			v.z, 0, -v.x,
			-v.y, v.x, 0
		};

		float a = 1 / (1 + c);

		float3x3 identity = {
			1, 0, 0,
			0, 1, 0,
			0, 0, 1
		};

		RotationMatrix = identity + vMatrix + mul(vMatrix, vMatrix) * a;
	}
}