Shader "Moonlit/ScholarToon" {
 Properties { _MainTex("Color",2D)="white"{} _BumpMap("Normal",2D)="bump"{} _Color("Tint",Color)=(1,1,1,1) _DeskMask("Desk mask",Float)=1 }
 SubShader { Tags{"RenderType"="Opaque"} LOD 250
 CGPROGRAM
 #pragma surface surf Toon fullforwardshadows vertex:vert addshadow
 #pragma target 3.0
 sampler2D _MainTex,_BumpMap;fixed4 _Color;float _DeskMask;
 struct Input {float2 uv_MainTex;float3 worldPos;float4 color:COLOR;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.color=v.color;}
 half4 LightingToon(SurfaceOutput s,half3 lightDir,half3 viewDir,half atten){half n=dot(s.Normal,lightDir);half bands=smoothstep(-.08,.18,n)*.55+smoothstep(.45,.65,n)*.35+.1;half rim=pow(1-saturate(dot(viewDir,s.Normal)),3)*.12;return half4(s.Albedo*_LightColor0.rgb*bands*atten+rim*_LightColor0.rgb*atten,s.Alpha);}
 void surf(Input i,inout SurfaceOutput o){if(_DeskMask>.5&&i.color.r>.7&&abs(i.worldPos.x)<.575&&i.worldPos.z>.36&&i.worldPos.z<1.0&&i.worldPos.y>.688)clip(-1);fixed4 c=tex2D(_MainTex,i.uv_MainTex)*_Color;float gray=dot(c.rgb,float3(.299,.587,.114));o.Albedo=lerp(gray.xxx,c.rgb,.65);o.Normal=UnpackNormal(tex2D(_BumpMap,i.uv_MainTex));o.Alpha=1;}
 ENDCG
 }
 Fallback "Diffuse"
}
