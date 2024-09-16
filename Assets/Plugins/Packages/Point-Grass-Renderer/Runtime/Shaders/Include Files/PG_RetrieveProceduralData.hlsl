void RetrieveProceduralData_float(float3 In, out float3 Out,
	out float3 LocalPos, out float3 WorldPos,
	out float3 LocalNormal, out float3 WorldNormal,
	out float4 Color,
	out float4 ExtraData, out float2 MeshUV, out float BladeLength, out float RandomValue) {
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	MeshPoint pnt = _MeshPoints[unity_InstanceID];

	LocalPos = pnt.position;
	WorldPos = mul(_ObjMatrix_L2W, float4(LocalPos, 1.0)).xyz;
	LocalNormal = pnt.normal;
	WorldNormal = mul((float3x3)_ObjMatrix_L2W, LocalNormal).xyz;
	Color = pnt.colour;

	ExtraData = pnt.extraData;
#else
	LocalPos = float3(0, 0, 0);
	WorldPos = TransformObjectToWorld(LocalPos);
	LocalNormal = float3(0, 1, 0);
	WorldNormal = TransformObjectToWorldDir(LocalNormal);
	Color = float4(1, 1, 1, 1);

	ExtraData = float4(0, 0, 1, 0);
#endif
	WorldNormal = normalize(WorldNormal);

	MeshUV = ExtraData.xy;
	BladeLength = ExtraData.z;
	RandomValue = ExtraData.w;

	Out = In;
}

void TransformWorldToObjectPosition_float(float3 In, out float3 Out) {
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	Out = mul(_ObjMatrix_W2L, float4(In, 1.0)).xyz;
#else
	Out = TransformWorldToObject(In);
#endif
}

void TransformWorldToObjectNormal_float(float3 In, out float3 Out) {
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	Out = mul((float3x3)_ObjMatrix_W2L, In);
#else
	Out = TransformWorldToObjectDir(In);
#endif
	Out = normalize(Out);
}