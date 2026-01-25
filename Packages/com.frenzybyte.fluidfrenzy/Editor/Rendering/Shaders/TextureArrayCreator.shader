Shader "Hidden/FluidFrenzy/TextureArrayCreator"
{
    Properties
    {
        _MainTex ("Source Normal Map", 2D) = "bump" {}
    }
    SubShader
    {
		CGINCLUDE

            #include "UnityCG.cginc"

		    struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }
		ENDCG

        Pass
        {
            Cull Off ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;

            float4 frag (v2f i) : SV_Target
            {
                // Simply sample the source texture and return its value.
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }

        Pass
        {
            Cull Off ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;

            float4 frag (v2f i) : SV_Target
            {
                // Sample the packed normal map.
                half4 packedNormal = tex2D(_MainTex, i.uv);
                
                // Decode it.
                half3 unpackedNormal = UnpackNormal(packedNormal);
                
                // The unpacked normal is in the [-1, 1] range. Convert it to the [0, 1] range to store it in a standard texture.
                return float4(unpackedNormal * 0.5 + 0.5, 1);
            }
            ENDCG
        }
    }
}
