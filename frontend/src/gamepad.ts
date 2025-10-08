const deadZone = 0.25;
const scale = 1.0 - deadZone;

export function scaleGamepadAxis(value: number): number {
    return Math.sign(value) * Math.max((Math.abs(value) - deadZone) / scale, 0);
}
