const deadZone = 0.1;
const scale = 1.0 - deadZone;

export function scaleGamepadAxis(value: number): number {
    if (value < 0) {
        return Math.max(0, Math.max((value + deadZone) / scale, 0));
    }
    return Math.max(0, Math.max((value - deadZone) / scale, 0));
}
