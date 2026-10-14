#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

static const float PI = 3.14159265f;

Texture2D SpriteTexture;
float start = 0;
float end = 1;
float dir = 1;

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};


struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float2 ToPolar(float2 uv, float2 center = float2(0.5, 0.5))
{
    float2 dir = uv - center;
    float radius = length(dir); 
    float angle = atan2(dir.y, dir.x); 
    float angleNormalized = (angle / 2 / PI) + 0.5;

    return float2(angleNormalized, radius);
}

float4 RoundMask(float2 uv, float offset, float fraction, float dir) {
    float ang = (ToPolar(uv).x - offset) * dir;
  return fmod(ang + 1, 1) <= fraction;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
  float4 col = input.Color;
  col.a = col.a * RoundMask(input.TextureCoordinates, start, end, dir);
    return tex2D(SpriteTextureSampler,input.TextureCoordinates) * col;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
