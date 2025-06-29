const cameraImg = document.querySelector(".camera img") as HTMLImageElement;

let prevSensorSteering: number | null = null;
window.addEventListener("deviceorientation", e => {
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
