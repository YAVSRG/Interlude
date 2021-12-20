﻿#version 330 core
layout (location = 0) in vec2 vPos;
layout (location = 1) in vec2 vUv;
layout (location = 2) in vec4 vColor;

uniform mat4 uProjection;

out vec2 fUv;
out vec4 fColor;

void main()
{
    gl_Position = uProjection * vec4(vPos, 0.0, 1.0);
    fUv = vUv;
    fColor = vColor;
}