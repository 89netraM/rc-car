import { isGamepadInputEnabled, isKeyboardInputEnabled, isMobileInputEnabled } from "./inputMode";

const cameraImg = document.querySelector(".camera img") as HTMLImageElement;

let prevSensorSteering: number | null = null;
window.addEventListener("deviceorientation", e => {
    if (!isMobileInputEnabled()) {
        return;
    }
    if (e.gamma == null || e.beta == null) {
        return;
    }

    let angle = e.gamma < 0
        ? e.beta
        : 180 - e.beta;
    angle = angle > 180 ? angle - 360 : angle;
    cameraImg.style.transform = `rotateZ(${-angle}deg)`;

    const steering = Math.max(-45, Math.min(angle, 45)) / 45;
    if (prevSensorSteering != null && Math.abs(prevSensorSteering - steering) < 0.01) {
        return;
    }
    prevSensorSteering = steering;
    setSteering(steering);
});

let gamepadLoop: number | null = null;
const steeringByGamepad: { [id: string]: number } = {};
window.addEventListener("gamepadconnected", e => {
    if (gamepadLoop != null) {
        return;
    }

    readGamepad(e.gamepad);
    gamepadLoop = setInterval(readGamepads, 20);
});
window.addEventListener("gamepaddisconnected", e => {
    delete steeringByGamepad[e.gamepad.id];

    if (gamepadLoop == null || navigator.getGamepads().length > 0) {
        return;
    }

    clearInterval(gamepadLoop);
    gamepadLoop = null;
    resetSteering();
});
function readGamepads(): void {
    if (!isGamepadInputEnabled()) {
        return;
    }
    for (const gamepad of navigator.getGamepads()) {
        if (gamepad == null) {
            continue;
        }
        const didUpdate = readGamepad(gamepad);
        if (didUpdate) {
            break;
        }
    }
}
function readGamepad(gamepad: Gamepad): boolean {
    const prevSteering = steeringByGamepad[gamepad.id];
    const steering = gamepad.axes[0];
    if (Math.abs(prevSteering - steering) < 0.01) {
        return false;
    }
    steeringByGamepad[gamepad.id] = steering;
    setSteering(steering);
    return true;
}

let keyboardSteering = 0;
let keyboardSteeringA = false;
let keyboardSteeringLeft = false;
let keyboardSteeringD = false;
let keyboardSteeringRight = false;
window.addEventListener("keydown", e => {
    if (!isKeyboardInputEnabled()) {
        return;
    }
    if (e.repeat) {
        return;
    }
    switch (e.key) {
        case "a":
            keyboardSteeringA = true;
            break;
        case "ArrowLeft":
            keyboardSteeringLeft = true;
            break;
        case "d":
            keyboardSteeringD = true;
            break;
        case "ArrowRight":
            keyboardSteeringRight = true;
            break;
    }
    setKeyboardSteering();
});
window.addEventListener("keyup", e => {
    if (!isKeyboardInputEnabled()) {
        return;
    }
    switch (e.key) {
        case "a":
            keyboardSteeringA = false;
            break;
        case "ArrowLeft":
            keyboardSteeringLeft = false;
            break;
        case "d":
            keyboardSteeringD = false;
            break;
        case "ArrowRight":
            keyboardSteeringRight = false;
            break;
    }
    setKeyboardSteering();
});
function setKeyboardSteering(): void {
    const prevKeyboardSteering = keyboardSteering;
    keyboardSteering = ((keyboardSteeringA || keyboardSteeringLeft) ? -1 : 0)
        + ((keyboardSteeringD || keyboardSteeringRight) ? 1 : 0);
    if (prevKeyboardSteering === keyboardSteering) {
        return;
    }
    setSteering(keyboardSteering);
}

async function resetSteering(): Promise<void> {
    await setSteering(0);
}

async function setSteering(steering: number): Promise<void> {
    await sendSteeringValue(steering);
}

async function sendSteeringValue(steering: number): Promise<void> {
    // TODO: Implement connection to backend
    console.log({ steering });
}
