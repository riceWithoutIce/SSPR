Shader "Hidden/SSPR"
{
    Properties
    {

    }
    SubShader
    {
        ZTest Always Cull Off ZWrite Off

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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _gHash;
            float _SizeX, _SizeY;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 size = float2(_SizeX, _SizeY);
                // uint hash = tex2D(_gHash, i.uv * size);

                // #if UNITY_UV_STARTS_AT_TOP
                //     if (hash == 0xffffffff)
                //     {
                // #else
                //     if (hash == 0)
                //     {
                // #endif
                //         return 0;
                //     }

                // float x = (hash & 0xffff) / _SizeX;
                // float y = (hash >> 16) / _SizeY;

                float2 uv = tex2D(_gHash, i.uv);
                // uv /= size;

                return fixed4(uv, 0, 0);

                fixed4 col = tex2D(_MainTex, uv);
                return col;
            }
            ENDCG
        }
    }
    
    Fallback Off
}
