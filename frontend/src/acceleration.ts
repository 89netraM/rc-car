import { isGamepadInputEnabled, isKeyboardInputEnabled, isMobileInputEnabled } from "./inputMode";

const accelerationInputElement = document.getElementById("acceleration") as HTMLInputElement;
accelerationInputElement.addEventListener("input", () => {
    if (!isMobileInputEnabled()) {
        return;
    }
    setAcceleration(accelerationInputElement.valueAsNumber);
});
accelerationInputElement.addEventListener("change", () => {
    if (!isMobileInputEnabled()) {
        return;
    }
    resetAcceleration();
});

let gamepadLoop: number | null = null;
const accelerationsByGamepad: { [id: string]: number } = {};
window.addEventListener("gamepadconnected", e => {
    if (gamepadLoop != null) {
        return;
    }

    readGamepad(e.gamepad);
    gamepadLoop = setInterval(readGamepads, 20);
});
window.addEventListener("gamepaddisconnected", e => {
    delete accelerationsByGamepad[e.gamepad.id];

    if (gamepadLoop == null || navigator.getGamepads().length > 0) {
        return;
    }

    clearInterval(gamepadLoop);
    gamepadLoop = null;
    resetAcceleration();
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
    const prevAcceleration = accelerationsByGamepad[gamepad.id];
    const acceleration = gamepad.buttons[7].value - gamepad.buttons[6].value;
    if (Math.abs(prevAcceleration - acceleration) < 0.01) {
        return false;
    }
    accelerationsByGamepad[gamepad.id] = acceleration;
    setAcceleration(acceleration);
    return true;
}

let keyboardAcceleration = 0;
let keyboardAccelerationW = false;
let keyboardAccelerationUp = false;
let keyboardAccelerationS = false;
let keyboardAccelerationDown = false;
window.addEventListener("keydown", e => {
    if (!isKeyboardInputEnabled()) {
        return;
    }
    if (e.repeat) {
        return;
    }
    switch (e.key) {
        case "w":
            keyboardAccelerationW = true;
            break;
        case "ArrowUp":
            keyboardAccelerationUp = true;
            break;
        case "s":
            keyboardAccelerationS = true;
            break;
        case "ArrowDown":
            keyboardAccelerationDown = true;
            break;
    }
    setKeyboardAcceleration();
});
window.addEventListener("keyup", e => {
    if (!isKeyboardInputEnabled()) {
        return;
    }
    switch (e.key) {
        case "w":
            keyboardAccelerationW = false;
            break;
        case "ArrowUp":
            keyboardAccelerationUp = false;
            break;
        case "s":
            keyboardAccelerationS = false;
            break;
        case "ArrowDown":
            keyboardAccelerationDown = false;
            break;
    }
    setKeyboardAcceleration();
});
function setKeyboardAcceleration(): void {
    const prevKeyboardAcceleration = keyboardAcceleration;
    keyboardAcceleration = ((keyboardAccelerationW || keyboardAccelerationUp) ? 1 : 0)
        + ((keyboardAccelerationS || keyboardAccelerationDown) ? -1 : 0);
    if (prevKeyboardAcceleration === keyboardAcceleration) {
        return;
    }
    setAcceleration(keyboardAcceleration);
}

async function resetAcceleration(): Promise<void> {
    await setAcceleration(0);
}

async function setAcceleration(acceleration: number): Promise<void> {
    accelerationInputElement.valueAsNumber = acceleration;
    await sendAccelerationValue(acceleration);
}

async function sendAccelerationValue(acceleration: number): Promise<void> {
    // TODO: Implement connection to backend
    console.log({ acceleration });
}
