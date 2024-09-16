void ShaderGraphFunction_float(float3 In, out float3 Out) {
	Out = In;
}

void ShaderGraphFunction_half(half3 In, out half3 Out) {
	Out = In;
}

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	struct MeshPoint{
		float3 position;
		float3 normal;
		float4 colour;
		float4 extraData;
	};
	
	StructuredBuffer<MeshPoint> _MeshPoints;

	float4x4 _ObjMatrix_L2W;
	float4x4 _ObjMatrix_W2L;
#endif

void ConfigureProcedural () {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	// Do nothing
	#endif
}