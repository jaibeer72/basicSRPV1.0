﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Texture Splatting"
{
	Properties{

		_MainTex("Texture", 2D) = "white" {}
		[NoScaleOffset] _Texture1("Texture 1", 2D) = "white" {}
		[NoScaleOffset] _Texture2("Texture 2", 2D) = "white" {}
		[NoScaleOffset] _Texture3("Texture 3", 2D) = "white" {}
		[NoScaleOffset] _Texture4("Texture 4", 2D) = "white" {}
	}
	SubShader{
		Pass{
			CGPROGRAM
			#pragma vertex MyVertexProgram
			#pragma fragment MyFragmentProgram

			#include "UnityCG.cginc"
			//Vatiables 
			uniform  sampler2D _MainTex;
			//This is for tileing and offset
			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			CBUFFER_END

			sampler2D _Texture1, _Texture2, _Texture3, _Texture4;
			
			struct Interpolators // declared as i 
			{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 uvSplat : TEXCOORD1;
			};

			struct VertexData // declared as v 
			{
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
			};

			//Vertex Shader Function 
			Interpolators MyVertexProgram(VertexData v)
			{
				Interpolators i;
				i.position = UnityObjectToClipPos(v.position);
				i.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
				i.uvSplat = v.uv;
				return i; 
			}

			//Fragment Shader Function.
			float4 MyFragmentProgram(Interpolators i ) : SV_TARGET
			{
				float4 splat = tex2D(_MainTex, i.uvSplat);
				return tex2D(_Texture1, i.uv) * splat.r +
					tex2D(_Texture2, i.uv) * splat.g +
					tex2D(_Texture3, i.uv) * splat.b +
					tex2D(_Texture4, i.uv) * (1 - splat.r - splat.g - splat.b);
			}	

			ENDCG
		}
	}
}
