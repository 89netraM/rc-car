import { sendHornToController } from "./controller";
import { isGamepadInputEnabled, isKeyboardInputEnabled } from "./inputMode";

const hornInputButton = document.getElementById("horn") as HTMLButtonElement;
hornInputButton.addEventListener("touchstart", e => {
    e.preventDefault();
    setHorn(true);
});
hornInputButton.addEventListener("touchcancel", handleTouchEnd);
hornInputButton.addEventListener("touchend", handleTouchEnd);
function handleTouchEnd(e: TouchEvent): void {
    e.preventDefault();
    setHorn(false);
}

let gamepadLoop: number | null = null;
const hornByGamepad: { [id: string]: boolean } = {};
window.addEventListener("gamepadconnected", e => {
    if (gamepadLoop != null) {
        return;
    }

    readGamepad(e.gamepad);
    gamepadLoop = setInterval(readGamepads, 20);
});
window.addEventListener("gamepaddisconnected", e => {
    delete hornByGamepad[e.gamepad.id];

    if (gamepadLoop == null || navigator.getGamepads().length > 0) {
        return;
    }

    clearInterval(gamepadLoop);
    gamepadLoop = null;
    resetHorn();
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
    const prevHorn = hornByGamepad[gamepad.id];
    const horn = gamepad.buttons[0].pressed;
    if (prevHorn === horn) {
        return false;
    }
    hornByGamepad[gamepad.id] = horn;
    setHorn(horn);
    return true;
}

window.addEventListener("keydown", e => {
    if (!isKeyboardInputEnabled()) {
        return;
    }
    if (e.repeat) {
        return;
    }
    if (e.key === " ") {
        setHorn(true);
    }
});
window.addEventListener("keyup", e => {
    if (!isKeyboardInputEnabled()) {
        return;
    }
    if (e.key === " ") {
        setHorn(false);
    }
});

async function resetHorn(): Promise<void> {
    await setHorn(false);
}

async function setHorn(horn: boolean): Promise<void> {
    await sendHornValue(horn);
}

async function sendHornValue(horn: boolean): Promise<void> {
    await sendHornToController(horn);
}
