import { ModelType } from "skinview-utils";
import { Group, Mesh, Object3D, Texture } from "three";
/**
 * Notice that innerLayer and outerLayer may NOT be the direct children of the Group.
 */
export declare class BodyPart extends Group {
    readonly innerLayer: Object3D;
    readonly outerLayer: Object3D;
    constructor(innerLayer: Object3D, outerLayer: Object3D);
}
export declare class SkinObject extends Group {
    readonly head: BodyPart;
    readonly body: BodyPart;
    readonly rightArm: BodyPart;
    readonly leftArm: BodyPart;
    readonly rightLeg: BodyPart;
    readonly leftLeg: BodyPart;
    private modelListeners;
    private slim;
    constructor(texture: Texture);
    get modelType(): ModelType;
    set modelType(value: ModelType);
    private getBodyParts;
    setInnerLayerVisible(value: boolean): void;
    setOuterLayerVisible(value: boolean): void;
}
export declare class CapeObject extends Group {
    readonly cape: Mesh;
    constructor(texture: Texture);
}
export declare class ElytraObject extends Group {
    readonly leftWing: Group;
    readonly rightWing: Group;
    constructor(texture: Texture);
    /**
     * Mirrors the position & rotation of left wing,
     * and apply them to the right wing.
     */
    updateRightWing(): void;
}
export declare type BackEquipment = "cape" | "elytra";
export declare class PlayerObject extends Group {
    readonly skin: SkinObject;
    readonly cape: CapeObject;
    readonly elytra: ElytraObject;
    constructor(skinTexture: Texture, capeTexture: Texture);
    get backEquipment(): BackEquipment | null;
    set backEquipment(value: BackEquipment | null);
}
