void CalculateBladeVertexOffset_float(float3 ObjectPosition, float3 ViewDirection, float3 BladeNormal, float BladeHeight, float BladeWidth, float BladeLength, out float3 VertexOffset) {
	BladeNormal = normalize(BladeNormal);

	float3 right = normalize(cross(BladeNormal, ViewDirection));
	float3 forward = cross(BladeNormal, right);

	float3 radialOffset = (right * ObjectPosition.x + forward * ObjectPosition.z) * BladeWidth;
	float3 verticalOffset = BladeNormal * ObjectPosition.y * BladeLength * BladeHeight;

	VertexOffset = radialOffset + verticalOffset;
}

void CalculateBladeVertexOffset_half(half3 ObjectPosition, half3 ViewDirection, half3 BladeNormal, half BladeHeight, half BladeWidth, half BladeLength, out half3 VertexOffset) {
	BladeNormal = normalize(BladeNormal);

	half3 right = normalize(cross(BladeNormal, ViewDirection));
	half3 forward = cross(BladeNormal, right);

	half3 radialOffset = (right * ObjectPosition.x + forward * ObjectPosition.z) * BladeWidth;
	half3 verticalOffset = BladeNormal * ObjectPosition.y * BladeLength * BladeHeight;

	VertexOffset = radialOffset + verticalOffset;
}