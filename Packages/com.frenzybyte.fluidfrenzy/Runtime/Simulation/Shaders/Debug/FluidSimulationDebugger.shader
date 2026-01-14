Shader "Hidden/FluidFrenzy/Debug/FluidSimulationDebugger"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            Texture2D _MainTex;
			SamplerState point_clamp_sampler;


			float4 _Levels;
			float4 _ColorSelection;

			# define INPUTLEVELS_BLACK _Levels.x
			# define INPUTLEVELS_WHITE _Levels.y
			# define INPUTLEVELS_GAMMA _Levels.z

			float4 ApplyLevels(float4 x)
			{
				float4 s = sign(x);
				float inBlack = INPUTLEVELS_BLACK;
				float inGamma = INPUTLEVELS_GAMMA;
				float inWhite = INPUTLEVELS_WHITE;
				return saturate(pow(max(abs(x) - inBlack,0) / (inWhite - inBlack), inGamma)) * s; 
			}


            float4 frag (v2f i) : SV_Target
            {
                float4 col = _MainTex.Sample(point_clamp_sampler, i.uv);

				col = ApplyLevels((col)) ;

				col *= _ColorSelection;

				if(dot(_ColorSelection, float4(1,1,1,1)) == 1)
				{
					col = dot(col, _ColorSelection).rrrr;
				}

                return float4(col.rgb, 1);
            }
            ENDCG
        }
    }
}
