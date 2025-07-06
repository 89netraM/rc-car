import { HubConnectionBuilder } from "@microsoft/signalr";
import { createAlert } from "./alerts";
import { updateBackDistance } from "./distance";

const connection = new HubConnectionBuilder()
    .withUrl("/controller")
    .build();

const reconnectionAlerts = new Array<() => void>();
connection.onreconnecting(err => {
    const dismiss = createAlert(err?.message ?? "Reconnecting to server...", null);
    reconnectionAlerts.push(dismiss);
});
connection.onreconnected(_ => {
    for (let dismiss; dismiss = reconnectionAlerts.pop();) {
        dismiss();
    }
    createAlert("Reconnected!", 2000);
});
connection.onclose(err => {
    createAlert(err?.message ?? "Connection to server closed", null);
});

connection.on("updateDistance", arg => {
    if (!("distance" in arg) || !(typeof arg.distance === "number" || arg.distance == null)) {
        createAlert("Received distance update without numeric distance property.");
    }
    updateBackDistance(arg.distance);
});

connection.start().catch((err: Error) =>{
    createAlert(err.message, null);
});

export async function sendAccelerationToController(acceleration: number): Promise<void> {
    try {
        await connection.send("setAcceleration", { acceleration });
    } catch (e) {
        createAlert((e as Error)?.message ?? e);
    }
}

export async function sendSteeringToController(steering: number): Promise<void> {
    try {
        await connection.send("setSteering", { steering });
    } catch (e) {
        createAlert((e as Error)?.message ?? e);
    }
}

export async function sendHornToController(active: boolean): Promise<void> {
    try {
        await connection.send("setHorn", { active });
    } catch (e) {
        createAlert((e as Error)?.message ?? e);
    }
}
