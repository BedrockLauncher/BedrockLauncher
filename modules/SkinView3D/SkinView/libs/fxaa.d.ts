import { EffectComposer } from "three/examples/jsm/postprocessing/EffectComposer.js";
import { RenderPass } from "three/examples/jsm/postprocessing/RenderPass.js";
import { ShaderPass } from "three/examples/jsm/postprocessing/ShaderPass.js";
import { SkinViewer, SkinViewerOptions } from "./viewer.js";
export declare class FXAASkinViewer extends SkinViewer {
    readonly composer: EffectComposer;
    readonly renderPass: RenderPass;
    readonly fxaaPass: ShaderPass;
    /**
     * Note: FXAA doesn't work well with transparent backgrounds.
     * It's recommended to use an opaque background and set `options.alpha` to false.
     */
    constructor(options?: SkinViewerOptions);
    setSize(width: number, height: number): void;
    private updateComposerSize;
    render(): void;
    dispose(): void;
}
