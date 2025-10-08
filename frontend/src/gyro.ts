const deadZone = 0.1;
const scale = 1.0 - deadZone;

export function scaleGyroInput(value: number): number {
    return Math.sign(value) * Math.max((Math.abs(value) - deadZone) / scale, 0);
}
