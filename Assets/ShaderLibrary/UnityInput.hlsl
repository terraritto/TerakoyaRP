#ifndef TERAKOYA_CUSTOM_INPUT_INCLUDED
#define TERAKOYA_CUSTOM_INPUT_INCLUDED

CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld; // オブジェクト空間 -> ワールド空間
float4x4 unity_WorldToObject; // ワールド空間 -> オブジェクト空間
float4 unity_LODFade;
real4 unity_WorldTransformParams;
CBUFFER_END

float4x4 unity_MatrixVP; // View行列とProjection行列をかけ合わせた行列
float4x4 unity_MatrixV; // ビュー行列
float4x4 unity_MatrixInvV; // ビュー行列(inv)
float4x4 unity_prev_MatrixM;
float4x4 unity_prev_MatrixIM;
float4x4 glstate_matrix_projection;

#endif
