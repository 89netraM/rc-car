import { HubConnectionBuilder } from "@microsoft/signalr";
import { createAlert } from "./alerts";

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

connection.start().catch((err: Error) =>{
    createAlert(err.message, null);
});

export async function sendAccelerationToController(acceleration: number): Promise<void> {
    try {
        await connection.send("setAcceleration", { Acceleration: acceleration });
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
