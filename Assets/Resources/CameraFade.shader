Shader "Game Institute/Camera Fade"
{
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
	}
	 
	SubShader 
	{
	 	Pass
	 	{
	 		ZTest Always Cull Off ZWrite Off
	 		Blend SrcAlpha OneMinusSrcAlpha 
	 		ColorMask RGB 
	 		Color [_Color]
		}
	}
	
}
