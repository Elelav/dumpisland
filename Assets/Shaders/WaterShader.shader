Shader "Custom/AnimatedWater"
{
    Properties
    {
        _DeepColor ("Deep Water Color", Color) = (0.1, 0.4, 0.7, 1)
        _ShallowColor ("Shallow Water Color", Color) = (0.3, 0.6, 0.9, 1)
        _FoamColor ("Foam Color", Color) = (0.8, 0.9, 1, 1)
        
        _WaveSpeed ("Wave Speed", Float) = 0.5
        _WaveScale ("Wave Scale", Float) = 5.0
        _WaveStrength ("Wave Strength", Float) = 0.3
        
        _FoamThreshold ("Foam Threshold", Range(0, 1)) = 0.7
        _FoamSoftness ("Foam Softness", Range(0, 1)) = 0.1
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
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
            
            float4 _DeepColor;
            float4 _ShallowColor;
            float4 _FoamColor;
            float _WaveSpeed;
            float _WaveScale;
            float _WaveStrength;
            float _FoamThreshold;
            float _FoamSoftness;
            
            // Простой шум (псевдо-случайная функция)
            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            
            // Шум на основе хеша
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // сглаживание
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Фрактальный шум (несколько октав)
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y * _WaveSpeed;
                
                // Две волны с разными направлениями
                float2 wave1 = uv * _WaveScale + float2(time * 0.5, time * 0.3);
                float2 wave2 = uv * _WaveScale * 1.3 + float2(-time * 0.3, time * 0.4);
                
                // Генерируем шум для волн
                float noise1 = fbm(wave1);
                float noise2 = fbm(wave2);
                
                // Комбинируем шумы
                float combinedNoise = (noise1 + noise2 * 0.5) / 1.5;
                
                // Определяем цвет воды
                fixed4 waterColor = lerp(_DeepColor, _ShallowColor, combinedNoise);
                
                // Добавляем пену на гребнях волн
                float foamMask = smoothstep(_FoamThreshold - _FoamSoftness, _FoamThreshold, combinedNoise);
                waterColor = lerp(waterColor, _FoamColor, foamMask);
                
                return waterColor;
            }
            ENDCG
        }
    }
}