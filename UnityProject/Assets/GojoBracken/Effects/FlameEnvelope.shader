Shader "GojoBracken/FlameEnvelope"
{
    Properties { _Strength ("Simulated anger", Range(0,1)) = 1 _Intensity ("Aura strength", Range(0,3)) = 1 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Strength;
            float _Intensity;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float3 world : TEXCOORD0; float3 normal : TEXCOORD1; float2 uv : TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o;
                float h = v.uv.y;
                float a = v.uv.x * 6.283185;
                float time = _Time.y;
                float wave = sin(a * 3 + h * 8 - time * 3) * 0.055
                           + sin(a * 5 - h * 11 + time * 2) * 0.035;
                float taper = sin(h * 3.14159);
                v.vertex.xz *= 1 + wave * taper * (0.3 + _Strength * 2);
                v.vertex.x += sin(h * 5 - time * 2) * h * h * 0.25 * _Strength;
                v.vertex.y *= lerp(0.65, 1.0, _Strength);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float rim = pow(1 - abs(dot(normalize(i.normal), normalize(_WorldSpaceCameraPos - i.world))), 0.8);
                float angle = i.uv.x * 6.283185;
                float flow = 0.5 + 0.5 * sin(angle * 6 + sin(i.uv.y * 13 - _Time.y * 4) + i.uv.y * 9 - _Time.y * 3);
                float baseFade = smoothstep(0, 0.08, i.uv.y);
                float tipFade = 1 - smoothstep(0.82, 1, i.uv.y);
                float opacity = lerp(0.025, 0.75, _Strength * _Strength);
                float edgeFade = smoothstep(0, 0.3, 1 - rim);
                float alpha = edgeFade * opacity * (0.35 + rim * 0.65) * (0.4 + flow * 0.6) * baseFade * tipFade;
                float3 blue = float3(0.04, 0.22, 1);
                float3 white = float3(0.65, 0.95, 1);
                float3 color = lerp(white, blue, rim * 0.75) * (1.2 + _Strength * 0.7);
                return fixed4(color, saturate(alpha * _Intensity));
            }
            ENDCG
        }
    }
}
