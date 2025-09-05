#ifndef RUBIK_STICKER_CORE_INCLUDED
#define RUBIK_STICKER_CORE_INCLUDED

float3 HSV2RGB(float3 c){
    float4 K=float4(1.0,2.0/3.0,1.0/3.0,3.0);
    float3 p=abs(frac(c.xxx+K.xyz)*6.0-K.www);
    return c.z*lerp(K.xxx, saturate(p-K.xxx), c.y);
}

float GetHueBase(float idx, float Hu, float Hd, float Hl, float Hr, float Hf, float Hb){
    return (idx<0.5)?Hu:(idx<1.5)?Hd:(idx<2.5)?Hl:(idx<3.5)?Hr:(idx<4.5)?Hf:Hb;
}

void BuildBasis(float3 n, out float3 u, out float3 v){
    float3 any = (abs(n.y)<0.95)? float3(0,1,0) : float3(1,0,0);
    u = normalize(cross(any,n));
    v = normalize(cross(n,u));
}

float2 Rotate2(float2 p, float deg){
    float r=radians(deg); float c=cos(r), s=sin(r);
    return float2(c*p.x - s*p.y, s*p.x + c*p.y);
}
#endif
