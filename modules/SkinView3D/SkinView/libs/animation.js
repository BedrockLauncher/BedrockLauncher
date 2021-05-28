import { Clock } from "three";
export function invokeAnimation(animation, player, time) {
    if (animation instanceof Function) {
        animation(player, time);
    }
    else {
        // must be IAnimation here
        animation.play(player, time);
    }
}
class AnimationWrapper {
    constructor(animation) {
        this.speed = 1.0;
        this.paused = false;
        this.progress = 0;
        this.lastTime = 0;
        this.started = false;
        this.toResetAndRemove = false;
        this.animation = animation;
    }
    play(player, time) {
        if (this.toResetAndRemove) {
            invokeAnimation(this.animation, player, 0);
            this.remove();
            return;
        }
        let delta;
        if (this.started) {
            delta = time - this.lastTime;
        }
        else {
            delta = 0;
            this.started = true;
        }
        this.lastTime = time;
        if (!this.paused) {
            this.progress += delta * this.speed;
        }
        invokeAnimation(this.animation, player, this.progress);
    }
    reset() {
        this.progress = 0;
    }
    remove() {
        // stub get's overriden
    }
    resetAndRemove() {
        this.toResetAndRemove = true;
    }
}
export class CompositeAnimation {
    constructor() {
        this.handles = new Set();
    }
    add(animation) {
        const handle = new AnimationWrapper(animation);
        handle.remove = () => {
            this.handles.delete(handle);
        };
        this.handles.add(handle);
        return handle;
    }
    play(player, time) {
        this.handles.forEach(handle => handle.play(player, time));
    }
}
export class RootAnimation extends CompositeAnimation {
    constructor() {
        super(...arguments);
        this.speed = 1.0;
        this.progress = 0.0;
        this.clock = new Clock(true);
    }
    get animation() {
        return this;
    }
    get paused() {
        return !this.clock.running;
    }
    set paused(value) {
        if (value) {
            this.clock.stop();
        }
        else {
            this.clock.start();
        }
    }
    runAnimationLoop(player) {
        if (this.handles.size === 0) {
            return;
        }
        this.progress += this.clock.getDelta() * this.speed;
        this.play(player, this.progress);
    }
    reset() {
        this.progress = 0;
    }
}
export const WalkingAnimation = (player, time) => {
    const skin = player.skin;
    // Multiply by animation's natural speed
    time *= 8;
    // Leg swing
    skin.leftLeg.rotation.x = Math.sin(time) * 0.5;
    skin.rightLeg.rotation.x = Math.sin(time + Math.PI) * 0.5;
    // Arm swing
    skin.leftArm.rotation.x = Math.sin(time + Math.PI) * 0.5;
    skin.rightArm.rotation.x = Math.sin(time) * 0.5;
    const basicArmRotationZ = Math.PI * 0.02;
    skin.leftArm.rotation.z = Math.cos(time) * 0.03 + basicArmRotationZ;
    skin.rightArm.rotation.z = Math.cos(time + Math.PI) * 0.03 - basicArmRotationZ;
    // Head shaking with different frequency & amplitude
    skin.head.rotation.y = Math.sin(time / 4) * 0.2;
    skin.head.rotation.x = Math.sin(time / 5) * 0.1;
    // Always add an angle for cape around the x axis
    const basicCapeRotationX = Math.PI * 0.06;
    player.cape.rotation.x = Math.sin(time / 1.5) * 0.06 + basicCapeRotationX;
};
export const RunningAnimation = (player, time) => {
    const skin = player.skin;
    time = time * 15 + Math.PI * 0.5;
    // Leg swing with larger amplitude
    skin.leftLeg.rotation.x = Math.cos(time + Math.PI) * 1.3;
    skin.rightLeg.rotation.x = Math.cos(time) * 1.3;
    // Arm swing
    skin.leftArm.rotation.x = Math.cos(time) * 1.5;
    skin.rightArm.rotation.x = Math.cos(time + Math.PI) * 1.5;
    const basicArmRotationZ = Math.PI * 0.1;
    skin.leftArm.rotation.z = Math.cos(time) * 0.1 + basicArmRotationZ;
    skin.rightArm.rotation.z = Math.cos(time + Math.PI) * 0.1 - basicArmRotationZ;
    // Jumping
    player.position.y = Math.cos(time * 2);
    // Dodging when running
    player.position.x = Math.cos(time) * 0.15;
    // Slightly tilting when running
    player.rotation.z = Math.cos(time + Math.PI) * 0.01;
    // Apply higher swing frequency, lower amplitude,
    // and greater basic rotation around x axis,
    // to cape when running.
    const basicCapeRotationX = Math.PI * 0.3;
    player.cape.rotation.x = Math.sin(time * 2) * 0.1 + basicCapeRotationX;
    // What about head shaking?
    // You shouldn't glance right and left when running dude :P
};
export const RotatingAnimation = (player, time) => {
    player.rotation.y = time;
};
function clamp(num, min, max) {
    return num <= min ? min : num >= max ? max : num;
}
export const FlyingAnimation = (player, time) => {
    // body rotation finishes in 0.5s
    // elytra expansion finishes in 3.3s
    if (time < 0)
        time = 0;
    time *= 20;
    const startProgress = clamp((time * time) / 100, 0, 1);
    player.rotation.x = startProgress * Math.PI / 2;
    player.skin.head.rotation.x = startProgress > .5 ? Math.PI / 4 - player.rotation.x : 0;
    const basicArmRotationZ = Math.PI * .25 * startProgress;
    player.skin.leftArm.rotation.z = basicArmRotationZ;
    player.skin.rightArm.rotation.z = -basicArmRotationZ;
    const elytraRotationX = .34906584;
    const elytraRotationZ = Math.PI / 2;
    const interpolation = Math.pow(.9, time);
    player.elytra.leftWing.rotation.x = elytraRotationX + interpolation * (.2617994 - elytraRotationX);
    player.elytra.leftWing.rotation.z = elytraRotationZ + interpolation * (.2617994 - elytraRotationZ);
    player.elytra.updateRightWing();
};
//# sourceMappingURL=animation.js.map