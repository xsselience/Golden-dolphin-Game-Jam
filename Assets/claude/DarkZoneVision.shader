// 圆形视野遮罩 Shader：用于 DarkZone 的「探索地下室」模式
// 在玩家周围挖一个圆形透明洞，洞外保持黑幕，洞内露出场景
Shader "Custom/DarkZoneVision"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0,0,0,1)
        _PlayerPos ("Player World Position", Vector) = (0,0,0,0)
        _Radius ("Vision Radius", Float) = 3
        _Feather ("Vision Edge Feather", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _PlayerPos;   // 玩家世界坐标 (x, y)
            float _Radius;       // 视野半径（世界单位）
            float _Feather;      // 视野边缘羽化宽度

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // 计算当前顶点的世界坐标，用于和玩家位置比较距离
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                // 当前像素到玩家的平面距离
                float dist = distance(i.worldPos.xy, _PlayerPos.xy);

                // 视野洞：半径内 alpha=0（透明露出场景），半径外 alpha=1（黑幕），中间平滑过渡
                float e = min(_Feather, _Radius);
                float holeAlpha = smoothstep(_Radius - e, _Radius, dist);
                col.a *= holeAlpha;

                return col;
            }
            ENDCG
        }
    }
}
