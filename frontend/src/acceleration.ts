const accelerationInputElement = document.getElementById("acceleration") as HTMLInputElement;
accelerationInputElement.addEventListener("input", () => setAcceleration(accelerationInputElement.valueAsNumber));
accelerationInputElement.addEventListener("change", () => resetAcceleration());

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
