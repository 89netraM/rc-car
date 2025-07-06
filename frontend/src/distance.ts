const text = document.getElementById("distance") as HTMLSpanElement;

export function updateBackDistance(distance: number | null): void {
    if (distance == null) {
        text.style.display = "none";
        return;
    }
    text.style.display = "inline";
    text.innerText = distance.toFixed(3);
}
