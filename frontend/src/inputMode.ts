const gamepadButton = document.querySelector("#input-mode input[value=\"gamepad\"]") as HTMLInputElement;
const mobileButton = document.querySelector("#input-mode input[value=\"mobile\"]") as HTMLInputElement;
const keyboardButton = document.querySelector("#input-mode input[value=\"keyboard\"]") as HTMLInputElement;

export function isGamepadInputEnabled(): boolean {
    return gamepadButton.checked;
}

export function isMobileInputEnabled(): boolean {
    return mobileButton.checked;
}

export function isKeyboardInputEnabled(): boolean {
    return keyboardButton.checked;
}
