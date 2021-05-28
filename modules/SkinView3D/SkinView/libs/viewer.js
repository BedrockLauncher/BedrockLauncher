import { inferModelType, isTextureSource, loadCapeToCanvas, loadImage, loadSkinToCanvas } from "skinview-utils";
import { NearestFilter, PerspectiveCamera, Scene, Texture, Vector2, WebGLRenderer } from "three";
import { RootAnimation } from "./animation.js";
import { PlayerObject } from "./model.js";
export class SkinViewer {
    constructor(options = {}) {
        this.animations = new RootAnimation();
        this._disposed = false;
        this._renderPaused = false;
        this.canvas = options.canvas === undefined ? document.createElement("canvas") : options.canvas;
        // texture
        this.skinCanvas = document.createElement("canvas");
        this.skinTexture = new Texture(this.skinCanvas);
        this.skinTexture.magFilter = NearestFilter;
        this.skinTexture.minFilter = NearestFilter;
        this.capeCanvas = document.createElement("canvas");
        this.capeTexture = new Texture(this.capeCanvas);
        this.capeTexture.magFilter = NearestFilter;
        this.capeTexture.minFilter = NearestFilter;
        this.scene = new Scene();
        // Use smaller fov to avoid distortion
        this.camera = new PerspectiveCamera(40);
        this.camera.position.y = -8;
        this.camera.position.z = 60;
        this.renderer = new WebGLRenderer({
            canvas: this.canvas,
            alpha: options.alpha !== false,
            preserveDrawingBuffer: options.preserveDrawingBuffer === true // default: false
        });
        this.renderer.setPixelRatio(window.devicePixelRatio);
        this.playerObject = new PlayerObject(this.skinTexture, this.capeTexture);
        this.playerObject.name = "player";
        this.playerObject.skin.visible = false;
        this.playerObject.cape.visible = false;
        this.scene.add(this.playerObject);
        if (options.skin !== undefined) {
            this.loadSkin(options.skin);
        }
        if (options.cape !== undefined) {
            this.loadCape(options.cape);
        }
        if (options.width !== undefined) {
            this.width = options.width;
        }
        if (options.height !== undefined) {
            this.height = options.height;
        }
        if (options.renderPaused === true) {
            this._renderPaused = true;
        }
        else {
            window.requestAnimationFrame(() => this.draw());
        }
    }
    loadSkin(source, model = "auto-detect", options = {}) {
        if (source === null) {
            this.resetSkin();
        }
        else if (isTextureSource(source)) {
            loadSkinToCanvas(this.skinCanvas, source);
            const actualModel = model === "auto-detect" ? inferModelType(this.skinCanvas) : model;
            this.skinTexture.needsUpdate = true;
            this.playerObject.skin.modelType = actualModel;
            if (options.makeVisible !== false) {
                this.playerObject.skin.visible = true;
            }
        }
        else {
            return loadImage(source).then(image => this.loadSkin(image, model, options));
        }
    }
    resetSkin() {
        this.playerObject.skin.visible = false;
    }
    loadCape(source, options = {}) {
        if (source === null) {
            this.resetCape();
        }
        else if (isTextureSource(source)) {
            loadCapeToCanvas(this.capeCanvas, source);
            this.capeTexture.needsUpdate = true;
            if (options.makeVisible !== false) {
                this.playerObject.backEquipment = options.backEquipment === undefined ? "cape" : options.backEquipment;
            }
        }
        else {
            return loadImage(source).then(image => this.loadCape(image, options));
        }
    }
    resetCape() {
        this.playerObject.backEquipment = null;
    }
    draw() {
        if (this.disposed || this._renderPaused) {
            return;
        }
        this.animations.runAnimationLoop(this.playerObject);
        this.render();
        window.requestAnimationFrame(() => this.draw());
    }
    /**
    * Renders the scene to the canvas.
    * This method does not change the animation progress.
    */
    render() {
        this.renderer.render(this.scene, this.camera);
    }
    setSize(width, height) {
        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(width, height);
    }
    dispose() {
        this._disposed = true;
        this.renderer.dispose();
        this.skinTexture.dispose();
        this.capeTexture.dispose();
    }
    get disposed() {
        return this._disposed;
    }
    /**
     * Whether rendering and animations are paused.
     * Setting this property to true will stop both rendering and animation loops.
     * Setting it back to false will resume them.
     */
    get renderPaused() {
        return this._renderPaused;
    }
    set renderPaused(value) {
        const toResume = !this.disposed && !value && this._renderPaused;
        this._renderPaused = value;
        if (toResume) {
            window.requestAnimationFrame(() => this.draw());
        }
    }
    get width() {
        return this.renderer.getSize(new Vector2()).width;
    }
    set width(newWidth) {
        this.setSize(newWidth, this.height);
    }
    get height() {
        return this.renderer.getSize(new Vector2()).height;
    }
    set height(newHeight) {
        this.setSize(this.width, newHeight);
    }
}
//# sourceMappingURL=viewer.js.map