﻿using System;

namespace UnityEngine.Rendering.Universal {
    public class LoadColorPass : ScriptableRenderPass {
        const string k_Tag = "Copy Color To RenderTexture";

        private Material BlitFlipMaterial { get; set; }
        private RenderTargetHandle source { get; set; }
        private RenderTargetHandle destination { get; set; }
        private RenderTargetHandle intermediate { get; set; }
        private RenderTextureDescriptor descriptor;

        private int UVTransformID;

        /// <summary>
        /// Create the CopyColorPass
        /// </summary>
        public LoadColorPass(RenderPassEvent evt, Material blitFlipMaterial) {
            renderPassEvent = evt;
            BlitFlipMaterial = blitFlipMaterial;
            UVTransformID = Shader.PropertyToID("_UVTransform");
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle intermediate, RenderTargetHandle destination) {
            this.source = RenderTargetHandle.CameraTarget;
            this.destination = destination;
            this.intermediate = intermediate;
            descriptor = baseDescriptor;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            var cmd = CommandBufferPool.Get(k_Tag);

            var colorRT = source.Identifier();
            var opaqueColorRT = destination.Identifier();
            if (SystemInfo.graphicsUVStartsAtTop) {
                var inter = intermediate.Identifier();

                var colorDescriptor = descriptor;
                colorDescriptor.depthBufferBits = 0;
                colorDescriptor.sRGB = true;
                cmd.GetTemporaryRT(intermediate.id, descriptor, FilterMode.Point);

                cmd.SetGlobalVector(UVTransformID, new Vector4(1.0f, -1.0f, 0.0f, 1.0f));
                cmd.Blit(colorRT, inter);
                cmd.SetGlobalTexture("_MainTex", inter);
                cmd.Blit(inter, opaqueColorRT, BlitFlipMaterial);
            }
            else {
                cmd.Blit(colorRT, opaqueColorRT);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd) {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            destination = RenderTargetHandle.CameraTarget;
            cmd.ReleaseTemporaryRT(intermediate.id);
        }
    }
}
