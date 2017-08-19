/*
Copyright (c) 2015 Kyle Halladay

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

Shader "KH/Dissolve/Dissolve Simple" 
{
	Properties 
	{
		_MainTex ("Diffuse (RGBA)", 2D) = "white"{}
		_NoiseTex ("Burn Map (RGB)", 2D) = "black"{}
		_DissolveValue ("Value", Range(0,1)) = 1.0
	}
	SubShader 
	{
		Tags {"Queue" = "Transparent"}
		
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			Cull back
			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D _MainTex;
			sampler2D _NoiseTex;
			fixed _DissolveValue;
			
			struct vIN
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};
			
			struct vOUT
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
			
			vOUT vert(vIN v)
			{
				vOUT o;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.texcoord;
				return o;
			}
			
			fixed4 frag(vOUT i) : COLOR
			{
				fixed4 mainTex = tex2D(_MainTex, i.uv);
				fixed noiseVal = tex2D(_NoiseTex, i.uv).r;
				mainTex.a *= floor(_DissolveValue + noiseVal.r);
				//mainTex.a *= floor(_DissolveValue + min(0.99, noiseVal.r));
				return mainTex;
			}
			
			ENDCG
		}
	} 
}
