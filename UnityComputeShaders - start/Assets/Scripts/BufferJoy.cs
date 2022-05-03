// <copyright file="BufferJoy.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace BufferJoy
{
    using UnityEngine;

    /// <summary>
    /// An implementation.
    /// </summary>
    public class BufferJoy : MonoBehaviour
    {
        private readonly int count = 10;

        [SerializeField]
        private ComputeShader shader;

        [SerializeField]
        private int texResolution = 1024;

        private Renderer rend;
        private RenderTexture outputTexture;

        private int circlesHandle;

        private int clearHandle;
        private Color clearColor;
        private Color circleColor;

        private Circle[] circleData;
        private ComputeBuffer buffer;

        // Use this for initialization
        private void Start()
        {
            this.outputTexture = new RenderTexture(this.texResolution, this.texResolution, 0);
            this.outputTexture.enableRandomWrite = true;
            this.outputTexture.Create();

            this.rend = this.GetComponent<Renderer>();
            this.rend.enabled = true;

            this.InitData();

            this.InitShader();
        }

        private void InitData()
        {
            this.circlesHandle = this.shader.FindKernel("Circles");

            // underscore(_) : I only care about the first out parameter
            this.shader.GetKernelThreadGroupSizes(this.circlesHandle, out uint threadGroupSizeX, out _, out _);

            // get the number of circle
            int total = (int)threadGroupSizeX * this.count;
            this.circleData = new Circle[total];

            const float speed = 100;
            const float halfSpeed = speed * 0.5f;
            const float minRadius = 10.0f;
            const float maxRadius = 30.0f;
            const float radiusRange = maxRadius - minRadius;

            // init random values for all circle
            for (int i = 0; i < total; i++)
            {
                Circle circle = this.circleData[i];
                circle.Origin.x = Random.value * this.texResolution;
                circle.Origin.y = Random.value * this.texResolution;
                circle.Velocity.x = (Random.value * speed) - halfSpeed;
                circle.Velocity.y = (Random.value * speed) - halfSpeed;
                circle.Radius = (Random.value * radiusRange) + minRadius;
                this.circleData[i] = circle;
            }
        }

        private void InitShader()
        {
            this.clearHandle = this.shader.FindKernel("Clear");

            this.shader.SetVector("clearColor", this.clearColor);
            this.shader.SetVector("circleColor", this.circleColor);
            this.shader.SetInt("texResolution", this.texResolution);

            this.shader.SetTexture(this.clearHandle, "Result", this.outputTexture);
            this.shader.SetTexture(this.circlesHandle, "Result", this.outputTexture);

            // float2 + float2 + float in circle
            const int stride = (2 + 2 + 1) * sizeof(float);
            this.buffer = new ComputeBuffer(this.circleData.Length, stride);
            this.buffer.SetData(this.circleData);
            this.shader.SetBuffer(this.circlesHandle, "circlesBuffer", this.buffer);

            this.rend.material.SetTexture("_MainTex", this.outputTexture);
        }

        private void DispatchKernels(int count)
        {
            this.shader.Dispatch(this.clearHandle, this.texResolution / 8, this.texResolution / 8, 1);
            this.shader.SetFloat("time", Time.time);
            this.shader.Dispatch(this.circlesHandle, count, 1, 1);
        }

        private void Update()
        {
            this.DispatchKernels(this.count);
        }

        private void OnApplicationQuit()
        {
            this.buffer.Release();
        }

        private struct Circle
        {
            public Vector2 Origin;
            public Vector2 Velocity;
            public float Radius;
        }
    }
}
