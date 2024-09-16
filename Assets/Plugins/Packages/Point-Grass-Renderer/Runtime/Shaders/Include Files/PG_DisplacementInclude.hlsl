struct DisplacementObject {
	float3 position;
	float radius;
	float strength;
};

uint _DisplacementCount;
StructuredBuffer<DisplacementObject> _DisplacementObjects; 



void CalculatePosition_float(float3 bladePos, DisplacementObject obj, out float3 normal, out float str, out float dist) {
	float3 offset = bladePos - obj.position;

	dist = length(offset);
	str = smoothstep(obj.radius, 0, dist) * obj.strength;
	normal = normalize(offset) * str;
}
void CalculatePosition_half(half3 bladePos, DisplacementObject obj, out half3 normal, out half str, out half dist) {
	half3 offset = bladePos - obj.position;

	dist = length(offset);
	str = smoothstep(obj.radius, 0, dist) * obj.strength;
	normal = normalize(offset) * str;
}

void CalculateDisplacement_float(float3 WorldPosition, out float3 Normal, out float Strength, out float Distance) {
	Normal = float3(0, 0, 0);
	Strength = 0;
	Distance = 100000; // Also acts as a clamp

	float3 norm;
	float str;
	float dist;
	for (uint i = 0; i < _DisplacementCount; i++) {
		CalculatePosition_float(WorldPosition, _DisplacementObjects[i], norm, str, dist);
		Normal += norm;
		Strength = max(Strength, str);
		Distance = min(Distance, dist);
	}
}
void CalculateDisplacement_half(half3 WorldPosition, out half3 Normal, out half Strength, out half Distance) {
	Normal = half3(0, 0, 0);
	Strength = 0;
	Distance = 100000; // Also acts as a clamp

	half3 norm;
	half str;
	half dist;
	for (uint i = 0; i < _DisplacementCount; i++) {
		CalculatePosition_half(WorldPosition, _DisplacementObjects[i], norm, str, dist);
		Normal += norm;
		Strength = max(Strength, str);
		Distance = min(Distance, dist);
	}
}

void RotateAboutAxisMatrix_Radians_float(float3 Axis, float Rotation, out float3x3 Out) {
	float s = sin(Rotation);
	float c = cos(Rotation);
	float one_minus_c = 1.0 - c;

	Axis = normalize(Axis);
	float3x3 output = {
		one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
		one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
		one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
	};

	Out = output;
}
void RotateAboutAxisMatrix_Radians_half(half3 Axis, half Rotation, out half3x3 Out) {
	half s = sin(Rotation);
	half c = cos(Rotation);
	half one_minus_c = 1.0 - c;

	Axis = normalize(Axis);
	half3x3 output = {
		one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
		one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
		one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
	};

	Out = output;
}