import { ModelType, RemoteImage, TextureSource } from "skinview-utils";
import { PerspectiveCamera, Scene, WebGLRenderer } from "three";
import { RootAnimation } from "./animation.js";
import { BackEquipment, PlayerObject } from "./model.js";
export interface LoadOptions {
    /**
     * Whether to make the object visible after the texture is loaded. Default is true.
     */
    makeVisible?: boolean;
}
export interface CapeLoadOptions extends LoadOptions {
    /**
     * The equipment (cape or elytra) to show, defaults to "cape".
     * If makeVisible is set to false, this option will have no effect.
     */
    backEquipment?: BackEquipment;
}
export interface SkinViewerOptions {
    width?: number;
    height?: number;
    skin?: RemoteImage | TextureSource;
    cape?: RemoteImage | TextureSource;
    /**
     * Whether the canvas contains an alpha buffer. Default is true.
     * This option can be turned off if you use an opaque background.
     */
    alpha?: boolean;
    /**
     * Render target.
     * A new canvas is created if this parameter is unspecified.
     */
    canvas?: HTMLCanvasElement;
    /**
     * Whether to preserve the buffers until manually cleared or overwritten. Default is false.
     */
    preserveDrawingBuffer?: boolean;
    /**
     * The initial value of `SkinViewer.renderPaused`. Default is false.
     * If this option is true, rendering and animation loops will not start.
     */
    renderPaused?: boolean;
}
export declare class SkinViewer {
    readonly canvas: HTMLCanvasElement;
    readonly scene: Scene;
    readonly camera: PerspectiveCamera;
    readonly renderer: WebGLRenderer;
    readonly playerObject: PlayerObject;
    readonly animations: RootAnimation;
    readonly skinCanvas: HTMLCanvasElement;
    readonly capeCanvas: HTMLCanvasElement;
    private readonly skinTexture;
    private readonly capeTexture;
    private _disposed;
    private _renderPaused;
    constructor(options?: SkinViewerOptions);
    loadSkin(empty: null): void;
    loadSkin<S extends TextureSource | RemoteImage>(source: S, model?: ModelType | "auto-detect", options?: LoadOptions): S extends TextureSource ? void : Promise<void>;
    resetSkin(): void;
    loadCape(empty: null): void;
    loadCape<S extends TextureSource | RemoteImage>(source: S, options?: CapeLoadOptions): S extends TextureSource ? void : Promise<void>;
    resetCape(): void;
    private draw;
    /**
    * Renders the scene to the canvas.
    * This method does not change the animation progress.
    */
    render(): void;
    setSize(width: number, height: number): void;
    dispose(): void;
    get disposed(): boolean;
    /**
     * Whether rendering and animations are paused.
     * Setting this property to true will stop both rendering and animation loops.
     * Setting it back to false will resume them.
     */
    get renderPaused(): boolean;
    set renderPaused(value: boolean);
    get width(): number;
    set width(newWidth: number);
    get height(): number;
    set height(newHeight: number);
}
